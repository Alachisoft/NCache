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
#if !NETCORE
using System;
using System.Collections;
using System.Web;
using System.Web.SessionState;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Common;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Web.SessionStateManagement;
using Alachisoft.NCache.Web.SessionState.DistributionStrategy;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;


namespace Alachisoft.NCache.Web.SessionState
{
    /// <summary>
    /// </summary>
    public class NSessionStateModule : IHttpModule
    {
        private const string APPLOCKKEY = "HMAppLocked";
        private const string APPID = "HMAppId";
        private const string SOURSE = "NHttpModule";

        private HttpSessionState _httpSession;
        private IDistributionStrategy _transformer = null;

        private bool _clearASPSession = true;
        private bool _isAlreadyLoaded = false;

        private static object _syncObj = new object();

        private string _applicationId = null;
        private string _cacheId = null;
        private bool _exceptionsEnabled = true;
        private bool _logs = false;
        private bool _detailedLogs = false;
        private bool _isAbandoned = false;
        private bool _writeExceptionsToEventLog = false;
        private ISessionCache _cache = null;//private Cache _cache = null;

        private EventHandler _onAcqStateHandler;
        private EventHandler _onReleaseStateHandler;
        private EventHandler _onDisposeHandler;
        private EventHandler _onEnd;

        private bool _acquireCalled = false;
        private bool _releaseCalled = false;

        private bool _isLocationAffinityEnabled = false;

        private static ILogger _ncacheLog;

        public static ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }


        /// <summary>
        /// Get application id specified in web.config
        /// </summary>
        internal string ApplicationId
        {
            get { return this._applicationId; }
        }

        /// <summary>
        /// Get cache name
        /// </summary>
        internal string CacheID
        {
            get { return this._cacheId; }
        }

        internal bool DetailedLogsEnabled
        {
            get { return this._detailedLogs; }
        }

        private ISessionCache GetCache(HttpContext context)
        {
            if (context != null)
            {
                if (this._cache == null) InitializeCache(context.ApplicationInstance);
            }
            return this._cache;
        }
  
        /// <summary>
        /// Initializes a module and prepares it to handle requests.
        /// </summary>
        /// <param name="application">An <see cref="System.Web.HttpApplication"/> object that provides 
        /// references to the intrinsic server objects (for example, 
        /// Request, Response, Session, and Server) used to service HTTP requests.
        /// </param>
        /// <remarks>
        /// </remarks>
        void IHttpModule.Init(HttpApplication application)
        {
            if (application == null) throw new ArgumentNullException("application");
            HttpContext context = application.Context;

            this._onAcqStateHandler = new EventHandler(OnAcquireRequestState);
            this._onReleaseStateHandler = new EventHandler(OnReleaseRequestState);
            this._onDisposeHandler = new EventHandler(this.OnDisposed);
            this._onEnd = new EventHandler(this.OnEnd);

            application.AcquireRequestState += this._onAcqStateHandler;
            application.ReleaseRequestState += this._onReleaseStateHandler;
            application.Disposed += this._onDisposeHandler;
            application.EndRequest += this._onEnd;

            if (NCacheSessionStateConfigReader.LoadSessionLocationSettings() != null)
                this._isLocationAffinityEnabled = true;

            GetCache(context);
            application.Application["NSessionStateModule"] = this;


        }


        private void InitializeCache(HttpApplication application)
        {
            this._cacheId = ConfigurationSettings.AppSettings["cacheName"];
            if (this._cacheId == null || this._cacheId == string.Empty)
            {
                throw new ConfigurationException("The 'cacheName' attribute cannot be null or empty string");
            }

            string[] boolValStrings = {"exceptionsEnabled", "writeExceptionsToEventLog",
                                       "enableLogs", "enableDetailLogs", "clearASPSession"};
            string configVal = null;
            bool value = false;

            for (int i = 0; i < boolValStrings.Length; i++)
            {
                configVal = ConfigurationSettings.AppSettings[boolValStrings[i]];
                if (configVal != null)
                {
                    if (configVal != "true" && configVal != "false")
                    {
                        throw new ConfigurationException("The '" + boolValStrings[i] + "' attribute must be one of the following values: true, false.");
                    }
                    else
                    {
                        value = Convert.ToBoolean(configVal);
                        switch (i)
                        {
                            case 0: this._exceptionsEnabled = value; break;
                            case 1: this._writeExceptionsToEventLog = value; break;
                            case 2: this._logs = value; break;
                            case 3: this._detailedLogs = value; break;
                            case 4: this._clearASPSession = value; break;
                        }
                    }
                }
            }
            if (this._logs || this._detailedLogs)
            {
                if (_ncacheLog == null)
                {
                    _ncacheLog = new NCacheLogger();
                    _ncacheLog.Initialize(LoggerNames.HttpModule, this._cacheId);

                    if (this._detailedLogs)
                    {
                        NCacheLog.SetLevel("all");
                    }
                    else
                    {
                        if (this._logs)
                            NCacheLog.SetLevel("info");
                    }
                }
            }

            _applicationId = ConfigurationSettings.AppSettings["sessionAppId"];
            _cache = new SingleRegionCache();

            try
            {
                this._cache.InitializeCache(this._cacheId);
                if(NCacheLog != null) NCacheLog.Info( "NSessionStateModule initialized");
                application.Application["NSessionStateModule.Cache"] = this._cache;

            }
            catch (Exception e)
            {
                RaiseExceptions(e);
            }
        }

        [Conditional("DEBUG")]
        private static void Trace(string message)
        {
            HttpContext.Current.Response.Write(message);
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module that implements 
        /// <see cref="System.Web.IHttpModule"/>.
        /// </summary>
        /// <remarks>
        /// Dispose performs any final cleanup work prior to removal of the module from 
        /// the execution pipeline.
        /// </remarks>
        void IHttpModule.Dispose()
        {
            if(NCacheLog != null) NCacheLog.Info( "NSessionStateModule disposed");
        }

        /// <summary>
        /// Fills the system ASP.NET session from NCache.
        /// </summary>
        /// <param name="context">The current http request context</param>
        private void FillAspNetSessionFromNCache(HttpContext context)
        {
            this._isAbandoned = false;
            ISessionCache cache = GetCache(context);
            if (cache == null) return;
            if (this._isAlreadyLoaded)
            {
                this._isAlreadyLoaded = false;
                return;
            }

            try
            {
                this._transformer.FillSessionFromCache(cache, this._httpSession, this, this._clearASPSession);
                //Trace("Session: " + _httpSession.Count + "<br>");
            }
            catch (Exception e)
            {
                RaiseExceptions(e, "NSessionStateModule.FillAspNetSessionFromNCache");
            }
        }

        /// <summary>
        /// Fills NCache from the system ASP.NET session.
        /// </summary>
        /// <param name="context">The current http request context</param>
        private void FillNCacheFromAspNetSession(HttpContext context)
        {
            ISessionCache cache = GetCache(context);
            // No need to fill Cache if _httpSession is null. ARIF
            if (cache == null || _httpSession == null) return;
            try
            {
                this._transformer.FillCacheFromSession(cache, this._httpSession, this, this._clearASPSession, this._isAbandoned);
            }
            catch (Exception e)
            {
                RaiseExceptions(e, "NSessionStateModule.FillNCacheFromAspNetSession");
            }
            finally
            {
                this._isAbandoned = false;
            }
        }

#region	/                 --- Public Methods ---           /

        /// <summary>
        /// Cancels the current session
        /// </summary>
        /// <param name="session">User session</param>
        public void Abandon(HttpSessionState session)
        {
            if (session == null) return;
            session.Abandon();
            this._isAbandoned = true;
        }

        /// <summary>
        /// Loads the session information from Cache
        /// to the regular ASP.NET session object
        /// </summary>
        /// <param name="context">The context of current http request</param>
        public void Load(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException("context cannot be null.");
            if (context.Session == null) throw new HttpException("session state is not available in this context.");

            try
            {
                ISessionCache cache = this.GetCache(context);
                IDistributionStrategy transformer = new MonolithicDistribution();
                transformer.FillSessionFromCache(cache, context.Session, this, false);
                this._isAlreadyLoaded = true;
            }
            catch (Exception exc)
            {
                RaiseExceptions(exc);
            }
        }

        /// <summary>
        /// Update Cache from the reqular ASP.NET session
        /// </summary>
        /// <param name="context">The context of current http request</param>
        public void Update(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException("context cannot be null.");
            if (context.Session == null) throw new HttpException("session state is not available in this context.");

            try
            {
                ISessionCache cache = this.GetCache(context);
                IDistributionStrategy transformer = new MonolithicDistribution();
                transformer.FillCacheFromSession(cache, context.Session, this, false, false);
            }
            catch (Exception exc)
            {
                RaiseExceptions(exc);
            }
        }

#endregion

#region	/                 --- HttpApplication Event Handlers ---           /

        /// <summary>
        /// Occurs when ASP.NET acquires the current state (for example, session state) associated 
        /// with the current request.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ea"></param>
        private void OnAcquireRequestState(object source, EventArgs ea)
        {
            _transformer = new MonolithicDistribution();
            this._acquireCalled = true;
            this._releaseCalled = false;
            HttpApplication application = (HttpApplication)source;
            HttpContext context = application.Context;
            _httpSession = context.Session;

            if (_httpSession != null)
            {
                FillAspNetSessionFromNCache(context);
            }
            else
            {
                if(NCacheLog != null) NCacheLog.Error("NSessionStateModule.OnAcquireRequestState", "Session is null");
            }
        }

        /// <summary>
        /// Occurs after ASP.NET finishes executing all request handlers. This event causes state 
        /// modules to save the current state data.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ea"></param>
        private void OnReleaseRequestState(object source, EventArgs ea)
        {
            this._releaseCalled = true;
            FillNCacheFromAspNetSession(HttpContext.Current);
            _transformer = null;
        }

        /// <summary>
        /// Occours 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ea"></param>
        private void OnEnd(object source, EventArgs ea)
        {
            try
            {
                if (this._acquireCalled)
                {
                    if (!this._releaseCalled)
                    {
                        OnReleaseRequestState(source, ea);
                    }
                }
            }
            finally
            {
                this._acquireCalled = false;
                this._releaseCalled = false;
            }
        }

        /// <summary>
        /// Resposible of disposing cache when application domain disposes.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ea"></param>
        private void OnDisposed(object source, EventArgs ea)
        {
            try
            {
                if (this._cache != null)
                {
                    if(NCacheLog != null) NCacheLog.Error("NSessionStateModule.Disposed", "");
                    this._cache.Dispose();
                    this._cache = null;
                }
            }
            catch (Exception e)
            {
                RaiseExceptions(e, "NSessionStateModule.Disposed");
            }
        }

#endregion

        internal void RaiseExceptions(Exception exc)
        {
            RaiseExceptions(exc, "");
        }

        internal void RaiseExceptions(Exception exc, string sessionID)
        {
            if(NCacheLog != null) NCacheLog.Error(sessionID + " :" + exc.ToString());
         
            if (this._writeExceptionsToEventLog)
            {
                try
                {
                    AppUtil.LogEvent(SOURSE, exc.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                }
                catch (Exception)
                {
                    throw exc;
                }
            }
            if (this._exceptionsEnabled)
            {
                throw exc;
            }
        }

    }    
}
#endif