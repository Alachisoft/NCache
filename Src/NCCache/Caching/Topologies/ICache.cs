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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Locking;

namespace Alachisoft.NCache.Caching.Topologies
{
    /// <summary>
    /// This interface defines the basic set of operations a Cache is required to implement. 
    /// </summary>

    internal interface ICache
	{
		/// <summary>
		/// returns the number of objects contained in the cache.
		/// </summary>
		long Count{ get; }

		/// <summary>
		/// Removes all entries from the cache.
		/// </summary>
        /// <param name="notification">callback entry for write behind</param>
        /// <param name="updateOptions">data source update options</param>
        void Clear(Notifications notification, DataSourceUpdateOptions updateOptions, OperationContext operationContext);

		/// <summary>
		/// Determines whether the cache contains a specific key.
		/// </summary>
		/// <param name="key">The key to locate in the cache.</param>
		/// <returns>true if the cache contains an element 
		/// with the specified key; otherwise, false.</returns>
        bool Contains(object key, OperationContext operationContext);

		/// <summary>
		/// Retrieve the object from the cache. A string key is passed as parameter.
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <returns>cache entry.</returns>
        CacheEntry Get(object key, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext);

		/// <summary>
		/// Adds a pair of key and value to the cache. Throws an exception or reports error 
		/// if the specified key already exists in the cache.
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <param name="cacheEntry">the cache entry.</param>
		/// <returns>returns the result of operation.</returns>
        CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext);

		/// <summary>
		/// Adds a pair of key and value to the cache. If the specified key already exists 
		/// in the cache; it is updated, otherwise a new item is added to the cache.
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <param name="cacheEntry">the cache entry.</param>
		/// <returns>returns the result of operation.</returns>
        CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext);

		/// <summary>
		/// Removes the object and key pair from the cache. The key is specified as parameter.
		/// Moreover it take a removal reason and a boolean specifying if a notification should
		/// be raised.
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <param name="removalReason">reason for the removal.</param>
		/// <param name="notify">boolean specifying to raise the event.</param>
		/// <returns>item value</returns>
        CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext);


		/// <summary>
		/// Determines whether the cache contains the specific keys.
		/// </summary>
		/// <param name="keys">The keys to locate in the cache.</param>
		/// <returns>List of keys that are not found in the cache.</returns>
        Hashtable Contains(IList keys, OperationContext operationContext);

		/// <summary>
		/// Retrieve the objectd from the cache. An array of keys is passed as parameter.
		/// </summary>
		/// <param name="keys">keys of the entries.</param>
		/// <returns>key and value pairs</returns>
        IDictionary Get(object[] keys, OperationContext operationContext);

		/// <summary>
		/// Adds key and value pairs to the cache. Throws an exception or returns a list
		/// of keys that failed to add in the cache.
		/// </summary>
		/// <param name="keys">keys of the entries.</param>
		/// <param name="cacheEntries">the cache entries.</param>
		/// <returns>List of keys that are added or that alredy exists in the cache and their status</returns>
        Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext);

		/// <summary>
		/// Adds key and value pairs to the cache. If any of the specified key already exists 
		/// in the cache; it is updated, otherwise a new item is added to the cache.
		/// </summary>
		/// <param name="keys">keys of the entries.</param>
		/// <param name="cacheEntries">the cache entries.</param>
		/// <returns>return successful keys and there status.</returns>
        Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext);

		/// <summary>
		/// Removes key and value pairs from the cache. The keys are specified as parameter.
		/// Moreover it take a removal reason and a boolean specifying if a notification should
		/// be raised.
		/// </summary>
		/// <param name="keys">keys of the entries.</param>
		/// <param name="removalReason">reason for the removal.</param>
		/// <param name="notify">boolean specifying to raise the event.</param>
		/// <returns>List of keys and values that are removed from cache</returns>
        Hashtable Remove(object[] keys, ItemRemoveReason ir, bool notify, OperationContext operationContext);

		/// <summary>
		/// Returns a .NET IEnumerator interface so that a client should be able
		/// to iterate over the elements of the cache store.
		/// </summary>
		/// <returns>IDictionaryEnumerator enumerator.</returns>
		IDictionaryEnumerator GetEnumerator();

        /// <summary>
        /// Return all the keys currently present in cache
        /// </summary>
        Array Keys { get; }

        void LogBackingSource();

        double GetReplicaCounters(string counterName);


    }
}
