//  Copyright (c) 2021 Alachisoft
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
using System.Collections;
using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.Resources;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Runtime.Caching;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Events;
using EventType = Alachisoft.NCache.Persistence.EventType;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Storage;
//using Alachisoft.NCache.MapReduce;
using Alachisoft.NCache.Common.Caching;
using System.Threading;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Common.FeatureUsageData;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    /// <summary>
    /// A class to serve as the base for all local cache implementations.
    /// </summary>

    internal class LocalCacheBase : CacheBase
    {
        /// <summary> The underlying physical data store. </summary>
        protected ICacheStorage _cacheStore;

        /// <summary> The statistics for this cache scheme. </summary>
        [CLSCompliant(false)]
        protected CacheStatistics _stats;

        /// <summary> Flag when set allow items to be evicted asynchronously..</summary>
        [CLSCompliant(false)]
        internal bool _allowAsyncEviction = true;

        [CLSCompliant(false)]
        protected CacheBase _parentCache;

        /// <summary>The PreparedQueryTable for Search/SearchEntries performance optimization.</summary>
        private Hashtable _preparedQueryTable = Hashtable.Synchronized(new Hashtable());
        /// <summary>The size of PreparedQueryTable. Configurable from alachisoft.ncache.service.exe.config.</summary>
        private int _preparedQueryTableSize = 1000;
        /// <summary>The evictionPercentage. Configurable from alachisoft.ncache.service.exe.config.</summary>
        private int _preparedQueryEvictionPercentage = 10;

        /// <summary>Data chunk size for cache data reader. Configurable from alachisoft.ncache.service.exe.config.</summary>
        private int _dataChunkSize = 512 * 1024;

        private Hashtable _stateTransferKeyList;

        
        int insertCounter = 0;
        int removeCounter = 0;

        ShutDownServerInfo _shutdownServer = null;

        private PullBaseNotificationManager _notificationManager;

        private DateTime? _lastErrorTime = null;
        

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="listener"></param>
        /// <param name="timeSched"></param>
        /// <param name="asyncProcessor"></param>
        /// <param name="expiryMgr"></param>
        /// <param name="perfStatsColl"></param>
        public LocalCacheBase(IDictionary properties, CacheBase parentCache, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(properties, listener, context)
        {
            _preparedQueryTableSize = ServiceConfiguration.PreparedQueryTableSize;

            _preparedQueryEvictionPercentage = ServiceConfiguration.PreparedQueryEvictionPercentage;

            _dataChunkSize = ServiceConfiguration.DataChunkSize;

            _stats = new CacheStatistics();

            _stats.InstanceName = _context.PerfStatsColl.InstanceName;

            _parentCache = parentCache;

            _notificationManager = new PullBaseNotificationManager(Listener, context.NCacheLog);
        }


        public LocalCacheBase()
        {

        }


        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _stats = null;

            if (_notificationManager != null)
                _notificationManager.Dispose();

            base.Dispose();
        }

        #endregion

        /// <summary>
        /// returns the statistics of the Clustered Cache.
        /// </summary>
        public override CacheStatistics Statistics
        {
            get { return _stats.Clone() as CacheStatistics; }
        }

        internal override CacheStatistics ActualStats
        {
            get { return _stats; }
        }

        

        /// <summary>
        /// Returns the cache local to the node, i.e., internal cache.
        /// </summary>
        protected internal override CacheBase InternalCache
        {
            get { return this; }
        }

        private bool? _isSelfInternal;

        /// <summary> 
        /// Returns true if this is the root internal cache.
        /// </summary>
        protected bool IsSelfInternal
        {
            get
            {
                if (_isSelfInternal.HasValue)
                    return _isSelfInternal.Value;
                else
                {
                    if (_context.CacheInternal is CacheSyncWrapper)
                        _isSelfInternal = ReferenceEquals(this, ((CacheSyncWrapper)_context.CacheInternal).Internal);
                    else
                        _isSelfInternal = ReferenceEquals(this, _context.CacheInternal);

                    return _isSelfInternal.Value;
                }
            }
        }

        /// <summary>
        /// PreparedQueryTable to keep the track of recent successfull parsed queries.
        /// </summary>
        public Hashtable PreparedQueryTable
        {
            get { return _preparedQueryTable; }
        }

     
        #region	/                 --- CacheBase ---           /



        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public sealed override void Clear(Caching.Notifications notification, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            ClearInternal();

          
            EventContext eventContext = null;
            EventId eventId = null;

          

            if (IsSelfInternal)
            {
                _context.ExpiryMgr.Clear();


                if (_context.PerfStatsColl != null && _context.ExpiryMgr != null)
                {
                    _context.PerfStatsColl.SetExpirationIndexSize(_context.ExpiryMgr.IndexInMemorySize);
                }


                _context.PerfStatsColl.IncrementCountStats((long)Count);
               
            }

            _stats.UpdateCount(this.Count);

            OperationID opId = operationContext.OperatoinID;
            eventContext = new EventContext();
            eventId = EventId.CreateEventId(opId);
            eventId.EventType = EventType.CACHE_CLEARED_EVENT;
            eventContext.Add(EventContextFieldName.EventID, eventId);

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }

            _notificationManager.ClearCache(!_context.IsStartedAsMirror);
            _context.StorePoolManager?.Clear();
            _context.TransactionalPoolManager?.Clear();

            if (!_context.IsStartedAsMirror)
            {
                NotifyCacheCleared(false, operationContext, eventContext);
            }
        }

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element
        /// with the specified key; otherwise, false.</returns>
        public sealed override bool Contains(object key, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return false;
            if (ContainsInternal(key))
            {
                // 16 Dec 05 Don't remove *** As per discussion
                CacheEntry e = GetInternal(key, true, operationContext,false,false);
                if (e == null) return false;
                if (e.ExpirationHint != null && e.ExpirationHint.CheckExpired(_context))
                {
                    ExpirationHint exh = e.ExpirationHint;
                    ItemRemoveReason reason = (exh.GetExpiringHint() is FixedExpiration || exh.GetExpiringHint() is IdleExpiration) ? ItemRemoveReason.Expired : ItemRemoveReason.DependencyChanged;

                    var cloneEntry = operationContext.CloneCacheEntry;
                    if (cloneEntry) operationContext.CloneCacheEntry = false;

                    Remove(key, reason, true, null, 0, LockAccessType.IGNORE_LOCK, operationContext);

                    if(cloneEntry) operationContext.CloneCacheEntry = cloneEntry;

                    return false;
                }
                return true;
            }
            return false;
        }

        public override LockOptions Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            LockOptions lockInfo = new LockOptions();
            if (!IsCacheOperationAllowed(operationContext))
                return lockInfo;
            CacheEntry e = GetInternal(key, false, operationContext,false,false);
            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.locking).UpdateUsageTime();
            if (e != null)
            {
                e.Lock(lockExpiration, ref lockId, ref lockDate, operationContext);
                lockInfo.LockDate = lockDate;
                lockInfo.LockId = lockId;
                return lockInfo;
            }
            else
            {
                lockInfo.LockId = lockId = null;
                return lockInfo;
            }
        }

        public override void NotifyBlockActivity(string uniqueId, long interval)
        {
            _shutdownServer = new ShutDownServerInfo();
            _shutdownServer.BlockInterval = interval;
            _shutdownServer.BlockServerAddress = new Common.Net.Address(_context.Render.IPAddress, _context.Render.Port);
            _shutdownServer.RenderedAddress = new Common.Net.Address(_context.Render.IPAddress, _context.Render.Port);
            _shutdownServer.UniqueBlockingId = uniqueId;

            _context.CacheRoot.NotifyBlockActivityToClients(uniqueId, _context.Render.IPAddress.ToString(), interval, _context.Render.Port);
        }

       
        
        public override void WindUpReplicatorTask()
        {

        }

        public override void WaitForReplicatorTask(long interval)
        {

        }

        public override void NotifyUnBlockActivity(string uniqueId)
        {
            if (_shutdownServer != null)
            {
                _context.CacheRoot.NotifyUnBlockActivityToClients(uniqueId, _context.Render.IPAddress.ToString(), _context.Render.Port);
                _shutdownServer = null;
            }
        }

        public override List<ShutDownServerInfo> GetShutDownServers()
        {
            List<ShutDownServerInfo> ssServers = null;
            if (_shutdownServer != null)
            {
                ssServers.Add(_shutdownServer);
            }
            return ssServers;
        }

        public override bool IsShutdownServer(Address server)
        {
            if (_shutdownServer != null)
            {
                if (_shutdownServer.BlockServerAddress.IpAddress.ToString().Equals(server.IpAddress.ToString()))
                    return true;
            }
            return false;
        }

        public override bool IsOperationAllowed(object key, AllowedOperationType opType)
        {
            if (_shutdownServer != null)
                return false;
            return true;
        }

        public override bool IsOperationAllowed(AllowedOperationType opType, OperationContext operationContext)
        {
            if (_shutdownServer != null)
                return false;
            return true;
        }

        public override LockOptions IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            LockOptions lockInfo = new LockOptions();
            if (!IsCacheOperationAllowed(operationContext))
                return lockInfo;
            CacheEntry e = GetInternal(key, false, operationContext,false,false);
            if (e != null)
            {
                e.IsLocked(ref lockId, ref lockDate);
                lockInfo.LockDate = lockDate;
                lockInfo.LockId = lockId;
                return lockInfo;
            }
            else
            {
                lockId = null;
                return lockInfo;
            }
        }

        public override void UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            Object tmpLockId = null;
            DateTime tmpLockDate = DateTime.Now;
            CacheEntry e = GetInternal(key, false, operationContext,false,false);
            if (e != null)
            {
                if (isPreemptive)
                    e.ReleaseLock();
                else
                {
                    if (e.CompareLock(lockId))
                        e.ReleaseLock();
                }
            }
        }

        internal override void UpdateLockInfo(object key, object lockId, DateTime lockDate, LockExpiration lockExpiration, OperationContext operationContext)
        {
            CacheEntry entry = GetInternal(key, false, operationContext,false,false);
            if (entry != null)
            {
                entry.CopyLock(lockId, lockDate, lockExpiration);
            }
        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        public sealed override CacheEntry Get(object key, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            return Get(key, true, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
        }

        public override HashVector GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            return new HashVector();
        }

        public override HashVector GetTagData(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            return new HashVector();
        }

        private HashVector GetFromCache(ICollection keys, OperationContext operationContext)
        {
            if (keys == null) return null;

            return GetEntries(keys, operationContext);
        }

        public override Hashtable Remove(string[] tags, TagComparisonType tagComparisonType, bool notify, OperationContext operationContext)
        {
            return RemoveFromCache(GetTagKeys(tags, tagComparisonType, operationContext), notify, operationContext);
        }

        private Hashtable RemoveFromCache(ICollection keys, bool notify, OperationContext operationContext)
        {
            if (keys == null) return null;
            Object[] keysArray = new Object[keys.Count];
            keys.CopyTo(keysArray, 0);
            return Remove(keysArray, ItemRemoveReason.Removed, notify, operationContext);

        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        public sealed override CacheEntry Get(object key, bool isUserOperation, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry removal = null;
            if (!IsCacheOperationAllowed(operationContext))
                return removal;
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.LocalBase);

                CacheEntry e = GetInternal(key, isUserOperation, operationContext, false, operationContext.NeedUserPayload);

                if (accessType != LockAccessType.IGNORE_LOCK)
                {
                    if (e != null)
                    {
                        FeatureUsageCollector.Instance.GetFeature(FeatureEnum.locking).UpdateUsageTime();
                        if (accessType == LockAccessType.DONT_ACQUIRE)
                        {
                            bool success = e.CompareLock(lockId);
                            if (success)
                            {
                                //explicitly set the lockdate incase of compare lock. compare lock does not set the lockdate.
                                lockDate = e.LockDate;
                            }
                            else
                            {
                                success = !e.IsLocked(ref lockId, ref lockDate);
                            }


                            if (!success)
                            {
                                e.MarkFree(NCModulesConstants.Global);
                                e = null;
                            }
                        }
                        else if (accessType == LockAccessType.ACQUIRE && !e.Lock(lockExpiration, ref lockId, ref lockDate, operationContext))//internally sets the out parameters
                        {
                            e.MarkFree(NCModulesConstants.Global);
                            e = null;
                        }
                        

                       
                    }
                    else
                    {
                        lockId = null;

                    }
                }
                if (e != null)
                {
                    version = e.Version;
                }

                ExpirationHint exh = (e == null ? null : e.ExpirationHint);
                // 16 Dec 05 Don't remove *** As per discussion
                if (exh != null)
                {
                    // we know that we have converted the expiration for client cache.
                    // add key to touch the cache item on server.
                    
                    if (exh.CheckExpired(_context))
                    {
                        try
                        {
                            // If cache forward is set we skip the expiration.
                            if (!exh.NeedsReSync)
                            {
                                ItemRemoveReason reason = (exh.GetExpiringHint() is FixedExpiration || exh.GetExpiringHint() is IdleExpiration) ? ItemRemoveReason.Expired : ItemRemoveReason.DependencyChanged;

                                var cloneEntry = operationContext.CloneCacheEntry;
                                if (cloneEntry) operationContext.CloneCacheEntry = false;


                                removal = Remove(key, reason, true, null, 0, LockAccessType.IGNORE_LOCK, operationContext);

                                if(cloneEntry) operationContext.CloneCacheEntry = cloneEntry;

                                if (e != null)
                                    e.MarkFree(NCModulesConstants.Global);

                                
                                e = null;
                            }
                        }
                        finally
                        {
                            removal?.MarkFree(NCModulesConstants.Global);
                        }
                    }

                    if (exh.IsVariant && isUserOperation)
                    {
                        try
                        {
                            _context.ExpiryMgr.ResetVariant(exh);
                        }
                        catch (Exception ex)
                        {
                            
                            removal = RemoveInternal(key, ItemRemoveReason.Removed, false, operationContext,false,false);
                                   throw ex;

                        }
                        finally
                        {
                            removal?.MarkFree(NCModulesConstants.Global);
                        }
                    }
                }

                _stats.UpdateCount(this.Count);

                if (e != null)
                {
                    _stats.BumpHitCount();

                    if (operationContext.CloneCacheEntry)
                    {
                        e = e.DeepClone(operationContext.UseObjectPool ? _context.TransactionalPoolManager : _context.FakeObjectPool, true);
                        PopulateQueryInfo(key, e, operationContext);
                    }
                }
                else
                {
                    _stats.BumpMissCount();
                }
                return e;
            }
            finally
            {
                MiscUtil.ReturnEntryToPool(removal, _context.StorePoolManager);
                
                operationContext?.MarkFree(NCModulesConstants.LocalBase);
            }
        }


        /// <summary>
        /// Get the item size stored in cache
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>item size</returns>
        public override int GetItemSize(object key)
        {
            return 0;
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public sealed override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
        {
            return Add(key, cacheEntry, notify, true, operationContext);
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public sealed override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, bool isUserOperation, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return CacheAddResult.Failure;
            CacheAddResult result = CacheAddResult.Failure;
            CacheEntry entry = null;
            CacheEntry removalEntry = null;
            CacheEntry inputCacheEntry = cacheEntry;
            bool entryCloned = false;

            try
            {
                operationContext?.MarkInUse(NCModulesConstants.LocalBase);
                cacheEntry?.MarkInUse(NCModulesConstants.LocalBase) ;
                if (_stateTransferKeyList != null && _stateTransferKeyList.ContainsKey(key) && notify)
                    return CacheAddResult.KeyExists;


                RegisterPullbasedKeyNotification(key, cacheEntry);
                
                cacheEntry = cacheEntry.DeepClone(_context.StorePoolManager,true);
                entryCloned = true;

                result = AddInternal(key, cacheEntry, isUserOperation, operationContext);
                

                // Not enough space, evict and try again.
                if (result == CacheAddResult.NeedsEviction || result == CacheAddResult.SuccessNearEviction)
                {
                    Evict();
                    if (result == CacheAddResult.SuccessNearEviction)
                        result = CacheAddResult.Success;
                }

                //This code should be added to allow the user to add a key value pair that has expired.
                if (result == CacheAddResult.KeyExists)
                {
                    // 16 Dec 05): *** Don't remove *** As per discussion with I. M. Khan
                    entry= GetInternal(key, isUserOperation, operationContext,false,false);
                    if (entry.ExpirationHint != null && entry.ExpirationHint.CheckExpired(_context))
                    {
                        ExpirationHint exh = entry.ExpirationHint;
                        ItemRemoveReason reason = (exh.GetExpiringHint() is FixedExpiration || exh.GetExpiringHint() is IdleExpiration) ? ItemRemoveReason.Expired : ItemRemoveReason.DependencyChanged;

                        var cloneEntry = operationContext.CloneCacheEntry;
                        if(cloneEntry)  operationContext.CloneCacheEntry = false;

                        removalEntry =  Remove(key, ItemRemoveReason.Expired, true, null, 0, LockAccessType.IGNORE_LOCK, operationContext);

                       if(cloneEntry)  operationContext.CloneCacheEntry = cloneEntry;
                    }
                }

                // Operation completed!
                if (result == CacheAddResult.Success)
                {
                    if (cacheEntry.ExpirationHint != null)
                    {
                        cacheEntry.ExpirationHint.CacheKey = (string)key;

                        try
                        {
                            _context.ExpiryMgr.ResetHint(null, cacheEntry.ExpirationHint); //:muds
                        }
                        catch (Exception e)
                        {
                           entry =   RemoveInternal(key, ItemRemoveReason.Removed, false, operationContext,false,false);

                            MiscUtil.ReturnEntryToPool(entry, _context.StorePoolManager);
                            entry?.MarkFree(NCModulesConstants.Global);
                            throw e;
                        }
                        _context.ExpiryMgr.UpdateIndex(key, cacheEntry);
                    }

                    if (IsSelfInternal)
                    {
                        _context.PerfStatsColl.IncrementCountStats((long)Count);
                       

                    }
                    EventId eventId = null;
                    EventContext eventContext = null;
                    OperationID opId = operationContext.OperatoinID;
                 

                  
                }
                _stats.UpdateCount(this.Count);

            }
            finally
            {
                //version is updated on cache entry from store pool; this version is required for replication at
                //CacheSyncWrapper level.
                inputCacheEntry.Version = cacheEntry.Version;
                //Entry is rented from pool but operation failed so let's return it to the pool
                if (entryCloned && !cacheEntry.IsStored)
                    MiscUtil.ReturnEntryToPool(cacheEntry, _context.StorePoolManager);

                operationContext.RemoveValueByField(OperationContextFieldName.IndexMetaInfo);
                operationContext?.MarkFree(NCModulesConstants.LocalBase);
                entry?.MarkFree(NCModulesConstants.Global);
                cacheEntry?.MarkFree(NCModulesConstants.LocalBase);
                removalEntry?.MarkFree(NCModulesConstants.Global);

            }
            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }

            return result;
        }

        /// <summary>
        /// Add ExpirationHint against the given key
        /// Key must already exists in the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public sealed override bool Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
            CacheEntry entry = null;
            if (!IsCacheOperationAllowed(operationContext))
                return false;
            if (eh == null)
                return false;
            try
            {
                entry = GetInternal(key, false, operationContext,false,false);
                bool result = AddInternal(key, eh, operationContext);
                if (result)
                {

                    eh.CacheKey = (string)key;
                    if (!eh.Reset(_context))
                    {
                        RemoveInternal(key, eh);
                        throw new OperationFailedException("Unable to initialize expiration hint");
                    }
                    _context.ExpiryMgr.UpdateIndex(key, entry);
                }

                if (_context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.SetCacheSize(Size);
                }
                return result;
            }
            finally
            {
                entry?.MarkFree(NCModulesConstants.Global);
            }
        }

        /// <summary>
        /// Add ExpirationHint against the given key
        /// Key must already exists in the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public sealed override bool Add(object key, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return false;
            bool result = AddInternal(key);
            
            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }
            return result;
        }

        public sealed override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.LocalBase);
                operationContext?.MarkInUse(NCModulesConstants.LocalBase);

                return Insert(key, cacheEntry, notify, true, lockId, version, accessType, operationContext);
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.LocalBase);
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.LocalBase);
            }
        }

        public override void SetStateTransferKeyList(Hashtable keylist)
        {
            _stateTransferKeyList = keylist;
        }

        public override void UnSetStateTransferKeyList()
        {
            _stateTransferKeyList = null;
        }
        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public sealed override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, bool isUserOperation, object lockId, ulong version, LockAccessType access, OperationContext operationContext)
        {
            CacheInsResultWithEntry result = CacheInsResultWithEntry.CreateCacheInsResultWithEntry(operationContext.UseObjectPool ? _context.TransactionalPoolManager : _context.FakeObjectPool);
            if (!IsCacheOperationAllowed(operationContext))
                return result;
            CacheEntry pe = null;
            CacheEntry inputCacheEntry = cacheEntry;
            CacheEntry rollBack = null;
            bool entryCloned = false;
            
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.LocalBase);
                operationContext?.MarkInUse(NCModulesConstants.LocalBase);

                int maxEvict = 3;

                string clientId = null;
                Caching.Notifications cbEtnry = null;
                OperationID opId = operationContext.OperatoinID;
                EventId eventId = null;
                EventContext eventContext = null;

                
                {
                    pe = GetInternal(key, false, operationContext,true,true);
                    result.Entry = pe;

                    pe?.MarkInUse(NCModulesConstants.LocalBase);
                    pe?.MarkFree(NCModulesConstants.Global);


                    if (pe != null && access != LockAccessType.IGNORE_LOCK)
                    {
                        
                            if (access == LockAccessType.RELEASE || access == LockAccessType.DONT_RELEASE)
                            {
                                if (pe.IsItemLocked() && !pe.CompareLock(lockId))
                                {
                                    result.Result = CacheInsResult.ItemLocked;
                                    result.Entry = null;

                                    //return entry back to transactional pool
                                    MiscUtil.ReturnEntryToPool(pe, _context.TransactionalPoolManager);

                                    return result;
                                }
                            }
                            if (access == LockAccessType.DONT_RELEASE)
                            {
                                cacheEntry.CopyLock(pe.LockId, pe.LockDate, pe.LockExpiration);
                            }
                            else
                            {
                                cacheEntry.ReleaseLock();
                            }
                    }
                    // Bugfix 12852 - Multi-regional sessions do not work as expected even when session locking is disabled in new version i.e. Merger of asp.net and .net core sessions
                    // If entry is not present in cache and explicit release is called lock is not released. so releasing it
                    if (pe == null && access == LockAccessType.RELEASE)
                    {
                        cacheEntry.ReleaseLock();
                    }

                    ExpirationHint peExh = pe == null ? null : pe.ExpirationHint;

                    if (pe != null)
                    {
                        if (pe.Notifications != null)
                        {
                            cbEtnry = pe.Notifications;
                            cacheEntry = CacheHelper.MergeEntries(pe, cacheEntry);
                        }

                        cacheEntry.MergeCallbackListeners(pe);

                }

                    Caching.Notifications notification = cacheEntry.Notifications;

                    if (notification != null)
                    {
                        if (notification.ItemUpdateCallbackListener != null && notification.ItemUpdateCallbackListener.Count > 0)    //Pull based Notifications
                        {
                            foreach (object callback in notification.ItemUpdateCallbackListener)
                            {
                                if (((CallbackInfo)callback).CallbackType == CallbackType.PullBasedCallback)
                                    _notificationManager.RegisterKeyNotification(((CallbackInfo)callback).Client, key as string, callback as CallbackInfo, null);
                            }
                        }
                        if (notification.ItemRemoveCallbackListener != null && notification.ItemRemoveCallbackListener.Count > 0)    //Pull based Notifications
                        {
                            foreach (object callback in notification.ItemRemoveCallbackListener)
                            {
                                if (((CallbackInfo)callback).CallbackType == CallbackType.PullBasedCallback)
                                    _notificationManager.RegisterKeyNotification(((CallbackInfo)callback).Client, key as string, null, callback as CallbackInfo);
                            }
                        }
                    }

                    // register notification before adding sync dependency.
                    RegisterPullbasedKeyNotification(key, cacheEntry);

                    if (access == LockAccessType.PRESERVE_VERSION)
                    {
                        cacheEntry.Version = (UInt64)version;
                        isUserOperation = false;
                    }
                
                   
                    //Let's clone the input CacheEntry. Input entry is from transactiona pool. Clone entry is rented from 
                    //store pool
                    cacheEntry = cacheEntry.DeepClone(_context.StorePoolManager,true);
                    entryCloned = true;

                    result.Result = InsertInternal(key, cacheEntry, isUserOperation, pe, operationContext, true);

                    if ((result.Result == CacheInsResult.Success || result.Result == CacheInsResult.SuccessNearEvicition) && _stateTransferKeyList != null && _stateTransferKeyList.ContainsKey(key))
                    {
                        result.Result = result.Result == CacheInsResult.Success ? CacheInsResult.SuccessOverwrite : CacheInsResult.SuccessOverwriteNearEviction;
                    }
                    
                    // Not enough space, evict and try again.
                    if (result.Result == CacheInsResult.NeedsEviction || result.Result == CacheInsResult.SuccessNearEvicition || result.Result == CacheInsResult.SuccessOverwriteNearEviction)
                    {
                        //no need to evict on bridge target cache.
                        //items removal operations are replicated from the source cache to the target cache.
                        
                         Evict();

                        if (result.Result == CacheInsResult.SuccessNearEvicition)
                            result.Result = CacheInsResult.Success;
                        if (result.Result == CacheInsResult.SuccessOverwriteNearEviction)
                            result.Result = CacheInsResult.SuccessOverwrite;

                    }

                    // Operation completed!
                    if (result.Result == CacheInsResult.Success || result.Result == CacheInsResult.SuccessOverwrite)
                    {
                        //remove the old hint from expiry index.
                        if (peExh != null)
                            _context.ExpiryMgr.RemoveFromIndex(key);

                        if (cacheEntry.ExpirationHint != null)
                        {
                            cacheEntry.ExpirationHint.CacheKey = (string)key;
                            if (isUserOperation)
                            {
                                try
                                {
                                    _context.ExpiryMgr.ResetHint(peExh, cacheEntry.ExpirationHint);
                                }
                                catch (Exception e)
                                {
                                   rollBack = RemoveInternal(key, ItemRemoveReason.Removed, false, operationContext,false,false);
                                    //return entry back to store pool
                                    //!!!whenever need to remove this code; remove the entire block
                                    if ( MiscUtil.ReturnEntryToPool(rollBack, _context.StorePoolManager))
                                    {
                                        entryCloned = false;//turn off this flag as we are returning entry to the pool 
                                    }

                                    throw e;
                                }
                            }
                            else
                            {
                                cacheEntry.ExpirationHint.ReInitializeHint(Context);
                            }
                            _context.ExpiryMgr.UpdateIndex(key, cacheEntry);
                        }

                        if (IsSelfInternal)
                        {

                            _context.PerfStatsColl.IncrementCountStats((long)Count);
                           
                        }

                       

                        }
                    }

                _stats.UpdateCount(this.Count);

                if (pe != null && !isUserOperation) //Pull based Notifications in state transfer
                {
                    if (pe.ItemUpdateCallbackListener != null && pe.ItemUpdateCallbackListener.Count > 0)    //Pull based Notifications
                    {
                        foreach (object callback in pe.ItemUpdateCallbackListener)
                        {
                            _notificationManager.RegisterKeyNotification(((CallbackInfo)callback).Client, key as string, callback as CallbackInfo, null);
                        }
                    }
                    if (pe.ItemRemoveCallbackListener != null && pe.ItemRemoveCallbackListener.Count > 0)    //Pull based Notifications
                    {
                        foreach (object callback in pe.ItemRemoveCallbackListener)
                        {
                            _notificationManager.RegisterKeyNotification(((CallbackInfo)callback).Client, key as string, null, callback as CallbackInfo);
                        }
                    }
                }
                
                switch (result.Result)
                {
                    case CacheInsResult.SuccessOverwrite:
                        if (!_context.IsStartedAsMirror)
                            _notificationManager.KeyUpdated(key as string, isUserOperation, clientId);

                        if (notify && (ShouldNotifyItemUpdated || ShouldNotifyCustomUpdate(cbEtnry)))
                        {
                            EventCacheEntry eventCacheEntry = CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.None, cacheEntry, Context);
                            EventCacheEntry oldEventCacheEntry = CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.None, pe, Context);
                            if (cbEtnry != null)
                            {
                                if (cbEtnry.ItemUpdateCallbackListener != null && cbEtnry.ItemUpdateCallbackListener.Count > 0)
                                {
                                  
                                        eventId = EventId.CreateEventId(opId);
                                     
                                    eventContext = new EventContext();
                                    eventId.EventType = EventType.ITEM_UPDATED_CALLBACK;
                                    eventContext.Add(EventContextFieldName.EventID, eventId);
                                    eventContext.Item = eventCacheEntry;
                                    eventContext.OldItem = oldEventCacheEntry;
                                    eventContext.UniqueId =GenerateEventType(EventType.ITEM_UPDATED_CALLBACK)+ cacheEntry.Version.ToString();
                                    NotifyCustomUpdateCallback(key, cbEtnry.ItemUpdateCallbackListener, false, (OperationContext)operationContext.Clone(), eventContext);

                                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.data_sharing).UpdateUsageTime();
                                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.selective_events, FeatureEnum.data_sharing).UpdateUsageTime();
                                }
                            }
                            if (ShouldNotifyItemUpdated)
                            {
                                    eventId = EventId.CreateEventId(opId);
                            }
                        }
                        break;
                }
            }
            finally
            {
                //version is updated on cache entry from store pool; this version is required for replication
                //CacheSyncWrapper level.
                inputCacheEntry.Version = cacheEntry.Version;
                //Entry is rented from pool but could not be stored, so return it to the pool
                if (entryCloned && !cacheEntry.IsStored)
                    MiscUtil.ReturnEntryToPool(cacheEntry, _context.StorePoolManager);

                operationContext.RemoveValueByField(OperationContextFieldName.IndexMetaInfo);
                operationContext?.MarkFree(NCModulesConstants.LocalBase);

                if (pe != null)
                    pe.MarkFree(NCModulesConstants.LocalBase);

                if (pe != null)
                    pe.MarkFree(NCModulesConstants.Global);

                if (pe != null)
                    pe.MarkFree(NCModulesConstants.CacheStore);

                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.LocalBase);

                rollBack?.MarkFree(NCModulesConstants.Global);

            }
            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }
            return result;
        }


        private void VerifyLocking(CacheEntry oldEntry, string name, object lockId, LockAccessType lockAccessType)
        {
            if (oldEntry != null && lockAccessType != LockAccessType.IGNORE_LOCK)
            {
                if (lockAccessType == LockAccessType.DONT_RELEASE)
                {
                    if (oldEntry.IsItemLocked() && !oldEntry.CompareLock(lockId))
                    {
                        throw new Exception(string.Format("Collection '{ 0 }' is locked.", name));
                    }
                }
            }
        }
        

        public string GenerateEventType(EventType eventType)
        {
            switch(eventType)
            {
                case EventType.ITEM_REMOVED_CALLBACK:
                    return "Removed_cb";
                    break;
                case EventType.ITEM_UPDATED_CALLBACK:
                    return "Updated_cb";
                    break;
                case EventType.ITEM_ADDED_EVENT:
                    return "Added_event";
                    break;
                case EventType.ITEM_REMOVED_EVENT:
                    return "Removed_event";
                    break;
                case EventType.ITEM_UPDATED_EVENT:
                    return "Updated_event";
                    break;
                case EventType.ITEM_ADDED_CALLBACK:
                    return "Added_cb";
                    break;
                 default:
                    return "";
            }

        }

      

    
    
       
        private void RegisterPullbasedKeyNotification(object key, CacheEntry cacheEntry)
        {
            string clientId = null;
            if (cacheEntry != null)
            {
                if (cacheEntry.ItemUpdateCallbackListener != null && cacheEntry.ItemUpdateCallbackListener.Count > 0)    //Pull based Notifications
                {
                    foreach (object callback in cacheEntry.ItemUpdateCallbackListener)
                    {
                        clientId = ((CallbackInfo)callback).Client;
                        if (((CallbackInfo)callback).CallbackType == CallbackType.PullBasedCallback)
                            _notificationManager.RegisterKeyNotification(((CallbackInfo)callback).Client, key as string, callback as CallbackInfo, null);
                    }
                }
                if (cacheEntry.ItemRemoveCallbackListener != null && cacheEntry.ItemRemoveCallbackListener.Count > 0)    //Pull based Notifications
                {
                    foreach (object callback in cacheEntry.ItemRemoveCallbackListener)
                    {
                        clientId = ((CallbackInfo)callback).Client;
                        if (((CallbackInfo)callback).CallbackType == CallbackType.PullBasedCallback)
                            _notificationManager.RegisterKeyNotification(((CallbackInfo)callback).Client, key as string, null, callback as CallbackInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>item value</returns>
        public sealed override CacheEntry Remove(object key, ItemRemoveReason removalReason, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            return Remove(key, removalReason, notify, true, lockId, version, accessType, operationContext);
        }

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>item value</returns>
        public override CacheEntry Remove(object key, ItemRemoveReason removalReason, bool notify, bool isUserOperation, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry e = null;
            if (!IsCacheOperationAllowed(operationContext))
                return e;
            CacheEntry pe = null;
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.LocalBase);

                object actualKey = key;
                if (key is object[])
                {
                    actualKey = ((object[])key)[0];
                }

                if (accessType != LockAccessType.IGNORE_LOCK)
                {
                    pe = GetInternal(actualKey, false, operationContext, false, false);
                    if (pe != null)
                    {
                        if (pe.IsItemLocked() && !pe.CompareLock(lockId))
                        {
                            throw new LockingException(ErrorCodes.BasicCacheOperations.ITEM_LOCKED, ErrorMessages.GetErrorMessage(ErrorCodes.BasicCacheOperations.ITEM_LOCKED));
                        }
                    }
                }

                e = RemoveInternal(actualKey, removalReason, isUserOperation, operationContext, true, true);

                bool resyncRexpiredItems = false;
                if (e != null && e.ExpirationHint != null)
                    resyncRexpiredItems = e.ExpirationHint.NeedsReSync;

                EventId eventId = null;
                EventContext eventContext = null;
                OperationID opId = operationContext.OperatoinID;
                if (e != null)
                {
                    if (_stateTransferKeyList != null && _stateTransferKeyList.ContainsKey(key))
                        _stateTransferKeyList.Remove(key);
                    //AddWriteBehindTask(key, e, null, OpCode.Remove);

                    // commented by muds
                    //_context.ExpiryMgr.UpdateIndex(key, e.ExpirationHint, null);

                    try
                    {
                        //_context.ExpiryMgr.ResetHint(e.ExpirationHint, null);
                        if (e.ExpirationHint != null)
                        {
                            _context.ExpiryMgr.RemoveFromIndex(key);
                            e.ExpirationHint.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        NCacheLog.Error("LocalCacheBase.Remove(object, ItemRemovedReason, bool):", ex.ToString());
                    }
                    if (IsSelfInternal)
                    {
                        // Disposed the one and only cache entry.
                        e.Dispose();


                        if (!_context.IsDbSyncCoordinator && (removalReason == ItemRemoveReason.Expired ||
                            removalReason == ItemRemoveReason.DependencyChanged))
                        {
                            _context.PerfStatsColl.IncrementExpiryPerSecStats();
                        }
                        else if (!_context.CacheImpl.IsEvictionAllowed && removalReason == ItemRemoveReason.Underused)
                        {
                            _context.PerfStatsColl.IncrementEvictPerSecStats();
                        }

                        _context.PerfStatsColl.IncrementCountStats(Count);

                    }

                    _notificationManager.KeyRemoved(key as string, isUserOperation & !_context.IsStartedAsMirror, operationContext.GetValueByField(OperationContextFieldName.ClientId) as string);
                    Caching.Notifications cbEtnry = e.Notifications;
                    if ((notify && (ShouldNotifyItemRemoved || ShouldNotifyCustomRemove(cbEtnry))))
                    {

                        if (cbEtnry != null && cbEtnry.ItemRemoveCallbackListener.Count > 0)
                        {
                            
                            eventId = EventId.CreateEventId(opId);
                            
                            eventId.EventType = EventType.ITEM_REMOVED_CALLBACK;
                            eventContext = new EventContext();
                            eventContext.Add(EventContextFieldName.EventID, eventId);
                            EventCacheEntry eventCacheEntry = CacheHelper.CreateCacheEventEntry(cbEtnry.ItemRemoveCallbackListener, e, Context);
                            if (eventCacheEntry != null)
                                eventCacheEntry.ReSyncExpiredItems = resyncRexpiredItems;
                            eventContext.Item = eventCacheEntry;
                            eventContext.Add(EventContextFieldName.ItemRemoveCallbackList, cbEtnry.ItemRemoveCallbackListener.Clone());
                            eventContext.UniqueId = GenerateEventType(EventType.ITEM_REMOVED_CALLBACK) + e.Version.ToString();
                            //Will always reaise the whole entry for old clients
                            NotifyCustomRemoveCallback(actualKey, cbEtnry.ItemRemoveCallbackListener, removalReason, false, (OperationContext)operationContext.Clone(), eventContext);

                            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.data_sharing).UpdateUsageTime();
                            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.selective_events, FeatureEnum.data_sharing).UpdateUsageTime();
                        }

                        
                        eventId = EventId.CreateEventId(opId);
                       
                    }

                    MetaInformation metaInfo = new MetaInformation(null);
                    metaInfo.Type = e.ObjectType;

                  
                }

               
           
                else if (_stateTransferKeyList != null && _stateTransferKeyList.ContainsKey(key))
                {
                   
                    _stateTransferKeyList.Remove(key);
                  

                }
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.LocalBase);

                if (pe != null)
                {
                    pe.MarkFree(NCModulesConstants.Global);
                    pe.MarkFree(NCModulesConstants.CacheStore);
                }
            }
            _stats.UpdateCount(this.Count);

            // Remove the MetaInfo if found
            if (operationContext.Contains(OperationContextFieldName.IndexMetaInfo))
            {
                operationContext.RemoveValueByField(OperationContextFieldName.IndexMetaInfo);
            }

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }

            return e;
        }

        public override object RemoveSync(object[] keys, ItemRemoveReason reason, bool notify, OperationContext operationContext)
        {
            if (_parentCache != null)
            {
                return _parentCache.RemoveSync(keys, reason, notify, operationContext);
            }
            return null;
        }
        /// <summary>
        /// Broadcasts a user-defined event across the cluster.
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="data"></param>
        /// <param name="async"></param>
        public sealed override void SendNotification(object notifId, object data, OperationContext operationContext)
        {

            base.NotifyCustomEvent(notifId, data, false, operationContext, null);
        }

       
      

    
      

       

       
        public sealed override void DeclaredDeadClients(ArrayList clients)
        {

        }


        public override void DeclareDeadClients(string deadClient, ClientInfo info)
        {
        }


      
        public override Hashtable AddDepKeyList(Hashtable table, OperationContext operationContext)
        {
            Hashtable retVal = new Hashtable();
            return retVal;
        }

        public override Hashtable RemoveDepKeyList(Hashtable table, OperationContext operationContext)
        {
            Hashtable retVal = new Hashtable();
         
            return retVal;
        }

        #endregion

        #region	/                 --- Bulk operations ---           /

        /// <summary>
        /// Determines whether the cache contains the given keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>list of keys found in the cache</returns>
        public sealed override Hashtable Contains(IList keys, OperationContext operationContext)
        {
            Hashtable tbl = new Hashtable();
            if (!IsCacheOperationAllowed(operationContext))
                return tbl; 
            ArrayList successfulKeys = new ArrayList();
            ArrayList failedKeys = new ArrayList();

            foreach  (string key in keys)
            {
                var lockKey = GetStringFromPool(key);
                KeyLocker.GetReaderLock(lockKey);

                try
                {
                    bool result = Contains(key, operationContext);
                    if (result)
                    {
                        successfulKeys.Add(key);
                    }
                }

                catch (StateTransferException se)
                {
                    failedKeys.Add(key);
                }
                finally
                {

                    KeyLocker.ReleaseReaderLock(lockKey);

                }
            }

            if (successfulKeys.Count > 0)
                tbl["items-found"] = successfulKeys;
            if (failedKeys.Count > 0)
                tbl["items-transfered"] = failedKeys;

            return tbl;
        }

        /// <summary>
        /// Retrieve the objects from the cache.
        /// An array of keys is passed as parameter.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>key and entry pairs.</returns>
        public sealed override IDictionary Get(object[] keys, OperationContext operationContext)
        {
            HashVector entries = new HashVector();
            if (!IsCacheOperationAllowed(operationContext))
                return entries;
            CacheEntry e = null;
            for (int i = 0; i < keys.Length; i++)
            {
                if (operationContext != null &&  operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                var lockKey = GetStringFromPool((string)keys[i]);
                KeyLocker.GetReaderLock(lockKey);

                try
                {
                    if (operationContext != null)
                    {
                        operationContext.RemoveValueByField(OperationContextFieldName.EventContext);
                        OperationID opId = operationContext.OperatoinID;
                        EventId eventId = EventId.CreateEventId(opId);
                        eventId.EventUniqueID = opId.OperationId;
                        eventId.OperationCounter = opId.OpCounter;
                        eventId.EventCounter = i;
                        EventContext eventContext = new EventContext();
                        eventContext.Add(EventContextFieldName.EventID, eventId);
                        operationContext.Add(OperationContextFieldName.EventContext, eventContext);
                    }

                    e = Get(keys[i], operationContext);
                    if (e != null)
                    {
                        entries[keys[i]] = e;
                    }
                }
                catch (StateTransferException se)
                {
                    entries[keys[i]] = se;
                }
                finally
                {
                    KeyLocker.ReleaseReaderLock(lockKey);
                }
            }
            return entries;
        }

        /// <summary>
        /// For SearchEntries method, we need to get entries from cache for searched keys.
        /// Previously it was accomplished using normal get operation. But this special
        /// method is required because normal get operation throws 'StateTransferException'
        /// during state transfer which is not required in Search methods because
        /// search is broadcasted to all nodes and each node can send its data back no matter
        /// if state transfer is under process.
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        private HashVector GetEntries(ICollection keys, OperationContext operationContext)
        {
            HashVector entries = new HashVector();
            CacheEntry e = null;
            IEnumerator ieKeys = keys.GetEnumerator();


            while (ieKeys.MoveNext())
            {
                if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                String key = ieKeys.Current as string;

                var lockKey = GetStringFromPool(key);
                KeyLocker.GetReaderLock(lockKey);

                try
                {
                    e = GetEntryInternal(key, true, operationContext, false, operationContext.NeedUserPayload);

                    if (e != null)
                    {
                        ExpirationHint exh = e.ExpirationHint;
                        if (exh != null)
                        {
                            if (exh.CheckExpired(_context))
                            {
                                ItemRemoveReason reason = (exh.GetExpiringHint() is FixedExpiration ||
                                                           exh.GetExpiringHint() is IdleExpiration)
                                    ? ItemRemoveReason.Expired
                                    : ItemRemoveReason.DependencyChanged;
                                // If cache forward is set we skip the expiration.
                                if (!exh.NeedsReSync)
                                {
                                    var cloneEntry = operationContext.CloneCacheEntry;
                                    if (cloneEntry) operationContext.CloneCacheEntry = false;

                                    Remove(key, reason, true, null, 0, LockAccessType.IGNORE_LOCK, operationContext);

                                    if (cloneEntry) operationContext.CloneCacheEntry = cloneEntry;

                                    //return entry back to transactional pool

                                    e = null;
                                }
                            }

                            if (exh.IsVariant)
                            {
                                try
                                {
                                    _context.ExpiryMgr.ResetVariant(exh);
                                }
                                catch (Exception ex)
                                {

                                    var entry = RemoveInternal(key, ItemRemoveReason.Removed, false, operationContext,false,false);

                                    //return entry back to store pool
                                    MiscUtil.ReturnEntryToPool(entry, _context.StorePoolManager);
                                    
                                    throw ex;
                                }
                            }
                        }
                        if (e != null)
                        {
                            if (operationContext.CloneCacheEntry)
                                e = e.DeepClone(operationContext.UseObjectPool ? _context.TransactionalPoolManager : _context.FakeObjectPool,true);

                            entries[key] = e;
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    entries[key] = ex;
                }
                finally
                {
                    KeyLocker.ReleaseReaderLock(lockKey);
                }
            }
            return entries;
        }

        /// <summary>
        /// Adds key and value pairs to the cache. Throws an exception or returns the
        /// list of keys that failed to add in the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">the cache entries.</param>
        /// <returns>List of keys that are added or that alredy exists in the cache and their status.</returns>
        public sealed override Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {

            Hashtable table = new Hashtable();
            if (!IsCacheOperationAllowed(operationContext))
                return table;
            EventContext eventContext = null;
            EventId eventId = null;
            OperationID opId = operationContext.OperatoinID;
            try
            {
                cacheEntries?.MarkInUse(NCModulesConstants.LocalBase);
                operationContext.Add(OperationContextFieldName.DonotRegisterSyncDependency, true);
                operationContext?.MarkInUse(NCModulesConstants.LocalBase);


                for (int i = 0; i < keys.Length; i++)
                {
                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    }

                    var lockKey = GetStringFromPool((string)keys[i]);
                    KeyLocker.GetWriterLock(lockKey);
                    try
                    {
                        operationContext.RemoveValueByField(OperationContextFieldName.EventContext);
                        if (notify)
                        {
                            //generate EventId
                            eventId = new EventId();
                            eventId.EventUniqueID = opId.OperationId;
                            eventId.OperationCounter = opId.OpCounter;
                            eventId.EventCounter = i;
                            eventContext = new EventContext();
                            eventContext.Add(EventContextFieldName.EventID, eventId);
                            operationContext.Add(OperationContextFieldName.EventContext, eventContext);
                        }
                        CacheAddResult result = Add(keys[i], cacheEntries[i], notify, operationContext);
                        table[keys[i]] = result;
                        
                    }
                    catch (Exceptions.StateTransferException se)
                    {
                        table[keys[i]] = se;
                    }
                    catch (Exception inner)
                    {
                        table[keys[i]] = new OperationFailedException(inner.Message, inner);
                    }
                    finally
                    {
                        KeyLocker.ReleaseWriterLock(lockKey);
                        operationContext.RemoveValueByField(OperationContextFieldName.EventContext);
                    }

                }
                operationContext.RemoveValueByField(OperationContextFieldName.DonotRegisterSyncDependency);
                if (_context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.SetCacheSize(Size);
                }
                return table;
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.LocalBase);
                cacheEntries?.MarkFree(NCModulesConstants.LocalBase);
            }
        }

        /// <summary>
        /// Adds key and value pairs to the cache. If any of the specified keys
        /// already exists in the cache; it is updated, otherwise a new item is
        /// added to the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntry">the cache entries.</param>
        /// <returns>returns keys that are added or updated successfully and their status.</returns>
        public sealed override Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            Hashtable table = new Hashtable();
            if (!IsCacheOperationAllowed(operationContext))
                return table;
            EventContext eventContext = null;
            EventId eventId = null;
            OperationID opId = operationContext.OperatoinID;
            ArrayList syncDependencyKeys = null;
            operationContext.Add(OperationContextFieldName.DonotRegisterSyncDependency, true);

            for (int i = 0; i < keys.Length; i++)
            {
                if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);
                }

                var lockKey = GetStringFromPool((string)keys[i]);
                KeyLocker.GetWriterLock(lockKey);
                try
                {
                    operationContext.RemoveValueByField(OperationContextFieldName.EventContext);
                    if (notify)
                    {
                        eventId = new EventId();
                        eventId.EventUniqueID = opId.OperationId;
                        eventId.OperationCounter = opId.OpCounter;
                        eventId.EventCounter = i;
                        eventContext = new EventContext();
                        eventContext.Add(EventContextFieldName.EventID, eventId);
                        operationContext.Add(OperationContextFieldName.EventContext, eventContext);
                    }
                    CacheInsResultWithEntry result = Insert(keys[i], cacheEntries[i], notify, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                    
                    table.Add(keys[i], result);
                }
                catch (Exception e)
                {
                    table[keys[i]] = e;
                }
                finally
                {
                    KeyLocker.ReleaseWriterLock(lockKey);
                }
            }

            operationContext.RemoveValueByField(OperationContextFieldName.DonotRegisterSyncDependency);
            
            return table;
        }

        /// <summary>
        /// Removes key and value pairs from the cache. The keys are specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="keys">keys of the entry.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>list of removed keys</returns>
        public sealed override Hashtable Remove(IList keys, ItemRemoveReason removalReason, bool notify, OperationContext operationContext)
        {
            return Remove(keys, removalReason, notify, true, operationContext);
        }

        /// <summary>
        /// Removes key and value pairs from the cache. The keys are specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="keys">keys of the entry.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>list of removed keys</returns>
        public override Hashtable Remove(IList keys, ItemRemoveReason removalReason, bool notify, bool isUserOperation, OperationContext operationContext)
        {
            Hashtable table = new Hashtable();
            if (!IsCacheOperationAllowed(operationContext))
                return table;
            EventContext eventContext = null;
            EventId eventId = null;
            OperationID opId = operationContext.OperatoinID;
            ArrayList syncDependencyKeys = null;
            operationContext.Add(OperationContextFieldName.DonotRegisterSyncDependency, true);
            for (int i = 0; i < keys.Count; i++)
            {
                if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                var lockKey = GetStringFromPool((string)keys[i]);
                KeyLocker.GetWriterLock(lockKey);
                try
                {
                    operationContext.RemoveValueByField(OperationContextFieldName.EventContext);
                    if (notify)
                    {
                        //generate EventId
                        eventId = new EventId();
                        eventId.EventUniqueID = opId.OperationId;
                        eventId.OperationCounter = opId.OpCounter;
                        eventId.EventCounter = i;
                        eventContext = new EventContext();
                        eventContext.Add(EventContextFieldName.EventID, eventId);
                        operationContext.Add(OperationContextFieldName.EventContext, eventContext);
                    }
                    CacheEntry e = Remove(keys[i], removalReason, notify, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                    if (e != null)
                    {
                        table[keys[i]] = e;
                        
                    }
                }
                catch (StateTransferException e)
                {
                    table[keys[i]] = e;
                }
                finally
                {
                    KeyLocker.ReleaseWriterLock(lockKey);
                }
            }

            operationContext.RemoveValueByField(OperationContextFieldName.EventContext);

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }
            
            return table;
        }

        #endregion

        #region ---                         LocalCacheBase.XXXXInternal             ----

        /// <summary>
        /// Removes all entries from the cache.
        /// </summary>
        internal virtual void ClearInternal()
        {
        }

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        internal virtual bool ContainsInternal(object key)
        {
            return false;
        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cloneEntry">Flag indicates weather a deep clone of cache item is required or not</param>
        /// <param name="needUserPayload">If set false, cloned cache item will have no user payload. It will be a simple cache item with just meta data</param>
        /// <returns>cache entry.</returns>
        internal virtual CacheEntry GetInternal(object key, bool isUserOperation, OperationContext operationContext,bool cloneEntry,bool needUserPayload)
        {
            return null;
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        internal virtual CacheAddResult AddInternal(object key, CacheEntry cacheEntry, bool isUserOperation, OperationContext operationContext)
        {
            return CacheAddResult.Failure;
        }

        /// <summary>
        /// Add an ExpirationHint against the given key
        /// Key must already exists in the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        internal virtual bool AddInternal(object key, ExpirationHint eh, OperationContext operationContext)
        {
            return false;
        }

        /// <summary>
        /// Remove an ExpirationHint against the given key
        /// Key must already exists in the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        internal virtual bool RemoveInternal(object key, ExpirationHint eh)
        {
            return false;
        }

        /// <summary>
        /// Add an ExpirationHint against the given key
        /// Key must already exists in the cache
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal virtual bool AddInternal(object key)
        {
            return false;
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        internal virtual CacheInsResult InsertInternal(object key, CacheEntry cacheEntry, bool isUserOperation, CacheEntry oldEntry, OperationContext operationContext, bool updateIndex)
        {
            return CacheInsResult.Failure;
        }

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>item value</returns>
        internal virtual CacheEntry RemoveInternal(object key, ItemRemoveReason removalReason, bool isUserOperation, OperationContext operationContext, bool cloneEntry, bool needUserPayload)
        {
            return null;
        }

        

        internal virtual CacheEntry GetEntryInternal(object key, bool isUserOperation,OperationContext operationContext, bool cloneCacheEntry,bool needUserPayload)
        {
            return null;
        }

        
        public virtual void Evict()
        {
        }

        #endregion

        #region/            --- Keybased Notification Registration ---          /

        public override void RegisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.data_sharing).UpdateUsageTime();
            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.selective_events, FeatureEnum.data_sharing).UpdateUsageTime();

            if (keys != null)
            {
                foreach (string key in keys)
                {
                    if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    var lockKey = GetStringFromPool(key);
                    KeyLocker.GetWriterLock(lockKey);
                    try
                    {
                        RegisterKeyNotification(key, updateCallback, removeCallback, operationContext);
                    }
                    finally
                    {
                        KeyLocker.ReleaseWriterLock(lockKey);
                    }
                }
            }
        }

        /// <summary>
        /// Registers either an update or remove callback with an existing item.
        /// </summary>
        /// <param name="key">for which notificatin is to be registered</param>
        /// <param name="updateCallback">ItemUpdate callback to be registered.</param>
        /// <param name="removeCallback">ItemRemove callback to be registered.</param>
        public override void RegisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.data_sharing).UpdateUsageTime();
            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.selective_events, FeatureEnum.data_sharing).UpdateUsageTime();

            CacheEntry entry = Get(key, operationContext);
            CacheEntry oldEntry = entry;
            try
            {
                if (entry != null)
                {
                    entry = entry.DeepClone(_context.StorePoolManager, true);
                    long oldSize = entry.InMemorySize;
                    entry.AddCallbackInfo(updateCallback, removeCallback);
                    long newSize = entry.InMemorySize;

                    StoreInsResult result = _cacheStore.Insert(key, entry, false);
                    _cacheStore.ChangeCacheSize(newSize - oldSize);

                    //Even if one of the callbacks is pull based register
                    if ((updateCallback != null && updateCallback.CallbackType == CallbackType.PullBasedCallback) || (removeCallback != null && removeCallback.CallbackType == CallbackType.PullBasedCallback))
                    {
                        string clientId = null;
                        if (operationContext.Contains(OperationContextFieldName.ClientId))
                            clientId = operationContext.GetValueByField(OperationContextFieldName.ClientId) as string;
                        if (string.IsNullOrEmpty(clientId))
                            clientId = removeCallback.Client;

                        _notificationManager.RegisterKeyNotification(clientId, key, updateCallback, removeCallback);
                    }

                    if (result == StoreInsResult.NotEnoughSpace || result == StoreInsResult.SuccessNearEviction
                         || result == StoreInsResult.SuccessOverwriteNearEviction)
                    {
                        //items removal operations are replicated from the source cache to the target cache.
                        
                            Evict();
                    }

                    if (_context.PerfStatsColl != null)
                    {
                        _context.PerfStatsColl.SetCacheSize(Size);
                    }
                }
            }
            finally
            {
                MiscUtil.ReturnEntryToPool(oldEntry, _context.TransactionalPoolManager);
            }
        }

        public override void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            if (keys != null)
            {
                foreach (string key in keys)
                {
                    if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    var lockKey = GetStringFromPool(key);
                    KeyLocker.GetWriterLock(lockKey);
                    try
                    {
                        UnregisterKeyNotification(key, updateCallback, removeCallback, operationContext);
                    }
                    finally
                    {
                        KeyLocker.ReleaseWriterLock(lockKey);
                    }
                }
            }
        }

        /// <summary>
        /// Unregisters either an update or remove callback with an existing item.
        /// </summary>
        /// <param name="key">for which notificatin is to be unregistered</param>
        /// <param name="updateCallback">ItemUpdate callback to be unregistered.</param>
        /// <param name="removeCallback">ItemRemove callback to be unregistered.</param>

        public override void UnregisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            try
            {

                CacheEntry entry = Get(key, operationContext);
                CacheEntry oldEntry = entry;
                     
                if (entry != null)
                {

                    entry = entry.DeepClone(_context.StorePoolManager, true);
                    long oldSize = entry.InMemorySize;
                    entry.RemoveCallbackInfo(updateCallback, removeCallback);
                    long newSize = entry.InMemorySize;

                    string clientId = null;

                    if (updateCallback != null)
                        clientId = updateCallback.Client;
                    else if (removeCallback != null)
                        clientId = removeCallback.Client;

                    _notificationManager.UnRegisterKeyNotification(clientId, key, updateCallback, removeCallback);

                    _cacheStore.Insert(key, entry, false);
                    _cacheStore.ChangeCacheSize(newSize - oldSize);

                    if (_context.PerfStatsColl != null)
                    {
                        _context.PerfStatsColl.SetCacheSize(Size);
                    }
                    MiscUtil.ReturnEntryToPool(oldEntry, _context.TransactionalPoolManager);
                }
            }
            catch (StateTransferException)
            {
                //ignore state transfer expcetion
            }
        }

        #endregion
       

        #region /               --- Stream Operations ---                   /

        public override bool OpenStream(string key, string lockHandle, Alachisoft.NCache.Common.Enum.StreamModes mode, string group, string subGroup, ExpirationHint hint, EvictionHint evictinHint, OperationContext operationContext)
        {
            return true;
        }


        public override void CloseStream(string key, string lockHandle, OperationContext operationContext)
        {
            
        }
        public override int ReadFromStream(ref VirtualArray vBuffer, string key, string lockHandle, int offset, int length, OperationContext operationContext)
        {
            return 0;
        }

        public override void WriteToStream(string key, string lockHandle, Alachisoft.NCache.Common.DataStructures.VirtualArray vBuffer, int srcOffset, int dstOffset, int length, OperationContext operationContext)
        {
            
        }

        public override long GetStreamLength(string key, string lockHandle, OperationContext operationContext)
        {
           

            return 0;

        }


        #endregion

       

        public override Common.Events.PollingResult Poll(OperationContext context)
        {
            Common.Events.PollingResult pollingResult = null;
            if (!IsCacheOperationAllowed(context))
                return pollingResult;

            try
            {
                pollingResult = _notificationManager.Poll(context.GetValueByField(OperationContextFieldName.ClientId) as string);

                if (_context.NCacheLog.IsInfoEnabled && (pollingResult.RemovedKeys.Count + pollingResult.UpdatedKeys.Count) > 0)
                    _context.NCacheLog.Info("LocalCacheBase.PollRequest()", "Poll operation returning " + (pollingResult.RemovedKeys.Count + pollingResult.UpdatedKeys.Count) + " updates.");

                return pollingResult;
            }
            catch (Exception exc)
            {
                if (_context.NCacheLog.IsErrorEnabled)
                    _context.NCacheLog.Error("LocalCacheBase.Poll", exc.ToString());
                throw;
            }
        }

        public override void RegisterPollingNotification(short callbackId, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            if (_notificationManager != null)
                _notificationManager.RegisterPollNotification(callbackId, operationContext);
        }
        
        /// <summary>
        /// Determines whether the cache contains the given keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>list of keys found in the cache</returns>
        internal override void Touch(List<string> keys, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            for (int i = 0; i < keys.Count; i++)
            {
                var lockKey = GetStringFromPool(keys[i]);
                KeyLocker.GetReaderLock(lockKey);

                try
                {
                    TouchInternal(keys[i], operationContext);
                }
                catch (StateTransferException se)
                {
                }
                finally
                {
                    KeyLocker.ReleaseReaderLock(lockKey);
                }
            }
        }

        internal virtual void TouchInternal(string key, OperationContext operationContext)
        {
        }
        #region Event Handlers
        /// <summary>
        /// Hanlder for clustered item update callback notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// /// <param name="entry">callback entry</param>
        /// <returns>null</returns>
        private object handleNotifyUpdateCallback(object info)
        {
            object[] objs = (object[])info;
            NotifyCustomUpdateCallback(objs[0], objs[1], true, null, (EventContext)objs[2]);
            return null;
        }
        private object handleOldNotifyUpdateCallback(object info)
        {
            object[] objs = (object[])info;
            NotifyOldCustomUpdateCallback(objs[0], objs[1], true, null, (EventContext)objs[2]);
            return null;
        }
        private object handleNotifyRemoveCallback(object info)
        {
            object[] data = (object[])info;
            NotifyCustomRemoveCallback(data[0], data[1], (ItemRemoveReason)data[2], true, (OperationContext)data[3], (EventContext)data[4]);
            return null;
        }
        private object handleOldNotifyRemoveCallback(object info)
        {
            object[] data = (object[])info;
            NotifyOldCustomRemoveCallback(data[0], data[1], (ItemRemoveReason)data[2], true, (OperationContext)data[3], (EventContext)data[4]);
            return null;
        }
        private void handleNotifyAdd(object info)
        {
            object[] data = (object[])info;
            NotifyItemAdded(data[0], true, (OperationContext)data[1], (EventContext)data[2]);
        }
        private void handleOldNotifyAdd(object info)
        {
            object[] data = (object[])info;
            NotifyOldItemAdded(data[0], true, (OperationContext)data[1], (EventContext)data[2]);
        }
        /// <summary>
        /// Handler for item add event.
        /// </summary>
        /// <param name="info"></param>
        private void handleNotifyUpdate(object[] info)
        {
            object[] args = info as object[];
            NotifyItemUpdated(args[0], true, (OperationContext)args[1], (EventContext)args[2]);
        }
        private void handleOldNotifyUpdate(object[] info)
        {
            object[] args = info as object[];
            NotifyOldItemUpdated(args[0], true, (OperationContext)args[1], (EventContext)args[2]);
        }
        private void handleNotifyRemove(object info)
        {
            object[] objs = (object[])info;


            NotifyItemRemoved(objs[0], (ArrayList)objs[1], (ItemRemoveReason)objs[2], true, (OperationContext)objs[3], (EventContext)objs[4]);
        }
        private void handleOldNotifyRemove(object info)
        {
            object[] objs = (object[])info;

            NotifyOldItemRemoved(objs[0], (ArrayList)objs[1], (ItemRemoveReason)objs[2], true, (OperationContext)objs[3], (EventContext)objs[4]);
        }

        #endregion

        #region Raising Events
        public override void RaiseCustomUpdateCalbackNotifier(object key, ArrayList itemUpdateCallbackListener,
          EventContext eventContext)
        {
            handleNotifyUpdateCallback(new object[] { key, itemUpdateCallbackListener, eventContext });
        }
        public override void RaiseOldCustomUpdateCalbackNotifier(object key, ArrayList itemUpdateCallbackListener,
          EventContext eventContext)
        {
            handleOldNotifyUpdateCallback(new object[] { key, itemUpdateCallbackListener, eventContext });
        }
        public override void RaiseCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason,
           OperationContext operationContext, EventContext eventContext)
        {
            eventContext.Add(EventContextFieldName.ItemRemoveCallbackList,
                  cacheEntry.Notifications.ItemRemoveCallbackListener.Clone());
            handleNotifyRemoveCallback(new object[] { key, cacheEntry.Notifications.ItemRemoveCallbackListener, reason, operationContext, eventContext });
        }
        public override void RaiseOldCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason,
          OperationContext operationContext, EventContext eventContext)
        {
            eventContext.Add(EventContextFieldName.ItemRemoveCallbackList,
                  cacheEntry.Notifications.ItemRemoveCallbackListener.Clone());
            handleOldNotifyRemoveCallback(new object[] { key, cacheEntry.Notifications.ItemRemoveCallbackListener, reason, operationContext, eventContext });
        }
        /// <summary>
        /// Broadcasts an itemadd notifier across the cluster
        /// </summary>
        /// <param name="key"></param>
        public override void RaiseItemAddNotifier(object key, CacheEntry entry, OperationContext context,
            EventContext eventContext)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsItemAddNotifier)
            {

                handleNotifyAdd(new object[] { key, context, eventContext });
            }
        }
        
        /// <summary>
        /// Broadcasts an itemaupdate notifier across the cluster
        /// </summary>
        /// <param name="key"></param>
        public override void RaiseItemUpdateNotifier(object key, OperationContext operationContext, EventContext eventcontext)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsItemUpdateNotifier)
            {
                //RaiseGeneric(new Function((int)OpCodes.NotifyUpdate, new object[] { key, operationContext, eventcontext }));
                handleNotifyUpdate(new object[] { key, operationContext, eventcontext });
            }
        }

        public override void RaiseOldItemRemoveNotifier(object key, OperationContext operationContext, EventContext eventcontext)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsItemRemoveNotifier)
            {
                //RaiseGeneric(new Function((int)OpCodes.NotifyUpdate, new object[] { key, operationContext, eventcontext }));
                handleOldNotifyRemove(new object[] { key, operationContext, eventcontext });
            }
        }
        public override void RaiseItemRemoveNotifier(object packed)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsItemRemoveNotifier)
            {
                // RaiseGeneric(new Function((int)OpCodes.NotifyRemoval, packed));
                handleNotifyRemove(packed);
            }
        }
        public override void RaiseOldItemRemoveNotifier(object packed)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsItemRemoveNotifier)
            {
                handleOldNotifyRemove(packed);
            }
        }

        #endregion
        

        #region Private Methods

        private string GetStringFromPool(string str)
        {
            return Context.TransactionalPoolManager.StringPool.GetString(str);
        }

        #endregion

        protected virtual void PopulateQueryInfo(object key,CacheEntry entry, OperationContext operationContext)
        {
            
        }

    }
}
