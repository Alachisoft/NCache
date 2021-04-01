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

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.Statistics;
using Alachisoft.NCache.Runtime.CacheManagement;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.ErrorHandling;
using System;
using System.Collections.Generic;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Provides and manages the instance of <see cref="ICache"/>
    /// </summary>
    public sealed class CacheManager
    {
        #region Private Fields

        /// <summary> Underlying implementation of NCache. </summary>
        static private Cache _cache = new Cache();
        static private bool s_exceptions = true;

        /// <summary> Contains all initialized instances of caches. They can be accessed using their cache-ids </summary>
        static private CacheCollection _cacheCollection = new CacheCollection(StringComparer.InvariantCultureIgnoreCase);

        #endregion

        #region Public Properties 

        /// <summary>
        /// Returns <see cref="CacheCollection"/> of the caches initialized within the same application domain. 
        /// </summary>
        static public CacheCollection Caches
        {
            get { lock (_cacheCollection) { return _cacheCollection; } }
        }

        internal static bool ExceptionsEnabled
        {
            get
            {
                if (_cache != null) s_exceptions = _cache.ExceptionsEnabled;
                return s_exceptions;
            }
            set
            {
                s_exceptions = value;
                if (_cache != null) _cache.ExceptionsEnabled = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns an instance of <see cref="ICache"/> for this application.
        /// </summary>
        /// <param name="cacheName">The identifier for the <see cref="ICache"/>.</param>
        /// <param name="cacheConnectionOptions"><see cref="CacheConnectionOptions"/> parameters for <see cref="ICache"/> connection.</param>
        /// <param name="clientCacheName">The identifier for the ClientCache.</param>
        /// <param name="clientCacheConnectionOptions"><see cref="CacheConnectionOptions"/> parameters for ClientCache connection.</param>
        /// <returns>Instance of <see cref="ICache"/>.</returns>
        /// <remarks>
        /// The <paramref name="clientCacheName"/> parameter represents the registration/config name of the Client Cache (L1 Cache). 
        /// Depending upon the configuration the <see cref="ICache"/> object is 
        /// created inproc or outproc.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="cacheName"/> is a null reference.</exception>
        /// <code>
        /// CacheConnectionOptions cacheConnectionOptions = new CacheConnectionOptions();
        /// 
        /// cacheConnectionOptions.LoadBalance = true;
        /// cacheConnectionOptions.ConnectionRetries = 5;
        /// cacheConnectionOptions.Mode = IsolationLevel.OutProc;
        /// cacheConnectionOptions.ClientRequestTimeOut = TimeSpan.FromSeconds(30);
        /// cacheConnectionOptions.UserCredentials = new Credentials("domain\\user-id", "password");
        /// cacheConnectionOptions.RetryInterval = TimeSpan.FromSeconds(5);
        /// cacheConnectionOptions.ServerList = new List&lt;ServerInfo&gt;();
        /// {
        /// 	new ServerInfo("remoteServer",9800)
        /// };
        /// 
        /// CacheConnectionOptions clientConnectionOptions = new CacheConnectionOptions();
        /// clientConnectionOptions.Mode = IsolationLevel.InProc;
        /// 
        /// ICache cache = CacheManager.GetCache("myCache", cacheConnectionOptions,"clientCache",clientConnectionOptions);
        /// </code>

        public static ICache GetCache(string cacheName, CacheConnectionOptions cacheConnectionOptions = null)
        {
            if (string.IsNullOrWhiteSpace(cacheName)) throw new ArgumentNullException("cacheName");

            if (cacheConnectionOptions == null) cacheConnectionOptions = new CacheConnectionOptions();

            cacheConnectionOptions.Initialize(cacheName);

            Cache cache = GetCacheInternal(cacheName, cacheConnectionOptions);

            cache.SetMessagingServiceCacheImpl(cache.CacheImpl);

            return cache;


        }
        #endregion


        #region Private Methods 

       private static Cache GetCacheInternal(string cacheName, CacheConnectionOptions cacheConnectionOptions)

        {
            if (cacheName == null) throw new ArgumentNullException("cacheId");
            if (cacheName == string.Empty) throw new ArgumentException("cacheId cannot be an empty string");

           IsolationLevel mode = cacheConnectionOptions.Mode.Value;

            
           

            
            string cacheIdWithAlias = cacheName;

            int maxTries = 2;
           

            try
            {
                CacheServerConfig config = null;

                if (mode != IsolationLevel.OutProc)
                {
                    do
                    {
                        try
                        {
                            config = DirectoryUtil.GetCacheDom(cacheName, 
                                null, 
                                null, 
                                mode == IsolationLevel.InProc);
                        }
                        

                        catch (Exception ex)
                        {
                            if (mode == IsolationLevel.Default)
                                mode = IsolationLevel.OutProc;
                            else
                                throw ex;
                        }
                        if (config != null)
                        {
                            if (config.CacheType.ToLower().Equals("clustered-cache"))
                            {
                                throw new OperationFailedException(ErrorCodes.CacheInit.CLUSTER_INIT_IN_INPROC,ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CLUSTER_INIT_IN_INPROC));
                            }
                            switch (mode)
                            {
                                case IsolationLevel.InProc: config.InProc = true; break;
                                case IsolationLevel.OutProc: config.InProc = false; break;
                            }
                        }
                        break; 
                    } while (maxTries > 0);
                }

                lock (typeof(CacheManager))
                {
                    Cache primaryCache = null;

                    lock (_cacheCollection)
                    {
                        if (!_cacheCollection.Contains(cacheIdWithAlias))
                        {
                            CacheImplBase cacheImpl = null;

                            if (config != null && config.InProc)
                            {
                                NCache.Caching.Cache ncache = null;
                                Cache cache = null;
                                maxTries = 2;
                              
                                do
                                {
                                    try
                                    {
                                        CacheConfig cacheConfig = CacheConfig.FromDom(config);

                                        cache = new Cache(null, cacheConfig);

                                        
                                        ncache = CacheFactory.CreateFromPropertyString(cacheConfig.PropertyString, config, null, null, false, false);
                                        

                                        cacheImpl = new InprocCache(ncache, cacheConfig, cache, null,null);

                                        cache.CacheImpl = cacheImpl;

                                        if (primaryCache == null)
                                        {
                                            primaryCache = cache;
                                        }
                                        else
                                            primaryCache.AddSecondaryInprocInstance(cache);

                                        break;
                                    }
                                    catch (SecurityException se)
                                    {
                                        maxTries--;

                                        if (maxTries == 0)
                                            throw se;

                                       
                                    }
                                } while (maxTries > 0);
                            }
                            else
                            {
                                maxTries = 2;
                                do
                                {
                                    try
                                    {

                                        StatisticsCounter perfStatsCollector;
                                        if (ServiceConfiguration.PublishCountersToCacheHost)
                                        {
                                            perfStatsCollector = new CustomStatsCollector(cacheName, false);
                                            ClientConfiguration clientConfig = new ClientConfiguration(cacheName);
                                            clientConfig.LoadConfiguration();
                                            perfStatsCollector.StartPublishingCounters(clientConfig.BindIP);

                                        }
                                        else
                                        {
                                            perfStatsCollector = new PerfStatsCollector(cacheName, false);
                                        }

                                      
                                        primaryCache = new Cache(null, cacheName, perfStatsCollector);

                                        cacheImpl = new RemoteCache(cacheName, primaryCache, cacheConnectionOptions, perfStatsCollector);
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
                                _cacheCollection.AddCache(cacheIdWithAlias, primaryCache);
                                primaryCache.InitializeEncryption();
                            }
                        }
                        else
                        {
                            lock (_cacheCollection.GetCache(cacheIdWithAlias, false))
                            {
                                primaryCache = _cacheCollection.GetCache(cacheIdWithAlias, false) as Cache;
                              
                                primaryCache.AddRef();

                            }
                        }
                    }

                    lock (_cache)
                    {
                        // it is first cache instance.
                        if (_cache.CacheImpl == null)
                        {
                             primaryCache.ExceptionsEnabled = ExceptionsEnabled;
                            _cache = primaryCache;
                            
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

     
        #endregion
    }
}
