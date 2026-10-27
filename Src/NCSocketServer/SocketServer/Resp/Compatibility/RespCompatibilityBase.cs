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

namespace Alachisoft.NCache.SocketServer.Resp.Compatibility
{
    /// <summary>
    /// What this build refuses, stated once.
    /// <para>
    /// The Enterprise build puts these refusals in default interface members. That needs
    /// C# 8 and a runtime that supports them, and this assembly targets netstandard2.0 at
    /// LangVersion 7.3 - so the refusals live here instead, in an abstract base a provider
    /// derives from. The effect is the same: a command this build cannot serve throws
    /// <see cref="RespNotSupportedException"/> carrying the reason
    /// <see cref="RespUnsupported"/> gives, and the reason is written in one place rather
    /// than repeated by every layer that reports it.
    /// </para>
    /// <para>
    /// The 66 commands this build DOES serve are left abstract below, so a provider that
    /// forgets one fails to compile. That is the property the layer exists for, and it is
    /// the reason the refused members are implemented here rather than everything being
    /// implemented here and overridden selectively.
    /// </para>
    /// <para>
    /// When Open Source gains a capability, delete the override here and the command
    /// becomes a compiler error at every provider until it is written.
    /// </para>
    /// </summary>
    internal abstract class RespCompatibilityBase : IRespCompatibility
    {
        // ------------------------------------------------------------------
        // Refused by this build: 144 members.
        // ------------------------------------------------------------------

        /// <summary>ASKING. Always throws in this build.</summary>
        public virtual RespReply Asking(RespArgs args)
        {
            throw RespNotSupported.For("ASKING");
        }

        /// <summary>READONLY. Always throws in this build.</summary>
        public virtual RespReply ReadOnly(RespArgs args)
        {
            throw RespNotSupported.For("READONLY");
        }

        /// <summary>READWRITE. Always throws in this build.</summary>
        public virtual RespReply ReadWrite(RespArgs args)
        {
            throw RespNotSupported.For("READWRITE");
        }

        /// <summary>WAITAOF. Always throws in this build.</summary>
        public virtual RespReply WaitForAppendOnlyFile(RespArgs args)
        {
            throw RespNotSupported.For("WAITAOF");
        }

        /// <summary>REPLCONF. Always throws in this build.</summary>
        public virtual RespReply ReplicationConfig(RespArgs args)
        {
            throw RespNotSupported.For("REPLCONF");
        }

        /// <summary>MODULE. Always throws in this build.</summary>
        public virtual RespReply Module(RespArgs args)
        {
            throw RespNotSupported.For("MODULE");
        }

        /// <summary>HGETDEL. Always throws in this build.</summary>
        public virtual RespReply HashGetDelete(RespArgs args)
        {
            throw RespNotSupported.For("HGETDEL");
        }

        /// <summary>RESTORE-ASKING. Always throws in this build.</summary>
        public virtual RespReply RestoreAsking(RespArgs args)
        {
            throw RespNotSupported.For("RESTORE-ASKING");
        }

        /// <summary>ACL. Always throws in this build.</summary>
        public virtual RespReply Acl(RespArgs args)
        {
            throw RespNotSupported.For("ACL");
        }

        /// <summary>SETBIT. Always throws in this build.</summary>
        public virtual long SetBit(string key, long offset, bool value)
        {
            throw RespNotSupported.For("SETBIT");
        }

        /// <summary>GETBIT. Always throws in this build.</summary>
        public virtual bool GetBit(string key, long offset)
        {
            throw RespNotSupported.For("GETBIT");
        }

        /// <summary>BITCOUNT. Always throws in this build.</summary>
        public virtual long BitCount(string key, RespArgs range)
        {
            throw RespNotSupported.For("BITCOUNT");
        }

        /// <summary>BITPOS. Always throws in this build.</summary>
        public virtual long BitPosition(string key, bool value, RespArgs range)
        {
            throw RespNotSupported.For("BITPOS");
        }

        /// <summary>BITOP. Always throws in this build.</summary>
        public virtual long BitOperation(string operation, string destination, IList<string> sources)
        {
            throw RespNotSupported.For("BITOP");
        }

        /// <summary>BITFIELD, BITFIELD_RO. Always throws in this build.</summary>
        public virtual RespReply BitField(string key, RespArgs operations, bool readOnly)
        {
            throw RespNotSupported.For("BITFIELD");
        }

        /// <summary>CLUSTER. Always throws in this build.</summary>
        public virtual RespReply Cluster(RespArgs args)
        {
            throw RespNotSupported.For("CLUSTER");
        }

        /// <summary>GEOADD. Always throws in this build.</summary>
        public virtual long GeoAdd(string key, RespArgs args)
        {
            throw RespNotSupported.For("GEOADD");
        }

        /// <summary>GEOPOS. Always throws in this build.</summary>
        public virtual RespReply GeoPosition(string key, IList<byte[]> members)
        {
            throw RespNotSupported.For("GEOPOS");
        }

        /// <summary>GEODIST. Always throws in this build.</summary>
        public virtual double? GeoDistance(string key, byte[] first, byte[] second, string unit)
        {
            throw RespNotSupported.For("GEODIST");
        }

        /// <summary>GEOHASH. Always throws in this build.</summary>
        public virtual IList<byte[]> GeoHash(string key, IList<byte[]> members)
        {
            throw RespNotSupported.For("GEOHASH");
        }

        /// <summary>GEOSEARCH. Always throws in this build.</summary>
        public virtual RespReply GeoSearch(string key, RespArgs args)
        {
            throw RespNotSupported.For("GEOSEARCH");
        }

        /// <summary>GEOSEARCHSTORE. Always throws in this build.</summary>
        public virtual long GeoSearchAndStore(string destination, string key, RespArgs args)
        {
            throw RespNotSupported.For("GEOSEARCHSTORE");
        }

        /// <summary>GEORADIUS, GEORADIUS_RO. Always throws in this build.</summary>
        public virtual RespReply GeoRadius(string key, RespArgs args, bool readOnly)
        {
            throw RespNotSupported.For("GEORADIUS");
        }

        /// <summary>GEORADIUSBYMEMBER, GEORADIUSBYMEMBER_RO. Always throws in this build.</summary>
        public virtual RespReply GeoRadiusByMember(string key, RespArgs args, bool readOnly)
        {
            throw RespNotSupported.For("GEORADIUSBYMEMBER");
        }

        /// <summary>HSET, HMSET. Always throws in this build.</summary>
        public virtual long HashSet(string key, IList<RespField> fields)
        {
            throw RespNotSupported.For("HSET");
        }

        /// <summary>HGET. Always throws in this build.</summary>
        public virtual byte[] HashGet(string key, byte[] field)
        {
            throw RespNotSupported.For("HGET");
        }

        /// <summary>HMGET. Always throws in this build.</summary>
        public virtual IList<byte[]> HashGetMany(string key, IList<byte[]> fields)
        {
            throw RespNotSupported.For("HMGET");
        }

        /// <summary>HDEL. Always throws in this build.</summary>
        public virtual long HashDelete(string key, IList<byte[]> fields)
        {
            throw RespNotSupported.For("HDEL");
        }

        /// <summary>HEXISTS. Always throws in this build.</summary>
        public virtual bool HashExists(string key, byte[] field)
        {
            throw RespNotSupported.For("HEXISTS");
        }

        /// <summary>HSETNX. Always throws in this build.</summary>
        public virtual bool HashSetIfAbsent(string key, byte[] field, byte[] value)
        {
            throw RespNotSupported.For("HSETNX");
        }

        /// <summary>HLEN. Always throws in this build.</summary>
        public virtual long HashLength(string key)
        {
            throw RespNotSupported.For("HLEN");
        }

        /// <summary>HSTRLEN. Always throws in this build.</summary>
        public virtual long HashFieldLength(string key, byte[] field)
        {
            throw RespNotSupported.For("HSTRLEN");
        }

        /// <summary>HGETALL, HKEYS, HVALS. Always throws in this build.</summary>
        public virtual IList<RespField> HashEntries(string key)
        {
            throw RespNotSupported.For("HGETALL");
        }

        /// <summary>HINCRBY. Always throws in this build.</summary>
        public virtual long HashIncrementBy(string key, byte[] field, long delta)
        {
            throw RespNotSupported.For("HINCRBY");
        }

        /// <summary>HINCRBYFLOAT. Always throws in this build.</summary>
        public virtual double HashIncrementByFloat(string key, byte[] field, double delta)
        {
            throw RespNotSupported.For("HINCRBYFLOAT");
        }

        /// <summary>HRANDFIELD. Always throws in this build.</summary>
        public virtual RespReply HashRandomField(string key, RespArgs args)
        {
            throw RespNotSupported.For("HRANDFIELD");
        }

        /// <summary>HSCAN. Always throws in this build.</summary>
        public virtual RespReply HashScan(string key, RespArgs args)
        {
            throw RespNotSupported.For("HSCAN");
        }

        /// <summary>HEXPIRE, HPEXPIRE, HEXPIREAT, HPEXPIREAT. Always throws in this build.</summary>
        public virtual IList<long> HashExpire(string key, RespExpiry expiry, RespExpiryCondition condition, IList<byte[]> fields)
        {
            throw RespNotSupported.For("HEXPIRE");
        }

        /// <summary>HTTL, HPTTL. Always throws in this build.</summary>
        public virtual IList<long> HashTimeToLive(string key, IList<byte[]> fields)
        {
            throw RespNotSupported.For("HTTL");
        }

        /// <summary>HEXPIRETIME, HPEXPIRETIME. Always throws in this build.</summary>
        public virtual IList<long> HashExpireTime(string key, IList<byte[]> fields)
        {
            throw RespNotSupported.For("HEXPIRETIME");
        }

        /// <summary>HPERSIST. Always throws in this build.</summary>
        public virtual IList<long> HashPersist(string key, IList<byte[]> fields)
        {
            throw RespNotSupported.For("HPERSIST");
        }

        /// <summary>HGETEX. Always throws in this build.</summary>
        public virtual IList<byte[]> HashGetEx(string key, IList<byte[]> fields, RespExpiry expiry, bool persist)
        {
            throw RespNotSupported.For("HGETEX");
        }

        /// <summary>HSETEX. Always throws in this build.</summary>
        public virtual bool HashSetEx(string key, IList<RespField> fields, RespExpiry expiry, RespWriteCondition condition)
        {
            throw RespNotSupported.For("HSETEX");
        }

        /// <summary>PFADD. Always throws in this build.</summary>
        public virtual bool HyperLogLogAdd(string key, IList<byte[]> elements)
        {
            throw RespNotSupported.For("PFADD");
        }

        /// <summary>PFCOUNT. Always throws in this build.</summary>
        public virtual long HyperLogLogCount(IList<string> keys)
        {
            throw RespNotSupported.For("PFCOUNT");
        }

        /// <summary>PFMERGE. Always throws in this build.</summary>
        public virtual void HyperLogLogMerge(string destination, IList<string> sources)
        {
            throw RespNotSupported.For("PFMERGE");
        }

        /// <summary>PFDEBUG. Always throws in this build.</summary>
        public virtual RespReply HyperLogLogDebug(RespArgs args)
        {
            throw RespNotSupported.For("PFDEBUG");
        }

        /// <summary>PFSELFTEST. Always throws in this build.</summary>
        public virtual RespReply HyperLogLogSelfTest()
        {
            throw RespNotSupported.For("PFSELFTEST");
        }

        /// <summary>KEYS. Always throws in this build.</summary>
        public virtual IList<string> Keys(byte[] pattern)
        {
            throw RespNotSupported.For("KEYS");
        }

        /// <summary>SCAN. Always throws in this build.</summary>
        public virtual RespReply Scan(RespArgs args)
        {
            throw RespNotSupported.For("SCAN");
        }

        /// <summary>MOVE. Always throws in this build.</summary>
        public virtual bool MoveToDatabase(string key, int database)
        {
            throw RespNotSupported.For("MOVE");
        }

        /// <summary>MIGRATE. Always throws in this build.</summary>
        public virtual RespReply Migrate(RespArgs args)
        {
            throw RespNotSupported.For("MIGRATE");
        }

        /// <summary>SORT, SORT_RO. Always throws in this build.</summary>
        public virtual RespReply Sort(string key, RespArgs args, bool readOnly)
        {
            throw RespNotSupported.For("SORT");
        }

        /// <summary>LPUSH, RPUSH, LPUSHX, RPUSHX. Always throws in this build.</summary>
        public virtual long ListPush(string key, RespListEnd end, IList<byte[]> values, bool onlyIfExists)
        {
            throw RespNotSupported.For("LPUSH");
        }

        /// <summary>LPOP, RPOP. Always throws in this build.</summary>
        public virtual IList<byte[]> ListPop(string key, RespListEnd end, int count)
        {
            throw RespNotSupported.For("LPOP");
        }

        /// <summary>LLEN. Always throws in this build.</summary>
        public virtual long ListLength(string key)
        {
            throw RespNotSupported.For("LLEN");
        }

        /// <summary>LINDEX. Always throws in this build.</summary>
        public virtual byte[] ListIndex(string key, long index)
        {
            throw RespNotSupported.For("LINDEX");
        }

        /// <summary>LSET. Always throws in this build.</summary>
        public virtual void ListSet(string key, long index, byte[] value)
        {
            throw RespNotSupported.For("LSET");
        }

        /// <summary>LRANGE. Always throws in this build.</summary>
        public virtual IList<byte[]> ListRange(string key, long start, long stop)
        {
            throw RespNotSupported.For("LRANGE");
        }

        /// <summary>LREM. Always throws in this build.</summary>
        public virtual long ListRemove(string key, long count, byte[] value)
        {
            throw RespNotSupported.For("LREM");
        }

        /// <summary>LPOS. Always throws in this build.</summary>
        public virtual RespReply ListPosition(string key, byte[] value, RespArgs args)
        {
            throw RespNotSupported.For("LPOS");
        }

        /// <summary>LINSERT. Always throws in this build.</summary>
        public virtual long ListInsert(string key, bool before, byte[] pivot, byte[] value)
        {
            throw RespNotSupported.For("LINSERT");
        }

        /// <summary>LTRIM. Always throws in this build.</summary>
        public virtual void ListTrim(string key, long start, long stop)
        {
            throw RespNotSupported.For("LTRIM");
        }

        /// <summary>BLPOP, BRPOP. Always throws in this build.</summary>
        public virtual RespReply ListBlockingPop(IList<string> keys, RespListEnd end, double timeoutSeconds)
        {
            throw RespNotSupported.For("BLPOP");
        }

        /// <summary>BLMOVE, BRPOPLPUSH. Always throws in this build.</summary>
        public virtual byte[] ListBlockingMove(string source, string destination, RespListEnd from, RespListEnd to, double timeoutSeconds)
        {
            throw RespNotSupported.For("BLMOVE");
        }

        /// <summary>BLMPOP. Always throws in this build.</summary>
        public virtual RespReply ListBlockingPopMany(RespArgs args)
        {
            throw RespNotSupported.For("BLMPOP");
        }

        /// <summary>RPOPLPUSH, LMOVE. Always throws in this build.</summary>
        public virtual byte[] ListMove(string source, string destination, RespListEnd from, RespListEnd to)
        {
            throw RespNotSupported.For("RPOPLPUSH");
        }

        /// <summary>LMPOP. Always throws in this build.</summary>
        public virtual RespReply ListPopMany(RespArgs args)
        {
            throw RespNotSupported.For("LMPOP");
        }

        /// <summary>EVAL, EVAL_RO. Always throws in this build.</summary>
        public virtual RespReply Eval(byte[] script, IList<string> keys, IList<byte[]> arguments, bool readOnly)
        {
            throw RespNotSupported.For("EVAL");
        }

        /// <summary>EVALSHA, EVALSHA_RO. Always throws in this build.</summary>
        public virtual RespReply EvalSha(string sha1, IList<string> keys, IList<byte[]> arguments, bool readOnly)
        {
            throw RespNotSupported.For("EVALSHA");
        }

        /// <summary>SCRIPT. Always throws in this build.</summary>
        public virtual RespReply Script(RespArgs args)
        {
            throw RespNotSupported.For("SCRIPT");
        }

        /// <summary>FUNCTION. Always throws in this build.</summary>
        public virtual RespReply Function(RespArgs args)
        {
            throw RespNotSupported.For("FUNCTION");
        }

        /// <summary>FCALL, FCALL_RO. Always throws in this build.</summary>
        public virtual RespReply FunctionCall(string name, IList<string> keys, IList<byte[]> arguments, bool readOnly)
        {
            throw RespNotSupported.For("FCALL");
        }

        /// <summary>DBSIZE. Always throws in this build.</summary>
        public virtual long DatabaseSize()
        {
            throw RespNotSupported.For("DBSIZE");
        }

        /// <summary>SHUTDOWN. Always throws in this build.</summary>
        public virtual void Shutdown(RespArgs args)
        {
            throw RespNotSupported.For("SHUTDOWN");
        }

        /// <summary>DEBUG. Always throws in this build.</summary>
        public virtual RespReply Debug(RespArgs args)
        {
            throw RespNotSupported.For("DEBUG");
        }

        /// <summary>MONITOR. Always throws in this build.</summary>
        public virtual void Monitor()
        {
            throw RespNotSupported.For("MONITOR");
        }

        /// <summary>SLOWLOG. Always throws in this build.</summary>
        public virtual RespReply SlowLog(RespArgs args)
        {
            throw RespNotSupported.For("SLOWLOG");
        }

        /// <summary>SWAPDB. Always throws in this build.</summary>
        public virtual void SwapDatabases(int first, int second)
        {
            throw RespNotSupported.For("SWAPDB");
        }

        /// <summary>LATENCY. Always throws in this build.</summary>
        public virtual RespReply Latency(RespArgs args)
        {
            throw RespNotSupported.For("LATENCY");
        }

        /// <summary>MEMORY. Always throws in this build.</summary>
        public virtual RespReply Memory(RespArgs args)
        {
            throw RespNotSupported.For("MEMORY");
        }

        /// <summary>LOLWUT. Always throws in this build.</summary>
        public virtual RespReply Lolwut(RespArgs args)
        {
            throw RespNotSupported.For("LOLWUT");
        }

        /// <summary>SAVE. Always throws in this build.</summary>
        public virtual void Save()
        {
            throw RespNotSupported.For("SAVE");
        }

        /// <summary>BGSAVE. Always throws in this build.</summary>
        public virtual void BackgroundSave(bool scheduleOnly)
        {
            throw RespNotSupported.For("BGSAVE");
        }

        /// <summary>BGREWRITEAOF. Always throws in this build.</summary>
        public virtual void BackgroundRewriteAppendOnlyFile()
        {
            throw RespNotSupported.For("BGREWRITEAOF");
        }

        /// <summary>LASTSAVE. Always throws in this build.</summary>
        public virtual long LastSave()
        {
            throw RespNotSupported.For("LASTSAVE");
        }

        /// <summary>REPLICAOF, SLAVEOF. Always throws in this build.</summary>
        public virtual void ReplicaOf(string host, int port)
        {
            throw RespNotSupported.For("REPLICAOF");
        }

        /// <summary>FAILOVER. Always throws in this build.</summary>
        public virtual RespReply Failover(RespArgs args)
        {
            throw RespNotSupported.For("FAILOVER");
        }

        /// <summary>SYNC. Always throws in this build.</summary>
        public virtual RespReply Sync()
        {
            throw RespNotSupported.For("SYNC");
        }

        /// <summary>PSYNC. Always throws in this build.</summary>
        public virtual RespReply PartialSync(RespArgs args)
        {
            throw RespNotSupported.For("PSYNC");
        }

        /// <summary>WAIT. Always throws in this build.</summary>
        public virtual long WaitForReplicas(int replicas, long timeoutMilliseconds)
        {
            throw RespNotSupported.For("WAIT");
        }

        /// <summary>ROLE. Always throws in this build.</summary>
        public virtual RespReply Role()
        {
            throw RespNotSupported.For("ROLE");
        }

        /// <summary>SADD. Always throws in this build.</summary>
        public virtual long SetAdd(string key, IList<byte[]> members)
        {
            throw RespNotSupported.For("SADD");
        }

        /// <summary>SREM. Always throws in this build.</summary>
        public virtual long SetRemove(string key, IList<byte[]> members)
        {
            throw RespNotSupported.For("SREM");
        }

        /// <summary>SISMEMBER. Always throws in this build.</summary>
        public virtual bool SetContains(string key, byte[] member)
        {
            throw RespNotSupported.For("SISMEMBER");
        }

        /// <summary>SCARD. Always throws in this build.</summary>
        public virtual long SetCardinality(string key)
        {
            throw RespNotSupported.For("SCARD");
        }

        /// <summary>SMEMBERS. Always throws in this build.</summary>
        public virtual IList<byte[]> SetMembers(string key)
        {
            throw RespNotSupported.For("SMEMBERS");
        }

        /// <summary>SPOP. Always throws in this build.</summary>
        public virtual IList<byte[]> SetPop(string key, int count)
        {
            throw RespNotSupported.For("SPOP");
        }

        /// <summary>SRANDMEMBER. Always throws in this build.</summary>
        public virtual IList<byte[]> SetRandomMembers(string key, int count)
        {
            throw RespNotSupported.For("SRANDMEMBER");
        }

        /// <summary>SUNION, SINTER, SDIFF. Always throws in this build.</summary>
        public virtual IList<byte[]> SetCombine(string operation, IList<string> keys)
        {
            throw RespNotSupported.For("SUNION");
        }

        /// <summary>SUNIONSTORE, SINTERSTORE, SDIFFSTORE. Always throws in this build.</summary>
        public virtual long SetCombineAndStore(string operation, string destination, IList<string> keys)
        {
            throw RespNotSupported.For("SUNIONSTORE");
        }

        /// <summary>SINTERCARD. Always throws in this build.</summary>
        public virtual long SetIntersectionCardinality(IList<string> keys, long limit)
        {
            throw RespNotSupported.For("SINTERCARD");
        }

        /// <summary>SMISMEMBER. Always throws in this build.</summary>
        public virtual IList<bool> SetContainsMany(string key, IList<byte[]> members)
        {
            throw RespNotSupported.For("SMISMEMBER");
        }

        /// <summary>SMOVE. Always throws in this build.</summary>
        public virtual bool SetMove(string source, string destination, byte[] member)
        {
            throw RespNotSupported.For("SMOVE");
        }

        /// <summary>SSCAN. Always throws in this build.</summary>
        public virtual RespReply SetScan(string key, RespArgs args)
        {
            throw RespNotSupported.For("SSCAN");
        }

        /// <summary>ZADD. Always throws in this build.</summary>
        public virtual long SortedSetAdd(string key, IList<RespScoredMember> members, RespArgs options)
        {
            throw RespNotSupported.For("ZADD");
        }

        /// <summary>ZINCRBY. Always throws in this build.</summary>
        public virtual double SortedSetIncrementBy(string key, byte[] member, double delta)
        {
            throw RespNotSupported.For("ZINCRBY");
        }

        /// <summary>ZREM. Always throws in this build.</summary>
        public virtual long SortedSetRemove(string key, IList<byte[]> members)
        {
            throw RespNotSupported.For("ZREM");
        }

        /// <summary>ZSCORE. Always throws in this build.</summary>
        public virtual double? SortedSetScore(string key, byte[] member)
        {
            throw RespNotSupported.For("ZSCORE");
        }

        /// <summary>ZMSCORE. Always throws in this build.</summary>
        public virtual IList<double?> SortedSetScores(string key, IList<byte[]> members)
        {
            throw RespNotSupported.For("ZMSCORE");
        }

        /// <summary>ZRANK, ZREVRANK. Always throws in this build.</summary>
        public virtual long? SortedSetRank(string key, byte[] member, bool reverse)
        {
            throw RespNotSupported.For("ZRANK");
        }

        /// <summary>ZRANGE, ZREVRANGE, ZRANGEBYSCORE, ZREVRANGEBYSCORE, ZRANGEBYLEX, ZREVRANGEBYLEX. Always throws in this build.</summary>
        public virtual RespReply SortedSetRange(string key, RespArgs args)
        {
            throw RespNotSupported.For("ZRANGE");
        }

        /// <summary>ZRANGESTORE. Always throws in this build.</summary>
        public virtual long SortedSetRangeAndStore(string destination, string key, RespArgs args)
        {
            throw RespNotSupported.For("ZRANGESTORE");
        }

        /// <summary>ZREMRANGEBYRANK, ZREMRANGEBYSCORE, ZREMRANGEBYLEX. Always throws in this build.</summary>
        public virtual long SortedSetRemoveRange(string key, RespArgs args)
        {
            throw RespNotSupported.For("ZREMRANGEBYRANK");
        }

        /// <summary>ZCARD. Always throws in this build.</summary>
        public virtual long SortedSetCardinality(string key)
        {
            throw RespNotSupported.For("ZCARD");
        }

        /// <summary>ZCOUNT, ZLEXCOUNT. Always throws in this build.</summary>
        public virtual long SortedSetCount(string key, RespArgs range)
        {
            throw RespNotSupported.For("ZCOUNT");
        }

        /// <summary>ZPOPMIN, ZPOPMAX. Always throws in this build.</summary>
        public virtual IList<RespScoredMember> SortedSetPop(string key, bool highest, int count)
        {
            throw RespNotSupported.For("ZPOPMIN");
        }

        /// <summary>BZPOPMIN, BZPOPMAX. Always throws in this build.</summary>
        public virtual RespReply SortedSetBlockingPop(IList<string> keys, bool highest, double timeoutSeconds)
        {
            throw RespNotSupported.For("BZPOPMIN");
        }

        /// <summary>ZRANDMEMBER. Always throws in this build.</summary>
        public virtual RespReply SortedSetRandomMember(string key, RespArgs args)
        {
            throw RespNotSupported.For("ZRANDMEMBER");
        }

        /// <summary>ZSCAN. Always throws in this build.</summary>
        public virtual RespReply SortedSetScan(string key, RespArgs args)
        {
            throw RespNotSupported.For("ZSCAN");
        }

        /// <summary>ZUNION, ZINTER, ZDIFF. Always throws in this build.</summary>
        public virtual RespReply SortedSetCombine(string operation, RespArgs args)
        {
            throw RespNotSupported.For("ZUNION");
        }

        /// <summary>ZUNIONSTORE, ZINTERSTORE, ZDIFFSTORE. Always throws in this build.</summary>
        public virtual long SortedSetCombineAndStore(string operation, string destination, RespArgs args)
        {
            throw RespNotSupported.For("ZUNIONSTORE");
        }

        /// <summary>ZINTERCARD. Always throws in this build.</summary>
        public virtual long SortedSetIntersectionCardinality(RespArgs args)
        {
            throw RespNotSupported.For("ZINTERCARD");
        }

        /// <summary>ZMPOP. Always throws in this build.</summary>
        public virtual RespReply SortedSetPopMany(RespArgs args)
        {
            throw RespNotSupported.For("ZMPOP");
        }

        /// <summary>BZMPOP. Always throws in this build.</summary>
        public virtual RespReply SortedSetBlockingPopMany(RespArgs args)
        {
            throw RespNotSupported.For("BZMPOP");
        }

        /// <summary>XADD. Always throws in this build.</summary>
        public virtual RespStreamId StreamAdd(string key, RespStreamId? id, IList<RespField> fields, RespArgs options)
        {
            throw RespNotSupported.For("XADD");
        }

        /// <summary>XLEN. Always throws in this build.</summary>
        public virtual long StreamLength(string key)
        {
            throw RespNotSupported.For("XLEN");
        }

        /// <summary>XRANGE, XREVRANGE. Always throws in this build.</summary>
        public virtual IList<RespStreamEntryView> StreamRange(string key, RespArgs args, bool reverse)
        {
            throw RespNotSupported.For("XRANGE");
        }

        /// <summary>XDEL. Always throws in this build.</summary>
        public virtual long StreamDelete(string key, IList<RespStreamId> ids)
        {
            throw RespNotSupported.For("XDEL");
        }

        /// <summary>XTRIM. Always throws in this build.</summary>
        public virtual long StreamTrim(string key, RespArgs args)
        {
            throw RespNotSupported.For("XTRIM");
        }

        /// <summary>XREAD. Always throws in this build.</summary>
        public virtual IList<RespStreamSlice> StreamRead(RespArgs args)
        {
            throw RespNotSupported.For("XREAD");
        }

        /// <summary>XGROUP. Always throws in this build.</summary>
        public virtual RespReply StreamGroup(RespArgs args)
        {
            throw RespNotSupported.For("XGROUP");
        }

        /// <summary>XREADGROUP. Always throws in this build.</summary>
        public virtual IList<RespStreamSlice> StreamReadGroup(RespArgs args)
        {
            throw RespNotSupported.For("XREADGROUP");
        }

        /// <summary>XACK. Always throws in this build.</summary>
        public virtual long StreamAcknowledge(string key, string group, IList<RespStreamId> ids)
        {
            throw RespNotSupported.For("XACK");
        }

        /// <summary>XINFO. Always throws in this build.</summary>
        public virtual RespReply StreamInfo(RespArgs args)
        {
            throw RespNotSupported.For("XINFO");
        }

        /// <summary>XPENDING. Always throws in this build.</summary>
        public virtual RespReply StreamPending(RespArgs args)
        {
            throw RespNotSupported.For("XPENDING");
        }

        /// <summary>XCLAIM. Always throws in this build.</summary>
        public virtual RespReply StreamClaim(RespArgs args)
        {
            throw RespNotSupported.For("XCLAIM");
        }

        /// <summary>XAUTOCLAIM. Always throws in this build.</summary>
        public virtual RespReply StreamAutoClaim(RespArgs args)
        {
            throw RespNotSupported.For("XAUTOCLAIM");
        }

        /// <summary>XSETID. Always throws in this build.</summary>
        public virtual void StreamSetId(string key, RespStreamId id, RespArgs args)
        {
            throw RespNotSupported.For("XSETID");
        }

        /// <summary>MULTI. Always throws in this build.</summary>
        public virtual void Multi()
        {
            throw RespNotSupported.For("MULTI");
        }

        /// <summary>EXEC. Always throws in this build.</summary>
        public virtual RespReply Exec()
        {
            throw RespNotSupported.For("EXEC");
        }

        /// <summary>DISCARD. Always throws in this build.</summary>
        public virtual void Discard()
        {
            throw RespNotSupported.For("DISCARD");
        }

        /// <summary>WATCH. Always throws in this build.</summary>
        public virtual void Watch(IList<string> keys)
        {
            throw RespNotSupported.For("WATCH");
        }

        /// <summary>UNWATCH. Always throws in this build.</summary>
        public virtual void Unwatch()
        {
            throw RespNotSupported.For("UNWATCH");
        }

        // ------------------------------------------------------------------
        // Served by this build: 49 members, left abstract so the compiler
        // requires a provider to supply every one of them.
        // ------------------------------------------------------------------

        /// <summary>AUTH.</summary>
        public abstract bool Authenticate(string username, byte[] password);

        /// <summary>HELLO.</summary>
        public abstract RespReply Hello(RespArgs args);

        /// <summary>PING.</summary>
        public abstract RespReply Ping(byte[] message);

        /// <summary>ECHO.</summary>
        public abstract byte[] Echo(byte[] message);

        /// <summary>QUIT.</summary>
        public abstract void Quit();

        /// <summary>RESET.</summary>
        public abstract void Reset();

        /// <summary>SELECT.</summary>
        public abstract void Select(int database);

        /// <summary>CLIENT.</summary>
        public abstract RespReply Client(RespArgs args);

        /// <summary>DEL.</summary>
        public abstract long Delete(IList<string> keys);

        /// <summary>UNLINK.</summary>
        public abstract long Unlink(IList<string> keys);

        /// <summary>EXISTS.</summary>
        public abstract long Exists(IList<string> keys);

        /// <summary>TYPE.</summary>
        public abstract RespKeyType KeyType(string key);

        /// <summary>TOUCH.</summary>
        public abstract long Touch(IList<string> keys);

        /// <summary>EXPIRE, PEXPIRE, EXPIREAT, PEXPIREAT.</summary>
        public abstract bool Expire(string key, RespExpiry expiry, RespExpiryCondition condition);

        /// <summary>TTL, PTTL.</summary>
        public abstract long TimeToLiveMilliseconds(string key);

        /// <summary>EXPIRETIME, PEXPIRETIME.</summary>
        public abstract long ExpireTimeMilliseconds(string key);

        /// <summary>PERSIST.</summary>
        public abstract bool Persist(string key);

        /// <summary>RANDOMKEY.</summary>
        public abstract string RandomKey();

        /// <summary>RENAME.</summary>
        public abstract void Rename(string key, string newKey);

        /// <summary>RENAMENX.</summary>
        public abstract bool RenameIfAbsent(string key, string newKey);

        /// <summary>COPY.</summary>
        public abstract bool Copy(string source, string destination, bool replace);

        /// <summary>DUMP.</summary>
        public abstract byte[] Dump(string key);

        /// <summary>RESTORE.</summary>
        public abstract void Restore(string key, RespExpiry expiry, byte[] serialized, bool replace);

        /// <summary>OBJECT.</summary>
        public abstract RespReply ObjectIntrospection(RespArgs args);

        /// <summary>LCS.</summary>
        public abstract RespReply LongestCommonSubsequence(string first, string second, RespArgs args);

        /// <summary>PUBLISH, SPUBLISH.</summary>
        public abstract long Publish(string channel, byte[] message, bool shard);

        /// <summary>SUBSCRIBE, PSUBSCRIBE, SSUBSCRIBE.</summary>
        public abstract RespReply Subscribe(IList<byte[]> channels, bool pattern, bool shard);

        /// <summary>UNSUBSCRIBE, PUNSUBSCRIBE, SUNSUBSCRIBE.</summary>
        public abstract RespReply Unsubscribe(IList<byte[]> channels, bool pattern, bool shard);

        /// <summary>PUBSUB.</summary>
        public abstract RespReply PubSubIntrospection(RespArgs args);

        /// <summary>INFO.</summary>
        public abstract RespReply Info(RespArgs sections);

        /// <summary>CONFIG.</summary>
        public abstract RespReply Config(RespArgs args);

        /// <summary>COMMAND.</summary>
        public abstract RespReply Command(RespArgs args);

        /// <summary>TIME.</summary>
        public abstract RespReply Time();

        /// <summary>FLUSHDB.</summary>
        public abstract void FlushDatabase(bool asynchronous);

        /// <summary>FLUSHALL.</summary>
        public abstract void FlushAll(bool asynchronous);

        /// <summary>GET.</summary>
        public abstract byte[] Get(string key);

        /// <summary>SET, SETEX, PSETEX, SETNX.</summary>
        public abstract RespSetOutcome Set(string key, byte[] value, RespExpiry expiry, RespWriteCondition condition, bool returnPrevious);

        /// <summary>GETSET.</summary>
        public abstract byte[] GetSet(string key, byte[] value);

        /// <summary>GETDEL.</summary>
        public abstract byte[] GetDelete(string key);

        /// <summary>GETEX.</summary>
        public abstract byte[] GetWithExpiry(string key, RespExpiry expiry, bool persist);

        /// <summary>MGET.</summary>
        public abstract IList<byte[]> GetMany(IList<string> keys);

        /// <summary>MSET.</summary>
        public abstract void SetMany(IList<KeyValuePair<string, byte[]>> pairs);

        /// <summary>APPEND.</summary>
        public abstract long Append(string key, byte[] value);

        /// <summary>STRLEN.</summary>
        public abstract long StringLength(string key);

        /// <summary>GETRANGE, SUBSTR.</summary>
        public abstract byte[] GetRange(string key, long start, long end);

        /// <summary>SETRANGE.</summary>
        public abstract long SetRange(string key, long offset, byte[] value);

        /// <summary>INCR, DECR, INCRBY, DECRBY.</summary>
        public abstract long IncrementBy(string key, long delta);

        /// <summary>MSETNX.</summary>
        public abstract bool SetManyIfAllAbsent(IList<KeyValuePair<string, byte[]>> pairs);

        /// <summary>INCRBYFLOAT.</summary>
        public abstract double IncrementByFloat(string key, double delta);

    }
}
