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
using System.Collections;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Lifetime;
using System.Diagnostics;
using System.Net;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Remoting;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Statistics;

using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Config.Dom;
using System.Collections.Generic;

using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Caching.Queries;

using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Caching.Enumeration;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered;
#endif

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// The main class that is the interface of the system with the outside world. This class
    /// is remotable (MarshalByRefObject). 
    /// </summary>
    public class Cache : MarshalByRefObject, IEnumerable, ICacheEventsListener, IClusterEventsListener, IDisposable
    {

        #region	/                 --- Performance statistics collection Task ---           /

        #endregion

        /// <summary> The name of the cache instance. </summary>
        private CacheInfo _cacheInfo = new CacheInfo();

        /// <summary> The runtime context associated with the current cache. </summary>
        private CacheRuntimeContext _context = new CacheRuntimeContext();

        /// <summary> sponsor object </summary>
        private ISponsor _sponsor = new RemoteObjectSponsor();

        /// <summary> The runtime context associated with the current cache. </summary>
        private RemotingChannels _channels;

        /// <summary> delegate for custom  remove callback notifications. </summary>
        private event CustomRemoveCallback _customRemoveNotif;

        /// <summary> delegate for custom update callback notifications. </summary>
        private event CustomUpdateCallback _customUpdateNotif;
        private event CacheStoppedCallback _cacheStopped;


        private event NodeJoinedCallback _memberJoined;
        private event NodeLeftCallback _memberLeft;

        private event ClientsInvalidatedCallback _clientsInvalidated;
        private event HashmapChangedCallback _hashmapChanged;

        public delegate void CacheStoppedEvent(string cacheName);
        public delegate void CacheStartedEvent(string cacheName);

        public static CacheStoppedEvent OnCacheStopped;
        public static CacheStartedEvent OnCacheStarted;

        private event ConfigurationModified _configurationModified;

        private ArrayList _connectedClients = new ArrayList();

        /// <summary> Indicates wtherher a cache is InProc or not. </summary>
        private bool _inProc;

        public static int ServerPort;

        private static float s_clientsRequests = 0;
        private static float s_clientsBytesRecieved = 0;
        private static float s_clientsBytesSent = 0;
        private long _lockIdTicker = 0;

        /// <summary> Holds the cach type name. </summary>
        private string _cacheType;
        
        private bool _isPersistEnabled = false;
        private int _persistenceInterval = 5;

       
        string _cacheserver = "NCache";

        public bool IsPersistEnabled
        {
            get { return _isPersistEnabled; }
            set { _isPersistEnabled = value; }
        }
        public int PersistenceInterval
        {
            get { return _persistenceInterval; }
            set { _persistenceInterval = value; }
        }
        public bool IsInProc
        {
            get { return _inProc; }
        }

        internal CacheRuntimeContext Context
        {
            get { return _context; }
        }

        /// <summary> Thread to reset Instantaneous Stats after every one second.</summary>
        private Thread _instantStatsUpdateTimer;
        private bool threadRunning = true;
        TimeSpan InstantStatsUpdateInterval = new TimeSpan(0, 0, 1);
  
        static bool s_logClientEvents;

        /// <summary>
        /// Default constructor.
        /// </summary>
        static Cache()
        {
            MiscUtil.RegisterCompactTypes();
            string tmpStr = System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.LogClientEvents"];
            if (tmpStr != null && tmpStr != "")
            {
                s_logClientEvents = Convert.ToBoolean(tmpStr);
            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Cache()
        {
            _context.CacheRoot = this;
        }

        public ILogger NCacheLog
        {
            get { return _context.NCacheLog; }
        }

        /// <summary>
        /// Overlaoded constructor, used internally by Cache Manager.
        /// </summary>
        /// <param name="configString">property string used to create cache.</param>

        protected internal Cache(string configString)
        {
            _context.CacheRoot = this;
            _cacheInfo = ConfigHelper.GetCacheInfo(configString);
        }
        /// <summary>
        /// Overlaoded constructor, used internally by Cache Manager.
        /// </summary>
        /// <param name="configString">property string used to create cache.</param>
        protected internal Cache(string configString, CacheRenderer renderer)
        {
            _context.CacheRoot = this;
            _cacheInfo = ConfigHelper.GetCacheInfo(configString);
            _context.Render = renderer;
        }

        /// <summary>
        /// Finalizer for the cache.
        /// </summary>
        ~Cache()
        {
            //remoting specific disposing
            if (this != null)
            {
                try
                {
                    RemotingServices.Disconnect(this);
                }
                catch (Exception) { }
            }

            Dispose(false);
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            lock (this)
            {
                if (_cacheStopped != null && !CacheType.Equals("mirror-server"))
                {
                    Delegate[] invocationList = this._cacheStopped.GetInvocationList();

                    foreach (Delegate subscriber in invocationList)
                    {
                        CacheStoppedCallback callback = (CacheStoppedCallback)subscriber;
                        try
                        {
                            callback(this._cacheInfo.Name, null);
                        }
                        catch (Exception e)
                        {
                            NCacheLog.Error("Cache.Dispose", "Error occurred while invoking cache stopped event: " + e.ToString());
                            //Ignore and move on to fire next
                        }
                        finally
                        {
                            this._cacheStopped -= callback;
                        }
                    }
                }

                try
                {
                    if (_inProc) RemotingServices.Disconnect(this);
                }
                catch (Exception) { }

                if (_context.CacheImpl != null)
                    _context.CacheImpl.StopServices();
                

               

               

                if (_connectedClients != null)
                    lock (_connectedClients.SyncRoot)
                    {
                        _connectedClients.Clear();
                    }

                _cacheStopped = null;
                _sponsor = null;

                if (NCacheLog != null)
                {
                    NCacheLog.CriticalInfo("Cache.Dispose", "Cache stopped successfully");
                    NCacheLog.Flush();
                    NCacheLog.Close();
                }


                try
                {
                    if (_channels != null) _channels.UnregisterTcpChannels();
                }
                catch (Exception) { }
                _channels = null;
                GC.Collect();

                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }

                if (_context != null)
                {
                    _context.Dispose();
                }

                //Dispose snaphot pool for this cache.
                if (disposing)
                {
                    CacheSnapshotPool.Instance.DisposePool(_context.CacheRoot.Name);
                }

            }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            try
            {
                Dispose(true);
                
            }
            catch (Exception exp)
            {
                throw;
            }
        }

        #endregion

        /// <summary>
        /// Name of the cache.
        /// </summary>
        public String Name
        {
            get { return _cacheInfo.Name; }
        }

        /// <summary>
        /// Property string used to create cache.
        /// </summary>
        public String ConfigString
        {
            get { return _cacheInfo.ConfigString; }
            set { _cacheInfo.ConfigString = value; }
        }

        /// <summary>
        /// Returns true if the cache is running, false otherwise.
        /// </summary>
        public bool IsRunning
        {
            get { return _context.CacheImpl != null; }
        }


#if !CLIENT
        public bool IsCoordinator
        {
            get
            {
                if (_context.IsClusteredImpl)
                {
                    return ((ClusterCacheBase)_context.CacheImpl).Cluster.IsCoordinator;
                }
                else
                    return false;
            }
        }
#endif
        /// <summary>
        /// Get the running cache type name.
        /// </summary>
        public string CacheType
        {
            get { return _cacheType; }
        }

        public bool SerializationEnabled
        {
            get { return true; }
        }

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        public long Count
        {
            get
            {
                // Cache has possibly expired so return default.
                if (!IsRunning) return 0;
                return _context.CacheImpl.Count;
            }
        }

        /// <summary>
        /// returns the statistics of the Clustered Cache.
        /// </summary>
        public CacheStatistics Statistics
        {
            get
            {
                // Cache has possibly expired so return default.
                if (!IsRunning) return new CacheStatistics(String.Empty, _cacheInfo.ClassName);
                return _context.CacheImpl.Statistics;
            }
        }


        public event CacheStoppedCallback CacheStopped
        {
            add { _cacheStopped += value; }
            remove { _cacheStopped -= value; }
        }

        /// <summary> delegate for cusotm remove callback notifications.  </summary>
        public event CustomRemoveCallback CustomRemoveCallbackNotif
        {
            add { _customRemoveNotif += value; }
            remove { _customRemoveNotif -= value; }
        }



        /// <summary> delegate for cusotm update callback notifications.  </summary>
        public event CustomUpdateCallback CustomUpdateCallbackNotif
        {
            add { _customUpdateNotif += value; }
            remove { _customUpdateNotif -= value; }
        }


        public event NodeJoinedCallback MemberJoined
        {
            add { _memberJoined += value; }
            remove { _memberJoined -= value; }
        }

        public event NodeLeftCallback MemberLeft
        {
            add { _memberLeft += value; }
            remove { _memberLeft -= value; }
        }

        public event ConfigurationModified ConfigurationModified
        {
            add { _configurationModified += value; }
            remove { _configurationModified -= value; }
        }

        public event HashmapChangedCallback HashmapChanged
        {
            add { _hashmapChanged += value; }
            remove { _hashmapChanged -= value; }
        }

        public event ClientsInvalidatedCallback ClientsInvalidated
        {
            add { _clientsInvalidated += value; }
            remove { _clientsInvalidated -= value; }
        }

        #region	/                 --- Initialization ---           /

        /// <summary>
        /// Starts the cache functionality.
        /// </summary>
        /// <param name="renderer">It provides the events like onCientConnected etc from the SocketServer. 
        /// Also the portSocketServer is listenting for client connections.</param>
        protected internal virtual void Start(CacheRenderer renderer, bool twoPhaseInitialization)
        {
            Start(renderer, false, twoPhaseInitialization);
        }
        internal protected void StartPhase2()
        {

        }
        /// <summary>
        /// Start the cache functionality.
        /// </summary>
        protected internal virtual void Start(CacheRenderer renderer, bool isStartingAsMirror, bool twoPhaseInitialization)
        {
            try
            {
                if (IsRunning)
                {
                    Stop();
                }
                ConfigReader propReader = new PropsConfigReader(ConfigString);
                _context.Render = renderer;
                if (renderer != null)
                {
                    renderer.OnClientConnected += new CacheRenderer.ClientConnected(OnClientConnected);
                    renderer.OnClientDisconnected += new CacheRenderer.ClientDisconnected(OnClientDisconnected);
                }
                Initialize(propReader.Properties, false, isStartingAsMirror, twoPhaseInitialization);
            }
            catch (Exception exception)
            {
               throw;
            }
        }

        /// <summary>
        /// Fired when a client is connected with the socket server.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="cacheId"></param>
        public void OnClientDisconnected(string client, string cacheId)
        {
            if (_context.CacheImpl != null) 
            {
                lock (_connectedClients.SyncRoot)
                {
                    _connectedClients.Remove(client);
                }

                _context.CacheImpl.ClientDisconnected(client, _inProc);

                if (s_logClientEvents)
                {
                    AppUtil.LogEvent(_cacheserver, "Client \"" + client + "\" has disconnected from " + _cacheInfo.Name, EventLogEntryType.Information, EventCategories.Information, EventID.ClientDisconnected);
                }
                _context.NCacheLog.CriticalInfo("Cache.OnClientDisconnected", "Client \"" + client + "\" has disconnected from cache");

            }
        }

        public void OnClientForceFullyDisconnected(string clientId)
        {
            if (s_logClientEvents)
            {
                AppUtil.LogEvent(_cacheserver, "Client \"" + clientId + "\" has forcefully been disconnected due to scoket dead lock from " + _cacheInfo.Name, EventLogEntryType.Information, EventCategories.Information, EventID.ClientDisconnected);
            }
            _context.NCacheLog.CriticalInfo("Cache.OnClientForceFullyDisconnected", "Client \"" + clientId + "\" has disconnected from cache");

        }

        public void OnClientConnected(string client, string cacheId)
        {
            if (_context.CacheImpl != null) 
            {
                if (!_connectedClients.Contains(client))
                    lock (_connectedClients.SyncRoot)
                    {
                        _connectedClients.Add(client);
                    }

                _context.CacheImpl.ClientConnected(client, _inProc);
                if (s_logClientEvents)
                {
                    AppUtil.LogEvent(_cacheserver, "Client \"" + client + "\" has connected to " + _cacheInfo.Name, EventLogEntryType.Information, EventCategories.Information, EventID.ClientConnected);
                }
                _context.NCacheLog.CriticalInfo("Cache.OnClientConnected", "Client \"" + client + "\" has connected to cache");

            }
        }

        /// <summary>
        /// Stop the internal working of the cache.
        /// </summary>
        public virtual void Stop()
        {
            Dispose();
        }
   
internal void Initialize(IDictionary properties, bool inProc)
        {
            Initialize(properties, inProc, false, false);
        }

        internal void Initialize(IDictionary properties, bool inProc, bool isStartingAsMirror, bool twoPhaseInitialization)
        {
           
            //Just to initialze the HP time
            HPTime time = HPTime.Now;
            for (int i = 1; i < 1000; i++) time = HPTime.Now;
            _inProc = inProc;
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                lock (this)
                {
                    if (!properties.Contains("cache"))
                        throw new ConfigurationException("Missing configuration attribute 'cache'");

                    IDictionary cacheConfig = (IDictionary)properties["cache"];

                    if (properties["server-end-point"] != null)
                        cacheConfig.Add("server-end-point", (IDictionary)properties["server-end-point"]);

                    if (cacheConfig.Contains("name"))
                        _cacheInfo.Name = Convert.ToString(cacheConfig["name"]).Trim();

                    _cacheInfo.CurrentPartitionId = GetCurrentPartitionId(_cacheInfo.Name, cacheConfig);

                    if (cacheConfig.Contains("log"))
                    {
                        _context.NCacheLog = new NCacheLogger();
                        _context.NCacheLog.Initialize(cacheConfig["log"] as IDictionary, _cacheInfo.CurrentPartitionId, _cacheInfo.Name, isStartingAsMirror, inProc);

                    }
                    else
                    {
                        _context.NCacheLog = new NCacheLogger();
                        _context.NCacheLog.Initialize(null, _cacheInfo.CurrentPartitionId, _cacheInfo.Name);
                    }


                    _context.SerializationContext = _cacheInfo.Name;
                    _context.TimeSched = new TimeScheduler();
                    _context.TimeSched.AddTask(new SystemMemoryTask(_context.NCacheLog));
                    _context.AsyncProc = new AsyncProcessor(_context.NCacheLog); 
                    if (!inProc)
                    {
                        if (_cacheInfo.CurrentPartitionId != string.Empty)
                        {
                            _context.PerfStatsColl = new PerfStatsCollector(Name + "-" + _cacheInfo.CurrentPartitionId, inProc);
                        }
                        else
                        {
                            _context.PerfStatsColl = new PerfStatsCollector(Name, inProc);
                        }

                        _context.PerfStatsColl.NCacheLog = _context.NCacheLog;
                    }
                    else
                    {
                        _channels = new RemotingChannels();
                        _channels.RegisterTcpChannels("InProc", 0);
                        if (_cacheInfo.CurrentPartitionId != string.Empty)
                            RemotingServices.Marshal(this, _cacheInfo.Name + ":" + _cacheInfo.CurrentPartitionId);
                        else
                            RemotingServices.Marshal(this, _cacheInfo.Name);

                        //get the port number selected.
                        TcpServerChannel serverChannel = (TcpServerChannel)_channels.TcpServerChannel;
                        string uri = serverChannel.GetChannelUri();
                        int port = Convert.ToInt32(uri.Substring(uri.LastIndexOf(":") + 1));
                        if (isStartingAsMirror)// this is a special case for Partitioned Mirror Topology.
                            _context.PerfStatsColl = new PerfStatsCollector(Name + "-" + "replica", false);
                        else
                            _context.PerfStatsColl = new PerfStatsCollector(Name, port, inProc);

                        _context.PerfStatsColl.NCacheLog = _context.NCacheLog;
                    }

                    _context.IsStartedAsMirror = isStartingAsMirror;

                    CreateInternalCache(cacheConfig, isStartingAsMirror, twoPhaseInitialization);
                    //setting cache Impl instance

                    if (inProc && _context.CacheImpl != null)
                    {
                        
                        //we keep unserialized user objects in case of local inproc caches...
                        _context.CacheImpl.KeepDeflattedValues = false;// we no longer keep seralized data in inproc cache
                    }
                    // we bother about perf stats only if the user has read/write rights over counters.
                    _context.PerfStatsColl.IncrementCountStats(CacheHelper.GetLocalCount(_context.CacheImpl));

                    _cacheInfo.ConfigString = ConfigHelper.CreatePropertyString(properties);


                }

                _context.NCacheLog.CriticalInfo("Cache '" + _context.CacheRoot.Name + "' started successfully.");
            }
            catch (ConfigurationException e)
            {
                Dispose();
                throw;
            }
            catch (Exception e)
            {
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.Message, e);
            }
        }



        private string GetCurrentPartitionId(string cacheId, IDictionary config)
        {
            cacheId = cacheId.ToLower();
            if (config.Contains("cache-classes"))
            {
                config = config["cache-classes"] as Hashtable;
                if (config.Contains(cacheId))
                {
                    config = config[cacheId] as Hashtable;
                    if (config.Contains("type"))
                    {
                        string type = config["type"] as string;
                    }
                }
            }
            return string.Empty;
        }



        private void CreateInternalCache(IDictionary properties, bool isStartingAsMirror, bool twoPhaseInitialization)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                if (!properties.Contains("class"))
                    throw new ConfigurationException("Missing configuration attribute 'class'");
                String cacheScheme = Convert.ToString(properties["class"]);

                if (!properties.Contains("cache-classes"))
                    throw new ConfigurationException("Missing configuration section 'cache-classes'");
                IDictionary cacheClasses = (IDictionary)properties["cache-classes"];

                if (!cacheClasses.Contains(cacheScheme.ToLower()))
                    throw new ConfigurationException("Can not find cache class '" + cacheScheme + "'");
                IDictionary schemeProps = (IDictionary)cacheClasses[cacheScheme.ToLower()];

                if (!schemeProps.Contains("type"))
                    throw new ConfigurationException("Can not find the type of cache, invalid configuration for cache class '" + cacheScheme + "'");

                if (Name.Length < 1)
                    _cacheInfo.Name = cacheScheme;

                //Initialize the performance counters, if enabled.
                bool bEnableCounter = true;
                if (properties.Contains("perf-counters"))
                {
                    Hashtable perfCountersProps = properties["perf-counters"] as Hashtable;
                    if (perfCountersProps != null)
                    {
                        if (perfCountersProps.Contains("enabled"))
                            bEnableCounter = Convert.ToBoolean(perfCountersProps["enabled"]);
                    }
                }
                bool isClusterable = true;

                _context.ExpiryMgr = new ExpirationManager(schemeProps, _context);

                _cacheInfo.ClassName = Convert.ToString(schemeProps["type"]).ToLower();
                _context.AsyncProc.Start();
#if !CLIENT
                if (_cacheInfo.ClassName.CompareTo("replicated-server") == 0)
                    {
                        if (isClusterable)
                        {
                            _context.CacheImpl = new ReplicatedServerCache(cacheClasses, schemeProps, this, _context, this);
                            _context.CacheImpl.Initialize(cacheClasses, schemeProps, twoPhaseInitialization);
                        }
					}
                    else if (_cacheInfo.ClassName.CompareTo("partitioned-server") == 0 ) 
                    {
                        if (isClusterable)
                        {
                            _context.CacheImpl = new PartitionedServerCache(cacheClasses, schemeProps, this, _context, this);
                            _context.CacheImpl.Initialize(cacheClasses, schemeProps, twoPhaseInitialization);
                        }
                    }
                else
#endif
                {
                    if (_cacheInfo.ClassName.CompareTo("local-cache") == 0)
                    {
                        LocalCacheImpl cache = new LocalCacheImpl();
                        cache.Internal = CacheBase.Synchronized(new IndexedLocalCache(cacheClasses, cache, schemeProps, this, _context));

                        _context.CacheImpl = cache;
                    }
                    else
                    {
                        throw new ConfigurationException("Specified cache class '" + _cacheInfo.ClassName + "' is not available in this edition of " + _cacheserver + ".");
                    }
                }
                _cacheType = _cacheInfo.ClassName;


                // Start the expiration manager if the cache was created sucessfully!
                if (_context.CacheImpl != null)
                {
                    /// there is no need to do expirations on the Async replica's; 
                    /// Expired items are removed fromreplica by the respective active partition.
                    if (!isStartingAsMirror)
                        _context.ExpiryMgr.Start();

                    if (bEnableCounter)
                    {
                        _context.PerfStatsColl.InitializePerfCounters((isStartingAsMirror ? !isStartingAsMirror : this._inProc));
                    }
                }
                else
                {
                    _context.ExpiryMgr.Dispose();

                }
            }
            catch (ConfigurationException e)
            {
                _context.NCacheLog.Error("Cache.CreateInternalCache()", e.ToString());
                _context.CacheImpl = null;
                Dispose();
                throw;
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.CreateInternalCache()", e.ToString());
                _context.CacheImpl = null;
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }

        private void CreateInternalCache2(IDictionary properties)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                String cacheScheme = Convert.ToString(properties["name"]).Trim();

                if (Name.Length < 1)
                    _cacheInfo.Name = cacheScheme;

                //Initialize the performance counters, if enabled.
                bool bEnableCounter = true;

                if (properties.Contains("perf-counters"))
                {
                    Hashtable perfCountersProps = properties["perf-counters"] as Hashtable;
                    if (perfCountersProps != null)
                    {
                        if (perfCountersProps.Contains("enabled"))
                            bEnableCounter = Convert.ToBoolean(perfCountersProps["enabled"]);
                    }
                }
                               
                 _context.ExpiryMgr = new ExpirationManager(properties, _context);

                IDictionary clusterProps = null;

                if (properties.Contains("cluster"))
                {
                    clusterProps = properties["cluster"] as IDictionary;
                    if (clusterProps.Contains("topology"))
                        _cacheInfo.ClassName = Convert.ToString(clusterProps["topology"]).Trim();
                }
                else
                {
                    _cacheInfo.ClassName = "local";
                }

                _context.AsyncProc.Start();
                
                if (_cacheInfo.ClassName.CompareTo("local") == 0)
                {
                    LocalCacheImpl cache = new LocalCacheImpl();

                    cache.Internal = CacheBase.Synchronized(new LocalCache(properties, cache, properties, this, _context));
                    _context.CacheImpl = cache;
                }
                else
                {
                    throw new ConfigurationException("Specified cache class '" + _cacheInfo.ClassName + "' is not available in this edition of "+_cacheserver+".");
                }

                _cacheType = _cacheInfo.ClassName;

                // Start the expiration manager if the cache was created sucessfully!
                if (_context.CacheImpl != null)
                {
                    _context.ExpiryMgr.Start();
                    if (bEnableCounter)
                    {
                        _context.PerfStatsColl.InitializePerfCounters(this._inProc);
                    }
                }
                else
                {
                    _context.ExpiryMgr.Dispose();
                }
            }
            catch (ConfigurationException e)
            {
                _context.NCacheLog.Error("Cache.CreateInternalCache()", e.ToString());
                _context.CacheImpl = null;
                Dispose();
                throw;
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.CreateInternalCache()", e.ToString());
                _context.CacheImpl = null;
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }
        /// <summary>
        /// Creates the underlying instance of the actual cache.
        /// </summary>
        /// <param name="properties"></param>
        public override object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService();
            if (lease != null && _sponsor != null)
                lease.Register(_sponsor);
            return lease;
        }

        #region /       RemoteObjectSponsor      /
        private class RemoteObjectSponsor : ISponsor
        {
            /// <summary>
            /// Requests a sponsoring client to renew the lease for the specified object.
            /// </summary>
            /// <param name="lease">The lifetime lease of the object that requires lease renewal.</param>
            /// <returns>The additional lease time for the specified object.</returns>
            public TimeSpan Renewal(ILease lease)
            {
                return TimeSpan.FromMinutes(10);
            }
        }
        #endregion

        #endregion

        internal static void Resize(ref object[] array, int newLength)
        {
            if (array == null) return;
            if (array.Length == newLength) return;

            object[] copyArray = new object[newLength];
            for (int i = 0; i < newLength; i++)
            {
                if (i < array.Length)
                    copyArray[i] = array[i];
                else
                    break;
            }
            array = copyArray;
        }

        internal static void Resize(ref string[] array, int newLength)
        {
            if (array == null) return;
            if (array.Length == newLength) return;

            string[] copyArray = new string[newLength];
            for (int i = 0; i < newLength; i++)
            {
                if (i < array.Length)
                    copyArray[i] = array[i];
                else
                    break;
            }
            array = copyArray;
        }

        internal static void Resize(ref CacheEntry[] array, int newLength)
        {
            if (array == null) return;
            if (array.Length == newLength) return;

            CacheEntry[] copyArray = new CacheEntry[newLength];
            for (int i = 0; i < newLength; i++)
            {
                if (i < array.Length)
                    copyArray[i] = array[i];
                else
                    break;
            }
            array = copyArray;
        }

        public Dictionary<string, int> GetRunningServers(string ipAddress, int serverPort)
        {
            Dictionary<string, int> runningServers = new Dictionary<string, int>();
            if (this.CacheType.Equals("replicated-server"))
            {
                string connectedIpAddress = ipAddress;
                int connectedPort = serverPort;
#if  !CLIENT
            ArrayList nodes = ((ClusterCacheBase)this._context.CacheImpl)._stats.Nodes;

            foreach (NodeInfo i in nodes)
            {
               
                if (i.RendererAddress != null)
                {
                    ipAddress = (string)i.RendererAddress.IpAddress.ToString();
                    serverPort = i.RendererAddress.Port;
                    runningServers.Add(ipAddress, serverPort);
                }
               
            }
#else
             runningServers.Add(ipAddress, serverPort);
#endif
            }
            return runningServers;
        }


        public void GetLeastLoadedServer(ref string ipAddress, ref int serverPort)
        {
            string connectedIpAddress = ipAddress;
            int connectedPort = serverPort;

            if (!this._context.IsClusteredImpl) return;
            if (_inProc) return;

            ArrayList nodes = ((ClusterCacheBase)this._context.CacheImpl)._stats.Nodes;

            NodeInfo localNodeInfo = null;
            int min = int.MaxValue;

            foreach (NodeInfo i in nodes)
            {
                if (i.IsStartedAsMirror) continue;

                if (i.ConnectedClients.Count < min)
                {
                    if (i.RendererAddress != null)
                    {
                        ipAddress = (string)i.RendererAddress.IpAddress.ToString();
                        serverPort = i.RendererAddress.Port;
                    }
                    else
                        ipAddress = (string)i.Address.IpAddress.ToString();

                    min = i.ConnectedClients.Count;
                }

                if (i.Address.IpAddress.ToString().Equals(connectedIpAddress))
                    localNodeInfo = i;
            }

            ///we dont need to reconnect the the selected server if the selected server has same clients
            ///as the server with which the client is currently connected.
            if (localNodeInfo != null && localNodeInfo.ConnectedClients.Count == min)
            {
                ipAddress = connectedIpAddress;
                serverPort = connectedPort;
            }
        }




        #region	/                 --- Clear ---           /

        /// <summary>
        /// Clear all the contents of cache
        /// </summary>
        /// <returns></returns>
        public void Clear()
        {
            Clear(new BitSet(), null, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Clear all the contents of cache
        /// </summary>
        /// <returns></returns>
        public void Clear(OperationContext operationContext)
        {
            Clear(new BitSet(), null, operationContext);
        }

        /// <summary>
        /// Clear all the contents of cache
        /// </summary>
        /// <returns></returns>
        public void Clear(BitSet flag, CallbackEntry cbEntry, OperationContext operationContext)
        {
            // Cache has possibly expired so do default.
            if (!IsRunning) return; 

          
            try
            {
                _context.CacheImpl.Clear(null, operationContext);
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Clear()", inner.ToString());
                throw new OperationFailedException("Clear operation failed. Error: " + inner.Message, inner);
            }
        }

        #endregion



        #region	/                 --- Contains ---           /

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element 
        /// with the specified key; otherwise, false.</returns>
        public bool Contains(object key, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.Contains", "");
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return false; 

            try
            {
                return _context.CacheImpl.Contains(key, operationContext);
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Contains()", inner.ToString());
                throw new OperationFailedException("Contains operation failed. Error : " + inner.Message, inner);
            }
        }


        #endregion


        #region	/                 --- Get ---           /

        /// <summary>
        /// Get the CacheEntry stored in the cache.
        /// </summary>
        /// <param name="key">The key used as reference to find the desired object</param>
        /// <param name="lockId"></param>
        /// <param name="lockDate"></param>
        /// <param name="lockTimeout"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
        /// <returns>Returns a CacheEntry</returns>
        public object GetCacheEntry(object key, ref object lockId, ref DateTime lockDate, TimeSpan lockTimeout, LockAccessType accessType, OperationContext operationContext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null; 

            try
            {
                _context.PerfStatsColl.MsecPerGetBeginSample();
                _context.PerfStatsColl.IncrementGetPerSecStats();
                CacheEntry entry = null;

                LockExpiration lockExpiration = null;

                //if lockId will be empty if item is not already lock provided by user
                if ((lockId != null && lockId.ToString() == "") || lockId == null)
                {
                    if (accessType == LockAccessType.ACQUIRE)
                    {
                        lockId = GetLockId(key);
                        lockDate = DateTime.Now;

                        if (!TimeSpan.Equals(lockTimeout, TimeSpan.Zero))
                        {
                            lockExpiration = new LockExpiration(lockTimeout);
                        }
                    }
                }

                object generatedLockId = lockId;

                // if only key is provided by user
                if ((accessType == LockAccessType.IGNORE_LOCK || accessType == LockAccessType.DONT_ACQUIRE))
                {
                    entry = _context.CacheImpl.Get(key, operationContext);
                }

                 //if key and locking information is provided by user
                else
                {

                    entry = _context.CacheImpl.Get(key, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
                }

                if (entry == null && accessType == LockAccessType.ACQUIRE)
                {
                    if (lockId == null || generatedLockId.Equals(lockId))
                    {
                        lockId = null;
                        lockDate = new DateTime();
                    }
                }

                
                _context.PerfStatsColl.MsecPerGetEndSample();

                if (entry != null)
                {
                    _context.PerfStatsColl.IncrementHitsPerSecStats();
                }
                else
                {
                    _context.PerfStatsColl.IncrementMissPerSecStats();
                }

                return entry;
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.Get()", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Get()", inner.ToString());
                throw new OperationFailedException("Get operation failed. Error : " + inner.Message, inner);
            }
        }

        /// <summary>
        /// Retrieve the object from the cache. A key is passed as parameter.
        /// </summary>
        public CompressedValueEntry Get(object key)
        {
            return Get(key, new BitSet(), new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

       

        public void Unlock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (IsRunning)
            {
                _context.CacheImpl.UnLock(key, lockId, isPreemptive, operationContext);
            }
        }

        public bool IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (IsRunning)
            {
                object passedLockId = lockId;
                LockOptions lockInfo = _context.CacheImpl.IsLocked(key, ref lockId, ref lockDate, operationContext);
                if (lockInfo != null)
                {
                    if (lockInfo.LockId == null)
                        return false;

                    lockId = lockInfo.LockId;
                    lockDate = lockInfo.LockDate;

                    return !object.Equals(lockInfo.LockId, passedLockId);
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public bool Lock(object key, TimeSpan lockTimeout, out object lockId, out DateTime lockDate, OperationContext operationContext)
        {
            lockId = null;
            lockDate = DateTime.UtcNow;
            LockExpiration lockExpiration = null;
            if (!TimeSpan.Equals(lockTimeout, TimeSpan.Zero))
            {
                lockExpiration = new LockExpiration(lockTimeout);
            }

            if (IsRunning)
            {
                object generatedLockId = lockId = GetLockId(key);
                LockOptions lockInfo = _context.CacheImpl.Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
                if (lockInfo != null)
                {
                    lockId = lockInfo.LockId;
                    lockDate = lockInfo.LockDate;
                    if (generatedLockId.Equals(lockInfo.LockId))
                        return true;
                    return false;
                }
                else
                {
                    lockId = null;
                    return false;
                }
            }
            return false;
        }

        public CompressedValueEntry Get(object key, BitSet flagMap, OperationContext operationContext)
        {
            object lockId = null;
            DateTime lockDate = DateTime.UtcNow;
						return Get(key, flagMap, ref lockId, ref lockDate, TimeSpan.Zero, LockAccessType.IGNORE_LOCK, operationContext);
        }

        private object GetLockId(object key)
        {
            long nextId = 0;
            lock (this)
            {
                nextId = _lockIdTicker++;
            }
            return System.Diagnostics.Process.GetCurrentProcess().Id.ToString() + "-" + Environment.MachineName + "-" + key.ToString() + "-" + nextId;
        }

        /// <summary>
        /// Retrieve the object from the cache.
        /// </summary>
        /// <param name="key"></param>
               /// <returns></returns>
        [CLSCompliant(false)]
        public CompressedValueEntry Get(object key, BitSet flagMap,ref object lockId, ref DateTime lockDate, TimeSpan lockTimeout, LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.GetGrp", "");
            // Cache has possibly expired so do default.
            if (!IsRunning) return null; 

            CompressedValueEntry result = new CompressedValueEntry();
            CacheEntry e = null;
            try
            {
                _context.PerfStatsColl.MsecPerGetBeginSample();
                _context.PerfStatsColl.IncrementGetPerSecStats();
                _context.PerfStatsColl.IncrementHitsRatioPerSecBaseStats();
                HPTimeStats getTime = new HPTimeStats();
                getTime.BeginSample();

                LockExpiration lockExpiration = null;
                if (accessType == LockAccessType.ACQUIRE)
                {
                    lockId = GetLockId(key);
                    lockDate = DateTime.UtcNow;

                    if (!TimeSpan.Equals(lockTimeout, TimeSpan.Zero))
                    {
                        lockExpiration = new LockExpiration(lockTimeout);
                    }
                }

                object generatedLockId = lockId;

                e = _context.CacheImpl.Get(key, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
                

                if (e == null && accessType == LockAccessType.ACQUIRE)
                {
                    if (lockId == null || generatedLockId.Equals(lockId))
                    {
                        lockId = null;
                        lockDate = new DateTime();
                    }
                }
                if (flagMap != null)
                {
                    
                    if (e != null)
                    {
                        /// increment the counter for hits/sec
                        _context.PerfStatsColl.MsecPerGetEndSample();
                        result.Value = e.Value;
                        result.Flag = e.Flag;
                    }

                }
                _context.PerfStatsColl.MsecPerGetEndSample();
                getTime.EndSample();
                
                /// update the counter for hits/sec or misses/sec
                if (result.Value != null)
                {
                    _context.PerfStatsColl.IncrementHitsRatioPerSecStats();
                    _context.PerfStatsColl.IncrementHitsPerSecStats();
                }
                else
                {
                    _context.PerfStatsColl.IncrementMissPerSecStats();
                }
                if (result.Value is CallbackEntry)
                    result.Value = ((CallbackEntry)result.Value).Value;
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.Get()", "Get operation failed. Error : " + inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Get()", "Get operation failed. Error : " + inner.ToString());
                throw new OperationFailedException("Get operation failed. Error :" + inner.Message, inner);
            }

            return result;
        }

        #endregion



        #region	/                 --- Bulk Get ---           /

        /// <summary>
        /// Retrieve the array of objects from the cache.
        /// An array of keys is passed as parameter.
        /// </summary>
        public IDictionary GetBulk(object[] keys, BitSet flagMap, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.GetBlk", "");

            if (keys == null) throw new ArgumentNullException("keys");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null; 

            Hashtable table = null;
            try
            {
              
                HPTimeStats getTime = new HPTimeStats();
                getTime.BeginSample();

                table = _context.CacheImpl.Get(keys, operationContext);

                if (table != null)
                {
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (table.ContainsKey(keys[i]))
                        {
                            if (table[keys[i]] != null)
                            {
                                CacheEntry entry = table[keys[i]] as CacheEntry;
                                CompressedValueEntry val = new CompressedValueEntry();
                                val.Value = entry.Value is CallbackEntry ? ((CallbackEntry)entry.Value).Value : entry.Value;
                                val.Flag = entry.Flag;
                                table[keys[i]] = val;
                            }
                        }
                    }
                }
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.Get()", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Get()", inner.ToString());
                throw new OperationFailedException("Get operation failed. Error : " + inner.Message, inner);
            }
            return table;
        }



        #endregion



        #region	/                 --- Add ---           /


        /// <summary>
        /// Convert CompactCacheEntry to CacheEntry, CompactCacheEntry may be serialized
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        CacheEntry MakeCacheEntry(CompactCacheEntry cce)
        {
            bool isAbsolute = false;
            int priority = (int)CacheItemPriority.Normal;

            int opt = (int)cce.Options;

            if (opt != 255)
            {
                isAbsolute = Convert.ToBoolean(opt & 1);
                opt = (opt >> 1);
                opt = (opt >> 1);
                priority = opt - 2;
            }

            ExpirationHint eh = ExpirationHelper.MakeExpirationHint(cce.Expiration, isAbsolute);
            CacheEntry e = new CacheEntry(cce.Value, eh, new PriorityEvictionHint((CacheItemPriority)priority));       
            e.QueryInfo = cce.QueryInfo;
            e.Flag = cce.Flag;


            e.LockId = cce.LockId;
            e.LockAccessType = cce.LockAccessType;

            return e;
        }

        public bool AddExpirationHint(object key, ExpirationHint hint, OperationContext operationContext)
        {
            if (!IsRunning) return false;
            try
            {

                return _context.CacheImpl.Add(key, hint, operationContext);
            }
            catch (Exception ex)
            {
                _context.NCacheLog.Error("Add operation failed. Error: " + ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Add a CompactCacheEntry, it may be serialized
        /// </summary>
        /// <param name="entry"></param>
        public void AddEntry(object entry, OperationContext operationContext)
        {
            // check if cache is running.
            if (!IsRunning) return;

            CompactCacheEntry cce = null;
            cce = (CompactCacheEntry)entry;

            CacheEntry e = MakeCacheEntry(cce);
            Add(cce.Key, e.Value, e.ExpirationHint, e.EvictionHint, e.QueryInfo, e.Flag,operationContext);
        }

        /// <summary>
        /// Basic Add operation, takes only the key and object as parameter.
        /// </summary>
        public void Add(object key, object value)
        {
            Add(key, value, null, null, null, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Overload of Add operation. Uses an additional ExpirationHint parameter to be used 
        /// for Item Expiration Feature.
        /// </summary>
        internal void Add(object key, object value, ExpirationHint expiryHint,OperationContext operationContext)
        {
            Add(key, value, expiryHint, null, null, operationContext);
        }

        /// <summary>
        /// Overload of Add operation. Uses an additional EvictionHint parameter to be used for 
        /// Item auto eviction policy.
        /// </summary>
        internal void Add(object key, object value, EvictionHint evictionHint)
        {
            Add(key, value, null, evictionHint, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Overload of Add operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>

        internal void Add(object key, object value, ExpirationHint expiryHint, EvictionHint evictionHint, OperationContext operationContext)
        {
            Add(key, value, expiryHint, evictionHint, null, operationContext);
        }

        /// <summary>
        /// Overload of Add operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>

        internal void Add(object key, object value, ExpirationHint expiryHint, EvictionHint evictionHint, Hashtable queryInfo, OperationContext operationContext)
        {
            Add(key, value, expiryHint, evictionHint,queryInfo, new BitSet(), operationContext);
        }

        /// <summary>
        /// Overload of Add operation. uses additional paramer of Flag for checking if compressed or not
        /// </summary>
        public void Add(object key, object value,
                        ExpirationHint expiryHint, EvictionHint evictionHint,
                        Hashtable queryInfo, BitSet flag, 
                       OperationContext operationContext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");
            if (!value.GetType().IsSerializable)
                throw new ArgumentException("value is not serializable");
            if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                throw new ArgumentException("expiryHint is not serializable");
            if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                throw new ArgumentException("evictionHint is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return; 
           
            CacheEntry e = new CacheEntry(value, expiryHint, evictionHint);
            ////Object size for inproc
            object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
            if (dataSize != null)
                e.DataSize = Convert.ToInt64(dataSize);
           
            e.QueryInfo = queryInfo;
            e.Flag.Data |= flag.Data;        
            try
            {
                HPTimeStats addTime = new HPTimeStats();
                _context.PerfStatsColl.MsecPerAddBeginSample();
                
                addTime.BeginSample();
                Add(key, e, operationContext);
                addTime.EndSample();
                _context.PerfStatsColl.MsecPerAddEndSample();
            }
            catch (Exception inner)
            {
                throw;
            }
        }


        /// <summary>
        /// Overload of Add operation for bulk additions. Uses EvictionHint and ExpirationHint arrays.
        /// </summary>        
        public IDictionary Add(string[] keys, CacheEntry[] enteries, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.InsertBlk", "");

            if (keys == null) throw new ArgumentNullException("keys");
            if (enteries == null) throw new ArgumentNullException("entries");

            try
            {
                Hashtable result = _context.CacheImpl.Add(keys, enteries, true, operationContext);
                if (result != null)
                {

                    Hashtable tmp = (Hashtable)result.Clone();
                    IDictionaryEnumerator ide = tmp.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        CacheAddResult addResult = CacheAddResult.Failure;
                        if (ide.Value is CacheAddResult)
                        {
                            addResult = (CacheAddResult)ide.Value;
                            switch (addResult)
                            {
                                case CacheAddResult.Failure:
                                    break;
                                case CacheAddResult.KeyExists:
                                    result[ide.Key] = new OperationFailedException("The specified key already exists.");
                                    break;
                                case CacheAddResult.NeedsEviction:
                                    result[ide.Key] = new OperationFailedException("The cache is full and not enough items could be evicted.");
                                    break;
                                case CacheAddResult.Success:
                                    result.Remove(ide.Key);
                                    break;
                            }
                        }
                    }
                }
                return result;


            }
            catch (Exception)
            {
                //NCacheLog.Error(_context.CacheName, "Cache.Add():", inner.ToString());
                throw;
            }

        }

        

        /// <summary>
        /// Internal Add operation. Does write-through as well.
        /// </summary>
        public void Add(object key, CacheEntry e, OperationContext operationContext)
        {
            object value = e.Value;
            try
            {
                CacheAddResult result = CacheAddResult.Failure;

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.Add", key as string);

                result = _context.CacheImpl.Add(key, e, true, operationContext);

                switch (result)
                {
                    case CacheAddResult.Failure:
                        break;

                    case CacheAddResult.NeedsEviction:
                        throw new OperationFailedException("The cache is full and not enough items could be evicted.", false);

                    case CacheAddResult.KeyExists:
                        throw new OperationFailedException("The specified key already exists.", false);

                    case CacheAddResult.Success:
                        _context.PerfStatsColl.IncrementAddPerSecStats();
                        break;
                }
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.Add():", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Add():", inner.ToString());
                throw new OperationFailedException("Add operation failed. Error : " + inner.Message, inner);
            }
        }

        #endregion

        #region	/                 --- Bulk Add ---           /


        /// <summary>
        /// Add array of CompactCacheEntry to cache, these may be serialized
        /// </summary>
        /// <param name="entries"></param>
        public IDictionary AddEntries(object[] entries, OperationContext operationContext)
        {
            // check if cache is running.
            if (!IsRunning) return null;

            string[] keys = new string[entries.Length];
            object[] values = new object[entries.Length];
            CallbackEntry[] callbackEnteries = new CallbackEntry[entries.Length]; 
            ExpirationHint[] exp = new ExpirationHint[entries.Length];
            EvictionHint[] evc = new EvictionHint[entries.Length];
            BitSet[] flags = new BitSet[entries.Length];
            Hashtable[] queryInfo = new Hashtable[entries.Length];

            CallbackEntry cbEntry = null;

            for (int i = 0; i < entries.Length; i++)
            {
                CompactCacheEntry cce = (CompactCacheEntry)SerializationUtil.CompactDeserialize(entries[i], _context.SerializationContext);
                keys[i] = cce.Key as string;
                CacheEntry ce = MakeCacheEntry(cce);
                if (ce != null)
                {
                    if (ce.Value is CallbackEntry)
                        cbEntry = ce.Value as CallbackEntry;
                    else
                        cbEntry = null;

                    callbackEnteries[i] = cbEntry;


                    object value = ce.Value as CallbackEntry;
                    values[i] = value == null ? ce.Value : ((CallbackEntry)ce.Value).Value;

                    exp[i] = ce.ExpirationHint;
                    evc[i] = ce.EvictionHint;
                    queryInfo[i] = ce.QueryInfo;
                    flags[i] = ce.Flag;
                }
            }

            IDictionary items = Add(keys, values, callbackEnteries, exp,  evc,  queryInfo, flags, operationContext);
            return items;
        }
       
        /// <summary>
        /// Overload of Add operation for bulk additions. Uses EvictionHint and ExpirationHint arrays.
        /// </summary>        
        public IDictionary Add(string[] keys, object[] values, CallbackEntry[] callbackEnteries,
                                ExpirationHint[] expirations, EvictionHint[] evictions, Hashtable[] queryInfos, BitSet[] flags,OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.InsertBlk", "");

            if (keys == null) throw new ArgumentNullException("keys");
            if (values == null) throw new ArgumentNullException("items");
            if (keys.Length != values.Length) throw new ArgumentException("keys count is not equals to values count");
            CacheEntry[] enteries = new CacheEntry[values.Length];
            long[] sizes = null;
            object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
            if (dataSize != null)
            {
                sizes = (long[])dataSize;
            }
            for (int i = 0; i < values.Length; i++)
            {
                if (keys[i] == null) throw new ArgumentNullException("key");
                if (values[i] == null) throw new ArgumentNullException("value");

                if (!keys[i].GetType().IsSerializable)
                    throw new ArgumentException("key is not serializable");
                if (!values[i].GetType().IsSerializable)
                    throw new ArgumentException("value is not serializable");
                if ((expirations[i] != null) && !expirations[i].GetType().IsSerializable)
                    throw new ArgumentException("expiryHint is not serializable");
                if ((evictions[i] != null) && !evictions[i].GetType().IsSerializable)
                    throw new ArgumentException("evictionHint is not serializable");

                // Cache has possibly expired so do default.
                if (!IsRunning) return null;

                enteries[i] = new CacheEntry(values[i], expirations[i], evictions[i]);


                enteries[i].QueryInfo = queryInfos[i];
                enteries[i].Flag.Data |= flags[i].Data;
                if (sizes != null)
                    enteries[i].DataSize = sizes[i];
                if (callbackEnteries[i] != null)
                {
                    CallbackEntry cloned = callbackEnteries[i].Clone() as CallbackEntry;
                    cloned.Value = values[i];
                    cloned.Flag = enteries[i].Flag;
                    enteries[i].Value = cloned;
                }
            }

            try
            {
                IDictionary result;
                HPTimeStats addTime = new HPTimeStats();

                CacheEntry[] clone = null;
                addTime.BeginSample();
                result = Add(keys, enteries, operationContext);
                addTime.EndSample();
                return result;
            }
            catch (Exception inner)
            {
                throw;
            }

        }

        /// <summary>
        /// Internal Add operation for bulk additions. Does write-through as well.
        /// </summary>
        private Hashtable Add(object[] keys, CacheEntry[] entries, OperationContext operationContext)
        {
            try
            {
                Hashtable result = new Hashtable();
                result = _context.CacheImpl.Add(keys, entries, true, operationContext);
                if (result != null)
                {
                    
                    Hashtable tmp = (Hashtable)result.Clone();
                    IDictionaryEnumerator ide = tmp.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        CacheAddResult addResult = CacheAddResult.Failure;
                        if (ide.Value is CacheAddResult)
                        {
                            addResult = (CacheAddResult)ide.Value;
                            switch (addResult)
                            {
                                case CacheAddResult.Failure:
                                    break;
                                case CacheAddResult.KeyExists:
                                    result[ide.Key] = new OperationFailedException("The specified key already exists.");
                                    break;
                                case CacheAddResult.NeedsEviction:
                                    result[ide.Key] = new OperationFailedException("The cache is full and not enough items could be evicted.");
                                    break;
                                case CacheAddResult.Success:
                                    result.Remove(ide.Key);
                                    break;
                            }
                        }
                    }
                }
                return result;
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.Add():", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Add():", inner.ToString());
                throw new OperationFailedException("Add operation failed. Error : " + inner.Message, inner);
            }
        }

        #endregion


        #region	/                 --- Insert ---           /


        /// <summary>
        /// Insert a CompactCacheEntry, it may be serialized
        /// </summary>
        /// <param name="entry"></param>

        [CLSCompliant(false)]
        public void InsertEntry(object entry, OperationContext operationContext)
        {
            if (!IsRunning)
                return;

            CompactCacheEntry cce = null;
            cce = (CompactCacheEntry)entry;

            CacheEntry e = MakeCacheEntry(cce);

           
Insert(cce.Key, e.Value, e.ExpirationHint, e.EvictionHint,e.QueryInfo, e.Flag, e.LockId, e.LockAccessType, operationContext);

        }


        /// <summary>
        /// Basic Insert operation, takes only the key and object as parameter.
        /// </summary>
        [CLSCompliant(false)]
        public void Insert(object key, object value)
        {
            Insert(key, value, null, null, null, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Overload of Insert operation. Uses an additional ExpirationHint parameter to be used 
        /// for Item Expiration Feature.
        /// </summary>
        [CLSCompliant(false)]
        public void Insert(object key, object value, ExpirationHint expiryHint)
        {
            Insert(key, value, expiryHint, null, null, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Overload of Insert operation. Uses an additional EvictionHint parameter to be used 
        /// for Item auto eviction policy.
        /// </summary>
        [CLSCompliant(false)]
        public void Insert(object key, object value, EvictionHint evictionHint)
        {
            Insert(key, value, null, evictionHint, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Overload of Insert operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        internal void Insert(object key, object value, ExpirationHint expiryHint, EvictionHint evictionHint, OperationContext operationContext)
        {
            Insert(key, value, expiryHint,  evictionHint, null, operationContext);
        }
        internal void Insert(object key, object value, ExpirationHint expiryHint, EvictionHint evictionHint, Hashtable queryInfo, OperationContext operationContext)
        {
            Insert(key, value, expiryHint, evictionHint, queryInfo, new BitSet(), operationContext);
        }

        /// <summary>
        /// Overload of Insert operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        [CLSCompliant(false)]
        public void Insert(object key, object value, ExpirationHint expiryHint, EvictionHint evictionHint, Hashtable queryInfo, BitSet flag, OperationContext operationContext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");
            if (!value.GetType().IsSerializable)
                throw new ArgumentException("value is not serializable");
            if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                throw new ArgumentException("expiryHint is not not serializable");
            if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                throw new ArgumentException("evictionHint is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning)
                return; 

            CacheEntry e = new CacheEntry(value, expiryHint, evictionHint);

            e.QueryInfo = queryInfo;
            e.Flag.Data |= flag.Data;

          

            // update the counters for various statistics
            try
            {
                CacheEntry clone;
                    clone = e;
                _context.PerfStatsColl.MsecPerUpdBeginSample();
                Insert(key, e, null, LockAccessType.IGNORE_LOCK, operationContext);
                _context.PerfStatsColl.MsecPerUpdEndSample();
            }
            catch (Exception inner)
            {
                _context.NCacheLog.CriticalInfo("Cache.Insert():", inner.ToString());
                throw;
            }
        }

        /// <summary>
        /// Overload of Insert operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        [CLSCompliant(false)]
        public void Insert(object key, object value,
                           ExpirationHint expiryHint, EvictionHint evictionHint,
            Hashtable queryInfo, BitSet flag, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.Insert", "");
                if (key == null) throw new ArgumentNullException("key");
                if (value == null) throw new ArgumentNullException("value");

                if (!key.GetType().IsSerializable)
                    throw new ArgumentException("key is not serializable");
                if (!value.GetType().IsSerializable)
                    throw new ArgumentException("value is not serializable");
                if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                    throw new ArgumentException("expiryHint is not not serializable");
                if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                    throw new ArgumentException("evictionHint is not serializable");

                // Cache has possibly expired so do default.
                if (!IsRunning)
                    return; 

                CacheEntry e = new CacheEntry(value, expiryHint, evictionHint);
                e.QueryInfo = queryInfo;
                e.Flag.Data |= flag.Data;

                object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
                if (dataSize != null)
                    e.DataSize = Convert.ToInt64(dataSize);
          


            /// update the counters for various statistics
            
                _context.PerfStatsColl.MsecPerUpdBeginSample();
                Insert(key, e, lockId, accessType, operationContext);
                _context.PerfStatsColl.MsecPerUpdEndSample();
            }
            catch (Exception inner)
            {
                if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("Cache.Insert():", inner.ToString());
                throw;
            }
        }

        /// <summary>
        /// Internal Insert operation. Does a write thru as well.
        /// </summary>
        private void Insert(object key, CacheEntry e, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
           
            HPTimeStats insertTime = new HPTimeStats();
            insertTime.BeginSample();

            object value = e.Value;
            try
            {
                CacheInsResultWithEntry retVal = CascadedInsert(key, e, true, lockId, accessType, operationContext);
                insertTime.EndSample();

                switch (retVal.Result)
                {
                    case CacheInsResult.Failure:
                        break;

                    case CacheInsResult.NeedsEviction:
                    case CacheInsResult.NeedsEvictionNotRemove:
                       throw new OperationFailedException("The cache is full and not enough items could be evicted.", false);

                    case CacheInsResult.SuccessOverwrite:
                        _context.PerfStatsColl.IncrementUpdPerSecStats();
                        break;
                    case CacheInsResult.Success:
                        _context.PerfStatsColl.IncrementAddPerSecStats();
                        break;
                    case CacheInsResult.ItemLocked:
                        throw new LockingException("Item is locked.");
                }
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.Insert():", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Insert():", inner.ToString());
                _context.NCacheLog.CriticalInfo("Cache.Insert():", inner.ToString());

                throw new OperationFailedException("Insert operation failed. Error : " + inner.Message, inner);
            }
        }


        #endregion


        #region	/                 --- Bulk Insert ---           /


        /// <summary>
        /// Insert array of CompactCacheEntry to cache, these may be serialized
        /// </summary>
        /// <param name="entries"></param>
        public IDictionary InsertEntries(object[] entries, OperationContext operationContext)
        {
            // check if cache is running.
            if (!IsRunning) return null;

            string[] keys = new string[entries.Length];
            object[] values = new object[entries.Length];

            CallbackEntry[] callbackEnteries = new CallbackEntry[entries.Length]; 
            ExpirationHint[] exp = new ExpirationHint[entries.Length];
            EvictionHint[] evc = new EvictionHint[entries.Length];
            BitSet[] flags = new BitSet[entries.Length];
            Hashtable[] queryInfo = new Hashtable[entries.Length];
            CallbackEntry cbEntry = null;

            for (int i = 0; i < entries.Length; i++)
            {
                CompactCacheEntry cce = (CompactCacheEntry)SerializationUtil.CompactDeserialize(entries[i], _context.SerializationContext);
                keys[i] = cce.Key as string;
                CacheEntry ce = MakeCacheEntry(cce);
                if (ce != null)
                {
                    if (ce.Value is CallbackEntry)
                        cbEntry = ce.Value as CallbackEntry;
                    else
                        cbEntry = null;

                    callbackEnteries[i] = cbEntry;

                    object value = ce.Value as CallbackEntry;
                    values[i] = value == null ? ce.Value : ((CallbackEntry)ce.Value).Value;

                    exp[i] = ce.ExpirationHint;
                    evc[i] = ce.EvictionHint;
                    queryInfo[i] = ce.QueryInfo;
                    flags[i] = ce.Flag;
                }
            }

            IDictionary items= Insert(keys, values, callbackEnteries, exp, evc, queryInfo, flags, operationContext);
           
            return items;
        }

        /// <summary>
        /// Overload of Insert operation for bulk inserts. Uses an additional ExpirationHint parameter to be used 
        /// for Item Expiration Feature.
        /// </summary>
        public IDictionary Insert(object[] keys, object[] values, ExpirationHint expiryHint, OperationContext operationContext)
        {
            return Insert(keys, values, expiryHint, null, operationContext);
        }

        /// <summary>
        /// Overload of Insert operation for bulk inserts. Uses an additional EvictionHint parameter to be used 
        /// for Item auto eviction policy.
        /// </summary>
        public IDictionary Insert(object[] keys, object[] values, EvictionHint evictionHint, OperationContext operationContext)
        {
            return Insert(keys, values, null, evictionHint, operationContext);
        }

        /// <summary>
        /// Overload of Insert operation for bulk inserts. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        public IDictionary Insert(object[] keys, object[] values, ExpirationHint expiryHint, EvictionHint evictionHint, OperationContext operationContext)
        {

            if (keys == null) throw new ArgumentNullException("keys");
            if (values == null) throw new ArgumentNullException("items");
            if (keys.Length != values.Length)
                throw new ArgumentException("keys count is not equals to values count");

            CacheEntry[] ce = new CacheEntry[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                object key = keys[i];
                object value = values[i];


                if (key == null) throw new ArgumentNullException("key");
                if (value == null) throw new ArgumentNullException("value");

                if (!key.GetType().IsSerializable)
                    throw new ArgumentException("key is not serializable");
                if (!value.GetType().IsSerializable)
                    throw new ArgumentException("value is not serializable");
                if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                    throw new ArgumentException("expiryHint is not not serializable");
                if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                    throw new ArgumentException("evictionHint is not serializable");

                // Cache has possibly expired so do default.
                if (!IsRunning) return null; 

                ce[i] = new CacheEntry(value, expiryHint, evictionHint);

            }
            /// update the counters for various statistics
            try
            {
                return Insert(keys, ce, operationContext);
            }
            catch (Exception inner)
            {
                throw;
            }
        }


        /// <summary>
        /// Overload of Insert operation for bulk inserts. Uses EvictionHint and ExpirationHint arrays.
        /// </summary>
        public IDictionary Insert(object[] keys, object[] values, CallbackEntry[] callbackEnteries,
                                           ExpirationHint[] expirations, EvictionHint[] evictions,
                                           Hashtable[] queryInfos, BitSet[] flags,OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.InsertBlk", "");

            if (keys == null) throw new ArgumentNullException("keys");
            if (values == null) throw new ArgumentNullException("items");
            if (keys.Length != values.Length) throw new ArgumentException("keys count is not equals to values count");
           

            CacheEntry[] ce = new CacheEntry[values.Length];
            long[] sizes = null;
            object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
            if (dataSize != null)
            {
                sizes = (long[])dataSize;
            }
            for (int i = 0; i < values.Length; i++)
            {

                if (keys[i] == null) throw new ArgumentNullException("key");
                if (values[i] == null) throw new ArgumentNullException("value");

                if (!keys[i].GetType().IsSerializable)
                    throw new ArgumentException("key is not serializable");
                if (!values[i].GetType().IsSerializable)
                    throw new ArgumentException("value is not serializable");
                if ((expirations[i] != null) && !expirations[i].GetType().IsSerializable)
                    throw new ArgumentException("expiryHint is not not serializable");
                if ((evictions[i] != null) && !evictions[i].GetType().IsSerializable)
                    throw new ArgumentException("evictionHint is not serializable");

                // Cache has possibly expired so do default.
                if (!IsRunning) return null;

                ce[i] = new CacheEntry(values[i], expirations[i], evictions[i]);



                ce[i].QueryInfo = queryInfos[i];
                ce[i].Flag.Data |= flags[i].Data;
                if(sizes != null)
                    ce[i].DataSize = sizes[i];
                if (callbackEnteries[i] != null)
                {
                    CallbackEntry cloned = callbackEnteries[i].Clone() as CallbackEntry;
                    cloned.Value = values[i];
                    cloned.Flag = ce[i].Flag;
                    ce[i].Value = cloned;
                }
            }

            /// update the counters for various statistics
            try
            {
               

                HPTimeStats insertTime = new HPTimeStats();
                insertTime.BeginSample();

                IDictionary result = Insert(keys, ce, operationContext);

                insertTime.EndSample();

                return result;
            }
            catch (Exception inner)
            {
                throw;
            }
        }

        /// <summary>
        /// Internal Insert operation. Does a write thru as well.
        /// </summary>
        public Hashtable Insert(object[] keys, CacheEntry[] entries, OperationContext operationContext)
        {
            try
            {
                Hashtable result;
                result = CascadedInsert(keys, entries, true, operationContext);
                if (result != null)
                {

                    Hashtable tmp = (Hashtable)result.Clone();
                    IDictionaryEnumerator ide = tmp.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        CacheInsResultWithEntry insResult = null;
                        if (ide.Value is CacheInsResultWithEntry)
                        {
                            insResult = (CacheInsResultWithEntry)ide.Value;
                            switch (insResult.Result)
                            {
                                case CacheInsResult.Failure:
                                    break;

                                case CacheInsResult.NeedsEviction:
                                    result[ide.Key] = new OperationFailedException("The cache is full and not enough items could be evicted.");
                                    break;

                                case CacheInsResult.Success:
                                    result.Remove(ide.Key);
                                    break;

                                case CacheInsResult.SuccessOverwrite:
                                    result.Remove(ide.Key);
                                    break;
                            }
                        }
                    }
                }
                return result;
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.Insert()", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Insert()", inner.ToString());
                throw new OperationFailedException("Insert operation failed. Error : " + inner.Message, inner);
            }
        }


        #endregion



        #region	/                 --- Remove ---           /

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        public CompressedValueEntry Remove(object key, OperationContext operationContext)
        {
            return Remove(key as string, new BitSet(), null, null, LockAccessType.IGNORE_LOCK, operationContext);
        }

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        [CLSCompliant(false)]
        public CompressedValueEntry Remove(string key, BitSet flag, CallbackEntry cbEntry, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null; 
            try
            {
                HPTimeStats removeTime = new HPTimeStats();
                removeTime.BeginSample();

                _context.PerfStatsColl.MsecPerDelBeginSample();

                object packedKey = key;

                CacheEntry e = CascadedRemove(key, packedKey, ItemRemoveReason.Removed, true, lockId,accessType, operationContext);
                _context.PerfStatsColl.MsecPerDelEndSample();
                _context.PerfStatsColl.IncrementDelPerSecStats();
                removeTime.EndSample();

                if (e != null)
                {
                    CompressedValueEntry obj = new CompressedValueEntry();
                    obj.Value = e.Value;
                    obj.Flag = e.Flag;
                    if (obj.Value is CallbackEntry)
                        obj.Value = ((CallbackEntry)obj.Value).Value;
                    return obj;
                }
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable) _context.NCacheLog.Error("Cache.Remove()", ex.ToString());
                throw ex;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Remove()", inner.ToString());
                throw new OperationFailedException("Remove operation failed. Error : " + inner.Message, inner);
            }
            return null;
        }

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        [CLSCompliant(false)]
        public void Delete(string key, BitSet flag, CallbackEntry cbEntry, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return;

            try
            {
                HPTimeStats removeTime = new HPTimeStats();
                removeTime.BeginSample();

                _context.PerfStatsColl.MsecPerDelBeginSample();

                object packedKey = key;

                CacheEntry e = CascadedRemove(key, packedKey, ItemRemoveReason.Removed, true, lockId,accessType, operationContext);

                _context.PerfStatsColl.MsecPerDelEndSample();
                _context.PerfStatsColl.IncrementDelPerSecStats();
                removeTime.EndSample();
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable) _context.NCacheLog.Error("Cache.Delete()", ex.ToString());
                throw ex;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Delete()", inner.ToString());
                throw new OperationFailedException("Delete operation failed. Error : " + inner.Message, inner);
            }
        }

        #endregion



        #region	/                 --- Bulk Remove ---           /

        /// <summary>
        /// Removes the objects for the given keys from the cache.
        /// The keys are specified as parameter.
        /// </summary>
        /// <param name="keys">array of keys to be removed</param>
        /// <param name="flagMap"></param>
        /// <param name="cbEntry"></param>
        /// <param name="operationContext"></param>
        /// <returns>keys that failed to be removed</returns>
        public IDictionary Remove(object[] keys, BitSet flagMap, CallbackEntry cbEntry, OperationContext operationContext)
        {
            if (keys == null) throw new ArgumentNullException("keys");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null; 

            try
            {
                HPTimeStats removeTime = new HPTimeStats();
                removeTime.BeginSample();

                IDictionary removed = CascadedRemove(keys, ItemRemoveReason.Removed, true, operationContext);
                removeTime.EndSample();

                CompressedValueEntry val = null;
                if (removed != null)
                {
                    object[] keysCollection = new object[removed.Count];
                    removed.Keys.CopyTo(keysCollection, 0);
                    IEnumerator ie = keysCollection.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        CacheEntry entry = removed[ie.Current] as CacheEntry;
                        if (entry != null)
                        {
                            val = new CompressedValueEntry();
                            val.Value = entry.Value;
                            if (val.Value is CallbackEntry)
                                val.Value = ((CallbackEntry)val.Value).Value;
                            val.Flag = entry.Flag;
                            removed[ie.Current] = val;
                        }
                    }
                }
                return removed;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Remove()", inner.ToString());
                throw new OperationFailedException("Remove operation failed. Error : " + inner.Message, inner);
            }
            return null;
        }

        /// <summary>
        /// Removes the objects for the given keys from the cache.
        /// The keys are specified as parameter.
        /// </summary>
        /// <param name="keys">array of keys to be removed</param>
        public void Delete(object[] keys, BitSet flagMap, CallbackEntry cbEntry, OperationContext operationContext)
        {
            if (keys == null) throw new ArgumentNullException("keys");

            // Cache has possibly expired so do default.
            if (!IsRunning) return; 

           

            try
            {
                HPTimeStats removeTime = new HPTimeStats();
                removeTime.BeginSample();
                IDictionary removed = CascadedRemove(keys, ItemRemoveReason.Removed, true, operationContext);
                removeTime.EndSample();
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Delete()", inner.ToString());
                throw new OperationFailedException("Delete operation failed. Error : " + inner.Message, inner);
            }
        }
        #endregion

        #region	/                 --- GetEnumerator ---           /

        /// <summary>
        /// Retrieves a dictionary enumerator used to iterate through the key settings and their 
        /// values contained in the cache
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            // Cache has possibly expired so do default.
            if (!IsRunning)
                return null; 

            try
            {
                return new Alachisoft.NCache.Caching.Util.CacheEnumerator(_context.SerializationContext, _context.CacheImpl.GetEnumerator());
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.GetEnumerator()", inner.ToString());
                throw new OperationFailedException("GetEnumerator failed. Error : " + inner.Message, inner);
            }
        }

        public EnumerationDataChunk GetNextChunk(EnumerationPointer pointer, OperationContext operationContext)
        {
            EnumerationDataChunk chunk = null;

            if (!IsRunning)
                return null;

            try
            {
                chunk = _context.CacheImpl.GetNextChunk(pointer, operationContext);
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.GetNextChunk()", inner.ToString());
                throw new OperationFailedException("GetNextChunk failed. Error : " + inner.Message, inner);
            }

            return chunk;
        }

        #endregion
        
        #region	/                 --- ICacheEventsListener ---           /

        /// <summary>
        /// Fired when an item is removed from the cache having CacheItemRemoveCallback.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="value">CallbackEntry containing the callback and actual item</param>
        /// <param name="reason">reason the item was removed</param>
        void ICacheEventsListener.OnCustomRemoveCallback(object key, object value, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext)
        {
            CallbackEntry cbEntry = value as CallbackEntry;

            ArrayList removeCallbacklist = eventContext.GetValueByField(EventContextFieldName.ItemRemoveCallbackList) as ArrayList;


            if (removeCallbacklist != null && removeCallbacklist.Count > 0)
            {
                foreach (CallbackInfo cbInfo in removeCallbacklist)
                {

                    if (reason == ItemRemoveReason.Expired && cbInfo != null && !cbInfo.NotifyOnExpiration)
                        continue;

                    if (_connectedClients != null && _connectedClients.Contains(cbInfo.Client))
                    {
                        if (_customRemoveNotif != null)
                        {
                            Delegate[] dltList = _customRemoveNotif.GetInvocationList();
                            for (int i = dltList.Length - 1; i >= 0; i--)
                            {
                                CustomRemoveCallback subscriber = (CustomRemoveCallback)dltList[i];
                                try
                                {
                                    subscriber.BeginInvoke(key, new object[] { null, cbInfo }, reason, null, eventContext, new System.AsyncCallback(CustomRemoveAsyncCallbackHandler), subscriber);
                                }
                                catch (System.Net.Sockets.SocketException e)
                                {
                                    _context.NCacheLog.Error("Cache.OnCustomRemoveCallback()", e.ToString());
                                    _customRemoveNotif -= subscriber;
                                }
                                catch (Exception e)
                                {
                                    _context.NCacheLog.Error("Cache.OnCustomRemoveCallback", e.ToString());
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This callback is called by .Net framework after asynchronous call for 
        /// OnCustomRemoveCallback has ended. 
        /// </summary>
        /// <param name="ar"></param>
        internal void CustomRemoveAsyncCallbackHandler(IAsyncResult ar)
        {
            CustomRemoveCallback subscribber = (CustomRemoveCallback)ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Client is dead, so remove from the event handler list
                _context.NCacheLog.Error("Cache.CustomRemoveAsyncCallbackHandler", ex.ToString());
                lock (_customRemoveNotif)
                {
                    _customRemoveNotif -= subscribber;
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.CustomRemoveAsyncCallbackHandler", e.ToString());
            }
        }
        /// <summary>
        /// Fired when an item is updated and it has CacheItemUpdate callback attached with it.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="value">CallbackEntry containing the callback and actual item</param>
        void ICacheEventsListener.OnCustomUpdateCallback(object key, object value, OperationContext operationContext, EventContext eventContext)
        {
            ArrayList updateListeners = value as ArrayList;

            if (updateListeners != null && updateListeners.Count > 0)
            {
                updateListeners = updateListeners.Clone() as ArrayList;
                foreach (CallbackInfo cbInfo in updateListeners)
                {
                    if (_connectedClients != null && _connectedClients.Contains(cbInfo.Client))
                    {
                        if (_customUpdateNotif != null)
                        {
                            Delegate[] dltList = _customUpdateNotif.GetInvocationList();
                            for (int i = dltList.Length - 1; i >= 0; i--)
                            {
                                CustomUpdateCallback subscriber = (CustomUpdateCallback)dltList[i];
                                try
                                {
                                    subscriber.BeginInvoke(key, cbInfo, eventContext, new System.AsyncCallback(CustomUpdateAsyncCallbackHandler), subscriber);
                                }
                                catch (System.Net.Sockets.SocketException e)
                                {
                                    _context.NCacheLog.Error("Cache.OnCustomUpdateCallback()", e.ToString());
                                    _customUpdateNotif -= subscriber;
                                }
                                catch (Exception e)
                                {
                                    _context.NCacheLog.Error("Cache.OnCustomUpdateCallback", e.ToString());
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fire when hasmap changes when 
        /// - new node joins
        /// - node leaves
        /// - manual/automatic load balance
        /// </summary>
        /// <param name="newHashmap">new hashmap</param>
        void ICacheEventsListener.OnHashmapChanged(NewHashmap newHashmap, bool updateClientMap)
        {
                if (this._hashmapChanged == null) return;
                Delegate[] dlgList = this._hashmapChanged.GetInvocationList();

                NewHashmap.Serialize(newHashmap, this._context.SerializationContext, updateClientMap);

                foreach (HashmapChangedCallback subscriber in dlgList)
                {
                    try
                    {
                        subscriber.BeginInvoke(newHashmap, null, new AsyncCallback(HashmapChangedAsyncCallbackHandler), subscriber);
                    }
                    catch (System.Net.Sockets.SocketException ex)
                    {
                        _context.NCacheLog.Error("Cache.OnHashmapChanged", ex.ToString());
                        this._hashmapChanged -= subscriber;
                    }
                    catch (Exception ex)
                    {
                        _context.NCacheLog.Error("Cache.OnHashmapChanged", ex.ToString());
                    }
                }
        }

        /// <summary>
        /// Called when OnHashmapChanged is invoked and processed.
        /// </summary>
        /// <param name="result">result</param>
        internal void HashmapChangedAsyncCallbackHandler(IAsyncResult result)
        {
            HashmapChangedCallback subscriber = (HashmapChangedCallback)result.AsyncState;
            try
            {
                subscriber.EndInvoke(result);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _context.NCacheLog.Error("Cache.HashmapChangedAsyncCallbackHandler", ex.ToString());
                this._hashmapChanged -= subscriber;
            }
            catch (Exception ex)
            {
                _context.NCacheLog.Error("Cache.HashmapChangedAsyncCallbackHandler", ex.ToString());
            }
        }

        /// <summary>
        /// This callback is called by .Net framework after asynchronous call for 
        /// OnCustomUpdateCallback has ended. 
        /// </summary>
        /// <param name="ar"></param>
        internal void CustomUpdateAsyncCallbackHandler(IAsyncResult ar)
        {
            CustomUpdateCallback subscribber = (CustomUpdateCallback)ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Client is dead, so remove from the event handler list
                _context.NCacheLog.Error("Cache.CustomUpdateAsyncCallbackHandler", ex.ToString());
                lock (_customUpdateNotif)
                {
                    _customUpdateNotif -= subscribber;
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.CustomUpdateAsyncCallbackHandler", e.ToString());
            }
        }


        /// <summary>
        /// Clears the list of all callback listeners when disposing.
        /// </summary>
        private void ClearCallbacks()
        {
            if (_cacheStopped != null)
            {
                Delegate[] dltList = _cacheStopped.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    CacheStoppedCallback subscriber = (CacheStoppedCallback)dltList[i];
                    _cacheStopped -= subscriber;
                }
            }
           
            if (_customUpdateNotif != null)
            {
                Delegate[] dltList = _customUpdateNotif.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    CustomUpdateCallback subscriber = (CustomUpdateCallback)dltList[i];
                    _customUpdateNotif -= subscriber;
                }
            }
            if (_customRemoveNotif != null)
            {
                Delegate[] dltList = _customRemoveNotif.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    CustomRemoveCallback subscriber = (CustomRemoveCallback)dltList[i];
                    _customRemoveNotif -= subscriber;
                }
            }
        }

        #endregion



        #region /                 --- Search ---           /

        public QueryResultSet Search(string query, IDictionary values, OperationContext operationContext)
        {
            if (!IsRunning) return null;

            if (query == null || query == String.Empty)
                throw new ArgumentNullException("query");

            try
            {
                return _context.CacheImpl.Search(query, values, operationContext);
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("search operation failed. Error: " + ex.ToString());
                }
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (Alachisoft.NCache.Parser.TypeIndexNotDefined inner)
            {
                throw new Runtime.Exceptions.TypeIndexNotDefined(inner.Message);
            }
            catch (Alachisoft.NCache.Parser.AttributeIndexNotDefined inner)
            {
                throw new Runtime.Exceptions.AttributeIndexNotDefined(inner.Message);
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("search operation failed. Error: " + ex.ToString());
                throw new OperationFailedException("search operation failed. Error: " + ex.Message, ex);
            }
        }

        public QueryResultSet SearchEntries(string query, IDictionary values, OperationContext operationContext)
        {
            if (!IsRunning) return null;

            if (query == null || query == String.Empty)
                throw new ArgumentNullException("query");
            try
            {
                return _context.CacheImpl.SearchEntries(query, values, operationContext);
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("search operation failed. Error: " + ex.ToString());
                }
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("search operation failed. Error: " + ex.ToString());

                if (ex is Alachisoft.NCache.Parser.TypeIndexNotDefined)
                    throw new Runtime.Exceptions.TypeIndexNotDefined("search operation failed. Error: " + ex.Message, ex);
                if (ex is Alachisoft.NCache.Parser.AttributeIndexNotDefined)
                    throw new Runtime.Exceptions.AttributeIndexNotDefined("search operation failed. Error: " + ex.Message, ex);

                throw new OperationFailedException("search operation failed. Error: " + ex.Message, ex);
            }
        }

        #endregion

        
        #region /               --- CacheImpl Calls for Cascading Dependnecies ---          /

        internal CacheInsResultWithEntry CascadedInsert(object key, CacheEntry entry, bool notify, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
  

          CacheInsResultWithEntry result = _context.CacheImpl.Insert(key, entry, notify, lockId, accessType, operationContext);
            return result;
        }


        internal Hashtable CascadedInsert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            Hashtable table = _context.CacheImpl.Insert(keys, cacheEntries, notify, operationContext);
            return table;
        }

        internal CacheEntry CascadedRemove(object key, object pack, ItemRemoveReason reason, bool notify, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry oldEntry = _context.CacheImpl.Remove(pack, reason, notify, lockId, accessType, operationContext);

            return oldEntry;
        }
        internal Hashtable CascadedRemove(IList keys, ItemRemoveReason reason, bool notify, OperationContext operationContext)
        {
            Hashtable table = _context.CacheImpl.Remove(keys, reason, notify, operationContext);
            return table;
        }

       





      

        

        #endregion

        #region IClusterEventsListener Members
        public void OnClientsInvalidated(ArrayList invalidatedClientsList)
        {
            if (_clientsInvalidated != null)
            {
                //object add = SerializationUtil.CompactSerialize(address, _context.SerializationContext);
                Delegate[] dltList = _clientsInvalidated.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    ClientsInvalidatedCallback subscriber = (ClientsInvalidatedCallback)dltList[i];
                    try
                    {
                        subscriber.BeginInvoke(invalidatedClientsList, new System.AsyncCallback(ClientsInvalidatedAsyncCallbackHandler), subscriber);
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        _context.NCacheLog.Error("Cache.OnMemberJoined()", e.ToString());
                        _clientsInvalidated -= subscriber;
                    }
                    catch (Exception e)
                    {
                        _context.NCacheLog.Error("Cache.OnMemberJoined", e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// This callback is called by .Net framework after asynchronous call for 
        /// OnItemAdded has ended. 
        /// </summary>
        /// <param name="ar"></param>
        internal void ClientsInvalidatedAsyncCallbackHandler(IAsyncResult ar)
        {
            ClientsInvalidatedCallback subscribber = (ClientsInvalidatedCallback)ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Client is dead, so remove from the event handler list
                _context.NCacheLog.Error("Cache.MemberJoinedAsyncCallbackHandler", ex.ToString());
                lock (_memberJoined)
                {
                    _clientsInvalidated -= subscribber;
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.MemberJoinedAsyncCallbackHandler", e.ToString());
            }
        }
        
#if !CLIENT
        public int GetNumberOfClientsToDisconect()
        {

            ReplicatedServerCache impl = this._context.CacheImpl as ReplicatedServerCache;
            if (impl == null) return 0;
            
            if (_inProc) return 0;

            ArrayList nodes = ((ClusterCacheBase)this._context.CacheImpl)._stats.Nodes;

            int clientsToDisconnect = 0;
            int totalClients = 0;
            int currNodeClientCount = 0;
            int maxClientsPerNode;
            try
            {
                foreach (NodeInfo i in nodes)
                {
                    if (((ClusterCacheBase)this._context.CacheImpl).Cluster.LocalAddress.CompareTo(i.Address) == 0)
                    {
                        currNodeClientCount = i.ConnectedClients.Count;
                    }
                    totalClients = totalClients + i.ConnectedClients.Count;
                }
            }
            catch (Exception)
            {
            }

            maxClientsPerNode = (int)Math.Ceiling(((decimal)totalClients / nodes.Count));

            if (currNodeClientCount > maxClientsPerNode)
            {
                clientsToDisconnect = currNodeClientCount - maxClientsPerNode;
                return clientsToDisconnect;
            }

            return 0;

        }
#endif
        public void OnMemberJoined(Alachisoft.NCache.Common.Net.Address clusterAddress, Alachisoft.NCache.Common.Net.Address serverAddress)
        {
            int clientsToDisconnect = 0;
            try
            {
#if !CLIENT
                    clientsToDisconnect = this.GetNumberOfClientsToDisconect();
#endif
            }
            catch (Exception)
            {
                clientsToDisconnect = 0;
            }

            if (_memberJoined != null)
            {
                Delegate[] dltList = _memberJoined.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    NodeJoinedCallback subscriber = (NodeJoinedCallback)dltList[i];
                    try
                    {
                        if (i > (clientsToDisconnect - 1))
                            subscriber.BeginInvoke(clusterAddress, serverAddress, false, null, new System.AsyncCallback(MemberJoinedAsyncCallbackHandler), subscriber);
                        else
                            subscriber.BeginInvoke(clusterAddress, serverAddress, true, null, new System.AsyncCallback(MemberJoinedAsyncCallbackHandler), subscriber);
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        _context.NCacheLog.Error("Cache.OnMemberJoined()", e.ToString());
                        _memberJoined -= subscriber;
                    }
                    catch (Exception e)
                    {
                        _context.NCacheLog.Error("Cache.OnMemberJoined", e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// This callback is called by .Net framework after asynchronous call for 
        /// OnItemAdded has ended. 
        /// </summary>
        /// <param name="ar"></param>
        internal void MemberJoinedAsyncCallbackHandler(IAsyncResult ar)
        {
            NodeJoinedCallback subscribber = (NodeJoinedCallback)ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Client is dead, so remove from the event handler list
                _context.NCacheLog.Error("Cache.MemberJoinedAsyncCallbackHandler", ex.ToString());
                lock (_memberJoined)
                {
                    _memberJoined -= subscribber;
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.MemberJoinedAsyncCallbackHandler", e.ToString());
            }
        }

        public void OnMemberLeft(Alachisoft.NCache.Common.Net.Address clusterAddress, Alachisoft.NCache.Common.Net.Address serverAddress)
        {
            if (_memberLeft != null)
            {

                Delegate[] dltList = _memberLeft.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    NodeLeftCallback subscriber = (NodeLeftCallback)dltList[i];
                    try
                    {
                        subscriber.BeginInvoke(clusterAddress, serverAddress, null, new System.AsyncCallback(MemberLeftAsyncCallbackHandler), subscriber);
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        _context.NCacheLog.Error("Cache.OnMemberLeft()", e.ToString());
                        _memberLeft -= subscriber;
                    }
                    catch (Exception e)
                    {
                        _context.NCacheLog.Error("Cache.OnMemberLeft", e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// This callback is called by .Net framework after asynchronous call for 
        /// OnItemAdded has ended. 
        /// </summary>
        /// <param name="ar"></param>
        internal void MemberLeftAsyncCallbackHandler(IAsyncResult ar)
        {
            NodeLeftCallback subscribber = (NodeLeftCallback)ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Client is dead, so remove from the event handler list
                _context.NCacheLog.Error("Cache.MemberLeftAsyncCallbackHandler", ex.ToString());
                lock (_memberLeft)
                {
                    _memberLeft -= subscribber;
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.MemberLeftAsyncCallbackHandler", e.ToString());
            }
        }

        #endregion

        #region /              --- Key based notifications ---           /

        /// <summary>
        /// Registers the item update/remove or both callbacks with the specified key.
        /// Keys should exist before the registration.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="updateCallback"></param>
        /// <param name="removeCallback"></param>
        public void RegisterKeyNotificationCallback(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            if (!IsRunning) return;
            if (key == null) throw new ArgumentNullException("key");
            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            try
            {
                _context.CacheImpl.RegisterKeyNotification(key, updateCallback, removeCallback, operationContext);
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.RegisterKeyNotificationCallback() ", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.RegisterKeyNotificationCallback() ", inner.ToString());
                throw new OperationFailedException("RegisterKeyNotification failed. Error : " + inner.Message, inner);
            }

        }

        internal void RegisterKeyNotificationCallback(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            if (!IsRunning) return; 
            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("Keys count can not be zero");
            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            try
            {
                _context.CacheImpl.RegisterKeyNotification(keys, updateCallback, removeCallback, operationContext);
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.RegisterKeyNotificationCallback() ", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.RegisterKeyNotificationCallback() ", inner.ToString());
                throw new OperationFailedException("RegisterKeyNotification failed. Error : " + inner.Message, inner);
            }

        }

        /// <summary>
        /// Unregisters the item update/remove or both callbacks with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="updateCallback"></param>
        /// <param name="removeCallback"></param>
        public void UnregisterKeyNotificationCallback(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            if (!IsRunning) return; 
            if (key == null) throw new ArgumentNullException("key");
            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            try
            {
                _context.CacheImpl.UnregisterKeyNotification(key, updateCallback, removeCallback, operationContext);
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.UnregisterKeyNotificationCallback() ", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.UnregisterKeyNotificationCallback()", inner.ToString());
                throw new OperationFailedException("UnregisterKeyNotification failed. Error : " + inner.Message, inner);
            }

        }

        internal void UnregisterKeyNotificationCallback(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            if (!IsRunning) return; 
            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("Keys count can not be zero");
            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            try
            {
                _context.CacheImpl.UnregisterKeyNotification(keys, updateCallback, removeCallback, operationContext);
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.UnregisterKeyNotificationCallback() ", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.UnregisterKeyNotificationCallback()", inner.ToString());
                throw new OperationFailedException("UnregisterKeyNotification failed. Error : " + inner.Message, inner);
            }

        }

        #endregion

        /// <summary>
        /// Apply runtime configuration settings.
        /// </summary>
        /// <param name="cacheConfig"></param>

        public Exception CanApplyHotConfig(HotConfig hotConfig)
        {
            if (this._context.CacheImpl != null && !this._context.CacheImpl.CanChangeCacheSize(hotConfig.CacheMaxSize))
                return new Exception("You need to remove some data from cache before applying the new size");
            return null;
        }

        public void ApplyHotConfiguration(HotConfig hotConfig)
        {

            if (hotConfig != null)
            {
                #region Error Logging Config
                if (hotConfig.IsErrorLogsEnabled && _cacheInfo != null)
                {
                    if (!_context.NCacheLog.IsErrorEnabled)
                    {

                        string cache_name = _cacheInfo.Name;
                        if (_cacheInfo.CurrentPartitionId != null && _cacheInfo.CurrentPartitionId != string.Empty)
                            cache_name += "-" + _cacheInfo.CurrentPartitionId;
                        _context.NCacheLog.SetLevel("error");
                    }

                    if (hotConfig.IsDetailedLogsEnabled)
                        _context.NCacheLog.SetLevel("all");
                }

                else if (!hotConfig.IsErrorLogsEnabled)
                {
                    _context.NCacheLog.SetLevel("off");
                }


                #endregion
                this._context.CacheImpl.InternalCache.MaxSize = this._context.CacheImpl.ActualStats.MaxSize = hotConfig.CacheMaxSize;
                this._context.ExpiryMgr.CleanInterval = hotConfig.CleanInterval;
                this._context.CacheImpl.InternalCache.EvictRatio = hotConfig.EvictRatio / 100;


                if (this._configurationModified != null) this._configurationModified(hotConfig);
            }

        }


        #region /              --- Manual Load Balancing ---           /

        public void BalanceDataLoad()
        {
           _context.CacheImpl.BalanceDataLoad();
        }

        #endregion

        public NewHashmap GetOwnerHashMap(out int bucketSize)
        {
            return _context.CacheImpl.GetOwnerHashMapTable(out bucketSize);
        }

        internal static float ClientsRequests
        {
            get { return Interlocked.Exchange(ref s_clientsRequests, 0); }
        }

        internal static float ClientsBytesSent
        {
            get { return Interlocked.Exchange(ref s_clientsBytesSent, 0); }
        }

        internal static float ClientsBytesRecieved
        {
            get { return Interlocked.Exchange(ref s_clientsBytesRecieved, 0); }
        }

        public CacheServerConfig Configuration
        {
            get { return _cacheInfo.Configuration; }
            set { _cacheInfo.Configuration = value; }
        }

        /// <summary>
        /// Update socket server statistics
        /// </summary>
        /// <param name="stats"></param>
        public void UpdateSocketServerStats(SocketServerStats stats)
        {
            Interlocked.Exchange(ref s_clientsRequests, s_clientsRequests + stats.Requests);
            Interlocked.Exchange(ref s_clientsBytesSent, s_clientsBytesSent + stats.BytesSent);
            Interlocked.Exchange(ref s_clientsBytesRecieved, s_clientsBytesRecieved + stats.BytesRecieved);
        }

        /// <summary>
        /// To Get the string for the TypeInfoMap used in Queries.
        /// </summary>
        /// <returns>String representation of the TypeInfoMap for this cache.</returns>
        public TypeInfoMap GetTypeInfoMap()
        {
            
            if (!IsRunning) return null;
            else return _context.CacheImpl.TypeInfoMap;

        }
        public bool IsServerNodeIp(Address clientAddress)
        {
            return _context.CacheImpl.IsServerNodeIp(clientAddress);
        }

   
    }
}

