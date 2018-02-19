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
// limitations under the License.

using System;
using System.Collections;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Runtime;

using System.Threading;

namespace Alachisoft.NCache.Web.SessionStateManagement
{
    /// <summary>
    /// RegionalCache is a manager to support location affinity.
    /// </summary>
    public class SingleRegionCache : ISessionCache
    {
        /// <summary> The one and only cache </summary>


        private Alachisoft.NCache.Web.Caching.Cache _cache = null;

        public string PrimaryPrefix { get { return ""; } }
        private int _operationRetry=0;
        private int _operationRetryDelayInterval;
        

        public SingleRegionCache()
        { }

        public SingleRegionCache(int operationRetry, int operationRetryDelayInterval)
        {
            _operationRetry = operationRetry;
            _operationRetryDelayInterval = operationRetryDelayInterval;
        }

        #region ISessionCache Members

        public void InitializeCache(string cache)
        {


             _cache = Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cache);

        }

        /// <summary>
        /// Returns cache id.
        /// </summary>
        public string GetCacheId
        {
            get { return _cache.ToString(); }
        }

        public void Dispose()
        {
            if (_cache == null)
                return;

            _cache.Dispose();
            _cache = null; 
        }

        public string CurrentSessionCache
        {
            get { return _cache.ToString(); }
            set { }
        }

        public bool ExceptionsEnabled
        {
            get
            { return _cache.ExceptionsEnabled; }
            set
            { _cache.ExceptionsEnabled = value; }
        }

        [CLSCompliant(false)]
		public void Add( string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            _cache.Add(key, value, absoluteExpiration, slidingExpiration, priority);
        }

		[CLSCompliant(false)]
        public void Insert( string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
		{
            _cache.Insert(key, value, absoluteExpiration, slidingExpiration, priority);
        }

        public object Get(string key, bool enableRetry)
        {
            return _cache.Get(key);
        }
        
        public object Remove(string key, bool enableRetry)
        { 
            object value = null;
            int retry = _operationRetry;
            do{
                try
                {
                    value = _cache.Remove(key);
                    break;
                }
                catch (Exception ex)
                {
                    string message = ex.Message;

                    if (message != null && !(message.ToLower().Contains("connection with server") ||
                        message.ToLower().Contains("no server is available")) || !enableRetry)
                    {
                        throw;
                    }

                    if (retry <= 0)
                        throw ex;
                    
                    retry--;

                    if (_operationRetryDelayInterval > 0)
                        Thread.Sleep(_operationRetryDelayInterval);     
                }
            }
            while (retry >= 0);

            return value;
        }
        

		[CLSCompliant(false)]

        public void Insert(string key, CacheItem item, bool enableRetry)
        {
            _cache.Insert(key, item);
        }           
        
        public object Get(string key)
        {
            return _cache.Get(key);
        }
        
        public object Remove(string key)
        {
            return _cache.Remove(key);
        }

        public IEnumerator GetEnumerator()
        {
            return _cache.GetEnumerator();
        }
        #endregion

        #region ISessionCache Members

		[CLSCompliant(false)]
        public object Get(string key, ref LockHandle lockHandle, bool acquireLock, bool enableRetry)
        {
            object value = null;
            int retry = _operationRetry;
            do
            {
                try
                {
                    value = _cache.Get(key, Alachisoft.NCache.Web.Caching.Cache.NoLockExpiration, ref lockHandle, acquireLock); 

                    break;
                }
                catch (Exception ex)
                {
                    string message = ex.Message;

                    if (message != null && !(message.ToLower().Contains("connection with server") ||
                        message.ToLower().Contains("no server is available")) || !enableRetry)
                    {
                        throw;
                    }

                    if (retry <= 0)
                        throw ex;
                    
                    retry--;

                    if (_operationRetryDelayInterval > 0)
                        Thread.Sleep(_operationRetryDelayInterval);     
                }
            }
            while (retry >= 0);
           
            return value;
        }

		[CLSCompliant(false)]
        public object Remove(string key, LockHandle lockHandle, bool enableRetry)
        {
            object value = null;
            int retry = _operationRetry;
            do{
                try
                {
                    value = _cache.Remove(key, lockHandle);
                    break;
                }
                catch(Exception ex)
                {
                    string message = ex.Message;

                    if (message != null && !(message.ToLower().Contains("connection with server") ||
                        message.ToLower().Contains("no server is available")) || !enableRetry)
                    {
                        throw;
                    }

                    if (retry <= 0)
                        throw ex;

                    retry--;

                    if (_operationRetryDelayInterval > 0)
                        Thread.Sleep(_operationRetryDelayInterval);     
                }
            }
            while(retry>=0);
            return value;
        }


		[CLSCompliant(false)]

        public void Insert(string key, CacheItem item, LockHandle lockHandle, bool releaseLock, bool enableRetry)
        {

            int retry = _operationRetry;
            do
            {
                try
                {
                    _cache.Insert(key, item, lockHandle, releaseLock);
                    break;
                }
                catch(Exception ex)
                {
                    string message = ex.Message;

                    if (message != null && !(message.ToLower().Contains("connection with server") ||
                        message.ToLower().Contains("no server is available")) || !enableRetry)
                    {
                        throw;
                    }

                    if (retry <= 0)
                        throw ex;

                    retry--;

                    if (_operationRetryDelayInterval > 0)
                        Thread.Sleep(_operationRetryDelayInterval);     
                }
            } while (retry >= 0);

        }

        #endregion

        #region ISessionCache Members
        
        public void Unlock(string key)
        {
            int retry = _operationRetry;
            do
            {
                try
                {
                    _cache.Unlock(key);
                    break;
                }
                catch(Exception ex)
                {
                    string message = ex.Message;

                    if (message != null && !(message.ToLower().Contains("connection with server") ||
                        message.ToLower().Contains("no server is available")))
                    {
                        throw;
                    }

                    if (retry <= 0)
                        throw ex;

                    retry--;

                    if (_operationRetryDelayInterval > 0)
                        Thread.Sleep(_operationRetryDelayInterval);     
                }
            }
            while (retry >= 0);

        }

        #endregion

        
    }
}
