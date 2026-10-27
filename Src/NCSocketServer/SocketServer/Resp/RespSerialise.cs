//  Copyright (c) 2018 Alachisoft
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Alachisoft.NCache.SocketServer.Resp.Protocol;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// DUMP and RESTORE, and the LCS string comparison.
    /// <para>
    /// The payload is NOT Redis's RDB serialisation and deliberately does not pretend to be. It
    /// carries its own magic, so a real Redis RESTORE rejects it loudly rather than misreading it,
    /// and a payload from a real Redis is rejected here for the same reason. A DUMP taken from this
    /// server restores to this server; it does not move data to or from Redis. Claiming RDB
    /// compatibility and getting a byte wrong somewhere in it would corrupt data silently, which is
    /// a far worse failure than an honest refusal.
    /// </para>
    /// <para>
    /// There is one type tag because NCache Open Source has one type. The Enterprise build writes a
    /// tag per data type and a per-type body; when OSS gains collections, this is where that goes.
    /// </para>
    /// </summary>
    internal static class RespSerialise
    {
        /// <summary>"NCRP" - so a payload from somewhere else is rejected rather than misread.</summary>
        private static readonly byte[] Magic = { (byte)'N', (byte)'C', (byte)'R', (byte)'P' };

        private const byte FormatVersion = 1;

        /// <summary>The only type OSS stores. Values above this are reserved for collections.</summary>
        private const byte TypeString = 1;

        /// <summary>
        /// The largest pair of values LCS will compare, counted in cells of the table it builds.
        /// <para>
        /// The cost of LCS is the PRODUCT of the two lengths, not either one on its own, so a limit
        /// on each value separately would bound nothing: two values of 64 KB each would ask for a
        /// table of 4.29 billion cells. 4,194,304 cells is 16 MB as int[] and a few milliseconds -
        /// two values of 2 KB, or 4 KB against 1 KB - which is well beyond what LCS is used for.
        /// </para>
        /// </summary>
        private const long LcsMaxCells = 4L * 1024 * 1024;

        // ---------------------------------------------------------------- DUMP

        public static RespCommandOutcome Dump(
            RespRequest request, RespWriter writer, RespCacheGateway gateway)
        {
            if (request.ArgCount != 1) return WrongArgs(writer, "dump");

            string key = Text(request.GetArg(0));
            byte[] value = gateway.Get(key);

            if (value == null)
            {
                writer.WriteNull();
                return RespCommandOutcome.Continue;
            }

            using (var buffer = new MemoryStream())
            {
                buffer.Write(Magic, 0, Magic.Length);
                buffer.WriteByte(FormatVersion);
                buffer.WriteByte(TypeString);

                byte[] length = BitConverter.GetBytes(value.Length);
                buffer.Write(length, 0, length.Length);
                buffer.Write(value, 0, value.Length);

                byte[] body = buffer.ToArray();
                byte[] crc = BitConverter.GetBytes(Crc32(body, 0, body.Length));

                var payload = new byte[body.Length + crc.Length];
                Buffer.BlockCopy(body, 0, payload, 0, body.Length);
                Buffer.BlockCopy(crc, 0, payload, body.Length, crc.Length);

                writer.WriteBulkString(payload);
            }

            return RespCommandOutcome.Continue;
        }

        // ---------------------------------------------------------------- RESTORE

        /// <summary>RESTORE key ttl payload [REPLACE] [ABSTTL] [IDLETIME n] [FREQ n]</summary>
        public static RespCommandOutcome Restore(
            RespRequest request, RespWriter writer, RespCacheGateway gateway)
        {
            if (request.ArgCount < 3) return WrongArgs(writer, "restore");

            string key = Text(request.GetArg(0));

            long ttl;
            if (!TryLong(request.GetArg(1), out ttl)) return NotAnInteger(writer);
            if (ttl < 0)
            {
                writer.WriteError("ERR Invalid TTL value, must be >= 0");
                return RespCommandOutcome.Continue;
            }

            bool replace = false, absoluteTtl = false;
            for (int i = 3; i < request.ArgCount; i++)
            {
                if (request.ArgEquals(i, "REPLACE")) replace = true;
                else if (request.ArgEquals(i, "ABSTTL")) absoluteTtl = true;
                // Accepted and ignored: NCache tracks an access counter only under LFU eviction and
                // a last-access time only under the legacy LRU policy, so neither can be honoured -
                // and refusing them would break clients that always send them.
                else if (request.ArgEquals(i, "IDLETIME") && i + 1 < request.ArgCount) i++;
                else if (request.ArgEquals(i, "FREQ") && i + 1 < request.ArgCount) i++;
                else return SyntaxError(writer);
            }

            if (!replace && gateway.Contains(key))
            {
                writer.WriteError("BUSYKEY Target key name already exists.");
                return RespCommandOutcome.Continue;
            }

            byte[] payload = request.GetArg(2);
            string problem;
            byte[] value = Unpack(payload, out problem);
            if (problem != null)
            {
                writer.WriteError("ERR " + problem);
                return RespCommandOutcome.Continue;
            }

            DateTime? deadline = null;
            if (ttl > 0)
            {
                deadline = absoluteTtl
                    ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ttl)
                    : DateTime.UtcNow.AddMilliseconds(ttl);

                // An ABSTTL that has already passed restores a key that is expired the moment it
                // exists, and Redis answers OK with nothing there. Writing it with a deadline in
                // the past left it readable here until the next expiration sweep, so the key
                // outlived its own TTL; removing it is the state the client is promised.
                if (deadline.Value <= DateTime.UtcNow)
                {
                    gateway.Delete(key);
                    writer.WriteSimpleString("OK");
                    return RespCommandOutcome.Continue;
                }
            }

            gateway.Set(key, value, deadline.HasValue
                ? gateway.CreateAbsoluteExpiration(deadline.Value)
                : null);

            writer.WriteSimpleString("OK");
            return RespCommandOutcome.Continue;
        }

        /// <summary>Validates the envelope and returns the value, or sets <paramref name="problem"/>.</summary>
        private static byte[] Unpack(byte[] payload, out string problem)
        {
            problem = null;

            const int header = 4 + 1 + 1 + 4;      // magic, version, type, length
            if (payload == null || payload.Length < header + 4)
            {
                problem = "DUMP payload version or checksum are wrong";
                return null;
            }

            for (int i = 0; i < Magic.Length; i++)
            {
                if (payload[i] != Magic[i])
                {
                    problem = "DUMP payload version or checksum are wrong";
                    return null;
                }
            }

            int bodyLength = payload.Length - 4;
            uint expected = BitConverter.ToUInt32(payload, bodyLength);
            if (Crc32(payload, 0, bodyLength) != expected)
            {
                problem = "DUMP payload version or checksum are wrong";
                return null;
            }

            int at = Magic.Length;
            if (payload[at++] != FormatVersion)
            {
                problem = "DUMP payload version or checksum are wrong";
                return null;
            }

            byte type = payload[at++];
            if (type != TypeString)
            {
                // A payload from the Enterprise build carrying a collection. Saying so is more use
                // than the generic checksum message, because the cause is a product difference
                // rather than a corrupt payload.
                problem = "DUMP payload holds a data type this build does not support";
                return null;
            }

            int length = BitConverter.ToInt32(payload, at);
            at += 4;

            if (length < 0 || at + length > bodyLength)
            {
                problem = "DUMP payload version or checksum are wrong";
                return null;
            }

            var value = new byte[length];
            Buffer.BlockCopy(payload, at, value, 0, length);
            return value;
        }

        // ---------------------------------------------------------------- LCS

        /// <summary>LCS key1 key2 [LEN] [IDX] [MINMATCHLEN n] [WITHMATCHLEN]</summary>
        public static RespCommandOutcome Lcs(
            RespRequest request, RespWriter writer, RespCacheGateway gateway)
        {
            if (request.ArgCount < 2) return WrongArgs(writer, "lcs");

            bool wantLen = false, wantIdx = false, withMatchLen = false;
            long minMatchLen = 0;

            for (int i = 2; i < request.ArgCount; i++)
            {
                if (request.ArgEquals(i, "LEN")) wantLen = true;
                else if (request.ArgEquals(i, "IDX")) wantIdx = true;
                else if (request.ArgEquals(i, "WITHMATCHLEN")) withMatchLen = true;
                else if (request.ArgEquals(i, "MINMATCHLEN") && i + 1 < request.ArgCount)
                {
                    if (!TryLong(request.GetArg(++i), out minMatchLen)) return SyntaxError(writer);
                    if (minMatchLen < 0) minMatchLen = 0;
                }
                else return SyntaxError(writer);
            }

            if (wantLen && wantIdx)
            {
                writer.WriteError("ERR If you want both the length and indexes, please just use IDX.");
                return RespCommandOutcome.Continue;
            }

            byte[] a = gateway.Get(Text(request.GetArg(0))) ?? new byte[0];
            byte[] b = gateway.Get(Text(request.GetArg(1))) ?? new byte[0];

            // In long arithmetic: 65536 * 65536 overflows an int to a negative number, so an int
            // product would pass this check on exactly the inputs it exists to refuse.
            long cells = (long)a.Length * b.Length;
            if (cells > LcsMaxCells)
            {
                writer.WriteError("ERR Strings too long for LCS (the product of the two lengths is " +
                                  cells.ToString(CultureInfo.InvariantCulture) + ", limit " +
                                  LcsMaxCells.ToString(CultureInfo.InvariantCulture) + ")");
                return RespCommandOutcome.Continue;
            }

            // LEN needs only the final number, and the recurrence reaches back exactly one row, so
            // two rows suffice and the table is never built.
            if (wantLen)
            {
                writer.WriteInteger(LcsLength(a, b));
                return RespCommandOutcome.Continue;
            }

            int rows = a.Length + 1, cols = b.Length + 1;
            var table = new int[rows, cols];
            for (int i = 1; i < rows; i++)
            {
                for (int j = 1; j < cols; j++)
                {
                    table[i, j] = a[i - 1] == b[j - 1]
                        ? table[i - 1, j - 1] + 1
                        : (table[i - 1, j] >= table[i, j - 1] ? table[i - 1, j] : table[i, j - 1]);
                }
            }

            int length = table[a.Length, b.Length];

            if (!wantIdx)
            {
                // Redis's backtrack prefers b on a tie (lcs1 > lcs2 ? i-- : j--). With >= here the
                // walk stepped a instead, which picks a different - equally long - subsequence, so
                // LCS answered "a" where Redis answers "b". The length was right either way, which
                // is what let it pass unnoticed.
                var bytes = new byte[length];
                int at = length, x = a.Length, y = b.Length;
                while (x > 0 && y > 0)
                {
                    if (a[x - 1] == b[y - 1]) { bytes[--at] = a[x - 1]; x--; y--; }
                    else if (table[x - 1, y] > table[x, y - 1]) x--;
                    else y--;
                }

                writer.WriteBulkString(bytes);
                return RespCommandOutcome.Continue;
            }

            var matches = new System.Collections.Generic.List<long[]>();
            int ai = a.Length, bi = b.Length;
            while (ai > 0 && bi > 0)
            {
                if (a[ai - 1] == b[bi - 1])
                {
                    int endA = ai - 1, endB = bi - 1;
                    while (ai > 0 && bi > 0 && a[ai - 1] == b[bi - 1]) { ai--; bi--; }

                    // Both ends are inclusive, so a range 4-7 is four bytes long, not three. The
                    // missing +1 understated every WITHMATCHLEN by one and made MINMATCHLEN drop a
                    // range that was long enough.
                    long run = endA - ai + 1;
                    if (run >= minMatchLen) matches.Add(new long[] { ai, endA, bi, endB, run });
                }
                else if (table[ai - 1, bi] > table[ai, bi - 1]) ai--;
                else bi--;
            }

            writer.WriteArrayHeader(4);
            writer.WriteBulkString(Encoding.ASCII.GetBytes("matches"));
            writer.WriteArrayHeader(matches.Count);
            foreach (long[] m in matches)
            {
                writer.WriteArrayHeader(withMatchLen ? 3 : 2);
                writer.WriteArrayHeader(2); writer.WriteInteger(m[0]); writer.WriteInteger(m[1]);
                writer.WriteArrayHeader(2); writer.WriteInteger(m[2]); writer.WriteInteger(m[3]);
                if (withMatchLen) writer.WriteInteger(m[4]);
            }
            writer.WriteBulkString(Encoding.ASCII.GetBytes("len"));
            writer.WriteInteger(length);

            return RespCommandOutcome.Continue;
        }

        /// <summary>The length alone, in O(min(m, n)) memory rather than O(m * n).</summary>
        private static int LcsLength(byte[] a, byte[] b)
        {
            if (b.Length > a.Length) { byte[] swap = a; a = b; b = swap; }

            var previous = new int[b.Length + 1];
            var current = new int[b.Length + 1];

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    current[j] = a[i - 1] == b[j - 1]
                        ? previous[j - 1] + 1
                        : (previous[j] >= current[j - 1] ? previous[j] : current[j - 1]);
                }

                int[] rotate = previous;
                previous = current;
                current = rotate;
                Array.Clear(current, 0, current.Length);
            }

            return previous[b.Length];
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Table-less CRC32, so the payload carries an integrity check without a lookup table.</summary>
        private static uint Crc32(byte[] data, int offset, int length)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = offset; i < offset + length; i++)
            {
                crc ^= data[i];
                for (int bit = 0; bit < 8; bit++)
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
            return ~crc;
        }

        private static bool TryLong(byte[] raw, out long value)
        {
            value = 0;
            if (raw == null || raw.Length == 0) return false;

            return long.TryParse(Encoding.ASCII.GetString(raw), NumberStyles.Integer,
                                 CultureInfo.InvariantCulture, out value);
        }

        private static string Text(byte[] value)
        {
            return value == null ? string.Empty : Encoding.UTF8.GetString(value);
        }

        private static RespCommandOutcome SyntaxError(RespWriter writer)
        {
            writer.WriteError("ERR syntax error");
            return RespCommandOutcome.Continue;
        }

        private static RespCommandOutcome NotAnInteger(RespWriter writer)
        {
            writer.WriteError("ERR value is not an integer or out of range");
            return RespCommandOutcome.Continue;
        }

        private static RespCommandOutcome WrongArgs(RespWriter writer, string command)
        {
            writer.WriteError("ERR wrong number of arguments for '" + command + "' command");
            return RespCommandOutcome.Continue;
        }
    }
}
