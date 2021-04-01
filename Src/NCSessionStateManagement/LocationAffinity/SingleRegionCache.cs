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
using System.Text;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Client;
using Alachisoft.NCache.Runtime;
using System.Threading;
using Runtime = Alachisoft.NCache.Runtime;
using Web = Alachisoft.NCache.Web;
using Alachisoft.NCache.Web.SessionStateManagement.LocationAffinity;
using Alachisoft.NCache.Common.FeatureUsageData;

namespace Alachisoft.NCache.Web.SessionStateManagement
{
    /// <summary>
    /// RegionalCache is a manager to support location affinity.
    /// </summary>
    public class SingleRegionCache : ISessionCache
    {
        /// <summary> The one and only cache </summary>


        private ICache _cache = null;


        /// <summary>
        /// Returns cache id.
        /// </summary>
        public string GetCacheId
        {
            get { return _cache.ToString(); }
        }

        public string PrimaryPrefix { get { return ""; } }
        private int _operationRetry=0;
        private int _operationRetryDelayInterval;
        internal static readonly TimeSpan NoLockExpiration = TimeSpan.Zero;


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
            CacheConnectionOptions cacheConnectionOptions = new CacheConnectionOptions();
            cacheConnectionOptions.AppName = FeatureUsageCollector.FeatureTag + FeatureEnum.aspnet_session;
            _cache = CacheManager.GetCache(cache, cacheConnectionOptions);

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

       
        private bool _isSessionCookieless = false;
        //Used in case of regional cache
        public bool IsSessionCookieless
        {
            get { return _isSessionCookieless; }
            set { _isSessionCookieless = value; }
        }
       

        public void Add(string sessionId, string key, object value)
        {
            _cache.Add(key, TagUtil.CreateTaggedCacheItem(value));
        }
        [CLSCompliant(false)]

        public void Add(string sessionId, string key, object value,  DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            _cache.Add(key, TagUtil.CreateTaggedCacheItem(value,absoluteExpiration,slidingExpiration,priority));
        }
		[CLSCompliant(false)]

        public void Insert(string sessionId, string key, object value,  DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            _cache.Insert(key, TagUtil.CreateTaggedCacheItem(value,absoluteExpiration,slidingExpiration,priority));
        }

        public object Get(string sessionId, string key, bool enableRetry)
        {
            object value = null;
            int retry = _operationRetry;
            do{
                try
                {
                    value = Get(sessionId, key, null, null);
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

        public object Get(string sessionId, string key, string group, string subGroup)
        {
            CacheItemVersion version = null;
            return _cache.Get<object>(key);
        }

        public object Remove(string sessionId, string key, bool enableRetry)
        { 
            object value = null;
            int retry = _operationRetry;
            do{
                try
                {
                    _cache.Remove<object>(key, out value);
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

      
        public void Insert(string sessionId, string key, object value, bool enableRetry)
        {
            //int retry = _operationRetry;
            //do{
            //    try
            //    {
                    _cache.Insert(key, TagUtil.CreateTaggedCacheItem(value));
            //        break;
            //    }
            //    catch (Exception ex)
            //    {
            //        string message = ex.Message;

            //        if (message != null && !(message.ToLower().Contains("connection with server") ||
            //            message.ToLower().Contains("no server is available")) || !enableRetry)
            //        {
            //            throw;
            //        }

            //        if (retry <= 0)
            //            throw ex;

            //        retry--;

            //        if (_operationRetryDelayInterval > 0)
            //            Thread.Sleep(_operationRetryDelayInterval);
            //    }
            //}
            //while (retry >= 0);
        }

		[CLSCompliant(false)]
        public void Insert(string sessionId, string key, CacheItem item, bool enableRetry)
        {
            int retry = _operationRetry;
            do{
                try
                {
                    Insert(sessionId, key, item, null);
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
        }             
  	
		[CLSCompliant(false)]
        public void Insert(string sessionId, string key, CacheItem item, string group)
        {
            //_cache.Insert(key, item, group, subGroup);
            //item.Group = group;
            //item.Tags = new Runtime.Caching.Tag[] { new Runtime.Caching.Tag(TagUtil.SESSION_TAG) };
            _cache.Insert(key, item);
        }

       


        public bool Contains(string sessionId, string key)
        {
            return _cache.Contains(key);
        }

        public object Get(string key)
        {
            return _cache.Get<object>(key);
        }

        public void Insert(string key, object value)
        {
            _cache.Insert(key, TagUtil.CreateTaggedCacheItem(value));
        }

        public void Add(string key, object value)
        {
            _cache.Add(key, TagUtil.CreateTaggedCacheItem(value));
        }

        public bool Contains(string key)
        {
            return _cache.Contains(key);
        }

        public object Remove(string key)
        {
            object retObj = null;
            _cache.Remove(key,out retObj);
            return retObj;
        }

        public IEnumerator GetEnumerator()
        {
            return _cache.GetEnumerator();
        }
        #endregion

        #region ISessionCache Members

		[CLSCompliant(false)]
        public object Get(string sessionId, string key, ref LockHandle lockHandle, bool acquireLock, bool enableRetry)
        {
           
            object value = null;
            int retry = _operationRetry;
            do
            {
                try
                {

                    value = _cache.Get<object>(key, acquireLock,NoLockExpiration, ref lockHandle); 

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
        public object Remove(string sessionId, string key, LockHandle lockHandle, bool enableRetry)
        {
            object value = null;
            int retry = _operationRetry;
            do{
                try
                {
                    object removedObj = null;
                    _cache.Remove(key, out removedObj, lockHandle);
                    return removedObj;
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

        public void Insert(string sessionId, string key, CacheItem item, LockHandle lockHandle, bool releaseLock, bool enableRetry)
        {
            int retry = _operationRetry;
            do
            {
                try
                {
                    //item.Tags = new Runtime.Caching.Tag[] { new Runtime.Caching.Tag(TagUtil.SESSION_TAG)};
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
        
        public void Unlock(string sessionId, string key)
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
