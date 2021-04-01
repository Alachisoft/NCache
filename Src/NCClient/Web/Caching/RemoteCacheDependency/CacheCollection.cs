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

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Represents a collection of the caches initialized within the same application domain. 
    /// </summary>
    /// <remarks>An instance of this class can not be created. The class implements the IEnumerable 
    /// interface.
    /// </remarks>
    public class CacheCollection : IEnumerable
    {
        internal Hashtable _caches;
        private StringComparer invariantCultureIgnoreCase;

        internal CacheCollection()
        {
            _caches = Hashtable.Synchronized(new Hashtable());
        }

        /// <summary>
        /// Creates a CacheCollection instance with the provided StringComparer
        /// </summary>
        /// <param name="invariantCultureIgnoreCase">String comparer to be used by the cache collection for GetHashCode implementations</param>
        public CacheCollection(StringComparer invariantCultureIgnoreCase)
        {
            _caches = Hashtable.Synchronized(new Hashtable(invariantCultureIgnoreCase));
        }

        /// <summary>
        /// Gets the Cache instance at the specified key.
        /// </summary>
        /// <param name="cacheId">The cache id that uniquely identifies each cache instance</param>
        /// <returns>Alachisoft.NCache.Client.Caching.Cache instance</returns>
        public ICache this[string cacheId]
        {
            get { return GetCache(cacheId, true); }
        }

        /// <summary>
        /// The count of the caches contained in the collection.
        /// </summary>
        public int Count
        {
            get { return _caches.Count; }
        }

        /// <summary>
        /// Returns an <seealso cref="IEnumerator"/> which iterates over existent caches
        /// </summary>
        /// <returns>An System.Collections.IDictionaryEnumerator for the Alachisoft.NCache.Client.CacheCollection</returns>
        public IEnumerator GetEnumerator()
        {
            lock(_caches)
            {
                return ((Hashtable)_caches.Clone()).GetEnumerator();
            }
        }

        internal Cache GetCache(string cacheId, bool useClientCache)
        {

            return _caches[cacheId.ToLower()] as Cache;
        }

        internal void AddCache(string cacheId, Cache cache)
        {
            _caches[cacheId.ToLower()] = cache;
        }

        internal bool Contains(object key)
        {
            return _caches.Contains(((string)key).ToLower());
        }

        internal void Remove(object key)
        {
            _caches.Remove(((string)key).ToLower());
        }
    }
}