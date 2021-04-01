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
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Diagnostics;
using System.Threading;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Web.SessionStateManagement;
using System.Runtime.Serialization.Formatters.Binary;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Client;

namespace Alachisoft.NCache.Web.SessionState
{
    public abstract class SessionStoreBase
    {
        private const string SOURCE = "NCacheSessionProvider";
        private const string APPLOCKKEY = "SSPAppLocked";
        private const string APPID = "SSPAppId";
        private const string SESSION_LOCK_COUNT = "LockCount";
        private const string LOC_IDENTIFIER = "New_Location";
        private const string ITEMS_KEY = "I";
        private const string STATIC_ITEMS_KEY = "O";
        private const string TIMEOUT_KEY = "T";
        private const string ACTIONS_KEY = "F";
        private const string SESSION_DATA = "SD";
        private static string s_applicationId = null;
        private static ISessionCache _cache = null;
        private string _cacheId = null;
        private int _defaultTimeout;
        private bool _writeExceptionsToEventLog = false;
        private bool _exceptionsEnabled = true;
        private bool _logs = false;
        protected bool _detailedLogs = false;
        private static bool s_cacheNeedInit = true;

        ///-1 by default i.e do as microsoft does(retry till request timeout). 
        /// otherwise if after retries completed and session is still locked return empty session.
        private int _sessionLockingRetries = -1;
        private bool _emptySessionWhenLocked = false;///for Ryan Air                                           
        private static EventHandler s_onAppDomainUnload;

        private static object s_dataLock = new object();
        private static int _inprocDelay = 5000;
        private string _appName;
        private bool _lockSessions = true;
        protected bool _isLocationAffinityEnabled = false;
        private static ILogger _ncacheLog;
        private int _operationRetry;
        private int _operationRetryInterval;
       // private NCacheSessionStateSettings _regionalCacheSessionStateSettings;

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

            /* RECONSIDER
            //get the application virtual path
            _appName = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;
            */

            string[] boolValStrings = {"exceptionsEnabled", "writeExceptionsToEventLog",
                                       "enableLogs", "enableDetailLogs", "enableSessionLocking"};
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
                        throw new ConfigurationErrorsException("The '" + boolValStrings[i] +
                                                               "' attribute must be one of the following values: true, false.");
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

            if (config["sessionLockingRetry"] != null)
                this._sessionLockingRetries = Convert.ToInt32(config["sessionLockingRetry"]);

            if (config["emptySessionWhenLocked"] != null)
                this._emptySessionWhenLocked = Convert.ToBoolean(config["emptySessionWhenLocked"]);

            //get cache name from configurations            
            _cacheId = config["cacheName"];

            //if (!string.IsNullOrEmpty(config["exceptionsEnabled"]) && config["exceptionsEnabled"].ToLower() == "true")
            //    _exceptionsEnabled = true;

            /* RECONSIDER
            //get session state configuration section
            Configuration cfg = WebConfigurationManager.OpenWebConfiguration(_appName);
            SessionStateSection sessionConfig = (SessionStateSection)cfg.GetSection("system.web/sessionState");

             _defaultTimeout = sessionConfig.Timeout.Minutes;
            */

            if (config["defaultSessionTimeout"] != null)
                this._defaultTimeout = Convert.ToInt32(config["defaultSessionTimeout"]);

           
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
                                string logFileNmae = _cacheId;

                                _ncacheLog = new NCacheLogger();
                                _ncacheLog.Initialize(LoggerNames.SessionStoreProvider, logFileNmae);

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
            //GC.Collect();
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
                _cache.IsSessionCookieless = IsSessionCookieless(context);
                PutInNCache(context, id, InsertContents(context, null, SessionInitializationActions.InitializeItem, timeOut), null, true);
                if (_detailedLogs) LogDebug("New session added to cache.", id);
            }
            catch (Exception)
            {
                try
                {
                    if (_cache != null)
                    {
                        string locationID = GetLocationID(context, id);
                        _cache.Remove(locationID, GetUniqueSessionId(id), false);
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
                _cache.IsSessionCookieless = IsSessionCookieless(context);
                PutInNCache(context, id, InsertContents(context, items, SessionInitializationActions.None, timeout), lockID, true);
            }
            catch (Exception ex1)
            {
                try
                {
                    if (_cache != null)
                    {
                        string locationID = GetLocationID(context, id);
                        _cache.Remove(locationID, GetUniqueSessionId(id), true);
                    }
                }
                catch (Exception ex2)
                {
                    if (_detailedLogs) LogDebug("SetAndReleaseItemExclusive failed, Exception:" + ex2, id);
                }

                try
                {
                    PutInNCache(context, id, InsertContents(context, items, SessionInitializationActions.None, timeout), lockID, false);
                }
                catch (Exception ex3)
                {
                    LogError(ex3, id);
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
                _cache.IsSessionCookieless = IsSessionCookieless(context);
                if (_cache != null)
                {
                    string locationID = GetLocationID(context, id);

                    lock (_cache)
                    {
                        bool removeFromCache = true;
                        if (lockID is LockHandle) removeFromCache = (((LockHandle)lockID).LockId != null);

                        if (removeFromCache)
                        {
                            if (_lockSessions)
                            {
                                _cache.Remove(locationID, GetUniqueSessionId(id), lockID as LockHandle, true);
                            }
                            else
                                _cache.Remove(locationID, GetUniqueSessionId(id), true);
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
                        string locationID = GetLocationID(context, id);

                        lock (_cache)
                        {
                            if (_lockSessions)
                            {
                                _cache.Remove(locationID, GetUniqueSessionId(id), lockID as LockHandle, false);
                            }
                            else
                                _cache.Remove(locationID, GetUniqueSessionId(id), false);
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
                _cache.Unlock(locationID, GetUniqueSessionId(sessionid));//, lockID);
                return true;
            }
            catch (Exception)
            {
                try
                {
                    _cache.Unlock(locationID, GetUniqueSessionId(sessionid));
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
            string locationID = GetLocationID(context, id);

            
            if (_cache == null)
            {
                LogError(new OperationFailedException("The specified cache is not running. "), id);
                return null;
            }

            try
            {
                _cache.IsSessionCookieless = IsSessionCookieless(context);
                byte[] buffer = null;
                lock (s_dataLock)
                {
                    if (s_cacheNeedInit) InitializeCache();
                }
                if (_lockSessions)
                {
                    try
                    {
                        buffer = (byte[])_cache.Get(locationID, GetUniqueSessionId(id), ref lockHandle, acquireLock, true);
                    }
                    catch (Exception)
                    {
                        buffer = (byte[])_cache.Get(locationID, GetUniqueSessionId(id), ref lockHandle, acquireLock, false);
                    }


                    lockID = lockHandle.LockId == null ? null : lockHandle;
                    lockDate = lockHandle == null ? DateTime.UtcNow : lockHandle.LockDate;
                }
                else
                {
                    try
                    {
                        buffer = (byte[])_cache.Get(locationID, GetUniqueSessionId(id), true);
                    }
                    catch (Exception)
                    {
                        buffer = (byte[])_cache.Get(locationID, GetUniqueSessionId(id), false);
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
                        if (this._sessionLockingRetries >= 0)
                        {
                            if (!context.ContainsItem(SESSION_LOCK_COUNT))
                                context[SESSION_LOCK_COUNT] = 0;

                            int retriesCompleted = (int)context[SESSION_LOCK_COUNT];

                            if (retriesCompleted < this._sessionLockingRetries)
                                context[SESSION_LOCK_COUNT] = retriesCompleted + 1;
                            else ///this will construct and send a dummy session to application
                            {
                                if (this._emptySessionWhenLocked)
                                {
                                    locked = false;///a dummy session is going to be returned
                                    lockID = null;
                                    lockTimedOut = true;

                                    return CreateEmptySession(context,_defaultTimeout);
                                }
                                else
                                {
                                    throw new ProviderException("Cannot acquire session lock. Session is already locked.");
                                }
                            }
                        }

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
                /// If item is not found in Remote cache. 
                /// Update cookies to do any further operations for this session request on current primary cache;
                /// and do not raise exception.

                    LogError(exc, id);

            }
            finally
            {
                if ((lockTimedOut || table != null) && context.ContainsItem(SESSION_LOCK_COUNT))
                    context.RemoveItem(SESSION_LOCK_COUNT);
               //    throw new ProviderException("Cannot acquire session lock. Session is already locked.");
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
        /// Adds the Current Primary cache's location identifier in the Response cookie collection.
        /// </summary>
        /// <param name="context">Context of current HttpRequest.</param>
        protected void UpdateCookies(IAspEnvironmentContext context)
        {
            if (_cache != null)
            {
                if (!string.IsNullOrEmpty(_cache.PrimaryPrefix))
                {
                    context.SetLocationCookie(_cache.PrimaryPrefix);
                    //context.Response.Cookies.Set(new HttpCookie(LOC_IDENTIFIER, _cache.PrimaryPrefix));
                    if (_logs) LogInfo("Session Location Changed: New_Location=" + _cache.PrimaryPrefix, null);
                }
            }
        }

        /// <summary>
        /// Returns the location identifier for this HttpRequest context. Response cookies are searched first 
        /// and if no cookie is found then Requeset cookies are searched for location identifier.
        /// If there isno cookie in Request or Response then the Location Identifier in the SessionId id returned.
        /// </summary>
        /// <param name="context">Context of current HttpRequest</param>
        /// <param name="sessionId">SessionId for the Request</param>
        /// <returns>Location Identifer if cookie is found other wise the sessionId is returned.</returns>
        private string GetLocationID(IAspEnvironmentContext context, string sessionId)
        {
            string locationID = sessionId;
            return locationID;
        }

        private int GetIndexOfLocationCookie(string[] allKeys)
        {
            for (int i = 0; i < allKeys.Length; i++)
            {
                if (allKeys[i].Equals(LOC_IDENTIFIER))
                    return i;
            }
            return -1;
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
            sessionItem.Expiration = new Runtime.Caching.Expiration(Runtime.Caching.ExpirationType.Sliding, new TimeSpan(0, (int)table[TIMEOUT_KEY], 0));
            lock (s_dataLock)
            {
                if (s_cacheNeedInit) InitializeCache();
            }

            string locationID = GetLocationID(context, id);

            if (_cache == null)
            {
                LogError(new OperationFailedException("The specified cache is not running. "), id);
                return;
            }

            if (_lockSessions)
            {
                _cache.Insert(locationID, GetUniqueSessionId(id), sessionItem, lockID as LockHandle, true, enableRetry);
            }
            else
                _cache.Insert(locationID, GetUniqueSessionId(id), sessionItem, enableRetry);
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

        private bool IsSessionCookieless(IAspEnvironmentContext context)
        {
            if (_isLocationAffinityEnabled)
            {
                try
                {
                    if (!string.IsNullOrEmpty(context.GetLocationCookie()))
                        return false;

                }
                catch (Exception e)
                {
                    if (NCacheLog != null) LogError(e.ToString(), null);
                }
            }
            return true;
        }

        /// <summary>
        /// Called when items in cache expires
        /// </summary>
        /// <param name="key">Expired item key</param>
        /// <param name="value">Expired item value</param>
        /// <param name="reason">Reason of expiration</param>
       

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
