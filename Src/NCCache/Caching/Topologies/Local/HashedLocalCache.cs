//  Copyright (c) 2019 Alachisoft
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
using System.Collections;
using System.Threading;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System;
using Alachisoft.NCache.Common.Locking;
using System.Collections.Generic;

using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Util;


namespace Alachisoft.NCache.Caching.Topologies.Local
{
    class HashedLocalCache : LocalCache,ISizableIndex
    {
        private int _bucketSize;

        /// <summary>
        /// A map that contains key lists against each bucket id.
        /// </summary>
        /// 
        private BucketStatistcs[] _keyList= new BucketStatistcs[AppUtil.MAX_BUCKETS];
        private int _stopLoggingThreshhold = 50;
        private long _inMemorySize = 0;
        private OpLogManager _logMgr;
      
        private object _filterMutex = new object();
        
        private readonly SemaphoreLock _keyListLock = new SemaphoreLock();
        private Thread _thread;
        private object _bucketStatsMutex = new object();
        //This lock is intended to synchronize user operations and state transfer. State transfer operations can only take WRITER lock.
        private ReaderWriterLockSlim _rwBucketsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public HashedLocalCache(IDictionary cacheClasses, CacheBase parentCache, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, bool logEntries)
            : base(cacheClasses, parentCache, properties, listener, context)
        {
            _logMgr = new OpLogManager(logEntries, context);
            _stats.LocalBuckets = new BucketStatistics[AppUtil.MAX_BUCKETS];
            _cacheStore.ISizableBucketIndexManager = this;

            _thread = new Thread(UpdateStats)
            {
                Name = "UpdateStats",
                IsBackground = true
            };

            _thread.Start();
        }

        public override int BucketSize
        {
            set 
            {
                _bucketSize = value;
            }
        }

        public override BucketStatistics[] LocalBuckets
        {
            get { return _stats.LocalBuckets; }
        }

        private void UpdatePendingBucketStats()
        {
            lock (_bucketStatsOperations)
            {
                int count = _bucketStatsOperations.Count;

                while (count > 0)
                {
                    BucketStatsOperation statsOperation = _bucketStatsOperations.Dequeue();

                    try
                    {
                        switch (statsOperation.Type)
                        {
                            case OperationType.Add:
                                IncrementBucketStats(statsOperation.Key as string, statsOperation.BucketId, statsOperation.Size, statsOperation.IsMessage, statsOperation.Topic);
                                break;
                            case OperationType.Insert:
                                switch (statsOperation.InsResult)
                                {
                                    case CacheInsResult.SuccessNearEvicition:
                                    case CacheInsResult.Success:
                                        IncrementBucketStats(statsOperation.Key as string, statsOperation.BucketId, statsOperation.Size, statsOperation.IsMessage, statsOperation.Topic);
                                        break;

                                    case CacheInsResult.SuccessOverwriteNearEviction:
                                    case CacheInsResult.SuccessOverwrite:
                                        DecrementBucketStats(statsOperation.Key as string, statsOperation.BucketId, statsOperation.OldSize, statsOperation.IsMessage, statsOperation.Topic);
                                        IncrementBucketStats(statsOperation.Key as string, statsOperation.BucketId, statsOperation.Size, statsOperation.IsMessage, statsOperation.Topic);
                                        break;
                                }
                                break;
                            case OperationType.Delete:
                                DecrementBucketStats(statsOperation.Key as string, statsOperation.BucketId, statsOperation.Size, statsOperation.IsMessage, statsOperation.Topic);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        _context.NCacheLog.Error("HashedLocalCache.UpdatePendingBucketStats", e.ToString());
                    }

                    count--;
                }
            }
        }

        public override void GetKeyList(int bucketId, bool startLogging, out ClusteredArrayList keyList)
        {
            try
            {
                UpdatePendingBucketStats();
               // _rwBucketsLock.EnterWriteLock();
                if (startLogging)
                    _logMgr.StartLogging(bucketId, LogMode.LogBeforeAfterActualOperation);

                keyList = new ClusteredArrayList();
                BucketStatistcs stats = _keyList[bucketId];
                if (stats!=null)
                {
                   keyList = stats.GetCacheKeys();  
                }
            }
            finally
            {
              //  _rwBucketsLock.ExitWriteLock();
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

        public override void PrepareBucketForStateTrxfer(int bucketId)
        {
            try
            {
                _rwBucketsLock.EnterWriteLock();

                if (LocalBuckets != null && LocalBuckets[bucketId] != null)
                {
                    BucketStatistics statistics = LocalBuckets[bucketId] as BucketStatistics;
                    statistics.IsStateTransferStarted = true;
                }

            }
            finally
            {
                _rwBucketsLock.ExitWriteLock();
            }
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

        private void IncrementBucketStats(string cacheKeyOrMessageId, int bucketId, long dataSize, bool isMessage, string topic)
        {
            if (_stats.LocalBuckets != null)
            {
                BucketStatistics bStats = _stats.LocalBuckets[bucketId];
                if (bStats != null)
                {
                    if (isMessage)
                        bStats.IncrementTopicStats(topic, dataSize);
                    else
                        bStats.Increment(dataSize);
                }
            }
            
            _stats.IncrementUpdateCount();

                HashVector keys;
                BucketStatistcs bucketStats = _keyList[bucketId];
                bool isKeyExist = false;

                if (bucketStats == null)
                {
                    bucketStats = new BucketStatistcs(bucketId);
                    _keyList[bucketId] = bucketStats;
                }

                long oldSize = bucketStats.InMemorySize;
                lock (bucketStats)
                {
                    if (isMessage)
                        bucketStats.AddMessage(topic, cacheKeyOrMessageId);
                    else
                        bucketStats.AddCacheKey(cacheKeyOrMessageId);
                }
                long newSize = bucketStats.InMemorySize;
                _inMemorySize += (newSize - oldSize);

                // TODO: [Umer] need to know what is this ? Found during Merge
                //isKeyExist = true;
                //if (isKeyExist) return;
        }

        private void DecrementBucketStats(string cacheKeyOrMessageId, int bucketId, long dataSize, bool isMessage, string topic)
        {
            if (_stats.LocalBuckets != null)
            {
                BucketStatistics bStats = _stats.LocalBuckets[bucketId];
                if (bStats != null)
                {
                    if (isMessage)
                        bStats.DecrementTopicStats(topic, dataSize);
                    else
                        bStats.Decrement(dataSize);
                }
            }

            _stats.IncrementUpdateCount();

            bool isKeysEmpty = false;
            HashVector keys = null;
            BucketStatistcs bucketStats = _keyList[bucketId];
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

                if (_inMemorySize < 0) _inMemorySize = 0;
            }
        }

        private void RemoveTopicStats(string topic)
        {
            lock (_bucketStatsMutex)
            {
                if (_stats.LocalBuckets != null)
                {
                    foreach (var stats in _stats.LocalBuckets)
                    {
                        if(stats!=null)
                            stats.RemoveTopic(topic);
                    }
                }
            }
            foreach (BucketStatistcs bucketStats in _keyList)
            {
                if (bucketStats != null)
                {
                    long oldSize = bucketStats.InMemorySize;
                    long newSize;
                    lock (bucketStats)
                    {
                        bucketStats.RemoveTopic(topic);
                        newSize = bucketStats.InMemorySize;
                    }
                    _inMemorySize = _inMemorySize + (newSize - oldSize);
                }
            }

            if (_inMemorySize < 0) _inMemorySize = 0;
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
                            Hashtable collectionOperations = loggedEnteries["collectionops"] as Hashtable;

                            if (removed != null && removed.Count > 0)
                            {
                                IDictionaryEnumerator ide = removed.GetEnumerator();
                                while (ide.MoveNext())
                                {
                                    OperationContext operationContext = null;

                                    try
                                    {
                                        operationContext = OperationContext.CreateAndMarkInUse(
                                            Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation
                                        );
                                        operationContext.CloneCacheEntry = false;
                                        operationContext.NeedUserPayload = false;
                                        Remove(ide.Key, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                                    }
                                    finally
                                    {
                                        MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                                        operationContext?.MarkFree(NCModulesConstants.CacheInternal);
                                    }
                                }
                            }

                            if (updated != null && updated.Count > 0)
                            {
                                IDictionaryEnumerator ide = updated.GetEnumerator();
                                CacheEntry entry = null;
                                while (ide.MoveNext())
                                {
                                    OperationContext operationContext = null;
                                    try
                                    {
                                        entry = ide.Value as CacheEntry;
                                        if (entry != null) 
                                        {
                                            operationContext = OperationContext.CreateAndMarkInUse(
                                             Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation
                                         );
                                            entry.MarkInUse(NCModulesConstants.CacheInternal);
                                                Add(ide.Key, entry, false, false, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                                        }
                                    }
                                    finally
                                    {
                                        MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                                        operationContext?.MarkFree(NCModulesConstants.CacheInternal);
                                        if (entry != null)
                                            entry.MarkFree(NCModulesConstants.CacheInternal);

                                        //This entry was initially allocted on transactional pool, then logged but not added to store
                                        //Above Add call will automatically allocate another entry from store pool
                                        MiscUtil.ReturnEntryToPool(entry, _context.TransactionalPoolManager);
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
            OperationContext operationContext = null;

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
                            OperationContext removeCallOperationContext = null;

                            try
                            {
                                removeCallOperationContext = OperationContext.CreateAndMarkInUse(
                                    Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation
                                );
                                removeCallOperationContext.CloneCacheEntry = false;
                                removeCallOperationContext.NeedUserPayload = false;
                                Remove(ie.Current, ItemRemoveReason.Removed, false, false, null, 0, LockAccessType.IGNORE_LOCK, removeCallOperationContext);
                            }
                            finally
                            {
                                MiscUtil.ReturnOperationContextToPool(removeCallOperationContext, _context.TransactionalPoolManager);
                                removeCallOperationContext?.MarkFree(NCModulesConstants.CacheInternal);
                            }
                        }
                    }

                }

                //remove messages
                OrderedDictionary topicWiseMessages = GetMessageList(bucketId,true);
                MessageInfo message = new MessageInfo();
                operationContext = OperationContext.CreateAndMarkInUse(
                    Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.InternalOperation, true
                );

                foreach (DictionaryEntry entry in topicWiseMessages)
                {
                    message.Topic = entry.Key as string;

                    foreach (string messageId in entry.Value as ClusteredArrayList)
                    {
                        message.MessageId = messageId;
                        RemoveMessagesInternal(message, MessageRemovedReason.Delivered, operationContext);
                    }
                }
            }
            finally
            {
                MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                operationContext?.MarkFree(NCModulesConstants.CacheInternal);
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
                        LocalBuckets[bucket]=null;
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
                    LocalBuckets = new BucketStatistics[AppUtil.MAX_BUCKETS];

                lock (_bucketStatsMutex)
                {
                    if (LocalBuckets[(int)ie.Current]==null)
                    {
                        LocalBuckets[(int)ie.Current] = new BucketStatistics();
                    } 
                }
            }
        }

        #region	/                 --- Cache overrides ---           /

        internal override CacheAddResult AddInternal(object key, CacheEntry cacheEntry, bool isUserOperation, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);
            CacheEntry clone = null;
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.LocalBase);
                cacheEntry?.MarkInUse(NCModulesConstants.LocalCache);

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId]!=null)
                {
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && isUserOperation)
                    {
                        clone = cacheEntry.DeepClone(_context.TransactionalPoolManager);
                        cacheEntry?.MarkInUse(NCModulesConstants.Replication);
                        _logMgr.LogOperation(bucketId, key, clone, OperationType.Add);
                        return CacheAddResult.Success;
                    }

                    Hashtable queryInfo = cacheEntry.QueryInfo;
                    CacheAddResult result = base.AddInternal(key, cacheEntry, isUserOperation, operationContext);

                    if (result == CacheAddResult.Success || result == CacheAddResult.SuccessNearEviction)
                    {                        
                        BucketStatsOperation statsOperation =
                       new BucketStatsOperation(bucketId, key, OperationType.Add, CacheInsResult.Success, 0, cacheEntry.InMemorySize, false, null);

                        lock (_bucketStatsOperations)
                        {
                            _bucketStatsOperations.Enqueue(statsOperation);
                        }
                        if (isUserOperation && _logMgr.IsLoggingEnbaled(bucketId,LogMode.LogBeforeAfterActualOperation))
                        {
                            clone = cacheEntry.DeepClone(_context.TransactionalPoolManager);
                            clone.QueryInfo = queryInfo;
                            _logMgr.LogOperation(bucketId, key, clone, OperationType.Add);
                        }
                    }

                    return result;
                }
            }
            finally
            {

                cacheEntry?.MarkFree(NCModulesConstants.LocalCache);

                operationContext?.MarkFree(NCModulesConstants.LocalBase);

                if (clone != null) clone.MarkFree(NCModulesConstants.LocalBase);

            }

            throw new StateTransferException("I am no more the owner of this bucket");
        }

        internal override bool AddInternal(object key, Alachisoft.NCache.Caching.AutoExpiration.ExpirationHint eh, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);

            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId]!=null)
                return base.AddInternal(key, eh,operationContext);
            
            throw new StateTransferException("I am no more the owner of this bucket");
        }

        internal override bool RemoveInternal(object key, Alachisoft.NCache.Caching.AutoExpiration.ExpirationHint eh)
        {
            int bucketId = GetBucketId(key as string);
            
            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null)
                return base.RemoveInternal(key, eh);

            throw new StateTransferException("I am no more the owner of this bucket");
        }

        internal override bool AddInternal(object key)
        {
            int bucketId = GetBucketId(key as string);
            
            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null)
                return base.AddInternal(key);
            
            throw new StateTransferException("I am no more the owner of this bucket");
        }
        private long GetEntrySize(CacheEntry entry)
        {
            return entry.Type == Common.Caching.EntryType.CacheItem ? entry.InMemorySize : entry.OldInMemorySize;
        }

        private Queue<BucketStatsOperation> _bucketStatsOperations = new Queue<BucketStatsOperation>(50000);

        internal struct BucketStatsOperation
        {
            internal int BucketId { get; }
            internal object Key { get; }
            internal OperationType Type { get; }
            internal CacheInsResult InsResult { get; }
            public long Size { get; }
            public long OldSize { get; }
            public bool IsMessage { get; }
            public string Topic { get; }

            internal BucketStatsOperation(int bucketId, object key, OperationType type, CacheInsResult insResult, long oldSize, long size, bool isMessage, string topic)
            {
                BucketId = bucketId;
                Key = key;
                Type = type;
                InsResult = insResult;
                OldSize = oldSize;
                Size = size;
                IsMessage = isMessage;
                Topic = topic;
            }
        }

        private void UpdateStats()
        {
            try
            {
                while(true)
                {
                    try
                    {
                        int count = 0;

                        lock (_bucketStatsOperations)
                        {
                            count = _bucketStatsOperations.Count;
                        }

                        while (count > 0)
                        {
                            BucketStatsOperation? operation = null;
                            lock (_bucketStatsOperations)
                            {
                                if (_bucketStatsOperations.Count == 0) break;
                                operation = _bucketStatsOperations.Dequeue();
                            }

                            BucketStatsOperation statsOperation = operation.Value;

                            try
                            {
                                switch (statsOperation.Type)
                                {
                                    case OperationType.Add:
                                        IncrementBucketStats(statsOperation.Key as string, statsOperation.BucketId, statsOperation.Size, statsOperation.IsMessage, statsOperation.Topic);
                                      
                                        break;
                                    case OperationType.Insert:
                                        switch (statsOperation.InsResult)
                                        {
                                            case CacheInsResult.SuccessNearEvicition:
                                            case CacheInsResult.Success:
                                                IncrementBucketStats(statsOperation.Key as string, statsOperation.BucketId, statsOperation.Size, statsOperation.IsMessage, statsOperation.Topic);
                                                break;

                                            case CacheInsResult.SuccessOverwriteNearEviction:
                                            case CacheInsResult.SuccessOverwrite:
                                                DecrementBucketStats(statsOperation.Key as string, statsOperation.BucketId, statsOperation.OldSize, statsOperation.IsMessage, statsOperation.Topic);
                                                IncrementBucketStats(statsOperation.Key as string, statsOperation.BucketId, statsOperation.Size, statsOperation.IsMessage, statsOperation.Topic);
                                                break;
                                        }
                                        break;
                                    case OperationType.Delete:
                                        DecrementBucketStats(statsOperation.Key as string, statsOperation.BucketId, statsOperation.Size, statsOperation.IsMessage, statsOperation.Topic);
                                        break;
                                }
                            }
                            catch (Exception e)
                            {
                                _context.NCacheLog.Error("HashedLocalCache.UpdateStats", e.ToString());
                            }

                            count--;
                        }

                        Thread.Sleep(1000);
                    }
                    catch (ThreadAbortException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        _context.NCacheLog.Error("HashedLocalCache.UpdateStats", e.ToString());
                    }
                }
            }
            catch(Exception e)
            {
                _context.NCacheLog.Error("HashedLocalCache.UpdateStats", e.ToString());
            }
        }

        internal override CacheInsResult InsertInternal(object key, CacheEntry cacheEntry, bool isUserOperation, CacheEntry oldEntry, OperationContext operationContext, bool updateIndex)
        {
            CacheEntry clone = null;
            int bucketId = GetBucketId(key as string);

            try
            {
                operationContext?.MarkInUse(NCModulesConstants.LocalBase);
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.CacheInternal);

                //fetch the operation logger...
                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null)
                {
                    long oldEntrysize = oldEntry == null ? 0 : GetEntrySize(oldEntry);

                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && isUserOperation)
                    {
                        clone = cacheEntry.DeepClone(_context.TransactionalPoolManager);
                        _logMgr.LogOperation(bucketId, key, clone, OperationType.Insert);
                        return oldEntry != null ? CacheInsResult.SuccessOverwrite : CacheInsResult.Success;
                    }

                    // POTeam only clone when it is not user operation i.e. for state txfr
                    Hashtable queryInfo = cacheEntry.QueryInfo;
                    CacheInsResult result = base.InsertInternal(key, cacheEntry, isUserOperation, oldEntry, operationContext, updateIndex);

                    bool logOperation = !operationContext.Contains(OperationContextFieldName.DoNotLog);

                    if (result != CacheInsResult.Failure && result != CacheInsResult.NeedsEviction)
                    {
                        BucketStatsOperation statsOperation = new BucketStatsOperation(bucketId, key, OperationType.Insert, result, oldEntrysize, cacheEntry.InMemorySize, false, null);

                        lock (_bucketStatsOperations)
                        {
                            _bucketStatsOperations.Enqueue(statsOperation);
                        }

                        if (isUserOperation && logOperation && _logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeAfterActualOperation))
                        {
                            clone = cacheEntry.DeepClone(_context.TransactionalPoolManager);
                            clone.QueryInfo = queryInfo;
                            _logMgr.LogOperation(bucketId, key, clone, OperationType.Insert);
                        }
                    }
                   
                    return result;
                }
            }
            finally
            {
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.CacheInternal);
                operationContext?.MarkFree(NCModulesConstants.LocalBase);
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.CacheInternal);
            }

            throw new StateTransferException("I am no more the owner of this bucket");
        }


        internal override CacheEntry RemoveInternal(object key, ItemRemoveReason removalReason, bool isUserOperation, OperationContext operationContext, bool cloneEntry, bool needUserPayload)
        {
            int bucketId = GetBucketId(key as string);
            CacheEntry entry = null;
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.LocalBase);

                if (isUserOperation)
                {
                    if (!(_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null))
                        throw new StateTransferException("I am no more the owner of this bucket");
                }
                if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && isUserOperation)
                {
                    CacheEntry e = GetInternal(key, isUserOperation, operationContext,cloneEntry,needUserPayload);
                    _logMgr.LogOperation(bucketId, key, null, OperationType.Delete);
                    
                    return e;
                }

                entry = base.RemoveInternal(key, removalReason, isUserOperation, operationContext,cloneEntry,needUserPayload);
                if (entry != null)
                {
                    BucketStatsOperation statsOperation =
                        new BucketStatsOperation(bucketId, key, OperationType.Delete, CacheInsResult.Success, 0, entry.InMemorySize, false, null);

                    lock (_bucketStatsOperations)
                    {
                        _bucketStatsOperations.Enqueue(statsOperation);
                    }

                    if (isUserOperation) _logMgr.LogOperation(bucketId, key, null, OperationType.Delete);
                    
                }
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.LocalBase);
            }
            return entry;
        }

        internal override CacheEntry GetInternal(object key, bool isUserOperation, OperationContext operationContext, bool cloneEntry, bool needUserPayload)
        {
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.LocalBase);

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
                            _context.NCacheLog.Error("GetInternal()", "Null Module: " + loggingModule + "[] Exception is " + e);
                        }
                        throw e;
                    }
                }
                CacheEntry entry = base.GetInternal(key, isUserOperation, operationContext,cloneEntry,needUserPayload);
                try
                {
                    if (entry == null && (isUserOperation && LocalBuckets[bucketId] == null))
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
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.LocalBase);
            }
        }

        internal override bool ContainsInternal(object key)
        {
            int bucketId = GetBucketId(key as string);

            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null)
                return base.ContainsInternal(key);

            throw new StateTransferException("I am no more the owner of this bucket");
        }

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        internal override void ClearInternal()
        {
            base.ClearInternal();
            ClearKeyList();
            _inMemorySize = 0;
            if (_logMgr != null) _logMgr.Dispose(); //it clears the operation loggers for each bucket   


            if (LocalBuckets == null)
                return;
            lock (_bucketStatsMutex)
            {
                //clear the bucket stats
                for (int i = 0; i < LocalBuckets.Length; i++)
                {
                    if(LocalBuckets[i]!=null)
                        LocalBuckets[i].Clear();
                }
            }
        }

        public override void Dispose()
        {
            if (_thread != null)
#if !NETCORE
                _thread.Abort();
#else
                _thread.Interrupt();
#endif
            if (_logMgr != null) _logMgr.Dispose();
            ClearKeyList();
            _inMemorySize = 0;
            base.Dispose();
        }

#endregion

        public long InMemorySize
        {
            get
            {
                return _inMemorySize + _keyList.Length * MemoryUtil.NetClassOverHead;
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

            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null)
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
                Message clone = null;
                if (message is EventMessage)
                {
                    EventMessage eventMessage = (EventMessage)message;
                    clone = new EventMessage(message.MessageId);
                    clone = eventMessage.Clone();
                    clone.MessageMetaData =new MessageMetaData(eventMessage.MessageId);
                    clone.MessageMetaData.TopicName = topic;
                    clone.MessageMetaData.IsNotify = false;
                    clone.MessageMetaData.DeliveryOption = Alachisoft.NCache.Runtime.Caching.DeliveryOption.All;
                    clone.MessageMetaData.ExpirationTime = message.MessageMetaData.ExpirationTime;
                    clone.MessageMetaData.TimeToLive = message.MessageMetaData.TimeToLive;
                }
                else
                {
                    clone = new Message(message.MessageId);
                    clone = message.Clone() as Message;
                }
               
                if (!_rwBucketsLock.IsWriteLockHeld)
                {
                    lockHeld = true;
                   _rwBucketsLock.EnterReadLock();
                }

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null)
                {
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                    {
                        OperationContext operationContext = null;

                        try
                        {
                            operationContext = OperationContext.CreateAndMarkInUse(
                                Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.InternalOperation, true
                            );
                            _logMgr.LogOperation(bucketId, message.MessageId, new StoreMessageOperation(topic, clone, operationContext), OperationType.MessageOperation);
                            return true;
                        }
                        finally
                        {
                            MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                            operationContext?.MarkFree(NCModulesConstants.CacheInternal);
                        }
                    }

                    if (base.StoreMessage(topic, message, context))
                    {
                        stored = true;

                        BucketStatsOperation statsOperation =
                       new BucketStatsOperation(bucketId, message.MessageId, OperationType.Add, CacheInsResult.Success, 0, message.Size, true, topic);

                        lock (_bucketStatsOperations)
                        {
                            _bucketStatsOperations.Enqueue(statsOperation);
                        }

                        if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeAfterActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                        {
                            OperationContext operationContext = null;

                            try
                            {
                                operationContext = OperationContext.CreateAndMarkInUse(
                                    Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.InternalOperation, true
                                );
                                _logMgr.LogOperation(bucketId, message.MessageId, new StoreMessageOperation(topic, clone, operationContext), OperationType.MessageOperation);
                            }
                            finally
                            {
                                MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                                operationContext?.MarkFree(NCModulesConstants.CacheInternal);
                            }
                        }
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

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null || context.Contains(OperationContextFieldName.InternalOperation))
                {
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                    {
                        OperationContext operationContext = null;

                        try
                        {
                            operationContext = OperationContext.CreateAndMarkInUse(
                                Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.InternalOperation, true
                            );

                            _logMgr.LogOperation(bucketId, messageInfo.MessageId, new AtomicRemoveMessageOperation(messageInfo, reason, operationContext), OperationType.MessageOperation);
                            return null;
                        }
                        finally
                        {
                            MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                            operationContext?.MarkFree(NCModulesConstants.CacheInternal);
                        }
                    }

                    message = base.RemoveMessagesInternal(messageInfo, reason,context);

                    if (message != null)
                    {
                        BucketStatsOperation statsOperation =
                      new BucketStatsOperation(bucketId, message.MessageId, OperationType.Delete, CacheInsResult.Success, 0, message.Size, true, message.MessageMetaData.TopicName);

                        lock (_bucketStatsOperations)
                        {
                            _bucketStatsOperations.Enqueue(statsOperation);
                        }
                    }
                    
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeAfterActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                    {
                        OperationContext operationContext = null;

                        try
                        {
                            operationContext = OperationContext.CreateAndMarkInUse(
                                Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.InternalOperation, true
                            );
                            _logMgr.LogOperation(bucketId, message.MessageId, new AtomicRemoveMessageOperation(messageInfo, reason, operationContext), OperationType.MessageOperation);
                        }
                        finally
                        {
                            MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                            operationContext?.MarkFree(NCModulesConstants.CacheInternal);
                        }
                    }
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

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null)
                {
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                    {
                        OperationContext operationContext = null;

                        try
                        {
                            operationContext = OperationContext.CreateAndMarkInUse(
                                Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.InternalOperation, true
                            );
                            _logMgr.LogOperation(bucketId, messageInfo.MessageId, new AssignmentOperation(messageInfo, subscriptionInfo, type, operationContext), OperationType.MessageOperation);
                            return true;
                        }
                        finally
                        {
                            MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                            operationContext?.MarkFree(NCModulesConstants.CacheInternal);
                        }
                    }

                     result = base.AssignmentOperation(messageInfo, subscriptionInfo,type,context);

                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeAfterActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                    {
                        OperationContext operationContext = null;

                        try
                        {
                            operationContext = OperationContext.CreateAndMarkInUse(
                                Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.InternalOperation, true
                            );
                            _logMgr.LogOperation(bucketId, messageInfo.MessageId, new AssignmentOperation(messageInfo, subscriptionInfo, type, operationContext), OperationType.MessageOperation);
                        }
                        finally
                        {
                            MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                            operationContext?.MarkFree(NCModulesConstants.CacheInternal);
                        }
                    }
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

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null && !context.Contains(OperationContextFieldName.InternalOperation))
                {
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation))
                    {
                        OperationContext operationContext = null;

                        try
                        {
                            operationContext = OperationContext.CreateAndMarkInUse(
                                Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.InternalOperation, true
                            );
                            _logMgr.LogOperation(bucketId, messageId, new AtomicAcknowledgeMessageOperation(clientId, messageId, messageId, operationContext), OperationType.MessageOperation);
                            return;
                        }
                        finally
                        {
                            MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                            operationContext?.MarkFree(NCModulesConstants.CacheInternal);
                        }
                    }

                    base.AcknowledgeMessageReceiptInternal(clientId, topic, messageId, context);

                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeAfterActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                    {
                        OperationContext operationContext = null;

                        try
                        {
                            operationContext = OperationContext.CreateAndMarkInUse(
                                Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.InternalOperation, true
                            );
                            _logMgr.LogOperation(bucketId, messageId, new AtomicAcknowledgeMessageOperation(clientId, messageId, messageId, operationContext), OperationType.MessageOperation);
                        }
                        finally
                        {
                            MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                            operationContext?.MarkFree(NCModulesConstants.CacheInternal);
                        }
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

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null && !context.Contains(OperationContextFieldName.InternalOperation))
                {
                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation))
                    {
                        OperationContext operationContext = null;

                        try
                        {
                            operationContext = OperationContext.CreateAndMarkInUse(
                                Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.InternalOperation, true
                            );
                            _logMgr.LogOperation(bucketId, messageInfo.MessageId, new AssignmentOperation(messageInfo, subscriptionInfo, TopicOperationType.RevokeAssignment, operationContext), OperationType.MessageOperation);
                            return;
                        }
                        finally
                        {
                            MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                            operationContext?.MarkFree(NCModulesConstants.CacheInternal);
                        }
                    }

                    base.RevokeAssignment(messageInfo, subscriptionInfo,context);

                    if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeAfterActualOperation) && !context.Contains(OperationContextFieldName.InternalOperation))
                    {
                        OperationContext operationContext = null;

                        try
                        {
                            operationContext = OperationContext.CreateAndMarkInUse(
                                Context.TransactionalPoolManager, NCModulesConstants.CacheInternal, OperationContextFieldName.InternalOperation, true
                            );
                            _logMgr.LogOperation(bucketId, messageInfo.MessageId, new AssignmentOperation(messageInfo, subscriptionInfo, TopicOperationType.RevokeAssignment, operationContext), OperationType.MessageOperation);
                        }
                        finally
                        {
                            MiscUtil.ReturnOperationContextToPool(operationContext, _context.TransactionalPoolManager);
                            operationContext?.MarkFree(NCModulesConstants.CacheInternal);
                        }
                    }
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

                if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null)
                {
                    if (base.StoreTransferrableMessage(topic, transferrableMessage))
                    {
                        stored = true;

                        BucketStatsOperation statsOperation =
                      new BucketStatsOperation(bucketId, message.MessageId, OperationType.Add, CacheInsResult.Success, 0, message.Size, true, topic);

                        lock (_bucketStatsOperations)
                        {
                            _bucketStatsOperations.Enqueue(statsOperation);
                        }
                    }
                }
            }
            finally
            {
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

        public override OrderedDictionary GetMessageList(int bucketId, bool includeEventMessages)
        {
            OrderedDictionary messageList = new OrderedDictionary();
            try
            {
                _rwBucketsLock.EnterWriteLock();
                BucketStatistcs bucketStats = _keyList[bucketId];
                if (bucketStats != null)
                {
                    lock (bucketStats)
                    {
                        messageList = bucketStats.GetTopicWiseMessagIds(includeEventMessages);
                    }
                }
            }
            finally
            {
                _rwBucketsLock.ExitWriteLock();
            }
            return messageList;
        }

        private void ClearKeyList()
        {
            lock (_keyList.SyncRoot)
            {
                for (int i = 0; i < _keyList.Length; i++)
                {
                    _keyList[i] = null;
                }
            }
        }

        

#region Private Methods

        private bool IsCollectionReplicationOperationAllowed(int bucketId)
        {
            bool allowed = false;
            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets[bucketId] != null)
            {
                BucketStatistics statistics = LocalBuckets[bucketId] as BucketStatistics;
                allowed = statistics.IsStateTransferStarted; 
            }

            return allowed;
        }

       

#endregion
    }
}