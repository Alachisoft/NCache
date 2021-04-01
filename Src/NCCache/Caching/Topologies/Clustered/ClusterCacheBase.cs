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
using System.Linq;
using System.Threading;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Common.Mirroring;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Caching;
using RequestStatus = Alachisoft.NCache.Common.DataStructures.RequestStatus;
using System.Net;
using Alachisoft.NCache.Config.Dom;
using System.Net.Sockets;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.FeatureUsageData;
#if SERVER
using Alachisoft.NGroups;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Util;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Monitoring;
using System.Collections.Generic;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
using Alachisoft.NCache.Caching.Topologies.Clustered.Results;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Events;


using System.Diagnostics;
using System.Text;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations.Messaging;
using Alachisoft.NCache.Common.Topologies.Clustered;
using Alachisoft.NCache.Common.ErrorHandling;

#endif
using Alachisoft.NCache.Common.Resources;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// A class to serve as the base for all clustered cache implementations.
    /// </summary>
    internal class ClusterCacheBase : CacheBase, IClusterParticipant, IPresenceAnnouncement, IDistributionPolicyMember
    {
        /// <summary>
        /// An enumeration that defines the various opcodes to be used by this cache scheme.

        /// </summary>
        internal enum OpCodes
        {
            /// <summary> Periodic update sent to all the servers in the group. </summary>
            PeriodicUpdate,

            /// <summary> On demand request of current status, similar to periodic req. </summary>
            ReqStatus,

            /// <summary> Clusterwide Contains(key) request </summary>
            Contains,

            /// <summary> Clusterwide GetCount() request </summary>
            GetCount,

            /// <summary> Clusterwide Get(key) request </summary>
            Get,

            /// <summary> Clusterwide Add(key, obj) request </summary>
            Add,

            /// <summary> Clusterwide Add(key, obj) request </summary>
            Insert,

            /// <summary> Clusterwide Remove(key) request </summary>
            Remove,

            /// <summary> Clusterwide Remove(key[]) request </summary>
            RemoveRange,

            /// <summary> Clusterwide Clear() request </summary>
            Clear,

            /// <summary> Clusterwide KeyList() request </summary>
            KeyList,

            /// <summary> Clusterwide addition notification </summary>
            NotifyAdd,

            /// <summary> Clusterwide updation notification </summary>
            NotifyUpdate,

            /// <summary> Clusterwide removal notification </summary>
            NotifyRemoval,

            /// <summary> Clusterwide cache clear notification </summary>
            NotifyClear,

            NotifyCustomNotif,

            /// <summary> Clusterwide GetKeys(group, subGroup) request</summary>
            GetKeys,

            /// <summary> Clusterwide GetData(group, subGroup) request</summary>
            GetData,

            /// <summary> Clusterwide Remove(group, subGroup) request </summary>
            RemoveGroup,

            /// <summary> Clusterwide RemoveKeyDep(key) request </summary>
            RemoveKeyDep,

            /// <summary> Clusterwide Add(key, expirationHint) request </summary>
            AddHint,

            /// <summary> Clusterwide Add(key, syncDependency) request </summary>
            AddSyncDependency,

            /// <summary> Clusterwide Search(querytext) request </summary>
            Search,

            /// <summary> Clusterwide SearchEntries(querytext) request </summary>
            SearchEntries,

            /// <summary> Custom item update callback request </summary>
            NotifyCustomUpdateCallback,

            /// <summary> Custom item remove callback request </summary>
            NotifyCustomRemoveCallback,

            /// <summary> poll request for updated data</summary>
            NotifyPollRequest,

            /// <summary> Verify data integrity request </summary>
            VerifyDataIntegrity,

            ///<summary>Get Data group info request </summary>
            GetDataGroupInfo,

            ///<summary>Clusterwide GetGroup(key, group, subgroup) </summary>
            GetGroup,

            ///<summary>Registers callback with an existing item.</summary>
            RegisterKeyNotification,

            ///<summary>UnRegisters callback with an existing item.</summary>
            UnregisterKeyNotification,

            /// <summary>Replicates connection string to all nodes.</summary>
            ReplicatedConnectionString,

            ///<summary>Locks the hashmap buckets.</summary>
            LockBuckets,

            ///<summary>Release the hashmap buckets.</summary>
            ReleaseBuckets,

            AnnounceStateTransfer,

            ///<summary>Transfer a bucket from the source to destination.</summary>
            TransferBucket,

            ///<summary>Sends an acknowledgment for the completion of state transfer.</summary>
            AckStateTxfr,

            EnquireTransferableBuckets,

            //<summary>signals end of state txfr</summary>
            SignalEndOfStateTxfr,

            EmptyBucket,

            BalanceNode,

            PublishMap,

            /// <summary>
            /// Signal write behind task status
            /// </summary>
            SignalWBTState,

            /// <summary>
            /// Write behind task is completed
            /// </summary>
            WBTCompleted,

            /// <summary>
            /// Notifies to the target node when write behind task is completed
            /// </summary>
            NotifyWBTResult,

            /// <summary>
            /// Queue needs to be copied from coordinator to new node
            /// </summary>
            TransferQueue,

            AddDepKeyList,

            RemoveDepKeyList,

            UpdateIndice,

            /// <summary> Clusterwide bulk removal notification </summary>
            NotifyBulkRemoval,

            /// <summary>Represents the async replication of invalidated items</summary>
            ReplicateOperations,
            LockKey,
            UnLockKey,
            UpdateLockInfo,
            IsLocked,
            CacheBecomaeActive,
            GetTag,

            /// <summary>Operation for opening stream.</summary>
            OpenStream,

            /// <summary>Operation for closing stream.</summary>
            CloseStream,

            /// <summary>Operation for reading chunk of data from stream.</summary>
            ReadFromStream,

            /// <summary>Operation for writing chunk of data to a stream.</summary>
            WriteToStream,

            /// <summary>Operation for getting stream length.</summary>
            GetStreamLength,

            /// <summary>Operation for getting keys with specified tags.</summary>
            GetKeysByTag

            ,

            /// <summary>Active query update notification request.</summary>
            NotifyCQUpdate,

            /// <summary>Unregister continuous query.</summary>
            UnRegisterCQ,

            /// <summary>Register continuous query.</summary>
            RegisterCQ,

            /// <summary>Search continuous query.</summary>
            SearchCQ,

            /// <summary>Search entries continuous query.</summary>
            SearchEntriesCQ,

            /// <summary>
            /// Delete Query
            /// </summary>
            DeleteQuery,

            InStateTransfer,

            GetContinuousQueryStateInfo,


            GetSessionCount,

            GetNextChunk,

            RemoveByTag,

            GetFilteredPersistentEvents,

            BlockActivity,

            EnqueueWBOp,

            Poll,

            RegisterPollingNotification,
            

            /// <summary>
            /// For Dead Clients
            /// </summary>
            DeadClients,

            /// <summary>
            /// Get Request Status against inquiry command
            /// </summary>
            GetClientRequestStatus,

            /// <summary>
            /// Execute Reader
            /// </summary>
            ExecuteReader,

            /// <summary>
            /// Get Reader Chunk in Cache data reader
            /// </summary>
            GetReaderChunk,

            /// <summary>
            /// Dispose Reader
            /// </summary>
            DisposeReader,

            /// <summary>
            /// Client connectivity status
            /// </summary>
            UpdateClientStatus,

            /// <summary>
            /// Execute Reader for Continous Query
            /// </summary>
            ExecuteReaderCQ,

            CacheLoader,
            /// <summary>
            /// Inform others that a client at this node is listening for other clients activity
            /// </summary>
            RegisterClientActivityListener,


            /// <summary>
            /// Unregister the above
            /// </summary>
            UnregisterClientActivityListener,

            /// <summary>
            /// Inform clients of client activity
            /// </summary>
            InformEveryoneOfClientActivity,

            /// <summary>
            /// Remove information about dead clients from respecrive nodes
            /// </summary>
            RemoveDeadClientInfo,
            Touch,
            TopicOperation,
            PublishMessage,
            GetMessage,
            GetTopicsState,
            Message_Acknowldegment,
            AssignmentOperation,
            RemoveMessages,
            StoreMessage,
            GetAssignedMessages,
            GetTransferrableMessage,
            GetMessageList,
            StopBucketLogging,
            ReplicateJudgment,
            SetOffline,
            SetOnline,
            MergeFeasibilityChecks,
            GetMessageCount,
            
            #region MirrorAndReplicatedStateTxfr

            TransferEntries,

            #endregion

            #region IListStore

            ListOperation,

            #endregion

            #region IDictionaryStore

            DictionaryOperation,

            #endregion

            #region IHashSetStore

            HashSetOperation,

            #endregion


            #region IQueueStore

            QueueOperation,

            #endregion

            #region ICollection

            CollectionOperation,

            #endregion

            #region ICounterStore

            CounterOperation,

            #endregion

            #region Collection Notification Registration

            RegisterCollectionNotification,
            UnregisterCollectionNotification,
            
            #endregion

            /// <summary>
            /// Clusterwide GetEntryAttributes(keys,columns,operationcontext) request
            /// </summary>
            GetAttribs,



            DryPoll,

            ModuleOperation,
            GetModuleState,
            ResetClusterAfterMaintaiance, 
            SurrogateCommand,
            NotifyOldCustomUpdateCallback,
            NotifyOldCustomRemoveCallback,
            NotifyOldAdd,
            NotifyOldUpdate,
            NotifyOldRemoval

        }

        protected int _onSuspectedWait = 5000;
        /// <summary> The default interval for statistics replication. </summary>
        protected long _statsReplInterval = 5000;

        protected const long forcedViewId = -5;
        //This id is sent by client to a single node, to direct node to  perform cluster operation possibly on replica as well.

        /// <summary> The cluster service. </summary>
        private ClusterService _cluster;

        /// <summary> The listener of the cluster events like member joined etc. etc. </summary>
        private IClusterEventsListener _clusterListener;

        /// <summary> The statistics for this cache scheme. </summary>
        internal ClusterCacheStatistics _stats;

        /// <summary> The runtime status of this node. </summary>
        internal Latch _statusLatch = new Latch();

        /// <summary> The initialization status status of this node. </summary>
        protected Latch _initialJoiningStatusLatch;

        /// <summary> keeps track of all server members </summary>
        protected bool _clusteredExceptions = true;

        protected bool _asyncOperation = true;
        protected FunctionObjectProvider _functionProvider = new FunctionObjectProvider(10);

        /// <summary> The physical storage for this cache </summary>
        internal CacheBase _internalCache;

        /// <summary> Contains CacheNodeInformation</summary>
        protected Hashtable _nodeInformationTable;

        protected long _autoBalancingInterval = 180 * 1000; //3 minutes

        /// <summary>
        /// The threshold that drives when to start auto load balancing
        /// on a node. Default value 10 means that auto load balancing will start when
        /// the node will have 10% more data than the current average data per node.
        /// </summary>
        protected int _autoBalancingThreshold = 60; //60% of the average data size per node

        protected bool _isAutoBalancingEnabled = false;

        protected ArrayList _hashmap;

        protected Hashtable _bucketsOwnershipMap;

        protected object _txfrTaskMutex;

        private string _nodeName;

        private int _taskSequenceNumber = 0;

        Hashtable _wbQueueTransferCorresponders = new Hashtable();

        private bool _hasDisposed = false;

        internal Hashtable _bucketStateTxfrStatus = new Hashtable();

        internal Hashtable _shutdownServers = new Hashtable();

        private bool isCQStateTransfer;
        private bool _requiresMessageStateTansfer = true;

        private HashVector<Address, HashSet<string>> _clientActivityListenersOnOtherNodes = new HashVector<Address, HashSet<string>>();

        private readonly object _registerLock = new object(), _otherNodesRegisterLock = new object();

        protected int _serverFailureWaitTime = 2000;//time in msec
        private bool _enableGeneralEvents = false;

        private EventManager _eventManager = new EventManager();
        //private int oldClients = 0;
        private static int oldClients = 0;
        protected ReplicationOperation GetClearReplicationOperation(int opCode, object info)
        {
            return GetReplicationOperation(opCode, info, 2, null, 0);
        }

        protected ReplicationOperation GetReplicationOperation(int opCode, object info, int operationSize,
            Array userPayLoad, long payLoadSize)
        {
            DictionaryEntry entry = new DictionaryEntry(opCode, info);
            ReplicationOperation operation = new ReplicationOperation(entry, operationSize, userPayLoad, payLoadSize);

            return operation;
        }

        protected internal long GetClientLastViewId(OperationContext operationContext)
        {
            long ClientLastViewId = -1;
            object clientLastViewId = operationContext.GetValueByField(OperationContextFieldName.ClientLastViewId);
            if (clientLastViewId != null)
            {
                ClientLastViewId = Convert.ToInt64(clientLastViewId);
            }
            return ClientLastViewId;
        }

        protected internal string GetIntendedRecipient(OperationContext operationContext)
        {
            string IntendedRecipient = "";
            object intendedRecipient = operationContext.GetValueByField(OperationContextFieldName.IntendedRecipient);

            if (intendedRecipient != null)
            {
                IntendedRecipient = intendedRecipient.ToString();

                ArrayList nodes = _stats.Nodes;
                if (nodes != null)
                {
                    foreach (NodeInfo node in nodes)
                    {
                        if (node.RendererAddress != null &&
                            node.RendererAddress.IpAddress.ToString().Equals(IntendedRecipient))
                        {
                            return node.Address.IpAddress.ToString();
                        }
                    }
                }
            }

            return IntendedRecipient;
        }

        protected internal Address GetReaderRecipient(OperationContext operationContext)
        {
            Address IntendedRecipient = default(Address);
            object intendedRecipient = operationContext.GetValueByField(OperationContextFieldName.IntendedRecipient);

            if (intendedRecipient != null)
            {
                IntendedRecipient = ParseRecipent(intendedRecipient.ToString());

                ArrayList nodes = _stats.Nodes;
                if (nodes != null)
                {
                    foreach (NodeInfo node in nodes)
                    {
                        if (node.RendererAddress != null && node.RendererAddress.IpAddress.Equals(IntendedRecipient.IpAddress))
                        {
                            return new Address(node.Address.IpAddress, IntendedRecipient.Port);
                        }
                    }
                }
            }

            return IntendedRecipient;
        }

        public Address ParseRecipent(string address)
        {
            string[] hostPort = address.Split(':');

            return new Address(hostPort[0], hostPort.Length > 1 ? Convert.ToInt32(hostPort[1]) : 0);
        }

        /// <summary>
        /// Returns the cache local to the node, i.e., internal cache.
        /// </summary>
        protected internal override CacheBase InternalCache
        {
            get { return _internalCache; }
        }

        public override TypeInfoMap TypeInfoMap
        {
            get { return InternalCache.TypeInfoMap; }
        }

        public bool IsCQStateTransfer
        {
            get { return isCQStateTransfer; }
            set { isCQStateTransfer = value; }

        }
        public bool RequiresMessageStateTransfer
        {
            get { return _requiresMessageStateTansfer; }
            set { _requiresMessageStateTansfer = value; }
        }

        public bool RequiresModuleStateTransfer
        {
            get; set;
        }

        public bool HasDisposed
        {
            get { return _hasDisposed; }
            set { _hasDisposed = value; }
        }

        public virtual bool IsRetryOnSuspected
        {
            get { return false; }
        }

        public virtual bool  ShouldDryPoll()
        {
            return false;
        }

        /// <summary>
        /// Get next task's sequence nuumber
        /// </summary>
        /// <returns></returns>
        protected int NextSequence()
        {
            return Interlocked.Increment(ref _taskSequenceNumber);
        }

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public ClusterCacheBase(IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(properties, listener, context)
        {
            this._nodeInformationTable = Hashtable.Synchronized(new Hashtable(10));

            _stats = new ClusterCacheStatistics();

            _stats.InstanceName = _context.PerfStatsColl.InstanceName;

            _nodeName = Environment.MachineName.ToLower();
            if (ServiceConfiguration.ServerFailureRetryDelayInterval > 0)
                _serverFailureWaitTime = ServiceConfiguration.ServerFailureRetryDelayInterval;

        }

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public ClusterCacheBase(IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context,
            IClusterEventsListener clusterListener)
            : base(properties, listener, context)
        {
            this._nodeInformationTable = Hashtable.Synchronized(new Hashtable(10));

            _stats = new ClusterCacheStatistics();

            _stats.InstanceName = _context.PerfStatsColl.InstanceName;


            _clusterListener = clusterListener;

            _nodeName = Environment.MachineName.ToLower();

            if (ServiceConfiguration.ServerFailureRetryDelayInterval > 0)
                _serverFailureWaitTime = ServiceConfiguration.ServerFailureRetryDelayInterval;


        }

        /// <summary>
        /// Perform a dummy get operation on cluster that triggers index updates on all
        /// the nodes in the cluster, has no other particular purpose.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        protected virtual void UpdateIndices(object key, bool async, OperationContext operationContext)
        {
            if (Cluster.Servers != null && Cluster.Servers.Count > 1)
            {
                if (async)
                {
                    if (_context.AsyncProc != null)
                        _context.AsyncProc.Enqueue(new UpdateIndicesTask(this, key));
                }
                else
                    UpdateIndices(key, operationContext);
            }

        }

        public virtual void UpdateLocalBuckets()
        {
        }

        public virtual void UpdateIndices(object key, OperationContext operationContext)
        {

        }

        public override void NotifyBlockActivity(string uniqueId, long interval)
        {
            try
            {
                Function func = new Function((int)OpCodes.BlockActivity,
                    new object[] { uniqueId, _cluster.LocalAddress, interval }, false);
                RspList results = Cluster.Broadcast(func, GroupRequest.GET_ALL, false, Priority.High);
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("ClusterCacheBase.NotifyBlockActivity", e.ToString());
            }

        }

        public override void NotifyUnBlockActivity(string uniqueId)
        {
            Address server = (Address)_cluster.Renderers[Cluster.LocalAddress];

            if (server != null)
            {
                if (server.IpAddress != null)
                {
                    _context.CacheRoot.NotifyUnBlockActivityToClients(uniqueId, server.IpAddress.ToString(), server.Port);
                }
                _shutdownServers.Remove(Cluster.LocalAddress);
            }
        }

     
        
        public override void WindUpReplicatorTask()
        {

        }

        public override void WaitForReplicatorTask(long interval)
        {
        }

        public override List<ShutDownServerInfo> GetShutDownServers()
        {
            List<ShutDownServerInfo> ssServers = null;
            if (_shutdownServers != null && _shutdownServers.Count > 0)
            {
                foreach (ShutDownServerInfo info in _shutdownServers.Values)
                {
                    ssServers.Add(info);
                }
            }
            return ssServers;
        }

        private Address GetClientMappedServerAddress(Address clusterAddress)
        {
            Address serverAddress = clusterAddress;
            if (Cluster.Renderers != null)
            {
                if (Cluster.Renderers.Contains(clusterAddress))
                {
                    Address mappedAddress = (Address)Cluster.Renderers[clusterAddress];
                    serverAddress = new Address(mappedAddress.IpAddress.ToString(), mappedAddress.Port);
                }
            }
            return serverAddress;
        }

        protected ArrayList GetClientMappedServers(ArrayList servers)
        {
            ArrayList mappedServers = new ArrayList();
            foreach (Address server in servers)
            {
                Address mapped = GetClientMappedServerAddress(server);
                if (!mappedServers.Contains(mapped))
                    mappedServers.Add(mapped);
            }

            return mappedServers;
        }

        public override bool IsShutdownServer(Address server)
        {
            if (_shutdownServers != null)
            {
                if (_shutdownServers.Contains(server))
                    return true;
            }
            return false;
        }

        public override bool IsOperationAllowed(object key, AllowedOperationType opType)
        {
            return true;
        }

        public override bool IsOperationAllowed(IList key, AllowedOperationType opType,
            OperationContext operationContext)
        {
            return true;
        }

        public override bool IsOperationAllowed(AllowedOperationType opType, OperationContext operationContext)
        {
            return true;
        }
        

        public override bool IsClusterInStateTransfer()
        {
            return false;
        }
     
        internal override bool IsClusterAvailableForMaintenance()
        {
            return !Cluster.IsClusterUnderStateTransfer();
        }

        public virtual void InitializePhase2()
        {
            if (Cluster != null) Cluster.InitializePhase2();
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (_nodeInformationTable != null)
            {
                lock (_nodeInformationTable.SyncRoot) _nodeInformationTable.Clear();
            }

            _statusLatch.Clear();
            if (_cluster != null)
            {
                _cluster.Dispose();
                _cluster = null;
            }

            _stats = null;
            base.Dispose();
        }

        #endregion

        internal override void StopServices()
        {
            if (_cluster != null)
            {
                _cluster.StopServices();
            }
        }

        public ClusterService Cluster
        {
            get { return _cluster; }
        }

        /// <summary> The hashtable that contains members and their info. </summary>
        protected ArrayList Members
        {
            get { return _cluster.Members; }
        }

        protected ArrayList ValidMembers
        {
            get { return _cluster.ValidMembers; }
        }

        protected ArrayList Servers
        {
            get { return _cluster.Servers; }
        }

        public virtual new ArrayList ActiveServers
        {
            get { return this.Members; }
        }

        /// <summary> The local address of this instance. </summary>
        protected Address LocalAddress
        {
            get { return _cluster.LocalAddress; }
        }

        /// <summary>
        /// returns the statistics of the Clustered Cache.
        /// </summary>
        public override CacheStatistics Statistics
        {
            get { return _stats.Clone() as CacheStatistics; }
        }

        public override List<CacheNodeStatistics> GetCacheNodeStatistics()
        {
            List<CacheNodeStatistics> statistics = new List<CacheNodeStatistics>();

            CacheNodeStatistics nodeStats = new CacheNodeStatistics(new ServerNode(null, Cluster.LocalAddress));
            nodeStats.DataSize = InternalCache.Size;
            nodeStats.ItemCount = InternalCache.Count;
            nodeStats.TotalCacheSize = InternalCache.MaxSize;
            nodeStats.Status = GetNodeStatus();
            statistics.Add(nodeStats);

            return statistics;
        }

        protected virtual CacheNodeStatus GetNodeStatus()
        {
            return CacheNodeStatus.Running;
        }

        internal override CacheStatistics ActualStats
        {
            get { return _stats; }
        }
        
        protected virtual byte GetFirstResponse
        {
            get { return GroupRequest.GET_FIRST; }
        }

        protected virtual byte GetAllResponses
        {
            get { return GroupRequest.GET_ALL; }
        }

       

        #region	/                 --- Cluster Initialization ---           /

        /// <summary>
        /// Method that allows the object to initialize itself. Passes the property map down
        /// the object hierarchy so that other objects may configure themselves as well..
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        protected override void Initialize(IDictionary cacheClasses, IDictionary properties)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                base.Initialize(cacheClasses, properties);
                if (properties.Contains("stats-repl-interval"))
                {
                    long val =
                        Convert.ToInt64(Convert.ToString(properties["stats-repl-interval"]).TrimEnd('s', 'e', 'c'));
                    if (val < 1) val = 1;
                    if (val > 300) val = 300;
                    val = val * 1000;
                    _statsReplInterval = val;
                }


                if (properties.Contains("data-load-balancing"))
                {
                    Hashtable autoBalancingProps = (Hashtable)properties["data-load-balancing"];

                    if (autoBalancingProps != null && autoBalancingProps.Count > 0)
                    {
                        IDictionaryEnumerator ide = autoBalancingProps.GetEnumerator();

                        while (ide.MoveNext())
                        {
                            switch (ide.Key as string)
                            {
                                case "enabled":
                                    this._isAutoBalancingEnabled = Convert.ToBoolean(ide.Value);
                                    break;

                                case "auto-balancing-threshold":
                                    this._autoBalancingThreshold = Convert.ToInt32(ide.Value);
                                    break;

                                case "auto-balancing-interval":
                                    this._autoBalancingInterval = Convert.ToInt64(ide.Value);
                                    this._autoBalancingInterval *= 1000; //convert into miliseconds
                                    break;
                            }
                        }
                    }

                    if (_isAutoBalancingEnabled)
                        FeatureUsageCollector.Instance.GetFeature(FeatureEnum.auto_load_balancing).UpdateUsageTime();
                }

                if (properties.Contains("async-operation"))
                {
                    _asyncOperation = Convert.ToBoolean(properties["async-operation"]);
                }
            }
            catch (Exception e)
            {
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }

        /// <summary>
        /// Method that allows the object to initialize itself. Passes the property map down
        /// the object hierarchy so that other objects may configure themselves as well..
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        protected virtual void InitializeCluster(IDictionary properties,
            string channelName,
            string domain,
            NodeIdentity identity)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                _cluster = new ClusterService(_context, this, this); 
                _cluster.ClusterEventsListener = _clusterListener;
                _cluster.Initialize(properties, channelName, domain, identity, _context.CacheRoot.IsInProc);
            }
            catch (Exception e)
            {
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }

        protected virtual void InitializeCluster(IDictionary properties,
            string channelName,
            string domain,
            NodeIdentity identity,
            bool twoPhaseInitialization, bool isPor)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                _cluster = new ClusterService(_context, this, this);
                _cluster.ClusterEventsListener = _clusterListener;
                _cluster.Initialize(properties, channelName, domain, identity, twoPhaseInitialization,
                    isPor, _context.CacheRoot.IsInProc);
            }
            catch (Exception e)
            {
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }


        protected void ConfirmClusterStartUP(bool isPOR, int retryNumber)
        {
            _cluster.ConfirmClusterStartUP(isPOR, retryNumber);
        }

        protected void HasStarted()
        {
            _cluster.HasStarted();
        }

        #endregion

        public void InitializeClusterPerformanceCounter(string instanceName)
        {
            _cluster.InitializeClusterPerformanceCounters(instanceName);
        }


        #region	/                 --- Cluster Membership ---           /

        /// <summary>
        /// Authenticate the client and see if it is allowed to join the list of valid members.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="identity"></param>
        /// <returns>true if the node is valid and belongs to the scheme's cluster</returns>
        public virtual bool AuthenticateNode(Address address, NodeIdentity identity)
        {
            return true;
        }

        /// <summary>
        /// Called when a new member joins the group.
        /// </summary>
        /// <param name="address">address of the joining member</param>
        /// <param name="identity">additional identity information</param>
        /// <returns>true if the node joined successfuly</returns>
        public virtual bool OnMemberJoined(Address address, NodeIdentity identity)
        {
            //The first node that joins the cluster is going to be the coordinater,
            //and should execute the cache loader only once.
#if SERVER
           
#endif
            return true;
        }

        /// <summary>
        /// Called when an existing member leaves the group.
        /// </summary>
        /// <param name="address">address of the joining member</param>
        /// <returns>true if the node left successfuly</returns>
        public virtual bool OnMemberLeft(Address address, NodeIdentity identity)
        {
            if (Context.NCacheLog.IsInfoEnabled)
                Context.NCacheLog.Info("ClusterCacheBase.OnMemberLeft()", "Member left: " + address);

            if (_context.ExpiryMgr != null)
            {
                if (_cluster.IsCoordinator)
                {
                    _context.ExpiryMgr.IsCoordinatorNode = true;
                }
               

            }
            
            

            lock (_wbQueueTransferCorresponders.SyncRoot)
            {
                if (_wbQueueTransferCorresponders.Contains(address))
                {
                    _wbQueueTransferCorresponders.Remove(address);
                }
            }

            return true;
        }

        /// <summary>
        /// Called after the membership has been changed. Lets the members do some
        /// member oriented tasks.
        /// </summary>

        public virtual void OnAfterMembershipChange()
        {

            if (_cluster.IsCoordinator && !_context.IsStartedAsMirror)
            {
                _statusLatch.SetStatusBit(NodeStatus.Coordinator, 0);

              
            }
           

            if (_context.ExpiryMgr != null)
            {
                _context.ExpiryMgr.IsCoordinatorNode = _cluster.IsCoordinator;
            }

            if (Context.NCacheLog.IsInfoEnabled)
                Context.NCacheLog.Info("ClusterCacheBase.OnAfterMembershipChange()",
                    "New Coordinator is: " + _cluster.Coordinator);

            if (_shutdownServers.Count > 0)
            {
                ArrayList removedServers = new ArrayList();
                foreach (Address addrs in _shutdownServers.Keys)
                {
                    if (!_cluster.Servers.Contains(addrs))
                    {
                        ShutDownServerInfo info = (ShutDownServerInfo)_shutdownServers[addrs];
                        _context.CacheRoot.NotifyUnBlockActivityToClients(info.UniqueBlockingId,
                            info.RenderedAddress.IpAddress.ToString(), info.RenderedAddress.Port);
                        removedServers.Add(addrs);
                    }
                }

                for (int i = 0; i < removedServers.Count; i++)
                {
                    _shutdownServers.Remove(removedServers[i]);
                }

            }

           
        }

        public virtual void OnExitMaintenanceMode(Address address)
        {

        }

        public virtual void RealeaseMaitnainceResourcesOnKill()
        {

        }

        public virtual object HandleClusterMessage(Address src, Function func, out Address destination,
            out NGroups.Message replicationMsg)
        {
            destination = null;
            replicationMsg = null;

            return null;
        }

        /// <summary>
        /// Handles the function requests.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public virtual object HandleClusterMessage(Address src, Function func)
        {
            switch (func.Opcode)
            {
                case (int)OpCodes.NotifyCustomNotif:
                    return handleCustomNotification(func.Operand);
                    break;

                case (int)OpCodes.NotifyCustomRemoveCallback:
                    return handleNotifyRemoveCallback(func.Operand);
                    break;
                case (int)OpCodes.NotifyOldCustomRemoveCallback:
                    return handleOldNotifyRemoveCallback(func.Operand);
                    break;
                case (int)OpCodes.NotifyCustomUpdateCallback:
                    return handleNotifyUpdateCallback(func.Operand);
                    break;
                case (int)OpCodes.NotifyOldCustomUpdateCallback:
                    return handleOldNotifyUpdateCallback(func.Operand);
                    break;
                case (int)OpCodes.RegisterKeyNotification:
                    return handleRegisterKeyNotification(func.Operand);
                    break;

                case (int)OpCodes.UnregisterKeyNotification:
                    return handleUnregisterKeyNotification(func.Operand);
                    break;

                case (int)OpCodes.ReplicatedConnectionString:
                    return handleReplicateConnectionString(func.Operand);
                    break;

                case (int)OpCodes.BalanceNode:
                    handleBalanceDataLoad(func.Operand);
                    break;

                case (int)OpCodes.PublishMap:
                    handlePublishMap(func.Operand);
                    break;

                case (int)OpCodes.OpenStream:
                    return handleOpenStreamOperation(src, (OpenStreamOperation)func.Operand);

                case (int)OpCodes.ReadFromStream:
                    return handleReadFromStreamOperation(src, (ReadFromStreamOperation)func.Operand);

                case (int)OpCodes.WriteToStream:
                    return handleWriteToStreamOperation(src, (WriteToStreamOperation)func.Operand);
                    
                case (int)OpCodes.TransferQueue:
                    return handleTransferQueue(func.Operand, src);

               
                case (int)OpCodes.GetFilteredPersistentEvents:
                    return handleGetFileteredEvents(func.Operand);

                case (int)OpCodes.BlockActivity:
                    return handleBlockActivity(func.Operand);

                    
                case (int)OpCodes.DeadClients:
                    HandleDeadClients(func.Operand);
                    break;

                case (int)OpCodes.Poll:
                    return handlePoll(func.Operand);

                case (int)OpCodes.DryPoll:
                    HandleDryPoll(func.Operand);
                    break;

                case (int)OpCodes.RegisterPollingNotification:
                    return handleRegisterPollingNotification(func.Operand);

                case (int)OpCodes.GetClientRequestStatus:
                    return handleRequestStatusInquiry(func.Operand);
               
                case (int)OpCodes.UpdateClientStatus:
                    handleUpdateClientStatus(src, func.Operand);
                    break;

                case (int)OpCodes.RegisterClientActivityListener:
                    handleClientActivityListenerRegistered(new[] { src, func.Operand });
                    break;
                case (int)OpCodes.UnregisterClientActivityListener:
                    handleClientActivityListenerUnregistered(new[] { src, func.Operand });
                    break;

                case (int)OpCodes.Touch:
                    return HandleTouch(func.Operand, src);

                case (int)OpCodes.TopicOperation:
                    return HandleTopicOperation((ClusterTopicOperation)func.Operand);

                case (int)OpCodes.Message_Acknowldegment:
                    HandleAcknowledgeMessageReceipt(func.Operand as AcknowledgeMessageOperation);
                    break;

                case (int)OpCodes.GetTopicsState:
                    return HandleGetTopicsState();

                case (int)OpCodes.GetTransferrableMessage:
                    return HandleGetTransferrableMessage(func.Operand as GetTransferrableMessageOperation);

                case (int)OpCodes.GetMessageList:
                    return HandleGetMessageList((int)func.Operand);

                case (int)OpCodes.AssignmentOperation:
                    return HandleAssignSubscription((AssignmentOperation)func.Operand);

                case (int)OpCodes.RemoveMessages:
                    HandleRemoveMessages((RemoveMessagesOperation)func.Operand);
                    break;

                case (int)OpCodes.StoreMessage:
                    HandleStoreMessage((StoreMessageOperation)func.Operand);
                    break;

                case (int)OpCodes.GetAssignedMessages:
                    return HandleGetAssignedMessages((GetAssignedMessagesOperation)func.Operand);
                case (int)OpCodes.NotifyPollRequest:
                    return HandlePollRequestCallback(func.Operand);



            }
            return null;
        }

      
        
        private object HandleGetMessageList(int bucketId)
        {
            if (InternalCache != null)
            {
                return InternalCache.GetMessageList(bucketId,true);
            }
            return null;
        }

        private object HandleGetTransferrableMessage(GetTransferrableMessageOperation operation)
        {
            if (InternalCache != null && operation != null)
            {
                return InternalCache.GetTransferrableMessage(operation.Topic, operation.Message);
            }
            return null;
        }
		
   

        private RequestStatus handleRequestStatusInquiry(object arguments)
        {
            if (_context.Render == null)
                return null;

            object[] data = (object[])arguments;
            return _context.Render.GetRequestStatus((string)data[0], (long)data[1], (long)data[2]);
        }

        #endregion

        protected static HashVector GetAllPayLoads(IList userPayLoad, IList compilationInfo)
        {
            HashVector result = new HashVector();
            VirtualArray payLoadArray = new VirtualArray(userPayLoad);
            Alachisoft.NCache.Common.DataStructures.VirtualIndex virtualIndex =
                new Alachisoft.NCache.Common.DataStructures.VirtualIndex();
            for (int i = 0; i < compilationInfo.Count; i++)
            {
                if ((long)compilationInfo[i] == 0)
                {
                    result[i] = null;
                }
                else
                {
                    VirtualArray atomicPayLoadArray = new VirtualArray((long)compilationInfo[i]);
                    Alachisoft.NCache.Common.DataStructures.VirtualIndex atomicVirtualIndex =
                        new Alachisoft.NCache.Common.DataStructures.VirtualIndex();

                    VirtualArray.CopyData(payLoadArray, virtualIndex, atomicPayLoadArray, atomicVirtualIndex,
                        (int)atomicPayLoadArray.Size);
                    virtualIndex.IncrementBy((int)atomicPayLoadArray.Size);
                    result[i] = atomicPayLoadArray.BaseArray;
                }
            }
            return result;
        }

        
        


        /// <summary>
        /// Returns the count of local cache items only.
        /// </summary>
        /// <returns>count of items.</returns>
        internal virtual long Local_Count()
        {
            if (_internalCache != null)
                return _internalCache.Count;

            return 0;
        }

        /// <summary>
        /// Periodic update (PUSH model), i.e., Publish cache statisitcs so that every node in
        /// the cluster gets an idea of the state of every other node.
        /// </summary>

        #region IPresenceAnnouncement Members

        public bool AnnouncePresence(bool urgent)
        {
            try
            {
                UpdateCacheStatistics();
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("ClusteredCacheBase.AnnouncePresence()",
                        " announcing presence ;urget " + urgent);
                if (this.ValidMembers.Count > 1)
                {
                    NodeInfo localStats = _stats.LocalNode;
                    localStats.StatsReplicationCounter++;
                    Function func = new Function((int)OpCodes.PeriodicUpdate, handleReqStatus());
                    if (!urgent)
                        Cluster.SendNoReplyMessage(func);
                    else
                        Cluster.Broadcast(func, GroupRequest.GET_NONE, false, Priority.Normal);
                }
                return true;
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.AnnouncePresence()", e.ToString());
            }
            return false;
        }

        #endregion

        #region /                   --- Clustered_Get ---                               /

        protected CacheEntry Clustered_Get(Address address, object key, OperationContext operationContext)
        {
            return Clustered_Get(address, key, operationContext, true);
        }

        /// <summary>
        /// Retrieve the object from the cluster.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>cache entry.</returns>
        protected CacheEntry Clustered_Get(Address address, object key, OperationContext operationContext,
            bool isUserOperaton)
        {
            CacheEntry retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.Get, new object[] { key, operationContext, isUserOperaton });
                object result = Cluster.SendMessage(address, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return retVal;
                }
                retVal = (CacheEntry)((OperationResponse)result).SerializablePayload;
                if (retVal != null && ((OperationResponse)result).UserPayload != null)
                    retVal.Value = ((OperationResponse)result).UserPayload;
            }

            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }

            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return retVal;
        }

        /// <summary>
        /// Retrieve the object from the cluster.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>cache entry.</returns>
        protected CacheEntry Clustered_Get(Address address, object key, ref ulong version, ref object lockId,
            ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType access,
            OperationContext operationContext)
        {

            CacheEntry retVal = null;
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.Topology);

                Priority priority = Priority.Normal;
                if (operationContext.Contains(OperationContextFieldName.IsClusteredOperation))
                {
                    priority = Priority.Critical;
                }
                Function func = new Function((int)OpCodes.Get, new object[] { key, lockId, lockDate, access, version, lockExpiration, operationContext });
                object result = Cluster.SendMessage(address, func, GetFirstResponse, priority);
                if (result == null)
                {
                    return retVal;
                }

                object[] objArr = (object[])((OperationResponse)result).SerializablePayload;
                retVal = objArr[0] as CacheEntry;

                if (retVal != null && ((OperationResponse)result).UserPayload != null)
                    retVal.Value = ((OperationResponse)result).UserPayload;

                lockId = objArr[1];

                lockDate = (DateTime)objArr[2];
                version = (ulong)objArr[3];
            }
            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }

            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.Topology);
            }
            if (retVal != null)
                retVal.MarkInUse(NCModulesConstants.Global);
            return retVal;
        }

        protected LockOptions Clustered_Lock(Address address, object key, LockExpiration lockExpiration,
            ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.Lock", "");
            LockOptions retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.LockKey,
                    new object[] { key, lockId, lockDate, lockExpiration, operationContext });
                object result = Cluster.SendMessage(address, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return retVal;
                }

                retVal = result as LockOptions;
                if (retVal != null)
                {
                    lockId = retVal.LockId;
                    lockDate = retVal.LockDate;
                }
            }

            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }

            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }

            return retVal;
        }

        protected LockOptions Clustered_IsLocked(Address address, object key, ref object lockId, ref DateTime lockDate,
            OperationContext operationContext)
        {
            LockOptions retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.IsLocked,
                    new object[] { key, lockId, lockDate, operationContext });
                object result = Cluster.SendMessage(address, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return retVal;
                }

                retVal = result as LockOptions;
                if (retVal != null)
                {
                    lockId = retVal.LockId;
                    lockDate = retVal.LockDate;
                }
            }

            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }

            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return retVal;
        }

        protected void Clustered_UnLock(Address address, object key, object lockId, bool isPreemptive,
            OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.Unlock", "");
            LockOptions retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.UnLockKey,
                    new object[] { key, lockId, isPreemptive, operationContext });
                Cluster.SendMessage(address, func, GroupRequest.GET_NONE);
            }

            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }

            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }


        protected IDictionary Clustered_GetEntryAttributes(Address dest, object key, IList<string> columns, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.GetAttribs", "");
            try
            {
                Function func = new Function((int)OpCodes.GetAttribs, new object[] { key, columns, operationContext });
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);
                if (result == null)
                    return null;
                else
                    return result as Hashtable;
            }
            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }

            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        /// <summary>
        /// Retrieve the objects from the cluster.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>cache entry.</returns>
        protected HashVector Clustered_Get(Address dest, object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.GetBlk", "");

            try
            {
                Function func = new Function((int)OpCodes.Get, new object[] { keys, operationContext });
                func.Cancellable = true;
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);
                if (result == null)
                    return null;
                return result as HashVector;
            }

            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }

            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }


        /// <summary>
        /// Retrieve the object from the cluster.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>cache entry.</returns>
        protected CacheEntry Clustered_GetGroup(Address dest, object key, string group, string subGroup,
            ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration,
            LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = null;

            try
            {
                operationContext?.MarkInUse(NCModulesConstants.Topology);

                Function func = new Function((int)OpCodes.GetGroup,
                    new object[]
                    {key, group, subGroup, lockId, lockDate, accessType, version, lockExpiration, operationContext});

                object result = Cluster.SendMessage(dest, func, GetFirstResponse);

                if (result == null)
                {
                    return retVal;
                }

                object[] objArr = (object[])((OperationResponse)result).SerializablePayload;
                retVal = objArr[0] as CacheEntry;
                if (retVal != null)
                {
                    retVal.Value = ((OperationResponse)result).UserPayload;
                }
                lockId = objArr[1];
                lockDate = (DateTime)objArr[2];
                version = (ulong)objArr[3];
                if (retVal != null)
                {
                  
                    retVal.MarkInUse(NCModulesConstants.Global);
                }
            }

            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }

            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.Topology);
            }
            return retVal;
        }

        /// <summary>
        /// Retrieve the object from the cluster.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>cache entry.</returns>
        protected Hashtable Clustered_GetGroup(Address dest, object[] keys, string group, string subGroup,
            OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.GetGroup,
                    new object[] { keys, group, subGroup, operationContext });
                func.Cancellable = true;

                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);

                if (result == null)
                    return null;
                return result as Hashtable;
            }
            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }
            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        public override PollingResult Poll(OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            PollingResult result = null;
            ArrayList dests = new ArrayList();


            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();
                result = Clustered_Poll(servers, operationContext);
            }
            else if (!VerifyClientViewId(clientLastViewId))
            {
                throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
            }
            else
            {
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }

                if (IsClusterUnderMaintenance() && HasUndermaintenanceReplica())
                {
                    ArrayList servers = GetNodeAndMirrorAddress();
                    result = Clustered_Poll(servers, operationContext);
                }
                else
                {
                    result = Local_Poll(operationContext);
                }

                if(ShouldDryPoll() && StartDryPoll(result))
                {
                    DryPoll(operationContext);
                    
                }
                
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }

            return result;
        }

        private ArrayList GetServerParticipatingInStateTransfer()
        {
            ArrayList servers = null;
            if (IsInStateTransfer()) //I have the updated map I can locate the replica
            {
                servers = GetDestInStateTransfer();
            }
            else
            {
                servers = this.ActiveServers.Clone() as ArrayList;
                return GetparticipatingServers(servers);
            }
            return servers;
        }

        public virtual ArrayList GetparticipatingServers(ArrayList servers)
        {
            return this.ActiveServers.Clone() as ArrayList;
        }

        internal virtual bool HasUndermaintenanceReplica()
        {
            return false;
        }

        internal virtual ArrayList GetNodeAndMirrorAddress()
        {
            return null;
        }

        /// <summary>
        /// Retrieve the list of keys from the cache for the given group or sub group.
        /// </summary>
        public override ArrayList GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            ArrayList list = new ArrayList();
            return list;
        }

        /// <summary>
        /// Retrieve the list of key and value pairs from the cache for the given group or sub group.
        /// </summary>
        public override HashVector GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            HashVector list = new HashVector();
            return list;
        }

        /// <summary>
        /// Retrieve the list of keys from the cache for the given tags.
        /// </summary>
        internal override ICollection GetTagKeys(string[] tags, TagComparisonType comparisonType,
            OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            ICollection list = null;
           
            return list;
        }

        /// <summary>
        /// Retrieve the list of key and value pairs from the cache for the given tags.
        /// </summary>
        public override HashVector GetTagData(string[] tags, TagComparisonType comparisonType,
            OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            HashVector list = null;
            

            return list;
        }

        /// <summary>
        /// Remove the list of key from the cache for the given tags.
        /// </summary>
        public override Hashtable Remove(string[] tags, TagComparisonType comparisonType, bool notify,
            OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            Hashtable result = null;
            return result;
        }

        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        public override Hashtable Remove(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            Hashtable result = null;
            return result;
        }
        
        protected virtual ICollection Local_GetTagKeys(string[] tags, TagComparisonType comparisonType,
            OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        protected virtual ArrayList Local_GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        protected virtual PollingResult Local_Poll(OperationContext context)
        {
            if (_internalCache != null)
                return _internalCache.Poll(context);
            return null;
        }

        protected virtual void Local_RegisterPollingNotification(short callbackId, OperationContext context)
        { }

        protected virtual HashVector Local_GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        protected virtual HashVector Local_GetTagData(string[] tags, TagComparisonType comparisonType,
            OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        protected virtual Hashtable Local_RemoveTag(string[] tags, TagComparisonType tagComparisonType, bool notify,
            OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        protected virtual Hashtable Local_RemoveGroup(string group, string subGroup, bool notify,
            OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        protected virtual bool VerifyClientViewId(long clientLastViewId)
        {
            return true;
        }

        protected virtual ArrayList GetDestInStateTransfer()
        {
            throw new NotImplementedException();
        }

        public virtual bool IsInStateTransfer()
        {
            return false;
        }

      

        /// <summary>
        /// Retrieve the list of keys fron the cache for the given group or sub group.
        /// </summary>
        protected ArrayList Clustered_GetGroupKeys(ArrayList dests, string group, string subGroup,
            OperationContext operationContext, Boolean throwSuspected = false)
        {
            ArrayList list = null;
            
            return list;
        }

        /// <summary>
        /// Retrieve the list of keys from the cache for the given group or sub group.
        /// </summary>
        protected HashVector Clustered_GetGroupData(ArrayList dests, string group, string subGroup,
            OperationContext operationContext, Boolean throwSuspected = false)
        {
            HashVector table = new HashVector();
            return table;
        }

        /// <summary>
        /// Retrieve the list of keys fron the cache for the given tags.
        /// </summary>
        protected ClusteredArrayList Clustered_GetTagKeys(ArrayList dests, string[] tags,
            TagComparisonType comparisonType, OperationContext operationContext, Boolean throwSuspected = false)
        {
            ClusteredArrayList keys = null;
            return keys;
        }

        /// <summary>
        /// Retrieve the list of keys from the cache for the given tags.
        /// </summary>
        protected HashVector Clustered_GetTagData(ArrayList dests, string[] tags, TagComparisonType comparisonType,
            OperationContext operationContext, Boolean throwSuspected = false)
        {
            HashVector table = new HashVector();
            return table;
        }


       
        /// <summary>
        /// Retrieve the list of keys and values from the cache based on the specified tag and remove from cache.
        /// </summary>
        protected Hashtable Clustered_RemoveByTag(ArrayList dests, string[] tags, TagComparisonType comparisonType,
            bool notify, OperationContext operationContext)
        {
            ClusteredArrayList list = Clustered_GetTagKeys(dests, tags, comparisonType, operationContext);
            return Remove(list.ToArray(), ItemRemoveReason.Removed, notify, operationContext);
        }

        /// <summary>
        /// Retrieve the list of keys and values from the cache based on the specified groups and remove from cache.
        /// </summary>
        protected Hashtable Clustered_RemoveGroup(ArrayList dests, string group, string subGroup, bool notify,
            OperationContext operationContext)
        {
            ArrayList list = Clustered_GetGroupKeys(dests, group, subGroup, operationContext);
            return Remove(list.ToArray(), ItemRemoveReason.Removed, notify, operationContext);
        }

        /// <summary>
        /// Returns the count of clustered cache items.
        /// </summary>
        /// <returns>Count of nodes in cluster.</returns>
        protected long Clustered_Count(ArrayList dests)
        {
            long retVal = 0;
            try
            {
                Function func = new Function((int)OpCodes.GetCount, null, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false);

                ClusterHelper.ValidateResponses(results, typeof(long), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(long));

                IEnumerator ia = rspList.GetEnumerator();
                while (ia.MoveNext())
                {
                    Rsp rsp = (Rsp)ia.Current;
                    if (rsp.Value != null)
                        retVal += Convert.ToInt64(rsp.Value);
                }
            }
            catch (CacheException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return retVal;
        }

        #endregion

        #region /                       ---PartitionedCacheBase.Clustered_GetGroupInfo ---          /

        /// <summary>
        /// Gets the data group info of the item. Node containing the item will return the
        /// data group information.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Result of the operation</returns>
        /// <remarks>On the other ndoe handleGetGroupInfo is called</remarks>
        public ClusteredOperationResult Clustered_GetGroupInfo(ArrayList dest, object key, bool excludeSelf,
            OperationContext operationContext)
        {
            ClusteredOperationResult retVal = null;
            return retVal;
        }

        /// <summary>
        /// Gets the data group info the items. Node containing items will return a table
        /// of Data grop information.
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        /// /// <remarks>On the other ndoe handleGetGroupInfo is called</remarks>
        public ICollection Clustered_GetGroupInfoBulk(ArrayList dest, object[] keys, bool excludeSelf,
            OperationContext operationContext)
        {
            ArrayList resultList = null;
            try
            {
                Function func = new Function((int)OpCodes.GetDataGroupInfo, new object[] { keys, operationContext },
                    excludeSelf);
                //RspList results = Cluster.BroadcastToMultiple(dest, func, GroupRequest.GET_ALL);
                func.Cancellable = true;
                RspList results = Cluster.Multicast(dest, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return resultList;
                }

                ClusterHelper.ValidateResponses(results, typeof(Hashtable), Name);

                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(Hashtable));

                if (rspList.Count <= 0)
                {
                    return null;
                }
                else
                {
                    resultList = new ArrayList();
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp)im.Current;
                        resultList.Add(new ClusteredOperationResult((Address)rsp.Sender, rsp.Value));
                    }
                }

            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return resultList;
        }

        #endregion

        /// <summary>
        /// Determines whether the cluster contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>address of the node that contains the specified key; otherwise, null.</returns>
        /// <summary>
        /// Determines whether the cluster contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>address of the node that contains the specified key; otherwise, null.</returns>
        protected Address Clustered_Contains(Address dest, object key, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.Cont", "");

            try
            {
                Function func = new Function((int)OpCodes.Contains, new object[] { key, operationContext });
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);

                if (result != null && (bool)result)
                    return dest;
                return null;
            }
            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }
            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        /// <summary>
        /// Determines whether the cluster contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>address of the node that contains the specified key; otherwise, null.</returns>
        protected Hashtable Clustered_Contains(Address dest, object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.ContBlk", "");

            try
            {
                Function func = new Function((int)OpCodes.Contains, new object[] { keys, operationContext });
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);

                if (result != null)
                    return result as Hashtable;
                return null;
            }
            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }
            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        /// <summary>
        /// Add the object to the cluster.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cacheEntry"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on every server-node in the cluster. If
        /// the operation fails on any one node the whole operation is considered to have failed
        /// and is rolled-back.
        /// </remarks>
        protected virtual CacheInsResultWithEntry Clustered_Insert(object key, CacheEntry cacheEntry)
        {
            return CacheInsResultWithEntry.CreateCacheInsResultWithEntry(_context.FakeObjectPool);
        }

        internal void UpdateStatistics()
        {
            UpdateCacheStatistics();
        }

        /// <summary>
        /// Updates the statistics for the cache scheme.
        /// </summary>
        protected virtual void UpdateCacheStatistics()
        {
            try
            {
                _stats.LocalNode.Statistics = _internalCache.Statistics;
                _stats.LocalNode.Status.Data = _statusLatch.Status.Data;

                _stats.SetServerCounts(Convert.ToInt32(Servers.Count),
                    Convert.ToInt32(ValidMembers.Count),
                    Convert.ToInt32(Members.Count - ValidMembers.Count));
                CacheStatistics c = CombineClusterStatistics(_stats);
                _stats.UpdateCount(c.Count);
                _stats.HitCount = c.HitCount;
                _stats.MissCount = c.MissCount;
                _stats.MaxCount = c.MaxCount;
                _stats.MaxSize = c.MaxSize;
            }
            catch (Exception)
            {
            }
        }

        public virtual CacheStatistics CombineClusterStatistics(ClusterCacheStatistics s)
        {
            return null;
        }

        /// <summary>
        /// Updates or Adds the objects to the cluster.
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="cacheEntries"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method either invokes <see cref="handleInsert"/> on any cluster node or invokes
        /// <see cref="Local_Insert"/> locally. The choice of the server node is determined by the
        /// <see cref="LoadBalancer"/>.
        /// </remarks>
        protected virtual Hashtable Clustered_Insert(object[] keys, CacheEntry[] cacheEntries,
            OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Broadcasts a user-defined event across the cluster.
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="data"></param>
        /// <param name="async"></param>
        public sealed override void SendNotification(object notifId, object data, OperationContext operationContext)
        {
            if (ActiveServers.Count > 1)
            {
                object info = new object[] { notifId, data };
                Function func = new Function((int)OpCodes.NotifyCustomNotif, info, false);
                Cluster.SendNoReplyMessageAsync(func);

            }
            else
            {
                handleCustomNotification(new object[] { notifId, data });
            }
        }

        #region /               --- replicate/handle Connection Srting ---                /

        public IList Clustered_ReplicateConnectionString(ArrayList dest, string connString, bool isSql, bool excludeSelf)
        {
            try
            {
                Function func = new Function((int)OpCodes.ReplicatedConnectionString, new object[] { connString, isSql },
                    excludeSelf);
                RspList results = Cluster.Multicast(dest, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return null;
                }

                ClusterHelper.ValidateResponses(results, typeof(bool), Name);

                return ClusterHelper.GetAllNonNullRsp(results, typeof(bool));
            }
            catch (CacheException e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.Clustered_ReplicateConnectionString()", e.ToString());
                throw;
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.Clustered_ReplicateConnectionString()", e.ToString());
                throw new GeneralFailureException(e.Message, e);
            }
        }


        protected virtual object handleReplicateConnectionString(object info)
        {
            object[] objs = (object[])info;
            string connString = (string)objs[0];
            bool isSql = (bool)objs[1];
            return true;
        }

       

        #endregion

        /// <summary>
        /// Hanlder for clustered user-defined notification.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private object handleCustomNotification(object info)
        {
            object[] objs = (object[])info;
            base.NotifyCustomEvent(objs[0], objs[1], false, null, null);
            return null;
        }

        /// <summary>
        /// Hanlder for clustered item update callback notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// /// <param name="entry">callback entry</param>
        /// <returns>null</returns>
        private object handleNotifyUpdateCallback(object info)
        {
            EventContext eventContext = null;

            object[] objs = (object[])info;
            ArrayList callbackListeners = objs[1] as ArrayList;
            Hashtable intendedNotifiers = objs[2] as Hashtable;
            if (objs.Length > 3)
                eventContext = objs[3] as EventContext;

            IDictionaryEnumerator ide = intendedNotifiers.GetEnumerator();
            while (ide.MoveNext())
            {
                CallbackInfo cbinfo = ide.Key as CallbackInfo;
                Address node = ide.Value as Address;

                if (node != null && !node.Equals(Cluster.LocalAddress))
                {
                    callbackListeners.Remove(cbinfo);
                }
            }

            NotifyCustomUpdateCallback(objs[0], objs[1], true, null, eventContext);
            return null;
        }
        private object handleOldNotifyUpdateCallback(object info)
        {
            EventContext eventContext = null;

            object[] objs = (object[])info;
            ArrayList callbackListeners = objs[1] as ArrayList;
            Hashtable intendedNotifiers = objs[2] as Hashtable;
            if (objs.Length > 3)
                eventContext = objs[3] as EventContext;

            IDictionaryEnumerator ide = intendedNotifiers.GetEnumerator();
            while (ide.MoveNext())
            {
                CallbackInfo cbinfo = ide.Key as CallbackInfo;
                Address node = ide.Value as Address;

                if (node != null && !node.Equals(Cluster.LocalAddress))
                {
                    callbackListeners.Remove(cbinfo);
                }
            }

            NotifyOldCustomUpdateCallback(objs[0], objs[1], true, null, eventContext);
            return null;
        }

        /// <summary>
        /// Hanlder for active query update callback notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// /// <param name="entry">callback entry</param>
        /// <returns>null</returns>

        /// <summary>
        /// Handler for item add event.
        /// </summary>
        /// <param name="info"></param>
        private void handleNotifyAdd(object[] info)
        {
            object[] args = info as object[];
            NotifyItemAdded(args[0], true, (OperationContext)args[1], (EventContext)args[2]);
        }
        private void handleOldNotifyAdd(object[] info)
        {
            object[] args = info as object[];
            NotifyOldItemAdded(args[0], true, (OperationContext)args[1], (EventContext)args[2]);
        }
        /// <summary>
        /// Handler for item add event.
        /// </summary>
        /// <param name="info"></param>
        private void handleNotifyUpdate(object[] info)
        {
            object[] args = info as object[];
            NotifyItemUpdated(args[0], true, (OperationContext)args[1], (EventContext)args[2]);
        }
        private void handleOldNotifyUpdate(object[] info)
        {
            object[] args = info as object[];
            NotifyOldItemUpdated(args[0], true, (OperationContext)args[1], (EventContext)args[2]);
        }

        private void handleNotifyRemove(object info)
        {
            object[] objs = (object[])info;

            NotifyItemRemoved(objs[0], objs[1], (ItemRemoveReason)objs[2], true, (OperationContext)objs[3], (EventContext)objs[4]);
        }

        private void handleOldNotifyRemove(object info)
        {
            object[] objs = (object[])info;

            NotifyOldItemRemoved(objs[0], null, ItemRemoveReason.Removed, true, (OperationContext)objs[1], (EventContext)objs[2]);
        }

       

        /// <summary>
        /// Hanlder for clustered item remove callback notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// /// <param name="entry">callback entry</param>
        /// <returns>null</returns>
        protected object handleNotifyRemoveCallback(object info)
        {
            object[] objs = (object[])info;
            Hashtable intendedNotifiers = objs[2] as Hashtable;
            OperationContext operationContext = objs[3] as OperationContext;
            EventContext eventContext = objs[4] as EventContext;
            // a deep clone is required here as callback list is going to be modified while async cluster
            //notification is being sent to the other nodes.
            eventContext = eventContext.Clone() as EventContext;

            ArrayList callbackList =
                eventContext.GetValueByField(EventContextFieldName.ItemRemoveCallbackList) as ArrayList;

            IDictionaryEnumerator ide = intendedNotifiers.GetEnumerator();
            while (ide.MoveNext())
            {
                CallbackInfo cbinfo = ide.Key as CallbackInfo;
                Address node = ide.Value as Address;

                if (node != null && !node.Equals(Cluster.LocalAddress))
                {
                    callbackList.Remove(cbinfo);
                }
            }
            NotifyCustomRemoveCallback(objs[0], callbackList, (ItemRemoveReason) objs[1], true, operationContext, eventContext);
            return null;

        }

        protected object handleOldNotifyRemoveCallback(object info)
        {
            object[] objs = (object[])info;
            Hashtable intendedNotifiers = objs[2] as Hashtable;
            OperationContext operationContext = objs[3] as OperationContext;
            EventContext eventContext = objs[4] as EventContext;
            // a deep clone is required here as callback list is going to be modified while async cluster
            //notification is being sent to the other nodes.
            eventContext = eventContext.Clone() as EventContext;

            ArrayList callbackList =
                eventContext.GetValueByField(EventContextFieldName.ItemRemoveCallbackList) as ArrayList;

            IDictionaryEnumerator ide = intendedNotifiers.GetEnumerator();
            while (ide.MoveNext())
            {
                CallbackInfo cbinfo = ide.Key as CallbackInfo;
                Address node = ide.Value as Address;

                if (node != null && !node.Equals(Cluster.LocalAddress))
                {
                    callbackList.Remove(cbinfo);
                }
            }
            NotifyOldCustomRemoveCallback(objs[0], callbackList, (ItemRemoveReason)objs[1], true, operationContext, eventContext);
            return null;

        }



        /// <summary>
        /// Initializing the cluster_stats object for WMI
        /// </summary>
        /// <param name="stats"></param>
        /// <param name="Name"></param>
        internal void postInstrumentatedData(ClusterCacheStatistics stats, string Name)
        {
            if (Name.IndexOf("_BK_NODE") != -1)
            {
                Name = Name.Remove(Name.IndexOf("_BK_"), Name.Length - Name.IndexOf("_BK_"));
            }
        }

        protected virtual Address GetDestinationForFilteredEvents()
        {
            return Cluster.Coordinator;
        }

        public override List<NCache.Persistence.Event> GetFilteredEvents(string clientID, Hashtable events,
            EventStatus registeredEventStatus)
        {
            try
            {
                object[] arguments = new object[] { clientID, events, registeredEventStatus };
                Address destination = GetDestinationForFilteredEvents();

                if (destination.Equals(Cluster.LocalAddress))
                {
                    return (List<NCache.Persistence.Event>)handleGetFileteredEvents(arguments);
                }
                else
                {
                    Function func = new Function((int)OpCodes.GetFilteredPersistentEvents, arguments, false);
                    List<NCache.Persistence.Event> filteredEvents =
                        (List<NCache.Persistence.Event>)
                            Cluster.SendMessage(GetDestinationForFilteredEvents(), func, GroupRequest.GET_FIRST);
                    return filteredEvents;
                }
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        protected virtual object handleGetFileteredEvents(object arguments)
        {
            object[] args = (object[])arguments;

            string clientID = (string)args[0];
            Hashtable events = (Hashtable)args[1];
            EventStatus registeredEventStatus = (EventStatus)args[2];
            if (_context.PersistenceMgr != null)
            {
                return _context.PersistenceMgr.GetFilteredEventsList(clientID, events, registeredEventStatus);
            }

            return null;
        }


        protected object handleBlockActivity(object arguments)
        {
            object[] args = (object[])arguments;

            ShutDownServerInfo ssInfo = new ShutDownServerInfo();
            ssInfo.UniqueBlockingId = (string)args[0];
            ssInfo.BlockServerAddress = (Address)args[1];
            ssInfo.BlockInterval = (long)args[2];
            ssInfo.RenderedAddress = (Address)_cluster.Renderers[ssInfo.BlockServerAddress];
            _shutdownServers.Add(ssInfo.BlockServerAddress, ssInfo);
            _context.CacheRoot.NotifyBlockActivityToClients(ssInfo.UniqueBlockingId,
                ssInfo.RenderedAddress.IpAddress.ToString(), ssInfo.BlockInterval, ssInfo.RenderedAddress.Port);
            return true;
        }



        #region	/                 --- Clustered Notifiers ---           /

        /// <summary>
        /// Broadcasts an itemadd notifier across the cluster excluding self
        /// </summary>
        /// <param name="key"></param>
        internal void RaiseGeneric(object data)
        {
            Cluster.SendNoReplyMessageAsync(data);
        }

        /// <summary>
        /// Broadcasts an itemadd notifier across the cluster
        /// </summary>
        /// <param name="key"></param>
        protected void RaiseGeneric(Address dest, object data)
        {
            Cluster.SendNoReplyMessageAsync(dest, data);
        }



        /// <summary>
        /// Broadcasts an itemadd notifier across the cluster
        /// </summary>
        /// <param name="key"></param>
        public override void RaiseItemAddNotifier(object key, CacheEntry entry, OperationContext context,
            EventContext eventContext)
        {
            try
            {
                if (entry != null)
                    entry.MarkInUse(NCModulesConstants.Topology);
                // If everything went ok!, initiate local and cluster-wide notifications.
                if (IsItemAddNotifier) //&& ValidMembers.Count > 1)
                {
                    if (eventContext == null)
                    {
                        eventContext = CreateEventContextForGeneralDataEvent(NCache.Persistence.EventType.ITEM_ADDED_EVENT, null,
                            entry, null);
                    }

                    if (Context.NCacheLog.IsInfoEnabled)
                        Context.NCacheLog.Info("ReplicatedBase.RaiseItemAddNotifier()", "onitemadded " + key);

                    RaiseGeneric(new Function((int)OpCodes.NotifyAdd, new object[] { key, context, eventContext }));
                    handleNotifyAdd(new object[] { key, context, eventContext });
                }
            }
            finally
            {
                if (entry != null)
                    entry.MarkFree(NCModulesConstants.Topology);
            }
        }

   
        protected EventContext CreateEventContextForGeneralDataEvent(NCache.Persistence.EventType eventType,
            OperationContext context, CacheEntry entry, CacheEntry oldEntry)
        {
            try
            {
                if (entry != null)
                    entry.MarkInUse(NCModulesConstants.Topology);

                EventContext eventContext = CreateEventContext(context, eventType);
                EventTypeInternal generalEventType = EventTypeInternal.ItemAdded;

                switch (eventType)
                {
                    case NCache.Persistence.EventType.ITEM_ADDED_EVENT:
                        generalEventType = EventTypeInternal.ItemAdded;
                        break;

                    case NCache.Persistence.EventType.ITEM_UPDATED_EVENT:
                        generalEventType = EventTypeInternal.ItemUpdated;
                        break;

                    case NCache.Persistence.EventType.ITEM_REMOVED_EVENT:
                        generalEventType = EventTypeInternal.ItemRemoved;
                        break;
                }

                eventContext.Item = CacheHelper.CreateCacheEventEntry(GetGeneralDataEventFilter(generalEventType), entry, Context);
                if (oldEntry != null)
                {
                    eventContext.OldItem = CacheHelper.CreateCacheEventEntry(GetGeneralDataEventFilter(generalEventType),
                        oldEntry, Context);
                }

                return eventContext;
            }
            finally
            {
                if (entry != null)
                    entry.MarkFree(NCModulesConstants.Topology);
            }
        }

        protected void FilterEventContextForGeneralDataEvents(Caching.Events.EventTypeInternal eventType, EventContext eventContext)
        {
            if (eventContext != null && eventContext.Item != null)
            {
                EventDataFilter filter = GetGeneralDataEventFilter(eventType);

                switch (filter)
                {

                    case EventDataFilter.None:
                        eventContext.Item = null;
                        eventContext.OldItem = null;
                        break;
                }
            }
        }


        /// <summary>
        /// Broadcasts an itemaupdate notifier across the cluster
        /// </summary>
        /// <param name="key"></param>
        public override void RaiseItemUpdateNotifier(object key, OperationContext operationContext, EventContext eventcontext)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsItemUpdateNotifier )//&& ValidMembers.Count > 1)
            {
                RaiseGeneric(new Function((int) OpCodes.NotifyUpdate, new object[] {key, operationContext, eventcontext}));
                handleNotifyUpdate(new object[] { key, operationContext, eventcontext });
            }
        }
      

        public override void RaiseOldItemRemoveNotifier(object key, OperationContext operationContext, EventContext eventcontext)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsItemRemoveNotifier)//&& ValidMembers.Count > 1)
            {
                RaiseGeneric(new Function((int)OpCodes.NotifyOldRemoval, new object[] { key, operationContext, eventcontext }));
                handleOldNotifyRemove(new object[] { key, operationContext, eventcontext });
            }
        }
        /// <summary>
        /// Broadcasts an itemremove notifier across the cluster
        /// </summary>
        /// <param name="packed">key or a list of keys to notify</param>
        protected void RaiseItemRemoveNotifier(object packed)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsItemRemoveNotifier)// && ValidMembers.Count > 1)
            {
                RaiseGeneric(new Function((int)OpCodes.NotifyRemoval, packed));
                handleNotifyRemove(packed);
            }
        }

        /// <summary>
        /// Broadcasts an itemremove notifier across the cluster
        /// </summary>
        /// <param name="packed">key or a list of keys to notify</param>
        protected void RaiseAsyncItemRemoveNotifier(object[] keys, object[] values, ItemRemoveReason reason,
            OperationContext operationContext, EventContext[] eventContexts)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsItemRemoveNotifier && ValidMembers.Count > 1)
            {
                _context.AsyncProc.Enqueue(new AsyncBroadcastNotifyRemoval(this, keys, values, reason, operationContext,
                    eventContexts));
            }
        }

        /// <summary>
        /// Broadcasts cache clear notifier across the cluster
        /// </summary>
        /// <param name="key"></param>
        protected void RaiseCacheClearNotifier()
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsCacheClearNotifier && Cluster.IsCoordinator &&
                (ValidMembers.Count - Servers.Count) > 1)
            {
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedBase.RaiseCacheClearNotifier()");
                RaiseGeneric(new Function((int)OpCodes.NotifyClear, null));
            }
        }

        /// <summary>
        /// sends a custom item remove callback to the node from which callback was added.
        /// </summary>
        /// <param name="dest">Addess of the callback node</param>
        /// <param name="packed">key,item and actual callback</param>
        private void RaiseCustomRemoveCalbackNotifier(ArrayList dests, object[] packed, bool async)
        {
            bool sendLocal = false;

            if (dests.Contains(Cluster.LocalAddress))
            {
                dests.Remove(_cluster.LocalAddress);
                sendLocal = true;
            }

            if (dests.Count > 0 && ValidMembers.Count > 1)
            {
                if (async)
                {
                    _cluster.SendNoReplyMcastMessageAsync(dests,
                        new Function((int)OpCodes.NotifyCustomRemoveCallback, packed));
                }
                else
                    _cluster.Multicast(dests, new Function((int)OpCodes.NotifyCustomRemoveCallback, packed),
                        GroupRequest.GET_ALL, false);
            }

            if (sendLocal)
            {
                handleNotifyRemoveCallback(packed);
            }
        }
        private void RaiseOldCustomRemoveCalbackNotifier(ArrayList dests, object[] packed, bool async)
        {
            bool sendLocal = false;

            if (dests.Contains(Cluster.LocalAddress))
            {
                dests.Remove(_cluster.LocalAddress);
                sendLocal = true;
            }

            if (dests.Count > 0 && ValidMembers.Count > 1)
            {
                if (async)
                {
                    _cluster.SendNoReplyMcastMessageAsync(dests,
                        new Function((int)OpCodes.NotifyCustomRemoveCallback, packed));
                }
                else
                    _cluster.Multicast(dests, new Function((int)OpCodes.NotifyCustomRemoveCallback, packed),
                        GroupRequest.GET_ALL, false);
            }

            if (sendLocal)
            {
                handleOldNotifyRemoveCallback(packed);
            }
        }
        /// <summary>
        /// sends a pull request to the client.
        /// </summary>
        /// <param name="dest">Addess of the callback node</param>
        /// <param name="packed">key,item and actual callback</param>
        private void RaisePollRequestNotifier(ArrayList dests, object[] packed, bool async)
        {
            bool sendLocal = false;

            if (dests.Contains(Cluster.LocalAddress))
            {
                dests.Remove(_cluster.LocalAddress);
                sendLocal = true;
            }

            if (dests.Count > 0 && ActiveServers.Count > 1)
            {
                if (async)
                {
                    _cluster.SendNoReplyMcastMessageAsync(dests, new Function((int)OpCodes.NotifyPollRequest, packed, false));
                }
                else
                    _cluster.Multicast(dests, new Function((int)OpCodes.NotifyPollRequest, packed, false), GroupRequest.GET_ALL, false);
            }

            if (sendLocal)
            {
                HandlePollRequestCallback(packed);
            }
        }

        /// <summary>
        /// Hanlder for clustered item remove callback notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// /// <param name="entry">callback entry</param>
        /// <returns>null</returns>
        protected object HandlePollRequestCallback(object info)
        {
            object[] objs = (object[])info;
            string clientId = objs[0] as string;
            short callbackId = (short)objs[1];
            EventTypeInternal eventType = (EventTypeInternal)objs[2];
            NotifyPollRequestCallback(clientId, callbackId, true, eventType);
            return null;
        }

        protected void RaiseAsyncCustomRemoveCalbackNotifier(object key, CacheEntry entry, ItemRemoveReason reason,
            OperationContext opContext, EventContext eventContext)
        {
            try
            {
                if (entry != null)
                    entry.MarkInUse(NCModulesConstants.Topology);

                bool notify = true;

                if (reason == ItemRemoveReason.Expired)
                {
                    int notifyOnExpirationCount = 0;
                    if (entry != null)
                    {
                        Caching.Notifications notification = entry.Notifications;

                        if (notification != null && notification.ItemRemoveCallbackListener != null)
                        {
                            for (int i = 0; i < notification.ItemRemoveCallbackListener.Count; i++)
                            {
                                CallbackInfo removeCallbackInfo = (CallbackInfo)notification.ItemRemoveCallbackListener[i];
                                if (removeCallbackInfo != null && removeCallbackInfo.NotifyOnExpiration)
                                    notifyOnExpirationCount++;
                            }
                        }
                    }

                    if (notifyOnExpirationCount <= 0) notify = false;
                }

                if (notify)
                    _context.AsyncProc.Enqueue(new AsyncBroadcastCustomNotifyRemoval(this, key, entry, reason, opContext,
                        eventContext));
            }
            catch (Exception e)
            {

            }
            finally
            {

                if (entry != null)
                    entry.MarkFree(NCModulesConstants.Topology);
            }
        }

        /// <summary>
        /// Reaises the custom item remove call baack.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="notification"></param>
        internal void RaiseCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason,
            bool async, OperationContext operationContext, EventContext eventContext)
        {
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);

                ArrayList destinations = null;
                ArrayList nodes = null;
                Hashtable intendedNotifiers = new Hashtable();
                Caching.Notifications notification = cacheEntry.Notifications;

                if (notification != null && notification.ItemRemoveCallbackListener.Count > 0)
                {
                    if (_stats.Nodes != null)
                    {
                        nodes = _stats.Nodes.Clone() as ArrayList;

                        destinations = new ArrayList();
                        foreach (CallbackInfo cbInfo in notification.ItemRemoveCallbackListener)
                        {
                            if (reason == ItemRemoveReason.Expired && cbInfo != null && !cbInfo.NotifyOnExpiration)
                                continue;

                            int index = nodes.IndexOf(new NodeInfo(Cluster.LocalAddress));
                            if (index != -1 && ((NodeInfo)nodes[index]).ConnectedClients.Contains(cbInfo.Client))
                            {
                                if (!destinations.Contains(Cluster.LocalAddress))
                                {
                                    destinations.Add(Cluster.LocalAddress);
                                }
                                intendedNotifiers[cbInfo] = Cluster.LocalAddress;
                                continue;
                            }
                            else
                            {
                                foreach (NodeInfo nInfo in nodes)
                                {
                                    if (nInfo.ConnectedClients != null && nInfo.ConnectedClients.Contains(cbInfo.Client))
                                    {
                                        if (!destinations.Contains(nInfo.Address))
                                        {
                                            destinations.Add(nInfo.Address);
                                            intendedNotifiers[cbInfo] = nInfo.Address;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (destinations != null && destinations.Count > 0)
                {
                    if (operationContext == null) operationContext = new OperationContext();

                    if (eventContext == null || !eventContext.HasEventID(EventContextOperationType.CacheOperation))
                    {
                        eventContext = CreateEventContext(operationContext, NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK);
                        eventContext.Item = CacheHelper.CreateCacheEventEntry(notification.ItemRemoveCallbackListener, cacheEntry, Context);
                   
                        eventContext.Add(EventContextFieldName.ItemRemoveCallbackList,
                            notification.ItemRemoveCallbackListener.Clone());
                    }
                    eventContext.Add(EventContextFieldName.ItemRemoveCallbackList,
                        notification.ItemRemoveCallbackListener.Clone());

                    object[] packed = new object[] { key, reason, intendedNotifiers, operationContext, eventContext };
                    ///Incase of parition and partition of replica, there can be same clients connected
                    ///to multiple server. therefore the destinations list will contain more then
                    ///one servers. so the callback will be sent to the same client through different server
                    ///to avoid this, we will check the list for local server. if client is connected with
                    ///local node, then there is no need to send callback to all other nodes
                    ///if there is no local node, then we select the first node in the list.
                    //if (destinations.Contains(Cluster.LocalAddress)) selectedServer.Add(Cluster.LocalAddress);
                    //else selectedServer.Add(destinations[0]);
                    RaiseCustomRemoveCalbackNotifier(destinations, packed, async);
                }
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);

                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Asynctask);

            }

        }
        internal void RaiseOldCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason,
    bool async, OperationContext operationContext, EventContext eventContext)
        {
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);

                ArrayList destinations = null;
                ArrayList nodes = null;
                Hashtable intendedNotifiers = new Hashtable();
                Caching.Notifications notification = cacheEntry.Notifications;

                if (notification != null && notification.ItemRemoveCallbackListener.Count > 0)
                {
                    if (_stats.Nodes != null)
                    {
                        nodes = _stats.Nodes.Clone() as ArrayList;

                        destinations = new ArrayList();
                        foreach (CallbackInfo cbInfo in notification.ItemRemoveCallbackListener)
                        {
                            if (reason == ItemRemoveReason.Expired && cbInfo != null && !cbInfo.NotifyOnExpiration)
                                continue;

                            int index = nodes.IndexOf(new NodeInfo(Cluster.LocalAddress));
                            if (index != -1 && ((NodeInfo)nodes[index]).ConnectedClients.Contains(cbInfo.Client))
                            {
                                if (!destinations.Contains(Cluster.LocalAddress))
                                {
                                    destinations.Add(Cluster.LocalAddress);
                                }
                                intendedNotifiers[cbInfo] = Cluster.LocalAddress;
                                continue;
                            }
                            else
                            {
                                foreach (NodeInfo nInfo in nodes)
                                {
                                    if (nInfo.ConnectedClients != null && nInfo.ConnectedClients.Contains(cbInfo.Client))
                                    {
                                        if (!destinations.Contains(nInfo.Address))
                                        {
                                            destinations.Add(nInfo.Address);
                                            intendedNotifiers[cbInfo] = nInfo.Address;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (destinations != null && destinations.Count > 0)
                {
                    if (operationContext == null) operationContext = new OperationContext();

                    if (eventContext == null || !eventContext.HasEventID(EventContextOperationType.CacheOperation))
                    {
                        eventContext = CreateEventContext(operationContext, NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK);
                        eventContext.Item = CacheHelper.CreateCacheEventEntry(notification.ItemRemoveCallbackListener, cacheEntry, Context);

                        eventContext.Add(EventContextFieldName.ItemRemoveCallbackList,
                            notification.ItemRemoveCallbackListener.Clone());
                    }
                    eventContext.Add(EventContextFieldName.ItemRemoveCallbackList,
                        notification.ItemRemoveCallbackListener.Clone());

                    object[] packed = new object[] { key, reason, intendedNotifiers, operationContext, eventContext };
                    ///Incase of parition and partition of replica, there can be same clients connected
                    ///to multiple server. therefore the destinations list will contain more then
                    ///one servers. so the callback will be sent to the same client through different server
                    ///to avoid this, we will check the list for local server. if client is connected with
                    ///local node, then there is no need to send callback to all other nodes
                    ///if there is no local node, then we select the first node in the list.
                    //if (destinations.Contains(Cluster.LocalAddress)) selectedServer.Add(Cluster.LocalAddress);
                    //else selectedServer.Add(destinations[0]);
                    RaiseOldCustomRemoveCalbackNotifier(destinations, packed, async);
                }
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);

                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Asynctask);

            }

        }
        /// <summary>
        /// Reaises the custom item remove call baack.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="notification"></param>
        internal void RaiseCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason)
        {
            RaiseCustomRemoveCalbackNotifier(key, cacheEntry, reason, null, null);
        }

        internal void RaisePollRequestNotifier(string clientId, short callbackId, NCache.Caching.Events.EventTypeInternal eventType)
        {
            ArrayList destinations = null;
            ArrayList nodes = null;

            if (_stats.Nodes != null)
            {
                nodes = _stats.Nodes.Clone() as ArrayList;

                destinations = new ArrayList();

                int index = nodes.IndexOf(new NodeInfo(Cluster.LocalAddress));
                if (index != -1 && ((NodeInfo)nodes[index]).ConnectedClients.Contains(clientId))
                {
                    if (!destinations.Contains(Cluster.LocalAddress))
                    {
                        destinations.Add(Cluster.LocalAddress);
                    }
                }
                else
                {
                    foreach (NodeInfo nInfo in nodes)
                    {
                        if (nInfo.ConnectedClients != null && nInfo.ConnectedClients.Contains(clientId))
                        {
                            if (!destinations.Contains(nInfo.Address))
                            {
                                destinations.Add(nInfo.Address);
                                break;
                            }
                        }
                    }
                }
            }
            if (destinations != null && destinations.Count > 0)
            {
                RaisePollRequestNotifier(destinations, new object[] { clientId, callbackId, eventType }, true);
            }
        }
        public override void RaiseOldCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason,
           OperationContext operationContext, EventContext eventContext)
        {
            RaiseOldCustomRemoveCalbackNotifier(key, cacheEntry, reason, true, operationContext, eventContext);
        }
        /// <summary>
        /// Reaises the custom item remove call baack.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="notification"></param>
        public override void RaiseCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason,
            OperationContext operationContext, EventContext eventContext)
        {
            RaiseCustomRemoveCalbackNotifier(key, cacheEntry, reason, true, operationContext, eventContext);
        }

        /// <summary>
        /// sends a custom item update callback to the node from which callback was added.
        /// </summary>
        /// <param name="dest">Addess of the callback node</param>
        /// <param name="packed">key,item and actual callback</param>
        private void RaiseCustomUpdateCalbackNotifier(ArrayList dests, object packed, EventContext eventContext, bool broadCasteClusterEvent = true)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            bool sendLocal = false;
            object[] objs = packed as object[];
            ArrayList callbackListeners = objs[1] as ArrayList;

            if (dests.Contains(Cluster.LocalAddress))
            {
                dests.Remove(Cluster.LocalAddress);
                sendLocal = true;
            }

            if (ValidMembers.Count > 1 && broadCasteClusterEvent)
            {
                _cluster.SendNoReplyMcastMessageAsync(dests,
                    new Function((int)OpCodes.NotifyCustomUpdateCallback,
                        new object[] { objs[0], callbackListeners.Clone(), objs[2], eventContext }));
            }

            if (sendLocal)
            {
                handleNotifyUpdateCallback(new object[] { objs[0], callbackListeners.Clone(), objs[2], eventContext });
            }
        }
        private void RaiseOldCustomUpdateCalbackNotifier(ArrayList dests, object packed, EventContext eventContext, bool broadCasteClusterEvent = true)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            bool sendLocal = false;
            object[] objs = packed as object[];
            ArrayList callbackListeners = objs[1] as ArrayList;

            if (dests.Contains(Cluster.LocalAddress))
            {
                dests.Remove(Cluster.LocalAddress);
                sendLocal = true;
            }

            if (ValidMembers.Count > 1 && broadCasteClusterEvent)
            {
                _cluster.SendNoReplyMcastMessageAsync(dests,
                    new Function((int)OpCodes.NotifyOldCustomUpdateCallback,
                        new object[] { objs[0], callbackListeners.Clone(), objs[2], eventContext }));
            }

            if (sendLocal)
            {
                handleOldNotifyUpdateCallback(new object[] { objs[0], callbackListeners.Clone(), objs[2], eventContext });
            }
        }
        private void RaiseCustomUpdateCalbackNotifier(ArrayList dests, object packed)
        {
            RaiseCustomUpdateCalbackNotifier(dests, packed, null);
        }


        protected void RaiseCustomUpdateCalbackNotifier(object key, ArrayList itemUpdateCallbackListener)
        {
            RaiseCustomUpdateCalbackNotifier(key, itemUpdateCallbackListener, null);
        }

        protected EventContext CreateEventContext(OperationContext operationContext,
            Alachisoft.NCache.Persistence.EventType eventType)
        {
            EventContext eventContext = new EventContext();
            OperationID opId = operationContext != null ? operationContext.OperatoinID : null;
            //generate event id
            if (operationContext == null || !operationContext.Contains(OperationContextFieldName.EventContext))
            //for atomic operations
            {
                eventContext.EventID = EventId.CreateEventId(opId);
            }
            else //for bulk
            {
                eventContext.EventID =
                    ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
            }

            eventContext.EventID.EventType = eventType;
            return eventContext;

        }

        protected virtual EventDataFilter GetGeneralDataEventFilter(Caching.Events.EventTypeInternal eventType)
        {
            return EventDataFilter.None;
        }

        protected void RaiseCustomUpdateCalbackNotifier(object key, CacheEntry entry, CacheEntry oldEntry,
            OperationContext operationContext)
        {
            try
            {
                if (entry != null)
                    entry.MarkInUse(NCModulesConstants.Topology);
                if (oldEntry != null)
                    oldEntry.MarkInUse(NCModulesConstants.Topology);

                Caching.Notifications value = oldEntry.Notifications;
                EventContext eventContext = null;

                if (value != null && value.ItemUpdateCallbackListener != null && value.ItemUpdateCallbackListener.Count > 0)
                {
                    eventContext = CreateEventContext(operationContext, Alachisoft.NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK);

                    if (value != null)
                    {
                        eventContext.Item = CacheHelper.CreateCacheEventEntry(value.ItemUpdateCallbackListener, entry, Context);
                        eventContext.OldItem = CacheHelper.CreateCacheEventEntry(value.ItemUpdateCallbackListener, oldEntry, Context);

                        RaiseCustomUpdateCalbackNotifier(key, (ArrayList)value.ItemUpdateCallbackListener, eventContext);
                    }
                }
                else if (oldEntry.ItemUpdateCallbackListener != null && oldEntry.ItemUpdateCallbackListener.Count > 0)
                {
                    eventContext = CreateEventContext(operationContext, Alachisoft.NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK);
                    eventContext.Item = CacheHelper.CreateCacheEventEntry(oldEntry.ItemUpdateCallbackListener, entry, Context);
                    eventContext.OldItem = CacheHelper.CreateCacheEventEntry(oldEntry.ItemUpdateCallbackListener, oldEntry, Context);

                    RaiseCustomUpdateCalbackNotifier(key, (ArrayList)oldEntry.ItemUpdateCallbackListener, eventContext);
                }
            }
            finally
            {
                if (entry != null)
                    entry.MarkFree(NCModulesConstants.Topology);
                if (oldEntry != null)
                    oldEntry.MarkFree(NCModulesConstants.Topology);
            }
        }


        public override void RaiseOldCustomUpdateCalbackNotifier(object key, ArrayList itemUpdateCallbackListener,
            EventContext eventContext)
        {
            bool broadCasteClusterEvent = true;
            ArrayList destinations = null;
            ArrayList nodes = null;
            Hashtable intendedNotifiers = new Hashtable();
            if (itemUpdateCallbackListener != null && itemUpdateCallbackListener.Count > 0)
            {
                itemUpdateCallbackListener = itemUpdateCallbackListener.Clone() as ArrayList;
                if (_stats.Nodes != null)
                {
                    nodes = _stats.Nodes.Clone() as ArrayList;
                    destinations = new ArrayList();
                    foreach (CallbackInfo cbInfo in itemUpdateCallbackListener)
                    {
                        int index = nodes.IndexOf(new NodeInfo(Cluster.LocalAddress));
                        if (index != -1 && ((NodeInfo)nodes[index]).ConnectedClients.Contains(cbInfo.Client))
                        {
                            if (!destinations.Contains(Cluster.LocalAddress))
                            {
                                destinations.Add(Cluster.LocalAddress);
                            }
                            intendedNotifiers[cbInfo] = Cluster.LocalAddress;
                            continue;
                        }
                        else
                        {
                            foreach (NodeInfo nInfo in nodes)
                            {
                                if (nInfo.ConnectedClients != null && nInfo.ConnectedClients.Contains(cbInfo.Client))
                                {
                                    if (!destinations.Contains(nInfo.Address))
                                    {
                                        destinations.Add(nInfo.Address);
                                        intendedNotifiers[cbInfo] = nInfo.Address;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (destinations != null && destinations.Count > 0)
            {
                object[] packed = new object[] { key, itemUpdateCallbackListener, intendedNotifiers };
                ArrayList selectedServer = new ArrayList(1);
                ///[Ata]Incase of parition and partition of replica, there can be same clients connected
                ///to multiple server. therefore the destinations list will contain more then
                ///one servers. so the callback will be sent to the same client through different server
                ///to avoid this, we will check the list for local server. if client is connected with
                ///local node, then there is no need to send callback to all other nodes
                ///if there is no local node, then we select the first node in the list.

                RaiseOldCustomUpdateCalbackNotifier(destinations, packed, eventContext, broadCasteClusterEvent);
            }
        }


        /// <summary>
        /// sends a custom item update callback to the node from which callback was added.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="notification">callback entry</param>
        public override void RaiseCustomUpdateCalbackNotifier(object key, ArrayList itemUpdateCallbackListener,
            EventContext eventContext)
        {
            bool broadCasteClusterEvent = true;
            ArrayList destinations = null;
            ArrayList nodes = null;
            Hashtable intendedNotifiers = new Hashtable();
            if (itemUpdateCallbackListener != null && itemUpdateCallbackListener.Count > 0)
            {
                itemUpdateCallbackListener = itemUpdateCallbackListener.Clone() as ArrayList;
                if (_stats.Nodes != null)
                {
                    nodes = _stats.Nodes.Clone() as ArrayList;
                    destinations = new ArrayList();
                    foreach (CallbackInfo cbInfo in itemUpdateCallbackListener)
                    {
                        int index = nodes.IndexOf(new NodeInfo(Cluster.LocalAddress));
                        if (index != -1 && ((NodeInfo)nodes[index]).ConnectedClients.Contains(cbInfo.Client))
                        {
                            if (!destinations.Contains(Cluster.LocalAddress))
                            {
                                destinations.Add(Cluster.LocalAddress);
                            }
                            intendedNotifiers[cbInfo] = Cluster.LocalAddress;
                            continue;
                        }
                        else
                        {
                            foreach (NodeInfo nInfo in nodes)
                            {
                                if (nInfo.ConnectedClients != null && nInfo.ConnectedClients.Contains(cbInfo.Client))
                                {
                                    if (!destinations.Contains(nInfo.Address))
                                    {
                                        destinations.Add(nInfo.Address);
                                        intendedNotifiers[cbInfo] = nInfo.Address;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (destinations != null && destinations.Count > 0)
            {
                object[] packed = new object[] { key, itemUpdateCallbackListener, intendedNotifiers };
                ArrayList selectedServer = new ArrayList(1);
                ///[Ata]Incase of parition and partition of replica, there can be same clients connected
                ///to multiple server. therefore the destinations list will contain more then
                ///one servers. so the callback will be sent to the same client through different server
                ///to avoid this, we will check the list for local server. if client is connected with
                ///local node, then there is no need to send callback to all other nodes
                ///if there is no local node, then we select the first node in the list.

                RaiseCustomUpdateCalbackNotifier(destinations, packed, eventContext, broadCasteClusterEvent);
            }
        }

        


        /// <summary>
        ///
        /// </summary>
        /// <param name="result"></param>
        /// <param name="writeBehindOperationCompletedCallback"></param>
        protected void RaiseWriteBehindTaskCompleted(OpCode operationCode, object result, Caching.Notifications notification,
            OperationContext operationContext)
        {
            Address dest = null;
            ArrayList nodes = null;

            if (notification != null && notification.WriteBehindOperationCompletedCallback != null)
            {
                if (_stats.Nodes != null)
                {
                    nodes = _stats.Nodes.Clone() as ArrayList;
                    foreach (NodeInfo nInfo in nodes)
                    {
                        AsyncCallbackInfo asyncInfo = notification.WriteBehindOperationCompletedCallback as AsyncCallbackInfo;
                        if (nInfo.ConnectedClients != null && nInfo.ConnectedClients.Contains(asyncInfo.Client))
                        {
                            dest = nInfo.Address;
                        }
                    }
                }
            }
            if (dest != null)
            {
                string destinS = "[" + dest.ToString() + "]";

                if (dest.Equals(Cluster.LocalAddress))
                {
                    DoWrite("ClusterCacheBase.RaiseWriteBehindTaskCompleted", "local notify, destinations=" + destinS,
                        operationContext);
                    NotifyWriteBehindTaskCompleted(operationCode, result as Hashtable, notification, operationContext);
                }
                else
                {
                    DoWrite("ClusterCacheBase.RaiseWriteBehindTaskCompleted",
                        "clustered notify, destinations=" + destinS, operationContext);

                    Function func = new Function((int)OpCodes.NotifyWBTResult,
                        new object[] { operationCode, result, notification, operationContext }, true);
                    Cluster.SendNoReplyMessage(dest, func);
                }
            }
        }

        #endregion

        public override void ClientConnected(string client, bool isInproc, ClientInfo clientInfo)
        {
           
            if (_stats != null && _stats.LocalNode != null)
            {
                NodeInfo localNode = _stats.LocalNode;

                if (localNode.ConnectedClients != null)
                {
                    lock (localNode.ConnectedClients.SyncRoot)
                    {
                        if (!localNode.ConnectedClients.Contains(client))
                        {
                            localNode.ConnectedClients.Add(client);
                        }
                    }

                    if (!isInproc) UpdateClientsCount(localNode.Address, localNode.ConnectedClients.Count);
                                        
                    if(clientInfo.ClientVersion < 5000)
                    {
                        Interlocked.Increment(ref oldClients);

                        if (localNode.OldConnectedClientsInfo.Count == 0)
                        {
                            OperationContext operationContext = null;

                            try
                            {
                               
                                _eventManager.StartPolling(_context, operationContext);
                            }
                            finally
                            {
                                MiscUtil.ReturnOperationContextToPool(operationContext, Context.FakeObjectPool);
                                operationContext?.MarkFree(NCModulesConstants.Topology);
                            }
                        }
                        lock (localNode.OldConnectedClientsInfo.SyncRoot)
                        {
                            if (!localNode.OldConnectedClientsInfo.Contains(client))
                            {
                                localNode.OldConnectedClientsInfo.Add(client);
                            }
                        }

                    }
                }
            }
            if (InternalCache != null)
            {
                InternalCache.ClientConnected(client, isInproc, clientInfo);
            }
        }
     
        public override void ClientDisconnected(string client, bool isInproc, ClientInfo clientInfo)
        {
            if (_stats != null && _stats.LocalNode != null)
            {
                NodeInfo localNode = (NodeInfo)_stats.LocalNode;

                if (localNode.ConnectedClients != null)
                {
                    lock (localNode.ConnectedClients.SyncRoot)
                    {
                            localNode.ConnectedClients.Remove(client);
                    }

                    if (!isInproc) UpdateClientsCount(localNode.Address, localNode.ConnectedClients.Count);
                }

                if (localNode.OldConnectedClientsInfo.Contains(client))
                {
                    Interlocked.Decrement(ref oldClients);
                    lock (localNode.OldConnectedClientsInfo.SyncRoot)
                    {
                        localNode.OldConnectedClientsInfo.Remove(client);
                    }
                }

                if (oldClients == 1)
                {
                    localNode.OldConnectedClientsInfo.Remove("GeneralEventsOldClients");
                    _eventManager.StopPolling();
                }

            }           
            if (InternalCache != null)
            {
                InternalCache.ClientDisconnected(client, isInproc, clientInfo);
            }
        }


       

        

        #region/            ---Key based Notification registration ---      /

        /// <summary>
        /// Must be override to provide the registration of key notifications.
        /// </summary>
        /// <param name="operand"></param>
        public virtual object handleRegisterKeyNotification(object operand)
        {
            return null;
        }

        /// <summary>
        /// Must be override to provide the unregistration of key notifications.
        /// </summary>
        /// <param name="operand"></param>
        public virtual object handleUnregisterKeyNotification(object operand) { return null; }

        public virtual object handleRegisterPollingNotification(object operand) { return null; }

        public virtual PollingResult handlePoll(object operand) { return null; }


        public virtual void HandleDryPoll(object operand) {  }

        /// <summary>
        /// Sends a cluster wide request to resgister the key based notifications.
        /// </summary>
        /// <param name="key">key agains which notificaiton is to be registered.</param>
        /// <param name="updateCallback"></param>
        /// <param name="removeCallback"></param>
        public override void RegisterKeyNotification(string key, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] { key, updateCallback, removeCallback, operationContext };
            if (_cluster.Servers.Count > 1)
            {
                Function fun = new Function((byte)OpCodes.RegisterKeyNotification, obj, false);
                _cluster.BroadcastToMultiple(_cluster.Servers, fun, GroupRequest.GET_ALL, true);
            }
            else
                handleRegisterKeyNotification(obj);
        }

        public override void RegisterPollingNotification(short callbackId, OperationContext operationContext)
        {
            object[] obj = new object[] { callbackId, operationContext };
            if (_cluster.Servers.Count > 1)
            {
                Function fun = new Function((byte)OpCodes.RegisterPollingNotification, obj, false);
                _cluster.BroadcastToMultiple(_cluster.Servers, fun, GroupRequest.GET_ALL, true);
            }
            else
                handleRegisterKeyNotification(obj);
        }

        public override void RegisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] { keys, updateCallback, removeCallback, operationContext };
            if (_cluster.Servers.Count > 1)
            {
                Function fun = new Function((byte) OpCodes.RegisterKeyNotification, obj, false);
                fun.Cancellable = true;
                _cluster.BroadcastToMultiple(_cluster.Servers, fun, GroupRequest.GET_ALL, true);
            }
            else
                handleRegisterKeyNotification(obj);
        }

        /// <summary>
        /// Sends a cluster wide request to unresgister the key based notifications.
        /// </summary>
        /// <param name="key">key agains which notificaiton is to be uregistered.</param>
        /// <param name="updateCallback"></param>
        /// <param name="removeCallback"></param>
        public override void UnregisterKeyNotification(string key, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] { key, updateCallback, removeCallback, operationContext };
            if (_cluster.Servers.Count > 1)
            {
                Function fun = new Function((byte)OpCodes.UnregisterKeyNotification, obj, false);
                _cluster.BroadcastToMultiple(_cluster.Servers, fun, GroupRequest.GET_ALL, true);
            }
            else
                handleUnregisterKeyNotification(obj);
        }

        public override void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] { keys, updateCallback, removeCallback, operationContext };
            if (_cluster.Servers.Count > 1)
            {
                Function fun = new Function((byte) OpCodes.UnregisterKeyNotification, obj, false);
                fun.Cancellable = true;
                _cluster.BroadcastToMultiple(_cluster.Servers, fun, GroupRequest.GET_ALL, true);
            }
            else
                handleUnregisterKeyNotification(obj);
        }

        #endregion

     

        #region IDistributionPolicyMember Members

        public virtual CacheNode[] GetMirrorMap()
        {
            return null;
        }

        public virtual void InstallMirrorMap(CacheNode[] nodes)
        {
        }

        public virtual DistributionMaps GetDistributionMaps(DistributionInfoData distInfo)
        {
            return null;
        }

        public virtual ArrayList HashMap
        {
            get { return _hashmap; }
            set { _hashmap = value; }
        }

        public virtual Hashtable BucketsOwnershipMap
        {
            get { return _bucketsOwnershipMap; }
            set { _bucketsOwnershipMap = value; }
        }

        public virtual void EmptyBucket(int bucketId)
        {
        }

        public virtual void InstallHashMap(DistributionMaps distributionMaps, ArrayList leftMbrs)
        {
        }

        public void Clustered_BalanceDataLoad(Address targetNode, Address requestingNode)
        {
            try
            {
                Function func = new Function((int)OpCodes.BalanceNode, requestingNode, false);
                Cluster.SendMessage(targetNode, func, GroupRequest.GET_NONE);
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        protected void handleUpdateLockInfo(object info)
        {
            object[] args = info as object[];
            bool acquireLock = (bool)args[0];
            object key = args[1];
            object lockId = args[2];
            DateTime lockDate = (DateTime)args[3];
            LockExpiration lockExpiration = (LockExpiration)args[4];
            OperationContext operationContext = null;

            if (args.Length > 6)
                operationContext = args[6] as OperationContext;
            else
                operationContext = args[5] as OperationContext;

            if (InternalCache != null)
            {
                if (acquireLock)
                    InternalCache.Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
                else
                {
                    bool isPreemptive = (bool)args[5];
                    InternalCache.UnLock(key, lockId, isPreemptive, operationContext);
                }
            }
        }

        private void handleBalanceDataLoad(object info)
        {
            Address requestingNode = info as Address;
            PartNodeInfo partNode = new PartNodeInfo(requestingNode, null, false);
            DistributionInfoData distData = new DistributionInfoData(DistributionMode.Manual, ClusterActivity.None,
                partNode, false);
            DistributionMaps maps = GetMaps(distData);

            if (maps.BalancingResult == BalancingResult.Default)
            {
                PublishMaps(maps);
            }
        }


        /// <summary>
        /// Update Client connectivity status
        /// </summary>
        public void UpdateClientStatus(string client, bool isConnected, ClientInfo info)
        {
            try
            {
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("ClusteredCacheBase.UpdateClientStatus()",
                        " Updating Client Status accross the cluster");

                Object[] objects = null;
                if (isConnected)
                    objects = new Object[] { client, true, info };
                else
                    objects = new Object[] { client, false, DateTime.Now };
                Function func = new Function((int)OpCodes.UpdateClientStatus, objects, true);
                Cluster.BroadcastToMultiple(Cluster.OtherServers, func, GroupRequest.GET_NONE);
                handleUpdateClientStatus(_stats.LocalNode.Address, objects);
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.AnnouncePresence()", e.ToString());
            }
        }

        /// <summary>
        /// Update Client connectivity status
        /// </summary>
        private void handleUpdateClientStatus(Address sender, object Obj)
        {
            try
            {
                object[] args = Obj as object[];

                string client = args[0].ToString();
                bool isConnected = (bool)args[1];

                if (isConnected)
                {
                    ClientInfo info = (ClientInfo)args[2];
                    if (_context.ConnectedClients != null)
                    {
                        _context.ConnectedClients.ClientConnected(client, info, sender);
                      
                    }
                }
                else
                {
                    if (_context.ConnectedClients != null)
                    {
                        if(_context.ConnectedClients.ClientDisconnected(client, sender, (DateTime)args[2]))
                            NotifyModulesOfDeadClient(client);
                    }
                }
            }
            catch (Exception ex)
            {
                Context.NCacheLog.Error("handleUpdateClientStatus.AnnouncePresence()", ex.ToString());
            }
        }

        internal virtual void NotifyModulesOfDeadClient(string client)
        {
        }

        public void PrintHashMap(ArrayList HashMap, Hashtable BucketsOwnershipMap, string ModuleName)
        {
            ArrayList newMap = HashMap;
            Hashtable newBucketsOwnershipMap = BucketsOwnershipMap;

            string moduleName = ModuleName;

            try
            {
                if (newMap != null)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int i = 0; i < newMap.Count; i++)
                    {
                        sb.Append("  " + newMap[i].ToString());
                        if ((i + 1) % 10 == 0)
                        {
                            sb.Remove(0, sb.Length);
                        }
                    }
                }

                HashMapBucket bkt;
                if (newBucketsOwnershipMap != null)
                {
                    IDictionaryEnumerator ide = newBucketsOwnershipMap.GetEnumerator();
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    while (ide.MoveNext())
                    {
                        Address owner = ide.Key as Address;
                        ArrayList myMap = ide.Value as ArrayList;
                        int functionBkts = 0, bktsUnderTxfr = 0, bktsNeedTxfr = 0;

                        for (int i = 0; i < myMap.Count; i++)
                        {
                            bkt = myMap[i] as HashMapBucket;
                            switch (bkt.Status)
                            {
                                case BucketStatus.Functional:
                                    functionBkts++;
                                    break;

                                case BucketStatus.UnderStateTxfr:
                                    bktsUnderTxfr++;
                                    break;

                                case BucketStatus.NeedTransfer:
                                    bktsNeedTxfr++;
                                    break;
                            }

                            sb.Append("  " + bkt.ToString());
                            if ((i + 1) % 10 == 0)
                            {
                                sb.Remove(0, sb.Length);
                            }
                        }

                    }

                }
            }
            catch (Exception e)
            {
            }
        }

        public void PublishMaps(DistributionMaps distributionMaps)
        {
            Clustered_PublishMaps(distributionMaps);
        }

        internal virtual bool PublishStats(bool urgent)
        {
            try
            {
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("ClusteredCacheBase.AnnouncePresence()",
                        " announcing presence ;urget " + urgent);
                if (this.ValidMembers.Count > 1)
                {
                    Function func = new Function((int)OpCodes.PeriodicUpdate, handleReqStatus());
                    if (!urgent)
                        Cluster.SendNoReplyMessage(func);
                    else
                        Cluster.Broadcast(func, GroupRequest.GET_NONE, false, Priority.Normal);
                }
                return true;
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.AnnouncePresence()", e.ToString());
            }
            return false;
        }

        /// <summary>
        /// Handler for Periodic update (PULL model), i.e. on demand fetch of information
        /// from every node.
        /// </summary>
        protected virtual object handleReqStatus()
        {
            if (_stats.LocalNode != null)
            {
                NodeInfo localStats = _stats.LocalNode;
                localStats.CacheNodeStatus = GetNodeStatus();
                localStats.StatsReplicationCounter++;
                return localStats.Clone() as NodeInfo;
            }
            return null;
        }

        public void Clustered_PublishMaps(DistributionMaps distributionMaps)
        {
            try
            {
                Function func = new Function((int)OpCodes.PublishMap, new object[] { distributionMaps }, false);
                Cluster.Broadcast(func, GroupRequest.GET_NONE, false, Priority.High);
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("PartitionedCache.Clustered_PublishMaps()", e.ToString());
                throw new GeneralFailureException(e.Message, e);
            }
        }

        private void handlePublishMap(object info)
        {
            try
            {
                object[] package = info as object[];

                DistributionMaps distributionMaps = package[0] as DistributionMaps;
                InstallHashMap(distributionMaps, null);
                UpdateLocalBuckets();
                StartStateTransfer(true);
            }
            catch (Exception e)
            {
                if (Context.NCacheLog.IsErrorEnabled)
                    Context.NCacheLog.Error("PartitionedCache.handlePublishMap()", e.ToString());
            }
        }

        protected virtual DistributionMaps GetMaps(DistributionInfoData info)
        {
            return null;
        }

        protected virtual void StartStateTransfer(bool isBalanceDataLoad)
        {
        }

        #endregion

        public virtual string GetGroupId(Address affectedNode, bool isMirror)
        {
            return String.Empty;
        }

       

        internal object handleTransferQueue(Object req, Address src)
        {
           
                return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="result"></param>
        /// <param name="entry"></param>
        /// <param name="taskId"></param>
        public override void NotifyWriteBehindTaskStatus(OpCode opCode, Hashtable result, Caching.Notifications notification,
            string taskId, string providerName, OperationContext operationContext)
        {
            DequeueWriteBehindTask(new string[] { taskId }, providerName, operationContext);

            if (notification != null && notification.WriteBehindOperationCompletedCallback != null)
            {
                RaiseWriteBehindTaskCompleted(opCode, result, notification, operationContext);
            }
        }

      

        /// <summary>
        ///
        /// </summary>
        /// <param name="taskId"></param>
        protected virtual void DequeueWriteBehindTask(string[] taskId, string providerName,
            OperationContext operationContext)
        {
            //to be implemented by derived classes.
        }

    
    

        protected void ConfigureClusterForRejoiningAsync(ArrayList nodes)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(OnConfigureCluster), nodes);
        }

        private void OnConfigureCluster(object arg)
        {
            try
            {
                ConfigureClusterForRejoining(arg as ArrayList);
            }
            catch (Exception e)
            {

            }
        }

        protected void ConfigureClusterForRejoining(ArrayList nodes)
        {
            Event evt = new Event(Event.CONFIGURE_NODE_REJOINING, nodes, Priority.High);
            _cluster.ConfigureLocalCluster(evt);
        }

     


       
        

     
        

        /// <summary>
        /// Get a hashtable containg information off all nodes connected
        /// </summary>
        internal Hashtable NodesInformation
        {
            get { lock (_nodeInformationTable.SyncRoot) return this._nodeInformationTable; }
        }

        /// <summary>
        /// Add server node information to table only if port is not -1 (inproc cache) and
        /// there is no previous entry
        /// </summary>
        /// <param name="ipAddress">ip address of server node</param>
        /// <param name="rendrerPort">socket server port</param>
        /// <param name="connectedClients">numbers of client connected to that server</param>
        protected void AddServerInformation(Address ipAddress, int rendrerPort, int connectedClients)
        {
            if (rendrerPort == 0) return;
            ClusterNodeInformation nodeInfo;
            lock (_nodeInformationTable)
            {
                string address = ipAddress.IpAddress.ToString();
                if (!_nodeInformationTable.Contains(address))
                {
                    nodeInfo = new ClusterNodeInformation(rendrerPort, connectedClients);
                    _nodeInformationTable.Add(address, nodeInfo);
                }
                else
                {
                    nodeInfo = _nodeInformationTable[address] as ClusterNodeInformation;
                    nodeInfo.AddRef();
                }
            }
        }

        /// <summary>
        /// Update the number of clients connected to server node
        /// </summary>
        /// <param name="ipAddress">ip address of server node</param>
        /// <param name="clientsConnected">new clients connected count</param>
        protected void UpdateClientsCount(Address ipAddress, int clientsConnected)
        {
            lock (_nodeInformationTable)
            {
                string address = ipAddress.IpAddress.ToString();
                if (_nodeInformationTable.Contains(address))
                {
                    ClusterNodeInformation nodeInfo = _nodeInformationTable[address] as ClusterNodeInformation;
                    if (clientsConnected > 0)
                    {
                        //the partition to which clients connected is an active partition.
                        nodeInfo.ActivePartition = ipAddress;
                        ((ClusterNodeInformation)_nodeInformationTable[address]).ConnectedClients = clientsConnected;
                    }
                }
            }
        }

        /// <summary>
        /// Removes server node information from table
        /// </summary>
        /// <param name="ipAddress">ip address of server node</param>
        protected void RemoveServerInformation(Address ipAddress, int rendererPort)
        {
            if (rendererPort == 0) return;
            string address = ipAddress.IpAddress.ToString();
            lock (_nodeInformationTable)
            {
                if (_nodeInformationTable.Contains(address))
                {
                    ClusterNodeInformation nodeInfo = _nodeInformationTable[address] as ClusterNodeInformation;
                    //we reinitialize the client count.
                    if (nodeInfo.ActivePartition != null && nodeInfo.ActivePartition.Equals(ipAddress))
                        nodeInfo.ConnectedClients = 0;

                    if (nodeInfo.RemoveRef())
                        NodesInformation.Remove(address);
                }
            }
        }

        public override RequestStatus GetClientRequestStatus(string clientId, long requestId, long commandId,
            Address intendedServer)
        {
            RequestStatus requestStatus = null;

            try
            {
                Function func = new Function((int)OpCodes.GetClientRequestStatus,
                    new object[] { clientId, requestId, commandId });
                object rsp = Cluster.SendMessage(intendedServer, func, GroupRequest.GET_FIRST);

                if (rsp != null)
                {
                    requestStatus = (RequestStatus)rsp;
                }

                return requestStatus;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public virtual bool StartDryPoll (PollingResult pollingResult)
        {
            return false;       
        }

        protected void DryPoll (OperationContext context)
        {
            try
            {
                _context.AsyncProc.Enqueue(new PollTask(this, context));
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        protected PollingResult Clustered_Poll(ArrayList dests, OperationContext context)
        {
            PollingResult result = null;
            try
            {
                Function func = new Function((int)OpCodes.Poll, new object[] { context }, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return null;
                }
                ClusterHelper.ValidateResponses(results, typeof(PollingResult), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(PollingResult));
                if (rspList.Count <= 0)
                {
                    return null;
                }
                else
                {
                    result = new PollingResult();
                    Hashtable tbl = new Hashtable();
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp)im.Current;
                        PollingResult cList = (PollingResult)rsp.Value;
                        if (cList != null)
                        {
                            result.RemovedKeys.AddRange(cList.RemovedKeys);
                            result.UpdatedKeys.AddRange(cList.UpdatedKeys);
                        }
                    }
                }
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return result;
        }

        protected void Clustered_RegisterPollingNotification(ArrayList servers, short callbackId, OperationContext context, bool excludeSelf)
        {
            try
            {
                Function func = new Function((int)OpCodes.RegisterPollingNotification, new object[] { callbackId, context }, false);
                Cluster.Multicast(servers, func, GroupRequest.GET_NONE, false);
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }

        }

        protected void Clustered_RegisterPollingNotification(short callbackId, OperationContext context)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            ArrayList dests = new ArrayList();


            long clientLastViewId = GetClientLastViewId(context);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();
                Clustered_RegisterPollingNotification(servers, callbackId, context, true);
            }
            else if (!VerifyClientViewId(clientLastViewId))
            {
                throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
            }
            else
            {
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
                Local_RegisterPollingNotification(callbackId, context);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }
        }

        protected PollingResult Clustered_Poll(OperationContext context)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            PollingResult result = null;
            ArrayList dests = new ArrayList();


            long clientLastViewId = GetClientLastViewId(context);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();
                result = Clustered_Poll(servers, context);
            }
            else if (!VerifyClientViewId(clientLastViewId))
            {
                throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
            }
            else
            {
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
                result = Local_Poll(context);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }

            return result;

        }


        #region /           ---Stream Operations---                             /

        protected virtual OpenStreamResult handleOpenStreamOperation(Address source, OpenStreamOperation operation)
        {
            OpenStreamResult result = new OpenStreamResult();
            if (InternalCache != null)
            {
                result.LockAcquired = InternalCache.OpenStream(operation.Key, operation.LockHandle, operation.Mode,
                    operation.Group, operation.SubGroup, operation.ExpirationHint, operation.EvictionHint,
                    operation.OperationContext);
                result.ExecutionResult = ClusterOperationResult.Result.Completed;
            }

            return result;
        }

        protected virtual CloseStreamResult handleCloseStreamOperation(Address source, CloseStreamOperation operation)
        {
            CloseStreamResult result = new CloseStreamResult(ClusterOperationResult.Result.Completed);
            if (InternalCache != null)
                InternalCache.CloseStream(operation.Key, operation.LockHandle, operation.OperationContext);
            return result;
        }

        protected virtual ReadFromStreamResult handleReadFromStreamOperation(Address source,
            ReadFromStreamOperation operation)
        {
            ReadFromStreamResult result = null;
            if (InternalCache != null)
            {
                VirtualArray vBuffer = new VirtualArray(operation.Length);
                int bytesRead = InternalCache.ReadFromStream(ref vBuffer, operation.Key, operation.LockHandle,
                    operation.Offset, operation.Length, operation.OperationContext);
                result = new ReadFromStreamResult(vBuffer, bytesRead, ClusterOperationResult.Result.Completed);
            }

            return result;
        }

        protected virtual WriteToStreamResult handleWriteToStreamOperation(Address source,
            WriteToStreamOperation operation)
        {
            WriteToStreamResult result = new WriteToStreamResult();
            if (InternalCache != null)
            {
                result.ExecutionResult = ClusterOperationResult.Result.Completed;
                InternalCache.WriteToStream(operation.Key, operation.LockHandle, operation.Buffer, operation.SrcOffset,
                    operation.DstOffset, operation.Length, operation.OperationContext);
            }
            return result;
        }

        protected virtual GetStreamLengthResult handleGetStreamLengthOperation(Address source,
            GetStreamLengthOperation operation)
        {
            GetStreamLengthResult result = new GetStreamLengthResult();
            if (InternalCache != null)
            {
                result.ExecutionResult = ClusterOperationResult.Result.Completed;
                result.Length = InternalCache.GetStreamLength(operation.Key, operation.LockHandle,
                    operation.OperationContext);
            }

            return result;
        }

        #endregion


        /// <summary>
        /// Hanlder for clustered item update callback notification.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private Object HandleNotifyTaskCallback(Object info)
        {
            Object[] objs = (Object[])info;
            EventContext eventContext = null;
            IList callbackListeners = (IList)((objs[1] is IList) ? objs[1] : null);
            Hashtable intendedNotifiers = (Hashtable)((objs[2] is Hashtable) ? objs[2] : null);
            if (objs.Length > 3)
            {
                eventContext = (EventContext)objs[3];
            }

            IEnumerator ide = intendedNotifiers.GetEnumerator();
            DictionaryEntry KeyValue;
            while (ide.MoveNext())
            {
                KeyValue = (DictionaryEntry)ide.Current;
                Object Key = KeyValue.Key;
                Object Value = KeyValue.Value;
                CallbackInfo cbinfo = (CallbackInfo)((Key is CallbackInfo) ? Key : null);
                Address node = (Address)((Value is Address) ? Value : null);

                if (node != null && !node.Equals(this.Cluster.LocalAddress))
                {
                    callbackListeners.Remove(cbinfo);
                }
            }
            return null;
        }
        

        private void HandleDeadClients(object p)
        {
            InternalCache.DeclareDeadClients((string)p, null);
        }

        public override void DeclareDeadClients(string deadClient, ClientInfo info)
        {
            try
            {
                if (_context.NCacheLog.IsInfoEnabled)
                {
                    _context.NCacheLog.Info("ClusteredCacheBase.DeclaredDeadClients()",
                        " DeclaredDeadClients Status accross the cluster");
                }
                if (this.ValidMembers.Count > 1)
                {
                    Function func = new Function((byte)OpCodes.DeadClients, deadClient);
                    Cluster.Broadcast(func, GroupRequest.GET_NONE, false, Priority.Normal);
                }
                else
                {
                    HandleDeadClients(deadClient);
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("ClusteredCacheBase.AnnouncePresence()", e.ToString());
            }
        }

        protected bool IsReplicationSequenced(NodeInfo localNodeInfo, NodeInfo updatedNodeInfo)
        {

            if (localNodeInfo == null)
                return false;

            if (localNodeInfo.StatsReplicationCounter < updatedNodeInfo.StatsReplicationCounter)
                return true;
            else
				{
                    if (localNodeInfo.NodeGuid != updatedNodeInfo.NodeGuid)
                        return true;
                }

            return false;
        }

      



        internal override void HandleDeadClientsNotification(string deadClient, ClientInfo info)
        {
            try
            {
                bool localSendOnly = false;
                if (Cluster.IsCoordinator)
                {
                  
                    CleanDeadClientInfos(deadClient);
                    handleCleanDeadClientInfos(new object[] { Cluster.LocalAddress, deadClient });
                   
                   
                }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.HandleDeadClientsNotification()", e.ToString());
            }
        }

     

        internal void handleClientActivityListenerRegistered(object operands)
        {
            try
            {
                object[] pair = (object[])operands;
                Address source = (Address)pair[0];
                string clientId = (string)pair[1];
                HashSet<string> clients;

                if (!source.Equals(_stats.LocalNode.Address))
                    lock (_otherNodesRegisterLock)
                    {
                        if (!_clientActivityListenersOnOtherNodes.TryGetValue(source, out clients))
                        {
                            clients = new HashSet<string>();
                            _clientActivityListenersOnOtherNodes.Add(source, clients);
                        }
                        if (!clients.Contains(clientId))
                            clients.Add(clientId);
                    }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.handleClientActivityListenerRegistered()", e.ToString());
            }
        }

        internal void handleClientActivityListenerUnregistered(object operands)
        {
            try
            {
                object[] pair = (object[])operands;
                Address source = (Address)pair[0];
                string clientId = (string)pair[1];
                HashSet<string> clients;

                if (!source.Equals(_stats.LocalNode.Address))
                    lock (_otherNodesRegisterLock)
                    {
                        if (_clientActivityListenersOnOtherNodes.TryGetValue(source, out clients))
                        {
                            clients.Remove(clientId);
                        }
                    }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.handleClientActivityListenerUnregistered()", e.ToString());
            }
        }





        internal void CleanDeadClientInfos(string clientId)
        {
            try
            {
                Cluster.BroadcastToMultiple(Cluster.OtherServers,
                    new Function((int)OpCodes.RemoveDeadClientInfo, clientId), GroupRequest.GET_NONE);

            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.CleanDeadClientInfos()", e.ToString());
            }
        }

        internal void handleCleanDeadClientInfos(object operand)
        {
            try
            {
                object[] operands = (object[])operand;
                Address sender = (Address)operands[0];
                string clientIds = (string)operands[1];
                lock (_stats.LocalNode.ConnectedClients.SyncRoot)
                {
                    foreach (NodeInfo node in _stats.Nodes)
                    {
                        node.OldConnectedClientsInfo.Remove(clientIds);
                    }
                }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.handleCleanDeadClientInfos()", e.ToString());
            }
        }

        public override IEnumerable<ClientInfo> GetConnectedClientsInfo()
        {
            if (_context.ConnectedClients != null)
            {
                return _context.ConnectedClients.GetAllConnectedClientInfos().Values;
            }
            return null;
        }

        public override bool IsClientConnected(string client)
        {
            var nodes = _stats.GetNodesClone();
            foreach (var nodeInfo in nodes)
            {
                if (nodeInfo.ConnectedClients.Contains(client))
                    return true;
            }
            return false;
        }

        #region -------------------------- Touch ------------------------------


        internal void Local_Touch(List<string> keys, OperationContext operationContext)
        {
            if (_internalCache != null)
                _internalCache.Touch(keys, operationContext);
        }

        private object HandleTouch(object info, Address src)
        {
            try
            {
                OperationContext operationContext = null;
                object[] args = (object[])info;
                if (args.Length > 1)
                    operationContext = args[1] as OperationContext;

                if (operationContext != null) operationContext.UseObjectPool = false;

                Local_Touch((List<string>)args[0], operationContext);
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        protected void Clustered_Touch(Address dest, List<string> keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.Clustered_Touch", "");

            try
            {
                Function func = new Function((int)OpCodes.Touch, new object[] { keys, operationContext });
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);
            }
            catch (Runtime.Exceptions.TimeoutException te)
            {
                throw;
            }
            catch (Runtime.Exceptions.SuspectedException se)
            {
                throw;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        #endregion


        #region pub-sub


          
      
        private TopicState HandleGetTopicsState()
        {
            if (InternalCache != null)
                return InternalCache.GetTopicsState();
            return null;
        }

       

        protected virtual bool HandleAssignSubscription(AssignmentOperation operation)
        {
            bool result = false;
            if (InternalCache != null)
            {
                result = InternalCache.AssignmentOperation(operation.MessageInfo, operation.SubscriptionInfo, operation.Type, operation.Context);
            }
            return result;
        }

        internal void HandleAcknowledgeMessageReceipt(AcknowledgeMessageOperation operation)
        {
            if (InternalCache != null)
            {
                InternalCache.AcknowledgeMessageReceipt(operation.ClientId, operation.TopicWiseMessageIds, operation.OperationContext);
            }
        }
        internal void HandleAcknowledgeMessageReceipt(AtomicAcknowledgeMessageOperation operation)
        {
            if (InternalCache != null)
            {
                InternalCache.AcknowledgeMessageReceipt(operation.ClientId, operation.Topic, operation.MessageId, operation.OperationContext);
            }

        }

      

        protected virtual void HandleRemoveMessages(RemoveMessagesOperation operation)
        {
            if (InternalCache != null) InternalCache.RemoveMessages(operation.MessagesToRemove, operation.Reason, operation.Context);
        }

        protected virtual void HandleRemoveMessages(AtomicRemoveMessageOperation operation)
        {
            if (InternalCache != null) InternalCache.RemoveMessages(operation.MessagesToRemove, operation.Reason, operation.Context);
        }

        protected virtual void HandleStoreMessage(StoreMessageOperation operation)
        {
            if (InternalCache != null) InternalCache.StoreMessage(operation.Topic, operation.Message, operation.Context);
        }

        protected virtual GetAssignedMessagesResponse HandleGetAssignedMessages(GetAssignedMessagesOperation operation)
        {
            GetAssignedMessagesResponse response = new GetAssignedMessagesResponse();

            if (InternalCache != null)
                response.MessageResponse= InternalCache.GetAssignedMessage(operation.SubscriptionInfo, operation.OperationContext);

            return response;
        }

        protected virtual bool HandleTopicOperation(ClusterTopicOperation topicOperation)
        {
            if (InternalCache != null) return InternalCache.TopicOperation(topicOperation.TopicOperation, topicOperation.OperationContext);

            return false;
        }


        public override bool TopicOperation(TopicOperation operation, OperationContext operationContext)
        {
            bool result = false;

            try

            {
                ClusterTopicOperation topicOperation = new ClusterTopicOperation(operation, operationContext);
                Function func = new Function((int)OpCodes.TopicOperation, topicOperation, false);
                RspList results = null;
                results = Cluster.Multicast(Servers, func, GroupRequest.GET_ALL, false);
                

                ClusterHelper.ValidateResponses(results, typeof(bool), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(bool));

                IEnumerator ia = rspList.GetEnumerator();
                while (ia.MoveNext())
                {
                    Rsp rsp = (Rsp)ia.Current;
                    if (rsp.Value != null)
                    {
                        result |= Convert.ToBoolean(rsp.Value);
                    }
                }
            }

            catch (CacheException e)
            {
                Context.NCacheLog.CriticalInfo(e.Message);
                throw e;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }

            return result;
        }

        public override MessageResponse GetAssignedMessage(SubscriptionInfo subscriptionInfo, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            GetAssignedMessagesResponse result = null;
            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();
                try
                {
                    result = Clustered_GetAssignedMessage(servers, subscriptionInfo, operationContext, IsRetryOnSuspected);
                }
                catch (NGroups.SuspectedException)
                {
                    if (!IsRetryOnSuspected) throw;

                    //Sleep is used to be sure that new view applied and node is marked in state transfer...
                    Thread.Sleep(_onSuspectedWait);
                    servers.Clear();

                    servers = GetServerParticipatingInStateTransfer();
                    result = Clustered_GetAssignedMessage(servers, subscriptionInfo, operationContext, IsRetryOnSuspected);
                }
            }
            else if (!VerifyClientViewId(clientLastViewId))
            {
                throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
            }
            else
            {
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
                if (IsClusterUnderMaintenance() && HasUndermaintenanceReplica())
                {
                    result = Clustered_GetAssignedMessage(GetNodeAndMirrorAddress(), subscriptionInfo, operationContext, IsRetryOnSuspected);
                }
                else
                {
                    return InternalCache.GetAssignedMessage(subscriptionInfo, operationContext);
                }
            }

            return result.MessageResponse;
        }

        protected GetAssignedMessagesResponse Clustered_GetAssignedMessage(ArrayList dests, SubscriptionInfo subscriptionInfo,
            OperationContext operationContext, bool throwSuspected = false)
        {
            GetAssignedMessagesResponse response = new GetAssignedMessagesResponse();

            try
            {
                GetAssignedMessagesOperation operation = new GetAssignedMessagesOperation(subscriptionInfo, operationContext);
                Function func = new Function((int)OpCodes.GetAssignedMessages, operation, false);

                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout);

                if (results == null)
                    return response;

                ClusterHelper.ValidateResponses(results, typeof(GetAssignedMessagesResponse), Name, throwSuspected);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(GetAssignedMessagesResponse));
                if (rspList.Count <= 0)
                    return response;
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp)im.Current;
                        GetAssignedMessagesResponse getResponse = (GetAssignedMessagesResponse)rsp.Value;
                        response.Merge(getResponse);
                    }
                }

                return response;
            }
            catch (CacheException)
            {
                throw;
            }
            catch (NGroups.SuspectedException e)
            {
                if (throwSuspected)
                {
                    throw;
                }

                throw new GeneralFailureException(e.Message, e);
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }

        }


        public override TopicState GetTopicsState()
        {
            TopicState retVal = null;

            int noOfRetries = 0;
            Exception error = null;
            do
            {
                try
                {
                    error = null;
                    Function func = new Function((int)OpCodes.GetTopicsState, null);
                    retVal = Cluster.SendMessage(Cluster.Coordinator, func, GroupRequest.GET_FIRST, false) as TopicState;

                    return retVal;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            while (noOfRetries++ < 3);

            if (error != null)
                throw new OperationFailedException("Colud not transfer topic state", error);


            return retVal;
        }


        public void TransferTopicState()
        {
            try
            {
                if (RequiresMessageStateTransfer)
                {
                    RequiresMessageStateTransfer = false;
                    Context.NCacheLog.CriticalInfo(Name + ".TransferTopicState", "Topic State Transfer has started.");

                    TopicState topicsState = GetTopicsState();
                    if (topicsState != null)
                        SetTopicsState(topicsState);

                    Context.NCacheLog.CriticalInfo(Name + ".TransferTopicState", "Topic State Transfer has ended.");
                }

            }
            catch (Exception ex)
            {
                Context.NCacheLog.Error(Name + ".TransferTopicState", " Transfering Topic State Transfer: " + ex.ToString());
            }

        }

    
        public void ApplyMessageOperation(object operation)
        {
            if (operation != null)
            {
                if (operation is StoreMessageOperation)
                {
                    HandleStoreMessage(operation as StoreMessageOperation);
                }
                else if (operation is AtomicRemoveMessageOperation)
                {
                    HandleRemoveMessages(operation as AtomicRemoveMessageOperation);
                }
                else if (operation is AtomicAcknowledgeMessageOperation)
                {
                    HandleAcknowledgeMessageReceipt(operation as AtomicAcknowledgeMessageOperation);
                }
                else if (operation is AssignmentOperation)
                {
                    HandleAssignSubscription(operation as AssignmentOperation);
                }
            }

        }


        public override TransferrableMessage GetTransferrableMessage(string topic, string messageId)
        {
            TransferrableMessage retVal = null;

            int noOfRetries = 0;
            Exception error = null;
            do
            {
                try
                {
                    error = null;
                    Function func = new Function((int)OpCodes.GetTransferrableMessage, new GetTransferrableMessageOperation(topic, messageId));
                    retVal = Cluster.SendMessage(Cluster.Coordinator, func, GroupRequest.GET_FIRST, false) as TransferrableMessage;

                    return retVal;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            while (noOfRetries++ < 3);

            if (error != null)
                throw new OperationFailedException("Colud not transfer topic state", error);

            return retVal;
        }

        public override OrderedDictionary GetMessageList(int bucketId, bool includeEventMessages)
        {
            OrderedDictionary retVal = null;

            int noOfRetries = 0;
            Exception error = null;
            do
            {
                try
                {
                    error = null;
                    Function func = new Function((int)OpCodes.GetMessageList, bucketId);
                    retVal = Cluster.SendMessage(Cluster.Coordinator, func, GroupRequest.GET_FIRST, false) as OrderedDictionary;

                    return retVal;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            while (noOfRetries++ < 3);

            if (error != null)
                throw new OperationFailedException("Colud not transfer topic state", error);

            return retVal;
        }

        internal virtual long Local_MessageCount(string topicName, OperationContext operationContext)
        {
            if (_internalCache != null)
            {
                return _internalCache.GetMessageCount(topicName, operationContext);
            }
            return 0;
        }

        protected long Clustered_MessageCount(ArrayList dests, string topicName)
        {
            long messageCount = 0;
            try
            {
                Function function = new Function((int)OpCodes.GetMessageCount, new object[] { topicName }, false);
                RspList results = Cluster.Multicast(dests, function, GroupRequest.GET_ALL, false);

                ClusterHelper.ValidateResponses(results, typeof(long), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(long));

                IEnumerator enumerator = rspList.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Rsp rsp = (Rsp)enumerator.Current;
                    if (rsp.Value != default(object))
                    {
                        messageCount += Convert.ToInt64(rsp.Value);
                    }
                }
            }
            catch (CacheException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
            return messageCount;
        }

        #endregion

        internal void StopBucketLoggingOnReplica(Address replicaAddress, ArrayList buckets)
        {
            Function func = new Function((int)OpCodes.StopBucketLogging, buckets, true);
            Cluster.SendMessage(replicaAddress, func, GroupRequest.GET_FIRST, false, Priority.Normal);
        }

        internal override void SetClusterInactive(string reason)
        {
            if (_cluster != null) _cluster.SetAsNonFunctional(reason);
        }

        
     


      
        
        
    }


  

}
