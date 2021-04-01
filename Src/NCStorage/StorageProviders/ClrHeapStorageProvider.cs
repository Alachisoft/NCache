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
using System.Data;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Propagator;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Collections;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Storage
{
    /// <summary>
    /// Implements the Heap cache storage option. Also implements ICacheStorage interface. 
    /// </summary>
    class ClrHeapStorageProvider : StorageProviderBase
    {
        /// <summary> Storage Map </summary>
        protected HashVector _itemDict;
        protected bool _evictionEnabled;

        ///// <summary> Size of data, in bytes, stored in store</summary>
      

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ClrHeapStorageProvider()
        {
            _itemDict = new HashVector(DEFAULT_CAPACITY, 0.7f);
        }

        /// <summary>
        /// Overloaded constructor. The passed in parameters specify the values for maxObjects 
        /// and maxSizeMB.
        /// </summary>
        /// <param name="maxDataSize">maximum size of data, in bytes, that store can contain.</param>
        public ClrHeapStorageProvider(long maxDataSize)
            : base(maxDataSize)
        {
            _itemDict = new HashVector(DEFAULT_CAPACITY, 0.7f);
        }

        /// <summary>
        /// Overloaded constructor. Takes the properties as a map.
        /// </summary>
        /// <param name="properties">properties collection</param>
        public ClrHeapStorageProvider(IDictionary properties, bool evictionEnabled, ILogger NCacheLog, IAlertPropagator alertPropagator)
            : base(properties, evictionEnabled, NCacheLog, alertPropagator)
        {
            _evictionEnabled = evictionEnabled;
            _itemDict = new HashVector(DEFAULT_CAPACITY, 0.7f);
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            lock (_itemDict.SyncRoot)
            {
                _itemDict.Clear();
                _itemDict = null;
            }
            base.Dispose();
        }

        #endregion

        #region	/                 --- ICacheStorage ---           /

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        public override long Count { get { return _itemDict.Count; } }

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public override void Clear()
        {
            lock (_itemDict.SyncRoot)
            {
                _itemDict.Clear();
                base.Cleared();
            }
        }

        /// <summary>
        /// Determines whether the store contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the store.</param>
        /// <returns>true if the store contains an element 
        /// with the specified key; otherwise, false.</returns>
        public override bool Contains(object key)
        {
            //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Store.Cont", "");

            return _itemDict.ContainsKey(key);
        }

        /// <summary>
        /// Provides implementation of Get method of the ICacheStorage interface.
        /// Get an object from the store, specified by the passed in key. 
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>object</returns>
        public override object Get(object key)
        {
            try
            {
                //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Store.Get", "");

                lock (_itemDict.SyncRoot)
                {
                    return _itemDict[key];
                }
            }
            catch (Exception e)
            {
                Trace.error("ClrHeapStorageProvider.Get()", e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get the size of item stored in cache, specified by the passed in key
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>item size</returns>
        public override int GetItemSize(object key)
        {
            try
            {
                object item = null;
                lock (_itemDict.SyncRoot)
                {
                    item = _itemDict[key];
                }
                var iSizableItem = item as ISizable;
                return iSizableItem != null ? (iSizableItem.InMemorySize + Common.MemoryUtil.GetStringSize(key)) : 0;
            }
            catch (Exception e)
            {
                Trace.error("ClrHeapStorageProvider.GetItemSize()", e.ToString());
                return 0;
            }
        }

        /// <summary>
        /// Provides implementation of Add method of the ICacheStorage interface.
        /// Add the key value pair to the store. 
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="item">object</param>
        /// <returns>returns the result of operation.</returns>
        public override StoreAddResult Add(object key, IStorageEntry item,  Boolean allowExtendedSize)
        {
            try
            {
                //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Store.Add", "");

                lock (_itemDict.SyncRoot)
                {
                    if (_itemDict.ContainsKey(key))
                    {
                        return StoreAddResult.KeyExists;
                    }
                }
				
                StoreStatus status = HasSpace((ISizable)item,Common.MemoryUtil.GetStringSize(key),allowExtendedSize);

                CheckIfCacheNearEviction();

                if (status == StoreStatus.HasNotEnoughSpace)
                {
                    return StoreAddResult.NotEnoughSpace;
                }

                lock (_itemDict.SyncRoot)
                {
                    _itemDict.Add(key, item);
                    base.Added(item , Common.MemoryUtil.GetStringSize(key));
                    item.MarkInUse(NCModulesConstants.CacheStore);
                }
                if (status == StoreStatus.NearEviction)
                {
                    return StoreAddResult.SuccessNearEviction;
                }
            }
            catch (OutOfMemoryException e)
            {
                //Trace.error("ClrHeapStorageProvider.Add()", e.ToString());
                return StoreAddResult.NotEnoughSpace;
            }
            catch (Exception e)
            {
                //Trace.error("ClrHeapStorageProvider.Add()", e.ToString());
                //return StoreAddResult.Failure;
                throw e;
            }
            return StoreAddResult.Success;
        }

        /// <summary>
        /// Provides implementation of Insert method of the ICacheStorage interface.
        /// Insert/Add the key value pair to the store. 
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="item">object</param>        
        /// <returns>returns the result of operation.</returns>
        public override StoreInsResult Insert(object key, IStorageEntry item, Boolean allowExtendedSize)
        {
            try
            {
                //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Store.Insert", "");

                IStorageEntry oldItem = (IStorageEntry)_itemDict[key];
				
                StoreStatus status = HasSpace(oldItem as ISizable, (ISizable)item,Common.MemoryUtil.GetStringSize(key),allowExtendedSize);
                
                if(_evictionEnabled)
                    CheckIfCacheNearEviction();

                if (status == StoreStatus.HasNotEnoughSpace)
                {
                    return StoreInsResult.NotEnoughSpace;
                }

                lock (_itemDict.SyncRoot)
                {
                    _itemDict[key] = item;
                    base.Inserted(oldItem, item, Common.MemoryUtil.GetStringSize(key));
                }
                if (status == StoreStatus.NearEviction)
                {
                    //the store is almost full, need to evict.
                    return oldItem != null ? StoreInsResult.SuccessOverwriteNearEviction : StoreInsResult.SuccessNearEviction;
                }

                if (item != null)
                    item.MarkInUse(NCModulesConstants.CacheStore);

                return oldItem != null ? StoreInsResult.SuccessOverwrite : StoreInsResult.Success;
            }
            catch (OutOfMemoryException e)
            {
                return StoreInsResult.NotEnoughSpace;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Provides implementation of Remove method of the ICacheStorage interface.
        /// Removes an object from the store, specified by the passed in key
        /// </summary>
        /// <param name="key">key</param>        
        /// <returns>object</returns>
        public override object Remove(object key)
        {

            IStorageEntry e = (IStorageEntry)Get(key);
            if (e != null)
            {
                lock (_itemDict.SyncRoot)
                {
                    _itemDict.Remove(key);
                    base.Removed(e , Common.MemoryUtil.GetStringSize(key),e.Type);
                    e?.MarkInUse(NCModulesConstants.Global);
                    e?.MarkFree(NCModulesConstants.CacheStore);
                }
            }
            return e;
        }
        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        public override IDictionaryEnumerator GetEnumerator()
        {
            return _itemDict.GetEnumerator();
        }

        /// <summary>
        /// returns the keys 
        /// </summary>
        public override Array Keys
        {
            get
            {
                lock (_itemDict.SyncRoot)
                {
                    Array arr = Array.CreateInstance(typeof(object), _itemDict.Keys.Count);
                    _itemDict.Keys.CopyTo(arr, 0);
                    return arr;
                }
            }
        }

        #endregion

    }
}
