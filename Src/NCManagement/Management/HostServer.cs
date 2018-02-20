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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Caching;
using System.Threading;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common;
using System.Timers;
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
using Alachisoft.NCache.Config;
using InstrumentCache = Alachisoft.NCache.Caching.Cache;
using System.IO;
using Alachisoft.NCache.Common.Monitoring.APILogging;
using Alachisoft.NCache.Management.APILogging;

namespace Alachisoft.NCache.Management
{
    public class HostServer : CacheServer
    {
        private static HostServer s_instance;
        OnCacheStopped _callback;

        string _cacheserver = "NCache";

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

        private void StopCache(CacheStopReason cacheStopReason)
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
        

        public void OnLicenseExpiration()
        {
            StopCache(CacheStopReason.Expired);
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


        [TargetMethod(ManagementUtil.MethodName.StartCache, 3)]
        public override void StartCache(string cacheId, string partitionId, bool twoPhaseInitialization)
        {
            StartCache(cacheId, partitionId, null, null, null, null, null, null, twoPhaseInitialization);
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
            try
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

#if COMMUNITY

                    if (InstrumentCache.OnCacheStarted != null)
                        InstrumentCache.OnCacheStarted(cache.Name);

#endif
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
            try
            {
                if (cacheInfo != null)
                {
                    cache = cacheInfo.Cache;
                }
                if (cache != null)
                {
                    try
                    {
                        StopCacheInstance(cache.Name, cacheInfo, CacheStopReason.Stoped);
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


        public override void StopCacheInstance(string cacheName, CacheInfo cacheInfo, CacheStopReason reason)
        {
            LeasedCache cache = cacheInfo.Cache;
            if (cache != null && cache.Name == cacheName)
            {
                if (cache.IsRunning)
                {
                    if (reason.Equals(CacheStopReason.Expired))
                        cache.NCacheLog.CriticalInfo("NCache license has expired on this machine. Stopping cache...");
                    cache.Stop();
                    if (cacheInfo != null)
                        cacheInfo.SyncConfiguration();

                    //instrumentation Code
#if COMMUNITY
                    
                        if (InstrumentCache.OnCacheStopped != null)
                            InstrumentCache.OnCacheStopped(cache.Name);
                   
#endif
                }
            }
            else
                throw new Runtime.Exceptions.ManagementException("Specified cacheId is not registered");
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
                Alachisoft.NCache.Util.MiscUtil.RegisterCompactTypes();
                if (configs != null)
                {
                    for (int i = 0; i < configs.Length; i++)
                    {
                        CacheServerConfig config = configs[i];
                        if (config != null && !String.IsNullOrEmpty(cacheName) && cacheName.ToLower().Equals(config.Name.ToLower()))
                        {
                            string props = GetProps(config);
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
            if (cacheInfo != null && cacheInfo.CacheName.ToLower().Equals(cacheId.ToLower()))
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
#if !(CLIENT)
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
        public override bool IsRunning(string cacheId)
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


        [TargetMethod(ManagementUtil.MethodName.GetProcessId)]
        public override int GetProcessID(string cacheId)
        {
            if (cacheInfo.CacheName.ToLower().Equals(cacheId.ToLower()))
                return System.Diagnostics.Process.GetCurrentProcess().Id;
            else
                return 0;
        }

        [TargetMethod(ManagementUtil.MethodName.GetCache, 1)]
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
        public override void TransferConnection(System.Net.Sockets.SocketInformation socketInfo, String cacheId, byte[] transferCommand)
        {
            ConnectionManager.OnClientConnected(socketInfo, transferCommand);
        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheName, 1)]
        public override string GetCacheName(int port)
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
        [TargetMethod(ManagementUtil.MethodName.StartAPILogging)]
        public bool StartAPILogging(string cacheID, string InstanceID, bool version)
        {
            if (cacheID != null)
            {
                try
                {
                    _forcefullStoppedLogging = false;
                    if (_apiLogger == null)
                    {
                        APILogger = new APIServerLogger();
                    }
                    APILogger.AddInstanceInformation(InstanceID.ToLower());

                    if (APILogging.APILogManager.APILogManger == null && APILogger != null && !APILogger.IsLoggingEnabled(InstanceID.ToLower()))
                    {

                        APILogging.APILogManager logmanger = new APILogging.APILogManager();
                        logmanger.StartLogging(cacheID, version);
                        APILogging.APILogManager.APILogManger = logmanger;
                        AppUtil.LogEvent("API logging sucessfully started on cache " + cacheID, EventLogEntryType.Information);
                        return true;

                    }
                    else
                    {
                        return APILogManager.EnableLogging;
                    }

                }
                catch
                {
                    return false;
                }
            }
            return false;

        }

        [TargetMethod(ManagementUtil.MethodName.StopAPILogging, 1)]
        public void StopAPILogging(string cacheID, string InstanceID, bool forceful)
        {
            try
            {
                if (APILogger.ContainsInstance(InstanceID.ToLower()) && forceful)
                    _forcefullStoppedLogging = forceful;
                if (cacheID != null && APILogger != null && APILogger.StopLogging(InstanceID.ToLower()))
                {
                    APILogging.APILogManager.APILogManger.StopLogging(cacheID);
                    APILogging.APILogManager.APILogManger = null;
                    AppUtil.LogEvent("API logging sucessfully stopped on cache " + cacheID, EventLogEntryType.Information);
                }

            }
            catch
            {

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

                    if (_forcefullStoppedLogging || _apiLogger == null)
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
        public override Dictionary<string, TopicStats> GetTopicStats(string cacheId)
        {
            Dictionary<string, TopicStats> topicStatList = new Dictionary<string, TopicStats>();
            if (cacheId == null)
                throw new ArgumentNullException("cacheId");
            if (cacheInfo != null)
            {
                InstrumentCache cache = cacheInfo.Cache;
                if (cache != null && !string.IsNullOrEmpty(cacheId) && cache.Name.ToLower().Equals(cacheId.ToLower()))
                {
                    topicStatList = cache.GetTopicsStats();
                }
            }
            return topicStatList;
        }


        [TargetMethod(ManagementUtil.MethodName.GetCacheProcessID, 1)]
        public int GetCacheProcessID(string cacheID)
        {
            if (!string.IsNullOrEmpty(cacheID))
                return Process.GetCurrentProcess().Id;
            else
                return 0;
        }
    }
}

