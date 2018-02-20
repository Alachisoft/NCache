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
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    class HashedOverflowCache : IndexedOverflowCache
    {
        private int _bucketSize;

        /// <summary>
        /// A map that contains key lists against each bucket id.
        /// </summary>
        private HashVector _keyList;
        private OpLogManager _logMgr;
        private int _stopLoggingThreshhold = 50;

        private bool _logEntries;

        public HashedOverflowCache(IDictionary cacheClasses, CacheBase parentCache, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, bool logEntries, ActiveQueryAnalyzer activeQueryAnalyzer)
            : base(cacheClasses, parentCache, properties, listener, context, activeQueryAnalyzer)
        {
            _logMgr = new OpLogManager(logEntries, context);
            _logEntries = logEntries;
        }

        public override int BucketSize
        {
            set { _bucketSize = value; }
        }

        public override void GetKeyList(int bucketId, bool startLogging, out ClusteredArrayList keyList)
        {
            if (startLogging)
                _logMgr.StartLogging(bucketId, LogMode.LogBeforeAfterActualOperation);
            keyList = new ClusteredArrayList();

            if (_keyList != null)
            {
                if (_keyList.Contains(bucketId))
                {
                    HashVector keyTbl = _keyList[bucketId] as HashVector;
                    keyList.AddRange(keyTbl.Keys);
                }
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

        public override void RemoveFromLogTbl(int bucketId)
        {
            if (_logMgr != null)
                _logMgr.RemoveLogger(bucketId);
        }
        public override void RemoveBucket(int bucket)
        {
            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("HashedCache.RemoveBucket", "removing bucket :" + bucket);
            //Remove from stats
            lock (LocalBuckets)
            {
                LocalBuckets.Remove(bucket); 
            }
            //Remove actual data of the bucket
            RemoveBucketData(bucket);
            //remove operation logger for the bucket from log table if any exists.
            RemoveFromLogTbl(bucket);
        }

        public override void RemoveBucketData(int bucketId)
        {
            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("HashedOverflowCache.RemoveBucketData", "removing bucket data:" + bucketId);
            ClusteredArrayList keys;
            GetKeyList(bucketId, false, out keys);

            if (keys != null)
            {
                keys = keys.Clone() as ClusteredArrayList;
                IEnumerator ie = keys.GetEnumerator();
                while (ie.MoveNext())
                {
                    Remove(ie.Current, ItemRemoveReason.Removed, false, false, null, 0, LockAccessType.IGNORE_LOCK,new OperationContext(OperationContextFieldName.OperationType,OperationContextOperationType.CacheOperation));
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

                lock (LocalBuckets)
                {
                    if (!LocalBuckets.Contains(ie.Current))
                    {
                        LocalBuckets[ie.Current] = new BucketStatistics();
                    } 
                }
            }
        }
        public override void StartLogging(int bucketId)
        {
            if (_logMgr != null) _logMgr.StartLogging(bucketId, LogMode.LogBeforeActualOperation);
        }

        private void IncrementBucketStats(string key, int bucketId, long dataSize)
        {
            if (_stats.LocalBuckets.Contains(bucketId))
            {
                ((BucketStatistics)_stats.LocalBuckets[bucketId]).Increment(dataSize);
            }

            if (_keyList == null)
                _keyList = new HashVector();

            if (_keyList.Contains(bucketId))
            {
                HashVector keys = (HashVector)_keyList[bucketId];
                keys[key] = null;
            }
            else
            {
                HashVector keys = new HashVector();
                keys[key] = null;
                _keyList[bucketId] = keys;
            }
        }

        private void DecrementBucketStats(string key, int bucketId, long dataSize)
        {
            if (_stats.LocalBuckets.Contains(bucketId))
            {
                ((BucketStatistics)_stats.LocalBuckets[bucketId]).Decrement(dataSize);
            }

            if (_keyList != null)
            {
                if (_keyList.Contains(bucketId))
                {
                    HashVector keys = (HashVector)_keyList[bucketId];
                    keys.Remove(key);

                    if (keys.Count == 0)
                        _keyList.Remove(bucketId);
                }
            }
        }

        private bool IsBucketTransfered(int bucketId)
        {
            return !_logMgr.IsOperationAllowed(bucketId);
        }

        private int GetBucketId(string key)
        {
            int hashCode = AppUtil.GetHashCode(key);
            int bucketId = hashCode / _bucketSize;

            if (bucketId < 0)
                bucketId *= -1;
            return bucketId;
        }
        public override HashVector LocalBuckets
        {
            get { return _stats.LocalBuckets; }
            set { _stats.LocalBuckets = value; }
        }

        public override void AddLoggedData(ArrayList bucketIds, OPLogType type = OPLogType.Cache)
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

                            if (removed != null && removed.Count > 0)
                            {
                                IDictionaryEnumerator ide = removed.GetEnumerator();
                                while (ide.MoveNext())
                                {
                                    Remove(ide.Key, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK,new OperationContext(OperationContextFieldName.OperationType,OperationContextOperationType.CacheOperation));
                                }
                            }

                            if (updated != null && updated.Count > 0)
                            {
                                IDictionaryEnumerator ide = updated.GetEnumerator();
                                while (ide.MoveNext())
                                {
                                    CacheEntry entry = ide.Value as CacheEntry;
                                    if (entry != null)
                                    {
                                        Insert(ide.Key, entry, false, false, null, 0, LockAccessType.IGNORE_LOCK,new OperationContext(OperationContextFieldName.OperationType,OperationContextOperationType.CacheOperation));
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

        #region	/                 --- Cache overrides ---           /

        public override void Dispose()
        {
            if (_logMgr != null) _logMgr.Dispose();
            if (_keyList != null) _keyList.Clear();
            base.Dispose();
        }

        internal override CacheAddResult AddInternal(object key, CacheEntry cacheEntry, bool isUserOperation, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);

            if (IsBucketTransfered(bucketId))
                throw new StateTransferException("I am no more the owner of this bucket");

            if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && isUserOperation)
            {
                _logMgr.LogOperation(bucketId, key, cacheEntry, OperationType.Add);
                return CacheAddResult.Success;
            }

            CacheAddResult result = base.AddInternal(key, cacheEntry, isUserOperation,operationContext);
            if (result == CacheAddResult.Success || result == CacheAddResult.SuccessNearEviction)
            {
                IncrementBucketStats(key as string, bucketId, cacheEntry.DataSize);
                if (isUserOperation) _logMgr.LogOperation(bucketId, key, cacheEntry, OperationType.Add);

            }
            return result;
        }

        internal override bool AddInternal(object key, Alachisoft.NCache.Caching.AutoExpiration.ExpirationHint eh, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);
            if (IsBucketTransfered(bucketId))
                throw new StateTransferException("I am no more the owner of this bucket");

            return base.AddInternal(key, eh,operationContext);
        }

        internal override bool AddInternal(object key, Alachisoft.NCache.Caching.CacheSynchronization.CacheSyncDependency syncDependency)
        {
            int bucketId = GetBucketId(key as string);
            if (IsBucketTransfered(bucketId))
                throw new StateTransferException("I am no more the owner of this bucket");

            return base.AddInternal(key, syncDependency);
        }

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        internal override void ClearInternal()
        {
            base.ClearInternal();
            if (_keyList != null) _keyList.Clear();
            if (_logMgr != null) _logMgr.Dispose(); //it clears the operation loggers for each bucket   

            lock (LocalBuckets)
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

        internal override CacheInsResult InsertInternal(object key, CacheEntry cacheEntry, bool isUserOperation, CacheEntry oldEntry, OperationContext operationContext, bool updateIndex)
        {
            int bucketId = GetBucketId(key as string);

            if (IsBucketTransfered(bucketId))
                throw new StateTransferException("I am no more the owner of this bucket");

            long oldEntrysize = oldEntry == null ? 0 : oldEntry.DataSize;

            if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && isUserOperation)
            {
                _logMgr.LogOperation(bucketId, key, cacheEntry, OperationType.Insert);
                return oldEntry != null ? CacheInsResult.SuccessOverwrite : CacheInsResult.Success;
            }

            CacheInsResult result = base.InsertInternal(key, cacheEntry, isUserOperation,oldEntry,operationContext, updateIndex);

            switch (result)
            {
                case CacheInsResult.SuccessNearEvicition:
                case CacheInsResult.Success:
                    if (isUserOperation) _logMgr.LogOperation(bucketId, key, cacheEntry, OperationType.Insert);
                    IncrementBucketStats(key as string, bucketId, cacheEntry.DataSize);
                    break;

                case CacheInsResult.SuccessOverwriteNearEviction:
                case CacheInsResult.SuccessOverwrite:
                    if (isUserOperation) _logMgr.LogOperation(bucketId, key, cacheEntry, OperationType.Insert);
                    DecrementBucketStats(key as string, bucketId, oldEntrysize);
                    IncrementBucketStats(key as string, bucketId, cacheEntry.DataSize);
                    break;
            }

            return result;

        }

        internal override CacheEntry RemoveInternal(object key, ItemRemoveReason removalReason, bool isUserOperation, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);
            if (isUserOperation)
            {
                if (IsBucketTransfered(bucketId))
                    throw new StateTransferException("I am no more the owner of this bucket");
            }

            if (_logMgr.IsLoggingEnbaled(bucketId, LogMode.LogBeforeActualOperation) && isUserOperation)
            {
                CacheEntry e = Get(key,operationContext);
                _logMgr.LogOperation(bucketId, key, null, OperationType.Delete);
                return e;
            }

            CacheEntry entry = base.RemoveInternal(key, removalReason, isUserOperation,operationContext);
            if (entry != null)
            {
                if (isUserOperation) _logMgr.LogOperation(bucketId, key, null, OperationType.Delete);
                DecrementBucketStats(key as string, bucketId, entry.DataSize);
            }
            return entry;
        }

        internal override CacheEntry GetInternal(object key, bool isUserOperation, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);
            if (IsBucketTransfered(bucketId))
                throw new StateTransferException("I am no more the owner of this bucket");

            return base.GetInternal(key, isUserOperation,operationContext);
        }

        internal override bool ContainsInternal(object key)
        {
            int bucketId = GetBucketId(key as string);
            if (IsBucketTransfered(bucketId))
                throw new StateTransferException("I am no more the owner of this bucket");

            return base.ContainsInternal(key);
        }

        #endregion
    }
}