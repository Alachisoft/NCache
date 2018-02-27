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
using System.Threading;
using System.Configuration.Provider;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading.Tasks;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Web.SessionState.Configuration;
using Microsoft.Extensions.Options;

namespace Alachisoft.NCache.Web.SessionState
{
    class NCacheDistributedCache : IDistributedCache, IDisposable
    {
        private Cache _cache;
        private static ILogger _logger;
        private readonly bool _writeExceptionsToEventLog;
        private readonly bool _exceptionsEnabled;
        private readonly string _cacheName;

        public NCacheDistributedCache(IOptions<NCacheSessionConfiguration> options)
        {
            if(string.IsNullOrEmpty(options.Value.CacheName))
                throw new Runtime.Exceptions.ConfigurationException("A valid Cache ID has not been specified.");

            _cacheName = options.Value.CacheName;

            if ((options.Value.EnableLogs != null && options.Value.EnableLogs.Value) ||
                (options.Value.EnableDetailLogs != null && options.Value.EnableDetailLogs.Value))
            {
                try
                {
                    _logger = new NCacheLogger();
                    _logger.Initialize(LoggerNames.SessionStoreProvider, options.Value.CacheName);

                    if (options.Value.EnableDetailLogs != null && options.Value.EnableDetailLogs.Value)
                        _logger.SetLevel("all");
                    else
                        _logger.SetLevel("info");
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
            }

            if (options.Value.WriteExceptionsToEventLog != null && options.Value.WriteExceptionsToEventLog.Value)
                _writeExceptionsToEventLog = true;

            if (options.Value.ExceptionsEnabled != null && options.Value.ExceptionsEnabled.Value)
                _exceptionsEnabled = true;

            CheckAndInitializeCache();
        }

        private void CheckAndInitializeCache()
        {
            if (_cache == null)
                try
                {
                    _cache = Caching.NCache.InitializeCache(_cacheName);
                    LogDebug("Cache " + _cache + " has been initialized. ");
                }
                catch (Exception ex)
                {
                    _cache = null;
                    LogError(ex);
                }
        }

        public byte[] Get(string key)
        {
            try
            {
                LogDebug("Getting item against key [" + key+ "]");
                CheckAndInitializeCache();
                if (_cache != null)
                    return _cache.Get(key) as byte[];
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return Task.Factory.StartNew(() => Get(key), token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            try
            {
                CheckAndInitializeCache();
                if (options != null)
                {
                    if (options.AbsoluteExpiration != null)
                    {
                        LogDebug("Inserting against key [" + key + "] with absolute expiration: " +
                                 options.AbsoluteExpiration.Value.DateTime);
                        _cache?.Insert(key, value, options.AbsoluteExpiration.Value.DateTime,
                            Cache.NoSlidingExpiration,
                            CacheItemPriority.Default);
                        return;
                    }
                    if (options.AbsoluteExpirationRelativeToNow != null)
                    {
                        var expiration = DateTime.Now.Add(options.AbsoluteExpirationRelativeToNow.Value);
                        LogDebug("Inserting against key [" + key + "] with absolute expiration relative to now: " +
                                 expiration);
                        _cache?.Insert(key, value, expiration,
                            Cache.NoSlidingExpiration, CacheItemPriority.Default);
                        return;
                    }
                    if (options.SlidingExpiration != null)
                    {
                        LogDebug("Inserting against key [" + key + "] with sliding expiration: " +
                                 options.SlidingExpiration.Value);
                        _cache?.Insert(key, value, Cache.NoAbsoluteExpiration, options.SlidingExpiration.Value,
                            CacheItemPriority.Default);
                        return;
                    }
                    _cache?.Insert(key, value);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options , CancellationToken token = default(CancellationToken))
        {
            return Task.Factory.StartNew(() => Set(key, value, options), token);
        }

        public void Refresh(string key)
        {
            try
            {
                LogDebug("Refreshing Key [" + key + "]");
                CheckAndInitializeCache();
                _cache?.Contains(key);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return Task.Factory.StartNew(()=>Refresh(key), token);
        }

        public void Remove(string key)
        {
            try
            {
                LogDebug("Removing Key ["+ key+"]");
                CheckAndInitializeCache();
                _cache?.Remove(key);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return Task.Factory.StartNew(()=> Remove(key), token);
        }

        public void LogError(Exception exception)
        {
            try
            {
                _logger?.Error("[Error]: " + exception);
            }
            catch (Exception ex)
            {
                if (_writeExceptionsToEventLog)
                {
                    try
                    {
                        AppUtil.LogEvent("NCacheDistributedCache", ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                    }
                    catch (Exception exc)
                    {
                        throw new ProviderException(exc.Message, exc);
                    }
                }
            }
            RaiseException(exception);
        }

        public void LogError(string message)
        {
            try
            {
                _logger?.Error("[Error]: " + message);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public void LogInfo(string message)
        {
            try
            {
                _logger?.Info("[Info]: " + message);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public void LogDebug(string message)
        {
            try
            {
                _logger?.Debug("[Debug]: " + message);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        /// <summary>
        /// Writes the exception to event log
        /// </summary>
        /// <param name="exc">Exception to be written</param>
        private void RaiseException(Exception exc)
        {
            if (_writeExceptionsToEventLog)
            {
                try
                {
                    AppUtil.LogEvent("NCacheDistributedCache", exc.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                }
                catch (Exception ex)
                {
                    throw new ProviderException(ex.Message, ex);
                }
            }
            if (_exceptionsEnabled)
            {
                throw new ProviderException(exc.Message, exc);
            }
        }

        public void Dispose()
        {
            _cache?.Dispose();
        }
    }
}
