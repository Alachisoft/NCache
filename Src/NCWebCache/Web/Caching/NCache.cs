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
using System.Collections;


using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Web.Caching.Util;
using Alachisoft.NCache.Web.RemoteClient;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Web.Statistics;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net;

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Provides static methods and properties to aid with clustered cache initialization and 
    /// access. This class cannot be inherited.
    /// </summary>
    /// <remarks>In a Web application more than one instances of a clustered cache can be created per application domain, 
    /// and they remain valid as long as the application domain remains active. A clustered cache makes 
    /// sharing and caching data in a cluster as simple as on a single server. In addition to a clustered cache
    /// that resides on multiple nodes, NCache also makes it possible for different applications or application
    /// domains in a single application to share and cache data seamlessly.
    /// <para>
    /// From a development perspective NCache provides a simple interface for integration with application. A
    /// call to <see cref="NCache.InitializeCache"/> requires just a registration id. The registration id is 
    /// specified at the time of desgining the cluster or cache. See the documentation for NCache for more information on 
    /// designing and implementing clusters. A scheme like this shields the development process from the complexities
    /// of cluster designs. Moreover it is possible to run the same application on different underlying cache types
    /// without actually changing a single line in the source code.
    /// </para> 
    /// </remarks>
    /// <requirements>
    /// <constraint>This member is not available in SessionState edition.</constraint> 
    /// </requirements>

    public sealed class NCache
    {
        /// <summary> Underlying implementation of NCache. </summary>

        static private Cache s_webCache = new Cache(null, "",null);

        /// <summary> Contains all initialized instances of caches. They can be accessed using their cache-ids </summary>
        static private CacheCollection s_webCaches = new CacheCollection();

        static private bool s_exceptions;
       

        /// <summary>
        /// Returns the collection of clustered caches for this application.
        /// </summary>
        /// <remarks>
        /// One instance of this class is created per application domain, and it remains 
        /// valid as long as the application domain remains active. Information about an 
        /// instance of this class is available through the Caches property of the 
        /// <see cref="NCache"/> object. 
        /// </remarks>
        /// <value>The instance of <see cref="Alachisoft.NCache.Web.Caching.CacheCollection"/> for 
        /// this Web application.</value>
        static public CacheCollection Caches
        {
            get { lock (s_webCaches) { return s_webCaches; } }
        }

        static NCache()
        {
            try
            {
                AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_DomainUnload);
            }
            catch (Exception e)
            {
            }
        }

        static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            try
            {
                lock (s_webCache)
                {
                    IEnumerator ie = s_webCaches.GetEnumerator();

                    while (ie.MoveNext())
                    {
                        Cache cache = ie.Current as Cache;
                        try
                        {
                            if (cache != null)
                                cache.Dispose();

                        }
                        catch (Exception)
                        {
                        }
                            
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Flag that indicates whether exceptions are enabled or not.
        /// </summary>
        /// <remarks>
        /// If this property is set the <see cref="Alachisoft.NCache.Web.Caching.Cache"/> object
        /// throws exceptions from public operations. If not set no exception is thrown and the
        /// operation fails silently. Setting this flag is especially helpful during 
        /// development phase of application since exceptions provide more information about
        /// the specific causes of failure. 
        /// </remarks>
        /// <value>true if exceptions are enabled, otherwise false.</value>
        /// <example> This sample shows how to set the <see cref="ExceptionsEnabled"/> property.
        /// <code>
        /// 
        /// NCache.Cache.ExceptionsEnabled = true;
        ///      
        /// </code>
        /// </example>
        public static bool ExceptionsEnabled
        {
            get
            {
                if (s_webCache != null) s_exceptions = s_webCache.ExceptionsEnabled;
                return s_exceptions;
            }
            set
            {
                s_exceptions = value;
                if (s_webCache != null) s_webCache.ExceptionsEnabled = value;
            }
        }

        /// <summary>
        /// Initializes the instance of <see cref="Alachisoft.NCache.Web.Caching.Cache"/> for this application.
        /// </summary>
        /// <param name="cacheId">The identifier for the <see cref="Alachisoft.NCache.Web.Caching.Cache"/>
        /// item to initialize.</param>
        /// <remarks>
        /// The <paramref name="cacheId"/> parameter represents the registration/config id of the cache. 
        /// Depending upon the configuration the <see cref="Alachisoft.NCache.Web.Caching.Cache"/> object is 
        /// created inproc or outproc.
        /// <para>
        /// Calling this method twice with the same <paramref name="cacheId"/> increments the reference count
        /// of the cache. The number of <see cref="InitializeCache"/> calls must be balanced by a corresponding
        /// same number of <see cref="Alachisoft.NCache.Web.Caching.Cache.Dispose"/> calls.
        /// </para>
        /// <para>
        /// Multiple cache instances can be inititalized within the same application domain. If multiple cache 
        /// instances are initialized, <see cref="NCache.Cache"/> refers to the first instance of the cache.
        /// </para>
        /// <para>
        /// <b>Note:</b> When starting a <see cref="Alachisoft.NCache.Web.Caching.Cache"/> as outproc, this method 
        /// attempts to start NCache service on the local machine if it is not already running. However it does not
        /// start the cache automatically. 
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="cacheId"/> is a null reference (Nothing in Visual Basic).</exception>
        /// <example> This sample shows how to use the <see cref="InitializeCache"/> method inside a sample Web application.
        /// <code>
        /// 
        ///	public override void Init()
        ///	{
        ///		// A cache with id 'myCache' is already registered.
        ///		try
        ///		{
        ///			Alachisoft.NCache.Web.Caching.Cache theCache = NCache.InitializeCache("myCache");
        ///		}
        ///		catch(Exception e)
        ///		{
        ///			// Cache is not available.
        ///		}
        ///	}
        ///      
        /// </code>
        /// </example>
        public static Cache InitializeCache(string cacheId)
        {
           return InitializeCache(cacheId, new CacheInitParams());
        }


        public static Cache InitializeCache(string cacheId, CacheInitParams initParams)
        {
            return InitializeCacheInternal(cacheId, initParams);
        }

        private static Cache InitializeCacheInternal(string cacheId, CacheInitParams initParams)
        {
            if (cacheId == null) throw new ArgumentNullException("cacheId");
            if (cacheId == string.Empty) throw new ArgumentException("cacheId cannot be an empty string");

            CacheMode mode = initParams.Mode;
        
            int maxTries = 2;
            try
            {
                CacheServerConfig config = null;                

                if (mode != CacheMode.OutProc)
                {
                    do
                    {
                        try
                        {
                            config = DirectoryUtil.GetCacheDom(cacheId, mode == CacheMode.InProc);
                        }
                        catch (Exception ex)
                        {
                            if (mode == CacheMode.Default)
                                mode = CacheMode.OutProc;
                            else
                                throw ex;
                        }
                        if (config != null)
                        {
                            switch (mode)
                            {
                                case CacheMode.InProc: config.InProc = true; break;
                                case CacheMode.OutProc: config.InProc = false; break;
                            }
                        }
                        break; 
                    } while (maxTries > 0);
                }

                lock (typeof(NCache))
                {

                    Cache primaryCache = null;

                    lock (s_webCaches)
                    {
                        if (!s_webCaches.Contains(cacheId))
                        {
                            CacheImplBase cacheImpl = null;

                            if (config != null && config.InProc)
                            {
                                Alachisoft.NCache.Caching.Cache ncache = null;
                                Cache cache = null;
                                maxTries = 2;
                                do
                                {
                                    try
                                    {
                                        CacheConfig cacheConfig = CacheConfig.FromDom(config);

                                        if (Web.Caching.APILogging.DebugAPIConfiguraions.LoggingEnabled)
                                            cache = new WrapperCache(new Cache(null, cacheConfig));
                                        else 
                                            cache = new Cache(null, cacheConfig);

                                        ncache = CacheFactory.CreateFromPropertyString(cacheConfig.PropertyString, config,
                                            false, false);
                                        
                                        cacheImpl = new InprocCache(ncache, cacheConfig, cache);
                                        cache.CacheImpl = cacheImpl;

                                        if (primaryCache == null)
                                            primaryCache = cache;
                                        break;
                                    }
                                    catch (Exception) { }

                                } while (maxTries > 0);
                            }
                            else
                            {
                                maxTries = 2;
                                do
                                {
                                    try
                                    {
                                        // does an AddRef() internally.

                                        PerfStatsCollector2 perfStatsCollector = new PerfStatsCollector2(cacheId, false);

                                        if (Web.Caching.APILogging.DebugAPIConfiguraions.LoggingEnabled)
                                            primaryCache = new WrapperCache(new Cache(null, cacheId, perfStatsCollector));
                                        else 

                                            primaryCache = new Cache(null, cacheId, perfStatsCollector);
                                        cacheImpl = new RemoteCache(cacheId, primaryCache, initParams, perfStatsCollector);
                                       
                                        perfStatsCollector.InitializePerfCounters(false);
                                        primaryCache.CacheImpl = cacheImpl;
                                        
                                        break;
                                    }
                                    catch (OperationNotSupportedException ex)
                                    {
                                        throw ex;
                                    }
                                } while (maxTries > 0);
                            }

                            if (primaryCache != null)
                            {
                                primaryCache.InitializeCompactFramework();
                                s_webCaches.AddCache(cacheId, primaryCache);
                            }
                        }
                        else
                        {
                            lock (s_webCaches.GetCache(cacheId))
                            {
                                primaryCache = s_webCaches.GetCache(cacheId) as Cache;
                                primaryCache.AddRef();
                            }
                        }
                    }

                    lock (s_webCache)
                    {
                        // it is first cache instance.
                        if (s_webCache.CacheImpl == null)
                        {
                            primaryCache.ExceptionsEnabled = ExceptionsEnabled;
                            s_webCache = primaryCache;
                        }
                    }
            return primaryCache;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
