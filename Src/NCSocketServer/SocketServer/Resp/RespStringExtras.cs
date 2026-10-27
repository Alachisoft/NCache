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
using System.Text;
using Alachisoft.NCache.SocketServer.Resp.Protocol;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// String commands that read or rewrite part of a value, plus the absolute-expiry readers.
    /// <para>
    /// None of these needed a new engine capability - each is a composition of a get, a set and
    /// some arithmetic. They live here because a command that NCache <em>can</em> serve must be
    /// served: "not supported" is reserved for things the engine genuinely cannot do, so that a
    /// client can trust the distinction.
    /// </para>
    /// <para>
    /// The ones that rewrite a value take the same cluster-wide lock the collection commands use,
    /// because read-then-write is not atomic on a server that handles requests in parallel.
    /// </para>
    /// </summary>
    internal static class RespStringExtras
    {
        private static readonly byte[] Empty = new byte[0];

        /// <summary>STRLEN key - 0 for a missing key, which is what Redis answers.</summary>
        public static RespCommandOutcome StrLen(RespRequest request, RespWriter writer, RespCacheGateway gateway)
        {
            if (request.ArgCount != 1) return WrongArgs(writer, "strlen");

            Alachisoft.NCache.Common.Caching.EntryType? type;
            byte[] value = gateway.Get(Text(request.GetArg(0)), out type);

            writer.WriteInteger(value == null ? 0 : value.Length);
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// GETRANGE key start end - inclusive, with negative offsets counting from the end. An
        /// inverted or out-of-range window is an empty string, never an error.
        /// </summary>
        public static RespCommandOutcome GetRange(RespRequest request, RespWriter writer, RespCacheGateway gateway)
        {
            if (request.ArgCount != 3) return WrongArgs(writer, "getrange");

            long rawStart, rawEnd;
            if (!TryLong(request.GetArg(1), out rawStart) || !TryLong(request.GetArg(2), out rawEnd))
                return NotAnInteger(writer);

            byte[] value = gateway.Get(Text(request.GetArg(0)));
            if (value == null || value.Length == 0) { writer.WriteBulkString(Empty); return RespCommandOutcome.Continue; }

            int start = Normalise(rawStart, value.Length);
            int end = Normalise(rawEnd, value.Length);

            if (start < 0) start = 0;
            if (end >= value.Length) end = value.Length - 1;

            if (start > end || start >= value.Length)
            {
                writer.WriteBulkString(Empty);
                return RespCommandOutcome.Continue;
            }

            var slice = new byte[end - start + 1];
            Buffer.BlockCopy(value, start, slice, 0, slice.Length);
            writer.WriteBulkString(slice);
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// APPEND key value - adds bytes to the end of the value, creating the key when absent, and
        /// returns the resulting length.
        /// <para>
        /// Read-modify-write, so it holds the key lease; see <see cref="RespKeyLock"/> for what that
        /// does and does not guarantee.
        /// </para>
        /// </summary>
        public static RespCommandOutcome Append(RespRequest request, RespWriter writer, RespCacheGateway gateway)
        {
            if (request.ArgCount != 2) return WrongArgs(writer, "append");

            string key = Text(request.GetArg(0));
            byte[] tail = request.GetArg(1) ?? Empty;
            int length;

            // Unconditional, and it is what makes the lease usable: NCache will not lock a key that
            // does not exist, so without this the very first APPENDs to a new key all run unlocked
            // and lose each other's writes - measured, 30 concurrent appends produced 28 bytes.
            // Redis creates the key here regardless (APPEND k "" leaves EXISTS at 1), so this costs
            // no correctness, only one engine call.
            EnsureExists(gateway, key);

            using (RespKeyLock.Acquire(gateway.CacheOrNull, key))
            {
                byte[] head = gateway.Get(key) ?? Empty;

                var joined = new byte[head.Length + tail.Length];
                Buffer.BlockCopy(head, 0, joined, 0, head.Length);
                Buffer.BlockCopy(tail, 0, joined, head.Length, tail.Length);

                Rewrite(gateway, key, joined);
                length = joined.Length;
            }

            writer.WriteInteger(length);
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// SETRANGE key offset value - overwrites the value from <c>offset</c>, growing it and
        /// zero-filling any gap, and returns the resulting length.
        /// <para>
        /// An empty value writes nothing and reports the current length, which for a missing key is
        /// 0 and must not create it - verified against real Redis, where SETRANGE k 0 "" on an absent
        /// key leaves EXISTS at 0.
        /// </para>
        /// </summary>
        public static RespCommandOutcome SetRange(RespRequest request, RespWriter writer, RespCacheGateway gateway)
        {
            if (request.ArgCount != 3) return WrongArgs(writer, "setrange");

            long offset;
            if (!TryLong(request.GetArg(1), out offset)) return NotAnInteger(writer);
            if (offset < 0)
            {
                writer.WriteError("ERR offset is out of range");
                return RespCommandOutcome.Continue;
            }

            string key = Text(request.GetArg(0));
            byte[] patch = request.GetArg(2) ?? Empty;
            int length;

            // An empty patch writes nothing, so it is a pure read: answer it without a lease, and
            // above all without EnsureExists, because SETRANGE k 0 "" must leave an absent key absent.
            if (patch.Length == 0)
            {
                byte[] existing = gateway.Get(key) ?? Empty;
                writer.WriteInteger(existing.Length);
                return RespCommandOutcome.Continue;
            }

            // See the note in Append: the key has to exist before it can be leased.
            EnsureExists(gateway, key);

            using (RespKeyLock.Acquire(gateway.CacheOrNull, key))
            {
                byte[] current = gateway.Get(key) ?? Empty;

                int needed = (int)Math.Max(current.Length, offset + patch.Length);
                var result = new byte[needed];               // zero-filled, which is the padding
                Buffer.BlockCopy(current, 0, result, 0, current.Length);
                Buffer.BlockCopy(patch, 0, result, (int)offset, patch.Length);

                Rewrite(gateway, key, result);
                length = result.Length;
            }

            writer.WriteInteger(length);
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// Creates the key with an empty value if it is absent, so a lease can be taken on it.
        /// <para>
        /// Atomic: the engine itself reports KEY_ALREADY_EXISTS rather than this checking first and
        /// racing, so two clients creating the same key concurrently cannot both win. An existing
        /// key is left exactly as it was, expiry included.
        /// </para>
        /// </summary>
        private static void EnsureExists(RespCacheGateway gateway, string key)
        {
            try { gateway.Add(key, Empty, null); }
            catch (Exception e) { RespLog.Detail("RespStringExtras.EnsureExists", key + ": " + e.Message); }
        }

        /// <summary>
        /// Stores a rewritten value, keeping whatever expiry the key already had.
        /// <para>
        /// The TTL has to be carried across by hand, and this is the part that is easy to get wrong:
        /// passing a null expiration to Set does NOT preserve it. Set replaces the whole entry
        /// including its expiration hint, which is right for a plain SET - Redis discards the TTL
        /// there - and wrong for APPEND and SETRANGE, where Redis keeps it. Confirmed against
        /// redis:8-alpine: SET k v EX 100 then SETRANGE k 2 XX leaves TTL at 99, not -1.
        /// </para>
        /// <para>
        /// The read of the remaining TTL and the write happen inside the caller's lease, so the
        /// window in which a concurrent EXPIRE could be lost is the lease's, not unbounded.
        /// </para>
        /// </summary>
        private static void Rewrite(RespCacheGateway gateway, string key, byte[] value)
        {
            long remaining = gateway.GetTtlMilliseconds(key);

            gateway.Set(key, value, null);

            if (remaining >= 0)
            {
                gateway.AddExpiration(key, gateway.CreateAbsoluteExpiration(
                    DateTime.UtcNow.AddMilliseconds(remaining)));
            }
        }

        /// <summary>
        /// EXPIRETIME / PEXPIRETIME - the absolute moment a key expires, rather than the remaining
        /// time. Derived from the remaining TTL, which is what the engine reports.
        /// <para>
        /// -2 means the key does not exist, -1 means it exists with no expiry. Both are Redis's own
        /// sentinels and clients branch on them.
        /// </para>
        /// </summary>
        public static RespCommandOutcome ExpireTime(
            RespRequest request, RespWriter writer, RespCacheGateway gateway, long divisor)
        {
            if (request.ArgCount != 1) return WrongArgs(writer, divisor == 1000L ? "expiretime" : "pexpiretime");

            string key = Text(request.GetArg(0));

            long remaining = gateway.GetTtlMilliseconds(key);
            if (remaining == -2 || remaining == -1)
            {
                writer.WriteInteger(remaining);
                return RespCommandOutcome.Continue;
            }

            long absoluteMs = NowMs() + remaining;
            writer.WriteInteger(divisor == 1000L ? absoluteMs / 1000L : absoluteMs);
            return RespCommandOutcome.Continue;
        }

        #region Helpers

        private static long NowMs()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        private static int Normalise(long index, int length)
        {
            long resolved = index < 0 ? length + index : index;
            if (resolved < int.MinValue) return int.MinValue;
            if (resolved > int.MaxValue) return int.MaxValue;
            return (int)resolved;
        }

        private static bool TryLong(byte[] value, out long result)
        {
            result = 0;
            if (value == null || value.Length == 0) return false;
            return long.TryParse(Encoding.UTF8.GetString(value), NumberStyles.Integer,
                                 CultureInfo.InvariantCulture, out result);
        }

        private static RespCommandOutcome NotAnInteger(RespWriter writer)
        {
            writer.WriteError("ERR value is not an integer or out of range");
            return RespCommandOutcome.Continue;
        }

        private static RespCommandOutcome WrongTypeError(RespWriter writer)
        {
            writer.WriteError("WRONGTYPE Operation against a key holding the wrong kind of value");
            return RespCommandOutcome.Continue;
        }

        private static RespCommandOutcome WrongArgs(RespWriter writer, string command)
        {
            writer.WriteError("ERR wrong number of arguments for '" + command + "' command");
            return RespCommandOutcome.Continue;
        }

        private static string Text(byte[] value)
        {
            return value == null ? string.Empty : Encoding.UTF8.GetString(value);
        }

        #endregion
    }
}
