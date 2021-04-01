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
using System.Threading;
using Alachisoft.NCache.Storage;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using System.Net;
using Alachisoft.NCache.Common;
using System.Collections.Generic;
using Alachisoft.NCache.Caching.Messaging;

using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Runtime.Caching;
using System.Text.RegularExpressions;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Caching.Enumeration;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Util;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    /// <summary>
    /// This class provides the local storage options i.e. the actual storage of objects. It is used 
    /// by the Cache Manager, replicated cache and partitioned cache.
    /// </summary>
    internal class LocalCache : LocalCacheBase
    {
        /// <summary> The eviction policy for the cache. </summary>
        protected IEvictionPolicy _evictionPolicy;

        /// <summary> Thread which evicts item when cache is full.</summary>
        private Thread _evictionThread;

        private object _eviction_sync_mutex = new object();

        /// <summary>Flag which indicates whether to explicitly call GC.Collect or not</summary>
        private bool _allowExplicitGCCollection = true;

        private bool _notifyCacheFull = false;

        private TopicManager _topicManager;

        private EnumerationIndex _enumerationIndex;

        /// <summary>
        /// Overloaded constructor. Takes the properties as a map.
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        /// <param name="listener">cache events listener</param>
        /// <param name="timeSched">scheduler to use for periodic tasks</param>
        public LocalCache(IDictionary cacheClasses, CacheBase parentCache, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(properties, parentCache, listener, context)  
        {
            _stats.ClassName = "local-cache";
            _topicManager = new TopicManager(context);
            Initialize(cacheClasses, properties);
            CreateEventsTopics();
            _cacheStore.ISizableEvictionIndexManager = _evictionPolicy;
            _cacheStore.ISizableExpirationIndexManager = _context.ExpiryMgr;
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (_cacheStore != null)
            {
                _cacheStore.Dispose();
                _cacheStore = null;
            }
            base.Dispose();
        }

        #endregion

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        public override long Count
        {

            get
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LocalCache.Count", "");

                if (_cacheStore == null)
                    throw new InvalidOperationException();

                return _cacheStore.Count;
            }
        }

        public override long SessionCount
        {
            get
            {
                return _stats.SessionCount;
            }
        }

        public override IPAddress ServerJustLeft
        {
            get { return null; }
            set {; }
        }

        public override int ServersCount
        {
            get { return 1; }
        }

        public override bool IsServerNodeIp(Address clientAddress)
        {
            return false;
        }

        /// <summary>
        /// Get the size of data in store, in bytes.
        /// </summary>
        internal override long Size
        {
            get
            {
                if (_cacheStore != null)
                    return _cacheStore.Size;
                return 0;
            }
        }

        internal override float EvictRatio
        {
            get
            {
                if (_evictionPolicy != null) return _evictionPolicy.EvictRatio;
                return 0;
            }
            set
            {
                if (_evictionPolicy != null)
                {
                    _evictionPolicy.EvictRatio = value;
                }
            }
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
                return _cacheStore.VirtualUnlimitedSpace;
            }
            set
            {
                _cacheStore.VirtualUnlimitedSpace = value;

            }
        }

        internal override long MaxSize
        {
            get
            {
                if (_cacheStore != null) return _cacheStore.MaxSize;
                return 0;
            }
            set
            {
                if (_cacheStore != null)
                {
                    //muds:
                    //if the cache has less data than the new maximum size.
                    //we can not apply the new size to the cache if the cache has already more data.
                    if (_cacheStore.Size <= value)
                    {
                        _cacheStore.MaxSize = value;
                        _stats.MaxSize = value;
                    }
                    else
                    {
                        throw new Exception("You need to remove some data from cache before applying the new size");
                    }
                }
            }
        }

        internal override bool CanChangeCacheSize(long size)
        {
            return (_cacheStore.Size <= size);
        }

        /// <summary>
        /// Method that allows the object to initialize itself. Passes the property map down 
        /// the object hierarchy so that other objects may configure themselves as well..
        /// </summary>
        /// <param name="properties">configuration properties</param>
        protected override void Initialize(IDictionary cacheClasses, IDictionary properties)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                base.Initialize(cacheClasses, properties);

                _allowExplicitGCCollection = ServiceConfiguration.EnableGCCollection;

                if (!properties.Contains("storage"))
                    throw new ConfigurationException("Missing configuration option 'storage'");

                if (properties.Contains("scavenging-policy"))
                {
                    IDictionary evictionProps = properties["scavenging-policy"] as IDictionary;
                    
                    if (Convert.ToDouble(evictionProps["evict-ratio"]) > 0)
                        _evictionPolicy = EvictionPolicyFactory.CreateEvictionPolicy(evictionProps, _context.NCacheLog);
                }
                else
                {
                    _evictionPolicy = EvictionPolicyFactory.CreateDefaultEvictionPolicy();
                }

                IDictionary storageProps = properties["storage"] as IDictionary;

                _cacheStore = CacheStorageFactory.CreateStorageProvider(storageProps, this._context.SerializationContext, _evictionPolicy != null, _context.NCacheLog, null);

                _cacheStore.ISizableMessageStore = _topicManager;
                _stats.MaxCount = _cacheStore.MaxCount;
                _stats.MaxSize = _cacheStore.MaxSize;
            }
            catch (ConfigurationException e)
            {
                if (_context != null)
                {
                    _context.NCacheLog.Error("LocalCache.Initialize()", e.ToString());
                }
                Dispose();
                throw;
            }
            catch (Exception e)
            {
                if (_context != null)
                {
                    _context.NCacheLog.Error("LocalCache.Initialize()", e.ToString());
                }
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }

        #region	/                 --- LocalCacheBase ---           /

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        internal override void ClearInternal()
        {
            if (_cacheStore == null)
                throw new InvalidOperationException();

            _cacheStore.Clear();

            _topicManager.Clear();

            _context.PerfStatsColl.SetCacheSize(0); // on clear cache cachesize set to zero

            if (_evictionThread != null)
            {
                NCacheLog.Flush();
#if !NETCORE
                _evictionThread.Abort();
#elif NETCORE
                _evictionThread.Interrupt();
#endif
            }

            if (_evictionPolicy != null)
            {
                _evictionPolicy.Clear();

                if (_context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.SetEvictionIndexSize(_evictionPolicy.IndexInMemorySize);
                }

            }
        }

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element 
        /// with the specified key; otherwise, false.</returns>
        internal override bool ContainsInternal(object key)
        {

            if (_cacheStore == null)
                throw new InvalidOperationException();
            return _cacheStore.Contains(key);
        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        internal override CacheEntry GetInternal(object key, bool isUserOperation, OperationContext operationContext, bool cloneEntry, bool needUserPayload)
        {

            if (_cacheStore == null)
                throw new InvalidOperationException();

            CacheEntry e = (CacheEntry)_cacheStore.Get(key);
            if (e != null)
            {
                EvictionHint evh = e.EvictionHint;
                if (isUserOperation && _evictionPolicy != null && evh != null && evh.IsVariant)
                    _evictionPolicy.Notify(key, evh, null);
                
                e.MarkInUse(NCModulesConstants.Global);

                if (cloneEntry)
                {
                    e = e.DeepClone(operationContext.UseObjectPool? _context.TransactionalPoolManager: _context.FakeObjectPool,true);
                }

            }
            return e;
        }

        /// <summary>
        /// Special purpose internal method. Derived classes don't override this method.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isUserOperation"></param>
        /// <returns></returns>
        internal override CacheEntry GetEntryInternal(object key, bool isUserOperation,OperationContext operationContext, bool cloneCacheEntry, bool needUserPayload)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LocalCache.GetInternal", "");

            if (_cacheStore == null)
                throw new InvalidOperationException();

            CacheEntry e = (CacheEntry)_cacheStore.Get(key);
            if (e == null) return e;

            EvictionHint evh = e.EvictionHint;
            if (isUserOperation && _evictionPolicy != null && evh != null && evh.IsVariant)
                _evictionPolicy.Notify(key, evh, null);
            e.MarkInUse(NCModulesConstants.Global);

            if (cloneCacheEntry) e = e.DeepClone(operationContext.UseObjectPool ? _context.TransactionalPoolManager : _context.FakeObjectPool);

            return e;
        }

       

        /// <summary>
        /// Get the item size stored in cache
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>item size</returns>
        public override int GetItemSize(object key)
        {
            if (_cacheStore == null) return 0;
            return _cacheStore.GetItemSize(key);
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        internal override CacheAddResult AddInternal(object key, CacheEntry cacheEntry, bool isUserOperation, OperationContext operationContext)
        {
            try
            {
                if (_cacheStore == null)
                    throw new InvalidOperationException();
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.LocalCache);

                if (_evictionPolicy != null)
                {
                    if (cacheEntry.EvictionHint is PriorityEvictionHint)
                        cacheEntry.Priority = ((PriorityEvictionHint)cacheEntry.EvictionHint).Priority;

                    EvictionHint oldEvictionHint = cacheEntry.EvictionHint;

                    cacheEntry.EvictionHint = _evictionPolicy.CompatibleHint(cacheEntry.EvictionHint, _context.StorePoolManager);

                    if (oldEvictionHint != null && !ReferenceEquals(oldEvictionHint, cacheEntry.EvictionHint))
                        MiscUtil.ReturnEvictionHintToPool(oldEvictionHint, _context.StorePoolManager);

                }

                // No Need to insert Eviction if Eviction is turned off it will reduce cache-entry overhead

                if (_evictionPolicy == null)
                    cacheEntry.EvictionHint = null;

                StoreAddResult result = _cacheStore.Add(key, cacheEntry, !isUserOperation);
                if (result == StoreAddResult.Success || result == StoreAddResult.SuccessNearEviction)
                {
                    cacheEntry.IsStored = true;
                    if (_evictionPolicy != null)
                        _evictionPolicy.Notify(key, null, cacheEntry.EvictionHint);
                }
                if (result == StoreAddResult.NotEnoughSpace && !_notifyCacheFull)
                {
                    _notifyCacheFull = true;
                    _context.NCacheLog.Error("LocalCache.AddInternal", "The cache is full and not enough items could be evicted.");
                }

                if (_context.PerfStatsColl != null)
                {
                    if (_evictionPolicy != null)
                        _context.PerfStatsColl.SetEvictionIndexSize((long)_evictionPolicy.IndexInMemorySize);

                    if (_context.ExpiryMgr != null)
                        _context.PerfStatsColl.SetExpirationIndexSize((long)_context.ExpiryMgr.IndexInMemorySize);
                }

                switch (result)
                {
                    case StoreAddResult.Success: return CacheAddResult.Success;
                    case StoreAddResult.KeyExists: return CacheAddResult.KeyExists;
                    case StoreAddResult.NotEnoughSpace: return CacheAddResult.NeedsEviction;
                    case StoreAddResult.SuccessNearEviction: return CacheAddResult.SuccessNearEviction;
                }

                return CacheAddResult.Failure;
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.LocalCache);

            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        internal override bool AddInternal(object key, ExpirationHint eh, OperationContext operationContext)
        {
            CacheEntry e = null;
            try
            {

                if (_cacheStore == null)
                    throw new InvalidOperationException();

                e = (CacheEntry)_cacheStore.Get(key);
               
                if (e == null) return false;
               
                //We only allow either idle expiration or Fixed expiration both cannot be set at the same time
                if ((e.ExpirationHint is IdleExpiration && eh is FixedExpiration)
                    || (e.ExpirationHint is FixedExpiration && eh is IdleExpiration))
                {
                    return false;
                }

                if (e.ExpirationHint == null)
                {
                    e.ExpirationHint = eh;
                }
                else
                {
                    if (e.ExpirationHint is AggregateExpirationHint)
                    {
                        ((AggregateExpirationHint)e.ExpirationHint).Add(eh);
                    }
                    else
                    {
                        AggregateExpirationHint aeh = AggregateExpirationHint.Create(Context.StorePoolManager);
                        aeh.Add(e.ExpirationHint);
                        aeh.Add(eh);
                        e.ExpirationHint = aeh;
                    }
                }
                e.MarkFree(NCModulesConstants.Global);
                e.MarkInUse(NCModulesConstants.LocalCache);

                //Our store may not be an in memory store

                _cacheStore.Insert(key, e, true);
                e.LastModifiedTime = System.DateTime.Now;

                if (_context.PerfStatsColl != null)
                {
                    if (_evictionPolicy != null)
                        _context.PerfStatsColl.SetEvictionIndexSize((long)_evictionPolicy.IndexInMemorySize);

                    if (_context.ExpiryMgr != null)
                        _context.PerfStatsColl.SetExpirationIndexSize((long)_context.ExpiryMgr.IndexInMemorySize);
                }
                return true;
            }
            finally
            {
                if (e!=null)
                    e.MarkFree(NCModulesConstants.LocalCache);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        internal override bool RemoveInternal(object key, ExpirationHint eh)
        {
            CacheEntry e = null;
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LocalCache.Remove", "");

                if (_cacheStore == null)
                    throw new InvalidOperationException();

                 e = (CacheEntry)_cacheStore.Get(key);
                if (e == null || e.ExpirationHint == null)
                {
                    return false;
                }
                else
                {
                    e.MarkInUse(NCModulesConstants.LocalCache);

                    if (e.ExpirationHint is AggregateExpirationHint)
                    {
                        AggregateExpirationHint AggHint = AggregateExpirationHint.Create(Context.StorePoolManager);
                        AggregateExpirationHint entryExpHint = (AggregateExpirationHint)e.ExpirationHint;

                        foreach (ExpirationHint exp in entryExpHint)
                        {
                            if (!exp.Equals(eh))
                            {
                                AggHint.Add(exp);
                            }
                            else
                            {
                                MiscUtil.ReturnExpirationHintToPool(exp, Context.StorePoolManager);
                            }
                        }
                        e.ExpirationHint = AggHint;
                    }
                    else if (e.ExpirationHint.Equals(eh))
                    {
                        MiscUtil.ReturnExpirationHintToPool(e.ExpirationHint, Context.StorePoolManager);
                        e.ExpirationHint = null;
                    }
                }

                // Our store may not be an in memory store
                if (_notifyCacheFull)
                {
                    _notifyCacheFull = false;
                }
                _cacheStore.Insert(key, e, true);
                e.LastModifiedTime = System.DateTime.Now;

                if (_context.PerfStatsColl != null)
                {
                    if (_evictionPolicy != null)
                        _context.PerfStatsColl.SetEvictionIndexSize((long)_evictionPolicy.IndexInMemorySize);

                    if (_context.ExpiryMgr != null)
                        _context.PerfStatsColl.SetExpirationIndexSize((long)_context.ExpiryMgr.IndexInMemorySize);
                }

                return true;
            }
            finally
            {
                if (e != null)
                    e.MarkFree(NCModulesConstants.LocalCache);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal override bool AddInternal(object key)
        {
            CacheEntry e = null;
            try
            {
                if (_cacheStore == null)
                    throw new InvalidOperationException();

                 e = (CacheEntry)_cacheStore.Get(key);
                if (e == null) return false;
                e.MarkInUse(NCModulesConstants.LocalCache);
                //Our store may not be an in memory store

                _cacheStore.Insert(key, e, true);
                e.LastModifiedTime = System.DateTime.Now;
                return true;
            }
            finally
            {
                if (e != null) e.MarkFree(NCModulesConstants.LocalCache);
            }
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        internal override CacheInsResult InsertInternal(object key, CacheEntry cacheEntry, bool isUserOperation, CacheEntry oldEntry, OperationContext operationContext, bool updateIndex)
        {
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.LocalCache);

                if (_cacheStore == null)
                    throw new InvalidOperationException();

                if (oldEntry != null && cacheEntry.Type != oldEntry.Type)
                    throw new Exception(string.Format("Cannot update '{0}' type with '{1}' type.", oldEntry.Type, cacheEntry.Type));

                if (cacheEntry.EvictionHint is PriorityEvictionHint)
                    cacheEntry.Priority = ((PriorityEvictionHint)cacheEntry.EvictionHint).Priority;

                if (_evictionPolicy != null)
                {
                    EvictionHint oldEvictionHint = cacheEntry.EvictionHint;

                    cacheEntry.EvictionHint = _evictionPolicy.CompatibleHint(cacheEntry.EvictionHint, _context.StorePoolManager);

                    if (oldEvictionHint != null && !ReferenceEquals(oldEvictionHint, cacheEntry.EvictionHint))
                        MiscUtil.ReturnEvictionHintToPool(oldEvictionHint, _context.StorePoolManager);
                }

                EvictionHint peEvh = oldEntry == null ? null : oldEntry.EvictionHint;

                // No Need to insert Eviction if Eviction is turned off it will reduce cache-entry overhead
                if (_evictionPolicy == null)
                    cacheEntry.EvictionHint = null;

                CacheEntry storedEntry = _cacheStore.Get(key) as CacheEntry;

                StoreInsResult result = _cacheStore.Insert(key, cacheEntry, !isUserOperation);

                // Operation completed!            
                if (result == StoreInsResult.Success || result == StoreInsResult.SuccessNearEviction)
                {
                    //This means that entry has become part of the store
                    cacheEntry.IsStored = true;
                    if (_evictionPolicy != null)
                        _evictionPolicy.Notify(key, null, cacheEntry.EvictionHint);

                    if (!object.ReferenceEquals(storedEntry, cacheEntry)) MiscUtil.ReturnEntryToPool(storedEntry, _context.StorePoolManager);
                }
                else if (result == StoreInsResult.SuccessOverwrite || result == StoreInsResult.SuccessOverwriteNearEviction)
                {
                    if(!object.ReferenceEquals(storedEntry,cacheEntry))  MiscUtil.ReturnEntryToPool(storedEntry, _context.StorePoolManager);
                    //This means that entry has become part of the store
                    cacheEntry.IsStored = true;

                    if (oldEntry != null)
                    {
                        //update the cache item version...
                        if (isUserOperation)
                        {
                            cacheEntry.UpdateVersion(oldEntry);
                        }

                        //update the cache item last modifeid time...
                        cacheEntry.UpdateLastModifiedTime(oldEntry);
                    }

                    if (_evictionPolicy != null)
                        _evictionPolicy.Notify(key, peEvh, cacheEntry.EvictionHint);
                }
                if (result == StoreInsResult.NotEnoughSpace && !_notifyCacheFull)
                {
                    _notifyCacheFull = true;
                    _context.NCacheLog.Error("LocalCache.InsertInternal", "The cache is full and not enough items could be evicted.");
                }

                if (_context.PerfStatsColl != null)
                {
                    if (_evictionPolicy != null)
                        _context.PerfStatsColl.SetEvictionIndexSize((long)_evictionPolicy.IndexInMemorySize);

                    if (_context.ExpiryMgr != null)
                        _context.PerfStatsColl.SetExpirationIndexSize((long)_context.ExpiryMgr.IndexInMemorySize);
                }

                switch (result)
                {
                    case StoreInsResult.Success: return CacheInsResult.Success;
                    case StoreInsResult.SuccessOverwrite: return CacheInsResult.SuccessOverwrite;
                    case StoreInsResult.NotEnoughSpace: return CacheInsResult.NeedsEviction;
                    case StoreInsResult.SuccessNearEviction: return CacheInsResult.SuccessNearEvicition;
                    case StoreInsResult.SuccessOverwriteNearEviction: return CacheInsResult.SuccessOverwriteNearEviction;
                }

                return CacheInsResult.Failure;
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.LocalCache);
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
        internal override CacheEntry RemoveInternal(object key, ItemRemoveReason removalReason, bool isUserOperation, OperationContext operationContext,bool cloneEntry, bool needUserPayload)
        {

            if (_cacheStore == null)
                throw new InvalidOperationException();

            CacheEntry e = (CacheEntry)_cacheStore.Remove(key);
            if (e != null)
            {
                //Entry is no more part of store
                e.IsStored = false;

                if (_evictionPolicy != null && e.EvictionHint != null)
                    _evictionPolicy.Remove(key, e.EvictionHint);

                if (_notifyCacheFull)
                {
                    _notifyCacheFull = false;
                }

                if (cloneEntry)
                {
                    var clone = e.DeepClone(operationContext.UseObjectPool ? _context.TransactionalPoolManager : _context.FakeObjectPool,true);
                    //return entry to the store pool
                    MiscUtil.ReturnEntryToPool(e, _context.StorePoolManager);
                    e = clone;
                }
            }

            if (_context.PerfStatsColl != null)
            {
                if (_evictionPolicy != null)
                    _context.PerfStatsColl.SetEvictionIndexSize((long)_evictionPolicy.IndexInMemorySize);

                if (_context.ExpiryMgr != null)
                    _context.PerfStatsColl.SetExpirationIndexSize((long)_context.ExpiryMgr.IndexInMemorySize);
            }
            return e;
        }

        public override EnumerationDataChunk GetNextChunk(EnumerationPointer pointer, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return null;
            if (_enumerationIndex == null)
                _enumerationIndex = new EnumerationIndex(this);

            EnumerationDataChunk nextChunk = _enumerationIndex.GetNextChunk(pointer);

            return nextChunk;
        }

        public override bool HasEnumerationPointer(EnumerationPointer pointer)
        {
            if (_enumerationIndex == null)
                return false;

            return _enumerationIndex.Contains(pointer);
        }
        #endregion

        #region	/                 --- ICache ---           /

        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        /// <returns>IDictionaryEnumerator enumerator.</returns>
        public override IDictionaryEnumerator GetEnumerator()
        {
            if (_cacheStore == null)
                throw new InvalidOperationException();

            return _cacheStore.GetEnumerator();
        }

        public override Array Keys
        {
            get
            {
                if (_cacheStore == null)
                    throw new InvalidOperationException();
                return _cacheStore.Keys;
            }
        }

        #endregion

        /// <summary>
        /// Evicts items from the store.
        /// </summary>
        /// <returns></returns>
        public override void Evict()
        {
            if (_evictionPolicy == null)
            {
                return;
            }
            lock (_eviction_sync_mutex)
            {
                if (_parentCache.IsEvictionAllowed)
                {
                    if (_allowAsyncEviction)
                    {
                        if (_evictionThread == null)
                        {
                            _evictionThread = new Thread(new ThreadStart(EvictAysnc));
                            _evictionThread.IsBackground = true;
                            _evictionThread.Start();
                        }
                    }
                    else
                    {
                        DoEvict(this);
                    }
                }
            }
        }

        /// <summary>
        /// Evicts the items from the cache.
        /// </summary>
        private void DoEvict(CacheBase cache)
        {
            try
            {
                if (_evictionPolicy != null)
                {
                    var sizeToEvict = (long)Math.Ceiling(Size * _evictionPolicy.EvictRatio);
                    long evictedSize = _evictionPolicy.Execute(cache, _context, Size);

                    if (sizeToEvict > evictedSize)
                    {
                        _context.MessageManager.Evict(sizeToEvict - evictedSize);
                    }
                }
            }
            finally
            {
            }
        }

        /// <summary>
        /// Called by async thread to evict the item from the store.
        /// </summary>
        private void EvictAysnc()
        {
            try
            {
                if (!IsSelfInternal)
                    DoEvict(_context.CacheImpl);
                else
                    DoEvict(_context.CacheInternal);
            }
            catch (ThreadAbortException) { }
            catch (ThreadInterruptedException) { }
            catch (Exception e)
            {

                if (_context != null)
                {
                    _context.NCacheLog.Error("LocalCache._evictionRun", e.ToString());
                }

            }
            finally
            {
                lock (_eviction_sync_mutex)
                {
                    _evictionThread = null;
                }
            }
        }

        internal override void TouchInternal(string key, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LocalCache.Touch", "");

            if (_cacheStore == null)
                throw new InvalidOperationException();

            CacheEntry e = (CacheEntry)_cacheStore.Get(key);
            if (e != null)
            {
                EvictionHint evh = e.EvictionHint;
                if (_evictionPolicy != null && evh != null && evh.IsVariant)
                    _evictionPolicy.Notify(key, evh, null);
                ExpirationHint exh = e.ExpirationHint;
                if (exh != null && exh.IsVariant)
                {
                    try
                    {
                        _context.ExpiryMgr.ResetVariant(exh);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }

        }

        public override void ClientDisconnected(string client, bool isInproc, Runtime.Caching.ClientInfo clientInfo)
        {
            base.ClientDisconnected(client, isInproc, clientInfo);
            _topicManager.OnClientDisconnected(client);
        }

        public override void ClientConnected(string client, bool isInproc, Runtime.Caching.ClientInfo clientInfo)
        {
            base.ClientConnected(client, isInproc, clientInfo);
        }


        #region /                       --- IMessageStore implementation ----                                   /

        public override bool TopicOperation(TopicOperation operation, OperationContext operationContext)
        {
            bool result = false;
            if(!IsCacheOperationAllowed(operationContext))
                return result;
            try
            {
                
                if (operation != null)
                {
                    switch (operation.TopicOperationType)
                    {
                        case TopicOperationType.Create:
                            result = CreateTopic(operation.Topic);
                            break;
                        case TopicOperationType.Get:
                            result = GetTopic(operation.Topic);
                            break;
                        case TopicOperationType.Remove:
                            result = DeleteTopic(operation.Topic);
                            break;
                        case TopicOperationType.Subscribe:
                            result = _topicManager.CreateSubscription(operation.Topic, ((SubscriptionOperation)operation).SubscriptionInfo);
                            break;
                        case TopicOperationType.UnSubscribe:
                            result = _topicManager.RemoveSubscription(operation.Topic, ((SubscriptionOperation)operation).SubscriptionInfo,((SubscriptionOperation)operation).IsDispose);
                            break;
                        case TopicOperationType.RefreshSubscription:
                            result = RefreshSubscription(operation.Topic, ((SubscriptionOperation)operation).SubscriptionInfo);
                            break;
                        case TopicOperationType.RemoveInavtiveSubscripber:
                            result = _topicManager.RemoveInactiveSubscriber(operation.Topic, ((SubscriptionOperation)operation).SubscriptionInfo);
                            break;
                       
                    }
                }

                if (result && _context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.SetCacheSize(Size);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
            result = false;
        }

      

        private bool CreateTopic(string topic)
        {
            
            _topicManager.CreateTopic(topic);
            
            return true;
        }

        private bool GetTopic(string topic)
        {
            return _topicManager.GetTopic(topic) != null;
        }

        private bool DeleteTopic(string topic)
        {
            return _topicManager.RemoveTopic(topic);
        }

        private bool RefreshSubscription(string topic, SubscriptionInfo subscriptionInfo)
        {
            Topic instance = _topicManager.GetTopic(topic);

            if (instance != null)
            {
                instance.RefreshSubscription(subscriptionInfo);
            }
           
            return true;
        }

        public override bool StoreMessage(string topic, Messaging.Message message, OperationContext context)
        {
            if (!IsCacheOperationAllowed(context))
                return false;
            Topic topicInstance = _topicManager.GetTopic(topic);
                bool stored = false;
                if (topicInstance != null)
                {
                    StoreStatus status = _cacheStore.HasSpace(message, 0, true);

                    _cacheStore.CheckIfCacheNearEviction();

                    if (status == StoreStatus.HasSpace || status == StoreStatus.NearEviction)
                    {
                        stored = topicInstance.StoreMessage(message, context);
                    }


                    if (_context.PerfStatsColl != null)
                    {
                        _context.PerfStatsColl.SetCacheSize(Size);
                    }

                    if (status != StoreStatus.HasSpace)
                    {
                        Evict();
                    }

                    if (status == StoreStatus.HasNotEnoughSpace)
                        throw new OperationFailedException(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED, ErrorMessages.GetErrorMessage(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED));

                }
                else
                {
                    throw new OperationFailedException(ErrorCodes.PubSub.TOPIC_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.PubSub.TOPIC_NOT_FOUND, topic));
                }

                return stored;
            }

        

        public override MessageInfo GetNextUnassignedMessage(TimeSpan timeout, OperationContext context)
        {
            return _topicManager.GetNextUnassignedMessage(timeout, context);
        }


        public override SubscriptionInfo GetSubscriber(string topic, Common.Enum.SubscriptionType type, OperationContext context)
        {
            Topic instance = _topicManager.GetTopic(topic);
            if (instance != null) return instance.GetSubscriberForAssignment(type, context);

            return null;
        }

        public override bool AssignmentOperation(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo, TopicOperationType type, OperationContext context)
        {
            Topic instance = _topicManager.GetTopic(messageInfo.Topic);
            if (instance != null)
            {
                switch (type)
                {
                    case TopicOperationType.AssignSubscription:
                        return instance.AssignSubscription(messageInfo, subscriptionInfo);

                    case TopicOperationType.RevokeAssignment:
                        instance.RevokeAssignment(messageInfo, subscriptionInfo);
                        return true;
                }
            }
            return false;
        }

        public override MessageResponse GetAssignedMessage(SubscriptionInfo subscriptionInfo, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return null;
            return _topicManager.GetAssignedMessages(subscriptionInfo);
        }

        public override void AcknowledgeMessageReceipt(string clientId, IDictionary<string, IList<string>> topicWiseMessageIds, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            HashVector errors = new HashVector();
            foreach (KeyValuePair<string, IList<string>> topicMessges in topicWiseMessageIds)
            {
                foreach (string messageId in topicMessges.Value)
                {
                    try
                    {
                        AcknowledgeMessageReceiptInternal(clientId, topicMessges.Key, messageId, operationContext);
                    }
                    catch (Exception e)
                    {
                        errors.Add(new MessageInfo() { Topic = topicMessges.Key, MessageId = messageId }, e);
                    }

                }
            }
        }

        protected virtual void AcknowledgeMessageReceiptInternal(string clientId, string topic, string messageId, OperationContext operationContext)
        {
            _topicManager.AcknowledgeMessageReceipt(clientId, topic, messageId, operationContext);
        }

        public override IList<MessageInfo> GetDeliveredMessages()
        {
            return _topicManager.GetDeliveredMessages();
        }

       
        public override IList<MessageInfo> GetExpiredMessages()
        {
            return _topicManager.GetExpiredMessages();
        }

        public override IList<MessageInfo> GetEvicatableMessages(long sizeToEvict)
        {
            return _topicManager.GetEvicatableMessages(sizeToEvict);
        }


        public override void RemoveMessages(IList<MessageInfo> messagesTobeRemoved, MessageRemovedReason reason, OperationContext context)
        {
            HashVector errors = new HashVector();
            foreach (MessageInfo messageInfo in messagesTobeRemoved)
            {
                try
                {
                    RemoveMessagesInternal(messageInfo, reason, context);
                }
                catch (Exception e)
                {
                    errors.Add(messageInfo, e);
                }
            }

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }
        }


        protected virtual Messaging.Message RemoveMessagesInternal(MessageInfo messageTobeRemoved, MessageRemovedReason reason, OperationContext context)
        {
            return _topicManager.RemoveMessage(messageTobeRemoved, reason);
        }

        public override IList<MessageInfo> GetUnacknowledgeMessages(TimeSpan assginmentTimeout)
        {
            return _topicManager.GetUnAcknowledgedMessages(assginmentTimeout);
        }


        public override void RevokeAssignment(MessageInfo message, SubscriptionInfo subscription, OperationContext context)
        {
            Topic instance = _topicManager.GetTopic(message.Topic);
            if (instance != null) instance.RevokeAssignment(message, subscription);
        }
        public override IList<string> GetNotifiableClients()
        {
            return _topicManager.GetNotifiableClients();
        }

        public override void RegiserTopicEventListener(ITopicEventListener listener)
        {
            _topicManager.RegisterTopicListener(listener);
        }
        public void RegisterSubscriptionEventListener(ISubscriptionEventListener listener)
        {

        }
        public override TopicState GetTopicsState()
        {
            return _topicManager.GetTopicsState();
        }

        public override void SetTopicsState(TopicState topicState)
        {
            _topicManager.SetTopicsState(topicState);
        }

        public override TransferrableMessage GetTransferrableMessage(string topic, string messageId)
        {
            return _topicManager.GetTransferrableMessage(topic, messageId);
        }

        public override bool StoreTransferrableMessage(string topic, TransferrableMessage message)
        {
            bool result = _topicManager.StoreTransferrableMessage(topic, message);
            if (result && _context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetCacheSize(Size);
            }

            return result;
        }

        public override void AcknowledgeMessageReceipt(string clientId, string topic, string messageId, OperationContext operationContext)
        {
            AcknowledgeMessageReceiptInternal(clientId, topic, messageId, operationContext);
        }

        public override OrderedDictionary GetMessageList(int bucketId, bool includeEventMessages)
        {
            return _topicManager.GetMessageList();
        }

        public override long GetMessageCount(string topicName, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return default;
            if (topicName == default(string))
            {
                throw new ArgumentNullException("topicName");
            }
            Topic topic = _topicManager.GetTopic(topicName);

            if (topic != default(Topic))
            {
                return topic.Count;
            }
            return 0;
        }

        public override Dictionary<string, TopicStats> GetTopicsStats(bool defaultTopicStats= false)
        {
            return _topicManager.GetTopicsStats(defaultTopicStats);
        }

        public override IDictionary<string, IList<string>> GetInActiveClientSubscriptions(TimeSpan inactivityThreshold)
        {
            return _topicManager.GetInActiveClientSubscriptions(inactivityThreshold);
        }

        public override IDictionary<string, IList<string>> GetActiveClientSubscriptions(TimeSpan inactivityThreshold)
        {
            return _topicManager.GetActiveClientSubscriptions(inactivityThreshold);
        }

        #endregion

    

        private void CreateEventsTopics()
        {
            try
            {
                //item level events
                _topicManager.CreateTopic(TopicConstant.ItemLevelEventsTopic);
                //Collection events
                _topicManager.CreateTopic(TopicConstant.CollectionEventsTopic);
            }
            catch (Exception) { }
        }
    }
}
