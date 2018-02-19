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
using System.Threading;

using Alachisoft;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Common.Mirroring;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NGroups;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Stack;
using Alachisoft.NGroups.Util;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Monitoring;
using System.Collections.Generic;

using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Runtime.Events;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// A class to serve as the base for all clustered cache implementations.
    /// </summary>
    internal class ClusterCacheBase : CacheBase, IClusterParticipant, IPresenceAnnouncement, IDistributionPolicyMember//, IMirrorManagementMember
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

            /// <summary> Clusterwide Add(key, expirationHint) request </summary>
            AddHint,

            /// <summary> Clusterwide KeyList() request </summary>
            KeyList,

            /// <summary> Clusterwide Search(querytext) request </summary>
            Search,

            /// <summary> Clusterwide SearchEntries(querytext) request </summary>
            SearchEntries,

            /// <summary> Custom item update callback request </summary>
            NotifyCustomUpdateCallback,

            /// <summary> Custom item remove callback request </summary>
            NotifyCustomRemoveCallback,

            ///<summary>Registers callback with an existing item.</summary>
            RegisterKeyNotification,

            ///<summary>UnRegisters callback with an existing item.</summary>
            UnregisterKeyNotification,

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

            UpdateIndice,

            /// <summary>Represents the async replication of invalidated items</summary>
            ReplicateOperations,
            LockKey,
            UnLockKey,
            UpdateLockInfo,
            IsLocked,
            GetNextChunk,

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

            UpdateClientStatus


        }

        /// <summary> The default interval for statistics replication. </summary>
        protected long _statsReplInterval = 5000;

        protected const long forcedViewId = -5; //This id is sent by client to a single node, to direct node to  perform cluster operation possibly on replica as well.
         
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

        protected int _onSuspectedWait = 5000;

        public virtual bool IsRetryOnSuspected
        {
            get { return false; }
        }

        protected ReplicationOperation GetClearReplicationOperation(int opCode, object info)
        {
            return GetReplicationOperation(opCode, info, 2, null, 0);
        }

        protected ReplicationOperation GetReplicationOperation(int opCode, object info, int operationSize, Array userPayLoad, long payLoadSize)
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
                        if (node.RendererAddress != null && node.RendererAddress.IpAddress.ToString().Equals(IntendedRecipient))
                        {
                            return node.Address.IpAddress.ToString();
                        }
                    }
                }
            }

            return IntendedRecipient;
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
            get
            {
                return InternalCache.TypeInfoMap;
            }
        }

        public bool HasDisposed
        {
            get { return _hasDisposed; }
            set { _hasDisposed = value; }
        }
        
        /// <summary>
        /// Get next task's sequence nuumber
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

        }

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public ClusterCacheBase(IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, IClusterEventsListener clusterListener)
            : base(properties, listener, context)
        {
            this._nodeInformationTable = Hashtable.Synchronized(new Hashtable(10));

            _stats = new ClusterCacheStatistics();
            _stats.InstanceName = _context.PerfStatsColl.InstanceName;

            _clusterListener = clusterListener;

            _nodeName = Environment.MachineName.ToLower();

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
        public ClusterService Cluster { get { return _cluster; } }

        /// <summary> The hashtable that contains members and their info. </summary>
        protected ArrayList Members { get { return _cluster.Members; } }
        protected ArrayList ValidMembers { get { return _cluster.ValidMembers; } }
        protected ArrayList Servers { get { return _cluster.Servers; } }

        public virtual ArrayList ActiveServers
        {
            get { return this.Members; }
        }

        /// <summary> The local address of this instance. </summary>
        protected Address LocalAddress { get { return _cluster.LocalAddress; } }

        /// <summary>
        /// returns the statistics of the Clustered Cache. 
        /// </summary>
        public override CacheStatistics Statistics
        {
            get
            {

                return _stats.Clone() as CacheStatistics;

            }
        }

        protected virtual CacheNodeStatus GetNodeStatus()
        {
            return CacheNodeStatus.Running;
        }

        internal override CacheStatistics ActualStats
        {
            get { return _stats; }
        }

        internal string BridgeSourceCacheId
        {
            get { return _cluster.BridgeSourceCacheId; }
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
                    long val = Convert.ToInt64(Convert.ToString(properties["stats-repl-interval"]).TrimEnd('s', 'e', 'c'));
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
                                    this._autoBalancingInterval *= 1000; 
                                    break;
                            }
                        }
                    }
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
                _cluster.Initialize(properties, channelName, domain, identity);
            }
            catch (Exception e)
            {
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }

        protected virtual void InitializeCluster(IDictionary properties,string channelName,string domain,NodeIdentity identity,bool twoPhaseInitialization, bool isPor)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                _cluster = new ClusterService(_context, this, this);
                _cluster.ClusterEventsListener = _clusterListener;
                _cluster.Initialize(properties, channelName, domain, identity, twoPhaseInitialization, isPor);
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
            return true;
        }

        /// <summary>
        /// Called when an existing member leaves the group.
        /// </summary>
        /// <param name="address">address of the joining member</param>
        /// <returns>true if the node left successfuly</returns>
        public virtual bool OnMemberLeft(Address address, NodeIdentity identity)
        {
            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ClusterCacheBase.OnMemberLeft()", "Member left: " + address);

            // we don't have any non-routable expiration thus no NodeExpiration.
            //// Add the leaving node in the list of left nodes.
            
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

            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ClusterCacheBase.OnAfterMembershipChange()", "New Coordinator is: " + _cluster.Coordinator);
        }

        public virtual object HandleClusterMessage(Address src, Function func, out Address destination, out Message replicationMsg)
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
                case (int)OpCodes.NotifyCustomRemoveCallback:
                    return handleNotifyRemoveCallback(func.Operand);
                    
                case (int)OpCodes.NotifyCustomUpdateCallback:
                    return handleNotifyUpdateCallback(func.Operand);
                  
                case (int)OpCodes.RegisterKeyNotification:
                    return handleRegisterKeyNotification(func.Operand);

                case (int)OpCodes.UnregisterKeyNotification:
                    return handleUnregisterKeyNotification(func.Operand);

                case (int)OpCodes.BalanceNode:
                    handleBalanceDataLoad(func.Operand);
                    break;

                case (int)OpCodes.PublishMap:
                    handlePublishMap(func.Operand);
                    break;

                case (int)OpCodes.ExecuteReader:
                    return handleExecuteReader(func.Operand);
                case (int)OpCodes.GetReaderChunk:
                    return handleGetReaderChunk(func.Operand);
                case (int)OpCodes.DisposeReader:
                    handleDisposeReader(func.Operand);
                    break;
                case (int)OpCodes.UpdateClientStatus:
                    handleUpdateClientStatus(func.Operand);
                    break;
            }
            return null;
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
                NCacheLog.Error("ClusterCacheBase.EndStateTransfer", " State transfer ended with Exception " + result.ToString());
            }

            /// Set the status to fully-functional (Running) and tell everyone about it.
            _statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);
            UpdateCacheStatistics();
            AnnouncePresence(true);
        }

        public void SignalEndOfStateTxfr(Address dest)
        {
            Function fun = new Function((int)OpCodes.SignalEndOfStateTxfr, new object());
            if (_cluster != null) _cluster.SendNoReplyMessage(dest, fun);
        }

        internal virtual Hashtable LockBuckets(ArrayList bucketIds)
        {
            return null;
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
            Function function = new Function((int)OpCodes.AnnounceStateTransfer, bucketIds, false);
            Cluster.Broadcast(function, GroupRequest.GET_NONE, false, Priority.Critical);
        }

        internal virtual StateTxfrInfo TransferBucket(ArrayList bucketIds, Address targetNode, byte transferType, bool sparsedBuckets, int expectedTxfrId, bool isBalanceDataLoad)
        {
            return Clustered_TransferBucket(targetNode, bucketIds, transferType, sparsedBuckets, expectedTxfrId, isBalanceDataLoad);
        }
        /// <summary>
        /// Retrieve the list of keys from the cache for the given group or sub group.
        /// </summary>
        protected StateTxfrInfo Clustered_TransferBucket(Address targetNode, ArrayList bucketIds, byte transferType, bool sparsedBuckets, int expectedTxfrId, bool isBalanceDataLoad)
        {
            try
            {
                Function func = new Function((int)OpCodes.TransferBucket, new object[] { bucketIds, transferType, sparsedBuckets, expectedTxfrId, isBalanceDataLoad }, true);
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ClusteredCacheBase.Clustered_TransferBucket", " Sending request for bucket transfer to " + targetNode);
                object result = Cluster.SendMessage(targetNode, func, GroupRequest.GET_FIRST, false);
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ClusteredCacheBase.Clustered_TransferBucket", " Response recieved from " + targetNode);

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
        protected static Hashtable GetAllPayLoads(Array userPayLoad, ArrayList compilationInfo)
        {
            Hashtable result = new Hashtable();

            VirtualArray payLoadArray = new VirtualArray(userPayLoad);
            Alachisoft.NCache.Common.DataStructures.VirtualIndex virtualIndex = new Alachisoft.NCache.Common.DataStructures.VirtualIndex();

            for (int i = 0; i < compilationInfo.Count; i++)
            {
                if ((long)compilationInfo[i] == 0)
                {
                    result[i] = null;
                }
                else
                {
                    VirtualArray atomicPayLoadArray = new VirtualArray((long)compilationInfo[i]);
                    Alachisoft.NCache.Common.DataStructures.VirtualIndex atomicVirtualIndex = new Alachisoft.NCache.Common.DataStructures.VirtualIndex();

                    VirtualArray.CopyData(payLoadArray, virtualIndex, atomicPayLoadArray, atomicVirtualIndex, (int)atomicPayLoadArray.Size);
                    virtualIndex.IncrementBy((int)atomicPayLoadArray.Size);
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
            Function function = new Function((int)OpCodes.ReleaseBuckets, bucketIds, false);
            Cluster.Broadcast(function, GroupRequest.GET_NONE, false, Priority.Critical);
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
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ClusteredCacheBase.AnnouncePresence()", " announcing presence ;urget " + urgent);
                if (this.ValidMembers.Count > 1)
                {
                    NodeInfo localStats = _stats.LocalNode;
                    Function func = new Function((int)OpCodes.PeriodicUpdate, localStats.Clone() as NodeInfo);
                    if (!urgent)
                        Cluster.SendNoReplyMessage(func);
                    else
                        Cluster.Broadcast(func, GroupRequest.GET_NONE,false,Priority.Normal);
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
        protected CacheEntry Clustered_Get(Address address, object key, OperationContext operationContext, bool isUserOperaton)
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
                if (retVal != null && ((OperationResponse)result).UserPayload !=null ) retVal.Value = ((OperationResponse)result).UserPayload;
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
        protected CacheEntry Clustered_Get(Address address, object key, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType access, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.Get", "");

            CacheEntry retVal = null;
            try
            {
                if (operationContext.Contains(OperationContextFieldName.IsClusteredOperation))
                    throw new InvalidReaderException("Reader state has been lost due to state transfer.");
                Function func = new Function((int)OpCodes.Get, new object[] { key, lockId, lockDate, access, lockExpiration, operationContext });
                object result = Cluster.SendMessage(address, func, GetFirstResponse);
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

        protected LockOptions Clustered_Lock(Address address, object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.Lock", "");
            LockOptions retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.LockKey, new object[] { key, lockId, lockDate, lockExpiration, operationContext });
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

        protected LockOptions Clustered_IsLocked(Address address, object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            LockOptions retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.IsLocked, new object[] { key, lockId, lockDate, operationContext });
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

        protected void Clustered_UnLock(Address address, object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.Unlock", "");
            LockOptions retVal = null;
            try
            {
                Function func = new Function((int)OpCodes.UnLockKey, new object[] { key, lockId, isPreemptive, operationContext });
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
        protected Hashtable Clustered_Get(Address dest, object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.GetBlk", "");

            try
            {
                Function func = new Function((int)OpCodes.Get, new object[] { keys, operationContext });
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

        /// <summary>
        /// Retrieve the objects from the cluster. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="excludeSelf">Set false to do a complete cluster lookup.</param>
        /// <returns>cache entry.</returns>
        protected Hashtable Clustered_Add(Address dest, object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustCacheBase.GetBlk", "");

            try
            {
                Function func = new Function((int)OpCodes.Get, new object[] { keys, operationContext });
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



        #region--------------------------------Cache Data Reader----------------------------------------------

        public override ClusteredList<ReaderResultSet> ExecuteReader(string query, IDictionary values, bool getData, int chunkSize, bool isInproc, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

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
                catch (NGroups.SuspectedException ex)
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

            }
            return recordSets;
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
        /// Retrieve the reader result set from the cache based on the specified query.
        /// </summary>
        protected ClusteredList<ReaderResultSet> Clustered_ExecuteReader(ArrayList dests, string queryText, IDictionary values, bool getData, int chunkSize, OperationContext operationContext, bool throwSuspected = false)
        {
            ClusteredList<ReaderResultSet> resultSet = new ClusteredList<ReaderResultSet>();

            try
            {
                Function func = new Function((int)OpCodes.ExecuteReader, new object[] { queryText, values, getData, chunkSize, operationContext }, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout * 10);

                if (results == null)
                    return null;
               
                if (throwSuspected) ClusterHelper.VerifySuspectedResponses(results);

                IList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(ReaderResultSet));
                try
                {
                    ClusterHelper.ValidateResponses(results, typeof(ReaderResultSet), Name);
                }
                catch (Exception e)
                {
                    Context.NCacheLog.Error("ClusteredCacheBase.Clustered_ExecuteReader()", e.ToString());

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
                    if (e is InvalidReaderException) throw;

                    throw new InvalidReaderException("Reader state has been lost.", e);
                }

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
                        ReaderResultSet rResultSet = (ReaderResultSet)rsp.Value;
                        resultSet.Add(rResultSet);
                    }
                }

                return resultSet;
            }
            catch (NGroups.SuspectedException e)
            {
                if (throwSuspected)
                {
                    throw;
                }

                throw new GeneralFailureException(e.Message, e);
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

        public override ReaderResultSet GetReaderChunk(string readerId, int nextIndex, bool isInproc, OperationContext operationContext)
        {
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            ReaderResultSet reader = null;

            string intenededRecepient = GetIntendedRecipient(operationContext);
            Array servers = Array.CreateInstance(typeof(Address), Cluster.Servers.Count);
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
                        reader = InternalCache.GetReaderChunk(readerId, nextIndex, isInproc, operationContext);
                    else
                    {
                        try
                        {
                            operationContext.Add(OperationContextFieldName.IsClusteredOperation, false);
                            reader = Clustered_GetReaderChunk(targetNode, readerId, nextIndex, operationContext);
                        }
                        catch (InvalidReaderException readerException)
                        {
                            if (!string.IsNullOrEmpty(readerId))
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
        private ReaderResultSet Clustered_GetReaderChunk(Address targetNode, string readerId, int nextIndex, OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.GetReaderChunk, new object[] { readerId, nextIndex, operationContext });
                object result = Cluster.SendMessage(targetNode, func, GroupRequest.GET_FIRST, Cluster.Timeout);

                ReaderResultSet readerChunk = result as ReaderResultSet;

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
                throw new GeneralFailureException(e.Message, e);
            }
        }

        public override void DisposeReader(string readerId, OperationContext operationContext)
        {
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            string intenededRecepient = GetIntendedRecipient(operationContext);
            Array servers = Array.CreateInstance(typeof(Address), Cluster.Servers.Count);
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
                Function func = new Function((int)OpCodes.DisposeReader, new object[] { readerId, operationContext });
                Cluster.SendNoReplyMessage(targetNode, func);

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
            if (_internalCache != null)
            {
                object[] data = (object[])arguments;
                return _internalCache.Local_ExecuteReader(data[0] as string, data[1] as IDictionary, (bool)data[2], (int)data[3], false, data[4] as OperationContext);
            }

            return null;
        }
        private object handleGetReaderChunk(object arguments)
        {
            if (_internalCache != null)
            {
                object[] data = (object[])arguments;
                return _internalCache.GetReaderChunk(data[0] as string, (int)data[1], false, data[2] as OperationContext);
            }

            return null;
        }
        private void handleDisposeReader(object arguments)
        {
            if (_internalCache != null)
            {
                object[] data = (object[])arguments;
                _internalCache.DisposeReader(data[0] as string, data[1] as OperationContext);
            }
        }
        #endregion



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
                    result = Clustered_Search(servers, query, values, operationContext,false);
                }
            }
            else if (!VerifyClientViewId(clientLastViewId))
            {
                throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
            }
            else
            {
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
                    result = Clustered_SearchEntries(servers, query, values, operationContext, IsRetryOnSuspected);
                }
                catch (Alachisoft.NGroups.SuspectedException ex)
                {
                    if (!IsRetryOnSuspected) throw;


                    //Sleep is used to be sure that new view applied and node is marked in state transfer...
                    Thread.Sleep(_onSuspectedWait);
                    servers = GetServerParticipatingInStateTransfer();
                    result = Clustered_SearchEntries(servers, query, values, operationContext,false);
                }
            }
            else if (!VerifyClientViewId(clientLastViewId))
            {
                throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
            }
            else
            {
                result = Local_SearchEntries(query, values, operationContext);
                if (IsInStateTransfer())
                {
                    throw new StateTransferInProgressException("Operation could not be completed due to state transfer");
                }
            }

            return result;
        }

        protected virtual QueryResultSet Local_SearchEntries(string query, IDictionary values, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        protected virtual QueryResultSet Local_Search(string query, IDictionary values, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        protected virtual bool VerifyClientViewId(long clientLastViewId)
        {
            throw new NotImplementedException();
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
        /// Retrieve the list of keys from the cache based on the specified query.
        /// </summary>
        protected QueryResultSet Clustered_Search(ArrayList dests, string queryText, IDictionary values, OperationContext operationContext, bool throwSuspected)
        {
            QueryResultSet resultSet = new QueryResultSet();

            try
            {
                Function func = new Function((int)OpCodes.Search, new object[] { queryText, values, operationContext }, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout * 10);

                if (results == null)
                    return null;
                ClusterHelper.ValidateResponses(results, typeof(QueryResultSet), Name, throwSuspected);
                ArrayList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(QueryResultSet));
                if (rspList.Count <= 0)
                    return null;
                else
                {
                    IEnumerator im = rspList.GetEnumerator();
                    while (im.MoveNext())
                    {
                        Rsp rsp = (Rsp)im.Current;
                        QueryResultSet cResultSet = (QueryResultSet)rsp.Value;
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
            catch (Runtime.Exceptions.SuspectedException e)
            {
                if (throwSuspected)
                {
                    throw;
                }

                throw new GeneralFailureException(e.Message, e);
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
        /// Retrieve the list of keys and values from the cache based on the specified query.
        /// </summary>
        protected QueryResultSet Clustered_SearchEntries(ArrayList dests, string queryText, IDictionary values, OperationContext operationContext, bool throwSuspected)
        {
            QueryResultSet resultSet = new QueryResultSet();

            try
            {
                Function func = new Function((int)OpCodes.SearchEntries, new object[] { queryText, values, operationContext }, false);
                RspList results = Cluster.Multicast(dests, func, GroupRequest.GET_ALL, false, Cluster.Timeout * 10);

                if (results == null)
                    return null;
                ClusterHelper.ValidateResponses(results, typeof(QueryResultSet), Name, throwSuspected);
                ArrayList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(QueryResultSet));
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
                        QueryResultSet cResultSet = (QueryResultSet)rsp.Value;
                        resultSet.Compile(cResultSet);
                    }
                }

                return resultSet;
            }
            catch (NGroups.SuspectedException e)
            {
                if (throwSuspected)
                {
                    throw;
                }

                throw new GeneralFailureException(e.Message, e);
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
                ArrayList rspList = ClusterHelper.GetAllNonNullRsp(results, typeof(long));

                IEnumerator ia = rspList.GetEnumerator();
                while (ia.MoveNext())
                {
                    Rsp rsp = (Rsp)ia.Current;
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
                _stats.SessionCount = c.SessionCount;
            }
            catch (Exception)
            {
            }
        }


        /// <summary>
        /// Update Client connectivity status 
        /// </summary>
        public void UpdateClientStatus(string client, bool isConnected)
        {
            try
            {
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ClusteredCacheBase.UpdateClientStatus()", " Updating Client Status accross the cluster");
                if (this.ValidMembers.Count > 1)
                {
                    Object[] objects = null;
                    if (isConnected)
                        objects = new Object[] { client, true };
                    else
                        objects = new Object[] { client, false, DateTime.Now };
                    Function func = new Function((int)OpCodes.UpdateClientStatus, objects, true);
                    Cluster.Broadcast(func, GroupRequest.GET_NONE, false, Priority.Normal);
                }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ClusteredCacheBase.AnnouncePresence()", e.ToString());
            }
        }

        /// <summary>
        /// Update Client connectivity status 
        /// </summary>
        private void handleUpdateClientStatus(object Obj)
        {
            Object[] args = Obj as Object[];

            if (_context.ClientDeathDetection != null)
            {
                string client = args[0].ToString();
                bool isConnected = (bool)args[1];
                if (isConnected)
                    _context.ClientDeathDetection.ClientConnected(client);
                else
                    _context.ClientDeathDetection.ClientDisconnected(client, (DateTime)args[2]);
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
        protected virtual Hashtable Clustered_Insert(object[] keys, CacheEntry[] cacheEntries, OperationContext operationContext)
        {
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

            ArrayList callbackList = eventContext.GetValueByField(EventContextFieldName.ItemRemoveCallbackList) as ArrayList;

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
            NotifyCustomRemoveCallback(objs[0], null, (ItemRemoveReason)objs[1], true, operationContext, eventContext);
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


        protected object handleReplicateBridgeOperation(object arguments)
        {
             object[] args = (object[])arguments;
            ArrayList operations = (ArrayList)args[0];
            return null;
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
                    _cluster.SendNoReplyMcastMessageAsync(dests, new Function((int)OpCodes.NotifyCustomRemoveCallback, packed));
                }
                else
                    _cluster.Multicast(dests, new Function((int)OpCodes.NotifyCustomRemoveCallback, packed), GroupRequest.GET_ALL, false);
            }

            if (sendLocal)
            {
                handleNotifyRemoveCallback(packed);
            }
        }


        protected void RaiseAsyncCustomRemoveCalbackNotifier(object key, CacheEntry entry, ItemRemoveReason reason, OperationContext opContext, EventContext eventContext)
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
                                CallbackInfo removeCallbackInfo = (CallbackInfo)cbEntry.ItemRemoveCallbackListener[i];
                                if (removeCallbackInfo != null && removeCallbackInfo.NotifyOnExpiration)
                                    notifyOnExpirationCount++;
                            }
                        }
                    }

                    if (notifyOnExpirationCount <= 0) notify = false;
                }

                if(notify)
                    _context.AsyncProc.Enqueue(new AsyncBroadcastCustomNotifyRemoval(this, key, entry, reason, opContext, eventContext));
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
        internal void RaiseCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason, bool async, OperationContext operationContext, EventContext eventContext)
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
                    eventContext = CreateEventContext(operationContext, Persistence.EventType.ITEM_REMOVED_CALLBACK);
                    eventContext.Item = CacheHelper.CreateCacheEventEntry(cbEntry.ItemRemoveCallbackListener, cacheEntry);
                    eventContext.Add(EventContextFieldName.ItemRemoveCallbackList, cbEntry.ItemRemoveCallbackListener.Clone());
                }

                object[] packed = new object[] { key, reason, intendedNotifiers, operationContext, eventContext };
                ///Incase of parition and partition of replica, there can be same clients connected
                ///to multiple server. therefore the destinations list will contain more then 
                ///one servers. so the callback will be sent to the same client through different server
                ///to avoid this, we will check the list for local server. if client is connected with
                ///local node, then there is no need to send callback to all other nodes
                ///if there is no local node, then we select the first node in the list.
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

        /// <summary>
        /// Reaises the custom item remove call baack.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cbEntry"></param>
        internal void RaiseCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext)
        {
            RaiseCustomRemoveCalbackNotifier(key, cacheEntry, reason, true, operationContext, eventContext);
        }

        /// <summary>
        /// sends a custom item update callback to the node from which callback was added.
        /// </summary>
        /// <param name="dest">Addess of the callback node</param>
        /// <param name="packed">key,item and actual callback</param>
        private void RaiseCustomUpdateCalbackNotifier(ArrayList dests, object packed, EventContext eventContext, bool broadCasteClusterEvent= true)
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
                _cluster.SendNoReplyMcastMessageAsync(dests, new Function((int)OpCodes.NotifyCustomUpdateCallback, new object[] { objs[0], callbackListeners.Clone(), objs[2], eventContext }));
            }

            if (sendLocal)
            {
                handleNotifyUpdateCallback(new object[] { objs[0], callbackListeners.Clone(), objs[2], eventContext });
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

        protected EventContext CreateEventContext(OperationContext operationContext, Alachisoft.NCache.Persistence.EventType eventType)
        {
            EventContext eventContext = new EventContext();
            OperationID opId = operationContext != null ? operationContext.OperatoinID : null;
            //generate event id
            if (operationContext == null || !operationContext.Contains(OperationContextFieldName.EventContext)) //for atomic operations
            {
                eventContext.EventID = EventId.CreateEventId(opId);
            }
            else //for bulk
            {
                eventContext.EventID = ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
            }

            eventContext.EventID.EventType = eventType;
            return eventContext;

        }

        protected virtual EventDataFilter GetGeneralDataEventFilter(EventType eventType)
        {
            return EventDataFilter.DataWithMetadata;
        }

        protected void RaiseCustomUpdateCalbackNotifier(object key, CacheEntry entry, CacheEntry oldEntry, OperationContext operationContext)
        {
            CallbackEntry value = oldEntry.Value as CallbackEntry;
            EventContext eventContext = null;

            if (value != null && value.ItemUpdateCallbackListener != null && value.ItemUpdateCallbackListener.Count > 0)
            {
                eventContext = CreateEventContext(operationContext, Alachisoft.NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK);

                if (value != null)
                {
                    eventContext.Item = CacheHelper.CreateCacheEventEntry(value.ItemUpdateCallbackListener, entry);
                    eventContext.OldItem = CacheHelper.CreateCacheEventEntry(value.ItemUpdateCallbackListener, oldEntry);

                    RaiseCustomUpdateCalbackNotifier(key, (ArrayList)value.ItemUpdateCallbackListener, eventContext);
                }
            }
        }

        /// <summary>
        /// sends a custom item update callback to the node from which callback was added.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="cbEntry">callback entry</param>
        protected void RaiseCustomUpdateCalbackNotifier(object key, ArrayList itemUpdateCallbackListener, EventContext eventContext, bool broadCasteClusterEvent = true)
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
                ///Incase of parition and partition of replica, there can be same clients connected
                ///to multiple server. therefore the destinations list will contain more then 
                ///one servers. so the callback will be sent to the same client through different server
                ///to avoid this, we will check the list for local server. if client is connected with
                ///local node, then there is no need to send callback to all other nodes
                ///if there is no local node, then we select the first node in the list.
                RaiseCustomUpdateCalbackNotifier(destinations, packed, eventContext, broadCasteClusterEvent);
            }
        }

        #endregion

        public override void ClientConnected(string client, bool isInproc)
        {
            if (_stats != null && _stats.LocalNode != null)
            {
                NodeInfo localNode = (NodeInfo)_stats.LocalNode;
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
                }
            }
        }

        public override void ClientDisconnected(string client, bool isInproc)
        {
            if (_stats != null && _stats.LocalNode != null)
            {
                NodeInfo localNode = (NodeInfo)_stats.LocalNode;
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
        }

        #region/            ---Key based Notification registration ---      /

        /// <summary>
        /// Must be override to provide the registration of key notifications.
        /// </summary>
        /// <param name="operand"></param>
        public virtual object handleRegisterKeyNotification(object operand) { return null; }

        /// <summary>
        /// Must be override to provide the unregistration of key notifications.
        /// </summary>
        /// <param name="operand"></param>
        public virtual object handleUnregisterKeyNotification(object operand) { return null; }

        /// <summary>
        /// Sends a cluster wide request to resgister the key based notifications.
        /// </summary>
        /// <param name="key">key agains which notificaiton is to be registered.</param>
        /// <param name="updateCallback"></param>
        /// <param name="removeCallback"></param>
        public override void RegisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
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

        public override void RegisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] { keys, updateCallback, removeCallback, operationContext };
            if (_cluster.Servers.Count > 1)
            {
                Function fun = new Function((byte)OpCodes.RegisterKeyNotification, obj, false);
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
        public override void UnregisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
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

        public override void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] { keys, updateCallback, removeCallback, operationContext };
            if (_cluster.Servers.Count > 1)
            {
                Function fun = new Function((byte)OpCodes.UnregisterKeyNotification, obj, false);
                _cluster.BroadcastToMultiple(_cluster.Servers, fun, GroupRequest.GET_ALL, true);
            }
            else
                handleUnregisterKeyNotification(obj);
        }
        #endregion

        #region IDistributionPolicyMember Members

        public virtual CacheNode[] GetMirrorMap() { return null; }
        public virtual void InstallMirrorMap(CacheNode[] nodes) { }

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

        public virtual void EmptyBucket(int bucketId) { }

        public virtual void InstallHashMap(DistributionMaps distributionMaps, ArrayList leftMbrs) { }

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
            DistributionInfoData distData = new DistributionInfoData(DistributionMode.Manual, ClusterActivity.None, partNode);
            DistributionMaps maps = GetMaps(distData);

            if (maps.BalancingResult == BalancingResult.Default)
            {
                PublishMaps(maps);
            }
        }

        public void PrintHashMap(ArrayList HashMap, Hashtable BucketsOwnershipMap, string ModuleName)
        {
            ArrayList newMap = HashMap;
            Hashtable newBucketsOwnershipMap = BucketsOwnershipMap;

            string moduleName = ModuleName;

            try
            {
                //print hashmap
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
                else
                {
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

        public void Clustered_PublishMaps(DistributionMaps distributionMaps)
        {
            try
            {
                Function func = new Function((int)OpCodes.PublishMap, new object[] { distributionMaps }, false);
                Cluster.Broadcast(func, GroupRequest.GET_NONE, false, Priority.Critical);
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
                if (Context.NCacheLog.IsErrorEnabled) Context.NCacheLog.Error("PartitionedCache.handlePublishMap()", e.ToString());
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

        public virtual string GetGroupId(Address affectedNode, bool isMirror) { return String.Empty; }


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
            Event evt = new Event(Event.CONFIGURE_NODE_REJOINING, nodes, Priority.Critical);
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
      

    }
}




