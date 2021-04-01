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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Collections;

namespace Alachisoft.NCache.Storage
{
    /// <summary>
    /// Interface that defines the standard operations to be implemented by all the cache stores.
    /// </summary>
    public interface ICacheStorage : IDisposable, ISizeableIndexStore, ISizableIndex
    {
        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        long Count { get; }

        /// <summary>
        /// returns the size of data, in bytes, stored in cache
        /// </summary>
        long Size { get; }

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        long MaxSize { get; set; }

        /// <summary>
        /// returns the number if objects contained in the cache.
        /// </summary>
        long MaxCount { get; set; }

        /// <summary>
        /// retrun all the keys currently in the cache
        /// </summary>
        Array Keys { get; }


        /// <summary>
        /// Returns true if storage has to check for space, false otherwise.       
        /// </summary>
        Boolean VirtualUnlimitedSpace { get; set; }

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the store contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the store.</param>
        /// <returns>true if the store contains an element 
        /// with the specified key; otherwise, false.</returns>
        bool Contains(object key);

        /// <summary>
        /// Get an object from the store, specified by the passed in key. Must be implemented 
        /// by cache stores. 
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <returns>cache entry.</returns>
        object Get(object key);

        /// <summary>
        /// Get the size of item stored in store
        /// </summary>
        /// <param name="key">The key whose items size to get</param>
        /// <returns>Item size</returns>
        int GetItemSize(object key);

     

        /// <summary>
        /// Add the key value pair to the store. Must be implemented by cache stores.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="item">object</param>
        /// <returns>returns the result of operation.</returns>
        StoreAddResult Add(object key, IStorageEntry item, Boolean allowExtendedSize);

        /// <summary>
        /// Insert the key value pair to the store. Must be implemented by cache stores.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="item">object</param>
        /// <returns>returns the result of operation.</returns>
        StoreInsResult Insert(object key, IStorageEntry item, Boolean allowExtendedSize);

        /// <summary>
        /// Removes an object from the store, specified by the passed in key. Must be 
        /// implemented by cache stores.
        /// </summary>
        /// <param name="key">The key whose value to remove.</param>
        /// <returns>cache entry.</returns>
        object Remove(object key);       

        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        /// <returns>IDictionaryEnumerator enumerator.</returns>
        IDictionaryEnumerator GetEnumerator();

        

        /// <summary>
        /// Increases/decreases cache size.
        /// </summary>
        /// <param name="change">Amount of change in cache size.</param>
        void ChangeCacheSize(long change);


        ClusteredArrayList GetBucketKeyList(int bucketId);

        StoreStatus HasSpace(ISizable item, long keySize, Boolean allowExtendedSize);

        void CheckIfCacheNearEviction();

    }
}
