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
using System.Collections;
using System.Text;
using System.Threading;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Web.SessionStateManagement.LocationAffinity;

namespace Alachisoft.NCache.Web.SessionStateManagement

{
    /// <summary>
    /// RegionalCache is a manager to support location affinity.
    /// </summary>
    public class RegionalCache : ISessionCache
    {
        private bool _enableLogs = false;
        private string _primaryCache = string.Empty;
        private string _currentSessionCache = string.Empty;
        private string _sid = "";
        private static NCacheSessionStateSettings _settings = null;
        private bool _isSessionCookieless = false;
        private static ILogger _ncacheLog;

        private static ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }
        //Check if it is a request of not.
        public bool IsSessionCookieless
        {
            get { return _isSessionCookieless; }
            set { _isSessionCookieless = value; }
        }

        /// <summary> 
        /// All caches (primary and secondary) are contained in this table against sid-prefix.
        /// </summary>
        private Hashtable _caches = null;

        /// <summary>
        /// contains a timer for each secondary cache. 
        /// timer perodic event is used to recycle the secondary cache connection.
        /// </summary>
        private System.Timers.Timer _connectionRecycler = null;

        /// <summary>
        /// synchronizes the cache operations and cache connection recycling.
        /// </summary>
        private ReaderWriterLock _sync = new ReaderWriterLock();

        /// <summary>
        /// Returns the prefix of the primary cache.
        /// </summary>
        public string PrimaryPrefix
        {
            get { return _sid; }
        }

        /// <summary>
        /// Returns primary cache id.
        /// </summary>
        public string GetCacheId
        {
            get { return _primaryCache; }
        }

        public RegionalCache(ILogger NCacheLog, NCacheSessionStateSettings setting)
        {
            _ncacheLog = NCacheLog;
            _settings = setting;

            if (_settings != null)
            {
                if (_settings.RecycleInterval != -1)
                {
                    _connectionRecycler = new System.Timers.Timer(_settings.RecycleInterval * 60 * 1000);
                    _connectionRecycler.AutoReset = true;
                    _connectionRecycler.Elapsed += new System.Timers.ElapsedEventHandler(RecycleCacheConnection);
                    _connectionRecycler.Start();
                }
            }
        }

        void RecycleCacheConnection(object sender, System.Timers.ElapsedEventArgs e)
        {
            _sync.AcquireWriterLock(Timeout.Infinite);
            try
            {
                DisposeSecondaryCaches();
                InitializeSecondaryCaches();
            }
            finally
            {
                _sync.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// disposes the secondary cache connections after secondary-connection-recycle-interval.
        /// </summary>
        private void DisposeSecondaryCaches()
        {
            if (_caches != null)
            {
                IDictionaryEnumerator ide = _caches.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (((string)ide.Key).ToLower() != _primaryCache.ToLower())
                    {
                        try
                        {
                            ((Alachisoft.NCache.Web.Caching.Cache)ide.Value).Dispose();
                        }
                        catch (Exception e)
                        {
                            if(NCacheLog != null) NCacheLog.Error("RegionalCache.RecycleSecondaryConnections", "Failed to dispose \"" + ide.Key + "\". Exception: " + e.ToString());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// initializes the secondary cache connections after the secondary-connection-recycle-interval.
        /// </summary>
        private void InitializeSecondaryCaches()
        {
            if (_settings.SecondaryCaches != null && _settings.SecondaryCaches.Count > 0)
            {
                foreach (string cacheId in _settings.SecondaryCaches.Values)
                {
                    try
                    {

                        _caches[cacheId] = Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cacheId);
                    }
                    catch (Exception e)
                    {
                        if (NCacheLog != null) NCacheLog.Error("RegionalCache.RecycleSecondaryConnections", "Failed to initialize \"" + cacheId + "\". Exception: " + e.ToString());
                    }
                }
                ExceptionsEnabled = true;
            }
        }

        /// <summary>
        /// Get the cacheId based upon the unique sessionId prefix.
        /// </summary>
        /// <param name="sessionId">SessionID</param>
        /// <returns>The cacheId.</returns>
        string GetCache(string sessionId)
        {
            string sidPrefix = string.Empty;
            string cacheId = string.Empty;

            if (sessionId != null)
            { sidPrefix = sessionId.Substring(0, 4); }

            if (_settings.PrimaryCache.Contains(sidPrefix))
                cacheId = (string)_settings.PrimaryCache[sidPrefix];
            else if (_settings.SecondaryCaches.Contains(sidPrefix))
                cacheId = (string)_settings.SecondaryCaches[sidPrefix];

            return cacheId;
        }

        public void Log(string module, string message)
        {
            ((Alachisoft.NCache.Web.Caching.Cache)_caches[_primaryCache]).Log(module, message);
        }

        void GetCache(string sessionId, string key, out object value, string group, string subGroup, out Alachisoft.NCache.Web.Caching.Cache cache, bool isGet)
        {
            value = null;

            //Implementation For Raiynair cookieless robots
            string sidPrefix = string.Empty;
            sidPrefix = sessionId.Substring(0, 4);
            if (!_settings.PrimaryCache.Contains(sidPrefix) && _isSessionCookieless)
            {
                _isSessionCookieless = false;
                cache = null;
                return;

            }
            _isSessionCookieless = false;

            cache = _caches[GetCache(sessionId)] as Alachisoft.NCache.Web.Caching.Cache;

            if (cache != null)
            {
                if (isGet)
                {
                    value = cache.Get(key);
                }
            }
            else
            {
                if (_currentSessionCache == null || _currentSessionCache == string.Empty)
                {
                    IDictionaryEnumerator cacheDic = _caches.GetEnumerator();
                    do
                    {
                        /// This code will ensure that primary cache is used first to be lookedin.
                        if (cache == null)
                            cache = _caches[_primaryCache] as Alachisoft.NCache.Web.Caching.Cache;
                        else
                        {
                            cache = (Alachisoft.NCache.Web.Caching.Cache)cacheDic.Value;// select the cache from enumerator.
                            // primary cache is already traversed so no need to do it again.
                            if (cache.ToString().Equals(_primaryCache))
                                continue;
                        }

                        if (isGet)
                        {
                            value = cache.Get(key);
                        }
                        else
                        {
                            if (cache.Contains(key))
                            {
                                value = new object();
                                break;
                            }
                        }
                    } while (cacheDic.MoveNext() && value == null);

                    if (value != null) // value was found from one of the caches. so use that cache for all further operations during this HTTP request.
                    {
                        _currentSessionCache = cache.ToString();
                    }
                    else// value wasn't found from any of the caches. thus use hte primary cache for all further operations.
                    {
                        _currentSessionCache = _primaryCache;
                    }
                }
                else
                {
                    cache = _caches[_currentSessionCache] as Alachisoft.NCache.Web.Caching.Cache;
                    if (isGet)
                    {
                        value = cache.Get(key);
                    }
                }
            }
        }


        void GetCache(string sessionId, string key, out object value, string group, string subGroup, out Alachisoft.NCache.Web.Caching.Cache cache, bool isGet, ref LockHandle lockHandle, bool acquireLock)
        {


            value = null;
            
            //Implementation For Raiynair cookieless robots
            string sidPrefix = string.Empty;
            sidPrefix = sessionId.Substring(0, 4);
            if (!_settings.PrimaryCache.Contains(sidPrefix) && _isSessionCookieless)
            {
                _isSessionCookieless = false;
                cache = null;
                return;

            }
            _isSessionCookieless = false;

            cache = _caches[GetCache(sessionId)] as Alachisoft.NCache.Web.Caching.Cache;

            if (cache != null)
            {
                if (isGet)
                {
                    value = cache.Get(key, Alachisoft.NCache.Web.Caching.Cache.NoLockExpiration, ref lockHandle, acquireLock);
                }
            }
            else
            {
                if (_currentSessionCache == null || _currentSessionCache == string.Empty)
                {
                    IDictionaryEnumerator cacheDic = _caches.GetEnumerator();
                    do
                    {
                        /// This code will ensure that primary cache is used first to be lookedin.
                        if (cache == null)
                            cache = _caches[_primaryCache] as Alachisoft.NCache.Web.Caching.Cache;
                        else
                        {
                            cache = (Alachisoft.NCache.Web.Caching.Cache)cacheDic.Value;// select the cache from enumerator.
                            // primary cache is already traversed so no need to do it again.
                            if (cache.ToString().Equals(_primaryCache))
                                continue;
                        }

                        if (isGet)
                        {

                            value = cache.Get(key,  Alachisoft.NCache.Web.Caching.Cache.NoLockExpiration, ref lockHandle, acquireLock);
                        }
                        else
                        {
                            if (cache.Contains(key))
                            {
                                value = new object();
                                break;
                            }
                        }
                    } while (cacheDic.MoveNext() && value == null);

                    if (value != null) // value was found from one of the caches. so use that cache for all further operations during this HTTP request.
                    {
                        _currentSessionCache = cache.ToString();
                    }
                    else// value wasn't found from any of the caches. thus use hte primary cache for all further operations.
                    {
                        _currentSessionCache = _primaryCache;
                    }
                }
                else
                {
                    cache = _caches[_currentSessionCache] as Alachisoft.NCache.Web.Caching.Cache;
                    if (isGet)
                    {
                        value = cache.Get(key, Alachisoft.NCache.Web.Caching.Cache.NoLockExpiration, ref lockHandle, acquireLock);
                    }
                }
            }
        }

        #region ISessionCache Members

        #region Initialize RegionalCache
        public void InitializeCache(string cache)
        {
            _caches = new Hashtable();

            if (_settings.PrimaryCache != null && _settings.PrimaryCache.Count > 0)
            {
                foreach (string cacheId in _settings.PrimaryCache.Values)
                {
                    if (!_caches.ContainsKey(cacheId))
                    {
                        try
                        {
                            _caches.Add(cacheId, Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cacheId));

                            _primaryCache = cacheId;
                        }
                        catch (Exception e) { throw e; }
                        break;
                    }
                }

                foreach (string prefix in _settings.PrimaryCache.Keys)
                {
                    this._sid = prefix;
                    break;
                }
            }
            if (_settings.SecondaryCaches != null && _settings.SecondaryCaches.Count > 0)
            {
                foreach (string cacheId in _settings.SecondaryCaches.Values)
                {
                    if (!_caches.ContainsKey(cacheId))
                    {
                        try
                        {

                            _caches.Add(cacheId, Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cacheId));

                        }
                        catch (Exception e)
                        {
                            if (NCacheLog != null) NCacheLog.Error("RegionalCache.InitializeCache", "Failed to initialize \"" + cacheId + "\". Exception: " + e.ToString());
                        }
                    }
                }
            }
        }
        #endregion

        public void Dispose()
        {
            if (_connectionRecycler != null)
            {
                _connectionRecycler.Dispose();
                _connectionRecycler = null;
            }

            if (_caches == null)
                return;

            foreach (DictionaryEntry entry in _caches)
            {
                Alachisoft.NCache.Web.Caching.Cache cache = entry.Value as Alachisoft.NCache.Web.Caching.Cache;
                if (cache != null)
                    cache.Dispose();
            }
            _caches.Clear();
            _caches = null;
        }

        public bool ExceptionsEnabled
        {
            get
            {
                foreach (string cacheId in _caches.Keys)
                {
                    Alachisoft.NCache.Web.Caching.Cache cache = (Alachisoft.NCache.Web.Caching.Cache)_caches[cacheId];
                    return cache.ExceptionsEnabled;
                }
                return false;
            }
            set
            {
                foreach (string cacheId in _caches.Keys)
                {
                    Alachisoft.NCache.Web.Caching.Cache cache = (Alachisoft.NCache.Web.Caching.Cache)_caches[cacheId];
                    cache.ExceptionsEnabled = value;
                }
            }
        }

        public string CurrentSessionCache
        {
            get { return _currentSessionCache; }
            set { _currentSessionCache = value; }
        }

        public void Add(string sessionId, string key, object value)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;
            GetCache(sessionId, key, out obj, "", "", out cache, false);
            if (cache != null)
            {
                cache.Add(key, TagUtil.CreateTaggedCacheItem(value));
            }
        }

        public void Add(string sessionId, string key, object value, CacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;
            GetCache(sessionId, key, out obj, "", "", out cache, false);
            if (cache != null)
            {
                cache.Add(key, TagUtil.CreateTaggedCacheItem(value , dependency , absoluteExpiration , slidingExpiration , priority));
            }
        }

        public object Get(string sessionId, string key, bool enableRetry)
        {
            return Get(sessionId, key, null, null);
        }

        public object Get(string sessionId, string key, string group, string subGroup)
        {
            object obj = null;
            Alachisoft.NCache.Web.Caching.Cache cache = null;

            _sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                GetCache(sessionId, key, out obj, group, subGroup, out cache, true);
                return obj;
            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
        }

        public object Get(string sessionId, string key, ref LockHandle lockHandle, bool acquireLock, bool enableRetry)
        {
            object obj = null;
            Alachisoft.NCache.Web.Caching.Cache cache = null;

            _sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                GetCache(sessionId, key, out obj, null, null, out cache, true, ref lockHandle, acquireLock);
                return obj;
            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
        }

        public object Remove(string sessionId, string key, bool enableRetry)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;

            _sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                GetCache(sessionId, key, out obj, "", "", out cache, false);
                if (cache != null)
                    return cache.Remove(key);
            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
            return null;
        }

        public object Remove(string sessionId, string key, LockHandle lockHandle, bool enableRetry)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;

            _sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                GetCache(sessionId, key, out obj, "", "", out cache, false);
                if (cache != null)
                    return cache.Remove(key
                        , lockHandle
                        );
            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
            return null;
        }


        public void RemoveAsync(string sessionId, string key)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;
            GetCache(sessionId, key, out obj, "", "", out cache, false);
            if (cache != null)
                cache.RemoveAsync(key, null, DSWriteOption.None, null);
        }

        public void Insert(string sessionId, string key, object value, bool enableRetry)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;
            GetCache(sessionId, key, out obj, "", "", out cache, false);
            if (cache != null)
                cache.Insert(key, TagUtil.CreateTaggedCacheItem(value));
        }

        [CLSCompliant(false)]
        public void Insert(string sessionId, string key, CacheItem item, bool enableRetry)
        {
            item.Tags = new Runtime.Caching.Tag[] { new Runtime.Caching.Tag(TagUtil.SESSION_TAG) };
            Insert(sessionId, key, item, null, null);
        }

        [CLSCompliant(false)]
        public void Insert(string sessionId, string key, CacheItem item, LockHandle lockHandle, bool releaseLock, bool enableRetry)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;

            _sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                GetCache(sessionId, key, out obj, "", "", out cache, false);
                if (cache != null)
                {
                    item.Tags = new Runtime.Caching.Tag[] { new Runtime.Caching.Tag(TagUtil.SESSION_TAG) };
                    cache.Insert(key, item, lockHandle, releaseLock);
                }
            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
        }

        [CLSCompliant(false)]
        public void Insert(string sessionId, string key, object value, CacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;
            GetCache(sessionId, key, out obj, "", "", out cache, false);
            if (cache != null)
                cache.Insert(key,TagUtil.CreateTaggedCacheItem(value,dependency,absoluteExpiration,slidingExpiration,priority));
        }

        [CLSCompliant(false)]
        public void Insert(string sessionId, string key, CacheItem item, string group, string subGroup)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;

            _sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                GetCache(sessionId, key, out obj, group, subGroup, out cache, false);
                if (cache != null)
                {
                    item.Group = group;
                    item.SubGroup = subGroup;
                    item.Tags = new Runtime.Caching.Tag[] { new Runtime.Caching.Tag(TagUtil.SESSION_TAG) };
                
                    cache.Insert(key, item, DSWriteOption.None, null);
                }
            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
        }

        public void InsertAsync(string sessionId, string key, object value)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;
            GetCache(sessionId, key, out obj, "", "", out cache, false);
            if (cache != null)
                cache.InsertAsync(key, TagUtil.CreateTaggedCacheItem(value) ,null,null,null);
        }

        [CLSCompliant(false)]
        public void InsertAsync(string sessionId, string key, CacheItem item)
        {
            InsertAsync(sessionId, key, item, null, null);
        }

        [CLSCompliant(false)]
        public void InsertAsync(string sessionId, string key, object value, CacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;
            GetCache(sessionId, key, out obj, "", "", out cache, false);
            if (cache != null)
                cache.InsertAsync(key, TagUtil.CreateTaggedCacheItem(value,dependency,absoluteExpiration,slidingExpiration,priority), DSWriteOption.None, null);
        }

        [CLSCompliant(false)]
        public void InsertAsync(string sessionId, string key, CacheItem item, string group, string subGroup)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;
            GetCache(sessionId, key, out obj, group, subGroup, out cache, false);
            if (cache != null)
            {
                item.Tags = new Runtime.Caching.Tag[] { new Runtime.Caching.Tag(TagUtil.SESSION_TAG) };
                cache.InsertAsync(key, item, DSWriteOption.None, null);
            }
        }

        public bool Contains(string sessionId, string key)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = null;
            object obj = null;
            GetCache(sessionId, key, out obj, "", "", out cache, false);
            if (cache != null)
                return cache.Contains(key);
            return false;
        }

        public object Get(string key)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = (Alachisoft.NCache.Web.Caching.Cache)_caches[_primaryCache];
            if (cache != null)
                return cache.Get(key);
            else
                return null;
        }

        public void Insert(string key, object value)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = (Alachisoft.NCache.Web.Caching.Cache)_caches[_primaryCache];
            if (cache != null)
                cache.Insert(key, TagUtil.CreateTaggedCacheItem(value));
        }

        public void Add(string key, object value)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = (Alachisoft.NCache.Web.Caching.Cache)_caches[_primaryCache];
            if (cache != null)
            {
                cache.Add(key,TagUtil.CreateTaggedCacheItem(value));
            }
        }

        public bool Contains(string key)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = (Alachisoft.NCache.Web.Caching.Cache)_caches[_primaryCache];
            if (cache != null)
                return cache.Contains(key);
            else
                return false;
        }

        public object Remove(string key)
        {
            Alachisoft.NCache.Web.Caching.Cache cache = (Alachisoft.NCache.Web.Caching.Cache)_caches[_primaryCache];
            if (cache != null)
                return cache.Remove(key);
            else
                return null;
        }

        public IEnumerator GetEnumerator()
        {
            Alachisoft.NCache.Web.Caching.Cache cache = (Alachisoft.NCache.Web.Caching.Cache)_caches[_primaryCache];
            if (cache != null)
                return cache.GetEnumerator();
            else
                return null;
        }

        public void Unlock(string sessionId, string key)
        {
            object obj = null;
            Alachisoft.NCache.Web.Caching.Cache cache = null;

            _sync.AcquireReaderLock(Timeout.Infinite);
            try
            {
                GetCache(sessionId, key, out obj, null, null, out cache, true);             
                if (cache != null)
                    cache.Unlock(key);
            }
            finally
            {
                _sync.ReleaseReaderLock();
            }
        }

        #endregion
      
    }
}
