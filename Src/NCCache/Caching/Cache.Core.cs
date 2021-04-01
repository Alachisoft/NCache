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
using System.Collections;
using System.Threading;
#if !NETCORE
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Lifetime;
using Alachisoft.NCache.Common.Remoting;

#endif
using System.Diagnostics;
using System.IO;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Serialization;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.ErrorHandling;
#if SERVER
using Alachisoft.NCache.Caching.Topologies.Clustered;
#endif
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Propagator;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Caching.Enumeration;
using Alachisoft.NCache.Persistence;
using EnumerationPointer = Alachisoft.NCache.Common.DataStructures.EnumerationPointer;
using Exception = System.Exception;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Runtime.Caching;
using ClientInfo = Alachisoft.NCache.Runtime.Caching.ClientInfo;
using Alachisoft.NCache.Caching.Messaging;
using System.Runtime;
using Alachisoft.NCache.Common.Topologies.Clustered;
using Alachisoft.NCache.Common.Locking;

using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Common.Resources;
using System.Threading.Tasks;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;

using Alachisoft.NCache.Common.Pooling.Stats;
using Alachisoft.NCache.Caching.CacheHealthAlerts;
using Alachisoft.NCache.Common.FeatureUsageData;
using Alachisoft.NCache.Common.FeatureUsageData.Dom;

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// The main class that is the interface of the system with the outside world. This class
    /// is remotable (MarshalByRefObject). 
    /// </summary>
    public class Cache : MarshalByRefObject, IEnumerable, ICacheEventsListener, IClusterEventsListener, IDisposable
    {
        /// <summary> The name of the cache instance. </summary>
        private CacheInfo _cacheInfo = new CacheInfo();

        /// <summary> The runtime context associated with the current cache. </summary>
        private CacheRuntimeContext _context = new CacheRuntimeContext();

#if !NETCORE
        /// <summary> sponsor object </summary>
        private ISponsor _sponsor = new RemoteObjectSponsor();

        /// <summary> The runtime context associated with the current cache. </summary>
        private RemotingChannels _channels;
#endif

        /// <summary> delegate for item addition notifications. </summary>
        private event ItemAddedCallback _itemAdded;

        /// <summary> delegate for item updation notifications. </summary>
        private event ItemUpdatedCallback _itemUpdated;

        /// <summary> delegate for item removal notifications. </summary>
        private event ItemRemovedCallback _itemRemoved;

        /// <summary> delegate for cache clear notifications. </summary>
        private event CacheClearedCallback _cacheCleared;

        /// <summary> delegate for custom notification. </summary>
        private event CustomNotificationCallback _cusotmNotif;

        /// <summary> delegate for custom  remove callback notifications. </summary>
        private event CustomRemoveCallback _customRemoveNotif;

        /// <summary> delegate for custom update callback notifications. </summary>
        private event CustomUpdateCallback _customUpdateNotif;


      
        /// <summary> delegate for async operations. </summary>

        private event AsyncOperationCompletedCallback _asyncOperationCompleted;

        private event DataSourceUpdatedCallback _dataSourceUpdated;

        private event CacheStoppedCallback _cacheStopped;
        private event CacheBecomeActiveCallback _cacheBecomeActive;

        private event NodeJoinedCallback _memberJoined;
        private event NodeLeftCallback _memberLeft;


#if !DEVELOPMENT

        private event HashmapChangedCallback _hashmapChanged;
#endif
        private event OperationModeChangedCallback _operationModeChange;
        private event BlockClientActivity _blockClientActivity;
        private event UnBlockClientActivity _unblockClientActivity;

        public delegate void CacheStoppedEvent(string cacheName);

        public delegate void CacheStartedEvent(string cacheName);

        public static CacheStoppedEvent OnCacheStopped;
        public static CacheStartedEvent OnCacheStarted;

        private event ConfigurationModified _configurationModified;
        private event CompactTypeModifiedCallback _compactTypeModified;
        private event PollRequestCallback _pollRequestNotif;
        
        private ArrayList _connectedClients = new ArrayList();

        /// <summary> Indicates wtherher a cache is InProc or not. </summary>
        private bool _inProc;

        public const int COMPRESSIONTHRESHOLD = 2;
        private bool _compressionEnabled = true;
        private long _compressionThresholdSize = COMPRESSIONTHRESHOLD;
        private bool _deathDetectionEnabled = false;
        private int _graceTime = 0;
        public static int ServerPort;

        private static float s_clientsRequests = 0;
        private static float s_clientsBytesRecieved = 0;
        private static float s_clientsBytesSent = 0;
        private long _lockIdTicker = 0;

        /// <summary> Holds the cach type name. </summary>
        private string _cacheType;

        private object _shutdownMutex = new object();

        private Hashtable _encryptionTable = new Hashtable();

        private Hashtable _encryptionInfo = new Hashtable();

        private bool _isPersistEnabled = false;

        private bool _isDeathDeductionEnabled = false;

        private int _persistenceInterval = 5;

        private string _uniqueId;
        private string _blockserverIP;
        private long _blockinterval = 180;
        private DateTime _startShutDown;
        private Latch _shutDownStatusLatch = new Latch(ShutDownStatus.NONE);
        private bool isClustered;
      
        public AsyncProcessor _asyncProcessor;
        // created seperately for async clear, add, insert and remove operations from client for graceful shutdown.

        private string _cacheserver = "NCache";

        private StringPoolTrimmingTask _stringPoolTrimmingTask;
        private HealthMonitor healthMonitor = null;

        public bool _isClustered
        {
            get { return isClustered; }
            set { isClustered = value; }
        }



        internal IDataFormatService CachingSubSystemDataService
        {
            get { return _cachingSubSystemDataService; }
            set { _cachingSubSystemDataService = value; }
        }

        public long BlockInterval
        {
            get
            {
                long startTime = (_startShutDown.Ticks - 621355968000000000) / 10000;
                long timeout = Convert.ToInt32(_blockinterval * 1000) - (int)((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - startTime);
                return (timeout / 1000);
            }
            set { _blockinterval = value; }
        }

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

        public DataFormat DataFormat
        {
            get { return _context.InMemoryDataFormat; }
        }

        public PoolManager FakeObjectPoolManager
        {
            get => Context.FakeObjectPool;
        }

        public PoolManager TransactionalPoolManager
        {
            get => Context.TransactionalPoolManager;
        }

        private SQLDependencySettings _sqlDependencySettings;

        /// <summary> Thread to reset Instantaneous Stats after every one second.</summary>
        private Thread _instantStatsUpdateTimer;

        private bool threadRunning = true;
        private TimeSpan InstantStatsUpdateInterval = new TimeSpan(0, 0, 1);
        private static bool s_logClientEvents;

        private IDataFormatService _socketServerDataService;
        private IDataFormatService _cachingSubSystemDataService;
        public string _serverCacheName = "";

        public bool _enableEventsPolling = false;
        private EventManager _eventManager = new EventManager();

        //private CacheClientConnectivityChangedCallback _clientConnectivityChanged;

        /// <summary>
        /// Default constructor.
        /// </summary>
        static Cache()
        {
            MiscUtil.RegisterCompactTypes(null);
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
#if !NETCORE
                    RemotingServices.Disconnect(this);
#endif
                }
                catch (Exception)
                {
                }
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
                try
                {
                    if (_cacheStopped != null && CacheType != null && !CacheType.Equals("mirror-server"))
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
                                NCacheLog.Error("Cache.Dispose",
                                    "Error occurred while invoking cache stopped event: " + e.ToString());
                                ///Ignore and move on to fire next
                            }
                            finally
                            {
                                this._cacheStopped -= callback;
                            }
                        }
                    }

                  

#if !NETCORE
                    try
                    {
                        if (_inProc) RemotingServices.Disconnect(this);
                    }
                    catch (Exception)
                    {
                    }
#endif
                    if (_context.CacheImpl != null)
                        _context.CacheImpl.StopServices();

                   
                   

                    if (_cacheStopped != null && CacheType != null && CacheType.Equals("mirror-server")) _cacheStopped(_cacheInfo.Name, null);

                   

                    if (_asyncProcessor != null)
                    {
                        _asyncProcessor.Stop();
                        _asyncProcessor = null;
                    }

                    if (_connectedClients != null)
                        lock (_connectedClients.SyncRoot)
                        {
                            _connectedClients.Clear();
                        }

                    ClearCallbacks();
                    _cacheStopped = null;
                    _cacheCleared = null;

                    _itemAdded = null;
                    _itemUpdated = null;
                    _itemRemoved = null;
                    _cusotmNotif = null;

#if !NETCORE
                    _sponsor = null;
#endif

                    if (NCacheLog != null)
                    {
                        NCacheLog.CriticalInfo("Cache.Dispose", "Cache stopped successfully");
                        NCacheLog.Flush();
                        NCacheLog.Close();
                    }

#if !NETCORE
                    try
                    {
                        if (_channels != null) _channels.UnregisterTcpChannels();
                    }
                    catch (Exception)
                    {
                    }
                    _channels = null;
#endif
                    if (disposing)
                    {
                        GC.SuppressFinalize(this);
                    }

                    if (_context != null)
                    {
                        _context.Dispose();
                    }

                    //Dispose snapshot pool for this cache.
                    if (disposing)
                    {
                        if (CacheSnapshotPool.Instance != null)
                            CacheSnapshotPool.Instance.DisposePool(_context.CacheRoot.Name);
                    }
                }
                catch (Exception)
                {

                    throw;
                }
                finally
                {
                    if (_context != null)
                        _context.CacheImpl = null;

                }

            }
        }
        
        public bool IsClusterUnderMaintenance()
        {
            return _context.IsClusterUnderMaintenance();
        }
        
        public bool IsClusterAvailableForMaintenance()
        {
            return _context.IsClusterAvailableForMaintenance();
        }

        public bool IsClusterInStateTransfer()
        {
            return _context.IsClusterInStateTransfer();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            try
            {
                if (healthMonitor != null)
                {
                    healthMonitor.Dispose();
                    healthMonitor = null;
                }

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

#if !( CLIENT)
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

        public Hashtable CompactRegisteredTypesForDotNet
        {
            get { return Context.CompactKnownTypesNET; }
        }

        public Hashtable CompactRegisteredTypesForJava
        {
            get { return Context.CompactKnownTypesJAVA; }
        }


        public Hashtable EncryptionInformation
        {
            get { return _encryptionInfo; }

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

#if SERVER
        public string TargetCacheUniqueID
        {
            get
            {   return string.Empty;
            }
        }
#endif

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


     

        public List<CacheNodeStatistics> GetCacheNodeStatistics()
        {
            List<CacheNodeStatistics> statistics = null;
            if (!IsRunning)
            {
                statistics = new List<CacheNodeStatistics>();
                CacheNodeStatistics nodeStats = new CacheNodeStatistics(null);
                nodeStats.Status = CacheNodeStatus.Stopped;
                statistics.Add(nodeStats);
            }
            else
            {
                statistics = _context.CacheImpl.GetCacheNodeStatistics();
                if (statistics != null && statistics.Count > 0)
                    statistics[0].ClientCount = (ushort)_connectedClients.Count;
            }
            return statistics;

        }

        public long CompressThresholdSize { get { return _compressionThresholdSize * 1024; } }
        public bool CompressionEnabled { get { return _compressionEnabled; } }

        public SerializationFormat SerializationFormat { get { return _context.SerializationFormat; } }

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



      

        /// <summary>
        /// Gets or sets the cache item at the specified key.
        /// </summary>
        public object this[object key]
        {
            get
            {
                OperationContext operationContext = null;
                BitSet bitSet = null;
                try
                {
                    object lockId = null;
                    DateTime lockDate = DateTime.UtcNow;
                    ulong version = 0;

                  
                    operationContext = OperationContext.CreateAndMarkInUse(
                        Context.TransactionalPoolManager, NCModulesConstants.CacheCore, OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation
                    );
                    operationContext.UseObjectPool = false;
                    bitSet = BitSet.CreateAndMarkInUse(Context.TransactionalPoolManager, NCModulesConstants.CacheCore);
                    return GetGroup(key, bitSet, null, null, ref version, ref lockId, ref lockDate, TimeSpan.Zero, LockAccessType.IGNORE_LOCK, operationContext).Value;
                }
                finally
                {
                    MiscUtil.ReturnOperationContextToPool(operationContext, Context.TransactionalPoolManager);
                    operationContext?.MarkFree(NCModulesConstants.CacheCore);
                    MiscUtil.ReturnBitsetToPool(bitSet, _context.TransactionalPoolManager);
                    bitSet?.MarkFree(NCModulesConstants.CacheCore);
                }
            }
            set { Insert(key, value); }
        }


        /// <summary> delegate for item addition notifications. </summary>
        public event ItemAddedCallback ItemAdded
        {
            add { 
                _itemAdded += value;
                if (_itemAdded?.GetInvocationList().Length > 0)
                {
                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.general_events, FeatureEnum.data_sharing).UpdateUsageTime();
                }
            }
            remove { _itemAdded -= value; }
        }

        /// <summary> delegate for item updation notifications. </summary>
        public event ItemUpdatedCallback ItemUpdated
        {
            add { _itemUpdated += value;
                if (_itemUpdated?.GetInvocationList().Length > 0)
                {
                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.general_events, FeatureEnum.data_sharing).UpdateUsageTime();
                }
            }
            remove { _itemUpdated -= value; }
        }

        /// <summary> delegate for item removal notifications. </summary>
        public event ItemRemovedCallback ItemRemoved
        {
            add { _itemRemoved += value;

                if (_itemRemoved?.GetInvocationList().Length > 0)
                {
                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.general_events, FeatureEnum.data_sharing).UpdateUsageTime();
                }
            }
            remove { _itemRemoved -= value; }
        }

        /// <summary> delegate for cache clear notifications. </summary>
        public event CacheClearedCallback CacheCleared
        {
            add { _cacheCleared += value;

                if(_cacheCleared?.GetInvocationList().Length > 0)
                {
                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.cache_clear_event_feature, FeatureEnum.cache_event).UpdateUsageTime();
                }
            }
            remove { _cacheCleared -= value; }
        }

        /// <summary> delegate for cusotm notifications.  </summary>
        public event CustomNotificationCallback CustomNotif
        {
            add { _cusotmNotif += value; }
            remove { _cusotmNotif -= value; }
        }

        public event CacheBecomeActiveCallback CacheBecomeActive
        {
            add { _cacheBecomeActive += value; }
            remove { _cacheBecomeActive -= value; }
        }

        /// <summary> event for async operations.</summary>
        public event AsyncOperationCompletedCallback AsyncOperationCompleted
        {
            add { _asyncOperationCompleted += value; }
            remove { _asyncOperationCompleted -= value; }
        }

        public event DataSourceUpdatedCallback DataSourceUpdated
        {
            add { _dataSourceUpdated += value; }
            remove { _dataSourceUpdated -= value; }
        }

        public event NodeJoinedCallback MemberJoined
        {
            add { _memberJoined += value;

                if (_memberJoined?.GetInvocationList().Length > 0)
                {
                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.cluster_change_events).UpdateUsageTime();
                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.node_join_event, FeatureEnum.cluster_change_events).UpdateUsageTime();

                }
            }
            remove { _memberJoined -= value; }
        }

        public event NodeLeftCallback MemberLeft
        {
            add { _memberLeft += value;

                if (_memberLeft?.GetInvocationList().Length > 0)
                {
                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.cluster_change_events).UpdateUsageTime();
                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.node_leave_event, FeatureEnum.cluster_change_events).UpdateUsageTime();
                }
            }
            remove { _memberLeft -= value; }
        }

        public event ConfigurationModified ConfigurationModified
        {
            add { _configurationModified += value; }
            remove { _configurationModified -= value; }
        }

        public event CompactTypeModifiedCallback CompactTypeModified
        {
            add { _compactTypeModified += value; }
            remove { _compactTypeModified -= value; }
        }

        public event HashmapChangedCallback HashmapChanged
        {
            add { _hashmapChanged += value; }
            remove { _hashmapChanged -= value; }
        }

        public event OperationModeChangedCallback OperationModeChanged
        {
            add { _operationModeChange += value; }
            remove { _operationModeChange -= value; }
        }

        public event BlockClientActivity BlockActivity
        {
            add { _blockClientActivity += value; }
            remove { _blockClientActivity -= value; }
        }

        public event UnBlockClientActivity UnBlockActivity
        {
            add { _unblockClientActivity += value; }
            remove { _unblockClientActivity -= value; }
        }


        public IDataFormatService SocketServerDataService
        {
            get { return _socketServerDataService; }
            private set { _socketServerDataService = value; }
        }
        

        /// <summary> delegate for Sending pull requests.  </summary>
        internal event PollRequestCallback PollRequestCallbackNotif
        {
            add { _pollRequestNotif += value; }
            remove { _pollRequestNotif -= value; }
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

        protected internal void StartPhase2()
        {
#if SERVER
            if (_context.CacheImpl is ClusterCacheBase)
                ((ClusterCacheBase)_context.CacheImpl).InitializePhase2();
#endif
        }

        /// <summary>
        /// Start the cache functionality.
        /// </summary>
        protected internal virtual void Start(CacheRenderer renderer,
            bool isStartingAsMirror, bool twoPhaseInitialization)
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
        /// <param name="clientInfo"></param>
        public void OnClientDisconnected(string client, string cacheId, Runtime.Caching.ClientInfo clientInfo, long count)
        {
            if (_context.CacheImpl != null)
            {
                lock (_connectedClients.SyncRoot)
                {
                    _connectedClients.Remove(client);
                }

                _context.CacheImpl.ClientDisconnected(client, _inProc, clientInfo);

                if (ServiceConfiguration.LogClientEvents)
                {
                    AppUtil.LogEvent(_cacheserver, "Client \"" + client + "\" has disconnected from " + _cacheInfo.Name,
                        EventLogEntryType.Information, EventCategories.Information, EventID.ClientDisconnected);
                }
                _context.NCacheLog.CriticalInfo("Cache.OnClientDisconnected",
                    "Client \"" + client + "\" has disconnected from cache.  # Connected Clients " + count);

            }
        }

        public void OnClientForceFullyDisconnected(string clientId)
        {
            if (ServiceConfiguration.LogClientEvents)
            {
                AppUtil.LogEvent(_cacheserver,
                    "Client \"" + clientId + "\" has forcefully been disconnected due to scoket dead lock from " +
                    _cacheInfo.Name, EventLogEntryType.Information, EventCategories.Information,
                    EventID.ClientDisconnected);
            }
            _context.NCacheLog.CriticalInfo("Cache.OnClientForceFullyDisconnected",
                "Client \"" + clientId + "\" has disconnected from cache");

        }

        public void OnClientConnected(string client, string cacheId, ClientInfo clientInfo, long count)
        {
            if (_context.CacheImpl != null)
            {
                if (!_connectedClients.Contains(client))
                    lock (_connectedClients.SyncRoot)
                    {
                        _connectedClients.Add(client);
                    }
                _context.CacheImpl.ClientConnected(client, _inProc, clientInfo);
                if (ServiceConfiguration.LogClientEvents)
                {
                    AppUtil.LogEvent(_cacheserver, "Client \"" + client + "\" has connected to " + _cacheInfo.Name,
                        EventLogEntryType.Information, EventCategories.Information, EventID.ClientConnected);
                }
                _context.NCacheLog.CriticalInfo("Cache.OnClientConnected",
                    "Client \"" + client + "\" has connected to cache.  # Connected Clients  " + count);
            }
        }

        /// <summary>
        /// Stop the internal working of the cache.
        /// </summary>
        public virtual void Stop()
        {
            _startShutDown = DateTime.Now;
            Dispose();
        }

        public virtual bool VerifyNodeShutDown()
        {
            return true;
        }

        public List<ShutDownServerInfo> GetShutDownServers()
        {
            return _context.CacheImpl.GetShutDownServers();
        }

#if SERVER
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

                if (!_context.CacheImpl.IsShutdownServer(i.Address))
                {
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

            }

            ///we dont need to reconnect the the selected server if the selected server has same clients
            ///as the server with which the client is currently connected.
            if (localNodeInfo != null && localNodeInfo.ConnectedClients.Count == min)
            {
                ipAddress = connectedIpAddress;
                serverPort = connectedPort;
            }
        }

#endif


        public Dictionary<string, int> GetRunningServers(string ipAddress, int serverPort)
        {
            Dictionary<string, int> runningServers = new Dictionary<string, int>();
            if (this.CacheType.Equals("replicated-server") || this.CacheType.Equals("mirror-server"))
            {
                string connectedIpAddress = ipAddress;
                int connectedPort = serverPort;

#if SERVER

                ArrayList nodes = ((ClusterCacheBase)this._context.CacheImpl)._stats.Nodes;

                foreach (NodeInfo i in nodes)
                {
                    if (!_context.CacheImpl.IsShutdownServer(i.Address))
                    {
                        if (i.RendererAddress != null)
                        {
                            ipAddress = (string)i.RendererAddress.IpAddress.ToString();
                            serverPort = i.RendererAddress.Port;
                            runningServers.Add(ipAddress, serverPort);
                        }
                    }
                }
#else
                 runningServers.Add(ipAddress, serverPort);
#endif

            }
            return runningServers;
        }
        

        public void MakeCacheActiveNCManager(bool makeActive)
        {
            if (makeActive)
            {
                this._context.CacheImpl.CacheBecomeActive();
            }
        }

        internal void GetActiveNode(ref string ipAddress, ref int serverPort, ArrayList nodes)
        {
            string connectedIpAddress = ipAddress;

            NodeInfo localNodeInfo = null;

            foreach (NodeInfo i in nodes)
            {
                if (i.IsActive)
                {
                    if (!_context.CacheImpl.IsShutdownServer(i.Address))
                    {
                        if (i.RendererAddress != null)
                        {
                            ipAddress = (string)i.RendererAddress.IpAddress.ToString();
                            serverPort = i.RendererAddress.Port;
                        }
                        else
                            ipAddress = (string)i.Address.IpAddress.ToString();

                        break;
                    }
                }
            }
        }

#if SERVER
        public void GetActiveServer(ref string ipAddress, ref int serverPort)
        {
            string connectedIpAddress = ipAddress;
            int connectedPort = serverPort;

            if (!this._context.IsClusteredImpl) return;
            if (_inProc) return;

            ArrayList nodes = ((ClusterCacheBase)this._context.CacheImpl)._stats.Nodes;

            foreach (NodeInfo i in nodes)
            {
                if (i.IsActive)
                {
                    if (!_context.CacheImpl.IsShutdownServer(i.Address))
                    {
                        if (i.RendererAddress != null)
                            ipAddress = (string)i.RendererAddress.IpAddress.ToString();
                        else
                            ipAddress = (string)i.Address.IpAddress.ToString();

                        serverPort = connectedPort;
                    }
                }

            }

        }
#endif

        public void NotifyBlockActivityToClients(string uniqueId, string server, long interval, int port)
        {
            _uniqueId = uniqueId;
            _blockserverIP = server;
            _blockinterval = interval;

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.NONE | ShutDownStatus.SHUTDOWN_COMPLETED))
                _shutDownStatusLatch.SetStatusBit(ShutDownStatus.SHUTDOWN_INPROGRESS,
                    ShutDownStatus.NONE | ShutDownStatus.SHUTDOWN_COMPLETED);

            if (this._blockClientActivity == null) return;
            Delegate[] dlgList = this._blockClientActivity.GetInvocationList();

            _context.NCacheLog.CriticalInfo("Cache.NotifyBlockActivityToClients",
                "Notifying " + dlgList.Length + " clients to block activity.");

            foreach (BlockClientActivity subscriber in dlgList)
            {
                try
                {
#if !NETCORE
                    subscriber.BeginInvoke(uniqueId, server, interval, port, null, subscriber);
#elif NETCORE
                    //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                    TaskFactory factory = new TaskFactory();
                    System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(uniqueId, server, interval, port));
#endif
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    _context.NCacheLog.Error("Cache.NotifyBlockActivityToClients", ex.ToString());
                    this._blockClientActivity -= subscriber;
                }
                catch (Exception ex)
                {
                    _context.NCacheLog.Error("Cache.NotifyBlockActivityToClients", ex.ToString());
                }
            }
        }

        public void NotifyUnBlockActivityToClients(string uniqueId, string server, int port)
        {
            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                _shutDownStatusLatch.SetStatusBit(ShutDownStatus.SHUTDOWN_COMPLETED, ShutDownStatus.SHUTDOWN_INPROGRESS);

            if (this._unblockClientActivity == null) return;
            Delegate[] dlgList = this._unblockClientActivity.GetInvocationList();

            _context.NCacheLog.CriticalInfo("Cache.NotifyUnBlockActivityToClients",
                "Notifying " + dlgList.Length + " clients to unblock activity.");

            foreach (UnBlockClientActivity subscriber in dlgList)
            {
                try
                {
#if !NETCORE
                    subscriber.BeginInvoke(uniqueId, server, port, null, subscriber);
#elif NETCORE
                    //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                    TaskFactory factory = new TaskFactory();
                    System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(uniqueId, server, port));
#endif
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    _context.NCacheLog.Error("Cache.NotifyUnBlockActivityToClients", ex.ToString());
                    this._unblockClientActivity -= subscriber;
                }
                catch (Exception ex)
                {
                    _context.NCacheLog.Error("Cache.NotifyUnBlockActivityToClients", ex.ToString());
                }
            }

            _uniqueId = null;
            _blockserverIP = null;
            _blockinterval = 0;
        }

        /// <summary>
        /// shutdown function.
        /// </summary>
        public void ShutDownGraceful()
        {
            int shutdownTimeout = 180;
            int blockTimeout = 3;

            string expMsg = GracefulTimeout.GetGracefulShutDownTimeout(ref shutdownTimeout, ref blockTimeout);
            if (expMsg != null)
                _context.NCacheLog.CriticalInfo("Cache.GracefulShutDown", expMsg);

            BlockInterval = shutdownTimeout;

            try
            {
                _context.NCacheLog.CriticalInfo("Cache.ShutDownGraceful", "Waiting for " + blockTimeout + "seconds...");
                //Wait for activity to get completed...


                string uniqueID = Guid.NewGuid().ToString();

                _context.NCacheLog.CriticalInfo("Cache.ShutDownGraceful",
                    "Notifying Cluster and Clients to block activity.");
                _context.CacheImpl.NotifyBlockActivity(uniqueID, BlockInterval);

                _context.NCacheLog.CriticalInfo("Cache.ShutDownGraceful", "Waiting for Background process completion.");
                Thread.Sleep(blockTimeout * 1000); //wait before activity blocking.

                _context.NCacheLog.CriticalInfo("Cache.ShutDownGraceful", "Starting Windup Tasks.");

                if (_asyncProcessor != null)
                {
                    _asyncProcessor.WindUpTask();
                }

               
                _context.CacheImpl.WindUpReplicatorTask();

                _context.NCacheLog.CriticalInfo("Cache.ShutDownGraceful", "Windup Tasks Ended.");

                
                if (_asyncProcessor != null)
                {
                    if (BlockInterval > 0)
                        _asyncProcessor.WaitForShutDown(BlockInterval);
                }

               
                if (BlockInterval > 0)
                    _context.CacheImpl.WaitForReplicatorTask(BlockInterval);

                _context.CacheImpl.NotifyUnBlockActivity(uniqueID);

            }
            catch (ThreadAbortException ta)
            {
                _context.NCacheLog.Error("Cache.ShutdownGraceful", "Graceful Shutdown have stopped. " + ta.ToString());
            }
            catch (ThreadInterruptedException ti)
            {
                _context.NCacheLog.Error("Cache.ShutdownGraceful", "Graceful Shutdown have stopped. " + ti.ToString());
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.GracefulShutDown", "Graceful Shutdown have stopped. " + e.ToString());
            }
            finally
            {
                lock (_shutdownMutex)
                {
                    Monitor.PulseAll(_shutdownMutex);
                }
            }


        }

        private void CompressionThresholdSize(IDictionary properties)
        {
            if (properties.Contains("threshold"))
            {
                try
                {
                    _compressionThresholdSize = Convert.ToInt64(properties["threshold"]);
                    _compressionEnabled = Convert.ToBoolean(properties["enabled"]);
                }
                catch (Exception)
                {
                    _compressionThresholdSize = COMPRESSIONTHRESHOLD;
                    _compressionEnabled = true;
                }
            }

            else
            {
                _compressionThresholdSize = COMPRESSIONTHRESHOLD;
                _compressionEnabled = true;
            }
        }


        internal string GetDefaultReadThruProvider()
        {
            string defaultProvider = null;

            Provider[] providers = _cacheInfo.Configuration.BackingSource.Readthru.Providers;
            if (providers != null)
            {
                foreach (Provider pro in providers)
                {
                    if (pro.IsDefaultProvider)
                        defaultProvider = pro.ProviderName;
                }
            }
            else
                throw new ConfigurationException("Please configure at least one read-thru provider");

            return defaultProvider;
        }

        internal string GetDefaultWriteThruProvider()
        {
            string defaultProvider = null;

            Provider[] providers = _cacheInfo.Configuration.BackingSource.Writethru.Providers;
            if (providers != null)
            {
                foreach (Provider pro in providers)
                {
                    if (pro.IsDefaultProvider)
                        defaultProvider = pro.ProviderName;
                }
            }
            else
                throw new ConfigurationException("Please configure at least one write-thru provider");

            return defaultProvider;
        }

        internal void Initialize(IDictionary properties, bool inProc)
        {
            Initialize(properties, inProc, false, false);
        }

        internal void Initialize(IDictionary properties, bool inProc,
            bool isStartingAsMirror, bool twoPhaseInitialization)
        {
            if (_context._cmptKnownTypesforJava == null) _context._cmptKnownTypesforJava = new Hashtable(new EqualityComparer());
            if (_context._cmptKnownTypesforNet == null) _context._cmptKnownTypesforNet = new Hashtable(new EqualityComparer());

            //Just to initialze the HP time
            HPTime time = HPTime.Now;
            for (int i = 1; i < 1000; i++) time = HPTime.Now;
            _inProc = inProc;
            _context.InProc = inProc;
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                
                lock (this)
                {
                    if (!properties.Contains("cache"))
                        throw new ConfigurationException("Missing configuration attribute 'cache'");

                    #region ------------------------------------------- [Initializing PoolManager] -------------------------------------------

                    var createFakePools = false;

                    if (_cacheInfo == null || _cacheInfo.Configuration == null)
                    {
                        createFakePools = true;
                    }
                    else
                    {
                        if (_cacheInfo.Configuration.CacheType.Equals("local-cache") || _cacheInfo.Configuration.CacheType.Equals("client-cache"))
                        {
                            createFakePools = true;
                        }
                        else
                        {
                            switch (_cacheInfo.Configuration.Cluster?.Topology)
                            {
                                case "mirror-server":
                                case "replicated-server":
                                    createFakePools = true;
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    
                    InitializeObjectPools(createFakePools);
                    InitializeOldPoolManager(createFakePools);

                    #endregion

                    IDictionary cacheConfig = (IDictionary)properties["cache"];

                    #region ---------------------------------------- [Fastening Data Format of Cache] ----------------------------------------



                    #region New Code - 2018.05.28_1044
                    if (cacheConfig.Contains("data-format"))
                    {
                        if (_cacheInfo.Configuration != null && _cacheInfo.Configuration.CacheType.CompareTo("client-cache") == 0)
                        {
                            if (((string)cacheConfig["data-format"]).ToLower().Equals("object"))
                            {
                                SocketServerDataService = new ObjectDataFormatService(_context);
                                _context.CachingSubSystemDataService = CachingSubSystemDataService = new BinaryDataFormatService(_context);
                                _context.InMemoryDataFormat = DataFormat.Object;
                            }
                            else
                            {
                                SocketServerDataService = new BinaryDataFormatService(_context);
                                _context.CachingSubSystemDataService = CachingSubSystemDataService = new ObjectDataFormatService(_context);
                                _context.InMemoryDataFormat = DataFormat.Binary;
                            }
                        }
                        else
                        {
                            if (inProc)
                            {
                                SocketServerDataService = new ObjectDataFormatService(_context);
                                _context.CachingSubSystemDataService = CachingSubSystemDataService = new BinaryDataFormatService(_context);
                                _context.InMemoryDataFormat = DataFormat.Object;
                            }
                            else
                            {
                                SocketServerDataService = new BinaryDataFormatService(_context);
                                _context.CachingSubSystemDataService = CachingSubSystemDataService = new ObjectDataFormatService(_context);
                                _context.InMemoryDataFormat = DataFormat.Binary;
                            }
                        }

                    }

                    #endregion

                    #endregion

                    if (cacheConfig.Contains(SerializationUtility.SerializationConfigAttribute))
                    {
                        SerializationFormat format;
                        if (Enum.TryParse(cacheConfig[SerializationUtility.SerializationConfigAttribute].ToString(), true, out format))
                            _context.SerializationFormat = format;
                        switch (format)
                        {
                            case SerializationFormat.Binary:
                                FeatureUsageCollector.Instance.GetFeature(FeatureEnum.binary_serialization).UpdateUsageTime();
                                break;
                            case SerializationFormat.Json:
                                FeatureUsageCollector.Instance.GetFeature(FeatureEnum.json_serialization).UpdateUsageTime();
                                break;
                            default:
                                break;
                        }


                    }

                    if (properties["server-end-point"] != null)
                        cacheConfig.Add("server-end-point", (IDictionary)properties["server-end-point"]);


                    if (cacheConfig.Contains("name"))
                        _cacheInfo.Name = Convert.ToString(cacheConfig["name"]).Trim();

                    _cacheInfo.CurrentPartitionId = GetCurrentPartitionId(_cacheInfo.Name, cacheConfig);

                    if (cacheConfig.Contains("log"))
                    {
                        _context.NCacheLog = new NCacheLogger();
                        _context.NCacheLog.Initialize(cacheConfig["log"] as IDictionary, _cacheInfo.CurrentPartitionId,
                            _cacheInfo.Name, isStartingAsMirror, inProc);

                    }
                    else
                    {
                        _context.NCacheLog = new NCacheLogger();
                        _context.NCacheLog.Initialize(null, _cacheInfo.CurrentPartitionId, _cacheInfo.Name);
                    }

                    LogThreadInfo();

                    SerializationUtil.NCacheLog = _context.NCacheLog;

                    if (cacheConfig.Contains("compression"))
                    {
                        CompressionThresholdSize(cacheConfig["compression"] as IDictionary);
                        {
                            _context.CompressionEnabled = this._compressionEnabled;
                            _context.CompressionThreshold = this._compressionThresholdSize;

                            if (_context.CompressionEnabled)
                                FeatureUsageCollector.Instance.GetFeature(FeatureEnum.compression).UpdateUsageTime();
                        }
                    }
                    _context.SerializationContext = _cacheInfo.Name;

                    _context.TimeSched = new TimeScheduler();
                    _context.AsyncProc = new AsyncProcessor(_context.NCacheLog);
                    _asyncProcessor = new AsyncProcessor(_context.NCacheLog);
                    _context.MessageManager = new MessageManager(_context);

                    if (!inProc)
                    {
                        if (_cacheInfo.CurrentPartitionId != string.Empty)
                        {
                            if (ServiceConfiguration.PublishCountersToCacheHost)
                                _context.PerfStatsColl = new CustomStatsCollector(Name + "-" + _cacheInfo.CurrentPartitionId,
                                inProc);
                            else
                                _context.PerfStatsColl = new PerfStatsCollector(Name + "-" + _cacheInfo.CurrentPartitionId,
                                    inProc);
                        }
                        else
                        {
                            if (ServiceConfiguration.PublishCountersToCacheHost)
                                _context.PerfStatsColl = new CustomStatsCollector(Name, inProc);
                            else
                                _context.PerfStatsColl = new PerfStatsCollector(Name, inProc);
                        }

                        _context.PerfStatsColl.NCacheLog = _context.NCacheLog;

                    }
                    else
                    {
#if !NETCORE
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
#endif

                        if (isStartingAsMirror) // this is a special case for Partitioned Mirror Topology.
                        {
                            if (ServiceConfiguration.PublishCountersToCacheHost)
                                _context.PerfStatsColl = new CustomStatsCollector(Name + "-" + "replica", false);
                            else
                                _context.PerfStatsColl = new PerfStatsCollector(Name + "-" + "replica", false);
                        }
                        else
                        {
#if !NETCORE
                            if (ServiceConfiguration.PublishCountersToCacheHost)
                                _context.PerfStatsColl = new CustomStatsCollector(Name, port, inProc);
                            else
                                _context.PerfStatsColl = new PerfStatsCollector(Name, port, inProc);
#elif NETCORE
                           
                            if (ServiceConfiguration.PublishCountersToCacheHost)
                                _context.PerfStatsColl = new CustomStatsCollector(Name, inProc);
                            else
                                _context.PerfStatsColl = new PerfStatsCollector(Name, inProc); 
#endif

                            _context.PerfStatsColl.NCacheLog = _context.NCacheLog;
                        }

                    }

                    _context.IsStartedAsMirror = isStartingAsMirror;



                    MiscUtil.RegisterCompactTypes(_context.TransactionalPoolManager);
                    SetCacheTopology();

                    CreateInternalCache(cacheConfig, isStartingAsMirror, twoPhaseInitialization);

                    //setting cache Impl instance

                    if (cacheConfig.Contains("client-death-detection") || cacheConfig.Contains("client-activity-notification"))
                        _context.ConnectedClients = new ConnectedClientsLedger();

                    if (cacheConfig.Contains("client-death-detection"))
                    {
                        IDictionary deathDetection = cacheConfig["client-death-detection"] as IDictionary;
                        if (deathDetection.Contains("enable"))
                        {
                            _deathDetectionEnabled = Convert.ToBoolean(deathDetection["enable"]);
                            _graceTime = Convert.ToInt32(deathDetection["grace-interval"]);
                            if (_deathDetectionEnabled)
                            {
                                ConnectedClientsLedger.NotificationSpecification specification = new ConnectedClientsLedger
                                    .NotificationSpecification
                                {
                                    Callback = _context.CacheImpl.DeclareDeadClients,
                                    Period = _graceTime
                                };

                                _context.ConnectedClients.AddClientDeathDetectionSpecification(specification);

                            }
                        }
                    }
                    if (cacheConfig.Contains("client-activity-notification"))
                    {
                        IDictionary clientNotification = cacheConfig["client-activity-notification"] as IDictionary;
                        if (clientNotification.Contains("enabled"))
                        {
                            int retention = Convert.ToInt32(clientNotification["retention-period"]);
                            ConnectedClientsLedger.NotificationSpecification specification = new ConnectedClientsLedger
                                    .NotificationSpecification
                            {
                                Callback = _context.CacheImpl.HandleDeadClientsNotification,
                                Period = retention
                            };
                            _context.ConnectedClients.AddClientDeathNotificationSpecification(specification);
                            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.client_connectivity_notification).UpdateUsageTime();
                        }
                    }

               

                    if (inProc && _context.CacheImpl != null)
                    {
                        //we keep serialized user objects in case of local inproc caches...
                        _context.CacheImpl.KeepDeflattedValues = false;
                    }

                    // we bother about perf stats only if the user has read/write rights over counters.
                    _context.PerfStatsColl.IncrementCountStats(CacheHelper.GetLocalCount(_context.CacheImpl));

                    _cacheInfo.ConfigString = ConfigHelper.CreatePropertyString(properties);
#if SERVER
                    if (!(_context.CacheImpl is ClusterCacheBase))
#endif
                    {

                        
                    }
                }
                _context.CacheImpl.Parent = this;

                // StringPool triming task is added to TimeScheduler
                if (_stringPoolTrimmingTask == null)
                {
                    _stringPoolTrimmingTask = new StringPoolTrimmingTask(_context);
                    _context.TimeSched.AddTask(_stringPoolTrimmingTask);
                }


                if (_cacheInfo != null && _cacheInfo.Configuration != null && !_cacheInfo.Configuration.InProc)
                    StartMonitoring();

                _context.NCacheLog.CriticalInfo("Cache.Initialize", "'" + _context.CacheRoot.Name + "' is started successfully.");
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

        void StartMonitoring ()
        {
            try
            {
                if (_context != null)
                {
                    CacheHealthAlertReader reader = new CacheHealthAlertReader();
                    _context.HealthAlerts = reader.LoadConfiguration();
                }

                if (_context.HealthAlerts != null && _context.HealthAlerts.Enabled && !_context.IsStartedAsMirror)
                {
                    healthMonitor = new HealthMonitor(_context);
                    healthMonitor.InitializeThread();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void SetCacheTopology()
        {
            if (_cacheInfo.ClassName.Equals("mirror-server"))
                _context.CacheTopology = CacheTopology.Mirror;
            else if (_cacheInfo.ClassName.Equals("local-cache"))
                _context.CacheTopology = CacheTopology.Local;

        }

        private void InitializeObjectPools(bool createFakePool)
        {
            //Store pool is intended to be used at store level ie. InternalCache
            //Objects created on store pool can't leave InternalCache boundry. 
            _context.StorePoolManager = new PoolManager(createFakePool);
            InitializeStoreObjectPools(_context.StorePoolManager);

            //Transactional pool is intended to be used for transactions ie. running oprations. This pool is of fixed size.
            //Objects alloated on this pool must be returned to the pool at the end of transaction
            _context.TransactionalPoolManager = new TransactionalPoolManager(createFakePool);
            InitializeTransactionalObjectPools(_context.TransactionalPoolManager,ServiceConfiguration.TransactionalPoolCapacity);

            //Fake pool is intended to be used when current operation is coming from cluster for e.g. in handleGet()
            _context.FakeObjectPool = new PoolManager(true);
            InitializeTransactionalObjectPools(_context.FakeObjectPool, ServiceConfiguration.TransactionalPoolCapacity);
        }

        private void InitializeStoreObjectPools(PoolManager poolManager)
        {
            poolManager.CreatePool<byte>(Common.Pooling.ArrayPoolType.Byte,true);

            poolManager.CreatePool(
                     Common.Pooling.ObjectPoolType.BitSet, new Common.Pooling.PoolingOptions<BitSet>(
                          new BitSetInstantiator()
                     )
                 );
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.CacheEntry, new Common.Pooling.PoolingOptions<CacheEntry>(
                     new CacheEntryInstantiator()
                )
            );
          
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.IdleExpiration, new Common.Pooling.PoolingOptions<IdleExpiration>(
                    new IdleExpirationInstantiator()
                )
            );
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.FixedExpiration, new Common.Pooling.PoolingOptions<FixedExpiration>(
                    new FixedExpirationInstantiator()
                )
            );
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.FixedIdleExpiration, new Common.Pooling.PoolingOptions<FixedIdleExpiration>(
                    new FixedIdleExpirationInstantiator()
                )
            );
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.NodeExpiration, new Common.Pooling.PoolingOptions<NodeExpiration>(
                    new NodeExpirationInstantiator()
                )
            );
           
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.LargeUserBinaryObject, new Common.Pooling.PoolingOptions<LargeUserBinaryObject>(
                    new LargeUserBinaryObjectInstantiator()
                )
            );
          
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.SmallUserBinaryObject, new Common.Pooling.PoolingOptions<SmallUserBinaryObject>(
                    new SmallUserBinaryObjectInstatiator()
                )
            );
           
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.TTLExpiration, new Common.Pooling.PoolingOptions<TTLExpiration>(
                    new TTLExpirationInstantiator()
                )
            );
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.CounterHint, new Common.Pooling.PoolingOptions<CounterHint>(
                    new CounterHintInstantiator()
                )
            );
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.TimestampHint, new Common.Pooling.PoolingOptions<TimestampHint>(
                    new TimestampHintInstantiator()
                )
            );
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.PriorityEvictionHint, new Common.Pooling.PoolingOptions<PriorityEvictionHint>(
                    new PriorityEvictionHintInstantiator()
                )
            );
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.Notifications, new Common.Pooling.PoolingOptions<Notifications>(
                    new NotificationsInstantiator()
                )
            );
           
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.GroupInfo, new Common.Pooling.PoolingOptions<GroupInfo>(
                    new GroupInfoInstantiator()
                )
            );
          
            poolManager.CreatePool(
                Common.Pooling.ObjectPoolType.AggregateExpirationHint, new Common.Pooling.PoolingOptions<AggregateExpirationHint>(
                    new AggregateExpirationHintInstantiator()
                )
            );

        }

        private void InitializeTransactionalObjectPools(PoolManager poolManager, int poolCapacity)
        {
            poolManager.CreateSimplePool(
                     Common.Pooling.ObjectPoolType.CompressedValueEntry, new Common.Pooling.PoolingOptions<CompressedValueEntry>(
                          new CompressedValueEntryInstantiator(), poolCapacity
                     )
                 );

            poolManager.CreateSimplePool(
                     Common.Pooling.ObjectPoolType.BitSet, new Common.Pooling.PoolingOptions<BitSet>(
                          new BitSetInstantiator(),poolCapacity
                     )
                 );
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.CacheEntry, new Common.Pooling.PoolingOptions<CacheEntry>(
                     new CacheEntryInstantiator(), poolCapacity
                )
            );
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.IdleExpiration, new Common.Pooling.PoolingOptions<IdleExpiration>(
                    new IdleExpirationInstantiator(), poolCapacity
                )
            );
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.FixedExpiration, new Common.Pooling.PoolingOptions<FixedExpiration>(
                    new FixedExpirationInstantiator(), poolCapacity
                )
            );
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.FixedIdleExpiration, new Common.Pooling.PoolingOptions<FixedIdleExpiration>(
                    new FixedIdleExpirationInstantiator(), poolCapacity
                )
            );
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.NodeExpiration, new Common.Pooling.PoolingOptions<NodeExpiration>(
                    new NodeExpirationInstantiator(), poolCapacity
                )
            );
   
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.LargeUserBinaryObject, new Common.Pooling.PoolingOptions<LargeUserBinaryObject>(
                    new LargeUserBinaryObjectInstantiator(), poolCapacity
                )
            );
     
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.SmallUserBinaryObject, new Common.Pooling.PoolingOptions<SmallUserBinaryObject>(
                    new SmallUserBinaryObjectInstatiator(), poolCapacity
                )
            );
            
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.TTLExpiration, new Common.Pooling.PoolingOptions<TTLExpiration>(
                    new TTLExpirationInstantiator(), poolCapacity
                )
            );
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.CounterHint, new Common.Pooling.PoolingOptions<CounterHint>(
                    new CounterHintInstantiator(), poolCapacity
                )
            );
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.TimestampHint, new Common.Pooling.PoolingOptions<TimestampHint>(
                    new TimestampHintInstantiator(), poolCapacity
                )
            );
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.PriorityEvictionHint, new Common.Pooling.PoolingOptions<PriorityEvictionHint>(
                    new PriorityEvictionHintInstantiator(), poolCapacity
                )
            );
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.Notifications, new Common.Pooling.PoolingOptions<Notifications>(
                    new NotificationsInstantiator(), poolCapacity
                )
            );
          
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.GroupInfo, new Common.Pooling.PoolingOptions<GroupInfo>(
                    new GroupInfoInstantiator(), poolCapacity
                )
            );
           
            poolManager.CreateSimplePool(
                Common.Pooling.ObjectPoolType.AggregateExpirationHint, new Common.Pooling.PoolingOptions<AggregateExpirationHint>(
                    new AggregateExpirationHintInstantiator(), poolCapacity
                )
            );
            poolManager.CreateSimplePool(
               Common.Pooling.ObjectPoolType.CacheInsResultWithEntry, new Common.Pooling.PoolingOptions<CacheInsResultWithEntry>(
                    new CacheInsResultWithEntryInstantiator(), poolCapacity
               )
           );

            poolManager.CreateSimplePool(
                       Common.Pooling.ObjectPoolType.OperationContext, new Common.Pooling.PoolingOptions<OperationContext>(
                           new OperationContextInstantiator(), poolCapacity
                       )
                   );

            poolManager.CreatePool<byte>(Common.Pooling.ArrayPoolType.Byte, false);
        }

        private void InitializeOldPoolManager(bool hardCreateFakePools)
        {
           
        }

    

     
        
        
        
        private void LogThreadInfo()
        {
            int maxworkerThreads = 0;
            int maxcompletionPortThreads = 0;
            int minworkerThreads = 0;
            int mincompletionPortThreads = 0;
            System.Threading.ThreadPool.GetMaxThreads(out maxworkerThreads, out maxcompletionPortThreads);
            System.Threading.ThreadPool.GetMinThreads(out minworkerThreads, out mincompletionPortThreads);
            _context.NCacheLog.CriticalInfo("CacheInfo ", "Process Id : " + Process.GetCurrentProcess().Id + "; Max Worker Threads : " + maxworkerThreads + " ; Max Completion Port Threads : " + maxcompletionPortThreads
                + " ; Min Worker Threads : " + minworkerThreads + " ; Min Completion Port Threads : " + mincompletionPortThreads + " ; IsServerGC: " + GCSettings.IsServerGC);

        }

        private void FilterOutDotNetTypes(Hashtable cmptKnownTypes, Hashtable cmptKnownTypesdotNet, Hashtable cmptKnownTypesJava, bool compact)
        {
            if (cmptKnownTypes != null)
            {
                IDictionaryEnumerator ide = cmptKnownTypes.GetEnumerator();

                if (!compact)
                {
                    while (ide.MoveNext())
                    {
                        Hashtable compactType = (Hashtable)ide.Value;
                        Hashtable classes = (Hashtable)compactType["known-classes"];

                        if (classes == null) continue;

                        IDictionaryEnumerator ide2 = classes.GetEnumerator();

                        while (ide2.MoveNext())
                        {
                            Hashtable typeInfo = (Hashtable)ide2.Value;

                            if (typeInfo["type"].ToString().ToLower().Equals("java"))
                            {
                                if (!cmptKnownTypesJava.Contains((string)ide.Key))
                                    cmptKnownTypesJava.Add((string)ide.Key, new Hashtable(new EqualityComparer()));

                                ((Hashtable)cmptKnownTypesJava[((string)ide.Key)]).Add((string)typeInfo["name"], typeInfo);

                                if (!typeInfo.Contains("portable"))
                                {
                                    typeInfo.Add("portable", compactType["portable"]);
                                }
                            }
                            else if (typeInfo["type"].ToString().ToLower().Equals("net"))
                            {
                                if (!cmptKnownTypesdotNet.Contains((string)ide.Key))
                                    cmptKnownTypesdotNet.Add((string)ide.Key, new Hashtable(new EqualityComparer()));

                                ((Hashtable)cmptKnownTypesdotNet[((String)ide.Key)]).Add((String)typeInfo["name"], typeInfo);

                                if (!typeInfo.Contains("portable"))
                                {
                                    typeInfo.Add("portable", compactType["portable"]);
                                }
                            }
                        }

                        if (Convert.ToBoolean(compactType["portable"]))
                        {
                            if (cmptKnownTypesJava.Count > 0 && (Hashtable)cmptKnownTypesJava[((string)ide.Key)] != null)
                            {
                                ((Hashtable)cmptKnownTypesJava[((string)ide.Key)]).Add((string)"Alachisoft.NCache.AttributeUnion", compactType["attribute-union-list"]);
                            }
                            if (cmptKnownTypesdotNet.Count > 0 && (Hashtable)cmptKnownTypesdotNet[((string)ide.Key)] != null)
                            {
                                ((Hashtable)cmptKnownTypesdotNet[((string)ide.Key)]).Add((string)"Alachisoft.NCache.AttributeUnion", compactType["attribute-union-list"]);
                            }
                        }
                    }
                }
                else
                {
                    while (ide.MoveNext())
                    {
                        Hashtable compactType = (Hashtable)ide.Value;

                        if (compactType["type"] != null)
                        {
                            if (compactType["type"].ToString().ToLower().Equals("java"))
                            {
                                if (!cmptKnownTypesJava.Contains((string)ide.Key))
                                    cmptKnownTypesJava.Add((string)ide.Key, new Hashtable(new EqualityComparer()));

                                ((Hashtable)cmptKnownTypesJava[((string)ide.Key)]).Add((string)compactType["name"], compactType);
                            }
                            else if (compactType["type"].ToString().ToLower().Equals("net"))
                            {
                                if (compactType.Contains("arg-types"))
                                    compactType["arg-types"] = FilterOutNestedGenerics((Hashtable)compactType["arg-types"]);

                                if (!cmptKnownTypesdotNet.Contains((string)ide.Key))
                                    cmptKnownTypesdotNet.Add((string)ide.Key, new Hashtable(new EqualityComparer()));

                                ((Hashtable)cmptKnownTypesdotNet[((string)ide.Key)]).Add((string)compactType["name"], compactType);
                            }
                        }
                    }
                }

            }
        }

        private Hashtable FilterOutNestedGenerics(Hashtable htArgTypes)
        {
            Hashtable htArgTypes2 = null;
            if (htArgTypes != null && htArgTypes.Count > 0)
            {
                htArgTypes2 = new Hashtable(new EqualityComparer());
                IDictionaryEnumerator ide11 = htArgTypes.GetEnumerator();
                while (ide11.MoveNext())
                {
                    Hashtable innerGenericType = new Hashtable(new EqualityComparer());
                    htArgTypes2.Add(ide11.Key.ToString(), innerGenericType);
                    Hashtable argInstances = (Hashtable)ide11.Value;
                    IDictionaryEnumerator ide12 = argInstances.GetEnumerator();
                    while (ide12.MoveNext())
                    {
                        Hashtable instanceArgType = (Hashtable)ide12.Value;
                        if (instanceArgType.Contains("arg-types"))
                        {
                            instanceArgType["arg-types"] =
                                FilterOutNestedGenerics((Hashtable)instanceArgType["arg-types"]);
                        }
                        Hashtable innerGenericTypeDetail = new Hashtable(new EqualityComparer());
                        innerGenericTypeDetail.Add(instanceArgType["name"].ToString(), instanceArgType);
                        innerGenericType.Add(ide12.Key.ToString(), innerGenericTypeDetail);
                    }
                }
            }
            return htArgTypes2;
        }

        internal void Initialize2(IDictionary properties, bool inProc, string userId, string password)
        {
            if (_context._cmptKnownTypesforJava == null) _context._cmptKnownTypesforJava = new Hashtable();
            if (_context._cmptKnownTypesforNet == null) _context._cmptKnownTypesforNet = new Hashtable();

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
                    if (properties.Contains("name"))
                        _cacheInfo.Name = Convert.ToString(properties["name"]).Trim();

                    if (properties.Contains("log"))
                    {
                        _context.NCacheLog = new NCacheLogger();
                        _context.NCacheLog.Initialize(properties["log"] as IDictionary, _cacheInfo.CurrentPartitionId,
                            _cacheInfo.Name);


                    }
                    else
                    {
                        _context.NCacheLog = new NCacheLogger();
                        _context.NCacheLog.Initialize(null, _cacheInfo.CurrentPartitionId, _cacheInfo.Name);

                    }

                    _context.SerializationContext = _cacheInfo.Name;

                    _context.TimeSched = new TimeScheduler();
                    _context.AsyncProc = new AsyncProcessor(_context.NCacheLog);
                    _asyncProcessor = new AsyncProcessor(_context.NCacheLog);
             

                    if (!inProc)
                    {

                        if (ServiceConfiguration.PublishCountersToCacheHost)
                            _context.PerfStatsColl = new CustomStatsCollector(Name, inProc);
                        else
                            _context.PerfStatsColl = new PerfStatsCollector(Name, inProc);
                        _context.PerfStatsColl.NCacheLog = _context.NCacheLog;

                    }
                    else
                    {
#if !NETCORE
                        _channels = new RemotingChannels();
                        _channels.RegisterTcpChannels("InProc", 0);
                        RemotingServices.Marshal(this, _cacheInfo.Name);

                        //get the port number selected.
                        TcpServerChannel serverChannel = (TcpServerChannel)_channels.TcpServerChannel;
                        string uri = serverChannel.GetChannelUri();
                        int port = Convert.ToInt32(uri.Substring(uri.LastIndexOf(":") + 1));

                        if (ServiceConfiguration.PublishCountersToCacheHost)
                            _context.PerfStatsColl = new CustomStatsCollector(Name, port, inProc);
                        else
                            _context.PerfStatsColl = new PerfStatsCollector(Name, port, inProc);

#elif NETCORE
                        
                            if (ServiceConfiguration.PublishCountersToCacheHost)
                                _context.PerfStatsColl = new CustomStatsCollector(Name, inProc);
                            else
                                _context.PerfStatsColl = new PerfStatsCollector(Name, inProc); 
#endif
                        _context.PerfStatsColl.NCacheLog = _context.NCacheLog;

                    }

                    CreateInternalCache2(properties, userId, password);

                    // we bother about perf stats only if the user has read/write rights over counters.
                    _context.PerfStatsColl.IncrementCountStats(CacheHelper.GetLocalCount(_context.CacheImpl));


                    _cacheInfo.ConfigString = ConfigHelper.CreatePropertyString(properties);
                }
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
                        if (type == "partitioned-replicas-server")
                        {
                            if (config.Contains("cluster"))
                            {
                                config = config["cluster"] as Hashtable;
                                if (config.Contains("sub-group-id"))
                                    return config["sub-group-id"] as string;
                            }
                        }
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Initializes the Compact Serializatoin Framework for UserDefined types.
        /// </summary>
        /// <param name="properties"></param>
        internal void InitializeCompactFramework(Hashtable properties, bool throwExceptions)
        {

            Hashtable framework = new Hashtable(new EqualityComparer());

            try
            {
                framework = SerializationUtil.GetCompactTypes(properties, throwExceptions, _cacheInfo.Name);
            }
            catch (NullReferenceException)
            {
            }
            catch (Exception exc)
            {
                _context.NCacheLog.Error("Cache.InitializeCompactFramework", exc.Message);
            }
            //Register the User defined types to the Compact Framework.
            IDictionaryEnumerator ide3 = framework.GetEnumerator();
            while (ide3.MoveNext())
            {
                // Code change for handling Non Compact Fields in Compact Serilization context
                Hashtable handleNonCompactFields = (Hashtable)ide3.Value;
                Hashtable nonCompactFields = null;

                if (handleNonCompactFields.Contains("non-compact-fields"))
                    nonCompactFields = (Hashtable)handleNonCompactFields["non-compact-fields"];

                short typeHandle = (short)handleNonCompactFields["handle"];
                try
                {
                    CompactFormatterServices.RegisterCustomCompactType((System.Type)ide3.Key, (short)typeHandle, _cacheInfo.Name.ToLower(),
                        SerializationUtil.GetSubTypeHandle(this.Name, ((short)typeHandle).ToString(), (System.Type)ide3.Key),
                        SerializationUtil.GetAttributeOrder(this.Name),
                        SerializationUtil.GetPortibilaty((short)typeHandle, this.Name), nonCompactFields);

                    //Also register array type for custom types.
                    typeHandle += SerializationUtil.UserdefinedArrayTypeHandle;     //Same handle is used at client side.
                    System.Type arrayType = ((System.Type)ide3.Key).MakeArrayType();

                    CompactFormatterServices.RegisterCustomCompactType(arrayType, (short)typeHandle, _cacheInfo.Name.ToLower(),
                       SerializationUtil.GetSubTypeHandle(this.Name, ((short)typeHandle).ToString(), arrayType),
                       SerializationUtil.GetAttributeOrder(this.Name),
                       SerializationUtil.GetPortibilaty((short)typeHandle, this.Name), nonCompactFields);
                }
                catch (Exception exc)
                {
                    if (exc is TypeLoadException || exc is FileNotFoundException)
                        _context.NCacheLog.Error("Cache.InitializeCompactFramework", exc.Message);
                    else
                        throw exc;
                }
            }
        }

        private void  CreateInternalCache(IDictionary properties, bool isStartingAsMirror,
            bool twoPhaseInitialization)
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
                    throw new ConfigurationException(
                        "Can not find the type of cache, invalid configuration for cache class '" + cacheScheme + "'");

                if (Name.Length < 1)
                    _cacheInfo.Name = cacheScheme;

                //Initialize the performance counters, if enabled.
                bool bEnableCounter = true;

                if (properties.Contains("perf-counters"))
                    bEnableCounter = Convert.ToBoolean(properties["perf-counters"]);

                if (bEnableCounter)
                {
                    _context.PerfStatsColl.InitializePerfCounters((isStartingAsMirror
                        ? !isStartingAsMirror
                        : this._inProc));
                }

                 
                    _context.ExpiryMgr = new ExpirationManager(schemeProps, _context);

                    // For the removal of Cascaded Dependencies on Clean Interval.
                    _context.ExpiryMgr.TopLevelCache = this;

                    _cacheInfo.ClassName = Convert.ToString(schemeProps["type"]).ToLower();
                    _context.AsyncProc.Start();
                    _asyncProcessor.Start();




#if !CLIENT && !DEVELOPMENT
                if (_cacheInfo.ClassName.CompareTo("mirror-server") == 0)
                {
                    _context.CacheImpl = new MirrorCache(cacheClasses, schemeProps, this, _context, this);
                    _context.CacheImpl.Initialize(cacheClasses, schemeProps, twoPhaseInitialization);
                    _context.CacheTopology = CacheTopology.Mirror;
                }

                else
#endif
                    if (_cacheInfo.ClassName.CompareTo("local-cache") == 0)
                    {
                        LocalCacheImpl cache = new LocalCacheImpl(_context);
                        _context.CacheTopology = CacheTopology.Local;
                         cache.Internal = CacheBase.Synchronized(new LocalCache(cacheClasses, cache, schemeProps, this, _context));

                        _context.CacheImpl = cache;

                        if (_context.MessageManager != null) _context.MessageManager.StartMessageProcessing();
                    }
                    else
                    {
                        throw new ConfigurationException("Specified cache class '" + _cacheInfo.ClassName + "' is not available in this edition of " + _cacheserver + ".");
                    }
                    _cacheType = _cacheInfo.ClassName;
                    _isClustered = _cacheInfo.IsClusteredCache;
                    // Start the expiration manager if the cache was created sucessfully!
                    if (_context.CacheImpl != null)
                    {
                        /// there is no need to do expirations on the Async replica's; 
                        /// Expired items are removed fromreplica by the respective active partition.
                        if (!isStartingAsMirror)
                        {
                            _context.ExpiryMgr.Start();
                            _context.MessageManager.SetExpirationInterval(_context.ExpiryMgr.CleanInterval);
                        }
                        if (bEnableCounter)
                        {
#if SERVER
                            if (_context.CacheImpl is ClusterCacheBase)
                            {
                                ((ClusterCacheBase)_context.CacheImpl).InitializeClusterPerformanceCounter(_context.PerfStatsColl.InstanceName);
                            }
#endif
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

        

        private void CreateInternalCache2(IDictionary properties, string userId, string password)
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

                //there is no licensing in the express edtion but we still wants
                //to provide replicated cluster.

#if !NETCORE
    bool isClusterable = true;
#elif NETCORE
                bool isClusterable = false;
#endif
                if (bEnableCounter)
                {
                    _context.PerfStatsColl.InitializePerfCounters(this._inProc);
                }

                _context.ExpiryMgr = new ExpirationManager(properties, _context);
                //For the removal of Cascaded Dependencies on Clean Interval.
                _context.ExpiryMgr.TopLevelCache = this;

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
                _asyncProcessor.Start();


                // We should use an InternalCacheFactory for the code below
                if (_cacheInfo.ClassName.CompareTo("local") == 0)
                {
                    LocalCacheImpl cache = new LocalCacheImpl(_context);
                    cache.Internal = CacheBase.Synchronized(new LocalCache(properties, cache, properties, this, _context));

                    _context.CacheImpl = cache;
                }
                else
                {
                    throw new ConfigurationException("Specified cache class '" + _cacheInfo.ClassName +
                                                     "' is not available in this edition of " + _cacheserver + ".");
                }

                _cacheType = _cacheInfo.ClassName;

                // Start the expiration manager if the cache was created sucessfully!
                if (_context.CacheImpl != null)
                {
                    _context.ExpiryMgr.Start();
                    _context.MessageManager.SetExpirationInterval(_context.ExpiryMgr.CleanInterval);
                    if (bEnableCounter)
                    {
#if SERVER
                        if (_context.CacheImpl is ClusterCacheBase)
                        {
                            ((ClusterCacheBase)_context.CacheImpl).InitializeClusterPerformanceCounter(
                                _context.PerfStatsColl.InstanceName);
                        }
#endif

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
#if !NETCORE
            ILease lease = (ILease)base.InitializeLifetimeService();
            if (lease != null && _sponsor != null)
                lease.Register(_sponsor);
            return lease;
#elif NETCORE
            throw new NotImplementedException();
#endif
        }

#region /       RemoteObjectSponsor      /

#if !NETCORE
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
#endif
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
                {
                    copyArray[i] = array[i];
                    copyArray[i].MarkInUse(NCModulesConstants.Global);
                }
                else
                    break;
            }
            array = copyArray;
        }

#region	/                 --- Clear ---           /

        /// <summary>
        /// Clear all the contents of cache
        /// </summary>
        /// <returns></returns>
        public void Clear()
        {
            OperationContext operationContext = null;
            BitSet bitset = null;
            try
            {
                operationContext = OperationContext.CreateAndMarkInUse(
                    Context.TransactionalPoolManager, NCModulesConstants.CacheCore, OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation
                );
                bitset = BitSet.CreateAndMarkInUse(Context.FakeObjectPool, NCModulesConstants.CacheCore);
                Clear(bitset, null, operationContext);
            }
            finally
            {
                MiscUtil.ReturnOperationContextToPool(operationContext, Context.TransactionalPoolManager);
                operationContext?.MarkFree(NCModulesConstants.CacheCore);
                bitset?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Clear all the contents of cache
        /// </summary>
        /// <returns></returns>
        public void Clear(OperationContext operationContext)
        {
            BitSet bitset = null;
            try
            {
                bitset = BitSet.CreateAndMarkInUse(Context.FakeObjectPool, NCModulesConstants.CacheCore);
                Clear(bitset, null, operationContext);
            }
            finally
            {
                bitset?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Clear all the contents of cache
        /// </summary>
        /// <returns></returns>
        public void Clear(BitSet flag, Notifications notification, OperationContext operationContext)
        {
            // Cache has possibly expired so do default.
            if (!IsRunning) return; 

            object block = null;
            bool isNoBlock = false;
            block = operationContext.GetValueByField(OperationContextFieldName.NoGracefulBlock);
            if (block != null)
                isNoBlock = (bool)block;

            if (!isNoBlock)
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkWrite, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }
            }


            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);
          


            try
            {

                _context.CacheImpl.Clear(notification, updateOpts, operationContext);
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Clear()", inner.ToString());
                throw new OperationFailedException("Clear operation failed. Error: " + inner.Message, inner);
            }
        }


        /// <summary>
        /// Asynchronous version of Clear. Clears all the contents of cache
        /// </summary>
        public void ClearAsync(BitSet flagMap, Notifications notification, OperationContext operationContext)
        {
            // Cache has possibly expired so do default.
            if (!IsRunning) return; 
            _asyncProcessor.Enqueue(new AsyncClear(this, notification, flagMap, operationContext));
        }

#endregion



#region	/                 --- Contains ---           /

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element 
        /// with the specified key; otherwise, false.</returns>
        public Hashtable Contains(IList keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.Contains", "");
            if (keys == null) throw new ArgumentNullException("keys");
            if (!keys.GetType().IsSerializable)
                throw new ArgumentException("keys are not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null;

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkRead, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                return _context.CacheImpl.Contains(keys, operationContext);
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
        /// <returns>Returns a CacheEntry</returns>
        public object GetCacheEntry(object key, string group, string subGroup, ref object lockId, ref DateTime lockDate,
            TimeSpan lockTimeout, LockAccessType accessType, OperationContext operationContext, ref ulong itemVersion)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null;

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicRead))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }

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
               

              

               
                
                    // if key and group are provided by user
                    if (group != null)
                        entry = _context.CacheImpl.GetGroup(key, group, subGroup, ref itemVersion, ref lockId, ref lockDate,
                            null, LockAccessType.IGNORE_LOCK, operationContext);

                    // if key and locking information is provided by user
                    else
                        entry = _context.CacheImpl.Get(key, ref itemVersion, ref lockId, ref lockDate, lockExpiration,
                        accessType, operationContext);

                    if (entry == null && accessType == LockAccessType.ACQUIRE)
                    {
                        if (lockId == null || generatedLockId.Equals(lockId))
                        {
                            lockId = null;
                            lockDate = new DateTime();
                        }
                    }

                    
               

                _context.PerfStatsColl.MsecPerGetEndSample();
                _context.PerfStatsColl.IncrementHitsRatioPerSecBaseStats();
                if (entry != null)
                {
                    _context.PerfStatsColl.IncrementHitsPerSecStats();
                    _context.PerfStatsColl.IncrementHitsRatioPerSecStats();
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
                //throw new OperationFailedException(ErrorCodes.BasicCacheOperations.GET_OPERATION_FAILED,ErrorMessages.GetErrorMessage(ErrorCodes.BasicCacheOperations.GET_OPERATION_FAILED,inner.Message), inner);
            }
        }


        /// <summary>
        /// Retrieve the object from the cache. A key is passed as parameter.
        /// </summary>
        public CompressedValueEntry Get(object key)
        {
            OperationContext operationContext = null;
            BitSet bitSet = null;
            try
            {
                operationContext = OperationContext.CreateAndMarkInUse(
                    Context.TransactionalPoolManager, NCModulesConstants.CacheCore, OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation
                );
                operationContext.UseObjectPool = false;
                bitSet = BitSet.CreateAndMarkInUse(Context.TransactionalPoolManager, NCModulesConstants.CacheCore);
                return GetGroup(key, bitSet, null, null,  operationContext);
            }
            finally
            {
                MiscUtil.ReturnOperationContextToPool(operationContext, Context.TransactionalPoolManager);
                operationContext?.MarkFree(NCModulesConstants.CacheCore);
                MiscUtil.ReturnBitsetToPool(bitSet, _context.TransactionalPoolManager);
                bitSet?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        public void Unlock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (IsRunning)
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }
                _context.CacheImpl.UnLock(key, lockId, isPreemptive, operationContext);
            }
        }

        public bool IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (IsRunning)
            {

                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicRead))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                object passedLockId = lockId;
                LockOptions lockInfo = _context.CacheImpl.IsLocked(key, ref lockId, ref lockDate, operationContext);
                if (lockInfo != null)
                {
                    if (lockInfo.LockId == null || lockInfo.LockDate == null)
                        return false;

                    lockId = lockInfo.LockId;
                    lockDate = lockInfo.LockDate;

                    return !Equals(lockInfo.LockId, passedLockId);
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public bool Lock(object key, TimeSpan lockTimeout, out object lockId, out DateTime lockDate,
            OperationContext operationContext)
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

                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, BlockInterval * 1000);
                }

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

        internal CompressedValueEntry GetGroup(object key, BitSet flagMap, string group, string subGroup, OperationContext operationContext)
        {
            object lockId = null;
            DateTime lockDate = DateTime.UtcNow;
            ulong version = 0;
            return GetGroup(key, flagMap, group, subGroup, ref version, ref lockId, ref lockDate, TimeSpan.Zero, LockAccessType.IGNORE_LOCK,  operationContext);
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
        /// <param name="group"></param>
        /// <param name="subGroup"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        internal CompressedValueEntry GetGroup(object key, BitSet flagMap, string group, string subGroup,
            ref ulong version, ref object lockId, ref DateTime lockDate, TimeSpan lockTimeout, LockAccessType accessType, OperationContext operationContext)
        {
            //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.GetGrp", "");
            // Cache has possibly expired so do default.
            if (!IsRunning) return null;

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicRead))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, BlockInterval * 1000);
            }

            CompressedValueEntry result = CompressedValueEntry.CreateCompressedCacheEntry(operationContext.UseObjectPool? Context.TransactionalPoolManager:Context.FakeObjectPool);

            CacheEntry e = null;
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.CacheCore);

                _context.PerfStatsColl.MsecPerGetBeginSample();
                _context.PerfStatsColl.IncrementGetPerSecStats();
                _context.PerfStatsColl.IncrementHitsRatioPerSecBaseStats();

               

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

               

                if (group == null && subGroup == null)
                        e = _context.CacheImpl.Get(key, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
                    else
                    {
                        e = _context.CacheImpl.GetGroup(key, group, subGroup, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
                    }

                    if (e == null && accessType == LockAccessType.ACQUIRE)
                    {
                        if (lockId == null || generatedLockId.Equals(lockId))
                        {
                            lockId = null;
                            lockDate = new DateTime();
                        }
                    }
               

                if (e != null)
                {
                    /// increment the counter for hits/sec
                    _context.PerfStatsColl.MsecPerGetEndSample();
                    result.Value = e.Value;
                    result.Flag = e.Flag;
                    result.Type = e.Type;
                    result.Entry = e;
                }

                

                _context.PerfStatsColl.MsecPerGetEndSample();

                //getTime.EndSample();
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

            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable)
                    _context.NCacheLog.Error("Cache.Get()", "Get operation failed. Error : " + inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Get()", "Get operation failed. Error : " + inner.ToString());
                throw new OperationFailedException("Get operation failed. Error : " + inner.Message, inner);
                //throw new OperationFailedException(ErrorCodes.BasicCacheOperations.GET_OPERATION_FAILED, ErrorMessages.GetErrorMessage(ErrorCodes.BasicCacheOperations.GET_OPERATION_FAILED, inner.Message), inner);
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.CacheCore);
                if (e != null)
                    e.MarkFree(NCModulesConstants.Global);

            }
            return result;
        }

        private string GetDefaultProvider()
        {
            return "";
        }


#endregion



#region	/                 --- Bulk Get ---           /

        /// <summary>
        /// Retrieve the array of objects from the cache.
        /// An array of keys is passed as parameter.
        /// </summary>
        internal IDictionary GetBulk(object[] keys, BitSet flagMap, OperationContext operationContext)
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.GetBlk", "");
                flagMap?.MarkInUse(NCModulesConstants.CacheCore);

                if (keys == null) throw new ArgumentNullException("keys");

                // Cache has possibly expired so do default.
                if (!IsRunning) return null; 

                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkRead, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                HashVector table = null;
                try
                {
          
                    HPTimeStats getTime = new HPTimeStats();
                    getTime.BeginSample();
                    _context.PerfStatsColl.IncrementByGetPerSecStats(keys.Length);
                    _context.PerfStatsColl.MsecPerGetBeginSample();

                      table = (HashVector)_context.CacheImpl.Get(keys, operationContext);
                  


                    if (table != null)
                    {
                        _context.PerfStatsColl.IncrementByHitsPerSecStats(table.Count);
                        _context.PerfStatsColl.IncrementByHitsRatioPerSecStats(table.Count);
                        _context.PerfStatsColl.IncrementByHitsRatioPerSecBaseStats(keys.Length);

                        long misses = keys.Length - table.Count;

                        if (misses > 0)
                            _context.PerfStatsColl.IncrementByMissPerSecStats(misses);

                        /// increment the counter for hits/sec
                       
                       

                        ///We maintian indexes of keys that needs resync or are not fethced in this array
                        ///This saves us from instantiating 3 separate arrays and then resizing it; 3 arrays to
                        ///hold keys, enteries, and flags
                        int[] resyncIndexes = null;
                        int counter = 0;

                        
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);
                        
                            CacheEntry entry = null;
                            for (int i = 0; i < keys.Length; i++)
                            {
                                if (table[keys[i]] != null)
                                {
                                    try
                                    {

                                        entry = table[keys[i]] as CacheEntry;

                                        if (entry != null)
                                        {
                                            table[keys[i]] = CompressedValueEntry.CreateCompressedCacheEntry(Context.TransactionalPoolManager, entry);
                                        }
                                    }
                                    finally
                                    {
                                        entry?.MarkFree(NCModulesConstants.Global);
                                    }
                                }
                               
                            }
                        

                      

                        getTime.EndSample();
                    }
                    else
                    {
                        _context.PerfStatsColl.IncrementByMissPerSecStats(keys.Length);
                    }

                    _context.PerfStatsColl.MsecPerGetEndSample();

                }
                catch (OperationCanceledException inner)
                {
                    _context.NCacheLog.Error("Cache.Get()", inner.ToString());
                    throw;
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
                    //throw new OperationFailedException(ErrorCodes.BasicCacheOperations.GET_OPERATION_FAILED,ErrorMessages.GetErrorMessage(ErrorCodes.BasicCacheOperations.GET_OPERATION_FAILED,inner.Message), inner);
                }
                return table;
            }
            finally
            {
                flagMap?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        internal IDictionary GetBulkCacheItems(string[] keys, BitSet flagMap, OperationContext operationContext)
        {
            object lockId = string.Empty;
            DateTime lockDate = new DateTime();
            ulong cacheItemVersion = default(ulong);
            TimeSpan LockTimeout = default(TimeSpan);
            IDictionary dictionary = new Dictionary<string, object>();

            foreach (var key in keys)
            {
                object cacheEntry = GetCacheEntry(key, null, null, ref lockId, ref lockDate, LockTimeout, LockAccessType.IGNORE_LOCK, operationContext, ref cacheItemVersion);

                if (cacheEntry != null)
                    dictionary[key] = cacheEntry;
            }

            return dictionary;
        }


#endregion



#region	/                 --- Add ---           /




        /// <summary>
        /// Convert CompactCacheEntry to CacheEntry, CompactCacheEntry may be serialized
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal CacheEntry MakeCacheEntry(CompactCacheEntry cce)
        {
            bool isAbsolute = false;
            bool isResync = false;
            int priority = (int)CacheItemPriority.Normal;

            int opt = (int)cce.Options;

            if (opt != 255)
            {
                isAbsolute = Convert.ToBoolean(opt & 1);
                opt = (opt >> 1);
                isResync = Convert.ToBoolean(opt & 1);
                opt = (opt >> 1);
                priority = opt - 2;
            }

            ExpirationHint eh = ConvHelper.MakeExpirationHint(_context.FakeObjectPool, cce.Expiration, isAbsolute);

            if (eh != null && cce.Dependency != null)
            {
                eh = AggregateExpirationHint.Create(_context.FakeObjectPool, cce.Dependency, eh);
            }

            if (eh == null) eh = cce.Dependency;

            if (eh != null)
            {
                if (isResync) eh.SetBit(ExpirationHint.NEEDS_RESYNC);
            }

            CacheEntry e = null;
            try
            {
                EvictionHint evh = PriorityEvictionHint.Create(_context.FakeObjectPool, (CacheItemPriority)priority);
                e = CacheEntry.CreateCacheEntry(_context.FakeObjectPool, cce.Value, eh, evh);
                e.ProviderName = cce.ProviderName;
                string type = cce.Value.GetType().FullName;
                e.GroupInfo = new GroupInfo(cce.Group, cce.SubGroup, type);
                e.QueryInfo = cce.QueryInfo;
                e.Flag.Data = cce.Flag.Data;
                e.Notifications = cce.CallbackEntry;
                
                e.LockId = cce.LockId;
                e.LockAccessType = cce.LockAccessType;
                e.Version = (UInt32)cce.Version;
                e.ResyncProviderName = cce.ResyncProviderName;
                e.MarkInUse(NCModulesConstants.CacheCore);
            }
            finally
            {
            }
            return e;
        }



        /// <summary>
        /// Add an ExpirationHint against given key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hint"></param>
        /// <returns></returns>
        public bool AddExpirationHint(object key, ExpirationHint hint, OperationContext operationContext)
        {
            if (!IsRunning) return false;
            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

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
            CacheEntry e = null;
            try
            {
                if (!IsRunning) return;

                CompactCacheEntry cce = null;

                cce = (CompactCacheEntry)entry;

                e = MakeCacheEntry(cce);
                
                string group = null, subgroup = null, typeName = null;
                if (e.GroupInfo != null && e.GroupInfo.Group != null)
                {
                    group = e.GroupInfo.Group;
                    subgroup = e.GroupInfo.SubGroup;
                    typeName = e.GroupInfo.Type;
                }
                Add(cce.Key, e.Value, e.ExpirationHint, e.EvictionHint, group, subgroup, e.QueryInfo,
                    e.Flag, cce.ProviderName, e.ResyncProviderName, operationContext, null, e.Notifications, typeName);
            }
            finally
            {
                if (e != null)
                    e.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Basic Add operation, takes only the key and object as parameter.
        /// </summary>

        public void Add(object key, object value)
        {
            OperationContext operationContext = null;

            try
            {
                operationContext = OperationContext.CreateAndMarkInUse(
                    Context.TransactionalPoolManager, NCModulesConstants.CacheCore, OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation
                );
                Add(key, value, null, null, null, null, operationContext);
            }
            finally
            {
                MiscUtil.ReturnOperationContextToPool(operationContext, Context.TransactionalPoolManager);
                operationContext?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Overload of Add operation. Uses an additional ExpirationHint parameter to be used 
        /// for Item Expiration Feature.
        /// </summary>

        public void Add(object key, object value, ExpirationHint expiryHint, OperationContext operationContext)
        {
            Add(key, value, expiryHint, null, null, null, operationContext);
        }

        /// <summary>
        /// Overload of Add operation. Uses an additional EvictionHint parameter to be used for 
        /// Item auto eviction policy.
        /// </summary>

        public void Add(object key, object value, EvictionHint evictionHint)
        {
            OperationContext operationContext = null;

            try
            {
                operationContext = OperationContext.CreateAndMarkInUse(
                    Context.TransactionalPoolManager, NCModulesConstants.CacheCore, OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation
                );
                Add(key, value, null, evictionHint, null, null, operationContext);
            }
            finally
            {
                MiscUtil.ReturnOperationContextToPool(operationContext, Context.TransactionalPoolManager);
                operationContext?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Overload of Add operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>

        public void Add(object key, object value,
            ExpirationHint expiryHint,  EvictionHint evictionHint,
            string group, string subGroup, OperationContext operationContext
            )
        {
            Add(key, value, expiryHint, evictionHint, group, subGroup, null, operationContext, null);
        }

        /// <summary>
        /// Overload of Add operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>

        public void Add(object key, object value,
            ExpirationHint expiryHint,  EvictionHint evictionHint,
            string group, string subGroup, Hashtable queryInfo, OperationContext operationContext, string type
            )
        {
            BitSet bitset = null;
            try
            {
                bitset = BitSet.CreateAndMarkInUse(Context.TransactionalPoolManager, NCModulesConstants.CacheCore);
                Add(key, value, expiryHint, evictionHint, group, subGroup, queryInfo, bitset, null,
                null, operationContext, null, null, type);

            }
            finally
            {
                MiscUtil.ReturnBitsetToPool(bitset, _context.TransactionalPoolManager);
                bitset?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Overload of Add operation. uses additional paramer of Flag for checking if compressed or not
        /// </summary>
        public void Add(object key, object value,
            ExpirationHint expiryHint, EvictionHint evictionHint,
            string group, string subGroup, Hashtable queryInfo, BitSet flag, string providerName,
            string resyncProviderName, OperationContext operationContext, HPTime bridgeOperationTime, Notifications notification, string typeName)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            // Cache has possibly expired so do default.
            if (!IsRunning) return; 
            if (!IsRunning) return; 
            CacheEntry clone = null;
            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);

           
            //No Need to insert GroupInfo if its group property is null/empty it will only reduce cache-entry overhead

            GroupInfo grpInfo = null;
            if (!String.IsNullOrEmpty(group))
                grpInfo = new GroupInfo(group, subGroup, typeName);

            CacheEntry e = CacheEntry.CreateCacheEntry(Context.TransactionalPoolManager, value, expiryHint, evictionHint);

            try
            {

                if (operationContext != null && operationContext.GetValueByField(OperationContextFieldName.ItemVersion) != null)
                    e.Version = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);
                else
                    e.Version = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                e.GroupInfo = grpInfo;
                ////Object size for inproc
                object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
                if (dataSize != null)
                    e.DataSize = Convert.ToInt64(dataSize);
                e.ResyncProviderName = resyncProviderName;
                e.ProviderName = providerName;
                
                e.QueryInfo = queryInfo;
                e.Notifications = notification;

                e.Flag.Data |= flag.Data;
                e.MarkInUse(NCModulesConstants.CacheCore);
                _context.PerfStatsColl.MsecPerAddBeginSample();


                clone = e;

               

              
                operationContext?.MarkInUse(NCModulesConstants.CacheCore);


                Add(key, e, operationContext);

                _context.PerfStatsColl.MsecPerAddEndSample();

             
                {
                  
                    if (!ReferenceEquals(e, clone))
                        MiscUtil.ReturnEntryToPool(clone, Context.TransactionalPoolManager);
                }
            }
            catch (CacheException ex)
            { 
                throw new OperationFailedException(ex.ErrorCode, ex.Message);
            }
            catch (Exception inner)
            {
                throw new OperationFailedException(inner.Message);
            }

            finally
            {
                // return the added version.
                if (operationContext.Contains(OperationContextFieldName.ItemVersion))
                {
                    operationContext.RemoveValueByField(OperationContextFieldName.ItemVersion);
                }
                operationContext.Add(OperationContextFieldName.ItemVersion, e.Version);
                operationContext?.MarkFree(NCModulesConstants.CacheCore);

                if (e != null)
                    e.MarkFree(NCModulesConstants.CacheCore);
                 
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.CacheCore);

                MiscUtil.ReturnEntryToPool(e, Context.TransactionalPoolManager);
            }

        }

        /// <summary>
        /// called from web cache to initiate the custom notifications.
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="data"></param>
        /// <param name="async"></param>

        public void SendNotification(object notifId, object data, OperationContext operationContext)
        {
            // cache may have expired or not initialized.
            if (!IsRunning) return;

            if (notifId != null && !notifId.GetType().IsSerializable)
                throw new ArgumentException("notifId is not serializable");
            if (data != null && !data.GetType().IsSerializable)
                throw new ArgumentException("data is not serializable");

            try
            {
                _context.CacheImpl.SendNotification(notifId, data, operationContext);
            }
            catch (Exception)
            {
                throw;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="entry"></param>
        public void AddAsyncEntry(object entry, OperationContext operationContext)
        {
            if (!IsRunning) return;

            CompactCacheEntry cce =
                (CompactCacheEntry)SerializationUtil.CompactDeserialize(entry, _context.SerializationContext);

            bool isAbsolute = false;
            bool isResync = false;
            int priority = (int)CacheItemPriority.Normal;

            int opt = (int)cce.Options;

            if (opt != 255)
            {
                isAbsolute = Convert.ToBoolean(opt & 1);
                opt = (opt >> 1);
                isResync = Convert.ToBoolean(opt & 1);
                opt = (opt >> 1);
                priority = opt - 2;
            }

            ExpirationHint eh = ConvHelper.MakeExpirationHint(_context.FakeObjectPool, cce.Expiration, isAbsolute);

            if (eh != null && cce.Dependency != null)
            {
                eh = AggregateExpirationHint.Create(_context.FakeObjectPool, cce.Dependency, eh);
            }

            if (eh == null) eh = cce.Dependency;

            if (eh != null)
            {
                if (isResync) eh.SetBit(ExpirationHint.NEEDS_RESYNC);
            }

            string typeName = cce.Value.GetType().FullName;
            typeName = typeName.Replace("+", ".");

            EvictionHint evh = PriorityEvictionHint.Create(_context.FakeObjectPool, (CacheItemPriority)priority);
            AddAsync(cce.Key, cce.Value, eh, evh, cce.Group, cce.SubGroup, cce.Flag, cce.QueryInfo, null, operationContext, cce.CallbackEntry, typeName);
        }

        public void AddAsync(object key, object value,
            ExpirationHint expiryHint,  EvictionHint evictionHint,
            string group, string subGroup, OperationContext operationContext, string typeName)
        {
            BitSet bitset = null;
            try
            {
                bitset = BitSet.CreateAndMarkInUse(Context.FakeObjectPool, NCModulesConstants.CacheCore);
                AddAsync(key, value, expiryHint, evictionHint, group, subGroup, bitset, null, null,
                operationContext, null, typeName);
            }
            finally
            {
                bitset?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Overload of Add operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        public void AddAsync(object key, object value,
            ExpirationHint expiryHint, EvictionHint evictionHint,
            string group, string subGroup, BitSet Flag, Hashtable queryInfo, string provider,
            OperationContext operationContext, Notifications notification, string typeName)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            if (!_inProc)
            {
                if (key is byte[])
                {
                    key = SerializationUtil.CompactDeserialize((byte[])key, _context.SerializationContext);
                }

                if (value is byte[])
                {
                    value = SerializationUtil.CompactDeserialize((byte[])value, _context.SerializationContext);
                }
            }

            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                throw new ArgumentException("expiryHint is not not serializable");

            if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                throw new ArgumentException("evictionHint is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return;

            _asyncProcessor.Enqueue(new AsyncAdd(this, key, value, expiryHint, evictionHint, group,
                subGroup, Flag, queryInfo, provider, notification, operationContext, typeName));
        }



        /// <summary>
        /// Internal Add operation. Does write-through as well.
        /// </summary>
        internal void Add(object key, CacheEntry e, OperationContext operationContext)
        {
            object value = e.Value;
            try
            {
                if (e != null)
                    e.MarkInUse(NCModulesConstants.CacheCore);
                operationContext?.MarkInUse(NCModulesConstants.CacheCore);
                CacheAddResult result = CacheAddResult.Failure;
                object block = null;
                bool isNoBlock = false;
                block = operationContext.GetValueByField(OperationContextFieldName.NoGracefulBlock);
                if (block != null)
                    isNoBlock = (bool)block;

                if (!isNoBlock)
                {
                    if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                    {
                        if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
                            _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                                BlockInterval * 1000);
                    }
                }


                result = _context.CacheImpl.Add(key, e, true, operationContext);

                switch (result)
                {
                    case CacheAddResult.Failure:
                        break;

                    case CacheAddResult.NeedsEviction:
                        throw new OperationFailedException(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED, ErrorMessages.GetErrorMessage(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED));
                       
                    case CacheAddResult.KeyExists:
                        throw new OperationFailedException(ErrorCodes.BasicCacheOperations.KEY_ALREADY_EXISTS,ErrorMessages.GetErrorMessage(ErrorCodes.BasicCacheOperations.KEY_ALREADY_EXISTS), false);

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
            finally
            {
                if (e != null)
                    e.MarkFree(NCModulesConstants.CacheCore);
                operationContext?.MarkFree(NCModulesConstants.CacheCore);

            }
        }

#endregion

#region	/                 --- Bulk Add ---           /


        /// <summary>
        /// Add array of CompactCacheEntry to cache, these may be serialized
        /// </summary>
        /// <param name="entries"></param>
        public IDictionary AddEntries(object[] entries, out IDictionary itemVersions, OperationContext operationContext)
        {
            itemVersions = null;
            // check if cache is running.
            if (!IsRunning) return null;

            string[] keys = new string[entries.Length];
            object[] values = new object[entries.Length];
            Notifications[] callbackEnteries = new Notifications[entries.Length]; 
            ExpirationHint[] exp = new ExpirationHint[entries.Length];
            EvictionHint[] evc = new EvictionHint[entries.Length];
            BitSet[] flags = new BitSet[entries.Length];
            Hashtable[] queryInfo = new Hashtable[entries.Length];
            GroupInfo[] groupInfo = new GroupInfo[entries.Length];

            Notifications notification = null;

            for (int i = 0; i < entries.Length; i++)
            {
                CompactCacheEntry cce =
                    (CompactCacheEntry)SerializationUtil.CompactDeserialize(entries[i], _context.SerializationContext);
                keys[i] = cce.Key as string;
                CacheEntry ce = null;
                try
                {
                    ce = MakeCacheEntry(cce);
                    if (ce != null)
                    {
                        if (ce.Notifications != null)
                            notification = ce.Notifications;
                        else
                            notification = null;

                        callbackEnteries[i] = notification;

                        object value = ce.Value;
                        values[i] = value;

                        exp[i] = ce.ExpirationHint;
                        evc[i] = ce.EvictionHint;
                        queryInfo[i] = ce.QueryInfo;
                        flags[i] = ce.Flag;
                        string type = cce.Value.GetType().FullName;
                        GroupInfo gInfo = new GroupInfo(cce.Group, cce.SubGroup, type);

                        groupInfo[i] = gInfo;
                    }
                }
                finally
                {
                    if (ce != null)
                        ce.MarkFree(NCModulesConstants.CacheCore);
                }
            }

            IDictionary items = Add(keys, values, callbackEnteries, exp, evc, groupInfo, queryInfo, flags, null,
                out itemVersions, operationContext);
    
            return items;
        }

     

        /// <summary>
        /// Overload of Add operation for bulk additions. Uses EvictionHint and ExpirationHint arrays.
        /// </summary>        
        public IDictionary Add(string[] keys, object[] values, Notifications[] callbackEnteries,
            ExpirationHint[] expirations, EvictionHint[] evictions,
            GroupInfo[] groupInfos, Hashtable[] queryInfos, BitSet[] flags, string providerName, out IDictionary itemVersions,
            OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.InsertBlk", "");

            if (keys == null) throw new ArgumentNullException("keys");
            if (values == null) throw new ArgumentNullException("items");
            if (keys.Length != values.Length) throw new ArgumentException("keys count is not equals to values count");

            itemVersions = new Hashtable();

            CacheEntry[] enteries = CacheEntry.CreateCacheEntries(Context.FakeObjectPool, values.Length);
            //object size for inproc
            long[] sizes = null;
            object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
            if (dataSize != null)
            {
                sizes = (long[])dataSize;
            }
            try
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (keys[i] == null) throw new ArgumentNullException("key");
                    if (values[i] == null) throw new ArgumentNullException("value");

                    if (!keys[i].GetType().IsSerializable)
                        throw new ArgumentException("key is not serializable");
                    if ((expirations[i] != null) && !expirations[i].GetType().IsSerializable)
                        throw new ArgumentException("expiryHint is not serializable");
                    if ((evictions[i] != null) && !evictions[i].GetType().IsSerializable)
                        throw new ArgumentException("evictionHint is not serializable");

                    // Cache has possibly expired so do default.
                    if (!IsRunning) return null;

                    enteries[i].Value = values[i];
                    enteries[i].ExpirationHint = expirations[i];
                    enteries[i].EvictionHint =  evictions[i];

                    enteries[i].Version = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                    itemVersions[keys[i]] = enteries[i].Version;
                    

                    //+ No Need to insert GroupInfo if its group property is null/empty it will only reduce cache-entry overhead
                    if (groupInfos[i] != null && !String.IsNullOrEmpty(groupInfos[i].Group))
                        enteries[i].GroupInfo = groupInfos[i];

                    enteries[i].QueryInfo = queryInfos[i];
                    enteries[i].Flag.Data |= flags[i].Data;
                    enteries[i].ProviderName = providerName;
                    if (sizes != null)
                        enteries[i].DataSize = sizes[i];
                    if (callbackEnteries[i] != null)
                    {
                        Notifications cloned = callbackEnteries[i].Clone() as Notifications;
                        enteries[i].Notifications = cloned;
                    }
                    enteries?.MarkInUse(NCModulesConstants.CacheCore);
                }
                CacheEntry[] clone = null;

                try
                {
                    if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                    {
                        if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkWrite, operationContext))
                            _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                                BlockInterval * 1000);
                    }

                    IDictionary result;
                    HPTimeStats addTime = new HPTimeStats();

         
                    clone?.MarkInUse(NCModulesConstants.CacheCore);

                    addTime.BeginSample();
                    result = Add(keys, enteries, operationContext);
                    addTime.EndSample();

                    return result;
                }
                catch (Exception inner)
                {
                    throw;
                }
                finally
                {
                    if (clone != null)
                        clone.MarkFree(NCModulesConstants.CacheCore);
                }
            }
            finally
            {
                if (enteries != null)
                    enteries.MarkFree(NCModulesConstants.CacheCore);
                
            }

        }


        /// <summary>
        /// Overload of Add operation for bulk additions. Uses EvictionHint and ExpirationHint arrays.
        /// </summary>        
        public IDictionary Add(string[] keys, CacheEntry[] enteries, BitSet flag, string providerName, out IDictionary itemVersions,
            OperationContext operationContext)
        {

            itemVersions = new Hashtable();

           
            if (enteries != null)
                enteries.MarkInUse(NCModulesConstants.CacheCore);

            long[] sizes = null;
            object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
            if (dataSize != null)
            {
                sizes = (long[])dataSize;
            }
            for (int keyCount = 0; keyCount < keys.Length; keyCount++)
            {
                enteries[keyCount].Version = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                itemVersions[keys[keyCount]] = enteries[keyCount].Version;
                
            }
            CacheEntry[] clone = null;
            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkWrite, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                IDictionary result;

              
                result = Add(keys, enteries, operationContext);


                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

               
                    // Write-Thru isn't to be performed so there is no use for clones to exist
                    MiscUtil.ReturnEntriesToPool(clone, Context.TransactionalPoolManager);
               

                return result;
            }

            catch (Exception inner)
            {
                itemVersions = null;
                throw;
            }
            finally
            {
                if (clone != null) clone.MarkFree(NCModulesConstants.CacheCore);
                if (enteries != null) enteries.MarkFree(NCModulesConstants.CacheCore);
            }

        }

        /// <summary>
        /// For operations,which needs to be retried.
        /// </summary>
       


      


        /// <summary>
        /// Internal Add operation for bulk additions. Does write-through as well.
        /// </summary>
        private Hashtable Add(object[] keys, CacheEntry[] entries, OperationContext operationContext)
        {
            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.bulk_operations).UpdateUsageTime();

            try
            {
                if (entries != null)
                    entries.MarkInUse(NCModulesConstants.CacheCore);

                Hashtable result = new Hashtable();
                result = _context.CacheImpl.Add(keys, entries, true, operationContext);
                if (result != null && result.Count > 0)
                {
                    Hashtable tmp = (Hashtable)result.Clone();
                    IDictionaryEnumerator ide = tmp.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                        CacheAddResult addResult = CacheAddResult.Failure;
                        if (ide.Value is CacheAddResult)
                        {
                            addResult = (CacheAddResult)ide.Value;
                            switch (addResult)
                            {
                                case CacheAddResult.Failure:
                                    break;
                                case CacheAddResult.KeyExists:
                                    result[ide.Key] = new OperationFailedException(ErrorCodes.BasicCacheOperations.KEY_ALREADY_EXISTS,ErrorMessages.GetErrorMessage(ErrorCodes.BasicCacheOperations.KEY_ALREADY_EXISTS));
                                    break;
                                case CacheAddResult.NeedsEviction:
                                    result[ide.Key] =
                                        new OperationFailedException(
                                           ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED, ErrorMessages.GetErrorMessage(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED));
                                    break;
                                case CacheAddResult.Success:
                                    if (_context.PerfStatsColl != null)
                                        _context.PerfStatsColl.IncrementAddPerSecStats();
                                    result.Remove(ide.Key);
                                    break;
                            }
                        }
                        if (ide.Value == null)
                        {
                            result.Remove(ide.Key);
                        }
                    }
                }
                else
                {
                    if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementByAddPerSecStats(keys.Length);
                }
                return result;
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.Add():", inner.ToString());
                throw;
            }
            catch (OperationCanceledException ex)
            {
                _context.NCacheLog.Error("Cache.Add():", ex.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Add():", inner.ToString());
                throw new OperationFailedException("Add operation failed. Error : " + inner.Message, inner);
            }
            finally
            {
                if (entries != null)
                    entries.MarkFree(NCModulesConstants.CacheCore);
            }
        }

#endregion


#region	/                 --- Insert ---           /


        /// <summary>
        /// Insert a CompactCacheEntry, it may be serialized
        /// </summary>
        /// <param name="entry"></param>

        [CLSCompliant(false)]
        public ulong InsertEntry(object entry, OperationContext operationContext)
        {
            if (!IsRunning)
                return 0;

            CompactCacheEntry cce = null;
            cce = (CompactCacheEntry)entry;
            CacheEntry e = null;
            try
            {
                e = MakeCacheEntry(cce);
                if (operationContext != null && operationContext.GetValueByField(OperationContextFieldName.ItemVersion) != null)
                    e.Version = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);
                string group = null, subgroup = null, type = null;
                if (e.GroupInfo != null && e.GroupInfo.Group != null)
                {
                    group = e.GroupInfo.Group;
                    subgroup = e.GroupInfo.SubGroup;
                    type = e.GroupInfo.Type;
                }
                return Insert(cce.Key, e.Value, e.ExpirationHint, e.EvictionHint, group, subgroup,
                    e.QueryInfo, e.Flag, e.LockId, e.Version, e.LockAccessType, e.ProviderName, e.ResyncProviderName, operationContext, e.Notifications, type);
            }
            finally
            {
                if (e != null)
                    e.MarkFree(NCModulesConstants.CacheCore);
            }
        }


        /// <summary>
        /// Basic Insert operation, takes only the key and object as parameter.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Insert(object key, object value)
        {
            OperationContext operationContext = null;

            try
            {
                operationContext = OperationContext.CreateAndMarkInUse(
                    Context.TransactionalPoolManager, NCModulesConstants.CacheCore, OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation
                );
                return Insert(key, value, null, null, null, null, null, operationContext, null);
            }
            finally
            {
                MiscUtil.ReturnOperationContextToPool(operationContext, Context.TransactionalPoolManager);
                operationContext?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Overload of Insert operation. Uses an additional ExpirationHint parameter to be used 
        /// for Item Expiration Feature.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Insert(object key, object value, ExpirationHint expiryHint)
        {
            OperationContext operationContext = null;

            try
            {
                operationContext = OperationContext.CreateAndMarkInUse(
                    Context.TransactionalPoolManager, NCModulesConstants.CacheCore, OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation
                );
                return Insert(key, value, expiryHint, null, null, null, null, operationContext, null);
            }
            finally
            {
                MiscUtil.ReturnOperationContextToPool(operationContext, Context.TransactionalPoolManager);
                operationContext?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Overload of Insert operation. Uses an additional EvictionHint parameter to be used 
        /// for Item auto eviction policy.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Insert(object key, object value, EvictionHint evictionHint)
        {
            OperationContext operationContext = null;

            try
            {
                operationContext = OperationContext.CreateAndMarkInUse(
                    Context.TransactionalPoolManager, NCModulesConstants.CacheCore, OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation
                );
                return Insert(key, value, null, evictionHint, null, null, operationContext, null);
            }
            finally
            {
                MiscUtil.ReturnOperationContextToPool(operationContext, Context.TransactionalPoolManager);
                operationContext?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Overload of Insert operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>

        [CLSCompliant(false)]
        public ulong Insert(object key, object value,
            ExpirationHint expiryHint,  EvictionHint evictionHint,
            string group, string subGroup, OperationContext operationContext, string typeName
            )
        {
            return Insert(key, value, expiryHint, evictionHint, group, subGroup, null, operationContext, typeName);
        }


        [CLSCompliant(false)]
        public ulong Insert(object key, object value,
            ExpirationHint expiryHint,  EvictionHint evictionHint,
            string group, string subGroup, Hashtable queryInfo, OperationContext operationContext, string typeName
            )
        {
            BitSet bitset = null;
            try
            {
                bitset = BitSet.CreateAndMarkInUse(Context.TransactionalPoolManager, NCModulesConstants.CacheCore);
                return Insert(key, value, expiryHint, evictionHint, group, subGroup, queryInfo, bitset,
                operationContext, null, null, typeName);
            }
            finally
            {
                MiscUtil.ReturnBitsetToPool(bitset, _context.TransactionalPoolManager);
                bitset?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Overload of Insert operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Insert(object key, object value,
            ExpirationHint expiryHint, EvictionHint evictionHint,
            string group, string subGroup, Hashtable queryInfo, BitSet flag, OperationContext operationContext,
            HPTime bridgeOperationTime, Notifications notification, string type)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");
            if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                throw new ArgumentException("expiryHint is not not serializable");
            if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                throw new ArgumentException("evictionHint is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning)
                return 0;
            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);

          
            //+ No Need to insert GroupInfo if its group property is null/empty it will only reduce cache-entry overhead
            ulong version = 0;
            GroupInfo grpInfo = null;
            if (!String.IsNullOrEmpty(group))
                grpInfo = new GroupInfo(group, subGroup, type);
            CacheEntry e = null;
            CacheEntry clone = null;

            try
            { 
                e = CacheEntry.CreateCacheEntry(Context.TransactionalPoolManager, value, expiryHint, evictionHint);

                if (operationContext != null && operationContext.GetValueByField(OperationContextFieldName.ItemVersion) != null)
                    e.Version = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);
                else
                    e.Version = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                e.GroupInfo = grpInfo;
                
                e.QueryInfo = queryInfo;

                e.Notifications = notification;

                e.Flag.Data |= flag.Data;
                
                e.MarkInUse(NCModulesConstants.CacheCore);
                /// update the counters for various statistics

                try
                {


                    if (updateOpts == DataSourceUpdateOptions.WriteThru || updateOpts == DataSourceUpdateOptions.WriteBehind)
                    {
                        clone = (CacheEntry)e.DeepClone(Context.TransactionalPoolManager);
                        if (clone != null)
                            clone.MarkInUse(NCModulesConstants.CacheCore);
                    }
                    else
                    {
                        clone = e;
                    }

                    _context.PerfStatsColl.MsecPerUpdBeginSample();

                    version = Insert(key, e, null, 0, LockAccessType.IGNORE_LOCK, operationContext);

                    _context.PerfStatsColl.MsecPerUpdEndSample();


                }
                catch (Exception inner)
                {
                    throw;
                }
            }
            finally
            {
                if (e != null)
                {
                    MiscUtil.ReturnEntryToPool(e, _context.TransactionalPoolManager);
                    e.MarkFree(NCModulesConstants.CacheCore);
                }

                if (clone != null)
                {
                    clone.MarkFree(NCModulesConstants.CacheCore);
                }
            }

            return version;
        }


        /// <summary>
        /// Overload of Insert operation. For Bridge Operation in case of proper Item version maintained.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Insert(object key, object value,
            ExpirationHint expiryHint,  EvictionHint evictionHint,
            string group, string subGroup, Hashtable queryInfo, BitSet flag, ulong version,
            OperationContext operationContext, HPTime bridgeOperationTime, Notifications notification, string type = null)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");
            if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                throw new ArgumentException("expiryHint is not not serializable");
            if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                throw new ArgumentException("evictionHint is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning)
                return 0;
            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);

            //+  No Need to insert GroupInfo if its group property is null/empty it will only reduce cache-entry overhead
            GroupInfo grpInfo = null;
            if (!String.IsNullOrEmpty(group))
                grpInfo = new GroupInfo(group, subGroup,type);

            ulong itemVersion = 0;
            CacheEntry e = null;
            CacheEntry clone = null;


            try
            {
                e = CacheEntry.CreateCacheEntry(Context.FakeObjectPool,value, expiryHint, evictionHint);
                if (operationContext != null && operationContext.GetValueByField(OperationContextFieldName.ItemVersion) != null)
                    e.Version = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);
                else
                    e.Version = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                e.GroupInfo = grpInfo;
                

                e.Notifications = notification;
                e.QueryInfo = queryInfo;

                e.Flag.Data |= flag.Data;
             
                e.MarkInUse(NCModulesConstants.CacheCore);
                /// update the counters for various statistics

                try
                {

                    if ((updateOpts == DataSourceUpdateOptions.WriteThru ||
                         updateOpts == DataSourceUpdateOptions.WriteBehind) && e.HasQueryInfo)
                        clone = (CacheEntry)e.Clone();
                    else
                        clone = e;

                    if (clone != null)
                        clone.MarkInUse(NCModulesConstants.BackingSource);

                    _context.PerfStatsColl.MsecPerUpdBeginSample();

                    itemVersion = Insert(key, e, null, version, LockAccessType.PRESERVE_VERSION, operationContext);

                    _context.PerfStatsColl.MsecPerUpdEndSample();

                    
                   

                }
                catch (Exception inner)
                {
                    throw;
                }
            }
            finally
            {
                if (e != null)
                {
                    e.MarkFree(NCModulesConstants.CacheCore);
                }

                if (clone != null)
                    clone.MarkFree(NCModulesConstants.CacheCore);
            }

            return itemVersion;
        }


        /// <summary>
        /// Overload of Insert operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Insert(object key, object value,
            ExpirationHint expiryHint,  EvictionHint evictionHint,
            string group, string subGroup, Hashtable queryInfo, BitSet flag, object lockId, ulong version,
            LockAccessType accessType, string providerName, string resyncProviderName, OperationContext operationContext, Notifications notification, string typeName)
        {
            CacheEntry e = null;

            if (!IsRunning)
                return 0;
            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);

          
            CacheEntry clone = null;
            /// update the counters for various statistics
            ulong itemVersion;
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.CacheCore);
                e = Stash.CacheEntry;
                e.Value = value;
                e.ExpirationHint = expiryHint;
                e.EvictionHint = evictionHint;

                if (operationContext != null && operationContext.Contains(OperationContextFieldName.ItemVersion))
                    e.Version = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);
                else
                    e.Version = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                GroupInfo grpInfo = null;

                if (!String.IsNullOrEmpty(group))
                    grpInfo = new GroupInfo(group, subGroup, typeName);

                e.GroupInfo = grpInfo;
                e.QueryInfo = queryInfo;
                e.Flag.Data |= flag.Data;
                e.ResyncProviderName = resyncProviderName;
                e.ProviderName = providerName;
                e.Notifications = notification;

                object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
                if (dataSize != null)
                    e.DataSize = Convert.ToInt64(dataSize);
                e.MarkInUse(NCModulesConstants.CacheCore);
                
                try
                {

                    if (updateOpts == DataSourceUpdateOptions.WriteThru ||
                         updateOpts == DataSourceUpdateOptions.WriteBehind)
                    {
                        clone = e.DeepClone(Context.TransactionalPoolManager);
                        if (clone != null)
                            clone.MarkInUse(NCModulesConstants.CacheCore);
                    }
                    else
                        clone = e;
                
                    
                    _context.PerfStatsColl.MsecPerUpdBeginSample();

                    itemVersion = Insert(key, e, lockId, version, accessType, operationContext);

                    _context.PerfStatsColl.MsecPerUpdEndSample();

                    
                        // If cache is not a local cache and only Write-Behind was requested for this call, the Write-Behind operation 
                        // was performed at Topology level and since Write-Thru is not performed for this call, the clone created is of 
                        // no use. Therefore, returning it to pool.
                        if (!ReferenceEquals(e, clone))
                            MiscUtil.ReturnEntryToPool(clone, Context.TransactionalPoolManager);
                   
                }
                finally
                {
                    if (clone != null)
                        clone.MarkFree(NCModulesConstants.CacheCore);
                }
            }
            finally
            {
                if (e != null)
                    e.MarkFree(NCModulesConstants.CacheCore);
                operationContext?.MarkFree(NCModulesConstants.CacheCore);

                MiscUtil.ReturnEntryToPool(e, Context.TransactionalPoolManager);
            }

            return itemVersion;
        }


        [CLSCompliant(false)]
        public ulong Insert(object key, CacheEntry cacheEntry, OperationContext operationContext)
        {
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.CacheCore);

                if (!IsRunning)
                    return 0;
                
                cacheEntry.Version = 0;

                object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
                if (dataSize != null)
                    cacheEntry.DataSize = Convert.ToInt64(dataSize);


                ulong itemVersion;
                try
                {

                    _context.PerfStatsColl.MsecPerUpdBeginSample();

                    itemVersion = Insert(key, cacheEntry, string.Intern(""), 0, LockAccessType.IGNORE_LOCK, operationContext);

                    _context.PerfStatsColl.MsecPerUpdEndSample();
                }
                catch (Exception inner)
                {
                    NCacheLog.Error(inner.ToString());
                    throw;
                }

                return itemVersion;
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Return update option type set in the flag
        /// </summary>
        /// <param name="flag">falg</param>
        /// <returns>update option</returns>
        private DataSourceUpdateOptions UpdateOption(BitSet flag)
        {
            if (flag.IsBitSet(BitSetConstants.WriteThru)) return DataSourceUpdateOptions.WriteThru;
            else if (flag.IsBitSet(BitSetConstants.WriteBehind)) return DataSourceUpdateOptions.WriteBehind;
            else return DataSourceUpdateOptions.None;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="entry"></param>
        public void InsertAsyncEntry(object entry, OperationContext operationContext)
        {
            if (!IsRunning) return;

            CompactCacheEntry cce =
                (CompactCacheEntry)SerializationUtil.CompactDeserialize(entry, _context.SerializationContext);

            bool isAbsolute = false;
            bool isResync = false;
            int priority = (int)CacheItemPriority.Normal;

            int opt = (int)cce.Options;

            if (opt != 255)
            {
                isAbsolute = Convert.ToBoolean(opt & 1);
                opt = (opt >> 1);
                isResync = Convert.ToBoolean(opt & 1);
                opt = (opt >> 1);
                priority = opt - 2;
            }

            ExpirationHint eh = ConvHelper.MakeExpirationHint(_context.FakeObjectPool, cce.Expiration, isAbsolute);

            if (eh != null && cce.Dependency != null)
            {
                eh = AggregateExpirationHint.Create(_context.FakeObjectPool, cce.Dependency, eh);
            }

            if (eh == null) eh = cce.Dependency;

            if (eh != null)
            {
                if (isResync) eh.SetBit(ExpirationHint.NEEDS_RESYNC);
            }

            PriorityEvictionHint evh = PriorityEvictionHint.Create(_context.FakeObjectPool, (CacheItemPriority)priority);

            InsertAsync(cce.Key, cce.Value, eh,
               evh, cce.Group, cce.SubGroup, cce.Flag, cce.QueryInfo,
                null, operationContext, cce.CallbackEntry, null);
        }

        public void InsertAsync(object key, object value,
            ExpirationHint expiryHint,  EvictionHint evictionHint,
            string group, string subGroup, OperationContext operationContext, string typeName)
        {
            BitSet bitset = null;
            try
            {
                bitset = BitSet.CreateAndMarkInUse(Context.FakeObjectPool, NCModulesConstants.CacheCore);

                InsertAsync(key, value, expiryHint, evictionHint, group, subGroup, bitset, null, null,
                operationContext, null, typeName);
            }
            finally
            {
                bitset?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        /// <summary>
        /// Overload of Insert operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        public void InsertAsync(object key, object value,
            ExpirationHint expiryHint, EvictionHint evictionHint,
            string group, string subGroup, BitSet Flag, Hashtable queryInfo, string provider,
            OperationContext operationContext, Notifications notification, string typeName)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");
            if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                throw new ArgumentException("expiryHint is not not serializable");
            if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                throw new ArgumentException("evictionHint is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return;

            _asyncProcessor.Enqueue(new AsyncInsert(this, key, value, expiryHint, evictionHint, group,
                subGroup, Flag, queryInfo, provider, operationContext, notification, typeName));
        }



        /// <summary>
        /// Internal Insert operation. Does a write thru as well.
        /// </summary>
        internal ulong Insert(object key, CacheEntry e, object lockId, ulong version, LockAccessType accessType,
            OperationContext operationContext)
        {
            CacheInsResultWithEntry retVal = null;

            ulong retVersion = 0;
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.CacheCore);
                e?.MarkInUse(NCModulesConstants.CacheCore);
                retVal = CascadedInsert(key, e, true, lockId, version, accessType,
                   operationContext);


                switch (retVal.Result)
                {
                    case CacheInsResult.Failure:
                        break;

                    case CacheInsResult.NeedsEviction:
                    case CacheInsResult.NeedsEvictionNotRemove:
                        throw new OperationFailedException(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED, ErrorMessages.GetErrorMessage(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED),
                            false);
                    case CacheInsResult.SuccessOverwrite:
                        _context.PerfStatsColl.IncrementUpdPerSecStats();
                        //muds: new version of the item will be 1 more than the old item version.
                        retVersion = retVal.Entry == null ? e.Version : retVal.Entry.Version + 1;
                        break;

                    case CacheInsResult.Success:
                        _context.PerfStatsColl.IncrementAddPerSecStats();

                        retVersion = retVal.Entry == null ? e.Version : retVal.Entry.Version + 1;
                        break;

                    case CacheInsResult.IncompatibleGroup:
                        throw new OperationFailedException(
                            ErrorCodes.BasicCacheOperations.INSERTED_ITEM_DATAGROUP_MISMATCH,ErrorMessages.GetErrorMessage(ErrorCodes.BasicCacheOperations.INSERTED_ITEM_DATAGROUP_MISMATCH));

                    case CacheInsResult.ItemLocked:
                        throw new LockingException(ErrorCodes.BasicCacheOperations.ITEM_LOCKED,ErrorMessages.GetErrorMessage(ErrorCodes.BasicCacheOperations.ITEM_LOCKED));

                    case CacheInsResult.VersionMismatch:
                        throw new LockingException(ErrorCodes.BasicCacheOperations.ITEM_WITH_VERSION_DOESNT_EXIST, ErrorMessages.GetErrorMessage(ErrorCodes.BasicCacheOperations.ITEM_WITH_VERSION_DOESNT_EXIST));
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
                throw new OperationFailedException("Insert operation failed. Error : " + inner.Message, inner);
            }
            finally
            {
                e?.MarkFree(NCModulesConstants.CacheCore);
                retVal?.ReturnLeasableToPool();
                operationContext?.MarkFree(NCModulesConstants.CacheCore);

                MiscUtil.ReturnEntryToPool(retVal?.Entry, Context.TransactionalPoolManager);
                MiscUtil.ReturnCacheInsResultToPool(retVal, Context.TransactionalPoolManager);
            }
            return retVersion;
        }


#endregion


#region	/                 --- Bulk Insert ---           /


        /// <summary>
        /// Insert array of CompactCacheEntry to cache, these may be serialized
        /// </summary>
        /// <param name="entries"></param>
        public IDictionary InsertEntries(object[] entries, out IDictionary itemVersions, OperationContext operationContext)
        {
            itemVersions = null;
            // check if cache is running.
            if (!IsRunning) return null;

            string[] keys = new string[entries.Length];
            object[] values = new object[entries.Length];

            Notifications[] callbackEnteries = new Notifications[entries.Length]; 
            ExpirationHint[] exp = new ExpirationHint[entries.Length];
            EvictionHint[] evc = new EvictionHint[entries.Length];
            BitSet[] flags = new BitSet[entries.Length];
            Hashtable[] queryInfo = new Hashtable[entries.Length];
            GroupInfo[] groupInfos = new GroupInfo[entries.Length];
            Notifications notification = null;
            CacheEntry ce = null;

            for (int i = 0; i < entries.Length; i++)
            {
                CompactCacheEntry cce =
                    (CompactCacheEntry)SerializationUtil.CompactDeserialize(entries[i], _context.SerializationContext);
                keys[i] = cce.Key as string;
                try
                {
                    ce = MakeCacheEntry(cce);
                    if (ce != null)
                    {
                        if (ce.Notifications != null)
                            notification = ce.Notifications;
                        else
                            notification = null;

                        callbackEnteries[i] = notification;

                        values[i] = ce.Value;

                        exp[i] = ce.ExpirationHint;
                        evc[i] = ce.EvictionHint;
                        queryInfo[i] = ce.QueryInfo;
                        groupInfos[i] = ce.GroupInfo;
                        flags[i] = ce.Flag;
                    }
                }
                finally
                {
                    if (ce!=null)
                        ce.MarkFree(NCModulesConstants.CacheCore);
                }
            }

            IDictionary items = Insert(keys, values, callbackEnteries, exp, evc, groupInfos, queryInfo, flags, null,
                null, out itemVersions, operationContext);
     
            return items;
        }


        /// <summary>
        /// Overload of Insert operation for bulk inserts. Uses an additional ExpirationHint parameter to be used 
        /// for Item Expiration Feature.
        /// </summary>
        public IDictionary Insert(object[] keys, object[] values, ExpirationHint expiryHint,
            OperationContext operationContext)
        {
            return Insert(keys, values, expiryHint, null, null, null, operationContext);
        }

        /// <summary>
        /// Overload of Insert operation for bulk inserts. Uses an additional EvictionHint parameter to be used 
        /// for Item auto eviction policy.
        /// </summary>
        public IDictionary Insert(object[] keys, object[] values, EvictionHint evictionHint,
            OperationContext operationContext)
        {
            return Insert(keys, values, null, evictionHint, null, null, operationContext);
        }

        /// <summary>
        /// Overload of Insert operation for bulk inserts. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        public IDictionary Insert(object[] keys, object[] values,
            ExpirationHint expiryHint, EvictionHint evictionHint,
            string group, string subGroup, OperationContext operationContext)
        {

            if (keys == null) throw new ArgumentNullException("keys");
            if (values == null) throw new ArgumentNullException("items");
            if (keys.Length != values.Length)
                throw new ArgumentException("keys count is not equals to values count");

            CacheEntry[] ce = null;

            try
            {
                ce = CacheEntry.CreateCacheEntries(Context.FakeObjectPool, values.Length);
                for (int i = 0; i < values.Length; i++)
                {
                    object key = keys[i];
                    object value = values[i];


                    if (key == null) throw new ArgumentNullException("key");
                    if (value == null) throw new ArgumentNullException("value");

                    if (!key.GetType().IsSerializable)
                        throw new ArgumentException("key is not serializable");
                    if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                        throw new ArgumentException("expiryHint is not not serializable");
                    if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                        throw new ArgumentException("evictionHint is not serializable");

                    // Cache has possibly expired so do default.
                    if (!IsRunning) return null;

                    ce[i].Value = value;

                    ce[i].ExpirationHint = expiryHint;
                    ce[i].EvictionHint =  evictionHint;

                    GroupInfo grpInfo = null;

                    if (!String.IsNullOrEmpty(group))
                        grpInfo = new GroupInfo(group, subGroup);

                    ce[i].GroupInfo = grpInfo;

                }
                ce?.MarkInUse(NCModulesConstants.CacheCore);
                /// update the counters for various statistics
                try
                {
                    IDictionary itemVersions = new Hashtable();

                    return Insert(keys, ce, out itemVersions, operationContext);
                }
                catch (Exception inner)
                {
                    throw;
                }
            }
            finally
            {
                if (ce!=null)  ce.MarkFree(NCModulesConstants.CacheCore);
                
            }
        }

        /// <summary>
        /// Overload of Insert operation for bulk inserts. Uses EvictionHint and ExpirationHint arrays.
        /// </summary>
        public IDictionary Insert(object[] keys, object[] values, Notifications[] callbackEnteries,
            ExpirationHint[] expirations, EvictionHint[] evictions,
            GroupInfo[] groupInfos, Hashtable[] queryInfos, BitSet[] flags, string providername,
            string resyncProviderName, out IDictionary itemVersions, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.InsertBlk", "");

            if (keys == null) throw new ArgumentNullException("keys");
            if (values == null) throw new ArgumentNullException("items");
            if (keys.Length != values.Length) throw new ArgumentException("keys count is not equals to values count");

            itemVersions = new Hashtable();

            DataSourceUpdateOptions updateOpts = this.UpdateOption(flags[0]);
            
            CacheEntry[] ce = null;

            try
            {
                ce = CacheEntry.CreateCacheEntries(Context.FakeObjectPool, values.Length);

                //for object size in inproc
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
                    if ((expirations[i] != null) && !expirations[i].GetType().IsSerializable)
                        throw new ArgumentException("expiryHint is not not serializable");
                    if ((evictions[i] != null) && !evictions[i].GetType().IsSerializable)
                        throw new ArgumentException("evictionHint is not serializable");

                    // Cache has possibly expired so do default.
                    if (!IsRunning) return null;

                    ce[i].Value = values[i];

                    ce[i].ExpirationHint = expirations[i];

                    ce[i].EvictionHint = evictions[i];
                    
                    if (groupInfos[i] != null && !String.IsNullOrEmpty(groupInfos[i].Group))
                        ce[i].GroupInfo = groupInfos[i];
                    ce[i].QueryInfo = queryInfos[i];

                    ce[i].Flag.Data |= flags[i].Data;

                    ce[i].ProviderName = providername;

                    if (sizes != null)
                        ce[i].DataSize = sizes[i];

                    if (callbackEnteries[i] != null)
                    {
                        Notifications cloned = callbackEnteries[i].Clone() as Notifications;
                        ce[i].Notifications = cloned;
                    }
                }
                ce?.MarkInUse(NCModulesConstants.CacheCore);

                /// update the counters for various statistics
                CacheEntry[] clone = null;
                
                try
                {
                    if (updateOpts == DataSourceUpdateOptions.WriteBehind || updateOpts == DataSourceUpdateOptions.WriteThru)
                    {
                        clone = CacheEntry.CreateCacheEntries(Context.FakeObjectPool,ce.Length);
                        for (int i = 0; i < ce.Length; i++)
                        {
                            if (ce[i].HasQueryInfo)
                                clone[i] = (CacheEntry)ce[i].Clone();
                            else
                                clone[i] = ce[i];
                        }
                    }
                    clone?.MarkInUse(NCModulesConstants.BackingSource);
                    
                    HPTimeStats insertTime = new HPTimeStats();
                    insertTime.BeginSample();

                    IDictionary result = Insert(keys, ce, out itemVersions, operationContext);

                    insertTime.EndSample();

                    string[] filteredKeys = null;
                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    if (updateOpts != DataSourceUpdateOptions.None && keys.Length > result.Count)
                    {
                        filteredKeys = new string[keys.Length - result.Count];

                        for (int i = 0, j = 0; i < keys.Length; i++)
                        {
                            if (!result.Contains(keys[i]))
                            {
                                filteredKeys[j] = keys[i] as string;
                                j++;
                            }
                        }
                       
                      
                    }
                    return result;
                }
                catch (Exception inner)
                {
                    throw;
                }
                finally
                {
                    if (clone != null)
                        clone.MarkFree(NCModulesConstants.CacheCore);

                }
            }
            finally
            {
                if (ce != null) ce.MarkFree(NCModulesConstants.CacheCore);
            }
            
        }

        /// <summary>
        /// Internal Insert operation. Does a write thru as well.
        /// </summary>
        private Hashtable Insert(object[] keys, CacheEntry[] entries, out IDictionary itemVersions, OperationContext operationContext)
        {
            try
            {
                FeatureUsageCollector.Instance.GetFeature(FeatureEnum.bulk_operations).UpdateUsageTime();
                if (entries != null)
                    entries.MarkInUse(NCModulesConstants.CacheCore);
                Hashtable result;
                result = CascadedInsert(keys, entries, true, operationContext);
                itemVersions = new Hashtable();

                int index = 0;
                if (result != null && result.Count > 0)
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
                                    result[ide.Key] =
                                        new OperationFailedException(
                                          ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED, ErrorMessages.GetErrorMessage(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED));
                                    break;

                                case CacheInsResult.Success:
                                    if (_context.PerfStatsColl != null)
                                        _context.PerfStatsColl.IncrementAddPerSecStats();
                                    result.Remove(ide.Key);
                                    if (insResult.Entry != null)
                                        itemVersions[ide.Key] = insResult.Entry.Version;
                                    else
                                        itemVersions[ide.Key] = entries[index].Version;
                                    break;

                                case CacheInsResult.SuccessOverwrite:
                                    if (_context.PerfStatsColl != null)
                                        _context.PerfStatsColl.IncrementUpdPerSecStats();
                                    result.Remove(ide.Key);
                                    if (insResult.Entry != null)
                                        itemVersions[ide.Key] = insResult.Entry.Version + 1;
                                    else
                                        itemVersions[ide.Key] = entries[index].Version;
                                    break;

                                case CacheInsResult.IncompatibleGroup:
                                    result[ide.Key] =
                                        new OperationFailedException(
                                            "Data group of the inserted item does not match the existing item's data group");
                                    break;
                                case CacheInsResult.DependencyKeyNotExist:
                                    result[ide.Key] = new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND,ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));
                                    break;
                            }

                            MiscUtil.ReturnEntryToPool(insResult?.Entry, Context.TransactionalPoolManager);
                            MiscUtil.ReturnCacheInsResultToPool(insResult, Context.TransactionalPoolManager);
                        }
                        insResult?.ReturnLeasableToPool();
                        index++;
                    }
                }
                else
                {
                    for (int keyCount = 0; keyCount < keys.Length; keyCount++)
                    {
                        itemVersions[keys[keyCount]] = entries[keyCount].Version;
                    }
                    //In case of insert as add empty table is returned on successfull addition
                    if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementByAddPerSecStats(keys.Length);
                }
                return result;
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.Insert()", inner.ToString());
                throw;
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
            finally
            {
                if (entries != null)
                    entries.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        public IDictionary Insert(string[] keys, CacheEntry[] entries, BitSet flag, string providername,
            string resyncProviderName, out IDictionary itemVersions, OperationContext operationContext)
        {
            
            /// update the counters for various statistics
            /// 
            CacheEntry[] clone = null;

            try
            {
                if (entries != null && entries.Length > 0)
                    entries.MarkInUse(NCModulesConstants.CacheCore);

              

                for (int keyCount = 0; keyCount < keys.Length; keyCount++)
                {
                    entries[keyCount].Version = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                }

                IDictionary result = Insert(keys, entries, out itemVersions, operationContext);

                string[] filteredKeys = null;
                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                
                    // Write-Thru or Write-Behind isn't to be performed if all Insert operations failed so there is no use for 
                    // clones to exist (IF they exist)
                    MiscUtil.ReturnEntriesToPool(clone, Context.TransactionalPoolManager);
                
                return result;
            }
            catch (Exception inner)
            {
                throw;
            }
            finally
            {
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.CacheCore);
                if (entries != null && entries.Length > 0)
                    entries.MarkFree(NCModulesConstants.CacheCore);
            }
        }

#endregion



#region	/                 --- Remove ---           /

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        public CompressedValueEntry Remove(object key, OperationContext operationContext)
        {
            BitSet bitset = null;
            try
            {
                bitset = BitSet.CreateAndMarkInUse(Context.TransactionalPoolManager, NCModulesConstants.CacheCore);

                return Remove(key as string, bitset, null, null, 0, LockAccessType.IGNORE_LOCK, null, operationContext);
            }
            finally
            {
                MiscUtil.ReturnBitsetToPool(bitset, _context.TransactionalPoolManager);
                bitset?.MarkFree(NCModulesConstants.CacheCore);
            }

        }




        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        public void Remove(string group, string subGroup, OperationContext operationContext)
        {
            if (group == null) throw new ArgumentNullException("group");

            // Cache has possibly expired so do default.
            if (!IsRunning) return; 

            try
            {
                HPTimeStats removeTime = new HPTimeStats();
                removeTime.BeginSample();

                Hashtable removed = CascadedRemove(group, subGroup, true, operationContext);

                removeTime.EndSample();

                if (removed == null) 
                {

                }
                else
                {

                }

            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.RemoveByTag()", inner.ToString());
                throw;
            }
            catch (StateTransferInProgressException se)
            {
                throw se;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.RemoveByGroup()", inner.ToString());
                throw new OperationFailedException("Remove operation failed. Error : " + inner.Message, inner);
            }
            return;
        }



        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        [CLSCompliant(false)]
        public CompressedValueEntry Remove(string key, BitSet flag, Notifications notification, object lockId, ulong version,
            LockAccessType accessType, string providerName, OperationContext operationContext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null; 

            CacheEntry e = null;
            CacheEntry clone = null;
            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);

           

            try
            {

                _context.PerfStatsColl.MsecPerDelBeginSample();

                object packedKey = key;
                if (_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind)
                {
                    packedKey = new object[] { key, updateOpts, notification, providerName };
                }
                
                operationContext?.MarkInUse(NCModulesConstants.CacheCore);

                e = CascadedRemove(key, packedKey, ItemRemoveReason.Removed, true, lockId, version,
                    accessType, operationContext);

                _context.PerfStatsColl.MsecPerDelEndSample();
                _context.PerfStatsColl.IncrementDelPerSecStats();

                

                if (e != null)
                {
                    return CompressedValueEntry.CreateCompressedCacheEntry(Context.TransactionalPoolManager, e);
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
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.CacheCore);

                if (e != null)
                    e.MarkFree(NCModulesConstants.Global);
            }
            return null;
        }

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        [CLSCompliant(false)]
        public void Delete(string key, BitSet flag, Notifications notification, object lockId, ulong version,
            LockAccessType accessType, OperationContext operationContext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return;

            
            CacheEntry e = null;
            CacheEntry clone = null;

            try
            {

                _context.PerfStatsColl.MsecPerDelBeginSample();

                object packedKey = key;
               
                

                e = CascadedRemove(key, packedKey, ItemRemoveReason.Removed, true, lockId, version,
                    accessType, operationContext);

                _context.PerfStatsColl.MsecPerDelEndSample();
                _context.PerfStatsColl.IncrementDelPerSecStats();


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
            finally
            {
                if (e != null)
                    MiscUtil.ReturnEntryToPool(e, Context.TransactionalPoolManager);
            }
        }


        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        public void RemoveAsync(object key, OperationContext operationContext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return; 

            _asyncProcessor.Enqueue(new AsyncRemove(this, key, operationContext));
        }


#endregion



#region	/                 --- Bulk Remove ---           /

        /// <summary>
        /// Removes the objects for the given keys from the cache.
        /// The keys are specified as parameter.
        /// </summary>
        /// <param name="keys">array of keys to be removed</param>
        /// <returns>keys that failed to be removed</returns>
        public IDictionary Remove(object[] keys, BitSet flagMap, Notifications notification, string providerName,
            OperationContext operationContext)
        {
            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.bulk_operations).UpdateUsageTime();

            // Cache has possibly expired so do default.
            if (!IsRunning) return null;

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkWrite, operationContext))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }


           

            try
            {
                
                IDictionary removed = CascadedRemove(keys, ItemRemoveReason.Removed, true, operationContext);

                if (removed != null && removed.Count > 0)
                {
                    if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementByDelPerSecStats(removed.Count);
                }


                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);
                CacheEntry[] filteredEntries = null;
               
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
                            removed[ie.Current] = CompressedValueEntry.CreateCompressedCacheEntry(Context.TransactionalPoolManager, entry);
                        }
                    }
                }

                return removed;
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.Remove()", inner.ToString());
                throw;
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
        public void Delete(object[] keys, BitSet flagMap, Notifications notification, string providerName,
            OperationContext operationContext)
        {
            if (keys == null) throw new ArgumentNullException("keys");
            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.bulk_operations).UpdateUsageTime();

            // Cache has possibly expired so do default.
            if (!IsRunning) return;

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkWrite, operationContext))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }

            
            try
            {
                HPTimeStats removeTime = new HPTimeStats();
                removeTime.BeginSample();

             
                
                IDictionary removed = CascadedRemove(keys, ItemRemoveReason.Removed, true, operationContext);

                if (removed != null && removed.Count > 0)
                {
                    if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementByDelPerSecStats(removed.Count);
                }

                removeTime.EndSample();
                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);
                CacheEntry[] filteredEntries = null;

               

            }
            catch (OperationCanceledException ex)
            {
                _context.NCacheLog.Error("Cache.Delete()", ex.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Delete()", inner.ToString());
                throw new OperationFailedException("Delete operation failed. Error : " + inner.Message, inner);
            }
        }


#endregion

#region	/                 --- GetFilteredEvents ---

        public List<Event> GetFilteredEvents(string clientID, Hashtable events, EventStatus registeredEventStatus)
        {
            if (_context.PersistenceMgr != null)
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.ClusterRead, null))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                if (_context.PersistenceMgr.HasCompleteData())
                    return _context.PersistenceMgr.GetFilteredEventsList(clientID, events, registeredEventStatus);
                else
                {
                    return _context.CacheImpl.GetFilteredEvents(clientID, events, registeredEventStatus);
                }
            }
            return null;
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
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.ClusterRead, null))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                return new Alachisoft.NCache.Caching.Util.CacheEnumerator(_context.SerializationContext,
                    _context.CacheImpl.GetEnumerator());
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
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkWrite, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

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
        /// Fired when an item is added to the cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        void ICacheEventsListener.OnItemAdded(object key, OperationContext operationContext, EventContext eventContext)
        {
            if (_itemAdded != null)
            {
                key = SerializationUtil.CompactSerialize(key, _context.SerializationContext);
                Delegate[] dltList = _itemAdded.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    ItemAddedCallback subscriber = (ItemAddedCallback)dltList[i];

                    try
                    {
                        //(identified after an issue reported by GMO): No need to fire eventys Asynchronously it was required when we used remoting handle on Cache but
                        //it is not required anymore so firing events synchronously.] 
                        subscriber(key, eventContext);
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        _context.NCacheLog.Error("Cache.OnItemAdded()", e.ToString());
                        _itemAdded -= subscriber;
                    }
                    catch (Exception e)
                    {
                        _context.NCacheLog.Error("Cache.OnItemAdded", e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// This callback is called by .Net framework after asynchronous call for 
        /// OnItemAdded has ended. 
        /// </summary>
        /// <param name="ar"></param>
        internal void AddAsyncCallbackHandler(IAsyncResult ar)
        {
            ItemAddedCallback subscribber = (ItemAddedCallback)ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Client is dead, so remove from the event handler list
                _context.NCacheLog.Error("Cache.AddAsyncCallbackHandler", ex.ToString());
                lock (_itemAdded)
                {
                    _itemAdded -= subscribber;
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.AddAsyncCallbackHandler", e.ToString());
            }
        }

        /// <summary>
        /// handler for item updated event.
        /// </summary>
        /// <param name="key">key of the Item to be added</param>
        void ICacheEventsListener.OnItemUpdated(object key, OperationContext operationContext, EventContext eventContext)
        {
            if (_itemUpdated != null)
            {
                key = SerializationUtil.CompactSerialize(key, _context.SerializationContext);
                Delegate[] dltList = _itemUpdated.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    ItemUpdatedCallback subscriber = (ItemUpdatedCallback)dltList[i];
                    try
                    {
                        //[(identified after an issue reported by GMO): No need to fire eventys Asynchronously it was required when we used remoting handle on Cache but
                        //it is not required anymore so firing events synchronously.] 
                        subscriber(key, eventContext);
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        _context.NCacheLog.Error("Cache.OnItemUpdated()", e.ToString());
                        _itemUpdated -= subscriber;
                    }
                    catch (Exception e)
                    {
                        _context.NCacheLog.Error("Cache.OnItemUpdated", e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// This callback is called by .Net framework after asynchronous call for 
        /// OnItemUpdated has ended. 
        /// </summary>
        /// <param name="ar"></param>
        internal void UpdateAsyncCallbackHandler(IAsyncResult ar)
        {
            ItemUpdatedCallback subscribber = (ItemUpdatedCallback)ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Client is dead, so remove from the event handler list
                _context.NCacheLog.Error("Cache.UpdateAsyncCallbackHandler", ex.ToString());
                lock (_itemUpdated)
                {
                    _itemUpdated -= subscribber;
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.UpdateAsyncCallbackHandler", e.ToString());
            }
        }

        /// <summary>
        /// Fired when an item is removed from the cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="value">item itself</param>
        /// <param name="reason">reason the item was removed</param>
        public void OnItemRemoved(object key, object value, ItemRemoveReason reason, OperationContext operationContext,
            EventContext eventContext)
        {
            object data = null;
            if (value != null)
            {
                data = ((CacheEntry)value).Value; 
            }
            key = SerializationUtil.CompactSerialize(key, _context.SerializationContext);

            if (_itemRemoved != null)
            {
                Delegate[] dltList = _itemRemoved.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    ItemRemovedCallback subscriber = (ItemRemovedCallback)dltList[i];
                    try
                    {
                        //[ (identified after an issue reported by GMO): No need to fire eventys Asynchronously it was required when we used remoting handle on Cache but
                        //it is not required anymore so firing events synchronously.]
                        BitSet flag = null;
                        if (eventContext != null && eventContext.Item != null && eventContext.Item.Flags != null)
                        {
                            flag = eventContext.Item.Flags;
                        }
                        subscriber(key, data, reason, flag, eventContext);
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        _context.NCacheLog.Error("Cache.OnItemUpdated()", e.ToString());
                        _itemRemoved -= subscriber;
                    }
                    catch (Exception e)
                    {
                        _context.NCacheLog.Error("Cache.OnItemUpdated", e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// This callback is called by .Net framework after asynchronous call for 
        /// OnItemRemoved has ended. 
        /// </summary>
        /// <param name="ar"></param>
        internal void RemoveAsyncCallbackHandler(IAsyncResult ar)
        {
            ItemRemovedCallback subscribber = (ItemRemovedCallback)ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Client is dead, so remove from the event handler list
                _context.NCacheLog.Error("Cache.RemoveAsyncCallbackHandler", ex.ToString());
                lock (_itemRemoved)
                {
                    _itemRemoved -= subscribber;
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.RemoveAsyncCallbackHandler", e.ToString());
            }
        }

   

        /// <summary>
        /// Fired when multiple items are removed from the cache.
        /// </summary>
        public void OnItemsRemoved(object[] keys, object[] value, ItemRemoveReason reason,
            OperationContext operationContext, EventContext[] eventContext)
        {
            try
            {
                if (_itemRemoved != null)
                {
                    for (int i = 0; i < keys.Length; i++)
                    {
                        OnItemRemoved(keys[i], /*value[i]*/null, reason, operationContext, eventContext[i]);
                    }
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.OnItemsRemoved()", e.ToString());
            }
            catch
            {
            }
        }

        /// <summary>
        /// Fire when the cache is cleared.
        /// </summary>
        void ICacheEventsListener.OnCacheCleared(OperationContext operationContext, EventContext eventContext)
        {
            if (_cacheCleared != null)
            {
                Delegate[] dltList = _cacheCleared.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    CacheClearedCallback subscriber = (CacheClearedCallback)dltList[i];
                    try
                    {
                        //[(identified after an issue reported by GMO): No need to fire eventys Asynchronously it was required when we used remoting handle on Cache but
                        //it is not required anymore so firing events synchronously.] 

                        subscriber(eventContext);
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        _context.NCacheLog.Error("Cache.OnCacheCleared()", e.ToString());
                        _cacheCleared -= subscriber;
                    }
                    catch (Exception e)
                    {
                        _context.NCacheLog.Error("Cache.OnCacheCleared", e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// This callback is called by .Net framework after asynchronous call for 
        /// OnCacheCleared has ended. 
        /// </summary>
        /// <param name="ar"></param>
        internal void CacheClearAsyncCallbackHandler(IAsyncResult ar)
        {
            CacheClearedCallback subscribber = (CacheClearedCallback)ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Client is dead, so remove from the event handler list
                _context.NCacheLog.Error("Cache.CacheClearAsyncCallbackHandler", ex.ToString());
                lock (_cacheCleared)
                {
                    _cacheCleared -= subscribber;
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.CacheClearAsyncCallbackHandler", e.ToString());
            }
        }

        void ICacheEventsListener.OnCustomEvent(object notifId, object data, OperationContext operationContext,
            EventContext eventContext)
        {
            if (_cusotmNotif != null)
            {
                Delegate[] dltList = _cusotmNotif.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    CustomNotificationCallback subscriber = (CustomNotificationCallback)dltList[i];
                    try
                    {
#if !NETCORE
                        subscriber.BeginInvoke(notifId, data, eventContext, new System.AsyncCallback(CustomEventAsyncCallbackHandler), subscriber);
#elif NETCORE
                        //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                        TaskFactory factory = new TaskFactory();
                        System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(notifId, data, eventContext));
#endif
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        _context.NCacheLog.Error("Cache.OnCustomEvent()", e.ToString());
                        _cusotmNotif -= subscriber;
                    }
                    catch (Exception e)
                    {
                        _context.NCacheLog.Error("Cache.OnCustomEvent", e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// This callback is called by .Net framework after asynchronous call for 
        /// OnCustomEvent has ended. 
        /// </summary>
        /// <param name="ar"></param>
        internal void CustomEventAsyncCallbackHandler(IAsyncResult ar)
        {
            CustomNotificationCallback subscribber = (CustomNotificationCallback)ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Client is dead, so remove from the event handler list
                _context.NCacheLog.Error("Cache.CustomEventAsyncCallbackHandler", ex.ToString());
                lock (_cusotmNotif)
                {
                    _cusotmNotif -= subscribber;
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.CustomEventAsyncCallbackHandler", e.ToString());
            }
        }

        /// <summary>
        /// Fired when an item is removed from the cache having CacheItemRemoveCallback.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="value">CallbackEntry containing the callback and actual item</param>
        /// <param name="reason">reason the item was removed</param>
        void ICacheEventsListener.OnCustomRemoveCallback(object key, object value, ItemRemoveReason reason,
            OperationContext operationContext, EventContext eventContext)
        {
            Notifications notification = value as Notifications;

            ArrayList removeCallbacklist =
                eventContext.GetValueByField(EventContextFieldName.ItemRemoveCallbackList) as ArrayList;

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
#if !NETCORE
                                    subscriber.BeginInvoke(key, new object[] { null, cbInfo }, reason, null, eventContext,
                                        new System.AsyncCallback(CustomRemoveAsyncCallbackHandler), subscriber);
#elif NETCORE
                                    //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                                    TaskFactory factory = new TaskFactory();
                                    System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(key,new object[] { null, cbInfo },reason, null, eventContext));
#endif
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
        void ICacheEventsListener.OnCustomUpdateCallback(object key, object value, OperationContext operationContext,
            EventContext eventContext)
        {
            ArrayList updateListeners = value as ArrayList;

            if (updateListeners != null && updateListeners.Count > 0)
            {
                updateListeners = updateListeners.Clone() as ArrayList;
                foreach (CallbackInfo cbInfo in updateListeners)
                {
                    if (cbInfo.CallbackType == CallbackType.PullBasedCallback)
                        continue;

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
#if !NETCORE
                                    subscriber.BeginInvoke(key, cbInfo, eventContext,
                                        new System.AsyncCallback(CustomUpdateAsyncCallbackHandler), subscriber);
#elif NETCORE
                                    //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                                    TaskFactory factory = new TaskFactory();
                                    System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(key, cbInfo, eventContext));
#endif
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

#if !CLIENT && !DEVELOPMENT

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
#if !NETCORE
                    subscriber.BeginInvoke(newHashmap, null, new AsyncCallback(HashmapChangedAsyncCallbackHandler), subscriber);
#elif NETCORE
                           //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                        TaskFactory factory = new TaskFactory();
                        System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(newHashmap, null));
#endif
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
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operationCode"></param>
        /// <param name="result"></param>
        /// <param name="notification"></param>
        void ICacheEventsListener.OnWriteBehindOperationCompletedCallback(OpCode operationCode, object result,
            Notifications notification)
        {
            if (notification.WriteBehindOperationCompletedCallback == null) return;
            if (_dataSourceUpdated == null) return;

            Delegate[] dlgList = _dataSourceUpdated.GetInvocationList();
            for (int i = dlgList.Length - 1; i >= 0; i--)
            {
                DataSourceUpdatedCallback subscriber = (DataSourceUpdatedCallback)dlgList[i];
                try
                {
#if !NETCORE
                    subscriber.BeginInvoke(result, notification, operationCode,
                        new AsyncCallback(DSUpdateEventAsyncCallbackHandler), subscriber);
#elif NETCORE
                    //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                    TaskFactory factory = new TaskFactory();
                    System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(result, notification, operationCode));
#endif
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    _context.NCacheLog.Error("Cache.OnWriteBehindOperationCompletedCallback", ex.ToString());
                    _dataSourceUpdated -= subscriber;
                }
                catch (Exception ex)
                {
                    _context.NCacheLog.Error("Cache.OnWriteBehindOperationCompletedCallback", ex.ToString());
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        internal void DSUpdateEventAsyncCallbackHandler(IAsyncResult ar)
        {
            DataSourceUpdatedCallback subscriber = (DataSourceUpdatedCallback)ar.AsyncState;
            try
            {
                subscriber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _context.NCacheLog.Error("Cache.DSUpdateEventAsyncCallbackHandler", ex.ToString());
                _dataSourceUpdated -= subscriber;
            }
            catch (Exception ex)
            {
                _context.NCacheLog.Error("Cache.DSUpdateEventAsyncCallbackHandler", ex.ToString());
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
        /// Fired when an asynchronous opertaion is performed on cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="value">CallbackEntry containing the callback and actual item</param>
        internal void OnAsyncOperationCompleted(AsyncOpCode opCode, object result)
        {

            if (_asyncOperationCompleted != null)
            {
                Delegate[] dltList = _asyncOperationCompleted.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    AsyncOperationCompletedCallback subscriber = (AsyncOperationCompletedCallback)dltList[i];
                    try
                    {
#if !NETCORE
                        subscriber.BeginInvoke(opCode, result, null,
                            new System.AsyncCallback(AsyncOpAsyncCallbackHandler), subscriber);
#elif NETCORE
                        //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                        TaskFactory factory = new TaskFactory();
                        System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(opCode,result, null));
#endif
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        _context.NCacheLog.Error("Cache.OnAsyncOperationCompletedCallback()", e.ToString());
                        _asyncOperationCompleted -= subscriber;
                    }
                    catch (Exception e)
                    {
                        _context.NCacheLog.Error("Cache.OnAsyncOperationCompletedCallback", e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// This callback is called by .Net framework after asynchronous call for 
        /// OnCustomUpdateCallback has ended. 
        /// </summary>
        /// <param name="ar"></param>
        internal void AsyncOpAsyncCallbackHandler(IAsyncResult ar)
        {
            AsyncOperationCompletedCallback subscribber = (AsyncOperationCompletedCallback)ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Client is dead, so remove from the event handler list
                _context.NCacheLog.Error("Cache.AsyncOpAsyncCallbackHandler", ex.ToString());
                lock (_customUpdateNotif)
                {
                    _asyncOperationCompleted -= subscribber;
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.AsyncOpAsyncCallbackHandler", e.ToString());
            }
        }

        /// <summary>
        /// Clears the list of all callback listeners when disposing.
        /// </summary>
        private void ClearCallbacks()
        {
            if (_asyncOperationCompleted != null)
            {
                Delegate[] dltList = _asyncOperationCompleted.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    AsyncOperationCompletedCallback subscriber = (AsyncOperationCompletedCallback)dltList[i];
                    _asyncOperationCompleted -= subscriber;
                }
            }
            if (_cacheStopped != null)
            {
                Delegate[] dltList = _cacheStopped.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    CacheStoppedCallback subscriber = (CacheStoppedCallback)dltList[i];
                    _cacheStopped -= subscriber;
                }
            }
            if (_cacheCleared != null)
            {
                Delegate[] dltList = _cacheCleared.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    CacheClearedCallback subscriber = (CacheClearedCallback)dltList[i];
                    _cacheCleared -= subscriber;
                }
            }
            if (_itemUpdated != null)
            {
                Delegate[] dltList = _itemUpdated.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    ItemUpdatedCallback subscriber = (ItemUpdatedCallback)dltList[i];
                    _itemUpdated -= subscriber;
                }
            }
            if (_itemRemoved != null)
            {
                Delegate[] dltList = _itemRemoved.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    ItemRemovedCallback subscriber = (ItemRemovedCallback)dltList[i];
                    _itemRemoved -= subscriber;
                }
            }
            if (_itemAdded != null)
            {
                Delegate[] dltList = _itemAdded.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    ItemAddedCallback subscriber = (ItemAddedCallback)dltList[i];
                    _itemAdded -= subscriber;
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
            if (_cusotmNotif != null)
            {
                Delegate[] dltList = _cusotmNotif.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    CustomNotificationCallback subscriber = (CustomNotificationCallback)dltList[i];
                    _cusotmNotif -= subscriber;
                }
            }
        }


        /// <summary>
        /// Fire when operation Mode change changes when 
        /// - new node joins
        /// - node leaves
        /// - manual/automatic load balance
        /// </summary>
        /// <param name="newHashmap">new hashmap</param>
        void ICacheEventsListener.OnOperationModeChanged(OperationMode mode)
        {
            if (_operationModeChange == null) return;
            Delegate[] dlgList = _operationModeChange.GetInvocationList();

            foreach (OperationModeChangedCallback subscriber in dlgList)
            {
                try
                {
#if !NETCORE
                    subscriber.BeginInvoke(mode, new AsyncCallback(OperationModeAsyncCallbackHandler), subscriber);
#elif NETCORE
                        TaskFactory factory = new TaskFactory();
                        System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(mode));
#endif
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    _context.NCacheLog.Error("Cache.OnOperationModeChnaged", ex.ToString());
                    _operationModeChange -= subscriber;
                }
                catch (Exception ex)
                {
                    _context.NCacheLog.Error("Cache.OnOperationModeChnaged", ex.ToString());
                }

            }
        }

        /// <summary>
        /// Called when OnHashmapChanged is invoked and processed.
        /// </summary>
        /// <param name="result">result</param>
        internal void OperationModeAsyncCallbackHandler(IAsyncResult result)
        {
            OperationModeChangedCallback subscriber = (OperationModeChangedCallback)result.AsyncState;
            try
            {
                subscriber.EndInvoke(result);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _context.NCacheLog.Error("Cache.OperationModeChangeAsyncCallbackHandler", ex.ToString());
                _operationModeChange -= subscriber;
            }
            catch (Exception ex)
            {
                _context.NCacheLog.Error("Cache.OperationModeChangedAsyncCallbackHandler", ex.ToString());
            }
        }


#endregion

        

#region /               --- CacheImpl Calls for Cascading Dependnecies ---          /

        internal CacheInsResultWithEntry CascadedInsert(object key, CacheEntry entry, bool notify, object lockId,
            ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            try
            {
                object block = null;
                bool isNoBlock = false;
                block = operationContext.GetValueByField(OperationContextFieldName.NoGracefulBlock);
                if (block != null)
                    isNoBlock = (bool)block;

                if (!isNoBlock)
                {

                    if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                    {
                        if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
                            _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                                BlockInterval * 1000);
                    }
                }

                CacheInsResultWithEntry result = _context.CacheImpl.Insert(key, entry, notify, lockId, version, accessType,
                    operationContext);
                if (result == null) result = CacheInsResultWithEntry.CreateCacheInsResultWithEntry(_context.TransactionalPoolManager);
                if (result.Entry != null && result.Result != CacheInsResult.IncompatibleGroup)
                {
                    _context.CacheImpl.RemoveCascadingDependencies(key, result.Entry, operationContext, true);
                }

                return result;
            }
            finally
            {
            }
        }


        internal Hashtable CascadedInsert(object[] keys, CacheEntry[] cacheEntries, bool notify,
            OperationContext operationContext)
        {
            try
            {
                if (cacheEntries != null && cacheEntries.Length > 0)
                    cacheEntries.MarkInUse(NCModulesConstants.CacheCore);
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkWrite, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                Hashtable table = _context.CacheImpl.Insert(keys, cacheEntries, notify, operationContext);
                _context.CacheImpl.RemoveCascadingDependencies(table, operationContext, true);
                return table;

            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        internal CacheEntry CascadedRemove(object key, object pack, ItemRemoveReason reason, bool notify, object lockId,
            ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            object block;
            bool isNoBlock = false;
            try
            {
                block = operationContext.GetValueByField(OperationContextFieldName.NoGracefulBlock);
                if (block != null)
                    isNoBlock = (bool)block;
                operationContext?.MarkInUse(NCModulesConstants.CacheCore);
                if (!isNoBlock)
                {
                    if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                    {
                        if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
                            _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                                BlockInterval * 1000);
                    }
                }

                CacheEntry oldEntry = _context.CacheImpl.Remove(pack, reason, notify, lockId, version, accessType,
                    operationContext);

                if (oldEntry != null)
                    _context.CacheImpl.RemoveCascadingDependencies(key, oldEntry, operationContext);
                return oldEntry;
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        internal Hashtable CascadedRemove(IList keys, ItemRemoveReason reason, bool notify,
            OperationContext operationContext)
        {
            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkWrite, operationContext))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }

            Hashtable table = _context.CacheImpl.Remove(keys, reason, notify, operationContext);


            _context.CacheImpl.RemoveCascadingDependencies(table, operationContext);
            return table;
        }

        internal void RemoveCascadedDependencies(Hashtable keys, OperationContext operationContext)
        {
            _context.CacheImpl.RemoveDeleteQueryCascadingDependencies(keys, operationContext);
        }

        internal Hashtable CascadedRemove(string group, string subGroup, bool notify, OperationContext operationContext)
        {

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkWrite, operationContext))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }

            Hashtable table = _context.CacheImpl.Remove(group, subGroup, notify, operationContext);
            _context.CacheImpl.RemoveCascadingDependencies(table, operationContext);
            return table;
        }

        internal Hashtable CascadedRemove(string[] tags, TagComparisonType comaprisonType, bool notify,
            OperationContext operationContext)
        {

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkWrite, operationContext))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }

            Hashtable table = _context.CacheImpl.Remove(tags, comaprisonType, notify, operationContext);

            _context.CacheImpl.RemoveCascadingDependencies(table, operationContext);
            return table;
        }

#endregion

#region IClusterEventsListener Members

#if !CLIENT
        public int GetNumberOfClientsToDisconect()
        {           
            return 0;
        }
#endif

        public void OnMemberJoined(Alachisoft.NCache.Common.Net.Address clusterAddress,
            Alachisoft.NCache.Common.Net.Address serverAddress)
        {
#if !CLIENT
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
#endif

            if (_memberJoined != null)
            {
                Delegate[] dltList = _memberJoined.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    NodeJoinedCallback subscriber = (NodeJoinedCallback)dltList[i];
                    try
                    {
#if (CLIENT || DEVELOPMENT)
#if !NETCORE
                        subscriber.BeginInvoke(clusterAddress, serverAddress,false, null, new System.AsyncCallback(MemberJoinedAsyncCallbackHandler), subscriber);
#elif NETCORE
                        //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                        TaskFactory factory = new TaskFactory();
                        System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(clusterAddress, serverAddress, false, null));
#endif
#else
                        if (i > (clientsToDisconnect - 1))
                        {
#if !NETCORE
                            subscriber.BeginInvoke(clusterAddress, serverAddress, false, null,
                                new System.AsyncCallback(MemberJoinedAsyncCallbackHandler), subscriber);
#elif NETCORE
                            TaskFactory factory = new TaskFactory();
                            System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(clusterAddress, serverAddress, false, null));
#endif
                        }
                        else
                        {
#if !NETCORE
                            subscriber.BeginInvoke(clusterAddress, serverAddress, true, null,
                                new System.AsyncCallback(MemberJoinedAsyncCallbackHandler), subscriber);
#elif NETCORE
                            TaskFactory factory = new TaskFactory();
                            System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(clusterAddress, serverAddress, true, null));
#endif
                        }
#endif
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

        public void OnMemberLeft(Alachisoft.NCache.Common.Net.Address clusterAddress,
            Alachisoft.NCache.Common.Net.Address serverAddress)
        {
            if (_memberLeft != null)
            {
                Delegate[] dltList = _memberLeft.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    NodeLeftCallback subscriber = (NodeLeftCallback)dltList[i];
                    try
                    {
#if !NETCORE
                        subscriber.BeginInvoke(clusterAddress, serverAddress, null,
                            new System.AsyncCallback(MemberLeftAsyncCallbackHandler), subscriber);
#elif NETCORE
                        //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                        TaskFactory factory = new TaskFactory();
                        System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(clusterAddress, serverAddress, null));
#endif
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
        
        public void RegisterKeyNotificationCallback(string key, CallbackInfo updateCallback, CallbackInfo removeCallback,
            OperationContext operationContext)
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
                if (inner.IsTracable)
                    _context.NCacheLog.Error("Cache.RegisterKeyNotificationCallback() ", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.RegisterKeyNotificationCallback() ", inner.ToString());
                throw new OperationFailedException("RegisterKeyNotification failed. Error : " + inner.Message, inner);
            }
        }

        public void RegisterKeyNotificationCallback(string[] keys, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        {
            if (!IsRunning) return;
            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("Keys count can not be zero");
            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            try
            {
                _context.CacheImpl.RegisterKeyNotification(keys, updateCallback, removeCallback, operationContext);
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.RegisterKeyNotificationCallback()", inner.ToString());
                throw;
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable)
                    _context.NCacheLog.Error("Cache.RegisterKeyNotificationCallback() ", inner.ToString());
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
       
        public void UnregisterKeyNotificationCallback(string key, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
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
                if (inner.IsTracable)
                    _context.NCacheLog.Error("Cache.UnregisterKeyNotificationCallback() ", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.UnregisterKeyNotificationCallback()", inner.ToString());
                throw new OperationFailedException("UnregisterKeyNotification failed. Error : " + inner.Message, inner);
            }

        }

        public void UnregisterKeyNotificationCallback(string[] keys, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        {
            if (!IsRunning) return;
            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("Keys count can not be zero");
            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            try
            {
                _context.CacheImpl.UnregisterKeyNotification(keys, updateCallback, removeCallback, operationContext);
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.UnregisterKeyNotificationCallback()", inner.ToString());
                throw;
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable)
                    _context.NCacheLog.Error("Cache.UnregisterKeyNotificationCallback() ", inner.ToString());
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

        public Exception CanApplyHotConfig(long size)
        {
            if (this._context.CacheImpl != null && !this._context.CacheImpl.CanChangeCacheSize(size))
                return new Exception("You need to remove some data from cache before applying the new size");
            return null;
        }

        public bool IsHotApplyFeasible(long size)
        {
            return _context.CacheImpl != null && !_context.CacheImpl.CanChangeCacheSize(size);
                
        }

       

        /// <summary>
        /// The method will send updated compact types config to all connected clients
        /// </summary>
        /// <returns></returns>
        private Hashtable GetUpdatedCompactTypesConfig()
        {
            return Context._cmptKnownTypesforNet;
        }


#region /              --- Manual Load Balancing ---           /

        public void BalanceDataLoad()
        {
            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.ClusterRead, null))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }
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





#region /           --- Stream Operations ---                      /

        public string OpenStream(string key, StreamModes mode, string group, string subGroup, ExpirationHint expHint,
            EvictionHint evictionHint, OperationContext operationContext)
        {
            string lockHandle = Guid.NewGuid().ToString() + DateTime.Now.Ticks;
            return OpenStream(key, lockHandle, mode, group, subGroup, expHint, evictionHint, operationContext);
        }

        public string OpenStream(string key, string lockHandle, StreamModes mode, string group, string subGroup,
            ExpirationHint expHint, EvictionHint evictionHint, OperationContext operationContext)
        {
            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }

            if (_context.CacheImpl.OpenStream(key, lockHandle, mode, group, subGroup, expHint, evictionHint,
                operationContext))
                return lockHandle;
            else
                return null;
        }

        public void CloseStream(string key, string lockHandle, OperationContext operationContext)
        {
            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }
            _context.CacheImpl.CloseStream(key, lockHandle, operationContext);
        }

        public int ReadFromStream(ref VirtualArray vBuffer, string key, string lockHandle, int offset, int length,
            OperationContext operationContext)
        {
            int bytesRead = 0;
            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }


                _context.PerfStatsColl.IncrementGetPerSecStats();

                bytesRead = _context.CacheImpl.ReadFromStream(ref vBuffer, key, lockHandle, offset, length,
                    operationContext);
            }
            catch (StreamNotFoundException)
            {

                _context.PerfStatsColl.IncrementMissPerSecStats();

                throw;
            }

            _context.PerfStatsColl.IncrementHitsRatioPerSecStats();

            return bytesRead;
        }

        public void WriteToStream(string key, string lockHandle, VirtualArray vBuffer, int srcOffset, int dstOffset,
            int length, OperationContext operationContext)
        {
            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                _context.CacheImpl.WriteToStream(key, lockHandle, vBuffer, srcOffset, dstOffset, length,
                    operationContext);

                _context.PerfStatsColl.IncrementUpdPerSecStats();

            }
            catch (Exception)
            {
                throw;
            }
        }

        public long GetStreamLength(string key, string lockHandle, OperationContext operationContext)
        {
            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }

            return _context.CacheImpl.GetStreamLength(key, lockHandle, operationContext);
        }

#endregion




        /// <summary>
        ///  Fired when an callback attached with it.
        /// </summary>
        /// <param name="taskID"></param>
        /// <param name="value"></param>
        /// <param name="operationContext"></param>
        /// <param name="eventContext"></param>
        /// 

        public void OnTaskCallback(string taskID, Object value, OperationContext operationContext,
            EventContext eventContext)
        {
            
        }


        public bool IsServerNodeIp(Address clientAddress) 
        {

            return _context.CacheImpl.IsServerNodeIp(clientAddress);
        }

       
        public Common.DataStructures.RequestStatus GetClientRequestStatus(string clientId, long requestId,
            long commandId, string intendedServer)
        {
            if (!this._context.IsClusteredImpl || _inProc) return null;
#if !CLIENT
            ArrayList nodes = ((ClusterCacheBase)this._context.CacheImpl)._stats.Nodes;
            foreach (NodeInfo node in nodes)
            {
                if (node.RendererAddress != null && node.RendererAddress.IpAddress.ToString().Equals(intendedServer))
                {
                    return _context.CacheImpl.GetClientRequestStatus(clientId, requestId, commandId, node.Address);
                }
            }
#endif
            return null;
        }

        internal void PollReqAsyncCallbackHandler(IAsyncResult ar)
        {
            PollRequestCallback subscribber = (PollRequestCallback)ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Client is dead, so remove from the event handler list
                _context.NCacheLog.Error("Cache.PollReqAsyncCallbackHandler", ex.ToString());
                lock (_pollRequestNotif)
                {
                    _pollRequestNotif -= subscribber;
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("Cache.CustomRemoveAsyncCallbackHandler", e.ToString());
            }
        }

        void ICacheEventsListener.OnPollNotify(string clientId, short callbackId, Caching.Events.EventTypeInternal eventType)
        {
            if (_connectedClients != null && _connectedClients.Contains(clientId))
            {
                if (_pollRequestNotif != null)
                {
                    Delegate[] dltList = _pollRequestNotif.GetInvocationList();
                    for (int i = dltList.Length - 1; i >= 0; i--)
                    {
                        PollRequestCallback subscriber = (PollRequestCallback)dltList[i];
                        try
                        {
                            subscriber(clientId, callbackId, eventType);
                        }
                        catch (System.Net.Sockets.SocketException e)
                        {
                            _context.NCacheLog.Error("Cache.OnPollNotify", e.ToString());
                            _pollRequestNotif -= subscriber;
                        }
                        catch (Exception e)
                        {
                            _context.NCacheLog.Error("Cache.OnPollNotify", e.ToString());
                        }
                    }
                }
            }
        }
        public PollingResult Poll(OperationContext context)
        {
            return _context.CacheImpl.Poll(context);
        }

        public void RegisterPollingNotification(short callbackId, OperationContext context)
        {
            _context.CacheImpl.RegisterPollingNotification(callbackId, context);
        }

        public void UnRegisterClientActivityCallback(string clientId)
        {
            _context.CacheImpl.UnregisterClientActivityCallback(clientId);
        }

        public IEnumerable<ClientInfo> GetConnectedClientInfos()
        {
            return _context.CacheImpl.GetConnectedClientsInfo();
        }

        public void Touch(List<string> keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.Tounch", "");
            if (keys == null) throw new ArgumentNullException("keys");

            if (!IsRunning) return;

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkRead))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                _context.CacheImpl.Touch(keys, operationContext);
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Touch()", inner.ToString());
                throw new OperationFailedException("Contains operation failed. Error : " + inner.Message, inner);
            }
        }

        public void SetClusterInactive(string reason)
        {
            if (Context != null && Context.CacheImpl != null) Context.CacheImpl.SetClusterInactive(reason);

        }

#region ---------------------------- Messaging --------------------------------- 

        public bool TopicOpertion(TopicOperation operation, OperationContext operationContext)
        {
            if (!IsRunning) return false;
            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.AtomicRead, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }
                return _context.CacheImpl.TopicOperation(operation, operationContext);
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled)
                        _context.NCacheLog.Error("Topic operation failed. Error: " + ex);
                }
                throw;
            }
            catch (StateTransferInProgressException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled)
                    _context.NCacheLog.Error("Topic operation failed. Error: " + ex);
                throw new OperationFailedException("Topic operation failed. Error: " + ex.Message, ex);
            }
        }

        public void PublishMessage(string messageId, object payLoad, long creationTime, long expirationTime, Hashtable metaData, BitSet flagMap, OperationContext operationContext)
        {
            if (messageId == null) throw new ArgumentNullException("messageId");
            if (payLoad == null) throw new ArgumentNullException("payload");

            // Cache has possibly expired so do default.
            if (!IsRunning)
                return;
            BitSet bitset = null;
            try
            {
                Messaging.Message message = new Messaging.Message(messageId);
                bitset = BitSet.CreateAndMarkInUse(Context.FakeObjectPool,NCModulesConstants.CacheCore);
                message.PayLoad = payLoad;
                message.FlagMap = bitset;
                message.FlagMap.Data |= flagMap.Data;
                message.CreationTime = new DateTime(creationTime, DateTimeKind.Utc);

                message.MessageMetaData = new MessageMetaData(messageId);
                message.MessageMetaData.SubscriptionType = Common.Enum.SubscriptionType.Subscriber;

                string topicName = metaData[TopicConstant.TopicName] as string;
                topicName = topicName.Split(TopicConstant.TopicSeperator)[1];
                message.MessageMetaData.TopicName = topicName;
                message.MessageMetaData.IsNotify = bool.Parse(metaData[TopicConstant.NotifyOption] as string);
                message.MessageMetaData.DeliveryOption = (DeliveryOption)(int.Parse(metaData[TopicConstant.DeliveryOption] as string));
                message.MessageMetaData.ExpirationTime = expirationTime;
                message.MessageMetaData.TimeToLive = AppUtil.DiffSeconds(DateTime.Now) + new TimeSpan(expirationTime).TotalSeconds;


                object block = operationContext.GetValueByField(OperationContextFieldName.NoGracefulBlock);
                if (block != null && !(bool)block)
                {
                    if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                    {
                        if (!_context.CacheImpl.IsOperationAllowed(messageId, AllowedOperationType.AtomicWrite))
                            _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                                BlockInterval * 1000);
                    }
                }

                _context.CacheImpl.StoreMessage(topicName, message, operationContext);

                if (_context.PerfStatsColl != null)
                    _context.PerfStatsColl.IncrementMessagePublishedPerSec();
            }
            finally
            {
                bitset?.MarkFree(NCModulesConstants.CacheCore);
            }
        }

        public MessageResponse GetAssignedMessages(SubscriptionInfo subscriptionInfo, OperationContext operationContext)
        {
            if (!IsRunning) return null;

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.AtomicRead, operationContext))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }
            
            try
            {
                return _context.CacheImpl.GetAssignedMessage(subscriptionInfo, operationContext);
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable)
                    _context.NCacheLog.Error("Cache.GetMessage()", "Get Message operation failed. Error : " + inner.ToString());
                throw;
            }
            catch (StateTransferInProgressException)
            {
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.GetMessage()", "Get Message operation failed. Error : " + inner.ToString());
                throw new OperationFailedException(ErrorCodes.PubSub.GET_MESSAGE_OPERATION_FAILED,ErrorMessages.GetErrorMessage(ErrorCodes.PubSub.GET_MESSAGE_OPERATION_FAILED,inner.Message),inner);
            }
        }

        public Dictionary<string, TopicStats> GetTopicsStats(bool defaultTopicStats = false)
        {
            return _context.CacheImpl.GetTopicsStats(defaultTopicStats);
        }

        public double GetCounterValue(string counterName , bool replica, string category)
        {
            double value = 0.0;

            if(category == "Process")
            {
                if (counterName.Equals(CustomCounterNames.MemoryUsage, StringComparison.InvariantCultureIgnoreCase))
                {
                    value = Process.GetCurrentProcess().WorkingSet64;
                    return value;
                }
            }
            
            if (replica)
            {
                if (_context.PerfStatsColl != null)
                    if (counterName.Equals(CustomCounterNames.Count, StringComparison.InvariantCultureIgnoreCase) || counterName.Equals(CustomCounterNames.CacheSize, StringComparison.InvariantCultureIgnoreCase) || counterName.Equals(CounterNames.EvictionIndexSize, StringComparison.InvariantCultureIgnoreCase) || counterName.Equals(CounterNames.ExpirationIndexSize, StringComparison.InvariantCultureIgnoreCase))
                        value = _context.CacheImpl.GetReplicaCounters(counterName);
            }
            else
            {
                switch (counterName)
                {
                    case CustomCounterNames.RequestsPerSec:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.ResponsesPerSec:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.ClientBytesSentPerSec:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.ClientBytesReceiedPerSec:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.MsecPerCacheOperation:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.MsecPerCacheOperationBase:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.ResponseQueueCount:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.ResponseQueueSize:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.EventQueueCount:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.RequestLogPerSecond:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.RequestLogSize:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.ConnectedClients:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.MsecPerAddBulkAvg:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.MsecPerGetBulkAvg:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.MsecPerUpdBulkAvg:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.MsecPerDelBulkAvg:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.MsecPerAddBulkBase:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.MsecPerGetBulkBase:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.MsecPerUpdBulkBase:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    case CustomCounterNames.MsecPerDelBulkBase:
                        if (_context.Render != null)
                            value = _context.Render.GetCounterValue(counterName);
                        break;
                    default:                       
                        if (_context.PerfStatsColl != null)
                            value = _context.PerfStatsColl.GetCounterValue(counterName);
                        break;
                }
            }
            return value;
        }

        public void AcknowledgeMessageReceipt(string clientId, IDictionary<string, IList<string>> topicWiseMessageIds, OperationContext operationContext)
        {
            if (!IsRunning) return;
            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkWrite, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }
                _context.CacheImpl.AcknowledgeMessageReceipt(clientId, topicWiseMessageIds, operationContext);
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled)
                        _context.NCacheLog.Error("AcknowledgeMessageReceipt operation failed. Error: " + ex);
                }
                throw;
            }
            catch (StateTransferInProgressException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled)
                    _context.NCacheLog.Error("AcknowledgeMessageReceipt operation failed. Error: " + ex);
                throw new OperationFailedException("AcknowledgeMessageReceipt operation failed. Error: " + ex.Message, ex);
            }
        }

        public long GetMessageCount(string topicName, OperationContext operationContext)
        {
            if (topicName == null)
            {
                throw new ArgumentNullException("topicName");
            }
            if (!IsRunning)
            {
                // Cache has possibly expired so return default
                return 0;
            }
            return _context.CacheImpl.GetMessageCount(topicName, operationContext);
        }

#endregion

        public void LogCacnelCommand(string commandType, long requestID, string clientID)
        {
            if (_context.NCacheLog != null)
                _context.NCacheLog.CriticalInfo("Cache.CacnelExecution()", "Command : " + commandType + " Request ID : " + requestID + " has been cancelled for client : " + clientID);
        }

    

#region - [ PoolStats ] -

        public PoolStats GetPoolStats(PoolStatsRequest request)
        {
            var stats = TransactionalPoolManager.GetStats(request);
            stats.MergeStats(_context.StorePoolManager.GetStats(request));
            stats.MergeStats(_context.Render?.GetPoolStats(request));

            return stats;
        }

        #endregion

        #region ---------------------------- FeatureDataCollection -------------------------------
        public Dictionary<string, Common.FeatureUsageData.Feature> GetCacheFeaturesUsageReport()
        {
            return FeatureUsageCollector.Instance.GetFeatureReport();
        }

        public ClientProfileDom GetClientProfileReport()
        {
            return _context.Render.GetClientProfile();
        }
        #endregion
    }
}
