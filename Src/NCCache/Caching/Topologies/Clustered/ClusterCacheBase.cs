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
// limitations under the License.

using System;
using System.Collections;
using System.Linq;
using System.Threading;

using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.DatasourceProviders;

using Alachisoft.NCache.Runtime.Exceptions;

using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.Queries.Continuous;
using Alachisoft.NCache.Common.Mirroring;

using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common;

using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Caching;
using RequestStatus = Alachisoft.NCache.Common.DataStructures.RequestStatus;
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
using Alachisoft.NCache.MapReduce;
using Alachisoft.NCache.MapReduce.Notifications;

using Alachisoft.NCache.Common.DataReader;
using System.Diagnostics;
using Alachisoft.NCache.Common.Queries;

using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations.Messaging;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// A class to serve as the base for all clustered cache implementations.
    /// </summary>
    internal class ClusterCacheBase : CacheBase, IClusterParticipant, IPresenceAnnouncement, IDistributionPolicyMember
        //, IMirrorManagementMember
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
            /// OpCode for MapReduce
            /// </summary>
            MapReduceOperation,
            /// <summary>
            /// MapReduce Task Call Notification 
            /// </summary>
            NotifyTaskCallback,
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
            StopBucketLogging
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


        protected int _autoBalancingThreshold = 60; //60% of the average data size per node

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

        private HashVector<string, CacheClientConnectivityChangedCallback> _registeredClientsForNotification =
            new HashVector<string, CacheClientConnectivityChangedCallback>();

        private HashVector<Address, HashSet<string>> _clientActivityListenersOnOtherNodes =
            new HashVector<Address, HashSet<string>>();



        private readonly object _registerLock = new object(), _otherNodesRegisterLock = new object();

        protected int _serverFailureWaitTime = 2000;//time in msec
        

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

            return new Address(hostPort[0], hostPort.Length > 1 ? Convert.ToInt32(hostPort[1]):0);
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

        public bool HasDisposed
        {
            get { return _hasDisposed; }
            set { _hasDisposed = value; }
        }

        public virtual bool IsRetryOnSuspected 
        {
            get { return false; }
        }


        /// <summary>
        /// Get next task's sequence number
        /// </summary>
        /// <returns></returns>
        protected int NextSequence()
        {
            return ++_taskSequenceNumber;
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
                Function func = new Function((int) OpCodes.BlockActivity,
                    new object[] {uniqueId, _cluster.LocalAddress, interval}, false);
                RspList results = Cluster.Broadcast(func, GroupRequest.GET_ALL, false, Priority.High);
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("ClusterCacheBase.NotifyBlockActivity", e.ToString());
            }

        }

        public override void NotifyUnBlockActivity(string uniqueId)
        {
            Address server = (Address) _cluster.Renderers[Cluster.LocalAddress];

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
                    Address mappedAddress = (Address) Cluster.Renderers[clusterAddress];
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

        public virtual ArrayList ActiveServers
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

        public override ActiveQueryAnalyzer QueryAnalyzer
        {
            get { return InternalCache.QueryAnalyzer; }
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
                    val = val*1000;
                    _statsReplInterval = val;
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
                _cluster = new ClusterService(_context, this, this); //, this);
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
                _cluster.Initialize(properties, channelName, domain, identity,  twoPhaseInitialization,
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


                if ((_context.ExpiryMgr.IsCoordinatorNode || _context.ExpiryMgr.IsSubCoordinatorNode) && _context.ExpiryMgr.NotifBasedDepManager.SilentListeners.Count > 0)
                {
                    Thread t =
                        new Thread(new ThreadStart(_context.ExpiryMgr.NotifBasedDepManager.ActivateSilentListeners));
                    t.Name = "YukonDependencyManager.ActivateSilentListeners";
                    t.IsBackground = true;
                    t.Start();
                }

                if (_context.IsDbSyncCoordinator && _context.SyncManager!=null && _context.SyncManager.InactiveDependencies.Count > 0)
                {
                    // to start a parallel thread for activating CacheSyncDependencies.
                    Thread t = new Thread(new ThreadStart(_context.SyncManager.ActivateDependencies));
                    t.Name = "CacheSyncManager.ActivateDpendencies";
                    t.IsBackground = true;
                    t.Start();
                }

            }

         

            if (address != null && _context.DsMgr != null && _context.DsMgr._writeBehindAsyncProcess!=null)
            {
                _context.DsMgr._writeBehindAsyncProcess.NodeLeft(address.ToString());
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

            if (_cluster.IsCoordinator )
            {
                _statusLatch.SetStatusBit(NodeStatus.Coordinator, 0);
            }
            else
            {
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
                        ShutDownServerInfo info = (ShutDownServerInfo) _shutdownServers[addrs];
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

                case (int)OpCodes.NotifyCustomUpdateCallback:
                    return handleNotifyUpdateCallback(func.Operand);
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

                case (int)OpCodes.OpenStream:
                    return handleOpenStreamOperation(src, (OpenStreamOperation)func.Operand);

                case (int)OpCodes.CloseStream:
                    return handleCloseStreamOperation(src, (CloseStreamOperation)func.Operand);

                case (int)OpCodes.ReadFromStream:
                    return handleReadFromStreamOperation(src, (ReadFromStreamOperation)func.Operand);

                case (int)OpCodes.WriteToStream:
                    return handleWriteToStreamOperation(src, (WriteToStreamOperation)func.Operand);

                case (int)OpCodes.GetStreamLength:
                    return handleGetStreamLengthOperation(src, (GetStreamLengthOperation)func.Operand);


                case (int)OpCodes.SignalWBTState:
                    handleSignalTaskState(func.Operand);
                    break;

                case (int)OpCodes.WBTCompleted:
                    handleWriteThruTaskCompleted(func.Operand);
                    break;

                case (int)OpCodes.EnqueueWBOp:
                    handleEnqueueDSOperation(func.Operand);
                    break;

                case (int)OpCodes.NotifyWBTResult:
                    handleNotifyWriteBehindOperationComplete(func.Operand);
                    break;

                case (int)OpCodes.TransferQueue:
                    return handleTransferQueue(func.Operand, src);

                case (int)OpCodes.NotifyCQUpdate:
                    return handleCQUpdateCallback(func.Operand);
                    break;
                case (int)OpCodes.GetContinuousQueryStateInfo:
                    return handleGetContinuousQueryStateInfo();

                case (int)OpCodes.GetFilteredPersistentEvents:
                    return handleGetFileteredEvents(func.Operand);

                case (int)OpCodes.BlockActivity:
                    return handleBlockActivity(func.Operand);

                case (int)OpCodes.MapReduceOperation:
                    return HandleMapReduceOperation(func.Operand);

                case (int)OpCodes.NotifyTaskCallback:
                    return HandleNotifyTaskCallback(func.Operand);
                case (int)OpCodes.DeadClients:
                    HandleDeadClients(func.Operand);
                    break;

                case (int)OpCodes.Poll:
                    return handlePoll(func.Operand);
                case (int)OpCodes.RegisterPollingNotification:
                    return handleRegisterPollingNotification(func.Operand);

                case (int)OpCodes.GetClientRequestStatus:
                    return handleRequestStatusInquiry(func.Operand);
                case (int)OpCodes.ExecuteReader:
                    return handleExecuteReader(func.Operand);
                case (int)OpCodes.ExecuteReaderCQ:
                    return handleExecuteReaderCQ(func.Operand);
                case (int)OpCodes.GetReaderChunk:
                    return handleGetReaderChunk(func.Operand);
                case (int)OpCodes.DisposeReader:
                    handleDisposeReader(func.Operand);
                    break;
                case (int)OpCodes.UpdateClientStatus:
                    handleUpdateClientStatus(src, func.Operand);
                    break;

                case (int)OpCodes.RegisterClientActivityListener:
                    handleClientActivityListenerRegistered(new[] { src, func.Operand });
                    break;
                case (int)OpCodes.UnregisterClientActivityListener:
                    handleClientActivityListenerUnregistered(new[] { src, func.Operand });
                    break;

                case (int)OpCodes.InformEveryoneOfClientActivity:
                    handleInformEveryoneOfClientActivity(func.Operand);
                    break;

                case (int)OpCodes.RemoveDeadClientInfo:
                    handleCleanDeadClientInfos(new object[] { src, func.Operand });
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
                return InternalCache.GetMessageList(bucketId);
            }
            return null;
        }

        private object HandleGetTransferrableMessage(GetTransferrableMessageOperation operation)
        {
            if(InternalCache != null && operation != null)
            {
                return InternalCache.GetTransferrableMessage(operation.Topic, operation.Message);
            }
            return null;
        }

       

        private Alachisoft.NCache.Common.DataStructures.RequestStatus handleRequestStatusInquiry(object arguments)
        {
            if (_context.Render == null)
                return null;

            object[] data = (object[]) arguments;
            return _context.Render.GetRequestStatus((string) data[0], (long) data[1], (long) data[2]);
        }

        #endregion

        #region /                       ---State transfer related virtual methods ---          /

        internal virtual void AutoLoadBalance()
        {
        }

        internal virtual bool DetermineClusterStatus()
        {
            return false;
        }

        /// <summary>
        /// Fetch state from a cluster member. If the node is the coordinator there is
        /// no need to do the state transfer.
        /// </summary>
        internal virtual void EndStateTransfer(object result)
        {
            if (result is Exception)
            {
                NCacheLog.Error("ClusterCacheBase.EndStateTransfer",
                    " State transfer ended with Exception " + result.ToString());
                /// What to do? if we failed the state transfer?. Proabably we'll keep
                /// servicing in degraded mode? For the time being we don't!
            }

            /// Set the status to fully-functional (Running) and tell everyone about it.
            _statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);
            UpdateCacheStatistics();
            AnnouncePresence(true);
        }

        public void SignalEndOfStateTxfr(Address dest)
        {
            Function fun = new Function((int) OpCodes.SignalEndOfStateTxfr, new object());
            if (_cluster != null) _cluster.SendNoReplyMessage(dest, fun);
        }

        internal virtual Hashtable LockBuckets(ArrayList bucketIds)
        {
            return null;
        }

        /// <summary>
        /// Announces that given buckets are under state transfer and every body
        /// in the cluster should know about their statetransfer.
        /// </summary>
        /// <param name="bucketIds"></param>
        internal virtual void AnnounceStateTransfer(ArrayList bucketIds)
        {
            Clustered_AnnounceStateTransfer(bucketIds);
        }

        protected void Clustered_AnnounceStateTransfer(ArrayList bucketIds)
        {
            Function function = new Function((int) OpCodes.AnnounceStateTransfer, bucketIds, false);
            Cluster.Broadcast(function, GroupRequest.GET_NONE, false, Priority.High);
        }

        internal virtual StateTxfrInfo TransferBucket(ArrayList bucketIds, Address targetNode, byte transferType,
            bool sparsedBuckets, int expectedTxfrId, bool isBalanceDataLoad)
        {
            return Clustered_TransferBucket(targetNode, bucketIds, transferType, sparsedBuckets, expectedTxfrId,
                isBalanceDataLoad);
        }

        /// <summary>
        /// Retrieve the list of keys from the cache for the given group or sub group.
        /// </summary>
        protected StateTxfrInfo Clustered_TransferBucket(Address targetNode, ArrayList bucketIds, byte transferType,
            bool sparsedBuckets, int expectedTxfrId, bool isBalanceDataLoad)
        {
            try
            {
                Function func = new Function((int) OpCodes.TransferBucket,
                    new object[] {bucketIds, transferType, sparsedBuckets, expectedTxfrId, isBalanceDataLoad}, true);
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("ClusteredCacheBase.Clustered_TransferBucket",
                        " Sending request for bucket transfer to " + targetNode);
                object result = Cluster.SendMessage(targetNode, func, GroupRequest.GET_FIRST, false);
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("ClusteredCacheBase.Clustered_TransferBucket",
                        " Response recieved from " + targetNode);

                OperationResponse opResponse = result as OperationResponse;
                StateTxfrInfo transferInfo = null;
                if (opResponse != null)
                {
                    transferInfo = opResponse.SerializablePayload as StateTxfrInfo;
                    if (transferInfo != null)
                    {
                        if (transferInfo.data != null)
                        {
                            string[] keys = new string[transferInfo.data.Keys.Count];
                            transferInfo.data.Keys.CopyTo(keys, 0);
                        }
                    }
                }

                return transferInfo;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        protected static HashVector GetAllPayLoads(IList userPayLoad, IList compilationInfo)
        {
            HashVector result = new HashVector();
            VirtualArray payLoadArray = new VirtualArray(userPayLoad);
            Alachisoft.NCache.Common.DataStructures.VirtualIndex virtualIndex =
                new Alachisoft.NCache.Common.DataStructures.VirtualIndex();
            for (int i = 0; i < compilationInfo.Count; i++)
            {
                if ((long) compilationInfo[i] == 0)
                {
                    result[i] = null;
                }
                else
                {
                    VirtualArray atomicPayLoadArray = new VirtualArray((long) compilationInfo[i]);
                    Alachisoft.NCache.Common.DataStructures.VirtualIndex atomicVirtualIndex =
                        new Alachisoft.NCache.Common.DataStructures.VirtualIndex();

                    VirtualArray.CopyData(payLoadArray, virtualIndex, atomicPayLoadArray, atomicVirtualIndex,
                        (int) atomicPayLoadArray.Size);
                    virtualIndex.IncrementBy((int) atomicPayLoadArray.Size);
                    result[i] = atomicPayLoadArray.BaseArray;
                }
            }
            return result;
        }

        internal virtual void AckStateTxfrCompleted(Address owner, ArrayList bucketIds)
        {
        }

        internal virtual void ReleaseBuckets(ArrayList bucketIds)
        {
            Clustered_ReleaseBuckets(bucketIds);
        }

        protected void Clustered_ReleaseBuckets(ArrayList bucketIds)
        {
            Function function = new Function((int) OpCodes.ReleaseBuckets, bucketIds, false);
            Cluster.Broadcast(function, GroupRequest.GET_NONE, false, Priority.High);
        }

        internal virtual void StartLogging(ArrayList bucketIds)
        {
        }

        internal virtual bool IsBucketsTransferable(ArrayList bucketIds, Address owner)
        {
            return true;
        }

        #endregion

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
                    Function func = new Function((int) OpCodes.PeriodicUpdate, handleReqStatus());
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

        #region Client Activity Notification


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
                Function func = new Function((int) OpCodes.Get, new object[] {key, operationContext, isUserOperaton});
                object result = Cluster.SendMessage(address, func, GroupRequest.GET_FIRST);
                if (result == null)
                {
                    return retVal;
                }
                retVal = (CacheEntry) ((OperationResponse) result).SerializablePayload;
                if (retVal != null && ((OperationResponse) result).UserPayload != null)
                    retVal.Value = ((OperationResponse) result).UserPayload;
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
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.Get", "");

            CacheEntry retVal = null;
            try
            {
                Priority priority = Priority.Normal;
                if (operationContext.Contains(OperationContextFieldName.IsClusteredOperation))
                {
                    priority = Priority.Critical;
                    return new CacheEntry() { IsSurrogate = true};
                }
                Function func = new Function((int)OpCodes.Get, new object[] { key, lockId, lockDate, access, version, lockExpiration, operationContext });
                object result = Cluster.SendMessage(address, func, GetFirstResponse, priority);
                if (result == null)
                {
                    return retVal;
                }

                object[] objArr = (object[]) ((OperationResponse) result).SerializablePayload;
                retVal = objArr[0] as CacheEntry;

                if (retVal != null && ((OperationResponse) result).UserPayload != null)
                    retVal.Value = ((OperationResponse) result).UserPayload;

                lockId = objArr[1];

                lockDate = (DateTime) objArr[2];
                version = (ulong) objArr[3];
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

        protected LockOptions Clustered_Lock(Address address, object key, LockExpiration lockExpiration,
            ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.Lock", "");
            LockOptions retVal = null;
            try
            {
                Function func = new Function((int) OpCodes.LockKey,
                    new object[] {key, lockId, lockDate, lockExpiration, operationContext});
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
                Function func = new Function((int) OpCodes.IsLocked,
                    new object[] {key, lockId, lockDate, operationContext});
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
                Function func = new Function((int) OpCodes.UnLockKey,
                    new object[] {key, lockId, isPreemptive, operationContext});
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
                Function func = new Function((int) OpCodes.Get, new object[] {keys, operationContext});
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
                Function func = new Function((int) OpCodes.GetGroup,
                    new object[]
                    {key, group, subGroup, lockId, lockDate, accessType, version, lockExpiration, operationContext});
                object result = Cluster.SendMessage(dest, func, GetFirstResponse);

                if (result == null)
                {
                    return retVal;
                }

                object[] objArr = (object[]) ((OperationResponse) result).SerializablePayload;
                retVal = objArr[0] as CacheEntry;
                if (retVal != null)
                {
                    retVal.Value = ((OperationResponse) result).UserPayload;
                }
                lockId = objArr[1];
                lockDate = (DateTime) objArr[2];
                version = (ulong) objArr[3];
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
        protected Hashtable Clustered_GetGroup(Address dest, object[] keys, string group, string subGroup,
            OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int) OpCodes.GetGroup,
                    new object[] {keys, group, subGroup, operationContext});
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
                result = Local_Poll(operationContext);
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
            }
            return servers;
        }
        /// <summary>
        /// Retrieve the list of keys from the cache for the given group or sub group.
        /// </summary>
        public override ArrayList GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            ArrayList list = null;
            ArrayList dests = new ArrayList();


            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();
                try
                {
                     list = Clustered_GetGroupKeys(servers, group, subGroup, operationContext, IsRetryOnSuspected);              
                }
                catch (Alachisoft.NGroups.SuspectedException ex)
                {
                    if (!IsRetryOnSuspected) throw;

                    //Sleep is used to be sure that new view applied and node is marked in state transfer...
                    Thread.Sleep(_onSuspectedWait);
                    servers = GetServerParticipatingInStateTransfer();

                list = Clustered_GetGroupKeys(servers, group, subGroup, operationContext);
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
                list = Local_GetGroupKeys(group, subGroup, operationContext);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }

            return list;
        }

        /// <summary>
        /// Retrieve the list of key and value pairs from the cache for the given group or sub group.
        /// </summary>
        public override HashVector GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            HashVector list = null;
            ArrayList dests = new ArrayList();


            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();

                try
                {
                    list = Clustered_GetGroupData(servers, group, subGroup, operationContext,IsRetryOnSuspected);
                }
                catch (Alachisoft.NGroups.SuspectedException ex)
                {
                    if (!IsRetryOnSuspected) throw;

                    //Sleep is used to be sure that new view applied and node is marked in state transfer...
                    Thread.Sleep(_onSuspectedWait);
                    servers = GetServerParticipatingInStateTransfer();

                    list = Clustered_GetGroupData(servers, group, subGroup, operationContext);                   

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
                list = Local_GetGroupData(group, subGroup, operationContext);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }

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
            ArrayList dests = new ArrayList();


            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();
                try
                {
                    list = Clustered_GetTagKeys(servers, tags, comparisonType, operationContext, IsRetryOnSuspected);                                  
                }
                catch (Alachisoft.NGroups.SuspectedException ex)
                {
                    if (!IsRetryOnSuspected) throw;

                    //Sleep is used to be sure that new view applied and node is marked in state transfer...
                    Thread.Sleep(_onSuspectedWait);
                    servers = GetServerParticipatingInStateTransfer();

                list = Clustered_GetTagKeys(servers, tags, comparisonType, operationContext);
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
                list = Local_GetTagKeys(tags, comparisonType, operationContext);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }

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
            ArrayList dests = new ArrayList();


            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();

                try
                {
                    list = Clustered_GetTagData(servers, tags, comparisonType, operationContext,IsRetryOnSuspected);
                }
                catch (Alachisoft.NGroups.SuspectedException ex)
                {
                    if (!IsRetryOnSuspected) throw;

                        //Sleep is used to be sure that new view applied and node is marked in state transfer...
                        Thread.Sleep(_onSuspectedWait);
                        servers = GetServerParticipatingInStateTransfer();

                        list = Clustered_GetTagData(servers, tags, comparisonType, operationContext);
                    

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
                list = Local_GetTagData(tags, comparisonType, operationContext);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }

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
            ArrayList dests = new ArrayList();

            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();
                result = Clustered_RemoveByTag(servers, tags, comparisonType, notify, operationContext);
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
                result = Local_RemoveTag(tags, comparisonType, notify, operationContext);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }

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
            ArrayList dests = new ArrayList();

            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();
                result = Clustered_RemoveGroup(servers, group, subGroup, notify, operationContext);
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
                result = Local_RemoveGroup(group, subGroup, notify, operationContext);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieve the list of keys from the cache based on the specified query.
        /// </summary>
        public override QueryResultSet Search(string query, IDictionary values, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            QueryResultSet result = null;
            ArrayList dests = new ArrayList();

            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();

                try
                {
                    result = Clustered_Search(servers, query, values, operationContext, IsRetryOnSuspected);
                }
                catch (Alachisoft.NGroups.SuspectedException ex)
                {
                    if (!IsRetryOnSuspected) throw;


                    //Sleep is used to be sure that new view applied and node is marked in state transfer...
                    Thread.Sleep(_onSuspectedWait);
                    servers.Clear();

                    servers = GetServerParticipatingInStateTransfer();
                    result = Clustered_Search(servers, query, values, operationContext);
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
                result = Local_Search(query, values, operationContext);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieve the list of keys and values from the cache based on the specified query.
        /// </summary>
        public override QueryResultSet SearchEntries(string query, IDictionary values, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            QueryResultSet result = null;
            ArrayList dests = new ArrayList();


            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();

                try 
                { 
                    result = Clustered_SearchEntries(servers, query, values, operationContext,IsRetryOnSuspected);
                }
                catch (Alachisoft.NGroups.SuspectedException ex)
                {
                    if (!IsRetryOnSuspected) throw;
                    
                    
                        //Sleep is used to be sure that new view applied and node is marked in state transfer...
                        Thread.Sleep(_onSuspectedWait);
                        servers = GetServerParticipatingInStateTransfer();
                        result = Clustered_SearchEntries(servers, query, values, operationContext);
                    

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
                result = Local_SearchEntries(query, values, operationContext);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
                
            }



            return result;
        }

        /// <summary>
        /// Retrieve the list of keys from the cache based on the specified query and also register for nitifications.
        /// </summary>
        public override QueryResultSet SearchCQ(string query, IDictionary values, string clientUniqueId, string clientId,
            bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext,
            QueryDataFilters datafilters)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            QueryResultSet result = null;
            ArrayList dests = new ArrayList();


            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer(); 
                result = Clustered_SearchCQ(servers, query, values, clientUniqueId, clientId, notifyAdd, notifyUpdate,
                    notifyRemove, operationContext, datafilters);
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
                result = Local_SearchCQ(query, values, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove,operationContext, datafilters);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieve the list of keys and values from the cache based on the specified query and also register for nitifications.
        /// </summary>
        public override QueryResultSet SearchEntriesCQ(string query, IDictionary values, string clientUniqueId,
            string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext,
            QueryDataFilters datafilters)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            QueryResultSet result = null;
            ArrayList dests = new ArrayList();

            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();
                result = Clustered_SearchEntriesCQ(servers, query, values, clientUniqueId, clientId, notifyAdd,
                    notifyUpdate, notifyRemove, operationContext, datafilters);
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
                result = Local_SearchEntriesCQ(query, values, clientUniqueId, clientId, notifyAdd, notifyUpdate,notifyRemove, operationContext, datafilters);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }

            return result;
        }

        protected virtual QueryResultSet Local_SearchEntriesCQ(string query, IDictionary values, string clientUniqueId,
            string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext,
            QueryDataFilters datafilters)
        {
            throw new NotImplementedException();
        }

        protected virtual QueryResultSet Local_SearchCQ(string query, IDictionary values, string clientUniqueId,
            string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext,
            QueryDataFilters datafilters)
        {
            throw new NotImplementedException();
        }

        protected virtual QueryResultSet Local_SearchEntries(string query, IDictionary values,
            OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        protected virtual QueryResultSet Local_Search(string query, IDictionary values,
            OperationContext operationContext)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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

        protected DeleteQueryResultSet Clustered_DeleteQuery(ArrayList dests, string query, IDictionary values,
            bool notify, bool isUserOperation, ItemRemoveReason ir, OperationContext operationContext, Boolean throwSuspected = false)
        {
            DeleteQueryResultSet res = new DeleteQueryResultSet();
            try
            {
                Function func = new Function((int) OpCodes.DeleteQuery,
                    new object[] {query, values, notify, isUserOperation, ir, operationContext}, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return res;
                }
                ClusterHelper.ValidateResponses(results, typeof(DeleteQueryResultSet), Name, throwSuspected);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (DeleteQueryResultSet));
                if (rspList.Count <= 0)
                {
                    return res;
                }
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp) im.Current;
                        DeleteQueryResultSet result = (DeleteQueryResultSet) rsp.Value;

                        if (result != null)
                        {
                            res.KeysEffectedCount += result.KeysEffectedCount;
                            if (result.KeysDependingOnMe != null && result.KeysDependingOnMe.Count > 0)
                            {
                                ICollection keyList = result.KeysDependingOnMe.Keys;
                                foreach (string key in keyList)
                                {
                                    if (!res.KeysDependingOnMe.Contains(key))
                                    {
                                        res.KeysDependingOnMe.Add(key, null);
                                    }
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

            return res;
        }

        /// <summary>
        /// Retrieve the list of keys fron the cache for the given group or sub group.
        /// </summary>
        protected ArrayList Clustered_GetGroupKeys(ArrayList dests, string group, string subGroup,
            OperationContext operationContext, Boolean throwSuspected = false)
        {
            ArrayList list = null;
            try
            {
                Function func = new Function((int) OpCodes.GetKeys, new object[] {group, subGroup, operationContext},
                    false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return null;
                }
                ClusterHelper.ValidateResponses(results, typeof(ArrayList), Name, throwSuspected);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (ArrayList));
                if (rspList.Count <= 0)
                {
                    return null;
                }
                else
                {
                    Hashtable tbl = new Hashtable();
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp) im.Current;
                        ArrayList cList = (ArrayList) rsp.Value;
                        if (cList != null)
                        {
                            foreach (object key in cList)
                            {
                                tbl[key] = null;
                            }
                        }
                    }
                    list = new ArrayList(tbl.Keys);
                }
            }
            catch (CacheException) { throw; }
            catch (Alachisoft.NGroups.SuspectedException e) 
            {
                if (throwSuspected)
                {
                    throw;
                }

                throw new GeneralFailureException(e.Message, e);
            }
            catch (Exception e) { throw new GeneralFailureException(e.Message, e); }
            return list;
        }

        /// <summary>
        /// Retrieve the list of keys from the cache for the given group or sub group.
        /// </summary>
        protected HashVector Clustered_GetGroupData(ArrayList dests, string group, string subGroup,
            OperationContext operationContext,Boolean throwSuspected=false)
        {
            HashVector table = new HashVector();
            try
            {
                Function func = new Function((int)OpCodes.GetData, new object[] { group, subGroup, operationContext },
                    false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return null;
                }
                ClusterHelper.ValidateResponses(results, typeof(HashVector), Name, throwSuspected);
             
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(HashVector));
                if (rspList.Count <= 0)
                {
                    return null;
                }
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp)im.Current;
                        IDictionary entries = (IDictionary)rsp.Value;
                        if (entries != null)
                        {
                            IDictionaryEnumerator ide = entries.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                try
                                {
                                    table.Add(ide.Key, ide.Value);
                                }
                                catch (ArgumentException ex) //Overwrite entry with an updated one
                                {
                                    CacheEntry entry = ide.Value as CacheEntry;
                                    CacheEntry existingEntry = table[ide.Key] as CacheEntry;
                                    if (entry != null && existingEntry != null)
                                    {
                                        if (entry.Version > existingEntry.Version)
                                        {
                                            table[ide.Key] = entry;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Alachisoft.NGroups.SuspectedException e)
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

            return table;
        }

        /// <summary>
        /// Retrieve the list of keys fron the cache for the given tags.
        /// </summary>
        protected ClusteredArrayList Clustered_GetTagKeys(ArrayList dests, string[] tags,
            TagComparisonType comparisonType, OperationContext operationContext, Boolean throwSuspected = false)
        {
            ClusteredArrayList keys = null;

            try
            {
                Function func = new Function((int)OpCodes.GetKeysByTag,
                    new object[] { tags, comparisonType, operationContext }, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout * 10);
                if (results == null)
                {
                    return null;
                }
                ClusterHelper.ValidateResponses(results, typeof(ArrayList), Name, throwSuspected);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(ArrayList));
                if (rspList.Count <= 0)
                {
                    return null;
                }
                else
                {
                    Hashtable tbl = new Hashtable();
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp)im.Current;
                        ICollection cList = (ICollection)rsp.Value;
                        if (cList != null)
                        {
                            foreach (object key in cList)
                            {
                                tbl[key] = null;
                            }
                        }
                    }
                    keys = new ClusteredArrayList(tbl.Keys);
                }
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Alachisoft.NGroups.SuspectedException e)
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
            return keys;
        }

        /// <summary>
        /// Retrieve the list of keys from the cache for the given tags.
        /// </summary>
        protected HashVector Clustered_GetTagData(ArrayList dests, string[] tags, TagComparisonType comparisonType,
            OperationContext operationContext,Boolean throwSuspected=false)
        {
            HashVector table = new HashVector();

            try
            {
                Function func = new Function((int) OpCodes.GetTag, new object[] {tags, comparisonType, operationContext},
                    false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout*10);
                if (results == null)
                {
                    return null;
                }
                ClusterHelper.ValidateResponses(results, typeof(HashVector), Name, throwSuspected);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (HashVector));
                if (rspList.Count <= 0)
                {
                    return null;
                }
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp) im.Current;
                        IDictionary entries = (IDictionary) rsp.Value;
                        if (entries != null)
                        {
                            IDictionaryEnumerator ide = entries.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                try
                                {
                                    table.Add(ide.Key, ide.Value);
                                }
                                catch (ArgumentException ex) //Overwrite entry with an updated one
                                {
                                    CacheEntry entry = ide.Value as CacheEntry;
                                    CacheEntry existingEntry = table[ide.Key] as CacheEntry;
                                    if (entry != null && existingEntry != null)
                                    {
                                        if (entry.Version > existingEntry.Version)
                                        {
                                            table[ide.Key] = entry;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return table;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Alachisoft.NGroups.SuspectedException e)
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

        internal DeleteQueryResultSet Clustered_Delete(ArrayList dests, string query, IDictionary values, bool notify,
            bool isUserOperation, ItemRemoveReason ir, OperationContext operationContext,Boolean throwSuspected=false)
        {
            return Clustered_DeleteQuery(dests, query, values, notify, isUserOperation, ir, operationContext,throwSuspected);
        }

        /// <summary>
        /// Retrieve the list of keys from the cache based on the specified query.
        /// </summary>
        protected QueryResultSet Clustered_Search(ArrayList dests, string queryText, IDictionary values,
            OperationContext operationContext,Boolean throwSuspected=false)
        {
            QueryResultSet resultSet = new QueryResultSet();

            try
            {
                Function func = new Function((int) OpCodes.Search, new object[] {queryText, values, operationContext},
                    false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout*10);

                if (results == null)
                    return null;
                ClusterHelper.ValidateResponses(results, typeof(QueryResultSet), Name, throwSuspected);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (QueryResultSet));
                if (rspList.Count <= 0)
                    return null;
                else
                {
                    IEnumerator im = rspList.GetEnumerator();                  
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp) im.Current;
                        QueryResultSet cResultSet = (QueryResultSet) rsp.Value;                       
                        resultSet.Compile(cResultSet);
                    }                   
                    //remove duplicates
                    if (resultSet.SearchKeysResult != null)
                    {
                        if (resultSet.SearchKeysResult.Count > 0)
                        {
                            Hashtable tbl = new Hashtable();

                            foreach (object key in resultSet.SearchKeysResult)
                            {
                                tbl[key] = null;
                            }
                            resultSet.SearchKeysResult = new ClusteredArrayList(tbl.Keys);                           
                        }
                    }                    
                }

                return resultSet;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Alachisoft.NGroups.SuspectedException e)
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

        /// <summary>
        /// Retrieve the list of keys and values from the cache based on the specified query.
        /// </summary>
        protected QueryResultSet Clustered_SearchEntries(ArrayList dests, string queryText, IDictionary values,
            OperationContext operationContext,Boolean throwSuspected=false)
        {
            QueryResultSet resultSet = new QueryResultSet();

            try
            {
                Function func = new Function((int) OpCodes.SearchEntries,
                    new object[] {queryText, values, operationContext}, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout*10);

                if (results == null)
                    return null;
                ClusterHelper.ValidateResponses(results, typeof(QueryResultSet), Name, throwSuspected);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (QueryResultSet));
                if (rspList.Count <= 0)
                {
                    return null;
                }
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp) im.Current;
                        QueryResultSet cResultSet = (QueryResultSet) rsp.Value;
                        resultSet.Compile(cResultSet);
                    }
                }

                return resultSet;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Alachisoft.NGroups.SuspectedException e)
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

        /// <summary>
        /// Retrieve the list of keys from the cache based on the specified query and also register the query for notification.
        /// </summary>
        protected QueryResultSet Clustered_SearchCQ(ArrayList dests, string query, IDictionary values,
            string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove,
            OperationContext operationContext, QueryDataFilters datafilters)
        {
            QueryResultSet resultSet = new QueryResultSet();
            try
            {
                ContinuousQuery cQuery = CQManager.GetCQ(query, values);

                Function func = new Function((int) OpCodes.SearchCQ,
                    new object[]
                    {
                        cQuery, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, operationContext,
                        datafilters
                    }, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout*10);

                if (results == null)
                    return null;
                ClusterHelper.ValidateResponses(results, typeof (QueryResultSet), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (QueryResultSet));
                if (rspList.Count <= 0)
                {
                    return null;
                }
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp) im.Current;
                        QueryResultSet cResultSet = (QueryResultSet) rsp.Value;
                        resultSet.Compile(cResultSet);
                    }
                    //remove duplicates
                    if (resultSet.SearchKeysResult != null)
                    {
                        if (resultSet.SearchKeysResult.Count > 0)
                        {
                            Hashtable tbl = new Hashtable();
                            foreach (object key in resultSet.SearchKeysResult)
                            {
                                tbl[key] = null;
                            }
                            resultSet.SearchKeysResult = new ClusteredArrayList(tbl.Keys);
                        }
                    }
                }

                resultSet.CQUniqueId = cQuery.UniqueId;
                return resultSet;
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
        /// Retrieve the list of keys and values from the cache based on the specified query and also register the query for notification.
        /// </summary>
        protected QueryResultSet Clustered_SearchEntriesCQ(ArrayList dests, string query, IDictionary values,
            string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove,
            OperationContext operationContext, QueryDataFilters datafilters)
        {
            QueryResultSet resultSet = new QueryResultSet();

            try
            {
                ContinuousQuery cQuery = CQManager.GetCQ(query, values);

                Function func = new Function((int) OpCodes.SearchEntriesCQ,
                    new object[]
                    {
                        cQuery, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, operationContext,
                        datafilters
                    }, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout*10);

                if (results == null)
                    return null;
                ClusterHelper.ValidateResponses(results, typeof (QueryResultSet), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (QueryResultSet));
                if (rspList.Count <= 0)
                {
                    return null;
                }
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp) im.Current;
                        QueryResultSet cResultSet = (QueryResultSet) rsp.Value;
                        resultSet.Compile(cResultSet);
                    }
                }

                resultSet.CQUniqueId = cQuery.UniqueId;
                return resultSet;
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
                //NCacheLog.DevTrace("Clustered_Count", "Sending count request to " + Global.CollectionToString(dests));

                Function func = new Function((int) OpCodes.GetCount, null, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false);

                ClusterHelper.ValidateResponses(results, typeof (long), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (long));

                IEnumerator ia = rspList.GetEnumerator();
                while (ia.MoveNext())
                {
                    Rsp rsp = (Rsp) ia.Current;
                    if (rsp.Value != null)
                    {                        
                        retVal += Convert.ToInt64(rsp.Value);
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
            try
            {
                Function func = new Function((int) OpCodes.GetDataGroupInfo, new object[] {key, operationContext},
                    excludeSelf);
                
                RspList results = Cluster.Multicast(dest, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return retVal;
                }

                ClusterHelper.ValidateResponses(results, typeof (DataGrouping.GroupInfo), Name);

                Rsp rsp = ClusterHelper.GetFirstNonNullRsp(results, typeof (DataGrouping.GroupInfo));
                if (rsp == null)
                {
                    return retVal;
                }
                retVal = new ClusteredOperationResult((Address) rsp.Sender, rsp.Value);
            }
            catch (CacheException e)
            {
                Context.NCacheLog.Error("PartitionedServerCacheBase.Clustered_GetGroupInfo()", e.ToString());
                throw;
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("PartitionedServerCacheBase.Clustered_GetGroupInfo()", e.ToString());
                throw new GeneralFailureException(e.Message, e);
            }
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
                Function func = new Function((int) OpCodes.GetDataGroupInfo, new object[] {keys, operationContext},
                    excludeSelf);
                RspList results = Cluster.Multicast(dest, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return resultList;
                }

                ClusterHelper.ValidateResponses(results, typeof (Hashtable), Name);

                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (Hashtable));

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
                        Rsp rsp = (Rsp) im.Current;
                        resultList.Add(new ClusteredOperationResult((Address) rsp.Sender, rsp.Value));
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
                Function func = new Function((int) OpCodes.Contains, new object[] {key, operationContext});
                object result = Cluster.SendMessage(dest, func, GroupRequest.GET_FIRST);

                if (result != null && (bool) result)
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
                Function func = new Function((int) OpCodes.Contains, new object[] {keys, operationContext});
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
            return new CacheInsResultWithEntry();
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
        public sealed override void SendNotification(object notifId, object data)
        {
            if (ActiveServers.Count > 1)
            {
                object info = new object[] {notifId, data};
                Function func = new Function((int) OpCodes.NotifyCustomNotif, info, false);
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
                Function func = new Function((int) OpCodes.ReplicatedConnectionString, new object[] {connString, isSql},
                    excludeSelf);
                RspList results = Cluster.Multicast(dest, func, GroupRequest.GET_ALL, false);
                if (results == null)
                {
                    return null;
                }

                ClusterHelper.ValidateResponses(results, typeof (bool), Name);

                return ClusterHelper.GetAllNonNullRsp(results, typeof (bool));
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
            object[] objs = (object[]) info;
            string connString = (string) objs[0];
            bool isSql = (bool) objs[1];
            Context.ExpiryMgr.CacheDbSyncManager.AddConnectionString(connString, isSql);
            return true;
        }

        protected virtual void handleEnqueueDSOperation(object info)
        {
            object[] objs = (object[]) info;
            if (objs != null && objs.Length > 0)
            {
                if (objs[0] is DSWriteBehindOperation)
                {
                    DSWriteBehindOperation operation = objs[0] as DSWriteBehindOperation;
                    _context.DsMgr.WriteBehind(operation);
                }
                else if (objs[0] is ArrayList)
                    _context.DsMgr.WriteBehind((ArrayList) objs[0]);

            }
        }

        #endregion

        /// <summary>
        /// Hanlder for clustered user-defined notification.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private object handleCustomNotification(object info)
        {
            object[] objs = (object[]) info;
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

            object[] objs = (object[]) info;
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

        /// <summary>
        /// Hanlder for active query update callback notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// /// <param name="entry">callback entry</param>
        /// <returns>null</returns>
        private object handleCQUpdateCallback(object info)
        {
            object[] objs = (object[]) info;
            NotifyCQUpdateCallback(objs[0], (QueryChangeType) objs[1], objs[2] as List<CQCallbackInfo>, true, null,
                (EventContext) objs[3]);
            return null;
        }

        #region /                   ---Delete Query ----                /

        public override DeleteQueryResultSet DeleteQuery(string query, IDictionary values, bool notify,
            bool isUserOperation, ItemRemoveReason ir, OperationContext operationContext)
        {
            DeleteQueryResultSet result = null;

            long clientLastViewId;
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            try
            {
                if (_internalCache == null)
                    throw new InvalidOperationException();
                clientLastViewId = GetClientLastViewId(operationContext);
                if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
                {
                    ArrayList servers = GetServerParticipatingInStateTransfer();
                    try
                    {
                        result = Clustered_Delete(servers, query, values, notify, isUserOperation, ir, operationContext,IsRetryOnSuspected);
                    }
                    catch (Alachisoft.NGroups.SuspectedException ex)
                    {
                        if (!IsRetryOnSuspected) throw;
                        //Sleep is used to be sure that new view applied and node is marked in state transfer...
                        Thread.Sleep(_onSuspectedWait);
                        servers = GetServerParticipatingInStateTransfer();
                        result = Clustered_Delete(servers, query, values, notify, isUserOperation, ir, operationContext);
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
                    result = Local_DeleteQuery(query, values, notify, isUserOperation, ir, operationContext);
                    if (IsInStateTransfer())
                    {
                        throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                    }
                }

                return result;
            }
            finally
            {
            }
        }

        private DeleteQueryResultSet Local_DeleteQuery(string query, IDictionary values, bool notify,
            bool isUserOperation, ItemRemoveReason ir, OperationContext operationContext)
        {
            return _internalCache.DeleteQuery(query, values, notify, isUserOperation, ir, operationContext);
        }

        protected DeleteQueryResultSet handleDeleteQuery(object info)
        {
            DeleteQueryResultSet result = new DeleteQueryResultSet();
            if (_internalCache != null)
            {
                object[] data = (object[]) info;
                return Local_DeleteQuery(data[0] as string, data[1] as IDictionary, (bool) data[2], (bool) data[3],
                    (ItemRemoveReason) data[4], data[5] as OperationContext);              
            }
            return result;
        }

        #endregion

        /// <summary>
        /// Hanlder for clustered item remove callback notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// /// <param name="entry">callback entry</param>
        /// <returns>null</returns>
        protected object handleNotifyRemoveCallback(object info)
        {
            object[] objs = (object[]) info;
            //CallbackEntry cbEntry = objs[1] as CallbackEntry;
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
            NotifyCustomRemoveCallback(objs[0], null, (ItemRemoveReason) objs[1], true, operationContext, eventContext);
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
                object[] arguments = new object[] {clientID, events, registeredEventStatus};
                Address destination = GetDestinationForFilteredEvents();

                if (destination.Equals(Cluster.LocalAddress))
                {
                    return (List<NCache.Persistence.Event>) handleGetFileteredEvents(arguments);
                }
                else
                {
                    Function func = new Function((int) OpCodes.GetFilteredPersistentEvents, arguments, false);
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
            object[] args = (object[]) arguments;

            string clientID = (string) args[0];
            Hashtable events = (Hashtable) args[1];
            EventStatus registeredEventStatus = (EventStatus) args[2];
            if (_context.PersistenceMgr != null)
            {
                return _context.PersistenceMgr.GetFilteredEventsList(clientID, events, registeredEventStatus);
            }

            return null;
        }


        protected object handleBlockActivity(object arguments)
        {
            object[] args = (object[]) arguments;

            ShutDownServerInfo ssInfo = new ShutDownServerInfo();
            ssInfo.UniqueBlockingId = (string) args[0];
            ssInfo.BlockServerAddress = (Address) args[1];
            ssInfo.BlockInterval = (long) args[2];
            ssInfo.RenderedAddress = (Address) _cluster.Renderers[ssInfo.BlockServerAddress];
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
        protected void RaiseItemAddNotifier(object key, CacheEntry entry, OperationContext context,
            EventContext eventContext)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsItemAddNotifier && ValidMembers.Count > 1)
            {
                if (eventContext == null)
                {
                    eventContext = CreateEventContextForGeneralDataEvent(NCache.Persistence.EventType.ITEM_ADDED_EVENT, null,
                        entry, null);
                }

                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("ReplicatedBase.RaiseItemAddNotifier()", "onitemadded " + key);
                RaiseGeneric(new Function((int) OpCodes.NotifyAdd, new object[] {key, context, eventContext}));
            }
        }

        protected EventContext CreateEventContextForGeneralDataEvent(NCache.Persistence.EventType eventType,
            OperationContext context, CacheEntry entry, CacheEntry oldEntry)
        {
            EventContext eventContext = CreateEventContext(context, eventType);
            EventType generalEventType = EventType.ItemAdded;

            switch (eventType)
            {
                case NCache.Persistence.EventType.ITEM_ADDED_EVENT:
                    generalEventType = EventType.ItemAdded;
                    break;

                case NCache.Persistence.EventType.ITEM_UPDATED_EVENT:
                    generalEventType = EventType.ItemUpdated;
                    break;

                case NCache.Persistence.EventType.ITEM_REMOVED_EVENT:
                    generalEventType = EventType.ItemRemoved;
                    break;
            }

            eventContext.Item = CacheHelper.CreateCacheEventEntry(GetGeneralDataEventFilter(generalEventType), entry);
            if (oldEntry != null)
            {
                eventContext.OldItem = CacheHelper.CreateCacheEventEntry(GetGeneralDataEventFilter(generalEventType),
                    oldEntry);
            }

            return eventContext;
        }

        protected void FilterEventContextForGeneralDataEvents(EventType eventType, EventContext eventContext)
        {
            if (eventContext != null && eventContext.Item != null)
            {
                EventDataFilter filter = GetGeneralDataEventFilter(eventType);

                switch (filter)
                {
                    case EventDataFilter.Metadata:
                        eventContext.Item.Value = null;
                        if (eventContext.OldItem != null) eventContext.OldItem.Value = null;
                        break;

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
        protected void RaiseItemUpdateNotifier(object key, OperationContext operationContext, EventContext eventcontext)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsItemUpdateNotifier && ValidMembers.Count > 1)
            {
                RaiseGeneric(new Function((int) OpCodes.NotifyUpdate, new object[] {key, operationContext, eventcontext}));
            }
        }

        /// <summary>
        /// Broadcasts an itemremove notifier across the cluster
        /// </summary>
        /// <param name="packed">key or a list of keys to notify</param>
        protected void RaiseItemRemoveNotifier(object packed)
        {
            // If everything went ok!, initiate local and cluster-wide notifications.
            if (IsItemRemoveNotifier && ValidMembers.Count > 1)
            {
                RaiseGeneric(new Function((int) OpCodes.NotifyRemoval, packed));
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
                RaiseGeneric(new Function((int) OpCodes.NotifyClear, null));
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
                        new Function((int) OpCodes.NotifyCustomRemoveCallback, packed));
                }
                else
                    _cluster.Multicast(dests, new Function((int) OpCodes.NotifyCustomRemoveCallback, packed),
                        GroupRequest.GET_ALL, false);
            }

            if (sendLocal)
            {
                handleNotifyRemoveCallback(packed);
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
            EventType eventType = (EventType)objs[2];
            NotifyPollRequestCallback(clientId, callbackId, true, eventType);
            return null;
        }

        protected void RaiseAsyncCustomRemoveCalbackNotifier(object key, CacheEntry entry, ItemRemoveReason reason,
            OperationContext opContext, EventContext eventContext)
        {
            try
            {
                bool notify = true;

                if (reason == ItemRemoveReason.Expired)
                {
                    int notifyOnExpirationCount = 0;
                    if (entry != null)
                    {
                        CallbackEntry cbEntry = entry.Value as CallbackEntry;

                        if (cbEntry != null && cbEntry.ItemRemoveCallbackListener != null)
                        {
                            for (int i = 0; i < cbEntry.ItemRemoveCallbackListener.Count; i++)
                            {
                                CallbackInfo removeCallbackInfo = (CallbackInfo) cbEntry.ItemRemoveCallbackListener[i];
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
        }

        /// <summary>
        /// Reaises the custom item remove call baack.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cbEntry"></param>
        internal void RaiseCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason,
            bool async, OperationContext operationContext, EventContext eventContext)
        {
            ArrayList destinations = null;
            ArrayList nodes = null;
            Hashtable intendedNotifiers = new Hashtable();
            CallbackEntry cbEntry = cacheEntry.Value as CallbackEntry;

            if (cbEntry != null && cbEntry.ItemRemoveCallbackListener.Count > 0)
            {
                if (_stats.Nodes != null)
                {
                    nodes = _stats.Nodes.Clone() as ArrayList;

                    destinations = new ArrayList();
                    foreach (CallbackInfo cbInfo in cbEntry.ItemRemoveCallbackListener)
                    {
                        if (reason == ItemRemoveReason.Expired && cbInfo != null && !cbInfo.NotifyOnExpiration)
                            continue;

                        int index = nodes.IndexOf(new NodeInfo(Cluster.LocalAddress));
                        if (index != -1 && ((NodeInfo) nodes[index]).ConnectedClients.Contains(cbInfo.Client))
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
                    eventContext.Item = CacheHelper.CreateCacheEventEntry(cbEntry.ItemRemoveCallbackListener, cacheEntry);
                    eventContext.Add(EventContextFieldName.ItemRemoveCallbackList,
                        cbEntry.ItemRemoveCallbackListener.Clone());
                }

                object[] packed = new object[] {key, reason, intendedNotifiers, operationContext, eventContext};
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

        /// <summary>
        /// Reaises the custom item remove call baack.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cbEntry"></param>
        internal void RaiseCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason)
        {
            RaiseCustomRemoveCalbackNotifier(key, cacheEntry, reason, null,null);
        }

        internal void RaisePollRequestNotifier(string clientId, short callbackId, EventType eventType)
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

        /// <summary>
        /// Reaises the custom item remove call baack.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cbEntry"></param>
        internal void RaiseCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason,
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
                handleNotifyUpdateCallback(new object[] {objs[0], callbackListeners.Clone(), objs[2], eventContext});
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
                    ((EventContext) operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
            }

            eventContext.EventID.EventType = eventType;
            return eventContext;

        }

        protected virtual EventDataFilter GetGeneralDataEventFilter(EventType eventType)
        {
            return EventDataFilter.DataWithMetadata;
        }

        protected void RaiseCustomUpdateCalbackNotifier(object key, CacheEntry entry, CacheEntry oldEntry,
            OperationContext operationContext)
        {
            CallbackEntry value = oldEntry.Value as CallbackEntry;
            EventContext eventContext = null;

            if (value != null && value.ItemUpdateCallbackListener != null && value.ItemUpdateCallbackListener.Count > 0)
            {
                eventContext = CreateEventContext(operationContext,
                    Alachisoft.NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK);

                if (value != null)
                {
                    eventContext.Item = CacheHelper.CreateCacheEventEntry(value.ItemUpdateCallbackListener, entry);
                    eventContext.OldItem = CacheHelper.CreateCacheEventEntry(value.ItemUpdateCallbackListener, oldEntry);

                    RaiseCustomUpdateCalbackNotifier(key, (ArrayList)value.ItemUpdateCallbackListener, eventContext);
                }
            }
            else if (oldEntry.ItemUpdateCallbackListener != null && oldEntry.ItemUpdateCallbackListener.Count > 0)
            {
                eventContext = CreateEventContext(operationContext, Alachisoft.NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK);
                eventContext.Item = CacheHelper.CreateCacheEventEntry(oldEntry.ItemUpdateCallbackListener, entry);
                eventContext.OldItem = CacheHelper.CreateCacheEventEntry(oldEntry.ItemUpdateCallbackListener, oldEntry);

                RaiseCustomUpdateCalbackNotifier(key, (ArrayList)oldEntry.ItemUpdateCallbackListener, eventContext);
            }

        }

        /// <summary>
        /// sends a custom item update callback to the node from which callback was added.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="cbEntry">callback entry</param>
        protected void RaiseCustomUpdateCalbackNotifier(object key, ArrayList itemUpdateCallbackListener,
            EventContext eventContext, bool broadCasteClusterEvent = true)
        {
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
                        if (index != -1 && ((NodeInfo) nodes[index]).ConnectedClients.Contains(cbInfo.Client))
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
                object[] packed = new object[] {key, itemUpdateCallbackListener, intendedNotifiers};
                ArrayList selectedServer = new ArrayList(1);
                ///Incase of parition and partition of replica, there can be same clients connected
                ///to multiple server. therefore the destinations list will contain more then 
                ///one servers. so the callback will be sent to the same client through different server
                ///to avoid this, we will check the list for local server. if client is connected with
                ///local node, then there is no need to send callback to all other nodes
                ///if there is no local node, then we select the first node in the list.
                //if (destinations.Contains(Cluster.LocalAddress)) selectedServer.Add(Cluster.LocalAddress);
                //else selectedServer.Add(destinations[0]);
                RaiseCustomUpdateCalbackNotifier(destinations, packed, eventContext, broadCasteClusterEvent);
            }
        }

        /// <summary>
        /// Sends a custom query update callback to the node from which active query was added.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="cbEntry">callback entry</param>
        protected virtual void RaiseCQCallbackNotifier(string key, QueryChangeType changeType,
            List<CQCallbackInfo> queries, OperationContext opertionContext, EventContext eventContext)
        {
            ArrayList destinations = null;
            ArrayList nodes = null;
            List<CQCallbackInfo> localNotified = new List<CQCallbackInfo>();
            Dictionary<Address, List<CQCallbackInfo>> remoteNotified = new Dictionary<Address, List<CQCallbackInfo>>();
            if (queries != null && queries.Count > 0)
            {
                if (_stats.Nodes != null)
                {
                    nodes = _stats.Nodes.Clone() as ArrayList;
                    destinations = new ArrayList();
                    foreach (CQCallbackInfo info in queries)
                    {
                        int index = nodes.IndexOf(new NodeInfo(Cluster.LocalAddress));
                        // CQManager.GetClients(info.CQId) returns null for some queryID
                        // This is a CQ bug that some queries are unable to get registered in case of POR/Part/Rep (more than one node)
                        // queryID is generated by server and kept at two different locations differently -> CQManager and Query predicates. 
                        // Inconsistent data structure produces this inconsisten scenario
                        IList clients = CQManager.GetClients(info.CQId);
                        if (clients != null)
                        {
                            foreach (string clientId in clients)
                            {
                                if (!CQManager.AllowNotification(info.CQId, clientId, changeType))
                                {
                                    continue;
                                }
                                info.ClientIds.Add(clientId);
                                EventDataFilter datafilter = CQManager.GetDataFilter(info.CQId, clientId, changeType);
                                info.DataFilters.Add(clientId, datafilter);

                                if (index != -1 && ((NodeInfo) nodes[index]).ConnectedClients.Contains(clientId))
                                {
                                    if (!localNotified.Contains(info))
                                    {
                                        localNotified.Add(info);
                                    }
                                }
                                else
                                {
                                    foreach (NodeInfo nInfo in nodes)
                                    {
                                        if (nInfo.ConnectedClients != null && nInfo.ConnectedClients.Contains(clientId))
                                        {
                                            if (!remoteNotified.ContainsKey(nInfo.Address))
                                            {
                                                remoteNotified[nInfo.Address] = new List<CQCallbackInfo>();
                                            }

                                            List<CQCallbackInfo> queryInfos = remoteNotified[nInfo.Address];
                                            if (!queryInfos.Contains(info))
                                            {
                                                queryInfos.Add(info);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (localNotified.Count > 0)
            {
                handleCQUpdateCallback(new object[] {key, changeType, localNotified, eventContext});
            }

            if (remoteNotified.Count > 0)
            {
                foreach (Address dest in remoteNotified.Keys)
                {
                    RaiseCQCallbackNotifier(dest, key, changeType, remoteNotified[dest], eventContext);
                }
            }
        }

        /// <summary>
        /// Sends a custom query update callback to the node which is connected to the client the query was added from.
        /// </summary>
        /// <param name="dest">Addess of the callback node</param>
        /// <param name="packed">key,item and actual callback</param>
        private void RaiseCQCallbackNotifier(Address dest, string key, QueryChangeType changeType,
            List<CQCallbackInfo> queries, EventContext eventContext)
        {
            if (Cluster.ValidMembers.Count > 1)
            {
                Function func = new Function((int) OpCodes.NotifyCQUpdate,
                    new object[] {key, changeType, queries, eventContext});
                ArrayList list = new ArrayList();
                list.Add(dest);
                Cluster.Multicast(list, func, GroupRequest.GET_NONE, false, Cluster.Timeout*10);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="writeBehindOperationCompletedCallback"></param>
        protected void RaiseWriteBehindTaskCompleted(OpCode operationCode, object result, CallbackEntry cbEntry,
            OperationContext operationContext)
        {
            Address dest = null;
            ArrayList nodes = null;

            if (cbEntry != null && cbEntry.WriteBehindOperationCompletedCallback != null)
            {
                if (_stats.Nodes != null)
                {
                    nodes = _stats.Nodes.Clone() as ArrayList;
                    foreach (NodeInfo nInfo in nodes)
                    {
                        AsyncCallbackInfo asyncInfo = cbEntry.WriteBehindOperationCompletedCallback as AsyncCallbackInfo;
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
                    NotifyWriteBehindTaskCompleted(operationCode, result as Hashtable, cbEntry, operationContext);
                }
                else
                {
                    DoWrite("ClusterCacheBase.RaiseWriteBehindTaskCompleted",
                        "clustered notify, destinations=" + destinS, operationContext);

                    Function func = new Function((int) OpCodes.NotifyWBTResult,
                        new object[] {operationCode, result, cbEntry, operationContext}, true);
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
                        if (!localNode.LocalConnectedClientsInfo.ContainsKey(client))
                        {
                            localNode.LocalConnectedClientsInfo.Add(client, clientInfo);                         
                        }
                        
                        if (!localNode.ConnectedClients.Contains(client))
                        {
                            localNode.ConnectedClients.Add(client);
                        }
                    }

                    if (!isInproc) UpdateClientsCount(localNode.Address, localNode.ConnectedClients.Count);
                }
            }
        }
        


        public override void ClientDisconnected(string client, bool isInproc)
        {

            if (_stats != null && _stats.LocalNode != null)
            {
                NodeInfo localNode = (NodeInfo) _stats.LocalNode;
                if (localNode.ConnectedClients != null)
                {
                    lock (localNode.ConnectedClients.SyncRoot)
                    {
                        if (localNode.ConnectedClients.Contains(client))
                        {
                            localNode.ConnectedClients.Remove(client);
                        }
                    }
                    
                    if (!isInproc) UpdateClientsCount(localNode.Address, localNode.ConnectedClients.Count);
                }
            }
            if (InternalCache != null)
            {
                InternalCache.ClientDisconnected(client, isInproc);
            }

          
        }

        internal override void EnqueueDSOperation(DSWriteBehindOperation operation)
        {
            this.CheckDataSourceAvailabilityAndOptions(DataSourceUpdateOptions.WriteBehind);
            EnqueueWriteBehindOperation(operation);
            if (operation.TaskId == null)
                operation.TaskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();
            operation.Source = Cluster.LocalAddress.ToString();
            _context.DsMgr.WriteBehind(operation);
        }

        internal override void EnqueueDSOperation(ArrayList operationList)
        {
            this.CheckDataSourceAvailabilityAndOptions(DataSourceUpdateOptions.WriteBehind);
            if (operationList == null) return;
            DSWriteBehindOperation operation = null;
            ArrayList operations = new ArrayList();
            for (int i = 0; i < operationList.Count; i++) //update taskid and source
            {
                operation = operationList[i] as DSWriteBehindOperation;
                if (operation.TaskId == null)
                    operation.TaskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();
                operation.Source = Cluster.LocalAddress.ToString();
                operations.Add(operation);
            }
            if (operations.Count > 0)
            {
                EnqueueWriteBehindOperation(operations);
                _context.DsMgr.WriteBehind(operations);
            }
        }

        protected virtual void EnqueueWriteBehindOperation(ArrayList operations)
        {
            //to be implemented by derived classes.
        }

        protected virtual void EnqueueWriteBehindOperation(DSWriteBehindOperation operation)
        {
            //to be implemented by derived classes.
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

        /// <summary>
        /// Sends a cluster wide request to resgister the key based notifications.
        /// </summary>
        /// <param name="key">key agains which notificaiton is to be registered.</param>
        /// <param name="updateCallback"></param>
        /// <param name="removeCallback"></param>
        public override void RegisterKeyNotification(string key, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] {key, updateCallback, removeCallback, operationContext};
            if (_cluster.Servers.Count > 1)
            {
                Function fun = new Function((byte) OpCodes.RegisterKeyNotification, obj, false);
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
            object[] obj = new object[] {keys, updateCallback, removeCallback, operationContext};
            if (_cluster.Servers.Count > 1)
            {
                Function fun = new Function((byte) OpCodes.RegisterKeyNotification, obj, false);
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
            object[] obj = new object[] {key, updateCallback, removeCallback, operationContext};
            if (_cluster.Servers.Count > 1)
            {
                Function fun = new Function((byte) OpCodes.UnregisterKeyNotification, obj, false);
                _cluster.BroadcastToMultiple(_cluster.Servers, fun, GroupRequest.GET_ALL, true);
            }
            else
                handleUnregisterKeyNotification(obj);
        }

        public override void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] {keys, updateCallback, removeCallback, operationContext};
            if (_cluster.Servers.Count > 1)
            {
                Function fun = new Function((byte) OpCodes.UnregisterKeyNotification, obj, false);
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
                Function func = new Function((int) OpCodes.BalanceNode, requestingNode, false);
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
            bool acquireLock = (bool) args[0];
            object key = args[1];
            object lockId = args[2];
            DateTime lockDate = (DateTime) args[3];
            LockExpiration lockExpiration = (LockExpiration) args[4];
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
                    bool isPreemptive = (bool) args[5];
                    InternalCache.UnLock(key, lockId, isPreemptive, operationContext);
                }
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
                        objects = new Object[] {client, true, info};
                    else
                        objects = new Object[] {client, false, DateTime.Now};
                    Function func = new Function((int) OpCodes.UpdateClientStatus, objects, true);
                    Cluster.BroadcastToMultiple(Cluster.OtherServers, func, GroupRequest.GET_NONE);
                    handleUpdateClientStatus(_stats.LocalNode.Address,objects);
                
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
                bool isConnected = (bool) args[1];

                if (isConnected)
                {
                    ClientInfo info = (ClientInfo) args[2];
                    if (_context.ConnectedClients != null)
                    {
                        _context.ConnectedClients.ClientConnected(client, info, sender);
                        if (Cluster.IsCoordinator && info.Status == ConnectivityStatus.Connected)
                            InformEveryoneOfClientActivity(Parent.Name, info, ConnectivityStatus.Connected, false);
                    }
                }
                else
                {
                    if (_context.ConnectedClients != null)
                    {
                        _context.ConnectedClients.ClientDisconnected(client, sender, (DateTime) args[2]);
                    }
                }
            }
            catch (Exception ex)
            {
                Context.NCacheLog.Error("handleUpdateClientStatus.AnnouncePresence()", ex.ToString());
            }
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
                        if ((i + 1)%10 == 0)
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
                            if ((i + 1)%10 == 0)
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


        internal virtual bool PublishStats(bool urgent)
        {
            try
            {
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("ClusteredCacheBase.AnnouncePresence()",
                        " announcing presence ;urget " + urgent);
                if (this.ValidMembers.Count > 1)
                {
                    Function func = new Function((int) OpCodes.PeriodicUpdate, handleReqStatus());
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

        internal WriteBehindQueueResponse TransferQueue(Address coordinator, WriteBehindQueueRequest req)
        {

            Function func = new Function((int) OpCodes.TransferQueue, req);
            object result = Cluster.SendMessage(coordinator, func, GroupRequest.GET_FIRST);

            return result as WriteBehindQueueResponse;
        }

        internal object handleTransferQueue(Object req, Address src)
        {
            if (_context.DsMgr != null)
            {
                DataSourceCorresponder corr = null;
                lock (_wbQueueTransferCorresponders.SyncRoot)
                {

                    if (_wbQueueTransferCorresponders.Contains(src))
                    {
                        corr = _wbQueueTransferCorresponders[src] as DataSourceCorresponder;
                    }
                    else
                    {
                        corr = new DataSourceCorresponder(_context.DsMgr, Context.NCacheLog);
                        _wbQueueTransferCorresponders[src] = corr;
                    }
                }
                WriteBehindQueueResponse rsp = corr.GetWriteBehindQueue(req as WriteBehindQueueRequest);
                if (rsp != null && rsp.NextChunkId == null) _wbQueueTransferCorresponders.Remove(src);
                return rsp;
            }
            else
                return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="result"></param>
        /// <param name="entry"></param>
        /// <param name="taskId"></param>
        public override void NotifyWriteBehindTaskStatus(OpCode opCode, Hashtable result, CallbackEntry cbEntry,
            string taskId, string providerName, OperationContext operationContext)
        {
            DequeueWriteBehindTask(new string[] {taskId}, providerName, operationContext);

            if (cbEntry != null && cbEntry.WriteBehindOperationCompletedCallback != null)
            {
                RaiseWriteBehindTaskCompleted(opCode, result, cbEntry, operationContext);
            }
        }

        public override void NotifyWriteBehindTaskStatus(Hashtable opResult, string[] taskIds, string provider,
            OperationContext context)
        {
            DequeueWriteBehindTask(taskIds, provider, context);
            CallbackEntry cbEntry = null;
            Hashtable status = null;
            if (opResult != null && opResult.Count > 0)
            {
                IDictionaryEnumerator result = opResult.GetEnumerator();
                while (result.MoveNext())
                {
                    DSWriteBehindOperation dsOperation = result.Value as DSWriteBehindOperation;
                    if (dsOperation == null) continue;
                    cbEntry = dsOperation.Entry.Value as CallbackEntry;
                    if (cbEntry != null && cbEntry.WriteBehindOperationCompletedCallback != null)
                    {
                        status = new Hashtable();
                        if (dsOperation.Exception != null)
                            status.Add(dsOperation.Key, dsOperation.Exception);
                        else
                            status.Add(dsOperation.Key, dsOperation.DSOpState);
                        try
                        {
                            RaiseWriteBehindTaskCompleted(dsOperation.OperationCode, status, cbEntry, context);
                        }
                        catch
                        {
                        }
                    }
                }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operand"></param>
        public void handleWriteThruTaskCompleted(object operand)
        {
            object[] data = operand as object[];
            string providerName = null;
            string[] taskIds = data[0] as string[];
            if (data.Length > 1)
            {
                providerName = data[1] as string;
            }
            if (taskIds != null)
                _context.DsMgr.DequeueWriteBehindTask(taskIds, providerName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operand"></param>
        public void handleNotifyWriteBehindOperationComplete(object operand)
        {
            object[] data = operand as object[];
            OperationContext operationContext = null;

            if (data.Length > 3)
                operationContext = data[3] as OperationContext;

            base.NotifyWriteBehindTaskCompleted((OpCode) data[0], data[1] as Hashtable, data[2] as CallbackEntry,
                operationContext);
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

        protected void SignalTaskState(ArrayList destinations, string taskId, string providerName, OpCode opCode,
            WriteBehindAsyncProcessor.TaskState state, OperationContext operationContext)
        {
            string destinS = string.Empty;
            for (int i = 0; i < destinations.Count; i++)
            {
                destinS += "[" + ((Address) destinations[i]).ToString() + "]";
            }
            DoWrite("ClusterCacheBase.SignalTaskState",
                "taskId=" + taskId + ", state=" + state.ToString() + ", destinations=" + destinS, operationContext);

            object[] operand = new object[] {taskId, state, providerName, opCode};
            ArrayList copyDest = destinations.Clone() as ArrayList;

            if (copyDest.Contains(Cluster.LocalAddress))
            {
                copyDest.Remove(Cluster.LocalAddress);
                handleSignalTaskState(operand);
            }

            if (copyDest.Count > 0)
            {
                Function func = new Function((int) OpCodes.SignalWBTState,
                    new object[] {taskId, state, providerName, opCode}, true);
                Cluster.SendNoReplyMulticastMessage(copyDest, func, false);
            }
        }

        protected void SignalBulkTaskState(ArrayList destinations, string taskId, string providerName, Hashtable table,
            OpCode opCode, WriteBehindAsyncProcessor.TaskState state)
        {
            object[] operand = new object[] {taskId, state, table, providerName, opCode};
            ArrayList copyDest = destinations.Clone() as ArrayList;

            if (copyDest.Contains(Cluster.LocalAddress))
            {
                copyDest.Remove(Cluster.LocalAddress);
                handleSignalTaskState(operand);
            }

            if (copyDest.Count > 0)
            {
                Function func = new Function((int) OpCodes.SignalWBTState, operand, true);
                Cluster.SendNoReplyMulticastMessage(copyDest, func, false);
            }
        }

        protected void handleSignalTaskState(object operand)
        {
            if (_context.DsMgr != null)
            {
                object[] data = operand as object[];
                string taskId = null;
                string providerName = null;
                OpCode code;
                WriteBehindAsyncProcessor.TaskState state;
                Hashtable table = null;

                if (data.Length == 4)
                {
                    taskId = (string) data[0];
                    state = (WriteBehindAsyncProcessor.TaskState) data[1];
                    providerName = (string) data[2];
                    code = (OpCode) data[3];
                    _context.DsMgr.SetState(taskId, providerName, code, state);
                }
                else if (data.Length == 5)
                {
                    taskId = (string) data[0];
                    state = (WriteBehindAsyncProcessor.TaskState) data[1];
                    table = (Hashtable) data[2];
                    providerName = (string) data[3];
                    code = (OpCode) data[4];
                    _context.DsMgr.SetState(taskId, providerName, code, state, table);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="key"></param>
        /// <param name="cacheEntry"></param>
        /// <param name="taskId"></param>
        /// <param name="operationCode"></param>
        protected void AddWriteBehindTask(Address source, string key, CacheEntry cacheEntry, string taskId,
            OpCode operationCode, OperationContext operationContext)
        {
            AddWriteBehindTask(source, key, cacheEntry, taskId, operationCode, WriteBehindAsyncProcessor.TaskState.Waite,
                operationContext);
        }

        protected void AddWriteBehindTask(Address source, string key, CacheEntry cacheEntry, string taskId,
            OpCode operationCode, WriteBehindAsyncProcessor.TaskState state, OperationContext operationContext)
        {
            this.CheckDataSourceAvailabilityAndOptions(DataSourceUpdateOptions.WriteBehind);

            string coord = source != null ? source.ToString() : null;

            DoWrite("ClusterCacheBase.AddWriteBehindTask", "taskId=" + taskId + ", source=" + coord, operationContext);
            _context.DsMgr.WriteBehind(_context.CacheImpl, key, cacheEntry, coord, taskId, cacheEntry.ProviderName,
                operationCode, state);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="table"></param>
        /// <param name="cbEntry"></param>
        /// <param name="taskId"></param>
        /// <param name="operationCode"></param>
        protected void AddWriteBehindTask(Address source, Hashtable table, CallbackEntry cbEntry, string taskId,
            OpCode operationCode)
        {
            AddWriteBehindTask(source, table, cbEntry, taskId, operationCode, WriteBehindAsyncProcessor.TaskState.Waite);
        }

        protected void AddWriteBehindTask(Address source, Hashtable table, CallbackEntry cbEntry, string taskId,
            OpCode operationCode, WriteBehindAsyncProcessor.TaskState state)
        {
            this.CheckDataSourceAvailabilityAndOptions(DataSourceUpdateOptions.WriteBehind);

            string[] keys = new string[table.Count];

            CacheEntry[] entries = null;
            string providerName = null;

            int i = 0;
            switch (operationCode)
            {
                case OpCode.Add:
                case OpCode.Update:
 
                    entries = new CacheEntry[table.Count];
                    foreach (DictionaryEntry de in table)
                    {
                        keys[i] = de.Key as string;
                        object entry = ((CacheEntry) de.Value).Value;
                        providerName = ((CacheEntry) de.Value).ProviderName;
                        entries[i] = (CacheEntry) de.Value;
                        if (entry is CallbackEntry)
                            entry = ((CallbackEntry) entry).Value;

                       
                        CallbackEntry callback = ((CacheEntry) table[keys[i]]).Value as CallbackEntry;
                        if (callback != null)
                        {
                            entries[i] = new CacheEntry(callback, null, null);
                        }
                        i++;
                        

                    } 
                    break;
                case OpCode.Remove:
                    entries = new CacheEntry[table.Count];
                    foreach (DictionaryEntry de in table)
                    {
                        if (de.Value is CacheEntry)
                        {
                            keys[i] = de.Key as string;
                            entries[i] = de.Value as CacheEntry;
                            providerName = entries[i].ProviderName;

                            i++;
                        }
                    }

                    if (entries.Length > 0 && cbEntry != null)
                    {
                        for (int j = 0; j < entries.Length; j++)
                        {
                            if (entries[j].Value is CallbackEntry)
                            {
                                ((CallbackEntry) entries[j].Value).WriteBehindOperationCompletedCallback =
                                    cbEntry.WriteBehindOperationCompletedCallback;
                            }
                            else
                            {
                                cbEntry.Value = entries[j].Value;
                                entries[j].Value = cbEntry;
                            }
                        }
                    }
                    break;
            }

            string coord = source != null ? source.ToString() : null;
            _context.DsMgr.WriteBehind(_context.CacheImpl, keys, entries, source.ToString(), taskId, providerName,
                operationCode, state);
        }

        private void CheckDataSourceAvailabilityAndOptions(DataSourceUpdateOptions updateOpts)
        {
            if (updateOpts != DataSourceUpdateOptions.None)
            {
                if (_context.DsMgr != null
                    && ((_context.DsMgr.IsWriteThruEnabled && updateOpts == DataSourceUpdateOptions.WriteThru)
                        ||
                        ( updateOpts == DataSourceUpdateOptions.WriteBehind)))
                    return;

                throw new OperationFailedException("Backing source not available. Verify backing source settings");
            }
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
                        ((ClusterNodeInformation) _nodeInformationTable[address]).ConnectedClients = clientsConnected;
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
                Function func = new Function((int) OpCodes.GetClientRequestStatus,
                    new object[] {clientId, requestId, commandId});
                object rsp = Cluster.SendMessage(intendedServer, func, GroupRequest.GET_FIRST);

                if (rsp != null)
                {
                    requestStatus = (RequestStatus) rsp;
                }

                return requestStatus;
            }
            catch (Exception e)
            {
                return null;
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


        #region MapReduce Methods

        public override void SubmitMapReduceTask(Runtime.MapReduce.MapReduceTask task, string taskId,
            TaskCallbackInfo callbackInfo, Filter filter, OperationContext operationContext)
        {
            try
            {
                if (IsInStateTransfer())
                    throw new GeneralFailureException("Cluster is in State Transfer.");

                // Getting the Sequence id of the task
                MapReduceOperation operation = new MapReduceOperation();
                operation.OpCode = MapReduceOpCodes.GetTaskSequence;

                Function sequenceFunc = new Function((int) OpCodes.MapReduceOperation, operation, false);
                Object result = this.Cluster.SendMessage(this.Cluster.Coordinator, sequenceFunc, GroupRequest.GET_FIRST);
                long sequenceID;
                if (result == null)
                {
                    throw new GeneralFailureException("Task Submission Failed");
                }
                sequenceID = (long) result;

                // Now submit the MapReduce Task...
                MapReduceOperation op = new MapReduceOperation();
                op.Data = task;
                op.Filter = filter;
                op.CallbackInfo = callbackInfo;
                op.OpCode = MapReduceOpCodes.SubmitMapReduceTask;
                op.TaskID = taskId;

                Function func = new Function((int) OpCodes.MapReduceOperation, op, false);
                RspList results = Cluster.Multicast(this.ActiveServers, func, GroupRequest.GET_ALL, false);

                ClusterHelper.ValidateResponses(results, typeof (TaskExecutionStatus), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (TaskExecutionStatus));

                // verify the response
                IEnumerator ia = rspList.GetEnumerator();
                while (ia.MoveNext())
                {
                    Rsp rsp = (Rsp) ia.Current;
                    if ((TaskExecutionStatus) rsp.Value == TaskExecutionStatus.Failure)
                    {
                        MapReduceOperation stopOperation = new MapReduceOperation();
                        stopOperation.OpCode = MapReduceOpCodes.RemoveFromSubmittedList;
                        stopOperation.TaskID = taskId;

                        Function stopFunction = new Function((int) OpCodes.MapReduceOperation, stopOperation, false);
                        this.Cluster.Multicast(this.Cluster.ActiveServers, stopFunction, GroupRequest.GET_ALL, false);
                        throw new GeneralFailureException("Task failed during submission on Node : " +
                                                          rsp.Sender.ToString());
                    }
                }

                //task submitted successfully on all nodes, so now start the task
                MapReduceOperation startingOperation = new MapReduceOperation();
                startingOperation.OpCode = MapReduceOpCodes.StartTask;
                startingOperation.TaskID = taskId;
                startingOperation.SequenceID = sequenceID;

                Function taskStartingFunction = new Function((int) OpCodes.MapReduceOperation, startingOperation, false);
                RspList runTaskCommandRsps = this.Cluster.Multicast(this.ActiveServers, taskStartingFunction,
                    GroupRequest.GET_ALL, false);

                ClusterHelper.ValidateResponses(runTaskCommandRsps, typeof (TaskExecutionStatus), Name);
                IList runTaskRspList = ClusterHelper.GetAllNonNullRsp(runTaskCommandRsps, typeof (TaskExecutionStatus));

                ia = null;
                ia = runTaskRspList.GetEnumerator();
                while (ia.MoveNext())
                {
                    Rsp rsp = (Rsp) ia.Current;
                    if ((TaskExecutionStatus) rsp.Value == TaskExecutionStatus.Failure)
                    {
                        MapReduceOperation stopRunningOperation = new MapReduceOperation();
                        stopRunningOperation.OpCode = MapReduceOpCodes.RemoveFromRunningList;
                        stopRunningOperation.TaskID = taskId;

                        Function stopRunningFunction = new Function((int) OpCodes.MapReduceOperation,
                            stopRunningOperation, false);
                        this.Cluster.Multicast(this.ActiveServers, stopRunningFunction, GroupRequest.GET_ALL, false);
                        throw new GeneralFailureException("Task failed while starting on Node : " +
                                                          rsp.Sender.ToString());
                    }
                }

            }
            catch (Exception ex)
            {
                throw new GeneralFailureException(ex.Message, ex);
            }
        }

        protected Object HandleMapReduceOperation(Object argument)
        {
            return Local_MapReduceOperation((MapReduceOperation) argument);
        }

        Object Local_MapReduceOperation(MapReduceOperation operation)
        {
            return InternalCache.TaskOperationReceived(operation);
        }

        public override void SendMapReduceOperation(ArrayList dests, MapReduceOperation operation)
        {
            try
            {
                operation.Source = this.Cluster.LocalAddress;
                Function func = new Function((int) OpCodes.MapReduceOperation, operation, true);
                this.Cluster.Multicast(dests, func, GetAllResponses);
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        public override void SendMapReduceOperation(Address target, MapReduceOperation operation)
        {
            try
            {
                operation.Source = this.Cluster.LocalAddress;
                Function func = new Function((int) OpCodes.MapReduceOperation, operation, false);
                this.Cluster.SendMessage(target, func, GroupRequest.GET_NONE);
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        /// <summary>
        /// Registers the item update/remove or both callbacks with the specified
        /// key. Keys should exist before the registration.
        /// </summary>
        /// <param name="taskID"></param>
        /// <param name="callbackInfo"></param>
        /// <param name="operationContext"></param>
        /// 

        public override void RegisterTaskNotification(String taskID, TaskCallbackInfo callbackInfo,
            OperationContext operationContext)
        {
            try
            {
                MapReduceOperation op = new MapReduceOperation();
                op.CallbackInfo = callbackInfo;
                op.OpCode = MapReduceOpCodes.RegisterTaskNotification;
                op.TaskID = taskID;

                Function func = new Function((int) OpCodes.MapReduceOperation, op, false);
                RspList results = this.Cluster.Multicast(this.ActiveServers, func, GroupRequest.GET_ALL, false);

                ClusterHelper.ValidateResponses(results, typeof (Boolean), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (Boolean));

                IEnumerator ia = rspList.GetEnumerator();
                while (ia.MoveNext())
                {
                    Rsp rsp = (Rsp) ia.Current;
                    if (rsp.Value != null)
                    {
                        //check if all responses are same
                    }
                }

            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("ClusterCacheBase.RegisterTaskNotification() ", inner.Message);
                throw new OperationFailedException("RegisterTaskNotification failed. Error : " + inner.Message, inner);
            }
        }

        /// <summary>
        /// Unregisters the item update/remove or both call backs with the specified key.
        /// </summary>
        /// <param name="taskID"></param>
        /// <param name="callbackInfo"></param>
        /// <param name="operationContext"></param>
        public override void UnregisterTaskNotification(String taskID, TaskCallbackInfo callbackInfo,
            OperationContext operationContext)
        {
            try
            {
                MapReduceOperation op = new MapReduceOperation();
                op.Data = callbackInfo;
                op.OpCode = MapReduceOpCodes.UnregisterTaskNotification;
                op.TaskID = taskID;

                Function func = new Function((int) OpCodes.MapReduceOperation, op, false);
                RspList results = this.Cluster.Multicast(this.ActiveServers, func, GroupRequest.GET_ALL, false);

                ClusterHelper.ValidateResponses(results, typeof (Boolean), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (Boolean));

                IEnumerator ia = rspList.GetEnumerator();
                while (ia.MoveNext())
                {
                    Rsp rsp = (Rsp) ia.Current;
                    if (rsp.Value != null)
                    {
                        //check if all responses are same
                    }
                }

            }
            catch (Exception inner)
            {
                _context.NCacheLog.Error("ClusterCacheBase.UnregisterTaskNotification() ", inner.Message);
                throw new OperationFailedException("UnregisterTaskNotification failed. Error : " + inner.Message, inner);
            }
        }

        public void RaiseTaskCalbackNotifier(string taskID, IList taskCallbackListener, EventContext eventContext)
        {
            if (this.Cluster.IsCoordinator)
            {
                ArrayList destinations = null;
                ArrayList nodes = null;
                Hashtable intendedNotifiers = new Hashtable();
                if (taskCallbackListener != null && taskCallbackListener.Count > 0)
                {
                    if (_stats.Nodes != null)
                    {
                        nodes = _stats.Nodes;
                        destinations = new ArrayList();
                        for (IEnumerator it = taskCallbackListener.GetEnumerator(); it.MoveNext();)
                        {
                            TaskCallbackInfo cbInfo = (TaskCallbackInfo) it.Current;
                            int index = nodes.IndexOf(new NodeInfo(this.Cluster.LocalAddress));
                            if (index != -1 && ((NodeInfo) nodes[index]).ConnectedClients.Contains(cbInfo.Client))
                            {
                                if (!destinations.Contains(Cluster.LocalAddress))
                                {
                                    destinations.Add(this.Cluster.LocalAddress);
                                }
                                intendedNotifiers.Add(cbInfo, this.Cluster.LocalAddress);
                                continue;
                            }
                            else
                            {
                                for (IEnumerator ite = nodes.GetEnumerator(); ite.MoveNext();)
                                {
                                    NodeInfo nInfo = (NodeInfo) ite.Current;
                                    if (nInfo.ConnectedClients != null && nInfo.ConnectedClients.Contains(cbInfo.Client))
                                    {
                                        if (!destinations.Contains(nInfo.Address))
                                        {
                                            destinations.Add(nInfo.Address);
                                            intendedNotifiers.Add(cbInfo, nInfo.Address);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (destinations != null && destinations.Count > 0)
                {
                    Object[] packed = new Object[]
                    {
                        taskID,
                        taskCallbackListener,
                        intendedNotifiers
                    };

                    // If everything went ok!, initiate local and cluster-wide notifications.
                    bool sendLocal = false;
                    Object[] objs = (Object[]) ((packed is Object[]) ? packed : null);
                    IList callbackListeners = (IList) ((objs[1] is IList) ? objs[1] : null);

                    if (destinations.Contains(this.Cluster.LocalAddress))
                    {
                        destinations.Remove(this.Cluster.LocalAddress);
                        sendLocal = true;
                    }

                    if (ValidMembers.Count > 1)
                    {
                        _cluster.SendNoReplyMcastMessageAsync(destinations,
                            new Function((int) OpCodes.NotifyTaskCallback, new Object[]
                            {
                                objs[0],
                                callbackListeners,
                                objs[2], eventContext
                            }));
                    }

                    if (sendLocal)
                    {
                        HandleNotifyTaskCallback(new Object[]
                        {
                            objs[0],
                            callbackListeners,
                            objs[2], eventContext
                        });
                    }

                }
            }
        }

        /// <summary>
        /// Hanlder for clustered item update callback notification.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private Object HandleNotifyTaskCallback(Object info)
        {
            Object[] objs = (Object[]) info;
            EventContext eventContext = null;
            IList callbackListeners = (IList) ((objs[1] is IList) ? objs[1] : null);
            Hashtable intendedNotifiers = (Hashtable) ((objs[2] is Hashtable) ? objs[2] : null);
            if (objs.Length > 3)
            {
                eventContext = (EventContext) objs[3];
            }

            IEnumerator ide = intendedNotifiers.GetEnumerator();
            DictionaryEntry KeyValue;
            while (ide.MoveNext())
            {
                KeyValue = (DictionaryEntry) ide.Current;
                Object Key = KeyValue.Key;
                Object Value = KeyValue.Value;
                CallbackInfo cbinfo = (CallbackInfo) ((Key is CallbackInfo) ? Key : null);
                Address node = (Address) ((Value is Address) ? Value : null);

                if (node != null && !node.Equals(this.Cluster.LocalAddress))
                {
                    callbackListeners.Remove(cbinfo);
                }
            }
            NotifyTaskCallback((string) objs[0], (IList) objs[1], false, null, eventContext);
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="cancelAll"></param>

        public override void CancelMapReduceTask(String taskId, bool cancelAll)
        {
            try
            {
                MapReduceOperation operation = new MapReduceOperation();

                // no use of cancelAll.
                operation.OpCode = MapReduceOpCodes.CancelTask;
                if (taskId == null || string.IsNullOrEmpty(taskId))
                {
                    throw new ArgumentNullException("taskId");
                }
                operation.TaskID = taskId;

                Function func = new Function((int) OpCodes.MapReduceOperation, operation, false);
                RspList results = this.Cluster.Multicast(this.ActiveServers, func, GroupRequest.GET_ALL, false);

                ClusterHelper.ValidateResponses(results, typeof (Boolean), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (Boolean));

                IEnumerator ia = rspList.GetEnumerator();
                while (ia.MoveNext())
                {
                    Rsp rsp = (Rsp) ia.Current;
                    if (rsp.Value != null)
                    {
                        //check if all responses are same
                    }
                }
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message, ex);
            }

        }

        /// <summary>
        /// Returns the list of taskIDs of the running tasks.
        /// </summary>
        /// <returns></returns>
        public override ArrayList GetRunningTasks()
        {
            ArrayList runningTasks = new ArrayList();
            try
            {
                MapReduceOperation operation = new MapReduceOperation();
                operation.OpCode = MapReduceOpCodes.GetRunningTasks;

                Function func = new Function((int) OpCodes.MapReduceOperation, operation, false);
                Object result = this.Cluster.SendMessage(this.Cluster.Coordinator, func, GroupRequest.GET_FIRST);
                runningTasks = (ArrayList) result;

            }
            catch (Exception ex)
            {
                throw new GeneralFailureException(ex.Message, ex);
            }
            return runningTasks;
        }

        /// <summary>
        /// Gets the Progress of the task with specified taskId.
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        /// 

        public override Runtime.MapReduce.TaskStatus GetTaskStatus(String taskId)
        {
            Runtime.MapReduce.TaskStatus status =
                new Runtime.MapReduce.TaskStatus(Runtime.MapReduce.TaskStatus.Status.Completed);
            try
            {
                MapReduceOperation operation = new MapReduceOperation();
                operation.OpCode = MapReduceOpCodes.GetTaskStatus;
                operation.TaskID = taskId;

                Function func = new Function((int) OpCodes.MapReduceOperation, operation, false);
                Object result = this.Cluster.SendMessage(this.Cluster.Coordinator, func, GroupRequest.GET_FIRST);

                if (result == null)
                {
                    throw new Exception("Task with specified key does not exist.");
                }

                status = (Runtime.MapReduce.TaskStatus) result;

            }
            catch (Exception ex)
            {
                throw new GeneralFailureException(ex.Message, ex);
            }
            return status;
        }


        public override List<Common.MapReduce.TaskEnumeratorResult> GetTaskEnumerator(
            Common.MapReduce.TaskEnumeratorPointer pointer, OperationContext operationContext)
        {
            List<Common.MapReduce.TaskEnumeratorResult> resultSets = new List<Common.MapReduce.TaskEnumeratorResult>();

            MapReduceOperation operation = new MapReduceOperation();
            operation.Data = pointer;
            operation.OpCode = MapReduceOpCodes.GetTaskEnumerator;
            operation.OperationContext = operationContext;

            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) // for dedicated request
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();
                resultSets = Clustered_GetTaskEnumerator(servers, operation);
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
                resultSets.Add((Common.MapReduce.TaskEnumeratorResult) this.Local_MapReduceOperation(operation));
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }
            return resultSets;
        }

        private List<Common.MapReduce.TaskEnumeratorResult> Clustered_GetTaskEnumerator(ArrayList destinations,
            MapReduceOperation operation)
        {
            List<Common.MapReduce.TaskEnumeratorResult> resultSet = new List<Common.MapReduce.TaskEnumeratorResult>();

            try
            {

                Function func = new Function((byte) OpCodes.MapReduceOperation, operation, false);
                RspList results = Cluster.Multicast(destinations, func, GroupRequest.GET_ALL, false);

                if (results == null)
                    return null;

                ClusterHelper.ValidateResponses(results, typeof (Common.MapReduce.TaskEnumeratorResult), this.Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (Common.MapReduce.TaskEnumeratorResult));
                if (rspList.Count <= 0)
                {
                    return null;
                }
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp) im.Current;
                        Common.MapReduce.TaskEnumeratorResult rResultSet =
                            (Common.MapReduce.TaskEnumeratorResult) rsp.Value;
                        resultSet.Add(rResultSet);
                    }
                }

                return resultSet;
            }
            catch (CacheException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                ///// Pointer Not Serializable.
                throw new GeneralFailureException(e.Message, e);
            }
        }


        public override Common.MapReduce.TaskEnumeratorResult GetNextRecord(
            Common.MapReduce.TaskEnumeratorPointer pointer, OperationContext operationContext)
        {
            _statusLatch.WaitForAny((byte) (NodeStatus.Initializing | NodeStatus.Running));
            if (_internalCache == null)
            {
                throw new OperationFailedException();
            }

            Common.MapReduce.TaskEnumeratorResult reader = null;

            Address intenededRecepient = pointer.ClusterAddress;
            ArrayList servers = new ArrayList(this.ActiveServers.Clone() as ArrayList);

            MapReduceOperation operation = new MapReduceOperation();
            operation.Data = pointer;
            operation.OpCode = MapReduceOpCodes.GetNextRecord;
            operation.OperationContext = operationContext;

            Address targetNode = null;

            if (intenededRecepient != null)
            {
                for (int i = 0; i < servers.Count; i++)
                {
                    Address server = (Address) servers[i];
                    if (server.IpAddress.ToString() == null
                        ? intenededRecepient == null
                        : server.IpAddress.ToString().Equals(intenededRecepient.IpAddress.ToString()))
                    {
                        targetNode = server;
                        break;
                    }
                }
                if (targetNode != null)
                {
                    if (targetNode.Equals(Cluster.LocalAddress))
                    {
                        reader = (Common.MapReduce.TaskEnumeratorResult) Local_MapReduceOperation(operation);
                    }
                    else
                    {
                        reader = Clustered_GetTaskNextRecord(targetNode, operation);
                    }
                }
                else
                {
                    throw new InvalidTaskEnumeratorException("Server " + intenededRecepient + " is not part of cluster.");
                }
            }
            return reader;
        }

        private Common.MapReduce.TaskEnumeratorResult Clustered_GetTaskNextRecord(Address targetNode,
            MapReduceOperation operation)
        {
            try
            {
                Function func = new Function((byte) OpCodes.MapReduceOperation, operation, false);
                Object result = Cluster.SendMessage(targetNode, func, GroupRequest.GET_FIRST, Cluster.Timeout);

                return (Common.MapReduce.TaskEnumeratorResult) result;
            }
            catch (NGroups.SuspectedException e)
            {
                throw new InvalidTaskEnumeratorException("Task Enumerator is Invalid");
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }
        }

        #endregion



        private void HandleDeadClients(object p)
        {
            InternalCache.DeclareDeadClients((string) p, null);
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
                    Function func = new Function((byte) OpCodes.DeadClients, deadClient);
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
            if (localNodeInfo.StatsReplicationCounter < updatedNodeInfo.StatsReplicationCounter)
                return true;
            else
            {
                if (localNodeInfo.NodeGuid != updatedNodeInfo.NodeGuid)
                    return true;
            }
            return false;
        }

        #region             --- Continuous Query State Transfer ---

        public ContinuousQueryStateInfo GetContinuousQueryStateInfo(Address source)
        {
            return Clustered_GetContinuousQueryStateInfo(source);
        }

        private ContinuousQueryStateInfo Clustered_GetContinuousQueryStateInfo(Address source)
        {
            ContinuousQueryStateInfo retVal = null;

            try
            {
                Function func = new Function((int) OpCodes.GetContinuousQueryStateInfo, null);
                ArrayList address = new ArrayList();
                address.Add(source);
                RspList results = Cluster.Multicast(address, func, GroupRequest.GET_FIRST, false, Cluster.Timeout*10);
                ClusterHelper.ValidateResponses(results, typeof (ContinuousQueryStateInfo), Name);
                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof (ContinuousQueryStateInfo));

                if (rspList.Count <= 0)
                    return null;
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    if (im.MoveNext())
                    {
                        Rsp rsp = (Rsp) im.Current;
                        retVal = (ContinuousQueryStateInfo) rsp.Value;
                    }
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

        private ContinuousQueryStateInfo handleGetContinuousQueryStateInfo()
        {
            ContinuousQueryStateInfo stateInfo = _internalCache.GetContinuousQueryStateInfo();

            if (stateInfo != null && CQManager != null)
                stateInfo.CQManagerState = CQManager.GetState();

            return stateInfo;
        }

        #endregion

        #region--------------------------------Cache Data Reader----------------------------------------------

        public override ClusteredList<ReaderResultSet> ExecuteReader(string query, IDictionary values, bool getData,
            int chunkSize, bool isInproc, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            
            try
            {                
                ClusteredList<ReaderResultSet> recordSets = new ClusteredList<ReaderResultSet>();
                ArrayList dests = new ArrayList();

                long clientLastViewId = GetClientLastViewId(operationContext);
                if (clientLastViewId == forcedViewId) // for dedicated request
                {
                    ArrayList servers = GetServerParticipatingInStateTransfer();
                    operationContext.Add(OperationContextFieldName.IsClusteredOperation, false);
                    try
                    {
                        recordSets = Clustered_ExecuteReader(servers, query, values, getData, chunkSize, operationContext, IsRetryOnSuspected);
                    }
                    catch (Alachisoft.NGroups.SuspectedException ex)
                    {
                        if (!IsRetryOnSuspected) throw;

                        //Sleep is used to be sure that new view applied and node is marked in state transfer...
                        Thread.Sleep(_onSuspectedWait);
                        servers = GetServerParticipatingInStateTransfer();

                        recordSets = Clustered_ExecuteReader(servers, query, values, getData, chunkSize, operationContext);


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
                    recordSets.Add(InternalCache.Local_ExecuteReader(query, values, getData, chunkSize, isInproc, operationContext));
                    if (IsInStateTransfer())
                    {
                        throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                    }
                }
                return recordSets;
            }
            catch (Exception e) 
            {
                throw;
            }           
        }

        public override List<ReaderResultSet> ExecuteReaderCQ(string query, IDictionary values, bool getData,
            int chunkSize, string clientUniqueId, string clientId,
            bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext,
            QueryDataFilters datafilters, bool isInproc)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            List<ReaderResultSet> recordSets = new List<ReaderResultSet>();
            ArrayList dests = new ArrayList();

            long clientLastViewId = GetClientLastViewId(operationContext);
            if (clientLastViewId == forcedViewId) // for dedicated request
            {
                ArrayList servers = GetServerParticipatingInStateTransfer();
                operationContext.Add(OperationContextFieldName.IsClusteredOperation, false);
                try
                {
                    recordSets=  Clustered_ExecuteReaderCQ(servers, query, values, getData, chunkSize, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters, isInproc, IsRetryOnSuspected);
                }
                catch (Exception)
                {
                      if (!IsRetryOnSuspected) throw;

                        //Sleep is used to be sure that new view applied and node is marked in state transfer...
                        Thread.Sleep(_onSuspectedWait);
                        servers = GetServerParticipatingInStateTransfer();
                    recordSets = Clustered_ExecuteReaderCQ(servers, query, values, getData, chunkSize, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters, isInproc, IsRetryOnSuspected);
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
                ContinuousQuery continuousQuery = CQManager.GetCQ(query, values);
                recordSets.Add(InternalCache.Local_ExecuteReaderCQ(continuousQuery, getData, chunkSize, operationContext));
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
               
            }
            return recordSets;
        }

        /// <summary>
        /// Retrieve the reader result set from the cache based on the specified query.
        /// </summary>
        protected ClusteredList<ReaderResultSet> Clustered_ExecuteReader(ArrayList dests, string queryText,
            IDictionary values, bool getData, int chunkSize, OperationContext operationContext,Boolean throwSuspected=false)
        {
            ClusteredList<ReaderResultSet> resultSet = new ClusteredList<ReaderResultSet>();

            try
            {
                Function func = new Function((int) OpCodes.ExecuteReader,
                    new object[] {queryText, values, getData, chunkSize, operationContext}, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout*10);

                if (results == null)
                    return resultSet;
                
                if (throwSuspected) ClusterHelper.VerifySuspectedResponses(results);

                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(ReaderResultSet));
                try
                {
                    
                    ClusterHelper.ValidateResponses(results, typeof(ReaderResultSet), Name);
                }
                catch(Exception e)
                {                  
                    if(rspList != null && rspList.Count >0)
                    {
                        IEnumerator im = rspList.GetEnumerator();
                        while (im.MoveNext())
                        {
                            Rsp rsp = (Rsp)im.Current;
                            ReaderResultSet rResultSet = (ReaderResultSet)rsp.Value;
                     
                            try
                            {
                                Clustered_DisposeReader(rsp.Sender as Address, rResultSet.ReaderID, operationContext);
                            }
                            catch(Exception ex)
                            {
                            }
                        }
                    }
                    if ( e is InvalidReaderException || e is Parser.ParserException || e is Parser.TypeIndexNotDefined || e is Parser.AttributeIndexNotDefined) throw;

                    throw new InvalidReaderException("Reader state has been lost.", e);
                }
               

                if (rspList.Count <= 0)
                {
                    return resultSet;
                }
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp) im.Current;
                        ReaderResultSet rResultSet = (ReaderResultSet) rsp.Value;

                        if (rResultSet != null && rResultSet.RecordSet!=null && rResultSet.RecordSet.Rows!=null)
                        {
                            if(operationContext.Contains(OperationContextFieldName.IsClusteredOperation))
                                operationContext.RemoveValueByField(OperationContextFieldName.IsClusteredOperation);

                            var rowCollection = rResultSet.RecordSet.Rows;
                            var rows = rowCollection.RowsVector;
                            IEnumerator rowEnum = rows.GetEnumerator();
                            IList<Int32> toBeRemoved = new List<Int32>();

                            while (rowEnum.MoveNext())
                            {
                                var pair = (DictionaryEntry)rowEnum.Current;
                                RecordRow row = pair.Value as RecordRow;

                                if (row != null && row.IsSurrogate)
                                {
                                    var entry = _context.CacheImpl.Get(row.GetColumnValue(QueryKeyWords.KeyColumn), operationContext);
                                    row.IsSurrogate = entry != null && entry.IsSurrogate;

                                    if (entry != null)
                                    {
                                        CompressedValueEntry cmpEntry = new CompressedValueEntry();
                                        cmpEntry.Value = entry.Value;
                                        if (cmpEntry.Value is CallbackEntry)
                                            cmpEntry.Value = ((CallbackEntry)cmpEntry.Value).Value;

                                        cmpEntry.Flag = ((CacheEntry)entry).Flag;                                       
                                    }
                                    else 
                                    {
                                        toBeRemoved.Add((Int32)pair.Key);
                                    }
                                }
                            }

                            if (toBeRemoved.Count > 0)
                            { 
                                IEnumerator ienum = toBeRemoved.GetEnumerator();
                                while(ienum.MoveNext())
                                {
                                    rowCollection.RemoveRow((Int32)ienum.Current);
                                }
                                toBeRemoved.Clear();
                            }

                        }

                        resultSet.Add(rResultSet);
                    }
                }

                return resultSet;
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Alachisoft.NGroups.SuspectedException e)
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

        protected List<ReaderResultSet> Clustered_ExecuteReaderCQ(ArrayList dests, string query, IDictionary values,
            bool getData, int chunkSize, string clientUniqueId, string clientId,
            bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext,
            QueryDataFilters datafilters, bool isInproc, Boolean throwSuspected = false)
        {
            List<ReaderResultSet> resultSet = new List<ReaderResultSet>();

            try
            {
                Function func = new Function((int) OpCodes.ExecuteReaderCQ,
                    new object[]
                    {
                        query, values, getData, chunkSize, clientUniqueId, clientId, notifyAdd, notifyUpdate,
                        notifyRemove,
                        operationContext, datafilters, isInproc
                    }, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout*10);

                if (results == null)
                    return resultSet;
                if (throwSuspected) ClusterHelper.VerifySuspectedResponses(results);

                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(ReaderResultSet));
                try
                {
                    ClusterHelper.ValidateResponses(results, typeof(ReaderResultSet), Name);
                   
                }
                catch (Exception e)
                {
                    if (rspList != null && rspList.Count > 0)
                    {
                        IEnumerator im = rspList.GetEnumerator();
                        while (im.MoveNext())
                        {
                            Rsp rsp = (Rsp)im.Current;
                            ReaderResultSet rResultSet = (ReaderResultSet)rsp.Value;

                            try
                            {
                                Clustered_DisposeReader(rsp.Sender as Address, rResultSet.ReaderID, operationContext);
                            }
                            catch (Exception ex)
                            {
                                Context.NCacheLog.Error("ClusteredCacheBase.Clustered_ExecuteReader.Clustered_DisposeReader()", ex.ToString());
                            }
                        }
                    }
                    if (e is InvalidReaderException || e is ParserException || e is TypeIndexNotDefined || e is AttributeIndexNotDefined) throw;
                    throw new InvalidReaderException("Reader state has been lost.", e);
                }
               

                if (rspList.Count <= 0)
                {
                    return resultSet;
                }
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp)im.Current;
                        ReaderResultSet rResultSet = (ReaderResultSet)rsp.Value;

                        if (rResultSet != null && rResultSet.RecordSet != null && rResultSet.RecordSet.Rows != null)
                        {
                            if (operationContext.Contains(OperationContextFieldName.IsClusteredOperation))
                                operationContext.RemoveValueByField(OperationContextFieldName.IsClusteredOperation);

                            var rowCollection = rResultSet.RecordSet.Rows;
                            var rows = rowCollection.RowsVector;
                            IEnumerator rowEnum = rows.GetEnumerator();
                            IList<Int32> toBeRemoved = new List<Int32>();

                            while (rowEnum.MoveNext())
                            {
                                var pair = (DictionaryEntry)rowEnum.Current;
                                RecordRow row = pair.Value as RecordRow;

                                if (row != null && row.IsSurrogate)
                                {
                                    var entry = _context.CacheImpl.Get(row.GetColumnValue(QueryKeyWords.KeyColumn), operationContext);
                                    row.IsSurrogate = entry != null && entry.IsSurrogate;

                                    if (entry != null)
                                    {
                                        CompressedValueEntry cmpEntry = new CompressedValueEntry();
                                        cmpEntry.Value = entry.Value;
                                        if (cmpEntry.Value is CallbackEntry)
                                            cmpEntry.Value = ((CallbackEntry)cmpEntry.Value).Value;

                                        cmpEntry.Flag = ((CacheEntry)entry).Flag;
                                        row.SetColumnValue(QueryKeyWords.ValueColumn, cmpEntry);
                                    }
                                    else
                                    {
                                        toBeRemoved.Add((Int32)pair.Key);
                                    }
                                }
                            }

                            if (toBeRemoved.Count > 0)
                            {
                                IEnumerator ienum = toBeRemoved.GetEnumerator();
                                while (ienum.MoveNext())
                                {
                                    rowCollection.RemoveRow((Int32)ienum.Current);
                                }
                                toBeRemoved.Clear();
                            }

                        }

                        resultSet.Add(rResultSet);
                    }
                }

                return resultSet;
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

        public override ReaderResultSet GetReaderChunk(string readerId, int nextIndex, bool isInproc,
            OperationContext operationContext)
        {
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            ReaderResultSet reader = null;

            Address intenededRecepient = GetReaderRecipient(operationContext);
            Array servers = Array.CreateInstance(typeof (Address), Cluster.Servers.Count);
            Cluster.Servers.CopyTo(servers);
            Address targetNode = null;

            if (intenededRecepient!=null)
            {
                for (int i = 0; i < servers.Length; i++)
                {
                    Address server = servers.GetValue(i) as Address;
                    if (server.IpAddress.Equals(intenededRecepient.IpAddress))
                    {
                        if (intenededRecepient.Port > 0 && !server.Port.Equals(intenededRecepient.Port)) continue;

                        targetNode = server;
                        break;
                    }
                }
                if (targetNode != null)
                {
                    if (targetNode.Equals(Cluster.LocalAddress))
                    {
                        reader = InternalCache.GetReaderChunk(readerId, nextIndex, isInproc, operationContext);
                    }
                    else
                    {
                        try
                        {
                            operationContext.Add(OperationContextFieldName.IsClusteredOperation, false);
                            reader = Clustered_GetReaderChunk(targetNode, readerId, nextIndex, operationContext);
                        }
                        catch (InvalidReaderException readerException)
                        {
                           if (! string.IsNullOrEmpty(readerId))
                               InternalCache.DisposeReader(readerId, operationContext);
                           throw readerException;
                        }
                    }
                }
                else
                    throw new InvalidReaderException("Reader state has been lost due to state transfer.");
            }
            return reader;
        }

        private ReaderResultSet Clustered_GetReaderChunk(Address targetNode, string readerId, int nextIndex,
            OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int) OpCodes.GetReaderChunk,
                    new object[] {readerId, nextIndex, operationContext});
                object result = Cluster.SendMessage(targetNode, func, GroupRequest.GET_FIRST, Cluster.Timeout);

                ReaderResultSet readerChunk = result as ReaderResultSet;

                if (readerChunk != null && readerChunk.RecordSet != null && readerChunk.RecordSet.Rows != null)
                {
                    if (operationContext.Contains(OperationContextFieldName.IsClusteredOperation))
                        operationContext.RemoveValueByField(OperationContextFieldName.IsClusteredOperation);

                    var rowCollection = readerChunk.RecordSet.Rows;
                    var rows = rowCollection.RowsVector;
                    IEnumerator rowEnum = rows.GetEnumerator();
                    IList<Int32> toBeRemoved = new List<Int32>();

                    while (rowEnum.MoveNext())
                    {
                        var pair = (DictionaryEntry)rowEnum.Current;
                        RecordRow row = pair.Value as RecordRow;

                        if (row != null && row.IsSurrogate)
                        {
                            var entry = _context.CacheImpl.Get(row.GetColumnValue(QueryKeyWords.KeyColumn), operationContext);
                            row.IsSurrogate = entry != null && entry.IsSurrogate;

                            if (entry != null)
                            {
                                CompressedValueEntry cmpEntry = new CompressedValueEntry();
                                cmpEntry.Value = entry.Value;
                                if (cmpEntry.Value is CallbackEntry)
                                    cmpEntry.Value = ((CallbackEntry)cmpEntry.Value).Value;

                                cmpEntry.Flag = ((CacheEntry)entry).Flag;
                                row.SetColumnValue(QueryKeyWords.ValueColumn, cmpEntry);                              
                            }
                            else
                            {
                                toBeRemoved.Add((Int32)pair.Key);
                            }                       
                        }
                    }

                    if (toBeRemoved.Count > 0)
                    {
                        IEnumerator ienum = toBeRemoved.GetEnumerator();
                        while (ienum.MoveNext())
                        {
                            rowCollection.RemoveRow((Int32)ienum.Current);
                        }
                        toBeRemoved.Clear();
                    }

                }
                return readerChunk;
            }
            catch (Alachisoft.NCache.Runtime.Exceptions.SuspectedException sexp)
            {
                throw new InvalidReaderException("Reader state has been lost due to state transfer.");
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public override void DisposeReader(string readerId, OperationContext operationContext)
        {
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            string intenededRecepient = GetIntendedRecipient(operationContext);
            Array servers = Array.CreateInstance(typeof (Address), Cluster.Servers.Count);
            Cluster.Servers.CopyTo(servers);
            Address targetNode = null;

            if (!string.IsNullOrEmpty(intenededRecepient))
            {
                for (int i = 0; i < servers.Length; i++)
                {
                    Address server = servers.GetValue(i) as Address;
                    if (server.IpAddress.ToString() == intenededRecepient)
                    {
                        targetNode = server;
                        break;
                    }
                }
                if (targetNode != null)
                {
                    if (targetNode.Equals(Cluster.LocalAddress))
                        InternalCache.DisposeReader(readerId, operationContext);
                    else
                        Clustered_DisposeReader(targetNode, readerId, operationContext);
                }
            }
        }

        private void Clustered_DisposeReader(Address targetNode, string readerId, OperationContext operationContext)
        {
            try
            {
                if (targetNode != null && targetNode.Equals(Cluster.LocalAddress))
                {
                    _internalCache.DisposeReader(readerId, operationContext);
                }
                else
                {
                    Function func = new Function((int)OpCodes.DisposeReader, new object[] { readerId, operationContext });
                    Cluster.SendNoReplyMessage(targetNode, func);
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

        private object handleExecuteReader(object arguments)
        {
            Stopwatch watch = new Stopwatch();

            try
            {
                watch.Start();
                if (_internalCache != null)
                {
                    object[] data = (object[])arguments;
                    return _internalCache.Local_ExecuteReader(data[0] as string, data[1] as IDictionary, (bool)data[2],
                        (int)data[3], false, data[4] as OperationContext);
                }
            }
            finally 
            {
                watch.Stop();
                double timeTaken = watch.Elapsed.TotalSeconds;

                if (timeTaken > 5)
                    Context.NCacheLog.Error("POR.handleExecuteReader() taken " + timeTaken + " seconds");                

            }

            return null;
        }

        private object handleExecuteReaderCQ(object arguments)
        {
            if (_internalCache != null)
            {
                object[] data = (object[]) arguments;
                return _internalCache.Local_ExecuteReaderCQ(data[0] as string, data[1] as IDictionary, (bool) data[2],
                    (int) data[3], data[4] as string, data[5] as string, (bool) data[6], (bool) data[7], (bool) data[8],
                    data[9] as OperationContext, data[10] as Queries.QueryDataFilters, (bool) data[11]);
            }

            return null;
        }

        private object handleGetReaderChunk(object arguments)
        {
            if (_internalCache != null)
            {
                object[] data = (object[]) arguments;
                return _internalCache.GetReaderChunk(data[0] as string, (int) data[1], false,
                    data[2] as OperationContext);
            }

            return null;
        }

        private void handleDisposeReader(object arguments)
        {
            if (_internalCache != null)
            {
                object[] data = (object[]) arguments;
                _internalCache.DisposeReader(data[0] as string, data[1] as OperationContext);
            }
        }

        #endregion


        internal override void HandleDeadClientsNotification(string deadClient, ClientInfo info)
        {
            try
            {
                bool localSendOnly = false;
                if (this is ReplicatedServerCache)
                {
                    localSendOnly = true;
                }
                if (Cluster.IsCoordinator)
                {
                    if (info != null)
                        InformEveryoneOfClientActivity(Parent.Name, info, ConnectivityStatus.Disconnected, localSendOnly);
                    CleanDeadClientInfos(deadClient);
                    handleCleanDeadClientInfos(new object[] { Cluster.LocalAddress, deadClient });
                }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.HandleDeadClientsNotification()", e.ToString());
            }
        }

        internal override void RegisterClientActivityCallback(string clientId,
            CacheClientConnectivityChangedCallback callback)
        {
            if (Context.ConnectedClients != null)
                try
                {
                    lock (_registerLock)
                        if (!_registeredClientsForNotification.ContainsKey(clientId))
                            _registeredClientsForNotification.Add(clientId, callback);
                    Cluster.BroadcastToMultiple(Cluster.OtherServers,
                        new Function((int) OpCodes.RegisterClientActivityListener, clientId), GroupRequest.GET_NONE);
                }
                catch (Exception e)
                {
                    Context.NCacheLog.Error("ClusteredCacheBase.RegisterClientActivityCallback()", e.ToString());
                }
        }

        internal override void UnregisterClientActivityCallback(string clientId)
        {
            if (Context.ConnectedClients != null)
                try
                {
                    lock (_registerLock)
                            _registeredClientsForNotification.Remove(clientId);
                    Cluster.BroadcastToMultiple(Cluster.OtherServers,
                        new Function((int) OpCodes.UnregisterClientActivityListener, clientId), GroupRequest.GET_NONE);
                }
                catch (Exception e)
                {
                    Context.NCacheLog.Error("ClusteredCacheBase.UnregisterClientActivityCallback()", e.ToString());
                }
        }

        internal void handleClientActivityListenerRegistered(object operands)
        {
            try
            {
                object[] pair = (object[]) operands;
                Address source = (Address) pair[0];
                string clientId = (string) pair[1];
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
                object[] pair = (object[]) operands;
                Address source = (Address) pair[0];
                string clientId = (string) pair[1];
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


        internal void InformEveryoneOfClientActivity(string cacheId, ClientInfo clientInfo, ConnectivityStatus status,
            bool localSendOnly)
        {
            if (Context.ConnectedClients != null && Context.ConnectedClients.IsClientDeathNotificationSpecified)
            {
                try
                {

                    lock (_registerLock)
                    {
                        foreach (
                            CacheClientConnectivityChangedCallback cacheClientConnectivityChangedCallback in
                                _registeredClientsForNotification.Values)
                        {
                            cacheClientConnectivityChangedCallback.Invoke(cacheId, clientInfo, status);
                        }
                    }

                    if (!localSendOnly)
                    {

                        HashVector<Address, HashSet<string>> othersMap = GetClientNotificationMap();
                        foreach (KeyValuePair<Address, HashSet<string>> keyValuePair in othersMap)
                        {
                            Cluster.SendNoReplyMessage(keyValuePair.Key,
                                new Function((int) OpCodes.InformEveryoneOfClientActivity,
                                    new object[] {keyValuePair.Value.ToArray(), cacheId, clientInfo, (int) status}));
                        }
                    }

                }
                catch (Exception e)
                {
                    Context.NCacheLog.Error("ClusteredCacheBase.handleClientConnectedActivityToCoordinator()",
                        e.ToString());
                }
            }
        }

        internal void handleInformEveryoneOfClientActivity(object operand)
        {
            try
            {
                object[] operands = (object[]) operand;
                string[] concernedClients = (string[]) operands[0];
                string cacheId = (string) operands[1];
                ClientInfo clientInfo = (ClientInfo) operands[2];
                ConnectivityStatus status = (ConnectivityStatus) operands[3];

                CacheClientConnectivityChangedCallback callback;
                lock (_registeredClientsForNotification)
                {
                        foreach (string concernedClient in concernedClients)
                        {
                            if (_registeredClientsForNotification.TryGetValue(concernedClient, out callback))
                                callback.Invoke(cacheId, clientInfo, status);
                        }
                }

            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.handleInformOthersOfConnectedClient()", e.ToString());
            }
        }

        internal HashVector<Address, HashSet<string>> GetClientNotificationMap()
        {
            HashSet<string> coveredClients;
            HashVector<Address, HashSet<string>> othersMap = new HashVector<Address, HashSet<string>>();
            lock (_registerLock)
            {
                coveredClients = new HashSet<string>(_registeredClientsForNotification.Keys);
            }
            lock (_otherNodesRegisterLock)
                foreach (
                    KeyValuePair<Address, HashSet<string>> clientActivityListenersOnOtherNode in
                        _clientActivityListenersOnOtherNodes)
                {
                    HashSet<string> setForThisNode = new HashSet<string>();
                    foreach (string clientId in clientActivityListenersOnOtherNode.Value)
                    {
                        if (!coveredClients.Contains(clientId))
                        {
                            coveredClients.Add(clientId);
                            setForThisNode.Add(clientId);
                        }
                    }
                    if (setForThisNode.Count != 0)
                        othersMap.Add(clientActivityListenersOnOtherNode.Key, setForThisNode);
                }
            return othersMap;
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
                        node.LocalConnectedClientsInfo.Remove(clientIds);
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

        private object HandleTouch(object info, Address src )
        {
            try
            {
                OperationContext operationContext = null;
                object[] args = (object[])info;
                if (args.Length > 1)
                    operationContext = args[1] as OperationContext;
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

          
      
        private ArrayList HandleGetTopicsState()
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
                result = InternalCache.AssignmentOperation(operation.MessageInfo, operation.SubscriptionInfo, operation.Type,operation.Context);
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
                InternalCache.AcknowledgeMessageReceipt(operation.ClientId, operation.Topic,operation.MessageId, operation.OperationContext);
            }
        }
        
        protected virtual void HandleRemoveMessages(RemoveMessagesOperation operation)
        {
            if (InternalCache != null) InternalCache.RemoveMessages(operation.MessagesToRemove, operation.Reason,operation.Context);
        }

        protected virtual void HandleRemoveMessages(AtomicRemoveMessageOperation operation)
        {
            if (InternalCache != null) InternalCache.RemoveMessages(operation.MessagesToRemove, operation.Reason,operation.Context);
        }

        protected virtual void HandleStoreMessage(StoreMessageOperation operation)
        {
            if (InternalCache != null) InternalCache.StoreMessage(operation.Topic, operation.Message, operation.Context);
        }

        protected virtual GetAssignedMessagesResponse HandleGetAssignedMessages(GetAssignedMessagesOperation operation)
        {
            GetAssignedMessagesResponse response = new GetAssignedMessagesResponse();
            if (InternalCache != null)
                response.AssignedMessages = InternalCache.GetAssignedMessage(operation.SubscriptionInfo, operation.OperationContext);
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

                RspList results = Cluster.Multicast(Servers, func, GroupRequest.GET_ALL, false);

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
                throw e;
            }
            catch (Exception e)
            {
                throw new GeneralFailureException(e.Message, e);
            }

            return result;
        }

        public override IDictionary<string, IList<object>> GetAssignedMessage(SubscriptionInfo subscriptionInfo, OperationContext operationContext)
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
                return InternalCache.GetAssignedMessage(subscriptionInfo, operationContext);
            }

            return result.AssignedMessages;
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

        public override ArrayList GetTopicsState()
        {
            ArrayList retVal = null;

            int noOfRetries = 0;
            Exception error = null;
            do
            {
                try
                {
                    error = null;
                    Function func = new Function((int)OpCodes.GetTopicsState, null);
                    retVal = Cluster.SendMessage(Cluster.Coordinator, func, GroupRequest.GET_FIRST, false) as ArrayList;

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

                    ArrayList topicsState = GetTopicsState();
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
            if(operation !=null)
            {
                if(operation is StoreMessageOperation)
                {
                    HandleStoreMessage(operation as StoreMessageOperation);
                }
                else if(operation is AtomicRemoveMessageOperation)
                {
                    HandleRemoveMessages(operation as RemoveMessagesOperation);
                }
                else if(operation is AtomicAcknowledgeMessageOperation)
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
                    Function func = new Function((int)OpCodes.GetTransferrableMessage, new GetTransferrableMessageOperation(topic,messageId));
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

        public override OrderedDictionary GetMessageList(int bucketId)
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

        #endregion
    
        internal void StopBucketLoggingOnReplica(Address replicaAddress, ArrayList buckets)
        {
            Function func = new Function((int)OpCodes.StopBucketLogging, buckets, true);
            Cluster.SendMessage(replicaAddress,func, GroupRequest.GET_FIRST, false, Priority.Normal);
        }
        
        internal override void SetClusterInactive(string reason)
        {
            if (_cluster != null) _cluster.SetAsNonFunctional(reason);            
        }
    }
}