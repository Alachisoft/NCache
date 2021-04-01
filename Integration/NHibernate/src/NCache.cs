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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Alachisoft.NCache.Integrations.NHibernate.Cache.Configuration;
using Alachisoft.NCache.Runtime.Caching;
using NHibernate;
using NHibernate.Cache;

namespace Alachisoft.NCache.Integrations.NHibernate.Cache
{
    class NCache : ICache
    {
        private static readonly IInternalLogger _logger = LoggerProvider.LoggerFor(typeof(Alachisoft.NCache.Integrations.NHibernate.Cache.NCacheProvider));
        private static Dictionary<string, CacheHandler> _caches = new Dictionary<string, CacheHandler>();
        private CacheHandler _cacheHandler = null;
        private readonly RegionConfiguration _regionConfig = null;
        private string _regionName = null;
        private string _connectionString = null;

        /// <summary>
        /// Initializes new cache region.
        /// </summary>
        /// <param name="regionName">Name of region.</param>
        /// <param name="properties"></param>
        public NCache(string regionName, IDictionary<string, string> properties)
        {
            try
            {
                if (_logger.IsDebugEnabled)
                {
                    _logger.Debug(String.Format("Initializing NCache with region : {0}", regionName));
                }

                _regionName = regionName;
                _regionConfig = ConfigurationManager.Instance.GetRegionConfiguration(regionName);

                lock (_caches)
                {
                    if (_caches.ContainsKey(_regionConfig.CacheName))
                    {
                        _cacheHandler = _caches[_regionConfig.CacheName];
                        _cacheHandler.IncrementRefCount();
                    }
                    else
                    {
                        _cacheHandler = new CacheHandler(_regionConfig.CacheName, ConfigurationManager.Instance.ExceptionEnabled);
                        _caches.Add(_regionConfig.CacheName, _cacheHandler);
                    }
                }

                if (properties["connection.connection_string"] != null)
                {
                    _connectionString = Convert.ToString(properties["connection.connection_string"]);
                }
            }
            catch (Exception e)
            {
                if (_logger.IsErrorEnabled)
                {
                    _logger.Error("Failed to initialize NCache. " + e.Message);
                }
                throw new CacheException("Failed to initialize NCache. "+e.Message,e);
            }
        }


        #region ICache Members
        /// <summary>
        /// Clear cache.
        /// </summary>
        public void Clear()
        {
            try
            {
                if (_logger.IsDebugEnabled)
                {
                    _logger.Debug(String.Format("Clearing Region Cache : {0}", _regionName));
                }

                _cacheHandler.Cache.Clear();

            }
            catch (Exception e)
            {
                if (_logger.IsErrorEnabled)
                {
                    _logger.Error("Clear operaion failed." + e.Message);
                }
                throw new CacheException("Clear operaion failed." + e.Message, e);
            }
        }

        /// <summary>
        /// Disposes cache.
        /// </summary>
        public void Destroy()
        {
            try
            {
                lock (_caches)
                {
                    if (_cacheHandler != null)
                    {
                        if (_logger.IsDebugEnabled)
                        {
                            _logger.Debug(String.Format("Destroying Region Cache : {0}", _regionName));
                        }
                        if (_cacheHandler.DecrementRefCount() == 0)
                        {
                            _caches.Remove(_regionConfig.CacheName);
                            _cacheHandler.DisposeCache();
                        }
                        _cacheHandler = null;
                    }
                }
            }
            catch (Exception e)
            {
                if (_logger.IsErrorEnabled)
                {
                    _logger.Error("Destroy operation failed." + e.Message);
                }
                throw new CacheException("Destroy operation failed." + e.Message, e);
            }
        }

        /// <summary>
        /// Get object from cache
        /// </summary>
        /// <param name="key">key of the object.</param>
        /// <returns></returns>
        public object Get(object key)
        {
            try
            {
                if (key == null)
                    return null;

                string cacheKey = ConfigurationManager.Instance.GetCacheKey(key);
                if (_logger.IsDebugEnabled)
                {
                    _logger.Debug(String.Format("Fetching object from the cache with key = {0}", cacheKey));
                }

                return _cacheHandler.Cache.Get<object>(cacheKey);
            }
            catch (Exception e)
            {
                if (_logger.IsErrorEnabled)
                {
                    _logger.Error("Get operation failed. " + e.Message);
                }
                throw new CacheException("Get operation failed. " + e.Message, e);
            }
        }

        /// <summary>
        /// Lock the item with the key provided
        /// </summary>
        /// <param name="key">The Key of the Item in the Cache to lock.</param>
        /// <exception cref="CacheException"></exception>
        public void Lock(object key)
        {
            if (key == null)
                throw new ArgumentNullException("key", "null key not allowed");

            string cacheKey = ConfigurationManager.Instance.GetCacheKey(key);

            Alachisoft.NCache.Client.LockHandle lockHandle = new Alachisoft.NCache.Client.LockHandle();

            try
            {
                if (!_cacheHandler.Cache.Lock(cacheKey, TimeSpan.MaxValue, out lockHandle))
                {
                    throw new CacheException("Unable to acquire lock on the key provided.");
                }
            }
            catch (Exception e)
            {
                throw new CacheException(e.Message);
            }

        }

        public long NextTimestamp()
        {
            return Timestamper.Next();
        }

        /// <summary>
        /// Insert an object in cahce with key specified.
        /// </summary>
        /// <param name="key">key of the object.</param>
        /// <param name="value">Object to be inserted in cache.</param>
        public void Put(object key, object value)
        {
            try
            {
                if (key == null)
                    throw new ArgumentNullException("key", "null key not allowed");

                if (value == null)
                    throw new ArgumentNullException("value", "null value not allowed");

                string cacheKey = ConfigurationManager.Instance.GetCacheKey(key);

                Alachisoft.NCache.Client.CacheItem item = new Alachisoft.NCache.Client.CacheItem(value);
                item.Priority = _regionConfig.CacheItemPriority;
                
                if (_regionConfig.ExpirationType.ToLower() == "sliding")
                    item.Expiration = new Expiration(ExpirationType.Sliding, new TimeSpan(0, 0, _regionConfig.ExpirationPeriod));
                else if (_regionConfig.ExpirationType.ToLower() == "absolute")
                    item.Expiration = new Expiration(ExpirationType.Absolute, new TimeSpan(0, 0, _regionConfig.ExpirationPeriod));


                if (_logger.IsDebugEnabled)
                    {
                        _logger.Debug(String.Format("Inserting: key={0}&value={1}", key, value.ToString()));
                    }
                    _cacheHandler.Cache.Insert(cacheKey, item);
            }
            catch (Exception e)
            {
                if (_logger.IsErrorEnabled)
                {
                    _logger.Error("Put operation failed. " + e.Message);
                }
                throw new CacheException("Put operation failed. " + e.Message, e);
            }
        }

        /// <summary>
        /// RegionName associated with current cache.
        /// </summary>
        public string RegionName
        {
            get { return _regionName; }
        }

        /// <summary>
        /// Remove an object from cache.
        /// </summary>
        /// <param name="key">Key of the object.</param>
        public void Remove(object key)
        {
            try
            {
                string cacheKey = ConfigurationManager.Instance.GetCacheKey(key);
  //              if (_regionConfig.UseAsync)
   //             {
                   
                    if (_logger.IsDebugEnabled)
                    {
                        _logger.Debug("Removing item with key: " + cacheKey);
                    }
                    _cacheHandler.Cache.Remove(cacheKey);
   //             }
            }
            catch (Exception e)
            {
                if (_logger.IsErrorEnabled)
                {
                    _logger.Error("Remove operation failed. " + e.Message);
                }
                throw new CacheException("Remove operation failed. " + e.Message, e);
            }
        }

        public int Timeout
        {
            get { return Timestamper.OneMs*60000; }
        }

        /// <summary>
        /// Unlock the item with the key provided
        /// </summary>
        /// <param name="key">The Key of the Item in the Cache to lock.</param>
        /// <exception cref="CacheException"></exception>
        public void Unlock(object key)
        {
            if (key == null)
                throw new ArgumentNullException("key", "null key not allowed");

            string cacheKey = ConfigurationManager.Instance.GetCacheKey(key);

            try
            {
                _cacheHandler.Cache.Unlock(cacheKey);
            }
            catch (Exception e)
            {
                throw new CacheException(e.Message);
            }
        }

        public Task<object> GetAsync(object key, CancellationToken cancellationToken)
        {
            if (key == null)
                throw new ArgumentNullException("key", "null key not allowed");

            TaskFactory<object> factory = new TaskFactory<object>(cancellationToken);

            Task<object> task = Task.Run(() => {
                return Get(key);
            }, cancellationToken);

            return task;
        }

        public Task PutAsync(object key, object value, CancellationToken cancellationToken)
        {
            if (key == null)
                throw new ArgumentNullException("key", "null key not allowed");

            if (value == null)
                throw new ArgumentNullException("value", "null value not allowed");

            Task task = Task.Run(() => {
                Put(key, value);
            }, cancellationToken);

            return task;
        }

        public Task RemoveAsync(object key, CancellationToken cancellationToken)
        {
            if (key == null)
                throw new ArgumentNullException("key", "null key not allowed");

            Task task = Task.Run(() => {
                Remove(key);
            });

            return task;
        }

        public Task ClearAsync(CancellationToken cancellationToken)
        {
            Task task = Task.Run(() => {
                Clear();
            });

            return task;
        }

        public Task LockAsync(object key, CancellationToken cancellationToken)
        {
            if (key == null)
                throw new ArgumentNullException("key", "null key not allowed");

            Task task = Task.Run(() => {
                Lock(key);
            });

            return task;
        }

        public Task UnlockAsync(object key, CancellationToken cancellationToken)
        {
            if (key == null)
                throw new ArgumentNullException("key", "null key not allowed");

            Task task = Task.Run(() => {
                Unlock(key);
            });

            return task;
        }

        #endregion
    }
}
