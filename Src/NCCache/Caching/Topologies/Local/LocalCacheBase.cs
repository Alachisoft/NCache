// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Statistics;

using Alachisoft.NCache.Parser;
using Alachisoft.NCache.Caching.Queries.Filters;
using Alachisoft.NCache.Caching.Queries.Continuous;

using Alachisoft.NCache.Runtime.Exceptions;

using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Util;

using Alachisoft.NCache.Common.Locking;

using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Caching.Queries;

using Alachisoft.NCache.Runtime.Caching;

using System.Collections.Generic;

using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Events;
using EventType = Alachisoft.NCache.Persistence.EventType;

using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Storage;

using Alachisoft.NCache.MapReduce;

using Alachisoft.NCache.Caching.CacheSynchronization;

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

        protected ActiveQueryAnalyzer _activeQueryAnalyzer;
        int insertCounter = 0;
        int removeCounter = 0;

        ShutDownServerInfo _shutdownServer = null;

        private TaskManager _taskManager = null;

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
        public LocalCacheBase(IDictionary properties, CacheBase parentCache, ICacheEventsListener listener, CacheRuntimeContext context, ActiveQueryAnalyzer activeQueryAnalyzer)
            : base(properties, listener, context)
        {
            _preparedQueryTableSize = ServiceConfiguration.PreparedQueryTableSize;
            
            _preparedQueryEvictionPercentage = ServiceConfiguration.PreparedQueryEvictionPercentage;

            _dataChunkSize = ServiceConfiguration.DataChunkSize;

            _activeQueryAnalyzer = activeQueryAnalyzer;

            _stats = new CacheStatistics();

            _stats.InstanceName = _context.PerfStatsColl.InstanceName;

            _parentCache = parentCache;

            _taskManager = new TaskManager(properties, context);
           
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

            
            if (_activeQueryAnalyzer != null)
            {
                _activeQueryAnalyzer.Dispose();
                _activeQueryAnalyzer = null;
            }
            if (_taskManager != null)
            {
                _taskManager.Dispose();
                _taskManager = null;
            }

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

        public override ActiveQueryAnalyzer QueryAnalyzer
        {
            get
            {
                return _activeQueryAnalyzer;
            }
        }

        /// <summary> 
        /// Returns the cache local to the node, i.e., internal cache.
        /// </summary>
        protected internal override CacheBase InternalCache
        {
            get { return this; }
        }

        /// <summary> 
        /// Returns true if this is the root internal cache.
        /// </summary>
        protected bool IsSelfInternal
        {
            get
            {
                if (_context.CacheInternal is CacheSyncWrapper)
                    return ReferenceEquals(this, ((CacheSyncWrapper)_context.CacheInternal).Internal);
                return ReferenceEquals(this, _context.CacheInternal);
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
        public sealed override void Clear(CallbackEntry cbEntry, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
        {
            ClearInternal();
            
            if(_context.SyncManager!=null)
                _context.SyncManager.Clear();

            EventContext eventContext = null;
            EventId eventId = null;

            //generate event id


            if (_activeQueryAnalyzer != null)
            {
                eventContext = new EventContext();
                eventId = EventId.CreateEventId(operationContext.OperatoinID);
                eventId.EventType = EventType.CQ_CALLBACK;
                eventContext.Add(EventContextFieldName.EventID, eventId);

                _activeQueryAnalyzer.Clear(operationContext, eventContext);
            }

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
            NotifyCacheCleared(false, operationContext, eventContext);
        }

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element 
        /// with the specified key; otherwise, false.</returns>
        public sealed override bool Contains(object key, OperationContext operationContext)
        {
            if (ContainsInternal(key))
            {
                
                CacheEntry e = GetInternal(key, true, operationContext);
                if (e == null) return false;
                if (e.ExpirationHint != null && e.ExpirationHint.CheckExpired(_context))
                {
                    ExpirationHint exh = e.ExpirationHint;
                    ItemRemoveReason reason = (exh.GetExpiringHint() is FixedExpiration || exh.GetExpiringHint() is IdleExpiration) ? ItemRemoveReason.Expired : ItemRemoveReason.DependencyChanged;
                    Remove(key, reason, true, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                    return false;
                }
                return true;
            }
            return false;
        }

        public override LockOptions Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            LockOptions lockInfo = new LockOptions();

            CacheEntry e = GetInternal(key, false, operationContext);
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
            CacheEntry e = GetInternal(key, false, operationContext);
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
            Object tmpLockId = null;
            DateTime tmpLockDate = DateTime.Now;
            CacheEntry e = GetInternal(key, false, operationContext);
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
            CacheEntry entry = GetInternal(key, false, operationContext);
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
            return GetFromCache(GetGroupKeys(group, subGroup, operationContext), operationContext);
        }

        public override HashVector GetTagData(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            return GetFromCache(GetTagKeys(tags, comparisonType, operationContext), operationContext);
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
            Object[] keysArray=new Object[keys.Count];
            keys.CopyTo(keysArray,0);
            return Remove(keysArray, ItemRemoveReason.Removed, notify, operationContext);

        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        public sealed override CacheEntry Get(object key, bool isUserOperation, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry e = GetInternal(key, isUserOperation, operationContext);
            if (accessType != LockAccessType.IGNORE_LOCK)
            {
                if (e != null)
                {
                    if (accessType == LockAccessType.DONT_ACQUIRE)
                    {
                        bool success = e.CompareLock(lockId);
                        if (success)
                        {
                            //explicitly set the lockdate incase of compare lock.
                            //compare lock does not set the lockdate.
                            lockDate = e.LockDate;
                        }
                        else
                        {
                            success = !e.IsLocked(ref lockId, ref lockDate);
                        }

                        if (!success) { e = null; }


                    }
                    else if (accessType == LockAccessType.ACQUIRE && !e.Lock(lockExpiration, ref lockId, ref lockDate, operationContext))//internally sets the out parameters
                    {
                        e = null;
                    }
                    else if (accessType == LockAccessType.GET_VERSION)
                    {
                        version = e.Version;
                    }
                    else if (accessType == LockAccessType.COMPARE_VERSION)
                    {
                        if (e.IsNewer(version))
                        {
                            version = e.Version;
                        }
                        else
                        {
                            version = 0;
                            e = null;
                        }
                    }
                    else if (accessType == LockAccessType.MATCH_VERSION)
                    {
                        if (!e.CompareVersion(version))
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

            if (exh != null)
            {
               
                if (exh.CheckExpired(_context))
                {
                    // If cache forward is set we skip the expiration.
                    if (!exh.NeedsReSync)
                    {
                        ItemRemoveReason reason = (exh.GetExpiringHint() is FixedExpiration || exh.GetExpiringHint() is IdleExpiration) ? ItemRemoveReason.Expired : ItemRemoveReason.DependencyChanged;
                        Remove(key, reason, true, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                        e = null;
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
                        RemoveInternal(key, ItemRemoveReason.Removed, false, operationContext);
                        throw ex;
                    }
                }
            }

            //////////////////////////////////////////

            _stats.UpdateCount(this.Count);
            if (e != null)
                _stats.BumpHitCount();
            else
                _stats.BumpMissCount();

            return e;
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
            CacheAddResult result = CacheAddResult.Failure;
            try
            {
                if (_stateTransferKeyList != null && _stateTransferKeyList.ContainsKey(key) && notify)
                    return CacheAddResult.KeyExists;


                if (_context.SyncManager != null && cacheEntry.SyncDependency != null && !operationContext.Contains(OperationContextFieldName.DonotRegisterSyncDependency)) _context.SyncManager.AddDependency(key, cacheEntry.SyncDependency);

                result = AddInternal(key, cacheEntry, isUserOperation,operationContext);

                // There was a failure so we must stop further operation
                if (result == CacheAddResult.Failure)
                {
                    if (_context.SyncManager != null && cacheEntry.SyncDependency != null && !operationContext.Contains(OperationContextFieldName.DonotRegisterSyncDependency))
                        _context.SyncManager.RemoveDependency(key, cacheEntry.SyncDependency);
                }

                // Not enough space, evict and try again.
                if (result == CacheAddResult.NeedsEviction || result == CacheAddResult.SuccessNearEviction)
                {
                       Evict();
                    

                    if (result == CacheAddResult.SuccessNearEviction)
                        result = CacheAddResult.Success;
                }

                // This code should be added to allow the user 
                // to add a key value pair that has expired.
                if (result == CacheAddResult.KeyExists)
                {
                    if (_context.SyncManager != null && cacheEntry.SyncDependency != null && !operationContext.Contains(OperationContextFieldName.DonotRegisterSyncDependency)) _context.SyncManager.RemoveDependency(key, cacheEntry.SyncDependency);

                    
                    CacheEntry e = GetInternal(key, isUserOperation, operationContext);
                    if (e.ExpirationHint != null && e.ExpirationHint.CheckExpired(_context))
                    {
                        ExpirationHint exh = e.ExpirationHint;
                        ItemRemoveReason reason = (exh.GetExpiringHint() is FixedExpiration || exh.GetExpiringHint() is IdleExpiration) ? ItemRemoveReason.Expired : ItemRemoveReason.DependencyChanged;
                        Remove(key, ItemRemoveReason.Expired, true, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
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
                            _context.ExpiryMgr.ResetHint(null, cacheEntry.ExpirationHint); 
                        }
                        catch (Exception e)
                        {
                            RemoveInternal(key, ItemRemoveReason.Removed, false, operationContext);
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
                    if (notify)
                    {
                        //generate event id
                        if (operationContext != null)
                        {
                            if (!operationContext.Contains(OperationContextFieldName.EventContext)) //for atomic operations
                            {
                                eventId = EventId.CreateEventId(opId);
                            }
                            else //for bulk
                            {
                                eventId = ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                            }

                            eventId.EventType = EventType.ITEM_ADDED_EVENT;
                            eventContext = new EventContext();
                            eventContext.Add(EventContextFieldName.EventID, eventId);
                            eventContext.Item = CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.DataWithMetadata, cacheEntry);
                        }

                        NotifyItemAdded(key, false, (OperationContext)operationContext.Clone(), eventContext);
                    }




                    //Only MetaInformation is needed from cacheEntry in CQ notifications.
                    if(_activeQueryAnalyzer.IsRegistered(key, (MetaInformation)operationContext.GetValueByField(OperationContextFieldName.IndexMetaInfo)))
                    {
                        if (operationContext.Contains(OperationContextFieldName.RaiseCQNotification))
                        {
                            //generate event id
                            if (!operationContext.Contains(OperationContextFieldName.EventContext))
                                //for atomic operations
                            {
                                eventId = EventId.CreateEventId(opId);
                            }
                            else //for bulk
                            {
                                eventId =((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                            }

                            eventId.EventType = EventType.CQ_CALLBACK;
                            eventContext = new EventContext();
                            eventContext.Add(EventContextFieldName.EventID, eventId);
                            eventContext.Item =CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.DataWithMetadata,cacheEntry);

                            ((IQueryOperationsObserver) _activeQueryAnalyzer).OnItemAdded(key,(MetaInformation)operationContext.GetValueByField(OperationContextFieldName.IndexMetaInfo), this,_context.CacheRoot.Name,(bool) operationContext.GetValueByField(OperationContextFieldName.RaiseCQNotification),operationContext, eventContext);
                        }
                        else
                        {
                            eventContext = null;
                            ((IQueryOperationsObserver) _activeQueryAnalyzer).OnItemAdded(key,(MetaInformation)operationContext.GetValueByField(OperationContextFieldName.IndexMetaInfo), this,_context.CacheRoot.Name, false, operationContext, eventContext);
                        }
                    }
                }
                _stats.UpdateCount(this.Count);

                if (result == CacheAddResult.NeedsEviction)
                    if (_context.SyncManager != null && cacheEntry.SyncDependency != null && !operationContext.Contains(OperationContextFieldName.DonotRegisterSyncDependency)) 
                        _context.SyncManager.RemoveDependency(key, cacheEntry.SyncDependency);
            }
            finally
            {
                //cacheEntry.QueryInfo = null;
                //cacheEntry.MetaInformation = null;
                operationContext.RemoveValueByField(OperationContextFieldName.IndexMetaInfo);
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
            if (eh == null)
                return false;
            CacheEntry entry = GetInternal(key, false, operationContext);
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

        /// <summary>
        /// Add ExpirationHint against the given key
        /// Key must already exists in the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public sealed override bool Add(object key, CacheSynchronization.CacheSyncDependency syncDependency, OperationContext operationContext)
        {
            if(_context.SyncManager!=null)
                _context.SyncManager.AddDependency(key, syncDependency);

            bool result = AddInternal(key, syncDependency);
            if (!result && _context.SyncManager!=null)
            {
                _context.SyncManager.RemoveDependency(key, syncDependency);
            }

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }
            return result;
        }

        public sealed override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            return Insert(key, cacheEntry, notify, true, lockId, version, accessType, operationContext);
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
           
            CacheInsResultWithEntry result = new CacheInsResultWithEntry();
            try
            {
                int maxEvict = 3;

                string clientId = null;
                CacheEntry pe = null;
                CallbackEntry cbEtnry = null;
                OperationID opId = operationContext.OperatoinID;
                EventId eventId = null;
                EventContext eventContext = null;

                {
                    pe = GetInternal(key, false, operationContext);
                    result.Entry = pe;

                    if (pe != null && access != LockAccessType.IGNORE_LOCK)
                    {
                        if (access == LockAccessType.COMPARE_VERSION)
                        {
                            if (!pe.CompareVersion(version))
                            {
                                result.Result = CacheInsResult.VersionMismatch;
                                result.Entry = null;
                                return result;
                            }
                        }
                        else
                        {
                            if (access == LockAccessType.RELEASE || access == LockAccessType.DONT_RELEASE)
                            {
                                if (pe.IsItemLocked() && !pe.CompareLock(lockId))
                                {
                                    result.Result = CacheInsResult.ItemLocked;
                                    result.Entry = null;
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
                        if (pe.Value is CallbackEntry)
                        {
                            cbEtnry = pe.Value as CallbackEntry;
                            cacheEntry = CacheHelper.MergeEntries(pe, cacheEntry);
                        }

                        cacheEntry.MergeCallbackListeners(pe);
                    }

                    CallbackEntry cbEntry = cacheEntry.Value as CallbackEntry;

                    if (access == LockAccessType.PRESERVE_VERSION)
                    {
                        cacheEntry.Version = (UInt64)version;
                        isUserOperation = false;
                    }

                    if (_context.SyncManager != null && cacheEntry.SyncDependency != null && !operationContext.Contains(OperationContextFieldName.DonotRegisterSyncDependency)) _context.SyncManager.AddDependency(key, cacheEntry.SyncDependency);

                    result.Result = InsertInternal(key, cacheEntry, isUserOperation, pe, operationContext, true);

                    if ((result.Result == CacheInsResult.Success || result.Result == CacheInsResult.SuccessNearEvicition) && _stateTransferKeyList != null && _stateTransferKeyList.ContainsKey(key))
                    {
                        result.Result = result.Result == CacheInsResult.Success ? CacheInsResult.SuccessOverwrite : CacheInsResult.SuccessOverwriteNearEviction;
                    }

                    // There was a failure so we must stop further operation
                    if (result.Result == CacheInsResult.Failure || result.Result == CacheInsResult.IncompatibleGroup)
                    {
                        if (_context.SyncManager != null && cacheEntry.SyncDependency != null && !operationContext.Contains(OperationContextFieldName.DonotRegisterSyncDependency)) _context.SyncManager.RemoveDependency(key, cacheEntry.SyncDependency);
                    }

                    // Not enough space, evict and try again.
                    if (result.Result == CacheInsResult.NeedsEviction || result.Result == CacheInsResult.SuccessNearEvicition || result.Result == CacheInsResult.SuccessOverwriteNearEviction)
                    {
                        
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
                                    RemoveInternal(key, ItemRemoveReason.Removed, false, operationContext);
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

                        //Only MetaInformation is needed from cacheEntry in CQ notifications.
                    if(_activeQueryAnalyzer.IsRegistered(key, (MetaInformation)operationContext.GetValueByField(OperationContextFieldName.IndexMetaInfo)))
                    {
                        if (operationContext.Contains(OperationContextFieldName.RaiseCQNotification))
                        {

                            if (operationContext.Contains(OperationContextFieldName.EventContext))
                            {
                                eventId = ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                            }
                            else
                            {
                                eventId = EventId.CreateEventId(opId);
                            }

                            eventContext = new EventContext();
                            eventId.EventType = EventType.CQ_CALLBACK;
                            eventContext.Add(EventContextFieldName.EventID, eventId);
                            eventContext.Item = CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.DataWithMetadata, cacheEntry);
                            eventContext.OldItem = CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.DataWithMetadata, pe);

                            ((IQueryOperationsObserver)_activeQueryAnalyzer).OnItemUpdated(key, (MetaInformation)operationContext.GetValueByField(OperationContextFieldName.IndexMetaInfo), this, _context.CacheRoot.Name, (bool)operationContext.GetValueByField(OperationContextFieldName.RaiseCQNotification), operationContext, eventContext);

                        }
                        else
                        {
                            ((IQueryOperationsObserver)_activeQueryAnalyzer).OnItemUpdated(key, (MetaInformation)operationContext.GetValueByField(OperationContextFieldName.IndexMetaInfo), this, _context.CacheRoot.Name, false, operationContext, null);
                        }

                    }
                 }

                }

                _stats.UpdateCount(this.Count);

              
                
                if (result.Result == CacheInsResult.NeedsEviction || result.Result == CacheInsResult.NeedsEvictionNotRemove)
                {
                    if (_context.SyncManager != null && cacheEntry.SyncDependency != null && !operationContext.Contains(OperationContextFieldName.DonotRegisterSyncDependency)) _context.SyncManager.AddDependency(key, cacheEntry.SyncDependency);
                }

                switch (result.Result)
                {
                    case CacheInsResult.Success:
                        if (notify)
                        {
                            //generate event id
                            if (!operationContext.Contains(OperationContextFieldName.EventContext)) //for atomic operations
                            {
                                eventId = EventId.CreateEventId(opId);
                                eventContext = new EventContext();
                            }
                            else //for bulk
                            {
                                eventId = ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                            }

                            eventContext = new EventContext();
                            eventId.EventType = EventType.ITEM_ADDED_EVENT;
                            eventContext.Add(EventContextFieldName.EventID, eventId);
                            eventContext.Item = CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.DataWithMetadata, cacheEntry);

                            NotifyItemAdded(key, false, (OperationContext)operationContext.Clone(), eventContext);
                        }

                        break;
                    case CacheInsResult.SuccessOverwrite:

                        if (notify)
                        {
                            EventCacheEntry eventCacheEntry = CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.DataWithMetadata, cacheEntry); ;
                            EventCacheEntry oldEventCacheEntry = CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.DataWithMetadata, pe);

                            if (cbEtnry != null)
                            {

                                if (cbEtnry.ItemUpdateCallbackListener != null && cbEtnry.ItemUpdateCallbackListener.Count > 0)
                                {
                                    if (!operationContext.Contains(OperationContextFieldName.EventContext)) //for atomic operations
                                    {
                                        eventId = EventId.CreateEventId(opId);
                                        eventContext = new EventContext();
                                    }
                                    else //for bulk
                                    {
                                        eventId = ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                                    }

                                    eventContext = new EventContext();
                                    eventId.EventType = EventType.ITEM_UPDATED_CALLBACK;
                                    eventContext.Add(EventContextFieldName.EventID, eventId);
                                    eventContext.Item = eventCacheEntry;
                                    eventContext.OldItem = oldEventCacheEntry;

                                    NotifyCustomUpdateCallback(key, cbEtnry.ItemUpdateCallbackListener, false, (OperationContext)operationContext.Clone(), eventContext);
                                }
                            }
                            //generate event id
                            if (!operationContext.Contains(OperationContextFieldName.EventContext)) //for atomic operations
                            {
                                eventId = EventId.CreateEventId(opId);
                                eventContext = new EventContext();
                            }
                            else //for bulk
                            {
                                eventId = ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                            }

                            eventContext = new EventContext();
                            eventId.EventType = EventType.ITEM_UPDATED_EVENT;
                            eventContext.Add(EventContextFieldName.EventID, eventId);
                            eventContext.Item = eventCacheEntry;
                            eventContext.OldItem = oldEventCacheEntry;

                            NotifyItemUpdated(key, false, (OperationContext)operationContext.Clone(), eventContext);
                        }
                        break;
                }
            }
            finally
            {
                operationContext.RemoveValueByField(OperationContextFieldName.IndexMetaInfo);

            }

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }

            return result;
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
            CacheEntry pe = null;
            //lock(this)
            {
                object actualKey = key;
                if (key is object[])
                {
                    actualKey = ((object[])key)[0];
                }

                if (accessType == LockAccessType.COMPARE_VERSION)
                {
                    pe = GetInternal(actualKey, false, operationContext);
                    if (pe != null)
                    {
                        if (!pe.CompareVersion(version))
                        {
                            throw new LockingException("Item in the cache does not exist at the specified version.");
                        }
                    }
                }
                else if (accessType != LockAccessType.IGNORE_LOCK)
                {
                    pe = GetInternal(actualKey, false, operationContext);
                    if (pe != null)
                    {
                        if (pe.IsItemLocked() && !pe.CompareLock(lockId))
                        {
                            throw new LockingException("Item is locked.");
                        }
                    }
                }



                e = RemoveInternal(actualKey, removalReason, isUserOperation, operationContext);

               
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
                   
                    try
                    {                       
                        if (e.ExpirationHint != null)
                        {
                            _context.ExpiryMgr.RemoveFromIndex(key);
                            ((IDisposable)e.ExpirationHint).Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        NCacheLog.Error("LocalCacheBase.Remove(object, ItemRemovedReason, bool):", ex.ToString());
                    }

                    if (_context.SyncManager != null && e.SyncDependency != null && !operationContext.Contains(OperationContextFieldName.DonotRegisterSyncDependency))
                        _context.SyncManager.RemoveDependency(actualKey, e.SyncDependency);

                    if (IsSelfInternal)
                    {
                        // Disposed the one and only cache entry.
                        ((IDisposable)e).Dispose();


                        if (!_context.IsDbSyncCoordinator && (removalReason == ItemRemoveReason.Expired ||
                            removalReason == ItemRemoveReason.DependencyChanged))
                        {

                            _context.PerfStatsColl.IncrementExpiryPerSecStats();

                        }

                        else if (!_context.CacheImpl.IsEvictionAllowed && removalReason == ItemRemoveReason.Underused)
                        {

                            _context.PerfStatsColl.IncrementEvictPerSecStats();

                        }

                        _context.PerfStatsColl.IncrementCountStats((long)Count);

                    }

 
                    if (notify)
                    {
                        CallbackEntry cbEtnry = e.Value as CallbackEntry;
                      
                        if (cbEtnry != null && cbEtnry.ItemRemoveCallbackListener != null && cbEtnry.ItemRemoveCallbackListener.Count > 0)
                        {
                            //generate event id
                            if (!operationContext.Contains(OperationContextFieldName.EventContext)) //for atomic operations
                            {
                                eventId = EventId.CreateEventId(opId);
                            }
                            else //for bulk
                            {
                                eventId = ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                            }

                            eventId.EventType = EventType.ITEM_REMOVED_CALLBACK;
                            eventContext = new EventContext();
                            eventContext.Add(EventContextFieldName.EventID, eventId);
                            EventCacheEntry eventCacheEntry = CacheHelper.CreateCacheEventEntry(cbEtnry.ItemRemoveCallbackListener, e);          
                            if (eventCacheEntry != null)
                                eventCacheEntry.ReSyncExpiredItems = resyncRexpiredItems;
                            eventContext.Item = eventCacheEntry;
                            eventContext.Add(EventContextFieldName.ItemRemoveCallbackList, cbEtnry.ItemRemoveCallbackListener.Clone());

                            //Will always reaise the whole entry for old clients
                            NotifyCustomRemoveCallback(actualKey, e, removalReason, false, (OperationContext)operationContext.Clone(), eventContext);
                        }

                        //generate event id
                        if (!operationContext.Contains(OperationContextFieldName.EventContext)) //for atomic operations
                        {
                            eventId = EventId.CreateEventId(opId);
                        }
                        else //for bulk
                        {
                            eventId = ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                        }

                        eventId.EventType = EventType.ITEM_REMOVED_EVENT;
                        eventContext = new EventContext();
                        eventContext.Add(EventContextFieldName.EventID, eventId);
                        eventContext.Item = CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.DataWithMetadata, e);
                        //Will always reaise the whole entry for old clients
                        NotifyItemRemoved(actualKey, e, removalReason, false, (OperationContext)operationContext.Clone(), eventContext);
                    }



                    //[After ExecutiveBoard Memory Issue] Only MetaInformation is needed from cacheEntry in CQ notifications. 
                    //Empty MetaInformation is passed with Type because in Remove only Type is needed.

                    MetaInformation metaInfo = new MetaInformation(null);
                    metaInfo.Type = e.ObjectType;

                    //if (_activeQueryAnalyzer.IsRegistered(key,(MetaInformation) operationContext.GetValueByField(OperationContextFieldName.IndexMetaInfo)))
                    if (_activeQueryAnalyzer.IsRegistered(key, metaInfo))
                    {
                        if (operationContext.Contains(OperationContextFieldName.RaiseCQNotification))
                        {
                            //generate event id
                            if (!operationContext.Contains(OperationContextFieldName.EventContext))
                                //for atomic operations
                            {
                                eventId = EventId.CreateEventId(opId);
                            }
                            else //for bulk
                            {
                                eventId =((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                            }

                            eventId.EventType = EventType.CQ_CALLBACK;
                            eventContext = new EventContext();
                            eventContext.Add(EventContextFieldName.EventID, eventId);
                            eventContext.Item =CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.DataWithMetadata, e);

                            ((IQueryOperationsObserver) _activeQueryAnalyzer).OnItemRemoved(key, metaInfo, this,_context.CacheRoot.Name,(bool) operationContext.GetValueByField(OperationContextFieldName.RaiseCQNotification),operationContext, eventContext);
                        }
                        else
                        {
                            ((IQueryOperationsObserver) _activeQueryAnalyzer).OnItemRemoved(key, metaInfo, this,_context.CacheRoot.Name, false, operationContext, null);
                        }
                    }

                }
                else if (_stateTransferKeyList != null && _stateTransferKeyList.ContainsKey(key))
                {
                    _stateTransferKeyList.Remove(key);
                    if (_activeQueryAnalyzer.IsRegistered(key,(MetaInformation) operationContext.GetValueByField(OperationContextFieldName.IndexMetaInfo)))
                    {
                        if (operationContext.Contains(OperationContextFieldName.RaiseCQNotification))
                        {
                            //generate event id
                            if (!operationContext.Contains(OperationContextFieldName.EventContext))
                                //for atomic operations
                            {
                                eventId = EventId.CreateEventId(opId);
                            }
                            else //for bulk
                            {
                                eventId =
                                    ((EventContext)
                                        operationContext.GetValueByField(OperationContextFieldName.EventContext))
                                        .EventID;
                            }

                            eventId.EventType = EventType.CQ_CALLBACK;
                            eventContext = new EventContext();
                            eventContext.Add(EventContextFieldName.EventID, eventId);
                            eventContext.Item =CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.DataWithMetadata, e);

                            ((IQueryOperationsObserver) _activeQueryAnalyzer).OnItemRemoved(key, null, this,_context.CacheRoot.Name,(bool) operationContext.GetValueByField(OperationContextFieldName.RaiseCQNotification),operationContext, eventContext);

                        }
                        else
                        {
                            ((IQueryOperationsObserver) _activeQueryAnalyzer).OnItemRemoved(key, null, this, _context.CacheRoot.Name, false, operationContext, null);
                        }
                    }

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
        public sealed override void SendNotification(object notifId, object data)
        {
            base.NotifyCustomEvent(notifId, data, false, null, null);
        }


        public sealed override QueryResultSet Search(string query, IDictionary values, OperationContext operationContext)
        {
            try
            {
                _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();
               
                QueryContext queryContext = PrepareSearch(query, values, true);               
                switch (queryContext.ResultSet.Type)
                {
                    case QueryType.AggregateFunction:
                        break;

                    default:
                        if(queryContext.InternalQueryResult != null)
                            queryContext.ResultSet.SearchKeysResult = queryContext.InternalQueryResult.GetArrayList();
                        break;
                }
                _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample() ;
                
                if (queryContext.ResultSet != null)
                {
                   
                    long totalRowReturn = 0;
                    if (queryContext.ResultSet.GroupByResult != null)
                        totalRowReturn = queryContext.ResultSet.GroupByResult.RowCount;

                    else if(queryContext.ResultSet.SearchEntriesResult != null)
                            totalRowReturn =  queryContext.ResultSet.SearchEntriesResult.Count;

                    else if (queryContext.ResultSet.SearchKeysResult != null)
                            totalRowReturn =  queryContext.ResultSet.SearchKeysResult.Count;
                    
                    _context.PerfStatsColl.IncrementAvgQuerySize(totalRowReturn);
                    
                    
                }
                _context.PerfStatsColl.IncrementQueryPerSec();
                return queryContext.ResultSet;
            }
            catch (Parser.ParserException pe)
            {
                RemoveReduction(query);
                //throw new OperationFailedException(pe.Message, pe);
                throw new Runtime.Exceptions.ParserException(pe.Message, pe);
            }
        }

        public sealed override QueryResultSet SearchCQ(ContinuousQuery query, OperationContext operationContext)
        {
            try
            {               
                _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();
                
                Reduction currentQueryReduction = GetPreparedReduction(query.CommandText);
                
                if (currentQueryReduction.Tag is GroupByPredicate ) throw new Parser.ParserException("Search query doesn't support Group By Section");
                if (currentQueryReduction.Tag is OrderByPredicate) throw new Parser.ParserException("Search query doesn't support Order By Section");

                if (currentQueryReduction.Tokens[0].ToString().ToLower().Equals("delete")) throw new Parser.ParserException("Only select query is supported.");

                QueryContext queryContext = SearchInternal(currentQueryReduction.Tag as Predicate, MiscUtil.DeepClone(query.AttributeValues));

                ClusteredArrayList result = queryContext.InternalQueryResult.GetArrayList();

                switch (queryContext.ResultSet.Type)
                {
                    case QueryType.AggregateFunction:
                        break;

                    default: queryContext.ResultSet.SearchKeysResult = result;
                        break;
                }

                _activeQueryAnalyzer.RegisterPredicate(queryContext.TypeName, query.CommandText, MiscUtil.DeepClone(query.AttributeValues), currentQueryReduction.Tag as Predicate, query.UniqueId, result);

                _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();

                if (queryContext.ResultSet != null)
                {

                    long totalRowReturn = 0;
                    if (queryContext.ResultSet.GroupByResult != null)
                        totalRowReturn = queryContext.ResultSet.GroupByResult.RowCount;

                    else if (queryContext.ResultSet.SearchEntriesResult != null)
                        totalRowReturn = queryContext.ResultSet.SearchEntriesResult.Count;

                    else if (queryContext.ResultSet.SearchKeysResult != null)
                        totalRowReturn = queryContext.ResultSet.SearchKeysResult.Count;

                    _context.PerfStatsColl.IncrementAvgQuerySize(totalRowReturn);


                }
                _context.PerfStatsColl.IncrementQueryPerSec();
                return queryContext.ResultSet;
            }
            catch (Parser.ParserException pe)
            {
                RemoveReduction(query.CommandText);
                //throw new OperationFailedException(pe.Message, pe);
                throw new Runtime.Exceptions.ParserException(pe.Message, pe);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public sealed override QueryResultSet SearchCQ(string queryId, OperationContext operationContext)
        {
            QueryResultSet resultSet = new QueryResultSet();
            _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();

            resultSet.SearchKeysResult = _activeQueryAnalyzer.Search(queryId);

            _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();

            if (resultSet != null)
            {
                long totalRowReturn = 0;
                if (resultSet.GroupByResult != null)
                    totalRowReturn = resultSet.GroupByResult.RowCount;

                else if (resultSet.SearchEntriesResult != null)
                    totalRowReturn = resultSet.SearchEntriesResult.Count;

                else if (resultSet.SearchKeysResult != null)
                    totalRowReturn = resultSet.SearchKeysResult.Count;

                _context.PerfStatsColl.IncrementAvgQuerySize(totalRowReturn);


            }
            _context.PerfStatsColl.IncrementQueryPerSec();
            return resultSet;
        }

        public sealed override QueryResultSet SearchEntries(string query, IDictionary values, OperationContext operationContext)
        {
            try
            {
               
                _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();
                QueryContext queryContext = PrepareSearch(query, values, true);
                switch (queryContext.ResultSet.Type)
                {
                    case QueryType.AggregateFunction:
                        break;

                    case QueryType.GroupByAggregateFunction:
                        break;

                    default:
                        IDictionary result = new HashVector();
                        ICollection keyList = null;
                        IList updatekeys = null;
                        if (queryContext.InternalQueryResult != null && queryContext.InternalQueryResult.Count > 0)
                            keyList = queryContext.InternalQueryResult.GetArrayList();

                        if (keyList != null && keyList.Count > 0)
                        {
                            //object[] keys = new object[keyList.Count];
                            //keyList.CopyTo(keys, 0);

                            IDictionary tmp = GetEntries(keyList, operationContext);
                            IDictionaryEnumerator ide = tmp.GetEnumerator();

                            CompressedValueEntry cmpEntry = null;

                            while (ide.MoveNext())
                            {
                                CacheEntry entry = ide.Value as CacheEntry;
                                if (entry != null)
                                {
                                    cmpEntry = new CompressedValueEntry();
                                    cmpEntry.Value = entry.Value;
                                    if (cmpEntry.Value is CallbackEntry)
                                        cmpEntry.Value = ((CallbackEntry)cmpEntry.Value).Value;

                                    cmpEntry.Flag = ((CacheEntry)ide.Value).Flag;
                                    result[ide.Key] = cmpEntry;
                                    if ((entry.ExpirationHint != null && entry.ExpirationHint.IsVariant))
                                    {
                                        if (updatekeys == null)
                                            updatekeys = new ClusteredArrayList();
                                        updatekeys.Add(ide.Key);
                                    }
                                }
                            }
                        }

                        queryContext.ResultSet.Type = QueryType.SearchEntries;
                        queryContext.ResultSet.SearchEntriesResult = result;
                        queryContext.ResultSet.UpdateIndicesKeys = updatekeys;

                        break;
                }
                _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();

                if (queryContext.ResultSet != null)
                {
                   
                    long totalRowReturn = 0;
                    if (queryContext.ResultSet.GroupByResult != null)
                        totalRowReturn =  queryContext.ResultSet.GroupByResult.RowCount;

                    else if (queryContext.ResultSet.SearchEntriesResult != null)
                        totalRowReturn = queryContext.ResultSet.SearchEntriesResult.Count;

                    else if (queryContext.ResultSet.SearchKeysResult != null)
                        totalRowReturn =  queryContext.ResultSet.SearchKeysResult.Count;

                    _context.PerfStatsColl.IncrementAvgQuerySize(totalRowReturn);
                }
                _context.PerfStatsColl.IncrementQueryPerSec();
                return queryContext.ResultSet;
            }
            catch (Parser.ParserException pe)
            {
                RemoveReduction(query);
                //throw new OperationFailedException(pe.Message, pe);
                throw new Runtime.Exceptions.ParserException(pe.Message, pe);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public sealed override QueryResultSet SearchEntriesCQ(ContinuousQuery query, OperationContext operationContext)
        {
            try
            {
                Reduction currentQueryReduction = null;
                _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();
                currentQueryReduction = GetPreparedReduction(query.CommandText);
                     
                if (currentQueryReduction.Tag is GroupByPredicate) throw new Parser.ParserException("Search query doesn't support Group By Section");
                if (currentQueryReduction.Tag is OrderByPredicate) throw new Parser.ParserException("Search query doesn't support Order By Section");

                if (currentQueryReduction.Tokens[0].ToString().ToLower().Equals("delete")) throw new Parser.ParserException("Only select query is supported.");
                
                QueryContext queryContext = SearchInternal(currentQueryReduction.Tag as Predicate, MiscUtil.DeepClone(query.AttributeValues));
                ClusteredArrayList resultList = queryContext.InternalQueryResult.GetArrayList();
                switch (queryContext.ResultSet.Type)
                {
                    case QueryType.AggregateFunction:
                        break;

                    default:
                        HashVector result = new HashVector();
                        ClusteredArrayList keyList = null;
                        if (queryContext.InternalQueryResult.Count > 0)
                            keyList = resultList;
                        if (keyList != null && keyList.Count > 0)
                        {

                            IDictionary tmp = GetEntries(keyList.ToArray(), operationContext);
                            IDictionaryEnumerator ide = tmp.GetEnumerator();

                            CompressedValueEntry cmpEntry = null;

                            while (ide.MoveNext())
                            {
                                CacheEntry entry = ide.Value as CacheEntry;
                                if (entry != null)
                                {
                                    cmpEntry = new CompressedValueEntry();
                                    cmpEntry.Value = entry.Value;
                                    if (cmpEntry.Value is CallbackEntry)
                                        cmpEntry.Value = ((CallbackEntry)cmpEntry.Value).Value;

                                    cmpEntry.Flag = ((CacheEntry)ide.Value).Flag;
                                    result[ide.Key] = cmpEntry;
                                }
                            }
                        }

                        queryContext.ResultSet.Type = QueryType.SearchEntries;
                        queryContext.ResultSet.SearchEntriesResult = result;

                        break;
                }

                _activeQueryAnalyzer.RegisterPredicate(queryContext.TypeName, query.CommandText, MiscUtil.DeepClone(query.AttributeValues), currentQueryReduction.Tag as Predicate, query.UniqueId, resultList);
                
                _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();
                if (queryContext.ResultSet != null)
                {

                    long totalRowReturn = 0;
                    if (queryContext.ResultSet.GroupByResult != null)
                        totalRowReturn = queryContext.ResultSet.GroupByResult.RowCount;

                    else if (queryContext.ResultSet.SearchEntriesResult != null)
                        totalRowReturn = queryContext.ResultSet.SearchEntriesResult.Count;

                    else if (queryContext.ResultSet.SearchKeysResult != null)
                        totalRowReturn = queryContext.ResultSet.SearchKeysResult.Count;

                    _context.PerfStatsColl.IncrementAvgQuerySize(totalRowReturn);
                }
                _context.PerfStatsColl.IncrementQueryPerSec();
                
                return queryContext.ResultSet;
            }
            catch (Parser.ParserException pe)
            {
                RemoveReduction(query.CommandText);
                //throw new OperationFailedException(pe.Message, pe);
                throw new Runtime.Exceptions.ParserException(pe.Message, pe);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public sealed override QueryResultSet SearchEntriesCQ(string queryId, OperationContext operationContext)
        {
            IDictionary result = new HashVector();
            QueryResultSet resultSet = new QueryResultSet();
            
            _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();

            ClusteredArrayList keys = _activeQueryAnalyzer.Search(queryId);

            IDictionary tmp = GetEntries(keys.ToArray(), operationContext);
            IDictionaryEnumerator ide = tmp.GetEnumerator();

            CompressedValueEntry cmpEntry = null;

            while (ide.MoveNext())
            {
                CacheEntry entry = ide.Value as CacheEntry;
                if (entry != null)
                {
                    cmpEntry = new CompressedValueEntry();
                    cmpEntry.Value = entry.Value;
                    if (cmpEntry.Value is CallbackEntry)
                        cmpEntry.Value = ((CallbackEntry)cmpEntry.Value).Value;

                    cmpEntry.Flag = ((CacheEntry)ide.Value).Flag;
                    result[ide.Key] = cmpEntry;
                }
            }

            resultSet.Type = QueryType.SearchEntries;
            resultSet.SearchEntriesResult = result;

            _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();
            if (resultSet != null)
            {
                long totalRowReturn = 0;
                if (resultSet.GroupByResult != null)
                    totalRowReturn = resultSet.GroupByResult.RowCount;

                else if (resultSet.SearchEntriesResult != null)
                    totalRowReturn = resultSet.SearchEntriesResult.Count;

                else if (resultSet.SearchKeysResult != null)
                    totalRowReturn = resultSet.SearchKeysResult.Count;

                _context.PerfStatsColl.IncrementAvgQuerySize(totalRowReturn);


            }
            _context.PerfStatsColl.IncrementQueryPerSec();

            return resultSet;
        }

        private QueryContext PrepareSearch(string query, IDictionary values, bool searchExecution, Boolean includeFilters = false)
        {
            Reduction currentQueryReduction = null;

            try
            {
                currentQueryReduction = GetPreparedReduction(query);

                if (searchExecution)
                {
                    if (currentQueryReduction.Tag is GroupByPredicate) throw new Parser.ParserException("Search query doesn't support Group By Section");
                    if (currentQueryReduction.Tag is OrderByPredicate) throw new Parser.ParserException("Search query doesn't support Order By Section");
                }

                if (currentQueryReduction.Tokens[0].ToString().ToLower().Equals("delete")) throw new Parser.ParserException("Only select query is supported.");

                return SearchInternal(currentQueryReduction.Tag as Predicate, values,includeFilters);
            }
            catch (Parser.ParserException pe)
            {
                RemoveReduction(query);
                //throw new OperationFailedException(pe.Message, pe);
                throw new Runtime.Exceptions.ParserException(pe.Message, pe);
            }
            //catch (Exception ex)
            //{
            //    throw ex;
            //}
        }


        public sealed override ReaderResultSet Local_ExecuteReader(string query, IDictionary values, bool getData, int chunkSize, bool isInproc, OperationContext operationContext)
        {
            _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();
            ReaderResultSet result = ExecuteReaderInternal(query, values, operationContext);
            _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();
            if (result != null && result.RecordSet == null) //empty result set will not be registered
            {
                _context.PerfStatsColl.IncrementQueryPerSec();
                return result;
            }
            string clientId = operationContext.GetValueByField(OperationContextFieldName.ClientId) != null ? operationContext.GetValueByField(OperationContextFieldName.ClientId).ToString() : null;
           
            if (RSManager != null)
            {
                if (chunkSize != -1)
                    result.ChunkSize = chunkSize;
                else
                    result.ChunkSize = _dataChunkSize;
                if (result.RecordSet != null){
                    result.RecordSet.SubsetInfo = new Common.DataReader.SubsetInfo();
                    result.RecordSet.SubsetInfo.LastAccessedRowID = 0;
                    result.RecordSet.SubsetInfo.StartIndex = 0;
                }
                result.GetData = getData;
                if (getData)
                {
                    if (result.RecordSet != null && result.RecordSet.GetColumnMetaData().Contains(QueryKeyWords.ValueColumn))
                    {
                        result.RecordSet.GetColumnMetaData()[QueryKeyWords.ValueColumn].IsHidden = false;
                    }
                }
                if (result.RecordSet != null)
                {
                    result.ReaderID = RSManager.RegisterReader(clientId, result);
                    result = RSManager.GetRecordSet(result.ReaderID, 0, isInproc,operationContext);//first chunk
                }


            }
            _context.PerfStatsColl.IncrementQueryPerSec();
            return result;
        }

        public sealed override ReaderResultSet Local_ExecuteReaderCQ(string query, IDictionary values, bool getData, int chunkSize, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, Queries.QueryDataFilters datafilters, bool isInproc)
        {
            ReaderResultSet resultSet;
            ContinuousQuery cQuery = CQManager.GetCQ(query, values);
            //check if already exists logic
            if (CQManager.Exists(cQuery))
            {
                resultSet = Local_ExecuteReaderCQ(cQuery.UniqueId, getData, chunkSize, operationContext);
                CQManager.Update(cQuery, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, datafilters);
            }
            else
            {
                resultSet = Local_ExecuteReaderCQ(cQuery, getData, chunkSize, operationContext);
                CQManager.Register(cQuery, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, datafilters);
            }
            return resultSet;
            //resultSet.CQUniqueId = cQuery.UniqueId;
            //return base.Local_ExecuteReaderCQ(query, values, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters, isInproc);
        }

        public sealed override ReaderResultSet Local_ExecuteReaderCQ(string queryId, bool getData, int chunkSize, OperationContext operationContext)
        {
            Hashtable result = new Hashtable();
            QueryResultSet resultSet = new QueryResultSet();

            _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();

            IList keys = _activeQueryAnalyzer.Search(queryId);


            resultSet.ReaderResult = new ReaderResultSet();

            resultSet.ReaderResult.RecordSet = new RecordSet();

            RecordColumn keyColumn = new RecordColumn(QueryKeyWords.KeyColumn);
            keyColumn.AggregateFunctionType = AggregateFunctionType.NOTAPPLICABLE;
            keyColumn.ColumnType = ColumnType.KeyColumn;
            keyColumn.DataType = ColumnDataType.String;
            keyColumn.IsFilled = true;
            keyColumn.IsHidden = false;

            RecordColumn valueColumn = new RecordColumn(QueryKeyWords.ValueColumn);
            valueColumn.AggregateFunctionType = AggregateFunctionType.NOTAPPLICABLE;
            valueColumn.ColumnType = ColumnType.ValueColumn;
            valueColumn.DataType = ColumnDataType.CompressedValueEntry;
            valueColumn.IsFilled = false;
            valueColumn.IsHidden = true;

            resultSet.ReaderResult.RecordSet.AddColumn(keyColumn);
            resultSet.ReaderResult.RecordSet.AddColumn(valueColumn);

            foreach (string key in keys)
            {
                RecordRow row = resultSet.ReaderResult.RecordSet.CreateRow();
                row[QueryKeyWords.KeyColumn] = key;
                resultSet.ReaderResult.RecordSet.AddRow(row);
            }


            string clientId = operationContext.GetValueByField(OperationContextFieldName.ClientId) != null ? operationContext.GetValueByField(OperationContextFieldName.ClientId).ToString() : null;
            if (RSManager != null)
            {
                resultSet.ReaderResult.ChunkSize = _dataChunkSize;
                if (resultSet.ReaderResult.RecordSet != null)
                {
                    resultSet.ReaderResult.RecordSet.SubsetInfo = new Common.DataReader.SubsetInfo();
                    resultSet.ReaderResult.RecordSet.SubsetInfo.LastAccessedRowID = 0;
                    resultSet.ReaderResult.RecordSet.SubsetInfo.StartIndex = 0;
                }

                if (resultSet.ReaderResult.RecordSet != null && resultSet.ReaderResult.RecordSet.GetColumnMetaData().Contains(QueryKeyWords.ValueColumn))
                {
                    resultSet.ReaderResult.RecordSet.GetColumnMetaData()[QueryKeyWords.ValueColumn].IsHidden = false;
                }

                resultSet.ReaderResult.ReaderID = RSManager.RegisterReader(clientId, resultSet.ReaderResult);
                resultSet.ReaderResult = RSManager.GetRecordSet(resultSet.ReaderResult.ReaderID, 0, false,operationContext);//first chunk
            }
            //return result;

            
            IDictionary tmp = GetEntries(keys, operationContext);
            IDictionaryEnumerator ide = tmp.GetEnumerator();

            CompressedValueEntry cmpEntry = null;

            while (ide.MoveNext())
            {
                CacheEntry entry = ide.Value as CacheEntry;
                if (entry != null)
                {
                    cmpEntry = new CompressedValueEntry();
                    cmpEntry.Value = entry.Value;
                    if (cmpEntry.Value is CallbackEntry)
                        cmpEntry.Value = ((CallbackEntry)cmpEntry.Value).Value;

                    cmpEntry.Flag = ((CacheEntry)ide.Value).Flag;
                    result[ide.Key] = cmpEntry;
                }
            }

            resultSet.Type = QueryType.SearchEntries;
            resultSet.SearchEntriesResult = result;

            _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();
            if (resultSet != null)
            {
                long totalRowReturn = 0;
                if (resultSet.GroupByResult != null)
                    totalRowReturn = resultSet.GroupByResult.RowCount;

                else if (resultSet.SearchEntriesResult != null)
                    totalRowReturn = resultSet.SearchEntriesResult.Count;

                else if (resultSet.SearchKeysResult != null)
                    totalRowReturn = resultSet.SearchKeysResult.Count;

                _context.PerfStatsColl.IncrementAvgQuerySize(totalRowReturn);


            }
            _context.PerfStatsColl.IncrementQueryPerSec();

            
            return resultSet.ReaderResult;
            //return base.Local_ExecuteReaderCQ(queryId, operationContext);
        }

        public sealed override ReaderResultSet Local_ExecuteReaderCQ(Queries.ContinuousQuery query, bool getData, int chunkSize, OperationContext operationContext)
        {
            try
            {
                Reduction currentQueryReduction = null;
                _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();
                currentQueryReduction = GetPreparedReduction(query.CommandText);
                QueryContext queryContext = ExecuteReaderCQInternal(currentQueryReduction.Tag as Predicate, MiscUtil.DeepClone(query.AttributeValues));
                ClusteredArrayList resultList = queryContext.InternalQueryResult.GetArrayList();
                ReaderResultSet result;
                switch (queryContext.ResultSet.Type)
                {
                    case Queries.QueryType.GroupByAggregateFunction:
                    case Queries.QueryType.OrderByQuery:
                        result = queryContext.ResultSet.ReaderResult;
                        break;
                    case Queries.QueryType.AggregateFunction:
                        result = new ReaderResultSet();
                        if (queryContext.ResultSet.AggregateFunctionResult.Value != null)
                        {
                            result.RecordSet = new RecordSet();
                            RecordColumn aggColumn = new RecordColumn(queryContext.ResultSet.AggregateFunctionResult.Key.ToString());
                            aggColumn.AggregateFunctionType = queryContext.ResultSet.AggregateFunctionType;
                            aggColumn.ColumnType = ColumnType.AggregateResultColumn;
                            aggColumn.DataType = RecordSet.ToColumnDataType(queryContext.ResultSet.AggregateFunctionResult.Value);
                            aggColumn.IsFilled = true;
                            aggColumn.IsHidden = false;
                            result.RecordSet.AddColumn(aggColumn);

                            RecordRow aggRow = result.RecordSet.CreateRow();
                            aggRow[0] = queryContext.ResultSet.AggregateFunctionResult.Value;
                            result.RecordSet.AddRow(aggRow);

                            result.IsGrouped = true;
                        }
                        break;
                    default:
                        //queryContext.Tree.Reduce();

                        result = new ReaderResultSet();
                        if (queryContext.InternalQueryResult.Count > 0)
                        {
                            result.RecordSet = new RecordSet();

                            RecordColumn keyColumn = new RecordColumn(QueryKeyWords.KeyColumn);
                            keyColumn.AggregateFunctionType = AggregateFunctionType.NOTAPPLICABLE;
                            keyColumn.ColumnType = ColumnType.KeyColumn;
                            keyColumn.DataType = ColumnDataType.String;
                            keyColumn.IsFilled = true;
                            keyColumn.IsHidden = false;

                            RecordColumn valueColumn = new RecordColumn(QueryKeyWords.ValueColumn);
                            valueColumn.AggregateFunctionType = AggregateFunctionType.NOTAPPLICABLE;
                            valueColumn.ColumnType = ColumnType.ValueColumn;
                            valueColumn.DataType = ColumnDataType.CompressedValueEntry;
                            valueColumn.IsFilled = false;
                            valueColumn.IsHidden = true;

                            result.RecordSet.AddColumn(keyColumn);
                            result.RecordSet.AddColumn(valueColumn);

                            foreach (string key in queryContext.InternalQueryResult)
                            {
                                RecordRow row = result.RecordSet.CreateRow();
                                row[QueryKeyWords.KeyColumn] = key;
                                result.RecordSet.AddRow(row);
                            }
                        }
                        break;
                }

                _activeQueryAnalyzer.RegisterPredicate(queryContext.TypeName, query.CommandText, MiscUtil.DeepClone(query.AttributeValues), currentQueryReduction.Tag as Predicate, query.UniqueId, resultList);

                _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();
                if (queryContext.ResultSet != null)
                {

                    long totalRowReturn = 0;
                    if (queryContext.ResultSet.GroupByResult != null)
                        totalRowReturn = queryContext.ResultSet.GroupByResult.RowCount;

                    else if (queryContext.ResultSet.SearchEntriesResult != null)
                        totalRowReturn = queryContext.ResultSet.SearchEntriesResult.Count;

                    else if (queryContext.ResultSet.SearchKeysResult != null)
                        totalRowReturn = queryContext.ResultSet.SearchKeysResult.Count;

                    _context.PerfStatsColl.IncrementAvgQuerySize(totalRowReturn);
                }
                _context.PerfStatsColl.IncrementQueryPerSec();

                string clientId = operationContext.GetValueByField(OperationContextFieldName.ClientId) != null ? operationContext.GetValueByField(OperationContextFieldName.ClientId).ToString() : null;
           
                if (RSManager != null)
                {
                    if (chunkSize != -1)
                        result.ChunkSize = chunkSize;
                    else
                        result.ChunkSize = _dataChunkSize;
                    if (result.RecordSet != null)
                    {
                        result.RecordSet.SubsetInfo = new Common.DataReader.SubsetInfo();
                        result.RecordSet.SubsetInfo.LastAccessedRowID = 0;
                        result.RecordSet.SubsetInfo.StartIndex = 0;
                    }

                    result.GetData = getData;
                    if (getData)
                    {
                        if (result.RecordSet != null && result.RecordSet.GetColumnMetaData().Contains(QueryKeyWords.ValueColumn))
                        {
                            result.RecordSet.GetColumnMetaData()[QueryKeyWords.ValueColumn].IsHidden = false;
                        }
                    }
                    if (result.RecordSet != null)
                    {
                        result.ReaderID = RSManager.RegisterReader(clientId, result);
                        result = RSManager.GetRecordSet(result.ReaderID, 0, false,operationContext);//first chunk
                    }
                }
                return result;
                //return result;
            }
            catch (Parser.ParserException pe)
            {
                RemoveReduction(query.CommandText);
                //throw new OperationFailedException(pe.Message, pe);
                throw new Runtime.Exceptions.ParserException(pe.Message, pe);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public sealed override ReaderResultSet GetReaderChunk(string readerId, int nextIndex, bool isInproc, OperationContext operationContext)
        {
            ReaderResultSet result = null;
            if (RSManager != null)
                result = RSManager.GetRecordSet(readerId, nextIndex, isInproc,operationContext);
            return result;
        }

        private ReaderResultSet ExecuteReaderInternal(string query, IDictionary values, OperationContext operationContext)
        {
            QueryContext queryContext = PrepareSearch(query, values, false,true);
            ReaderResultSet result = null;
            switch (queryContext.ResultSet.Type)
            {
                case Queries.QueryType.GroupByAggregateFunction:
                case Queries.QueryType.OrderByQuery:
                    result = queryContext.ResultSet.ReaderResult;
                    break;
                case Queries.QueryType.AggregateFunction:
                    result = new ReaderResultSet();
                    if (queryContext.ResultSet.AggregateFunctionResult.Value != null)
                    {
                        result.RecordSet = new RecordSet();
                        RecordColumn aggColumn = new RecordColumn(queryContext.ResultSet.AggregateFunctionResult.Key.ToString());
                        aggColumn.AggregateFunctionType = queryContext.ResultSet.AggregateFunctionType;
                        aggColumn.ColumnType = ColumnType.AggregateResultColumn;
                        aggColumn.DataType = RecordSet.ToColumnDataType(queryContext.ResultSet.AggregateFunctionResult.Value);
                        aggColumn.IsFilled = true;
                        aggColumn.IsHidden = false;
                        result.RecordSet.AddColumn(aggColumn);

                        RecordRow aggRow = result.RecordSet.CreateRow();
                        aggRow[0] = queryContext.ResultSet.AggregateFunctionResult.Value;
                        result.RecordSet.AddRow(aggRow);

                        result.IsGrouped = true;
                    }
                    break;
                default:
                    //queryContext.Tree.Reduce();

                    result = new ReaderResultSet();
                    if (queryContext.InternalQueryResult != null && queryContext.InternalQueryResult.Count > 0)
                    {
                        result.RecordSet = new RecordSet();

                        RecordColumn keyColumn = new RecordColumn(QueryKeyWords.KeyColumn);
                        keyColumn.AggregateFunctionType = AggregateFunctionType.NOTAPPLICABLE;
                        keyColumn.ColumnType = ColumnType.KeyColumn;
                        keyColumn.DataType = ColumnDataType.String;
                        keyColumn.IsFilled = true;
                        keyColumn.IsHidden = false;

                        RecordColumn valueColumn = new RecordColumn(QueryKeyWords.ValueColumn);
                        valueColumn.AggregateFunctionType = AggregateFunctionType.NOTAPPLICABLE;
                        valueColumn.ColumnType = ColumnType.ValueColumn;
                        valueColumn.DataType = ColumnDataType.CompressedValueEntry;
                        valueColumn.IsFilled = false;
                        valueColumn.IsHidden = true;

                        result.RecordSet.AddColumn(keyColumn);
                        result.RecordSet.AddColumn(valueColumn);
                        
                        foreach (string key in queryContext.InternalQueryResult)
                        {
                            RecordRow row = result.RecordSet.CreateRow();
                            row[QueryKeyWords.KeyColumn] = key;
                            result.RecordSet.AddRow(row);
                        }
                    }
                    break;
            }

            if (queryContext.ResultSet != null)
            {
                long totalRowReturn = 0;
                if (queryContext.ResultSet.GroupByResult != null)
                    totalRowReturn = queryContext.ResultSet.GroupByResult.RowCount;

                else if (queryContext.ResultSet.SearchEntriesResult != null)
                    totalRowReturn = queryContext.ResultSet.SearchEntriesResult.Count;

                else if (queryContext.ResultSet.SearchKeysResult != null)
                    totalRowReturn = queryContext.ResultSet.SearchKeysResult.Count;

                else if (queryContext.InternalQueryResult!=null)
                    totalRowReturn = queryContext.InternalQueryResult.Count;

                _context.PerfStatsColl.IncrementAvgQuerySize(totalRowReturn);
            }

            return result;
        }



        internal virtual QueryContext ExecuteReaderCQInternal(Predicate pred, IDictionary values)
        {
            QueryContext queryContext = new QueryContext(this);
            queryContext.AttributeValues = values;
            queryContext.CacheContext = _context.CacheRoot.Name;

            try
            {
                pred.Execute(queryContext, null);
                return queryContext;
            }
            catch (Exception)
            {
                throw;
                //return null;
            }
        }


        public sealed override void DisposeReader(string readerId, OperationContext context)
        {
            if (RSManager != null)
            {
                RSManager.DisposeReader(readerId);
            }
        }
        public sealed override void DeclaredDeadClients(ArrayList clients)
        {
           
        }


        public override void DeclareDeadClients(string deadClient, ClientInfo info)
        {
            if (_taskManager != null)
                _taskManager.DeadClient(deadClient);
            if (RSManager != null)
            {
                ArrayList clients = new ArrayList();
                clients.Add(deadClient);
                RSManager.DeadClients(clients);
            }
        }


        public sealed override DeleteQueryResultSet DeleteQuery(string query, IDictionary values, bool notify, bool isUserOperation, ItemRemoveReason ir, OperationContext operationContext)
        {
            Hashtable result = new Hashtable();
            ClusteredArrayList keysToBeRemoved;
            
            try
            {
                _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();
                QueryContext queryContext = PrepareDeleteQuery(query, values);
                keysToBeRemoved = queryContext.InternalQueryResult.GetArrayList();
                result = Remove(keysToBeRemoved.ToArray(), ir, notify, isUserOperation, operationContext);

                DeleteQueryResultSet resultSet = new DeleteQueryResultSet();
                resultSet.KeysDependingOnMe = ExtractDependentKeys(result);
                resultSet.KeysEffectedCount = result.Count;
                resultSet.KeysEffected = result;

                if (queryContext.ResultSet != null)
                {
                    long totalRowReturn = 0;
                    if (queryContext.ResultSet.GroupByResult != null)
                        totalRowReturn = queryContext.ResultSet.GroupByResult.RowCount;

                    else if (queryContext.ResultSet.SearchEntriesResult != null)
                        totalRowReturn = queryContext.ResultSet.SearchEntriesResult.Count;

                    else if (queryContext.ResultSet.SearchKeysResult != null)
                        totalRowReturn = queryContext.ResultSet.SearchKeysResult.Count;

                    _context.PerfStatsColl.IncrementAvgQuerySize(totalRowReturn);
                }
                _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();
                _context.PerfStatsColl.IncrementQueryPerSec();

                return resultSet;
            }
            catch (Parser.ParserException pe)
            {
                RemoveReduction(query);
                throw new Runtime.Exceptions.ParserException(pe.Message, pe);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private QueryContext PrepareDeleteQuery(string query, IDictionary values)
        {
            Reduction currentQueryReduction = null;

            try
            {
                currentQueryReduction = GetPreparedReduction(query);

                if (currentQueryReduction.Tokens[0].ToString().ToLower() != "delete")
                    throw new Parser.ParserException("ExecuteNonQuery only supports delete query");

                return DeleteQueryInternal(currentQueryReduction.Tag as Predicate, values);
            }
            catch (Parser.ParserException pe)
            {
                RemoveReduction(query);
                throw new Runtime.Exceptions.ParserException(pe.Message, pe);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public override Hashtable AddDepKeyList(Hashtable table, OperationContext operationContext)
        {
            Hashtable retVal = new Hashtable();
            IDictionaryEnumerator en = table.GetEnumerator();
            while (en.MoveNext())
            {
                operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                CacheEntry oldentry = Get(en.Key, operationContext);
                if (oldentry == null)
                    retVal.Add(en.Key, false);
                else
                {
                    KeyLocker.GetWriterLock(en.Key);
                    try
                    {
                        CacheEntry entry = oldentry.Clone() as CacheEntry;
                        if (entry.KeysDependingOnMe == null)
                            entry.KeysDependingOnMe = new HashVector();

                        ArrayList keys = (ArrayList) en.Value;
                        for (int i = 0; i < keys.Count; i++)
                        {
                            if (!entry.KeysDependingOnMe.Contains(keys[i]))
                                entry.KeysDependingOnMe.Add(keys[i], null);
                        }

                        try
                        {
                            if (InsertInternal(en.Key, entry, false, oldentry, operationContext, false) !=
                                CacheInsResult.SuccessOverwrite)
                            {
                                retVal.Add(en.Key, false);
                            }
                            else
                            {
                                retVal.Add(en.Key, true);
                                _context.ExpiryMgr.UpdateIndex(en.Key, entry);
                            }
                        }
                        catch (Exception e)
                        {
                            retVal.Add(en.Key, e);
                        }
                    }
                    finally
                    {
                        KeyLocker.ReleaseWriterLock(en.Key);
                    }
                }
            }

            operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
            return retVal;
        }

        public override Hashtable RemoveDepKeyList(Hashtable table, OperationContext operationContext)
        {
            Hashtable retVal = new Hashtable();
            if (table == null)
                return null;
            IDictionaryEnumerator en = table.GetEnumerator();
            while (en.MoveNext())
            {
                try
                {
                    CacheEntry pe = GetInternal(en.Key, true, operationContext);
                    if (pe != null)
                    {
                        KeyLocker.GetWriterLock(en.Key);
                        try
                        {
                            if (pe.KeysDependingOnMe != null)
                            {
                                ArrayList list = (ArrayList)en.Value;
                                for (int i = 0; i < list.Count; i++)
                                {
                                    pe.KeysDependingOnMe.Remove(list[i]);
                                }
                            }

                            CacheInsResult res = InsertInternal(en.Key, pe, false, pe, operationContext, false);
                            if (res != CacheInsResult.SuccessOverwrite)
                            {
                                retVal.Add(en.Key, false);
                                continue;
                            }
                            retVal.Add(en.Key, true);
                            _context.ExpiryMgr.UpdateIndex(en.Key, pe);
                        }
                        finally
                        {
                            KeyLocker.ReleaseWriterLock(en.Key);
                        }
                    }
                    else
                    {
                        retVal.Add(en.Key, false);
                    }
                }
                catch (Exception e)
                {
                    retVal.Add(en.Key, e);
                }

                if (_context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.SetCacheSize(Size);

                    if (_context.ExpiryMgr != null)
                        _context.PerfStatsColl.SetExpirationIndexSize(_context.ExpiryMgr.IndexInMemorySize);
                }
            }
            return retVal;
        }

        #endregion

        #region	/                 --- Bulk operations ---           /

        /// <summary>
        /// Determines whether the cache contains the given keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>list of keys found in the cache</returns>
        public sealed override Hashtable Contains(object[] keys, OperationContext operationContext)
        {
            Hashtable tbl = new Hashtable();
            ArrayList successfulKeys = new ArrayList();
            ArrayList failedKeys = new ArrayList();

            for (int i = 0; i < keys.Length; i++)
            {
                KeyLocker.GetReaderLock(keys[i]);
                try
                {
                    bool result = Contains(keys[i], operationContext);
                    if (result)
                    {
                        successfulKeys.Add(keys[i]);
                    }
                }

                catch (StateTransferException se)
                {
                    failedKeys.Add(keys[i]);
                }
                finally
                {

                    KeyLocker.ReleaseReaderLock(keys[i]);
    
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
            CacheEntry e = null;
            for (int i = 0; i < keys.Length; i++)
            {
                KeyLocker.GetReaderLock(keys[i]);
                try
                {
                    if (operationContext != null)
                    {
                        operationContext.RemoveValueByField(OperationContextFieldName.EventContext);
                        OperationID opId = operationContext.OperatoinID;
                        //generate EventId
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
                    KeyLocker.ReleaseReaderLock(keys[i]);
                }
            }
            return entries;
        }
        //private  GenerateEvent(){}

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
                String key = ieKeys.Current as string;
                KeyLocker.GetReaderLock(key);
                try
                {
                    e = GetEntryInternal(key, true);

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
                                    Remove(key, reason, true, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
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
                                    RemoveInternal(key, ItemRemoveReason.Removed, false, operationContext);
                                    throw ex;
                                }
                            }
                        }
                        if (e != null)
                            entries[key] = e;
                    }
                }
                catch (Exception ex)
                {
                    entries[key] = ex;
                }
                finally
                {
                    KeyLocker.ReleaseReaderLock(key);
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
            EventContext eventContext = null;
            EventId eventId = null;
            OperationID opId = operationContext.OperatoinID;
            IList<CacheSyncDependency> syncDependencies = null;
            ArrayList syncDependencyKeys = null;

            operationContext.Add(OperationContextFieldName.DonotRegisterSyncDependency, null);

            for (int i = 0; i < keys.Length; i++)
            {
                try
                {
                    KeyLocker.GetWriterLock(keys[i]);

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

                    if (cacheEntries[i].SyncDependency != null)
                    {
                        if (result != null && result == CacheAddResult.Success)
                        {
                            if (syncDependencies == null)
                                syncDependencies = new List<CacheSyncDependency>();

                            syncDependencies.Add(cacheEntries[i].SyncDependency);

                            if (syncDependencyKeys == null)
                                syncDependencyKeys = new ArrayList();
                            syncDependencyKeys.Add(keys[i]);
                        }
                    }
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
                   KeyLocker.ReleaseWriterLock(keys[i]);
                    operationContext.RemoveValueByField(OperationContextFieldName.EventContext);
                }
            }
            operationContext.RemoveValueByField(OperationContextFieldName.DonotRegisterSyncDependency);
            //AddWriteBehindTask(keys, cacheEntries, table, null, OpCode.Add);
            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }

            if (_context.SyncManager != null && syncDependencyKeys != null && syncDependencyKeys.Count > 0)
            {
                _context.SyncManager.AddBulkDependencies(syncDependencyKeys, syncDependencies);
            }

            return table;
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
            EventContext eventContext = null;
            EventId eventId = null;
            OperationID opId = operationContext.OperatoinID;
            IList<CacheSyncDependency> syncDependencies = null;
            ArrayList syncDependencyKeys = null;
            operationContext.Add(OperationContextFieldName.DonotRegisterSyncDependency, null);

            for (int i = 0; i < keys.Length; i++)
            {
                KeyLocker.GetWriterLock(keys[i]);
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
                    CacheInsResultWithEntry result = Insert(keys[i], cacheEntries[i], notify, null, 0, LockAccessType.IGNORE_LOCK, operationContext);

                    if (cacheEntries[i].SyncDependency != null)
                    {
                        if (result != null && result.Result == CacheInsResult.Success || result.Result == CacheInsResult.SuccessOverwrite)
                        {
                            if (syncDependencies == null)
                                syncDependencies = new List<CacheSyncDependency>();

                            syncDependencies.Add(cacheEntries[i].SyncDependency);

                            if (syncDependencyKeys == null)
                                syncDependencyKeys = new ArrayList();
                            syncDependencyKeys.Add(keys[i]);
                        }
                    }

                    table.Add(keys[i], result);
                }
                catch (Exception e)
                {
                    table[keys[i]] = e;
                }
                finally
                {
                    KeyLocker.ReleaseWriterLock(keys[i]);
                }
            }

            operationContext.RemoveValueByField(OperationContextFieldName.DonotRegisterSyncDependency);

            if (syncDependencyKeys != null && syncDependencyKeys.Count > 0)
            {
                if(_context.SyncManager!=null)
                _context.SyncManager.AddBulkDependencies(syncDependencyKeys, syncDependencies);
            }

            //AddWriteBehindTask(keys, cacheEntries, table, null, OpCode.Update);
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
            EventContext eventContext = null;
            EventId eventId = null;
            OperationID opId = operationContext.OperatoinID;
            IList<CacheSyncDependency> syncDependencies = null;
            ArrayList syncDependencyKeys = null;
            operationContext.Add(OperationContextFieldName.DonotRegisterSyncDependency, null);

            for (int i = 0; i < keys.Count; i++)
            {
                KeyLocker.GetWriterLock(keys[i]);
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

                        if (e.SyncDependency != null)
                        {
                            if (syncDependencies == null)
                                syncDependencies = new List<CacheSyncDependency>();

                            syncDependencies.Add(e.SyncDependency);

                            if (syncDependencyKeys == null)
                                syncDependencyKeys = new ArrayList();
                            syncDependencyKeys.Add(keys[i]);
                        }
                    }
                }
                catch (StateTransferException e)
                {
                    table[keys[i]] = e;
                }
                finally
                {                    
                    KeyLocker.ReleaseWriterLock(keys[i]); 
                }
            }

            operationContext.RemoveValueByField(OperationContextFieldName.EventContext);

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }

            if (syncDependencyKeys != null && syncDependencyKeys.Count > 0)
            {
                if(_context.SyncManager != null )
                    _context.SyncManager.RemoveBulkDependencies(syncDependencyKeys, syncDependencies);

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
        /// <returns>cache entry.</returns>
        internal virtual CacheEntry GetInternal(object key, bool isUserOperation, OperationContext operationContext)
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
        /// <param name="eh"></param>
        /// <returns></returns>
        internal virtual bool AddInternal(object key, CacheSynchronization.CacheSyncDependency syncDependency)
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
        internal virtual CacheEntry RemoveInternal(object key, ItemRemoveReason removalReason, bool isUserOperation, OperationContext operationContext)
        {
            return null;
        }


        internal virtual QueryContext SearchInternal(Predicate pred, IDictionary values,Boolean includeFilters=false)
        {
            return null;
        }

        internal virtual IDictionary SearchEntriesInternal(Predicate pred, IDictionary values)
        {
            return null;
        }

        internal virtual QueryContext DeleteQueryInternal(Predicate pred, IDictionary values)
        {
            return null;
        }

        internal virtual CacheEntry GetEntryInternal(object key, bool isUserOperation)
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
            if (keys != null)
            {
                foreach (string key in keys)
                {
                    KeyLocker.GetWriterLock(key);
                    try
                    {
                        RegisterKeyNotification(key, updateCallback, removeCallback, operationContext);
                    }
                    finally
                    {
                        KeyLocker.ReleaseWriterLock(key);
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
            // operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
            CacheEntry entry = Get(key, operationContext);
            
            if (entry != null)
            {
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

                }

                if (result == StoreInsResult.NotEnoughSpace || result == StoreInsResult.SuccessNearEviction
                     || result == StoreInsResult.SuccessOverwriteNearEviction)
                {
                    
                        Evict();
                    
                }

                if (_context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.SetCacheSize(Size);
                }
            }
        }

        public override void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            if (keys != null)
            {
                foreach (string key in keys)
                {
                    KeyLocker.GetWriterLock(key);
                    try
                    {
                        UnregisterKeyNotification(key, updateCallback, removeCallback, operationContext);
                    }
                    finally
                    {
                        KeyLocker.ReleaseWriterLock(key);
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
            try
            {

                CacheEntry entry = Get(key, operationContext);
                if (entry != null)
                {

                    long oldSize = entry.InMemorySize;
                    entry.RemoveCallbackInfo(updateCallback, removeCallback);
                    long newSize = entry.InMemorySize;

                    string clientId = null;

                    if (updateCallback != null)
                        clientId = updateCallback.Client;
                    else if (removeCallback != null)
                        clientId = removeCallback.Client;

                  
                    _cacheStore.Insert(key, entry, false);
                    _cacheStore.ChangeCacheSize(newSize - oldSize);

                    if (_context.PerfStatsColl != null)
                    {
                        _context.PerfStatsColl.SetCacheSize(Size);
                    }
                }
            }
            catch (StateTransferException)
            {
                //ignore state transfer expcetion
            }
        }

        #endregion

        #region	/            --- PreparedQueryTable operations ---           /

        /// <summary>
        /// Matches the query string in the PreparedQueryTable. If the query does not already exist
        /// in the PreparedQueryTable then it is parsed and the result is stored in the PreparedQueryTable and 
        /// return.
        /// Throws ArgumentException of the query is not successfully parsed.
        /// </summary>
        /// <param name="query">The query string.</param>
        /// <returns>The Reduction for the query specified.</returns>
        private Reduction GetPreparedReduction(string query)
        {
             Reduction reduction = null;
            lock (_preparedQueryTable.SyncRoot)
            {
                QueryIdentifier identifier = new QueryIdentifier(query);
                if (!_preparedQueryTable.ContainsKey(identifier))
                {
                    ParserHelper parser = new ParserHelper(InternalCache.NCacheLog);
                    if (parser.Parse(query) == ParseMessage.Accept)
                    {
                        reduction = parser.CurrentReduction;
                        try
                        {
                            AddPreparedReduction(query, reduction);
                        }
                        catch(Exception e)
                        {
                            bool log = _lastErrorTime == null;

                            if (_lastErrorTime != null && _lastErrorTime.HasValue)
                            {
                                TimeSpan diff = DateTime.Now - _lastErrorTime.Value;

                                if (diff != null && diff.TotalMinutes >= 60)
                                {
                                    log = true;
                                }
                            }
                            
                            if(log)
                            {
                                _lastErrorTime = DateTime.Now;
                                if (_context.NCacheLog != null && _context.NCacheLog.IsErrorEnabled)
                                    _context.NCacheLog.Error("LocalCache.GetPreparedReduction", "An error occured while caching parsed query. " + e.ToString());
                            }
                        }
                    }
                    else
                    {
                        throw new Parser.ParserException("Incorrect query format");
                    }
                }
               
                else
                    reduction = (Reduction)_preparedQueryTable[identifier];
            }
            return reduction;
        }


        private void RemoveReduction(string query)
        {
            lock (_preparedQueryTable.SyncRoot)
            {
                _preparedQueryTable.Remove(query);
            }
        }

        /// <summary>
        /// Adds the query to the PreparedQueryTable. This method also does the eviction 
        /// if PreparedQueryTable count is greater than eviction ratio.
        /// This method is only called from GetPreparedReduction(string).
        /// </summary>
        /// <param name="query">The query to add in the table.</param>
        /// <param name="currentReduction">The successful query result in the form of Reduction.</param>
        private void AddPreparedReduction(string query, Reduction currentReduction)
        {
            _preparedQueryTable.Add(new QueryIdentifier(query), currentReduction);
            if (_preparedQueryTable.Count > _preparedQueryTableSize)
            {
                ArrayList list = new ArrayList(_preparedQueryTable.Keys);
                list.Sort();
                int evictCount = (_preparedQueryTable.Count * _preparedQueryEvictionPercentage) / 100;
                for (int i = 0; i < evictCount; i++)
                    _preparedQueryTable.Remove(list[i]);
            }
        }

        #endregion

        #region /               --- Stream Operations ---                   /

        public override bool OpenStream(string key, string lockHandle, Alachisoft.NCache.Common.Enum.StreamModes mode, string group, string subGroup, ExpirationHint hint, EvictionHint evictinHint, OperationContext operationContext)
        {
            bool lockAcquired = false;
            CacheEntry entry = GetInternal(key, false, operationContext);


            if (entry != null)
            {

                if (!Util.CacheHelper.CheckDataGroupsCompatibility(new GroupInfo(group, subGroup), entry.GroupInfo))
                {
                    throw new OperationFailedException("Data group of the stream does not match the existing stream's data group");
                }

            }



            switch (mode)
            {
                case StreamModes.Read:
                    if (entry == null) throw new StreamNotFoundException();
                    lockAcquired = entry.RWLockManager.AcquireReaderLock(lockHandle);
                    break;

                case StreamModes.ReadWithoutLock:
                    if (entry == null) throw new StreamNotFoundException();
                    lockAcquired = true;//Just to signal.
                    break;

                case StreamModes.Write:
                    if (entry == null)
                    {
                        UserBinaryObject userBinaryObject = LargeUserBinaryObject.CreateUserBinaryObject(new byte[0]);
                        entry = new CacheEntry(userBinaryObject, hint, evictinHint);
                       
                        entry.GroupInfo = new Alachisoft.NCache.Caching.DataGrouping.GroupInfo(group, subGroup);
                        lockAcquired = entry.RWLockManager.AcquireWriterLock(lockHandle);
                        if (lockAcquired)
                            Insert(key, entry, true, true, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                        return lockAcquired;
                    }

                    lockAcquired = entry.RWLockManager.AcquireWriterLock(lockHandle);
                    break;
            }


            if (!lockAcquired)
                throw new StreamAlreadyLockedException();

            return lockAcquired;
        }

        public override void CloseStream(string key, string lockHandle, OperationContext operationContext)
        {

            CacheEntry entry = GetInternal(key, false, operationContext);

            if (entry != null)
            { 

                LockMode mode = entry.RWLockManager.Mode;
                switch (mode)
                {
                    case LockMode.Reader:
                        if (lockHandle != null && !entry.RWLockManager.ValidateLock(LockMode.Reader, lockHandle))
                            throw new StreamInvalidLockException();

                        entry.RWLockManager.ReleaseReaderLock(lockHandle);
                        break;

                    case LockMode.Write:
                        if (lockHandle != null && !entry.RWLockManager.ValidateLock(LockMode.Write, lockHandle))
                            throw new StreamInvalidLockException();

                        entry.RWLockManager.ReleaseWriterLock(lockHandle);
                        break;
                }
            }
        }

        public override int ReadFromStream(ref VirtualArray vBuffer, string key, string lockHandle, int offset, int length, OperationContext operationContext)
        {
            CacheEntry entry = Get(key, true, operationContext);

            if (entry != null)
            {
                if (!string.IsNullOrEmpty(lockHandle) && !entry.RWLockManager.ValidateLock(LockMode.Reader, lockHandle))
                    throw new StreamInvalidLockException();

                vBuffer = entry.Read(offset, length);
            }
            else
                throw new StreamNotFoundException();

            return (int)vBuffer.Size;
        }

        public override void WriteToStream(string key, string lockHandle, Alachisoft.NCache.Common.DataStructures.VirtualArray vBuffer, int srcOffset, int dstOffset, int length, OperationContext operationContext)
        {
            CacheEntry entry = GetInternal(key, false, operationContext);

            if (entry != null)
            {
                if (lockHandle == null || !entry.RWLockManager.ValidateLock(LockMode.Write, lockHandle))
                    throw new StreamInvalidLockException();
                //We clone the existing entry before removing so that hints are not disposed.
                CacheEntry cloneEntry = entry.Clone() as CacheEntry;
                //Remove the previous item from the store so that store can adjust its size
                Remove(key, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);

                cloneEntry.Write(vBuffer, srcOffset, dstOffset, length);
                CacheInsResultWithEntry result = Insert(key, cloneEntry, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                if (result != null && result.Result == CacheInsResult.NeedsEviction)
                    throw new CacheException("The cache is full and not enough items could be evicted.");

            }
            else
                throw new StreamNotFoundException();
        }

        public override long GetStreamLength(string key, string lockHandle, OperationContext operationContext)
        {
            CacheEntry entry = GetInternal(key, false, operationContext);

            if (entry != null)
            {
                if (!string.IsNullOrEmpty(lockHandle) && !entry.RWLockManager.ValidateLock(lockHandle))
                    throw new StreamInvalidLockException();

                return entry.Length;
            }
            else
                throw new StreamNotFoundException();

            return 0;

        }


        #endregion

        internal override ContinuousQueryStateInfo GetContinuousQueryStateInfo()
        {
            ContinuousQueryStateInfo result = null;

            if (_activeQueryAnalyzer != null)
                result = _activeQueryAnalyzer.GetStateInfo();

            return result;
        }

        internal override IList<PredicateHolder> GetContinuousQueryRegisteredPredicates(string type)
        {
            IList<PredicateHolder> result = null;

            if (_activeQueryAnalyzer != null)
                result = _activeQueryAnalyzer.GetPredicatesForType(type);

            return result;
        }

        public sealed override void UnRegisterCQ(string queryId)
        {
            _activeQueryAnalyzer.UnRegisterPredicate(queryId);
        }

        public sealed override void RegisterCQ(ContinuousQuery query, OperationContext operationContext)
        {
            SearchCQ(query, operationContext);
        }

     
        #region MapReduce Methods

        public override object TaskOperationReceived(MapReduceOperation operation)
        {
            if (_taskManager != null)
                return _taskManager.TaskOperationReceived(operation);
            return null;
        }

        public override void SubmitMapReduceTask(Runtime.MapReduce.MapReduceTask task, string taskId, MapReduce.Notifications.TaskCallbackInfo callbackInfo, Filter filter, OperationContext operationContext)
        {
            this.InternalCache.SubmitMapReduceTask(task, taskId, callbackInfo, filter, operationContext);
        }

        public override void RegisterTaskNotification(string taskID, MapReduce.Notifications.TaskCallbackInfo callbackInfo, OperationContext operationContext)
        {
            this.InternalCache.RegisterTaskNotification(taskID, callbackInfo, operationContext);
        }

        public override void UnregisterTaskNotification(string taskID, MapReduce.Notifications.TaskCallbackInfo callbackInfo, OperationContext operationContext)
        {
            this.InternalCache.UnregisterTaskNotification(taskID, callbackInfo, operationContext);
        }

        public override void CancelMapReduceTask(string taskId, bool cancelAll)
        {
            this.InternalCache.CancelMapReduceTask(taskId, cancelAll);
        }

        #endregion

        /// <summary>
        /// Determines whether the cache contains the given keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>list of keys found in the cache</returns>
        internal override void Touch(List<string> keys, OperationContext operationContext)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                KeyLocker.GetReaderLock(keys[i]);
                try
                {
                    TouchInternal(keys[i], operationContext);
                }
                catch (StateTransferException se)
                {
                }
                finally
                {
                    KeyLocker.ReleaseReaderLock(keys[i]);
                }
            }
        }

        internal virtual void TouchInternal(string key, OperationContext operationContext)
        {
        }

       
    }
}
