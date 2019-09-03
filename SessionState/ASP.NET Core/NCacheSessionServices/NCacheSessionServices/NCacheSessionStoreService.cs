
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using Alachisoft.NCache.Web.SessionState.Configuration;
using Alachisoft.NCache.Web.SessionState.Interface;
using Alachisoft.NCache.Web.SessionState.Utilities;
using Alachisoft.NCache.Web.SessionStateManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Alachisoft.NCache.Web.SessionState
{
    public class NCacheSessionStoreService : ISessionStoreService
    {
        private readonly IOptions<NCacheSessionConfiguration> _options;
        private readonly ISessionKeyManager _keyManager;
        private readonly NCacheCoreSessionStore _store;

        public NCacheSessionStoreService(IOptions<NCacheSessionConfiguration> options, ISessionKeyManager keyManager)
        {
            _options = options;
            _keyManager = keyManager;
            NameValueCollection nvc = new NameValueCollection {{"cacheName", _options.Value.CacheName}};

            if (_options.Value.ExceptionsEnabled != null)
                nvc.Add("exceptionsEnabled", _options.Value.ExceptionsEnabled.Value.ToString());

            if (_options.Value.WriteExceptionsToEventLog != null)
                nvc.Add("writeExceptionsToEventLog", _options.Value.WriteExceptionsToEventLog.Value.ToString());

            if (_options.Value.EnableLogs != null)
                nvc.Add("enableLogs", _options.Value.EnableLogs.Value.ToString());

            if (_options.Value.EnableDetailLogs != null)
                nvc.Add("enableDetailLogs", _options.Value.EnableDetailLogs.Value.ToString());

            if (_options.Value.EnableSessionLocking != null)
                nvc.Add("enableSessionLocking", _options.Value.EnableSessionLocking.Value.ToString());

            if (_options.Value.SessionAppId != null)
                nvc.Add("sessionAppId", _options.Value.SessionAppId);

            if (_options.Value.SessionLockingRetry != null)
                nvc.Add("sessionLockingRetry", _options.Value.SessionLockingRetry.Value.ToString());

            if (_options.Value.EmptySessionWhenLocked != null)
                nvc.Add("emptySessionWhenLocked", _options.Value.EmptySessionWhenLocked.Value.ToString());

            if (_options.Value.InprocDelay != null)
                nvc.Add("inprocDelay", _options.Value.InprocDelay.Value.ToString());

            if (_options.Value.OperationRetry != null)
                nvc.Add("operationRetry", _options.Value.OperationRetry.Value.ToString());

            if (_options.Value.OperationRetryInterval != null)
                nvc.Add("operationRetryInterval", _options.Value.OperationRetryInterval.Value.ToString());
            
                nvc.Add("defaultSessionTimeout", (_options.Value.RequestTimeout != 0? _options.Value.RequestTimeout.ToString():"120"));

            NCacheSessionStateSettings affinitySettings = null;
            if (_options.Value.EnableLocationAffinity != null && _options.Value.EnableLocationAffinity.Value)
            {
                affinitySettings = new NCacheSessionStateSettings
                {
                    PrimaryCache = new Hashtable(),
                    SecondaryCaches = new Hashtable()
                };
                foreach (var cacheAffinity in _options.Value.AffinityMapping)
                {
                    if (cacheAffinity.CacheName == null || cacheAffinity.CachePrefix == null ||
                        cacheAffinity.CachePrefix.Length < 4)
                    {
                        throw new ConfigurationErrorsException("Invalid cache affinity settings specified. ");
                    }
                    if (affinitySettings.PrimaryCache.Count == 0 &&
                        cacheAffinity.CacheName.Equals(_options.Value.CacheName))
                        affinitySettings.PrimaryCache.Add(cacheAffinity.CachePrefix, cacheAffinity.CacheName);
                    else if (!affinitySettings.SecondaryCaches.ContainsKey(cacheAffinity.CachePrefix))
                        affinitySettings.SecondaryCaches.Add(cacheAffinity.CachePrefix, cacheAffinity.CacheName);
                }
                if (affinitySettings.PrimaryCache.Count == 0)
                {
                    throw new ConfigurationErrorsException(
                        "No affinity setting has been specified for the primary cache. ");
                }
                _keyManager = new RegionalKeyWrapper(affinitySettings, _keyManager);
            }
            _store = new NCacheCoreSessionStore();
            _store.Initialize(null, nvc);
        }

        internal ISessionKeyManager KeyManager => _keyManager;

        internal NCacheSessionConfiguration Configuration => _options.Value;

        private AspCoreEnvironmentContext WrapContext(HttpContext context)
        {
            return new AspCoreEnvironmentContext(context);
        }

        public void CreateUninitializedItem(HttpContext context, string id, int timeOut)
        {
            var newContext = WrapContext(context);
            try
            {
                _store.CreateUninitializedItem(newContext, id, timeOut);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public void SetAndReleaseItemExclusive(HttpContext context, string id, object items, object lockId, bool newItem,
            int timeout)
        {
            var newContext = WrapContext(context);
            try
            {
                _store.SetAndReleaseItemExclusive(newContext, id, items, lockId, newItem, timeout);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public object GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId,
            out SessionInitializationActions action)
        {

            var newContext = WrapContext(context);
            try
            {
                return _store.GetItem(newContext, id, out locked, out lockAge, out lockId, out action);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public object GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge,
            out object lockId, out SessionInitializationActions action)
        {
            var newContext = WrapContext(context);
            try
            {
                return _store.GetItemExclusive(newContext, id, out locked, out lockAge, out lockId, out action);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public void RemoveItem(HttpContext context, string id, object lockId)
        {
            var newContext = WrapContext(context);
            try
            {
                _store.RemoveItem(newContext, id, lockId);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            var newContext = WrapContext(context);
            try
            {
                _store.ReleaseItemExclusive(newContext, id, lockId);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public object CreateNewStoreData(HttpContext context, int timeOut)
        {
            var newContext = WrapContext(context);
            try
            {
                return _store.CreateNewStoreData(newContext, timeOut);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public string GetSessionKey(HttpContext context, out bool isNew)
        {
            return _keyManager.GetSessionKey(context, out isNew);
        }

        public void ApplySessionKey(HttpContext context, string key)
        {
            _keyManager.ApplySessionKey(context, key);
        }

        public void LogError(string message, string sessionId)
        {
            _store.LogError(message, sessionId);
        }

        public void LogError(Exception ex, string sessionId)
        {
            _store.LogError(ex, sessionId);
        }

        public void LogInfo(string message, string sessionId)
        {
            _store.LogInfo(message, sessionId);
        }

        public void LogDebug(string message, string sessionId)
        {
            _store.LogDebug(message, sessionId);
        }
    }
}
