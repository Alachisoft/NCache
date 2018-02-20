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
using System.Collections.Specialized;
using System.Web;
using System.Web.Compilation;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Web.NOutputCache
{
    internal class NOutputCache : IHttpModule
    {
        private static readonly string _filterKey = "filterContextItem";

        private CacheContainer _cache;
        private static OutputCacheSettings _settings;
        private static OutPutCacheConfigReader _reader;
        private static object s_mutex = new object();

        private EventHandler _onResolveRequestCache;        
        private EventHandler _onUpdaterequestCache;
        private EventHandler _onDispose;

        private static ILogger _ncacheLog = null;

        private bool _init = false;

        private static ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

        internal CacheContainer Cache
        {
            get
            {
                return _cache;
            }
        }

        internal static OutPutCacheConfigReader Reader
        {
            get
            {
                return _reader;
            }
        }

        #region IHttpModule Members


        /// <summary>
        /// Initializes a module and prepares it to handle requests.
        /// </summary>
        /// <param name="application">An <see cref="System.Web.HttpApplication"/> object that provides 
        /// references to the intrinsic server objects (for example, 
        /// Request, Response, Session, and Server) used to service HTTP requests.
        /// </param>
        void IHttpModule.Init(HttpApplication application)
        {
            if (application == null)
            {
                throw new ArgumentNullException("application");
            }

            this._onResolveRequestCache = new EventHandler(ResolveRequestCache);
            this._onUpdaterequestCache = new EventHandler(UpdateRequestCache);
            this._onDispose = new EventHandler(Dispose);

            application.ResolveRequestCache += this._onResolveRequestCache;            
            application.UpdateRequestCache += this._onUpdaterequestCache;
            application.Disposed += this._onDispose;

            if (_reader == null)
            {
                lock (s_mutex)
                {
                    if (_reader == null)
                    {
                        NItem.RegisterTypeWithCompactFramework();
                        _reader = new OutPutCacheConfigReader();
                        _settings = _reader.LoadCacheSettings();
                        _reader.LoadPageSettings();
                    }
                }
            }

            try
            {                
                if (IsNullOrEmpty(_settings.CacheName))
                {
                    return;
                }

                if (_settings.EnableLogs || _settings.EnableDetailedLogs)
                {
                    _ncacheLog = new NCacheLogger();
                    _ncacheLog.Initialize(LoggerNames.OutputCache, _settings.CacheName);

                    if (_settings.EnableDetailedLogs)
                    {
                        NCacheLog.SetLevel("all");
                    }
                    else
                    {
                        if(_settings.EnableLogs)
                            NCacheLog.SetLevel("info");
                    }

                }

                lock (s_mutex)
                {

                    this._cache = CacheContainer.GetCacheInstance(_settings);

                }

                if(NCacheLog != null) NCacheLog.Info("NOutputCache initialized");
            }
            catch (Exception e)
            {
                RaiseException(e);           
            }
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
        }        

        #endregion        

        /// <summary>
        /// Occurs when ASP.NET completes an authorization event to let the caching modules
        /// serve requests from the cache, bypassing execution of the event handler (for
        /// example, a page or an XML Web service).
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ea"></param>
        private void ResolveRequestCache(object o, EventArgs args)
        {
            HttpApplication application = (HttpApplication)o;
            HttpContext context = application.Context;
            string key = context.Request.FilePath.ToLower();
            PageSettings pageSettings = _reader.GetPageSettings(key);

            if (pageSettings == null) return;
            if (this._cache == null) return;
            if (!pageSettings.CachingEnabled) return;
            if (!pageSettings.Get && context.Request.HttpMethod == "GET") return;
            else if (!pageSettings.Post && context.Request.HttpMethod == "POST") return;

            try
            {
                string varyingKey = key + this.CreateVaryingKey(pageSettings, context);
                NItem item = this._cache.Get(varyingKey, pageSettings) as NItem;

                if (item == null)
                {
                    NFilter nFilter = new NFilter(context.Response.Filter);
                    context.Response.Filter = nFilter;

                    context.Items[_filterKey] = nFilter;
                    return;
                }                   

                context.Response.ClearContent();
                context.Response.BinaryWrite(item.Buffer);

                application.CompleteRequest();
            }
            catch (Exception e)
            {
                RaiseException(e);                
            }
        }

        /// <summary>
        /// Occurs when ASP.NET finishes executing an event handler in order to let caching
        /// modules store responses that will be used to serve subsequent requests from
        /// the cache.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void UpdateRequestCache(object o, EventArgs args)
        {
            HttpApplication application = (HttpApplication)o;
            HttpContext context = application.Context;            
            string key = context.Request.FilePath.ToLower();
            PageSettings pageSettings = _reader.GetPageSettings(key);

            if (pageSettings == null) return;
            if (this._cache == null) return;
            if (!pageSettings.CachingEnabled) return;
            if (!pageSettings.Get && context.Request.HttpMethod == "GET") return;
            else if (!pageSettings.Post && context.Request.HttpMethod == "POST") return;
            
            try
            {                
                if (context.Response.StatusCode == 200)
                {
                    NFilter nFilter = context.Items[_filterKey] as NFilter;
                    if (nFilter != null)
                    {
                        context.Items.Remove(_filterKey);

                        NItem item = new NItem(PageHash(key), nFilter.ToArray());
                        this._cache.Insert(key + TagUtil.CreateTaggedCacheItem(this.CreateVaryingKey(pageSettings, context)), item, null, DateTime.Now.AddSeconds(pageSettings.ExpirationTime), Alachisoft.NCache.Web.Caching.Cache.NoSlidingExpiration);
                    }
                }
            }
            catch (Exception e)
            {
                RaiseException(e);               
            }
        }

        /// <summary>
        /// Get flag indicating if excptions are enabled in config
        /// </summary>
        private bool ExceptionsEnabled
        {
            get { return (_settings != null && _settings.ExceptionsEnabled); }
        }

        /// <summary>
        /// Occurs when ASP.NET application is disposed
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void Dispose(object o, EventArgs args)
        {
            try
            {
                if (CacheContainer.DisposeCacheInstance())
                {                   
                    if (NCacheLog != null) NCacheLog.Info("NOutputCache disposed");
                }
            }
            catch (Exception e)
            {
                RaiseException(e);
            }
        }

        /// <summary>
        /// Create key suffix depending on querystring and varybyparms set in web.config
        /// </summary>
        /// <param name="pageSettings">config page settings</param>
        /// <param name="queryString">querystring</param>
        /// <returns>key suffix</returns>
        private string CreateVaryingKey(PageSettings pageSettings, HttpContext context)
        {
            string keySuffix = string.Empty;
            NameValueCollection collection = null;

            HttpRequest request = context.Request;

            if (pageSettings.VaryByAllParams || pageSettings.VaryByParam.Length > 0)
            {
                if (request.HttpMethod == "POST")
                {
                    keySuffix += "P";
                    collection = request.Form;
                }
                else
                {
                    keySuffix += "O";
                    collection = request.QueryString;
                }

                if (collection != null && collection.Count > 0)
                {
                    if (pageSettings.VaryByAllParams)
                    {
                        keySuffix += this.AppendVaryByAll(collection);
                    }
                    else
                    {
                        keySuffix += this.AppendVaryBy(collection, pageSettings.VaryByParam);
                    }
                }
            }

            if (pageSettings.VaryByAllHeaders || pageSettings.VaryByHeader.Length > 0)
            {
                collection = request.Headers;
                if (collection != null && collection.Count > 0)
                {
                    keySuffix += "H";
                    if (pageSettings.VaryByAllHeaders)
                    {
                        keySuffix += this.AppendVaryByAll(collection);
                    }
                    else
                    {
                        keySuffix += this.AppendVaryBy(collection, pageSettings.VaryByHeader);
                    }
                }

            }

            if (!IsNullOrEmpty(pageSettings.VaryByCustom))
            {
                string custom = context.ApplicationInstance.GetVaryByCustomString(context, pageSettings.VaryByCustom);
                keySuffix += "C" + pageSettings.VaryByCustom + "=" + custom;
            }

            return keySuffix;
        }

        /// <summary>
        /// Calculate the hash code of the page. This is used to detect recompilation of page
        /// </summary>
        /// <param name="key">page path</param>
        /// <returns>hash code</returns>
        private long PageHash(string key)
        {
            long hashCode = 0;
			Type type = null;          

            try
            {
                type = BuildManager.GetCompiledType(key);
                if (type != null)
                {
                    string typeName = type.AssemblyQualifiedName;
                    string typeFileName = type.Module.FullyQualifiedName;
                    hashCode = ((long)typeName.GetHashCode() & 0xffffffff) + (((long)typeFileName.GetHashCode() & 0xffffffff));
                }
            }
            catch { }
            return hashCode;
        }

        /// <summary>
        /// Append all params in a string
        /// </summary>
        /// <param name="collection">query string</param>
        /// <returns>formatted all params</returns>
        private string AppendVaryByAll(NameValueCollection collection)
        {
            string keySuffix = string.Empty;
            string[] keys = collection.AllKeys;
            for (int i = 0; i < keys.Length; i++)
            {
                keySuffix += keys[i] + "=" + collection[keys[i]] + "&";
            }
            return keySuffix;
        }

        /// <summary>
        /// Append selective params in a string
        /// </summary>
        /// <param name="collection">query string</param>
        /// <param name="varyBy">params on which selection is based</param>
        /// <returns>formatted selective params</returns>
        private string AppendVaryBy(NameValueCollection collection, string[] varyBy)
        {
            string keySuffix = string.Empty;
            for (int i = 0; i < varyBy.Length; i++)
            {
                if (collection[varyBy[i]] != null)
                {
                    keySuffix += varyBy[i] + "=" + collection[varyBy[i]] + "&";
                }
            }
            return keySuffix;
        }

		/// <summary>
		/// Checks if the string is null or empty
		/// </summary>
		/// <param name="s">string</param>
		/// <returns>true if string is null or empty, false otherwise</returns>
		internal static bool IsNullOrEmpty(string s)
		{
			return (s == null || s == string.Empty);
		}

        internal void RaiseException(Exception exc)
        {
            if (NCacheLog != null) NCacheLog.Error(exc.ToString());
            if (this.ExceptionsEnabled) throw exc;
        }
    }
}
