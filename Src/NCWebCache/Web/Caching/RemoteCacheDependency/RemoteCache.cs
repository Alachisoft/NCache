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
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Web.Caching.RemoteCacheDependency
{
    class RemoteCache : IDisposable
    {
        public delegate void ItemExpired(string cacheID, string cacheKey, object expirationReason);

        private string _name;
        private Cache _cache;
        private CacheItemRemovedCallback _removeCallback;
        private Dictionary<string, CacheKeyStats> _remoteCacheKeys = new Dictionary<string, CacheKeyStats>();
        private const int _sleepInterval = 120000;
        private Queue _registerCallbackQueue = new Queue();
        Thread registerRemoveEvent;
        Thread checkCacheContain;
        private CacheClearedCallback _onCacheClear;


        public RemoteCache(string cacheId)
        {
            _name = cacheId;
            _removeCallback = new CacheItemRemovedCallback(OnItemRemoved);
        }

        public void Intialize()
        {
            try
            {
                lock (this)
                {
                    _cache = Alachisoft.NCache.Web.Caching.NCache.InitializeCache(_name);
                }

                _onCacheClear = new CacheClearedCallback(OnCacheCleared); //my
                _cache.CacheCleared += _onCacheClear; //my
                checkCacheContain = new Thread(new ThreadStart(Run));
                registerRemoveEvent = new Thread(new ThreadStart(RegUnRegRemoteCacheDependency));
                checkCacheContain.Start();
                registerRemoveEvent.Start();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void OnCacheCleared()
        {
            RemoteCacheKeyDependencyManager.RemoveCacheOnCacheClear(this);
        }

        public void RegisterRemoteCacheDependency(string cacheKey)
        {
            if (!string.IsNullOrEmpty(cacheKey))
            {
                bool isNewKey = false;
                lock (_remoteCacheKeys)
                {
                    if (_remoteCacheKeys.ContainsKey(cacheKey))
                    {
                        CacheKeyStats cacheStats = _remoteCacheKeys[cacheKey];
                        if (cacheStats != null)
                        {
                            cacheStats.refCount++;
                            cacheStats.HasExpired = false;
                        }
                        else
                        {
                            cacheStats = new CacheKeyStats();
                            cacheStats.refCount = 1;
                            cacheStats.HasExpired = false;
                        }
                    }
                    else
                    {
                        CacheKeyStats cacheStats = new CacheKeyStats();
                        cacheStats.refCount = 1;
                        cacheStats.HasExpired = false;

                        _remoteCacheKeys.Add(cacheKey, cacheStats);
                        isNewKey = true;
                    }
                }

                if (isNewKey)
                {
                    lock (_registerCallbackQueue)
                    {
                        ThreadTask registerThread = new ThreadTask(cacheKey, true, false);
                        _registerCallbackQueue.Enqueue(registerThread);
                        Monitor.Pulse(_registerCallbackQueue);
                    }
                }
            }
        }

        private void RegUnRegRemoteCacheDependency()
        {
            while (true)
            {
                try
                {
                    if (_registerCallbackQueue.Count > 0)
                    {
                        object cacheKey = null;
                        lock (_registerCallbackQueue)
                        {
                            cacheKey = _registerCallbackQueue.Dequeue();
                        }

                        if (cacheKey == null) continue;

                        ThreadTask threadTask = (ThreadTask) cacheKey;
                        if (threadTask.cacheKey == null)
                            throw new Exception(" cache key null");

                        if (threadTask.register)
                        {
                            try
                            {
                                _cache.RegisterKeyNotificationCallback(threadTask.cacheKey, null, _removeCallback);
                            }
                            catch (Exception e)
                            {
                                SetItemExpiration(cacheKey.ToString(), "Event Registration faliure");
                            }
                        }
                        else if (threadTask.unRegister)
                        {
                            _cache.UnRegisterKeyNotificationCallback(threadTask.cacheKey, null, _removeCallback);
                        }
                    }
                    else
                    {
                        lock (_registerCallbackQueue)
                        {
                            if (_registerCallbackQueue.Count == 0)
                            {
                                Monitor.Wait(_registerCallbackQueue);
                            }
                        }
                    }
                }
                catch (ThreadAbortException t)
                {
                    break;
                }
                catch (ThreadInterruptedException i)
                {
                    break;
                }
                catch (Exception ex)
                {
                }
            }
        }

        public void UnregisterRemoteCacheDependency(string cacheKey)
        {
            if (!string.IsNullOrEmpty(cacheKey))
            {
                lock (_remoteCacheKeys)
                {
                    if (_remoteCacheKeys.ContainsKey(cacheKey))
                    {
                        CacheKeyStats cacheKeyCounter = _remoteCacheKeys[cacheKey];
                        if (cacheKeyCounter != null)
                        {
                            cacheKeyCounter.refCount--;

                            if (cacheKeyCounter.refCount == 0)
                            {
                                _remoteCacheKeys.Remove(cacheKey);
                            }
                        }
                    }
                }

                lock (_registerCallbackQueue)
                {
                    ThreadTask registerThread = new ThreadTask(cacheKey, false, true);
                    _registerCallbackQueue.Enqueue(registerThread);
                    Monitor.Pulse(_registerCallbackQueue);
                }
            }
        }

        public void OnItemRemoved(string key, object value, CacheItemRemovedReason reason)
        {
            if (!string.IsNullOrEmpty(key))
            {
                SetItemExpiration(key, "Item Removed from Remote Cache");
            }
        }

        private void SetItemExpiration(string cacheKey, string reason)
        {
            if (!string.IsNullOrEmpty(cacheKey))
            {
                if (_remoteCacheKeys.ContainsKey(cacheKey))
                {
                    CacheKeyStats cacheStatus = _remoteCacheKeys[cacheKey];
                    if (cacheStatus != null)
                    {
                        lock (_remoteCacheKeys)
                        {
                            cacheStatus.HasExpired = true; //SET Boolean
                        }
                    }
                }
            }
        }

        public bool CheckExpiration(string cacheKey)
        {
            try
            {
                if (!string.IsNullOrEmpty(cacheKey))
                {
                    CacheKeyStats value;
                    lock (_remoteCacheKeys)
                    {
                        _remoteCacheKeys.TryGetValue(cacheKey, out value);
                    }

                    if (value != null) return value.HasExpired;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Run()
        {
            string ExpirationReason = string.Empty;
            List<string> remoteCacheKeys = new List<string>();
            try
            {
                while (true)
                {
                    remoteCacheKeys.Clear();

                    lock (_remoteCacheKeys)
                    {
                        foreach (string s in _remoteCacheKeys.Keys)
                        {
                            remoteCacheKeys.Add(s);
                        }
                    }

                    int retryCount;
                    foreach (string cachekey in remoteCacheKeys)
                    {
                        retryCount = 1;
                        while (retryCount <= 3)
                        {
                            try
                            {
                                if (!_cache.Contains(cachekey))
                                {
                                    ExpirationReason = "Item no longer exist in Remote Cache";
                                    SetItemExpiration(cachekey, ExpirationReason);
                                }

                                break;
                            }
                            catch (OperationFailedException ex)
                            {
                                ExpirationReason = ex.Message;
                                retryCount++;
                                continue;
                            }
                            catch (Exception ex)
                            {
                                ExpirationReason = ex.Message;
                                retryCount++;
                                continue;
                            }
                        }

                        if (retryCount > 3)
                            SetItemExpiration(cachekey, ExpirationReason);
                    }

                    Thread.Sleep(_sleepInterval);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public int GetCacheKeyCount()
        {
            if (_remoteCacheKeys != null)
                return _remoteCacheKeys.Count;

            return 0;
        }

        public void Dispose()
        {
#if !NETCORE
            registerRemoveEvent.Abort();
            checkCacheContain.Abort();
#else
            registerRemoveEvent.Interrupt();
            checkCacheContain.Interrupt();
#endif
            _remoteCacheKeys = null;
            _registerCallbackQueue = null;

            if (_cache != null)
                _cache.Dispose();
        }
    }
}