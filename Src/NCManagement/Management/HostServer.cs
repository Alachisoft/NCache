// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.Caching;
using System.Threading;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common;
using System.Diagnostics;
using Alachisoft.NCache.Common.RPCFramework;
using System.Collections;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Management.ClientConfiguration;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Config;
using InstrumentCache = Alachisoft.NCache.Caching.Cache;


namespace Alachisoft.NCache.Management
{
    public delegate void OnCacheStopped();

    public class HostServer: CacheServer
    {
         private static HostServer s_instance;
         OnCacheStopped _callback;

        string _cacheserver="NCache";
        
        /// <summary> Returns the application name of this session. </summary>
        static  internal string ObjectUri
        {
            get
            {
                return typeof(CacheServer).Name;
            }
        }

        public override string ClusterIP
        {
            get { return _clusterIp; }
            set { _clusterIp = value; }
        }
        public  static string ClientserverIp
        {
            get { return CacheServer.ClientserverIp; }
            set { CacheServer.ClientserverIp = value; }
        }

        public string LocalCacheIP
        {
            get { return _localCacheIp; }
            set { _localCacheIp = value; }
        }

        /// <summary>Socket server port</summary>
        private static int _socketServerPort;

        private static string _clusterIp;
        private static string _localCacheIp;

        private static System.Timers.Timer _evalWarningTask;
        private TimeScheduler _gcScheduler;

        private static CacheInfo cacheInfo = null;
        private OnCacheStopped cacheStoppedCallback;
        private Thread _processCheck;
        private volatile bool _containProcessFlag;
        
        /// <summary>
        /// Gets the singlton instance of CacheServer.
        /// </summary>
        public static HostServer Instance
        {
            get { return s_instance; }
            set { s_instance = value; }
        }

        public bool ContainCacheProcess
        {
            get { return _containProcessFlag; }
            set { _containProcessFlag = value; }
        }
        public HostServer()
        {

        }

        public string GetHostingCacheName()
        {
            if (cacheInfo != null && cacheInfo.Cache != null)
            {
                return cacheInfo.Cache.Name;
            }
            return null;
        }



        private void StopCache(CacheServer.CacheStopReason cacheStopReason)
        {
            
            if (cacheInfo != null)
            {
                LeasedCache cache = cacheInfo.Cache;
                if (cache != null)
                    StopCacheInstance(cache.Name, cacheInfo, cacheStopReason);
            }
        }

        /// <summary>
        /// Finalizer for this object.
        /// </summary>
        ~HostServer()
        {
            Dispose();
        }

        
        /// <summary>
        /// Add garbage collection task to time sheduler
        /// </summary>

        private void StartGCTask()
        {
            bool enabled = ServiceConfiguration.EnableForcedGC;

            ///Only if CLR is loaded with Server GC, and user asked for forced GC
            if (enabled && System.Runtime.GCSettings.IsServerGC) //ALAWAYS RETURNS FALSE IN CASE OF MONO
            {
                int threshold = 0;

                if (ServiceConfiguration.GCThreshold != null)
                    threshold = ServiceConfiguration.ForcedGCThreshold;

                this._gcScheduler.AddTask(new GarbageCollectionTask(threshold));

            }
        }
              
        [TargetMethod(ManagementUtil.MethodName.GetCacheInfo, 1)]
        public override CacheInfo GetCacheInfo(string cacheId)
        {
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (cacheInfo != null)
                    return cacheInfo;
                return null;
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
        }
        
        /// <summary>
        /// Gets or sets the socket server port.
        /// </summary>
        public  static int SocketServerPort
        {
            get { return _socketServerPort; }
            set { _socketServerPort = value; }
        }

        [TargetMethod(ManagementUtil.MethodName.GetClientNodeStatus, 1)]
        public override ClientNodeStatusWrapper GetClientNodeStatus(string cacheId)
        {
            string clientCacheId = string.Empty;

            ClientNodeStatus status = ClientConfigManager.GetClientNodeStatus(cacheId.ToLower(), out clientCacheId);

            if (status == ClientNodeStatus.ClientCacheUnavailable)
            {
                StatusInfo statusInfo = GetCacheStatus(clientCacheId.ToLower(), null);
                if (statusInfo.IsRunning)
                {
                    status = ClientNodeStatus.ClientCacheDisabled;
                }
            }
            else if (status == ClientNodeStatus.ClientCacheDisabled)
            {
                StatusInfo statusInfo = GetCacheStatus(clientCacheId.ToLower(), null);

                if (statusInfo.IsRunning)
                    status = ClientNodeStatus.ClientCacheEnabled;
                else if (statusInfo.IsUnavailable)
                    status = ClientNodeStatus.ClientCacheUnavailable;
            }
            return new ClientNodeStatusWrapper(status);
        }
        
        [TargetMethod(ManagementUtil.MethodName.StartCache, 1)]
        public override void StartCache(string cacheId)
        {
            StartCache(cacheId, null);
        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 2)]
        public override void StartCache(string cacheId, string partitionId)
        {
            StartCache(cacheId, partitionId, false);
        }
        
        /// <summary>
        /// Start a cache and provide call backs
        /// </summary>
        /// <param name="cahcheID"></param>
        /// <param name="propertyString"></param>
        /// <param name="itemAdded"></param>
        /// <param name="itemRemoved"></param>
        /// <param name="itemUpdated"></param>
        /// <param name="cacheCleared"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        /// 
        [TargetMethod(ManagementUtil.MethodName.StartCache, 3)]
        public override void StartCache(string cacheId, string partitionId,
            bool twoPhaseInitialization)
        {
            if (cacheId == null) throw new ArgumentNullException("cacheId");
            ContainCacheProcess = true;
            LeasedCache cache = null;

            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try ///For a finally {...} 
            {
                try
                {
                    LoadConfiguration(cacheId);
                }
                catch (Exception e)
                {
                    string msg = String.Format("CacheServer failed to load configuration, Error {0}", e.Message);
                    AppUtil.LogEvent(msg, EventLogEntryType.Warning);
                    ContainCacheProcess = false;
                }

                cacheInfo = GetCacheInfo(cacheId.ToLower());

                if (cacheInfo != null)
                    cache = cacheInfo.Cache;
                try
                {
                    StartCacheInstance(cache, cacheInfo, twoPhaseInitialization);
                    AppUtil.LogEvent(_cacheserver, "\"" + cacheId + "\"" + " started successfully.", EventLogEntryType.Information, EventCategories.Information, EventID.CacheStart);
                }
                catch (Exception e)
                {
                    AppUtil.LogEvent(_cacheserver, "\"" + cacheId + "\" can not be started.\n" + e.ToString(), System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.CacheStartError);
                    ContainCacheProcess = false;
                    throw;
                }
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }

        private void StartCacheInstance(LeasedCache cache, CacheInfo cacheInfo, bool twoPhaseInitialization)
        {
            if (cache != null)
            {
                if (!cache.IsRunning)
                {
                    cacheInfo.SyncConfiguration();
                    cache.StartInstance(Renderer, twoPhaseInitialization);
                    if (InstrumentCache.OnCacheStarted != null)
                        InstrumentCache.OnCacheStarted(cache.Name);
                }
            }
            else
                throw new Runtime.Exceptions.ManagementException("Specified cacheId is not registered");
        }

        [TargetMethod(ManagementUtil.MethodName.StopCache, 1)]
        public override void StopCache(string cacheId)
        {
            StopCache(cacheId, null);
        }


        /// <summary>
        /// Stop a cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        [TargetMethod(ManagementUtil.MethodName.StopCache, 2)]
        public override void StopCache(string cacheId, string partitionId)
        {
            if (cacheId == null) throw new ArgumentNullException("cacheId");

            LeasedCache cache = null;

            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try ///For a finally {...}
            {
                if (cacheInfo != null)
                {
                    cache = cacheInfo.Cache;
                }
                if (cache != null)
                {
                    try
                    {
                        StopCacheInstance(cache.Name, cacheInfo, CacheServer.CacheStopReason.Stoped);
                    }
                    catch (Exception e)
                    {
                        throw;
                    }
                }
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }


        public override void StopCacheInstance(string cacheName, CacheInfo cacheInfo, CacheServer.CacheStopReason reason)
        {
            LeasedCache cache = cacheInfo.Cache;
            if (cache != null && cache.Name == cacheName)
            {
                if (cache.IsRunning)
                {
                    cache.Stop();
                    if(cacheInfo != null)
                        cacheInfo.SyncConfiguration();
                    InstrumentCache.OnCacheStopped(cache.Name);
                }
            }
            else

                throw new Runtime.Exceptions.ManagementException("Specified cacheId is not registered");
        }

        public void onCacheStopped()
        {
            cacheStoppedCallback();
        }

        public void RegisterCacheStopCallback(OnCacheStopped callback)
        {
            if (cacheStoppedCallback == null)
                cacheStoppedCallback = callback;
        }


        /// <summary>
        /// Load all the config sections from the configuration file.
        /// </summary>
        static private void LoadConfiguration(string cacheName)
        {
            
            try
            {
                CacheServerConfig[] configs = CacheConfigManager.GetConfiguredCaches();
                //Just need to call the static block - stupid logik i know
                Alachisoft.NCache.Util.MiscUtil.RegisterCompactTypes();
                if (configs != null)
                {
                    for (int i = 0; i < configs.Length; i++)
                    {
                        CacheServerConfig config = configs[i];
                        if (config != null && !String.IsNullOrEmpty(cacheName) && cacheName.ToLower().Equals(config.Name.ToLower()))
                        {
                            ///[] Until we completely move to using dom based configuration
                            ///we have to convert it to string props
                            string props = GetProps(config);
                            // all but the listed cache types are loaded in s_caches table.

                            string cacheId = config.Name.ToLower();

                            if (cacheInfo == null)
                            {
                                cacheInfo = new CacheInfo();
                                cacheInfo.Cache = new LeasedCache(props);
                                cacheInfo.CacheName = cacheName;
                                cacheInfo.CacheProps = config;
                            }
                            else
                            {
                                cacheInfo.CacheProps = config;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string msg = String.Format("CacheServer failed to load configuration information, Error {0}",
                    e.Message);
                AppUtil.LogEvent(msg, EventLogEntryType.Warning);
            }
        }

        [TargetMethod(ManagementUtil.MethodName.StopCacheOnCacheHost, 1)]
        public void StopCacheOnHost(string cacheId)
        {
            if(cacheInfo!=null && cacheInfo.CacheName.ToLower().Equals(cacheId.ToLower()))
                onCacheStopped();
        }


        /// <summary>
        /// Get string props representation of config
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static string GetProps(CacheServerConfig config)
        {
            ///[Ata] This is until we change the use of properties in Cache
            ///from props stirng or hashtable to Dom
            ///
            Hashtable table = ConfigConverter.ToHashtable(config);
            string props = ConfigReader.ToPropertiesString(table);
            return props;
        }

        /// <summary>
        /// Gets the status of NCache on this node.
        /// </summary>
        /// <returns>The ServerStatus.</returns>
        [TargetMethod(ManagementUtil.MethodName.GetCacheStatus, 1)]
        public override StatusInfo GetCacheStatus(string cacheId, string partitionId)
        {
            StatusInfo status = new StatusInfo();
            if (cacheInfo != null && cacheInfo.Cache.Name.ToLower().Equals(cacheId.ToLower()))
            {
                LeasedCache cache = cacheInfo.Cache;
                if (cache != null)
                {
                    if (cache.IsRunning) {
                        status.Status = CacheStatus.Running;
                        status.IsCoordinator = cache.IsCoordinator;
                    }
                }
            }
            return status;
        }

        /// <summary>
        /// Gets the list of all the configured cache servers in a clustered cache irrespective
        /// of running or stopped.
        /// </summary>
        /// <param name="cacheId"></param>
        /// <returns></returns>
        [TargetMethod(ManagementUtil.MethodName.GetCacheServers, 1)]
        public override Node[] GetCacheServers(string cacheId)
        {
            if (cacheId == null)
                throw new ArgumentNullException("cacheId");

            List<Node> serverNodes = new List<Node>();
            try
            {
                _rwLock.AcquireReaderLock(Timeout.Infinite);
                CacheInfo cacheInfo = GetCacheInfo(cacheId.ToLower());
                if (cacheInfo != null)
                {
                    if (cacheInfo.CacheProps.CacheType == "clustered-cache")
                    {
                        List<Address> nodeAddresses = cacheInfo.CacheProps.Cluster.GetAllConfiguredNodes();
                        ServerNode server = null;
                        foreach (Address node in nodeAddresses)
                        {
                            server = new ServerNode();
                            server.Address = node;
                            serverNodes.Add(server);
                        }
                    }
                }
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
            return serverNodes.ToArray();
        }

       
        /// <summary>
        /// Gets the list of all configured caches on this server.
        /// </summary>
        /// <returns></returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.GetAllConfiguredCaches, 1)]
        public override ConfiguredCacheInfo[] GetAllConfiguredCaches()
        {
            return base.GetAllConfiguredCaches();
        }


        [TargetMethod(ManagementUtil.MethodName.ClearCacheContent)]
        public override void ClearCacheContent(string cacheId)
        {
            if (cacheInfo != null)
            {
                Cache cache = cacheInfo.Cache;
                if (cache != null)
                    cache.Clear();
            }
        }

        [TargetMethod(ManagementUtil.MethodName.IsRunning)]
        public override bool IsRunning(string cacheId)
        {
            if (cacheInfo != null)
            {
                Cache cache = cacheInfo.Cache;
                if (cache != null && !String.IsNullOrEmpty(cacheId))
                    if (cache.Name.ToLower().Equals(cacheId.ToLower())) 
                        return cache.IsRunning;
            }
            return false;
        }

        [TargetMethod(ManagementUtil.MethodName.GetStatistics)]
        public override CacheStatistics GetStatistics(string cacheId)
        {
            if (cacheInfo != null)
            {
                Cache cache = cacheInfo.Cache;
                if (cache != null && cache.IsRunning)
                    return cache.Statistics;
            }
            return null;
        }

        [TargetMethod(ManagementUtil.MethodName.BalanceDataloadOnCache)]
        public override void BalanceDataloadOnCache(string cacheId)
        {
            if (cacheInfo != null)
            {
                Cache cache = cacheInfo.Cache;

                if (cache != null)
                    cache.BalanceDataLoad();
            }

        }

        [TargetMethod(ManagementUtil.MethodName.GetProcessId)]
        public int GetProcessID(string cacheId)
        {
            if (cacheInfo.CacheName.ToLower().Equals(cacheId.ToLower()))
                return System.Diagnostics.Process.GetCurrentProcess().Id;
            else
                return 0;
        }

        [TargetMethod(ManagementUtil.MethodName.GetCache,1)]
        public override InstrumentCache GetCache(string cacheId)
        {
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (cacheInfo != null)
                        return cacheInfo.Cache;
                return null;
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
        }

        [TargetMethod(ManagementUtil.MethodName.StartCachePhase2, 1)]
        public void StartCachePhase2(string cacheId)
        {
            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                LeasedCache cache = GetCacheInstance(cacheId.ToLower(), null) as LeasedCache;
                if (cache != null) cache.StartInstancePhase2();
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }
        [TargetMethod(ManagementUtil.MethodName.TransferConnection)]
        public override void TransferConnection(System.Net.Sockets.SocketInformation socketInfo, String cacheId, byte[] transferCommand)
        {
            ConnectionManager.OnClientConnected(socketInfo, transferCommand);
        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheName, 1)]
        public override string GetCacheName(int port)
        {
            return cacheInfo.CacheProps.Name;
        }
        public void InitiateCacheHostStopThread()
        {
            _processCheck = new Thread(new ThreadStart(StopCacheHostForcefully));
            _processCheck.IsBackground = true;
            _processCheck.Start();
        }
        private void StopCacheHostForcefully()
        {
            Thread.Sleep(30000);
            if (!ContainCacheProcess)
            {
                System.Environment.Exit(0);
            }
        }

        [TargetMethod(ManagementUtil.MethodName.CanApplyHotConfig, 1)]
        public override string CanApplyHotConfiguration(string cacheId, CacheServerConfig config)
        {
            Exception e = CanApplyHotConfig(config);

            if (e != null) return e.Message;
            return null;
        }

        public Exception CanApplyHotConfig(CacheServerConfig config)
        {
            if (cacheInfo != null)
            {
                if (cacheInfo != null)
                {
                    InstrumentCache cache = cacheInfo.Cache;
                    if (cache != null)
                        return cacheInfo.Cache.CanApplyHotConfig(config.Storage.Size * 1024 * 1024);
                }
            }
            return null;

        }

        [TargetMethod(ManagementUtil.MethodName.HotApplyConfiguration, 1)]
        public override void ApplyHotConfiguration(string cacheId, HotConfig hotConfig)
        {
            if (cacheInfo != null)
            {
                InstrumentCache cache = cacheInfo.Cache;
                if (cache != null)
                    cache.ApplyHotConfiguration(hotConfig);
            }
        }

        
    }
    
}

