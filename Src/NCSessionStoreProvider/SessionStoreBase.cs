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
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Threading;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Web.SessionStateManagement;

using System.Runtime.Serialization.Formatters.Binary;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common;
using System.Diagnostics;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Web.SessionState
{
    /// <summary>
    /// Identifies whether a session item from a data store is for a session that requires initialization.
    /// </summary>
    [Flags]
    public enum SessionInitializationActions
    {
        None = 0,
        InitializeItem = 1,
    }

    public abstract class SessionStoreBase
    {
        private const string SOURCE = "NCacheSessionProvider";
        private const string SESSION_LOCK_COUNT = "LockCount";
        private const string TIMEOUT_KEY = "T";
        private const string ACTIONS_KEY = "F";
        private const string SESSION_DATA = "SD";
        private static string s_applicationId = null;
        private static ISessionCache _cache = null;
        private string _cacheId = null;
        private bool _exceptionsEnabled = true;
        private bool _logs = false;
        protected bool _detailedLogs = false;
        private static bool s_cacheNeedInit = true;
        private static EventHandler s_onAppDomainUnload;
        private bool _writeExceptionsToEventLog = false;
        private static object s_dataLock = new object();
        private static int _inprocDelay = 5000;
        private string _appName;
        private bool _lockSessions = true;
        private static ILogger _ncacheLog;
        private int _operationRetry;
        private int _operationRetryInterval;

        public static ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

        public int OperationRetry
        {
            get { return _operationRetry; }
        }

        public static string ApplicationId
        {
            get { return s_applicationId; }
        }

        public static string SessionCacheName
        {
            get
            {
                if (_cache != null)
                {
                    return _cache.GetCacheId;
                }
                else
                { return null; }
            }
        }

        /// <summary>
        /// Initializes the provider. Takes, as input, the name of the provider and a
        /// NameValueCollection of configuration settings. This method is used to set 
        /// property values for the provider instance, including implementation-specific 
        /// values and options specified in the configuration file
        /// (Machine.config or Web.config).
        /// 
        /// "name" The friendly name of the provider.</param>
        /// config A collection of the name/value pairs representing the
        /// provider-specific attributes specified in the
        /// configuration for this provider.</param>
        /// </summary>
        /// <param name="name">Friendly name of provider</param>
        /// <param name="config">Representing the provider specific attributes</param>
        public virtual void Initialize(string name, NameValueCollection config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (string.IsNullOrEmpty(config["cacheName"])) throw new ConfigurationErrorsException("The 'cacheName' attribute cannot be null or empty string");


            if (string.IsNullOrEmpty(config["description"])) config["description"] = "NCache Session Storage Provider";

            if (name == null || name.Length == 0) name = SOURCE;

            string[] boolValStrings = {"exceptionsEnabled", "writeExceptionsToEventLog", "enableLogs", "enableDetailLogs", "enableSessionLocking"};
            string configVal = null;
            bool value = false;

            for (int i = 0; i < boolValStrings.Length; i++)
            {
                configVal = config[boolValStrings[i]];
                if (configVal != null)
                {
                    if (!configVal.Equals("true", StringComparison.OrdinalIgnoreCase) &&
                        !configVal.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ConfigurationErrorsException("The '" + boolValStrings[i] + "' attribute must be one of the following values: true, false.");
                    }
                    value = Convert.ToBoolean(configVal);
                    switch (i)
                    {
                        case 0:
                            _exceptionsEnabled = value;
                            break;
                        case 1:
                            _writeExceptionsToEventLog = value;
                            break;
                        case 2:
                            _logs = value;
                            break;
                        case 3:
                        {
                            _detailedLogs = value;
                                if(_detailedLogs)
                                    _logs = value;
                        }
                            break;
                        case 4:
                            _lockSessions = value;
                            break;

                    }
                }
            }

            if (config["sessionAppId"] != null)
                s_applicationId = config["sessionAppId"];

            //get cache name from configurations            
            _cacheId = config["cacheName"];

            string inprocDelay = config["inprocDelay"];

            if (!string.IsNullOrEmpty(inprocDelay))
                _inprocDelay = Convert.ToInt32(inprocDelay);

            if (_inprocDelay <= 5000) _inprocDelay = 5000;

            if (!String.IsNullOrEmpty(config["operationRetry"]))
            {
                try
                {
                    this._operationRetry = Convert.ToInt32(config["operationRetry"]);
                }
                catch (Exception e)
                {
                    throw new Exception("Invalid value specified for operationRetry.");
                }
            }
            if (!String.IsNullOrEmpty(config["operationRetryInterval"]))
            {
                try
                {
                    this._operationRetryInterval = Convert.ToInt32(config["operationRetryInterval"]);
                }
                catch (Exception e)
                {
                    throw new Exception("Invalid value specified for operationRetryInterval.");
                }
            }
            InitializeCache();
        }

        private string GetUniqueSessionId(string sessionId)
        {
            return string.IsNullOrEmpty(s_applicationId) ? sessionId : sessionId + "." + s_applicationId;
        }

        private void InitializeCache()
        {
            lock (s_dataLock)
            {
                try
                {
                    if (_cache == null)
                    {
                        if (_logs || _detailedLogs)
                        {
                            if (_ncacheLog == null)
                            {
                                _ncacheLog = new NCacheLogger();
                                _ncacheLog.Initialize(LoggerNames.SessionStoreProvider, _cacheId);

                                if (_detailedLogs)
                                {
                                    NCacheLog.SetLevel("all");
                                }
                                else
                                {
                                    if (_logs)
                                        NCacheLog.SetLevel("info");
                                }
                            }
                        }

                        _cache = new SingleRegionCache(_operationRetry, _operationRetryInterval);
                        _cache.InitializeCache(_cacheId);
                        _cache.ExceptionsEnabled = true;
                        s_cacheNeedInit = false;
                        OnCacheInitialize();
                        if (_logs) LogInfo("NSessionStoreProvider initialized", null);
                        Thread.Sleep(_inprocDelay);
                    }
                }
                catch (Exception exc)
                {
                    _cache = null; // so that next time cache can be initialized. Check the above condition if(_cache == null)                 
                    LogError(exc, null);
                }
            }
        }

        protected virtual void OnCacheInitialize()
        {
            
        }

        public virtual void Dispose()
        {
        }

        private void OnAppDomainUnload(object unusedObject, EventArgs unusedEventArgs)
        {
            DisposeCache();
        }

        protected void DisposeCache()
        {
            try
            {
                if (_cache != null)
                {
                    lock (s_dataLock)
                    {
                        s_cacheNeedInit = true;
                        _cache.Dispose();
                        _cache = null;
                    }
                }
                if (_logs) LogInfo("Disposed", null);
            }
            catch (Exception exc)
            {
                LogError(exc, null);
            }
            finally
            {
            }
        }

        public virtual void CreateUninitializedItem(IAspEnvironmentContext context, string id, int timeOut)
        {
            try
            {
                PutInNCache(context, id, InsertContents(context, null, SessionInitializationActions.InitializeItem, timeOut), null, true);
                if (_detailedLogs) LogDebug("New session added to cache.", id);
            }
            catch (Exception)
            {
                try
                {
                    if (_cache != null)
                    {
                        _cache.Remove(GetUniqueSessionId(id), false);
                    }
                }
                catch (Exception)
                {
                }

                try
                {
                    PutInNCache(context, id, InsertContents(context, null, SessionInitializationActions.InitializeItem, timeOut), null, false);
                    if (_detailedLogs) LogDebug("New session added to cache.", id);
                }
                catch (Exception exc)
                {
                    LogError(exc, id);
                }
            }
        }

        /**<summary>
         * Inserts item in store. 
         * When an items expires, its automatically been removed from cache, so we do not have
         * to check the expiration time, and simply an Insert call to cache will add the items if
         * its new or updates the item if it already exists, so newItem parameter is also useless
         * </summary>**/
        public virtual void SetAndReleaseItemExclusive(IAspEnvironmentContext context, string id, object items, object lockID, bool newItem,int timeout)
        {
            try
            {
                PutInNCache(context, id, InsertContents(context, items, SessionInitializationActions.None, timeout), lockID, true);
            }
            catch (Exception)
            {
                try
                {
                    if (_cache != null)
                    {
                        _cache.Remove(GetUniqueSessionId(id), true);
                    }
                }
                catch (Exception ex)
                {
                    if (_detailedLogs) LogDebug("SetAndReleaseItemExclusive failed, Exception:" + ex, id);
                }

                try
                {
                    PutInNCache(context, id, InsertContents(context, items, SessionInitializationActions.None, timeout), lockID, false);
                }
                catch (Exception exc)
                {
                    LogError(exc, id);
                }
            }
        }

        public virtual object GetItem(IAspEnvironmentContext context, string id, out bool locked, out TimeSpan lockAge, out object lockID, out SessionInitializationActions action)
        {
            return getSessionStoreItem(false, context, id, out locked, out lockAge, out lockID, out action);
        }

        public virtual object GetItemExclusive(IAspEnvironmentContext context, string id, out bool locked, out TimeSpan lockAge, out object lockID, out SessionInitializationActions action)
        {
            return getSessionStoreItem(_lockSessions, context, id, out locked, out lockAge, out lockID, out action);
        }

        public virtual void RemoveItem(IAspEnvironmentContext context, string id, object lockID)
        {
            try
            {
                if (_cache != null)
                {
                    lock (_cache)
                    {
                        bool removeFromCache = true;
                        if (lockID is LockHandle) removeFromCache = (((LockHandle)lockID).LockId != null);

                        if (removeFromCache)
                        {
                            if (_lockSessions)
                            {
                                _cache.Remove(GetUniqueSessionId(id), lockID as LockHandle, true);
                            }
                            else
                                _cache.Remove(GetUniqueSessionId(id), true);
                        }
                    }
                }
                if (_detailedLogs) LogDebug("Session removed from cache.", id);
            }
            catch (Exception)
            {
                try
                {
                    if (_cache != null)
                    {
                        lock (_cache)
                        {
                            if (_lockSessions)
                            {
                                _cache.Remove(GetUniqueSessionId(id), lockID as LockHandle, false);
                            }
                            else
                                _cache.Remove(GetUniqueSessionId(id), false);
                        }
                    }
                    if (_detailedLogs) LogDebug("Session removed from cache. ", id);
                }
                catch (Exception exc)
                {
                    if (_detailedLogs) LogDebug("RemoveItem failed, Exception: " + exc.ToString(), id);
                    LogError(exc, id);
                }
            }
        }

        public virtual void ResetItemTimeout(IAspEnvironmentContext context, string id) { }

        public virtual void ReleaseItemExclusive(IAspEnvironmentContext context, string id, object lockID)
        {
            ///We are using current context items to hold some values. So we have to delete them before
            ///current request completes or is forced to complete.
            context.RemoveItem(SESSION_LOCK_COUNT);

            if (_lockSessions)
            {
                ReleaseSessionItemLock(context, id, lockID);
            }
        }

        private bool ReleaseSessionItemLock(IAspEnvironmentContext context, string sessionid, object lockID)
        {
            string locationID = sessionid;
            try
            {
                _cache.Unlock(GetUniqueSessionId(sessionid));
                return true;
            }
            catch (Exception)
            {
                try
                {
                    _cache.Unlock(GetUniqueSessionId(sessionid));
                    return true;
                }
                catch (Exception e)
                {
                    if (_detailedLogs) LogDebug("ReleaseSessionItemLock failed, Exception: " + e.ToString(), sessionid);
                    LogError(e, sessionid);
                }
            }
            return false;
        }
        public virtual void InitializeRequest(IAspEnvironmentContext context) { }

        public virtual void EndRequest(IAspEnvironmentContext context) { }

        private object getSessionStoreItem(bool acquireLock, IAspEnvironmentContext context, string id, out bool locked, out TimeSpan lockAge, out object lockID, out SessionInitializationActions action)
        {
            //lock age is set to zero, so SessionStateModule will,
            //after 0.5 sec, calls this GetItemExclusive function until a value or
            //null reference is returned. If no data was found, 'locked' is set to false and
            //null reference is returned which tells SessionStateModule to call CreateDataStore
            //function to create new data store

            locked = false;
            lockAge = TimeSpan.Zero;
            lockID = null;
            action = SessionInitializationActions.InitializeItem;
            DateTime lockDate = DateTime.UtcNow;

            LockHandle lockHandle = new LockHandle(null, lockDate);
            object items = null;
            Hashtable table = null;
            bool lockTimedOut = false;

            if (_cache == null)
            {
                LogError(new OperationFailedException("The specified cache is not running. "), id);
            }
            try
            {
                byte[] buffer = null;
                lock (s_dataLock)
                {
                    if (s_cacheNeedInit) InitializeCache();
                }
                if (_lockSessions)
                {
                    try
                    {
                        buffer = (byte[])_cache.Get(GetUniqueSessionId(id), ref lockHandle, acquireLock, true);
                    }
                    catch (Exception)
                    {
                        buffer = (byte[])_cache.Get(GetUniqueSessionId(id), ref lockHandle, acquireLock, false);
                    }


                    lockID = lockHandle.LockId == null ? null : lockHandle;
                    lockDate = lockHandle == null ? DateTime.UtcNow : lockHandle.LockDate;
                }
                else
                {
                    try
                    {
                        buffer = (byte[])_cache.Get( GetUniqueSessionId(id), true);
                    }
                    catch (Exception)
                    {
                        buffer = (byte[])_cache.Get( GetUniqueSessionId(id), false);
                    }

                }
                if (buffer != null)
                {
                    using (MemoryStream stream = new MemoryStream(buffer))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        table = formatter.Deserialize(stream) as Hashtable;
                        stream.Close();
                    }
                }
                if (_lockSessions && !String.IsNullOrEmpty(lockHandle.LockId))
                {
                    DateTime now = DateTime.UtcNow;
                    if ((0L < lockDate.Ticks) && (lockDate.Ticks < now.Ticks))
                    {
                        lockAge = (TimeSpan)(now - lockDate);
                    }
                    else
                    {
                        lockAge = TimeSpan.Zero;
                    }

                    /// Following 'if' block is executed if item is locked.
                    /// i.e NCache API returns null and out variables are populated with lockId and lockDate
                    /// Note: Item exists in cache but is locked.
                    if (table == null)
                    {
                        locked = true;
                        return null;
                    }
                }

                /// If item exists in cahce and lock acqusition was successfull or locking was disabled.
                if (table != null)
                {
                    items = GetContents(context, table, ref action);
                    if (action == SessionInitializationActions.InitializeItem)
                    {
                        int timeout = (int)table[TIMEOUT_KEY];
                        items = CreateNewStoreData(context, timeout);
                    }
                }
                else
                {
                    if (NCacheLog != null && NCacheLog.IsInfoEnabled) NCacheLog.Warn("SessionStoreBase.GetSessionItem", "Session not found in cache. " + id);
                }
            }
            catch (Exception exc)
            {
                LogError(exc, id);
            }
            finally
            {
                if ((lockTimedOut || table != null) && context.ContainsItem(SESSION_LOCK_COUNT))
                    context.RemoveItem(SESSION_LOCK_COUNT);
            }
            return items;
        }

        protected virtual object CreateEmptySession(IAspEnvironmentContext context,int sessionTimeout)
        {
            return null;
        }

        public virtual object CreateNewStoreData(IAspEnvironmentContext context, int timeOut)
        {
            return null;
        }
        
        /// <summary>
        /// Insert information in cache, along with the expirations and callback.
        /// </summary>
        /// <param name="id">Session ID</param>
        /// <param name="table">Value needed to be stored</param>
        /// 
        private void PutInNCache(IAspEnvironmentContext context, string id, Hashtable table, object lockID, bool enableRetry)
        {
            byte[] buffer = null;
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, table);
                buffer = stream.ToArray();
                stream.Close();
            }
            CacheItem sessionItem = new CacheItem(buffer);
            sessionItem.Priority = CacheItemPriority.NotRemovable;
            sessionItem.SlidingExpiration = new TimeSpan(0, (int)table[TIMEOUT_KEY], 0);
            lock (s_dataLock)
            {
                if (s_cacheNeedInit) InitializeCache();
            }

            if (_cache == null)
            {
                LogError(new OperationFailedException("The specified cache is not running. "), id);
            }



            if (_lockSessions)
            {
                _cache.Insert(GetUniqueSessionId(id), sessionItem, lockID as LockHandle, true, enableRetry);
            }
            else
                _cache.Insert(GetUniqueSessionId(id), sessionItem, enableRetry);
            _cache.CurrentSessionCache = null;
        }

        /// <summary>
        /// Form a hashtable to be added to cache
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <param name="flag"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private Hashtable InsertContents(IAspEnvironmentContext context, object data, SessionInitializationActions flag, int timeout)
        {
            Hashtable items = new Hashtable(4);

            if (data != null)
            {
                byte[] buffer = SerializeSession(data);
                items.Add(SESSION_DATA, buffer);
                items.Add(TIMEOUT_KEY, timeout);
            }
            else
                items.Add(TIMEOUT_KEY, (int)timeout);
            
            items.Add(ACTIONS_KEY, flag);

            return items;
        }

        /// <summary>
        /// Extract session data from hastable fetched from cache
        /// </summary>
        /// <param name="context"></param>
        /// <param name="items"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        private object GetContents(IAspEnvironmentContext context, Hashtable items, ref SessionInitializationActions flag)
        {
            flag = (SessionInitializationActions)items[ACTIONS_KEY];
            byte[] buffer = items[SESSION_DATA] as byte[];
            int timeout = (int)items[TIMEOUT_KEY];
            return DeserializeSession(buffer,timeout);
        }

        protected abstract object DeserializeSession(byte[] buffer, int timeout);

        protected abstract byte[] SerializeSession(object sessionData);
  
        public void LogError(Exception exception, string sessionID)
        {
            try
            {
                if (sessionID == null) sessionID = "";
                if (NCacheLog != null) NCacheLog.Error("[Error]", sessionID + " :" + exception);
            }
            catch (Exception ex)
            {
                if (_writeExceptionsToEventLog)
                {
                    try
                    {
                        AppUtil.LogEvent(SOURCE, ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                    }
                    catch (Exception exc)
                    {
                        throw new ProviderException(exc.Message, exc);
                    }
                }
            }
            RaiseException(exception, sessionID);
        }

        public void LogError(string message, string sessionID)
        {
            try
            {
                if (sessionID == null) sessionID = "";
                if (NCacheLog != null) NCacheLog.Error("[Error]", sessionID + " :" + message);

            }
            catch (Exception ex)
            {
                LogError(ex, sessionID);
            }
        }

        public void LogInfo(string message, string sessionID)
        {
                try
                {
                    if (sessionID == null) sessionID = "";
                    if (NCacheLog != null) NCacheLog.Info("[Info]", sessionID + " :" + message);
                }
                catch (Exception ex)
                {
                    LogError(ex, sessionID);
                }
        }

        public void LogDebug(string message, string sessionID)
        {
                try
                {
                    if (sessionID == null) sessionID = "";
                    if (NCacheLog != null) NCacheLog.Debug("[Debug]", sessionID + " :" + message);
                }
                catch (Exception ex)
                {
                    LogError(ex, sessionID);
                }
        }

        private void RaiseException(Exception exc)
        {
            LogError(exc, "");
        }

        /// <summary>
        /// Writes the exception to event log
        /// </summary>
        /// <param name="exc">Exception to be written</param>
        private void RaiseException(Exception exc, string sessionID)
        {
            if (_writeExceptionsToEventLog)
            {
                try
                {
                    AppUtil.LogEvent(SOURCE, exc.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
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
    }
}
