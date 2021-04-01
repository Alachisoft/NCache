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
using System.Collections.Generic;

//using System.Linq;
using System.Text;

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
using System.Configuration;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Management.ClientConfiguration;
using System.Security.Principal;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Caching.Util;
using InstrumentCache = Alachisoft.NCache.Caching.Cache;
using Alachisoft.NCache.Management.APILogging;
using Alachisoft.NCache.Common.Topologies.Clustered;
using System.Net;
using Alachisoft.NCache.Management.Management;
using Alachisoft.NCache.Management.Statistics;
using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Common.Pooling.Stats;
using Alachisoft.NCache.Common.FeatureUsageData.Dom;

namespace Alachisoft.NCache.Management
{
    public class HostServer: CacheServer
    {
        private static HostServer s_instance;
        OnCacheStopped _callback;
        string _cacheserver="NCache";
        /// <summary> Returns the application name of this session. </summary>
        static new internal string ObjectUri
        {
            get
            {
                return typeof(CacheServer).Name;
            }
        }

        public new string ClusterIP
        {
            get { return _clusterIp; }
            set { _clusterIp = value; }
        }
        public new static string ClientserverIp
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
        private bool _forcefullStoppedLogging;
        Object _lock = new object();
        APIServerLogger _apiLogger; 
        /// <summary>
        /// Gets the singlton instance of CacheServer.
        /// </summary>
        public new static HostServer Instance
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
        public APIServerLogger APILogger
        {
            get { return _apiLogger; }
            set { _apiLogger = value; }
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

        static bool GetLicenseLoggingStatus()
        {


            string val = ConfigurationManager.AppSettings["NCacheServer.LicenseLogging"];
            if (String.IsNullOrEmpty(val))
                return false;

            bool result;
            return Boolean.TryParse(val, out result) ? result : false;

        }

        /// <summary>
        /// Finalizer for this object.
        /// </summary>
        ~HostServer()
        {
            
            base.Dispose();
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
        public new static int SocketServerPort
        {
            get { return _socketServerPort; }
            set { _socketServerPort = value; }
        }

        [TargetMethod(ManagementUtil.MethodName.GetClientNodeStatus, 1)]
        public override ClientNodeStatusWrapper GetClientNodeStatus(string cacheId)
        {
            string clientCacheId = string.Empty;

            ClientNodeStatus status = ClientConfigManager.GetClientNodeStatus(cacheId.ToLower(), out clientCacheId);
            
            return new ClientNodeStatusWrapper(status);
        }


        [TargetMethod(ManagementUtil.MethodName.VerifyWindowsUser, 1)]
        public override bool VerifyWindowsUser(string nodeName, string userName, string password)
        {
            return Alachisoft.NCache.Common.Util.SecurityUtil.VerifyWindowsUserForRole(nodeName, userName, password, WindowsBuiltInRole.Administrator);
        }

        //Method introduced to check weather a user is a windows administrator or not
        [TargetMethod(ManagementUtil.MethodName.VerfyAdministrator, 1)]
        public override bool VerfyAdministrator(string userName, string password)
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole
               (WindowsBuiltInRole.Administrator);

        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 1)]
        public override void StartCache(string cacheId)
        {
            StartCache(cacheId, null, null, null, null, null, null, null, false);
        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 4)]
        public override void StartCache(string cacheId, string partitionId, bool twoPhaseInitialization)
        {
            StartCache(cacheId, partitionId, null, null, null, null, null, null, twoPhaseInitialization);
        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 5)]
        public override void StartCache(string cacheId, string partitionId)
        {
            StartCache(cacheId, partitionId, null, null, null, null, null, null, false);
        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 6)]
        public override void StartCache(string cacheId,
            ItemAddedCallback itemAdded,
            ItemRemovedCallback itemRemoved,
            ItemUpdatedCallback itemUpdated,
            CacheClearedCallback cacheCleared,
            CustomRemoveCallback customRemove,
            CustomUpdateCallback customUpdate)
        {
            StartCache(cacheId, null, itemAdded, itemRemoved, itemUpdated, cacheCleared, customRemove, customUpdate, false);
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
        [TargetMethod(ManagementUtil.MethodName.StartCache, 7)]
        public override void StartCache(string cacheId, string partitionId,
            ItemAddedCallback itemAdded,
            ItemRemovedCallback itemRemoved,
            ItemUpdatedCallback itemUpdated,
            CacheClearedCallback cacheCleared,
            CustomRemoveCallback customRemove,
            CustomUpdateCallback customUpdate,
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
                    StartCacheInstance(cache, cacheInfo, itemAdded, itemRemoved, itemUpdated, cacheCleared, customRemove, customUpdate, twoPhaseInitialization);
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

        private void StartCacheInstance(LeasedCache cache, CacheInfo cacheInfo,
            ItemAddedCallback itemAdded,
            ItemRemovedCallback itemRemoved,
            ItemUpdatedCallback itemUpdated,
            CacheClearedCallback cacheCleared,
            CustomRemoveCallback customRemove,
            CustomUpdateCallback customUpdate, 
            bool twoPhaseInitialization)
        {
            if (cache != null)
            {
                if (itemAdded != null)
                    cache.ItemAdded += itemAdded;
                if (itemRemoved != null)
                    cache.ItemRemoved += itemRemoved;
                if (itemUpdated != null)
                    cache.ItemUpdated += itemUpdated;
                if (cacheCleared != null)
                    cache.CacheCleared += cacheCleared;
                if (customRemove != null)
                    cache.CustomRemoveCallbackNotif += customRemove;
                if (customUpdate != null)
                    cache.CustomUpdateCallbackNotif += customUpdate;

                if (!cache.IsRunning)
                {
                    cacheInfo.SyncConfiguration();
                    cache.StartInstance(Renderer, twoPhaseInitialization);

                    if (InstrumentCache.OnCacheStarted != null)
                        InstrumentCache.OnCacheStarted(cache.Name);

                }
            }

            else

                throw new Runtime.Exceptions.ManagementException(ErrorCodes.CacheInit.CACHE_ID_NOT_REGISTERED, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_ID_NOT_REGISTERED));

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
                    if (!cache.VerifyNodeShutDown())
                        throw new Runtime.Exceptions.ManagementException("Graceful shutdown is already in progress...");

                    if (reason.Equals(CacheStopReason.Expired))
                        cache.NCacheLog.CriticalInfo("NCache license has expired on this machine. Stopping cache...");

                    cache.Stop();
                    if (cacheInfo != null)
                        cacheInfo.SyncConfiguration();

                    if (InstrumentCache.OnCacheStopped != null)
                        InstrumentCache.OnCacheStopped(cache.Name);
                }
            }
            else

                throw new Runtime.Exceptions.ManagementException(ErrorCodes.CacheInit.CACHE_ID_NOT_REGISTERED, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_ID_NOT_REGISTERED));
        }

        public void onCacheStopped()
        {
            if (cacheStoppedCallback != null)
            {
                cacheStoppedCallback();

            }
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
                Alachisoft.NCache.Util.MiscUtil.RegisterCompactTypes(null);
                if (configs != null)
                {
                    for (int i = 0; i < configs.Length; i++)
                    {
                        CacheServerConfig config = configs[i];
                        if (config != null && !String.IsNullOrEmpty(cacheName) && cacheName.ToLower().Equals(config.Name.ToLower()))
                        {
                            /// Until we completely move to using dom based configuration
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

        [TargetMethod(ManagementUtil.MethodName.StopCacheOnHost, 1)]
        public override void StopCacheOnCacheHost(string cacheId)
        {
            if(cacheInfo!=null && cacheInfo.CacheName.ToLower().Equals(cacheId.ToLower()))
                onCacheStopped();
        }

       

        /// <summary>
        /// Gets the status of NCache on this node.
        /// </summary>
        /// <returns>The ServerStatus.</returns>
        [TargetMethod(ManagementUtil.MethodName.GetCacheStatus, 1)]
        public new StatusInfo GetCacheStatus(string cacheId, string partitionId)
        {
            StatusInfo status = new StatusInfo();
            if (cacheInfo != null && cacheInfo.Cache.Name.ToLower().Equals(cacheId.ToLower()))
            {
                LeasedCache cache = cacheInfo.Cache;
                if (cache != null)
                {
                    if (cache.IsRunning)
                        status.Status = CacheStatus.Running;
                    #if !(DEVELOPMENT || CLIENT)
                        status.IsCoordinator = cache.IsCoordinator;
                    #endif 
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
        public new Node[] GetCacheServers(string cacheId)
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
        public new ConfiguredCacheInfo[] GetAllConfiguredCaches()
        {
            return base.GetAllConfiguredCaches();
        }

        
        [TargetMethod(ManagementUtil.MethodName.ClearCacheContent)]
        public override void ClearCacheContent(string cacheId)
        {
            if (cacheInfo != null)
            {
                InstrumentCache cache = cacheInfo.Cache;
                if (cache != null)
                    cache.Clear();
            }
        }

        [TargetMethod(ManagementUtil.MethodName.IsRunning)]
        public override bool  IsRunning(string cacheId)
        {
            if (cacheInfo != null)
            {
                InstrumentCache cache = cacheInfo.Cache;
                if (cache != null && !String.IsNullOrEmpty(cacheId))
                    if (cache.Name.ToLower().Equals(cacheId.ToLower())) 
                        return cache.IsRunning;
            }
            return false;
        }
        
        [TargetMethod(ManagementUtil.MethodName.IsUnderStateTransfer)]
        public override bool IsUnderStateTransfer(string cacheId)
        {
            if (cacheInfo != null)
            {
                InstrumentCache cache = cacheInfo.Cache;
                if (cache != null && cache.IsRunning)
                {
                    return cache.IsClusterInStateTransfer();
                }
            }
            return false;

        }
        
        [TargetMethod(ManagementUtil.MethodName.GetStatistics)]
        public override CacheStatistics GetStatistics(string cacheId)
        {
            if (cacheInfo != null)
            {
                InstrumentCache cache = cacheInfo.Cache;
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
                InstrumentCache cache = cacheInfo.Cache;

                if (cache != null)
                    cache.BalanceDataLoad();
            }

        }

        [TargetMethod(ManagementUtil.MethodName.GetProcessId)]
        public override int GetProcessID(string cacheId)
        {
            if (cacheInfo.CacheName.ToLower().Equals(cacheId.ToLower()))
                return System.Diagnostics.Process.GetCurrentProcess().Id;
            else
                return 0;
        }

        [TargetMethod(ManagementUtil.MethodName.GetCache,1)]
        public override Alachisoft.NCache.Caching.Cache GetCache(string cacheId)
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
        public override void StartCachePhase2(string cacheId)
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

        [TargetMethod(ManagementUtil.MethodName.GetCacheStatistics, 1)]
        public override CacheNodeStatistics[] GetCacheStatistics(string cacheId)
        {
            Alachisoft.NCache.Caching.Cache cache = GetCacheInstance(cacheId.ToLower(), null);
            List<CacheNodeStatistics> statistics = null;
            if (cache != null)
            {
                statistics = cache.GetCacheNodeStatistics();
                if (statistics != null)
                {
                    return statistics.ToArray();
                }
            }
            return null;

        }

        [TargetMethod(ManagementUtil.MethodName.TransferConnection)]
        public override  void TransferConnection(System.Net.Sockets.SocketInformation socketInfo, String cacheId, byte[] transferCommand)
        {
            ConnectionManager.OnClientConnected(socketInfo, transferCommand);
        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheName, 1)]
        public override  string GetCacheName(int port)
        {
            return cacheInfo.CacheProps.Name;
        }
        
         [TargetMethod(ManagementUtil.MethodName.GetClientProcessStats, 1)]
         public override List<Alachisoft.NCache.Common.Monitoring.ClientProcessStats> GetClientProcessStats(string cacheId)
         {
             if (cacheId == null)
                 throw new ArgumentNullException("cacheId");

             List<Alachisoft.NCache.Common.Monitoring.ClientProcessStats> clients = Renderer.GetClientProcessStats(cacheId.ToLower());
             return clients;
         }

         [TargetMethod(ManagementUtil.MethodName.GetCacheClients, 1)]
         public override List<Alachisoft.NCache.Common.Monitoring.ClientNode> GetCacheClients(string cacheId)
         {
             if (cacheId == null)
                 throw new ArgumentNullException("cacheId");

             List<Alachisoft.NCache.Common.Monitoring.ClientNode> clients = Renderer.GetClientList(cacheId.ToLower());
             return clients;
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
                AppUtil.LogEvent(_cacheserver, "Process self distruction occur", System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.CacheStartError);
                System.Environment.Exit(0);
            }
        }

        [TargetMethod(ManagementUtil.MethodName.GetAPILogData, 1)]
        public Hashtable GetAPILogging(string cacheID, string instanceID)
        {
            Hashtable data = new Hashtable();
            List<Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem> apiLogData = null;
            try
            {

                if (cacheID != null)
                {
                   
                    if (_forcefullStoppedLogging || _apiLogger==null)
                    {
                        throw new APILoggingException("Logging has been stopped due to connection lost");
                    }
                    if (APILogging.APILogManager.APILogManger != null)
                    {
                        long startTime = APILogger.GetInstanceStartTime(instanceID.ToLower());
                        if (startTime != -1)
                            apiLogData = APILogging.APILogManager.APILogManger.GetEntry(ref startTime);
                        lock (_lock)
                        {
                            data.Add("items", apiLogData);
                        }
                        APILogger.UpdateInstanceStartTime(instanceID.ToLower(), startTime);
                    }

                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return data;
        }

        [TargetMethod(ManagementUtil.MethodName.GetTopicStats, 1)]
        public override Dictionary<string, TopicStats> GetTopicStats(string cacheId,bool defaultTopicStats=false )
        {
            Dictionary<string, TopicStats> topicStatList = new Dictionary<string, TopicStats>();
            if (cacheId == null)
                throw new ArgumentNullException("cacheId");
            if (cacheInfo != null)
            {
                InstrumentCache cache = cacheInfo.Cache;
                if (cache != null && !string.IsNullOrEmpty(cacheId) && cache.Name.ToLower().Equals(cacheId.ToLower()))
                {
                    topicStatList = cache.GetTopicsStats(defaultTopicStats);
                }
            }
            return topicStatList;
        }

        [TargetMethod(ManagementUtil.MethodName.GetCounterValue, 1)]
        public override double GetCounterValue(string cacheId, string counterName, string category, bool replica = false)
        {
            double value = 0.0;
          
            if (cacheInfo != null)
            {
                InstrumentCache cache = cacheInfo.Cache;
                if (cache != null && !string.IsNullOrEmpty(cacheId) && cache.Name.ToLower().Equals(cacheId.ToLower()))
                {
                    value = cache.GetCounterValue(counterName, replica, category);
                }
            }
            return value;
        }


        [TargetMethod(ManagementUtil.MethodName.GetCacheProcessID, 1)]
        public new int GetCacheProcessID(string cacheID)
        {
            if (!string.IsNullOrEmpty(cacheID))
                return Process.GetCurrentProcess().Id;
            else
                return 0;
        }

        [TargetMethod(ManagementUtil.MethodName.CanApplyHotConfig, 2)]
        public override bool CanApplyHotConfiguration(string cacheId, long size)
        {
            if (cacheInfo != null)
            {
                InstrumentCache cache = cacheInfo.Cache;
                if (cache != null)
                    return cacheInfo.Cache.IsHotApplyFeasible(size * 1024 * 1024);
            }
            return false;
        }

        [TargetMethod(ManagementUtil.MethodName.GetPoolStats, 1)]
        public sealed override PoolStats GetPoolStats(PoolStatsRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(request.CacheId))
                throw new OperationFailedException("Invalid CacheId specified!");

            if (cacheInfo != null)
            {
                var cache = cacheInfo.Cache;

                if (cache != null && $"{request.CacheId}".ToLowerInvariant().Equals($"{cache.Name}".ToLowerInvariant()))
                {
                    return cache.GetPoolStats(request);
                }
            }
            return new PoolStats();
        }

        [TargetMethod(ManagementUtil.MethodName.GetFeatureUsageReport, 1)]
        public override Dictionary<string, Common.FeatureUsageData.Feature> GetFeatureUsageReport(string cacheId)
        {
            Dictionary<string, Common.FeatureUsageData.Feature> featureUsageReport = null;
            if (cacheInfo != null)
            {
                InstrumentCache cache = cacheInfo.Cache;
                if (cache != null)
                    featureUsageReport = cache.GetCacheFeaturesUsageReport();
            }

            return featureUsageReport;
        }


        [TargetMethod(ManagementUtil.MethodName.GetClientProfileReport, 1)]
        public override ClientProfileDom GetClientProfileReport(string cacheId)
        {
            ClientProfileDom clientProfile = null;
            if (cacheInfo != null)
            {
                InstrumentCache cache = cacheInfo.Cache;
                if (cache != null)
                    clientProfile = cache.GetClientProfileReport();
            }

            return clientProfile;
        }
    }
}

