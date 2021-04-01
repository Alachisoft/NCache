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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Collections;
using Alachisoft.NCache.Common.Enum;
using System;
using System.Collections;
using System.Threading;

namespace Alachisoft.NCache.Storage
{
    /// <summary>
    /// Synchronized wrapper over cache stores. Provides internal as well as external
    /// thread safety.
    /// </summary>
    internal class StorageProviderSyncWrapper : StorageProviderBase
    {
        /// <summary> enwrapped cache store </summary>
        protected StorageProviderBase _storage = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="storageProvider">The cache store to be wrapped.</param>
        public StorageProviderSyncWrapper(StorageProviderBase storageProvider)
        {
            if (storageProvider == null)
                throw new ArgumentNullException("storageProvider");
            _storage = storageProvider;
            _syncObj = _storage.Sync;
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (_storage != null)
            {
                _storage.Dispose();
                _storage = null;
            }
            base.Dispose();
        }

        #endregion

        /// <summary>
        /// get or set the maximam size of store, in bytes
        /// </summary>
        public override long MaxSize
        {
            get { return _storage.MaxSize; }
            set { _storage.MaxSize = value; }
        }

        public override long MaxCount
        {
            get { return base.MaxCount; }
            set { base.MaxCount = value; }
        }

        #region	/                 --- ICacheStorage ---           /

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        public override long Count
        {
            get
            {
                Sync.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return _storage.Count;
                }
                finally
                {
                    Sync.ReleaseReaderLock();
                }
            }
        }

        public override long Size
        {
            get
            {
                Sync.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    return _storage.Size;
                }
                finally
                {
                    Sync.ReleaseWriterLock();
                }
            }
        }

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public override void Clear()
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _storage.Clear();
            }
            finally
            {
                Sync.ReleaseWriterLock();
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
            Sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _storage.Contains(key);
            }
            finally
            {
                Sync.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Provides implementation of Get method of the ICacheStorage interface.
        /// Get an object from the store, specified by the passed in key. 
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>object</returns>
        public override object Get(object key)
        {
            Sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _storage.Get(key);
            }
            finally
            {
                Sync.ReleaseReaderLock();
            }
        }

        public override int GetItemSize(object key)
        {
            Sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _storage.GetItemSize(key);
            }
            finally
            {
                Sync.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Provides implementation of Add method of the ICacheStorage interface.
        /// Add the key value pair to the store. 
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="item">object</param>
        /// <returns>returns the result of operation.</returns>
        public override StoreAddResult Add(object key, IStorageEntry item, Boolean allowExtendedSize)
        {
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                return _storage.Add(key, item, allowExtendedSize);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
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
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                return _storage.Insert(key, item, allowExtendedSize);
            }
            finally
            {
                Sync.ReleaseWriterLock();
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
            Sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                return _storage.Remove(key);
            }
            finally
            {
                Sync.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        public override IDictionaryEnumerator GetEnumerator()
        {
            return _storage.GetEnumerator();
        }


        /// <summary>
        /// returns all the keys of a particular cache store..         
        /// </summary>        
        public override Array Keys
        {
            get
            {
                Sync.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    return _storage.Keys;
                }
                finally
                {
                    Sync.ReleaseWriterLock();
                }
            }
        }

        #endregion

    }
}