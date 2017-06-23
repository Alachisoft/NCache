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

using System;
using System.Collections;
using System.Threading;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Storage;
using Alachisoft.NCache.Runtime.Exceptions;

using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using System.Net;
using Alachisoft.NCache.Caching.Queries;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    /// <summary>
    /// This class provides the local storage options i.e. the actual storage of objects. It is used 
    /// by the Cache Manager, replicated cache and partitioned cache.
    /// </summary>
	internal class LocalCache : LocalCacheBase
    {
        /// <summary> The underlying physical data store. </summary>
        protected ICacheStorage _cacheStore;

        /// <summary> The eviction policy for the cache. </summary>
        protected IEvictionPolicy _evictionPolicy;

        /// <summary> Thread which evicts item when cache is full.</summary>
        private Thread _evictionThread;

        private object _eviction_sync_mutex = new object();

        /// <summary>Flag which indicates whether to explicitly call GC.Collect or not</summary>
        private bool _allowExplicitGCCollection = true;

        private bool _notifyCacheFull = false;

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
            Initialize(cacheClasses, properties);
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
                if (_cacheStore != null) return _cacheStore.Size;
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
        public override bool VirtualUnlimitedSpace
        {
            get
            {
                return this._cacheStore.VirtualUnlimitedSpace;
            }
            set
            {
                this._cacheStore.VirtualUnlimitedSpace = value;
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

                if (System.Configuration.ConfigurationSettings.AppSettings.Get("NCache.EnableGCCollection") != null)
                {
                    _allowExplicitGCCollection = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings.Get("NCache.EnableGCCollection"));
                }


                if (!properties.Contains("storage"))
                    throw new ConfigurationException("Missing configuration option 'storage'");

                if (properties.Contains("scavenging-policy"))
                {
                    IDictionary evictionProps = properties["scavenging-policy"] as IDictionary;

                    if (evictionProps != null && evictionProps.Contains("eviction-enabled"))
                        if (Convert.ToBoolean(evictionProps["eviction-enabled"]) && Convert.ToDouble(evictionProps["evict-ratio"]) > 0)
                            _evictionPolicy = EvictionPolicyFactory.CreateEvictionPolicy(evictionProps);

                }
                else
                {
                    _evictionPolicy = EvictionPolicyFactory.CreateDefaultEvictionPolicy();
                }


                IDictionary storageProps = properties["storage"] as IDictionary;               

               
				_cacheStore = CacheStorageFactory.CreateStorageProvider(storageProps, this._context.SerializationContext, _evictionPolicy != null, _context.NCacheLog);

                _stats.MaxCount = _cacheStore.MaxCount;
                _stats.MaxSize = _cacheStore.MaxSize;
            }
            catch (ConfigurationException e)
            {
               if (_context != null)
                {
                    _context.NCacheLog.Error("LocalCache.Initialize()",  e.ToString()); 
                }
                Dispose();
                throw;
            }
            catch (Exception e)
            {
				if (_context != null)
                {
                    _context.NCacheLog.Error("LocalCache.Initialize()",  e.ToString()); 
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

            _context.PerfStatsColl.SetCacheSize(0); // on clear cache cachesize set to zero

            if (_evictionThread != null)
            {
                NCacheLog.Flush();
                _evictionThread.Abort();
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
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LocalCache.Cont", "");

            if (_cacheStore == null)
                throw new InvalidOperationException();
            return _cacheStore.Contains(key);
        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        internal override CacheEntry GetInternal(object key, bool isUserOperation, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LocalCache.Get", "");

            if (_cacheStore == null)
                throw new InvalidOperationException();
            
            CacheEntry e = (CacheEntry)_cacheStore.Get(key);
            if (e != null)
            {
                EvictionHint evh = e.EvictionHint;
                if (isUserOperation && _evictionPolicy != null && evh != null && evh.IsVariant)
                    _evictionPolicy.Notify(key, evh, null);
            }

            return e;
        }

        /// <summary>
        /// Special purpose internal method. Derived classes don't override this method.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isUserOperation"></param>
        /// <returns></returns>
        internal override CacheEntry GetEntryInternal(object key, bool isUserOperation)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LocalCache.GetInternal", "");

            if (_cacheStore == null)
                throw new InvalidOperationException();

            CacheEntry e = (CacheEntry)_cacheStore.Get(key);
            if (e == null) return e;

            EvictionHint evh = e.EvictionHint;
            if (isUserOperation && _evictionPolicy != null && evh != null && evh.IsVariant)
                _evictionPolicy.Notify(key, evh, null);

            return e;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        internal override bool AddInternal(object key, ExpirationHint eh, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LocalCache.Add_2", "");

            if (_cacheStore == null)
                throw new InvalidOperationException();

            CacheEntry e = (CacheEntry)_cacheStore.Get(key);
            if (e == null) return false;

            //We only allow either idle expiration or Fixed expiration both cannot be set at the same time
            if ((e.ExpirationHint is IdleExpiration && eh is FixedExpiration)
                || (e.ExpirationHint is FixedExpiration && eh is IdleExpiration))
            {
                return false;
            }

            e.ExpirationHint = eh;
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
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LocalCache.Add_1", "");
            if (_cacheStore == null)
                throw new InvalidOperationException();

            if (_evictionPolicy != null)
            {
                if (cacheEntry.EvictionHint is PriorityEvictionHint)
                    cacheEntry.Priority = ((PriorityEvictionHint)cacheEntry.EvictionHint).Priority;

                cacheEntry.EvictionHint = _evictionPolicy.CompatibleHint(cacheEntry.EvictionHint);
            }

            //No Need to insert Eviction if Eviction is turned off it will reduce cache-entry overhead
            if (_evictionPolicy == null)
                cacheEntry.EvictionHint = null;

            StoreAddResult result = _cacheStore.Add(key, cacheEntry, !isUserOperation);
            // Operation completed!
            if (result == StoreAddResult.Success || result == StoreAddResult.SuccessNearEviction)
            {
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


        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        internal override CacheInsResult InsertInternal(object key, CacheEntry cacheEntry, bool isUserOperation, CacheEntry oldEntry, OperationContext operationContext, bool updateIndex)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LocalCache.Insert", "");

            if (_cacheStore == null)
                throw new InvalidOperationException();

			if (cacheEntry.EvictionHint is PriorityEvictionHint)
				cacheEntry.Priority = ((PriorityEvictionHint)cacheEntry.EvictionHint).Priority;

            if (_evictionPolicy != null)
            {
                cacheEntry.EvictionHint = _evictionPolicy.CompatibleHint(cacheEntry.EvictionHint);
            }

            EvictionHint peEvh = oldEntry == null ? null : oldEntry.EvictionHint;

            // No Need to insert Eviction if Eviction is turned off it will reduce cache-entry overhead

            if (_evictionPolicy == null)
                cacheEntry.EvictionHint = null;
            
            //

            StoreInsResult result = _cacheStore.Insert(key, cacheEntry, !isUserOperation);
            // Operation completed!            
            if (result == StoreInsResult.Success || result == StoreInsResult.SuccessNearEviction)
            {
                if (_evictionPolicy != null)
                    _evictionPolicy.Notify(key, null, cacheEntry.EvictionHint);
            }
            else if (result == StoreInsResult.SuccessOverwrite || result == StoreInsResult.SuccessOverwriteNearEviction)
            {
                //update the cache item last modifeid time...
                cacheEntry.UpdateLastModifiedTime(oldEntry);

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



        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>item value</returns>
        internal override CacheEntry RemoveInternal(object key, ItemRemoveReason removalReason, bool isUserOperation, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LocalCache.Remove", "");

            if (_cacheStore == null)
                throw new InvalidOperationException();

            CacheEntry e = (CacheEntry)_cacheStore.Remove(key);
            if (e != null)
            {
                if (_evictionPolicy != null && e.EvictionHint!=null)
                    _evictionPolicy.Remove(key, e.EvictionHint);

                if (_notifyCacheFull)
                {
                    _notifyCacheFull = false;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        internal override bool RemoveInternal(object key, ExpirationHint eh)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LocalCache.Remove", "");

            if (_cacheStore == null)
                throw new InvalidOperationException();

            CacheEntry e = (CacheEntry)_cacheStore.Get(key);
            if (e == null || e.ExpirationHint == null)
            {
                return false;
            }
            else if (e.ExpirationHint.Equals(eh))
            {
                e.ExpirationHint = null;
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
                    _evictionPolicy.Execute(cache, _context, Size);
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
                    _context.NCacheLog.Error("LocalCache._evictionRun",  e.ToString()); 
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
    }
}

