// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Locking;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    class HashedLocalCache : IndexedLocalCache
    {
        private int _bucketSize;

        /// <summary>
        /// A map that contains key lists against each bucket id.
        /// </summary>
        private HashVector _keyList;
        private int _stopLoggingThreshhold = 50;
        private long _keyListSize = 0;

        private OpLogManager _logMgr;

        private readonly SemaphoreLock _keyListLock = new SemaphoreLock();

        public HashedLocalCache(IDictionary cacheClasses, CacheBase parentCache, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, bool logEntries)
            : base(cacheClasses, parentCache, properties, listener, context)
        {
            _logMgr = new OpLogManager(logEntries, context);
            _stats.LocalBuckets = new HashVector();
        }

        public override int BucketSize
        {
            set { _bucketSize = value; }
        }

        public override HashVector LocalBuckets
        {
            get { return _stats.LocalBuckets; }
            set { _stats.LocalBuckets = value; }
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

        /// <summary>
        /// Starts logging on the specified bucket. All operation are logged
        /// untill we explicitly stop logging. Logging operations helps synchronize
        /// the state of the cluste during the state transfer of a bucket.
        /// </summary>
        /// <param name="bucketId"></param>
        public override void StartLogging(int bucketId)
        {
            if (_logMgr != null) _logMgr.StartLogging(bucketId, LogMode.LogBeforeActualOperation);
        }


        public override Hashtable GetLogTable(ArrayList bucketIds, ref bool isLoggingStopped)
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

        private void IncrementBucketStats(string key, int bucketId, long dataSize)
        {
            if (_stats.LocalBuckets != null && _stats.LocalBuckets.Contains(bucketId))
            {
                ((BucketStatistics)_stats.LocalBuckets[bucketId]).Increment(dataSize);
            }

            if (_keyList == null)
                _keyList = new HashVector();

            if (_keyList != null)
            {
                HashVector keys;
                bool isKeyExist = false;
        
                _keyListLock.Enter();

                if (!_keyList.Contains(bucketId))
                {
                    keys = new HashVector();
                    keys[key] = null;
                    long keySize = keys.BucketCount * MemoryUtil.NetHashtableOverHead;
                    long currentsize = _keyListSize;
                    _keyListSize = currentsize + keySize;
                    _keyList[bucketId] = keys;
                    isKeyExist = true;
                }

                _keyListLock.Exit();
         
                if (isKeyExist) return;
                keys = (HashVector)_keyList[bucketId];
                long oldSize = keys.BucketCount * MemoryUtil.NetHashtableOverHead;
                lock (keys)
                {
                    keys[key] = null;
                }
                long newSize = keys.BucketCount * MemoryUtil.NetHashtableOverHead;
                long tmpsize = _keyListSize;
                _keyListSize = tmpsize + (newSize - oldSize);
            }
        }

        private void DecrementBucketStats(string key, int bucketId, long dataSize)
        {
            if (_stats.LocalBuckets != null && _stats.LocalBuckets.Contains(bucketId))
            {
                ((BucketStatistics)_stats.LocalBuckets[bucketId]).Decrement(dataSize);
            }
            bool isKeysEmpty = false;
            HashVector keys = null;
            if (_keyList != null)
            {

                _keyListLock.Enter();

                    if (_keyList.Contains(bucketId))
                    {
                        keys = (HashVector)_keyList[bucketId];
                    }

                _keyListLock.Exit();

                if (keys != null)
                {
                    long oldSize = keys.BucketCount * MemoryUtil.NetHashtableOverHead;
                    long newSize = 0;
                    lock (keys)
                    {
                        keys.Remove(key);
                        if (keys.Count != 0)
                            newSize = keys.BucketCount * MemoryUtil.NetHashtableOverHead;
                        else
                            isKeysEmpty = true;
                    }
                    long currentsize = _keyListSize;
                    _keyListSize = currentsize + (newSize - oldSize);
                    if (isKeysEmpty)
                    {
                        _keyListLock.Enter();

                            _keyList.Remove(bucketId);

                             _keyListLock.Exit();
                    }
                    if (_keyListSize < 0) _keyListSize = 0;
                }
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
        public override void AddLoggedData(ArrayList bucketIds)
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
                                    Remove(ide.Key, ItemRemoveReason.Removed, false, null, LockAccessType.IGNORE_LOCK, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
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
                                        Add(ide.Key, entry, false, false, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
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
                        Remove(ie.Current, ItemRemoveReason.Removed, false, false, null, LockAccessType.IGNORE_LOCK, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                    }
                }

            }
        }

        public override void RemoveBucket(int bucket)
        {
            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("HashedCache.RemoveBucket", "removing bucket :" + bucket);
            //Remove from stats
            if (LocalBuckets != null) LocalBuckets.Remove(bucket);
            //Remove actual data of the bucket
            RemoveBucketData(bucket);
            //remove operation logger for the bucket from log table if any exists.
            RemoveFromLogTbl(bucket);
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

                if (!LocalBuckets.Contains(ie.Current))
                {
                    LocalBuckets[ie.Current] = new BucketStatistics();
                }
            }
        }

        #region	/                 --- Cache overrides ---           /

        internal override CacheAddResult AddInternal(object key, CacheEntry cacheEntry, bool isUserOperation, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);

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
                    IncrementBucketStats(key as string, bucketId, clone.DataSize);
                    if (isUserOperation) _logMgr.LogOperation(bucketId, key, clone, OperationType.Add);
                }

                return result;
            }

            throw new StateTransferException("I am no more the owner of this bucket");
        }

        internal override bool AddInternal(object key, Alachisoft.NCache.Caching.AutoExpiration.ExpirationHint eh, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);

            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId))
                return base.AddInternal(key, eh, operationContext);

            throw new StateTransferException("I am no more the owner of this bucket");
        }

        internal override CacheInsResult InsertInternal(object key, CacheEntry cacheEntry, bool isUserOperation, CacheEntry oldEntry, OperationContext operationContext, bool updateIndex)
        {
            int bucketId = GetBucketId(key as string);

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
                        IncrementBucketStats(key as string, bucketId, cacheEntry.DataSize);
                        break;

                    case CacheInsResult.SuccessOverwriteNearEviction:
                    case CacheInsResult.SuccessOverwrite:
                        if (isUserOperation) _logMgr.LogOperation(bucketId, key, clone, OperationType.Insert);
                        DecrementBucketStats(key as string, bucketId, oldEntrysize);
                        IncrementBucketStats(key as string, bucketId, cacheEntry.DataSize);
                        break;
                }

                return result;
            }

            throw new StateTransferException("I am no more the owner of this bucket");
        }

        internal override bool RemoveInternal(object key, Alachisoft.NCache.Caching.AutoExpiration.ExpirationHint eh)
        {
            int bucketId = GetBucketId(key as string);

            if (_logMgr.IsOperationAllowed(bucketId) && LocalBuckets.Contains(bucketId))

                return base.RemoveInternal(key, eh);

            throw new StateTransferException("I am no more the owner of this bucket");
        }

        internal override CacheEntry RemoveInternal(object key, ItemRemoveReason removalReason, bool isUserOperation, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);

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

            CacheEntry entry = base.RemoveInternal(key, removalReason, isUserOperation, operationContext);
            if (entry != null)
            {
                DecrementBucketStats(key as string, bucketId, entry.DataSize);
                if (isUserOperation) _logMgr.LogOperation(bucketId, key, null, OperationType.Delete);
            }

            return entry;
        }

        internal override CacheEntry GetInternal(object key, bool isUserOperation, OperationContext operationContext)
        {
            int bucketId = GetBucketId(key as string);
            if (isUserOperation)
            {
                if (!_logMgr.IsOperationAllowed(bucketId))
                    throw new StateTransferException("I am no more the owner of this bucket");
            }
            CacheEntry entry = base.GetInternal(key, isUserOperation, operationContext);

            if (entry == null && (isUserOperation && !LocalBuckets.Contains(bucketId)))
                throw new StateTransferException("I am no more the owner of this bucket");

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
            if (_logMgr != null) _logMgr.Dispose(); //it clears the operation loggers for each bucket   
            _keyListSize = 0;

            if (LocalBuckets == null)
                return;
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

        public override void Dispose()
        {
            if (_logMgr != null) _logMgr.Dispose();
            if (_keyList != null) _keyList.Clear();
            _keyListSize = 0;
            base.Dispose();
        }

        #endregion


        public long InMemorySize
        {
            get
            {
                if (_keyList != null)
                    return _keyListSize + (_keyList.BucketCount * MemoryUtil.NetHashtableOverHead);
                else return 0;
            }
        }
        internal override long Size
        {
            get
            {
                return base.Size + InMemorySize;
            }
        }
    }
}