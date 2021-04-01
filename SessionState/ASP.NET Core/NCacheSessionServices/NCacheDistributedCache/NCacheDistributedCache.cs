using System;
using System.Configuration.Provider;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading.Tasks;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Client;
using Alachisoft.NCache.Caching.Distributed.Configuration;
using Microsoft.Extensions.Options;
using System.Threading;

namespace Alachisoft.NCache.Caching.Distributed
{
    class NCacheDistributedCache : IDistributedCache, IDisposable
    {
        private ICache _cache;
        private static ILogger _logger;
        private readonly bool _writeExceptionsToEventLog;
        private readonly bool _exceptionsEnabled;
        private readonly string _cacheName;
        private readonly IOptions<NCacheConfiguration> _options;

        public NCacheDistributedCache(IOptions<NCacheConfiguration> options)
        {
            _options = options;

            if (string.IsNullOrEmpty(options.Value.CacheName))
                throw new Runtime.Exceptions.ConfigurationException("A valid Cache ID has not been specified.");

            _cacheName = _options.Value.CacheName;

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

        internal NCacheConfiguration Configuration => _options.Value;

        private void CheckAndInitializeCache()
        {
            if (_cache == null)
                try
                {
                    CacheConnectionOptions cacheInitParams = new CacheConnectionOptions();

                    if (_options.Value.ServerList != null)
                        cacheInitParams.ServerList = _options.Value.ServerList;

                    if (!string.IsNullOrEmpty(_options.Value.BindIP))
                        cacheInitParams.ClientBindIP = _options.Value.BindIP;

                    if (_options.Value.Mode != null)
                        cacheInitParams.Mode = _options.Value.Mode;

                    if (_options.Value.LoadBalance != null)
                        cacheInitParams.LoadBalance = (bool)_options.Value.LoadBalance;

                    if (_options.Value.ClientRequestTimeOut != null)
                        cacheInitParams.ClientRequestTimeOut = _options.Value.ClientRequestTimeOut;

                    if (_options.Value.ConnectionTimeout != null)
                        cacheInitParams.ConnectionTimeout = _options.Value.ConnectionTimeout;

                    if (_options.Value.RetryInterval != null)
                        cacheInitParams.RetryInterval = _options.Value.RetryInterval;

                    if (_options.Value.RetryConnectionDelay != null)
                        cacheInitParams.RetryConnectionDelay = _options.Value.RetryConnectionDelay;

                    if (!string.IsNullOrEmpty(_options.Value.AppName))
                        cacheInitParams.AppName = _options.Value.AppName;

                    if (_options.Value.RequestTimeout != null)
                        cacheInitParams.ClientRequestTimeOut = TimeSpan.FromSeconds((double)_options.Value.RequestTimeout);

                    _cache = CacheManager.GetCache(_cacheName, cacheInitParams);
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
                LogDebug("Getting item against key [" + key + "]");
                CheckAndInitializeCache();
                if (_cache != null)
                    return _cache.Get<byte[]>(key);
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
                        CacheItem cacheItem = new CacheItem(value);
                        cacheItem.Expiration = new Runtime.Caching.Expiration(Runtime.Caching.ExpirationType.Absolute, TimeSpan.FromTicks(options.AbsoluteExpiration.Value.DateTime.Ticks));
                        cacheItem.Priority = CacheItemPriority.Default;
                        _cache?.Insert(key, cacheItem);
                        return;
                    }
                    if (options.AbsoluteExpirationRelativeToNow != null)
                    {
                        var expiration = DateTime.Now.Add(options.AbsoluteExpirationRelativeToNow.Value);
                        LogDebug("Inserting against key [" + key + "] with absolute expiration relative to now: " +
                                 expiration);
                        CacheItem cacheItem = new CacheItem(value);
                        cacheItem.Expiration = new Runtime.Caching.Expiration(Runtime.Caching.ExpirationType.Absolute, options.AbsoluteExpirationRelativeToNow.Value);
                        cacheItem.Priority = CacheItemPriority.Default;

                        _cache?.Insert(key, cacheItem);
                        return;
                    }
                    if (options.SlidingExpiration != null)
                    {
                        LogDebug("Inserting against key [" + key + "] with sliding expiration: " +
                                 options.SlidingExpiration.Value);
                        CacheItem cacheItem = new CacheItem(value);
                        cacheItem.Expiration = new Runtime.Caching.Expiration(Runtime.Caching.ExpirationType.Sliding, options.SlidingExpiration.Value);
                        cacheItem.Priority = CacheItemPriority.Default;

                        _cache?.Insert(key, cacheItem);
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

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
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
            return Task.Factory.StartNew(() => Refresh(key), token);
        }

        public void Remove(string key)
        {
            try
            {
                LogDebug("Removing Key [" + key + "]");
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
            return Task.Factory.StartNew(() => Remove(key), token);
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
