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
using System.IO;
using System.Collections;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Configuration;
using System.Timers;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Stats;


using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.RPCFramework;


using System.Management;
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

namespace Alachisoft.NCache.Management
{

    /// <summary>
    /// Manages cache start and stop and keeps a named collection of caches
    /// </summary>
    public class CacheServer : MarshalByRefObject, IDisposable, ICacheServer
    {
        private static CacheServer s_instance;
        string _cacheserver="NCache";

      
        /// <summary>
        /// Enumeration specifying type of channle
        /// </summary>
        public enum Channel
        {
            /// <summary>
            /// Bind cluster channle
            /// </summary>
            Cluster = 0,
            /// <summary>
            /// Bind client server channle
            /// </summary>
            SocketServer
        }

        public enum CacheStopReason
        {
            Expired,
            Stoped,
            ForcedStoped
        }

        private static object serviceObject;

        /// <summary> Returns the application name of this session. </summary>
        static internal string ObjectUri
        {
            get
            {
                return typeof(CacheServer).Name;
            }
        }

        public virtual string ClusterIP
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
        private static Hashtable s_caches = Hashtable.Synchronized(new Hashtable());

        private static Hashtable s_partitionedCaches = Hashtable.Synchronized(new Hashtable());

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

        private static System.Timers.Timer _evalWarningTask;
        private TimeScheduler _gcScheduler;
        private TimeScheduler _portPoolScheduler;

        private static IConnectionManager _connectionManager;
        private Alachisoft.NCache.Management.HostServer hostServer;
        public static IConnectionManager ConnectionManager
        {
            get { return _connectionManager; }
            set { _connectionManager = value; }
        }

        /// <summary>
        /// Static constructor
        /// </summary>
        static CacheServer()
        {
            try
            {
                Alachisoft.NCache.Util.MiscUtil.RegisterCompactTypes();
                RegisterCompactTypes();
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
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.RPCFramework.TargetMethodParameter), 165);

            #region [Register Cache Server Assemblies]
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.ProductVersion), 302);
            CompactFormatterServices.RegisterCompactType(typeof(CacheServerConfig), 177);
            CompactFormatterServices.RegisterCompactType(typeof(Log), 178);
            CompactFormatterServices.RegisterCompactType(typeof(PerfCounters), 179);
            CompactFormatterServices.RegisterCompactType(typeof(QueryIndex), 181);
            CompactFormatterServices.RegisterCompactType(typeof(Class), 182);
            CompactFormatterServices.RegisterCompactType(typeof(Cleanup), 194);
            CompactFormatterServices.RegisterCompactType(typeof(Storage), 195);
            CompactFormatterServices.RegisterCompactType(typeof(EvictionPolicy), 196);
            CompactFormatterServices.RegisterCompactType(typeof(Cluster), 197);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Config.Dom.Channel), 198);
            CompactFormatterServices.RegisterCompactType(typeof(NodeIdentity), 199);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.StatusInfo), 200);
            CompactFormatterServices.RegisterCompactType(typeof(ClientNodeStatusWrapper), 250);
            CompactFormatterServices.RegisterCompactType(typeof(AutoLoadBalancing), 203);
            CompactFormatterServices.RegisterCompactType(typeof(ClientNodes), 204);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Config.Dom.ClientNode), 205);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Management.CacheRegisterationInfo), 212);
            CompactFormatterServices.RegisterCompactType(typeof(CacheStatusOnServerContainer), 213);
            CompactFormatterServices.RegisterCompactType(typeof(CacheStatistics), 65);
            CompactFormatterServices.RegisterCompactType(typeof(ClusterCacheStatistics), 66);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Management.ClientConfiguration.Dom.ClientConfiguration), 214);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Management.ClientConfiguration.Dom.NodeConfiguration), 215);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheConfiguration), 216);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer), 219);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Management.CacheInfo), 220);
           
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Management.Management.BindedIpMap), 237);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Management.Management.NodeInfoMap), 238);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Config.Dom.User), 240);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Config.Dom.User[]), 241);

            CompactFormatterServices.RegisterCompactType(typeof(Parameter), 244);
            CompactFormatterServices.RegisterCompactType(typeof(Parameter[]), 245);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Config.Dom.ClientNode[]), 247);
            CompactFormatterServices.RegisterCompactType(typeof(Class[]), 249);
           
            CompactFormatterServices.RegisterCompactType(typeof(Attrib), 251);

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.RtContextValue), 300);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Config.Dom.ClientDeathDetection), 355);

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.Util.HotConfig), 347);
            #endregion

            #region Live Upgrade Classes

            #endregion

            #region [Register Monitor Server Assemblies]

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Net.Address), 110);
            
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Net.Address[]), 233);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Management.ClientConfiguration.CacheServerList), 239);
           
            #endregion
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

        }

        /// <summary>
        /// Finalizer for this object.
        /// </summary>
        ~CacheServer()
        {

            Dispose(false);
        }

           

        /// <summary>
        /// returns product version of the server
        /// </summary>
        /// <returns></returns>
        [TargetMethod(ManagementUtil.MethodName.GetProductVersion)]
        public Common.ProductVersion GetProductVersion()
        {
            return Common.ProductVersion.ProductInfo;
        }

        /// <summary>
        /// 
        /// </summary>
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


                FileStream fs = new FileStream(folderLevel2 + "\\" + name, FileMode.Create, FileAccess.Write);
                fs.Write(buffer, 0, buffer.Length);
                fs.Flush();
                fs.Close();
            }
        }

        /// <summary>
        /// Add garbage collection task to time sheduler
        /// </summary>

        private void StartGCTask()
        {
            bool enabled = true;

            bool.TryParse(ConfigurationSettings.AppSettings["NCacheServer.EnableForcedGC"], out enabled);

            ///Only if CLR is loaded with Server GC, and user asked for forced GC
            if (enabled && System.Runtime.GCSettings.IsServerGC) //ALAWAYS RETURNS FALSE IN CASE OF MONO
            {
                int threshold = 0;
                if (int.TryParse(System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.ForcedGCThreshold"], out threshold))

                {
                    this._gcScheduler.AddTask(new GarbageCollectionTask(threshold));
                }
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

            for (IDictionaryEnumerator idenum = s_caches.GetEnumerator(); idenum.MoveNext(); )
            {
                CacheInfo cacheInfo = (CacheInfo)idenum.Value;
                if (cacheInfo != null)
                {
                   try{
                        if(cacheInfo.Service!=null)
                        {
                            cacheInfo.Service.Dispose();
                            cacheInfo.Service = null;
                        }
                        if (cacheInfo.CacheServer != null)
                        {
                            cacheInfo.CacheServer.Dispose();
                            cacheInfo.CacheServer = null;
                        }
                   }catch(Exception){}
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
        public Cache this[string cacheId]
        {
            get
            {
                if (cacheId == null) throw new ArgumentNullException("cacheId");
                return GetCache(cacheId);
            }
        }

        public virtual Cache GetCache(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId);
                if (cacheServer != null)
                    return cacheServer.GetCache(cacheId);
                else
                    throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException("Specified cacheId is not registered.");
            }
            catch (Exception ex)
            {
                DisposeOnException(cacheId, ex);
            }
            return null;
        }

        [TargetMethod(ManagementUtil.MethodName.Authorize, 1)]
        public bool Authorize()
        {
                    return false;
        }

        /// <summary>
        /// Returns a list of running caches
        /// </summary>
        /// <returns>list of running caches</returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.GetRunningCaches, 3)]
        public ArrayList GetRunningCaches()
        {
            ArrayList runningCache = new ArrayList(5);
            IDictionary coll = GetCacheProps();
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
            {
                return this[cacheId];
            }
            else
            {
                if (cacheId == null) throw new ArgumentNullException("cacheId");
            }
            return null;
        }

        /// <summary>
        /// Get All configured partition of replica caches
        /// </summary>
        /// <returns></returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.GetConfiguredPartitionedReplicaCaches, 1)]
        public ConfiguredCacheInfo[] GetConfiguredPartitionedReplicaCaches()
        {
            ConfiguredCacheInfo[] configuredCaches = new ConfiguredCacheInfo[s_partitionedCaches.Count];

            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                IDictionaryEnumerator en = s_partitionedCaches.GetEnumerator();
                int i = 0;
                while (en.MoveNext())
                {
                    string cacheId = en.Key as string;
                    Hashtable partitionedCaches = en.Value as Hashtable;
                    if (partitionedCaches != null)
                    {
                        foreach (DictionaryEntry de in partitionedCaches)
                        {
                            string partId = de.Key as string;
                            CacheInfo cacheInfo = (CacheInfo)de.Value;

                            ConfiguredCacheInfo configuredCache = new ConfiguredCacheInfo();
                            configuredCache.CacheId = cacheInfo.CacheProps.Name;
                            configuredCache.IsRunning = cacheInfo.Cache.IsRunning;
                            configuredCache.DataCapacity = cacheInfo.CacheProps.Storage.Size;
                            configuredCache.CachePropString = GetProps(cacheInfo.CacheProps);
                            configuredCache.PartId = partId;

                            if (cacheInfo.CacheProps.CacheType == "clustered-cache")
                            {
                                if (cacheInfo.CacheProps.Cluster != null)
                                {
                                    switch (cacheInfo.CacheProps.Cluster.Topology)
                                    {
                                        case "replicated-server":
                                            configuredCache.Topology = CacheTopology.Replicated;
                                            break;
                                        
                                        case "partitioned-server":
                                            configuredCache.Topology = CacheTopology.Partitioned;
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
                return configuredCaches;
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
        }
               

        /// <summary>
        /// Gets the cache instance ignoring the backup/replica id. For e.g. if we have two instances
        /// of the same cache por_test (master node id) and por_test_bk_node_node1
        /// a replica of the same cache. Now we try to first connect the master id and if it is not
        /// available or running then we try to connect to its backup although its id is different.
        /// </summary>
        /// <param name="cacheId">Id of the cache</param>
        /// <returns>Cache Instance</returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.GetCacheInstanceIgnoreReplica, 1)]
        public Cache GetCacheInstanceIgnoreReplica(string cacheId)
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
                else if (s_partitionedCaches.Contains(cacheId))
                {
                    Hashtable partitionedTable = s_partitionedCaches[cacheId] as Hashtable;
                    if (partitionedTable != null)
                    {
                        IDictionaryEnumerator ide = partitionedTable.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            CacheInfo cacheInfo = ide.Value as CacheInfo;

                            if (cacheInfo != null)
                                cache = cacheInfo.Cache;

                            return cache;
                        }
                    }
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
                    ICacheServer cacheServer = GetCacheServer(cacheInfo.CacheName);
                    if (cacheServer != null)
                    {
                        cacheInfo.CacheProps.IsRunning = cacheServer.IsRunning(cacheInfo.CacheName);
                    }
                    else
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

                    IDictionaryEnumerator ide = s_partitionedCaches.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        string cacheName = ide.Key as string;
                        Hashtable tbl = new Hashtable();
                        Hashtable partitionedTable = ide.Value as Hashtable;
                        cacheProps.Add(cacheName, tbl);

                        IDictionaryEnumerator ie = partitionedTable.GetEnumerator();
                        while (ie.MoveNext())
                        {
                            CacheInfo cacheInfo = ie.Value as CacheInfo;
                            tbl.Add(ie.Key, cacheInfo.CacheProps);
                        }
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



        //Numan Hanif: OverLoaded Method For New Dom Config [Tools]
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
        /// <param name="hotApply"></param>
        /// <returns></returns>

        [TargetMethod(ManagementUtil.MethodName.RegisterCache, 1)]
        public bool RegisterCache(string cacheId, CacheServerConfig config, string partId, bool overwrite, bool hotApply)
        {
            if (cacheId == null) throw new ArgumentNullException("cacheId");

            //We load configuration before applying the new changes to preserve some old values in the config.
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
                    //(WARNING): This code is here to save the old value of connection-retries
                    //in the cluster configuration to avoid override by NCManager. 
                    //This code should be removed after these options appears on UI.

                }
                else
                {
                    ///[] This is until we change the use of properties in Cache
                    ///from props stirng or hashtable to Dom
                    ///                        
                    string props = GetProps(config);

                    ClientConfigManager.LocalCacheId = this.LocalCacheIP;
                    ClientConfigManager.AddCache(cacheId, config);

                    cacheInfo = new CacheInfo();
                    cacheInfo.CacheName = config.Name;
                    cacheInfo.CacheProps = config;
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
        public ICollection Caches
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
        public void UpdateClientServersList(string cacheId, string[] servers, ref string xml, bool loadBalance)
        {
            ClientConfigManager.UpdateServerNodes(cacheId.ToLower(), servers, ref xml, loadBalance);
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

            if (ServiceConfiguration.BindToIP != null)
                bindIP = ServiceConfiguration.BindToIP.ToString();

            else
                bindIP = System.Environment.MachineName.ToLower();

            _clientserverip = bindIP;
            ClientConfigManager.BindIP = bindIP;
            ClientConfigManager.AvailableNIC(DetectNICs());
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
                    RemoveClientCacheConfiguration(cacheId, removeServerOnly);
                    StopCache(cacheId);
                    LeasedCache cache = null;
                    
                    if (cacheInfo != null)
                        cache = cacheInfo.Cache;

                    if (cache != null)
                        cache.Dispose();

                    s_caches.Remove(cacheId.ToLower());

                    RemoveDeployedAssemblies(cacheId); // no need to check return values; if remove then fine else not need to break the process

                }
                
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }

        private void RemoveClientCacheConfiguration(string cacheId, bool removeServerOnly)
        {
            if (!removeServerOnly)
                ClientConfigManager.RemoveCache(cacheId.ToLower());
            else
            {
                bool serverListChanged = false;
                bool serverExistsAsClient = false;
                string serverIP = GetBindIP();
                ClientConfiguration.Dom.ClientConfiguration clientConfiguration = ClientConfigManager.GetClientConfiguration(cacheId);
                ClientConfiguration.Dom.CacheConfiguration[] cacheConfigurations = clientConfiguration.CacheConfigurations;
                foreach (ClientConfiguration.Dom.CacheConfiguration cacheConfiguraion in cacheConfigurations)
                {
                    if (cacheConfiguraion.CacheId != null && cacheConfiguraion.CacheId == cacheId)
                    {
                        List<ClientConfiguration.Dom.CacheServer> servers = new List<ClientConfiguration.Dom.CacheServer>(cacheConfiguraion.Servers);
                        if (servers.Count == 1 && servers[0].ServerName == serverIP)
                        {
                            ClientConfigManager.RemoveCache(cacheId.ToLower());
                            break;
                        }

                        else
                        {
                            CacheServerConfig serverConfig = GetCacheConfiguration(cacheId);
                            if (serverConfig != null &&
                                serverConfig.ClientNodes != null &&
                                serverConfig.ClientNodes.NodesList.Count > 0)
                            {
                                foreach (Alachisoft.NCache.Config.Dom.ClientNode clientNode in serverConfig.ClientNodes.NodesList)
                                {
                                    if (clientNode.Name.Equals(serverIP))
                                    {
                                        serverExistsAsClient = true;
                                        break;
                                    }
                                }

                                if (serverExistsAsClient == true)
                                {
                                    if (cacheConfiguraion.RemoveServer(serverIP))
                                    {
                                        serverListChanged = true;
                                        cacheConfiguraion.ServersPriorityList = UpdateServerPriorityList(cacheConfiguraion.ServersPriorityList);
                                        clientConfiguration.CacheConfigurationsMap[cacheId] = cacheConfiguraion;
                                        ClientConfigManager.UpdateCacheConfiguration(cacheId, clientConfiguration);
                                        break;
                                    }
                                }
                                else
                                    ClientConfigManager.RemoveCache(cacheId.ToLower());
                            }
                        }
                    }
                }

                if (!serverListChanged)
                    ClientConfigManager.RemoveCache(cacheId.ToLower());
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
        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 1)]
        public virtual void StartCache(string cacheId)
        {
            StartCache(cacheId, null, false);
        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 2)]
        public virtual void StartCache(string cacheId, string partitionId)
        {
            StartCache(cacheId, partitionId, false);
        }


        [TargetMethod(ManagementUtil.MethodName.StartCachePhase2, 1)]
        public virtual void StartCachePhase2(string cacheId)
        {
            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId);
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
        public virtual void StartCache(string cacheId, string partitionId,bool twoPhaseInitialization)
        {
            if (cacheId == null) throw new ArgumentNullException("cacheId");
            CacheInfo cacheInfo = null;
            Process process = null;
            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try ///For a finally {...} 
            {
                if (s_caches.Contains(cacheId.ToLower()))
                {
                    cacheInfo = (CacheInfo)s_caches[cacheId.ToLower()];
                }
                else
                    throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException("Cache: " + cacheId + " is not registered on the server.");
                try
                {
                    ArrayList occupiedManagementPorts = new ArrayList();
                    if (!IsRunning(cacheId))
                    {
                        ResetManagementInfo(cacheInfo);
                        foreach (CacheInfo i in s_caches.Values)
                        {
                            if (i.ManagementPort != 0)
                                occupiedManagementPorts.Add(i.ManagementPort);
                        }
                        StringBuilder cparams = new StringBuilder();
                        cparams.Append("/i").Append(" ");
                        cparams.Append(cacheId).Append(" ");
                        cparams.Append("/p").Append(" ");
                        cacheInfo.ManagementPort = ManagementPortHandler.GenerateManagementPort(occupiedManagementPorts);
                        cparams.Append(cacheInfo.ManagementPort).Append(" ");
                        ProcessExecutor processExecutor = new ProcessExecutor(cparams.ToString());
                        process = processExecutor.Execute();
                        Thread.Sleep(2000);  // Wait for some reasonable time for process to complete
                        if (process.HasExited)
                            throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException("Unable to Start Separate process. Error: " + ProcessExitCodes.list[process.ExitCode]);
                        if (process.Id != 0)
                            cacheInfo.CacheProcessId = process.Id;
                        StartCacheOnCacheHost(cacheId);
                       
                    }
                    else
                        throw new Alachisoft.NCache.Runtime.Exceptions.ManagementException("Specified cacheId is already running");
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

                    if (process != null && !process.HasExited)
                        process.Kill();
                    throw;
                }
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Stop a cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
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
            StopCache(cacheId, partitionId, CacheStopReason.Stoped);
        }

        [TargetMethod(ManagementUtil.MethodName.StopCache, 3)]
        public virtual void StopCache(string cacheId, string partitionId, CacheStopReason stopReason)
        {
            if (cacheId == null) throw new ArgumentNullException("cacheId");
            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try ///For a finally {...}
            {
                CacheInfo cacheInfo = GetCacheInfo(cacheId.ToLower());
                ICacheServer cacheServer= null;
                bool isCacheStopped = false;
                if (cacheInfo == null) throw new Exception("Specified cacheId is not registered.");
                
                if (!IsRunning(cacheInfo.CacheName)) return;
                try
                {
                    cacheServer = GetCacheServer(cacheId);
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
                            if(retries ==2)
                            {
                                isCacheStopped = true;
                            }
                        }
                        finally
                        {
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
                        }
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }
        
        private void StopAllCaches(CacheStopReason reason)
        {
            ArrayList caches = new ArrayList();
            ArrayList cacheInfos = new ArrayList();
            for (IDictionaryEnumerator idenum = s_caches.GetEnumerator(); idenum.MoveNext(); )
            {
                CacheInfo cacheInfo = (CacheInfo)idenum.Current;
                if (cacheInfo != null)
                {
                    StopCache(cacheInfo.CacheName, null);
                }
            }
        }

        [TargetMethod(ManagementUtil.MethodName.StopAllCaches, 1)]
        public void StopAllCaches()
        {
            for (IDictionaryEnumerator idenum = s_caches.GetEnumerator(); idenum.MoveNext(); )
            {
                CacheInfo cacheInfo = (CacheInfo)idenum.Current;
                if (cacheInfo != null)
                {
                    StopCache(cacheInfo.CacheName, null);
                }
            }
        }


        private void OnLicenseExpiration()
        {
            StopAllCaches(CacheStopReason.Expired);
        }

        public virtual int GetProcessID(string cacheId )
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId);
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
                    ICacheServer cacheServer = GetCacheServer(cacheInfo.CacheName);
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
                runningcaches = ManagementPortHandler.DiscoverCachesViaWMI();
                if (runningcaches == null)
                {
                    List<ProcessInfo> processInfos= ManagementPortHandler.DiscoverCachesViaNetStat();
                    if (processInfos != null && processInfos.Count != 0)
                    {
                        foreach (ProcessInfo processInfo in processInfos)
                        {
                            string name = GetCacheName(processInfo.port_number);
                            CacheHostInfo info = new CacheHostInfo();
                            info.ManagementPort = processInfo.port_number;
                            info.ProcessId = processInfo.pid;
                            runningcaches.Add(name, info);
                        }
                    }
                }

                foreach (CacheServerConfig config in configs)
                {
                    ///[] Until we completely move to using dom based configuration
                    ///we have to convert it to string props
                    string props = GetProps(config);


                    // all but the listed cache types are loaded in s_caches table.

                    string cacheId = config.Name.ToLower();

                    if (!s_caches.Contains(cacheId))
                    {
                        cacheInfo = new CacheInfo();
                        cacheInfo.CacheName = cacheId;
                        cacheInfo.CacheProps = config;
                        CacheHostInfo cacheHostinfo = runningcaches[cacheId] as CacheHostInfo;
                        if (cacheHostinfo != null)
                        {
                            cacheInfo.CacheProcessId = cacheHostinfo.ProcessId;
                            cacheInfo.ManagementPort = cacheHostinfo.ManagementPort;
                        }

                        s_caches.Add(cacheId, cacheInfo);
                    }
                    else
                    {
                        cacheInfo = s_caches[cacheId] as CacheInfo;
                        cacheInfo.CacheProps = config;
                        CacheHostInfo cacheHostinfo = runningcaches[cacheId] as CacheHostInfo;
                        if (cacheHostinfo != null)
                        {
                            cacheInfo.CacheProcessId = cacheHostinfo.ProcessId;
                            cacheInfo.ManagementPort = cacheHostinfo.ManagementPort;
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

                // Detecting Network Interface Cards with enabled IPs through WMI:
                //
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

                string[] elementKeys = { "NCacheServer.BindToIP" };

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
                return (path + @"bin\service\Alachisoft.NCache.Service.exe.config");
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
                    if (cacheInfo.CacheProps.Cluster.CacheType == "replicated-server" ||
                    cacheInfo.CacheProps.Cluster.CacheType == "partitioned-server")
                        result = CacheStatusOnServer.ClusteredCache;
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
        /// <returns>'true' if the port is available, otherwise 'flase'</returns>
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

        /// <summary>
        /// Checks whether the newly added node arise port conflict or not
        /// </summary>
        /// <param name="port">Cluster port</param>
        /// <returns>'true' if the node is allowed, otherwise 'flase'</returns>
        //public bool NodeIsAllowed(int port)

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

        private double GetConfigID(string cacheId)
        {
            try
            {
                CacheServerConfig config = GetCacheConfiguration(cacheId);
                if (config != null)
                    return config.ConfigID;
                return 0;
            }
            catch
            {
                return 0;
            }
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
                ICacheServer cacheServer = GetCacheServer(cacheId);
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
            ICacheServer cacheServer = GetCacheServer(cacheId);
            List<Alachisoft.NCache.Common.Monitoring.ClientNode> clients = new List< Alachisoft.NCache.Common.Monitoring.ClientNode> ();
            if (cacheServer != null)
                clients = cacheServer.GetCacheClients(cacheId);
            return clients;
        }

        [TargetMethod(ManagementUtil.MethodName.GetClientProcessStats, 1)]
        public virtual List<Alachisoft.NCache.Common.Monitoring.ClientProcessStats> GetClientProcessStats(string cacheId)
        {
            List<Alachisoft.NCache.Common.Monitoring.ClientProcessStats> clients = new List<Alachisoft.NCache.Common.Monitoring.ClientProcessStats>();
            ICacheServer cacheServer = GetCacheServer(cacheId);
            if (cacheServer != null)
               clients=  cacheServer.GetClientProcessStats(cacheId);
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
                            configuredCache.ProcessID = cacheInfo.CacheProcessId;

                        configuredCache.ManagementPort = cacheInfo.ManagementPort;
                        configuredCache.DataCapacity = cacheInfo.CacheProps.Storage.Size;
                        configuredCache.CachePropString = GetProps(cacheInfo.CacheProps);

                        if (cacheInfo.CacheProps.CacheType == "clustered-cache")
                        {
                            if (cacheInfo.CacheProps.Cluster != null)
                            {
                                switch (cacheInfo.CacheProps.Cluster.Topology)
                                {
                                    case "replicated-server":
                                        configuredCache.Topology = CacheTopology.Replicated;
                                        break;

                                    case "partitioned-server":
                                        configuredCache.Topology = CacheTopology.Partitioned;
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
        public virtual Alachisoft.NCache.Caching.Statistics.CacheStatistics GetCacheStatistics2(string cacheId)
        {
            if (cacheId == null)
                throw new ArgumentNullException("cacheId");
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId);
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
                    string path = AppUtil.DeployedAssemblyDir + cacheName + "\\" + fileName;
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
                ICacheServer cacheServer = GetCacheServer(cacheId);
                if (cacheServer != null)
                    return cacheServer.CanApplyHotConfiguration(cacheId, config);
                else
                    return null;
            }
            catch (Exception ex) {
                DisposeOnException(cacheId, ex);
            }
            return null;
        }


        [TargetMethod(ManagementUtil.MethodName.MakeCacheActive)]
        public void MakeCacheActive(string cacheId, bool active)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId);
                if (cacheServer != null)
                {
                    cacheServer.MakeCacheActive(cacheId, active);
                }
            }
            catch (Exception ex)
            {
                DisposeOnException(cacheId, ex);
            }
        }


        [TargetMethod(ManagementUtil.MethodName.ClearCacheContent)]
        public virtual void ClearCacheContent(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId);
                if (cacheServer != null)
                {
                    cacheServer.ClearCacheContent(cacheId);
                }
            }
            catch (Exception ex)
            {
                DisposeOnException(cacheId, ex);
            }
        }
        
        public bool StartCacheOnCacheHost(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId);
                if (cacheServer != null)
                    cacheServer.StartCache(cacheId, null);
                else
                    return false;
            }
            catch (Exception ex)
            {
                DisposeOnException(cacheId, ex);
                throw;
            }
            return true;
        }

        [TargetMethod(ManagementUtil.MethodName.StopCacheOnCacheHost, 1)]
        public virtual void StopCacheOnCacheHost (string cacheId)
        {
            try
            {
                ICacheServer cacheServer =GetCacheServer(cacheId);
                if(cacheServer!=null)
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
                ICacheServer cacheServer = GetCacheServer(cacheId);
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

        [TargetMethod(ManagementUtil.MethodName.GetStatistics)]
        public virtual CacheStatistics GetStatistics(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId);
                if (cacheServer != null)
                {
                    return cacheServer.GetStatistics(cacheId);
                }
            }
            catch (Exception ex)
            {
                DisposeOnException(cacheId, ex);
            }
            return null;
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
                CacheInfo cacheInfo = this.GetCacheInfo(cacheId.ToLower());
                if (cacheInfo != null)
                {
                    status.Status = CacheStatus.Registered;
                    if (cacheInfo.CacheProps != null)
                    {
                        status.ConfigID = cacheInfo.CacheProps.ConfigID;
                    }
                    try
                    {
                        ICacheServer cacheServer = GetCacheServer(cacheId.ToLower());
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
                ICacheServer cacheServer = GetCacheServer(cacheId);
                if (cacheServer != null)
                    return cacheServer.GetCacheCount(cacheId);
            }
            catch (Exception ex)
            {
                DisposeOnException(cacheId, ex);
            }
            return 0;
        }


        [TargetMethod(ManagementUtil.MethodName.BalanceDataloadOnCache)]
        public virtual void BalanceDataloadOnCache(string cacheId)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId);
                if (cacheServer != null)
                    cacheServer.BalanceDataloadOnCache(cacheId);
            }
            catch (Exception ex)
            {
                DisposeOnException(cacheId, ex);
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

        [TargetMethod(ManagementUtil.MethodName.StopServer)]
        public void StopServer()
        {
        }

        [TargetMethod(ManagementUtil.MethodName.GetServerPlatform)]
        public string GetServerPlatform()
        {
            return Alachisoft.NCache.Common.ServerPlatform.isDotNet;
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
                                case "replicated-server":
                                    configuredCache.Topology = CacheTopology.Replicated;
                                    break;

                                case "partitioned-server":
                                    configuredCache.Topology = CacheTopology.Partitioned;
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
            Mapping clientMapping = GenerateMapping("NCacheServer.ServerEndPoint", "NCacheServer.Port", "NCacheServer.BindToIP");
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
                string mappingString = System.Configuration.ConfigurationSettings.AppSettings[mappingKey];
                
               int port = 0;
                if(portKey.Equals("NCacheServer.ManagementPort"))
                    port = ServiceConfiguration.ManagementPort;
                else if (portKey.Equals("NCacheServer.Port"))
                    port = ServiceConfiguration.Port;

                string ip = "";

                if (ipKey.Equals("NCacheServer.BindToIP"))
                    ip = ServiceConfiguration.BindToIP.ToString();

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
                AppUtil.LogEvent("NCache", "GetServerMappingForConfig: Unable to generate mapping from "+mappingKey+". Exception: " + ex.Message, EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
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

      

        [TargetMethod(ManagementUtil.MethodName.TransferConnection)]
        public virtual void TransferConnection(System.Net.Sockets.SocketInformation socketInfo, string cacheId, byte[] transferCommand)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId);
                if (cacheServer != null)
                    cacheServer.TransferConnection(socketInfo, cacheId, transferCommand);
            }
            catch (Exception ex)
            {
                DisposeOnException(cacheId, ex);
            }
        }

        private ICacheServer GetCacheServer(string cacheId)
        {
            CacheInfo cacheInfo = null;

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
                        if (managementPort > 0)
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
                catch (Exception ex)
                {
                    DisposeOnException(cacheId, ex);
                    return null;
                }

            }
            return null;
        }

        [TargetMethod(ManagementUtil.MethodName.HotApplyConfiguration, 1)]
        public virtual void ApplyHotConfiguration(string cacheId, HotConfig hotConfig)
        {
            try
            {
                ICacheServer cacheServer = GetCacheServer(cacheId);
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

        private void DisposeOnException(string cacheId, Exception ex)
        {
            try
            {
                AppUtil.LogEvent("NCache.DisposeOnException " + ex.ToString() + "[" + ex.StackTrace + "]", System.Diagnostics.EventLogEntryType.Error);
                if (!IsRunning(cacheId.ToLower()))
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
            }
            catch (Exception) { }
        }

        private void ResetManagementInfo(CacheInfo info)
        {
            try
            {
                if (info != null)
                {
                    info.ManagementPort = 0;
                    if( info.Service!= null)
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

      
    }
}

