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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.SocketServer.Resp.Protocol;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// The keyspace commands that move, describe or sample a key: COPY, RENAME, RENAMENX, OBJECT
    /// and RANDOMKEY.
    /// <para>
    /// Every one of these is markedly simpler here than in the Enterprise build, and for one
    /// reason: NCache Open Source has no distributed data types, so a key holds a value and nothing
    /// else. The Enterprise versions carry a per-type replay - a hash copied field by field, a list
    /// by range, a stream as three separate keys - because a key there may be any of those. Here a
    /// copy is a read and a write.
    /// </para>
    /// <para>
    /// That is worth stating rather than leaving as an absence, because the simplicity is load
    /// bearing: when OSS does gain a collection type, each of these grows a type switch, and the
    /// Enterprise files are the worked example of what that costs.
    /// </para>
    /// </summary>
    internal static class RespKeyspaceExtras
    {
        /// <summary>
        /// COPY source destination [DB index] [REPLACE]
        /// <para>
        /// Not atomic: the value is read and then written, so another client can see the
        /// destination missing after COPY was issued and before it returns. Redis copies in one
        /// step. The source TTL is carried across, which Redis also does unless REPLACE is given.
        /// </para>
        /// </summary>
        public static RespCommandOutcome Copy(
            RespRequest request, RespWriter writer, RespCacheGateway gateway)
        {
            if (request.ArgCount < 2) return WrongArgs(writer, "copy");

            bool replace = false;
            for (int i = 2; i < request.ArgCount; i++)
            {
                if (request.ArgEquals(i, "REPLACE")) { replace = true; continue; }

                if (request.ArgEquals(i, "DB"))
                {
                    if (i + 1 >= request.ArgCount) return SyntaxError(writer);

                    long db;
                    if (!TryLong(request.GetArg(i + 1), out db)) return NotAnInteger(writer);

                    // An NCache cache is one keyspace, which SELECT already says. Accepting DB 1
                    // and quietly ignoring it would copy into the same database and report
                    // success, which is worse than refusing.
                    if (db != 0)
                    {
                        writer.WriteError("ERR DB index is out of range");
                        return RespCommandOutcome.Continue;
                    }

                    i++;
                    continue;
                }

                return SyntaxError(writer);
            }

            string source = Text(request.GetArg(0));
            string destination = Text(request.GetArg(1));

            if (string.Equals(source, destination, StringComparison.Ordinal))
            {
                // Redis refuses rather than treating it as a no-op, because the intent is unclear.
                writer.WriteError("ERR source and destination objects are the same");
                return RespCommandOutcome.Continue;
            }

            byte[] value = gateway.Get(source);
            if (value == null)
            {
                writer.WriteInteger(0);
                return RespCommandOutcome.Continue;
            }

            if (!replace && gateway.Contains(destination))
            {
                writer.WriteInteger(0);
                return RespCommandOutcome.Continue;
            }

            long remaining = gateway.GetTtlMilliseconds(source);
            gateway.Set(destination, value, remaining > 0
                ? gateway.CreateAbsoluteExpiration(DateTime.UtcNow.AddMilliseconds(remaining))
                : null);

            writer.WriteInteger(1);
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// RENAME key newkey, and RENAMENX which refuses an existing destination.
        /// <para>
        /// NCache has no rename, so this is a copy followed by a delete. Not atomic: a concurrent
        /// reader can see both names present, and a failure between the two steps leaves both.
        /// Redis's RENAME is one step and no other client ever observes the intermediate state, so
        /// a client using RENAME as an atomic swap or publish step cannot rely on it here.
        /// </para>
        /// </summary>
        public static RespCommandOutcome Rename(
            RespRequest request, RespWriter writer, RespCacheGateway gateway, bool failIfExists)
        {
            if (request.ArgCount != 2) return WrongArgs(writer, failIfExists ? "renamenx" : "rename");

            string source = Text(request.GetArg(0));
            string destination = Text(request.GetArg(1));

            byte[] value = gateway.Get(source);
            if (value == null)
            {
                writer.WriteError("ERR no such key");
                return RespCommandOutcome.Continue;
            }

            if (string.Equals(source, destination, StringComparison.Ordinal))
            {
                // Renaming a key to itself succeeds in Redis and changes nothing.
                if (failIfExists) writer.WriteInteger(0);
                else writer.WriteSimpleString("OK");
                return RespCommandOutcome.Continue;
            }

            if (failIfExists && gateway.Contains(destination))
            {
                writer.WriteInteger(0);
                return RespCommandOutcome.Continue;
            }

            long remaining = gateway.GetTtlMilliseconds(source);
            gateway.Set(destination, value, remaining > 0
                ? gateway.CreateAbsoluteExpiration(DateTime.UtcNow.AddMilliseconds(remaining))
                : null);
            gateway.Delete(source);

            if (failIfExists) writer.WriteInteger(1);
            else writer.WriteSimpleString("OK");
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// OBJECT ENCODING|REFCOUNT|FREQ|IDLETIME key, and OBJECT HELP.
        /// </summary>
        public static RespCommandOutcome Object(
            RespRequest request, RespWriter writer, RespCacheGateway gateway)
        {
            if (request.ArgCount < 1) return WrongArgs(writer, "object");

            if (request.ArgEquals(0, "HELP"))
            {
                writer.WriteArrayHeader(4);
                writer.WriteSimpleString("OBJECT <subcommand> [<arg> ...]. Subcommands are:");
                writer.WriteSimpleString("ENCODING <key>");
                writer.WriteSimpleString("REFCOUNT <key>");
                writer.WriteSimpleString("FREQ <key> / IDLETIME <key>");
                return RespCommandOutcome.Continue;
            }

            if (request.ArgCount != 2) return WrongArgs(writer, "object");

            string key = Text(request.GetArg(1));
            EntryType? type = gateway.GetEntryType(key);

            // Every OBJECT subcommand answers nil for a missing key, as Redis does - not an error.
            // A client that probes a key's encoding before it exists must not have to catch.
            if (type == null &&
                (request.ArgEquals(0, "ENCODING") || request.ArgEquals(0, "REFCOUNT") ||
                 request.ArgEquals(0, "FREQ") || request.ArgEquals(0, "IDLETIME")))
            {
                writer.WriteNull();
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "ENCODING"))
            {
                writer.WriteBulkString(Encoding.ASCII.GetBytes(StringEncoding(gateway.Get(key))));
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "REFCOUNT"))
            {
                // NCache does not share value instances between keys the way Redis interns small
                // integers, so a live key is referenced exactly once. That is the truth here
                // rather than a stub.
                writer.WriteInteger(1);
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "FREQ"))
            {
                // Redis answers FREQ only under an LFU policy and errors otherwise, which is what
                // this is. NCache keeps an access counter only under LFU eviction too, but it
                // counts linearly and caps at 32767 rather than decaying over 0-255, so the
                // numbers are not comparable and reporting one would answer a different question.
                writer.WriteError("ERR An LFU maxmemory policy is not selected, access frequency " +
                                  "not tracked. Please note that when switching between maxmemory " +
                                  "policies at runtime LFU and LRU data will take some time to adjust.");
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "IDLETIME"))
            {
                writer.WriteError("ERR An LRU maxmemory policy is not selected, idle time not tracked");
                return RespCommandOutcome.Continue;
            }

            writer.WriteError("ERR Unknown subcommand or wrong number of arguments for '" +
                              Text(request.GetArg(0)) + "'. Try OBJECT HELP.");
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// RANDOMKEY - any key currently in the cache, or nil when it is empty.
        /// <para>
        /// Drawn from the first enumeration chunk rather than from the whole keyspace, so it is
        /// biased towards whatever the enumerator returns first. Redis documents RANDOMKEY as
        /// returning a random key and promises no particular distribution, which this satisfies -
        /// but it is not a uniform draw and is no basis for anything that needs one.
        /// </para>
        /// </summary>
        public static RespCommandOutcome RandomKey(
            RespRequest request, RespWriter writer, RespCacheGateway gateway)
        {
            if (request.ArgCount != 0) return WrongArgs(writer, "randomkey");

            var pointer = new Alachisoft.NCache.Common.DataStructures.EnumerationPointer();
            bool finished;
            List<string> keys = gateway.NextKeyChunk(ref pointer, out finished);

            if (keys == null || keys.Count == 0)
            {
                writer.WriteNull();
                return RespCommandOutcome.Continue;
            }

            string chosen = keys[new Random().Next(keys.Count)];
            writer.WriteBulkString(Encoding.UTF8.GetBytes(chosen));
            return RespCommandOutcome.Continue;
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Redis's OBJ_ENCODING_EMBSTR_SIZE_LIMIT.</summary>
        private const int EmbstrMaxLength = 44;

        /// <summary>
        /// The encoding Redis would report for the same string value.
        /// <para>
        /// NCache has one representation at every size, so there is no real encoding to report -
        /// but clients and tools branch on Redis's exact strings, and a name taken from the entry
        /// type alone was wrong for most values: every string read "raw". The name now comes from
        /// the value, with Redis's own rules. A client inferring memory layout from it will still
        /// be misled; it is the name Redis would give, not a description of what NCache stores.
        /// </para>
        /// <para>
        /// Only the string rules are here. The listpack, intset, hashtable and quicklist names
        /// describe collections, and this edition has none.
        /// </para>
        /// </summary>
        private static string StringEncoding(byte[] value)
        {
            if (value == null) return "raw";
            if (IsCanonicalInteger(value)) return "int";
            return value.Length <= EmbstrMaxLength ? "embstr" : "raw";
        }

        /// <summary>
        /// What Redis stores as an integer: optional minus, no leading zeros, no sign on zero, and
        /// within a signed 64-bit range. "007" and "+7" are strings to Redis, not integers.
        /// </summary>
        private static bool IsCanonicalInteger(byte[] value)
        {
            if (value.Length == 0 || value.Length > 20) return false;

            int start = value[0] == (byte)'-' ? 1 : 0;
            if (start == value.Length) return false;
            if (value[start] == (byte)'0' && (value.Length - start > 1 || start == 1)) return false;

            for (int i = start; i < value.Length; i++)
            {
                if (value[i] < (byte)'0' || value[i] > (byte)'9') return false;
            }

            long ignored;
            return long.TryParse(Encoding.ASCII.GetString(value), NumberStyles.AllowLeadingSign,
                                 CultureInfo.InvariantCulture, out ignored);
        }

        private static bool TryLong(byte[] raw, out long value)
        {
            value = 0;
            if (raw == null || raw.Length == 0) return false;

            return long.TryParse(Encoding.ASCII.GetString(raw), NumberStyles.AllowLeadingSign,
                                 CultureInfo.InvariantCulture, out value);
        }

        private static string Text(byte[] value)
        {
            return value == null ? string.Empty : Encoding.UTF8.GetString(value);
        }

        private static RespCommandOutcome NoSuchKey(RespWriter writer)
        {
            writer.WriteError("ERR no such key");
            return RespCommandOutcome.Continue;
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
