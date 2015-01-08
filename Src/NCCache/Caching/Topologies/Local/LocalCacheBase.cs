// Copyright (c) 2015 Alachisoft
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

using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Statistics;

using Alachisoft.NCache.Parser;
using Alachisoft.NCache.Caching.Queries.Filters;

using Alachisoft.NCache.Runtime.Exceptions;

using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Util;


using Alachisoft.NCache.Common.Locking;

using Alachisoft.NCache.Common.Stats;

using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Caching.Queries;



using System.Collections.Generic;

using Alachisoft.NCache.Common.Net;


using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Persistence;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    /// <summary>
    /// A class to serve as the base for all local cache implementations.
    /// </summary>

	internal class LocalCacheBase : CacheBase

    {
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

        private Hashtable _stateTransferKeyList;

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="listener"></param>
        /// <param name="timeSched"></param>
        /// <param name="asyncProcessor"></param>
        /// <param name="expiryMgr"></param>
        /// <param name="perfStatsColl"></param>
        public LocalCacheBase(IDictionary properties, CacheBase parentCache, ICacheEventsListener listener, CacheRuntimeContext context

)
            : base(properties, listener, context)
        {

            if (System.Configuration.ConfigurationSettings.AppSettings.Get("preparedQueryTableSize") != null)
            {
                _preparedQueryTableSize = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings.Get("preparedQueryTableSize"));
            }
            if (System.Configuration.ConfigurationSettings.AppSettings.Get("preparedQueryEvictionPercentage") != null)
            {
                _preparedQueryEvictionPercentage = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings.Get("preparedQueryEvictionPercentage"));
            }

            _stats = new CacheStatistics();

            _stats.InstanceName = _context.PerfStatsColl.InstanceName;

            _parentCache = parentCache;

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


        public override void UpdateClientsList(Hashtable list)
        {
            if (_stats != null) _stats.ClientsList = list;
        }

       


        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public sealed override void Clear(CallbackEntry cbEntry, OperationContext operationContext)
        {
            ClearInternal();
         
            if (IsSelfInternal)
            {
                _context.ExpiryMgr.Clear();

                if (_context.PerfStatsColl != null)
                {
                    if (_context.ExpiryMgr != null)
                        _context.PerfStatsColl.SetExpirationIndexSize(_context.ExpiryMgr.IndexInMemorySize);
                }

                _context.PerfStatsColl.IncrementCountStats((long)Count);

            }

            _stats.UpdateCount(this.Count);

            _stats.UpdateSessionCount(0);

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
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
            if (ContainsInternal(key))
            {
                CacheEntry e = GetInternal(key, true, operationContext);
                if (e == null) return false; 
                if (e.ExpirationHint != null && e.ExpirationHint.CheckExpired(_context))
                {
                    ExpirationHint exh = e.ExpirationHint;
                    ItemRemoveReason reason = ItemRemoveReason.Expired;
                    
					Remove(key, reason, true, null, LockAccessType.IGNORE_LOCK, operationContext);
                    
					return false;
                }
                return true;
            }
            return false;
        }

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
                
                _context.ExpiryMgr.UpdateIndex(key, entry.ExpirationHint);
            }

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }
            return result;
        }

        public override LockOptions Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            LockOptions lockInfo = new LockOptions();

            CacheEntry e = GetInternal(key, false, operationContext);
            if (e != null)
            {
                e.Lock(lockExpiration, ref lockId, ref lockDate);
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
        /// <param name="lockId"></param>
        /// <param name="lockDate"></param>
        /// <param name="lockExpiration"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
        /// <returns>cache entry.</returns>
        public sealed override CacheEntry Get(object key, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            return Get(key, true, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
        }


        private Hashtable GetFromCache(ArrayList keys, OperationContext operationContext)
        {
            if (keys == null) return null;

            return GetEntries(keys.ToArray(), operationContext);
        }

        private Hashtable RemoveFromCache(ArrayList keys, bool notify, OperationContext operationContext)
        {
            if (keys == null) return null;

            return Remove(keys.ToArray(), ItemRemoveReason.Removed, notify, operationContext);

        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="isUserOperation"></param>
        /// <param name="lockId"></param>
        /// <param name="lockDate"></param>
        /// <param name="lockExpiration"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
        /// <returns>cache entry.</returns>
        public sealed override CacheEntry Get(object key, bool isUserOperation, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
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
                    else if (accessType == LockAccessType.ACQUIRE && !e.Lock(lockExpiration, ref lockId, ref lockDate))//internally sets the out parameters
                    {
                        e = null;
                    }
                }
                else
                {
                    lockId = null;
                }
            }

            ExpirationHint exh = (e == null ? null : e.ExpirationHint);
            if (exh != null)
            {
                if (exh.CheckExpired(_context))
                {
                    // If cache forward is set we skip the expiration.
                    if (!exh.NeedsReSync)
                    {

                        ItemRemoveReason reason = ItemRemoveReason.Expired;
                        
                        Remove(key, reason, true, null, LockAccessType.IGNORE_LOCK, operationContext);

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
        public override sealed CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, bool isUserOperation,
            OperationContext operationContext)
        {

            CacheAddResult result = CacheAddResult.Failure;
           if (_stateTransferKeyList != null && _stateTransferKeyList.ContainsKey(key) && notify)
                    return CacheAddResult.KeyExists;
                result = AddInternal(key, cacheEntry, isUserOperation);

                // Not enough space, evict and try again.
                if (result == CacheAddResult.NeedsEviction || result == CacheAddResult.SuccessNearEviction)
                {
                    Evict();
                    if (result == CacheAddResult.SuccessNearEviction)
                        result = CacheAddResult.Success;
                }

                if (result == CacheAddResult.KeyExists)
                {
                    CacheEntry e = GetInternal(key, isUserOperation, operationContext);
                    if (e.ExpirationHint != null && e.ExpirationHint.CheckExpired(_context))
                    {
                        ExpirationHint exh = e.ExpirationHint;

                        Remove(key, ItemRemoveReason.Expired, true, null, LockAccessType.IGNORE_LOCK, operationContext);
                    }
                }

                // Operation completed!
                if (result == CacheAddResult.Success)
                {
                    if (cacheEntry.ExpirationHint != null)
                    {
                        cacheEntry.ExpirationHint.CacheKey = (string) key;

                        try
                        {
                            _context.ExpiryMgr.ResetHint(null, cacheEntry.ExpirationHint); //:muds
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
                        _context.PerfStatsColl.IncrementCountStats((long) Count);
                    }
                    _stats.UpdateCount(this.Count);
                }

                if (_context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.SetCacheSize(Size);
                }

               return result;
            }
        

        public sealed override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            return Insert(key, cacheEntry, notify, true, lockId, accessType, operationContext);
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
        public sealed override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, bool isUserOperation, object lockId, LockAccessType access, OperationContext operationContext)
        {
            CacheInsResultWithEntry result = new CacheInsResultWithEntry();
            try
            {
                CacheEntry pe = null;
                CallbackEntry cbEtnry = null;
                OperationID opId = operationContext.OperatoinID;
                EventId eventId = null;
                EventContext eventContext = null;
                
                pe = GetInternal(key, false, operationContext);
                result.Entry = pe;

                if (pe != null && access != LockAccessType.IGNORE_LOCK)
                {
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
                ExpirationHint peExh = pe == null ? null : pe.ExpirationHint;

                if (pe != null && pe.Value is CallbackEntry)
                {
                    cbEtnry = pe.Value as CallbackEntry;
                    cacheEntry = CacheHelper.MergeEntries(pe, cacheEntry);
                }
                result.Result = InsertInternal(key, cacheEntry, isUserOperation, pe, operationContext);

                if ((result.Result == CacheInsResult.Success || result.Result == CacheInsResult.SuccessNearEvicition) && _stateTransferKeyList != null &&
                    _stateTransferKeyList.ContainsKey(key))
                {
                    result.Result = result.Result == CacheInsResult.Success ? CacheInsResult.SuccessOverwrite : CacheInsResult.SuccessOverwriteNearEviction;
                }
                // Not enough space, evict and try again.
                if (result.Result == CacheInsResult.NeedsEviction || result.Result == CacheInsResult.SuccessNearEvicition
                    || result.Result == CacheInsResult.SuccessOverwriteNearEviction)
                {
                    Evict();
                    if (result.Result == CacheInsResult.SuccessNearEvicition) result.Result = CacheInsResult.Success;
                    if (result.Result == CacheInsResult.SuccessOverwriteNearEviction) result.Result = CacheInsResult.SuccessOverwrite;
                }

                // Operation completed!
                if (result.Result == CacheInsResult.Success || result.Result == CacheInsResult.SuccessOverwrite)
                {
                    // commented by muds
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
                }

                _stats.UpdateCount(this.Count);
                switch (result.Result)
                {
                    case CacheInsResult.Success:
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
                        }
                        break;
                }
            }
            finally
            {
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
        /// <param name="lockId"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
        /// <returns>item value</returns>
        public sealed override CacheEntry Remove(object key, ItemRemoveReason removalReason, bool notify, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            return Remove(key, removalReason, notify, true, lockId, accessType, operationContext);
        }

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <param name="isUserOperation"></param>
        /// <param name="lockId"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
        /// <returns>item value</returns>
        public override CacheEntry Remove(object key, ItemRemoveReason removalReason, bool notify, bool isUserOperation, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry e = null;
            CacheEntry pe = null;
            {
                object actualKey = key;
                if (key is object[])
                {
                    actualKey = ((object[])key)[0];
                }

                if (accessType != LockAccessType.IGNORE_LOCK)
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
                EventId eventId = null;
                EventContext eventContext = null;
                OperationID opId = operationContext.OperatoinID;
                if (e != null)
                {
                    if (_stateTransferKeyList != null && _stateTransferKeyList.ContainsKey(key))
                        _stateTransferKeyList.Remove(key);
                    // commented by muds
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

                    if (IsSelfInternal)
                    {
                        // Disposed the one and only cache entry.
                        ((IDisposable)e).Dispose();

                        if (removalReason == ItemRemoveReason.Expired)
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
                        CallbackEntry cbEtnry = e.Value as CallbackEntry;// e.DeflattedValue(_context.SerializationContext);
                        
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
                            eventContext.Item = eventCacheEntry;
                            eventContext.Add(EventContextFieldName.ItemRemoveCallbackList, cbEtnry.ItemRemoveCallbackListener.Clone());
                            
                            //Will always reaise the whole entry for old clients
                            NotifyCustomRemoveCallback(actualKey, e, removalReason, false, (OperationContext)operationContext.Clone(), eventContext);
                        }
                    }

                   

                }
                else if (_stateTransferKeyList != null && _stateTransferKeyList.ContainsKey(key))
                {
                    _stateTransferKeyList.Remove(key);        
                }

            }
            _stats.UpdateCount(this.Count);

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

        public sealed override QueryResultSet Search(string query, IDictionary values, OperationContext operationContext)
        {
            try
            {
                _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();

                QueryContext queryContext = PrepareSearch(query, values);
                switch (queryContext.ResultSet.Type)
                {
                    case QueryType.AggregateFunction:
                        break;

                    default:
                        queryContext.Tree.Reduce();
                        queryContext.CacheContext = _context.SerializationContext;
                        queryContext.ResultSet.SearchKeysResult = queryContext.Tree.LeftList;
                        break;
                }

                _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();

                if (queryContext.ResultSet != null)
                {

                    long totalRowReturn = 0;
                    
                    if (queryContext.ResultSet.SearchEntriesResult != null)
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
                RemoveReduction(query);
                throw new Runtime.Exceptions.ParserException(pe.Message, pe);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public sealed override QueryResultSet SearchEntries(string query, IDictionary values, OperationContext operationContext)
        {
            try
            {
                _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();

                QueryContext queryContext = PrepareSearch(query, values);
                switch (queryContext.ResultSet.Type)
                {
                    case QueryType.AggregateFunction:
                        break;

                    default:
                        Hashtable result = new Hashtable();
                        ICollection keyList = null;
                        ArrayList updatekeys =null;
                        queryContext.Tree.Reduce();
                        queryContext.CacheContext = _context.SerializationContext;
                        if (queryContext.Tree.LeftList.Count > 0)
                            keyList = queryContext.Tree.LeftList;

                        if (keyList != null && keyList.Count > 0)
                        {

                            object[] keys = new object[keyList.Count];
                            keyList.CopyTo(keys, 0);

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
                                    if ((entry.ExpirationHint != null && entry.ExpirationHint.IsVariant))
                                    {
                                        if (updatekeys == null)
                                            updatekeys = new ArrayList();
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
                  
                    if (queryContext.ResultSet.SearchEntriesResult != null)
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
                RemoveReduction(query);
                throw new Runtime.Exceptions.ParserException(pe.Message, pe);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private QueryContext PrepareSearch(string query, IDictionary values)
        {
            Reduction currentQueryReduction = null;

            try
            {
                currentQueryReduction = GetPreparedReduction(query);
                if (currentQueryReduction.Tokens[0].ToString().ToLower() != "select")
                    throw new Parser.ParserException("Only select query is supported");

                return SearchInternal(currentQueryReduction.Tag as Predicate, values);
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
        public sealed override Hashtable Get(object[] keys, OperationContext operationContext)
        {
            Hashtable entries = new Hashtable();
            CacheEntry e = null;
            for (int i = 0; i < keys.Length; i++)
            {
                try
                {
                    
                    if(operationContext != null)
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
        private Hashtable GetEntries(object[] keys, OperationContext operationContext)
        {
            Hashtable entries = new Hashtable();
            CacheEntry e = null;

            for (int i = 0; i < keys.Length; i++)
            {
                try
                {
                    e = GetEntryInternal(keys[i], true);

                    if (e != null)
                    {
                        ExpirationHint exh = e.ExpirationHint;
                        if (exh != null)
                        {
                            if (exh.CheckExpired(_context))
                            {
                                ItemRemoveReason reason = ItemRemoveReason.Expired;
                                // If cache forward is set we skip the expiration.
                                if (!exh.NeedsReSync)
                                {
                                    Remove(keys[i], reason, true, null, LockAccessType.IGNORE_LOCK, operationContext);
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
                                    RemoveInternal(keys[i], ItemRemoveReason.Removed, false, operationContext);
                                    throw ex;
                                }
                            }
                        }
                        if(e!=null)
                            entries[keys[i]] = e;
                    }
                }
                catch (Exception ex)
                {
                    entries[keys[i]] = ex;
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
            for (int i = 0; i < keys.Length; i++)
            {
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
                    operationContext.RemoveValueByField(OperationContextFieldName.EventContext);
                }
            }

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
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
            for (int i = 0; i < keys.Length; i++)
            {
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
                    CacheInsResultWithEntry result = Insert(keys[i], cacheEntries[i], notify, null, LockAccessType.IGNORE_LOCK, operationContext);
                    table.Add(keys[i], result);
                }
                catch (Exception e)
                {
                    table[keys[i]] = e;
                }
                finally
                {
                    operationContext.RemoveValueByField(OperationContextFieldName.EventContext);
                }
            }

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }

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
        public sealed override Hashtable Remove(object[] keys, ItemRemoveReason removalReason, bool notify, OperationContext operationContext)
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
        public override Hashtable Remove(object[] keys, ItemRemoveReason removalReason, bool notify, bool isUserOperation, OperationContext operationContext)
        {
            Hashtable table = new Hashtable();
            EventContext eventContext = null;
            EventId eventId = null;
            OperationID opId = operationContext.OperatoinID;
            for (int i = 0; i < keys.Length; i++)
            {
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
                    CacheEntry e = Remove(keys[i], removalReason, notify, null, LockAccessType.IGNORE_LOCK, operationContext);
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
                    operationContext.RemoveValueByField(OperationContextFieldName.EventContext);
                }
            }


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
        internal virtual CacheAddResult AddInternal(object key, CacheEntry cacheEntry, bool isUserOperation)
        {
            return CacheAddResult.Failure;
        }

        internal virtual bool AddInternal(object key, ExpirationHint eh, OperationContext operationContext)
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
        internal virtual CacheInsResult InsertInternal(object key, CacheEntry cacheEntry, bool isUserOperation, CacheEntry oldEntry, OperationContext operationContext)
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

        internal virtual QueryContext SearchInternal(Predicate pred, IDictionary values)
        {
            return null;
        }

        internal virtual IDictionary SearchEntriesInternal(Predicate pred, IDictionary values)
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
                    RegisterKeyNotification(key, updateCallback, removeCallback, operationContext);
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
            CacheEntry entry = Get(key, operationContext);

            if (entry != null)
            {

                entry.AddCallbackInfo(updateCallback, removeCallback);
            }
        }

        public override void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            if (keys != null)
            {
                foreach (string key in keys)
                    UnregisterKeyNotification(key, updateCallback, removeCallback, operationContext);
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
                    entry.RemoveCallbackInfo(updateCallback, removeCallback);
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
                if (!_preparedQueryTable.ContainsKey(query))
                {
                    ParserHelper parser = new ParserHelper(InternalCache.NCacheLog);
                    if (parser.Parse(query) == ParseMessage.Accept)
                    {
                        reduction = parser.CurrentReduction;
                        AddPreparedReduction(query, reduction);
                    }
                    else
                    {
                        throw new Parser.ParserException("Incorrect query format");
                    }
                }
                else
                    reduction = (Reduction)_preparedQueryTable[query];
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

    }
}
