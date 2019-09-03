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
using System.Net;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.DatasourceProviders;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.CacheSynchronization;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Caching.CacheLoader;
using Alachisoft.NCache.Caching.Queries;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Licensing;
#if ENTERPRISE
using Alachisoft.NCache.Caching.Bridge;
using Alachisoft.NCache.Caching.Topologies.Clustered;
using Alachisoft.NCache.BridgeClient;
#endif
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Propagator;
using Alachisoft.NCache.Caching.EmailAlertPropagator;
using Alachisoft.NCache.Caching.AlertsPropagators;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Caching.Enumeration;
using Alachisoft.NCache.Security;
using Alachisoft.NCache.JNIBridge.JavaWrappers.Serialization;
using Alachisoft.NCache.Persistence;
using EnumerationPointer = Alachisoft.NCache.Common.DataStructures.EnumerationPointer;
using Exception = System.Exception;
using QueryResultSet = Alachisoft.NCache.Caching.Queries.QueryResultSet;
using Alachisoft.NCache.Runtime.DatasourceProviders;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.MapReduce;
using Alachisoft.NCache.MapReduce.Notifications;
using Alachisoft.NCache.Runtime.Processor;
using Alachisoft.NCache.Processor;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Runtime.Caching;
using ClientInfo = Alachisoft.NCache.Runtime.Caching.ClientInfo;
using Alachisoft.NCache.Caching.Messaging;
using System.Threading.Tasks;
using System.Runtime;
using Alachisoft.NCache.Common.Configuration.AttributeNames;
using Alachisoft.NCache.Common.Topologies.Clustered;
using Alachisoft.NCache.Common.Resources;

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// A class to contain cache creation parameters.
    /// </summary>

    /// <summary>
    /// opcodes to indentify the async operations.
    /// </summary>
    [Serializable]
    public enum AsyncOpCode
    {
        Add,
        Update,
        Remove,
        Clear
    }

    /// <summary>
    /// opcode to identify the type of operation
    /// </summary>

    public enum OpCode
    {
        Add,
        Update,
        Remove,
        Clear
    }

    /// <summary>
    /// Enumeration that defines the update operation on cache can update data source
    /// </summary>
    public enum DataSourceUpdateOptions
    {
        /// <summary>
        /// Do not update data source
        /// </summary>
        None = 0,

        /// <summary>
        /// Update data source synchronously
        /// </summary>
        WriteThru = 1,

        /// <summary>
        /// Update data source asynchronously
        /// </summary>
        WriteBehind = 2
    }

    /// <summary>
    /// opcodes to indentify the async operations.
    /// </summary>
    public enum AsyncOpResult
    {
        Success,
        Failure
    }

    /// <summary>
    /// Delegates for synchronous cache operations. 
    /// </summary>
    public delegate void ItemAddedCallback(object key, EventContext eventContext /*, object value*/);

    public delegate void ItemRemovedCallback(
        object key, object value, ItemRemoveReason reason, BitSet Flag, EventContext eventContext);

    public delegate void ItemUpdatedCallback(object key, EventContext eventContext /*, object value*/);

    public delegate void CacheClearedCallback(EventContext eventContext);

    public delegate void CustomNotificationCallback(object notifId, object data, EventContext eventContext);

    public delegate void CustomRemoveCallback(
        object key, object value, ItemRemoveReason reason, BitSet Flag, EventContext eventContext);

    public delegate void CustomUpdateCallback(object key, object value, EventContext eventContext);

    public delegate void CacheStoppedCallback(string cacheid, EventContext eventContext);

    public delegate void CacheBecomeActiveCallback(string cacheId, EventContext eventContext);

    public delegate void ConfigurationModified(HotConfig hotConfig);

    public delegate void CompactTypeModifiedCallback(Hashtable updatedCompactTypes, EventContext eventContext);

    public delegate void ActiveQueryCallback(
        object key, QueryChangeType changeType, List<CQCallbackInfo> activeQueries, EventContext eventContext);

    public delegate void TaskCallbackListener(string taskId, TaskCallbackInfo callbackInfo, EventContext eventcontext);


    /// <summary>
    /// Delegate for asynchronous cache operations.
    /// These are used to inform the user about async operations success or failure.
    /// In case of failure user will result will contain the exception.
    /// </summary>
    public delegate void AsyncOperationCompletedCallback(object opCode, object result, EventContext eventContext);

    /// <summary>
    /// Delegate for write behind operations
    /// </summary>
    /// <param name="operationCode">operation type</param>
    /// <param name="result">result</param>
    public delegate void DataSourceUpdatedCallback(object result, CallbackEntry cbEntry, OpCode operationCode);

#if !DEVELOPMENT
    /// <summary>
    /// Delegate for propagating hashmap changes to client
    /// </summary>
    /// <param name="newHashmap">new hashmap</param>
    public delegate void HashmapChangedCallback(NewHashmap hashMap, EventContext eventContext);
#endif

    /// <summary>
    /// Delegate for propagating operation mode changes to client
    /// </summary>
    /// <param name="newHashmap">new hashmap</param>
    public delegate void OperationModeChangedCallback(OperationMode mode);

    /// <summary>
    /// Delegate for node join and node leave operations.
    /// These are used to inform the user when a node joins or leaves the cluster.
    /// </summary>
    public delegate void NodeJoinedCallback(
        object clusterAddress, object serverAddress, bool reconnect, EventContext eventContext);

    public delegate void NodeLeftCallback(object clusterAddress, object serverAddress, EventContext eventContext);


    public delegate void BlockClientActivity(string uniqueId, string serverIp, long timeoutInterval, int port);

    public delegate void UnBlockClientActivity(string uniqueId, string serverIp, int port);

    public delegate void PollRequestCallback(string clientId, short callbackId, Alachisoft.NCache.Runtime.Events.EventType eventType);



    /// <summary>
    /// The main class that is the interface of the system with the outside world. This class
    /// is remotable (MarshalByRefObject). 
    /// </summary>
    public class Cache : MarshalByRefObject, IEnumerable, ICacheEventsListener, IClusterEventsListener, IDisposable
    {

        #region	/                 --- Performance statistics collection Task ---           /

        //		/// <summary>
        //		/// The Task that monitors activation state of the product, every hour. If expired
        //		/// the cache is disposed().
        //		/// </summary>
        //		class ActivationVerifierTask : TimeScheduler.Task
        //		{
        //			/// <summary> Reference to the parent. </summary>
        //			private Cache					_parent = null;
        //
        //			/// <summary> Periodic interval </summary>
        //			private long					_interval = 1000 * 60 * 60 * 1;
        //
        //			/// <summary>
        //			/// Constructor.
        //			/// </summary>
        //			/// <param name="parent"></param>
        //			/// <param name="interval"></param>
        //			internal ActivationVerifierTask(Cache parent, long interval)
        //			{
        //				_parent = parent;
        //				_interval = interval;
        //			}
        //				
        //			/// <summary>
        //			/// Sets the cancel flag.
        //			/// </summary>
        //			public void Cancel()
        //			{
        //				lock(this) { _parent = null; }
        //			}
        //
        //			/// <summary>
        //			/// returns true if the task has completed.
        //			/// </summary>
        //			/// <returns>bool</returns>
        //			public virtual bool cancelled()
        //			{
        //				lock(this) { return _parent == null; }
        //			}
        //				
        //			/// <summary>
        //			/// tells the scheduler about next interval.
        //			/// </summary>
        //			/// <returns></returns>
        //			public virtual long nextInterval()
        //			{
        //				return _interval;
        //			}
        //					
        //			/// <summary>
        //			/// This is the main method that runs as a thread. CacheManager does all sorts of house 
        //			/// keeping tasks in that method.
        //			/// </summary>
        //			public virtual void  run()
        //			{
        //				if(_parent == null) return;
        //				try
        //				{
        //					LicenseManager.LicenseType type = LicenseManager.LicenseMode;
        //					if(type == LicenseManager.LicenseType.Expired)
        //					{
        //						LicenseManager.RaiseExpiryEvent();
        //						_parent._context.CacheImpl.Dispose();
        //						_parent._context.CacheImpl = null;
        //						Cancel();
        //					}
        //					else
        //					{
        //						bool isClusterable = (type == LicenseManager.LicenseType.ActivePerProcessor) ||
        //							(type == LicenseManager.LicenseType.InEvaluation);
        //						if(ConvHelper.IsClusteredCache(_parent._context.CacheImpl))
        //						{
        //							LicenseManager.RaiseDevWarningEvent();
        //							_parent._context.CacheImpl.Dispose();
        //							_parent._context.CacheImpl = null;
        //							Cancel();
        //						}
        //					}
        //				}
        //				catch(Exception)
        //				{
        //				}
        //			}
        //		}
        //			

        #endregion

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


        /// <summary> delegate for active query callback notifications. </summary>
        private event ActiveQueryCallback _activeQueryNotif;

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

        // public delegate void CacheBecomeActiveEvent(string cacheName);
        public delegate void CacheStartedEvent(string cacheName);

        public static CacheStoppedEvent OnCacheStopped;
        public static CacheStartedEvent OnCacheStarted;

        private event ConfigurationModified _configurationModified;
        private event CompactTypeModifiedCallback _compactTypeModified;
        private event TaskCallbackListener _taskCallback;
        private event PollRequestCallback _pollRequestNotif;


#if ENTERPRISE
        private BridgeReplicator _bridgeReplicator;
#endif

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

        //public const int Compressed = 0x02;

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


#if JAVA
        string _cacheserver = "TayzGrid";
#else
        private string _cacheserver = "NCache";
#endif
        //public string UniqueBlockingId
        //{
        //    get { return _uniqueId; }
        //    set { _uniqueId = value; }
        //}

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

        public IClusterSplitManager ClusterSplitManager { get { return _context.ClusterSplitManager; }}

        //public NewTrace CacheTrace;
        public IAlertPropagator EmailAlertPropagator;
        private SQLDependencySettings _sqlDependencySettings;
        public ICacheSecurityProvider CacheSecurityProvider;

        /// <summary> Thread to reset Instantaneous Stats after every one second.</summary>
        private Thread _instantStatsUpdateTimer;

        private bool threadRunning = true;
        private TimeSpan InstantStatsUpdateInterval = new TimeSpan(0, 0, 1);
        private bool _isBridgeTargetCache;
        private string _bridgeId;
        private static bool s_logClientEvents;

        private IDataFormatService _socketServerDataService;
        private IDataFormatService _cachingSubSystemDataService;
        public bool _isClientCache = false;
        public string _serverCacheName = "";

        //private CacheClientConnectivityChangedCallback _clientConnectivityChanged;

        /// <summary>
        /// Default constructor.
        /// </summary>
        static Cache()
        {
            MiscUtil.RegisterCompactTypes();
#if JAVA
            string tmpStr = System.Configuration.ConfigurationSettings.AppSettings["CacheServer.LogClientEvents"];
#else
            //s_logClientEvents = ServiceConfiguration.LogClientEvents;
#endif

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
                //_channels.UnregisterTcpServerChannel();
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
                        //[Ata]quite unsafe
                        //_cacheStopped(_cacheInfo.Name);

                        Delegate[] invocationList = this._cacheStopped.GetInvocationList();

                        //NCacheLog.Error(_context.CacheName, "Cache.Dispose", "Cache stopped event invocation list size = " + invocationList.Length);
                        foreach (Delegate subscriber in invocationList)
                        {
                            CacheStoppedCallback callback = (CacheStoppedCallback)subscriber;
                            try
                            {
                                //callback.BeginInvoke(this._cacheInfo.Name, null, null);                            
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

#if ENTERPRISE
                    if (_bridgeReplicator != null)
                    {
                        _bridgeReplicator.Dispose();
                        _bridgeReplicator = null;
                        _context.BridgeReplicator = null;
                    }
#endif
                    if (_context.CacheImpl != null)
                        _context.CacheImpl.StopServices();

                    if (_context.DsMgr != null)
                    {
                        _context.DsMgr.Dispose();
                        _context.DsMgr = null;
                    }

                    //if (_context.ClientDeathDetection != null)
                    //{
                    //    _context.ClientDeathDetection.Dispose();
                    //    _context.ClientDeathDetection = null;
                    //}

                    if (_context.CSLMgr != null)
                    {
                        _context.CSLMgr.Dispose();
                        _context.CSLMgr = null;
                    }

                    if (_cacheStopped != null && CacheType != null && CacheType.Equals("mirror-server")) _cacheStopped(_cacheInfo.Name, null);

                    if (_context.SyncManager != null)
                    {
                        _context.SyncManager.Dispose();
                        _context.SyncManager = null;
                    }

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

                    //GC.Collect();

                    if (disposing)
                    {
                        //PortableTypes.Dispose(_context.CacheRoot.Name);
                        GC.SuppressFinalize(this);
                    }

                    if (_context != null)
                    {
                        _context.Dispose();
                    }

                    //Dispose snaphot pool for this cache.
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


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            try
            {
                Dispose(true);
                if (EmailAlertPropagator != null)
                    EmailAlertPropagator.RaiseAlert(EventID.CacheStop, _cacheserver,
                        "\"" + Name + "\"" + " stopped successfully.");

                if (_context.EntryProcessorManager != null)
                {
                    _context.EntryProcessorManager.Dispose();
                    _context.EntryProcessorManager = null;
                }

            }
            catch (Exception exp)
            {
                if (EmailAlertPropagator != null)
                    EmailAlertPropagator.RaiseAlert(EventID.CacheStop, _cacheserver,
                        "\"" + Name + "\"" + " cannot be stopped.");
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
        /// Gets the value which indicates whether this cache is the target
        /// cache for the bridge or not. If a cache is target cache for
        /// </summary>
        public bool IsBridgeTargetCache
        {
            set { _isBridgeTargetCache = value; }
            get { return _isBridgeTargetCache; }
        }

        public string BridgeId
        {
            set { _bridgeId = value; }
            get { return _bridgeId; }
        }

        /// <summary>
        /// Returns true if the cache is running, false otherwise.
        /// </summary>
        public bool IsRunning
        {
            get { return _context.CacheImpl != null; }
        }



#if !(DEVELOPMENT || CLIENT)
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
            //get { return !(_context.CacheImpl is LocalCacheImpl); }
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

#if ENTERPRISE
        public string TargetCacheUniqueID
        {
            get
            {
                if (this._context.CacheImpl is ClusterCacheBase)
                {
                    return ((ClusterCacheBase)this._context.CacheImpl).BridgeSourceCacheId;
                }
                else
                {
                    return string.Empty;
                }
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


        public void LogBridgeCachesStatus()
        {
#if ENTERPRISE
            if (_bridgeReplicator != null)
            {
                _bridgeReplicator.LogBridgeCachesStatus();
            }
#endif
        }

        public void LogBackingSourceStatus()
        {

            if (_context != null && _context.DsMgr != null)
            {

                _context.CacheImpl.LogBackingSource();
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



        /// <summary> delegate for cusotm remove callback notifications.  </summary>
        public event ActiveQueryCallback ActiveQueryCallbackNotif
        {
            add { _activeQueryNotif += value; }
            remove { _activeQueryNotif -= value; }
        }

        /// <summary>
        /// Gets or sets the cache item at the specified key.
        /// </summary>
        public object this[object key]
        {
            get
            {
                object lockId = null;
                DateTime lockDate = DateTime.UtcNow;
                ulong version = 0;
                return
                    GetGroup(key, new BitSet(), null, null, ref version, ref lockId, ref lockDate, TimeSpan.Zero,
                        LockAccessType.IGNORE_LOCK, "",
                        new OperationContext(OperationContextFieldName.OperationType,
                            OperationContextOperationType.CacheOperation)).Value;
            }
            set { Insert(key, value); }
        }


        /// <summary> delegate for item addition notifications. </summary>
        public event ItemAddedCallback ItemAdded
        {
            add { _itemAdded += value; }
            remove { _itemAdded -= value; }
        }

        /// <summary> delegate for item updation notifications. </summary>
        public event ItemUpdatedCallback ItemUpdated
        {
            add { _itemUpdated += value; }
            remove { _itemUpdated -= value; }
        }

        /// <summary> delegate for item removal notifications. </summary>
        public event ItemRemovedCallback ItemRemoved
        {
            add { _itemRemoved += value; }
            remove { _itemRemoved -= value; }
        }

        /// <summary> delegate for cache clear notifications. </summary>
        public event CacheClearedCallback CacheCleared
        {
            add { _cacheCleared += value; }
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

        public event CompactTypeModifiedCallback CompactTypeModified
        {
            add { _compactTypeModified += value; }
            remove { _compactTypeModified -= value; }
        }


#if !DEVELOPMENT
        public event HashmapChangedCallback HashmapChanged
        {
            add { _hashmapChanged += value; }
            remove { _hashmapChanged -= value; }
        }
#endif

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




        public event TaskCallbackListener TaskCallback
        {
            add { _taskCallback += value; }
            remove { _taskCallback -= value; }
        }

        /// <summary> delegate for Sending pull requests.  </summary>
        public event PollRequestCallback PollRequestCallbackNotif
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
        /// <param name="userId">userId</param>
        /// <param name="password">password</param>
        protected internal virtual void Start(CacheRenderer renderer, string userId, string password,
            bool twoPhaseInitialization)
        {
            Start(renderer, userId, password, false, twoPhaseInitialization);
        }

        protected internal void StartPhase2()
        {
#if ENTERPRISE
            if (_context.CacheImpl is ClusterCacheBase)
                ((ClusterCacheBase)_context.CacheImpl).InitializePhase2();
#endif
        }

        /// <summary>
        /// Start the cache functionality.
        /// </summary>
        protected internal virtual void Start(CacheRenderer renderer, string userId, string password,
            bool isStartingAsMirror, bool twoPhaseInitialization)
        {
            try
            {
                if (IsRunning)
                {
                    Stop(false);
                }
                ConfigReader propReader = new PropsConfigReader(ConfigString);
                _context.Render = renderer;
                if (renderer != null)
                {
                    renderer.OnClientConnected += new CacheRenderer.ClientConnected(OnClientConnected);
                    renderer.OnClientDisconnected += new CacheRenderer.ClientDisconnected(OnClientDisconnected);
                }
                //#if !EXPRESS
                Initialize(propReader.Properties, false, userId, password, isStartingAsMirror, twoPhaseInitialization);
                //JSerializationUtil.RegisterTypeInfoMap(_context.CacheRoot.GetTypeInfoMap());

                //#else
                //            Initialize2(propReader.Properties, false, userId, password);
                //#endif
                if (EmailAlertPropagator != null)
                    EmailAlertPropagator.RaiseAlert(EventID.CacheStart, _cacheserver,
                        "\"" + Name + "\"" + " started successfully.");
            }
            catch (Exception exception)
            {
                if (EmailAlertPropagator != null)
                    EmailAlertPropagator.RaiseAlert(EventID.CacheStart, _cacheserver,
                        "\"" + Name + "\"" + " cannot be started.");
                throw;
            }
        }

        /// <summary>
        /// Fired when a client is connected with the socket server.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="cacheId"></param>
        /// <param name="clientInfo"></param>
        public void OnClientDisconnected(string client, string cacheId, long count)
        {
            if (_context.CacheImpl != null) // && cacheId.ToLower() == _cacheInfo.Name.ToLower() )
            {
                lock (_connectedClients.SyncRoot)
                {
                    _connectedClients.Remove(client);
                }

                _context.CacheImpl.ClientDisconnected(client, _inProc);

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

        public void OnClientConnected(string client, string cacheId, ClientInfo clientInfo , long count)
        {
            if (_context.CacheImpl != null) // && cacheId.ToLower() == _cacheInfo.Name.ToLower())
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
                    "Client \"" + client + "\" has connected to cache.  # Connected Clients  " + count );

            }
        }

        /// <summary>
        /// Stop the internal working of the cache.
        /// </summary>
        public virtual void Stop(bool isGracefullShutdown)
        {
            _startShutDown = DateTime.Now;
            if (isGracefullShutdown)
            {
                _shutDownStatusLatch.SetStatusBit(ShutDownStatus.SHUTDOWN_INPROGRESS, ShutDownStatus.NONE);

                //int shutdownTimeout = 180;
                //try
                //{
                //    if (System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.GracefullShutdownTimeout"] != null)
                //    { shutdownTimeout = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.GracefullShutdownTimeout"]); }
                //}
                //catch (Exception ex)
                //{
                //    _context.NCacheLog.Error("Cache.Stop", "Invalid value is assigned to NCacheServer.GracefullShutdownTimeout. Reassigning it to default value.(180 seconds)");
                //    shutdownTimeout = 180;
                //}

                //if (shutdownTimeout <= 0)
                //{
                //    _context.NCacheLog.Error("Cache.Stop", "0 or negtive value is assigned to NCacheServer.GracefullShutdownTimeout. Reassigning it to default value.(180 seconds)");
                //    shutdownTimeout = 180;
                //}

                int shutdownTimeout = 180;
                int blockTimeout = 3;

                string expMsg = GracefulTimeout.GetGracefulShutDownTimeout(ref shutdownTimeout, ref blockTimeout);
                if (expMsg != null)
                    _context.NCacheLog.CriticalInfo("Cache.GracefulShutDown", expMsg);

                _context.NCacheLog.CriticalInfo("Cache.Stop", "Graceful Shutdown Timeout: " + shutdownTimeout);

                _context.NCacheLog.CriticalInfo("Cache.Stop", "Starting Graceful Shutdown Processor...");

                lock (_shutdownMutex)
                {
                    Thread _gracefulStopThread = new Thread(new ThreadStart(ShutDownGraceful));
                    _gracefulStopThread.Name = _cacheInfo.Name + ".GraceFullShutDownProcessor";
                    _gracefulStopThread.IsBackground = true;
                    _gracefulStopThread.Start();

                    Monitor.Wait(_shutdownMutex, shutdownTimeout * 1000);
                }
                _context.NCacheLog.CriticalInfo("Cache.Stop", "Graceful Shutdown Process has ended.");
            }

            Dispose();
        }

        public virtual bool VerifyNodeShutDown(bool isGraceful)
        {
            if (isGraceful)
            {
                List<ShutDownServerInfo> servers = GetShutDownServers();

                if (servers != null)
                    return false;
            }

            return true;
        }

        public List<ShutDownServerInfo> GetShutDownServers()
        {
            return _context.CacheImpl.GetShutDownServers();
        }

#if ENTERPRISE
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

                        //serverPort = connectedPort;

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

#if ENTERPRISE
                if (!Alachisoft.NCache.Licensing.LicenseManager.Express)
                //Numan Hanif[Express] Runtime Express  Check for Non-Express
                {
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
                }
                else
                {
                    runningServers.Add(ipAddress, serverPort);

                }
#else
                 runningServers.Add(ipAddress, serverPort);
#endif

            }
            return runningServers;
        }

        /// <summary>
        /// this method makes a bridge target cache active or passive depending on the value of the flag 
        /// passed to it. If true is passed it will make it active otherwise make the cache passive
        /// and clear it.
        /// </summary>        
        public void MakeCacheActivePassive(bool makeActive)
        {
            try
            {

#if ENTERPRISE
                if (!Alachisoft.NCache.Licensing.LicenseManager.Express)
                //Numan Hanif[Express] Runtime Check for Non-Express
                {
                    _context.CacheImpl.MakeBridgeTargetCacheActive(makeActive);
                }
#endif
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.MakeCacheActivePassive()", inner.ToString());
                throw new OperationFailedException("MakeCacheActivePassive operation failed. Error: " + inner.Message,
                    inner);
            }
        }


        public void MakeCacheActiveNCManager(bool makeActive)
        {
            if (makeActive)
            {
                this._isBridgeTargetCache = false;
                this._context.IsBridgeTargetCache = false;
                this._context.CacheRoot.IsBridgeTargetCache = false;
                this._context.CacheImpl.CacheBecomeActive();
            }
            else
            {
                this._isBridgeTargetCache = true;
                this._context.IsBridgeTargetCache = true;
                this._context.CacheRoot.IsBridgeTargetCache = true;
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

#if ENTERPRISE
        public void GetActiveServer(ref string ipAddress, ref int serverPort)
        {
            string connectedIpAddress = ipAddress;
            int connectedPort = serverPort;

            if (!this._context.IsClusteredImpl) return;
            if (_inProc) return;

            ArrayList nodes = ((ClusterCacheBase)this._context.CacheImpl)._stats.Nodes;


            //int min = int.MaxValue;
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

#if ENTERPRISE
                if (_bridgeReplicator != null)
                {
                    _bridgeReplicator.WindUpTask();
                }
#endif

                if (_context.DsMgr != null && /*_context.DsMgr.IsWriteBehindEnabled &&*/
                    _context.DsMgr._writeBehindAsyncProcess != null)
                {
                    _context.DsMgr.WindUpTask();
                }

                _context.CacheImpl.WindUpReplicatorTask();

                _context.NCacheLog.CriticalInfo("Cache.ShutDownGraceful", "Windup Tasks Ended.");


                if (_context.CSLMgr != null && !_context.CSLMgr.IsCacheLoaderTaskCompleted &&
                    _context.CSLMgr.IsCacheloaderEnabled)
                {
                    _context.CacheImpl.NotifyCacheLoaderExecution();
                }


                if (_asyncProcessor != null)
                {
                    if (BlockInterval > 0)
                        _asyncProcessor.WaitForShutDown(BlockInterval);
                }
#if ENTERPRISE
                if (_bridgeReplicator != null)
                {
                    if (BlockInterval > 0)
                        _bridgeReplicator.WaitForShutDown(BlockInterval);
                }
#endif
                if (_context.DsMgr != null && /*_context.DsMgr.IsWriteBehindEnabled && */
                    _context.DsMgr._writeBehindAsyncProcess != null)
                {
                    if (BlockInterval > 0)
                        _context.DsMgr.WaitForShutDown(BlockInterval);
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
            //catch (NullReferenceException e)
            //{
            //    _context.NCacheLog.Error("Cache.ShutdownGraceful", "Graceful Shutdown is stopped. " + e.ToString());
            //}
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

        internal void Initialize(IDictionary properties, bool inProc, string userId, string password)
        {
            Initialize(properties, inProc, userId, password, false, false);
        }

        internal void Initialize(IDictionary properties, bool inProc, string userId, string password,
            bool isStartingAsMirror, bool twoPhaseInitialization)
        {
            if (_context._cmptKnownTypesforJava == null) _context._cmptKnownTypesforJava = new Hashtable();
            if (_context._cmptKnownTypesforNet == null) _context._cmptKnownTypesforNet = new Hashtable();

         
            if (_context._dataSharingKnownTypesforNet == null) _context._dataSharingKnownTypesforNet = new Hashtable();
            
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

                    IDictionary cacheConfig = (IDictionary)properties["cache"];

                    if (cacheConfig.Contains("data-format"))
                    {

                        if (_cacheInfo.Configuration != null && _cacheInfo.Configuration.CacheType.CompareTo("client-cache") == 0)
                        {
                            if (inProc)
                            {
                                SocketServerDataService = new ObjectDataFormatService(_context);
                                _context.CachingSubSystemDataService = CachingSubSystemDataService = new BinaryDataFormatService();
                                _context.InMemoryDataFormat = DataFormat.Object;
                            }
                            else
                            {

                                SocketServerDataService = new BinaryDataFormatService();
                                _context.CachingSubSystemDataService = CachingSubSystemDataService = new ObjectDataFormatService(_context);
                                _context.InMemoryDataFormat = DataFormat.Binary;
                            }

                        }
                        else
                        {
                            if (((String)cacheConfig["data-format"]).ToLower().Equals("object") && inProc)
                            {
                                SocketServerDataService = new ObjectDataFormatService(_context);
                                _context.CachingSubSystemDataService = CachingSubSystemDataService = new BinaryDataFormatService();
                                _context.InMemoryDataFormat = DataFormat.Object;
                            }
                            else 
                            {
                                SocketServerDataService = new BinaryDataFormatService();
                                _context.CachingSubSystemDataService = CachingSubSystemDataService = new ObjectDataFormatService(_context);
                                _context.InMemoryDataFormat = DataFormat.Binary;
                            }
                        }
                    }

                    if (properties["server-end-point"] != null)
                        cacheConfig.Add("server-end-point", (IDictionary)properties["server-end-point"]);

                    if (!Alachisoft.NCache.Licensing.LicenseManager.Express) //Numan Hanif[Express] Runtime Checks
                    {
                        _context.SQLDepSettings = new SQLDependencySettings();
                        if (!inProc)
                        {
                            bool useDefaultSQLDependency = true;
                            if (cacheConfig.Contains("sql-dependency"))
                            {
                                Hashtable sqlDependency = cacheConfig["sql-dependency"] as Hashtable;
                                if (sqlDependency.Contains("use-default"))
                                    useDefaultSQLDependency = Convert.ToBoolean(sqlDependency["use-default"]);
                            }

                            //_context.SQLDepSettings.initialize(useDefaultSQLDependency, _context.Render.IPAddress);
                            _sqlDependencySettings = _context.SQLDepSettings;
                        }

                        //EmailNotifier: EmailNotification config                    
                        if (cacheConfig.Contains("alerts"))
                        {
                            IDictionary alertConfig = (IDictionary)cacheConfig["alerts"];
                            if (alertConfig.Contains("alerts-types"))
                            {
                                _context.CacheAlertTypes =
                                    Alachisoft.NCache.Caching.Util.AlertTypeHelper.Initialize(
                                        alertConfig["alerts-types"] as IDictionary);
                            }
                            else
                            {
                                _context.CacheAlertTypes = new AlertNotificationTypes();
                            }

                            if (alertConfig.Contains("email-notification"))
                            {
                                _context.EmailAlertNotifier = new EmailAlertNotifier();
                                EmailNotifierArgs emailNotifierArgs =
                                    new EmailNotifierArgs(alertConfig["email-notification"] as IDictionary, _context);
                                _context.EmailAlertNotifier.Initialize(emailNotifierArgs, _context.CacheAlertTypes);
                            }
                        }
                        else
                        {
                            _context.EmailAlertNotifier = new EmailAlertNotifier();
                        }
                        EmailAlertPropagator = _context.EmailAlertNotifier;
                        //-

                        if (cacheConfig.Contains("security"))
                        {
                            _context.CacheSecurityProvider = new ApiSecurityProvider();
                            _context.CacheSecurityProvider.Initialize(cacheConfig["security"]);
                        }
                        else
                        {
                            _context.CacheSecurityProvider = new ApiSecurityProvider();
                        }
                        CacheSecurityProvider = _context.CacheSecurityProvider;
                    }



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
                        }
                    }
                    _context.SerializationContext = _cacheInfo.Name;
               
                    _context.TimeSched = new TimeScheduler();
                    _context.AsyncProc = new AsyncProcessor(_context.NCacheLog);
                    _asyncProcessor = new AsyncProcessor(_context.NCacheLog);
                    _context.SyncManager = new CacheSyncManager(this, _context);
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
                                _context.PerfStatsColl = new PerfStatsCollector(Name, inProc); //TODO: ALACHISOFT Removing port as we cannot find it right now
#endif

                            _context.PerfStatsColl.NCacheLog = _context.NCacheLog;
                        }

                    }

                    _context.IsStartedAsMirror = isStartingAsMirror;


                    if (!Alachisoft.NCache.Licensing.LicenseManager.Express) //Numan Hanif [Express] Runtime Checks
                    {
                        if (cacheConfig.Contains("backing-source"))
                        {
                            try
                            {
                                long reminderTimeout = 10000;
                                IDictionary cacheClass = (IDictionary)cacheConfig["cache-classes"];
                                if (cacheClass != null)
                                {
                                    cacheClass = (IDictionary)cacheClass[_cacheInfo.Name.ToLower()];
                                    if (cacheClass != null)
                                    {
                                        if (cacheClass.Contains("op-timeout"))
                                        {
                                            reminderTimeout = Convert.ToInt64(cacheClass["op-timeout"]);
                                            if (reminderTimeout < 100) reminderTimeout = 100;
                                            if (reminderTimeout > 30000) reminderTimeout = 30000;
                                        }
                                    }
                                }

                                IDictionary dsConfig = (IDictionary)cacheConfig["backing-source"];
                                _context.DsMgr = new DatasourceMgr(this.Name, dsConfig, _context, reminderTimeout,
                                                    _cacheInfo.Configuration != null &&
                                                    _cacheInfo.Configuration.CacheType.Equals("client-cache",
                                                        StringComparison.InvariantCultureIgnoreCase));

                                // Add special backing source config for client cache to cacheInfo.
                                if (_cacheInfo.Configuration != null && _cacheInfo.Configuration.BackingSource == null &&
                                    _cacheInfo.Configuration.CacheType.Equals("client-cache", StringComparison.OrdinalIgnoreCase))
                                {
                                    _cacheInfo.Configuration.BackingSource =
                                        ConfigHelper.GetClientCacheBackingSource(_cacheInfo.Configuration.ClientCacheSettings.ServerCache,
                                            _cacheInfo.Configuration.ClientCacheSettings.OfflineQueueSize, inProc);
                                }

                                if (_context.DsMgr._dsUpdateProcessor != null)
                                    _context.DsMgr._dsUpdateProcessor.Start();
                                if (_context.DsMgr.IsReadThruEnabled)
                                {
                                    _context.DsMgr.DefaultReadThruProvider = GetDefaultReadThruProvider();
                                    _context.CacheReadThruDataService = _context.CachingSubSystemDataService;
                                }
                                if (_context.DsMgr.IsWriteThruEnabled)
                                {
                                    if (((String)cacheConfig["data-format"]).ToLower().Equals("object") && inProc)
                                    {
                                        _context.CacheWriteThruDataService = new BinaryDataFormatService();
                                    }
                                    else
                                    {
                                        _context.CacheWriteThruDataService = new ObjectDataFormatService(_context);

                                    }

                                    _context.CacheReadThruDataService = _context.CachingSubSystemDataService;
                                    _context.DsMgr.DefaultWriteThruProvider = GetDefaultWriteThruProvider();
                                }
                            }
                            catch (Exception e)
                            {
                                if (e is ConfigurationException)
                                {
                                    _context.NCacheLog.Error("Cache.Initialize()", e.ToString());
                                    string msg =
                                        String.Format(
                                            "Datasource provider (ReadThru/WriteThru) could not be initialized because of the following error: {0}",
                                            e.Message);
                                    AppUtil.LogEvent(msg, System.Diagnostics.EventLogEntryType.Warning);
                                    throw new Exception(msg);
                                }
                                else
                                {
                                    _context.NCacheLog.Error("Cache.Initialize()", e.ToString());
                                    string msg =
                                        String.Format(
                                            "Failed to initialize datasource sync. read-through/write-through will not be available, Error {0}",
                                            e.Message);
                                    AppUtil.LogEvent(msg, System.Diagnostics.EventLogEntryType.Warning);
                                    throw new Exception(msg, e);
                                }
                            }
                        }
#if ENTERPRISE
                        if (cacheConfig.Contains("conflict-resolver"))
                        {
                            try
                            {
                                IDictionary crlConfig = (IDictionary)cacheConfig["conflict-resolver"];
                                _context.BridgeConflictMgr = new BridgeConflictResolutionMgr(_context, crlConfig, this);
                            }
                            catch (Exception e)
                            {
                                if (e is ConfigurationException)
                                {
                                    _context.NCacheLog.Error("Cache.Initialize()", e.ToString());
                                    string msg = String.Format("Bridge conflict resolver could not be initialized because of the following error: {0}", e.Message);
                                    AppUtil.LogEvent(msg, System.Diagnostics.EventLogEntryType.Warning);
                                    throw new Exception(msg);
                                }
                                else
                                {
                                    _context.NCacheLog.Error("Cache.Initialize()", e.ToString());
                                    string msg = String.Format("Failed to initialize Bridge conflict resolver, it will not be available, Error {0}", e.Message);
                                    AppUtil.LogEvent(msg, System.Diagnostics.EventLogEntryType.Warning);
                                    throw new Exception(msg, e);
                                }
                            }
                        }
                        else
                        {
                            _context.BridgeConflictMgr = new BridgeConflictResolutionMgr(_context, this);
                        }
#endif
                        if (cacheConfig.Contains("cache-loader"))
                        {
                            try
                            {
                                IDictionary cslConfig = (IDictionary)cacheConfig["cache-loader"];
                                if (inProc && !isStartingAsMirror)
                                    _context.CSLMgr = new InprocCacheStartupLoader(cslConfig, this, _context.NCacheLog);
                                else
                                    if (_cacheInfo != null)
                                        _context.CSLMgr = new OutprocCacheStartupLoader(_cacheInfo.Name, cslConfig, _context);
                            }
                            catch (ConfigurationException ce)
                            {
                                _context.NCacheLog.Error("Cache.Initialize()", ce.ToString());
                                string msg =
                                    String.Format(
                                        "Failed to initialize cache startup loader. startup loader will not be available because it is enabled but input string is invalid.");
                                AppUtil.LogEvent(msg, System.Diagnostics.EventLogEntryType.Warning);
                            }
                            catch (Exception e)
                            {
                                _context.NCacheLog.Error("Cache.Initialize()", e.ToString());
                                string msg = String.Format("Failed to initialize cache startup loader, Error {0}",
                                    e.ToString());
                                AppUtil.LogEvent(msg, System.Diagnostics.EventLogEntryType.Warning);
                            }
                        }





                        bool isDataSharing = false;
                        if (properties.Contains("data-sharing"))
                        {
                            object dataSharingSettings = properties["data-sharing"];

                            Context._dataSharingKnownTypesforNet = (Hashtable)dataSharingSettings;
                            Context._dataSharingKnownTypesforNet = (Hashtable)Context._dataSharingKnownTypesforNet.Clone();

                            if (dataSharingSettings is Hashtable)
                            {
                                Context.CompactKnownTypes = (Hashtable)dataSharingSettings;

                                FilterOutDotNetTypes(Context.CompactKnownTypes, Context._cmptKnownTypesforNet, Context._cmptKnownTypesforJava,  false);
                                // Dot Net and java compact type hashtable, keeping them in different Hashtables

                                isDataSharing = true;
                                //[salman]
                                // initialize compact framework only if either read-thru, write-thru or cache startup loader is enabled.
                                if ((_context.DsMgr != null && _context.DsMgr.LoadCompactTypes)|| (_context.CSLMgr != null && _context.CSLMgr.IsCacheloaderEnabled))
                                {

                                    if (AuthenticateFeature.IsJavaEnabled && _inProc == false && Context._cmptKnownTypesforJava.Count > 0)
                                        JSerializationUtil.initialize( SerializationUtil.GetProtocolStringFromTypeMap( Context._cmptKnownTypesforJava), this.Name);

                                    InitializeCompactFramework(Context._cmptKnownTypesforNet, !_inProc);
                                    // in out proc exceptions should be thrown.
                                }
                               
                            }
                        }
                        

                        if (properties.Contains("compact-serialization"))
                        {
                            object cmptSettings = properties["compact-serialization"];
                            if (cmptSettings is Hashtable)
                            {
                                if (Context.CompactKnownTypes != null && Context.CompactKnownTypes.Count > 0 && isDataSharing)
                                {
                                    Hashtable temp = (Hashtable)cmptSettings;
                                    IEnumerable keys = temp.Keys;
                                    foreach (string var in keys)
                                    {
                                        if (!Context.CompactKnownTypes.Contains(var))
                                        {
                                            Context.CompactKnownTypes.Add(var, temp[var]);
                                        }
                                    }
                                }
                                else
                                    Context.CompactKnownTypes = (Hashtable)cmptSettings;

                                FilterOutDotNetTypes(Context.CompactKnownTypes, Context._cmptKnownTypesforNet, Context._cmptKnownTypesforJava, true);
                                // Dot Net and java compact type hashtable, keeping them in different Hashtables

                                if (AuthenticateFeature.IsJavaEnabled && _inProc == false &&
                                    CompactRegisteredTypesForJava.Count > 0)
                                    JSerializationUtil.initialize(
                                        SerializationUtil.GetProtocolStringFromTypeMap(CompactRegisteredTypesForJava),
                                        this.Name);

                                InitializeCompactFramework(Context._cmptKnownTypesforNet, !_inProc);
                                
                            }
                        }


                        if (properties.Contains("encryption"))
                        {
                            _encryptionInfo = (Hashtable)properties["encryption"];
                            Alachisoft.NCache.Security.Encryption.EncryptionMgr.InitializeEncryption(
                                Convert.ToBoolean((string)_encryptionInfo["enabled"]), (string)_encryptionInfo["key"],
                                this.Name, (string)_encryptionInfo["provider"]);
                        }
                    }

                    CreateInternalCache(cacheConfig, userId, password, isStartingAsMirror, twoPhaseInitialization);

                   
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
                        }
                    }

                    if (_context.CacheImpl != null && _context.DsMgr != null)
                    {
                        _context.DsMgr.CacheImpl = _context.CacheImpl;
                        //if (_context.ClientDeathDetection != null)
                        //{
                        //    _context.ClientDeathDetection.CacheImpl = _context.CacheImpl;
                        //    _context.ClientDeathDetection.OnDeadClientsDetected += _context.CacheImpl.DeclareDeadClients;
                        //}
                    }

                    //if (_context.ClientDeathNotifier != null)
                    //{
                    //    _context.ClientDeathNotifier.CacheImpl = _context.CacheImpl;
                    //    _context.ClientDeathNotifier.OnDeadClientsDetected +=
                    //        _context.CacheImpl.HandleDeadClientsNotification;
                    //    //ClientConnectivityChanged += _context.CacheImpl.NotifyClusterOfClientActivity;
                    //    //_context.ClientDeathNotifier.StartMonitoringClients();
                    //}

                    if (!_context.IsClusteredImpl && _context.DsMgr != null) _context.DsMgr.StartWriteBehindProcessor();
                    //if (_context.ClientDeathDetection != null) _context.ClientDeathDetection.StartMonitoringClients();


                    if (inProc && _context.CacheImpl != null)
                    {
                        //muds:
                        //we keep unserialized user objects in case of local inproc caches...
                        _context.CacheImpl.KeepDeflattedValues = false;
                        // _context.CacheImpl is LocalCacheImpl; we no longer keep seralized data in inproc cache
#if !(DEVELOPMENT || CLIENT)
                        if (_context.CacheImpl is PartitionedClientCache ||
                            _context.CacheImpl is ReplicatedClientCache ||
                            _context.CacheImpl is PartitionOfReplicasClientCache)
                        {
                            try
                            {
#if !NETCORE
                                _channels.UnregisterTcpChannels();
                                _channels = null;
#endif
                            }
                            catch (Exception)
                            {
                            }
                        }
#endif
                    }

                    // we bother about perf stats only if the user has read/write rights over counters.
                    _context.PerfStatsColl.IncrementCountStats(CacheHelper.GetLocalCount(_context.CacheImpl));
                    //_context.CacheStatsColl = new CacheStatsCollector(_context);


                    _cacheInfo.ConfigString = ConfigHelper.CreatePropertyString(properties);
                    //#if VS2005

                    //#endif



#if ENTERPRISE
                    if (!(_context.CacheImpl is ClusterCacheBase))
#endif
                    {
                       
                        if (_context.CSLMgr != null && _context.CSLMgr.IsCacheloaderEnabled)
                        {
                            try
                            {
                                _context.CSLMgr.Start();
                            }
                            catch (Exception e)
                            {

                            }
                        }
                    }
                    _context.EntryProcessorManager = new EntryProcessorManager(this.CacheType, _context, this);
                }
                _context.CacheImpl.Parent = this;

                ObjectPooling.ObjectPoolManager.InitializePool();

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
                                    cmptKnownTypesJava.Add((string)ide.Key, new Hashtable());

                                ((Hashtable)cmptKnownTypesJava[((string)ide.Key)]).Add((string)typeInfo["name"], typeInfo);

                                if (!typeInfo.Contains("portable"))
                                {
                                    typeInfo.Add("portable", compactType["portable"]);
                                }
                            }
                            else if (typeInfo["type"].ToString().ToLower().Equals("net"))
                            {
                                if (!cmptKnownTypesdotNet.Contains((string)ide.Key))
                                    cmptKnownTypesdotNet.Add((string)ide.Key, new Hashtable());

                                ((Hashtable)cmptKnownTypesdotNet[((String)ide.Key)]).Add((String)typeInfo["name"],typeInfo);

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
                                    cmptKnownTypesJava.Add((string)ide.Key, new Hashtable());

                                ((Hashtable)cmptKnownTypesJava[((string)ide.Key)]).Add((string)compactType["name"],compactType);
                            }
                            else if (compactType["type"].ToString().ToLower().Equals("net"))
                            {
                                if (compactType.Contains("arg-types"))
                                    compactType["arg-types"] = FilterOutNestedGenerics((Hashtable)compactType["arg-types"]);

                                if (!cmptKnownTypesdotNet.Contains((string)ide.Key))
                                    cmptKnownTypesdotNet.Add((string)ide.Key, new Hashtable());

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
                htArgTypes2 = new Hashtable();
                IDictionaryEnumerator ide11 = htArgTypes.GetEnumerator();
                while (ide11.MoveNext())
                {
                    Hashtable innerGenericType = new Hashtable();
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
                        Hashtable innerGenericTypeDetail = new Hashtable();
                        innerGenericTypeDetail.Add(instanceArgType["name"].ToString(), instanceArgType);
                        innerGenericType.Add(ide12.Key.ToString(), innerGenericTypeDetail);
                    }
                }
                //compactType["arg-types"] = htArgTypes2;               
            }
            return htArgTypes2;
        }

        internal void Initialize2(IDictionary properties, bool inProc, string userId, string password)
        {
            if (_context._cmptKnownTypesforJava == null) _context._cmptKnownTypesforJava = new Hashtable();
            if (_context._cmptKnownTypesforNet == null) _context._cmptKnownTypesforNet = new Hashtable();
          
            if (_context._dataSharingKnownTypesforNet == null) _context._dataSharingKnownTypesforNet = new Hashtable();
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
                    _context.SyncManager = new CacheSyncManager(this, _context);

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
                                _context.PerfStatsColl = new PerfStatsCollector(Name, inProc); //TODO: ALACHISOFT Removing port as we cannot find it right now
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

            Hashtable framework = new Hashtable();

            try
            {
                framework = SerializationUtil.GetCompactTypes(properties, throwExceptions, _cacheInfo.Name);
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
                        SerializationUtil.GetSubTypeHandle(this.Name, ((short)typeHandle).ToString(),(System.Type)ide3.Key),
                        SerializationUtil.GetAttributeOrder(this.Name),
                        SerializationUtil.GetPortibilaty((short)typeHandle, this.Name), nonCompactFields);

                    //Also register array type for custom types.
                    typeHandle += SerializationUtil.UserdefinedArrayTypeHandle;     //Same handle is used at client side.
                    System.Type arrayType = ((System.Type) ide3.Key).MakeArrayType();

                    CompactFormatterServices.RegisterCustomCompactType(arrayType, (short)typeHandle,_cacheInfo.Name.ToLower(),
                       SerializationUtil.GetSubTypeHandle(this.Name, ((short)typeHandle).ToString(),arrayType),
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

        private void CreateInternalCache(IDictionary properties, string userId, string password, bool isStartingAsMirror,
            bool twoPhaseInitialization)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                if (!properties.Contains("class"))
                    throw new ConfigurationException("Missing configuration attribute 'class'");
                String cacheScheme = Convert.ToString(properties["class"]);

                //if (!properties.Contains("cache-port"))
                //{
                //    throw new ConfigurationException("Missing configuration attribute 'cache-port'");
                //}
                //int cPort = Convert.ToInt32(properties["cache-port"]);
                //(Integer.parseInt(properties.get("client-port").toString()));

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

                bool isClusterable = false;

                if (bEnableCounter)
                {
                    _context.PerfStatsColl.InitializePerfCounters((isStartingAsMirror
                        ? !isStartingAsMirror
                        : this._inProc));
                }

                    if (!Alachisoft.NCache.Licensing.LicenseManager.Express)
                //Numan Hanif [Express] Runtime Check for Non-Express
                {
                    // Check the type of license the application 
                    if (LicenseManager.NCacheBuildType != LicenseManager.BuildType.CNTO && LicenseManager.NCacheBuildType!= LicenseManager.BuildType.CNT)
                    {
                        LicenseManager.LicenseType type;
                        try
                        {
                            type = LicenseManager.LicenseMode(_context.NCacheLog);
                            LicenseManager.LastInterval = DateTime.Now.TimeOfDay.Ticks;
                            LicenseManager.LastLicenseMode = type;
                        }
                        catch (Exception)
                        {
                            return;
                        }

                        if (type == LicenseManager.LicenseType.Expired)
                        {
#if JAVA
                        throw new LicensingException("Your license for using TayzGrid has expired. Please contact sales@alachisoft.com for further terms and conditions.");
#else
                            throw new LicensingException(
                                "Your license for using NCache has expired. Please contact sales@alachisoft.com for further terms and conditions.");
#endif
                        }

                        isClusterable = (type == LicenseManager.LicenseType.ActivePerProcessor) ||
                                        (type == LicenseManager.LicenseType.InEvaluation);
                    }

                }


                if (Alachisoft.NCache.Licensing.LicenseManager.Express)
                    //Numan Hanif [Express] Runtime Check for Express
                    isClusterable = true;
                _context.ExpiryMgr = new ExpirationManager(schemeProps, _context);
              
               
                //ARIF: For the removal of Cascaded Dependencies on Clean Interval.
                _context.ExpiryMgr.TopLevelCache = this;

                _cacheInfo.ClassName = Convert.ToString(schemeProps["type"]).ToLower();
                _context.AsyncProc.Start();
                _asyncProcessor.Start();


                //SAL: We should use an InternalCacheFactory for the code below
                if (_cacheInfo.IsClusteredCache && properties.Contains("bridge"))
                {
                    Hashtable bridgeProps = properties["bridge"] as Hashtable;

                    if (properties.Contains("server-end-point"))
                        bridgeProps.Add("server-end-point", properties["server-end-point"]);

                    if (bridgeProps != null)
                    {
                        if (bridgeProps.Contains("is-target-cache"))
                        {
                            _isBridgeTargetCache = Convert.ToBoolean(bridgeProps["is-target-cache"]);
                            _context.IsBridgeTargetCache = _isBridgeTargetCache;
                        }
                        if (bridgeProps.Contains("id"))
                        {
                            _bridgeId = bridgeProps["id"] as string;
                        }
#if ENTERPRISE
                        if (!_isBridgeTargetCache)
                        {
                            _bridgeReplicator = new BridgeReplicator(_context);
                            _bridgeReplicator.Initialize(bridgeProps);
                            _context.BridgeReplicator = _bridgeReplicator;
                        }
#endif
                    }
                }

#if !CLIENT && !DEVELOPMENT
                if (_cacheInfo.ClassName.CompareTo("replicated-server") == 0)
                {
                    _isPersistEnabled = ServiceConfiguration.EventsPersistence;
                    if (_isPersistEnabled)
                    {
                        _persistenceInterval = ServiceConfiguration.EventsPersistenceInterval;

                        _context.PersistenceMgr = new PersistenceManager(_persistenceInterval);
                    }
                    if (isClusterable)
                    {
                        _context.CacheImpl = new ReplicatedServerCache(cacheClasses, schemeProps, this, _context, this, userId, password);
                        _context.CacheImpl.Initialize(cacheClasses, schemeProps, userId, password, twoPhaseInitialization);
                    }
                }
                else if (_cacheInfo.ClassName.CompareTo("mirror-server") == 0 && isClusterable)
                {
                    _context.CacheImpl = new MirrorCache(cacheClasses, schemeProps, this, _context, this, userId, password);
                    _context.CacheImpl.Initialize(cacheClasses, schemeProps, userId, password, twoPhaseInitialization);
                }
                else
                if (_cacheInfo.ClassName.CompareTo("partitioned-replicas-server") == 0 && isClusterable)
                {

                    bool synchronous = false;
                    if (properties.Contains("replication-strategy"))
                    {
                        Hashtable replicationStrategy = properties["replication-strategy"] as Hashtable;
                        if (replicationStrategy.Contains("synchronous"))
                            synchronous = Convert.ToBoolean(replicationStrategy["synchronous"]);
                    }

                    if (synchronous)
                        _context.CacheImpl = new SynchronousPartitionOfReplicasCache(cacheClasses, schemeProps, this, _context, this, userId, password, isStartingAsMirror);
                    else
                        _context.CacheImpl = new AsynchronousPartitionOfReplicasCache(cacheClasses, schemeProps, this, _context, this, userId, password, isStartingAsMirror);


                    ClusterSplitSetting clusterSplitSettings = ConfigConverter.HashtableToDom.GetSplitBrainSetting(properties[ClusterSplitAttributeName.CLUSTER_SPLIT_DECTETION] as Hashtable);

                    _context.ClusterSplitManager = new ClusterSplitManager(_context, Convert.ToString(properties["name"]).ToLower(), clusterSplitSettings);
                    if (clusterSplitSettings.IsAutoRecoveryEnabled && _context.Render != null)
                    {
                        _context.Render.UserInfo = new UserInfo() { UserId = Common.EncryptionUtil.Encrypt(userId), Password = Common.EncryptionUtil.Encrypt(password) };

                         _context.Render.RegisterOperationModeChangeEvent();
                    }

                    _context.CacheImpl.Initialize(cacheClasses, schemeProps, userId, password, twoPhaseInitialization);
                }
                else if (_cacheInfo.ClassName.CompareTo("partitioned-server") == 0 && isClusterable)
                {
                    _context.CacheImpl = new PartitionedServerCache(cacheClasses, schemeProps, this, _context, this, userId, password);
                    _context.CacheImpl.Initialize(cacheClasses, schemeProps, userId, password, twoPhaseInitialization);
                }

                else
#endif


                if (_cacheInfo.ClassName.CompareTo("local-cache") == 0)
                {
                    LocalCacheImpl cache = new LocalCacheImpl(_context);

                    // Create special instance in case of client cache.
                    if (_cacheInfo.Configuration != null && _cacheInfo.Configuration.CacheType.CompareTo("client-cache") == 0)
                    {
                        cache = new ClientCache(_cacheInfo.Configuration.ClientCacheSettings.EnableDisconnected, _cacheInfo.Configuration.SynchronizationStrategy.Strategy);
                        _context.CacheWriteThruDataService = _context.CacheReadThruDataService = new ClientCache.OutprocDataFormatService(_context);
                        _isClientCache = true;
                        _context.IsClientCache = true;
                        _serverCacheName = _cacheInfo.Configuration.ClientCacheSettings.ServerCache;
                    }

                    ActiveQueryAnalyzer analyzer = new ActiveQueryAnalyzer(cache, schemeProps, _cacheInfo.Name, this.Context, Context._dataSharingKnownTypesforNet);

                    cache.Internal = CacheBase.Synchronized(new IndexedLocalCache(cacheClasses, cache, schemeProps, this, _context, analyzer));

                    _context.CacheImpl = cache;

                    if (!_isClientCache && _context.MessageManager != null) _context.MessageManager.StartMessageProcessing();
                }
                else
                {
                    throw new ConfigurationException("Specified cache class '" + _cacheInfo.ClassName + "' is not available in this edition of " + _cacheserver + ".");
                }
#if ENTERPRISE
               
                    ((CacheSyncWrapper)_context.CacheInternal).Replicator = _bridgeReplicator;
               
#endif
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
#if ENTERPRISE
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
                    LicenseManager.RaiseClusterNotAvailableEvent();
                }
            }
            catch (ConfigurationException e)
            {
                _context.NCacheLog.Error("Cache.CreateInternalCache()", e.ToString());
                _context.CacheImpl = null;
                Dispose();
                throw;
            }

            catch (LicensingException le)
            {
                _context.NCacheLog.Error("Cache.CreateInternalCache()", le.ToString());
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

                //muds:
                //there is no licensing in the express edtion but we still wants
                //to provide replicated cluster.
                bool isClusterable = true;

                if (bEnableCounter)
                {
                    _context.PerfStatsColl.InitializePerfCounters(this._inProc);
                }

                _context.ExpiryMgr = new ExpirationManager(properties, _context);
                //ARIF: For the removal of Cascaded Dependencies on Clean Interval.
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


                //SAL: We should use an InternalCacheFactory for the code below
#if ENTERPRISE
                if (_cacheInfo.ClassName.CompareTo("replicated") == 0 &&
                    !Alachisoft.NCache.Licensing.LicenseManager.Express)
                //Numan Hanif[Express] Runtime Check for Non-Express
                {
                    if (isClusterable)
                    //_context.CacheImpl = new ReplicatedServerCache(cacheClasses, schemeProps, this, _context);
                    {
                        _context.CacheImpl = new ReplicatedServerCache(properties, clusterProps, this, _context, this,
                            userId, password);
                        _context.CacheImpl.Initialize(properties, clusterProps, userId, password, false);
                    }
                }
                else
#endif
                    if (_cacheInfo.ClassName.CompareTo("local") == 0)
                    {
                        LocalCacheImpl cache = new LocalCacheImpl(_context);
                    //ActiveQueryAnalyzer analyzer = new ActiveQueryAnalyzer(cache);

                    cache.Internal =
                            CacheBase.Synchronized(new LocalCache(properties, cache, properties, this, _context, null));

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
#if ENTERPRISE
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

                    LicenseManager.RaiseClusterNotAvailableEvent();

                }
            }
            catch (ConfigurationException e)
            {
                _context.NCacheLog.Error("Cache.CreateInternalCache()", e.ToString());
                _context.CacheImpl = null;
                Dispose();
                throw;
            }

            catch (LicensingException le)
            {
                _context.NCacheLog.Error("Cache.CreateInternalCache()", le.ToString());
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

        private void CheckDataSourceAvailabilityAndOptions(DataSourceUpdateOptions updateOpts)
        {
            if (updateOpts != DataSourceUpdateOptions.None)
            {
                if (_context.DsMgr != null && (_context.DsMgr.IsWriteThruEnabled &&
                    (updateOpts == DataSourceUpdateOptions.WriteBehind || updateOpts == DataSourceUpdateOptions.WriteThru)))
                    return;
                throw new OperationFailedException("Backing source not available. Verify backing source settings");
            }
        }

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

#region	/                 --- Clear ---           /

        /// <summary>
        /// Clear all the contents of cache
        /// </summary>
        /// <returns></returns>
        public void Clear()
        {
            Clear(new BitSet(), null,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
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
            if (!IsRunning) return; //throw new InvalidOperationException();

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
            this.CheckDataSourceAvailabilityAndOptions(updateOpts);


            try
            {

                string providerName = null;
                if (operationContext.Contains(OperationContextFieldName.ReadThruProviderName))
                {
                    providerName =
                        (string)operationContext.GetValueByField(OperationContextFieldName.ReadThruProviderName);
                }

                _context.CacheImpl.Clear(cbEntry, updateOpts, operationContext);
                //_context.SyncManager.Clear();
                //if (updateOpts == DataSourceUpdateOptions.WriteThru)
                //{
                //    this._context.DsMgr.WriteThru(null, null, OpCode.Clear, providerName, operationContext);

                //}
                //else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind && !_context.IsBridgeTargetCache)
                //{
                //    CacheEntry entry = null;
                //    if (cbEntry != null)
                //    {
                //        entry = new CacheEntry(cbEntry, null, null);
                //    }
                //    this._context.DsMgr.WriteBehind(_context.CacheImpl, null, entry, null, null, providerName, OpCode.Clear, WriteBehindAsyncProcessor.TaskState.Execute);
                //}

                // update the counter for item count.
                //_context.PerfStatsColl.IncrementCountStats(ConvHelper.GetLocalCount(_context.CacheImpl));

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
        public void ClearAsync(BitSet flagMap, CallbackEntry cbEntry, OperationContext operationContext)
        {
            // Cache has possibly expired so do default.
            if (!IsRunning) return; //throw new InvalidOperationException();
            //_context.SyncManager.Clear();
            //_context.AsyncProc.Enqueue(new AsyncClear(this, cbEntry, flagMap, operationContext));
            _asyncProcessor.Enqueue(new AsyncClear(this, cbEntry, flagMap, operationContext));
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
            if (!IsRunning) return false; //throw new InvalidOperationException();

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicRead))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

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
        /// <returns>Returns a CacheEntry</returns>
        public object GetCacheEntry(object key, string group, string subGroup, ref object lockId, ref DateTime lockDate,
            TimeSpan lockTimeout, LockAccessType accessType, OperationContext operationContext, ref ulong itemVersion)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null; //throw new InvalidOperationException();

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

                //Alam: if lockId will be empty if item is not already lock provided by user
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

                //Alam: if only key is provided by user
                if (group == null && subGroup == null &&
                    (accessType == LockAccessType.IGNORE_LOCK || accessType == LockAccessType.DONT_ACQUIRE))
                {
                    entry = _context.CacheImpl.Get(key, operationContext);
                }

                    //Alam: if key , group and sub-group are provided by user
                else if (group != null)
                {
                    entry = _context.CacheImpl.GetGroup(key, group, subGroup, ref itemVersion, ref lockId, ref lockDate,
                        null, LockAccessType.IGNORE_LOCK, operationContext);
                }

                    //Alam: if key and locking information is provided by user
                else
                {

                    entry = _context.CacheImpl.Get(key, ref itemVersion, ref lockId, ref lockDate, lockExpiration,
                        accessType, operationContext);
                }

                if (entry == null && accessType == LockAccessType.ACQUIRE)
                {
                    if (lockId == null || generatedLockId.Equals(lockId))
                    {
                        lockId = null;
                        lockDate = new DateTime();
                    }
                }

                if (operationContext.Contains(OperationContextFieldName.ReadThru))
                {
                    bool isReadThruFlagSet =
                        Convert.ToBoolean(operationContext.GetValueByField(OperationContextFieldName.ReadThru));
                    string providerName = null;
                    if (operationContext.Contains(OperationContextFieldName.ReadThruProviderName))
                    {
                        providerName =
                            Convert.ToString(
                                operationContext.GetValueByField(OperationContextFieldName.ReadThruProviderName));
                    }

                    // if read-thru available, try to read from datasource.
                    bool itemIsLocked = false;
                    if (accessType == LockAccessType.DONT_ACQUIRE)
                    {
                        if (generatedLockId == null && lockId != null && ((string)lockId).CompareTo(string.Empty) != 0)
                            itemIsLocked = true;
                    }
                    else if (accessType == LockAccessType.ACQUIRE)
                    {
                        if (generatedLockId != null && lockId != null && ((string)lockId).CompareTo(string.Empty) != 0 &&
                            !generatedLockId.Equals(lockId)) itemIsLocked = true;
                    }

                    if (isReadThruFlagSet && (_context.DsMgr == null || (_context != null && !(_context.DsMgr.IsReadThruEnabled))))
                        throw new OperationFailedException("Backing source not available. Verify backing source settings");

                    if (entry == null && !itemIsLocked && !_isClientCache &&
                        (isReadThruFlagSet && _context.DsMgr != null && _context.DsMgr.IsReadThruEnabled))
                    {
                        BitSet bitset = new BitSet();
                        object reSynched = _context.DsMgr.ResyncCacheItem(key as string, out entry, ref bitset, group, subGroup,
                            providerName,
                            new OperationContext(OperationContextFieldName.OperationType,
                                OperationContextOperationType.CacheOperation));

                        if (reSynched == null) entry = null;
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
            }
        }


        /// <summary>
        /// Retrieve the object from the cache. A key is passed as parameter.
        /// </summary>
        public CompressedValueEntry Get(object key)
        {
            return GetGroup(key, new BitSet(), null, null, null, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        public IDictionary GetByTag(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            if (!IsRunning) return null;

            HashVector table = null;

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkRead, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                if (_context.PerfStatsColl != null)
                    _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();

                table = _context.CacheImpl.GetTagData(tags, comparisonType, operationContext);

                if (_context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();
                    _context.PerfStatsColl.IncrementQueryPerSec();
                    if (table != null)
                        _context.PerfStatsColl.IncrementAvgQuerySize(table.Count);
                }

                if (table != null)
                {
                    IDictionaryEnumerator ide = ((HashVector)table.Clone()).GetEnumerator();
                    while (ide.MoveNext())
                    {
                        object key = ide.Key;
                        CacheEntry entry = ide.Value as CacheEntry;
                        CompressedValueEntry val = new CompressedValueEntry();
                        val.Value = entry.Value is CallbackEntry ? ((CallbackEntry)entry.Value).Value : entry.Value;
                        val.Flag = entry.Flag;
                        table[key] = val;
                    }
                }
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.GetByTag()", inner.ToString());
                throw;
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.GetByTag()", inner.ToString());
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.GetByTag()", inner.ToString());
                throw new OperationFailedException("GetByTag operation failed. Error : " + inner.Message, inner);
            }

            return table;
        }


        public ICollection GetKeysByTag(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            if (!IsRunning) return null;

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkRead, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                if (_context.PerfStatsColl != null)
                    _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();

                ICollection result = _context.CacheImpl.GetTagKeys(tags, comparisonType, operationContext);

                if (_context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();
                    _context.PerfStatsColl.IncrementQueryPerSec();
                    if (result != null)
                        _context.PerfStatsColl.IncrementAvgQuerySize(result.Count);
                }

                return result;
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.GetKeysByTag()", inner.ToString());
                throw;
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.GetKeysByTag()", inner.ToString());
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.GetKeysByTag()", inner.ToString());
                throw new OperationFailedException("GetKeysByTag operation failed. Error : " + inner.Message, inner);
            }
        }

        /// <summary>
        /// Remove items from cache on the basis of specified Tags
        /// </summary>
        /// <param name="sTags">Tag names</param>
        public void RemoveByTag(string[] sTags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            if (!IsRunning) return;

            try
            {
                CascadedRemove(sTags, comparisonType, true, operationContext);
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
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.RemoveByTag()", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.RemoveByTag()", inner.ToString());
                throw new OperationFailedException("RemoveByTag operation failed. Error : " + inner.Message, inner);
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

        public CompressedValueEntry GetGroup(object key, BitSet flagMap, string group, string subGroup,
            string providerName, OperationContext operationContext)
        {
            object lockId = null;
            DateTime lockDate = DateTime.UtcNow;
            ulong version = 0;
            return GetGroup(key, flagMap, group, subGroup, ref version, ref lockId, ref lockDate, TimeSpan.Zero, LockAccessType.IGNORE_LOCK, providerName, operationContext);
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
        public CompressedValueEntry GetGroup(object key, BitSet flagMap, string group, string subGroup,
            ref ulong version, ref object lockId, ref DateTime lockDate, TimeSpan lockTimeout, LockAccessType accessType,
            string providerName, OperationContext operationContext)
        {
            //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.GetGrp", "");
            // Cache has possibly expired so do default.
            if (!IsRunning) return null; //throw new InvalidOperationException();

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicRead))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, BlockInterval * 1000);
            }

            CompressedValueEntry result = new CompressedValueEntry();
            CacheEntry e = null;
            try
            {
                _context.PerfStatsColl.MsecPerGetBeginSample();
                _context.PerfStatsColl.IncrementGetPerSecStats();
                _context.PerfStatsColl.IncrementHitsRatioPerSecBaseStats();

                //HPTimeStats getTime = new HPTimeStats();
                //getTime.BeginSample();

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

                if (_isClientCache)
                {
                    operationContext.Add(OperationContextFieldName.ReadThru, flagMap.IsBitSet(BitSetConstants.ReadThru));
                    if (providerName != null)
                        operationContext.Add(OperationContextFieldName.ReadThruProviderName, providerName);
                }

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
                if (flagMap != null)
                {
                    bool isReadThruFlagSet = flagMap.IsBitSet(BitSetConstants.ReadThru);

                    if (e != null)
                    {
                        /// increment the counter for hits/sec
                        _context.PerfStatsColl.MsecPerGetEndSample();
                        result.Value = e.Value;
                        result.Flag = e.Flag;
                    }

                    // if read-thru available, try to read from datasource.
                    bool itemIsLocked = false;
                    if (accessType == LockAccessType.DONT_ACQUIRE)
                    {
                        if (generatedLockId == null && lockId != null && ((string)lockId).CompareTo(string.Empty) != 0)
                            itemIsLocked = true;
                    }
                    else if (accessType == LockAccessType.ACQUIRE)
                    {
                        if (generatedLockId != null && lockId != null && ((string)lockId).CompareTo(string.Empty) != 0 &&
                            !generatedLockId.Equals(lockId)) itemIsLocked = true;
                    }
                    if (isReadThruFlagSet &&
                        (_context.DsMgr == null || (_context.DsMgr != null && !_context.DsMgr.IsReadThruEnabled)))
                    {
                        throw new OperationFailedException("Backing source not available. Verify backing source settings");
                    }
                    if (e == null && !itemIsLocked && !_isClientCache &&
                        (isReadThruFlagSet && _context.DsMgr != null && _context.DsMgr.IsReadThruEnabled))
                    {
                        result.Flag = new BitSet();
                        result.Value = _context.DsMgr.ResyncCacheItem(key as string, out e, ref result.Flag, group, subGroup, providerName,
                            new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                    }

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

                CallbackEntry cbEntry = result.Value as CallbackEntry;
                if (cbEntry != null)
                    result.Value = cbEntry.Value;
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
                throw new OperationFailedException("Get operation failed. Error :" + inner.Message, inner);
            }

            return result;
        }

        private string GetDefaultProvider()
        {
            return "";
        }


        /// <summary>
        /// Retrieve the list of keys from the cache for the given group or sub group.
        /// </summary>
        public ArrayList GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            if (group == null) throw new ArgumentNullException("group");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null; //throw new InvalidOperationException();


            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkRead, operationContext))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }

            try
            {
                HPTimeStats getTime = new HPTimeStats();

                if (_context.PerfStatsColl != null)
                    _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();

                getTime.BeginSample();
                ArrayList result = _context.CacheImpl.GetGroupKeys(group, subGroup, operationContext);
                getTime.EndSample();

                if (_context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();
                    _context.PerfStatsColl.IncrementQueryPerSec();
                    if (result != null)
                        _context.PerfStatsColl.IncrementAvgQuerySize(result.Count);
                }
                return result;
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.GetGroupKeys()", inner.ToString());
                throw;
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable)
                    _context.NCacheLog.Error("Cache.Get()", "Get operation failed. Error :" + inner.ToString());
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Get()", "Get operation failed. Error :" + inner.ToString());
                throw new OperationFailedException("Get operation failed. Error :" + inner.Message, inner);
            }
        }


        /// <summary>
        /// Retrieve the list of key and value pairs from the cache for the given group or sub group.
        /// </summary>
        public HashVector GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            if (group == null) throw new ArgumentNullException("group");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null; //throw new InvalidOperationException();


            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkRead, operationContext))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }
            try
            {
                HPTimeStats getTime = new HPTimeStats();
                getTime.BeginSample();

                if (_context.PerfStatsColl != null)
                    _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();

                HashVector table = _context.CacheImpl.GetGroupData(group, subGroup, operationContext);

                if (_context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();
                    _context.PerfStatsColl.IncrementQueryPerSec();
                    if (table != null)
                        _context.PerfStatsColl.IncrementAvgQuerySize(table.Count);
                }

                if (table != null)
                {
                    /// increment the counter for hits/sec
                    object[] keyArr = new object[table.Count];
                    table.Keys.CopyTo(keyArr, 0);
                    IEnumerator ie = keyArr.GetEnumerator();

                    CompressedValueEntry val = null;
                    while (ie.MoveNext())
                    {
                        val = new CompressedValueEntry();
                        CacheEntry entry = (CacheEntry)table[ie.Current];
                        val.Value = entry.Value;
                        if (val.Value is CallbackEntry)
                            val.Value = ((CallbackEntry)val.Value).Value;
                        val.Flag = entry.Flag;
                        table[ie.Current] = val;
                    }

                    getTime.EndSample();
                }
                return table;
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.GetGroupData()", inner.ToString());
                throw;
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable)
                    _context.NCacheLog.Error("Cache.Get()", "Get operation failed. Error : " + inner.ToString());
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.Get()", "Get operation failed. Error : " + inner.ToString());
                throw new OperationFailedException("Get operation failed. Error : " + inner.Message, inner);
            }
        }



#endregion



#region	/                 --- Bulk Get ---           /

        /// <summary>
        /// Retrieve the array of objects from the cache.
        /// An array of keys is passed as parameter.
        /// </summary>
        public IDictionary GetBulk(object[] keys, BitSet flagMap, string providerName, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.GetBlk", "");

            if (keys == null) throw new ArgumentNullException("keys");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null; //throw new InvalidOperationException();

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkRead, operationContext))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }

            HashVector table = null;
            try
            {
                //_context.PerfStatsColl.MsecPerGetBeginSample();
                //_context.PerfStatsColl.IncrementGetPerSecStats();
                HPTimeStats getTime = new HPTimeStats();
                getTime.BeginSample();
                _context.PerfStatsColl.IncrementByGetPerSecStats(keys.Length);
                _context.PerfStatsColl.MsecPerGetBeginSample();

                if(_isClientCache)
                {
                    operationContext.Add(OperationContextFieldName.ReadThruProviderName, providerName);
                }

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
                    //_context.PerfStatsColl.MsecPerGetEndSample();
                    //_context.PerfStatsColl.IncrementHitsPerSecStats();
                    bool readThruEnabled = _context.DsMgr != null && _context.DsMgr.IsReadThruEnabled;

                    ///We maintian indexes of keys that needs resync or are not fethced in this array
                    ///This saves us from instantiating 3 separate arrays and then resizing it; 3 arrays to
                    ///hold keys, enteries, and flags
                    int[] resyncIndexes = null;
                    int counter = 0;

                    if (readThruEnabled) resyncIndexes = new int[keys.Length];

                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    if (!_isClientCache)
                    {
                        for (int i = 0; i < keys.Length; i++)
                        {
                            if (table.ContainsKey(keys[i]))
                            {
                                if (table[keys[i]] != null)
                                {
                                    CacheEntry entry = table[keys[i]] as CacheEntry;
                                    if (entry != null)
                                    {
                                        CompressedValueEntry val = new CompressedValueEntry();
                                        val.Value = entry.Value is CallbackEntry
                                            ? ((CallbackEntry)entry.Value).Value
                                            : entry.Value;
                                        val.Flag = entry.Flag;
                                        table[keys[i]] = val;
                                    }
                                }
                            }
                            else if (readThruEnabled && flagMap.IsBitSet(BitSetConstants.ReadThru))
                            {
                                resyncIndexes[counter++] = i;
                            }
                        }
                    }


                    ///start resync operation only if there are some keys that failed to get
                    ///and readthru is enabled
                    if (readThruEnabled && counter > 0 && !_isClientCache)
                    {
                        if (providerName == null || providerName == string.Empty)
                            providerName = _context.DsMgr.DefaultReadThruProvider;
                        string[] failedKeys = new string[counter];
                        CacheEntry[] enteries = new CacheEntry[counter];
                        BitSet[] flags = new BitSet[counter];

                        for (int i = 0; i < counter; i++)
                        {
                            int index = resyncIndexes[i];

                            failedKeys[i] = keys[index] as string;
                            enteries[i] = table[keys[index]] as CacheEntry;
                            flags[i] = enteries[i] == null ? new BitSet() : enteries[i].Flag;
                        }

                        _context.DsMgr.ResyncCacheItem(table, failedKeys, enteries, flags, providerName,
                            new OperationContext(OperationContextFieldName.OperationType,
                                OperationContextOperationType.CacheOperation));
                    }

                    getTime.EndSample();

                    //lock (_context.CacheStatsColl)
                    //{
                    //    _context.CacheStatsColl.GetSample(getTime.Current, table.Count);
                    //    _context.CacheStatsColl.IncrementHits(table.Count);
                    //    _context.CacheStatsColl.IncrementMisses(keys.Length - table.Count);
                    //}
                }
                else
                {
                    _context.PerfStatsColl.IncrementByMissPerSecStats(keys.Length);
                }

                _context.PerfStatsColl.MsecPerGetEndSample();
                //					_context.PerfStatsColl.MsecPerGetEndSample();
                //					/// update the counter for hits/sec or misses/sec
                //					if(value != null)
                //						_context.PerfStatsColl.IncrementHitsPerSecStats();
                //					else
                //						_context.PerfStatsColl.IncrementMissPerSecStats();

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

            ExpirationHint eh = ConvHelper.MakeExpirationHint(_context.CacheRoot.Configuration.ExpirationPolicy, cce.Expiration, isAbsolute);

            if (eh != null && cce.Dependency != null)
            {
                eh = new AggregateExpirationHint(cce.Dependency, eh);
            }

            if (eh == null) eh = cce.Dependency;

            if (eh != null)
            {
                if (isResync) eh.SetBit(ExpirationHint.NEEDS_RESYNC);
            }

            CacheEntry e = new CacheEntry(cce.Value, eh, new PriorityEvictionHint((CacheItemPriority)priority));
            e.GroupInfo = new GroupInfo(cce.Group, cce.SubGroup);
            e.QueryInfo = cce.QueryInfo;
            e.Flag = cce.Flag;
            e.SyncDependency = cce.SyncDependency;

            e.LockId = cce.LockId;
            e.LockAccessType = cce.LockAccessType;
            e.Version = (UInt32)cce.Version;
            e.ResyncProviderName = cce.ResyncProviderName;

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

        public bool AddSyncDependency(object key, CacheSyncDependency syncDependency, OperationContext operationContext)
        {
            if (!IsRunning) return false;

            //_context.SyncManager.AddDependency(key, syncDependency);
            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                if (!_context.CacheImpl.Add(key, syncDependency, operationContext))
                {
                    //_context.SyncManager.RemoveDependency(key, syncDependency);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _context.NCacheLog.Error("Add operation failed. Error: " + ex.ToString());
                throw;
            }
            return true;
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
            //if (entry is byte[])
            //    cce = (CompactCacheEntry)SerializationUtil.SafeDeserialize(entry, _context.SerializationContext);
            //else
            cce = (CompactCacheEntry)entry;

            CacheEntry e = MakeCacheEntry(cce);

            if (e != null)
            {
                //object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
                //if (dataSize != null)
                //    e.DataSize = (long)dataSize;

                e.SyncDependency = cce.SyncDependency;
            }
            string group = null, subgroup = null;
            if (e.GroupInfo != null && e.GroupInfo.Group != null)
            {
                group = e.GroupInfo.Group;
                subgroup = e.GroupInfo.SubGroup;
            }
            Add(cce.Key, e.Value, e.ExpirationHint, e.SyncDependency, e.EvictionHint, group, subgroup, e.QueryInfo,
                e.Flag, cce.ProviderName, e.ResyncProviderName, operationContext, null);
        }

        /// <summary>
        /// Basic Add operation, takes only the key and object as parameter.
        /// </summary>

        public void Add(object key, object value)
        {


            Add(key, value, null, null, null, null, null,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));


        }

        /// <summary>
        /// Overload of Add operation. Uses an additional ExpirationHint parameter to be used 
        /// for Item Expiration Feature.
        /// </summary>

        public void Add(object key, object value, ExpirationHint expiryHint, OperationContext operationContext)
        {
            Add(key, value, expiryHint, null, null, null, null, operationContext);
        }

        /// <summary>
        /// Overload of Add operation. Uses an additional EvictionHint parameter to be used for 
        /// Item auto eviction policy.
        /// </summary>

        public void Add(object key, object value, EvictionHint evictionHint)
        {
            Add(key, value, null, null, evictionHint, null, null,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Overload of Add operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>

        public void Add(object key, object value,
            ExpirationHint expiryHint, CacheSyncDependency syncDependency, EvictionHint evictionHint,
            string group, string subGroup, OperationContext operationContext
            )
        {
            Add(key, value, expiryHint, syncDependency, evictionHint, group, subGroup, null, operationContext);
        }

        /// <summary>
        /// Overload of Add operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>

        public void Add(object key, object value,
            ExpirationHint expiryHint, CacheSyncDependency syncDependency, EvictionHint evictionHint,
            string group, string subGroup, Hashtable queryInfo, OperationContext operationContext
            )
        {
            Add(key, value, expiryHint, syncDependency, evictionHint, group, subGroup, queryInfo, new BitSet(), null,
                null, operationContext, null);
        }

        /// <summary>
        /// Overload of Add operation. uses additional paramer of Flag for checking if compressed or not
        /// </summary>
        public void Add(object key, object value,
            ExpirationHint expiryHint, CacheSyncDependency syncDependency, EvictionHint evictionHint,
            string group, string subGroup, Hashtable queryInfo, BitSet flag, string providerName,
            string resyncProviderName, OperationContext operationContext, HPTime bridgeOperationTime)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            // Cache has possibly expired so do default.
            if (!IsRunning) return; //throw new InvalidOperationException();
            if (!IsRunning) return; //throw new InvalidOperationException();

            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);

            this.CheckDataSourceAvailabilityAndOptions(updateOpts);

            //+ Numan@14102014: No Need to insert GroupInfo if its group property is null/empty it will only reduce cache-entry overhead

            GroupInfo grpInfo = null;
            if (!String.IsNullOrEmpty(group))
                grpInfo = new GroupInfo(group, subGroup);

            //+ Numan@14102014

            CacheEntry e = new CacheEntry(value, expiryHint, evictionHint);
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

            e.SyncDependency = syncDependency;
            e.QueryInfo = queryInfo;

            e.Flag.Data |= flag.Data;

            e.BridgeOpTimeStamp = bridgeOperationTime;

            try
            {
                _context.PerfStatsColl.MsecPerAddBeginSample();

                CacheEntry clone;
                if ((updateOpts == DataSourceUpdateOptions.WriteThru ||
                     updateOpts == DataSourceUpdateOptions.WriteBehind) && e.HasQueryInfo)
                    clone = (CacheEntry)e.Clone();
                else
                    clone = e;

                if (_isClientCache)
                {
                    operationContext.Add(OperationContextFieldName.WriteThru, flag.IsBitSet(BitSetConstants.WriteThru));
                    operationContext.Add(OperationContextFieldName.WriteBehind, flag.IsBitSet(BitSetConstants.WriteBehind));
                    if (providerName != null)
                        operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);
                }
                Add(key, e, operationContext);

                _context.PerfStatsColl.MsecPerAddEndSample();



                OperationResult dsResult = null;
                string taskId = System.Guid.NewGuid().ToString();
                if (updateOpts == DataSourceUpdateOptions.WriteThru && !_isClientCache)
                {
                    dsResult = this._context.DsMgr.WriteThru(key as string, clone, OpCode.Add, providerName,
                        operationContext);
                    if (dsResult != null && dsResult.DSOperationStatus == OperationResult.Status.FailureRetry)
                    {
                        WriteOperation operation = dsResult.Operation;
                        if (operation != null)
                        {
                            //set requeue and retry and log retry failure
                            _context.NCacheLog.Info("Retrying Write Operation" + operation.OperationType +
                                                    " operation for key:" + operation.Key);
                            //creating ds operation with updated write operation
                            //DSWriteBehindOperation dsOperation = new DSWriteBehindOperation(this.Name, operation.Key, operation, OpCode.Add, providerName, taskId, null, WriteBehindAsyncProcessor.TaskState.Execute);
                            DSWriteBehindOperation dsOperation = new DSWriteBehindOperation(_context, operation.Key, e,
                                OpCode.Add, providerName, 0, taskId, null, WriteBehindAsyncProcessor.TaskState.Execute);
                            //retrying with initial entry
                            _context.CacheImpl.EnqueueDSOperation(dsOperation);
                        }
                    }
                }
                else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind &&
                         !_context.IsBridgeTargetCache && !_isClientCache)
                {
                    this._context.DsMgr.WriteBehind(_context.CacheImpl, key, clone, null, taskId, providerName,
                        OpCode.Add, WriteBehindAsyncProcessor.TaskState.Execute);
                }

                //lock (_context.CacheStatsColl)
                //{
                //    _context.CacheStatsColl.AddSample(addTime.Current, 1);
                //}
            }
            catch (Exception inner)
            {
                //NCacheLog.Error(_context.CacheName, "Cache.Add()", inner.ToString());
                throw new OperationFailedException(inner.Message);
            }

            finally 
            {
                // return the added version.
                if(operationContext.Contains(OperationContextFieldName.ItemVersion))
                {
                    operationContext.RemoveValueByField(OperationContextFieldName.ItemVersion);
                }
                operationContext.Add(OperationContextFieldName.ItemVersion, e.Version);
            }

        }

        /// <summary>
        /// called from web cache to initiate the custom notifications.
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="data"></param>
        /// <param name="async"></param>

        public void SendNotification(object notifId, object data)
        {
            // cache may have expired or not initialized.
            if (!IsRunning) return;

            if (notifId != null && !notifId.GetType().IsSerializable)
                throw new ArgumentException("notifId is not serializable");
            if (data != null && !data.GetType().IsSerializable)
                throw new ArgumentException("data is not serializable");

            try
            {
                _context.CacheImpl.SendNotification(notifId, data);
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

            ExpirationHint eh = ConvHelper.MakeExpirationHint(_context.CacheRoot.Configuration.ExpirationPolicy, cce.Expiration, isAbsolute);

            if (eh != null && cce.Dependency != null)
            {
                eh = new AggregateExpirationHint(cce.Dependency, eh);
            }

            if (eh == null) eh = cce.Dependency;

            if (eh != null)
            {
                if (isResync) eh.SetBit(ExpirationHint.NEEDS_RESYNC);
            }

            AddAsync(cce.Key, cce.Value, eh, cce.SyncDependency, new PriorityEvictionHint((CacheItemPriority)priority),
                cce.Group, cce.SubGroup, cce.Flag, cce.QueryInfo, null, operationContext);
        }

        public void AddAsync(object key, object value,
            ExpirationHint expiryHint, CacheSyncDependency syncDependency, EvictionHint evictionHint,
            string group, string subGroup, OperationContext operationContext)
        {
            AddAsync(key, value, expiryHint, syncDependency, evictionHint, group, subGroup, new BitSet(), null, null,
                operationContext);
        }

        /// <summary>
        /// Overload of Add operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        public void AddAsync(object key, object value,
            ExpirationHint expiryHint, CacheSyncDependency syncDependency, EvictionHint evictionHint,
            string group, string subGroup, BitSet Flag, Hashtable queryInfo, string provider,
            OperationContext operationContext)
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
            //if (!value.GetType().IsSerializable)
            //    throw new ArgumentException("value is not serializable");
            if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                throw new ArgumentException("expiryHint is not not serializable");

            if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                throw new ArgumentException("evictionHint is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return; //throw new InvalidOperationException();

            //if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            //{
            //    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
            //        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, BlockInterval * 1000);
            //}

            //_context.AsyncProc.Enqueue(new AsyncAdd(this, key, value, expiryHint, syncDependency, evictionHint, group, subGroup, Flag, queryInfo, provider, operationContext));
            _asyncProcessor.Enqueue(new AsyncAdd(this, key, value, expiryHint, syncDependency, evictionHint, group,
                subGroup, Flag, queryInfo, provider, operationContext));
        }



        /// <summary>
        /// Internal Add operation. Does write-through as well.
        /// </summary>
        private void Add(object key, CacheEntry e, OperationContext operationContext)
        {
            object value = e.Value;
            try
            {
                //if (e.SyncDependency != null) _context.SyncManager.AddDependency(key,e.SyncDependency);
                CacheAddResult result = CacheAddResult.Failure;

                //muds:
                //Changes after NCache 3.6
                //decision was roled back. now there is no limit for session items in cache in Express Edition.
                //#if EXPRESS
                //                if (e.IsSessionItem && _context.CacheImpl.SessionCount > 1000)
                //                    throw new OperationFailedException("Maximum number of sessions reached for Express Edition.");
                //#endif
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

                //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.Add", key as string);

                result = _context.CacheImpl.Add(key, e, true, operationContext);

                switch (result)
                {
                    case CacheAddResult.Failure:
                        //_context.SyncManager.RemoveDependency(key, e.SyncDependency);
                        //throw new OperationFailedException("Generic operation failure; not enough information is available.");
                        break;

                    case CacheAddResult.NeedsEviction:
                        //_context.SyncManager.RemoveDependency(key, e.SyncDependency);
                        throw new OperationFailedException("The cache is full and not enough items could be evicted.",
                            false);

                    case CacheAddResult.KeyExists:
                        //_context.SyncManager.RemoveDependency(key, e.SyncDependency);
                        throw new OperationFailedException("The specified key already exists.", false);

                    case CacheAddResult.Success:

                        _context.PerfStatsColl.IncrementAddPerSecStats();

                        //_context.PerfStatsColl.IncrementCountStats(CacheHelper.GetLocalCount(_context.CacheImpl));
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
        public IDictionary AddEntries(object[] entries, out IDictionary itemVersions, OperationContext operationContext)
        {
            itemVersions = null;
            // check if cache is running.
            if (!IsRunning) return null;

            string[] keys = new string[entries.Length];
            object[] values = new object[entries.Length];
            //long[] sizes = null;
            //object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
            //if (dataSize != null)
            //{
            //    sizes = (long[])dataSize;
            //}
            CallbackEntry[] callbackEnteries = new CallbackEntry[entries.Length]; //Asif Imam
            ExpirationHint[] exp = new ExpirationHint[entries.Length];
            EvictionHint[] evc = new EvictionHint[entries.Length];
            CacheSyncDependency[] csd = new CacheSyncDependency[entries.Length];
            BitSet[] flags = new BitSet[entries.Length];
            Hashtable[] queryInfo = new Hashtable[entries.Length];
            GroupInfo[] groupInfo = new GroupInfo[entries.Length];

            CallbackEntry cbEntry = null;

            for (int i = 0; i < entries.Length; i++)
            {
                CompactCacheEntry cce =
                    (CompactCacheEntry)SerializationUtil.CompactDeserialize(entries[i], _context.SerializationContext);
                keys[i] = cce.Key as string;
                CacheEntry ce = MakeCacheEntry(cce);
                if (ce != null)
                {
                    if (ce.Value is CallbackEntry)
                        cbEntry = ce.Value as CallbackEntry;
                    else
                        cbEntry = null;

                    callbackEnteries[i] = cbEntry;

                    //if(sizes != null)
                    //    ce.DataSize = sizes[i];

                    object value = ce.Value as CallbackEntry;
                    values[i] = value == null ? ce.Value : ((CallbackEntry)ce.Value).Value;

                    exp[i] = ce.ExpirationHint;
                    evc[i] = ce.EvictionHint;
                    csd[i] = ce.SyncDependency;
                    queryInfo[i] = ce.QueryInfo;
                    flags[i] = ce.Flag;
                    GroupInfo gInfo = new GroupInfo(cce.Group, cce.SubGroup);

                    groupInfo[i] = gInfo;
                }
            }

            IDictionary items = Add(keys, values, callbackEnteries, exp, csd, evc, groupInfo, queryInfo, flags, null,
                out itemVersions, operationContext);
            if (items != null) CompileReturnSet(items as Hashtable);
            return items;
        }

        /// <summary>
        /// To remove success & failure-retry entries from return set in case of inproc cache
        /// </summary>
        private void CompileReturnSet(Hashtable returnSet)
        {
            if (returnSet != null && returnSet.Count > 0)
            {
                Hashtable tmp = (Hashtable)returnSet.Clone();
                foreach (DictionaryEntry entry in tmp)
                {
                    if (entry.Value is OperationResult.Status)
                    {
                        OperationResult.Status status = (OperationResult.Status)entry.Value;
                        if (status == OperationResult.Status.Success || status == OperationResult.Status.FailureRetry)
                            returnSet.Remove(entry.Key);
                    }

                }
            }
        }

        /// <summary>
        /// Add operations that takes the keys and objects as parameter and add
        /// all these key value pairs as a single bulk operation.
        /// </summary>
        //public IDictionary Add(object[] keys, object[] values, string group, string subGroup)
        //{
        //    return Add(keys, values, null, null, group, subGroup);
        //}


        /// <summary>
        /// Overload of Add operation for bulk additions. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        //public IDictionary Add(object[] keys, object[] values,
        //                     ExpirationHint expiryHint, EvictionHint evictionHint,
        //                     string group, string subGroup, OperationContext operationContext)
        //{
        //    if (keys == null) throw new ArgumentNullException("keys");
        //    if (values == null) throw new ArgumentNullException("items");
        //    if (keys.Length != values.Length)
        //        throw new ArgumentException("keys count is not equals to values count");
        //    CacheEntry[] ce = new CacheEntry[values.Length];


        //    for (int i = 0; i < values.Length; i++)
        //    {
        //        object key = keys[i];
        //        object value = values[i];

        //        if (key == null) throw new ArgumentNullException("key");
        //        if (value == null) throw new ArgumentNullException("value");

        //        if (!key.GetType().IsSerializable)
        //            throw new ArgumentException("key is not serializable");
        //        //if (!value.GetType().IsSerializable)
        //        //    throw new ArgumentException("value is not serializable");
        //        if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
        //            throw new ArgumentException("expiryHint is not serializable");
        //        if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
        //            throw new ArgumentException("evictionHint is not serializable");

        //        // Cache has possibly expired so do default.
        //        if (!IsRunning) return null; //throw new InvalidOperationException();

        //        ce[i] = new CacheEntry(value, expiryHint, evictionHint);

        //        //+ Numan@14102014: No Need to insert GroupInfo if its group property is null/empty it will only reduce cache-entry overhead

        //        GroupInfo grpInfo = null;// new GroupInfo(group, subGroup);
        //        if (!String.IsNullOrEmpty(group))
        //            grpInfo = new GroupInfo(group, subGroup);

        //        //+ Numan@14102014

        //        ce[i].GroupInfo = grpInfo;


        //    }

        //    try
        //    {
        //        //_context.PerfStatsColl.MsecPerAddBeginSample();
        //        return Add(keys, ce, operationContext);
        //        //_context.PerfStatsColl.MsecPerAddEndSample();
        //    }
        //    catch (Exception inner)
        //    {
        //        //NCacheLog.Error(_context.CacheName, "Cache.Add():", inner.ToString());
        //        throw;
        //    }

        //}

        //public IDictionary Add(string[] keys, object[] values, CallbackEntry[] callbackEnteries,
        //                        ExpirationHint[] expirations, CacheSyncDependency[] syncDependencies, EvictionHint[] evictions,
        //                        string group, string subGroup, Hashtable[] queryInfos)
        //{
        //    return Add(keys, values, callbackEnteries, expirations, syncDependencies, evictions, group, subGroup, queryInfos, null);
        //}

        /// <summary>
        /// Overload of Add operation for bulk additions. Uses EvictionHint and ExpirationHint arrays.
        /// </summary>        
        public IDictionary Add(string[] keys, object[] values, CallbackEntry[] callbackEnteries,
            ExpirationHint[] expirations, CacheSyncDependency[] syncDependencies, EvictionHint[] evictions,
            GroupInfo[] groupInfos, Hashtable[] queryInfos, BitSet[] flags, string providerName, out IDictionary itemVersions,
            OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.InsertBlk", "");

            if (keys == null) throw new ArgumentNullException("keys");
            if (values == null) throw new ArgumentNullException("items");
            if (keys.Length != values.Length) throw new ArgumentException("keys count is not equals to values count");
            //if (group == null && subGroup != null) throw new ArgumentException("Group must be specified for Sub Group");

            itemVersions = new Hashtable();

            DataSourceUpdateOptions updateOpts = this.UpdateOption(flags[0]);

            this.CheckDataSourceAvailabilityAndOptions(updateOpts);

            CacheEntry[] enteries = new CacheEntry[values.Length];
            //GroupInfo groupInfo = new GroupInfo(group, subGroup);
            //object size for inproc
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
                //if (!values[i].GetType().IsSerializable)
                //    throw new ArgumentException("value is not serializable");
                if ((expirations[i] != null) && !expirations[i].GetType().IsSerializable)
                    throw new ArgumentException("expiryHint is not serializable");
                if ((evictions[i] != null) && !evictions[i].GetType().IsSerializable)
                    throw new ArgumentException("evictionHint is not serializable");

                // Cache has possibly expired so do default.
                if (!IsRunning) return null;

                enteries[i] = new CacheEntry(values[i], expirations[i], evictions[i]);
                
                enteries[i].Version = (ulong)(DateTime.Now - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                itemVersions[keys[i]] = enteries[i].Version;

                enteries[i].SyncDependency = syncDependencies[i];

                //+ Numan@14102014: No Need to insert GroupInfo if its group property is null/empty it will only reduce cache-entry overhead
                if (groupInfos[i] != null && !String.IsNullOrEmpty(groupInfos[i].Group))
                    enteries[i].GroupInfo = groupInfos[i];
                //+ Numan@14102014

                enteries[i].QueryInfo = queryInfos[i];
                enteries[i].Flag.Data |= flags[i].Data;
                enteries[i].ProviderName = providerName;
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
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkWrite, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                IDictionary result;
                HPTimeStats addTime = new HPTimeStats();

                CacheEntry[] clone = null;
                if (updateOpts == DataSourceUpdateOptions.WriteBehind || updateOpts == DataSourceUpdateOptions.WriteThru)
                {
                    clone = new CacheEntry[enteries.Length];
                    for (int i = 0; i < enteries.Length; i++)
                    {
                        if (enteries[i].HasQueryInfo)
                            clone[i] = (CacheEntry)enteries[i].Clone();
                        else
                            clone[i] = enteries[i];
                    }
                }

                if (_isClientCache)
				{
                    operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);
				}
                addTime.BeginSample();
                result = Add(keys, enteries, operationContext);
                addTime.EndSample();

                //string[] filteredKeys = null;
                //object[] filteredValues = null;

                if (updateOpts != DataSourceUpdateOptions.None && keys.Length > result.Count)
                {
                    //    filteredKeys = new string[keys.Length - result.Count];
                    //    filteredValues = new object[keys.Length - result.Count];

                    //    for (int i = 0, j = 0; i < keys.Length; i++)
                    //    {
                    //        if (!result.Contains(keys[i]))
                    //        {
                    //            filteredKeys[j] = keys[i];
                    //            UserBinaryObject ubObject = values[i] as UserBinaryObject;
                    //            if (ubObject != null)
                    //            {
                    //                filteredValues[j] = CompressionUtil.Decompress(ubObject.GetFullObject(), enteries[i].Flag);
                    //                filteredValues[j] = Alachisoft.NCache.Util.EncryptionUtil.DecryptData((byte[])filteredValues[j], _context.SerializationContext);
                    //            }
                    //            else
                    //            {
                    //                //for local-inproc caches
                    //                filteredValues[j] = values[i];
                    //            }
                    //            //20110128
                    //            //filteredValues[j] = SerializationUtil.SafeDeserialize(filteredValues[j], _context.SerializationContext, enteries[i].Flag);
                    //            j++;
                    //        }
                    //    }

                    OperationResult[] dsResults = null;
                    string taskId = System.Guid.NewGuid().ToString();
                    if (updateOpts == DataSourceUpdateOptions.WriteThru && !_context.IsBridgeTargetCache && !_isClientCache)
                    {
                        dsResults = this._context.DsMgr.WriteThru(keys, clone, result as Hashtable, OpCode.Add,
                            providerName, operationContext);
                        //operations will be retried with previous entry
                        if (dsResults != null)
                            EnqueueRetryOperations(keys, clone, result as Hashtable, OpCode.Add, providerName, taskId,
                                operationContext);
                    }
                    else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind && !_isClientCache)
                    {
                        //CacheEntry[] centeries = new CacheEntry[1];
                        //centeries[0] = enteries[0];
                        /*Asif*/
                        this._context.DsMgr.WriteBehind(_context.CacheImpl, keys, clone, null, taskId, providerName,
                            OpCode.Add, WriteBehindAsyncProcessor.TaskState.Execute);
                    }
                }

                //lock (_context.CacheStatsColl)
                //{
                //    _context.CacheStatsColl.AddSample(addTime.Current, keys.Length);
                //}
                return result;
            }
            catch (Exception inner)
            {
                //NCacheLog.Error(_context.CacheName, "Cache.Add():", inner.ToString());
                throw;
            }

        }


        /// <summary>
        /// Overload of Add operation for bulk additions. Uses EvictionHint and ExpirationHint arrays.
        /// </summary>        
        public IDictionary Add(string[] keys, CacheEntry[] enteries, BitSet flag, string providerName, out IDictionary itemVersions,
            OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.InsertBlk", "");

            if (keys == null) throw new ArgumentNullException("keys");
            if (enteries == null) throw new ArgumentNullException("entries");

            itemVersions = new Hashtable();

            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);
            this.CheckDataSourceAvailabilityAndOptions(updateOpts);

            enteries[0].Flag.Data = flag.Data;
            long[] sizes = null;
            object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
            if (dataSize != null)
            {
                sizes = (long[])dataSize;
            }
            for (int keyCount = 0; keyCount < keys.Length; keyCount++)
            {
                enteries[keyCount].Version = (ulong)(DateTime.Now - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                itemVersions[keys[keyCount]] = enteries[keyCount].Version;
            }
          

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

                CacheEntry[] clone = null;
                if (updateOpts == DataSourceUpdateOptions.WriteBehind || updateOpts == DataSourceUpdateOptions.WriteThru)
                {
                    clone = new CacheEntry[enteries.Length];
                    for (int i = 0; i < enteries.Length; i++)
                    {
                        if (enteries[i].HasQueryInfo)
                            clone[i] = (CacheEntry)enteries[i].Clone();
                        else
                            clone[i] = enteries[i];
                    }
                }

                if (_isClientCache)
                {
                    operationContext.Add(OperationContextFieldName.ReadThruProviderName, providerName);
                }

                addTime.BeginSample();
                result = Add(keys, enteries, operationContext);
                addTime.EndSample();

                //string[] filteredKeys = null;
                //object[] filteredValues = null;

                if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                if (updateOpts != DataSourceUpdateOptions.None && keys.Length > result.Count)
                {
                    //filteredKeys = new string[keys.Length - result.Count];
                    //filteredValues = new object[keys.Length - result.Count];

                    //for (int i = 0, j = 0; i < keys.Length; i++)
                    //{
                    //    if (!result.Contains(keys[i]))
                    //    {
                    //        filteredKeys[j] = keys[i];
                    //        UserBinaryObject ubObject = enteries[i].Value as UserBinaryObject;
                    //        if (ubObject != null)
                    //        {
                    //            filteredValues[j] = CompressionUtil.Decompress(ubObject.GetFullObject(), enteries[i].Flag);
                    //            filteredValues[j] = Alachisoft.NCache.Util.EncryptionUtil.DecryptData((byte[])filteredValues[j], _context.SerializationContext);
                    //        }
                    //        else
                    //        {
                    //            //for local-inproc caches
                    //            filteredValues[j] = enteries[i].Value;
                    //        }
                    //        //20110128
                    //        //filteredValues[j] = SerializationUtil.SafeDeserialize(filteredValues[j], _context.SerializationContext, enteries[i].Flag);
                    //        j++;
                    //    }
                    //}

                    OperationResult[] dsResults = null;
                    string taskId = System.Guid.NewGuid().ToString();
                    if (updateOpts == DataSourceUpdateOptions.WriteThru && !_context.IsBridgeTargetCache && !_isClientCache)
                    {
                        dsResults = this._context.DsMgr.WriteThru(keys, clone, result as Hashtable, OpCode.Add,
                            providerName, operationContext);
                        //operations will be retried with previous entry
                        if (dsResults != null)
                            EnqueueRetryOperations(keys, clone, result as Hashtable, OpCode.Add, providerName, taskId,
                                operationContext);
                    }
                    else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind && !_isClientCache)
                    {
                        //CacheEntry[] centeries = new CacheEntry[1];
                        //centeries[0] = enteries[0];
                        /*Asif*/
                        this._context.DsMgr.WriteBehind(_context.CacheImpl, keys, clone, null, taskId, providerName,
                            OpCode.Add, WriteBehindAsyncProcessor.TaskState.Execute);
                    }
                }

                //lock (_context.CacheStatsColl)
                //{
                //    _context.CacheStatsColl.AddSample(addTime.Current, keys.Length);
                //}
                return result;
            }
           
            catch (Exception inner)
            {
                //NCacheLog.Error(_context.CacheName, "Cache.Add():", inner.ToString());
                itemVersions = null;
                throw;
            }

        }


        /// <summary>
        /// For operations,which needs to be retried.
        /// </summary>
        private void EnqueueRetryOperations(string[] keys, CacheEntry[] entries, Hashtable returnSet, OpCode opCode,
            string providerName, string taskId, OperationContext operationContext)
        {
            ArrayList operations = new ArrayList();
            DSWriteBehindOperation dsOperation = null;

            for (int i = 0; i < keys.Length; i++)
            {
                if (returnSet.Contains(keys[i]) && returnSet[keys[i]] is OperationResult.Status)
                {
                    OperationResult.Status status = (OperationResult.Status)returnSet[keys[i]];
                    if (status == OperationResult.Status.FailureRetry)
                    {
                        _context.NCacheLog.Info("Retrying Write Operation: " + opCode + " for key:" + keys[i]);
                        dsOperation = new DSWriteBehindOperation(_context, keys[i], entries[i], opCode, providerName, 0,
                            taskId, null, WriteBehindAsyncProcessor.TaskState.Execute);
                        operations.Add(dsOperation);
                    }
                }
            }
            if (operations.Count > 0)
            {
                _context.CacheImpl.EnqueueDSOperation(operations);
            }
        }

        /// <summary>
        /// Internal Add operation for bulk additions. Does write-through as well.
        /// </summary>
        private Hashtable Add(object[] keys, CacheEntry[] entries, OperationContext operationContext)
        {
            //object value = e.Value;
            try
            {
                Hashtable result = new Hashtable();
                //_context.SyncManager.AddDependency(keys, entries);
                result = _context.CacheImpl.Add(keys, entries, true, operationContext);
                if (result != null && result.Count > 0)
                {
                    //muds:
                    //there is a chance that all the keys could not be added to the 
                    //cache successfully. so remove dependency for failed keys.
                    //_context.SyncManager.RemoveDependency(keys, entries, result);

                    Hashtable tmp = (Hashtable)result.Clone();
                    IDictionaryEnumerator ide = tmp.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);
                        
                        CacheAddResult addResult = CacheAddResult.Failure;
                        if (ide.Value is CacheAddResult)
                        {
                            addResult = (CacheAddResult)ide.Value;
                            switch (addResult)
                            {
                                case CacheAddResult.Failure:
                                    //result[ide.Key] = //new OperationFailedException("Generic operation failure; not enough information is available.");
                                    break;
                                case CacheAddResult.KeyExists:
                                    result[ide.Key] = new OperationFailedException("The specified key already exists.");
                                    break;
                                case CacheAddResult.NeedsEviction:
                                    result[ide.Key] =
                                        new OperationFailedException(
                                            "The cache is full and not enough items could be evicted.");
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
            ;
            //if (entry is byte[])
            //    cce = (CompactCacheEntry)SerializationUtil.SafeDeserialize(entry, _context.SerializationContext);
            //else
            cce = (CompactCacheEntry)entry;

            CacheEntry e = MakeCacheEntry(cce);
            if (operationContext != null && operationContext.GetValueByField(OperationContextFieldName.ItemVersion) != null)
                e.Version = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);
            string group = null, subgroup = null;
            if (e.GroupInfo != null && e.GroupInfo.Group != null)
            {
                group = e.GroupInfo.Group;
                subgroup = e.GroupInfo.SubGroup;
            }
            return Insert(cce.Key, e.Value, e.ExpirationHint, e.SyncDependency, e.EvictionHint, group, subgroup,
                e.QueryInfo, e.Flag, e.LockId, e.Version, e.LockAccessType, null, e.ResyncProviderName, operationContext);
            //Insert(cce.Key, e);
        }


        /// <summary>
        /// Basic Insert operation, takes only the key and object as parameter.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Insert(object key, object value)
        {
            return Insert(key, value, null, null, null, null, null,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Overload of Insert operation. Uses an additional ExpirationHint parameter to be used 
        /// for Item Expiration Feature.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Insert(object key, object value, ExpirationHint expiryHint)
        {
            return Insert(key, value, expiryHint, null, null, null, null,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Overload of Insert operation. Uses an additional EvictionHint parameter to be used 
        /// for Item auto eviction policy.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Insert(object key, object value, EvictionHint evictionHint)
        {
            return Insert(key, value, null, null, evictionHint, null, null,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Overload of Insert operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>

        [CLSCompliant(false)]
        public ulong Insert(object key, object value,
            ExpirationHint expiryHint, CacheSyncDependency syncDependency, EvictionHint evictionHint,
            string group, string subGroup, OperationContext operationContext
            )
        {
            return Insert(key, value, expiryHint, syncDependency, evictionHint, group, subGroup, null, operationContext);
        }


        [CLSCompliant(false)]
        public ulong Insert(object key, object value,
            ExpirationHint expiryHint, CacheSyncDependency syncDependency, EvictionHint evictionHint,
            string group, string subGroup, Hashtable queryInfo, OperationContext operationContext
            )
        {
            return Insert(key, value, expiryHint, syncDependency, evictionHint, group, subGroup, queryInfo, new BitSet(),
                operationContext, null);
        }

        /// <summary>
        /// Overload of Insert operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Insert(object key, object value,
            ExpirationHint expiryHint, CacheSyncDependency syncDependency, EvictionHint evictionHint,
            string group, string subGroup, Hashtable queryInfo, BitSet flag, OperationContext operationContext,
            HPTime bridgeOperationTime)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");
            //if (!value.GetType().IsSerializable)
            //    throw new ArgumentException("value is not serializable");
            if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                throw new ArgumentException("expiryHint is not not serializable");
            if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                throw new ArgumentException("evictionHint is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning)
                return 0; //throw new InvalidOperationException();

            //if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            //{
            //    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
            //        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, _blockinterval * 1000);
            //}


            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);

            this.CheckDataSourceAvailabilityAndOptions(updateOpts);

            //+ Numan@14102014: No Need to insert GroupInfo if its group property is null/empty it will only reduce cache-entry overhead

            GroupInfo grpInfo = null;
            if (!String.IsNullOrEmpty(group))
                grpInfo = new GroupInfo(group, subGroup);

            //+ Numan@14102014


            CacheEntry e = new CacheEntry(value, expiryHint, evictionHint);
            if (operationContext != null && operationContext.GetValueByField(OperationContextFieldName.ItemVersion) != null)
                e.Version = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);
            else
                e.Version = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
            e.GroupInfo = grpInfo;

            e.SyncDependency = syncDependency;


            e.QueryInfo = queryInfo;

            e.Flag.Data |= flag.Data;

            e.BridgeOpTimeStamp = bridgeOperationTime;

            /// update the counters for various statistics
            ulong version = 0;
            try
            {
                CacheEntry clone;

                if ((updateOpts == DataSourceUpdateOptions.WriteThru ||
                     updateOpts == DataSourceUpdateOptions.WriteBehind) && e.HasQueryInfo)
                    clone = (CacheEntry)e.Clone();
                else

                    clone = e;

                _context.PerfStatsColl.MsecPerUpdBeginSample();

                version = Insert(key, e, null, 0, LockAccessType.IGNORE_LOCK, operationContext);

                _context.PerfStatsColl.MsecPerUpdEndSample();



                OperationResult dsResult = null;
                string taskId = System.Guid.NewGuid().ToString();
                if (updateOpts == DataSourceUpdateOptions.WriteThru && !_isClientCache)
                {
                    dsResult = this._context.DsMgr.WriteThru(key as string, clone, OpCode.Update, null, operationContext);
                    if (dsResult != null && dsResult.DSOperationStatus == OperationResult.Status.FailureRetry)
                    {
                        WriteOperation operation = dsResult.Operation;
                        if (operation != null)
                        {
                            //set requeue and retry and log retry failure
                            _context.NCacheLog.Info("Retrying Write Operation" + operation.OperationType +
                                                    " operation for key:" + operation.Key);
                            //creating ds operation with updated write operation
                            //DSWriteBehindOperation dsOperation = new DSWriteBehindOperation(this.Name, operation.Key, operation, OpCode.Update, null, taskId, null, WriteBehindAsyncProcessor.TaskState.Execute);
                            DSWriteBehindOperation dsOperation = new DSWriteBehindOperation(_context, operation.Key, e,
                                OpCode.Update, null, 0, taskId, null, WriteBehindAsyncProcessor.TaskState.Execute);
                            //retrying with initial entry
                            _context.CacheImpl.EnqueueDSOperation(dsOperation);
                        }

                    }
                }
                else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind &&
                         !_context.IsBridgeTargetCache && !_isClientCache)
                {
                    this._context.DsMgr.WriteBehind(_context.CacheImpl, key, clone, null, taskId, null,
                        OpCode.Update, WriteBehindAsyncProcessor.TaskState.Execute);
                }

            }
            catch (Exception inner)
            {
                //NCacheLog.Error(_context.CacheName, "Cache.Insert()", inner.ToString());
                throw;
            }

            return version;
        }


        /// <summary>
        /// Overload of Insert operation. For Bridge Operation in case of proper Item version maintained.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Insert(object key, object value,
            ExpirationHint expiryHint, CacheSyncDependency syncDependency, EvictionHint evictionHint,
            string group, string subGroup, Hashtable queryInfo, BitSet flag, ulong version,
            OperationContext operationContext, HPTime bridgeOperationTime)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");
            //if (!value.GetType().IsSerializable)
            //    throw new ArgumentException("value is not serializable");
            if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                throw new ArgumentException("expiryHint is not not serializable");
            if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                throw new ArgumentException("evictionHint is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning)
                return 0; //throw new InvalidOperationException();

            //if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            //{
            //    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
            //        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, _blockinterval * 1000);
            //}

            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);

            this.CheckDataSourceAvailabilityAndOptions(updateOpts);


            //+ Numan@14102014: No Need to insert GroupInfo if its group property is null/empty it will only reduce cache-entry overhead

            //GroupInfo grpInfo = new GroupInfo(group, subGroup);
            GroupInfo grpInfo = null;
            if (!String.IsNullOrEmpty(group))
                grpInfo = new GroupInfo(group, subGroup);

            //+ Numan@14102014



            CacheEntry e = new CacheEntry(value, expiryHint, evictionHint);
            if (operationContext != null && operationContext.GetValueByField(OperationContextFieldName.ItemVersion) != null)
                e.Version = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);
            else
                e.Version = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
            e.GroupInfo = grpInfo;

            e.SyncDependency = syncDependency;


            e.QueryInfo = queryInfo;

            e.Flag.Data |= flag.Data;

            e.BridgeOpTimeStamp = bridgeOperationTime;

            /// update the counters for various statistics
            ulong itemVersion = 0;
            try
            {

                CacheEntry clone;
                if ((updateOpts == DataSourceUpdateOptions.WriteThru ||
                     updateOpts == DataSourceUpdateOptions.WriteBehind) && e.HasQueryInfo)
                    clone = (CacheEntry)e.Clone();
                else
                    clone = e;


                _context.PerfStatsColl.MsecPerUpdBeginSample();

                itemVersion = Insert(key, e, null, version, LockAccessType.PRESERVE_VERSION, operationContext);

                _context.PerfStatsColl.MsecPerUpdEndSample();



                OperationResult dsResult = null;
                string taskId = System.Guid.NewGuid().ToString();
                if (updateOpts == DataSourceUpdateOptions.WriteThru && !_isClientCache)
                {
                    dsResult = this._context.DsMgr.WriteThru(key as string, clone, OpCode.Update, null, operationContext);
                    if (dsResult != null && dsResult.DSOperationStatus == OperationResult.Status.FailureRetry)
                    {
                        WriteOperation operation = dsResult.Operation;
                        if (operation != null)
                        {
                            //set requeue and retry and log retry failure
                            _context.NCacheLog.Info("Retrying Write Operation" + operation.OperationType +
                                                    " operation for key:" + operation.Key);
                            //creating ds operation with updated write operation
                            //DSWriteBehindOperation dsOperation = new DSWriteBehindOperation(this.Name, operation.Key, operation, OpCode.Update, null, taskId, null, WriteBehindAsyncProcessor.TaskState.Execute);
                            DSWriteBehindOperation dsOperation = new DSWriteBehindOperation(_context, operation.Key, e,
                                OpCode.Update, null, 0, taskId, null, WriteBehindAsyncProcessor.TaskState.Execute);
                            //retrying with initial entry
                            _context.CacheImpl.EnqueueDSOperation(dsOperation);
                        }
                    }
                }
                else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind &&
                         !_context.IsBridgeTargetCache && !_isClientCache)
                {
                    this._context.DsMgr.WriteBehind(_context.CacheImpl, key, clone, null, taskId, null,
                        OpCode.Update, WriteBehindAsyncProcessor.TaskState.Execute);
                }

            }
            catch (Exception inner)
            {
                //NCacheLog.Error(_context.CacheName, "Cache.Insert()", inner.ToString());
                throw;
            }

            return itemVersion;
        }

        static CacheEntry s_entry;
        // POTeam, Just a bareMinimum test to check max output of cache. => 1.9 Million
        //private Hashtable _POTeamBareMinCache = new Hashtable();

        /// <summary>
        /// Overload of Insert operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Insert(object key, object value,
            ExpirationHint expiryHint, CacheSyncDependency syncDependency, EvictionHint evictionHint,
            string group, string subGroup, Hashtable queryInfo, BitSet flag, object lockId, ulong version,
            LockAccessType accessType, string providerName, string resyncProviderName, OperationContext operationContext)
        {
            //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.Insert", "");
           
            // Cache has possibly expired so do default.
            if (!IsRunning)
                return 0; //throw new InvalidOperationException();

            //if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            //{
            //    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
            //        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, _blockinterval * 1000);
            //}


            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);

            this.CheckDataSourceAvailabilityAndOptions(updateOpts);
            CacheEntry e = null;
            if (s_entry == null)
            {
                e = ObjectPooling.ObjectPoolManager.EntryObjectPool.GetObject();//new CacheEntry(value, expiryHint, evictionHint);
                e.Value = value;
                e.ExpirationHint = expiryHint;
                e.EvictionHint = evictionHint;

                if (operationContext != null && operationContext.Contains(OperationContextFieldName.ItemVersion))
                    e.Version = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);
                else
                    e.Version = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                //+ Numan@14102014: No Need to insert GroupInfo if its group property is null/empty it will only reduce cache-entry overhead
                GroupInfo grpInfo = null;

                if (!String.IsNullOrEmpty(group))
                    grpInfo = new GroupInfo(group, subGroup);

                //+ Numan@14102014

                e.GroupInfo = grpInfo;
                e.SyncDependency = syncDependency;
                e.QueryInfo = queryInfo;
                e.Flag.Data |= flag.Data;
                e.ResyncProviderName = resyncProviderName;
                e.ProviderName = providerName;

                object dataSize = operationContext.GetValueByField(OperationContextFieldName.ValueDataSize);
                if (dataSize != null)
                    e.DataSize = Convert.ToInt64(dataSize);

               // s_entry = e;
            }
            else
                e = s_entry;
            //#if VS2005
            //            if (!String.IsNullOrEmpty(lockId as string)) e.LockId = lockId;
            //#else
            //            if (lockId != null && (string)lockId != String.Empty)
            //                e.LockId = lockId;

            //#endif

            /// update the counters for various statistics
            ulong itemVersion;
            try
            {
                CacheEntry clone;
                if ((updateOpts == DataSourceUpdateOptions.WriteThru ||
                     updateOpts == DataSourceUpdateOptions.WriteBehind) && e.HasQueryInfo)
                    clone = (CacheEntry)e.Clone();
                else
                    clone = e;

                if (_isClientCache)
                {
                    operationContext.Add(OperationContextFieldName.WriteThru, flag.IsBitSet(BitSetConstants.WriteThru));
                    operationContext.Add(OperationContextFieldName.WriteBehind, flag.IsBitSet(BitSetConstants.WriteBehind));
                    if (providerName != null)
                        operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);
                }

                _context.PerfStatsColl.MsecPerUpdBeginSample();

                itemVersion = Insert(key, e, lockId, version, accessType, operationContext);
                
                _context.PerfStatsColl.MsecPerUpdEndSample();

                if (updateOpts != DataSourceUpdateOptions.None)
                {
                    OperationResult dsResult = null;
                    string taskId = System.Guid.NewGuid().ToString();

                    if (updateOpts == DataSourceUpdateOptions.WriteThru && !_isClientCache)
                    {
                        dsResult = this._context.DsMgr.WriteThru(key as string, clone, OpCode.Update, providerName,
                            operationContext);
                        if (dsResult != null && dsResult.DSOperationStatus == OperationResult.Status.FailureRetry)
                        {
                            WriteOperation operation = dsResult.Operation;
                            if (operation != null)
                            {
                                //set requeue and retry and log retry failure
                                _context.NCacheLog.Info("Retrying Write Operation" + operation.OperationType +
                                                        " operation for key:" + operation.Key);
                                //creating ds operation with updated write operation
                                //DSWriteBehindOperation dsOperation = new DSWriteBehindOperation(this.Name, operation.Key, operation, OpCode.Update, providerName, taskId, null, WriteBehindAsyncProcessor.TaskState.Execute);
                                DSWriteBehindOperation dsOperation = new DSWriteBehindOperation(_context, operation.Key, e,
                                    OpCode.Update, providerName, 0, taskId, null,
                                    WriteBehindAsyncProcessor.TaskState.Execute);
                                _context.CacheImpl.EnqueueDSOperation(dsOperation);
                            }
                        }
                    }
                    else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind &&
                             !_context.IsBridgeTargetCache && !_isClientCache)
                    {
                        this._context.DsMgr.WriteBehind(_context.CacheImpl, key, clone, null, taskId, providerName,
                            OpCode.Update, WriteBehindAsyncProcessor.TaskState.Execute);
                    }
                }

            }
            catch (Exception inner)
            {
                //NCacheLog.Error(inner.ToString());
                throw;
            }

            return itemVersion;
        }


        [CLSCompliant(false)]
        public ulong Insert(object key, CacheEntry cacheEntry, OperationContext operationContext)
        {
            if (!IsRunning)
                return 0;

            DataSourceUpdateOptions updateOpts = this.UpdateOption(cacheEntry.Flag);

            this.CheckDataSourceAvailabilityAndOptions(updateOpts);

            cacheEntry.Version = 0;
                //if (operationContext != null && operationContext.Contains(OperationContextFieldName.ItemVersion))
                //    cacheEntry.Version = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);
                //else
                //    cacheEntry.Version = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;

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

            ExpirationHint eh = ConvHelper.MakeExpirationHint(_context.CacheRoot.Configuration.ExpirationPolicy, cce.Expiration, isAbsolute);

            if (eh != null && cce.Dependency != null)
            {
                eh = new AggregateExpirationHint(cce.Dependency, eh);
            }

            if (eh == null) eh = cce.Dependency;

            if (eh != null)
            {
                if (isResync) eh.SetBit(ExpirationHint.NEEDS_RESYNC);
            }

            InsertAsync(cce.Key, cce.Value, eh, cce.SyncDependency,
                new PriorityEvictionHint((CacheItemPriority)priority), cce.Group, cce.SubGroup, cce.Flag, cce.QueryInfo,
                null, operationContext);
        }

        public void InsertAsync(object key, object value,
            ExpirationHint expiryHint, CacheSyncDependency syncDependency, EvictionHint evictionHint,
            string group, string subGroup, OperationContext operationContext)
        {
            InsertAsync(key, value, expiryHint, syncDependency, evictionHint, group, subGroup, new BitSet(), null, null,
                operationContext);
        }

        /// <summary>
        /// Overload of Insert operation. Uses additional EvictionHint and ExpirationHint parameters.
        /// </summary>
        public void InsertAsync(object key, object value,
            ExpirationHint expiryHint, CacheSyncDependency syncDependency, EvictionHint evictionHint,
            string group, string subGroup, BitSet Flag, Hashtable queryInfo, string provider,
            OperationContext operationContext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");
            //if (!value.GetType().IsSerializable)
            //    throw new ArgumentException("value is not serializable");
            if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                throw new ArgumentException("expiryHint is not not serializable");
            if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                throw new ArgumentException("evictionHint is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return; //throw new InvalidOperationException();

            //if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            //{
            //    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
            //        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, BlockInterval * 1000);
            //}
            //_context.AsyncProc.Enqueue(new AsyncInsert(this, key, value, expiryHint, syncDependency, evictionHint, group, subGroup, Flag, queryInfo,provider, operationContext));
            _asyncProcessor.Enqueue(new AsyncInsert(this, key, value, expiryHint, syncDependency, evictionHint, group,
                subGroup, Flag, queryInfo, provider, operationContext));
        }



        /// <summary>
        /// Internal Insert operation. Does a write thru as well.
        /// </summary>
        private ulong Insert(object key, CacheEntry e, object lockId, ulong version, LockAccessType accessType,
            OperationContext operationContext)
        {
    
            object value = e.Value;
            ulong retVersion = 0;
            try
            {
                CacheInsResultWithEntry retVal = CascadedInsert(key, e, true, lockId, version, accessType,
                    operationContext);


                //insertTime.EndSample();

                switch (retVal.Result)
                {
                    case CacheInsResult.Failure:
                        break;

                    case CacheInsResult.NeedsEviction:
                    case CacheInsResult.NeedsEvictionNotRemove:
                        throw new OperationFailedException("The cache is full and not enough items could be evicted.",
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
                            "Data group of the inserted item does not match the existing item's data group.");

                    case CacheInsResult.ItemLocked:
                        throw new LockingException("Item is locked.");

                    case CacheInsResult.VersionMismatch:
                        throw new LockingException("Item does not exist at the specified version.");
                }

                if (retVal.Entry != null)
                {
                    CacheEntry oldEntry = retVal.Entry;
                    ObjectPooling.ObjectPoolManager.PayloadObjectPool.PutObject((SmallUserBinaryObject)oldEntry.Value);
                    ObjectPooling.ObjectPoolManager.EntryObjectPool.PutObject(oldEntry);
                }

                ObjectPooling.ObjectPoolManager.ResultPool.PutObject(retVal);
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

            CallbackEntry[] callbackEnteries = new CallbackEntry[entries.Length]; //Asif Imam
            ExpirationHint[] exp = new ExpirationHint[entries.Length];
            EvictionHint[] evc = new EvictionHint[entries.Length];
            CacheSyncDependency[] csd = new CacheSyncDependency[entries.Length];
            BitSet[] flags = new BitSet[entries.Length];
            Hashtable[] queryInfo = new Hashtable[entries.Length];
            GroupInfo[] groupInfos = new GroupInfo[entries.Length];
            CallbackEntry cbEntry = null;

            for (int i = 0; i < entries.Length; i++)
            {
                CompactCacheEntry cce =
                    (CompactCacheEntry)SerializationUtil.CompactDeserialize(entries[i], _context.SerializationContext);
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
                    csd[i] = ce.SyncDependency;
                    queryInfo[i] = ce.QueryInfo;
                    groupInfos[i] = ce.GroupInfo;
                    flags[i] = ce.Flag;
                }
            }

            IDictionary items = Insert(keys, values, callbackEnteries, exp, csd, evc, groupInfos, queryInfo, flags, null,
                null, out itemVersions, operationContext);
            if (items != null) CompileReturnSet(items as Hashtable);
            return items;
        }


        /// <summary>
        /// Basic Insert operation that takes keys and objects as parameter and
        /// inserts key value pairs in a single bulk operation.
        /// </summary>
        //public IDictionary Insert(object[] keys, object[] values, string group, string subGroup)
        //{
        //    return Insert(keys, values, null, null, group, subGroup);
        //}

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

            CacheEntry[] ce = new CacheEntry[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                object key = keys[i];
                object value = values[i];


                if (key == null) throw new ArgumentNullException("key");
                if (value == null) throw new ArgumentNullException("value");

                if (!key.GetType().IsSerializable)
                    throw new ArgumentException("key is not serializable");
                //if (!value.GetType().IsSerializable)
                //    throw new ArgumentException("value is not serializable");
                if ((expiryHint != null) && !expiryHint.GetType().IsSerializable)
                    throw new ArgumentException("expiryHint is not not serializable");
                if ((evictionHint != null) && !evictionHint.GetType().IsSerializable)
                    throw new ArgumentException("evictionHint is not serializable");

                // Cache has possibly expired so do default.
                if (!IsRunning) return null; //throw new InvalidOperationException();

                ce[i] = new CacheEntry(value, expiryHint, evictionHint);

                GroupInfo grpInfo = null;

                if (!String.IsNullOrEmpty(group))
                    grpInfo = new GroupInfo(group, subGroup);

                ce[i].GroupInfo = grpInfo;
            }
            /// update the counters for various statistics
            try
            {
                //_context.PerfStatsColl.MsecPerUpdBeginSample();
                IDictionary itemVersions = new Hashtable();

                return Insert(keys, ce, out itemVersions, operationContext);
                //_context.PerfStatsColl.MsecPerUpdEndSample();
            }
            catch (Exception inner)
            {
                //NCacheLog.Error(_context.CacheName, "Cache.Insert()", inner.ToString());
                throw;
            }
        }

        //public IDictionary Insert(object[] keys, object[] values, CallbackEntry[] callbackEnteries,
        //                                   ExpirationHint[] expirations, CacheSyncDependency[] syncDependencies, EvictionHint[] evictions,
        //                                   string group, string subGroup, Hashtable[] queryInfos)
        //{
        //    return Insert(keys, values, callbackEnteries, expirations, syncDependencies, evictions, group, subGroup, queryInfos, null);
        //}

        /// <summary>
        /// Overload of Insert operation for bulk inserts. Uses EvictionHint and ExpirationHint arrays.
        /// </summary>
        public IDictionary Insert(object[] keys, object[] values, CallbackEntry[] callbackEnteries,
            ExpirationHint[] expirations, CacheSyncDependency[] syncDependencies, EvictionHint[] evictions,
            GroupInfo[] groupInfos, Hashtable[] queryInfos, BitSet[] flags, string providername,
            string resyncProviderName, out IDictionary itemVersions, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.InsertBlk", "");

            if (keys == null) throw new ArgumentNullException("keys");
            if (values == null) throw new ArgumentNullException("items");
            if (keys.Length != values.Length) throw new ArgumentException("keys count is not equals to values count");
            //if (group == null && subGroup != null) throw new ArgumentException("Group must be specified for Sub Group");

            itemVersions = new Hashtable();

            DataSourceUpdateOptions updateOpts = this.UpdateOption(flags[0]);

            this.CheckDataSourceAvailabilityAndOptions(updateOpts);

            CacheEntry[] ce = new CacheEntry[values.Length];
            //GroupInfo groupInfo = new GroupInfo(group, subGroup);

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
                //if (!values[i].GetType().IsSerializable)
                //    throw new ArgumentException("value is not serializable");
                if ((expirations[i] != null) && !expirations[i].GetType().IsSerializable)
                    throw new ArgumentException("expiryHint is not not serializable");
                if ((evictions[i] != null) && !evictions[i].GetType().IsSerializable)
                    throw new ArgumentException("evictionHint is not serializable");

                // Cache has possibly expired so do default.
                if (!IsRunning) return null;

                ce[i] = new CacheEntry(values[i], expirations[i], evictions[i]);

                ce[i].SyncDependency = syncDependencies[i];

                //+ Numan@14102014: No Need to insert GroupInfo if its group property is null/empty it will only reduce cache-entry overhead
                if (groupInfos[i] != null && !String.IsNullOrEmpty(groupInfos[i].Group))
                    ce[i].GroupInfo = groupInfos[i];
                //+ Numan@14102014


                ce[i].QueryInfo = queryInfos[i];
                ce[i].Flag.Data |= flags[i].Data;
                ce[i].ProviderName = providername;
                if (sizes != null)
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
                CacheEntry[] clone = null;
                if (updateOpts == DataSourceUpdateOptions.WriteBehind || updateOpts == DataSourceUpdateOptions.WriteThru)
                {
                    clone = new CacheEntry[ce.Length];
                    for (int i = 0; i < ce.Length; i++)
                    {
                        if (ce[i].HasQueryInfo)
                            clone[i] = (CacheEntry)ce[i].Clone();
                        else
                            clone[i] = ce[i];
                    }
                }

                if (_isClientCache)
                {
                    operationContext.Add(OperationContextFieldName.WriteThruProviderName, providername);
                }
                HPTimeStats insertTime = new HPTimeStats();
                insertTime.BeginSample();

                IDictionary result = Insert(keys, ce, out itemVersions, operationContext);

                insertTime.EndSample();

                string[] filteredKeys = null;
                //object[] filteredValues = null;
                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                if (updateOpts != DataSourceUpdateOptions.None && keys.Length > result.Count)
                {
                    filteredKeys = new string[keys.Length - result.Count];
                    //filteredValues = new object[keys.Length - result.Count];

                    for (int i = 0, j = 0; i < keys.Length; i++)
                    {
                        if (!result.Contains(keys[i]))
                        {
                            filteredKeys[j] = keys[i] as string;
                            //        UserBinaryObject ubObject = values[i] as UserBinaryObject;
                            //        if (ubObject != null)
                            //        {
                            //            filteredValues[j] = CompressionUtil.Decompress(ubObject.GetFullObject(), ce[i].Flag);
                            //            filteredValues[j] = Alachisoft.NCache.Util.EncryptionUtil.DecryptData((byte[])filteredValues[j], _context.SerializationContext);
                            //        }
                            //        else
                            //            filteredValues[j] = values[i];
                            //        //20110128
                            //        //filteredValues[j] = SerializationUtil.SafeDeserialize(filteredValues[j], _context.SerializationContext, ce[i].Flag);
                            j++;
                        }
                    }
                    OperationResult[] dsResults = null;
                    string taskId = System.Guid.NewGuid().ToString();
                    if (updateOpts == DataSourceUpdateOptions.WriteThru && !_context.IsBridgeTargetCache && !_isClientCache)
                    {
                        dsResults = this._context.DsMgr.WriteThru(filteredKeys, clone, result as Hashtable,
                            OpCode.Update, null, operationContext);
                        //operations will be retried with previous entry
                        if (dsResults != null)
                            EnqueueRetryOperations(filteredKeys, clone, result as Hashtable, OpCode.Update, null, taskId,
                                operationContext);
                    }
                    else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind && !_isClientCache)
                    {
                        //CacheEntry[] centries = new CacheEntry[1];
                        //centries[0] = ce[0];
                        /*Asif*/
                        this._context.DsMgr.WriteBehind(_context.CacheImpl, filteredKeys, clone, null, taskId,
                            providername, OpCode.Update, WriteBehindAsyncProcessor.TaskState.Execute);
                    }
                }

                //lock (_context.CacheStatsColl)
                //    _context.CacheStatsColl.UpdateSample(insertTime.Current, keys.Length);

                return result;
            }
            catch (Exception inner)
            {
                //CacheTrace.error("Cache.Insert()", inner.ToString());
                throw;
            }
        }

        /// <summary>
        /// Internal Insert operation. Does a write thru as well.
        /// </summary>
        private Hashtable Insert(object[] keys, CacheEntry[] entries, out IDictionary itemVersions, OperationContext operationContext)
        {
            //object value = e.Value;
            try
            {
                //if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                //{
                //    if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkWrite, operationContext))
                //        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, _blockinterval * 1000);
                //}
                Hashtable result;
                itemVersions = new Hashtable();
                //_context.SyncManager.AddDependency(keys, entries);
                //result = _context.CacheImpl.Insert(keys, entries, true);
                result = CascadedInsert(keys, entries, true, operationContext);

                int index = 0;
                if (result != null && result.Count > 0)
                {
                    //arif: now this is handled at localcachebase
                    //muds:
                    //there is a chance that some items could not be updated successfully.
                    //so we must remove the dependency for such items.
                    //_context.SyncManager.RemoveDependency(keys, entries, result);

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
                                    //result[ide.Key] = new OperationFailedException("Generic operation failure; not enough information is available.");
                                    break;

                                case CacheInsResult.NeedsEviction:
                                    result[ide.Key] =
                                        new OperationFailedException(
                                            "The cache is full and not enough items could be evicted.");
                                    break;

                                case CacheInsResult.Success:
                                    if (_context.PerfStatsColl != null)
                                        _context.PerfStatsColl.IncrementAddPerSecStats();
                                    result.Remove(ide.Key);
                                    if(insResult.Entry != null)
                                        itemVersions[ide.Key] = insResult.Entry.Version;
                                    else
                                        itemVersions[ide.Key] = entries[index].Version;
                                    break;

                                case CacheInsResult.SuccessOverwrite:
                                    if (_context.PerfStatsColl != null)
                                        _context.PerfStatsColl.IncrementUpdPerSecStats();
                                    result.Remove(ide.Key);
                                    if(insResult.Entry != null)
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
                                    result[ide.Key] = new OperationFailedException("One of the dependency keys does not exist.");
                                    break;
                            }
                        }

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
        }
        
        public IDictionary Insert(string[] keys, CacheEntry[] entries, BitSet flag, string providername,
            string resyncProviderName, out IDictionary itemVersions, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("Cache.InsertBlk", "");

            if (keys == null) throw new ArgumentNullException("keys");
            if (entries == null) throw new ArgumentNullException("items");

            itemVersions = new Hashtable();

            entries[0].Flag.Data = flag.Data;

            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);

            this.CheckDataSourceAvailabilityAndOptions(updateOpts);

            /// update the counters for various statistics
            try
            {
                CacheEntry[] clone = null;
                if (updateOpts == DataSourceUpdateOptions.WriteBehind || updateOpts == DataSourceUpdateOptions.WriteThru)
                {
                    clone = new CacheEntry[entries.Length];
                    for (int i = 0; i < entries.Length; i++)
                    {
                        if (entries[i].HasQueryInfo)
                            clone[i] = (CacheEntry)entries[i].Clone();
                        else
                            clone[i] = entries[i];
                    }
                }
                
                for (int keyCount = 0; keyCount < keys.Length; keyCount++)
                {
                    entries[keyCount].Version = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                }

                // POTeam
                //HPTimeStats insertTime = new HPTimeStats();
                //insertTime.BeginSample();

                IDictionary result = Insert(keys, entries, out itemVersions, operationContext);

                //insertTime.EndSample();

                string[] filteredKeys = null;
                //object[] filteredValues = null;
                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                if (updateOpts != DataSourceUpdateOptions.None && keys.Length > result.Count)
                {
                    filteredKeys = new string[keys.Length - result.Count];
                    //filteredValues = new object[keys.Length - result.Count];

                    for (int i = 0, j = 0; i < keys.Length; i++)
                    {
                        if (!result.Contains(keys[i]))
                        {
                            filteredKeys[j] = keys[i] as string;
                            //UserBinaryObject ubObject = entries[i].Value as UserBinaryObject;
                            //if (ubObject != null)
                            //{
                            //    filteredValues[j] = CompressionUtil.Decompress(ubObject.GetFullObject(), entries[i].Flag);
                            //    filteredValues[j] = Alachisoft.NCache.Util.EncryptionUtil.DecryptData((byte[])filteredValues[j], _context.SerializationContext);
                            //}
                            //else
                            //    filteredValues[j] = entries[i].Value;
                            ////20110128
                            ////filteredValues[j] = SerializationUtil.SafeDeserialize(filteredValues[j], _context.SerializationContext, ce[i].Flag);
                            j++;
                        }
                    }
                    OperationResult[] dsResults = null;
                    string taskId = System.Guid.NewGuid().ToString();
                    if (updateOpts == DataSourceUpdateOptions.WriteThru && !_context.IsBridgeTargetCache && !_isClientCache)
                    {
                        dsResults = this._context.DsMgr.WriteThru(filteredKeys, clone, result as Hashtable,
                            OpCode.Update, null, operationContext);
                        //operations will be retried with previous entry
                        if (dsResults != null)
                            EnqueueRetryOperations(filteredKeys, clone, result as Hashtable, OpCode.Update, null, taskId,
                                operationContext);
                    }
                    else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind && !_isClientCache)
                    {
                        //CacheEntry[] centries = new CacheEntry[1];
                        //centries[0] = ce[0];
                        /*Asif*/
                        this._context.DsMgr.WriteBehind(_context.CacheImpl, filteredKeys, clone, null, taskId,
                            providername, OpCode.Update, WriteBehindAsyncProcessor.TaskState.Execute);
                    }
                }

                //lock (_context.CacheStatsColl)
                //    _context.CacheStatsColl.UpdateSample(insertTime.Current, keys.Length);

                return result;
            }
            catch (Exception inner)
            {
                //CacheTrace.error("Cache.Insert()", inner.ToString());
                throw;
            }
        }

#endregion



#region	/                 --- Remove ---           /

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        public CompressedValueEntry Remove(object key, OperationContext operationContext)
        {
            return Remove(key as string, new BitSet(), null, null, 0, LockAccessType.IGNORE_LOCK, null, operationContext);
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
            if (!IsRunning) return; //throw new InvalidOperationException();

            try
            {
                //					_context.PerfStatsColl.MsecPerDelBeginSample();
                HPTimeStats removeTime = new HPTimeStats();
                removeTime.BeginSample();

                //Hashtable removed = _context.CacheImpl.Remove(group, subGroup, true);
                Hashtable removed = CascadedRemove(group, subGroup, true, operationContext);

                removeTime.EndSample();

                if (removed == null) //[Asif Imasm]
                {
                    //lock (_context.CacheStatsColl)
                    //    _context.CacheStatsColl.RemoveSample(removeTime.Current, 0);
                }
                else
                {
                    //lock (_context.CacheStatsColl)
                    //    _context.CacheStatsColl.RemoveSample(removeTime.Current, removed.Count);
                }

                //					_context.PerfStatsColl.MsecPerDelEndSample();
                //					_context.PerfStatsColl.IncrementDelPerSecStats();
                //_context.PerfStatsColl.IncrementCountStats(ConvHelper.GetLocalCount(_context.CacheImpl));
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
        public CompressedValueEntry Remove(string key, BitSet flag, CallbackEntry cbEntry, object lockId, ulong version,
            LockAccessType accessType, string providerName, OperationContext operationContext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null; //throw new InvalidOperationException();

            //if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            //{
            //    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
            //        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, _blockinterval * 1000);
            //}


            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);

            this.CheckDataSourceAvailabilityAndOptions(updateOpts);

            try
            {
                //HPTimeStats removeTime = new HPTimeStats();
                //removeTime.BeginSample();
                _context.PerfStatsColl.MsecPerDelBeginSample();

                //CacheEntry e = _context.CacheImpl.Remove(key, ItemRemoveReason.Removed, true);

                object packedKey = key;
                if (_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind)
                {
                    packedKey = new object[] { key, updateOpts, cbEntry, providerName };
                }

                if (_isClientCache) 
                {
                    operationContext.Add(OperationContextFieldName.WriteThru, flag.IsBitSet(BitSetConstants.WriteThru));
                    operationContext.Add(OperationContextFieldName.WriteBehind, flag.IsBitSet(BitSetConstants.WriteBehind));
                    if (providerName != null)
                        operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);
                }
                CacheEntry e = CascadedRemove(key, packedKey, ItemRemoveReason.Removed, true, lockId, version,
                    accessType, operationContext);

                ///
                /// cascading handler
                ///
                //if (e != null && e.SyncDependency != null) _context.SyncManager.RemoveDependency(key, e.SyncDependency);

                _context.PerfStatsColl.MsecPerDelEndSample();
                _context.PerfStatsColl.IncrementDelPerSecStats();
                //removeTime.EndSample();

                if (e != null)
                {
                    OperationResult dsResult = null;
                    if (updateOpts == DataSourceUpdateOptions.WriteThru && !_context.IsBridgeTargetCache && !_isClientCache)
                    {
                        dsResult = this._context.DsMgr.WriteThru(key as string, e, OpCode.Remove, providerName,
                            operationContext);
                        if (dsResult != null && dsResult.DSOperationStatus == OperationResult.Status.FailureRetry)
                        {
                            WriteOperation operation = dsResult.Operation;
                            if (operation != null)
                            {
                                //set requeue and retry and log retry failure
                                _context.NCacheLog.Info("Retrying Write Operation" + operation.OperationType +
                                                        " operation for key:" + operation.Key);
                                //creating ds operation with updated write operation
                                //DSWriteBehindOperation dsOperation = new DSWriteBehindOperation(this.Name, operation.Key, operation, OpCode.Remove, providerName, taskId, null, WriteBehindAsyncProcessor.TaskState.Execute);
                                string taskId = System.Guid.NewGuid().ToString();
                                DSWriteBehindOperation dsOperation = new DSWriteBehindOperation(_context, operation.Key,
                                    e, OpCode.Remove, providerName, 0, taskId, null,
                                    WriteBehindAsyncProcessor.TaskState.Execute); //retrying with initial entry
                                _context.CacheImpl.EnqueueDSOperation(dsOperation);
                            }
                        }
                    }
                    else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind && !_isClientCache)
                    {
                        if (cbEntry != null)
                        {
                            if (e.Value is CallbackEntry)
                            {
                                ((CallbackEntry)e.Value).WriteBehindOperationCompletedCallback =
                                    cbEntry.WriteBehindOperationCompletedCallback;
                            }
                            else
                            {
                                cbEntry.Value = e.Value;
                                e.Value = cbEntry;
                            }
                        }
                        string taskId = System.Guid.NewGuid().ToString();
                        this._context.DsMgr.WriteBehind(_context.CacheImpl, key as string, e, null, taskId, providerName,
                            OpCode.Remove, WriteBehindAsyncProcessor.TaskState.Execute);
                    }
                }


                //lock (_context.CacheStatsColl)
                //    _context.CacheStatsColl.RemoveSample(removeTime.Current, 1);
                //_context.PerfStatsColl.IncrementCountStats(ConvHelper.GetLocalCount(_context.CacheImpl));

                if (e != null)
                {
                    CompressedValueEntry obj = new CompressedValueEntry();
                    obj.Value = e.Value; //e.DeflattedValue(_context.SerializationContext);
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
        public void Delete(string key, BitSet flag, CallbackEntry cbEntry, object lockId, ulong version,
            LockAccessType accessType, string providerName, OperationContext operationContext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return; //throw new InvalidOperationException();

            //if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            //{
            //    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
            //        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, _blockinterval * 1000);
            //}


            DataSourceUpdateOptions updateOpts = this.UpdateOption(flag);

            this.CheckDataSourceAvailabilityAndOptions(updateOpts);

            try
            {
                //HPTimeStats removeTime = new HPTimeStats();
                //removeTime.BeginSample();
                _context.PerfStatsColl.MsecPerDelBeginSample();

                //CacheEntry e = _context.CacheImpl.Remove(key, ItemRemoveReason.Removed, true);

                object packedKey = key;
                if (_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind)
                {
                    packedKey = new object[] { key, updateOpts, cbEntry, providerName };
                }

                if (_isClientCache)
                {
                    operationContext.Add(OperationContextFieldName.WriteThru, flag.IsBitSet(BitSetConstants.WriteThru));
                    operationContext.Add(OperationContextFieldName.WriteBehind, flag.IsBitSet(BitSetConstants.WriteBehind));
                    if (providerName != null)
                        operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);
                }

                CacheEntry e = CascadedRemove(key, packedKey, ItemRemoveReason.Removed, true, lockId, version,
                    accessType, operationContext);

                ///
                /// cascading handler
                ///
                //if (e != null && e.SyncDependency != null) _context.SyncManager.RemoveDependency(key, e.SyncDependency);


                _context.PerfStatsColl.MsecPerDelEndSample();
                _context.PerfStatsColl.IncrementDelPerSecStats();

                //removeTime.EndSample();

                if (e != null)
                {
                    OperationResult dsResult = null;
                    if (updateOpts == DataSourceUpdateOptions.WriteThru && !_context.IsBridgeTargetCache && !_isClientCache)
                    {
                        dsResult = this._context.DsMgr.WriteThru(key as string, e, OpCode.Remove, providerName,
                            operationContext);
                        if (dsResult != null && dsResult.DSOperationStatus == OperationResult.Status.FailureRetry)
                        {
                            WriteOperation operation = dsResult.Operation;
                            if (operation != null)
                            {
                                string taskId = System.Guid.NewGuid().ToString();
                                //set requeue and retry and log retry failure
                                _context.NCacheLog.Info("Retrying Write Operation" + operation.OperationType +
                                                        " operation for key:" + operation.Key);
                                //creating ds operation with updated write operation
                                //DSWriteBehindOperation dsOperation = new DSWriteBehindOperation(this.Name, operation.Key, operation, OpCode.Remove, providerName, taskId, null, WriteBehindAsyncProcessor.TaskState.Execute);
                                DSWriteBehindOperation dsOperation = new DSWriteBehindOperation(_context, operation.Key,
                                    e, OpCode.Remove, providerName, 0, taskId, null,
                                    WriteBehindAsyncProcessor.TaskState.Execute); //retrying with initial entry
                                _context.CacheImpl.EnqueueDSOperation(dsOperation);
                            }
                        }
                    }
                    else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind && !_isClientCache)
                    {
                        string taskId = System.Guid.NewGuid().ToString();
                        if (cbEntry != null)
                        {
                            if (e.Value is CallbackEntry)
                            {
                                ((CallbackEntry)e.Value).WriteBehindOperationCompletedCallback =
                                    cbEntry.WriteBehindOperationCompletedCallback;
                            }
                            else
                            {
                                cbEntry.Value = e.Value;
                                e.Value = cbEntry;
                            }
                        }
                        this._context.DsMgr.WriteBehind(_context.CacheImpl, key as string, e, null, taskId, providerName,
                            OpCode.Remove, WriteBehindAsyncProcessor.TaskState.Execute);
                    }
                }
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


        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        public void RemoveAsync(object key, OperationContext operationContext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            // Cache has possibly expired so do default.
            if (!IsRunning) return; //throw new InvalidOperationException();

            //if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            //{
            //    if (!_context.CacheImpl.IsOperationAllowed(key, AllowedOperationType.AtomicWrite))
            //        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, BlockInterval * 1000);
            //}

            //_context.AsyncProc.Enqueue(new AsyncRemove(this, key, operationContext));
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
        public IDictionary Remove(object[] keys, BitSet flagMap, CallbackEntry cbEntry, string providerName,
            OperationContext operationContext)
        {
            if (keys == null) throw new ArgumentNullException("keys");

            // Cache has possibly expired so do default.
            if (!IsRunning) return null; //throw new InvalidOperationException();

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkWrite, operationContext))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }

            DataSourceUpdateOptions updateOpts = UpdateOption(flagMap);

            this.CheckDataSourceAvailabilityAndOptions(updateOpts);

            try
            {
                //					_context.PerfStatsColl.MsecPerDelBeginSample();
                HPTimeStats removeTime = new HPTimeStats();
                removeTime.BeginSample();

                if (_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind)
                {
                    object pack = new object[] { keys[0], updateOpts, cbEntry, providerName };
                    keys[0] = pack;
                }

                if (_isClientCache)
                {
                    operationContext.Add(OperationContextFieldName.WriteThru, flagMap.IsBitSet(BitSetConstants.WriteThru));
                    operationContext.Add(OperationContextFieldName.WriteBehind, flagMap.IsBitSet(BitSetConstants.WriteBehind));
                    if (providerName != null)
                        operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);
                }
                //IDictionary removed = _context.CacheImpl.Remove(keys, ItemRemoveReason.Removed, true);
                IDictionary removed = CascadedRemove(keys, ItemRemoveReason.Removed, true, operationContext);

                if (removed != null && removed.Count > 0)
                {
                    if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementByDelPerSecStats(removed.Count);
                }


                removeTime.EndSample();

                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                if (updateOpts != DataSourceUpdateOptions.None && removed != null && removed.Count > 0
                    && !(_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind))
                {
                    string[] filteredKeys = null;
                    CacheEntry[] filteredEntries = null;

                    filteredKeys = new string[removed.Count];
                    filteredEntries = new CacheEntry[removed.Count];
                    int j = 0;

                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (removed[keys[i]] is CacheEntry)
                        {
                            filteredKeys[j] = keys[i] as string;
                            filteredEntries[j++] = removed[keys[i]] as CacheEntry;
                        }
                    }

                    //Incase there are exceptions in table
                    if (removed.Count > j)
                    {
                        Resize(ref filteredKeys, j);
                        Resize(ref filteredEntries, j);
                    }

                    OperationResult[] dsResults = null;
                    string taskId = System.Guid.NewGuid().ToString();
                    if (updateOpts == DataSourceUpdateOptions.WriteThru && !_context.IsBridgeTargetCache && !_isClientCache)
                    {
                        Hashtable returnset = new Hashtable();
                        dsResults = this._context.DsMgr.WriteThru(filteredKeys, filteredEntries,
                            /*removed as Hashtable*/ returnset, OpCode.Remove, providerName, operationContext);
                        //operations will be retried with previous entry
                        if (dsResults != null)
                            EnqueueRetryOperations(filteredKeys, filteredEntries, returnset, OpCode.Remove, providerName,
                                taskId, operationContext);
                    }
                    else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind && !_isClientCache)
                    {
                        if (cbEntry != null && filteredEntries != null)
                        {
                            for (int i = 0; i < filteredEntries.Length; i++)
                            {
                                if (filteredEntries[i].Value is CallbackEntry)
                                {
                                    ((CallbackEntry)filteredEntries[i].Value).WriteBehindOperationCompletedCallback =
                                        cbEntry.WriteBehindOperationCompletedCallback;
                                }
                                else
                                {
                                    cbEntry.Value = filteredEntries[i].Value;
                                    filteredEntries[i].Value = cbEntry;
                                }
                            }
                        }
                        this._context.DsMgr.WriteBehind(_context.CacheImpl, filteredKeys, filteredEntries, null, taskId,
                            providerName, OpCode.Remove, WriteBehindAsyncProcessor.TaskState.Execute);
                    }
                }

                //					_context.PerfStatsColl.MsecPerDelEndSample();
                //					_context.PerfStatsColl.IncrementDelPerSecStats();
                //_context.PerfStatsColl.IncrementCountStats(ConvHelper.GetLocalCount(_context.CacheImpl));
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
                            val.Value = entry.Value; //entry.DeflattenObject(_context.SerializationContext);
                            if (val.Value is CallbackEntry)
                                val.Value = ((CallbackEntry)val.Value).Value;
                            val.Flag = entry.Flag;
                            removed[ie.Current] = val;
                        }
                    }

                    //lock (_context.CacheStatsColl)
                    //    _context.CacheStatsColl.RemoveSample(removeTime.Current, removed.Count);
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
        public void Delete(object[] keys, BitSet flagMap, CallbackEntry cbEntry, string providerName,
            OperationContext operationContext)
        {
            if (keys == null) throw new ArgumentNullException("keys");

            // Cache has possibly expired so do default.
            if (!IsRunning) return; //throw new InvalidOperationException();

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkWrite, operationContext))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }

            DataSourceUpdateOptions updateOpts = UpdateOption(flagMap);

            this.CheckDataSourceAvailabilityAndOptions(updateOpts);

            try
            {
                //					_context.PerfStatsColl.MsecPerDelBeginSample();
                HPTimeStats removeTime = new HPTimeStats();
                removeTime.BeginSample();

                if (_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind)
                {
                    object pack = new object[] { keys[0], updateOpts, cbEntry, providerName };
                    keys[0] = pack;
                }

                if (_isClientCache)
                {
                    operationContext.Add(OperationContextFieldName.WriteThru, flagMap.IsBitSet(BitSetConstants.WriteThru));
                    operationContext.Add(OperationContextFieldName.WriteBehind, flagMap.IsBitSet(BitSetConstants.WriteBehind));
                    if (providerName != null)
                        operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);
                }
                //IDictionary removed = _context.CacheImpl.Remove(keys, ItemRemoveReason.Removed, true);
                IDictionary removed = CascadedRemove(keys, ItemRemoveReason.Removed, true, operationContext);

                if (removed != null && removed.Count > 0)
                {
                    if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementByDelPerSecStats(removed.Count);
                }

                removeTime.EndSample();
                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                if (updateOpts != DataSourceUpdateOptions.None && removed != null && removed.Count > 0
                    && !(_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind))
                {
                    string[] filteredKeys = null;
                    CacheEntry[] filteredEntries = null;

                    filteredKeys = new string[removed.Count];
                    filteredEntries = new CacheEntry[removed.Count];
                    int j = 0;

                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (removed[keys[i]] is CacheEntry)
                        {
                            filteredKeys[j] = keys[i] as string;
                            filteredEntries[j++] = removed[keys[i]] as CacheEntry;
                        }
                    }

                    //Incase there are exceptions in table
                    if (removed.Count > j)
                    {
                        Resize(ref filteredKeys, j);
                        Resize(ref filteredEntries, j);
                    }

                    OperationResult[] dsResults = null;
                    string taskId = System.Guid.NewGuid().ToString();

                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    if (updateOpts == DataSourceUpdateOptions.WriteThru && !_context.IsBridgeTargetCache && !_isClientCache)
                    {
                        Hashtable returnset = new Hashtable();
                        dsResults = this._context.DsMgr.WriteThru(filteredKeys, filteredEntries,
                            /*removed as Hashtable*/returnset, OpCode.Remove, providerName, operationContext);
                        //operations will be retried with previous entry
                        if (dsResults != null)
                            EnqueueRetryOperations(filteredKeys, filteredEntries, returnset, OpCode.Remove, providerName,
                                taskId, operationContext);
                    }
                    else if (!_context.IsClusteredImpl && updateOpts == DataSourceUpdateOptions.WriteBehind && !_isClientCache)
                    {
                        if (cbEntry != null && filteredEntries != null)
                        {
                            for (int i = 0; i < filteredEntries.Length; i++)
                            {
                                if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                                if (filteredEntries[i].Value is CallbackEntry)
                                {
                                    ((CallbackEntry)filteredEntries[i].Value).WriteBehindOperationCompletedCallback =
                                        cbEntry.WriteBehindOperationCompletedCallback;
                                }
                                else
                                {
                                    cbEntry.Value = filteredEntries[i].Value;
                                    filteredEntries[i].Value = cbEntry;
                                }
                            }
                        }
                        this._context.DsMgr.WriteBehind(_context.CacheImpl, filteredKeys, filteredEntries, null, taskId,
                            providerName, OpCode.
                                Remove, WriteBehindAsyncProcessor.TaskState.Execute);
                    }
                }

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

        ///// <summary>
        ///// Removes the objects for the given keys asynchronously from the cache.
        ///// The keys are specified as parameter.
        ///// </summary>
        //public void RemoveAsync(object[] keys)
        //{
        //    if (keys == null) throw new ArgumentNullException("keys");

        //    // Cache has possibly expired so do default.
        //    if (!IsRunning) return; //throw new InvalidOperationException();

        //    for (int i = 0; i < keys.Length; i++)
        //    {
        //        object key = keys[i];
        //        if (key == null) throw new ArgumentNullException("key");
        //        if (!key.GetType().IsSerializable)
        //            throw new ArgumentException("key is not serializable");
        //    }
        //    //_context.AsyncProc.Enqueue(new AsyncRemove(this, keys));
        //}


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
                return null; //throw new InvalidOperationException();

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
                        //[KS (identified after an issue reported by GMO): No need to fire eventys Asynchronously it was required when we used remoting handle on Cache but
                        //it is not required anymore so firing events synchronously.] 
                        //subscriber.BeginInvoke(key, new System.AsyncCallback(AddAsyncCallbackHandler), subscriber);
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
                        //[KS (identified after an issue reported by GMO): No need to fire eventys Asynchronously it was required when we used remoting handle on Cache but
                        //it is not required anymore so firing events synchronously.] 
                        //subscriber.BeginInvoke(key, new System.AsyncCallback(UpdateAsyncCallbackHandler), subscriber);
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
                data = ((CacheEntry)value).Value; //((CacheEntry)value).DeflattenObject(_context.SerializationContext);
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
                        //[KS (identified after an issue reported by GMO): No need to fire eventys Asynchronously it was required when we used remoting handle on Cache but
                        //it is not required anymore so firing events synchronously.]
                        //subscriber.BeginInvoke(key, data, reason, ((CacheEntry)value).Flag, new System.AsyncCallback(RemoveAsyncCallbackHandler), subscriber);
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
        /// This callback is called by .Net framework after asynchronous call for 
        /// OnItemRemoved has ended. 
        /// </summary>
        /// <param name="ar"></param>
        internal void ActiveQueryAsyncCallbackHandler(IAsyncResult ar)
        {
            ActiveQueryCallback subscribber = (ActiveQueryCallback)ar.AsyncState;

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
                    _activeQueryNotif -= subscribber;
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
                        //[KS (identified after an issue reported by GMO): No need to fire eventys Asynchronously it was required when we used remoting handle on Cache but
                        //it is not required anymore so firing events synchronously.] 
                        //subscriber.BeginInvoke(new System.AsyncCallback(CacheClearAsyncCallbackHandler), subscriber);
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
            CallbackEntry cbEntry = value as CallbackEntry;

            ArrayList removeCallbacklist =
                eventContext.GetValueByField(EventContextFieldName.ItemRemoveCallbackList) as ArrayList;


            //if (cbEntry != null && cbEntry.ItemRemoveCallbackListener != null && cbEntry.ItemRemoveCallbackListener.Count > 0)
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
            if (!Alachisoft.NCache.Licensing.LicenseManager.Express)
            // Numan Hanif [Express] Runtime Check for Non-Express
            {

                if (this._hashmapChanged == null) return;
                Delegate[] dlgList = this._hashmapChanged.GetInvocationList();

                NewHashmap.Serialize(newHashmap, this._context.SerializationContext, updateClientMap);

                foreach (HashmapChangedCallback subscriber in dlgList)
                {
                    try
                    {
#if !NETCORE
                        subscriber.BeginInvoke(newHashmap, null, new AsyncCallback(HashmapChangedAsyncCallbackHandler),
                            subscriber);
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
        /// <param name="cbEntry"></param>
        void ICacheEventsListener.OnWriteBehindOperationCompletedCallback(OpCode operationCode, object result,
            CallbackEntry cbEntry)
        {
            if (cbEntry.WriteBehindOperationCompletedCallback == null) return;
            if (_dataSourceUpdated == null) return;

            Delegate[] dlgList = _dataSourceUpdated.GetInvocationList();
            for (int i = dlgList.Length - 1; i >= 0; i--)
            {
                DataSourceUpdatedCallback subscriber = (DataSourceUpdatedCallback)dlgList[i];
                try
                {
#if !NETCORE
                    subscriber.BeginInvoke(result, cbEntry, operationCode,
                        new AsyncCallback(DSUpdateEventAsyncCallbackHandler), subscriber);
#elif NETCORE
                    //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                    TaskFactory factory = new TaskFactory();
                    System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(result, cbEntry, operationCode));
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

        void ICacheEventsListener.OnActiveQueryChanged(object key, QueryChangeType changeType,
            List<CQCallbackInfo> activeQueries, OperationContext operationContext, EventContext eventContext)
        {
            key = SerializationUtil.CompactSerialize(key, _context.SerializationContext);

            if (activeQueries != null && activeQueries.Count > 0)
            {
                if (_activeQueryNotif != null)
                {
                    Delegate[] dltList = _activeQueryNotif.GetInvocationList();
                    for (int i = dltList.Length - 1; i >= 0; i--)
                    {
                        ActiveQueryCallback subscriber = (ActiveQueryCallback)dltList[i];
                        try
                        {
#if !NETCORE
                            subscriber.BeginInvoke(key, changeType, activeQueries, eventContext,
                                new System.AsyncCallback(ActiveQueryAsyncCallbackHandler), subscriber);
#elif NETCORE
                            //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                            TaskFactory factory = new TaskFactory();
                            System.Threading.Tasks.Task task = factory.StartNew(() => subscriber(key, changeType, activeQueries, eventContext));
#endif
                        }
                        catch (System.Net.Sockets.SocketException e)
                        {
                            _context.NCacheLog.Error("Cache.OnActiveQueryChanged()", e.ToString());
                            _activeQueryNotif -= subscriber;
                        }
                        catch (Exception e)
                        {
                            _context.NCacheLog.Error("Cache.OnActiveQueryChanged", e.ToString());
                        }
                    }
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
                        subscriber.BeginInvoke(mode, new AsyncCallback(OperationModeAsyncCallbackHandler),subscriber);
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



        #region /                 --- Search ---           /

        public QueryResultSet Search(string query, IDictionary values, OperationContext operationContext)
        {
            if (!IsRunning) return null;

            if (query == null || query == String.Empty)
                throw new ArgumentNullException("query");

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkRead, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                return _context.CacheImpl.Search(query, values, operationContext);
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled)
                        _context.NCacheLog.Error("search operation failed. Error: " + ex.ToString());
                }
                throw;
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.Search()", inner.ToString());
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
                if (_context.NCacheLog.IsErrorEnabled)
                    _context.NCacheLog.Error("search operation failed. Error: " + ex.ToString());
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
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkRead, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                return _context.CacheImpl.SearchEntries(query, values, operationContext);
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled)
                        _context.NCacheLog.Error("search operation failed. Error: " + ex.ToString());
                }
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.SearchEnteries()", inner.ToString());
                throw;
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled)
                    _context.NCacheLog.Error("search operation failed. Error: " + ex.ToString());

                if (ex is Alachisoft.NCache.Parser.TypeIndexNotDefined)
                    throw new Runtime.Exceptions.TypeIndexNotDefined("search operation failed. Error: " + ex.Message, ex);
                if (ex is Alachisoft.NCache.Parser.AttributeIndexNotDefined)
                    throw new Runtime.Exceptions.AttributeIndexNotDefined(
                        "search operation failed. Error: " + ex.Message, ex);

                throw new OperationFailedException("search operation failed. Error: " + ex.Message, ex);
            }
        }

        public QueryResultSet SearchCQ(string query, IDictionary values, string clientUniqueId, string clientId,
            bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext,
            QueryDataFilters datafilters)
        {
            if (!IsRunning) return null;

            if (query == null || query == String.Empty)
                throw new ArgumentNullException("query");

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkWrite, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                return _context.CacheImpl.SearchCQ(query, values, clientUniqueId, clientId, notifyAdd, notifyUpdate,
                    notifyRemove, operationContext, datafilters);
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled)
                        _context.NCacheLog.Error("search operation failed. Error: " + ex.ToString());
                }
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.SearchCQ()", inner.ToString());
                throw;
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled)
                    _context.NCacheLog.Error("search operation failed. Error: " + ex.ToString());
                throw new OperationFailedException("search operation failed. Error: " + ex.Message, ex);
            }
        }

        public QueryResultSet SearchEntriesCQ(string query, IDictionary values, string clientUniqueId, string clientId,
            bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext,
            QueryDataFilters datafilters)
        {
            if (!IsRunning) return null;

            if (query == null || query == String.Empty)
                throw new ArgumentNullException("query");
            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkWrite, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                return _context.CacheImpl.SearchEntriesCQ(query, values, clientUniqueId, clientId, notifyAdd,
                    notifyUpdate, notifyRemove, operationContext, datafilters);
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled)
                        _context.NCacheLog.Error("search operation failed. Error: " + ex.ToString());
                }
                throw;
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.SearchCQEnteries()", inner.ToString());
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled)
                    _context.NCacheLog.Error("search operation failed. Error: " + ex.ToString());
                throw new OperationFailedException("search operation failed. Error: " + ex.Message, ex);
            }
        }



#endregion

#region /                 --- Cache Data Reader ---           /
        public ClusteredList<Alachisoft.NCache.Common.DataReader.ReaderResultSet> ExecuteReader(string query, IDictionary values, bool getData, int chunkSize, bool isInproc, OperationContext operationContext)
        {
            if (!IsRunning)
            {
                throw new StateTransferInProgressException("Operation could not be completed due to state transfer");

                //return null;
            }

            if (query == null || query == String.Empty)
                throw new ArgumentNullException("query");
            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkRead, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, BlockInterval * 1000);
                }
                return _context.CacheImpl.ExecuteReader(query, values, getData, chunkSize, isInproc, operationContext);
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.ExecuteReader()", inner.ToString());
                throw;
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("ExecuteReader operation failed. Error: " + ex.ToString());
                }
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("ExecuteReader operation failed. Error: " + ex.ToString());

                if (ex is Alachisoft.NCache.Parser.TypeIndexNotDefined)
                    throw new Runtime.Exceptions.TypeIndexNotDefined("ExecuteReader operation failed. Error: " + ex.Message, ex);
                if (ex is Alachisoft.NCache.Parser.AttributeIndexNotDefined)
                    throw new Runtime.Exceptions.AttributeIndexNotDefined("ExecuteReader operation failed. Error: " + ex.Message, ex);

                throw new OperationFailedException("ExecuteReader operation failed. Error: " + ex.Message, ex);
            }
        }
        public Alachisoft.NCache.Common.DataReader.ReaderResultSet GetReaderChunk(string readerId, int nextChunk, bool isInproc, OperationContext operationContext)
        {
            if (!IsRunning) return null;

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkRead, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, BlockInterval * 1000);
                }

                return _context.CacheImpl.GetReaderChunk(readerId, nextChunk, isInproc, operationContext);
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("GetReaderChunk operation failed. Error: " + ex.ToString());
                }
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("GetReaderChunk operation failed. Error: " + ex.ToString());
                throw new OperationFailedException("GetReaderChunk operation failed. Error: " + ex.Message, ex);
            }
        }
        public void DisposeReader(string readerId, OperationContext operationContext)
        {
            if (!IsRunning) return;

            try
            {
                _context.CacheImpl.DisposeReader(readerId, operationContext);
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("DisposeReader operation failed. Error: " + ex.ToString());
                }
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("DisposeReader operation failed. Error: " + ex.ToString());
                throw new OperationFailedException("DisposeReader operation failed. Error: " + ex.Message, ex);
            }
        }

        public List<Alachisoft.NCache.Common.DataReader.ReaderResultSet> ExecuteReaderCQ(string query, IDictionary values, bool getData, int chunkSize, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters)
        {
            if (!IsRunning) return null;

            if (query == null || query == String.Empty)
                throw new ArgumentNullException("query");
            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkRead, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE, BlockInterval * 1000);
                }
                return _context.CacheImpl.ExecuteReaderCQ(query, values, getData, chunkSize, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters, IsInProc);
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.ExecuteReaderCQ()", inner.ToString());
                throw;
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("ExecuteReader operation failed. Error: " + ex.ToString());
                }
                throw;
            }
            catch (StateTransferInProgressException inner)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("ExecuteReader operation failed. Error: " + ex.ToString());

                if (ex is Alachisoft.NCache.Parser.TypeIndexNotDefined)
                    throw new Runtime.Exceptions.TypeIndexNotDefined("ExecuteReader operation failed. Error: " + ex.Message, ex);
                if (ex is Alachisoft.NCache.Parser.AttributeIndexNotDefined)
                    throw new Runtime.Exceptions.AttributeIndexNotDefined("ExecuteReader operation failed. Error: " + ex.Message, ex);

                throw new OperationFailedException("ExecuteReader operation failed. Error: " + ex.Message, ex);
            }
        }


#endregion

#region /                 --- Delete/RemoveQuery ---           /

        public void DeleteQuery(string query, IDictionary values, OperationContext operationContext)
        {
            if (!IsRunning) return;

            if (query == null || query == String.Empty)
                throw new ArgumentNullException("query");
            try
            {

                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkRead, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                operationContext.Add(OperationContextFieldName.NotifyRemove, true);
                DeleteQueryResultSet result = _context.CacheImpl.DeleteQuery(query, values, true, true,
                    ItemRemoveReason.Removed, operationContext);

                operationContext.Add(OperationContextFieldName.NotifyRemove, false);
                if (result != null)
                {
                    RemoveCascadedDependencies(result.KeysDependingOnMe, operationContext);
                }

            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.DeleteQuery()", inner.ToString());
                throw;
            }
            catch (StateTransferInProgressException ex)
            {
                throw;
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled)
                        _context.NCacheLog.Error("delete query operation failed. Error: " + ex.ToString());
                }
                throw;
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled)
                    _context.NCacheLog.Error("delete query operation failed. Error: " + ex.ToString());
                throw new OperationFailedException("delete query operation failed. Error: " + ex.Message, ex);
            }
        }

        public int RemoveQuery(string query, IDictionary values, OperationContext operationContext)
        {
            if (!IsRunning)
                return 0;

            if (query == null || query == String.Empty)
                throw new ArgumentNullException("query");
            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkRead, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                operationContext.Add(OperationContextFieldName.NotifyRemove, true);
                DeleteQueryResultSet result = _context.CacheImpl.DeleteQuery(query, values, true, true,
                    ItemRemoveReason.Removed, operationContext);

                operationContext.Add(OperationContextFieldName.NotifyRemove, false);
                if (result != null)
                {
                    RemoveCascadedDependencies(result.KeysDependingOnMe, operationContext);
                }


                if (_context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.MsecPerDelEndSample();
                    _context.PerfStatsColl.IncrementDelPerSecStats(result.KeysEffectedCount +
                                                                   result.KeysDependingOnMe.Count);
                }


                if (result != null)
                    return result.KeysEffectedCount;
                else
                    return 0;

            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.RemoveQuery()", inner.ToString());
                throw;
            }
            catch (StateTransferInProgressException ex)
            {
                throw;
            }
            catch (OperationFailedException ex)
            {
                if (ex.IsTracable)
                {
                    if (_context.NCacheLog.IsErrorEnabled)
                        _context.NCacheLog.Error("Remove operation failed. Error: " + ex.ToString());
                }
                throw;
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog.IsErrorEnabled)
                    _context.NCacheLog.Error("Remove operation failed. Error: " + ex.ToString());

                if (ex is Alachisoft.NCache.Parser.TypeIndexNotDefined)
                    throw new Runtime.Exceptions.TypeIndexNotDefined("Remove operation failed. Error: " + ex.Message, ex);
                if (ex is Alachisoft.NCache.Parser.AttributeIndexNotDefined)
                    throw new Runtime.Exceptions.AttributeIndexNotDefined(
                        "Remove operation failed. Error: " + ex.Message, ex);

                throw new OperationFailedException("Remove operation failed. Error: " + ex.Message, ex);
            }
        }

#endregion

#region  /          --- Processor ---                /

        public Hashtable InvokeEntryProcessor(string[] keys, IEntryProcessor entryProcessor, Object[] arguments,
            BitSet dsWriteOption, String defaultWriteThru, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity)
            {
                ServerMonitor.LogClientActivity("Cache.invokeEntryProcessor", "");
            }

            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }

            // Cache has possibly expired so do default.
            if (!this.IsRunning)
            {
                return null;
            }

            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.AtomicRead, operationContext) ||
                    !_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.AtomicWrite, operationContext))
                {
                    _shutDownStatusLatch.WaitForAny((byte)(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE),
                        BlockInterval * 1000);
                }
            }
            if (_context.EntryProcessorManager != null)
            {
                return _context.EntryProcessorManager.ProcessEntries(keys, entryProcessor, arguments, dsWriteOption,
                    defaultWriteThru, operationContext);
            }

            return null;
        }

#endregion


#region /               --- CacheImpl Calls for Cascading Dependnecies ---          /

        internal CacheInsResultWithEntry CascadedInsert(object key, CacheEntry entry, bool notify, object lockId,
            ulong version, LockAccessType accessType, OperationContext operationContext)
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
            if (result.Entry != null && result.Result != CacheInsResult.IncompatibleGroup)
                _context.CacheImpl.RemoveCascadingDependencies(key, result.Entry, operationContext);
            
            return result;
        }


        internal Hashtable CascadedInsert(object[] keys, CacheEntry[] cacheEntries, bool notify,
            OperationContext operationContext)
        {
            if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
            {
                if (!_context.CacheImpl.IsOperationAllowed(keys, AllowedOperationType.BulkWrite, operationContext))
                    _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                        BlockInterval * 1000);
            }

            Hashtable table = _context.CacheImpl.Insert(keys, cacheEntries, notify, operationContext);
            _context.CacheImpl.RemoveCascadingDependencies(table, operationContext);
            return table;
        }

        internal CacheEntry CascadedRemove(object key, object pack, ItemRemoveReason reason, bool notify, object lockId,
            ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            object block;
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

            CacheEntry oldEntry = _context.CacheImpl.Remove(pack, reason, notify, lockId, version, accessType,
                operationContext);

            if (oldEntry != null)
                _context.CacheImpl.RemoveCascadingDependencies(key, oldEntry, operationContext);
            return oldEntry;
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

            if (_context.PerfStatsColl != null)
                _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();
            Hashtable table = _context.CacheImpl.Remove(group, subGroup, notify, operationContext);
            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();
                _context.PerfStatsColl.IncrementQueryPerSec();
                if (table != null)
                    _context.PerfStatsColl.IncrementAvgQuerySize(table.Count);
            }
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

            if (_context.PerfStatsColl != null)
                _context.PerfStatsColl.MsecPerQueryExecutionTimeBeginSample();

            Hashtable table = _context.CacheImpl.Remove(tags, comaprisonType, notify, operationContext);

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.MsecPerQueryExecutionTimeEndSample();
                _context.PerfStatsColl.IncrementQueryPerSec();
                if (table != null)
                    _context.PerfStatsColl.IncrementAvgQuerySize(table.Count);
            }

            _context.CacheImpl.RemoveCascadingDependencies(table, operationContext);
            return table;
        }

#endregion

#region IClusterEventsListener Members


        //muds:
        //if !(development || client || express)
#if !(DEVELOPMENT || CLIENT)
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

        public void OnMemberJoined(Alachisoft.NCache.Common.Net.Address clusterAddress,
            Alachisoft.NCache.Common.Net.Address serverAddress)
        {
#if !CLIENT && !DEVELOPMENT
            int clientsToDisconnect = 0;
            try
            {
#if !(DEVELOPMENT || CLIENT)
                if (!Alachisoft.NCache.Licensing.LicenseManager.Express)
                //Numan Hanif [Express] Runtime Check for Non-Express
                {
                    clientsToDisconnect = this.GetNumberOfClientsToDisconect();
                }
#endif
            }
            catch (Exception)
            {
                clientsToDisconnect = 0;
            }
#endif

            if (_memberJoined != null)
            {
                //object add = SerializationUtil.CompactSerialize(address, _context.SerializationContext);
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

                //object add = SerializationUtil.CompactSerialize(address, _context.SerializationContext);
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
        //#if !EXPRESS
        public void RegisterKeyNotificationCallback(string key, CallbackInfo updateCallback, CallbackInfo removeCallback,
            OperationContext operationContext)
        //#else
        //        internal void RegisterKeyNotificationCallback(string key, CallbackInfo updateCallback, CallbackInfo removeCallback)
        //#endif
        {
            if (!IsRunning) return; //throw new InvalidOperationException();
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
            if (!IsRunning) return; //throw new InvalidOperationException();
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
        //#if !EXPRESS
        public void UnregisterKeyNotificationCallback(string key, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        //#else
        //        internal void UnregisterKeyNotificationCallback(string key, CallbackInfo updateCallback, CallbackInfo removeCallback)
        //#endif
        {
            if (!IsRunning) return; //throw new InvalidOperationException();
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
            if (!IsRunning) return; //throw new InvalidOperationException();
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

        public void ApplyHotConfiguration(HotConfig hotConfig)
        {

            if (hotConfig != null)
            {

                if (hotConfig.BackingSource != null && this._context.DsMgr != null)
                {
                    if (hotConfig.BackingSource.Contains("backing-source"))
                    {
                        Hashtable backingSource = (Hashtable)hotConfig.BackingSource["backing-source"];
                        if (backingSource.Contains("write-thru"))
                        {
                            Hashtable writeThru = (Hashtable)backingSource["write-thru"];

                            if (Convert.ToBoolean(writeThru["enabled"]) && writeThru.Contains("write-behind"))
                            {
                                String mode = "non-batch";
                                int throttlingrate = -1;
                                int failedOpsQueue = -1;
                                int failedOpsEvictionRatio = -1;
                                int batchInterval = -1;
                                int operationDelay = -1;

                                Hashtable writeBehind = (Hashtable)writeThru["write-behind"];

                                if (writeBehind.Contains("mode"))
                                    mode = writeBehind["mode"].ToString();

                                if (writeBehind.Contains("throttling-rate-per-sec"))
                                    throttlingrate = Convert.ToInt32(writeBehind["throttling-rate-per-sec"]);

                                if (writeBehind.Contains("failed-operations-queue-limit"))
                                    failedOpsQueue = Convert.ToInt32(writeBehind["failed-operations-queue-limit"]);

                                if (writeBehind.Contains("failed-operations-eviction-ratio"))
                                    failedOpsEvictionRatio =
                                        Convert.ToInt32(writeBehind["failed-operations-eviction-ratio"]);

                                if (mode.Equals("batch") && writeBehind.Contains("batch-mode-config"))
                                {
                                    Hashtable batchConfig = (Hashtable)writeBehind["batch-mode-config"];

                                    if (batchConfig.Contains("batch-interval"))
                                        batchInterval = Convert.ToInt32(batchConfig["batch-interval"]);

                                    if (batchConfig.Contains("operation-delay"))
                                        operationDelay = Convert.ToInt32(batchConfig["operation-delay"]);
                                }

                                this._context.DsMgr.HotApplyWriteBehind(mode, throttlingrate, failedOpsQueue,
                                    failedOpsEvictionRatio, batchInterval, operationDelay);
                            }
                        }
                    }
                }

#region Error Logging Config
                if (_context.NCacheLog != null)
                {
                    if (hotConfig.IsErrorLogsEnabled)
                    {
                        if (hotConfig.IsDetailedLogsEnabled)
                        {
                            _context.NCacheLog.SetLevel("all");
                        }
                        else
                        {
                            _context.NCacheLog.SetLevel("error");
                        }
                    }
                    else
                    {
                        _context.NCacheLog.SetLevel("off");
                    }
                }


#endregion


#region EmailNotifier


                if (hotConfig.AlertNotifier != null)
                {
                    //EmailNotifier: EmailNotification config
                    if (hotConfig.AlertNotifier.Contains("alerts-types"))
                    {
                        _context.CacheAlertTypes =
                            Alachisoft.NCache.Caching.Util.AlertTypeHelper.Initialize(
                                hotConfig.AlertNotifier["alerts-types"] as IDictionary);
                    }
                    else
                    {
                        _context.CacheAlertTypes = new AlertNotificationTypes();
                    }

                    if (hotConfig.AlertNotifier.Contains("email-notification"))
                    {
                        if (_context.EmailAlertNotifier == null)
                        {
                            _context.EmailAlertNotifier = new EmailAlertNotifier();
                        }
                        else
                        {
                            _context.EmailAlertNotifier.Unintialize();
                        }
                        EmailNotifierArgs emailNotifierArgs =
                            new EmailNotifierArgs(hotConfig.AlertNotifier["email-notification"] as IDictionary, _context);
                        _context.EmailAlertNotifier.Initialize(emailNotifierArgs, _context.CacheAlertTypes);
                    }
                    else
                    {
                        _context.EmailAlertNotifier = new EmailAlertNotifier();
                    }

                    EmailAlertPropagator = _context.EmailAlertNotifier;
                    //-
                }

                //Security

                if (_context.CacheSecurityProvider == null)
                {
                    _context.CacheSecurityProvider = new ApiSecurityProvider();
                }
                else
                {
                    _context.CacheSecurityProvider.UnInitialize();
                }

                _context.CacheSecurityProvider.Initialize(hotConfig.SecurityEnabled, hotConfig.SecurityDomainController,
                    hotConfig.SecurityUsers as IDictionary);

                CacheSecurityProvider = _context.CacheSecurityProvider;



#endregion

#if ENTERPRISE
                if (this._context.CacheImpl is PartitionOfReplicasCacheBase)
                // Incase of hot apply the size of replica cache should also change
                {
                    this._context.CacheImpl.MaxSize =
                        this._context.CacheImpl.ActualStats.MaxSize = hotConfig.CacheMaxSize;
                }
                else
                {
                    this._context.CacheImpl.InternalCache.MaxSize =
                        this._context.CacheImpl.ActualStats.MaxSize = hotConfig.CacheMaxSize;
                }
#else
                this._context.CacheImpl.InternalCache.MaxSize = this._context.CacheImpl.ActualStats.MaxSize = hotConfig.CacheMaxSize;
#endif
                if (!this._context.IsStartedAsMirror)
                {
                    this._context.ExpiryMgr.CleanInterval = hotConfig.CleanInterval;
                    _context.MessageManager.SetExpirationInterval(_context.ExpiryMgr.CleanInterval);
                }
                this._context.CacheImpl.InternalCache.EvictRatio = hotConfig.EvictRatio / 100;

                // Update expiration policies
                if (this.Configuration.ExpirationPolicy.IsExpirationEnabled = hotConfig.ExpirationEnabled)
                {
                    this.Configuration.ExpirationPolicy.AbsoluteExpiration.Default = hotConfig.AbsoluteDefault;
                    this.Configuration.ExpirationPolicy.AbsoluteExpiration.DefaultEnabled = hotConfig.AbsoluteDefaultEnabled;
                    this.Configuration.ExpirationPolicy.AbsoluteExpiration.LongerEnabled = hotConfig.AbsoluteLongerEnabled;
                    this.Configuration.ExpirationPolicy.AbsoluteExpiration.Longer = hotConfig.AbsoluteLonger;
                    //  this.Configuration.ExpirationPolicy.AbsoluteExpiration.LongestEnabled = hotConfig.AbsoluteLongestEnabled;
                    //  this.Configuration.ExpirationPolicy.AbsoluteExpiration.Longest = hotConfig.AbsoluteLongest;

                    this.Configuration.ExpirationPolicy.SlidingExpiration.Default = hotConfig.SlidingDefault;
                    this.Configuration.ExpirationPolicy.SlidingExpiration.DefaultEnabled = hotConfig.DefaultSlidingEnabled;
                    this.Configuration.ExpirationPolicy.SlidingExpiration.LongerEnabled = hotConfig.SlidingLongerEnabled;
                    this.Configuration.ExpirationPolicy.SlidingExpiration.Longer = hotConfig.SlidingLonger;
                    //   this.Configuration.ExpirationPolicy.SlidingExpiration.LongestEnabled = hotConfig.SlidingLongestEnabled;
                    // this.Configuration.ExpirationPolicy.SlidingExpiration.Longest = hotConfig.SlidingLongest;

                }

                this._compressionEnabled = this._context.CompressionEnabled = hotConfig.CompressionEnabled;
                this._compressionThresholdSize = this._context.CompressionThreshold = hotConfig.CompressionThreshold;
                bool wasBridgeTargetCache = _isBridgeTargetCache && !hotConfig.IsBridgeTargetCache;
                this._isBridgeTargetCache = hotConfig.IsBridgeTargetCache;
                this._context.IsBridgeTargetCache = this._isBridgeTargetCache;

                if (wasBridgeTargetCache)
                {
                    //cache has become active now, therefore we should disconnect bridge client.
                    if (_cacheBecomeActive != null) _cacheBecomeActive(this.Name, null);
                    if (_context.CacheImpl != null) _context.CacheImpl.CacheBecomeActive();
                }

                if (this._configurationModified != null) this._configurationModified(hotConfig);

#if !EXPRESS

#region [Compact Serialization at Runtime]

                if (hotConfig.CompactSerialization != null && hotConfig.CompactSerialization.Count > 0 && hotConfig.CompactSerialization.Count != Context._cmptKnownTypesforNet.Count)
                {
                    FilterOutDotNetTypes(hotConfig.CompactSerialization, Context._cmptKnownTypesforNet, Context._cmptKnownTypesforJava,  true);

                    if ((_context.DsMgr != null && _context.DsMgr.LoadCompactTypes) || (_context.CSLMgr != null && _context.CSLMgr.IsCacheloaderEnabled))
                    {

                        if (AuthenticateFeature.IsJavaEnabled && _inProc == false && CompactRegisteredTypesForJava.Count > 0)
                            JSerializationUtil.initialize( SerializationUtil.GetProtocolStringFromTypeMap(CompactRegisteredTypesForJava), this.Name);

                        InitializeCompactFramework(Context._cmptKnownTypesforNet, !_inProc);
                        // in out proc exceptions should be thrown.
                    }

                    if (!_context.CacheImpl.IsStartedAsMirror && _compactTypeModified != null)
                        _compactTypeModified(GetUpdatedCompactTypesConfig(), null);
                }

#endregion


                if (hotConfig.BridgeConfig != null && hotConfig.BridgeConfig.Contains("bridge"))
                {

                    Hashtable bridgeProps = hotConfig.BridgeConfig["bridge"] as Hashtable;

                    if (bridgeProps != null)
                    {
                        _context.IsBridgeTargetCache = false;

                        if (bridgeProps.Contains("id"))
                        {
                            _bridgeId = bridgeProps["id"] as string;
                        }
#if ENTERPRISE
                        if (_context.BridgeReplicator == null)
                        {
                            _bridgeReplicator = new BridgeReplicator(_context);
                            _bridgeReplicator.Initialize(bridgeProps);
                            _context.BridgeReplicator = _bridgeReplicator;
                            ((CacheSyncWrapper)_context.CacheInternal).Replicator = _bridgeReplicator;
                        }
                        else
                        {
                            _context.BridgeReplicator.AddServers(bridgeProps);
                        }
#endif
                    }
                }
#endif

#if ENTERPRISE
                if (this._context.CacheImpl is PartitionOfReplicasCacheBase && !this._context.IsStartedAsMirror)
                // Incase of hot apply the size of replica cache should also change
                {
                    this._context.CacheImpl.ApplyHotConfiguration(hotConfig);
                }
#endif
            }

        }

        /// <summary>
        /// The method will send updated compact types config to all connected clients
        /// </summary>
        /// <returns></returns>
        private Hashtable GetUpdatedCompactTypesConfig()
        {
            //return SerializationUtil.GetCompactTypes(Context._cmptKnownTypesforNet, !_inProc, _cacheInfo.Name);           
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

#if !DEVELOPMENT
        public NewHashmap GetOwnerHashMap(out int bucketSize)
        {
            return _context.CacheImpl.GetOwnerHashMapTable(out bucketSize);
        }

#endif


        //#if ENTERPRISE
        //        [CLSCompliant(false)]
        //        public void ClientDataRecived(string ClientID, long DataSize, IncommingOperationType type, int OperationsCount)
        //        {
        //            if (_context.IsClusteredImpl)
        //            {
        //                _context.CacheImpl.ClientDataRecived(ClientID, DataSize, type, OperationsCount);
        //            }
        //        }
        //#endif

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

        //public PersistenceManager PersistenceManager
        //{
        //    get { return _persistenceManager; }
        //    set { _persistenceManager = value; }
        //}

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
            //throw new Exception("The method or operation is not implemented.");
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


#region MapReduce Methods

        public void SubmitMapReduceTask(Runtime.MapReduce.MapReduceTask task, string taskId,
            TaskCallbackInfo callbackInfo, Filter filter, OperationContext operationContext)
        {
            if (!IsRunning)
                return;

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.ClusterRead, null))
                    {
                        _shutDownStatusLatch.WaitForAny(
                            (byte)(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE), BlockInterval * 1000);
                    }
                }

                if (CacheType.Contains("partitioned") || CacheType.Contains("partitioned-replica") || CacheType.Contains("local-cache"))
                    _context.CacheImpl.SubmitMapReduceTask(task, taskId, callbackInfo, filter, operationContext);
                else
                    throw new NotSupportedException("This feature is not Supported in Mirror and Replicated Topology.");

            }
            catch (Exception exx)
            {
                throw new OperationFailedException(exx.Message);
            }
        }

        public void CancelTask(string taskId)
        {
            if (!IsRunning)
                return;
            try
            {
                _context.NCacheLog.CriticalInfo("Cache.CancelTask",
                    "MapReduce task with taskId '" + taskId + "' is being cancelled");
                _context.CacheImpl.CancelMapReduceTask(taskId, false);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }
        }

        public ArrayList RunningTasks
        {
            get
            {
                if (!IsRunning)
                    return null;
                try
                {
                    return _context.CacheImpl.GetRunningTasks();
                }
                catch (Exception ex)
                {
                    throw new OperationFailedException(ex.Message);
                }
            }
        }

        public Runtime.MapReduce.TaskStatus TaskStatus(string taskId)
        {
            if (!IsRunning)
                return null;
            try
            {
                return _context.CacheImpl.GetTaskStatus(taskId);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }
        }

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
            IList taskListeners = (IList)value;

            if (taskListeners != null && taskListeners.Count > 0)
            {
                for (IEnumerator it = taskListeners.GetEnumerator(); it.MoveNext(); )
                {
                    TaskCallbackInfo cbInfo = (TaskCallbackInfo)it.Current;
                    if (_connectedClients != null && _connectedClients.Contains(cbInfo.Client))
                    {
                        if (_taskCallback != null)
                        {
                            _taskCallback.Invoke(taskID, cbInfo, eventContext);
                        }
                    }
                }
            }
        }



        public List<Common.MapReduce.TaskEnumeratorResult> GetTaskEnumerator(
            Common.MapReduce.TaskEnumeratorPointer pointer, OperationContext operationContext)
        {
            if (!IsRunning)
                return null;

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.ClusterRead, null))
                    {
                        _shutDownStatusLatch.WaitForAny(
                            (byte)(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE), BlockInterval * 1000);
                    }
                }

                if (CacheType.Contains("partitioned") || CacheType.Contains("partitioned-replicas") || CacheType.Contains("local-cache"))
                    return _context.CacheImpl.GetTaskEnumerator(pointer, operationContext);
                else
                    throw new NotSupportedException(
                        "this feature is only supported in Partition and Partition of Replica Topologies");
            }
            catch (NotSupportedException ex)
            {
                _context.NCacheLog.Error("Cache.getTaskEnumerator()", ex.ToString());
                throw ex;
            }
        }

        public Common.MapReduce.TaskEnumeratorResult GetTaskNextRecord(Common.MapReduce.TaskEnumeratorPointer pointer,
            OperationContext operationContext)
        {
            if (!IsRunning)
                return null;

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.ClusterRead, null))
                    {
                        _shutDownStatusLatch.WaitForAny(
                            (byte)(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE), BlockInterval * 1000);
                    }
                }

                if (CacheType.Contains("partitioned") || CacheType.Contains("partitioned-replicas") || CacheType.Contains("local-cache"))
                    return _context.CacheImpl.GetNextRecord(pointer, operationContext);
                else
                    throw new InvalidOperationException("Invalid Operation.");
            }
            catch (NotSupportedException ex)
            {
                _context.NCacheLog.Error("Cache.GetTaskNextRecord()", ex.ToString());
                throw ex;
            }
        }


        public void RegisterTaskNotificationCallback(string taskId, TaskCallbackInfo callbackInfo, OperationContext operationContext)
        {
            if (!IsRunning)
            {
                return;
            }
            if (taskId == null || string.IsNullOrEmpty(taskId))
            {
                throw new ArgumentNullException("taskId");
            }
            if (callbackInfo == null)
            {
                throw new ArgumentNullException("callbackInfo");
            }

            try
            {
                _context.CacheImpl.RegisterTaskNotification(taskId, callbackInfo, operationContext);
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable)
                {
                    _context.NCacheLog.Error("Cache.RegisterTaskNotificationCallback() ", inner.ToString());
                }
                throw inner;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.RegisterTaskNotificationCallback() ", inner.ToString());
                throw new OperationFailedException("RegisterTaskNotificationCallback failed. Error : " + inner.Message, inner);
            }
        }

#endregion


        public bool IsServerNodeIp(Address clientAddress) ////Numan Hanif [Express]
        {

            return _context.CacheImpl.IsServerNodeIp(clientAddress);
        }

        public IPAddress ServerJustLeft //Numan Hanif [Express]
        {
            get
            {
                if (Alachisoft.NCache.Licensing.LicenseManager.Express)
                //Numan Hanif[Express] Runtime Check for Non-Express
                {
                    return _context.CacheImpl.ServerJustLeft;
                }
                else
                {
                    throw new Exception("Operation only Supported in Express");
                }
            }
        }


        public string RegisterCQ(string query, IDictionary values, string clientUniqueId, string clientId,
            bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext,
            QueryDataFilters datafilters)
        {
            if (!IsRunning) return null;

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.BulkWrite, operationContext))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                return _context.CacheImpl.RegisterCQ(query, values, clientUniqueId, clientId, notifyAdd, notifyUpdate,
                    notifyRemove, operationContext, datafilters);
            }
            catch (OperationCanceledException inner)
            {
                _context.NCacheLog.Error("Cache.RegisterContinuousQuery()", inner.ToString());
                throw;
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.RegisterContinuousQuery()", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.RegisterContinuousQuery()", inner.ToString());
                throw new OperationFailedException(
                    "RegisterContinuousQuery operation failed. Error : " + inner.Message, inner);
            }
        }

        /// <summary>
        /// Unregister the active query.
        /// </summary>
        /// <param name="queryId">Active query Id</param>
        public void UnRegisterCQ(string serverUniqueId, string clientUniqueId, string clientId)
        {
            if (!IsRunning) return;

            try
            {
                if (_shutDownStatusLatch.IsAnyBitsSet(ShutDownStatus.SHUTDOWN_INPROGRESS))
                {
                    if (!_context.CacheImpl.IsOperationAllowed(AllowedOperationType.ClusterRead, null))
                        _shutDownStatusLatch.WaitForAny(ShutDownStatus.SHUTDOWN_COMPLETED | ShutDownStatus.NONE,
                            BlockInterval * 1000);
                }

                _context.CacheImpl.UnRegisterCQ(serverUniqueId, clientUniqueId, clientId);
            }
            catch (OperationFailedException inner)
            {
                if (inner.IsTracable) _context.NCacheLog.Error("Cache.UnRegisterCQ()", inner.ToString());
                throw;
            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("Cache.UnRegisterCQ()", inner.ToString());
                throw new OperationFailedException("UnRegisterCQ operation failed. Error : " + inner.Message, inner);
            }
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

        public void HotApplyBridgeReplicator(bool isRemoved)
        {
#if !CLIENT
            if (_context != null && _context.BridgeReplicator != null && _bridgeReplicator != null)
            {
                if (!isRemoved && !_context.BridgeReplicator.IsRunning)
                {
                    _context.BridgeReplicator.Start();
                    _context.BridgeReplicator.StartStateTransfer();
                }
                else
                {
                    _bridgeReplicator.Dispose();
                    _bridgeReplicator = null;
                    _context.BridgeReplicator = null;
                }
            }
#endif
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

        void ICacheEventsListener.OnPollNotify(string clientId, short callbackId, Runtime.Events.EventType eventType)
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

        public bool CheckCacheSecurityAuthorization(string cacheId, string userId, string password)
        {
            if (userId == String.Empty) userId = null;
            if (password == String.Empty) password = null;
            if (cacheId == null) throw new Exception("Cache ID can not be null");
            bool isAuthorized = false;
            try
            {
                isAuthorized = CacheSecurityProvider.Authorize(userId, password, cacheId);
                if (!isAuthorized)
                {

                    throw new SecurityException("You do not have permissions to perform this operation");

                }
            }
            catch (Exception ex)
            {
                Common.AppUtil.LogEvent("Exception while authorizing: " + ex, System.Diagnostics.EventLogEntryType.Error);

                throw;
            }
            return isAuthorized;


        }

        public void RegisterClientActivityCallback(string clientId, CacheClientConnectivityChangedCallback callback)
        {
            _context.CacheImpl.RegisterClientActivityCallback(clientId, callback);
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

            Messaging.Message message = new Messaging.Message(messageId);

            message.PayLoad = payLoad;
            message.FlagMap = new BitSet();
            message.FlagMap.Data |= flagMap.Data;
            message.CreationTime = new DateTime(creationTime, DateTimeKind.Utc);

            message.MessageMetaData = new MessageMetaData(messageId);
            message.MessageMetaData.SubscriptionType = SubscriptionType.Subscriber;

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

        public IDictionary<string, IList<object>> GetAssignedMessages(SubscriptionInfo subscriptionInfo, OperationContext operationContext)
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
                throw new OperationFailedException("Get Message operation failed. Error :" + inner.Message, inner);
            }
        }

        public Dictionary<string, TopicStats> GetTopicsStats()
        {
            return _context.CacheImpl.GetTopicsStats();
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
                    if (counterName.Equals(CustomCounterNames.Count, StringComparison.InvariantCultureIgnoreCase) || counterName.Equals(CustomCounterNames.CacheSize, StringComparison.InvariantCultureIgnoreCase) || counterName.Equals(CounterNames.GroupIndexSize, StringComparison.InvariantCultureIgnoreCase) || counterName.Equals(CounterNames.EvictionIndexSize, StringComparison.InvariantCultureIgnoreCase) || counterName.Equals(CounterNames.ExpirationIndexSize, StringComparison.InvariantCultureIgnoreCase) || counterName.Equals(CounterNames.QueryIndexSize, StringComparison.InvariantCultureIgnoreCase))
                        value = _context.CacheImpl.GetReplicaCounters(counterName);
            }
            else
            {
                switch (counterName)
                {
                    case CustomCounterNames.RequestPerSec:
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
                    case CustomCounterNames.GeneralNotificationQueueSize:
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
                    case CustomCounterNames.RequestLogCount:
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

        public long GetMessageCount(string topicName)
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
            return _context.CacheImpl.GetMessageCount(topicName);
        }

        #endregion


        public void LogCacnelCommand (string commandType , long requestID, string clientID )
        {
            if (_context.NCacheLog !=null)
                _context.NCacheLog.CriticalInfo ("Cache.CacnelExecution()", "Command : " + commandType + " Request ID : " + requestID + " has been cancelled for client : " + clientID );
        }


    }
}
