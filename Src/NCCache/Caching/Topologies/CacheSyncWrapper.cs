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
using System.Collections.Generic;
using System.Threading;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Caching.Statistics;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
#endif
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Resources;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Locking;

using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;
namespace Alachisoft.NCache.Caching.Topologies
{
    /// <summary>
    /// Synchronized wrapper over cache. Provides internal as well as external
    /// thread safety.
    /// </summary>
    internal class CacheSyncWrapper : CacheBase
    {
        /// <summary> The en-wrapped instance of cache. </summary>
        private CacheBase _cache;
        

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CacheSyncWrapper(CacheBase cache)
        {
            if (cache == null)
                throw new ArgumentNullException("cache");
            _cache = cache;
            _context = cache.Context;
            _syncObj = _cache.Sync;
            _keyLockManager = cache.KeyLocker;
        }
        
        #region	IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (_cache != null)
            {
                _cache.Dispose();
                _cache = null;
            }
            base.Dispose();
        }

        #endregion

        /// <summary>
        /// get/set listener of Cache events. 'null' implies no listener.
        /// </summary>
        public CacheBase Internal
        {
            get { return _cache; }
        }

        /// <summary>
        /// Returns true if cache is started as backup mirror cache, false otherwise.
        /// In Case of replica space will not be checked at storage level
        /// </summary>
        /// 
        public override bool VirtualUnlimitedSpace
        {
            get
            {
                return _cache.VirtualUnlimitedSpace;
            }

            set
            {
                Sync.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    _cache.VirtualUnlimitedSpace = value;
                }
                finally
                {
                    Sync.ReleaseWriterLock();
                }
            }
        }

        /// <summary> 
        /// Returns the cache local to the node, i.e., internal cache.
        /// </summary>
        protected internal override CacheBase InternalCache
        {
            get { return _cache.InternalCache; }
        }

        public override TypeInfoMap TypeInfoMap
        {
            get
            {
                return _cache.TypeInfoMap;
            }
        }

        /// <summary>
        /// get/set the name of the cache.
        /// </summary>
        public override string Name
        {
            get { return Internal.Name; }
            set { Internal.Name = value; }
        }

        /// <summary>
        /// get/set listener of Cache events. 'null' implies no listener.
        /// </summary>
        public override ICacheEventsListener Listener
        {
            get { return Internal.Listener; }
            set { Internal.Listener = value; }
        }

        public override int GetItemSize(object key)
        {
            return _cache.GetItemSize(key);
        }

        /// <summary>
        /// Notifications are enabled.
        /// </summary>
        public override Notifications Notifiers
        {
            get { return Internal.Notifiers; }
            set { Internal.Notifiers = value; }
        }

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        public override long Count
        {
            get
            {
                return Internal.Count;
            }
        }

        internal override long Size
        {
            get
            {
                return Internal.Size;
            }
        }

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        public override long SessionCount
        {
            get
            {
                return Internal.SessionCount;
            }
        }

        /// <summary>
        /// returns the statistics of the Clustered Cache. 
        /// </summary>
        internal override CacheStatistics ActualStats
        {
            get
            {
                return Internal.ActualStats;
            }
        }

        /// <summary>
        /// returns the statistics of the Clustered Cache. 
        /// </summary>
        public override CacheStatistics Statistics
        {
            get
            {
                return Internal.Statistics;
            }
        }
        

      

        public override Array Keys
        {
            get
            {
                return Internal.Keys;
            }
        }      

        #region	/                 --- ICache ---                                 /

        internal override float EvictRatio
        {
            get
            {
                return Internal.EvictRatio;
            }
            set
            {
                Sync.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    Internal.EvictRatio = value;
                }
                finally
                {
                    Sync.ReleaseWriterLock();
                }
            }
        }


        public override IDictionary GetEntryAttributeValues(object key, IList<string> columns, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetEntryAttributeValues", "enter");

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetReaderLock(lockKey);

            try
            {
                return Internal.GetEntryAttributeValues(key, columns, operationContext);
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(lockKey);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetEntryAttributeValues", "exit");
            }
        }

        internal override bool CanChangeCacheSize(long size)
        {
            return Internal.CanChangeCacheSize(size);
        }

        internal override long MaxSize
        {
            get
            {
                return Internal.MaxSize;
            }
            set
            {
                Internal.MaxSize = value;
            }
        }

        public override void SetStateTransferKeyList(Hashtable keylist)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _cache.SetStateTransferKeyList(keylist);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public override void UnSetStateTransferKeyList()
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _cache.UnSetStateTransferKeyList();
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public override void Clear(Caching.Notifications notification, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
        {
            Clear(notification, updateOptions, null, operationContext);
        }

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public override void Clear(Caching.Notifications notification, DataSourceUpdateOptions updateOptions, string taskId, OperationContext operationContext)
        {

            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                Internal.Clear(notification, updateOptions, operationContext);
#if SERVER
                if (_context.CacheImpl.RequiresReplication)
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.Clear, new object[] { notification, taskId, operationContext });
#endif
            }
            finally
            {
                Sync.ReleaseWriterLock();

            }
        }

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element 
        /// with the specified key; otherwise, false.</returns>
        public override bool Contains(object key, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Cont", "enter");

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetReaderLock(lockKey);

            try
            {
                return Internal.Contains(key, operationContext);
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(lockKey);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Cont", "exit");
            }
        }

        /// <summary>
        /// Determines whether the cache contains the specified keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>list of existing keys.</returns>
        public override Hashtable Contains(IList keys, OperationContext operationContext)
        {
            try
            {
                return Internal.Contains(keys, operationContext);
            }
            finally
            {

            }
        }

        public override LockOptions IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            LockOptions lockInfo = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.IsLock", "enter");

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetReaderLock(lockKey);

            try
            {
                lockInfo = Internal.IsLocked(key, ref lockId, ref lockDate, operationContext);
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(lockKey);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.IsLock", "exit");

            }
            return lockInfo;
        }

        public override LockOptions Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            LockOptions lockInfo = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Lock", "enter");

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetReaderLock(lockKey);

            try
            {
                lockInfo = Internal.Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
#if SERVER
                if (_context.CacheImpl.RequiresReplication)
                {
                    string uniqueKey = System.Guid.NewGuid().ToString() + key;
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UpdateLockInfo, new object[] { true, key, lockId, lockDate, lockExpiration, operationContext });
                }
#endif
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(lockKey);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Lock", "exit");

            }
            return lockInfo;
        }

        public override void UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Unlock", "enter");

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetReaderLock(lockKey);

            try
            {
                Internal.UnLock(key, lockId, isPreemptive, operationContext);
#if SERVER
                if (_context.CacheImpl.RequiresReplication)
                {
                    string uniqueKey = System.Guid.NewGuid().ToString() + key;
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UpdateLockInfo, new object[] { false, key, lockId, DateTime.Now, null, isPreemptive, operationContext });
                }
#endif
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(lockKey);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Unlock", "exit");

            }
        }

        internal override void UpdateLockInfo(object key, object lockId, DateTime lockDate, LockExpiration lockExpiration, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.UpdLock", "enter");

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetReaderLock(lockKey);

            try
            {
                Internal.UpdateLockInfo(key, lockId, lockDate, lockExpiration, operationContext);
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(lockKey);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.UpdLock", "exit");

            }
        }

        public override CacheEntry Get(object key, bool IsUserOperation, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry entry = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Get_2", "enter");

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetReaderLock(lockKey);

            try
            {
                entry = Internal.Get(key, IsUserOperation, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
#if SERVER

                if (accessType == LockAccessType.ACQUIRE && entry != null && _context.CacheImpl.RequiresReplication)
                {
                    string uniqueKey = System.Guid.NewGuid().ToString() + key;
                    operationContext?.MarkInUse(NCModulesConstants.Replication);
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UpdateLockInfo, new object[] { true, key, lockId, lockDate, lockExpiration, operationContext });
                }
#endif
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(lockKey);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Get_2", "exit");

            }
            return entry;
        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        public override CacheEntry Get(object key, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType access, OperationContext operationContext)
        {
            CacheEntry entry = null;

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetReaderLock(lockKey);

            try
            {
                operationContext?.MarkInUse(NCModulesConstants.CacheSync);

                entry = Internal.Get(key, ref version, ref lockId, ref lockDate, lockExpiration, access, operationContext);
#if SERVER
                if (access == LockAccessType.ACQUIRE && entry != null && _context.CacheImpl.RequiresReplication)
                {
                    operationContext?.MarkInUse(NCModulesConstants.Replication);
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UpdateLockInfo, new object[] { true, key, lockId, lockDate, lockExpiration, operationContext });
                }
#endif
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.CacheSync);

                KeyLocker.ReleaseReaderLock(lockKey);

            }
            return entry;
        }

        /// <summary>
        /// Retrieve the objects from the cache. An array of keys is passed as parameter.
        /// </summary>
        /// <param name="key">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        public override IDictionary  Get(object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetBlk", "enter");
            try
            {
                return Internal.Get(keys, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetBlk", "exit");
            }
        }
        

        public override HashVector GetTagData(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            HashVector tbl = null;
            try
            {
                tbl = Internal.GetTagData(tags, comparisonType, operationContext);
            }
            finally
            {
            }
            return tbl;
        }

        internal override ICollection GetTagKeys(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetTagInternal", "enter");

            try
            {
                return Internal.GetTagKeys(tags, comparisonType, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetTagInternal", "exit");

            }
        }

        /// <summary>
        /// Retrieve the keys from the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>list of keys.</returns>
        public override ArrayList GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetGrpKeys", "enter");
            try
            {
                return Internal.GetGroupKeys(group, subGroup, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetGrpKeys", "exit");
            }
        }

        public override Common.Events.PollingResult Poll(OperationContext operationContext)
        {
            return Internal.Poll(operationContext);
        }

        public virtual void LogBackingSource()
        {
             Internal.LogBackingSource();
        }

        public override void RegisterPollingNotification(short callbackId, OperationContext operationContext)
        {
            Internal.RegisterPollingNotification(callbackId, operationContext);
        }


        /// <summary>
        /// Retrieve the keys from the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>list of keys.</returns>
        public override CacheEntry GetGroup(object key, string group, string subGroup, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry entry = null;

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetReaderLock(lockKey);

            try
            {
                operationContext?.MarkInUse(NCModulesConstants.CacheSync);

                entry = Internal.GetGroup(key, group, subGroup, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
#if SERVER
                if (accessType == LockAccessType.ACQUIRE && entry != null && _context.CacheImpl.RequiresReplication)
                {
                    operationContext?.MarkInUse(NCModulesConstants.Replication);
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UpdateLockInfo, new object[] { true, key, lockId, lockDate, lockExpiration, operationContext });
                }
#endif
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.CacheSync);

                KeyLocker.ReleaseReaderLock(lockKey);


            }
            return entry;
        }

        /// <summary>
        /// Gets the data group information of the item.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override DataGrouping.GroupInfo GetGroupInfo(object key, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetGrpInfo", "enter");

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetReaderLock(lockKey);

            try
            {
                return Internal.GetGroupInfo(key, operationContext);
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(lockKey);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetGrpInf", "exit");

            }
        }

        /// <summary>
        /// Gets the data groups of the items.
        /// </summary>
        /// <param name="keys">Keys of the items</param>
        /// <returns>Hash-table containing key of the item as 'key' and GroupInfo as 'value'</returns>
        public override Hashtable GetGroupInfoBulk(object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetGrpInfBlk", "enter");
            try
            {
                return Internal.GetGroupInfoBulk(keys, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetGrpInfBlk", "exit");

            }
        }
        /// <summary>
        /// Retrieve the keys from the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>key and value pairs.</returns>
        public override HashVector GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetGrpData", "enter");
            try
            {
                return Internal.GetGroupData(group, subGroup, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetGrpData", "exit");

            }
        }

        /// <summary>
        /// Gets/sets the list of data groups contained in the cache.
        /// </summary>
        public override ArrayList DataGroupList
        {
            get
            {
                return Internal.DataGroupList;
            }
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
        {
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.CacheSync);
                operationContext?.MarkInUse(NCModulesConstants.CacheSync);

                return Add(key, cacheEntry, notify, null, operationContext);
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.CacheSync);
                operationContext?.MarkInUse(NCModulesConstants.CacheSync);

            }
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, string taskId, OperationContext operationContext)
        {
#if SERVER 
            bool requiresReplication = _context.CacheImpl.RequiresReplication;
#endif
            CacheEntry clone = null;

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetWriterLock(lockKey);
            try
            {

                operationContext?.MarkInUse(NCModulesConstants.CacheSync);

                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.CacheSync);
#if SERVER
                
              
                if ( requiresReplication)
                {
                    clone = cacheEntry.DeepClone(Context.FakeObjectPool);
                    if (clone != null)
                        clone.MarkInUse(NCModulesConstants.CacheSync);
                }

#endif

                CacheAddResult result = Internal.Add(key, cacheEntry, notify, operationContext);
#if SERVER 
                if ( requiresReplication)
                {
                    if (result == CacheAddResult.Success || result == CacheAddResult.SuccessNearEviction)
                    {
                        
                        if (requiresReplication)
                        {
                            CacheEntry cloneWithoutvalue; Array userPayLoad; long payLoadSize ;
                            _context.CachingSubSystemDataService.GetEntryClone(clone, out cloneWithoutvalue, out userPayLoad, out payLoadSize);
                            _context.CacheImpl.EnqueueForReplication(cacheEntry.Type != Common.Caching.EntryType.CacheItem?null:key, (int)ClusterCacheBase.OpCodes.Add, new object[] { key, cloneWithoutvalue, taskId, operationContext }, clone.Size, userPayLoad, payLoadSize);

                            #region [Enable When Entries for Bridge are to be Cloned from Transactional Pool]
                         
                            #endregion
                        }
                    }
                    else
                    {
                        #region [Enable When Entries for Bridge are to be Cloned from Transactional Pool]
                        // Bridge or Replication operations are ignored since add failed
                        //if (!ReferenceEquals(cacheEntry, clone))
                        //    MiscUtil.ReturnEntryToPool(clone, Context.TransactionalPoolManager);
                        #endregion
                    }
                }
#endif
                return result;
            }
            finally
            { 
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.CacheSync);
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.CacheSync);
                operationContext?.MarkFree(NCModulesConstants.CacheSync);
                KeyLocker.ReleaseWriterLock(lockKey);
             
            }
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, bool IsUserOperation, OperationContext operationContext)
        {

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_2", "enter");

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetWriterLock(lockKey);
            CacheEntry clone = null;

            try
            {
                
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.CacheSync);
#if SERVER 
                
                if (clone != null)
                    clone.MarkInUse(NCModulesConstants.CacheSync);
#endif
                CacheAddResult result = Internal.Add(key, cacheEntry, notify, IsUserOperation, operationContext);

                return result;
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_2", "exit");
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.CacheSync);
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.CacheSync);
                KeyLocker.ReleaseWriterLock(lockKey);

            }
        }

        /// <summary>
        /// Add ExpirationHint against the given key
        /// Key must already exists in the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public override bool Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
            bool depAdded;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_3", "enter");

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetWriterLock(lockKey);

            try
            {
                
                depAdded = Internal.Add(key, eh, operationContext);
#if SERVER
                if (depAdded)
                {
                    if (_context.CacheImpl.RequiresReplication)
                    {
                        _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.AddHint, new object[] { key, eh, operationContext });
                    }
                }
#endif
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(lockKey);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_3", "exit");

            }
            return depAdded;
        }

        public override bool Add(object key, OperationContext operationContext)
        {
            bool depAdded = false;

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_4", "enter");

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetWriterLock(lockKey);

            try
            {
                depAdded = Internal.Add(key, operationContext);
#if SERVER
                if (depAdded)
                {
                    if (_context.CacheImpl.RequiresReplication)
                    {
                        string uniqueKey = System.Guid.NewGuid().ToString() + key;
                
                        _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.AddSyncDependency, new object[] { key, operationContext });
                    }
                }
#endif
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(lockKey);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_4", "exit");

            }
            return depAdded;
        }

        /// <summary>
        /// Adds key and value pairs to the cache. Throws an exception or returns the
        /// list of keys that already exists in the cache.
        /// </summary>
        /// <param name="keys">key of the entry.</param>
        /// <param name="cacheEntries">the cache entry.</param>
        /// <returns>List of keys that are added or that alredy exists in the cache and their status</returns>
        public override Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.CacheSync);
                    return Add(keys, cacheEntries, notify, null, operationContext);
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.CacheSync);
            }
        }

        /// <summary>
        /// Adds key and value pairs to the cache. Throws an exception or returns the
        /// list of keys that already exists in the cache.
        /// </summary>
        /// <param name="keys">key of the entry.</param>
        /// <param name="cacheEntries">the cache entry.</param>
        /// <returns>List of keys that are added or that alredy exists in the cache and their status</returns>
        public override Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, string taskId, OperationContext operationContext)
        {
            Hashtable result = null;

            CacheEntry[] clone = null;
#if SERVER 
            bool requiresReplication = _context.CacheImpl.RequiresReplication;
#endif
            //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.AddBlk", "enter");
            try
            {
#if SERVER
                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.CacheSync);

               


                if (requiresReplication)
                {
                    clone =  new CacheEntry[cacheEntries.Length];
                    for (int i = 0; i < cacheEntries.Length; i++)
                    {
                        clone[i] = cacheEntries[i].DeepClone(Context.FakeObjectPool);
                        clone[i].MarkInUse(NCModulesConstants.CacheSync);
                    }
                }
#endif
                result = Internal.Add(keys, cacheEntries, notify, operationContext);



#if SERVER
                if (requiresReplication)
                {
                    ArrayList operationList = new ArrayList();
                    ArrayList successfulKeys = new ArrayList();
                    ArrayList successfulEnteries = new ArrayList();

                    if (result != null && result.Count > 0)
                    {
                        for (int i = 0; i < keys.Length; i++)
                        {
                            if (result.Contains(keys[i]))
                            {
                                string key = keys[i] as string;
                                if (result[keys[i]] is CacheAddResult)
                                {
                                    CacheAddResult addResult = (CacheAddResult)result[keys[i]];
                                    if (addResult == CacheAddResult.Success || addResult == CacheAddResult.SuccessNearEviction)
                                    {
                                        if (requiresReplication)
                                        {
                                            successfulKeys.Add(keys[i]);
                                            if (clone != null)
                                                successfulEnteries.Add(clone[i]);

                                        }
                                    }
                                    else
                                    {
                                     
                                    }
                                }
                                else
                                {
                                    
                                }
                            }
                            else
                            {
                              
                            }
                        }
                    }
                    else
                    {
                     
                    }
                    
                    if (successfulKeys.Count > 0)
                    {
                        for (int i = 0; i < successfulKeys.Count; i++)
                        {
                            CacheEntry entry = successfulEnteries[i] as CacheEntry;
                            CacheEntry cloneValue; Array userPayLoad; long payLoadSize;
                            _context.CachingSubSystemDataService.GetEntryClone(entry, out cloneValue, out userPayLoad, out payLoadSize);
                            cloneValue?.MarkInUse(NCModulesConstants.Replication);

                            _context.CacheImpl.EnqueueForReplication(successfulKeys[i], (int)ClusterCacheBase.OpCodes.Add, new object[] { successfulKeys[i], cloneValue, (taskId != null) ? taskId + "-" + i : null, operationContext }, entry.Size, userPayLoad, payLoadSize);

                        }
                    }
                }
#endif
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.CacheSync);

                if (clone != null)
                    clone.MarkFree(NCModulesConstants.CacheSync);
            }
            return result;
        }



        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.CacheSync);

                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.CacheSync);

                return Insert(key, cacheEntry, notify, null, lockId, version, accessType, operationContext);
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.CacheSync);
                operationContext?.MarkFree(NCModulesConstants.CacheSync);

            }
        }

        private static object GenerateObject(object key)
        {
            return new object();
        }

        private static Func<object, object> funcDelegate = GenerateObject;

        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, string taskId, object lockId, ulong version, LockAccessType access, OperationContext operationContext)
        {

            bool replicateToBridge = false;
            bool requiresReplication = false;
            bool isBridgeOperation = false;
#if SERVER
            requiresReplication = _context.CacheImpl.RequiresReplication;
#endif            
            CacheEntry clone = null;
            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetWriterLock(lockKey);
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.CacheSync);
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.CacheSync);

#if SERVER

               
                if (requiresReplication || replicateToBridge)
                {
                        clone = cacheEntry.DeepClone(Context.FakeObjectPool);
                        if (clone != null)
                            clone.MarkInUse(NCModulesConstants.CacheSync);
                }


#endif
                CacheInsResultWithEntry result = Internal.Insert(key, cacheEntry, notify, lockId, version, access, operationContext);

#if SERVER
                if (replicateToBridge || requiresReplication)
                {
                    if (result.Result == CacheInsResult.Success || result.Result == CacheInsResult.SuccessNearEvicition || result.Result == CacheInsResult.SuccessOverwrite || result.Result == CacheInsResult.SuccessOverwriteNearEviction)
                    {
                        if (replicateToBridge && !isBridgeOperation)
                        {
                            if (clone != null)
                            {
                                clone.Version = cacheEntry.Version;
                            }

                        }

                        if (requiresReplication)
                        {
                            CacheEntry cloneValue; Array userPayLoad; long payLoadSize;
                            _context.CachingSubSystemDataService.GetEntryClone(clone, out cloneValue, out userPayLoad, out payLoadSize);
                            cloneValue?.MarkInUse(NCModulesConstants.Replication);
                            if ((result.Result == CacheInsResult.SuccessOverwrite && access == LockAccessType.DONT_RELEASE))
                            {
                                clone.CopyLock(result.Entry.LockId, result.Entry.LockDate, result.Entry.LockExpiration);
                            }
                            if (access == LockAccessType.RELEASE)
                                cloneValue.Flag.UnsetBit(BitSetConstants.LockedItem);

                            _context.CacheImpl.EnqueueForReplication(key, (int)ClusterCacheBase.OpCodes.Insert, new object[] { key, cloneValue, taskId, operationContext }, clone.Size, userPayLoad, payLoadSize);

                         
                        }
                    }
                    else
                    {
                        #region [Enable When Entries for Bridge are to be Cloned from Transactional Pool]
                        // Bridge or replication operations are ignored since insert failed
                        //if (!ReferenceEquals(cacheEntry, clone))
                        //    MiscUtil.ReturnEntryToPool(clone, Context.TransactionalPoolManager);
                        #endregion
                    }
                }
#endif

                return result;
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(lockKey);
                operationContext?.MarkFree(NCModulesConstants.CacheSync);
                
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.CacheSync);
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.CacheSync);
            }
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, bool IsUserOperation, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            bool replicateToBridge = false;
            bool requiresReplication = false;
#if SERVER
           
            requiresReplication = _context.CacheImpl.RequiresReplication;
#endif

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetWriterLock(lockKey);

            CacheEntry clone = null;

            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.CacheSync);
#if SERVER 
                
                if (requiresReplication || replicateToBridge)
                {
                    if (cacheEntry.HasQueryInfo)
                        clone = cacheEntry.Clone() as CacheEntry;
                    else
                        clone = cacheEntry;
                }
                if (clone != null)
                    clone.MarkInUse(NCModulesConstants.CacheSync);
#endif

                CacheInsResultWithEntry result = Internal.Insert(key, cacheEntry, notify, IsUserOperation, lockId, version, accessType, operationContext);
#if SERVER
                if (replicateToBridge && IsUserOperation)
                {
                    if (result.Result == CacheInsResult.Success || result.Result == CacheInsResult.SuccessNearEvicition || result.Result == CacheInsResult.SuccessOverwrite || result.Result == CacheInsResult.SuccessOverwriteNearEviction)
                    {
                        if (clone != null)
                        {
                            clone.Version = cacheEntry.Version;
                        }
                    }

                }
#endif
                return result;
            }
            finally
            {

                KeyLocker.ReleaseWriterLock(lockKey);
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.CacheSync);
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.CacheSync);
              

            }
        }

        public override Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.CacheSync);
                return Insert(keys, cacheEntries, notify, null, operationContext);
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.CacheSync);
            }
        }

        /// <summary>
        /// Adds key and value pairs to the cache. If any of the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">the cache entries.</param>
        /// <returns>returns the results for inserted keys</returns>
        public override Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, string taskId, OperationContext operationContext)
        {
            Hashtable result = null;
            bool requiresReplication = false;
            CacheEntry[] clone = null;
            try {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.CacheSync);
#if SERVER
                requiresReplication = _context.CacheImpl.RequiresReplication;

               
                if ( requiresReplication)
                {
                    clone = new CacheEntry[cacheEntries.Length];
                    for (int i = 0; i < cacheEntries.Length; i++)
                    {
                        clone[i] = cacheEntries[i].DeepClone(Context.FakeObjectPool);
                        clone[i].MarkInUse(NCModulesConstants.CacheSync);
                    }
                }

#endif

                try
                {
                    result = Internal.Insert(keys, cacheEntries, notify, operationContext);
                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);
#if SERVER
                    if ( requiresReplication)
                    {
                        if (result != null && result.Count > 0)
                        {
                            ArrayList operationList = new ArrayList();
                            ArrayList successfulKeys = new ArrayList();
                            ArrayList successfulEnteries = new ArrayList();

                            for (int i = 0; i < keys.Length; i++)
                            {
                                if (result.Contains(keys[i]))
                                {
                                    string key = keys[i] as string;
                                    CacheInsResultWithEntry resultWithEntry = result[keys[i]] as CacheInsResultWithEntry;
                                    if (resultWithEntry != null)
                                    {
                                        CacheInsResult insResult = resultWithEntry.Result;
                                        if (insResult == CacheInsResult.Success || insResult == CacheInsResult.SuccessOverwrite)
                                        {
                                            if (requiresReplication)
                                            {
                                                successfulKeys.Add(keys[i]);
                                                successfulEnteries.Add(clone[i]);
                                            }
                                        }
                                        else
                                        {
                                           
                                        }
                                    }
                                    else
                                    {
                                    
                                    }
                                }
                                else
                                {
                                   
                                }
                            }
                            
                            if (successfulKeys.Count > 0)
                            {
                                for (int i = 0; i < successfulKeys.Count; i++)
                                {
                                    CacheEntry entry = successfulEnteries[i] as CacheEntry;
                                    CacheEntry cloneValue = null; Array userPayLoad; long payLoadSize;
                                    _context.CachingSubSystemDataService.GetEntryClone(entry, out cloneValue, out userPayLoad, out payLoadSize);
                                    cloneValue?.MarkInUse(NCModulesConstants.Replication);
                                    _context.CacheImpl.EnqueueForReplication(successfulKeys[i], (int)ClusterCacheBase.OpCodes.Insert, new object[] { successfulKeys[i], cloneValue, (taskId != null) ? taskId + "-" + i : null, operationContext }, entry.Size, userPayLoad, payLoadSize);

                                }
                            }


                        }
                        else
                        {
                            #region [Enable When Entries for Bridge are to be Cloned from Transactional Pool]
                            // All keys probably failed to insert so return all clones to pool
                            //MiscUtil.ReturnEntriesToPool(clone, Context.TransactionalPoolManager);
                            #endregion
                        }
                    }
#endif
                }
                finally
                {
                    if (clone != null)
                        clone.MarkFree(NCModulesConstants.CacheSync);
                }

                return result;
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.CacheSync);
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
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            return Remove(key, ir, notify, null, lockId, version, accessType, operationContext);
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
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, string taskId, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry entry = null;
           

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetWriterLock(lockKey);
            try
            {
                
                
                operationContext?.MarkInUse(NCModulesConstants.CacheSync);

                entry = Internal.Remove(key, ir, notify, lockId, version, accessType, operationContext);
#if SERVER

                object removeOnReplica = operationContext.GetValueByField(OperationContextFieldName.RemoveOnReplica);
                if (entry != null)
                {
                  
                    if (_context.CacheImpl.RequiresReplication)
                        _context.CacheImpl.EnqueueForReplication(entry.Type != Common.Caching.EntryType.CacheItem ? null : key, (int)ClusterCacheBase.OpCodes.Remove, new object[] { key, taskId, operationContext });
                }
                else if (removeOnReplica != null)
                {
                    _context.NCacheLog.Error("CacheSync Remove on Replica Key : " + key);

                    if (_context.CacheImpl.RequiresReplication)
                        _context.CacheImpl.EnqueueForReplication(key, (int)ClusterCacheBase.OpCodes.Remove, new object[] { key, taskId, operationContext });
                }
#endif
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.CacheSync);

                KeyLocker.ReleaseWriterLock(lockKey);

            }
            return entry;
        }

        public override Hashtable Remove(IList keys, ItemRemoveReason ir, bool notify, OperationContext operationContext)
        {
            return Remove(keys, ir, notify, null, operationContext);
        }

        /// <summary>
        /// Removes the key and pairs from the cache. The keys are specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="keys">key of the entries.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>removed keys list</returns>
        public override Hashtable Remove(IList keys, ItemRemoveReason ir, bool notify, string taskId, OperationContext operationContext)
        {

            Hashtable result = null;
            object[] successfulKeys = null;
            
            bool requiresReplication = false;

#if SERVER
            requiresReplication = _context.CacheImpl.RequiresReplication;

#endif

            try
            {
                result = Internal.Remove(keys, ir, notify, operationContext);
                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);
#if SERVER
                if (requiresReplication)
                {
                    if (result != null && result.Count > 0)
                    {
                       successfulKeys = new object[result.Count];
                        int j = 0;
                        for (int i = 0; i < keys.Count; i++)
                        {
                            if (result.Contains(keys[i]))
                            {
                                if (requiresReplication)
                                    successfulKeys[j] = keys[i];

                                j++;
                            }
                        }
                        if (requiresReplication)
                        {
                            //it is required by the replicator and we do not want this operation 2 be overriden
                            //in optimized queue.
                            string uniqueKey = System.Guid.NewGuid().ToString() + keys[0];
                            _context.CacheImpl.EnqueueForReplication(null, (int)Alachisoft.NCache.Caching.Topologies.Clustered.ClusterCacheBase.OpCodes.RemoveRange, new object[] { keys, ir, taskId, operationContext });
                        }
                    }

                }
#endif
            }
            finally
            {

            }
            return result;
        }

        public override Hashtable Cascaded_remove(Hashtable keyValues, ItemRemoveReason ir, bool notify, bool isUserOperation, OperationContext operationContext)
        {
            Hashtable removedItems = new Hashtable();
            ArrayList removedItemsList = new ArrayList();
            CacheEntry e;
            if (keyValues != null)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Remove_3", "enter");
                bool replicateToBridge = false;
                bool isBridgeOperation = false;
                
                try
                {

                    IDictionaryEnumerator ide = keyValues.GetEnumerator();

                    while (ide.MoveNext())
                    {
                        e = ide.Value as CacheEntry;
                        if (e != null && e.KeysDependingOnMe != null)
                        {
                            Hashtable entriesTable = new Hashtable();
                            string[] nextRemovalKeys = new string[e.KeysDependingOnMe.Count];
                            e.KeysDependingOnMe.Keys.CopyTo(nextRemovalKeys, 0);
                           while (nextRemovalKeys != null && nextRemovalKeys.Length > 0)
                            {
                                entriesTable = Remove(nextRemovalKeys, ir, notify, operationContext);
                                if (entriesTable != null)
                                {
                                    IDictionaryEnumerator ide2 = entriesTable.GetEnumerator();
                                    if (ide2.MoveNext())
                                    {
                                        if (ide2.Value != null)
                                        {
                                            removedItems[ide2.Key] = ide2.Value;
                                            removedItemsList.Add(ide2.Key);
                                        }
                                    }
                                }
                                nextRemovalKeys = ExtractKeys(entriesTable);
                            }
                        }
                    }
                }
                finally
                {
                    if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Remove_3", "exit");

                }
            }
            return removedItems;
        }

        public override Hashtable Remove(IList keys, ItemRemoveReason ir, bool notify, bool isUserOperation, OperationContext operationContext)
        {
            Hashtable result = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Remove_4", "enter");
            
            try
            {

                result = Internal.Remove(keys, ir, notify, isUserOperation, operationContext);

               
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Remove_4", "exit");

            }
            return result;
        }

        public override object RemoveSync(object[] keys, ItemRemoveReason reason, bool notify, OperationContext operationContext)
        {
            return Internal.RemoveSync(keys, reason, notify, operationContext);
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
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, bool isUserOperation, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry entry = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Remove_5", "enter");

            var lockKey = GetStringFromPool((string)key);
            KeyLocker.GetWriterLock(lockKey);
            try
            {



                entry = Internal.Remove(key, ir, notify, isUserOperation, lockId, version, accessType, operationContext);


            }
            finally
            {
                KeyLocker.ReleaseWriterLock(lockKey);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Remove_5", "exit");

            }
            return entry;
        }


        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        public override Hashtable Remove(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            Hashtable result = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemoveGrp", "enter");

            try
            {
                result = Internal.Remove(group, subGroup, notify, operationContext);
#if SERVER
                if (_context.CacheImpl.RequiresReplication)
                {
                    //it is required by the replicator and we do not want this operation 2 be overriden
                    //in optimized queue.
                    string uniqueKey = System.Guid.NewGuid().ToString() + group;
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.RemoveGroup, new object[] { group, subGroup, operationContext });
                }
#endif
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemoveGrp", "exit");

            }
            return result;
        }

        public override Hashtable Remove(string[] tags, TagComparisonType tagComparisonType, bool notify, OperationContext operationContext)
        {
            Hashtable removedKeys = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemoveByTag", "enter");

            try
            {
                removedKeys = Internal.Remove(tags, tagComparisonType, notify, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemoveByTag", "exit");

            }
            return removedKeys;
        }

      

        public override void DeclaredDeadClients(ArrayList clients)
        {
            InternalCache.DeclaredDeadClients(clients);
        }

        /// <summary>
        /// Broadcasts a user-defined event across the cluster.
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="data"></param>
        /// <param name="async"></param>
        public override void SendNotification(object notifId, object data, OperationContext operationContext)
        {
            Internal.SendNotification(notifId, data, operationContext);
        }

        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        /// <returns>IDictionaryEnumerator enumerator.</returns>
        public override IDictionaryEnumerator GetEnumerator()
        {
            return Internal.GetEnumerator();
        }

        public override EnumerationDataChunk GetNextChunk(EnumerationPointer pointer, OperationContext operationContext)
        {
            return Internal.GetNextChunk(pointer, operationContext);
        }

        public override bool HasEnumerationPointer(EnumerationPointer pointer)
        {
            return Internal.HasEnumerationPointer(pointer);
        }

        #endregion

        #region/            --- Key based notification registration ---         /

        public override void RegisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
#if SERVER
           
#endif
            var lockKey = GetStringFromPool(key);
            KeyLocker.GetWriterLock(lockKey);

            try
            {
#if SERVER

              
#endif
                Internal.RegisterKeyNotification(key, updateCallback, removeCallback, operationContext);

            }
            finally
            {
                KeyLocker.ReleaseWriterLock(lockKey);
            }

        }

        public override void RegisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
#if SERVER
          
            bool mirrorReplication = _context.CacheImpl.RequiresReplication;
#endif

            Internal.RegisterKeyNotification(keys, updateCallback, removeCallback, operationContext);
#if SERVER

            if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(ExceptionsResource.OperationFailed);

            if (mirrorReplication)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (mirrorReplication)
                    {
                        string uniqueKey = System.Guid.NewGuid().ToString() + keys[i];
                        _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.RegisterKeyNotification, new object[] { keys[i], updateCallback, removeCallback, operationContext });
                    }
                }
            }
#endif

        }

        public override void UnregisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            var lockKey = GetStringFromPool(key);
            KeyLocker.GetWriterLock(lockKey);

            try
            {
                Internal.UnregisterKeyNotification(key, updateCallback, removeCallback, operationContext);
#if SERVER
                
                if (_context.CacheImpl.RequiresReplication)
                {
                    string uniqueKey = System.Guid.NewGuid().ToString() + key;
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UnregisterKeyNotification, new object[] { key, updateCallback, removeCallback, operationContext });
                }
#endif
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(lockKey);
            }

        }

        public override void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        {
#if SERVER
        
            bool mirrorReplication = _context.CacheImpl.RequiresReplication;
#endif

#if SERVER
           
#endif
            Internal.UnregisterKeyNotification(keys, updateCallback, removeCallback, operationContext);
#if SERVER
            if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(ExceptionsResource.OperationFailed);

            if ( mirrorReplication)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (mirrorReplication)
                    {
                        _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UnregisterKeyNotification, new object[] { keys[i], updateCallback, removeCallback, operationContext });
                    }
                }
            }
#endif
        }

        #endregion

      

#region /          --- Cascaded Key dependencies Opertions ---            /
        public override Hashtable AddDepKeyList(Hashtable table, OperationContext operationContext)
        {
#if SERVER
            bool requiresReplication = _context.CacheImpl.RequiresReplication;
#endif
            Hashtable result = null;
            try
            {
                result = Internal.AddDepKeyList(table, operationContext);

#if SERVER
                if (requiresReplication)
                {
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.AddDepKeyList, new object[] { table, operationContext }, 0, null, 0);
                }
#endif
            }
            finally
            {

            }
            return result;
        }

        public override Hashtable RemoveDepKeyList(Hashtable table, OperationContext operationContext)
        {
            try
            {
                return Internal.RemoveDepKeyList(table, operationContext);
            }
            finally
            {
            }
        }
#endregion

#region/                    --- Stream ---                               /

        public override bool OpenStream(string key, string lockHandle, StreamModes mode, string group, string subGroup, ExpirationHint hint, EvictionHint evictinHint, OperationContext operationContext)
        {
            bool lockAcquired = false;

#if SERVER
            bool requiresReplication = _context.CacheImpl.RequiresReplication;
#endif
            var lockKey = GetStringFromPool(key);
            KeyLocker.GetReaderLock(lockKey);

            try
            {
                lockAcquired = InternalCache.OpenStream(key, lockHandle, mode, group, subGroup, hint, evictinHint, operationContext);

                if (lockAcquired)
                {
#if SERVER
                    if (requiresReplication)
                    {
                        OpenStreamOperation streamOperation = new OpenStreamOperation(key, lockHandle, mode, group, subGroup, hint, evictinHint, operationContext);
                        _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.OpenStream, streamOperation);
                    }
#endif
                }

            }
            finally
            {
                KeyLocker.ReleaseReaderLock(lockKey);
            }
            return lockAcquired;
        }

        public override void CloseStream(string key, string lockHandle, OperationContext operationContext)
        {

#if SERVER
         
            bool requiresReplication = _context.CacheImpl.RequiresReplication;
#endif
            var lockKey = GetStringFromPool(key);
            KeyLocker.GetReaderLock(lockKey);

            try
            {
                InternalCache.CloseStream(key, lockHandle, operationContext);
#if SERVER

                if (requiresReplication)
                {
                    CloseStreamOperation streamOperation = new CloseStreamOperation(key, lockHandle, operationContext);
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.CloseStream, streamOperation);
                }
#endif
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(lockKey);
            }
        }

        public override int ReadFromStream(ref VirtualArray vBuffer, string key, string lockHandle, int offset, int length, OperationContext operationContext)
        {
            return InternalCache.ReadFromStream(ref vBuffer, key, lockHandle, offset, length, operationContext);
        }

        public override void WriteToStream(string key, string lockHandle, VirtualArray vBuffer, int srcOffset, int dstOffset, int length, OperationContext operationContext)
        {

#if SERVER
          
            bool requiresReplication = _context.CacheImpl.RequiresReplication;
#endif
            var lockKey = GetStringFromPool(key);
            KeyLocker.GetWriterLock(lockKey);

            try
            {
                InternalCache.WriteToStream(key, lockHandle, vBuffer, srcOffset, dstOffset, length, operationContext);
#if SERVER

                if (requiresReplication)
                {
                    WriteToStreamOperation streamOperation = new WriteToStreamOperation(key, lockHandle, vBuffer, srcOffset, dstOffset, length, operationContext);
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.WriteToStream, streamOperation);
                }
#endif
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(lockKey);
            }
        }

        public override long GetStreamLength(string key, string lockHandle, OperationContext operationContext)
        {
            return InternalCache.GetStreamLength(key, lockHandle, operationContext);
        }
#endregion




        internal override void Touch(List<string> keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Touch", "enter");
            try
            {
                Internal.Touch(keys, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Touch", "exit");
            }
        }


#region ---------------------- IMessageStore Implementation ----------------

        public override MessageInfo GetNextUnassignedMessage(TimeSpan timeout, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetNextUnassignedMessage", "enter");

            MessageInfo result = null;

            try
            {
                result = Internal.GetNextUnassignedMessage(timeout, context);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetNextUnassignedMessage", "exit");

            }

            return result;
        }

      

        public override IList<MessageInfo> GetUnacknowledgeMessages(TimeSpan assginmentTimeout)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetUnacknowledgeMessages", "enter");

            IList<MessageInfo> result = null;

            try
            {
                result = Internal.GetUnacknowledgeMessages(assginmentTimeout);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetUnacknowledgeMessages", "exit");

            }
            return result;
        }
        public override void RevokeAssignment(MessageInfo message, SubscriptionInfo subscription, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RevokeSubscription", "enter");

            var lockKey = GetStringFromPool(message.MessageId);
            KeyLocker.GetWriterLock(lockKey);

            try
            {
                Internal.RevokeAssignment(message, subscription, context);
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(lockKey);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RevokeSubscription", "exit");

            }
        }

        public override IList<string> GetNotifiableClients()
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetNotifiableClients", "enter");

            try
            {
                return Internal.GetNotifiableClients();
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetNotifiableClients", "exit");

            }
        }

        public override SubscriptionInfo GetSubscriber(string topic, Common.Enum.SubscriptionType type, OperationContext context)
        {

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetSubscriber", "enter");

            SubscriptionInfo result = null;

            try
            {
                result = Internal.GetSubscriber(topic, type, context);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetSubscriber", "exit");

            }
            return result;
        }

        public override IList<SubscriptionInfo> GetAllSubscriber(string topic, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetAllSubscriber", "enter");

            IList<SubscriptionInfo> result = null;

            try
            {
                result = Internal.GetAllSubscriber(topic, context);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetAllSubscriber", "exit");

            }


            return result;
        }

        public override MessageResponse GetAssignedMessage(SubscriptionInfo subscriptionInfo, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetAssignedMessage", "enter");

            MessageResponse result = null;

            try
            {
                result = Internal.GetAssignedMessage(subscriptionInfo, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetAssignedMessage", "exit");

            }
            return result;
        }

        public override IList<MessageInfo> GetDeliveredMessages()
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetDeliveredMessages", "enter");

            IList<MessageInfo> result = null;

            try
            {
                result = Internal.GetDeliveredMessages();
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetDeliveredMessages", "exit");

            }
            return result;
        }

        public override IList<MessageInfo> GetExpiredMessages()
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetExpiredMessages", "enter");

            IList<MessageInfo> result = null;

            try
            {
                result = Internal.GetExpiredMessages();
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetExpiredMessages", "exit");

            }
            return result;
        }

        public override IList<MessageInfo> GetEvicatableMessages(long sizeToEvict)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetEvicatableMessages", "enter");

            IList<MessageInfo> result = null;

            try
            {
                result = Internal.GetEvicatableMessages(sizeToEvict);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetEvicatableMessages", "exit");

            }
            return result;
        }

        public override bool StoreMessage(string topic, Message message, OperationContext context)
        {
            bool stored = false;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.StoreMessage", "enter");

            var lockKey = GetStringFromPool(message.MessageId);
            KeyLocker.GetWriterLock(lockKey);

            try
            {
                stored = Internal.StoreMessage(topic, message, context);
#if !CLIENT
                //we do not want to replicate event message
                if (_context.CacheImpl.RequiresReplication && !(message is EventMessage))
                {
                    StoreMessageOperation storeMessageOperation = new StoreMessageOperation(topic, message, context);
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.StoreMessage, storeMessageOperation);
                }
#endif
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(lockKey);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.StoreMessage", "exit");
            }
            return stored;
        }

        public override bool AssignmentOperation(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo, TopicOperationType type, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.AssignmentOperation", "enter");

            bool result = false;

            var lockKey = GetStringFromPool(messageInfo.MessageId);
            KeyLocker.GetWriterLock(lockKey);

            try
            {
                result = Internal.AssignmentOperation(messageInfo, subscriptionInfo, type, context);
#if !CLIENT
                if (_context.CacheImpl.RequiresReplication)
                {
                    if (!messageInfo.IsEventMessage)
                    {
                        AssignmentOperation subscriptionOperation = new AssignmentOperation(messageInfo, subscriptionInfo, type, context);
                        _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.AssignmentOperation, subscriptionOperation);
                    }

                }
#endif
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(lockKey);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.AssignmentOperation", "exit");
            }
            return result;
        }

        public override void AcknowledgeMessageReceipt(string clientId, IDictionary<string, IList<string>> topicWiseMessageIds, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity)
                ServerMonitor.LogClientActivity("CacheSyncWrp.AcknowledgeMessageReceipt", "enter");

            try
            {
                Internal.AcknowledgeMessageReceipt(clientId, topicWiseMessageIds, operationContext);
#if !CLIENT
                if (_context.CacheImpl.RequiresReplication)
                {
                    AcknowledgeMessageOperation acknowledgeMessageOperation = new AcknowledgeMessageOperation(clientId, topicWiseMessageIds, operationContext);
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.Message_Acknowldegment, acknowledgeMessageOperation);
                }
#endif
            }
            finally
            {
                if (ServerMonitor.MonitorActivity)
                    ServerMonitor.LogClientActivity("CacheSyncWrp.AcknowledgeMessageReceipt", "exit");

            }

        }


        public override void RemoveMessages(IList<MessageInfo> messagesTobeRemoved, MessageRemovedReason reason, OperationContext context)
        {
            if (!IsCacheOperationAllowed(context))
                return;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemoveMessages", "enter");

            try
            {
                Internal.RemoveMessages(messagesTobeRemoved, reason, context);
#if !CLIENT
                if (_context.CacheImpl.RequiresReplication)
                {
                    RemoveMessagesOperation removeMessagesOperation = new RemoveMessagesOperation(messagesTobeRemoved, reason, context);
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.RemoveMessages, removeMessagesOperation);
                }
#endif
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemoveMessages", "exit");
            }
        }

        public override bool TopicOperation(TopicOperation operation, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.TopicOperation", "enter");

            bool result;

            try
            {
                result = Internal.TopicOperation(operation, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.TopicOpertion", "exit");

            }
            return result;
        }


        public override void RegiserTopicEventListener(ITopicEventListener listener)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RegiserTopicEventListener", "enter");
            try
            {
                Internal.RegiserTopicEventListener(listener);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RegiserTopicEventListener", "exit");

            }

        }

        public override OrderedDictionary GetMessageList(int bucketId, bool includeEventMessages)
        {
            return Internal.GetMessageList(bucketId,includeEventMessages);
        }

        public override long GetMessageCount(string topicName, OperationContext operationContext)
        {
            if (Internal == null)
            {
                throw new InvalidOperationException(string.Empty, new ArgumentNullException("Internal"));
            }
            return Internal.GetMessageCount(topicName, operationContext);
        }

        public override TransferrableMessage GetTransferrableMessage(string topic, string messageId)
        {

            var lockKey = GetStringFromPool(topic);
            _keyLockManager.GetReaderLock(lockKey);

            try
            {
                return Internal.GetTransferrableMessage(topic, messageId);
            }
            finally
            {
                _keyLockManager.ReleaseReaderLock(lockKey);
            }
        }

        public override bool StoreTransferrableMessage(string topic, TransferrableMessage message)
        {
            var lockKey = GetStringFromPool(message.Message.MessageId);
            KeyLocker.GetWriterLock(lockKey);

            try
            {
                return Internal.StoreTransferrableMessage(topic, message);
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(lockKey);
            }
        }

        public override TopicState GetTopicsState()
        {
            return Internal.GetTopicsState();
        }
        
        public override void SetTopicsState(TopicState topicState)
        {
            Internal.SetTopicsState(topicState);
        }
       
#endregion


        public override void ClientDisconnected(string client, bool isInproc, Runtime.Caching.ClientInfo clientInfo)
        {
            if (Internal != null)
            {
                Internal.ClientDisconnected(client, isInproc, clientInfo);
            }

        }

        public override void ClientConnected(string client, bool isInproc, Runtime.Caching.ClientInfo clientInfo)
        {
            if (Internal != null)
            {
                Internal.ClientConnected(client, isInproc, clientInfo);
            }

        }

        public override Dictionary<string, TopicStats> GetTopicsStats(bool defaultTopicStats= false)
        {
            return Internal.GetTopicsStats(defaultTopicStats);
        }

        public override IDictionary<string, IList<string>> GetInActiveClientSubscriptions(TimeSpan inactivityThreshold)
        {
            return Internal.GetInActiveClientSubscriptions(inactivityThreshold);
        }

        public override IDictionary<string, IList<string>> GetActiveClientSubscriptions(TimeSpan inactivityThreshold)
        {
            return Internal.GetActiveClientSubscriptions(inactivityThreshold);
        }

        internal override void SetClusterInactive(string reason)
        {
            if (Context != null && Context.CacheImpl != null) Context.CacheImpl.SetClusterInactive(reason);
        }

       
        #region Private Methods

        private string GetStringFromPool(string str)
        {
            return Context.TransactionalPoolManager.StringPool.GetString(str);
        }

        #endregion

    }

}
