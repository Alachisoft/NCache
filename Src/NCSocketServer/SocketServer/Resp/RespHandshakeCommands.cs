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
using Alachisoft.NCache.SocketServer.Resp.Protocol;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// The non-CRUD commands a real client library issues before it will send its first GET.
    /// <para>
    /// None of these are CRUD, but without them StackExchange.Redis and redis-py fail during
    /// connect and never reach a cache operation at all. Each is answered with the smallest reply
    /// that keeps a client happy - this is not an attempt to implement Redis introspection.
    /// </para>
    /// <para>
    /// SECURITY: this port has no authentication at all - AUTH is not part of NCache OSS, and the
    /// AUTH clause of HELLO is refused rather than consumed. A client must never be told it
    /// authenticated against a server that cannot check, which is the one shape of this that would
    /// be worse than having none.
    /// </para>
    /// </summary>
    internal static class RespHandshakeCommands
    {
        /// <summary>
        /// Reported to clients as the Redis version. A 6.x value makes libraries settle on RESP2-era
        /// behaviour without probing for features this listener does not implement. The real build
        /// is reported separately as ncache_version.
        /// </summary>
        public const string ReportedRedisVersion = "6.2.0";

        public static string NCacheVersion
        {
            get { return "5.3 SP6 OSS"; }
        }

        #region HELLO

        /// <summary>
        /// HELLO [protover [SETNAME name]]
        /// <para>
        /// Sent on connect by StackExchange.Redis 2.x and redis-py 4+. RESP3 is refused with
        /// -NOPROTO, which is the documented way to tell a client to fall back to RESP2. Replying
        /// RESP2 content to a RESP3 request instead would desynchronise the client's parser.
        /// </para>
        /// </summary>
        public static RespCommandOutcome Hello(RespRequest request, RespWriter writer, RespConnectionState state, string cacheName)
        {
            if (request.ArgCount > 0)
            {
                long protocol;
                if (!TryParseLong(request.GetArg(0), out protocol))
                {
                    writer.WriteError("NOPROTO unsupported protocol version");
                    return RespCommandOutcome.Continue;
                }

                if (protocol != 2)
                {
                    writer.WriteError("NOPROTO unsupported protocol version");
                    return RespCommandOutcome.Continue;
                }

                // SETNAME is consumed; an AUTH clause is refused.
                //
                // Skipping it, which this used to do, is the dangerous reading: the client's
                // handshake succeeds and it carries on believing it is authenticated on a server
                // with no authentication at all. Refusing says plainly that the credentials went
                // nowhere.
                for (int i = 1; i < request.ArgCount; i++)
                {
                    if (request.ArgEquals(i, "AUTH"))
                    {
                        writer.WriteError("ERR unsupported command 'HELLO AUTH': " + RespUnsupported.NotInOss);
                        return RespCommandOutcome.Continue;
                    }
                    if (request.ArgEquals(i, "SETNAME") && i + 1 < request.ArgCount)
                    {
                        state.Name = Text(request.GetArg(i + 1));
                        i++;
                        continue;
                    }
                    writer.WriteError("ERR syntax error in HELLO");
                    return RespCommandOutcome.Continue;
                }
            }

            // RESP2 renders a map as a flat array of alternating key/value entries.
            writer.WriteArrayHeader(14);
            // "redis" rather than "ncache": clients assert on this field to confirm they are
            // speaking to a Redis-protocol server at all, and a different value makes some of them
            // refuse the connection. The real product is reported as ncache_version in INFO.
            writer.WriteBulkString("server"); writer.WriteBulkString("redis");
            writer.WriteBulkString("version"); writer.WriteBulkString(ReportedRedisVersion);
            writer.WriteBulkString("proto"); writer.WriteInteger(2);
            writer.WriteBulkString("id"); writer.WriteInteger(state.Id);
            writer.WriteBulkString("mode"); writer.WriteBulkString("standalone");
            writer.WriteBulkString("role"); writer.WriteBulkString("master");
            writer.WriteBulkString("modules"); writer.WriteArrayHeader(0);

            RespLog.Detail("RespHandshake.Hello",
                "client " + state.RemoteEndPoint + " completed HELLO on cache '" + cacheName + "'.");

            return RespCommandOutcome.Continue;
        }

        #endregion

        #region SELECT / RESET

        /// <summary>
        /// SELECT index - only database 0 exists, because one cache is one keyspace.
        /// <para>
        /// A non-zero index is refused rather than silently mapped onto the same keyspace: a client
        /// configured for database 3 should fail loudly, not quietly share database 0's keys. The
        /// request is also remembered on the connection, because a library that pipelines
        /// <c>SELECT 3</c> ahead of <c>GET k</c> does not stop at the first error - without the
        /// memory the GET would run against database 0 and the client would never know.
        /// </para>
        /// </summary>
        public static RespCommandOutcome Select(RespRequest request, RespWriter writer, RespConnectionState state)
        {
            if (request.ArgCount != 1)
            {
                writer.WriteError("ERR wrong number of arguments for 'select' command");
                return RespCommandOutcome.Continue;
            }

            long index;
            if (!TryParseLong(request.GetArg(0), out index))
            {
                writer.WriteError("ERR value is not an integer or out of range");
                return RespCommandOutcome.Continue;
            }

            state.SelectedDatabase = index;

            if (index != 0)
            {
                writer.WriteError(UnsupportedDatabaseError(index));
                return RespCommandOutcome.Continue;
            }

            writer.WriteSimpleString("OK");
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// Commands still answered while the connection has a non-zero database selected: enough
        /// to come back to database 0, leave, or start over. Everything else is refused.
        /// </summary>
        private static readonly HashSet<string> AllowedOffDatabaseZero =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "SELECT", "QUIT", "RESET", "AUTH", "HELLO"
            };

        // AUTH is listed above although this build does not implement it: letting it through means
        // the refusal table answers it, so a client that sent SELECT 1 and then AUTH is told that
        // AUTH is unsupported rather than being told about databases, which answers a question it
        // did not ask.

        /// <summary>
        /// Refuses a command when the connection has asked for any database other than 0.
        /// Returns true when the caller should stop - the refusal is already written.
        /// </summary>
        public static bool DatabaseBlocks(RespRequest request, RespWriter writer, RespConnectionState state)
        {
            if (state.SelectedDatabase == 0) return false;
            if (AllowedOffDatabaseZero.Contains(request.Name ?? string.Empty)) return false;

            writer.WriteError(UnsupportedDatabaseError(state.SelectedDatabase));
            return true;
        }

        /// <summary>
        /// The one message for every path that targets a database other than 0 - SELECT itself,
        /// the commands that follow it, and the MOVE/SWAPDB entries in <see cref="RespUnsupported"/>
        /// - so a client sees the same explanation whichever way it got there.
        /// </summary>
        public static string UnsupportedDatabaseError(long database)
        {
            return "ERR unsupported operation: NCache only supports database 0 and databases cannot " +
                   "be changed (database " + database.ToString(CultureInfo.InvariantCulture) + " requested)";
        }

        /// <summary>RESET - back to a fresh connection: no name, database 0. Redis does the same.</summary>
        public static RespCommandOutcome Reset(RespWriter writer, RespConnectionState state)
        {
            state.Name = string.Empty;
            state.SelectedDatabase = 0;
            writer.WriteSimpleString("RESET");
            return RespCommandOutcome.Continue;
        }

        #endregion

        #region CLIENT

        /// <summary>
        /// CLIENT SETNAME | GETNAME | ID | SETINFO | NO-EVICT | NO-TOUCH
        /// <para>
        /// StackExchange.Redis sends CLIENT SETNAME when a client name is configured, and recent
        /// redis-py sends CLIENT SETINFO on every connect. Both must succeed quietly.
        /// </para>
        /// </summary>
        public static RespCommandOutcome Client(RespRequest request, RespWriter writer, RespConnectionState state)
        {
            if (request.ArgCount < 1)
            {
                writer.WriteError("ERR wrong number of arguments for 'client' command");
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "PAUSE"))
            {
                // Would need pausing command execution across client connections.
                writer.WriteError("ERR unsupported command 'CLIENT PAUSE': " + RespUnsupported.NotInNCache);
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "SETNAME"))
            {
                if (request.ArgCount != 2) return WrongSubcommandArity(writer, "client|setname");

                string name = Text(request.GetArg(1));

                // Redis refuses names containing a space or a newline, and the reason is CLIENT
                // LIST: its output is space-separated fields on one line per client, so a name
                // carrying either character would corrupt the very report it appears in.
                if (name.IndexOf(' ') >= 0 || name.IndexOf('\n') >= 0 || name.IndexOf('\r') >= 0)
                {
                    writer.WriteError("ERR Client names cannot contain spaces, newlines or special characters.");
                    return RespCommandOutcome.Continue;
                }

                state.Name = name;
                writer.WriteSimpleString("OK");
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "LIST"))
            {
                var provider = ClientListProvider;
                writer.WriteBulkString(provider == null ? string.Empty : SafeClientList(provider));
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "GETNAME"))
            {
                if (string.IsNullOrEmpty(state.Name)) writer.WriteNull();
                else writer.WriteBulkString(state.Name);
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "ID"))
            {
                writer.WriteInteger(state.Id);
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "SETINFO"))
            {
                if (request.ArgCount != 3) return WrongSubcommandArity(writer, "client|setinfo");

                if (request.ArgEquals(1, "LIB-NAME")) state.LibraryName = Text(request.GetArg(2));
                else if (request.ArgEquals(1, "LIB-VER")) state.LibraryVersion = Text(request.GetArg(2));

                writer.WriteSimpleString("OK");
                return RespCommandOutcome.Continue;
            }

            // Knobs that only affect Redis-internal behaviour; accepting them is harmless and
            // stops a client's optional tuning call from surfacing as an error.
            if (request.ArgEquals(0, "NO-EVICT") || request.ArgEquals(0, "NO-TOUCH") || request.ArgEquals(0, "REPLY"))
            {
                writer.WriteSimpleString("OK");
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "INFO"))
            {
                writer.WriteBulkString(DescribeClient(state));
                return RespCommandOutcome.Continue;
            }

            writer.WriteError("ERR Unknown CLIENT subcommand '" + Text(request.GetArg(0)) + "'");
            return RespCommandOutcome.Continue;
        }

        #endregion

        #region COMMAND / CONFIG / INFO

        /// <summary>
        /// COMMAND [COUNT|INFO|GETKEYS|LIST|DOCS]
        /// <para>
        /// Answered from <see cref="RespCommandTable"/> rather than stubbed. GETKEYS is the one
        /// that matters beyond introspection: a cluster-aware client asks it when it meets a
        /// command whose key positions it does not recognise, and an empty answer would leave it
        /// sending that command to an arbitrary node.
        /// </para>
        /// <para>
        /// DOCS stays an empty array. redis-cli 7+ sends it on connect and accepts nothing back;
        /// the full documentation payload is large and no client behaviour depends on it.
        /// </para>
        /// </summary>
        public static RespCommandOutcome Command(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount == 0)
            {
                WriteCommandList(writer);
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "COUNT"))
            {
                writer.WriteInteger(RespCommandTable.Count);
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "LIST"))
            {
                // Redis answers LIST with a flat array of names and nothing else - verified against
                // redis:8-alpine, which replies *N followed by N bulk strings. Sharing the entry
                // writer with bare COMMAND was wrong twice over: a caller that asked for names had
                // to pick them out of six-field entries, and the flag strings in those entries read
                // like command names to anything scanning the reply ("readonly" is a flag on every
                // read command, and also a Redis command this listener refuses).
                //
                // FILTERBY (MODULE / ACLCAT / PATTERN) is not implemented - there are no modules and
                // no ACL categories here - so any argument after LIST is refused rather than
                // ignored. Answering the unfiltered list would answer a different question from the
                // one asked, and Redis itself replies "syntax error" to a LIST argument it cannot
                // honour (COMMAND LIST EXTRA).
                if (request.ArgCount != 1)
                {
                    writer.WriteError("ERR syntax error");
                    return RespCommandOutcome.Continue;
                }

                writer.WriteArrayHeader(RespCommandTable.Count);
                foreach (var entry in RespCommandTable.Entries) writer.WriteBulkString(entry.Name);
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "INFO"))
            {
                // One reply element per name asked about, in order. An unknown name is a null
                // element rather than an omission, so the caller can line answers up with requests.
                int names = request.ArgCount - 1;
                if (names <= 0) { WriteCommandList(writer); return RespCommandOutcome.Continue; }

                writer.WriteArrayHeader(names);
                for (int i = 1; i < request.ArgCount; i++)
                {
                    var entry = RespCommandTable.Find(Text(request.GetArg(i)));
                    if (entry == null) writer.WriteNull(); else WriteCommandEntry(writer, entry);
                }
                return RespCommandOutcome.Continue;
            }

            if (request.ArgEquals(0, "GETKEYS"))
            {
                if (request.ArgCount < 2)
                {
                    writer.WriteError("ERR Unknown subcommand or wrong number of arguments for 'GETKEYS'");
                    return RespCommandOutcome.Continue;
                }

                var args = new List<byte[]>();
                for (int i = 1; i < request.ArgCount; i++) args.Add(request.GetArg(i));

                string name = Text(args[0]);
                if (RespCommandTable.Find(name) == null)
                {
                    writer.WriteError("ERR Invalid command specified");
                    return RespCommandOutcome.Continue;
                }

                var keys = RespCommandTable.ExtractKeys(name, args);
                if (keys.Count == 0)
                {
                    writer.WriteError("ERR The command has no key arguments");
                    return RespCommandOutcome.Continue;
                }

                writer.WriteArrayHeader(keys.Count);
                foreach (var key in keys) writer.WriteBulkString(key);
                return RespCommandOutcome.Continue;
            }

            // DOCS and anything else: an empty array, which every client tolerates.
            writer.WriteEmptyArray();
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// Every command as a full COMMAND INFO entry - what bare COMMAND returns. COMMAND LIST does
        /// NOT come through here; it answers names only, as Redis does.
        /// </summary>
        private static void WriteCommandList(RespWriter writer)
        {
            writer.WriteArrayHeader(RespCommandTable.Count);
            foreach (var entry in RespCommandTable.Entries) WriteCommandEntry(writer, entry);
        }

        /// <summary>
        /// One COMMAND INFO element: name, arity, flags, first key, last key, step. Six fields is
        /// the RESP2 shape; clients index into it positionally, so the order is fixed.
        /// </summary>
        private static void WriteCommandEntry(RespWriter writer, RespCommandTable.Entry entry)
        {
            writer.WriteArrayHeader(6);
            writer.WriteBulkString(entry.Name);
            writer.WriteInteger(entry.Arity);

            writer.WriteArrayHeader(entry.Flags.Length);
            foreach (var flag in entry.Flags) writer.WriteSimpleString(flag);

            writer.WriteInteger(entry.FirstKey);
            writer.WriteInteger(entry.LastKey);
            writer.WriteInteger(entry.Step);
        }

        /// <summary>
        /// CONFIG GET returns an empty map; CONFIG SET is refused.
        /// <para>
        /// StackExchange.Redis probes CONFIG GET for timeout and maxmemory during handshake and
        /// treats "not configured" as fine. CONFIG SET is refused outright - NCache configuration
        /// must never be mutable from an unauthenticated port.
        /// </para>
        /// </summary>
        public static RespCommandOutcome Config(RespRequest request, RespWriter writer)
        {
            if (request.ArgCount >= 1 && request.ArgEquals(0, "GET"))
            {
                // Redis 7 accepts several patterns in one CONFIG GET and answers with the union.
                var matched = new List<KeyValuePair<string, string>>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 1; i < request.ArgCount; i++)
                {
                    string pattern = Text(request.GetArg(i));

                    foreach (var parameter in ConfigParameters())
                    {
                        if (!PubSub.RespGlob.IsMatch(pattern, parameter.Key)) continue;
                        if (!seen.Add(parameter.Key)) continue;
                        matched.Add(parameter);
                    }
                }

                // Flat key/value array in RESP2, which is how a map is carried on this protocol.
                writer.WriteArrayHeader(matched.Count * 2);
                foreach (var parameter in matched)
                {
                    writer.WriteBulkString(parameter.Key);
                    writer.WriteBulkString(parameter.Value);
                }
                return RespCommandOutcome.Continue;
            }

            if (request.ArgCount >= 1 && request.ArgEquals(0, "RESETSTAT"))
            {
                writer.WriteSimpleString("OK");
                return RespCommandOutcome.Continue;
            }

            if (request.ArgCount >= 1 && request.ArgEquals(0, "SET"))
            {
                // The same shape every other refusal uses, as CLIENT PAUSE does. Naming the
                // listener in a reply said where the refusal came from rather than what it means:
                // a client cannot act on which component answered, and the wording was the only
                // one in the layer that did not read like the rest.
                writer.WriteError("ERR unsupported command 'CONFIG SET': " + RespUnsupported.NotInNCache);
                return RespCommandOutcome.Continue;
            }

            writer.WriteError("ERR Unknown CONFIG subcommand");
            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// The configuration a client can meaningfully read back.
        /// <para>
        /// Deliberately short. Every entry here is either something this listener genuinely has
        /// (maxclients, timeout, port, bind) or a Redis knob whose value is a real statement about
        /// how NCache behaves - appendonly is "no" because there is no AOF, and maxmemory is 0
        /// because NCache does not evict on a byte ceiling handed to it over this port. Redis
        /// parameters that would only be guesses are left out rather than answered with a default
        /// copied from Redis.
        /// </para>
        /// <para>
        /// The maxmemory family is spelt out separately rather than collapsed, because clients glob
        /// for "maxmemory*" and expect more than one answer.
        /// </para>
        /// </summary>
        private static IEnumerable<KeyValuePair<string, string>> ConfigParameters()
        {
            var port = MaxConnectionsProvider == null ? 0 : PortProvider();
            var maxclients = MaxConnectionsProvider == null ? 0 : MaxConnectionsProvider();

            yield return Pair("maxmemory", "0");
            yield return Pair("maxmemory-policy", "noeviction");
            yield return Pair("maxmemory-samples", "5");
            yield return Pair("maxmemory-clients", "0");
            yield return Pair("maxclients", maxclients.ToString(CultureInfo.InvariantCulture));
            yield return Pair("timeout", IdleTimeoutProvider == null
                ? "0" : IdleTimeoutProvider().ToString(CultureInfo.InvariantCulture));
            yield return Pair("port", port.ToString(CultureInfo.InvariantCulture));
            yield return Pair("databases", "1");
            yield return Pair("appendonly", "no");
            yield return Pair("save", string.Empty);

            // No keyspace notifications are emitted, so the flag set is empty - which is Redis's
            // own default, and what tells a client to skip rather than wait for events. Answering
            // nothing at all made clients that read this with .Single() fail outright.
            yield return Pair("notify-keyspace-events", string.Empty);
            yield return Pair("proto-max-bulk-len", Protocol.RespLimits.MaxBulkLength
                .ToString(CultureInfo.InvariantCulture));
        }

        private static KeyValuePair<string, string> Pair(string key, string value)
        {
            return new KeyValuePair<string, string>(key, value);
        }

        /// <summary>
        /// One CLIENT LIST line per live connection. Supplied by the listener, which owns the
        /// connection set; null until it is wired, in which case CLIENT LIST answers empty rather
        /// than failing.
        /// </summary>
        internal static Func<string> ClientListProvider { get; set; }

        private static string SafeClientList(Func<string> provider)
        {
            try { return provider() ?? string.Empty; }
            catch (Exception e)
            {
                RespLog.Detail("RespHandshake.ClientList", e.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// The single-line description of one connection, in Redis's field=value form. Kept next to
        /// CLIENT INFO so the two cannot drift into reporting different shapes for the same client.
        /// </summary>
        internal static string DescribeClient(RespConnectionState state)
        {
            if (state == null) return string.Empty;

            return "id=" + state.Id.ToString(CultureInfo.InvariantCulture) +
                   " addr=" + state.RemoteEndPoint +
                   " name=" + state.Name +
                   " lib-name=" + state.LibraryName +
                   " lib-ver=" + state.LibraryVersion +
                   " db=0 resp=2" +
                   " sub=" + state.SubscriptionCount.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Wired by the listener, which owns the configuration these report.</summary>
        internal static Func<int> PortProvider { get; set; }
        internal static Func<int> MaxConnectionsProvider { get; set; }
        internal static Func<int> IdleTimeoutProvider { get; set; }

        // INFO moved to RespInfo, which owns the section table and the per-section builders.
        // It was a single hand-built block here that ignored its section argument entirely.

        #endregion

        #region Helpers

        private static RespCommandOutcome WrongSubcommandArity(RespWriter writer, string name)
        {
            writer.WriteError("ERR wrong number of arguments for '" + name + "' command");
            return RespCommandOutcome.Continue;
        }

        private static string Text(byte[] raw)
        {
            return raw == null ? string.Empty : Encoding.UTF8.GetString(raw);
        }

        private static bool TryParseLong(byte[] raw, out long value)
        {
            value = 0;
            if (raw == null || raw.Length == 0) return false;

            return long.TryParse(
                Encoding.ASCII.GetString(raw), NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out value);
        }

        #endregion
    }
}
