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


using System.IO;
using System.Collections;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Configuration;
using System.Timers;
using System.Collections.Generic;
using System.Security.Principal;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.RPCFramework;
using Alachisoft.NCache.Runtime.Exceptions;
#if !NETCORE
using System.Management;
#endif
using System.Xml;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Management.ClientConfiguration;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Management.Management;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Management.ClientConfiguration.Dom;
using System;
using Alachisoft.NCache.Common.Monitoring.APILogging;
using System.Net.Sockets;
using Alachisoft.NCache.Common.Communication.Exceptions;
using Alachisoft.NCache.Common.Topologies.Clustered;
using Alachisoft.NCache.Common.ErrorHandling;
using System.Net;
using Alachisoft.NCache.Common.Caching.Statistics.CustomCounters;
using Alachisoft.NCache.Management.Statistics;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Communication;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Pooling.Stats;
using System.Linq;
using System.Threading.Tasks;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Common.FeatureUsageData.Dom;
#if NETCORE
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
#endif


namespace Alachisoft.NCache.Management
{
    /// <summary>
    /// Manages cache start and stop and keeps a named collection of caches
    /// </summary>
    public class CacheServer : MarshalByRefObject, ICacheServer
    {
        private static CacheServer s_instance;
        string _cacheserver = "NCache";
       
        /// <summary>
        /// Enumeration specifying type of channel
        /// </summary>
        public enum Channel
        {
            /// <summary>
            /// Bind cluster channel
            /// </summary>
            Cluster = 0,
            /// <summary>
            /// Bind client server channel
            /// </summary>
            SocketServer
        }

        public FeatureDataManager FeatureDataManager { get => _featureDataManager; set => _featureDataManager = value; }

        public enum CacheStopReason
        {
            Expired,
            Stoped,
            ForcedStoped
        }

        private static object serviceObject = new object();

        /// <summary> Returns the application name of this session. </summary>
        static internal string ObjectUri
        {
            get
            {
                return typeof(CacheServer).Name;
            }
        }
        public string ClusterIP
        {
            get { return _clusterIp; }
            set { _clusterIp = value; }
        }
        public static string ClientserverIp
        {
            get { return CacheServer._clientserverip; }
            set { CacheServer._clientserverip = value; }
        }

        private string LocalCacheIP
        {
            get { return _localCacheIp; }
            set { _localCacheIp = value; }
        }

        /// <summary> A Hashtable that contains a list of all the cache objects created so far. </summary>
        public static Hashtable s_caches = Hashtable.Synchronized(new Hashtable(StringComparer.CurrentCultureIgnoreCase));

        /// <summary> Reader writer lock used to synchronize access to internals. </summary>
        internal ReaderWriterLock _rwLock = new ReaderWriterLock();

        /// <summary>Socket server port</summary>
        private static int _socketServerPort;
        /// <summary>Server port</summary>
        private static int _clusterPort = 9800;

        private static string _clusterIp;
        private static string _clientserverip;
        private static string _localCacheIp;
        private CacheRenderer _renderer;
        private static bool waitForDebugger = false;

        private static System.Timers.Timer _evalWarningTask;
        private TimeScheduler _gcScheduler;
        private TimeScheduler _portPoolScheduler;
        
        private static IConnectionManager _connectionManager;
        private Alachisoft.NCache.Management.HostServer hostServer;
        private FeatureDataManager _featureDataManager;
        private static IDictionary<string, IDictionary<string, List<OverloadInfo>>> _methodDic;
        private static string _asmVersion;
        private static int eventCount = 0;
        private static bool _logOnce = true;

        static DateTime lastLicenseInfoAccessTime = DateTime.Now;
        static private int CachedLicenseInfoTimeout = 5;

        private static DateTime weeklyDate = DateTime.Now.AddDays(7);
        public static Hashtable _clientCustomCounters = Hashtable.Synchronized(new Hashtable(StringComparer.CurrentCultureIgnoreCase));

        public static IConnectionManager ConnectionManager
        {
            get { return _connectionManager; }
            set { _connectionManager = value; }
        }
        public static IDictionary<string, IDictionary<string, List<OverloadInfo>>> MethodsDictionary
        {
            get { return _methodDic; }
            set { _methodDic = value; }
        }

        private static CachePortsConfigManger _cachePortsConfigManger;
        private static ServerLicenseInfo licenseInfo;
        // ILogger _logger;

        /// <summary>
        /// Static constructor
        /// </summary>
        static CacheServer()
        {
            try
            {
                //Just need to call the static block - stupid logik i know
                Alachisoft.NCache.Util.MiscUtil.RegisterCompactTypes(null);
              
                RegisterCompactTypes();

               
                try
                {
                    ReflectedWebAPI webAPi = new ReflectedWebAPI();
                    MethodsDictionary = webAPi.Initialize();
                    _asmVersion = webAPi.AssemblyVersion;
                }
                catch (Exception ex)
                {
                    AppUtil.LogEvent("Exception occured while reflecting API: " + ex.ToString(), EventLogEntryType.Error);
                }
                string debugString = ConfigurationSettings.AppSettings["WaitForDebugger"];
                if (!string.IsNullOrEmpty(debugString))
                    bool.TryParse(debugString, out waitForDebugger);

            }
            catch (Exception e)
            {
                string msg = String.Format("CacheServer failed to load configuration, Error {0}", e.Message);
                AppUtil.LogEvent(msg, EventLogEntryType.Warning);
            }
        }
              
        public string Source
        {
            get { return null; }
            set { }
        }
        public static void RegisterCompactTypes()
        {
            CompactFormatterServices.RegisterCompactType(typeof(TargetMethodParameter), 165);

            #region [Register Cache Server Assemblies]

            CompactFormatterServices.RegisterCompactType(typeof(Common.ProductVersion), 302);
            CompactFormatterServices.RegisterCompactType(typeof(CacheServerConfig), 177);
            CompactFormatterServices.RegisterCompactType(typeof(Log), 178);
            CompactFormatterServices.RegisterCompactType(typeof(PerfCounters), 179);
       
            CompactFormatterServices.RegisterCompactType(typeof(Config.Dom.Type), 184);
            CompactFormatterServices.RegisterCompactType(typeof(CompactClass), 186);
            CompactFormatterServices.RegisterCompactType(typeof(BackingSource), 187);
            CompactFormatterServices.RegisterCompactType(typeof(Readthru), 188);
            CompactFormatterServices.RegisterCompactType(typeof(Writethru), 189);
            CompactFormatterServices.RegisterCompactType(typeof(Provider), 190);
            
            CompactFormatterServices.RegisterCompactType(typeof(Config.Dom.Notifications), 192);
            CompactFormatterServices.RegisterCompactType(typeof(Cleanup), 194);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Config.Dom.Storage), 195);
            CompactFormatterServices.RegisterCompactType(typeof(EvictionPolicy), 196);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Config.Dom.Cluster), 197);
            CompactFormatterServices.RegisterCompactType(typeof(Config.Dom.Channel), 198);
            CompactFormatterServices.RegisterCompactType(typeof(NodeIdentity), 199);
            CompactFormatterServices.RegisterCompactType(typeof(StatusInfo), 200);
            CompactFormatterServices.RegisterCompactType(typeof(ReplicationStrategy), 201);
            CompactFormatterServices.RegisterCompactType(typeof(Config.Dom.Security), 202);
            CompactFormatterServices.RegisterCompactType(typeof(AutoLoadBalancing), 203);
            CompactFormatterServices.RegisterCompactType(typeof(ClientNodes), 204);
            CompactFormatterServices.RegisterCompactType(typeof(Config.Dom.ClientNode), 205);
            CompactFormatterServices.RegisterCompactType(typeof(SQLDependencyConfig), 210);
            CompactFormatterServices.RegisterCompactType(typeof(CacheRegisterationInfo), 212);
            CompactFormatterServices.RegisterCompactType(typeof(CacheStatusOnServerContainer), 213);
            CompactFormatterServices.RegisterCompactType(typeof(CacheStatistics), 65);
            CompactFormatterServices.RegisterCompactType(typeof(ClusterCacheStatistics), 66);
            CompactFormatterServices.RegisterCompactType(typeof(ClientConfiguration.Dom.ClientConfiguration), 214);
            CompactFormatterServices.RegisterCompactType(typeof(NodeConfiguration), 215);
            CompactFormatterServices.RegisterCompactType(typeof(CacheConfiguration), 216);
            CompactFormatterServices.RegisterCompactType(typeof(UserCredentials), 218);
            CompactFormatterServices.RegisterCompactType(typeof(ClientConfiguration.Dom.CacheServer), 219);
            CompactFormatterServices.RegisterCompactType(typeof(CacheInfo), 220);
            CompactFormatterServices.RegisterCompactType(typeof(BindedIpMap), 237);
            CompactFormatterServices.RegisterCompactType(typeof(NodeInfoMap), 238);
            CompactFormatterServices.RegisterCompactType(typeof(User), 240);
            CompactFormatterServices.RegisterCompactType(typeof(User[]), 241);
            CompactFormatterServices.RegisterCompactType(typeof(CompactClass[]), 243);
            CompactFormatterServices.RegisterCompactType(typeof(Parameter), 244);
            CompactFormatterServices.RegisterCompactType(typeof(Parameter[]), 245);
            CompactFormatterServices.RegisterCompactType(typeof(ProviderAssembly), 246);
            CompactFormatterServices.RegisterCompactType(typeof(Config.Dom.ClientNode[]), 247);
            CompactFormatterServices.RegisterCompactType(typeof(Provider[]), 248);
            CompactFormatterServices.RegisterCompactType(typeof(ClientNodeStatusWrapper), 250);
            CompactFormatterServices.RegisterCompactType(typeof(Attrib), 251);
            CompactFormatterServices.RegisterCompactType(typeof(Config.Dom.Type[]), 252);
            CompactFormatterServices.RegisterCompactType(typeof(PortableClass), 253);
            CompactFormatterServices.RegisterCompactType(typeof(PortableClass[]), 254);
            CompactFormatterServices.RegisterCompactType(typeof(AttributeListUnion), 255);
            CompactFormatterServices.RegisterCompactType(typeof(PortableAttribute), 256);
            CompactFormatterServices.RegisterCompactType(typeof(PortableAttribute[]), 257);
            CompactFormatterServices.RegisterCompactType(typeof(RtContextValue), 300);
            CompactFormatterServices.RegisterCompactType(typeof(HotConfig), 347);
            CompactFormatterServices.RegisterCompactType(typeof(ClientDeathDetection), 355);
            CompactFormatterServices.RegisterCompactType(typeof(LoaderTag), 282);
            CompactFormatterServices.RegisterCompactType(typeof(SynchronizationStrategy), 378);
            CompactFormatterServices.RegisterCompactType(typeof(ClientCustomCounters), 514);

            #endregion

            #region [Register Monitor Server Assemblies]

            CompactFormatterServices.RegisterCompactType(typeof(CacheNodeStatistics), 221);
            CompactFormatterServices.RegisterCompactType(typeof(ServerNode), 222);
            CompactFormatterServices.RegisterCompactType(typeof(EventViewerEvent), 223);
            CompactFormatterServices.RegisterCompactType(typeof(Node), 224);
            CompactFormatterServices.RegisterCompactType(typeof(Address), 110);
            CompactFormatterServices.RegisterCompactType(typeof(Common.Monitoring.ClientNode), 226);
            CompactFormatterServices.RegisterCompactType(typeof(ConfiguredCacheInfo), 227);
            CompactFormatterServices.RegisterCompactType(typeof(ClientProcessStats), 228);

            CompactFormatterServices.RegisterCompactType(typeof(CacheNodeStatistics[]), 229);
            CompactFormatterServices.RegisterCompactType(typeof(ServerNode[]), 230);
            CompactFormatterServices.RegisterCompactType(typeof(EventViewerEvent[]), 231);
            CompactFormatterServices.RegisterCompactType(typeof(Node[]), 232);
            CompactFormatterServices.RegisterCompactType(typeof(Common.Monitoring.ClientNode[]), 234);
            CompactFormatterServices.RegisterCompactType(typeof(Address[]), 233);
            CompactFormatterServices.RegisterCompactType(typeof(ConfiguredCacheInfo[]), 235);
            CompactFormatterServices.RegisterCompactType(typeof(ClientProcessStats[]), 236);
            CompactFormatterServices.RegisterCompactType(typeof(CacheServerList), 239);
            CompactFormatterServices.RegisterCompactType(typeof(WriteBehind), 276);
            CompactFormatterServices.RegisterCompactType(typeof(BatchConfig), 277);

            #endregion

            CompactFormatterServices.RegisterCompactType(typeof(HealthAlerts), 536);
            CompactFormatterServices.RegisterCompactType(typeof(ResourceAtribute), 537);
        }

        /// <summary>
        /// Gets the singlton instance of CacheServer.
        /// </summary>
        public static CacheServer Instance
        {
            get { return s_instance; }
            set { s_instance = value; }
        }



        public CacheServer()
        {
            if (this._gcScheduler == null)
            {
                this._gcScheduler = new TimeScheduler();
            }
            this._gcScheduler.Start();

            this.StartGCTask();
            _featureDataManager = new FeatureDataManager(this);
        }



       
       
        /// <summary>
        /// Finalizer for this object.
        /// </summary>
        ~CacheServer()
        {

            Dispose(false);
        }

        /// <summary>
        /// Obtains a lifetime service object to control the lifetime policy for this instance.
        /// </summary>
        /// <returns>An object of type ILease used to control the lifetime 
        /// policy for this instance.</returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

      
        [TargetMethod(ManagementUtil.MethodName.GetTayzGridServer, 1)]
        public bool IsTayzGridServer()
        {
            return false;
        }

        /// <param name="name">Name of the file (assembly)</param>
        /// <param name="buffer"></param>
        /// 
        [TargetMethod(ManagementUtil.MethodName.CopyAssemblies, 1)]
        public void CopyAssemblies(string cacheName, string name, byte[] buffer)
        {

            if (AppUtil.InstallDir != null)
            {
                System.IO.Directory.CreateDirectory(AppUtil.DeployedAssemblyDir);
                string folderLevel2 = System.IO.Path.Combine(AppUtil.DeployedAssemblyDir, cacheName);
                System.IO.Directory.CreateDirectory(folderLevel2.Trim());
                FileStream fs = new FileStream(folderLevel2 + Path.DirectorySeparatorChar + name, FileMode.Create, FileAccess.Write);
                fs.Write(buffer, 0, buffer.Length);
                fs.Flush();
                fs.Close();
            }
        }


        /// <param name="name">Name of the file (assembly)</param>
        /// <param name="filesData"> Dictionary containing file data along with file name</param>
        /// 
        [TargetMethod(ManagementUtil.MethodName.CopyAssemblies, 2)]
        public void CopyAssemblies(string cacheName, Dictionary<string, byte[]> filesData)
        {
            foreach (var fileData in filesData)
            {
                CopyAssemblies(cacheName, fileData.Key, fileData.Value);
            }
        }

        /// <summary>
        /// Add garbage collection task to time sheduler
        /// </summary>

        private void StartGCTask()
        {
            bool enabled = ServiceConfiguration.EnableForcedGC;

            if (enabled && System.Runtime.GCSettings.IsServerGC) //ALAWAYS RETURNS FALSE IN CASE OF MONO
            {
                int threshold = 0;

                if (ServiceConfiguration.GCThreshold != null)
                    threshold = ServiceConfiguration.ForcedGCThreshold;
                this._gcScheduler.AddTask(new GarbageCollectionTask(threshold));
            }
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        /// <remarks>
        /// </remarks>
        private void Dispose(bool disposing)
        {
            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (_evalWarningTask != null)
                {
                    try
                    {
                        _evalWarningTask.Stop();
                        _evalWarningTask.Close();
                        _evalWarningTask.Dispose();
                    }
                    catch (Exception e)
                    {

                    }
                }
                
                if (this._gcScheduler != null)
                {
                    lock (this._gcScheduler)
                    {
                        this._gcScheduler.Stop();
                        this._gcScheduler.Dispose();

                    }
                }
                if (this._portPoolScheduler != null)
                {
                    lock (this._portPoolScheduler)
                    {
                        this._portPoolScheduler.Stop();
                        this._portPoolScheduler.Dispose();
                    }
                }

            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
            if (disposing) GC.SuppressFinalize(this);

            for (IDictionaryEnumerator idenum = s_caches.GetEnumerator(); idenum.MoveNext();)
            {
                CacheInfo cacheInfo = (CacheInfo)idenum.Value;
                if (cacheInfo != null)
                {
                    try
                    {
                        if (cacheInfo.Service != null)
                        {
                            cacheInfo.Service.Dispose();
                            cacheInfo.Service = null;
                        }
                        if (cacheInfo.CacheServer != null)
                        {
                            cacheInfo.CacheServer.Dispose();
                            cacheInfo.CacheServer = null;
                        }
                    }
                    catch (Exception) { }
                }
            }

        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
       
        #endregion

        /// <summary>
        /// Clear cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <summary>
        /// 
        [TargetMethod(ManagementUtil.MethodName.ClearCache, 1)]
        public void ClearCache(string cacheId)
        {
            ClearCacheContent(cacheId);
        }

       

       

        /// <summary>
        /// finds and returns a cache object, that was previously created.
        /// </summary>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        public Alachisoft.NCache.Caching.Cache this[string cacheId]
        {
            get
            {
                if (cacheId == null) throw new ArgumentNullException("cacheId");
                return GetCache(cacheId);
            }
        }

        public virtual Alachisoft.NCache.Caching.Cache GetCache(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                    return cacheServer.GetCache(cacheId);
                else
                    throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException(ErrorCodes.CacheInit.CACHE_ID_NOT_REGISTERED,ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_ID_NOT_REGISTERED));
            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
            }
            return null;
        }

        [TargetMethod(ManagementUtil.MethodName.HotApplyConfiguration, 1)]
        public virtual void ApplyHotConfiguration(string cacheId, HotConfig hotConfig)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                    cacheServer.ApplyHotConfiguration(cacheId, hotConfig);
                else
                    throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException("Specified cacheId is not started");
            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
            }
        }

        /// <summary>
        /// Get a list of running caches (local + clustered)
        /// </summary>
        /// <returns>list of running caches</returns> 
        /// 
        [TargetMethod(ManagementUtil.MethodName.GetRunningCaches, 2)]
        public ArrayList GetRunningCaches()
        {
            try
            {
                return GetRunningCachesInternal();
            }
            catch (SecurityException sx)
            {
                throw sx;
            }
        }

        /// <summary>
        /// Returns a list of running caches
        /// </summary>
        /// <returns>list of running caches</returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.GetRunningCaches, 3)]
        private ArrayList GetRunningCachesInternal()
        {
            ArrayList runningCache = new ArrayList(5);
            IDictionary coll = CacheProps;
            foreach (DictionaryEntry entry in coll)
            {
                //in case of partitioned replica caches we get hashtables.
                //so we must check what is the type of ie.value....
                if (entry.Value is CacheServerConfig)
                {
                    CacheServerConfig config = (CacheServerConfig)entry.Value;
                    if (config.CacheType == "local-cache" || config.CacheType == "clustered-cache")
                    {

                        if (IsRunning((string)entry.Key))


                            runningCache.Add(entry.Key);
                    }
                }
                if (entry.Value is String)
                {

                    if (((string)entry.Value).IndexOf("local-cache", 0) != -1 || ((string)entry.Value).IndexOf("clustered-cache", 0) != -1)
                    {
                        if (IsRunning((string)entry.Key))



                            runningCache.Add(entry.Key);
                    }
                }
            }
            return runningCache;
        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheInstance, 1)]
        public Alachisoft.NCache.Caching.Cache GetCacheInstance(string cacheId, string partitionId)
        {
            if (partitionId == null || partitionId == string.Empty)
                return this[cacheId];
            else
                if (cacheId == null)
                    throw new ArgumentNullException("cacheId");
            return null;
        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheInstance, 2)]
        public Alachisoft.NCache.Caching.Cache GetCacheInstance(string cacheId)
        {
            return this[cacheId.ToLower()];
        }

        /// <summary>
        /// Gets the cache instance ignoring the backup/replica id. For e.g. if we have two instances
        /// of the same cache por_test (master node id) and por_test_bk_node_node1
        /// a replica of the same cache. Now we try to first connect the master id and if it is not
        /// available or running then we try to connect to its backup although its id is different.
        /// </summary>
        /// <param name="cacheId">Id of the cache</param>
        /// <param name="userId">user id</param>
        /// <param name="password">password</param>
        /// <returns>Cache Instance</returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.GetCacheInstanceIgnoreReplica, 2)]
        public Alachisoft.NCache.Caching.Cache GetCacheInstanceIgnoreReplica(string cacheId)
        {

            if (cacheId == null) throw new Exception("Cache ID can not be null");
            cacheId = cacheId.ToLower();
            bool isLockAcquired = false;
            if (!_rwLock.IsWriterLockHeld)
            {
                _rwLock.AcquireReaderLock(Timeout.Infinite);
                isLockAcquired = true;
            }
            Alachisoft.NCache.Caching.Cache cache = null;
            try
            {
                cache = this[cacheId];
                if (cache != null && cache.IsRunning)
                {
                    return cache;
                }
            }
            finally
            {
                if (isLockAcquired)
                    _rwLock.ReleaseReaderLock();
            }
            return cache;
        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheRenderer, 1)]
        public CacheRenderer GetCacheRenderer()
        {
            return Renderer;
        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheProps, 1)]
        public IDictionary GetCacheProps()
        {
            Hashtable cacheProps = new Hashtable();
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                IDictionaryEnumerator en = s_caches.GetEnumerator();
                while (en.MoveNext())
                {
                    CacheInfo cacheInfo = (CacheInfo)en.Value;
                    try
                    {
                        ICacheServer cacheServer = GetCacheServer(cacheInfo.CacheName, false);
                        if (cacheServer != null)
                        {
                            cacheInfo.CacheProps.IsRunning = cacheServer.IsRunning(cacheInfo.CacheName);
                        }
                        else
                        {
                            cacheInfo.CacheProps.IsRunning = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        cacheInfo.CacheProps.IsRunning = false;
                    }
                    cacheProps.Add(cacheInfo.CacheName, cacheInfo.CacheProps);
                }
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            return cacheProps;

        }

        /// <summary>
        /// A collection of the cache infos registered with the server.
        /// </summary>
        /// <remarks>
        /// CacheProps are in new format now. Instead of saving the props string,
        /// it now saves CacheServerConfig instance:
        /// 
        /// |local-cache-id               | CacheServerConfig instance
        /// |partitioned-replica-cache-id | IDictionary
        ///                               | replica-id  | CacheServerConfig instance
        /// </remarks>
        public IDictionary CacheProps
        {
            get
            {
                Hashtable cacheProps = new Hashtable();
                _rwLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    IDictionaryEnumerator en = s_caches.GetEnumerator();
                    while (en.MoveNext())
                    {
                        CacheInfo cacheInfo = (CacheInfo)en.Value;

                        cacheProps.Add(cacheInfo.CacheName, cacheInfo.CacheProps);
                    }

                    return cacheProps;
                }
                finally
                {
                    _rwLock.ReleaseReaderLock();
                }
            }
        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheConfiguration, 1)]
        public CacheServerConfig GetCacheConfiguration(string cacheId)
        {
            CacheInfo cacheInfo = GetCacheInfo(cacheId.ToLower());
            CacheServerConfig config = null;

            if (cacheInfo != null)
                config = cacheInfo.CacheProps;

            return config;
        }

        [TargetMethod(ManagementUtil.MethodName.GetNewConfiguration, 1)]
        public Alachisoft.NCache.Config.NewDom.CacheServerConfig GetNewConfiguration(string cacheId)
        {
            CacheInfo cacheInfo = GetCacheInfo(cacheId.ToLower());
            CacheServerConfig config = null;
            if (cacheInfo != null)
            {
                config = cacheInfo.CacheProps;
                return Alachisoft.NCache.Config.NewDom.DomHelper.convertToNewDom(config);
            }
            return null;
        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheServerConfiguration, 1)]
        public Config.NewDom.CacheServerConfig[] GetCacheServerConfiguration()
        {
            Config.NewDom.CacheServerConfig[] cacheServerConfigs = new Config.NewDom.CacheServerConfig[s_caches.Count];
            int index = 0;
            IDictionaryEnumerator ide = s_caches.GetEnumerator();
            while (ide.MoveNext())
            {
                CacheInfo cacheInfo = ide.Value as CacheInfo;
                cacheServerConfigs[index] = Alachisoft.NCache.Config.NewDom.DomHelper.convertToNewDom(cacheInfo.CacheProps);
                index++;
            }
            return cacheServerConfigs;
        }


        [TargetMethod(ManagementUtil.MethodName.GetCacheInfo, 1)]
        public virtual CacheInfo GetCacheInfo(string cacheId)
        {
            CacheInfo cacheInfo = null;

            if (s_caches.Contains(cacheId.ToLower()))
                cacheInfo = s_caches[cacheId.ToLower()] as CacheInfo;

            return cacheInfo;
        }

        [TargetMethod(ManagementUtil.MethodName.GetHostName, 1)]
        public string GetHostName()
        {
            string localhost = System.Environment.MachineName;
            return localhost;
        }

        [TargetMethod(ManagementUtil.MethodName.GetUpdatedCacheConfiguration, 1)]
        public CacheRegisterationInfo GetUpdatedCacheConfiguration(string cacheId, string partId, string newNode, bool isJoining)
        {
            CacheServerConfig config = null;
            ArrayList affectedNodes = new ArrayList();
            ArrayList affectedPartitions = new ArrayList();
            try
            {
                config = CacheConfigManager.GetUpdatedCacheConfig(cacheId.ToLower(), partId, newNode, ref affectedNodes, isJoining);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return new CacheRegisterationInfo(config, affectedNodes, affectedPartitions);
        }

        [TargetMethod(ManagementUtil.MethodName.GetNewUpdatedCacheConfiguration, 1)]
        public NewCacheRegisterationInfo GetNewUpdatedCacheConfiguration(string cacheId, string partId, string newNode, bool isJoining)
        {
            Alachisoft.NCache.Management.CacheRegisterationInfo oldCacheInfo = GetUpdatedCacheConfiguration(cacheId.ToLower(), partId, newNode, isJoining);
            Alachisoft.NCache.Config.NewDom.CacheServerConfig newDom = Alachisoft.NCache.Config.NewDom.DomHelper.convertToNewDom(oldCacheInfo.UpdatedCacheConfig);
            Alachisoft.NCache.Management.NewCacheRegisterationInfo newCacheInfo = new NewCacheRegisterationInfo(newDom, oldCacheInfo.AffectedNodes, oldCacheInfo.AffectedPartitions);
            return newCacheInfo;
        }

        /// <summary>
        /// Gets or sets the cache renderer.
        /// </summary>
        public CacheRenderer Renderer
        {
            get { return _renderer; }
            set { _renderer = value; }
        }

        [TargetMethod(ManagementUtil.MethodName.RegisterCache, 2)]
        public bool RegisterCache(string cacheId, Alachisoft.NCache.Config.NewDom.CacheServerConfig config, string partId, bool overwrite, bool hotApply)
        {
            Alachisoft.NCache.Config.Dom.CacheServerConfig oldDom = Alachisoft.NCache.Config.NewDom.DomHelper.convertToOldDom(config);
            return RegisterCache(cacheId.ToLower(), oldDom, partId, overwrite, hotApply);
        }

        /// <summary>
        /// Adds Server Node
        /// </summary>
        /// <param name="cacheId"></param>
        /// <param name="config"></param>
        /// <param name="partId"></param>
        /// <param name="overwrite"></param>
        /// <param name="userId"></param>
        /// <param name="password"></param>
        /// <param name="hotApply"></param>
        /// <returns></returns>

        [TargetMethod(ManagementUtil.MethodName.RegisterCache, 1)]
        public bool RegisterCache(string cacheId, CacheServerConfig config, string partId, bool overwrite, bool hotApply)
        {
                CacheServerConfig oldConfig = this.GetCacheConfiguration(cacheId);
              
            return RegisterCacheInternal(cacheId, config, partId, overwrite, hotApply, false);
        }

        [TargetMethod(ManagementUtil.MethodName.GetNodeInfo, 1)]
        public NodeInfoMap GetNodeInfo()
        {
            Hashtable nodeInfo = new Hashtable();
            nodeInfo[Channel.Cluster] = ClusterIP;
            nodeInfo[Channel.SocketServer] = ClientConfigManager.BindIP;
            return new NodeInfoMap(nodeInfo);
        }
        /// <summary>
        /// Gets or sets the socket server port.
        /// </summary>
        public static int SocketServerPort
        {
            get { return _socketServerPort; }
            set { _socketServerPort = value; }
        }


        /// <summary>
        /// returns the collection of cache objects.
        /// </summary>
        public Hashtable Caches
        {
            get
            {
                return s_caches;
            }
        }

        [TargetMethod(ManagementUtil.MethodName.RemoveCacheServerFromClientConfig, 1)]
        public void RemoveCacheServerFromClientConfig(string cacheId, string serverName)
        {
            ClientConfigManager.RemoveCacheServer(cacheId.ToLower(), serverName);
        }

        [TargetMethod(ManagementUtil.MethodName.RemoveCacheFromClientConfig, 1)]
        public void RemoveCacheFromClientConfig(string cacheId)
        {
            ClientConfigManager.RemoveCache(cacheId.ToLower());
        }

        [TargetMethod(ManagementUtil.MethodName.UpdateClientServersList, 1)]
        public void UpdateClientServersList(string cacheId, CacheServerList serversPriorityList, string serverRuntimeContext)
        {
            ClientConfigManager.UpdateServerNodes(cacheId.ToLower(), serversPriorityList, serverRuntimeContext == "1" ? RtContextValue.JVCACHE : RtContextValue.NCACHE);
        }

        [TargetMethod(ManagementUtil.MethodName.UpdateClientServersList, 2)]
        public void UpdateClientServersList(string cacheId, string[] servers, ref string xml, string clientCacheId, bool loadBalance)
        {
            ClientConfigManager.UpdateServerNodes(cacheId.ToLower(), servers, ref xml, clientCacheId, loadBalance);
        }


        [TargetMethod(ManagementUtil.MethodName.GetClientConfiguration, 1)]
        public ClientConfiguration.Dom.ClientConfiguration GetClientConfiguration(string cacheId)
        {
            return ClientConfigManager.GetClientConfiguration(cacheId.ToLower());
        }

        [TargetMethod(ManagementUtil.MethodName.UpdateClientConfiguration, 1)]
        public void UpdateClientConfiguration(string cacheId, ClientConfiguration.Dom.ClientConfiguration configuration)
        {
            ClientConfigManager.UpdateCacheConfiguration(cacheId.ToLower(), configuration);
        }

        [TargetMethod(ManagementUtil.MethodName.GetBindIP, 1)]
        public string GetBindIP()
        {
            return ClientConfigManager.BindIP;
        }

        public static Hashtable GetBinding(String cacheId)
        {
            Hashtable binding = new Hashtable();
            binding.Add("ip", _clusterIp);
            binding.Add("port", _clusterPort);
            return binding;
        }

        [TargetMethod(ManagementUtil.MethodName.GetClientConfigId, 1)]
        public int GetClientConfigId()
        {
            return ClientConfigManager.GetConfigurationId();
        }

        [TargetMethod(ManagementUtil.MethodName.GetClientNodeStatus, 1)]
        public virtual ClientNodeStatusWrapper GetClientNodeStatus(string cacheId)
        {
            string clientCacheId = string.Empty;

            ClientNodeStatus status = ClientConfigManager.GetClientNodeStatus(cacheId, out clientCacheId);
            
            return new ClientNodeStatusWrapper(status);
        }

        /// <summary>
        /// Enable logging for specified subsystem
        /// </summary>
        /// <param name="subsystem">Subsystem for which logging will be enabled</param>
        /// <param name="type">Type of logging to enable</param>  
        /// 
        [TargetMethod(ManagementUtil.MethodName.EnableLogging, 1)]
        public void EnableLogging(LoggingInfo.LoggingSubsystem subsystem, LoggingInfo.LoggingType type)
        {
            try
            {
                this.Renderer.SetLoggingStatus(subsystem, type, LoggingInfo.LogsStatus.Enable);
                AppUtil.LogEvent(RuntimeContext.CurrentContextName, subsystem.ToString() + " logging enabled successfully", EventLogEntryType.Information, EventCategories.Information, EventID.LoggingEnabled);
            }
            catch (Exception exc)
            {
                AppUtil.LogEvent(RuntimeContext.CurrentContextName, exc.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                throw;
            }
        }

        [TargetMethod(ManagementUtil.MethodName.VerifyWindowsUser, 1)]
        public virtual bool VerifyWindowsUser(string nodeName, string userName, string password)
        {
            return Alachisoft.NCache.Common.Util.SecurityUtil.VerifyWindowsUserForRole(nodeName, userName, password, WindowsBuiltInRole.Administrator);
        }

        //Method introduced to check weather a user is a windows administrator or not
        [TargetMethod(ManagementUtil.MethodName.VerfyAdministrator, 1)]
        public virtual bool VerfyAdministrator(string userName, string password)
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole
               (WindowsBuiltInRole.Administrator);

        }

        /// <summary>
        /// Disbale logging
        /// </summary>
        /// <param name="subsystem">Subsystem for which logging will be disabled</param>
        /// <param name="type">Type of logging to disable</param>
        /// 
        [TargetMethod(ManagementUtil.MethodName.DisableLogging, 1)]
        public void DisableLogging(LoggingInfo.LoggingSubsystem subsystem, LoggingInfo.LoggingType type)
        {
            try
            {
                this.Renderer.SetLoggingStatus(subsystem, type, LoggingInfo.LogsStatus.Disable);
                AppUtil.LogEvent("NCache", subsystem.ToString() + " logging disabled successfully", EventLogEntryType.Information, EventCategories.Information, EventID.LoggingDisabled);
            }
            catch (Exception exc)
            {
                AppUtil.LogEvent("NCache", exc.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                throw;
            }
        }

        [TargetMethod(ManagementUtil.MethodName.SynchronizeClientConfig, 1)]
        public void SynchronizeClientConfig()
        {
            string bindIP;

            if (ServiceConfiguration.BindToClientServerIP != null)
                bindIP = ServiceConfiguration.BindToClientServerIP.ToString();
            else
                bindIP = System.Environment.MachineName.ToLower();
            _clientserverip = bindIP;
            ClientConfigManager.BindIP = bindIP;
            try
            {
                ClientConfigManager.LoadConfiguration();
            }
            catch (Exception parserConfigurationException)
            {
                throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException(parserConfigurationException.Message);
            }
        }


        /// <summary>
        /// Update TCP cache settings that includes updated list of TCP members
        /// </summary>
        /// <param name="cacheId"></param>
        /// <param name="props"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        [TargetMethod(ManagementUtil.MethodName.ApplyCacheConfiguration, 2)]
        public bool ApplyCacheConfiguration(string cacheId, Alachisoft.NCache.Config.NewDom.CacheServerConfig props, bool hotApply)
        {
            Alachisoft.NCache.Config.Dom.CacheServerConfig oldDom = Alachisoft.NCache.Config.NewDom.DomHelper.convertToOldDom(props);
            return ApplyCacheConfiguration(cacheId, oldDom, hotApply);

        }

        /// <summary>
        /// Update TCP cache settings that includes updated list of TCP members
        /// </summary>
        /// <param name="cacheId"></param>
        /// <param name="props"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        /// 
        [TargetMethod(ManagementUtil.MethodName.ApplyCacheConfiguration, 1)]
        public bool ApplyCacheConfiguration(string cacheId, CacheServerConfig props, bool hotApply)
        {
            if (cacheId == null) throw new ArgumentNullException("cacheId");

            cacheId = cacheId.ToLower();
            CacheInfo cacheInfo = GetCacheInfo(cacheId);
            if (cacheInfo != null)
            {
                cacheInfo.CacheProps = props;
                SaveConfiguration();
                return true;
            }

            return false;
        }
        private bool RemoveDeployedAssemblies(string cacheId)
        {
            string path = AppUtil.InstallDir;
            if (path != null)
            {
                string deployedAssembliesFolder = System.IO.Path.Combine(AppUtil.DeployedAssemblyDir, cacheId);
                try
                {
                    System.IO.Directory.Delete(deployedAssembliesFolder, true);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Un-register cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        /// 
        [TargetMethod(ManagementUtil.MethodName.UnregisterCache, 1)]
        public void UnregisterCache(string cacheId, string partId, bool removeServerOnly)
        {
            if (cacheId == null) throw new ArgumentNullException("cacheId");

            cacheId = cacheId.ToLower();
            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (s_caches.Contains(cacheId.ToLower()))
                {
                    CacheInfo cacheInfo = (CacheInfo)s_caches[cacheId.ToLower()];
                    StopCache(cacheId);
                    LeasedCache cache = null;

                    if (cacheInfo != null)
                        cache = cacheInfo.Cache;

                    if (cache != null)
                        cache.Dispose();

                    s_caches.Remove(cacheId.ToLower());
                    RemoveDeployedAssemblies(cacheId); //No need to check return values; if remove then fine else not need to break the process
                    try
                    {
                        _cachePortsConfigManger.UnRegisterCachePort(cacheId.ToLower());
                    }
                    catch (Exception ex)
                    {

                    }


                }
                SaveConfiguration();
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }
        

        private Dictionary<int, ClientConfiguration.Dom.CacheServer> UpdateServerPriorityList(Dictionary<int, ClientConfiguration.Dom.CacheServer> dictionary)
        {
            Dictionary<int, ClientConfiguration.Dom.CacheServer> newDictionary = new Dictionary<int, ClientConfiguration.Dom.CacheServer>();
            int i = 0;
            foreach (KeyValuePair<int, ClientConfiguration.Dom.CacheServer> keyValuePair in dictionary)
                newDictionary.Add(i++, keyValuePair.Value);

            return newDictionary;
        }


        [TargetMethod(ManagementUtil.MethodName.ReloadSrvcConfig, 1)]
        public void ReloadSrvcConfig()
        {
            ServiceConfiguration.Load();
            if (ServiceConfiguration.HasPerfmonChanged)
            {
                ServiceConfiguration.HasPerfmonChanged = false;
            }
        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 1)]
        public virtual void StartCache(string cacheId)
        {
            StartCache(cacheId, null, null, null, null, null, null, null, false);
        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 4)]
        public virtual void StartCache(string cacheId, string partitionId, bool twoPhaseInitialization)
        {
            StartCache(cacheId, partitionId, null, null, null, null, null, null, twoPhaseInitialization);
        }

        [TargetMethod(ManagementUtil.MethodName.StartCachePhase2, 1)]
        public virtual void StartCachePhase2(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                    cacheServer.StartCachePhase2(cacheId);
            }
            catch (Exception ex)
            {
                DisposeOnException(cacheId, ex);
                throw;
            }
        }

        [TargetMethod(ManagementUtil.MethodName.StopCachesOnNode)]
        public void StopCachesOnNode(ArrayList cacheName)
        {
            try
            {
                foreach (string cacheId in cacheName)
                {
                    StopCache(cacheId, null);
                }
            }
            catch
            {
                throw;
            }
        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 5)]
        public virtual void StartCache(string cacheId, string partitionId)
        {
            StartCache(cacheId, partitionId, null, null, null, null, null, null, false);
        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 6)]
        public virtual void StartCache(string cacheId,
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
        public virtual void StartCache(string cacheId, string partitionId,
            ItemAddedCallback itemAdded,
            ItemRemovedCallback itemRemoved,
            ItemUpdatedCallback itemUpdated,
            CacheClearedCallback cacheCleared,
            CustomRemoveCallback customRemove,
            CustomUpdateCallback customUpdate,
            bool twoPhaseInitialization)
        {
            if (cacheId == null)
                throw new ArgumentNullException("cacheId");

            CacheInfo cacheInfo = null;
            Process process = null;

            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try ///For a finally {...} 
            {
                if (s_caches.Contains(cacheId.ToLower()))
                {
                    cacheInfo = (CacheInfo)s_caches[cacheId.ToLower()];
                }
                else
                    throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException("Cache: " + cacheId + " is not registered on the server.");
                lock (cacheInfo)
                {
                    try
                    {

                        if (!IsRunning(cacheId.ToLower()))
                        {
                            cacheInfo.ManagementPort = _cachePortsConfigManger.GetCachePort(cacheId);
                            process = StartCacheProcess(cacheId.ToLower(), cacheInfo.ManagementPort);
                            Thread.Sleep(2000);  // Wait for some reasonable time for process to complete
#if !NETCORE
                            if (process.HasExited)
#else
                        if (process.HasExited && process.ExitCode != 0)
#endif
                            {
                                if (process.ExitCode == 8)
                                {
                                    ResetManagementPort(cacheId);
                                    try
                                    {
                                        process = StartCacheProcess(cacheId.ToLower(), cacheInfo.ManagementPort);
                                        Thread.Sleep(2000);  // Wait for some reasonable time for process to complete
                                        if (process.HasExited)
                                            throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException("Unable to Start Separate process. Error: " + ProcessExitCodes.list[process.ExitCode]);
                                    }
                                    catch
                                    {
                                        throw;
                                    }

                                }
                                else
                                    throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException("Unable to Start Separate process. Error: " + ProcessExitCodes.list[process.ExitCode]);
                            }

                            bool started = false;
#if NETCORE
                        //Retry if cache host is not yet started.
                        for (int i = 0; i < 3; i++)
                        {
                            try
                            {
#endif
                            started = StartCacheOnCacheHost(cacheId);

#if NETCORE
                                if (started) break;
                            }
                            catch (Exception e)
                            {
                                AppUtil.LogEvent(_cacheserver, "\"" + cacheId + "\" was not started in due time.\n" + e, System.Diagnostics.EventLogEntryType.Warning, EventCategories.Warning, EventID.CacheStartError);
                                if (i > 1)
                                    throw;
                                Thread.Sleep(2000);
                            }
                        }
#endif
                            if (!started)
                                throw new Exception("specified cache id could not be started");
                            if (process.Id != 0)
                                cacheInfo.CacheProcessId = process.Id;
                           
                        }
                    }
                    catch (Exception e)
                    {
                        //bug 8175 fixed
                        string[] excessive = { "Please refer to Windows Event Logs for details" };
                        if (e.Message.Contains(excessive[0]))
                        {
                            string[] logException = e.ToString().Split(excessive, StringSplitOptions.None);
                            AppUtil.LogEvent(_cacheserver, "\"" + cacheId + "\" can not be started.\n" + logException[0] + logException[1], System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.CacheStartError);
                        }
                        else
                            AppUtil.LogEvent(_cacheserver, "\"" + cacheId + "\" can not be started.\n" + e.ToString(), System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.CacheStartError);
                        DisposeOnException(cacheId, e);

                        if (process != null && !process.HasExited && !(e is Runtime.Exceptions.TimeoutException))

                            process.Kill();
                        throw;
                    }
                }
            }
            finally
            {
             _rwLock.ReleaseReaderLock();
            }
        }
        

        [TargetMethod(ManagementUtil.MethodName.StopCache, 1)]
        public virtual void StopCache(string cacheId)
        {
            StopCache(cacheId, null);
        }
      

        

      
    
        /// <summary>
        /// Stop a cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        [TargetMethod(ManagementUtil.MethodName.StopCache, 2)]
        public virtual void StopCache(string cacheId, string partitionId)
        {
           
            if (cacheId == null) throw new ArgumentNullException("cacheId");
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try ///For a finally {...}
            {
                CacheInfo cacheInfo = GetCacheInfo(cacheId.ToLower());
                ICacheServer cacheServer = null;
                bool isCacheStopped = false;
                bool isSecurityException = false;
                if (cacheInfo != null)
                {
                    lock (cacheInfo)
                    {
                        if (!IsRunning(cacheInfo.CacheName)) return;
                        try
                        {
                            cacheServer = GetCacheServer(cacheId, false);
                            if (cacheServer == null) return;

                            for (int retries = 0; retries < 3; retries++)
                            {
                                try
                                {
                                    cacheServer.StopCache(cacheId, null);
                                    AppUtil.LogEvent(_cacheserver, "\"" + cacheId + "\"" + " stopped successfully.", EventLogEntryType.Information, EventCategories.Information, EventID.CacheStop);
                                    isCacheStopped = true;
                                    break;
                                }
                                catch (Exception e)
                                {
                                    if (e is SecurityException)
                                    {
                                        isSecurityException = true;
                                        throw e;
                                    }
                                    if (retries == 2)
                                    {
                                        isCacheStopped = true;
                                    }
                                }
                                finally
                                {
                                    if (!isSecurityException)
                                        StopCacheOnCacheHost(cacheId);

                                    if (isCacheStopped)
                                    {
                                        if (cacheServer != null)
                                        {
                                            cacheServer.Dispose();
                                            cacheServer = null;
                                        }
                                        if (cacheInfo != null)
                                        {

                                            cacheInfo.CacheProcessId = 0;
                                            if (cacheInfo.CacheServer != null)
                                            {
                                                cacheInfo.CacheServer.Dispose();
                                                cacheInfo.CacheServer = null;
                                            }
                                            if (cacheInfo.Service != null)
                                            {
                                                cacheInfo.Service.Dispose();
                                                cacheInfo.Service = null;
                                            }
                                        }
                                    }
                                    isSecurityException = false;
                                }
                            }
                        }
                        catch (Exception e)
                        {

                            if (e is SecurityException)
                                AppUtil.LogEvent("NCache", "\"" + cacheId + "\" can not be stopped. You do not have permissions to perform this operation", System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.SecurityError);
                            throw;
                        }
                    }
                }
                else
                {
                    throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException(ErrorCodes.CacheInit.CACHE_ID_NOT_REGISTERED, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_ID_NOT_REGISTERED));

                }
            }
            finally
            {
                _rwLock.ReleaseReaderLock ();

            }
        }

       
        [TargetMethod(ManagementUtil.MethodName.GetShutdownTimeout, 1)]
        public int GetShutdownTimeout()
        {
            int shutdownTimeout = 180;
            int blockTimeout = 3;

            string expMsg = GracefulTimeout.GetGracefulShutDownTimeout(ref shutdownTimeout, ref blockTimeout);
            if (expMsg != null)
                AppUtil.LogEvent(_cacheserver, expMsg, System.Diagnostics.EventLogEntryType.Warning, EventCategories.Warning, EventID.GeneralInformation);
            return shutdownTimeout;
        }

        public void StopAllCaches(CacheStopReason reason)
        {
            ArrayList caches = new ArrayList();
            ArrayList cacheInfos = new ArrayList();
            foreach (DictionaryEntry de in s_caches)
            {
                //CacheInfo should not be null
                if (de.Value != null)
                {
                    string cacheName = ((de.Value) as CacheInfo).CacheName;

                    try
                    {
                        StopCache(cacheName, null);
                    }
                    catch (Exception ex)
                    {
                        AppUtil.LogEvent($"Could not stop cache {cacheName} due to {ex.ToString()} ", EventLogEntryType.Warning);
                    }
                }
            }
        }

        [TargetMethod(ManagementUtil.MethodName.StopAllCaches, 1)]
        public void StopAllCaches()
        {
            for (IDictionaryEnumerator idenum = s_caches.GetEnumerator(); idenum.MoveNext();)
            {
                CacheInfo cacheInfo = (CacheInfo)idenum.Current;
                if (cacheInfo != null)
                {
                    StopCache(cacheInfo.CacheName, null);
                }
            }
        }

        private void StopCachesOnExpiry(object info)
        {
            StopAllCaches(CacheStopReason.Expired);
        }

        private void OnLicenseExpiration()
        {
            StopAllCaches(CacheStopReason.Expired);
        }

        public virtual int GetProcessID(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                {
                    return cacheServer.GetProcessID(cacheId);
                }
            }
            catch (Exception ex)
            {
                DisposeOnException(cacheId, ex);
                return 0;
            }
            return 0;
        }

        public virtual void StopCacheInstance(string cache, CacheInfo cacheInfo, CacheServer.CacheStopReason reason)
        {
            try
            {
                if (IsRunning(cache))
                {
                    ICacheServer cacheServer = GetCacheServer(cacheInfo.CacheName, false);
                    if (cacheServer != null)
                    {
                        cacheServer.StopCacheInstance(cache, cacheInfo, reason);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException("Could not stop cache " + cacheInfo.CacheName);
            }
        }
        /// <summary>
        /// Load all the config sections from the configuration file.
        /// </summary>
        public void LoadConfiguration()
        {
            CacheInfo cacheInfo = null;
            try
            {
                CacheServerConfig[] configs = CacheConfigManager.GetConfiguredCaches();
                Hashtable runningcaches = null;
                foreach (CacheServerConfig config in configs)
                {
                    ///Until we completely move to using dom based configuration
                    ///we have to convert it to string props
                    string props = GetProps(config);
                    // all but the listed cache types are loaded in s_caches table.

                    string cacheId = config.Name.ToLower();

                    if (!s_caches.Contains(cacheId))
                    {
                        cacheInfo = new CacheInfo();
                        cacheInfo.CacheName = cacheId;
                        s_caches.Add(cacheId, cacheInfo);                        

                    }
                    else
                    {
                        cacheInfo = s_caches[cacheId] as CacheInfo;
                    }
                    cacheInfo.CacheProps = config;
                }
            }
            catch (Exception e)
            {
                string msg = String.Format("CacheServer failed to load configuration information, Error {0}",
                    e.Message);
                AppUtil.LogEvent(msg, EventLogEntryType.Warning);
            }
        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheName, 1)]
        public virtual string GetCacheName(int port)
        {            
            CacheService service = null;
            ICacheServer cacheServer = null;
            string name = null;
            try
            {
                if (port > 0)
                {
                    service = new CacheRPCService(ServiceConfiguration.BindToIP.ToString(), port);
                    cacheServer = service.GetCacheServer(new TimeSpan(0, 0, 30));
                    if (cacheServer != null)
                        name = cacheServer.GetCacheName(port);
                }
            }
            catch (Exception ex)
            {
            }
            return name.ToLower();
        }

        /// <summary>
        /// Get string props representation of config
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        internal static string GetProps(CacheServerConfig config)
        {
            /// This is until we change the use of properties in Cache
            ///from props stirng or hashtable to Dom
            Hashtable table = ConfigConverter.ToHashtable(config);
            string props = ConfigReader.ToPropertiesString(table);
            return props;
        }
        /// <summary>
        /// Save caches to configuration
        /// </summary>
        static private void SaveConfiguration()
        {
            try
            {
                CacheConfigManager.SaveConfiguration(s_caches, null);
            }
            catch (Exception e)
            {
                string msg = String.Format("CacheServer failed to save configuration information, Error {0}",
                    e.Message);
                AppUtil.LogEvent(msg, EventLogEntryType.Warning);
            }
        }
        /// <summary>
        /// Detect and return all the available NICs on this machine
        /// </summary>
        [TargetMethod(ManagementUtil.MethodName.DetectNICs, 1)]
        public Hashtable DetectNICs()
        {
            Hashtable connectedNICs = new Hashtable();
            try
            {
#if !NETCORE
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("Select * from Win32_NetworkAdapterConfiguration WHERE IPEnabled=True");

                foreach (ManagementObject mo in searcher.Get())
                {
                    string[] ipAddresses = mo.GetPropertyValue("IPAddress") as string[];

                    foreach (string ipAddress in ipAddresses)
                    {
                        System.Net.IPAddress Address;
                        if (System.Net.IPAddress.TryParse(ipAddress, out Address))
                            if (!connectedNICs.ContainsKey(ipAddress) && Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                connectedNICs.Add(ipAddress, mo.GetPropertyValue("Description"));
                            }
                    }
                }
#elif NETCORE
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (/*ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||*/ ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                connectedNICs.Add(ip.Address.ToString(), ni.Description);
                            }
                        }
                    }
                }
#endif
                connectedNICs.Add("127.0.0.1", "Loopback Address");
                return connectedNICs;
            }

            catch (Exception)
            {

            }

            return connectedNICs;
        }

        [TargetMethod(ManagementUtil.MethodName.BindToIP, 1)]
        public void BindToIP(BindedIpMap bindIPMap)
        {
            Hashtable bindTable = bindIPMap.Map;
            if (bindTable.Count == 0)
                return;
            string path = ServiceFilePath;
            if (File.Exists(path))
            {
                XmlDocument document = new XmlDocument();
                document.Load(path);

                string xPath = "/configuration/appSettings/add";

                string[] elementKeys = { "NCacheServer.BindToIP", "NCacheServer.BindToClientServerIP" };

                string[] elementValues = { bindTable.ContainsKey(Channel.Cluster) ? (string)bindTable[Channel.Cluster] : "", bindTable.ContainsKey(Channel.SocketServer) ? (string)bindTable[Channel.SocketServer] : "" };

                for (int i = 0; i < elementKeys.Length; i++)
                {
                    this.ChangeAttribute(document, xPath, elementKeys[i], elementValues[i]);
                }
                document.Save(path);
            }
        }

        /// <summary>
        /// Changes the value of 'Value' attribute in app.config. If the attribute does not exists, it creates a new one
        /// </summary>
        /// <param name="document">document object</param>
        /// <param name="path">path of 'Key' attribute</param>
        /// <param name="key">key name</param>
        /// <param name="newValue">new value of 'Value' attribute</param>
        private void ChangeAttribute(XmlDocument document, string path, string key, string newValue)
        {
            string format = "{0}[@key='{1}']";
            string xPath = String.Format(format, path, key);
            XmlNode node = document.SelectSingleNode(xPath);
            if (node == null)
            {
                //create a new node
                node = document.CreateNode(XmlNodeType.Element, "add", "");
                //set its attributes
                ((XmlElement)node).SetAttribute("key", key);
                if (newValue == string.Empty && ClusterIP != null)
                    ((XmlElement)node).SetAttribute("value", ClusterIP);
                else
                    ((XmlElement)node).SetAttribute("value", newValue);
                //append it at the end of appSettings element
                XmlNodeList list = document.GetElementsByTagName("appSettings");
                list[0].AppendChild(node);
            }
            else
            {
                if (newValue == string.Empty && ClusterIP != null && key == "NCacheServer.BindToIP")
                {
                    node.Attributes["value"].Value = ClusterIP;
                }
                else if (newValue == string.Empty && _clientserverip != null && key == "NCacheServer.BindToClientServerIP")
                {
                    node.Attributes["value"].Value = _clientserverip;
                }
                else
                    node.Attributes["value"].Value = newValue;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.BindedIp, 1)]
        public BindedIpMap BindedIp()
        {
            Hashtable bindedIps = new Hashtable(2);
            string path = ServiceFilePath;
            // Read the value of a specific key from the config file:
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNodeList list = doc.GetElementsByTagName("appSettings");

            foreach (XmlNode node in list[0].ChildNodes)
            {
                try
                {
                    // To skip the comment tags
                    if (node.NodeType != XmlNodeType.Comment)
                    {
                        // Get the value of 'bindToIP' tag   
                        if (node.Attributes[0].Value == "NCacheServer.BindToIP")

                        {
                            bindedIps[Channel.Cluster] = node.Attributes[1].Value;
                        }
                        else if (node.Attributes[0].Value == "NCacheServer.BindToClientServerIP")
                        {
                            bindedIps[Channel.SocketServer] = node.Attributes[1].Value;
                        }
                    }
                }
                catch (Exception) { }
            }
            return new BindedIpMap(bindedIps);
        }

        /// <summary>
        /// Return service config file path
        /// </summary>
        private string ServiceFilePath
        {
            get
            {
                string path = Process.GetCurrentProcess().MainModule.FileName + ".config";
                if (File.Exists(path)) return path;

                //else, get the path of config file from Windows Registry:
                path = AppUtil.InstallDir;
                if (path == null || path == string.Empty)
                {
                    throw new Exception("Missing installation folder information");
                }
                string binDirectory = path + Path.DirectorySeparatorChar + "bin";
                string serviceDirectory = binDirectory + Path.DirectorySeparatorChar + "service";
#if NETCORE
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    return (serviceDirectory + Path.DirectorySeparatorChar + ServiceConfiguration.NETCORE_SERVICE_WIN+".config");
                else
                    return (serviceDirectory + Path.DirectorySeparatorChar + ServiceConfiguration.NETCORE_SERVICE_UNIX + ".config");
#else
                return (serviceDirectory + Path.DirectorySeparatorChar + "Alachisoft.NCache.Service.exe.config");
#endif
            }
        }

        /// <summary>
        /// Gets the Max port number, among all the ports of registered caches on this machine
        /// </summary>
        /// <returns>Max cluster port</returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.GetMaxPort, 1)]
        public int GetMaxPort()
        {
            IDictionary regCaches = CacheProps;
            IDictionaryEnumerator ie = regCaches.GetEnumerator();

            CacheConfig cfg = null;
            int maxPort = 7800, loop = 1;

            while (ie.MoveNext())
            {
                if (ie.Value is CacheServerConfig)
                {
                    cfg = CacheConfig.FromConfiguration(ie.Value as CacheServerConfig);
                }
                else if (ie.Value is Hashtable)
                {
                    IDictionaryEnumerator ide = ((Hashtable)ie.Value).GetEnumerator();
                    while (ide.MoveNext())
                    {
                        cfg = CacheConfig.FromConfiguration(ie.Value as CacheServerConfig);
                        break;
                    }
                }

                if (cfg.ClusterPort != 0)
                {
                    if (loop == 1)
                    {
                        maxPort = cfg.ClusterPort + cfg.ClusterPortRange;
                        loop++;
                    }
                    else if (maxPort < (cfg.ClusterPort + cfg.ClusterPortRange))
                    {
                        maxPort = cfg.ClusterPort + cfg.ClusterPortRange;
                    }
                }
            }
            return maxPort;
        }



        /// <summary>
        /// Gets the Max Socket port number, among all the ports of registered caches on this machine
        /// </summary>
        /// <returns>Max Socket port</returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.GetMaxSocketPort, 1)]
        public int GetMaxSocketPort()
        {
            return 0;
        }

        /// <summary>
        /// Checks if the current cache is a Cluster cache or not, used in NCache UnReg cache tool as now UnReg is only applicable to cluster caches only
        /// </summary>
        /// <returns>true if Cluster Cache</returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.IsClusteredCache, 1)]

        public CacheStatusOnServerContainer IsClusteredCache(string cacheId)
        {
            CacheStatusOnServer result = CacheStatusOnServer.Unregistered;
            CacheStatusOnServerContainer status = new CacheStatusOnServerContainer();
            if (cacheId != null && cacheId != string.Empty)
            {
                CacheInfo cacheInfo = GetCacheInfo(cacheId);
                if (cacheInfo != null)
                {
                    if (cacheInfo.CacheProps.Cluster != null)
                    {
                        if (cacheInfo.CacheProps.Cluster.CacheType == "replicated-server" ||
                        cacheInfo.CacheProps.Cluster.CacheType == "partitioned-server" )
                            result = CacheStatusOnServer.ClusteredCache;
                    }
                    else
                    {
                        result = CacheStatusOnServer.LocalCache;
                    }
                }
                status.cacheStatus = result;
                return status;
            }
            throw new ArgumentException("cacheId");
        }

        /// <summary>
        /// Checks whether the specified port is available (non-conflicting) or not
        /// </summary>
        /// <param name="port">Cluster port</param>
        /// <returns>'true' if the port is available, otherwise 'false'</returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.IsPortAvailable, 1)]
        public bool IsPortAvailable(int port, string cacheName)
        {
            IDictionary regCaches = CacheProps;
            IDictionaryEnumerator ie = regCaches.GetEnumerator();

            CacheConfig cfg;
            bool isAvailable = true;

            while (ie.MoveNext())
            {
                if (ie.Value is CacheServerConfig)
                {
                    cfg = CacheConfig.FromConfiguration(ie.Value as CacheServerConfig);
                    if (cfg.CacheId.ToLower().Equals(cacheName.ToLower()) && port == cfg.ClusterPort)
                    {
                        continue;
                    }
                    for (int i = 0; i < cfg.ClusterPortRange; i++)
                    {
                        if (port == cfg.ClusterPort + i)
                        {
                            isAvailable = false;
                            break;
                        }
                    }
                }
                else if (ie.Value is Hashtable)
                {
                    Hashtable partitionedTable = ie.Value as Hashtable;
                    IDictionaryEnumerator ide = partitionedTable.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        cfg = CacheConfig.FromConfiguration(ie.Value as CacheServerConfig);
                        if (cfg.CacheId.ToLower().Equals(cacheName.ToLower()) && port == cfg.ClusterPort)
                        {
                            continue;
                        }
                        for (int i = 0; i < cfg.ClusterPortRange; i++)
                        {
                            if (port == cfg.ClusterPort + i)
                            {
                                isAvailable = false;
                                break;
                            }
                        }
                    }
                }
                if (isAvailable == false)
                    break;

            }

            return isAvailable;
        }


        [TargetMethod(ManagementUtil.MethodName.NodeIsAllowed, 1)]
        public bool NodeIsAllowed(int port, string id)
        {
            IDictionary regCaches = CacheProps;
            IDictionaryEnumerator ie = regCaches.GetEnumerator();

            CacheConfig cfg = null;
            bool isAllowed = true;

            while (ie.MoveNext())
            {
                if (ie.Value is CacheServerConfig)
                {
                    cfg = CacheConfig.FromConfiguration(ie.Value as CacheServerConfig);
                }
                else if (ie.Value is Hashtable)
                {
                    foreach (DictionaryEntry de in ((Hashtable)ie.Value))
                    {
                        cfg = CacheConfig.FromConfiguration(ie.Value as CacheServerConfig);
                        break;
                    }
                }

                if (cfg != null)
                {
                    if (cfg.ClusterPort == port && cfg.CacheId != id)
                        isAllowed = false;
                }
            }

            return isAllowed;
        }
        
        /// <summary>
        /// Starts monitoring the client activity.
        /// </summary>
        /// 
        [TargetMethod(ManagementUtil.MethodName.StartMonitoringActivity, 1)]
        public void StartMonitoringActivity()
        {
            try
            {
                ServerMonitor.StartMonitoring();
                AppUtil.LogEvent("Activity monitoring is started", EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                AppUtil.LogEvent("An error occurred while starting activity monitoring " + e.ToString(), EventLogEntryType.Error);
                throw;
            }

        }
        /// <summary>
        /// Stops monitoring client activity.
        /// </summary>
        /// 
        [TargetMethod(ManagementUtil.MethodName.StopMonitoringActivity, 1)]
        public void StopMonitoringActivity()
        {
            try
            {
                ServerMonitor.StopMonitoring();
                AppUtil.LogEvent("Activity monitoring is stopped", EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                AppUtil.LogEvent("An error occurred while stopping activity monitoring " + e.ToString(), EventLogEntryType.Error);
                throw;
            }
        }

        /// <summary>
        /// Publishes the observed client activity into a file.
        /// </summary>
        /// 
        [TargetMethod(ManagementUtil.MethodName.PublishActivity, 1)]
        public void PublishActivity()
        {
            Logger logger = new Logger();

            try
            {
                logger.Initialize("ActivityLog", "ActivityLogs");
                logger.WriteSingleLine("Monitoring start_time :" + ServerMonitor.StartTime.ToString(Logger.TIME_FORMAT) + " end_time :" + ServerMonitor.EndTime.ToString(Logger.TIME_FORMAT));

                Hashtable activityTable = ServerMonitor.GetCompletedClientActivity();
                if (activityTable != null)
                {
                    logger.WriteClientActivities(activityTable, true);
                }

                activityTable = ServerMonitor.GetCurrentClientActivity();
                if (activityTable != null)
                {
                    logger.WriteClientActivities(activityTable, false);
                }
                ServerMonitor.Reset();
            }
            catch (Exception e)
            {
                AppUtil.LogEvent("An error occurred while logging client activity " + e.ToString(), EventLogEntryType.Error);
                throw;
            }
            finally
            {
                logger.Close();
            }
        }

        #region IMonitorServer Members
        [TargetMethod(ManagementUtil.MethodName.GetCacheStatistics, 1)]
        public virtual CacheNodeStatistics[] GetCacheStatistics(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                {
                    return cacheServer.GetCacheStatistics(cacheId);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return null;
        }

        /// <summary>
        /// Gets the list of all the configured cache servers in a clustered cache irrespective
        /// of running or stopped.
        /// </summary>
        /// <param name="cacheId"></param>
        /// <returns></returns>
        [TargetMethod(ManagementUtil.MethodName.GetCacheServers, 1)]
        public virtual Node[] GetCacheServers(string cacheId)
        {
            if (cacheId == null)
                throw new ArgumentNullException("cacheId");

            List<Node> serverNodes = new List<Node>();
            try
            {
                _rwLock.AcquireReaderLock(Timeout.Infinite);
                CacheInfo cacheInfo = GetCacheInfo(cacheId);
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

        [TargetMethod(ManagementUtil.MethodName.GetCacheClients, 1)]
        public virtual List<Alachisoft.NCache.Common.Monitoring.ClientNode> GetCacheClients(string cacheId)
        {
            ICacheServer cacheServer = GetCacheServer(cacheId,false);
            List<Alachisoft.NCache.Common.Monitoring.ClientNode> clients = new List<Alachisoft.NCache.Common.Monitoring.ClientNode>();
            if (cacheServer != null)
                clients = cacheServer.GetCacheClients(cacheId);
            return clients;
        }

        [TargetMethod(ManagementUtil.MethodName.GetClientProcessStats, 1)]
        public virtual List<Alachisoft.NCache.Common.Monitoring.ClientProcessStats> GetClientProcessStats(string cacheId)
        {
            List<Alachisoft.NCache.Common.Monitoring.ClientProcessStats> clients = new List<Alachisoft.NCache.Common.Monitoring.ClientProcessStats>();
            ICacheServer cacheServer = GetCacheServer(cacheId, false);
            if (cacheServer != null)
                clients = cacheServer.GetClientProcessStats(cacheId);
            return clients;
        }

        /// <summary>
        /// Gets the list of all configured caches on this server.
        /// </summary>
        /// <returns></returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.GetAllConfiguredCaches, 1)]
        public virtual ConfiguredCacheInfo[] GetAllConfiguredCaches()
        {
            ConfiguredCacheInfo[] configuredCaches = new ConfiguredCacheInfo[s_caches.Count];

            try
            {
                _rwLock.AcquireReaderLock(Timeout.Infinite);
                IDictionaryEnumerator ide = s_caches.GetEnumerator();
                int i = 0;
                while (ide.MoveNext())
                {
                    CacheInfo cacheInfo = ide.Value as CacheInfo;
                    if (cacheInfo != null)
                    {
                        ConfiguredCacheInfo configuredCache = new ConfiguredCacheInfo();
                        configuredCache.CacheId = cacheInfo.CacheProps.Name;
                        if (!cacheInfo.CacheProps.InProc)
                            configuredCache.IsRunning = IsRunning(cacheInfo.CacheProps.Name);
                        if (configuredCache.IsRunning)
                        {
                            configuredCache.ProcessID = cacheInfo.CacheProcessId;
                            if (configuredCache.ProcessID <= 0)
                                configuredCache.ProcessID = GetCacheProcessID(configuredCache.CacheId);
                        }
                        configuredCache.ManagementPort = cacheInfo.ManagementPort;
                        configuredCache.DataCapacity = cacheInfo.CacheProps.Storage.Size;
                        configuredCache.CachePropString = GetProps(cacheInfo.CacheProps);

                        if (cacheInfo.CacheProps.CacheType == "clustered-cache")
                        {
                            if (cacheInfo.CacheProps.Cluster != null)
                            {
                                switch (cacheInfo.CacheProps.Cluster.Topology)
                                {                                    
                                    case "mirror-server":
                                        configuredCache.Topology = CacheTopology.Mirror;
                                        break;
                                }
                            }
                        }
                        else if (cacheInfo.CacheProps.CacheType == "local-cache")
                        {
                            configuredCache.Topology = CacheTopology.Local;
                        }
                        configuredCaches[i] = configuredCache;
                        i++;
                    }
                }
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
            return configuredCaches;
        }


        [TargetMethod(ManagementUtil.MethodName.GetCacheStatistics2, 1)]
        public Alachisoft.NCache.Caching.Statistics.CacheStatistics GetCacheStatistics2(string cacheId)
        {
            if (cacheId == null)
                throw new ArgumentNullException("cacheId");

            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                {
                    return cacheServer.GetStatistics(cacheId);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return null;
        }

        #endregion
        [TargetMethod(ManagementUtil.MethodName.GetClusterIP, 1)]
        public string GetClusterIP()
        {
            return ClusterIP;
        }

        [TargetMethod(ManagementUtil.MethodName.GetLocalCacheIP, 1)]
        public string GetLocalCacheIP()
        {
            return LocalCacheIP;
        }

        [TargetMethod(ManagementUtil.MethodName.SetLocalCacheIP)]
        public void SetLocalCacheIP(string ip)
        {
            LocalCacheIP = ip;
        }
        
        [TargetMethod(ManagementUtil.MethodName.GetAssembly, 1)]
        public byte[] GetAssembly(string cacheName, string fileName)
        {
            byte[] asmData = null;
            if (AppUtil.InstallDir != null)
            {
                FileStream fs = null;
                try
                {
                    string path = AppUtil.DeployedAssemblyDir + cacheName + Path.DirectorySeparatorChar + fileName;
                    fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                    asmData = new byte[fs.Length];
                    fs.Read(asmData, 0, asmData.Length);
                    fs.Flush();
                }
                catch (Exception)
                {
                    return asmData;
                }
                finally
                {
                    if (fs != null)
                    {
                        fs.Close();
                        fs = null;
                    }
                }
            }
            return asmData;
        }

        [TargetMethod(ManagementUtil.MethodName.CacheProps, 1)]
        IDictionary ICacheServer.CacheProps()
        {
            throw new NotImplementedException();
        }

        [TargetMethod(ManagementUtil.MethodName.GetSocketServerPort, 1)]
        public int GetSocketServerPort()
        {
            return _socketServerPort;
        }
        [TargetMethod(ManagementUtil.MethodName.CanApplyHotConfig, 1)]
        public virtual string CanApplyHotConfiguration(string cacheId, CacheServerConfig config)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                    return cacheServer.CanApplyHotConfiguration(cacheId, config);
                else
                    return null;
            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
            }

            return null;
        }

      

        [TargetMethod(ManagementUtil.MethodName.MakeCacheActive)]
        public void MakeCacheActive(string cacheId, bool active)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                {
                    cacheServer.MakeCacheActive(cacheId, active);
                }
            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
            }
        }
        
        [TargetMethod(ManagementUtil.MethodName.ClearCacheContent)]
        public virtual void ClearCacheContent(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                {
                    cacheServer.ClearCacheContent(cacheId);
                }
            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
            }
        }

        public bool StartCacheOnCacheHost(string cacheId)
        {
            int retryCount = 3;
            int i = 0;
            try
            {
                int timeout = 80; 
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                for (; i < retryCount; i++)
                {
                    try
                    {
                        ICacheServer cacheServer = GetCacheServer(cacheId, true);
                        if (cacheServer != null)
                        {
                            cacheServer.StartCache(cacheId, null);
                            break;
                        }
                    }
                    catch (Runtime.Exceptions.TimeoutException ex)
                    {
                        AppUtil.LogEvent("Timeout while connectiong to server: " + ex.ToString(), EventLogEntryType.Error);

                        if (ServiceConfiguration.RelicaJoinDelay == 0)
                            throw;
                    }
                    catch (SocketException ex)
                    {
                        AppUtil.LogEvent("Socket exception occured : " + ex.ToString() + " process retry number: " + i.ToString(), EventLogEntryType.Error);
                    }
                    catch (ChannelException ex)
                    {
                        AppUtil.LogEvent("Channel exception occured : " + ex.ToString() + " process retry number: " + i.ToString(), EventLogEntryType.Error);
                        Thread.Sleep(2000);
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    if (stopWatch.ElapsedMilliseconds >= (timeout * 1000))
                        throw new Runtime.Exceptions.TimeoutException("Cache could not be started. Failed to contact cache process");
                }
            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
                throw;
            }
            return true;
        }


        [TargetMethod(ManagementUtil.MethodName.StopCacheOnHost, 1)]
        public virtual void StopCacheOnCacheHost(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                    cacheServer.StopCacheOnCacheHost(cacheId);
            }
            catch (Exception ex)
            {
                DisposeOnException(cacheId, ex);
                throw;
            }
        }

        [TargetMethod(ManagementUtil.MethodName.IsRunning)]
        public virtual bool IsRunning(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                {
                    return cacheServer.IsRunning(cacheId);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }
        [TargetMethod(ManagementUtil.MethodName.IsUnderStateTransfer)]
        public virtual bool IsUnderStateTransfer(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                {
                    return cacheServer.IsUnderStateTransfer(cacheId);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        [TargetMethod(ManagementUtil.MethodName.GetStatistics)]
        public virtual CacheStatistics GetStatistics(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                {
                    return cacheServer.GetStatistics(cacheId);
                }
            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
            }
            return null;
        }

        internal string Decrypt(byte[] cypherText)
        {
            return Common.EncryptionUtil.Decrypt(cypherText);
        }

        /// <summary>
        /// Gets the status of NCache on this node.
        /// </summary>
        /// <returns>The ServerStatus.</returns>
        [TargetMethod(ManagementUtil.MethodName.GetCacheStatus, 1)]
        public virtual StatusInfo GetCacheStatus(string cacheId, string partitionId)
        {
            StatusInfo status = new StatusInfo();
            if (!string.IsNullOrEmpty(cacheId))
            {
                CacheInfo cacheInfo = this.GetCacheInfo(cacheId);
                if (cacheInfo != null)
                {
                    status.Status = CacheStatus.Registered;
                    if (cacheInfo.CacheProps != null)
                    {
                        status.ConfigID = cacheInfo.CacheProps.ConfigID;
                        status.ConfigVersion = cacheInfo.CacheProps.ConfigVersion;
                    }
                    try
                    {
                        ICacheServer cacheServer = GetCacheServer(cacheId, false);
                        if (cacheServer != null)
                            status = cacheServer.GetCacheStatus(cacheId, partitionId);
                    }
                    catch (Alachisoft.NCache.Runtime.Exceptions.ManagementException ex)
                    {
                    }
                    catch (Alachisoft.NCache.Runtime.Exceptions.TimeoutException ex)
                    {
                    }
                    catch (Exception ex)
                    {
                        DisposeOnException(cacheId, ex);
                    }
                }
            }
            return status;

        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheCount)]
        public virtual long GetCacheCount(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                    return cacheServer.GetCacheCount(cacheId);
            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
            }

            return 0;
        }

        [TargetMethod(ManagementUtil.MethodName.BalanceDataloadOnCache)]
        public virtual void BalanceDataloadOnCache(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                    cacheServer.BalanceDataloadOnCache(cacheId);
            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
            }
        }

        [TargetMethod(ManagementUtil.MethodName.IsCacheRegistered)]
        public bool IsCacheRegistered(string cacheId)
        {
            if (cacheId != null)
            {
                CacheInfo cacheInfo = GetCacheInfo(cacheId);
                if (cacheInfo != null)
                {
                    if (cacheInfo.CacheName != null)
                        return true;
                }
            }

            return false;
        }

        [TargetMethod(ManagementUtil.MethodName.GetSnmpPorts)]
        public Hashtable GetSnmpPorts()
        {
            return new Hashtable();
        }

        [TargetMethod(ManagementUtil.MethodName.StopServer)]
        public void StopServer()
        {
            ReleaseServiceObject();

        }

        [TargetMethod(ManagementUtil.MethodName.GetServerPlatform)]
        public string GetServerPlatform()
        {
            return Alachisoft.NCache.Common.ServerPlatform.isDotNet;
        }

        [TargetMethod(ManagementUtil.MethodName.GetServerLicenseInfo)]
        public ServerLicenseInfo GetServerLicenseInfo()
        {
            licenseInfo = new ServerLicenseInfo();
            lastLicenseInfoAccessTime = DateTime.Now;

            return licenseInfo;
        }

        [TargetMethod(ManagementUtil.MethodName.LogBackingSourceStatus)]
        public virtual void LogBackingSourceStatus(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer == null) throw new Runtime.Exceptions.ManagementException("Specified cacheId: " + cacheId + " is not registered.");

                if (!(cacheServer.IsRunning(cacheId))) throw new Runtime.Exceptions.ManagementException("Specified cacheId: " + cacheId + " is not started.");

                cacheServer.LogBackingSourceStatus(cacheId);

            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
            }
        }


        



        /// <summary>
        /// Gets the list of servers which are up and are part of a clustered cache.
        /// </summary>
        /// <param name="cacheId"></param>
        /// <returns></returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.GetRunningCacheServers, 1)]
        public List<ServerNode> GetRunningCacheServers(string cacheId)
        {
            if (cacheId == null)
                throw new ArgumentNullException("cacheId");

            List<ServerNode> serverNodes = new List<ServerNode>();

            try
            {
                _rwLock.AcquireReaderLock(Timeout.Infinite);

                CacheInfo cacheInfo = GetCacheInfo(cacheId.ToLower());
                if (cacheInfo != null)
                {
                    if (IsRunning(cacheInfo.CacheName) && cacheInfo.CacheProps.CacheType == "clustered-cache")
                    {
                        ClusterCacheStatistics stats = GetStatistics(cacheInfo.CacheName) as ClusterCacheStatistics;
                        if (stats != null)
                        {
                            foreach (NodeInfo node in stats.Nodes)
                            {
                                ServerNode serverNode = new ServerNode();
                                serverNode.Address = node.Address;
                                serverNode.IsReplica = node.IsStartedAsMirror;
                                serverNode.InProcInstance = node.IsInproc;
                                if (node.RendererAddress != null)
                                    serverNode.ClientPort = node.RendererAddress.Port;

                                if(node.IsStartedAsMirror)
                                {
                                    serverNode.Name = node.Address.IpAddress.ToString() + "-replica";
                                }

                                if (node.IsStartedAsMirror && stats.Nodes.Count > 2)
                                {
                                    foreach (NodeInfo node2 in stats.Nodes)
                                    {
                                        if (node2.SubgroupName == node.SubgroupName && node2.Address.IpAddress.ToString() != node.Address.IpAddress.ToString())
                                        {
                                            serverNode.NodeAt = node2.Address.IpAddress.ToString();
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    serverNode.NodeAt = node.Address.IpAddress.ToString();
                                }

                                serverNodes.Add(serverNode);
                            }
                        }

                    }
                }
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
            return serverNodes;
        }


        /// <summary>
        /// Gets the basic cache related information for given cache id.
        /// </summary>
        /// <param name="cacheId">Name of the cache.</param>
        /// <returns></returns>
        [TargetMethod(ManagementUtil.MethodName.GetCacheConfigurationInfo, 1)]
        public ConfiguredCacheInfo GetCacheConfigurationInfo(string cacheId)
        {
            if (cacheId == null)
                throw new ArgumentNullException("cacheId");

            ConfiguredCacheInfo configuredCache = null;
            CacheInfo cacheInfo = GetCacheInfo(cacheId);
            try
            {
                _rwLock.AcquireReaderLock(Timeout.Infinite);

                if (cacheInfo != null)
                {
                    configuredCache = new ConfiguredCacheInfo();
                    configuredCache.CacheId = cacheInfo.CacheProps.Name;
                    configuredCache.IsRunning = IsRunning(cacheInfo.CacheProps.Name);
                    configuredCache.DataCapacity = cacheInfo.CacheProps.Storage.Size;
                    configuredCache.CachePropString = GetProps(cacheInfo.CacheProps);
                    configuredCache.ManagementPort = cacheInfo.ManagementPort;

                    if (cacheInfo.CacheProps.CacheType == "clustered-cache")
                    {
                        if (cacheInfo.CacheProps.Cluster != null)
                        {
                            switch (cacheInfo.CacheProps.Cluster.Topology)
                            {
                                case "mirror-server":
                                    configuredCache.Topology = CacheTopology.Mirror;
                                    break;

                            }
                        }
                    }
                    else if (cacheInfo.CacheProps.CacheType == "local-cache")
                    {
                        configuredCache.Topology = CacheTopology.Local;

                    }
                }
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
            return configuredCache;
        }


        [TargetMethod(ManagementUtil.MethodName.GetServerMappingForConfig)]
        public Hashtable GetServerMappingForConfig()
        {
            Hashtable serverIPMapping = new Hashtable();
            Mapping managementMapping = GenerateMapping("NCacheServer.MgmtEndPoint", "NCacheServer.ManagementPort", "NCacheServer.BindToIP");
            if (managementMapping != null)
            {
                serverIPMapping.Add("management-ip-mapping", managementMapping);
            }
            Mapping clientMapping = GenerateMapping("NCacheServer.ServerEndPoint", "NCacheServer.Port", "NCacheServer.BindToClientServerIP");
            if (clientMapping != null)
            {
                serverIPMapping.Add("client-ip-mapping", clientMapping);
            }
            return serverIPMapping;
        }

        private Mapping GenerateMapping(string mappingKey, string portKey, string ipKey)
        {
            try
            {
                string mappingString = "";

                if (mappingKey.Equals("NCacheServer.MgmtEndPoint"))
                    mappingString = ServiceConfiguration.ManagementEndPoint;
                else if (mappingKey.Equals("NCacheServer.ServerEndPoint"))
                    mappingString = ServiceConfiguration.ServerEndPoint;

                int port = 0;
                if (portKey.Equals("NCacheServer.ManagementPort"))
                    port = ServiceConfiguration.ManagementPort;
                else if (portKey.Equals("NCacheServer.Port"))
                    port = ServiceConfiguration.Port;

                string ip = "";

                if (ipKey.Equals("NCacheServer.BindToIP"))
                    ip = ServiceConfiguration.BindToIP.ToString();
                else if (ipKey.Equals("NCacheServer.BindToClientServerIP"))
                    ip = ServiceConfiguration.BindToClientServerIP.ToString();

                //Input validation is already performed on Configuration
                if (!String.IsNullOrEmpty(mappingString))
                {
                    string[] mappingAddress = mappingString.Split(':');
                    if (mappingAddress.Length == 2)
                    {
                        Mapping mapping = new Mapping();
                        mapping.PublicIP = mappingAddress[0];
                        mapping.PublicPort = Convert.ToInt32(mappingAddress[1]);
                        mapping.PrivateIP = ip;
                        mapping.PrivatePort = port;
                        return mapping;
                    }
                }
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("NCache", "GetServerMappingForConfig: Unable to generate mapping from " + mappingKey + ". Exception: " + ex.Message, EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
            }
            return null;
        }


        [TargetMethod(ManagementUtil.MethodName.UpdateServerMappingConfig)]
        public void UpdateServerMappingConfig(MappingConfiguration.Dom.MappingConfiguration mappingConfiguration)
        {
            try
            {
                MappingConfiguration.MappingConfigurationManager.UpdateMappingConfiguration(mappingConfiguration);
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("NCache", "UpdateServerMappingConfig: Unable to update mapping configuration. Exception: " + ex.Message, EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
            }
        }

        [TargetMethod(ManagementUtil.MethodName.GetServerMappingForClient)]
        public MappingConfiguration.Dom.MappingConfiguration GetServerMappingForClient()
        {
            try
            {
                return MappingConfiguration.MappingConfigurationManager.GetMappingConfiguration();
            }
            catch (Exception)
            {
                return null;
            }
        }


        [TargetMethod(ManagementUtil.MethodName.GarbageCollect)]
        public void GarbageCollect(bool block, bool isCompactLOH)
        {
            GC.Collect(2, GCCollectionMode.Forced);
        }

        public static void ReleaseServiceObject()
        {
            try
            {
                lock (serviceObject)
                {
                    Monitor.PulseAll(serviceObject);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static void SetWaitOnServiceObject()
        {
            try
            {
                lock (serviceObject)
                {
                    Monitor.Wait(serviceObject);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public Common.ProductVersion GetProductVersion()
        {
            throw new NotImplementedException();
        }

        [TargetMethod(ManagementUtil.MethodName.TransferConnection)]
        public virtual void TransferConnection(System.Net.Sockets.SocketInformation socketInfo, string cacheId, byte[] transferCommand)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null)
                    cacheServer.TransferConnection(socketInfo, cacheId, transferCommand);
            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
            }
        }

        private ICacheServer GetCacheServer(string cacheId, bool assignServer)
        {
            CacheInfo cacheInfo = null;

            if (String.IsNullOrEmpty(cacheId))
            {
                throw new Exception("cacheId is not initialized.");
            }
            
            if (s_caches.Contains(cacheId.ToLower()))
                cacheInfo = s_caches[cacheId.ToLower()] as CacheInfo;
            if (cacheInfo != null)
            {
                try
                {
                    if (cacheInfo.CacheServer != null)
                        return cacheInfo.CacheServer;
                    else
                    {
                        int managementPort = cacheInfo.ManagementPort;
                        try
                        {
                            if (managementPort == 0 && _cachePortsConfigManger != null)
                                managementPort = _cachePortsConfigManger.GetCachePort(cacheId.ToLower());
                        }
                        catch (Exception ex)
                        {
                           
                        }
                        if (assignServer)
                        {
                            CacheService service = new CacheRPCService(ServiceConfiguration.BindToIP.ToString(), managementPort);
                            ICacheServer cacheServer = service.GetCacheServer(new TimeSpan(0, 0, 30));
                            if (cacheServer != null)
                            {
                                cacheInfo.Service = service;
                                cacheInfo.CacheServer = cacheServer;
                                return cacheServer;
                            }
                        }
                    }
                }
                catch (ChannelException) { throw; }
                catch (Exception ex)
                {
                    AppUtil.LogEvent("Ncache", ex.ToString(), System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.CacheStartError);
                    return null;
                }

            }
            return null;
        }
        private void DisposeOnException(string cacheId, Exception ex)
        {

        }

        public List<string> GetCaches(bool inproc)
        {
            List<string> inprocCaches = new List<string>();
            IDictionaryEnumerator ide = s_caches.GetEnumerator();
            CacheInfo info = null;
            try
            {
                while (ide.MoveNext())
                {
                    info = GetCacheInfo(ide.Key.ToString());

                    if (info != null)
                    {
                        inprocCaches.Add(ide.Key.ToString().ToLower());

                    }
                }
            }
            catch (Exception ex)
            {

            }
            return inprocCaches;
        }

     

        private bool RegisterCacheInternal(string cacheId, CacheServerConfig config, string partId, bool overwrite, bool hotApply, bool isLocalNode)
        {
            if (cacheId == null)
                throw new ArgumentNullException("cacheId");

            cacheId = cacheId.ToLower();
            CacheInfo cacheInfo = null;

            _rwLock.AcquireWriterLock(Timeout.Infinite);
            
            try
            {
                if (s_caches.Contains(cacheId))
                {
                    if (!overwrite)
                    {
                        return false;
                    }
                    cacheInfo = (CacheInfo)s_caches[cacheId];
                    CacheServerConfig currentConfig = GetCacheConfiguration(cacheId);
                    if (currentConfig != null)
                    {
                        if (currentConfig.DeploymentVersion > config.DeploymentVersion)
                        {
                            throw new VersionException("Deployment has been changed on cache '" + cacheId + "'. Reload it to continue with operation.", 5);
                        }
                    }
                }
                else
                {
                    /// This is until we change the use of properties in Cache
                    ///from props stirng or hashtable to Dom
                    ///                        
                    string props = GetProps(config);

                    ClientConfigManager.LocalCacheId = this.LocalCacheIP;
                    ClientConfigManager.AddCache(cacheId, config);

                    cacheInfo = new CacheInfo();
                    cacheInfo.CacheName = config.Name;
                    cacheInfo.CacheProps = config;
                    try
                    {
                        cacheInfo.ManagementPort = _cachePortsConfigManger.RegisterCachePort(cacheId.ToLower());

                    }
                    catch (Exception ex)
                    {

                    }
                    s_caches.Add(cacheId, cacheInfo);
                }
                cacheInfo.CacheProps = config;


                if ("local-cache".Equals(config.CacheType))
                {
                    ClientConfigManager.LocalCacheId = (this.LocalCacheIP);
                    ClientConfigManager.AddCache(cacheId, config);
                }
              
                if (hotApply && cacheInfo != null && IsRunning(cacheId))
                {
                    CacheConfig cc = CacheConfig.FromConfiguration(config);

                    Alachisoft.NCache.Caching.Util.HotConfig hotConfig = new Alachisoft.NCache.Caching.Util.HotConfig();
                    hotConfig.IsErrorLogsEnabled = cc.IsErrorLogsEnabled;
                    hotConfig.IsDetailedLogsEnabled = cc.IsDetailedLogsEnabled;
                    hotConfig.CacheMaxSize = cc.CacheMaxSize;
                    hotConfig.CleanInterval = cc.CleanInterval;
                    hotConfig.EvictRatio = cc.EvictRatio;
                    
                    hotConfig.SecurityEnabled = cc.SecurityEnabled;
                    hotConfig.SecurityDomainController = cc.SecurityDomainController;
                    hotConfig.SecurityUsers = cc.SecurityUsers;

                    hotConfig.CompressionEnabled = cc.CompressionEnabled;
                    hotConfig.CompressionThreshold = cc.CompressionThreshold;
                    hotConfig.BackingSource = cc.BackingSource;
                    ApplyHotConfiguration(cacheId, hotConfig);

                }
                SaveConfiguration();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }

            return true;
        }

        
        [TargetMethod(ManagementUtil.MethodName.GetAPILogData, 1)]
        public Hashtable GetAPILogData(string cacheID, string instanceID)
        {
            if (cacheID != null)
            {
                ICacheServer cacheServer = GetCacheServer(cacheID, false);
                if (cacheServer != null)
                {
                    return cacheServer.GetAPILogData(cacheID, instanceID);

                }
            }
            return null;

        }

        [TargetMethod(ManagementUtil.MethodName.SavePerfmonLogFilePath, 1)]
        public void SavePerfmonLogFilePath(string logpath)
        {
            try
            {
                if (logpath == null)
                    return;
                string path = ServiceFilePath;
                if (File.Exists(path))
                {
                    XmlDocument document = new XmlDocument();
                    document.Load(path);

                    string xPath = "/configuration/appSettings/add";

                    string elementKeys = "NCacheServer.PerfmonLogPath";
                    string elementValues = logpath;
                    this.ChangeAttribute(document, xPath, elementKeys, elementValues);
                    document.Save(path);
                }

                ServiceConfiguration.ChangePerfmonLogPath(logpath);
            }
            catch
            {

            }

        }

        [TargetMethod(ManagementUtil.MethodName.GetPerfmonConfiguration, 1)]
        public bool GetPerfmonConfiguration()
        {
            return ServiceConfiguration.PerfmonEnable;
        }
        [TargetMethod(ManagementUtil.MethodName.GetPerfmonLoggingPath, 1)]
        public string GetPerfmonLoggingPath()
        {
            return ServiceConfiguration.PerfmonPath;
        }

        private void DisposeOnStart(string cacheId)
        {
            try
            {
                CacheInfo cacheInfo = null;
                if (s_caches != null && s_caches.ContainsKey(cacheId.ToLower()))
                    cacheInfo = s_caches[cacheId.ToLower()] as CacheInfo;

                if (cacheInfo != null)
                {
                    cacheInfo.ManagementPort = 0;
                    cacheInfo.CacheProcessId = 0;

                    if (cacheInfo.CacheServer != null)
                    {
                        cacheInfo.CacheServer.Dispose();
                        cacheInfo.CacheServer = null;
                    }
                    if (cacheInfo.Service != null)
                    {
                        cacheInfo.Service.Dispose();
                        cacheInfo.Service = null;
                    }
                }
            }
            catch (Exception) { }
        }

       
        internal void ResetManagementInfo(CacheInfo info)
        {
            try
            {
                if (info != null)
                {
                    info.ManagementPort = 0;
                    if (info.Service != null)
                    {
                        info.Service.Dispose();
                        info.Service = null;
                    }
                    if (info.CacheServer != null)
                    {
                        info.CacheServer.Dispose();
                        info.CacheServer = null;
                    }
                    info.CacheProcessId = 0;
                }
            }
            catch
            {

            }

        }

        [TargetMethod(ManagementUtil.MethodName.GetTopicStats, 1)]
        public virtual Dictionary<string, TopicStats> GetTopicStats(string cacheId, bool defaultTopicStats= false)
        {
            Dictionary<string, TopicStats> topicStats = new Dictionary<string, TopicStats>();
            ICacheServer cacheServer = GetCacheServer(cacheId, false);
            if (cacheServer != null)
                topicStats = cacheServer.GetTopicStats(cacheId,defaultTopicStats);
            return topicStats;

        }

        [TargetMethod(ManagementUtil.MethodName.GetCounterValue, 1)]
        public virtual double GetCounterValue(string cacheId, string counterName, string category, bool replica = false)
        {
            double value = -10.0;
            try
            {
                if (category == "NCache Client")
                {
                    if (!string.IsNullOrEmpty(cacheId))
                    {
                        if (_clientCustomCounters.Contains(cacheId))
                        {
                            Alachisoft.NCache.Management.Statistics.CustomStatsCollector counters = (Alachisoft.NCache.Management.Statistics.CustomStatsCollector)_clientCustomCounters[cacheId];
                            value = counters.GetCounterValue(counterName);
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(cacheId))
                    {
                        ICacheServer cacheServer = GetCacheServer(cacheId, false);
                        if (cacheServer != null)
                            value = cacheServer.GetCounterValue(cacheId, counterName, category, replica);
                    }
                }
            }
            catch (Exception e)
            { }
            return (float)value;
        }

        /// <summary>
        /// Get Perfmon categories currently residing on the machine
        /// </summary>
        /// <returns>list of category names and description</returns>
        [TargetMethod(ManagementUtil.MethodName.GetPerfmonCategoryNames, 1)]
        public Dictionary<string,string> GetPerfmonCategoryNames()
        {
            return PerformanceCounterCategory.GetCategories().ToDictionary(item => item.CategoryName, item => item.CategoryHelp);
        }

        /// <summary>
        /// Get instance names of a Perfmon category
        /// </summary>
        /// <param name="categoryName">Name of perfmon category from <seealso cref="GetPerfmonCategories"/></param>
        /// <returns></returns>
        [TargetMethod(ManagementUtil.MethodName.GetPerfmonInstanceNames, 1)]
        public string[] GetPerfmonInstanceNames(string categoryName)
        {
            PerformanceCounterCategory category = new PerformanceCounterCategory(categoryName);
            return category.GetInstanceNames();
        }

        /// <summary>
        /// Get counter names of a perfmon category
        /// </summary>
        /// <param name="categoryName">name of perfmon category</param>
        /// <param name="instanceName">name of instance of perfmon category</param>
        /// <returns></returns>
        [TargetMethod(ManagementUtil.MethodName.GetPerfmonCounterNames, 1)]
        public string[] GetPerfmonCounterNames(string categoryName, string instanceName)
        {
            PerformanceCounterCategory category = new PerformanceCounterCategory(categoryName);
            return (from counter in category.GetCounters(instanceName) select counter.CounterName).ToArray();
        }

        /// <summary>
        /// Get Perfmon Value
        /// This is used by web monitor
        /// </summary>
        /// <param name="cacheId">name of cache</param>
        /// <param name="counterName">perfmon counter name</param>
        /// <param name="category">perfmon counter category</param>
        /// <param name="processInstance">perfmoncache instance</param>
        /// <param name="replica">is the node a replica or not</param>
        /// <returns>value of perfmon counter</returns>
        [TargetMethod(ManagementUtil.MethodName.GetPerfmonValue, 1)]
        public virtual double GetPerfmonValue(string cacheId, string counterName, string category, string processInstance, bool replica = false)
        {
            double value = -10.0;
            try
            {
                if (string.IsNullOrEmpty(cacheId))
                    return value;

                PerfmonStatsCollector counters = PerfmonStatsCollector.GetInstance(cacheId, counterName, category, processInstance, _clusterIp);
                value = counters.GetCounterValue(processInstance, counterName);
            }
            catch (Exception e)
            { }
            return (float)value;
        }

        internal int GetCacheHostProcessID(string cacheId)
        {
            return ((CacheInfo)s_caches[cacheId]).CacheProcessId;
        }

        [TargetMethod(ManagementUtil.MethodName.GetPerfmonValues, 1)]
        public List<PerfmonCounterDetails> GetPerfmonValues(List<PerfmonCounterDetails> counterDetails, string cacheId)
        {
            List<PerfmonCounterDetails> result = new List<PerfmonCounterDetails>();
            PerfmonStatsCollector counters = PerfmonStatsCollector.GetInstance();

            foreach (PerfmonCounterDetails counterDetail in counterDetails)
            {
                result.Add(new PerfmonCounterDetails()
                {
                    Category = counterDetail.Category,
                    Instance = counterDetail.Instance,
                    Counter = counterDetail.Counter,
                    Value = ServiceConfiguration.PublishCountersToCacheHost ? Instance.GetCounterValue(cacheId, counterDetail.Counter, counterDetail.Category, counterDetail.Instance.EndsWith("-replica") ? true : false) : counters.GetCounterValue(counterDetail.Instance, counterDetail.Counter, counterDetail.Category, _clusterIp)
                });
            }
            return result;
        }

        [TargetMethod(ManagementUtil.MethodName.GetConfigurationVersion, 1)]
        public ConfigurationVersion GetConfigurationVersion(string cacheId)
        {
            ConfigurationVersion configValues = new ConfigurationVersion();
            if (!string.IsNullOrEmpty(cacheId))
            {
                try
                {
                    CacheServerConfig curentConfiguration = GetCacheConfiguration(cacheId);
                    if (curentConfiguration != null)
                    {
                        configValues.ConfigID = curentConfiguration.ConfigID;
                        configValues.ConfigVersion = curentConfiguration.ConfigVersion;
                        configValues.DeploymentVersion = curentConfiguration.DeploymentVersion;
                        return configValues;
                    }
                }
                catch
                {
                    return configValues;
                }
            }
            return configValues;
        }

        public void AssignManagementPorts()
        {
            try
            {
                List<string> registeredCaches = new List<string>();
                IDictionaryEnumerator ide = s_caches.GetEnumerator();
                CacheInfo info = null;
                try
                {
                    while (ide.MoveNext())
                    {
                        info = GetCacheInfo(ide.Key.ToString());
                        if (info != null)
                            registeredCaches.Add(ide.Key.ToString().ToLower());
                    }
                }
                catch (Exception ex)
                {
                    return;
                }
                _cachePortsConfigManger = new CachePortsConfigManger();
                _cachePortsConfigManger.ConfiguredCaches = registeredCaches;
                _cachePortsConfigManger.Initialize();
                try
                {
                    AssignServerstoRunningCaches();
                }
                catch (Exception ex)
                {
                    AppUtil.LogEvent("Failed to assign ports to running caches : " + ex.ToString(), EventLogEntryType.Error);
                }

                try
                {
                    _cachePortsConfigManger.SynchronizeTable();
                    ide = _cachePortsConfigManger.CachePortTable.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (s_caches.ContainsKey(ide.Key.ToString().ToLower()))
                        {
                            CacheInfo cacheInfo = (CacheInfo)s_caches[ide.Key.ToString().ToLower()];
                            cacheInfo.ManagementPort = _cachePortsConfigManger.GetCachePort(ide.Key.ToString().ToLower());
                            s_caches[ide.Key.ToString().ToLower()] = cacheInfo;
                        }
                    }
                }
                catch (Exception ex)
                {

                }

            }
            catch (Exception ex)
            {

            }
            //Get Licenses for all running caches
            List<string> cachesName = new List<string>();
            IDictionaryEnumerator ide1 = _cachePortsConfigManger.CachePortTable.GetEnumerator();
            while (ide1.MoveNext())
            {
                cachesName.Add(ide1.Key.ToString());
            }
        }
       
        public void DisposeCacheportsConfigmanger()
        {
            if (_cachePortsConfigManger != null)
            {
                _cachePortsConfigManger.Dispose();
            }
        }

       

        [TargetMethod(ManagementUtil.MethodName.GetCacheProcessID, 1)]
        public int GetCacheProcessID(string cacheID)
        {
            if (!string.IsNullOrEmpty(cacheID))
            {
                ICacheServer cacheServer = GetCacheServer(cacheID, false);
                if (cacheServer != null)
                {
                    return cacheServer.GetCacheProcessID(cacheID);
                }
            }
            return 0;
        }



      

        #region Cluster-Split-Manager related methods


        [TargetMethod(ManagementUtil.MethodName.PublishCustomClientCounters)]
        public virtual void PublishCustomClientCounters(string cacheId, ClientCustomCounters customCounters)
        {
            try
            {
                Statistics.CustomStatsCollector customStats;
                cacheId = cacheId.ToLower();

                if (!_clientCustomCounters.ContainsKey(cacheId))
                {
                    customStats = new Statistics.CustomStatsCollector(cacheId, 0, cacheId);
                    customStats.InitializePerfCounters(false);
                    customStats.UpdateCounters(customCounters);
                    _clientCustomCounters.Add(cacheId, customStats);
                }
                else
                {
                    customStats = (Statistics.CustomStatsCollector)_clientCustomCounters[cacheId];
                    customStats.UpdateCounters(customCounters);
                    _clientCustomCounters[cacheId] = customStats;
                }               

            }
            catch (Exception e)
            {

            }

        }
        #endregion

        private void ResetManagementPort(string cacheId)
        {
            try
            {
                _cachePortsConfigManger.UnRegisterCachePort(cacheId);
                CacheInfo info = s_caches[cacheId.ToLower()] as CacheInfo;
                info.CacheServer = null;
                info.Service = null;
                info.ManagementPort = _cachePortsConfigManger.RegisterCachePort(cacheId);
                s_caches[cacheId.ToLower()] = info;

            }
            catch (Exception ex)
            {

            }
        }

        private Process StartCacheProcess(string cacheId, int port)
        {
            Process process = null;
            try
            {
              
                StringBuilder cparams = new StringBuilder();
                cparams.Append("/i").Append(" ");
                cparams.Append(cacheId.ToLower()).Append(" ");
                cparams.Append("/p").Append(" ");
                cparams.Append(port).Append(" ");

                if (ServiceConfiguration.EnableCacheDebugging)
                    cparams.Append("/debug");
                ProcessExecutor processExecutor = new ProcessExecutor(cparams.ToString());
                process = processExecutor.Execute();
            }
            catch (Exception ex)
            {
                throw;
            }
            return process;
        }

        public void AssignServerstoRunningCaches()
        {
            OSInfo currentOS = OSInfo.Windows;
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                currentOS = OSInfo.Linux;
#endif
            if (currentOS == OSInfo.Linux)
            {
                try
                {
                    Hashtable runningcaches = null;
                    try
                    {
                        runningcaches = ManagementPortHandler.DiscoverCachesViaPGrep();
                    }
                    catch (Exception ex)
                    {
                        string msg = String.Format("CacheServer failed to load running caches information, Error {0}",
                        ex.ToString());
                        AppUtil.LogEvent(msg, EventLogEntryType.Warning);
                    }

                    IDictionaryEnumerator enm = runningcaches.GetEnumerator();
                    while (enm.MoveNext())
                    {
                        if (s_caches.ContainsKey(enm.Key.ToString()))
                        {
                            CacheInfo cacheInfo = (CacheInfo)s_caches[enm.Key.ToString().ToLower()];
                            cacheInfo.CacheProcessId = ((CacheHostInfo)enm.Value).ProcessId;
                            cacheInfo.ManagementPort = ((CacheHostInfo)enm.Value).ManagementPort;
                            s_caches[enm.Key.ToString().ToLower()] = cacheInfo;
                            _cachePortsConfigManger.AssignRunningPorts(enm.Key.ToString().ToLower(), cacheInfo.ManagementPort);
                            GetCacheServer(enm.Key.ToString(), true);
                        }
                    }
                }
                catch
                {

                }
                return;
            }
                

            try
            {
                Hashtable runningcaches = null;
                try
                {

                    runningcaches = ManagementPortHandler.DiscoverCachesViaWMI();

                    if (runningcaches.Count < 1)
                    {
                        List<ProcessInfo> processInfos = ManagementPortHandler.DiscoverCachesViaNetStat();
                        if (processInfos != null && processInfos.Count != 0)
                        {
                            foreach (ProcessInfo processInfo in processInfos)
                            {
                                string name = GetCacheName(processInfo.port_number);
                                CacheHostInfo info = new CacheHostInfo();
                                info.ProcessId = processInfo.pid;
                                info.ManagementPort = processInfo.port_number;
                                runningcaches.Add(name, info);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string msg = String.Format("CacheServer failed to load running caches information, Error {0}",
                    ex.ToString());
                    AppUtil.LogEvent(msg, EventLogEntryType.Warning);
                }

                IDictionaryEnumerator enm = runningcaches.GetEnumerator();
                while (enm.MoveNext())
                {
                    if (s_caches.ContainsKey(enm.Key.ToString()))
                    {
                        CacheInfo cacheInfo = (CacheInfo)s_caches[enm.Key.ToString().ToLower()];
                        cacheInfo.CacheProcessId = ((CacheHostInfo)enm.Value).ProcessId;
                        cacheInfo.ManagementPort = ((CacheHostInfo)enm.Value).ManagementPort;
                        s_caches[enm.Key.ToString().ToLower()] = cacheInfo;
                        _cachePortsConfigManger.AssignRunningPorts(enm.Key.ToString().ToLower(), cacheInfo.ManagementPort);
                        GetCacheServer(enm.Key.ToString(), true);
                    }
                }
            }
            catch
            {

            }
        }
        
        [TargetMethod(ManagementUtil.MethodName.AreCacheHostCountersEnabled, 1)]
        public bool AreCacheHostCountersEnabled()
        {
            try
            {
                return ServiceConfiguration.PublishCountersToCacheHost;
            }
            catch
            {
                return false;
            }
        }

        [TargetMethod(ManagementUtil.MethodName.GetConfigurationOfAllCaches)]
        public Dictionary<string, Config.NewDom.CacheServerConfig> GetConfigurationOfAllCaches(CacheTopology topology)
        {
            Dictionary<string, Config.NewDom.CacheServerConfig> dictionary = new Dictionary<string, Config.NewDom.CacheServerConfig>();
            foreach (var item in s_caches.Values)
            {
                CacheInfo cacheInfo = item as CacheInfo;
                if (cacheInfo != null && cacheInfo.CacheProps!=null)
                {
                    if(topology == CacheTopology.Clustered)
                    {
                        if (cacheInfo.CacheProps.CacheType != "clustered-cache")
                            continue;
                    }
                    else
                    {
                        if (cacheInfo.CacheProps.CacheType == "clustered-cache")
                            continue;
                    }
                    dictionary.Add(cacheInfo.CacheName, Config.NewDom.DomHelper.convertToNewDom(cacheInfo.CacheProps));
                }
            }
            return dictionary;
        }


        [TargetMethod(ManagementUtil.MethodName.CanApplyHotConfig, 2)]
        public virtual bool CanApplyHotConfiguration(string cacheId, long size)
        {
            ICacheServer cacheServer = GetCacheServer(cacheId, false);
            if (cacheServer != null)
                return cacheServer.CanApplyHotConfiguration(cacheId, size);
            return false;
        }


        [TargetMethod(ManagementUtil.MethodName.GetConfigurationId, 1)]
        public virtual string GetConfigurationId(string cacheId)
        {
            CacheServerConfig cacheConfig = GetCacheConfiguration(cacheId);
            return cacheConfig.ConfigID;
        }

        public void ChannelDisconnected(IChannelDisconnected channelDisconnection)
        {
            throw new NotImplementedException();
        }

       

        [TargetMethod(ManagementUtil.MethodName.GetPoolStats, 1)]
        public virtual PoolStats GetPoolStats(PoolStatsRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(request.CacheId))
                throw new OperationFailedException("Invalid CacheId specified!");

            var cacheServer = GetCacheServer(request.CacheId, false);

            if (cacheServer != null)
                return cacheServer.GetPoolStats(request);

            return new PoolStats();
        }

        
        [TargetMethod(ManagementUtil.MethodName.GetOSPlatform, 1)]
        public OSInfo GetOSPlatform()
        {
            OSInfo currentOS = OSInfo.Windows;
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                currentOS = OSInfo.Linux;
#endif
            return currentOS;
        }

        [TargetMethod(ManagementUtil.MethodName.GetFeatureUsageReport, 1)]
        public virtual Dictionary<string, Common.FeatureUsageData.Feature> GetFeatureUsageReport(string cacheId)
        {
            Dictionary<string, Common.FeatureUsageData.Feature> featureDataUsageReport = null;
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null && cacheServer.IsRunning(cacheId))
                    featureDataUsageReport = cacheServer.GetFeatureUsageReport(cacheId);
            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
            }

            return featureDataUsageReport;
        }

        [TargetMethod(ManagementUtil.MethodName.GetMachineId, 1)]
        public virtual string GetMachineId()
        {
            string machineId = ProfileUsageCollector.Instance.ReportHardwareProfile().MachineID;
            if (String.IsNullOrEmpty(machineId))
            {
                FeatureConfigManager featureConfigMgr = new FeatureConfigManager();
                machineId = featureConfigMgr.GetMachineId();
            }
            return machineId;
        }

        public string GetPossibleMachinesInCluster()
        {
            string otherServers = string.Empty;
            List<string> otherMachinesIP = new List<string>();
            List<Task<string>> tasks = new List<Task<string>>();

            Config.NewDom.CacheServerConfig[] cacheServerConfig = GetCacheServerConfiguration();

            foreach (var cacheConfig in cacheServerConfig)
            {
                if (cacheConfig.CacheDeployment != null)
                {
                    List<Address> serverNodes = cacheConfig.CacheDeployment.Servers.GetAllConfiguredNodes();
                    serverNodes.ForEach(address =>
                    {

                        if (!address.IpAddress.Equals(ServiceConfiguration.BindToIP) && !otherMachinesIP.Contains(address.IpAddress.ToString()))
                            otherMachinesIP.Add(address.IpAddress.ToString());
                    });
                }
            }

            foreach (var machineAddr in otherMachinesIP)
            {
                try
                {
                    tasks.Add(GetMachineIdTask(machineAddr));
                }
                catch (Exception e)
                {
                }
            }

            Task.WaitAll(tasks.ToArray(), 60000);
            foreach (Task<string> configTask in tasks)
            {
                string machineId;
                if (configTask.IsCompleted)
                {
                    machineId = configTask.Result;
                    if (machineId != null && !machineId.Equals(string.Empty))
                    {
                        otherServers += machineId + ",";
                    }
                }
            }
            otherServers = otherServers.TrimEnd(new char[] { ',' });
            return otherServers;
        }


        private Task<string> GetMachineIdTask(string cacheServer)
        {
            Task<string> task = new Task<string>(() =>
            {
                try
                {
                    using (NCacheRPCService cacheService = new NCacheRPCService(cacheServer))
                    {
                        using (ICacheServer nCacheServer = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30)))
                        {
                            return nCacheServer.GetMachineId();
                        }
                    }
                }
                catch (Exception e)
                {
                    return "";
                }

            });
            task.Start();
            return task;
        }

        [TargetMethod(ManagementUtil.MethodName.GetClientProfileReport, 1)]
        public virtual ClientProfileDom GetClientProfileReport(string cacheId)
        {
            ClientProfileDom clientProfile = null;
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId, false);
                if (cacheServer != null && cacheServer.IsRunning(cacheId))
                    clientProfile = cacheServer.GetClientProfileReport(cacheId);

            }
            catch (Exception e)
            {
                DisposeOnException(cacheId, e);
            }

            return clientProfile;
        }
    }
}

