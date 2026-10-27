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
    /// INCR, DECR, INCRBY, DECRBY and INCRBYFLOAT, held as plain strings.
    /// <para>
    /// NCache Open Source has no counter entry type - there is no CreateDataType and no
    /// CounterAdd anywhere in the engine - so the arithmetic is done here on a string value under
    /// the key lease, which is the one thing OSS does provide for a read-modify-write.
    /// </para>
    /// <para>
    /// That is not a compromise. <b>Redis has no counter type either</b>: INCR and GET address one
    /// and the same string, so <c>SET k 5</c> followed by <c>INCR k</c> answers 6, and
    /// <c>GET k</c> after an <c>INCR</c> answers the digits. The Enterprise build keeps counters as
    /// their own entry type and pays for it - a key a SET created has to be adopted into a counter
    /// before the first increment can land, and that adoption is a delete and a recreate. Here
    /// there is nothing to adopt, so the awkward case does not exist and the behaviour is closer to
    /// Redis rather than further from it.
    /// </para>
    /// <para>
    /// What is given up is the engine's atomic increment. Every increment here is a read, an add
    /// and a write under the lease, so it is atomic against other commands in this listener and not
    /// against a native NCache client writing the same key. Enterprise is atomic from the second
    /// increment onwards; this is never atomic in that sense. A client that needs a hard counter
    /// across mixed clients wants the Enterprise build.
    /// </para>
    /// </summary>
    internal static class RespCounters
    {
        /// <summary>
        /// Redis's own rule for a stored integer: optional sign then digits, nothing else.
        /// <para>
        /// Deliberately stricter than the parser used for arguments, which tolerates surrounding
        /// whitespace - " 1" would otherwise be accepted as a base and Redis rejects it with
        /// "value is not an integer or out of range".
        /// </para>
        /// </summary>
        private static bool TryStoredLong(byte[] raw, out long value)
        {
            value = 0;
            if (raw == null || raw.Length == 0) return false;

            string text = Encoding.ASCII.GetString(raw);
            return long.TryParse(text, NumberStyles.AllowLeadingSign,
                                 CultureInfo.InvariantCulture, out value);
        }

        private static bool TryArgumentLong(byte[] raw, out long value)
        {
            value = 0;
            if (raw == null || raw.Length == 0) return false;

            return long.TryParse(Encoding.ASCII.GetString(raw), NumberStyles.Integer,
                                 CultureInfo.InvariantCulture, out value);
        }

        /// <summary>INCR / DECR / INCRBY / DECRBY.</summary>
        /// <param name="fixedDelta">The step for INCR and DECR; ignored when <paramref name="takesDelta"/>.</param>
        /// <param name="takesDelta">True for INCRBY and DECRBY, which read the step from the command.</param>
        /// <param name="negate">True for DECRBY, whose argument is subtracted.</param>
        public static RespCommandOutcome IncrBy(
            RespRequest request, RespWriter writer, RespCacheGateway gateway,
            string command, long fixedDelta, bool takesDelta, bool negate)
        {
            int expected = takesDelta ? 2 : 1;
            if (request.ArgCount != expected) return WrongArgs(writer, command);

            long delta = fixedDelta;
            if (takesDelta)
            {
                if (!TryArgumentLong(request.GetArg(1), out delta)) return NotAnInteger(writer);
                if (negate)
                {
                    // long.MinValue has no positive counterpart, so negating it overflows. Redis
                    // answers "decrement would overflow" rather than wrapping to a negative total.
                    if (delta == long.MinValue) return OutOfRange(writer);
                    delta = -delta;
                }
            }

            string key = KeyOf(request, 0);

            using (gateway.Lock(key))
            {
                byte[] current = gateway.Get(key);

                long value = 0;
                if (current != null && current.Length > 0 && !TryStoredLong(current, out value))
                    return NotAnInteger(writer);

                long result;
                try
                {
                    result = checked(value + delta);
                }
                catch (OverflowException)
                {
                    return OutOfRange(writer);
                }

                // The TTL survives the increment, as it does in Redis - only the value changes.
                // Set replaces the whole entry including its expiration, so the remaining time is
                // read and reapplied by hand, the same hand-off APPEND and SETRANGE make.
                long remaining = gateway.GetTtlMilliseconds(key);
                gateway.Set(key, Digits(result), null);
                if (remaining > 0)
                {
                    gateway.AddExpiration(
                        key, gateway.CreateAbsoluteExpiration(DateTime.UtcNow.AddMilliseconds(remaining)));
                }

                writer.WriteInteger(result);
            }

            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// INCRBYFLOAT key increment
        /// <para>
        /// Stored as a string like every other value here, so unlike the Enterprise build there is
        /// no counter to convert out of first.
        /// </para>
        /// </summary>
        public static RespCommandOutcome IncrByFloat(
            RespRequest request, RespWriter writer, RespCacheGateway gateway)
        {
            if (request.ArgCount != 2) return WrongArgs(writer, "incrbyfloat");

            double delta;
            if (!TryDouble(request.GetArg(1), out delta)) return NotAFloat(writer);

            string key = KeyOf(request, 0);
            string formatted;

            using (gateway.Lock(key))
            {
                byte[] current = gateway.Get(key);

                double value = 0;
                if (current != null && current.Length > 0 && !TryDouble(current, out value))
                    return NotAFloat(writer);

                double result = value + delta;
                if (double.IsNaN(result) || double.IsInfinity(result))
                {
                    writer.WriteError("ERR increment would produce NaN or Infinity");
                    return RespCommandOutcome.Continue;
                }

                formatted = FormatDouble(result);

                long remaining = gateway.GetTtlMilliseconds(key);
                gateway.Set(key, Encoding.UTF8.GetBytes(formatted), null);
                if (remaining > 0)
                {
                    gateway.AddExpiration(
                        key, gateway.CreateAbsoluteExpiration(DateTime.UtcNow.AddMilliseconds(remaining)));
                }
            }

            writer.WriteBulkString(Encoding.UTF8.GetBytes(formatted));
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// MSETNX key value [key value ...] - writes every pair, or none.
        /// <para>
        /// Each pair is attempted with the engine's own "only if absent", and on the first refusal
        /// the keys already written are removed again. The guarantee comes from that rollback
        /// rather than from atomicity, so a reader during the window can see some of the keys - a
        /// sentinel key would close it and would itself be visible in the keyspace, which is worse.
        /// </para>
        /// </summary>
        public static RespCommandOutcome MSetNx(
            RespRequest request, RespWriter writer, RespCacheGateway gateway)
        {
            if (request.ArgCount < 2 || request.ArgCount % 2 != 0) return WrongArgs(writer, "msetnx");

            int pairs = request.ArgCount / 2;

            // A key named twice in one call is written once, with its last value - Redis sets the
            // pairs in order, so the later one wins. Folding duplicates here also keeps the
            // rollback honest: the second insert of the same key used to fail the add-if-absent
            // and report "the key already existed", so MSETNX answered 0 for a call that should
            // have succeeded, and then deleted what it had just written.
            var order = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
            var keys = new System.Collections.Generic.List<string>(pairs);
            var values = new System.Collections.Generic.List<byte[]>(pairs);

            for (int i = 0; i < pairs; i++)
            {
                string name = KeyOf(request, i * 2);
                byte[] value = request.GetArg(i * 2 + 1) ?? new byte[0];

                int at;
                if (order.TryGetValue(name, out at)) { values[at] = value; continue; }

                order[name] = keys.Count;
                keys.Add(name);
                values.Add(value);
            }

            // Anything already present means the whole call reports 0 and writes nothing. Checking
            // first narrows the rollback window; the add-if-absent below is what actually decides.
            foreach (string name in keys)
            {
                if (gateway.Contains(name))
                {
                    writer.WriteInteger(0);
                    return RespCommandOutcome.Continue;
                }
            }

            var written = new System.Collections.Generic.List<string>(keys.Count);
            bool collided = false;

            for (int i = 0; i < keys.Count; i++)
            {
                if (!gateway.Add(keys[i], values[i], null))
                {
                    collided = true;
                    break;
                }

                written.Add(keys[i]);
            }

            if (collided)
            {
                foreach (string key in written) gateway.Delete(key);
                writer.WriteInteger(0);
                return RespCommandOutcome.Continue;
            }

            writer.WriteInteger(1);
            return RespCommandOutcome.Continue;
        }

        // ---------------------------------------------------------------- helpers

        private static byte[] Digits(long value)
        {
            return Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
        }

        private static bool TryDouble(byte[] raw, out double value)
        {
            value = 0;
            if (raw == null || raw.Length == 0) return false;

            string text = Encoding.ASCII.GetString(raw).Trim();

            // Redis accepts these spellings for an infinite operand even though it refuses an
            // infinite result.
            if (text == "inf" || text == "+inf") { value = double.PositiveInfinity; return true; }
            if (text == "-inf") { value = double.NegativeInfinity; return true; }

            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>Redis's shortest round-trip form, with no trailing zeros.</summary>
        internal static string FormatDouble(double value)
        {
            string text = value.ToString("R", CultureInfo.InvariantCulture);

            if (text.IndexOf('E') >= 0 || text.IndexOf('e') >= 0)
                text = value.ToString("F17", CultureInfo.InvariantCulture);

            if (text.IndexOf('.') >= 0)
            {
                text = text.TrimEnd('0');
                if (text.EndsWith(".", StringComparison.Ordinal))
                    text = text.Substring(0, text.Length - 1);
            }

            return text;
        }

        private static string KeyOf(RespRequest request, int index)
        {
            return Encoding.UTF8.GetString(request.GetArg(index));
        }

        private static RespCommandOutcome NotAnInteger(RespWriter writer)
        {
            writer.WriteError("ERR value is not an integer or out of range");
            return RespCommandOutcome.Continue;
        }

        private static RespCommandOutcome OutOfRange(RespWriter writer)
        {
            writer.WriteError("ERR increment or decrement would overflow");
            return RespCommandOutcome.Continue;
        }

        private static RespCommandOutcome NotAFloat(RespWriter writer)
        {
            writer.WriteError("ERR value is not a valid float");
            return RespCommandOutcome.Continue;
        }

        private static RespCommandOutcome WrongArgs(RespWriter writer, string command)
        {
            writer.WriteError("ERR wrong number of arguments for '" + command + "' command");
            return RespCommandOutcome.Continue;
        }
    }
}
