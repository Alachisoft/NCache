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
using Alachisoft.NCache.SocketServer.Resp.Protocol;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// Commands this listener refuses, and the engine capability each one would need.
    /// <para>
    /// A client must be able to tell three situations apart, because it can act on each
    /// differently:
    /// </para>
    /// <list type="number">
    /// <item>a typo or a command from a newer Redis - <c>unknown command</c>;</item>
    /// <item>a real Redis command NCache cannot serve - <c>unsupported command</c>, saying either
    ///       <c>not supported in NCache</c> or <c>not supported in NCache OSS</c>, so the caller
    ///       knows whether the command exists at all or is an edition boundary;</item>
    /// <item>a command that works.</item>
    /// </list>
    /// <para>
    /// The rule for being in this table is strict: <b>only commands that need an engine capability
    /// NCache does not have.</b> Anything expressible over what the engine already provides is
    /// implemented rather than listed here - a command that half-works, or that says "unsupported"
    /// when it could have worked, is worse than either honest answer.
    /// </para>
    /// <para>
    /// In NCache Open Source that rule also covers whole families the Enterprise listener serves:
    /// the distributed list, set, hash and counter data types, streams, in-place partial value
    /// rewrites and RESP-level cluster routing all rest on Enterprise engine features. They are
    /// listed here rather than left to fall through as "unknown command", so a client can tell
    /// "this server cannot do that" from "this server has never heard of that".
    /// </para>
    /// </summary>
    internal static class RespUnsupported
    {
        /// <summary>A family of refused commands and which of the two reasons applies to it.</summary>
        private sealed class Gap
        {
            public readonly string Reason;
            public readonly string[] Commands;
            public Gap(string reason, params string[] commands) { Reason = reason; Commands = commands; }
        }

        /// <summary>
        /// The command does not exist in NCache at all - no edition serves it, and asking again on a
        /// bigger licence will not help.
        /// </summary>
        internal const string NotInNCache = "not supported in NCache";

        /// <summary>
        /// NCache Enterprise serves the command and this build does not, because it rests on an
        /// Enterprise engine feature. Worth saying separately: it tells the caller the command is
        /// real and the limit is the edition, which is a different decision from the one above.
        /// </summary>
        internal const string NotInOss = "not supported in NCache OSS";

        private static readonly Gap[] Gaps =
        {
            // ======== not in NCache, any edition ========

            // Would need a sorted/scored collection, which the NCache engine does not provide.
            new Gap(NotInNCache,
                "ZADD", "ZINCRBY", "ZREM", "ZSCORE", "ZMSCORE", "ZRANK", "ZREVRANK",
                "ZRANGE", "ZREVRANGE", "ZRANGEBYSCORE", "ZREVRANGEBYSCORE", "ZRANGEBYLEX",
                "ZREVRANGEBYLEX", "ZRANGESTORE", "ZREMRANGEBYRANK", "ZREMRANGEBYSCORE",
                "ZREMRANGEBYLEX", "ZCARD", "ZCOUNT", "ZLEXCOUNT", "ZPOPMIN", "ZPOPMAX",
                "BZPOPMIN", "BZPOPMAX", "ZRANDMEMBER", "ZSCAN", "ZUNION", "ZINTER", "ZDIFF",
                "ZUNIONSTORE", "ZINTERSTORE", "ZDIFFSTORE", "ZINTERCARD", "ZMPOP", "BZMPOP"),

            // Would need a queued multi-key batch with rollback; NCache locking is single-key.
            new Gap(NotInNCache,
                "MULTI", "EXEC", "DISCARD", "WATCH", "UNWATCH"),

            // Would need an embedded script interpreter, which NCache does not host.
            new Gap(NotInNCache,
                "EVAL", "EVALSHA", "EVAL_RO", "EVALSHA_RO", "SCRIPT",
                "FUNCTION", "FCALL", "FCALL_RO"),

            // Would need bit-level addressing inside a stored value.
            new Gap(NotInNCache,
                "SETBIT", "GETBIT", "BITCOUNT", "BITPOS", "BITOP", "BITFIELD", "BITFIELD_RO"),

            // Would need a probabilistic cardinality structure.
            new Gap(NotInNCache,
                "PFADD", "PFCOUNT", "PFMERGE", "PFDEBUG", "PFSELFTEST"),

            // Geospatial indexing builds on sorted sets, which are absent above.
            new Gap(NotInNCache,
                "GEOADD", "GEOPOS", "GEODIST", "GEOHASH", "GEOSEARCH", "GEOSEARCHSTORE",
                "GEORADIUS", "GEORADIUSBYMEMBER", "GEORADIUS_RO", "GEORADIUSBYMEMBER_RO"),

            // Would need a way to park a request inside the engine and wake it on a write.
            new Gap(NotInNCache,
                "BLPOP", "BRPOP", "BLMOVE", "BRPOPLPUSH", "BLMPOP"),

            // Would need an atomic move between two keys.
            new Gap(NotInNCache,
                "RPOPLPUSH", "LMOVE", "LMPOP", "SMOVE", "MIGRATE"),

            // Collection commands Enterprise does not serve either, so they stay in this group even
            // though the family they belong to is Enterprise-only below. Remove-by-value needs a
            // binary-safe element comparison the engine does not have; the rest need server-side
            // set algebra, per-field increments, random selection, or a float counter.
            new Gap(NotInNCache,
                "LREM", "LPOS", "LINSERT", "LTRIM"),

            new Gap(NotInNCache,
                "SMISMEMBER", "SUNION", "SINTER", "SDIFF",
                "SUNIONSTORE", "SINTERSTORE", "SDIFFSTORE", "SINTERCARD"),

            new Gap(NotInNCache,
                "HINCRBY", "HINCRBYFLOAT", "HRANDFIELD"),

            // Stream commands Enterprise does not serve: they need per-consumer registry state.
            new Gap(NotInNCache,
                "XAUTOCLAIM", "XSETID"),

            // Cluster commands Enterprise does not serve; it implements CLUSTER and nothing else.
            new Gap(NotInNCache,
                "READONLY", "READWRITE", "ASKING", "RESTORE-ASKING"),

            // HGETDEL is newer than any NCache hash support and is not implemented in any edition,
            // so it is refused outright rather than as part of the hash family below.
            new Gap(NotInNCache,
                "HGETDEL"),

            // Would need Redis-compatible sorting with BY/GET lookups and STORE support. SORT_RO is
            // the same command without STORE, so it shares the gap - every other read-only variant
            // in this table sits with the command it mirrors, and a client that falls back from one
            // to the other must not get two different explanations.
            new Gap(NotInNCache,
                "SORT", "SORT_RO"),

            // Would need a resumable cursor over the keyspace or a collection.
            new Gap(NotInNCache,
                "SCAN", "HSCAN", "SSCAN", "KEYS", "DBSIZE"),

            // NCache manages persistence and replication itself.
            new Gap(NotInNCache,
                "SAVE", "BGSAVE", "BGREWRITEAOF", "LASTSAVE", "REPLICAOF", "SLAVEOF",
                "FAILOVER", "SYNC", "PSYNC", "WAIT", "WAITAOF", "REPLCONF"),

            // NCache reports replication through its own topology, not Redis's role model.
            new Gap(NotInNCache,
                "ROLE"),

            // These would bypass NCache's own management surface, which is where they belong.
            //
            // FLUSHDB and FLUSHALL were listed here, on the reasoning that emptying a cache
            // belongs to NCache's own management surface. They are implemented now: they map
            // cleanly onto Cache.Clear, which is the same call the protobuf ClearCommand makes,
            // so refusing them was withholding something the engine already exposes.
            new Gap(NotInNCache,
                "SHUTDOWN", "DEBUG", "MONITOR", "SLOWLOG", "LATENCY", "MEMORY",
                "LOLWUT", "MODULE"),

            // There is no identity on this port to grant anything to, in any edition: no users,
            // no per-key permissions, and nothing for a rule to name.
            new Gap(NotInNCache,
                "ACL"),

            // An NCache cache is a single keyspace: database 0 and nothing else.
            new Gap(NotInNCache,
                "MOVE", "SWAPDB"),

            // ======== served by NCache Enterprise, not by this build ========
            //
            // Each of these rests on an Enterprise engine feature: the distributed list, set,
            // dictionary and counter data types, stream storage with consumer groups, the
            // cluster-wide key lock an in-place partial rewrite needs, and RESP-level slot routing
            // (CRC16 plus MOVED). The commands are real and the limit is the edition, so they are
            // answered differently from everything above.

            new Gap(NotInOss,
                "LPUSH", "RPUSH", "LPUSHX", "RPUSHX", "LPOP", "RPOP",
                "LLEN", "LINDEX", "LSET", "LRANGE"),

            new Gap(NotInOss,
                "SADD", "SREM", "SISMEMBER", "SCARD", "SMEMBERS", "SPOP", "SRANDMEMBER"),

            new Gap(NotInOss,
                "HSET", "HMSET", "HGET", "HMGET", "HDEL", "HEXISTS", "HSETNX",
                "HLEN", "HSTRLEN", "HGETALL", "HKEYS", "HVALS"),

            // The per-field TTL family Redis added in 7.4. Enterprise serves it; without a hash
            // type there is nothing here to attach a field expiry to. Listed rather than left out
            // because a real Redis command must never answer "unknown command" - that tells a
            // client it is talking to an older server and should try something else, when the
            // truth is that this edition will never serve it. HGETDEL is the one member of the
            // family no edition implements, so it keeps its own gap above.
            new Gap(NotInOss,
                "HEXPIRE", "HPEXPIRE", "HEXPIREAT", "HPEXPIREAT",
                "HTTL", "HPTTL", "HEXPIRETIME", "HPEXPIRETIME",
                "HPERSIST", "HGETEX", "HSETEX"),

            new Gap(NotInOss,
                "XADD", "XLEN", "XRANGE", "XREVRANGE", "XDEL", "XTRIM", "XREAD",
                "XGROUP", "XREADGROUP", "XACK", "XINFO", "XPENDING", "XCLAIM"),

            new Gap(NotInOss,
                "CLUSTER"),

            // Enterprise checks a password on this port - shared, from its own configuration - and
            // this build has no gate to check one against. Refusing is the only honest answer: a
            // server that accepted AUTH and answered +OK would tell a client it had authenticated
            // when nothing had been verified, which is worse than having no authentication, and it
            // is what this listener used to do. The HELLO AUTH clause is refused the same way.
            new Gap(NotInOss,
                "AUTH"),
        };

        private static readonly Dictionary<string, string> Reasons = Build();

        private static Dictionary<string, string> Build()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var gap in Gaps)
                foreach (var command in gap.Commands)
                    map[command] = gap.Reason;
            return map;
        }

        /// <summary>True when this is a real Redis command that NCache cannot serve.</summary>
        public static bool IsKnownButUnsupported(string command)
        {
            return command != null && Reasons.ContainsKey(command);
        }

        /// <summary>
        /// Writes the refusal. Two distinct shapes, because a client that sees "unsupported" knows
        /// to stop asking, while one that sees "unknown" may simply be talking to an older server.
        /// </summary>
        public static RespCommandOutcome Write(RespRequest request, RespWriter writer)
        {
            string name = request.Name ?? "?";
            string reason;

            if (Reasons.TryGetValue(name, out reason))
            {
                writer.WriteError("ERR unsupported command '" + name + "': " + reason);
            }
            else
            {
                writer.WriteError("ERR unknown command '" + name +
                                  "', with args beginning with: " + request.DescribeArgs());
            }

            return RespCommandOutcome.Continue;
        }

        /// <summary>
        /// Why a refused command is refused, or null when this table does not refuse it. The
        /// compatibility layer throws with this text, so the reason is stated once - here - and not
        /// restated, or re-wrapped, by every layer that reports it.
        /// </summary>
        public static string ReasonFor(string command)
        {
            string needs;
            return command != null && Reasons.TryGetValue(command, out needs) ? needs : null;
        }

        /// <summary>Every command name this table refuses - used by the conformance test.</summary>
        public static IEnumerable<string> AllUnsupported()
        {
            return Reasons.Keys;
        }
    }
}
