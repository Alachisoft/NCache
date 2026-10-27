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

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// Metadata for the commands this listener implements, in the shape Redis reports through
    /// COMMAND, COMMAND COUNT, COMMAND INFO and COMMAND GETKEYS.
    /// <para>
    /// This is descriptive, not dispatching: the authoritative list of what runs is still the
    /// switch in <see cref="RespCommandDispatcher.Dispatch"/>. What lives here is the arity and
    /// key-position information a client needs in order to route a command it did not write - a
    /// cluster-aware library asks COMMAND GETKEYS when it meets a command whose key layout it does
    /// not already know, and answering "no keys" would make it send that command to an arbitrary
    /// node.
    /// </para>
    /// <para>
    /// Arity follows Redis's convention exactly, because clients arithmetic on it: a positive
    /// number is the exact argument count including the command name, and a negative number means
    /// "at least this many". GET is 2, MGET is -2.
    /// </para>
    /// <para>
    /// An entry here is a promise: COMMAND COUNT counts these, COMMAND LIST names them, and a
    /// client that introspects will call what it is told about. So this list must hold exactly the
    /// commands <see cref="RespCommandDispatcher.Dispatch"/> serves - nothing that answers
    /// "unsupported". Listing a refused command is worse than omitting a supported one: the client
    /// is told the command exists and only finds out otherwise when it runs.
    /// </para>
    /// </summary>
    internal static class RespCommandTable
    {
        internal sealed class Entry
        {
            public readonly string Name;
            public readonly int Arity;
            public readonly string[] Flags;
            public readonly int FirstKey;
            public readonly int LastKey;
            public readonly int Step;

            public Entry(string name, int arity, string[] flags, int firstKey, int lastKey, int step)
            {
                Name = name;
                Arity = arity;
                Flags = flags;
                FirstKey = firstKey;
                LastKey = lastKey;
                Step = step;
            }
        }

        private static readonly string[] R = { "readonly", "fast" };
        private static readonly string[] W = { "write", "denyoom" };
        private static readonly string[] Wf = { "write", "denyoom", "fast" };
        private static readonly string[] Admin = { "admin", "noscript", "loading", "stale" };
        private static readonly string[] Conn = { "fast", "loading", "stale" };
        private static readonly string[] Pubsub = { "pubsub", "fast", "loading", "stale" };

        private static readonly Entry[] All =
        {
            // ---- strings / CRUD ----
            new Entry("get",       2, R,  1, 1, 1),
            new Entry("getex",    -2, Wf, 1, 1, 1),
            new Entry("set",      -3, W,  1, 1, 1),
            new Entry("setnx",     3, Wf, 1, 1, 1),
            new Entry("setex",     4, W,  1, 1, 1),
            new Entry("psetex",    4, W,  1, 1, 1),
            new Entry("getset",    3, W,  1, 1, 1),
            new Entry("getdel",    2, Wf, 1, 1, 1),
            new Entry("del",      -2, W,  1, -1, 1),
            new Entry("unlink",   -2, W,  1, -1, 1),
            new Entry("exists",   -2, R,  1, -1, 1),
            new Entry("touch",    -2, R,  1, -1, 1),
            new Entry("mget",     -2, R,  1, -1, 1),
            new Entry("mset",     -3, W,  1, -1, 2),
            new Entry("type",      2, R,  1, 1, 1),

            // No keys: FLUSHDB/FLUSHALL take an optional ASYNC|SYNC and nothing else.
            new Entry("flushdb",  -1, Admin, 0, 0, 0),
            new Entry("flushall", -1, Admin, 0, 0, 0),

            // ---- string shape ----
            new Entry("strlen",    2, R,  1, 1, 1),
            new Entry("append",    3, Wf, 1, 1, 1),
            new Entry("setrange",  4, W,  1, 1, 1),
            new Entry("getrange",  4, R,  1, 1, 1),
            new Entry("substr",    4, R,  1, 1, 1),

            // ---- expiration ----
            new Entry("expire",   -3, Wf, 1, 1, 1),
            new Entry("pexpire",  -3, Wf, 1, 1, 1),
            new Entry("expireat", -3, Wf, 1, 1, 1),
            new Entry("pexpireat",-3, Wf, 1, 1, 1),
            new Entry("ttl",       2, R,  1, 1, 1),
            new Entry("pttl",      2, R,  1, 1, 1),
            new Entry("persist",   2, Wf, 1, 1, 1),
            new Entry("expiretime",2, R,  1, 1, 1),
            new Entry("pexpiretime",2, R, 1, 1, 1),

            // The counter, hash, list and set families, the stream family, and APPEND/SETRANGE
            // were described here while the Enterprise listener served them. NCache Open Source
            // refuses all of them (see RespUnsupported), so they are absent rather than listed:
            // COMMAND COUNT and COMMAND LIST are built from this array, and a client that reads
            // them must not be told about a command that will answer "unsupported".

            // ---- pub/sub ----
            new Entry("subscribe",    -2, Pubsub, 0, 0, 0),
            new Entry("unsubscribe",  -1, Pubsub, 0, 0, 0),
            new Entry("psubscribe",   -2, Pubsub, 0, 0, 0),
            new Entry("punsubscribe", -1, Pubsub, 0, 0, 0),
            new Entry("publish",       3, Pubsub, 0, 0, 0),
            new Entry("pubsub",       -2, Pubsub, 0, 0, 0),
            new Entry("ssubscribe",   -2, Pubsub, 0, 0, 0),
            new Entry("sunsubscribe", -1, Pubsub, 0, 0, 0),
            new Entry("spublish",      3, Pubsub, 0, 0, 0),

            // ---- connection / handshake ----
            new Entry("ping",    -1, Conn, 0, 0, 0),
            new Entry("echo",     2, Conn, 0, 0, 0),
            new Entry("time",     1, Conn, 0, 0, 0),
            new Entry("quit",     1, Conn, 0, 0, 0),
            new Entry("hello",   -1, Conn, 0, 0, 0),
            new Entry("select",   2, Conn, 0, 0, 0),
            new Entry("reset",    1, Conn, 0, 0, 0),
            new Entry("client",  -2, Admin, 0, 0, 0),
            new Entry("command", -1, Conn, 0, 0, 0),
            new Entry("config",  -2, Admin, 0, 0, 0),
            new Entry("info",    -1, Conn, 0, 0, 0),

            // Counters, held as strings in this build - the arity and key shape a client needs are
            // the same either way, so nothing here reveals the difference.
            new Entry("incr",        2, Wf, 1, 1, 1),
            new Entry("decr",        2, Wf, 1, 1, 1),
            new Entry("incrby",      3, Wf, 1, 1, 1),
            new Entry("decrby",      3, Wf, 1, 1, 1),
            new Entry("incrbyfloat", 3, Wf, 1, 1, 1),

            // Key/value pairs that cannot be split the way MSET can, because the all-or-nothing
            // rule spans every pair.
            new Entry("msetnx",     -3, W,  1, -1, 2),

            // Two keys, both of which matter, so a cluster-aware client must keep them together.
            new Entry("copy",       -3, W,  1, 2, 1),
            new Entry("rename",      3, W,  1, 2, 1),
            new Entry("renamenx",    3, Wf, 1, 2, 1),
            new Entry("lcs",        -3, R,  1, 2, 1),

            new Entry("object",     -2, R,  2, 2, 1),
            new Entry("randomkey",   1, R,  0, 0, 0),
            new Entry("dump",        2, R,  1, 1, 1),
            new Entry("restore",    -4, W,  1, 1, 1),

            // No "cluster" entry: RESP-level slot routing is not in NCache Open Source, and
            // advertising CLUSTER is what makes a cluster-aware client try to use it.
        };

        private static readonly Dictionary<string, Entry> ByName = Build();

        private static Dictionary<string, Entry> Build()
        {
            var map = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in All) map[e.Name] = e;
            return map;
        }

        public static int Count { get { return All.Length; } }

        public static IEnumerable<Entry> Entries { get { return All; } }

        public static Entry Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Entry entry;
            return ByName.TryGetValue(name, out entry) ? entry : null;
        }

        /// <summary>
        /// The keys inside a command, as COMMAND GETKEYS reports them.
        /// <para>
        /// Driven by the same firstkey/lastkey/step triple Redis publishes, so a caller that reads
        /// COMMAND INFO and one that calls COMMAND GETKEYS get answers that agree. A lastkey of -1
        /// means "to the end of the arguments", which is how the variadic commands are described.
        /// </para>
        /// </summary>
        /// <param name="args">The full command, argument 0 being the command name.</param>
        public static List<byte[]> ExtractKeys(string name, IList<byte[]> args)
        {
            var keys = new List<byte[]>();

            Entry entry = Find(name);
            if (entry == null || entry.FirstKey <= 0 || entry.Step <= 0) return keys;

            int last = entry.LastKey < 0 ? args.Count - 1 : entry.LastKey;
            if (last >= args.Count) last = args.Count - 1;

            for (int i = entry.FirstKey; i <= last; i += entry.Step)
                keys.Add(args[i]);

            return keys;
        }
    }
}
