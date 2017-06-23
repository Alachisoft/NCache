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

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Represents a collection of the caches initialized within the same application domain. 
    /// </summary>
    /// <remarks>An instance of this class can not be created. The class implements the IEnumerable 
    /// interface.
    /// </remarks>
    public class CacheCollection : IEnumerable
    {
        private Hashtable _caches;

        internal CacheCollection()
        {
            _caches = Hashtable.Synchronized(new Hashtable());
        }

        /// <summary>
        /// Gets the Cache instance at the specified key.
        /// </summary>
        /// <param name="cacheId">The cache id that uniquely identifies each cache instance</param>
        /// <returns>Alachisoft.NCache.Web.Caching.Cache instance</returns>
        public Cache this[string cacheId]
        {
            get { return GetCache(cacheId); }
        }

        /// <summary>
        /// The count of the caches contained in the collection.
        /// </summary>
        public int Count
        {
            get { return _caches.Count; }
        }

        /// <summary>
        /// Returns a <see cref="Alachisoft.NCache.Web.Caching.CacheCollectionEnumerator"/> to enumerate
        /// over the caches contained in the collection.
        /// </summary>
        /// <returns><see cref="Alachisoft.NCache.Web.Caching.CacheCollectionEnumerator"/></returns>
        public IEnumerator GetEnumerator()
        {
            return new CacheCollectionEnumerator(this._caches);
        }

        internal Cache GetCache(string cacheId)
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
