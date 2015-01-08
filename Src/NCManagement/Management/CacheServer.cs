// Copyright (c) 2015 Alachisoft
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
using System.Net;
using System.IO;
using System.Collections;
using System.Text;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Configuration;
using Microsoft.Win32;
using System.Timers;
using System.Collections.Generic;
using System.Timers;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Stats;


using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.RPCFramework;

using Alachisoft.NCache.Runtime.Exceptions;


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



using Runtime = Alachisoft.NCache.Runtime;
//using Alachisoft.NCache.CloudLicensing.UsageLogging.LogReports;

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

        private enum CacheStopReason
        {
            Expired,
            Stoped,
            ForcedStoped
        }

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

        public string LocalCacheIP
        {
            get { return _localCacheIp; }
            set { _localCacheIp = value; }
        }


        /// <summary> A Hashtable that contains a list of all the cache objects created so far. </summary>
        private static Hashtable s_caches = Hashtable.Synchronized(new Hashtable());

        private static Hashtable s_partitionedCaches = Hashtable.Synchronized(new Hashtable());

        /// <summary> Reader writer lock used to synchronize access to internals. </summary>
        private ReaderWriterLock _rwLock = new ReaderWriterLock();

        /// <summary>Socket server port</summary>
        private static int _socketServerPort;

        private static string _clusterIp;
        private static string _clientserverip;
        private static string _localCacheIp;
        private CacheRenderer _renderer;

        private static System.Timers.Timer _evalWarningTask;
        private TimeScheduler _gcScheduler;


        /// <summary>
        /// Static constructor
        /// </summary>
        static CacheServer()
        {
            try
            {
                LoadConfiguration();
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


        private static void NotifyEvalLicense(object source, ElapsedEventArgs e)
        {
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
            // This lease never expires
            return null;
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

                foreach (CacheInfo cacheInfo in s_caches.Values)
                {
                    try
                    {
                        if (cacheInfo != null && cacheInfo.Cache != null)
                            cacheInfo.Cache.StopInstance();
                    }
                    catch (Exception)
                    {
                    }
                }

                foreach (Hashtable partitionedTable in s_partitionedCaches.Values)
                {
                    foreach (CacheInfo cacheInfo in partitionedTable.Values)
                    {
                        try
                        {
                            if (cacheInfo != null && cacheInfo.Cache != null)
                                cacheInfo.Cache.StopInstance();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

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
                        if (this._gcScheduler != null)
                        {
                            this._gcScheduler.Stop();
                            this._gcScheduler.Dispose();
                        }
                    }
                }

            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
            if (disposing) GC.SuppressFinalize(this);
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

            if (cacheId == null) throw new ArgumentNullException("cacheId");
            cacheId = cacheId.ToLower();//check this
            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                Cache cache = GetCacheInstance(cacheId, null);
                if (cache != null)
                    cache.Clear();

               

            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }

        }


        public string LicenseKey
        {
            get
            {
                return RegHelper.GetLicenseKey(0);
            }
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
                _rwLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    if (s_caches.Contains(cacheId.ToLower()))
                    {
                        CacheInfo cacheInfo = (CacheInfo)s_caches[cacheId.ToLower()];

                        if (cacheInfo != null)
                            return cacheInfo.Cache;
                    }
                    else if (s_partitionedCaches.Contains(cacheId.ToLower()))
                    {
                        Hashtable partitionedTable = s_partitionedCaches[cacheId.ToLower()] as Hashtable;
                        IDictionaryEnumerator ide = partitionedTable.GetEnumerator();
                        LeasedCache stoppedCache = null;
                        while (ide.MoveNext())
                        {
                            CacheInfo cacheInfo = ide.Value as CacheInfo;

                            if (cacheInfo != null)
                            {
                                LeasedCache cache = cacheInfo.Cache;
                                if (cache.IsRunning)
                                    return cacheInfo.Cache;

                                if (stoppedCache == null)
                                    stoppedCache = cacheInfo.Cache;
                            }
                        }
                        return stoppedCache;
                    }
                    return null;
                }
                finally
                {
                    _rwLock.ReleaseReaderLock();
                }
            }
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
                    if (config.CacheType == "local-cache")
                    {
                        Cache instance = GetCacheInstance((string)entry.Key);
                        if (instance != null && instance.IsRunning)
                            runningCache.Add(entry.Key);
                    }
                }
                if (entry.Value is String)
                {

                    if (((string)entry.Value).IndexOf("local-cache", 0) != -1)

                    {

                        Cache instance = GetCacheInstance((string)entry.Key);
                        if (instance != null && instance.IsRunning)
                            runningCache.Add(entry.Key);
                    }
                }
            }
            return runningCache;
        }


        [TargetMethod(ManagementUtil.MethodName.GetCacheInstance, 1)]
        public Cache GetCacheInstance(string cacheId)
        {

            return this[cacheId];
        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheInstance, 2)]
        public Cache GetCacheInstance(string cacheId, string partitionId)
        {

            Cache returnCache = null;
            if (partitionId == null || partitionId == string.Empty)
            {
                returnCache = this[cacheId];
            }
            else
            {
                if (cacheId == null)
                {
                    throw new ArgumentNullException("cacheId");
                }

                _rwLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    Hashtable partitionTable = s_partitionedCaches[cacheId.ToLower()] as Hashtable;
                    if (partitionTable != null)
                    {
                        CacheInfo cacheInfo = (CacheInfo)partitionTable[partitionId.ToLower()];
                        if (cacheInfo != null)
                        {
                            returnCache = cacheInfo.Cache;
                        }
                    }
                }
                finally
                {
                    _rwLock.ReleaseReaderLock();
                }
            }

            return returnCache;
        }


        /// <summary>
        /// Gets the list of all configured caches on this server.
        /// </summary>
        /// <returns></returns>
        /// 
        [TargetMethod(ManagementUtil.MethodName.GetAllConfiguredCaches, 1)]
        public ConfiguredCacheInfo[] GetAllConfiguredCaches()
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
                    ConfiguredCacheInfo configuredCache = new ConfiguredCacheInfo();
                    configuredCache.CacheId = cacheInfo.CacheProps.Name;
                    configuredCache.IsRunning = cacheInfo.Cache.IsRunning;
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
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
            return configuredCaches;
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


        [TargetMethod(ManagementUtil.MethodName.GetCacheStatistics2, 1)]
        public Alachisoft.NCache.Caching.Statistics.CacheStatistics GetCacheStatistics2(string cacheId)
        {
            if (cacheId == null)
                throw new ArgumentNullException("cacheId");

            try
            {
                _rwLock.AcquireReaderLock(Timeout.Infinite);

                if (s_caches.Contains(cacheId.ToLower()))
                {

                    CacheInfo cacheInfo = s_caches[cacheId.ToLower()] as CacheInfo;
                    return cacheInfo == null ? null : cacheInfo.Cache.Statistics;
                }
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
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
        public Node[] GetCacheServers(string cacheId)
        {
            if (cacheId == null)
                throw new ArgumentNullException("cacheId");

            List<Node> serverNodes = new List<Node>();
            try
            {
                _rwLock.AcquireReaderLock(Timeout.Infinite);

                if (s_caches.Contains(cacheId.ToLower()))
                {
                    CacheInfo cacheInfo = s_caches[cacheId.ToLower()] as CacheInfo;
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
            Cache cache = null;
            try
            {
                if (s_caches.Contains(cacheId))
                {
                    CacheInfo cacheInfo = (CacheInfo)s_caches[cacheId];

                    if (cacheInfo != null)
                        cache = cacheInfo.Cache;

                    if (cache != null && cache.IsRunning)
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

                            if (cache != null && cache.IsRunning)
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

        [TargetMethod(ManagementUtil.MethodName.GetCacheProps, 1)]
        public IDictionary GetCacheProps()

        {
            return CacheProps;
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

                       cacheProps.Add(cacheInfo.Cache.Name, cacheInfo.CacheProps);
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
            CacheInfo cacheInfo = GetCacheInfo(cacheId);
            CacheServerConfig config = null;

            if (cacheInfo != null)
                config = cacheInfo.CacheProps;

            return config;
        }

        // New Method for New Dom [Tools]
        [TargetMethod(ManagementUtil.MethodName.GetNewConfiguration, 1)]
        public Alachisoft.NCache.Config.NewDom.CacheServerConfig GetNewConfiguration(string cacheId)
        {
            return Alachisoft.NCache.Config.NewDom.DomHelper.convertToNewDom(GetCacheConfiguration(cacheId));
        }


        [TargetMethod(ManagementUtil.MethodName.GetCacheInfo, 1)]
        public CacheInfo GetCacheInfo(string cacheId)
        {
            CacheInfo cacheInfo = null;

            if (s_caches.Contains(cacheId.ToLower()))
                cacheInfo = s_caches[cacheId.ToLower()] as CacheInfo;

            else if (s_partitionedCaches.Contains(cacheId.ToLower()))
            {
                Hashtable partitionedTable = s_partitionedCaches[cacheId.ToLower()] as Hashtable;
                if (partitionedTable != null)
                {
                    foreach (DictionaryEntry de in partitionedTable)
                    {
                        cacheInfo = de.Value as CacheInfo;
                        if (cacheInfo != null)
                        {
                            break;
                        }
                    }
                }
            }

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
                config = CacheConfigManager.GetUpdatedCacheConfig(cacheId, partId, newNode, ref affectedNodes, isJoining);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //muds:
            //update the config

            return new CacheRegisterationInfo(config, affectedNodes, affectedPartitions);
        }

        //Numan Hanif New Method For GeUpdatedCacheConfig for new Dom Changes
        [TargetMethod(ManagementUtil.MethodName.GetNewUpdatedCacheConfiguration, 1)]
        public NewCacheRegisterationInfo GetNewUpdatedCacheConfiguration(string cacheId, string partId, string newNode, bool isJoining)
        {

            Alachisoft.NCache.Management.CacheRegisterationInfo oldCacheInfo = GetUpdatedCacheConfiguration(cacheId, partId, newNode, isJoining);

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


        [TargetMethod(ManagementUtil.MethodName.GarbageCollect)]
        public void GarbageCollect(bool block, bool isCompactLOH)
        {
            
            GC.Collect(2, GCCollectionMode.Forced);
        }

        //OverLoaded Method For New Dom Config [Tools]

        [TargetMethod(ManagementUtil.MethodName.RegisterCache, 4)]
        public bool RegisterCache(string cacheId, Alachisoft.NCache.Config.NewDom.CacheServerConfig config, string partId, bool overwrite, bool hotApply)

        {
            Alachisoft.NCache.Config.Dom.CacheServerConfig oldDom = Alachisoft.NCache.Config.NewDom.DomHelper.convertToOldDom(config);

            return RegisterCache(cacheId, oldDom, partId, overwrite, hotApply);

        }

        /// <summary>
        /// Register cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <param name="props"></param>
        /// <param name="overwrite"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        /// 

        [TargetMethod(ManagementUtil.MethodName.RegisterCache, 2)]
        public bool RegisterCache(string cacheId, CacheServerConfig config, string partId, bool overwrite, bool hotApply)

        {
            if (cacheId == null) throw new ArgumentNullException("cacheId");
            cacheId = cacheId.ToLower();
            CacheInfo cacheInfo = null;

            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {

                if (s_partitionedCaches.Contains(cacheId.ToLower()))

                    throw new Runtime.Exceptions.ManagementException("A cache with same cacheId already exists");


                if (s_caches.Contains(cacheId.ToLower()))
                {
                    if (!overwrite)
                    {
                       
                        return false;
                    }
                    cacheInfo = (CacheInfo)s_caches[cacheId.ToLower()];
                    //(WARNING) This code is here to save the old value of connection-retries
                    //in the cluster configuration to avoid override by NCManager. 
                    //This code should be removed after these options appears on UI.
                    //props = GetChangedConfigForTemp(cacheId.ToLower(), cacheInfo.CacheProps, props);

                }
                else
                {
                    ///[Ata] This is until we change the use of properties in Cache
                    ///from props stirng or hashtable to Dom
                    ///                        
                    string props = GetProps(config);

                    ClientConfigManager.LocalCacheId = this.LocalCacheIP;
                    ClientConfigManager.AddCache(cacheId, config.RuntimeContext);

                    cacheInfo = new CacheInfo();
                    cacheInfo.Cache = new LeasedCache(props);

                    s_caches[cacheId.ToLower()] = cacheInfo;
                }
                cacheInfo.CacheProps = config;

                if (hotApply && cacheInfo != null && cacheInfo.Cache != null && cacheInfo.Cache.IsRunning)
                {
                    CacheConfig cc = CacheConfig.FromConfiguration(config);

                    Alachisoft.NCache.Caching.Util.HotConfig hotConfig = new Alachisoft.NCache.Caching.Util.HotConfig();
                    hotConfig.IsErrorLogsEnabled = cc.IsErrorLogsEnabled;
                    hotConfig.IsDetailedLogsEnabled = cc.IsDetailedLogsEnabled;
                    hotConfig.CacheMaxSize = cc.CacheMaxSize;
                    hotConfig.CleanInterval = cc.CleanInterval;
                    hotConfig.EvictRatio = cc.EvictRatio;

                    cacheInfo.Cache.ApplyHotConfiguration(hotConfig);
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


        // OverLoaded Method For New Dom Config [Tools]
        [TargetMethod(ManagementUtil.MethodName.RegisterCache, 3)]
        public bool RegisterCache(string cacheId, Alachisoft.NCache.Config.NewDom.CacheServerConfig config, string partId, bool overwrite, bool hotApply, bool isLocalNode)
        {
            Alachisoft.NCache.Config.Dom.CacheServerConfig oldDom = Alachisoft.NCache.Config.NewDom.DomHelper.convertToOldDom(config);
            return RegisterCache(cacheId, oldDom, partId, overwrite, hotApply, isLocalNode);
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
        public bool RegisterCache(string cacheId, CacheServerConfig config, string partId, bool overwrite, bool hotApply, bool isLocalNode)
        {

            if (cacheId == null) throw new ArgumentNullException("cacheId");

            //We load configuration before applying the new changes to preserve some old values in the config.
                      

            cacheId = cacheId.ToLower();
            CacheInfo cacheInfo = null;

            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {

                if (s_partitionedCaches.Contains(cacheId.ToLower()))

                    throw new Runtime.Exceptions.ManagementException("A cache with same cacheId already exists");


                if (s_caches.Contains(cacheId.ToLower()))
                {
                    if (!overwrite)
                    {
                        
                        return false;
                    }
                    cacheInfo = (CacheInfo)s_caches[cacheId.ToLower()];
                    //(WARNING) This code is here to save the old value of connection-retries
                    //in the cluster configuration to avoid override by NCManager. 
                    //This code should be removed after these options appears on UI.
                    //props = GetChangedConfigForTemp(cacheId.ToLower(), cacheInfo.CacheProps, props);

                }
                else
                {
                    ///This is until we change the use of properties in Cache
                    ///from props stirng or hashtable to Dom
                    ///                        
                    string props = GetProps(config);

                    ClientConfigManager.LocalCacheId = this.LocalCacheIP;
                    ClientConfigManager.AddCache(cacheId, config);

                    cacheInfo = new CacheInfo();
                    cacheInfo.Cache = new LeasedCache(props);

                    s_caches[cacheId.ToLower()] = cacheInfo;
                }
                cacheInfo.CacheProps = config;

                if (hotApply && cacheInfo != null && cacheInfo.Cache != null && cacheInfo.Cache.IsRunning)
                {
                    CacheConfig cc = CacheConfig.FromConfiguration(config);

                    Alachisoft.NCache.Caching.Util.HotConfig hotConfig = new Alachisoft.NCache.Caching.Util.HotConfig();
                    hotConfig.IsErrorLogsEnabled = cc.IsErrorLogsEnabled;
                    hotConfig.IsDetailedLogsEnabled = cc.IsDetailedLogsEnabled;
                    hotConfig.CacheMaxSize = cc.CacheMaxSize;
                    hotConfig.CleanInterval = cc.CleanInterval;
                    hotConfig.EvictRatio = cc.EvictRatio;
                    
                    cacheInfo.Cache.ApplyHotConfiguration(hotConfig);
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

        private string GetChangedConfigForTemp(string cacheId, string oldProps, string updatedProps)
        {
            if (oldProps != null)
            {
                PropsConfigReader pcr = new PropsConfigReader(oldProps);
                IDictionary cacheConfig = pcr.Properties;
                if (cacheConfig != null)
                {
                    Hashtable configTable = new Hashtable();
                    cacheConfig.Add("id", cacheId);
                    configTable.Add("configuration", cacheConfig);
                    CacheServerConfig[] configDomList = ConfigConverter.ToDom(configTable);
                    if (configDomList != null && configDomList.Length > 0)
                    {
                        CacheServerConfig oldConfig = configDomList.GetValue(0) as CacheServerConfig;
                        if (oldConfig != null)
                        {
                            if (oldConfig.Cluster != null && oldConfig.Cluster.Channel != null && oldConfig.Cluster.Channel != null)
                            {
                                int retries = oldConfig.Cluster.Channel.ConnectionRetries;
                                int retryInterval = oldConfig.Cluster.Channel.ConnectionRetryInterval;

                                int indexOfTcpConfig = updatedProps.ToLower().IndexOf("tcp(");

                                if (indexOfTcpConfig > -1)
                                {
                                    string str = "connection_retries=" + retries + ";connection_retry_interval=" + retryInterval + ";";
                                    updatedProps = updatedProps.Insert(indexOfTcpConfig + 4, str);
                                }
                            }
                        }
                    }

                }
            }
            return updatedProps;
        }
        [TargetMethod(ManagementUtil.MethodName.GetNodeInfo, 1)]
        public NodeInfoMap GetNodeInfo()
        {
            Hashtable nodeInfo = new Hashtable();
            nodeInfo[Channel.Cluster] = ClusterIP;
            nodeInfo[Channel.SocketServer] = ClientConfigManager.BindIP;
            return new NodeInfoMap(nodeInfo);
        }

        public Exception CanApplyHotConfig(string cacheId, CacheServerConfig config)
        {
            CacheInfo cacheInfo = null;
            cacheId = cacheId.ToLower();
            Hashtable result = new Hashtable();

            Alachisoft.NCache.Caching.Util.HotConfig hotConfig = new Alachisoft.NCache.Caching.Util.HotConfig();

            hotConfig.IsErrorLogsEnabled = config.Log.TraceErrors;
            hotConfig.IsDetailedLogsEnabled = config.Log.TraceNotices;
            hotConfig.CacheMaxSize = config.Storage.Size * 1024 * 1024; //from MBs to bytes
            hotConfig.CleanInterval = config.Cleanup.Interval;
            hotConfig.EvictRatio = (float)config.EvictionPolicy.EvictionRatio;

            if (s_caches.Contains(cacheId))
            {
                cacheInfo = (CacheInfo)s_caches[cacheId];
                return cacheInfo.Cache.CanApplyHotConfig(hotConfig);
            }

            return null;
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
                ArrayList caches = new ArrayList();
                _rwLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    IDictionaryEnumerator en = s_caches.GetEnumerator();
                    while (en.MoveNext())
                    {
                        CacheInfo cacheInfo = (CacheInfo)en.Value;

                       
                        caches.Add(cacheInfo.Cache);
                    }
                    return caches;
                }
                finally
                {
                    _rwLock.ReleaseReaderLock();
                }
            }
        }

        public IDictionary PartitionedReplicaCaches
        {
            get
            {
                Hashtable caches = new Hashtable();
                Hashtable tmp = null;
                _rwLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    IDictionaryEnumerator en = s_partitionedCaches.GetEnumerator();
                    while (en.MoveNext())
                    {
                        string cacheId = en.Key as string;

                        if (!caches.Contains(cacheId.ToLower()))
                        {
                            tmp = new Hashtable();
                            caches.Add(cacheId.ToLower(), tmp);
                        }

                        Hashtable partitionedCaches = en.Value as Hashtable;
                        if (partitionedCaches != null)
                        {
                            foreach (DictionaryEntry de in partitionedCaches)
                            {
                                string partId = de.Key as string;
                                CacheInfo cacheInfo = (CacheInfo)de.Value;

                                if (tmp != null)
                                    tmp.Add(partId, cacheInfo.Cache);
                            }
                        }
                    }
                    return caches;
                }
                finally
                {
                    _rwLock.ReleaseReaderLock();
                }
            }
        }


        [TargetMethod(ManagementUtil.MethodName.RemoveCacheServerFromClientConfig, 1)]
        public void RemoveCacheServerFromClientConfig(string cacheId, string serverName)
        {
            ClientConfigManager.RemoveCacheServer(cacheId, serverName);
        }

        [TargetMethod(ManagementUtil.MethodName.RemoveCacheFromClientConfig, 1)]
        public void RemoveCacheFromClientConfig(string cacheId)
        {
            ClientConfigManager.RemoveCache(cacheId);
        }

        [TargetMethod(ManagementUtil.MethodName.UpdateClientServersList, 1)]
        public void UpdateClientServersList(string cacheId, CacheServerList serversPriorityList, string serverRuntimeContext)
        {
            ClientConfigManager.UpdateServerNodes(cacheId, serversPriorityList, serverRuntimeContext == "1" ? RtContextValue.JVCACHE : RtContextValue.NCACHE);
        }

        [TargetMethod(ManagementUtil.MethodName.UpdateClientServersList, 2)]
        public void UpdateClientServersList(string cacheId, string[] servers, ref string xml, bool loadBalance)
        {
            ClientConfigManager.UpdateServerNodes(cacheId, servers, ref xml, loadBalance);
        }

        [TargetMethod(ManagementUtil.MethodName.GetClientConfiguration, 1)]
        public ClientConfiguration.Dom.ClientConfiguration GetClientConfiguration(string cacheId)
        {
            return ClientConfigManager.GetClientConfiguration(cacheId);
        }

        [TargetMethod(ManagementUtil.MethodName.UpdateClientConfiguration, 1)]
        public void UpdateClientConfiguration(string cacheId, ClientConfiguration.Dom.ClientConfiguration configuration)
        {
            ClientConfigManager.UpdateCacheConfiguration(cacheId, configuration);
        }

        [TargetMethod(ManagementUtil.MethodName.GetBindIP, 1)]
        public string GetBindIP()
        {
            return ClientConfigManager.BindIP;
        }

        [TargetMethod(ManagementUtil.MethodName.GetClientConfigId, 1)]
        public int GetClientConfigId()
        {
            return ClientConfigManager.GetConfigurationId();
        }


        [TargetMethod(ManagementUtil.MethodName.GetClientNodeStatus, 1)]
        public ClientNodeStatusWrapper GetClientNodeStatus(string cacheId)
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
                this._renderer.SetLoggingStatus(subsystem, type, LoggingInfo.LogsStatus.Enable);
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
                this._renderer.SetLoggingStatus(subsystem, type, LoggingInfo.LogsStatus.Disable);
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

            string bindIP = System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.BindToIP"];

            IPAddress ipAddr = null;

            if (bindIP != null && bindIP != string.Empty)
            {
                try
                {
                    IPAddress.Parse(bindIP);

                }
                catch (Exception)
                {
                    bindIP = System.Environment.MachineName.ToLower();
                }
            }
            else
            {
                bindIP = System.Environment.MachineName.ToLower();
            }
            _clientserverip = bindIP;
            ClientConfigManager.BindIP = bindIP;

            ClientConfigManager.AvailableNIC(DetectNICs());


            ClientConfigManager.LoadConfiguration();

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

            if (s_caches.Contains(cacheId))
            {
                _rwLock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    CacheInfo cacheInfo = (CacheInfo)s_caches[cacheId.ToLower()];
                    if (cacheInfo != null)
                    {
                        cacheInfo.CacheProps = props;
                        SaveConfiguration();
                    }
                }
                finally
                {
                    _rwLock.ReleaseWriterLock();
                }
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

                    RemoveClientCacheConfiguration(cacheId, removeServerOnly);

                    StopCache(cacheId);


                    CacheInfo cacheInfo = (CacheInfo)s_caches[cacheId.ToLower()];
                    LeasedCache cache = null;

                    if (cacheInfo != null)
                        cache = cacheInfo.Cache;

                    if (cache != null)
                        cache.Dispose();



                    s_caches.Remove(cacheId);

                    RemoveDeployedAssemblies(cacheId); // no need to check return values; if remove then fine else not need to break the process

                }
                else if (s_partitionedCaches.Contains(cacheId.ToLower()))
                {


                    StopCache(cacheId);


                    Hashtable partitionedTable = s_partitionedCaches[cacheId.ToLower()] as Hashtable;
                    if (partitionedTable != null)
                    {
                        IDictionaryEnumerator ide = partitionedTable.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            CacheInfo cacheInfo = ide.Value as CacheInfo;
                            LeasedCache cache = null;

                            if (cacheInfo != null)
                                cache = cacheInfo.Cache;

                            if (cache != null)
                                cache.Dispose();
                        }
                        s_partitionedCaches.Remove(cacheId);

                    }
                }

                SaveConfiguration();
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
                    if (cacheConfiguraion.CacheId == cacheId)
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
                                    if (clientNode.Name == serverIP)
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
        public void StartCache(string cacheId)
        {
            StartCache(cacheId, null, false);
        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 2)]
        public void StartCache(string cacheId, string partitionId)
        {
            StartCache(cacheId, partitionId, false);
        }


        [TargetMethod(ManagementUtil.MethodName.StartCachePhase2, 1)]
        public void StartCachePhase2(string cacheId)
        {
            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                LeasedCache cache = GetCacheInstance(cacheId, null) as LeasedCache;
                if (cache != null) cache.StartInstancePhase2();
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
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
        public void StartCache(string cacheId, string partitionId,
          bool twoPhaseInitialization)
        {
            if (cacheId == null) throw new ArgumentNullException("cacheId");
            CacheInfo cacheInfo = null;
            ArrayList caches = null;
            LeasedCache cache = null;
            ArrayList cacheInfos = null;

            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try ///For a finally {...} 
            {
                try
                {
                    LoadConfiguration();
                }
                catch (Exception e)
                {
                    string msg = String.Format("CacheServer failed to load configuration, Error {0}", e.Message);
                    AppUtil.LogEvent(msg, EventLogEntryType.Warning);
                }

                if (s_caches.Contains(cacheId.ToLower()))
                {
                    cacheInfo = (CacheInfo)s_caches[cacheId.ToLower()];

                    if (cacheInfo != null)
                        cache = cacheInfo.Cache;

                    if (cache != null)
                    {
                        if (caches == null)
                            caches = new ArrayList();

                        if (cacheInfos == null)
                            cacheInfos = new ArrayList();

                        caches.Add(cache);
                        cacheInfos.Add(cacheInfo);
                    }
                }
                else if (s_partitionedCaches.Contains(cacheId.ToLower()))
                {
                    Hashtable partitionTable = s_partitionedCaches[cacheId.ToLower()] as Hashtable;
                    if (partitionTable != null)
                    {
                        IDictionaryEnumerator ide = partitionTable.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            cacheInfo = (CacheInfo)ide.Value;

                            if (cacheInfo != null)
                                cache = cacheInfo.Cache;

                            if (cache != null)
                            {
                                if (caches == null)
                                    caches = new ArrayList();

                                if (cacheInfos == null)
                                    cacheInfos = new ArrayList();

                                caches.Add(cache);
                                cacheInfos.Add(cacheInfo);
                            }
                        }
                    }
                }

                try
                {
                    StartCacheInstance(caches, cacheInfos,twoPhaseInitialization);
                    AppUtil.LogEvent(_cacheserver, "\"" + cacheId + "\"" + " started successfully.", EventLogEntryType.Information, EventCategories.Information, EventID.CacheStart);
                }
                catch (Exception e)
                {
                    AppUtil.LogEvent(_cacheserver, "\"" + cacheId + "\" can not be started.\n" + e.ToString(), System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.CacheStartError);
                    throw;
                }
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }

        private void StartCacheInstance(ArrayList caches, ArrayList cacheInfos,
            bool twoPhaseInitialization)
        {
            if (caches != null && caches.Count > 0)
            {
                for (int i = 0; i < caches.Count; i++)
                {
                    LeasedCache cache = caches[i] as LeasedCache;
                    CacheInfo cacheInfo = cacheInfos[i] as CacheInfo;

                    if (!cache.IsRunning)
                    {
                        cacheInfo.SyncConfiguration();
                        cache.StartInstance(_renderer, twoPhaseInitialization);


                    }
                }
            }
            else

                throw new Runtime.Exceptions.ManagementException("Specified cache name is not registered");

        }

        [TargetMethod(ManagementUtil.MethodName.StopCache, 1)]
        public void StopCache(string cacheId)
        {
            StopCache(cacheId, null);
        }

        /// <summary>
        /// Stop a cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        [TargetMethod(ManagementUtil.MethodName.StopCache, 2)]
        public void StopCache(string cacheId, string partitionId)

        {
            StopCache(cacheId, partitionId, CacheStopReason.Stoped);
        }

        private void StopCache(string cacheId, string partitionId, CacheStopReason stopReason)
        {
            if (cacheId == null) throw new ArgumentNullException("cacheId");
            ArrayList cacheInfos = null;
            ArrayList caches = null;
            CacheInfo cacheInfo = null;
            LeasedCache cache = null;

            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try ///For a finally {...}
            {

                cacheInfo = GetCacheInfo(cacheId.ToLower());
                if (cacheInfo != null)
                {
                    cache = cacheInfo.Cache;
                }
                if (cache != null)
                {
                    if (caches == null)
                        caches = new ArrayList();

                    if (cacheInfos == null)
                        cacheInfos = new ArrayList();

                    caches.Add(cache);
                    cacheInfos.Add(cacheInfo);
                }

                try
                {

                    StopCacheInstance(caches, cacheInfos, stopReason);
                    AppUtil.LogEvent(_cacheserver, "\"" + cacheId + "\"" + " stopped successfully.", EventLogEntryType.Information, EventCategories.Information, EventID.CacheStop);

                }
                catch (Exception e)
                {

                    AppUtil.LogEvent(_cacheserver, "\"" + cacheId + "\" can not be stopped.\n" + e.ToString(), System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.CacheStopError);

                    throw;
                }
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }

        
        private static void StopAllCaches(CacheStopReason reason)
        {
            ArrayList caches = new ArrayList();
            ArrayList cacheInfos = new ArrayList();
            foreach(DictionaryEntry de in s_caches)
            {
                //CacheInfo should not be null
                if (de.Value != null)
                {
                    caches.Add(((de.Value) as CacheInfo).Cache);
                    cacheInfos.Add(de.Value);
                }
            }
            StopCacheInstance(caches, cacheInfos, reason);
        }

        private static void OnLicenseExpiration()
        {
                StopAllCaches(CacheStopReason.Expired);
        }

        private static void StopCacheInstance(ArrayList caches, ArrayList cacheInfos, CacheStopReason reason)
        {
            if (caches != null && caches.Count > 0)
            {
                for (int i = 0; i < caches.Count; i++)
                {
                    LeasedCache cache = caches[i] as LeasedCache;
                    CacheInfo cacheInfo = cacheInfos[i] as CacheInfo;

                    if (cache.IsRunning)
                    {
                        if(reason == CacheStopReason.Expired)
                            cache.NCacheLog.CriticalInfo("NCache license has expired on this machine. Stopping cache...");
                        cache.StopInstance();
                        cacheInfo.SyncConfiguration();

                        //instrumentation Code

                    }
                }
            }
            else
                throw new Runtime.Exceptions.ManagementException("Specified cache name is not registered");
        }

        /// <summary>
        /// Load all the config sections from the configuration file.
        /// </summary>
        static private void LoadConfiguration()
        {
            CacheInfo cacheInfo = null;
            try
            {
                CacheServerConfig[] configs = CacheConfigManager.GetConfiguredCaches();
                //Just need to call the static block - stupid logik i know
                Alachisoft.NCache.Util.MiscUtil.RegisterCompactTypes();
                for (int i = 0; i < configs.Length; i++)
                {
                    CacheServerConfig config = configs[i];

                    ///[Ata] Until we completely move to using dom based configuration
                    ///we have to convert it to string props
                    string props = GetProps(config);

                    cacheInfo = new CacheInfo();
                    cacheInfo.Cache = new LeasedCache(props);
                    cacheInfo.CacheProps = config;

                    // all but the listed cache types are loaded in s_caches table.
                    if (!(cacheInfo.Cache.Statistics.ClassName == "replicated-client"
                        || cacheInfo.Cache.Statistics.ClassName == "partitioned-replicas-client"
                        || cacheInfo.Cache.Statistics.ClassName == "partitioned-client"))
                    {
                        string cacheId = config.Name.ToLower();

                        if (!s_caches.Contains(cacheId))
                        {
                            s_caches.Add(cacheId, cacheInfo);
                        }
                        else
                        {
                            cacheInfo = s_caches[cacheId] as CacheInfo;
                            cacheInfo.CacheProps = config;
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

        private static string GetProps(CacheServerConfig[] configs)
        {
            Hashtable table = ConfigConverter.ToHashtable(configs);
            string props = ConfigReader.ToPropertiesString(table);
            return props;
        }

        static private void SaveConfiguration(Alachisoft.NCache.Config.Dom.CacheServerConfig cacheConfig)
        {
            try
            {
                if (!s_caches.Contains(cacheConfig.Name) && !s_caches.Contains(cacheConfig.Name.ToLower()))
                    CacheConfigManager.SaveConfiguration(s_caches, s_partitionedCaches);
                else
                    throw new Exception("Cache with the same name is already registered on the server.");
            }
            catch (Exception e)
            {
                string msg = String.Format("Error: {0}", e.Message);
                
                throw new Exception(msg);
            }
        }
        /// <summary>
        /// Save caches to configuration
        /// </summary>
        static private void SaveConfiguration()
        {
            try
            {
                CacheConfigManager.SaveConfiguration(s_caches, s_partitionedCaches);
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
                        if (!connectedNICs.ContainsKey(ipAddress))
                        {
                            connectedNICs.Add(ipAddress, mo.GetPropertyValue("Description"));
                        }
                    }
                }


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
            int maxPort = 0, loop = 1;

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
                        maxPort = cfg.ClusterPort + cfg.ClusterPortRange - 1;
                        loop++;
                    }
                    else if (maxPort < (cfg.ClusterPort + cfg.ClusterPortRange))
                    {
                        maxPort = cfg.ClusterPort + cfg.ClusterPortRange - 1;
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
                if (s_caches.Contains(cacheId))
                {
                    CacheInfo cacheInfo = s_caches[cacheId] as CacheInfo;

                    if (cacheInfo.Cache.Statistics.ClassName == "partitioned-server" ||  cacheInfo.Cache.Statistics.ClassName == "replicated-server")
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
        [TargetMethod(ManagementUtil.MethodName.PortIsAvailable, 1)]
        public bool PortIsAvailable(int port)
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
                    if (cfg.ClusterPort != 0)
                    {
                        for (int i = 0; i < cfg.ClusterPortRange; i++)
                        {
                            if (port == cfg.ClusterPort + i)
                            {
                                isAvailable = false;
                            }
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
                        if (cfg.ClusterPort != 0)
                        {
                            for (int i = 0; i < cfg.ClusterPortRange; i++)
                            {
                                if (port == cfg.ClusterPort + i)
                                {
                                    isAvailable = false;
                                }
                            }
                        }
                        break;
                    }
                }

            }

            return isAvailable;
        }

        /// <summary>
        /// Checks whether the newly added node arise port conflict or not
        /// </summary>
        /// <param name="port">Cluster port</param>
        /// <returns>'true' if the node is allowed, otherwise 'flase'</returns>
        

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
        /// Gets the status of NCache on this node.
        /// </summary>
        /// <returns>The ServerStatus.</returns>

        [TargetMethod(ManagementUtil.MethodName.GetCacheStatus, 1)]
        public StatusInfo GetCacheStatus(string cacheId, string partitionId)
        {
            StatusInfo status = new StatusInfo();
            CacheInfo cacheInfo = this.GetCacheInfo(cacheId);

            if (cacheInfo != null && cacheInfo.CacheProps != null)
            {
                status.ConfigID = cacheInfo.CacheProps.ConfigID;
            }


            if (cacheInfo != null)
            {
                LeasedCache cache = cacheInfo.Cache;
                if (cache != null)
                {
                    status.Status = cache.IsRunning ? CacheStatus.Running : CacheStatus.Registered;

#if !CLIENT
                        status.IsCoordinator = cache.IsCoordinator;
#endif
                }
            }

            return status;
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
                AppUtil.LogEvent("An error occured while starting activity monitoring " + e.ToString(), EventLogEntryType.Error);
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
                AppUtil.LogEvent("An error occured while stopping activity monitoring " + e.ToString(), EventLogEntryType.Error);
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
                AppUtil.LogEvent("An error occured while logging client activity " + e.ToString(), EventLogEntryType.Error);
                throw;
            }
            finally
            {
                logger.Close();
            }
        }


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
        public string CanApplyHotConfiguration(string cacheId, CacheServerConfig config)
        {
            Exception e = CanApplyHotConfig(cacheId, config);

            if (e != null) return e.Message;
            return null;
        }



        [TargetMethod(ManagementUtil.MethodName.ClearCacheContent)]
        public void ClearCacheContent(string cacheId)
        {
            Cache cache = GetCacheInstance(cacheId, null);

            if (cache != null)
                cache.Clear();
        }

        [TargetMethod(ManagementUtil.MethodName.IsRunning)]
        public bool IsRunning(string cacheId)
        {
            Cache cache = GetCacheInstance(cacheId, null);

            if (cache != null)
                return cache.IsRunning;

            return false;
        }

        [TargetMethod(ManagementUtil.MethodName.GetStatistics)]
        public CacheStatistics GetStatistics(string cacheId)
        {
            Cache cache = GetCacheInstance(cacheId, null);

            if (cache != null && cache.IsRunning)
                return cache.Statistics;

            return null;
        }

        [TargetMethod(ManagementUtil.MethodName.GetCacheCount)]
        public long GetCacheCount(string cacheId)
        {
            Cache cache = GetCacheInstance(cacheId, null);

            if (cache != null)
                return cache.Count;

            return 0;
        }


        [TargetMethod(ManagementUtil.MethodName.BalanceDataloadOnCache)]
        public void BalanceDataloadOnCache(string cacheId)
        {
            Cache cache = GetCacheInstance(cacheId, null);

            if (cache != null)
                cache.BalanceDataLoad();
        }


        [TargetMethod(ManagementUtil.MethodName.IsCacheRegistered)]
        public bool IsCacheRegistered(string cacheId)
        {
            if (cacheId != null)
            {
                if (s_caches[cacheId.ToLower()] != null)
                    return true;
            }

            return false;
        }

        [TargetMethod(ManagementUtil.MethodName.StopServer)]
        public void StopServer()
        {
            Console.WriteLine("Stop server called.");
        }

        [TargetMethod(ManagementUtil.MethodName.GetServerPlatform)]
        public string GetServerPlatform()
        {
            return Alachisoft.NCache.Common.ServerPlatform.isDotNet;
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
                string port = System.Configuration.ConfigurationSettings.AppSettings[portKey];
                string ip = System.Configuration.ConfigurationSettings.AppSettings[ipKey];

                if (!(String.IsNullOrEmpty(mappingString) ||
                      String.IsNullOrEmpty(port) ||
                      String.IsNullOrEmpty(ip)))
                {
                    string[] mappingAddress = mappingString.Split(':');
                    if (mappingAddress.Length == 2)
                    {
                        Mapping mapping = new Mapping();
                        mapping.PublicIP = mappingAddress[0];
                        mapping.PublicPort = Convert.ToInt32(mappingAddress[1]);
                        mapping.PrivateIP = ip;
                        mapping.PrivatePort = Convert.ToInt32(port);
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
    
    }

   
}

