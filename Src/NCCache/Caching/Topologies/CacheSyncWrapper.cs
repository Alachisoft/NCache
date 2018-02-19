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
using System.Threading;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.Topologies.Clustered;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.DataReader;

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
            _KeyLockManager = _cache.KeyLocker;
            _syncObj = cache.Sync;
        }


        #region	/                 --- IDisposable ---           /

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
                Sync.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return _cache.VirtualUnlimitedSpace;
                }
                finally
                {
                    Sync.ReleaseReaderLock();
                }
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
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Count_get", "enter");

                try
                {
                    return Internal.Count;
                }
                finally
                {
                    if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Count_get", "exit");

                }
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
        /// returns the statistics of the Clustered Cache. 
        /// </summary>
        internal override CacheStatistics ActualStats
        {
            get
            {
                if (!Sync.IsWriterLockHeld)
                {
                    Sync.AcquireReaderLock(Timeout.Infinite);
                }
                try
                {
                    return Internal.ActualStats;
                }
                finally
                {
                    if (!Sync.IsWriterLockHeld)
                    {
                        Sync.ReleaseReaderLock();
                    }
                }
            }
        }

        /// <summary>
        /// returns the statistics of the Clustered Cache. 
        /// </summary>
        public override CacheStatistics Statistics
        {
            get
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Stats_get", "enter");

                if (!Sync.IsWriterLockHeld)
                {
                    Sync.AcquireReaderLock(Timeout.Infinite);
                }
                try
                {
                    return Internal.Statistics;
                }
                finally
                {
                    if (!Sync.IsWriterLockHeld)
                    {
                        Sync.ReleaseReaderLock();
                    }
                    if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Stats_get", "exit");

                }
            }
        }



        #region /                   --- Hashed cache related ---        /

        public override void GetKeyList(int bucketId, bool startLogging, out ClusteredArrayList keyList)
        {
            Sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                _cache.GetKeyList(bucketId, startLogging, out keyList);
            }
            finally
            {
                Sync.ReleaseReaderLock();
            }
        }

        public override void RemoveBucket(int bucket)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _cache.RemoveBucket(bucket);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public override void RemoveExtraBuckets(ArrayList bucketIds)
        {
            try
            {
                Sync.AcquireWriterLock(Timeout.Infinite);
                _cache.RemoveExtraBuckets(bucketIds);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public override Hashtable GetLogTable(ArrayList bucketIds, ref bool isLoggingStopped)
        {
            Sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _cache.GetLogTable(bucketIds, ref isLoggingStopped);
            }
            finally
            {
                Sync.ReleaseReaderLock();
            }
        }
        public override int BucketSize
        {
            set
            {
                Sync.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    _cache.BucketSize = value;
                }
                finally
                {
                    Sync.ReleaseWriterLock();
                }
            }
        }
        public override void RemoveBucketData(int bucketId)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _cache.RemoveBucketData(bucketId);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }
        public override void AddLoggedData(ArrayList bucketIds)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _cache.AddLoggedData(bucketIds);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public override void UpdateLocalBuckets(ArrayList bucketIds)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _cache.UpdateLocalBuckets(bucketIds);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public override void RemoveFromLogTbl(int bucketId)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _cache.RemoveFromLogTbl(bucketId);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public override void StartLogging(int bucketId)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _cache.StartLogging(bucketId);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public override HashVector LocalBuckets
        {
            get
            {
                Sync.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return _cache.LocalBuckets;
                }
                finally
                {
                    Sync.ReleaseReaderLock();
                }
            }
            set
            {
                Sync.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    _cache.LocalBuckets = value;
                }
                finally
                {
                    Sync.ReleaseWriterLock();
                }
            }
        }

        #endregion

        #region	/                 --- ICache ---           /

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

        internal override bool CanChangeCacheSize(long size)
        {
            return Internal.CanChangeCacheSize(size);
        }

        internal override long MaxSize
        {
            get
            {
                Sync.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return Internal.MaxSize;
                }
                finally
                {
                    Sync.ReleaseReaderLock();
                }
            }
            set
            {
                Sync.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    Internal.MaxSize = value;
                }
                finally
                {
                    Sync.ReleaseWriterLock();
                }
            }
        }

        public override void UpdateClientsList(Hashtable list)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                Internal.UpdateClientsList(list);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        



        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public override void Clear(CallbackEntry cbEntry, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Clear", "enter");

            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                Internal.Clear(cbEntry, operationContext);
                if (_context.CacheImpl.RequiresReplication)
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.Clear, new object[] { cbEntry, operationContext });
            }
            finally
            {
                Sync.ReleaseWriterLock();
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Clear", "exit");

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

            KeyLocker.GetReaderLock(key);

            try
            {
                return Internal.Contains(key, operationContext);
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(key);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Cont", "exit");
            }
        }


        /// <summary>
        /// Determines whether the cache contains the specified keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>list of existing keys.</returns>
        public override Hashtable Contains(object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.ContBlk", "enter");

            try
            {
                return Internal.Contains(keys, operationContext);
            }
            finally
            {
               
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.ContBlk", "exit");

            }
        }

        internal override void UpdateLockInfo(object key, object lockId, DateTime lockDate, LockExpiration lockExpiration, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.UpdLock", "enter");
            KeyLocker.GetReaderLock(key);

            try
            {
                Internal.UpdateLockInfo(key, lockId, lockDate, lockExpiration, operationContext);
            }
            finally
            { 
                KeyLocker.ReleaseReaderLock(key);
    
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.UpdLock", "exit");

            }
        }
        public override CacheEntry Get(object key, bool IsUserOperation, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry entry = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Get_2", "enter");
            KeyLocker.GetReaderLock(key);

            try
            {
                entry = Internal.Get(key, IsUserOperation, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
                if (accessType == LockAccessType.ACQUIRE && entry != null && _context.CacheImpl.RequiresReplication)
                {
                    string uniqueKey = System.Guid.NewGuid().ToString() + key;
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UpdateLockInfo, new object[] { true, key, lockId, lockDate, lockExpiration, operationContext });
                }
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(key);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Get_2", "exit");

            }
            return entry;
        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="lockId"></param>
        /// <param name="lockDate"></param>
        /// <param name="lockExpiration"></param>
        /// <param name="access"></param>
        /// <param name="operationContext"></param>
        /// <returns>cache entry.</returns>
        public override CacheEntry Get(object key, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType access, OperationContext operationContext)
        {
            CacheEntry entry = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Get_2", "enter");
            KeyLocker.GetReaderLock(key);

            try
            {
                entry = Internal.Get(key, ref lockId, ref lockDate, lockExpiration, access, operationContext);

                if (access == LockAccessType.ACQUIRE && entry != null && _context.CacheImpl.RequiresReplication)
                {
                    string uniqueKey = System.Guid.NewGuid().ToString() + key;
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UpdateLockInfo, new object[] { true, key, lockId, lockDate, lockExpiration, operationContext });
                }

            }
            finally
            {
                KeyLocker.ReleaseReaderLock(key);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Get_2", "exit");

            }
            return entry;
        }
      
        public override LockOptions IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            LockOptions lockInfo = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.IsLock", "enter");

            KeyLocker.GetReaderLock(key);

            try
            {
                lockInfo = Internal.IsLocked(key, ref lockId, ref lockDate, operationContext);
            }
            finally
            { 
                KeyLocker.ReleaseReaderLock(key);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.IsLock", "exit");

            }
            return lockInfo;
        }

        public override LockOptions Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            LockOptions lockInfo = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Lock", "enter");

            KeyLocker.GetReaderLock(key);
            try
            {
                lockInfo = Internal.Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
                if (_context.CacheImpl.RequiresReplication)
                {
                    string uniqueKey = System.Guid.NewGuid().ToString() + key;
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UpdateLockInfo, new object[] { true, key, lockId, lockDate, lockExpiration, operationContext });
                }
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(key);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Lock", "exit");

            }
            return lockInfo;
        }

        public override void UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Unlock", "enter");
            KeyLocker.GetReaderLock(key);
            try
            {
                Internal.UnLock(key, lockId, isPreemptive, operationContext);

                if (_context.CacheImpl.RequiresReplication)
                {
                    string uniqueKey = System.Guid.NewGuid().ToString() + key;
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UpdateLockInfo, new object[] { false, key, lockId, DateTime.Now, null, isPreemptive, operationContext });
                }
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(key);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Unlock", "exit");

            }
        }


        /// <summary>
        /// Retrieve the objects from the cache. An array of keys is passed as parameter.
        /// </summary>
        /// <param name="key">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        public override Hashtable Get(object[] keys, OperationContext operationContext)
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
        
        public override bool Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
            bool depAdded = false;
            
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_3", "enter");
            KeyLocker.GetWriterLock(key);

            try
            {
                depAdded = Internal.Add(key, eh, operationContext);

                if (depAdded && _context.CacheImpl.RequiresReplication)
                {
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.AddHint, new object[] { key, eh, operationContext });
                }
            }
            finally
            { 
                KeyLocker.ReleaseWriterLock(key);
    
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_3", "exit");

            }
            return depAdded;
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <param name="notify"></param>
        /// <param name="operationContext"></param>
        /// <returns>returns the result of operation.</returns>
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
        {
            bool requiresReplication = _context.CacheImpl.RequiresReplication;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_1", "enter");

            KeyLocker.GetWriterLock(key);
            try
            {
                CacheEntry clone = null;
                if ((requiresReplication))
                {
                    if (cacheEntry.HasQueryInfo)
                        clone = cacheEntry.Clone() as CacheEntry;
                    else
                        clone = cacheEntry;
                }

                CacheAddResult result = Internal.Add(key, cacheEntry, notify, operationContext);

                if (requiresReplication)
                {
                    if (result == CacheAddResult.Success || result == CacheAddResult.SuccessNearEviction)
                    {
                        if (requiresReplication)
                        {
                            CacheEntry cloneWithoutvalue = clone.CloneWithoutValue() as CacheEntry;

                            Array userPayLoad = cacheEntry.UserData;
                            long payLoadSize = cacheEntry.DataSize;
                            _context.CacheImpl.EnqueueForReplication(key, (int)ClusterCacheBase.OpCodes.Add, new object[] { key, cloneWithoutvalue, operationContext }, clone.Size, userPayLoad, payLoadSize);
                        }
                    }
                }
                return result;
            }
            finally
            { 
                KeyLocker.ReleaseWriterLock(key);
    
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_1", "exit");

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
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, bool IsUserOperation, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_2", "enter");

            KeyLocker.GetWriterLock(key);
            try
            {
                CacheAddResult result = Internal.Add(key, cacheEntry, notify, IsUserOperation, operationContext);
                return result;
            }
            finally
            { 
                KeyLocker.ReleaseWriterLock(key);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_2", "exit");
            }
        }

        /// <summary>
        /// Adds key and value pairs to the cache. Throws an exception or returns the
        /// list of keys that already exists in the cache.
        /// </summary>
        /// <param name="keys">key of the entry.</param>
        /// <param name="cacheEntries">the cache entry.</param>
        /// <param name="notify"></param>
        /// <param name="operationContext"></param>
        /// <returns>List of keys that are added or that alredy exists in the cache and their status</returns>
        public override Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            Hashtable result = null;
            bool requiresReplication = _context.CacheImpl.RequiresReplication;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.AddBlk", "enter");
            try
            {
                CacheEntry[] clone = null;
                if (requiresReplication)
                {
                    clone = new CacheEntry[cacheEntries.Length];
                    for (int i = 0; i < cacheEntries.Length; i++)
                    {
                        if (cacheEntries[i].HasQueryInfo)
                            clone[i] = (CacheEntry)cacheEntries[i].Clone();
                        else
                            clone[i] = cacheEntries[i];
                    }
                }

                result = Internal.Add(keys, cacheEntries, notify, operationContext);

                if (requiresReplication)
                {
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
                                }
                            }
                        }
                    }

                    if (successfulKeys.Count > 0)
                    {
                        for (int i = 0; i < successfulKeys.Count; i++)
                        {
                            CacheEntry entry = successfulEnteries[i] as CacheEntry;
                            _context.CacheImpl.EnqueueForReplication(successfulKeys[i], (int)ClusterCacheBase.OpCodes.Add, new object[] { successfulKeys[i], entry.CloneWithoutValue() as CacheEntry, operationContext }, entry.Size, entry.UserData, entry.DataSize);
                        }
                    }
                }
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.AddBlk", "exit");

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
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, LockAccessType access, OperationContext operationContext)
        {
            bool requiresReplication = false;

            requiresReplication = _context.CacheImpl.RequiresReplication;

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Insert_1", "enter");

            KeyLocker.GetWriterLock(key);
            try
            {

                CacheEntry clone = null;

                if (requiresReplication)
                {
                    if (cacheEntry.HasQueryInfo)
                        clone = cacheEntry.Clone() as CacheEntry;
                    else
                        clone = cacheEntry;
                }

                CacheInsResultWithEntry result = Internal.Insert(key, cacheEntry, notify, lockId, access, operationContext);

                if (requiresReplication)
                {
                    if (result.Result == CacheInsResult.Success || result.Result == CacheInsResult.SuccessNearEvicition || result.Result == CacheInsResult.SuccessOverwrite || result.Result == CacheInsResult.SuccessOverwriteNearEviction)
                    {
                        CacheEntry cloneWithoutvalue = clone.CloneWithoutValue() as CacheEntry;

                        if (result.Result == CacheInsResult.SuccessOverwrite &&
                            access == LockAccessType.DONT_RELEASE)
                        {
                            clone.CopyLock(result.Entry.LockId, result.Entry.LockDate, result.Entry.LockExpiration);
                        }
                        _context.CacheImpl.EnqueueForReplication(key, (int)ClusterCacheBase.OpCodes.Insert, new object[] { key, cloneWithoutvalue, operationContext }, clone.Size, cacheEntry.UserData, cacheEntry.DataSize);

                    }
                }

                return result;
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(key);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Insert_1", "exit");

            }
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, bool IsUserOperation, object lockId, LockAccessType accessType, OperationContext operationContext)
        {

            bool requiresReplication = false;
            requiresReplication = _context.CacheImpl.RequiresReplication;

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Insert_2", "enter");

            KeyLocker.GetWriterLock(key);

            try
            {
                CacheInsResultWithEntry result = Internal.Insert(key, cacheEntry, notify, IsUserOperation, lockId, accessType, operationContext);
                return result;
            }
            finally
            { 
                KeyLocker.ReleaseWriterLock(key);
    
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Insert_2", "exit");

            }
        }

        /// <summary>
        /// Adds key and value pairs to the cache. If any of the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">the cache entries.</param>
        /// <returns>returns the results for inserted keys</returns>
        public override Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            Hashtable result = null;
            bool requiresReplication = false;
            requiresReplication = _context.CacheImpl.RequiresReplication;
            CacheEntry[] clone = null;
            if (requiresReplication)
            {
                clone = new CacheEntry[cacheEntries.Length];
                for (int i = 0; i < cacheEntries.Length; i++)
                {
                    if (cacheEntries[i].HasQueryInfo)
                        clone[i] = (CacheEntry)cacheEntries[i].Clone();
                    else
                        clone[i] = cacheEntries[i];
                }
            }

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.InsertBlk", "enter");

            try
            {
                result = Internal.Insert(keys, cacheEntries, notify, operationContext);

                if (requiresReplication)
                {
                    if (result != null && result.Count > 0)
                    {
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
                                }
                            }
                        }

                        if (successfulKeys.Count > 0)
                        {
                            for (int i = 0; i < successfulKeys.Count; i++)
                            {
                                CacheEntry entry = successfulEnteries[i] as CacheEntry;
                                _context.CacheImpl.EnqueueForReplication(successfulKeys[i], (int)ClusterCacheBase.OpCodes.Insert, new object[] { successfulKeys[i], entry.CloneWithoutValue() as CacheEntry, operationContext }, entry.Size, entry.UserData, entry.DataSize);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.InsertBlk", "exit");
            }

            return result;
        }

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="ir"></param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <param name="lockId"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <returns>item value</returns>
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry entry = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Remove_1", "enter");
            KeyLocker.GetWriterLock(key);

            try
            {

                entry = Internal.Remove(key, ir, notify, lockId, accessType, operationContext);

                object removeOnReplica = operationContext.GetValueByField(OperationContextFieldName.RemoveOnReplica);

                if (entry != null)
                {
                    if (_context.CacheImpl.RequiresReplication)
                        _context.CacheImpl.EnqueueForReplication(key, (int)ClusterCacheBase.OpCodes.Remove, new object[] { key, operationContext });

                }
                else if (removeOnReplica != null)
                {
                    _context.NCacheLog.Error("CacheSync Remove on Replica Key : " + key);

                    if (_context.CacheImpl.RequiresReplication)
                        _context.CacheImpl.EnqueueForReplication(key, (int)ClusterCacheBase.OpCodes.Remove, new object[] { key, operationContext });
                }
            }
            finally
            { 
                KeyLocker.ReleaseWriterLock(key);
    
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Remove_1", "exit");

            }
            return entry;
        }

        /// <summary>
        /// Removes the key and pairs from the cache. The keys are specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="keys">key of the entries.</param>
        /// <param name="ir"></param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <param name="operationContext"></param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <returns>removed keys list</returns>
        public override Hashtable Remove(IList keys, ItemRemoveReason ir, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemoveBlk", "enter");

            Hashtable result = null;
            object[] successfulKeys = null;
            bool requiresReplication = false;

            requiresReplication = _context.CacheImpl.RequiresReplication;

            try
            {
                result = Internal.Remove(keys, ir, notify, operationContext);

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
                            // we generate a unique key to be passed to async replicator because
                            //it is required by the replicator and we do not want this operation 2 be overriden
                            //in optimized queue.
                            string uniqueKey = System.Guid.NewGuid().ToString() + keys[0];
                            _context.CacheImpl.EnqueueForReplication(null, (int)Alachisoft.NCache.Caching.Topologies.Clustered.ClusterCacheBase.OpCodes.RemoveRange, new object[] { keys, ir, operationContext });
                        }
                    }
                }
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemoveBlk", "exit");

            }
            return result;
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
        /// <param name="ir"></param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <param name="isUserOperation"></param>
        /// <param name="lockId"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <returns>item value</returns>
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, bool isUserOperation, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry entry = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Remove_5", "enter");
            KeyLocker.GetWriterLock(key);

            try
            {

                entry = Internal.Remove(key, ir, notify, isUserOperation, lockId, accessType, operationContext);
            }
            finally
            { 
                KeyLocker.ReleaseWriterLock(key);
    
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Remove_5", "exit");

            }
            return entry;
        }


        public override QueryResultSet Search(string query, IDictionary values, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "enter");

            try
            {
                return Internal.Search(query, values, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "exit");

            }
        }

        public override QueryResultSet SearchEntries(string query, IDictionary values, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.SearchEnt", "enter");

            try
            {
                return Internal.SearchEntries(query, values, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.SearchEnt", "exit");

            }
        }

        #region ------------- Data Reader -----------------


        public override ReaderResultSet Local_ExecuteReader(string query, IDictionary values, bool getData, int chunkSize, bool isInproc, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.ExeReader", "enter");

            try
            {
                return Internal.Local_ExecuteReader(query, values, getData, chunkSize, isInproc, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.ExeReader", "exit");

            }
        }


        public override ReaderResultSet GetReaderChunk(string readerId, int nextIndex, bool isInproc, OperationContext operationContext)
        {
            return Internal.GetReaderChunk(readerId, nextIndex, isInproc, operationContext);
        }
        public override void DisposeReader(string readerId, OperationContext context)
        {
            
            Internal.DisposeReader(readerId, context);
            
        }
        public override void DeclaredDeadClients(ArrayList clients)
        {
            InternalCache.DeclaredDeadClients(clients);
        }


        #endregion


        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        /// <returns>IDictionaryEnumerator enumerator.</returns>
        public override IDictionaryEnumerator GetEnumerator()
        {
            try
            {
                return Internal.GetEnumerator();
            }
            finally
            {
            }
        }

        public override EnumerationDataChunk GetNextChunk(EnumerationPointer pointer, OperationContext operationContext)
        {
            try
            {
                return Internal.GetNextChunk(pointer, operationContext);
            }
            finally
            {
            }
        }

        public override bool HasEnumerationPointer(EnumerationPointer pointer)
        {
            try
            {
                return Internal.HasEnumerationPointer(pointer);
            }
            finally
            {
            }
        }

        #endregion

        #region/            --- Key based notification registration ---           /

        public override void RegisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            KeyLocker.GetReaderLock(key);
            try
            {

                Internal.RegisterKeyNotification(key, updateCallback, removeCallback, operationContext);

                if (_context.CacheImpl.RequiresReplication)
                {
                    string uniqueKey = System.Guid.NewGuid().ToString() + key;
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.RegisterKeyNotification, new object[] { key, updateCallback, removeCallback, operationContext });
                }
            }
            finally
            { 
                KeyLocker.ReleaseReaderLock(key);
    
            }
        }

        public override void RegisterKeyNotification(string[] keys, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        {

            bool mirrorReplication = _context.CacheImpl.RequiresReplication;
            Internal.RegisterKeyNotification(keys, updateCallback, removeCallback, operationContext);

            if (mirrorReplication)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    _context.CacheImpl.EnqueueForReplication(null,(int) ClusterCacheBase.OpCodes.RegisterKeyNotification,new object[] {keys[i], updateCallback, removeCallback, operationContext});
                }
            }

        }

        public override void UnregisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {

            KeyLocker.GetReaderLock(key);
            try
            {
                Internal.UnregisterKeyNotification(key, updateCallback, removeCallback, operationContext);

                if (_context.CacheImpl.RequiresReplication)
                {
                    string uniqueKey = System.Guid.NewGuid().ToString() + key;
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UnregisterKeyNotification, new object[] { key, updateCallback, removeCallback, operationContext });
                }
            }
            finally
            { 
                KeyLocker.ReleaseReaderLock(key);
            }
        }

        public override void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            bool mirrorReplication = _context.CacheImpl.RequiresReplication;
            Internal.UnregisterKeyNotification(keys, updateCallback, removeCallback, operationContext);

            if (mirrorReplication)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    _context.CacheImpl.EnqueueForReplication(null, (int)ClusterCacheBase.OpCodes.UnregisterKeyNotification, new object[] { keys[i], updateCallback, removeCallback, operationContext });
                }
            }
        }

        #endregion


    }
}
