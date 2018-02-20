// Copyright (c) 2018 Alachisoft
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
// limitations under the License

using System;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Web.Caching;

namespace Alachisoft.NCache.Web.NOutputCache
{
    /// <summary>
    /// A class that will determine which cache to use
    /// There will be three cache that can be initialized
    /// Cache, ClientCache, and ClientCache without using cache sync dependency
    /// The class provide only three functions
    /// Get, Remove, and Insert
    /// </summary>
    internal sealed class CacheContainer
    {
        private static CacheContainer _container;
        private static int _refCount = 0;

        private Cache _cache, _noSyncLocalCache;
        private bool _useNoSync = false;

        /// <summary>
        /// Get cache container that have underlying cache object(s)
        /// </summary>
        /// <param name="settings">output cache settings specified in web.config</param>
        /// <returns></returns>
        public static CacheContainer  GetCacheInstance(OutputCacheSettings settings)
        {
            if (_container == null)
            {
                _container = new CacheContainer(settings);
            }
            _refCount++;
            return _container;
        }

        /// <summary>
        /// Initialize cache(s) according to settings
        /// </summary>
        /// <param name="settings">output cache settings specified in web.config</param>
        private CacheContainer(OutputCacheSettings settings)
        {
            this._cache = Caching.NCache.InitializeCache(settings.CacheName);

            if (this._cache != null) this._cache.ExceptionsEnabled = true;
            if (this._noSyncLocalCache != null) this._noSyncLocalCache.ExceptionsEnabled = true;

        }

        /// <summary>
        /// Get object from underlying cache
        /// </summary>
        /// <param name="key">key used to reference the object</param>
        /// <returns>returned object</returns>
        public object Get(string key, PageSettings pageSettings)
        {
            if (!this._useNoSync)
            {
                return this._cache.Get(key);
            }
            else
            {
                object result = this._noSyncLocalCache.Get(key);
                if (result != null) return result;

                result = this._cache.Get(key);
                if (result != null)
                {
                    this._noSyncLocalCache.Insert(key, result, null, DateTime.Now.AddSeconds(pageSettings.ExpirationTime), Cache.NoSlidingExpiration, CacheItemPriority.Default);
                }
                return result;
            }
        }

        /// <summary>
        /// Insert object to underlying cache
        /// </summary>
        /// <param name="key">key used to reference the object</param>
        /// <param name="value">object</param>
        /// <param name="dependency">dependency</param>
        /// <param name="absoluteExpiration">absolute time expiration</param>
        /// <param name="slidingExpiration">sliding time expiration</param>
        public void Insert(string key, object value, CacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            if (!this._useNoSync)
            {
                this._cache.Insert(key, value, dependency, absoluteExpiration, slidingExpiration, CacheItemPriority.Default);
            }
            else
            {
                this._cache.Insert(key, TagUtil.CreateTaggedCacheItem(value, dependency, absoluteExpiration, slidingExpiration, CacheItemPriority.Default));
                this._noSyncLocalCache.Insert(key, TagUtil.CreateTaggedCacheItem(value), dependency, absoluteExpiration, slidingExpiration, CacheItemPriority.Default);
            }
        }

        /// <summary>
        /// Remove object from underlying cache
        /// </summary>
        /// <param name="key">key used to reference the object</param>
        public void Remove(string key)
        {
            if (!this._useNoSync)
            {
                this._cache.Remove(key);
            }
            else
            {
                try
                {
                    this._cache.Remove(key);
                }
                finally
                {
                    this._noSyncLocalCache.Remove(key);
                }
            }
        }

        /// <summary>
        /// Dispose off underlying cache(s)
        /// </summary>
        /// <returns>true if object are disposed because they are no longer referenced, false otherwise</returns>
        public static bool DisposeCacheInstance()
        {
            if (--_refCount == 0)
            {
                _container.Dispose();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Dispose off underlying cache(s)
        /// </summary>
        private void Dispose()
        {
            if (this._cache != null) this._cache.Dispose();
            if (this._noSyncLocalCache != null) this._noSyncLocalCache.Dispose();
        }
    }
}