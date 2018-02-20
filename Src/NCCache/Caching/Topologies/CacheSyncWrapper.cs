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
using System.Collections.Generic;
using System.Threading;

using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.Queries.Continuous;
using Alachisoft.NCache.Caching.Statistics;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
#endif
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;

using Alachisoft.NCache.MapReduce;

using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Common.Queries;

using Alachisoft.NCache.Caching.Messaging;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
#endif
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

        /// <summary> AsyncBridgeReplicator forwards operation to the bridge. As it
        /// is inside the wrapper therefore all the operation will be queued in right sequence.
        /// </summary>


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

        internal override ContinuousQueryStateInfo GetContinuousQueryStateInfo()
        {
            return _cache.GetContinuousQueryStateInfo();
        }

        internal override IList<PredicateHolder> GetContinuousQueryRegisteredPredicates(string type)
        {

            return _cache.GetContinuousQueryRegisteredPredicates(type);
        }



        public override ActiveQueryAnalyzer QueryAnalyzer
        {
            get
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.QueryAnalyzer", "enter");
                try
                {
                    return Internal.QueryAnalyzer;
                }
                finally
                {
                    if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.QueryAnalyzer", "exit");
                }

            }
        }

        // For MapReduce.
        public override Array Keys
        {
            get
            {
                return Internal.Keys;
            }
        }

        public override object TaskOperationReceived(MapReduceOperation operation)
        {
            if (ServerMonitor.MonitorActivity)
            {
                ServerMonitor.LogClientActivity("CacheSyncWrp.MapReduceOperationReceived", "enter");
            }

            try
            {
                return Internal.TaskOperationReceived(operation);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity)
                {
                    ServerMonitor.LogClientActivity("CacheSyncWrp.MapReduceOperationReceived", "exit");
                }
            }
        }

        #region /                   --- Hashed cache related ---        /

        public override void GetKeyList(int bucketId, bool startLogging, out ClusteredArrayList keyList)
        {

            _cache.GetKeyList(bucketId, startLogging, out keyList);

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

        public override Hashtable GetLogTable(ArrayList bucketIds, ref bool isLoggingStopped, OPLogType type = OPLogType.Cache)
        {
            Sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _cache.GetLogTable(bucketIds, ref isLoggingStopped, type);
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

        public override void AddLoggedData(ArrayList bucketIds, OPLogType type = OPLogType.Cache)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _cache.AddLoggedData(bucketIds, type);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        public override void UpdateLocalBuckets(ArrayList bucketIds)
        {
            try
            {
                _cache.UpdateLocalBuckets(bucketIds);
            }
            finally
            {

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
            _cache.StartLogging(bucketId);
        }

        public override void StartBucketFilteration(int bucketID, FilterType type)
        {
            _cache.StartBucketFilteration(bucketID, type);
        }

        public override void StopBucketFilteration(IList<Int32> buckets, FilterType type)
        {
            _cache.StopBucketFilteration(buckets, type);
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
                //  Sync.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    Internal.MaxSize = value;
                }
                finally
                {
                    //Sync.ReleaseWriterLock();
                }
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
        public override void Clear(CallbackEntry cbEntry, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
        {
            Clear(cbEntry, updateOptions, null, operationContext);
        }

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public override void Clear(CallbackEntry cbEntry, DataSourceUpdateOptions updateOptions, string taskId, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Clear", "enter");

            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                Internal.Clear(cbEntry, updateOptions, operationContext);

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

            }
            finally
            {
                KeyLocker.ReleaseReaderLock(key);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Unlock", "exit");

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

        public override CacheEntry Get(object key, bool IsUserOperation, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry entry = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Get_2", "enter");
            KeyLocker.GetReaderLock(key);
            try
            {
                entry = Internal.Get(key, IsUserOperation, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);

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
        /// <returns>cache entry.</returns>
        public override CacheEntry Get(object key, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType access, OperationContext operationContext)
        {
            CacheEntry entry = null;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Get_2", "enter");
            KeyLocker.GetReaderLock(key);
            try
            {
                entry = Internal.Get(key, ref version, ref lockId, ref lockDate, lockExpiration, access, operationContext);

            }
            finally
            {
                KeyLocker.ReleaseReaderLock(key);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Get_2", "exit");

            }
            return entry;
        }

        /// <summary>
        /// Retrieve the objects from the cache. An array of keys is passed as parameter.
        /// </summary>
        /// <param name="key">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        public override IDictionary Get(object[] keys, OperationContext operationContext)
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
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetGrp", "enter");
            KeyLocker.GetReaderLock(key);

            try
            {
                entry = Internal.GetGroup(key, group, subGroup, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);

            }
            finally
            {
                KeyLocker.ReleaseReaderLock(key);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetGrp", "exit");

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
            KeyLocker.GetReaderLock(key);
            try
            {
                return Internal.GetGroupInfo(key, operationContext);
            }
            finally
            {
                KeyLocker.ReleaseReaderLock(key);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetGrpInf", "exit");

            }
        }

        /// <summary>
        /// Gets the data groups of the items.
        /// </summary>
        /// <param name="keys">Keys of the items</param>
        /// <returns>Hashtable containing key of the item as 'key' and GroupInfo as 'value'</returns>
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
            return Add(key, cacheEntry, notify, null, operationContext);
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

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_1", "enter");

            KeyLocker.GetWriterLock(key);
            try
            {

                CacheAddResult result = Internal.Add(key, cacheEntry, notify, operationContext);
                return result;
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(key);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_1", "exit");

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

            KeyLocker.GetWriterLock(key);
            try
            {
                depAdded = Internal.Add(key, eh, operationContext);

            }
            finally
            {
                KeyLocker.ReleaseWriterLock(key);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_3", "exit");

            }
            return depAdded;
        }

        public override bool Add(object key, CacheSynchronization.CacheSyncDependency syncDependency, OperationContext operationContext)
        {
            bool depAdded = false;

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Add_4", "enter");

            KeyLocker.GetWriterLock(key);
            try
            {
                depAdded = Internal.Add(key, syncDependency, operationContext);
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(key);
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
            return Add(keys, cacheEntries, notify, null, operationContext);
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

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.AddBlk", "enter");
            try
            {
                result = Internal.Add(keys, cacheEntries, notify, operationContext);

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
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            return Insert(key, cacheEntry, notify, null, lockId, version, accessType, operationContext);
        }

        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, string taskId, object lockId, ulong version, LockAccessType access, OperationContext operationContext)
        {

            
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Insert_1", "enter");

            KeyLocker.GetWriterLock(key);

            try
            {

                CacheInsResultWithEntry result = Internal.Insert(key, cacheEntry, notify, lockId, version, access, operationContext);

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
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, bool IsUserOperation, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Insert_2", "enter");

            KeyLocker.GetWriterLock(key);

            try
            {

                CacheInsResultWithEntry result = Internal.Insert(key, cacheEntry, notify, IsUserOperation, lockId, version, accessType, operationContext);
                return result;
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(key);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Insert_2", "exit");

            }
        }

        public override Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            return Insert(keys, cacheEntries, notify, null, operationContext);
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

   

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.InsertBlk", "enter");

            try
            {
                result = Internal.Insert(keys, cacheEntries, notify, operationContext);

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
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Remove_1", "enter");


            KeyLocker.GetWriterLock(key);
            try
            {


                entry = Internal.Remove(key, ir, notify, lockId, version, accessType, operationContext);
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(key);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Remove_1", "exit");

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
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemoveBlk", "enter");

            Hashtable result = null;
            try
            {

                result = Internal.Remove(keys, ir, notify, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemoveBlk", "exit");

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
                                entriesTable = Remove(nextRemovalKeys, ir, notify, operationContext);//_context.CacheImpl.Remove(nextRemovalKeys, ItemRemoveReason.DependencyChanged, true);
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
            KeyLocker.GetWriterLock(key);
            try
            {
                entry = Internal.Remove(key, ir, notify, isUserOperation, lockId, version, accessType, operationContext);
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(key);
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

        public override ReaderResultSet Local_ExecuteReaderCQ(string query, IDictionary values, bool getData, int chunkSize, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters, bool isInproc)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.ExeReaderCQ", "enter");

            try
            {
                return Internal.Local_ExecuteReaderCQ(query, values, getData, chunkSize, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters, isInproc);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.ExeReaderCQ", "exit");

            }
        }

        public override ReaderResultSet Local_ExecuteReaderCQ(ContinuousQuery query, bool getData, int chunkSize, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.ExeReaderCQ", "enter");

            try
            {
                return Internal.Local_ExecuteReaderCQ(query, getData, chunkSize, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.ExeReaderCQ", "exit");

            }
        }

        public override ReaderResultSet Local_ExecuteReaderCQ(string queryId, bool getData, int chunkSize, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.ExeReaderCQ", "enter");

            try
            {
                return Internal.Local_ExecuteReaderCQ(queryId, getData, chunkSize, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.ExeReaderCQ", "exit");

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

        public override QueryResultSet SearchCQ(ContinuousQuery query, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "enter");

            try
            {
                return Internal.SearchCQ(query, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "exit");
            }
        }

        public override QueryResultSet SearchCQ(string query, IDictionary values, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "enter");

            try
            {
                return Internal.SearchCQ(query, values, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "exit");
            }
        }

        public override QueryResultSet SearchCQ(string queryId, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "enter");

            try
            {
                return Internal.SearchCQ(queryId, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "exit");
            }
        }

        public override QueryResultSet SearchEntriesCQ(string queryId, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "enter");

            try
            {
                return Internal.SearchEntriesCQ(queryId, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "exit");
            }
        }

        public override QueryResultSet SearchEntriesCQ(string query, IDictionary values, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "enter");

            try
            {
                return Internal.SearchEntriesCQ(query, values, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "exit");
            }
        }

        public override QueryResultSet SearchEntriesCQ(ContinuousQuery query, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "enter");

            try
            {
                return Internal.SearchEntriesCQ(query, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.Search", "exit");
            }
        }

        public override DeleteQueryResultSet DeleteQuery(string query, IDictionary values, bool notify, bool isUserOperation, ItemRemoveReason reason, OperationContext operationContext)
        {
            DeleteQueryResultSet result = new Queries.DeleteQueryResultSet();

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.DeleteQueryEnt", "enter");

            try
            {
                result = Internal.DeleteQuery(query, values, notify, isUserOperation, reason, operationContext);

                result.KeysEffected = null;
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.DeleteQueryEnt", "exit");

            }
            return result;
        }

        /// <summary>
        /// Broadcasts a user-defined event across the cluster.
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="data"></param>
        /// <param name="async"></param>
        public override void SendNotification(object notifId, object data)
        {
            Internal.SendNotification(notifId, data);
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

            {
                KeyLocker.GetWriterLock(key);
                try
                {

                    Internal.RegisterKeyNotification(key, updateCallback, removeCallback, operationContext);

                }
                finally
                {
                    KeyLocker.ReleaseWriterLock(key);
                }

            }
        }

        public override void RegisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {

            Internal.RegisterKeyNotification(keys, updateCallback, removeCallback, operationContext);

        }

        public override void UnregisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {

            {
                KeyLocker.GetWriterLock(key);
                try
                {

                    Internal.UnregisterKeyNotification(key, updateCallback, removeCallback, operationContext);
                }
                finally
                {
                    KeyLocker.ReleaseWriterLock(key);
                }

            }
        }

        public override void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        {


            Internal.UnregisterKeyNotification(keys, updateCallback, removeCallback, operationContext);
        }

        #endregion

        #region /          --- Cascaded Key dependencies Opertions ---            /
        public override Hashtable AddDepKeyList(Hashtable table, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.AddDepKey", "enter");

            Hashtable result = null;
            try
            {
                result = Internal.AddDepKeyList(table, operationContext);

            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.AddDepKey", "exit");

            }
            return result;
        }

        public override Hashtable RemoveDepKeyList(Hashtable table, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemDepKey", "enter");
            try
            {
                return Internal.RemoveDepKeyList(table, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemDepKey", "exit");
            }
        }
        #endregion

        #region/                    --- Stream ---                               /

        public override bool OpenStream(string key, string lockHandle, StreamModes mode, string group, string subGroup, ExpirationHint hint, EvictionHint evictinHint, OperationContext operationContext)
        {
            bool lockAcquired = false;

            KeyLocker.GetReaderLock(key);
            try
            {


                lockAcquired = InternalCache.OpenStream(key, lockHandle, mode, group, subGroup, hint, evictinHint, operationContext);

            }
            finally
            {
                KeyLocker.ReleaseReaderLock(key);
            }
            return lockAcquired;
        }

        public override void CloseStream(string key, string lockHandle, OperationContext operationContext)
        {


            KeyLocker.GetReaderLock(key);
            try
            {
                InternalCache.CloseStream(key, lockHandle, operationContext);

            }
            finally
            {
                KeyLocker.ReleaseReaderLock(key);
            }
        }

        public override int ReadFromStream(ref VirtualArray vBuffer, string key, string lockHandle, int offset, int length, OperationContext operationContext)
        {
            return InternalCache.ReadFromStream(ref vBuffer, key, lockHandle, offset, length, operationContext);
        }

        public override void WriteToStream(string key, string lockHandle, VirtualArray vBuffer, int srcOffset, int dstOffset, int length, OperationContext operationContext)
        {

            KeyLocker.GetWriterLock(key);

            try
            {

                InternalCache.WriteToStream(key, lockHandle, vBuffer, srcOffset, dstOffset, length, operationContext);

            }
            finally
            {  
                KeyLocker.ReleaseWriterLock(key);
            }
        }

        public override long GetStreamLength(string key, string lockHandle, OperationContext operationContext)
        {
            return InternalCache.GetStreamLength(key, lockHandle, operationContext);
        }
        #endregion

        #region/             --- ContinuousQuery  ---                            /

        public override void UnRegisterCQ(string serverUniqueId, string clientUniqueId, string clientId)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.UnRegisterCQ", "enter");
            try
            {
                Internal.UnRegisterCQ(serverUniqueId, clientUniqueId, clientId);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.UnRegisterCQ", "exit");
            }
        }

        public override void UnRegisterCQ(string queryId)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.UnRegisterCQ", "enter");
            try
            {
                Internal.UnRegisterCQ(queryId);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.UnRegisterCQ", "exit");
            }
        }

        public override void RegisterCQ(ContinuousQuery query, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RegisterCQ", "enter");
            try
            {
                Internal.RegisterCQ(query, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RegisterCQ", "exit");
            }
        }

        public override string RegisterCQ(string query, IDictionary values, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RegisterCQ", "enter");
            try
            {
                return Internal.RegisterCQ(query, values, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RegisterCQ", "exit");
            }
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

        public override MessageInfo GetNextUndeliveredMessage(TimeSpan timeout, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetNextUndeliveredMessage", "enter");

            MessageInfo result = null;

            try
            {
                result = Internal.GetNextUndeliveredMessage(timeout, context);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetNextUndeliveredMessage", "exit");

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

            try
            {
                KeyLocker.GetWriterLock(message.MessageId);
                Internal.RevokeAssignment(message,subscription,context);
            }
            finally
            {
                KeyLocker.ReleaseWriterLock(message.MessageId);
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

        public override SubscriptionInfo GetSubscriber(string topic, SubscriptionType type, OperationContext context)
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

        public override IDictionary<string, IList<object>> GetAssignedMessage(SubscriptionInfo subscriptionInfo, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.GetAssignedMessage", "enter");

            IDictionary<string, IList<object>> result = null;

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
            try
            {
                KeyLocker.GetWriterLock(message.MessageId);

                 stored = Internal.StoreMessage(topic, message, context);

            }
            finally
            {
                KeyLocker.ReleaseWriterLock(message.MessageId);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.StoreMessage", "exit");
            }
            return stored;
        }


        public override bool AssignmentOperation(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo, TopicOperationType type, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.AssignmentOperation", "enter");

            bool result = false;

            try
            {
                KeyLocker.GetWriterLock(messageInfo.MessageId);

                result = Internal.AssignmentOperation(messageInfo, subscriptionInfo, type,context);

            }
            finally
            {
                KeyLocker.ReleaseWriterLock(messageInfo.MessageId);
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

            }
            finally
            {
                if (ServerMonitor.MonitorActivity)
                    ServerMonitor.LogClientActivity("CacheSyncWrp.AcknowledgeMessageReceipt", "exit");

            }
        }

        public override void RemoveMessages(IList<MessageInfo> messagesTobeRemoved, MessageRemovedReason reason, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CacheSyncWrp.RemoveMessages", "enter");

            try
            {
                Internal.RemoveMessages(messagesTobeRemoved, reason,context);

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

        public override OrderedDictionary GetMessageList(int bucketId)
        {
            return Internal.GetMessageList(bucketId);
        }

        public override TransferrableMessage GetTransferrableMessage(string topic, string messageId)
        {
            try
            {
                _keyLockManager.GetReaderLock(messageId);
                return Internal.GetTransferrableMessage(topic, messageId);
            }
            finally
            {
                _keyLockManager.ReleaseReaderLock(messageId);
            }
        }

        public override bool StoreTransferrableMessage(string topic, TransferrableMessage message)
        {
            try
            {
                _keyLockManager.GetWriterLock(message.Message.MessageId);
                return Internal.StoreTransferrableMessage(topic, message);
            }
            finally
            {
                _keyLockManager.ReleaseWriterLock(message.Message.MessageId);
            }
        }

        public override ArrayList GetTopicsState()
        {
            return Internal.GetTopicsState();
        }

        public override void SetTopicsState(ArrayList topicState)
        {
            Internal.SetTopicsState(topicState);
        }
        #endregion


        public override void ClientDisconnected(string client, bool isInproc)
        {
            if (Internal != null)
            {
                Internal.ClientDisconnected(client, isInproc);
            }
        }

        public override Dictionary<string, TopicStats> GetTopicsStats()
        {
            return Internal.GetTopicsStats();
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
    }
}
