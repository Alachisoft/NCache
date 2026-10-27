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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.CacheManagement;
using Alachisoft.NCache.Runtime.Caching;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// The only place RESP touches the cache engine.
    /// <para>
    /// Every method here calls the same <see cref="Alachisoft.NCache.Caching.Cache"/> entry point
    /// the corresponding protobuf command handler calls, with the same arguments - no parallel cache
    /// logic, and nothing reimplemented. Where the protobuf handler builds a
    /// <c>BitSet</c>/<c>ExpirationHint</c>/<c>OperationContext</c>, this does the same.
    /// </para>
    /// <para>
    /// Scope: each RESP command maps to one engine call wherever one exists. Where Redis is
    /// atomic and NCache offers no single call (PERSIST, SET XX, SET KEEPTTL, GETSET) the calls are
    /// made in sequence with no lock, so a concurrent writer on the same key can be lost. See
    /// poc-divergences.md sections 2 to 4.
    /// </para>
    /// </summary>
    internal sealed class RespCacheGateway
    {
        private const string Module = "RespCacheGateway";

        private readonly string _cacheName;
        private readonly object _resolveLock = new object();
        private volatile Alachisoft.NCache.Caching.Cache _cache;

        // The instance that raised CacheStopped. It keeps reporting IsRunning for seconds while it
        // disposes, so it must not be served or resolved again; a restarted cache is a new instance.
        private volatile Alachisoft.NCache.Caching.Cache _stoppedCache;

        public RespCacheGateway(string cacheName, int respPort, System.Net.IPAddress selfIp)
        {
            _cacheName = cacheName;
        }

        #region Key ownership

        /// <summary>
        /// The engine's own statistics for this cache, or null when it is not up yet. Backs the
        /// Memory, Stats and Keyspace sections of INFO.
        /// </summary>
        public Alachisoft.NCache.Caching.Statistics.CacheStatistics GetStatistics()
        {
            try
            {
                var cache = TryPeekCache();
                return cache == null ? null : cache.Statistics;
            }
            catch (Exception e)
            {
                RespLog.Detail(Module + ".GetStatistics", e.Message);
                return null;
            }
        }

        /// <summary>Bytes currently held by this cache, or 0 while it is starting.</summary>
        /// <summary>Live item count, as opposed to the periodically refreshed Statistics snapshot.</summary>
        public long GetKeyCount()
        {
            try
            {
                var cache = TryPeekCache();
                return cache == null ? 0 : cache.Count;
            }
            catch (Exception e)
            {
                RespLog.Detail(Module + ".GetKeyCount", e.Message);
                return 0;
            }
        }

        public long GetCacheSize()
        {
            try
            {
                var cache = TryPeekCache();
                return cache == null ? 0 : cache.Size;
            }
            catch (Exception e)
            {
                RespLog.Detail(Module + ".GetCacheSize", e.Message);
                return 0;
            }
        }

        /// <summary>Resolves the cache if it is running; null instead of throwing while it is not.</summary>
        private Alachisoft.NCache.Caching.Cache TryPeekCache()
        {
            try { return GetCache(); }
            catch { return null; }
        }

        /// <summary>
        /// The resolved cache, for the parts of the RESP layer that drive engine features this
        /// gateway does not wrap - messaging and the collection data types.
        /// <para>
        /// Returns null rather than throwing while the cache is still starting, so a poller can
        /// simply skip a cycle instead of having to catch.
        /// </para>
        /// </summary>
        internal Alachisoft.NCache.Caching.Cache CacheOrNull
        {
            get { return TryPeekCache(); }
        }

        /// <summary>A fresh operation context, matching the one the key-value paths use.</summary>
        internal static OperationContext NewOperationContext()
        {
            return NewContext();
        }

        /// <summary>Wraps a payload the way the engine expects to receive it.</summary>
        internal static object ToUserBinaryObject(Alachisoft.NCache.Caching.Cache cache, byte[] value)
        {
            return ToUserBinary(cache, value);
        }

        #endregion

        #region Cache resolution

        /// <summary>
        /// Resolves the cache on first use and keeps it.
        /// <para>
        /// Resolution is deliberately lazy. The cache host binds its RESP port while starting up,
        /// but the cache itself is only created later, when the NCache service calls StartCache. A
        /// client that connects in that window gets <c>-LOADING</c> and can retry, which is exactly
        /// how a real Redis behaves while it loads its dataset.
        /// </para>
        /// </summary>
        private Alachisoft.NCache.Caching.Cache GetCache()
        {
            var cache = _cache;

            if (cache != null && cache.IsRunning) return cache;

            // Resolving while the stopped instance is still disposing blocks in the provider until
            // the stop completes, and every command would queue behind it.
            var stopping = _stoppedCache;
            if (cache == null && stopping != null && stopping.IsRunning)
            {
                throw new RespErrorMapper.CacheUnavailableException(
                    "NCache cache '" + _cacheName + "' is not running");
            }

            // The lookup runs outside _resolveLock. It takes the cache server's _rwLock, which
            // HostServer.StartCache holds as writer for the whole start and, while holding it, calls
            // back into this gateway through OnCacheStarted. Waiting on _rwLock while holding
            // _resolveLock deadlocked that start whenever a client connected during it. Two threads
            // resolving at once is harmless - both get the same instance.
            Alachisoft.NCache.Caching.Cache resolved = null;
            try
            {
                var provider = CacheProvider.Provider;
                if (provider != null)
                {
                    // Same acquisition the protobuf executer uses (SocketServer/NCache.cs).
                    // Empty credentials, and there are none to pass: this port has no
                    // authentication, so no caller identity ever reaches here to be threaded
                    // through to NCache's own security.
                    resolved = provider.GetCacheInstanceIgnoreReplica(
                        _cacheName.ToLower());
                }
            }
            catch (Exception e)
            {
                RespLog.Detail(Module + ".GetCache", "cache '" + _cacheName + "' not available yet: " + e.Message);
                resolved = null;
            }

            if (resolved == null || !resolved.IsRunning || ReferenceEquals(resolved, _stoppedCache))
                throw new RespErrorMapper.CacheUnavailableException(
                    "NCache cache '" + _cacheName + "' is not running");

            // Only publishing happens under the lock, so CacheStopped is subscribed exactly once
            // however many threads resolved concurrently.
            lock (_resolveLock)
            {
                if (_cache != null && _cache.IsRunning) return _cache;

                // The protobuf path stops serving a cache on this same event (SocketServer/NCache.cs);
                // without it RESP kept calling into the cache while it was being disposed.
                resolved.CacheStopped += OnCacheStopped;
                _cache = resolved;
                RespLog.Critical(Module + ".GetCache", "RESP listener attached to cache '" + _cacheName + "'.");
                return resolved;
            }
        }

        /// <summary>
        /// The cache if it is already resolved and running, otherwise null. Never looks it up, so
        /// it never waits on the cache server's lock - for callers on the accept path.
        /// </summary>
        internal Alachisoft.NCache.Caching.Cache ResolvedCacheOrNull
        {
            get
            {
                var cache = _cache;
                return cache != null && cache.IsRunning ? cache : null;
            }
        }

        /// <summary>
        /// Raised first thing in Cache.Dispose. From here on every command gets <c>-LOADING</c>
        /// instead of reaching a cache whose parts are being torn down.
        /// <para>
        /// <c>IsRunning</c> is not enough on its own: it reads <c>CacheImpl != null</c>, which
        /// Dispose clears last, after stopping the cluster services and disposing the cache context
        /// - so for as long as a stop takes, the cache still reports itself running while an
        /// operation entering it fails on a part that has already gone. That is the
        /// NullReferenceException a client saw when a node was stopped under it.
        /// </para>
        /// </summary>
        private void OnCacheStopped(string cacheId, EventContext eventContext)
        {
            // No _resolveLock: this runs on the thread stopping the cache, and a resolve holding
            // that lock could be waiting on the very stop this callback is part of.
            _stoppedCache = _cache;
            _cache = null;
            RespLog.Critical(Module + ".OnCacheStopped",
                "RESP listener detached from cache '" + _cacheName + "': the cache is stopping.");

            // Inline, and before anything is queued: this gives the listening port back, and the
            // port cannot wait. The service treats the cache as stopped as soon as Cache.Dispose
            // returns and may start a replacement host at once, and that host binds this same port.
            // Queuing the release made it a race against a thread-pool work item - one that RESP
            // itself can delay, since a blocking command parks a pool thread for as long as its
            // timeout. Closing a listening socket does not block, so it belongs here.
            var stopping = Stopping;
            if (stopping != null)
            {
                try { stopping(); }
                catch (Exception e) { RespLog.Error(Module + ".OnCacheStopped", e.ToString()); }
            }

            var stopped = Stopped;
            if (stopped != null)
            {
                // Off this thread, so disposing the live connections never holds up Cache.Dispose.
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { stopped(); }
                    catch (Exception e) { RespLog.Error(Module + ".OnCacheStopped", e.ToString()); }
                });
            }
        }

        /// <summary>
        /// Run once the cache has begun stopping. The listener uses it to drop its clients, as the
        /// protobuf path does on the same event.
        /// </summary>
        internal Action Stopped { get; set; }

        /// <summary>
        /// Run inline as the cache begins stopping, before <see cref="Stopped"/> is queued. The
        /// listener uses it to release its port, which is the one part of the teardown that has to
        /// be finished before <c>Cache.Dispose</c> returns. Must not block.
        /// </summary>
        internal Action Stopping { get; set; }

        /// <summary>
        /// A command that was already inside the cache when it began stopping fails with whatever
        /// the half-disposed cache throws; report that as the cache not running.
        /// </summary>
        internal Exception TranslateStopped(Exception exception)
        {
            if (_cache == null && _stoppedCache != null && !(exception is RespErrorMapper.CacheUnavailableException))
                return new RespErrorMapper.CacheUnavailableException("NCache cache '" + _cacheName + "' is not running");
            return exception;
        }

        private static OperationContext NewContext()
        {
            return new OperationContext(
                OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
        }

        /// <summary>
        /// Flags stamped on every value RESP writes.
        /// <para>
        /// <see cref="BitSetConstants.BinaryData"/> is what makes cross-protocol reads work: the
        /// client-side deserializer short-circuits on that bit and hands back the raw byte[] rather
        /// than running it through CompactBinaryFormatter. Without it a typed NCache client reading
        /// a RESP-written key would try to deserialize arbitrary bytes and throw.
        /// </para>
        /// </summary>
        private static BitSet BinaryFlag()
        {
            var flag = new BitSet();
            flag.SetBit(BitSetConstants.BinaryData);
            return flag;
        }

        #endregion

        #region Read

        /// <summary>GET - returns null on a miss. Mirrors Command/GetCommand.cs.</summary>
        public byte[] Get(string key)
        {
            EntryType? type;
            return Get(key, out type);
        }

        /// <summary>
        /// GET, also reporting which kind of entry answered.
        /// <para>
        /// Redis has one string type. NCache has a plain cache item and a Counter entry, and INCR
        /// creates the latter, so a string command that cannot tell them apart hands the client the
        /// serialised counter instead of its digits. The engine already carries the type on the
        /// reply to this very call, so taking it here costs nothing - a separate probe would have
        /// doubled the round trips on the hottest command in the protocol.
        /// </para>
        /// </summary>
        /// <param name="type">The entry's type, or null when the key is absent.</param>
        public byte[] Get(string key, out EntryType? type)
        {
            var cache = GetCache();

            ulong version = 0;
            object lockId = null;
            DateTime lockDate = DateTime.Now;

            CompressedValueEntry entry = cache.GetGroup(
                key, BinaryFlag(), null, null,
                ref version, ref lockId, ref lockDate,
                TimeSpan.Zero, LockAccessType.IGNORE_LOCK, NewContext());


            // A miss comes back as a live CompressedValueEntry carrying no value, not as null, so
            // the value is what says whether the key is there - Type would read as a default.
            type = entry == null || entry.Value == null ? (EntryType?)null : entry.Type;

            return ToBytes(cache, entry);
        }

        /// <summary>
        /// TYPE - the kind of entry under a key, or null when there is none. One engine call, the
        /// same one <see cref="Get(string, out EntryType?)"/> makes.
        /// </summary>
        public EntryType? GetEntryType(string key)
        {
            EntryType? type;
            Get(key, out type);
            return type;
        }

        /// <summary>MGET - one bulk call, misses come back as null entries. Mirrors BulkGetCommand.cs.</summary>
        public byte[][] GetBulk(string[] keys)
        {
            EntryType?[] types;
            return GetBulk(keys, out types);
        }

        /// <inheritdoc cref="GetBulk(string[])"/>
        /// <param name="types">
        /// One entry per key, in the same order, null where the key was a miss. MGET needs these
        /// for the same reason GET does - a counter has to be rendered as its digits.
        /// </param>
        public byte[][] GetBulk(string[] keys, out EntryType?[] types)
        {
            var cache = GetCache();
            var results = new byte[keys.Length][];
            types = new EntryType?[keys.Length];

            IDictionary found = cache.GetBulk(
                keys, BinaryFlag(), NewContext());

            if (found == null) return results;

            for (int i = 0; i < keys.Length; i++)
            {
                var entry = found[keys[i]] as CompressedValueEntry;
                types[i] = entry == null || entry.Value == null ? (EntryType?)null : entry.Type;
                results[i] = ToBytes(cache, entry);
            }
            return results;
        }

        /// <summary>EXISTS - counts how many of the given keys are present. Mirrors ContainsCommand.cs.</summary>
        public int CountExisting(IList keys)
        {
            var cache = GetCache();

            Hashtable result = cache.Contains(keys, NewContext());
            if (result == null) return 0;

            var found = result["items-found"] as ArrayList;
            return found == null ? 0 : found.Count;
        }

        public bool Contains(string key)
        {
            return CountExisting(new ArrayList { key }) > 0;
        }

        /// <summary>
        /// Pulls the byte[] out of whatever the engine returned.
        /// <para>
        /// For an out-of-proc server cache the value is already a <see cref="UserBinaryObject"/>,
        /// because BinaryDataFormatService stores the client's bytes untouched. The fallback path
        /// through SocketServerDataService matches what GetCommand does for the other cases.
        /// </para>
        /// </summary>
        private static byte[] ToBytes(Alachisoft.NCache.Caching.Cache cache, CompressedValueEntry entry)
        {
            // A miss: no entry, or an entry carrying no value at all.
            if (entry == null || entry.Value == null) return null;

            var binary = entry.Value as UserBinaryObject;
            if (binary == null)
            {
                var flag = entry.Flag;
                binary = cache.SocketServerDataService.GetClientData(entry.Value, ref flag, LanguageContext.DOTNET) as UserBinaryObject;
            }

            if (binary == null) return null;

            // Past this point the key definitely exists, so the result must not be null - callers
            // turn null into the RESP null bulk string, which tells the client there is no key.
            //
            // SmallUserBinaryObject.GetFullObject() returns null when its length is zero, so a key
            // stored with an empty value reassembles to null even though the entry is perfectly
            // real. Redis treats an empty value as a value: SET k "" then GET k returns an empty
            // bulk string, and EXISTS returns 1. Without this coalesce, GET contradicted EXISTS and
            // TYPE on the very same key.
            return binary.GetFullObject() ?? Empty;
        }

        private static readonly byte[] Empty = new byte[0];

        #endregion

        #region Write

        /// <summary>
        /// SET - overwrite by default. Mirrors Command/InsertCommand.cs.
        /// <para>
        /// A null <paramref name="expiration"/> clears any TTL the key had, because Insert replaces
        /// the whole entry including its expiration hint. That matches Redis, where a plain SET
        /// discards the previous TTL.
        /// </para>
        /// </summary>
        public void Set(string key, byte[] value, ExpirationHint expiration)
        {
            var cache = GetCache();

            cache.Insert(
                key, ToUserBinary(cache, value),
                expiration, null, null,
                null, null,
                BinaryFlag(), null, 0, LockAccessType.IGNORE_LOCK,
                null, null, NewContext(), null, null);
        }

        /// <summary>
        /// SETNX / SET NX - add only when absent. Mirrors Command/AddCommand.cs.
        /// <para>
        /// Atomic, and the only conditional write here that is: the engine itself reports
        /// KEY_ALREADY_EXISTS rather than us checking first and racing.
        /// </para>
        /// </summary>
        /// <returns>false when the key already existed.</returns>
        public bool Add(string key, byte[] value, ExpirationHint expiration)
        {
            var cache = GetCache();

            try
            {
                cache.Add(
                    key, ToUserBinary(cache, value),
                    expiration, null, null,
                    null, null,
                    BinaryFlag(), null, null, NewContext(), null, null, null);
                return true;
            }
            catch (Runtime.Exceptions.OperationFailedException e)
            {
                if (e.ErrorCode == Common.ErrorHandling.ErrorCodes.BasicCacheOperations.KEY_ALREADY_EXISTS)
                    return false;
                throw;
            }
        }

        /// <summary>DEL / GETDEL - removes and hands back the old value. Mirrors Command/RemoveCommand.cs.</summary>
        /// <returns>The removed value, or null when the key was absent.</returns>
        public byte[] Remove(string key)
        {
            var cache = GetCache();

            CompressedValueEntry entry = cache.Remove(
                key, BinaryFlag(), null, null, 0,
                LockAccessType.IGNORE_LOCK, null, NewContext());

            return ToBytes(cache, entry);
        }

        /// <summary>
        /// True when the key existed and was removed. DEL counts removals, and a removed key with a
        /// null payload still counts, so existence is checked rather than inferring it from the value.
        /// </summary>
        /// <summary>
        /// FLUSHDB / FLUSHALL - empties the cache. Mirrors Command/ClearCommand.cs.
        /// <para>
        /// The same engine call the protobuf handler makes, with the same flag, a null
        /// <c>Notifications</c> (which is what ClearCommand passes unless a datasource-cleared
        /// callback is registered) and the same RaiseCQNotification context, so continuous queries
        /// and cache-cleared events fire exactly as they do for a protobuf client.
        /// </para>
        /// <para>
        /// SCOPE: on a partitioned cache this is <b>cluster-wide</b>, not node-local -
        /// PartitionedServerCache.Clear calls Clustered_Clear whenever more than one server is in
        /// the cluster. That is the opposite of Redis, where FLUSHALL sent to one node empties only
        /// that node and a client has to repeat it against every master. One call here wipes the
        /// entire cache.
        /// </para>
        /// </summary>
        public void Clear()
        {
            var cache = GetCache();

            var context = NewContext();
            context.Add(OperationContextFieldName.RaiseCQNotification, true);

            cache.Clear(BinaryFlag(), null, context);
        }

        public bool Delete(string key)
        {
            var cache = GetCache();

            CompressedValueEntry entry = cache.Remove(
                key, BinaryFlag(), null, null, 0,
                LockAccessType.IGNORE_LOCK, null, NewContext());

            return entry != null;
        }

        private static object ToUserBinary(Alachisoft.NCache.Caching.Cache cache, byte[] value)
        {
            // Same conversion AddAndInsertCommandBase performs on the protobuf path, so a value
            // written over RESP is byte-for-byte what a protobuf client would have stored.
            return UserBinaryObject.CreateUserBinaryObject(value ?? new byte[0]);
        }

        #endregion

        #region Expiration

        /// <summary>
        /// Builds an absolute expiration hint for a UTC deadline, or null for "no expiry".
        /// <para>
        /// The hint is constructed here rather than handed to the engine as one of NCache's
        /// configured-policy sentinel values. That matters: DependencyHelper.GetExpirationHint will
        /// silently replace a caller's expiry with the cache's configured default when any
        /// expiration policy is enabled. Building a FixedExpiration directly and passing it to
        /// Insert bypasses that entirely, so a RESP client's EX really is the TTL it asked for.
        /// </para>
        /// </summary>
        public ExpirationHint CreateAbsoluteExpiration(DateTime utcDeadline)
        {
            var cache = GetCache();
            return FixedExpiration.Create(cache.Context.FakeObjectPool, utcDeadline.ToUniversalTime());
        }

        /// <summary>
        /// EXPIRE and friends, as a straight call to NCache's own "set expiration on a key" API.
        /// <para>
        /// WARNING - this AGGREGATES rather than replaces. LocalCache.AddInternal merges the new
        /// hint into an AggregateExpirationHint, and an aggregate expires as soon as ANY of its
        /// hints does, so repeated EXPIRE calls can only shorten a TTL, never extend it. It also
        /// returns false without doing anything if the existing hint is a sliding one. This is real
        /// NCache behaviour and this layer surfaces it rather than engineering around it. See
        /// poc-divergences.md section 3.
        /// </para>
        /// </summary>
        /// <returns>false when the key is absent or the engine refused the hint.</returns>
        public bool AddExpiration(string key, ExpirationHint expiration)
        {
            var cache = GetCache();
            return cache.AddExpirationHint(key, expiration, NewContext());
        }

        /// <summary>
        /// GETEX must replace the deadline, not aggregate it with the old one. Preserve the
        /// entry's metadata and type (including counters) when reinserting. This read/write
        /// sequence is not atomic against writers that do not honor the RESP key lease.
        /// </summary>
        public bool ReplaceExpiration(string key, DateTime? deadline)
        {
            var cache = GetCache();
            object lockId = null;
            DateTime lockDate = DateTime.Now;
            ulong version = 0;
            var entry = cache.GetCacheEntry(key, null, null, ref lockId, ref lockDate,
                TimeSpan.Zero, LockAccessType.IGNORE_LOCK, NewContext(), ref version) as CacheEntry;
            if (entry == null) return false;
            if (!deadline.HasValue && entry.ExpirationHint == null) return true;

            var replacement = entry.DeepClone(cache.Context.FakeObjectPool);
            // DataTypeEntry.DeepClone uses Clone and can share the old hint with the stored
            // entry. Do not return that hint to its pool when changing the replacement.
            
            replacement.ExpirationHint = deadline.HasValue ? CreateAbsoluteExpiration(deadline.Value) : null;
            var context = NewContext();
            context.Add(OperationContextFieldName.RaiseCQNotification, true);
            cache.Insert(key, replacement, context);
            return true;
        }

        /// <summary>
        /// Remaining life in milliseconds: -2 no such key, -1 no expiry set.
        /// <para>
        /// Reads the entry's hint the same way GetCacheItemCommand does. A sliding
        /// (IdleExpiration) hint reports -1: it renews on every access, so there is no fixed
        /// deadline a Redis client could act on. See poc-divergences.md section 5.
        /// </para>
        /// </summary>
        public long GetTtlMilliseconds(string key)
        {
            var cache = GetCache();

            object lockId = null;
            DateTime lockDate = DateTime.Now;
            ulong version = 0;

            var entry = cache.GetCacheEntry(
                key, null, null, ref lockId, ref lockDate,
                TimeSpan.Zero, LockAccessType.IGNORE_LOCK, NewContext(), ref version) as CacheEntry;

            if (entry == null) return -2;

            DateTime? absolute = FindAbsoluteExpiry(entry.ExpirationHint, key);
            if (!absolute.HasValue) return -1;

            double remaining = (absolute.Value - DateTime.UtcNow).TotalMilliseconds;
            return remaining <= 0 ? 0 : (long)remaining;
        }

        /// <summary>
        /// Finds the effective absolute deadline in a hint tree.
        /// <para>
        /// For an aggregate the EARLIEST deadline wins, because AggregateExpirationHint.CheckExpired
        /// expires the entry as soon as any hint has expired.
        /// </para>
        /// </summary>
        private static DateTime? FindAbsoluteExpiry(ExpirationHint hint, string key)
        {
            if (hint == null) return null;

            var fixedExpiry = hint as FixedExpiration;
            if (fixedExpiry != null) return fixedExpiry.AbsoluteTime;

            if (hint is IdleExpiration)
            {
                RespLog.Detail(Module + ".Ttl",
                    "key '" + key + "' has a sliding (idle) expiration, which has no Redis equivalent; reporting no TTL.");
                return null;
            }

            var aggregate = hint as AggregateExpirationHint;
            if (aggregate != null)
            {
                DateTime? earliest = null;
                foreach (ExpirationHint child in aggregate.Hints)
                {
                    DateTime? candidate = FindAbsoluteExpiry(child, key);
                    if (candidate.HasValue && (!earliest.HasValue || candidate.Value < earliest.Value))
                        earliest = candidate;
                }
                return earliest;
            }

            return null;
        }

        /// <summary>
        /// PERSIST - drop any expiry by re-inserting the value with no hint.
        /// <para>
        /// Insert is the only API that can clear an expiration, so this is a read followed by a
        /// write. Two consequences, both accepted knowingly: a concurrent writer between the two
        /// calls is lost, and the rebuilt entry carries only the value - group, tags, queryInfo and
        /// provider names are dropped, so the key disappears from NCache query results. See
        /// poc-divergences.md sections 2 and 4.
        /// </para>
        /// </summary>
        /// <returns>true when the key existed and actually had an expiry to remove.</returns>
        public bool Persist(string key)
        {
            long ttl = GetTtlMilliseconds(key);
            if (ttl == -2 || ttl == -1) return false;

            byte[] value = Get(key);
            if (value == null) return false;

            Set(key, value, null);
            return true;
        }

        /// <summary>
        /// Takes a cluster-wide lock around a multi-step operation on this key.
        /// <para>
        /// Dispose it to release. Redis runs each command to completion before starting the next,
        /// so anything built here out of several engine calls has to be made to look the same way -
        /// an increment is a read, an add and a write, and two clients doing that at once would
        /// otherwise both read the same value and one of them would be lost.
        /// </para>
        /// <para>
        /// The lease is taken on the data key itself and coordinates only this listener's own
        /// handlers: the writes pass no lock id, so NCache treats them as IGNORE_LOCK and an
        /// ordinary NCache client is never blocked by one. Failing to acquire is not an error - a
        /// Redis client has no notion of a lock, so the operation proceeds unlocked rather than
        /// inventing a failure mode the caller could not interpret.
        /// </para>
        /// </summary>
        public RespKeyLock Lock(string key)
        {
            return RespKeyLock.Acquire(CacheOrNull, key);
        }

        /// <summary>
        /// One chunk of keys from the cache, for RANDOMKEY.
        /// <para>
        /// The engine enumerates in chunks behind a pointer rather than handing over the whole
        /// keyspace, which is the right shape for a server that may hold millions of keys - and the
        /// reason RANDOMKEY samples within the first chunk rather than drawing uniformly from
        /// everything.
        /// </para>
        /// </summary>
        /// <remarks>
        /// The pointer type is spelled out in full because there are two EnumerationPointer
        /// classes in the tree - this one and a protobuf message of the same name - and a bare
        /// reference resolves to whichever namespace a future using happens to bring in.
        /// </remarks>
        public List<string> NextKeyChunk(
            ref Alachisoft.NCache.Common.DataStructures.EnumerationPointer pointer, out bool finished)
        {
            finished = true;
            var keys = new List<string>();

            var cache = CacheOrNull;
            if (cache == null) return keys;

            var chunk = cache.GetNextChunk(pointer, NewOperationContext());
            if (chunk == null) return keys;

            pointer = chunk.Pointer;
            finished = chunk.IsLastChunk;

            if (chunk.Data != null)
            {
                foreach (object key in chunk.Data)
                {
                    string text = key as string;
                    if (!string.IsNullOrEmpty(text)) keys.Add(text);
                }
            }

            return keys;
        }

        #endregion
    }
}
