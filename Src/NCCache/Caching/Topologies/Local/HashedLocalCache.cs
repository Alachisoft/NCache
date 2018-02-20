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

using System.Collections;
using System.Threading;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Common.Queries.Filters;
using Alachisoft.NCache.Caching.Queries.Filters;
using Alachisoft.NCache.Common.Queries;
using System.Collections.Generic;

using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    class HashedLocalCache : IndexedLocalCache,ISizableIndex
    {
        private int _bucketSize;

        /// <summary>
        /// A map that contains key lists against each bucket id.
        /// </summary>
        /// 
        private HashVector _keyList;
        private int _stopLoggingThreshhold = 50;
        private long _inMemorySize = 0;
        private OpLogManager _logMgr;
        private BucketBasedKeyFilter _bucketFilter;
        private object _filterMutex = new object();
        private CompoundKeyFilter _compoundFilter;
#if NET40
        private readonly SemaphoreLock _keyListLock = new SemaphoreLock();
#else
        private readonly object _keyListLock = new object();
#endif
        private object _bucketStatsMutex = new object();
        //This lock is intended to synchronize user operations and state transfer. State transfer operations can only take WRITER lock.
        private ReaderWriterLockSlim _rwBucketsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public HashedLocalCache(IDictionary cacheClasses, CacheBase parentCache, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, bool logEntries, ActiveQueryAnalyzer activeQueryAnalyzer)
            : base(cacheClasses, parentCache, properties, listener, context, activeQueryAnalyzer)
        {
            _logMgr = new OpLogManager(logEntries, context);
            _stats.LocalBuckets = new HashVector(1000);
            _cacheStore.ISizableBucketIndexManager = this;
        }

        public override int BucketSize
        {
            set 
            {
                _bucketSize = value;
                FilterHelper.BucketSize = _bucketSize;
            }
        }

        public override HashVector LocalBuckets
        {
            get { return _stats.LocalBuckets; }
        }

        public override void GetKeyList(int bucketId, bool startLogging, out ClusteredArrayList keyList)
        {
            try
            {
                _rwBucketsLock.EnterWriteLock();
                if (startLogging)
                    _logMgr.StartLogging(bucketId, LogMode.LogBeforeAfterActualOperation);

                keyList = new ClusteredArrayList();
                if (_keyList != null)
                {
                    if (_keyList.Contains(bucketId))
                    {
                        BucketStatistcs bucketStats = _keyList[bucketId] as BucketStatistcs;
                        keyList = bucketStats.GetCacheKeys();
                    }
                }
            }
            finally
            {
                _rwBucketsLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Starts logging on the specified bucket. All operation are logged
        /// untill we explicitly stop logging. Logging operations helps synchronize
        /// the state of the cluste during the state transfer of a bucket.
        /// </summary>
        /// <param name="bucketId"></param>
        public override void StartLogging(int bucketId)
        {
             try
            {
                _rwBucketsLock.EnterWriteLock();
            if (_logMgr != null)
            {
                _logMgr.StartLogging(bucketId, LogMode.LogBeforeActualOperation);              
            }
            }
             finally
             {
                 _rwBucketsLock.ExitWriteLock();
             }
        }

        public override void StartBucketFilteration(int bucketID, FilterType type)        
        {
            lock (_filterMutex)
            {
                switch (type)
                {
                    case FilterType.BucketFilter:

                        _bucketFilter = new BucketBasedKeyFilter(bucketID);
                        break;
                    case FilterType.CompoundFilter:
                        {                            
                            if (_compoundFilter == null) _compoundFilter = new CompoundKeyFilter();
                            _compoundFilter.FilterBucket(bucketID);                           
                        }
                        break;
                }            
            }
        }

        public override void StopBucketFilteration(IList<Int32> buckets, FilterType type)
        {
            lock (_filterMutex)
            {
                switch (type)
                {
                    case FilterType.BucketFilter:
                        {
                            _bucketFilter = null;
                        }
                        break;
                    case FilterType.CompoundFilter:
                        {
                            if (_compoundFilter != null && buckets!=null)
                            {
                                foreach (var b in buckets)
                                {
                                    _compoundFilter.RemoveBucket(b);
                                }

                                if (_compoundFilter.IsEmpty)
                                {
                                    _compoundFilter = null;
                                }
                            }                          
                        }
                        break;
                }
            }
        }


        public override IKeyFilter GetBucketFilter()
        {
            lock(_filterMutex)
                return _bucketFilter;
        }

        public override IKeyFilter GetCompoundFilter()
        {
            lock (_filterMutex)
                return _compoundFilter;
        }

        public override Hashtable GetLogTable(ArrayList bucketIds, ref bool isLoggingStopped, OPLogType type = OPLogType.Cache)
        {
            Hashtable result = null;
            int logCount = 0;
            IEnumerator ie = bucketIds.GetEnumerator();
            while (ie.MoveNext())
            {
                Hashtable tmp = _logMgr.GetLogTable((int)ie.Current);
                if (tmp != null)
                {
                    if (result == null)
                        result = tmp;
                    else
                    {
                        ArrayList removed = tmp["removed"] as ArrayList;
                        ArrayList updated = tmp["updated"] as ArrayList;

                        if (removed != null)
                        {
                            ((ArrayList)result["removed"]).AddRange(removed);
                            logCount += removed.Count;
                        }

                        if (updated != null)
                        {
                            ((ArrayList)result["updated"]).AddRange(updated);
                            logCount += updated.Count;
                        }
                    }
                }
            }
            if (logCount < _stopLoggingThreshhold)
            {
                isLoggingStopped = true;
                _logMgr.StopLogging(bucketIds);
            }
            else
                isLoggingStopped = false;

            return result;
        }

        private void IncrementBucketStats(string cacheKeyOrMessageId, int bucketId, long dataSize,bool isMessage,string topic)
        {
            lock (_bucketStatsMutex)
            {
                if (_stats.LocalBuckets != null && _stats.LocalBuckets.Contains(bucketId))
                {
                    if(isMessage)
                        ((BucketStatistics)_stats.LocalBuckets[bucketId]).IncrementTopicStats(topic,dataSize);
                    else
                        ((BucketStatistics)_stats.LocalBuckets[bucketId]).Increment(dataSize);
                } 
            }

            if (_keyList == null)
            {
                _keyList = new HashVector();
            }
            if (_keyList != null)
            {
                HashVector keys;
                BucketStatistcs bucketStats = null;
                bool isKeyExist = false;

#if NET40
                try
                {
                    _keyListLock.Enter();

#else
                lock (_keyListLock)
                {
#endif
                    if (!_keyList.Contains(bucketId))
                    {
                        bucketStats = new BucketStatistcs(bucketId);
                        _keyList[bucketId] = bucketStats;

                        if (isMessage)
                            bucketStats.AddMessage(topic, cacheKeyOrMessageId);
                        else
                            bucketStats.AddCacheKey(cacheKeyOrMessageId);

                        _inMemorySize += bucketStats.InMemorySize;
                        isKeyExist = true;

                    }
                    else
                    {


                    }
#if NET40
                }
                finally
                {
                    _keyListLock.Exit();
                }
#else
                }
#endif               
                if (isKeyExist) return;

                bucketStats = (BucketStatistcs)_keyList[bucketId];
                long oldSize = bucketStats.InMemorySize;

                lock (bucketStats)
                {
                    if (isMessage)
                        bucketStats.AddMessage(topic, cacheKeyOrMessageId);
                    else
                        bucketStats.AddCacheKey(cacheKeyOrMessageId);
                }

                long newSize = bucketStats.InMemorySize;
                long tmpsize = _inMemorySize;

                _inMemorySize = tmpsize + (newSize - oldSize);

            }
        }

        private void DecrementBucketStats(string cacheKeyOrMessageId, int bucketId, long dataSize,bool isMessage,string topic)
        {
            lock (_bucketStatsMutex)
            {
                if (_stats.LocalBuckets != null && _stats.LocalBuckets.Contains(bucketId))
                {
                    if(isMessage)
                        ((BucketStatistics)_stats.LocalBuckets[bucketId]).DecrementTopicStats(topic,dataSize);
                    else
                        ((BucketStatistics)_stats.LocalBuckets[bucketId]).Decrement(dataSize);
                } 
            }
            bool isKeysEmpty= false;
            HashVector keys = null;
            BucketStatistcs bucketStats = null;
            if (_keyList != null)
            {
#if NET40
                try
                {
                    _keyListLock.Enter();
#else
                lock(_keyListLock)
                {
#endif


                    if (_keyList.Contains(bucketId))
                    {
                        bucketStats = (BucketStatistcs)_keyList[bucketId];
                    }
#if NET40
                }
                finally
                {
                    _keyListLock.Exit();
                }
#else
                }
#endif
                if (bucketStats != null)
                {
                    long oldSize = bucketStats.InMemorySize;
                    long newSize = 0;
                    
                    lock (bucketStats)
                    {
                        if (isMessage)
                            bucketStats.RemoveMessage(topic, cacheKeyOrMessageId);
                        else
                            bucketStats.RemoveCacheKey(cacheKeyOrMessageId);

                        newSize = bucketStats.InMemorySize;
                    }
                    long currentsize = _inMemorySize;

                    _inMemorySize = currentsize + (newSize - oldSize);

                    if (isKeysEmpty)
                    {
#if NET40
                        _keyListLock.Enter();
#else
                        lock (_keyListLock)
                        {
#endif
                            _keyList.Remove(bucketId);

#if NET40
                             _keyListLock.Exit();
#else
                        }
#endif
                           
                        
                    }
                    if (_inMemorySize < 0) _inMemorySize = 0;
                }
            }
        }

        private void RemoveTopicStats(string topic)
        {
            lock (_bucketStatsMutex)
            {
                if (_stats.LocalBuckets != null)
                {
                    foreach (DictionaryEntry stats in _stats.LocalBuckets)
                    {
                        ((BucketStatistics)stats.Value).RemoveTopic(topic);
                    }
                }
            }

            BucketStatistcs bucketStats = null;
            if (_keyList != null)
            {
#if NET40
                try
                {
                    _keyListLock.Enter();
#else
                    lock(_keyListLock)
                    {
#endif
                    foreach (DictionaryEntry pair in _keyList)
                    {
                        bucketStats = pair.Value as BucketStatistcs;
                        long oldSize = bucketStats.InMemorySize;

                        bucketStats.RemoveTopic(topic);

                        long newSize = bucketStats.InMemorySize;
                        _inMemorySize = _inMemorySize + (newSize - oldSize);
                    }

#if NET40
                }
                finally
                {
                    _keyListLock.Exit();
                }
#else
                    }
#endif
                if (_inMemorySize < 0) _inMemorySize = 0;
            }
        
        }

        private int GetBucketId(string key)
        { 
            int hashCode = AppUtil.GetHashCode(key);
            int bucketId = hashCode / _bucketSize;

            if (bucketId < 0)
                bucketId *= -1;
            return bucketId;
        }

        public override void RemoveFromLogTbl(int bucketId)
        {
            if (_logMgr != null)
                _logMgr.RemoveLogger(bucketId);
        }

        public override void AddLoggedData(ArrayList bucketIds, OPLogType type= OPLogType.Cache)
        {
             AddLoggedEntriesInternal(bucketIds);
        }

        private void AddLoggedEntriesInternal(ArrayList bucketIds)
            {
            if (bucketIds != null)
            {
                IEnumerator ie = bucketIds.GetEnumerator();
                while (ie.MoveNext())
                {
                    if (_logMgr != null)
                    {
                        Hashtable loggedEnteries = _logMgr.GetLoggedEnteries((int)ie.Current);
                        if (loggedEnteries != null && loggedEnteries.Count > 0)
                        {
                            Hashtable removed = loggedEnteries["removed"] as Hashtable;
                            Hashtable updated = loggedEnteries["updated"] as Hashtable;
                            ArrayList messageOperatons = loggedEnteries["messageops"] as ArrayList;

                            if (removed != null && removed.Count > 0)
                            {
                                IDictionaryEnumerator ide = removed.GetEnumerator();
                                while (ide.MoveNext())
                                {
                                    //nTrace.error("HashedLocalCache.AddLoggedData", "Removing Logged Key: " + ide.Key);
                                    Remove(ide.Key, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                                }
                            }

                            if (updated != null && updated.Count > 0)
                            {
                                IDictionaryEnumerator ide = updated.GetEnumerator();
                                while (ide.MoveNext())
                                {
                                    //nTrace.error("HashedLocalCache.AddLoggedData", "Adding Logged Key: " + ide.Key);
                                    CacheEntry entry = ide.Value as CacheEntry;
                                    if (entry != null)
                                    {
                                        Add(ide.Key, entry, false, false, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                                    }
                                }
                            }

                            if (messageOperatons != null)
                            {
                                foreach (object operation in messageOperatons)
                                {
                                    try
                                    {
                                        ((Clustered.ClusterCacheBase)_context.CacheImpl).ApplyMessageOperation(operation);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                        }
                         
                        //disable logging for this bucket...
                        _logMgr.RemoveLogger((int)ie.Current);
                    }
                }
            }
        }

        public override void RemoveBucketData(int bucketId)
        {
            try
            {
                _rwBucketsLock.EnterWriteLock();

                ClusteredArrayList keys;
                GetKeyList(bucketId, false, out keys);
                if (keys != null)
                {
                    keys = keys.Clone() as ClusteredArrayList;
                    IEnumerator ie = keys.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        if (ie.Current != null)
                        {
                            Remove(ie.Current, ItemRemoveReason.Removed, false, false, null, 0, LockAccessType.IGNORE_LOCK, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                        }
                    }

                }

                //remove messages
                OrderedDictionary topicWiseMessages = GetMessageList(bucketId);
                MessageInfo message = new MessageInfo();
                OperationContext context = OperationContext.CreateWith(OperationContextFieldName.InternalOperation, true);

                foreach (DictionaryEntry entry in topicWiseMessages)
                {
                    message.Topic = entry.Key as string;

                    foreach (string messageId in entry.Value as ClusteredArrayList)
                    {
                        message.MessageId = messageId;
                        RemoveMessagesInternal(message, MessageRemovedReason.Delivered, context);
                    }
                }
            }
            finally
            {
                _rwBucketsLock.ExitWriteLock();
            }
        }

        public override void RemoveBucket(int bucket)
        {
            try
            {
                _rwBucketsLock.EnterWriteLock();
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("HashedCache.RemoveBucket", "removing bucket :" + bucket);
                //Remove from stats
                if (LocalBuckets != null)
                {
                    lock (_bucketStatsMutex)
                    {
                        LocalBuckets.Remove(bucket);
                    }
                }

                //Remove actual data of the bucket
                RemoveBucketData(bucket);
                //remove operation logger for the bucket from log table if any exists.
                RemoveFromLogTbl(bucket);
            }
            finally
            {
                _rwBucketsLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bucketIds"></param>
        public override void RemoveExtraBuckets(ArrayList myBuckets)
        {
            for (int i = 0; i < 1000; i++)
            {
                if (!myBuckets.Contains(i))
                {
                    RemoveBucket(i);
                }
            }
        }

        public override void UpdateLocalBuckets(ArrayList bucketIds)
        {
            IEnumerator ie = bucketIds.GetEnumerator();
            while (ie.MoveNext())
            {
                if (LocalBuckets == null)
                    LocalBuckets = new HashVector();

                lock (_bucketStatsMutex)
                {
                    if (!LocalBuckets.Contains(ie.Current))
                    {
                        LocalBuckets[ie.Current] = new BucketStatistics();
                    } 
                }
            }
        }

        #region	/                 --- Cache overrides ---           /

        internal override CacheAddResult AddInternal(object key, CacheEntry cacheEntry, bool isUserOperation, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);

            try
            {
                _rwBucketsLock.EnterReadLock();
                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId))
                {
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && isUserOperation)
                    {
                        _logMgr.LogOperation(bucketId, key, cacheEntry, OperationType.Add);
                        return CacheAddResult.Success;
                    }

                    CacheEntry clone = (CacheEntry)cacheEntry.Clone();
                    CacheAddResult result = base.AddInternal(key, cacheEntry, isUserOperation, operationContext);

                    if (result == CacheAddResult.Success || result == CacheAddResult.SuccessNearEviction)
                    {
                        IncrementBucketStats(key as string, bucketId, clone.DataSize,false,null);
                        if (isUserOperation) _logMgr.LogOperation(bucketId, key, clone, OperationType.Add);
                    }

                    return result;
                }
            }
            finally
            {
                _rwBucketsLock.ExitReadLock();
            }

            throw new StateTransferException("I am no more the owner of this bucket");
        }

        internal override bool AddInternal(object key, Alachisoft.NCache.Caching.AutoExpiration.ExpirationHint eh, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);

            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId))
                return base.AddInternal(key, eh,operationContext);
            
            throw new StateTransferException("I am no more the owner of this bucket");
        }

        internal override bool RemoveInternal(object key, Alachisoft.NCache.Caching.AutoExpiration.ExpirationHint eh)
        {
            int bucketId = GetBucketId(key as string);
            
            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId))
                return base.RemoveInternal(key, eh);

            throw new StateTransferException("I am no more the owner of this bucket");
        }

        internal override bool AddInternal(object key, Alachisoft.NCache.Caching.CacheSynchronization.CacheSyncDependency syncDependency)
        {
            int bucketId = GetBucketId(key as string);
            
            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId))
                return base.AddInternal(key, syncDependency);
            
            throw new StateTransferException("I am no more the owner of this bucket");
        }

        internal override CacheInsResult InsertInternal(object key, CacheEntry cacheEntry, bool isUserOperation, CacheEntry oldEntry, OperationContext operationContext, bool updateIndex)
        {
            int bucketId = GetBucketId(key as string);
            OperationLogger opLogger = null;

            try
            {
                _rwBucketsLock.EnterReadLock();
                 
                //fetch the operation logger...
                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId))
                {
                    long oldEntrysize = oldEntry == null ? 0 : oldEntry.DataSize;

                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && isUserOperation)
                    {
                        _logMgr.LogOperation(bucketId, key, cacheEntry, OperationType.Insert);
                        return oldEntry != null ? CacheInsResult.SuccessOverwrite : CacheInsResult.Success;
                    }

                    CacheEntry clone = (CacheEntry)cacheEntry.Clone();

                    CacheInsResult result = base.InsertInternal(key, cacheEntry, isUserOperation, oldEntry, operationContext, updateIndex);

                    switch (result)
                    {
                        case CacheInsResult.SuccessNearEvicition:
                        case CacheInsResult.Success:
                            if (isUserOperation) _logMgr.LogOperation(bucketId, key, clone, OperationType.Insert);
                            IncrementBucketStats(key as string, bucketId, cacheEntry.DataSize, false, null);
                            break;

                        case CacheInsResult.SuccessOverwriteNearEviction:
                        case CacheInsResult.SuccessOverwrite:
                            if (isUserOperation) _logMgr.LogOperation(bucketId, key, clone, OperationType.Insert);
                            DecrementBucketStats(key as string, bucketId, oldEntrysize, false, null);
                            IncrementBucketStats(key as string, bucketId, cacheEntry.DataSize, false, null);
                            break;
                    }

                    return result;
                }
            }
            finally
            {
                _rwBucketsLock.ExitReadLock();
            }
            
            throw new StateTransferException("I am no more the owner of this bucket");
        }

        internal override CacheEntry RemoveInternal(object key, ItemRemoveReason removalReason, bool isUserOperation, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);
            CacheEntry entry = null;
            try
            {
                _rwBucketsLock.EnterReadLock();
                if (isUserOperation)
                {
                    if (!(_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId)))
                        throw new StateTransferException("I am no more the owner of this bucket");
                }
                if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && isUserOperation)
                {
                    CacheEntry e = GetInternal(key, isUserOperation, operationContext);
                    _logMgr.LogOperation(bucketId, key, null, OperationType.Delete);
                    return e;
                }

                entry = base.RemoveInternal(key, removalReason, isUserOperation, operationContext);
                if (entry != null)
                {
                    DecrementBucketStats(key as string, bucketId, entry.DataSize, false, null);
                    if (isUserOperation) _logMgr.LogOperation(bucketId, key, null, OperationType.Delete);
                }
            }
            finally
            {
                _rwBucketsLock.ExitReadLock();
            }
            return entry;
        }

        internal override CacheEntry GetInternal(object key, bool isUserOperation, OperationContext operationContext)
        {

            int bucketId = GetBucketId(key as string);
            if (isUserOperation)
            {
                try
                {

                    if (!_logMgr.IsOperationAllowed(bucketId))
                        throw new StateTransferException("I am no more the owner of this bucket");
                }
                catch (Exception e)
                {
                    if (e is NullReferenceException)
                    {
                        string loggingModule = _logMgr == null ? "_logMgr is null" : "_logMgr is not null";
                        _context.NCacheLog.Error("GetInternal()","Null Module: "+ loggingModule + "[] Exception is " + e);
                    }
                        throw e;
                }
            }
            CacheEntry entry = base.GetInternal(key, isUserOperation, operationContext);

            try
            {
                if (entry == null && (isUserOperation && !LocalBuckets.Contains(bucketId)))
                    throw new StateTransferException("I am no more the owner of this bucket");
            }
            catch (Exception e)
            {
                if (e is NullReferenceException)
                {
                    string loggingModule = LocalBuckets == null ? "LocalBuckets is null" : "LocalBuckets is not null";
                    _context.NCacheLog.Error("GetInternal()", "Null Module: " + loggingModule + "[] Exception is " + e);
                }
                throw e;
            }
            return entry;
        }

        internal override bool ContainsInternal(object key)
        {
            int bucketId = GetBucketId(key as string);

            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId))
                return base.ContainsInternal(key);

            throw new StateTransferException("I am no more the owner of this bucket");
        }

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        internal override void ClearInternal()
        {
            base.ClearInternal();
            if (_keyList != null) _keyList.Clear();
            _inMemorySize = 0;
            if (_logMgr != null) _logMgr.Dispose(); //it clears the operation loggers for each bucket   


            if (LocalBuckets == null)
                return;
            lock (_bucketStatsMutex)
            {
                //clear the bucket stats
                IDictionaryEnumerator ide = LocalBuckets.GetEnumerator();
                while (ide.MoveNext())
                {
                    BucketStatistics stats = ide.Value as BucketStatistics;
                    if (stats != null)
                    {
                        stats.Clear();
                    }
                } 
            }
        }

        public override void Dispose()
        {
            if (_logMgr != null) _logMgr.Dispose();
            if (_keyList != null) _keyList.Clear();
            _inMemorySize = 0;
            base.Dispose();
        }

        #endregion

        public long InMemorySize
        {
            get
            {
                if (_keyList != null)
                {
                    return _inMemorySize + _keyList.BucketCount * MemoryUtil.NetHashtableOverHead;
                }
                return 0;
            }
        }
        internal override long Size
        {
            get
            {                
                return base.Size + InMemorySize;
            }
        }

        public long IndexInMemorySize
        {
            get { return InMemorySize; }
        }

        internal override void TouchInternal(string key, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);

            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId))
                base.TouchInternal(key, operationContext);

            throw new StateTransferException("I am no more the owner of this bucket");
        }

        public override bool StoreMessage(string topic, Message message, OperationContext context)
        {
            bool stored = false;
            bool lockHeld = false;
            try
            {
                int bucketId = GetBucketId(message.MessageId as string);
                Message clone = message.Clone() as Message;
               
                if (!_rwBucketsLock.IsWriteLockHeld)
                {
                    lockHeld = true;
                   _rwBucketsLock.EnterReadLock();
                }

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId))
                {
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                    {
                        _logMgr.LogOperation(bucketId, message.MessageId, new StoreMessageOperation(topic, clone, OperationContext.CreateWith(OperationContextFieldName.InternalOperation, true)), OperationType.MessageOperation);
                        return true;
                    }

                    if (base.StoreMessage(topic, message, context))
                    {
                        stored = true;
                        IncrementBucketStats(message.MessageId, bucketId, message.Size, true, topic);

                        if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeAfterActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                            _logMgr.LogOperation(bucketId, message.MessageId, new StoreMessageOperation(topic, clone, OperationContext.CreateWith(OperationContextFieldName.InternalOperation, true)), OperationType.MessageOperation);
                        return true;
                    }
                }
                else
                    throw new StateTransferException("I am no more the owner of this bucket");
            }
            finally
            {
                if(lockHeld)
                    _rwBucketsLock.ExitReadLock();
            }
            return stored;
        }

        protected override Message RemoveMessagesInternal(MessageInfo messageInfo, MessageRemovedReason reason,OperationContext context)
        {
            Message message = null;
            bool lockHeld = false;
            try
            {
                int bucketId = GetBucketId(messageInfo.MessageId as string);
                if (!_rwBucketsLock.IsWriteLockHeld)
                {
                    lockHeld = true;
                    _rwBucketsLock.EnterReadLock();
                }

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId) || context.Contains(OperationContextFieldName.InternalOperation))
                {
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                    {
                        _logMgr.LogOperation(bucketId, messageInfo.MessageId, new AtomicRemoveMessageOperation(messageInfo, reason, OperationContext.CreateWith(OperationContextFieldName.InternalOperation, true)), OperationType.MessageOperation);
                        return null;
                    }

                    message = base.RemoveMessagesInternal(messageInfo, reason,context);

                    if(message != null)
                        DecrementBucketStats(message.MessageId, bucketId, message.Size, true, message.MessageMetaData.TopicName);

                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeAfterActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                        _logMgr.LogOperation(bucketId, message.MessageId, new AtomicRemoveMessageOperation(messageInfo, reason, OperationContext.CreateWith(OperationContextFieldName.InternalOperation, true)), OperationType.MessageOperation);
                }
            }
            finally
            {
                if(lockHeld)
                    _rwBucketsLock.ExitReadLock();
            }

            return message;
        }

        public override bool AssignmentOperation(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo,TopicOperationType type, OperationContext context)
        {
            bool result = false;
            bool lockHeld = false;
            try
            {
                int bucketId = GetBucketId(messageInfo.MessageId as string);
                if (!_rwBucketsLock.IsWriteLockHeld)
                {
                    lockHeld = true;
                    _rwBucketsLock.EnterReadLock();
                }

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId))
                {
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                    {
                        _logMgr.LogOperation(bucketId, messageInfo.MessageId, new AssignmentOperation(messageInfo, subscriptionInfo,type, OperationContext.CreateWith(OperationContextFieldName.InternalOperation, true)), OperationType.MessageOperation);
                        return true;
                    }

                     result = base.AssignmentOperation(messageInfo, subscriptionInfo,type,context);
    
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeAfterActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                        _logMgr.LogOperation(bucketId, messageInfo.MessageId, new AssignmentOperation(messageInfo, subscriptionInfo,type, OperationContext.CreateWith(OperationContextFieldName.InternalOperation, true)), OperationType.MessageOperation);
                }
            }
            finally
            {
                if(lockHeld)
                    _rwBucketsLock.ExitReadLock();
            }
            return result;
        }

        protected override void AcknowledgeMessageReceiptInternal(string clientId, string topic, string messageId, OperationContext context)
        {
            bool lockHeld = false;
            try
            {
                int bucketId = GetBucketId(messageId);
                if (!_rwBucketsLock.IsWriteLockHeld)
                {
                    lockHeld = true;
                    _rwBucketsLock.EnterReadLock();
                }

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId) && !context.Contains(OperationContextFieldName.InternalOperation))
                {
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation))
                    {
                        _logMgr.LogOperation(bucketId, messageId, new AtomicAcknowledgeMessageOperation(clientId, messageId, messageId, OperationContext.CreateWith(OperationContextFieldName.InternalOperation, true)), OperationType.MessageOperation);
                        return;
                    }

                    base.AcknowledgeMessageReceiptInternal(clientId, topic, messageId, context);

                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeAfterActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                        _logMgr.LogOperation(bucketId, messageId, new AtomicAcknowledgeMessageOperation(clientId, messageId, messageId, OperationContext.CreateWith(OperationContextFieldName.InternalOperation, true)), OperationType.MessageOperation);
                }
                else
                    throw new StateTransferException("I am no more the owner of this bucket");
            }
            finally
            {
                if(lockHeld)
                    _rwBucketsLock.ExitReadLock();
            }
        }

        public override void RevokeAssignment(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo,OperationContext context)
        {
            bool lockHeld = false;
            try
            {
                int bucketId = GetBucketId(messageInfo.MessageId as string);
                if (!_rwBucketsLock.IsWriteLockHeld)
                {
                    lockHeld = true;
                    _rwBucketsLock.EnterReadLock();
                }

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId) && !context.Contains(OperationContextFieldName.InternalOperation))
                {
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation))
                    {
                        _logMgr.LogOperation(bucketId, messageInfo.MessageId, new AssignmentOperation(messageInfo, subscriptionInfo, TopicOperationType.RevokeAssignment, OperationContext.CreateWith(OperationContextFieldName.InternalOperation,true)), OperationType.MessageOperation);
                        return;
                    }

                    base.RevokeAssignment(messageInfo, subscriptionInfo,context);

                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeAfterActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                        _logMgr.LogOperation(bucketId, messageInfo.MessageId, new AssignmentOperation(messageInfo, subscriptionInfo, TopicOperationType.RevokeAssignment, OperationContext.CreateWith(OperationContextFieldName.InternalOperation, true)), OperationType.MessageOperation);
                }
            }
            finally
            {
                if(lockHeld)
                    _rwBucketsLock.ExitReadLock();
            }
        }
        
        public override bool StoreTransferrableMessage(string topic, TransferrableMessage transferrableMessage)
        {
            bool stored = false;
            try
            {
                Message message = transferrableMessage.Message;

                int bucketId = GetBucketId(message.MessageId as string);
                _rwBucketsLock.EnterReadLock();

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId))
                {
                    if (base.StoreTransferrableMessage(topic, transferrableMessage))
                    {
                        stored = true;
                        IncrementBucketStats(message.MessageId, bucketId, message.Size, true, topic);
                    }
                }
            }
            finally
            {
                _rwBucketsLock.ExitReadLock();
            }
            return stored;
        }

        public override bool TopicOperation(TopicOperation operation, OperationContext operationContext)
        {
            bool result = base.TopicOperation(operation, operationContext);

            if(operation.TopicOperationType == TopicOperationType.Remove)
            {
                RemoveTopicStats(operation.Topic);
            }

            return result;
        }

        public override OrderedDictionary GetMessageList(int bucketId)
        {
            OrderedDictionary messageList = new OrderedDictionary();
            try
            {
                _rwBucketsLock.EnterWriteLock();
               
                if (_keyList != null)
                {
                    if (_keyList.Contains(bucketId))
                    {
                        BucketStatistcs bucketStats = _keyList[bucketId] as BucketStatistcs;
                        messageList  = bucketStats.GetTopicWiseMessagIds();
                    }
                }
            }
            finally
            {
                _rwBucketsLock.ExitWriteLock();
            }
            return messageList;
        }
    }
}