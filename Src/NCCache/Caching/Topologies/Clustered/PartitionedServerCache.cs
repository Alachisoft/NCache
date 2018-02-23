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
using Alachisoft.NCache.Common.Monitoring;


using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.DatasourceProviders;

using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Util;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
using Alachisoft.NCache.Caching.Topologies.Clustered.Results;
using Alachisoft.NCache.Caching.Queries;
using System.Collections.Generic;
using System.Net;

using Alachisoft.NCache.MapReduce;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NGroups.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Caching.Messaging;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// This class provides the partitioned cluster cache primitives. 
    /// </summary>
    internal class PartitionedServerCache :
        PartitionedCacheBase,
        ICacheEventsListener
    {
        /// <summary> The periodic update task. </summary>
        private PeriodicPresenceAnnouncer _taskUpdate;

        /// <summary>The data groups allowed for this node</summary>
        private IDictionary _dataAffinity;

        private DistributionManager _distributionMgr;


        private StateTransferTask _stateTransferTask;

        private object _txfrTaskMutex = new object();

      

        private Hashtable _corresponders;

        private long _clusterItemCount;

        private bool threadRunning = true;
        private int confirmClusterStartUP = 3;


        protected IPAddress _srvrJustLeft = null;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        /// <param name="listener">cache events listener</param>
        public PartitionedServerCache(IDictionary cacheClasses, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(properties, listener, context)
        {
            _stats.ClassName = "partitioned-server";
            Initialize(cacheClasses, properties);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        /// <param name="listener">cache events listener</param>
        public PartitionedServerCache(IDictionary cacheClasses, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, IClusterEventsListener clusterListener)
            : base(properties, listener, context, clusterListener)
        {
            _stats.ClassName = "partitioned-server";
            IsCQStateTransfer = true;
            RequiresMessageStateTransfer = true;
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            HasDisposed = true;

            if (_taskUpdate != null)
            {
                _taskUpdate.Cancel();
                _taskUpdate = null;
            }
            if (_internalCache != null)
            {
                _internalCache.Dispose();
                _internalCache = null;
            }

            if (_stateTransferTask != null)
            {
                _stateTransferTask.Stop();
                _stateTransferTask = null;
            }

            if (CQManager != null)
            {
                CQManager.Dispose();
                CQManager = null;
            }

            threadRunning = false;
            base.Dispose();
        }

        #endregion

        /// <summary> 
        /// Returns the cache local to the node, i.e., internal cache.
        /// </summary>
        protected internal override CacheBase InternalCache
        {
            get { return _internalCache; }
        }

        public DistributionManager DistributionMgr
        {
            get { return _distributionMgr; }
            set { _distributionMgr = value; }
        }

        #region	/                 --- Initialization ---           /

        /// <summary>
        /// Method that allows the object to initialize itself. Passes the property map down 
        /// the object hierarchy so that other objects may configure themselves as well..
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        public override void Initialize(IDictionary cacheClasses, IDictionary properties, bool twoPhaseInitialization)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                base.Initialize(cacheClasses, properties);

                IDictionary frontCacheProps = ConfigHelper.GetCacheScheme(cacheClasses, properties, "internal-cache");
                string cacheType = Convert.ToString(frontCacheProps["type"]).ToLower();
                ActiveQueryAnalyzer analyzer = new ActiveQueryAnalyzer(this, frontCacheProps, _context.CacheImpl.Name, this.Context,Context._dataSharingKnownTypesforNet);
                if (cacheType.CompareTo("local-cache") == 0)
                {
                    _internalCache = CacheBase.Synchronized(new HashedLocalCache(cacheClasses, this, frontCacheProps, this, _context, false, analyzer));
                }
                else if (cacheType.CompareTo("overflow-cache") == 0)
                {
                    _internalCache = CacheBase.Synchronized(new HashedOverflowCache(cacheClasses, this, frontCacheProps, this, _context, false, analyzer));
                }
                else
                {
                    throw new ConfigurationException("invalid or non-local class specified in partitioned cache");
                }

                if (properties.Contains("data-affinity"))
                {
                    _dataAffinity = (IDictionary)properties["data-affinity"];
                }

                _distributionMgr = new DistributionManager(_autoBalancingThreshold, _internalCache.MaxSize);
                
                _distributionMgr.NCacheLog = Context.NCacheLog;

                InternalCache.BucketSize = _distributionMgr.BucketSize;

                _stats.Nodes = ArrayList.Synchronized(new ArrayList());

                _initialJoiningStatusLatch = new Latch();

                InitializeCluster(properties, Name, MCAST_DOMAIN, new Identity(true, (_context.Render != null ? _context.Render.Port : 0), (_context.Render != null ? _context.Render.IPAddress : null)),  twoPhaseInitialization, false);
                _stats.GroupName = Cluster.ClusterName;
                _distributionMgr.LocalAddress = LocalAddress;

                postInstrumentatedData(_stats, Name);

                //Wait for the completion of installation of new view 
                if (!twoPhaseInitialization)
                {
                    _initialJoiningStatusLatch.WaitForAny(NodeStatus.Running);
                    DetermineClusterStatus();
                    _statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);
                    AnnouncePresence(true);
                }

                for (int i = 0; i < confirmClusterStartUP; i++)
                {
                    ConfirmClusterStartUP(false, i + 1);
                }

                HasStarted();
               
            }
            catch (ConfigurationException)
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

        #endregion

        /// <summary>
        /// Fetch state from a cluster member. If the node is the coordinator there is
        /// no need to do the state transfer.
        /// </summary>
        protected override void StartStateTransfer(bool isBalanceDataLoad)
        {
            /// Tell everyone that we are not fully-functional, i.e., initilizing.
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.StartStateTransfer()", "Requesting state transfer " + LocalAddress);

            if (_stateTransferTask == null)
                _stateTransferTask = new StateTransferTask(this, Cluster.LocalAddress);
            _stateTransferTask.IsBalanceDataLoad = isBalanceDataLoad;
            _stateTransferTask.DoStateTransfer(_distributionMgr.GetBucketsList(Cluster.LocalAddress), false );

        }

        private int GetBucketId(string key)
        {
            int hashCode = AppUtil.GetHashCode(key);
            int bucketId = hashCode / _distributionMgr.BucketSize;

            if (bucketId < 0)
                bucketId *= -1;
            return bucketId;
        }

        #region	/                 --- Overrides for ClusteredCache ---           /

        /// <summary>
        /// Called after the membership has been changed. Lets the members do some
        /// member oriented tasks.
        /// </summary>
        public override void OnAfterMembershipChange()
        {
            try
            {
                NotifyHashmapChanged(Cluster.LastViewID, _distributionMgr.GetOwnerHashMapTable(Cluster.Renderers), GetClientMappedServers(Cluster.Servers.Clone() as ArrayList), true, true);
            }
            catch (Exception exc)
            {
            }

            base.OnAfterMembershipChange();

            if (_taskUpdate == null)
            {
                _taskUpdate = new PeriodicPresenceAnnouncer(this, _statsReplInterval);
                _context.TimeSched.AddTask(_taskUpdate);
                _statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);

                //Set joining completion status.
                _initialJoiningStatusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);
            }

            // Cancel all task on topology/membership change...
            // Node Join or Node Leave.
            MapReduceOperation operation = new MapReduceOperation();
            operation.OpCode = MapReduceOpCodes.CancellAllTasks;
            HandleMapReduceOperation(operation);


            if (_context.MessageManager != null) _context.MessageManager.StartMessageProcessing();
            if (_context.DsMgr != null )
                _context.DsMgr.StartWriteBehindProcessor();

            StartStateTransfer(false);
            UpdateCacheStatistics();
        }

        /// <summary>
        /// Called when a new member joins the group.
        /// </summary>
        /// <param name="address">address of the joining member</param>
        /// <param name="identity">additional identity information</param>
        /// <returns>true if the node joined successfuly</returns>
        public override bool OnMemberJoined(Address address, NodeIdentity identity)
        {
            if (!base.OnMemberJoined(address, identity) || !((Identity)identity).HasStorage)
                return false;

            // Cancel all task on topology/membership change...
            // Node Join or Node Leave.
            MapReduceOperation operation = new MapReduceOperation();
            operation.OpCode = MapReduceOpCodes.CancellAllTasks;
            HandleMapReduceOperation(operation);

            NodeInfo info = new NodeInfo(address as Address);
            if (identity.RendererAddress != null)
                info.RendererAddress = new Address(identity.RendererAddress, identity.RendererPort);
            info.IsInproc = identity.RendererPort == 0;
            info.SubgroupName = identity.SubGroupName;
            _stats.Nodes.Add(info);
            _distributionMgr.OnMemberJoined(address, identity);

            if (LocalAddress.CompareTo(address) == 0)
            {
                 
                UpdateLocalBuckets();

                _stats.LocalNode = info;
                if (_dataAffinity != null)
                {
                    DataGrouping.DataAffinity da = new DataGrouping.DataAffinity(_dataAffinity);
                    _stats.LocalNode.DataAffinity = da;
                    if (da.AllBindedGroups != null)
                    {
                        IEnumerator ie = da.AllBindedGroups.GetEnumerator();
                        while (ie.MoveNext())
                        {
                            if (!_stats.ClusterDataAffinity.Contains(ie.Current))
                            {
                                _stats.ClusterDataAffinity.Add(ie.Current);
                            }

                             
                            if (_stats.PartitionsHavingDatagroup.Contains(ie.Current))
                            {
                                ArrayList nodeList = (ArrayList)_stats.PartitionsHavingDatagroup[ie.Current];
                                if (!nodeList.Contains(address))
                                {
                                    nodeList.Add(address);
                                }
                            }
                            else
                            {
                                ArrayList nodeList = new ArrayList();
                                nodeList.Add(address);
                                _stats.PartitionsHavingDatagroup[ie.Current] = nodeList;
                            }
                        }

                         
                        if (!_stats.DatagroupsAtPartition.Contains(address))
                        {
                            _stats.DatagroupsAtPartition[address] = da.Groups;
                        }
                    }
                    _dataAffinity = null;
                }

            }

            if (!info.IsInproc)
                AddServerInformation(address, identity.RendererPort, info.ConnectedClients.Count);

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.OnMemberJoined()", "Partition extended: " + address);
            return true;
        }

        /// <summary>
        /// Called when an existing member leaves the group.
        /// </summary>
        /// <param name="address">address of the joining member</param>
        /// <returns>true if the node left successfuly</returns>
        public override bool OnMemberLeft(Address address, NodeIdentity identity)
        {
            if (!base.OnMemberLeft(address, identity))
                return false;

            // Cancel all task on topology/membership change...
            // Node Join or Node Leave.
            MapReduceOperation operation = new MapReduceOperation();
            operation.OpCode = MapReduceOpCodes.CancellAllTasks;
            HandleMapReduceOperation(operation);

            NodeInfo info = _stats.GetNode(address as Address);

            if (_context.ConnectedClients != null)
                _context.ConnectedClients.ClientsDisconnected(info.ConnectedClients, info.Address, DateTime.Now);

            _stats.Nodes.Remove(info);
            _distributionMgr.OnMemberLeft(address, identity);
             
            if (_stats.DatagroupsAtPartition.Contains(address))
            {
                ArrayList datagroups = (ArrayList)_stats.DatagroupsAtPartition[address];
                if (datagroups != null && datagroups.Count > 0)
                {
                    IEnumerator ie = datagroups.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        if (_stats.PartitionsHavingDatagroup.Contains(ie.Current))
                        {
                            ArrayList nodeList = (ArrayList)_stats.PartitionsHavingDatagroup[ie.Current];
                            if (nodeList != null)
                            {
                                if (nodeList.Contains(address))
                                    nodeList.Remove(address);
                            }
                        }
                    }
                }
                _stats.DatagroupsAtPartition.Remove(address);
            }

             
            UpdateLocalBuckets();

            if (_corresponders != null)
            {
                StateTxfrCorresponder cor = _corresponders[address] as StateTxfrCorresponder;
                if (cor != null)
                {
                    cor.Dispose();
                    _corresponders.Remove(address);
                }
            }
            if (!info.IsInproc)
                RemoveServerInformation(address, identity.RendererPort);

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.OnMemberLeft()", "Partition shrunk: " + address);
            return true;
        }

        /// <summary>
        /// Handles the function requests.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public override object HandleClusterMessage(Address src, Function func)
        {
            switch (func.Opcode)
            {
                case (int)OpCodes.PeriodicUpdate:
                    return handlePresenceAnnouncement(src, func.Operand);

                case (int)OpCodes.ReqStatus:
                    return this.handleReqStatus();

                case (int)OpCodes.GetCount:
                    return handleCount();

                case (int)OpCodes.Get:
                    return handleGet(func.Operand);

                case (int)OpCodes.Insert:
                    return handleInsert(src, func.Operand, func.UserPayload);

                case (int)OpCodes.Contains:
                    return handleContains(func.Operand);

                case (int)OpCodes.Add:
                    return handleAdd(src, func.Operand, func.UserPayload);

                case (int)OpCodes.AddHint:
                    return handleAddHint(func.Operand);

                case (int)OpCodes.AddSyncDependency:
                    return handleAddSyncDependency(func.Operand);

                case (int)OpCodes.Remove:
                    return handleRemove(src, func.Operand);

                case (int)OpCodes.Clear:
                    return handleClear(src, func.Operand);

                case (int)OpCodes.KeyList:
                    return handleKeyList();

                case (int)OpCodes.NotifyAdd:
                    return handleNotifyAdd(func.Operand);

                case (int)OpCodes.NotifyUpdate:
                    return handleNotifyUpdate(func.Operand);

                case (int)OpCodes.NotifyRemoval:
                    return handleNotifyRemoval(func.Operand);

                case (int)OpCodes.NotifyBulkRemoval:
                    return handleNotifyBulkRemoval(func.Operand);

                case (int)OpCodes.GetKeys:
                    return handleGetKeys(func.Operand);

                case (int)OpCodes.GetData:
                    return handleGetData(func.Operand);

                case (int)OpCodes.RemoveGroup:
                    return handleRemoveGroup(func.Operand);

                case (int)OpCodes.AddDepKeyList:
                    return handleAddDepKeyList(func.Operand);

                case (int)OpCodes.RemoveDepKeyList:
                    return handleRemoveDepKeyList(func.Operand);

                case (int)OpCodes.Search:
                    return handleSearch(func.Operand);

                case (int)OpCodes.SearchEntries:
                    return handleSearchEntries(func.Operand);

                case (int)OpCodes.DeleteQuery:
                    return handleDeleteQuery(func.Operand);

                case (int)OpCodes.VerifyDataIntegrity:
                    return handleVerifyDataIntegrity(func.Operand);

                case (int)OpCodes.GetDataGroupInfo:
                    return handleGetGroupInfo(func.Operand);

                case (int)OpCodes.GetGroup:
                    return handleGetGroup(func.Operand);

                case (int)OpCodes.LockBuckets:
                    return handleLockBuckets(func.Operand);

                case (int)OpCodes.ReleaseBuckets:
                    handleReleaseBuckets(func.Operand, src);
                    break;

                case (int)OpCodes.TransferBucket:
                    return handleTransferBucket(src, func.Operand);

                case (int)OpCodes.AckStateTxfr:
                    handleAckStateTxfr(func.Operand, src);
                    break;

                case (int)OpCodes.AnnounceStateTransfer:
                    handleAnnounceStateTransfer(func.Operand, src);
                    break;

                case (int)OpCodes.SignalEndOfStateTxfr:
                    handleSignalEndOfStateTxfr(src);
                    break;

                case (int)OpCodes.LockKey:
                    return handleLock(func.Operand);

                case (int)OpCodes.UnLockKey:
                    handleUnLock(func.Operand);
                    break;

                case (int)OpCodes.IsLocked:
                    return handleIsLocked(func.Operand);

                case (int)OpCodes.GetTag:
                    return handleGetTag(func.Operand);

                case (int)OpCodes.GetKeysByTag:
                    return handleGetKeysByTag(func.Operand);

                case (int)OpCodes.UnRegisterCQ:
                    return handleUnRegisterCQ(func.Operand);

                case (int)OpCodes.RegisterCQ:
                    return handleRegisterCQ(func.Operand);

                case (int)OpCodes.SearchCQ:
                    return handleSearchCQ(func.Operand);

                case (int)OpCodes.SearchEntriesCQ:
                    return handleSearchEntriesCQ(func.Operand);

                case (int)OpCodes.RemoveByTag:
                    return handleRemoveByTag(func.Operand);

                case (int)OpCodes.GetNextChunk:
                    return handleGetNextChunk(src, func.Operand);


            }
            return base.HandleClusterMessage(src, func);
        }

        private object handleRemoveByTag(object info)
        {
            if (_internalCache != null)
            {
                object[] data = (object[])info;
                Hashtable removed = Local_RemoveTag(data[0] as string[], (TagComparisonType)data[1], (bool)data[2], data[3] as OperationContext);
                return removed;
            }

            return null;
        }

        private object handleSearchEntriesCQ(object info)
        {
            if (_internalCache != null)
            {
                QueryResultSet rs;
                object[] data = (object[])info;
                ContinuousQuery query = data[0] as ContinuousQuery;
                if (CQManager.Exists(query))
                {
                    rs = _internalCache.SearchEntriesCQ(query.UniqueId, data[6] as OperationContext);
                    CQManager.Update(query, data[1] as string, data[2] as string, (bool)data[3], (bool)data[4], (bool)data[5], (QueryDataFilters) data[7]);
                    return rs;
                }
                else
                {
                    rs = _internalCache.SearchEntriesCQ(query, data[6] as OperationContext);
                    CQManager.Register(query, data[1] as string, data[2] as string, (bool)data[3], (bool)data[4], (bool)data[5], (QueryDataFilters)data[7]);
                    return rs;
                }
            }

            return null;
        }

        private object handleSearchCQ(object info)
        {
            if (_internalCache != null)
            {
                QueryResultSet rs;
                object[] data = (object[])info;
                ContinuousQuery query = data[0] as ContinuousQuery;
                if (CQManager.Exists(query))
                {
                    rs = _internalCache.SearchCQ(query.UniqueId, data[6] as OperationContext);
                    CQManager.Update(query, data[1] as string, data[2] as string, (bool)data[3], (bool)data[4], (bool)data[5], (QueryDataFilters)data[7]);
                    return rs;
                }
                else
                {
                    rs = _internalCache.SearchCQ(query, data[6] as OperationContext);
                    CQManager.Register(query, data[1] as string, data[2] as string, (bool)data[3], (bool)data[4], (bool)data[5], (QueryDataFilters)data[7]);
                    return rs;
                }
            }

            return null;
        }

        private object handleRegisterCQ(object info)
        {
            if (_internalCache != null)
            {
                object[] data = (object[])info;
                ContinuousQuery query = data[0] as ContinuousQuery;
                if (CQManager.Exists(query))
                {
                    CQManager.Update(query, data[1] as string, data[2] as string, (bool)data[3], (bool)data[4], (bool)data[5], (QueryDataFilters)data[7]);
                }
                else
                {
                    _internalCache.RegisterCQ(query, data[6] as OperationContext);
                    CQManager.Register(query, data[1] as string, data[2] as string, (bool)data[3], (bool)data[4], (bool)data[5], (QueryDataFilters)data[7]);
                }
            }

            return null;
        }

        private object handleUnRegisterCQ(object info)
        {
            if (_internalCache != null)
            {
                object[] data = (object[])info;
                string queryId = data[0] as string;
                if (CQManager.UnRegister(queryId, data[1] as string, data[2] as string))
                {
                    _internalCache.UnRegisterCQ(queryId);
                }
            }

            return null;
        }

        private object handleGetKeysByTag(object info)
        {
            if (_internalCache != null)
            {
                object[] data = (object[])info;                
                ICollection keys = _internalCache.GetTagKeys(data[0] as string[], (TagComparisonType)data[1], data[2] as OperationContext);
                return new ArrayList(keys);
            }

            return null;
        }

        #endregion

        #region	/                 --- Statistics Replication ---           /

        /// <summary>
        /// Periodic update (PULL model), i.e. on demand fetch of information from every node.
        /// </summary>
        internal override bool DetermineClusterStatus()
        {
            try
            {
                Function func = new Function((int)OpCodes.ReqStatus, null);
                RspList results = Cluster.BroadcastToServers(func, GroupRequest.GET_ALL, false);

                for (int i = 0; i < results.size(); i++)
                {
                    Rsp rsp = (Rsp)results.elementAt(i);
                    NodeInfo nodeInfo = rsp.Value as NodeInfo;
                    if (nodeInfo != null)
                    {
                        handlePresenceAnnouncement(rsp.Sender as Address, nodeInfo);
                    }
                }
            }
            ///This is Madness..
            ///No .. this was previously maintained as it is i.e. using exception as flow control
            ///Null can be rooted down to MirrorManager.GetGroupInfo where null is intentionally thrown and here instead of providing a check ...
            ///This flow can be found in every DetermineCluster function of every topology
            catch (NullReferenceException) { }
            catch (Exception e)
            {
                NCacheLog.Error("ParitionedCache.DetermineClusterStatus()", e.ToString());
            }
            return false;
        }
        
        /// <summary>
        /// Handler for Periodic update (PUSH model).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        private object handlePresenceAnnouncement(Address sender, object obj)
        {
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServer.handlePresenceAnnouncement", "sender :" + sender);
            NodeInfo other = null;
            NodeInfo info = null;

            lock (Servers.SyncRoot)
            {
                other = obj as NodeInfo;
                info = _stats.GetNode(sender as Address);
                if (other != null && info != null)
                {
                    if (!IsReplicationSequenced(info, other)) return null;
                    info.Statistics = other.Statistics;
                    info.StatsReplicationCounter = other.StatsReplicationCounter;
                    info.Status = other.Status;
                    info.ConnectedClients = other.ConnectedClients;
                    info.LocalConnectedClientsInfo = other.LocalConnectedClientsInfo;
                    info.DataAffinity = other.DataAffinity;
                    info.CacheNodeStatus = other.CacheNodeStatus;

                    if (other.DataAffinity != null && other.DataAffinity.Groups != null)
                    {
                        IEnumerator ie = other.DataAffinity.Groups.GetEnumerator();
                        while (ie.MoveNext())
                        {
                            if (!_stats.ClusterDataAffinity.Contains(ie.Current))
                            {
                                _stats.ClusterDataAffinity.Add(ie.Current);
                            }

                             
                            if (_stats.PartitionsHavingDatagroup.Contains(ie.Current))
                            {
                                ArrayList nodeList = (ArrayList)_stats.PartitionsHavingDatagroup[ie.Current];
                                if (!nodeList.Contains(sender))
                                {
                                    nodeList.Add(sender);
                                }
                            }
                            else
                            {
                                ArrayList nodeList = new ArrayList();
                                nodeList.Add(sender);
                                _stats.PartitionsHavingDatagroup[ie.Current] = nodeList;
                            }
                        }
                         
                        if (!_stats.DatagroupsAtPartition.Contains(sender))
                        {
                            _stats.DatagroupsAtPartition[sender] = other.DataAffinity.Groups;
                        }
                    }                    
                }

            }
            if (other != null && info != null)
            {
                _distributionMgr.UpdateBucketStats(other);
                if (!info.IsInproc)
                    UpdateClientsCount(sender, info.ConnectedClients.Count);
            }
            UpdateCacheStatistics(false);
            return null;
        }


        /// <summary>
        /// Updates the statistics for the cache scheme.
        /// </summary>
        protected override void UpdateCacheStatistics()
        {
            UpdateCacheStatistics(true);
        }

        /// <summary>
        /// Updates the statistics for the cache scheme.
        /// </summary>
        protected void UpdateCacheStatistics(bool updateBucketstats)
        {
            base.UpdateCacheStatistics();
            if (updateBucketstats) _distributionMgr.UpdateBucketStats(_stats.LocalNode);
        }

        public override CacheStatistics CombineClusterStatistics(ClusterCacheStatistics s)
        {
            CacheStatistics c = ClusterHelper.CombinePartitionStatistics(s);
            return c;
        }

        /// <summary>
        /// Update the list of the clients connected to this node
        /// and replicate it over the entire cluster.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="isInproc"></param>
        /// <param name="clientInfo"></param>
        public override void ClientConnected(string client, bool isInproc, ClientInfo clientInfo)
        {
            base.ClientConnected(client, isInproc, clientInfo);
            if (_context.ConnectedClients != null) UpdateClientStatus(client, true, clientInfo);
            PublishStats(false);
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);
        }

        /// <summary>
        /// Update the list of the clients connected to this node
        /// and replicate it over the entire cluster.
        /// </summary>
        /// <param name="client"></param>
        public override void ClientDisconnected(string client, bool isInproc)
        {
            base.ClientDisconnected(client, isInproc);
            if (_context.ConnectedClients != null) UpdateClientStatus(client, false, null);
            PublishStats(false);
        }

        public override CacheStatistics Statistics
        {
            get
            {
                CacheStatistics stats = _stats.Clone() as CacheStatistics;
                long maxSize = 0;
                foreach (NodeInfo nodeInfo in _stats.Nodes)
                {
                    if (nodeInfo.Statistics != null)
                    {
                        maxSize += nodeInfo.Statistics.MaxSize;
                    }
                }
                stats.MaxSize = maxSize;
                return stats;

            }
        }

        #endregion

        #region /                 --- data distribution + state transfer ---           /

        public override void BalanceDataLoad()
        {
            Clustered_BalanceDataLoad(Cluster.Coordinator, Cluster.LocalAddress);
        }

        /// <summary>
        /// Return the next node in load balancing order.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Address GetNextNode(string key, string group)
        {
            return _distributionMgr.SelectNode(key, group);
        }

        private IDictionary GetTargetNodes(ClusteredArrayList keys, string group)
        {
            Hashtable targetNodes = new Hashtable();
            Address targetNode = null;

            if (keys != null)
            {
                foreach (object key in keys)
                {
                    targetNode = GetNextNode(key as string, group);
                    if (targetNode != null)
                    {
                        if (targetNodes.Contains(targetNode))
                        {
                            Hashtable keyList = (Hashtable)targetNodes[targetNode];
                            keyList.Add(key, null);
                        }
                        else
                        {
                            Hashtable keyList = new Hashtable();
                            keyList.Add(key, null);
                            targetNodes[targetNode] = keyList;
                        }
                    }
                }
            }
            return targetNodes;
        }

        internal override Hashtable LockBuckets(ArrayList bucketIds)
        {
            return Clustered_LockBuckets(bucketIds, LocalAddress, Cluster.Coordinator);
        }

        /// <summary>
        /// Locks the buckets which are under the process of state transfer. A locked
        /// bucket can not be assigned to a node while loadbalancing. Only a coordinator
        /// node can lock the buckets.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private Hashtable handleLockBuckets(object info)
        {
            Hashtable result = null;
            try
            {
                object[] package = info as object[];
                ArrayList bucketIds = package[0] as ArrayList;
                Address owner = package[1] as Address;

                if (Cluster.IsCoordinator)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("received lock bucket request from " + owner);
                    result = _distributionMgr.LockBuckets(bucketIds, owner);
                }
                else
                {
                    NCacheLog.Error("PartitionedServerCache.handleLockBuckets", "i am not coordinator but i have received bucket lock request.");
                }
            }
            catch (Exception)
            {
                return result;
            }
            finally
            {

            }
            return result;
        }



        private void handleAnnounceStateTransfer(object info, Address src)
        {
            ArrayList bucketIds = info as ArrayList;
            _distributionMgr.ChangeBucketStatusToStateTransfer(bucketIds, src);
        }

        private OperationResponse handleTransferBucket(Address sender, object info)
        {
            object[] pack = info as object[];

            ArrayList bucketIds = pack[0] as ArrayList;
            byte transferType = (byte)pack[1];
            bool sparsedBuckets = (bool)pack[2];
            int expectedTxfrId = (int)pack[3];
            bool isBalanceDataLoad = (bool)pack[4];
        
            if (_corresponders == null)
                _corresponders = new Hashtable();

            StateTxfrCorresponder corresponder = null;
            lock (_corresponders.SyncRoot)
            {
                corresponder = _corresponders[sender] as StateTxfrCorresponder;

                if (corresponder == null)
                {
                    corresponder = new StateTxfrCorresponder(this, _distributionMgr, sender, transferType);
                    _corresponders[sender] = corresponder;
                }
            }
            corresponder.IsBalanceDataLoad = isBalanceDataLoad;
            //ask the corresponder to transfer data for the bucket(s).

            StateTxfrInfo transferInfo = corresponder.TransferBucket(bucketIds, sparsedBuckets, expectedTxfrId);
            OperationResponse rsp = new OperationResponse();
            rsp.SerializablePayload = transferInfo;
            rsp.SerilizationStream = transferInfo.SerlizationStream;
            return rsp;
        }



        private void handleReleaseBuckets(object info, Address src)
        {
            try
            {
                ArrayList bucketIds = info as ArrayList;
                _distributionMgr.ReleaseBuckets(bucketIds, src);
            }
            catch (Exception e)
            {
                NCacheLog.Error("PartitionedServerCache.handleReleaseBuckets", e.ToString());
            }
        }

        internal override void AckStateTxfrCompleted(Address owner, ArrayList bucketIds)
        {
            Clustered_AckStateTxfrCompleted(owner, bucketIds);
        }

        private void handleAckStateTxfr(object info, Address client)
        {
            object[] keys = null;
            try
            {
                ArrayList bucketIds = (ArrayList)info;
                IEnumerator ie = bucketIds.GetEnumerator();
                while (ie.MoveNext())
                {
                     
                    //remove this bucket from the local buckets.
                    //this bucket has been transfered to some other node.
                    InternalCache.RemoveBucket((int)ie.Current);
                }
            }
            catch (Exception)
            {

            }
        }

        private void handleSignalEndOfStateTxfr(Address requestingNode)
        {
            if (_corresponders != null)
            {
                StateTxfrCorresponder cor = _corresponders[requestingNode] as StateTxfrCorresponder;
                if (cor != null)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.handleSignalEndOfTxfr", requestingNode.ToString() + " Corresponder removed.");
                    cor.Dispose();
                    _corresponders.Remove(requestingNode);
                }
            }
        }

        public override void UpdateLocalBuckets()
        {
            if (BucketsOwnershipMap != null)
            {
                if (BucketsOwnershipMap.Contains(LocalAddress))
                {
                    ArrayList myBuckets = BucketsOwnershipMap[LocalAddress] as ArrayList;
                    if (myBuckets != null)
                    {
                        ArrayList bucketIds = new ArrayList();
                        foreach (HashMapBucket bucket in myBuckets)
                        {
                            bucketIds.Add(bucket.BucketId);
                        }
                        _internalCache.UpdateLocalBuckets(bucketIds);
                    }
                }
            }
        }

        #endregion


        #region	/                 --- Replicate Connection String ---           /

        public override void ReplicateConnectionString(string connString, bool isSql)
        {
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            try
            {
                if (_internalCache == null) throw new InvalidOperationException();
                if (Servers.Count >= 1)
                {
                    Clustered_ReplicateConnectionString(Cluster.Servers, connString, isSql, true);
                }
            }
            finally { }
        }

        #endregion


        #region	/                 --- Cascading Dependencies ---           /

        public override Hashtable AddDepKeyList(Hashtable table, OperationContext operationContext)
        {
            Hashtable targetNodes = null;
            ArrayList contains = new ArrayList();
            Hashtable result = new Hashtable();
            Hashtable tmpData = null;
            Hashtable totalKeys = null;
            Address targetNode = null;
            bool suspectedErrorOccured = false;

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            try
            {
                if (_internalCache == null) throw new InvalidOperationException();

                if (totalKeys == null)
                    totalKeys = new Hashtable();

                foreach (object key in table.Keys)
                {
                    totalKeys.Add(key, null);
                }

                while (totalKeys.Count > 0)
                {
                    ClusteredArrayList _keys = new ClusteredArrayList(totalKeys.Keys);

                    targetNodes = (Hashtable)GetTargetNodes(_keys, null);

                    if (targetNodes != null && targetNodes.Count == 0)
                    {
                        foreach (object key in totalKeys.Keys)
                        {
                            result[key] = new OperationFailedException("No target node available to accommodate the data.");
                        }
                        return result;
                    }

                    IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                    Hashtable keyList = null;
                    //We select one node at a time for contain operation.
                    while (ide.MoveNext())
                    {
                        targetNode = ide.Key as Address;
                        keyList = (Hashtable)ide.Value;
                        if (targetNode != null) break;
                    }

                    if (targetNode != null)
                    {
                        Hashtable tempTable = new Hashtable();
                        foreach (object key in keyList.Keys)
                        {
                            tempTable.Add(key, table[key]);
                        }

                        try
                        {
                            if (targetNode.Equals(Cluster.LocalAddress))
                            {
                                tmpData = Local_AddDepKeyList(tempTable, operationContext);
                            }
                            else
                            {
                                tmpData = Clustered_AddDepKeyList(targetNode, tempTable, operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            suspectedErrorOccured = true;
                            Thread.Sleep(_serverFailureWaitTime);
                            //we redo the operation
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.AddDepKeyList()", targetNode + " left while update DependencyKeyList. Error: " + se.ToString());
                            continue;
                        }
                        catch (Alachisoft.NGroups.SuspectedException se)
                        {
                            suspectedErrorOccured = true;
                            Thread.Sleep(_serverFailureWaitTime);
                            //we redo the operation
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.AddDepKeyList()", targetNode + " left while update DependencyKeyList. Error: " + se.ToString());
                            continue;
                        }
                        catch (Runtime.Exceptions.TimeoutException te)
                        {
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.AddDepKeyList()", targetNode + " operation timed out. Error: " + te.ToString());
                            if (suspectedErrorOccured)
                            {
                                suspectedErrorOccured = false;
                            }
                            else
                            {
                                IDictionaryEnumerator den = tempTable.GetEnumerator();
                                while (den.MoveNext())
                                {
                                    totalKeys.Remove(den.Key);
                                }
                            }
                            continue;
                        }

                        //list of items which have been transfered to some other node.
                        //so we need to revisit them.
                        ArrayList remainingKeys = null;

                        if (tmpData != null && tmpData.Count > 0)
                        {
                            remainingKeys = new ArrayList();
                            IDictionaryEnumerator ie = tmpData.GetEnumerator();
                            while (ie.MoveNext())
                            {
                                if (ie.Value is StateTransferException)
                                    remainingKeys.Add(ie.Key);
                                else
                                {
                                    totalKeys.Remove(ie.Key);
                                    result[ie.Key] = ie.Value;
                                }
                            }
                        }

                        if (remainingKeys != null && remainingKeys.Count > 0)
                        {
                            _distributionMgr.Wait(remainingKeys[0], null);
                        }
                    }
                }
                return result;
            }
            finally
            {
            }
        }

        private Hashtable Local_AddDepKeyList(Hashtable table, OperationContext operationContext)
        {
            Hashtable retVal = null;
            if (_internalCache != null)
            {
                retVal = _internalCache.AddDepKeyList(table, operationContext);
            }
            return retVal;
        }

        private Hashtable handleAddDepKeyList(object info)
        {
            try
            {
                object[] objs = (object[])info;
                OperationContext oc = objs[1] as OperationContext;
                return Local_AddDepKeyList((Hashtable)objs[0], oc);
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        public override Hashtable RemoveDepKeyList(Hashtable table, OperationContext operationContext)
        {
            if (table == null) return null;
            Hashtable targetNodes = null;
            ArrayList contains = new ArrayList();
            Hashtable result = new Hashtable();
            Hashtable tmpData = null;
            Hashtable totalKeys = null;
            Address targetNode = null;
            bool suspectedErrorOccured = false;

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            try
            {
                if (_internalCache == null) throw new InvalidOperationException();

                if (totalKeys == null)
                    totalKeys = new Hashtable();

                foreach (object key in table.Keys)
                {
                    totalKeys.Add(key, null);
                }

                while (totalKeys.Count > 0)
                {
                    ClusteredArrayList _keys = new ClusteredArrayList(totalKeys.Keys);
                    targetNodes = (Hashtable)GetTargetNodes(_keys, null);

                    if (targetNodes != null && targetNodes.Count == 0)
                    {
                        foreach (object key in totalKeys.Keys)
                        {
                            result[key] = new OperationFailedException("No target node available to accommodate the data.");
                        }
                        return result;
                    }

                    IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                    Hashtable keyList = null;
                    
                    //We select one node at a time for contain operation.
                    while (ide.MoveNext())
                    {
                        targetNode = ide.Key as Address;
                        keyList = (Hashtable)ide.Value;
                        if (targetNode != null) break;
                    }

                    if (targetNode != null)
                    {
                        Hashtable tempTable = new Hashtable();
                        
                        foreach (object key in keyList.Keys)
                        {
                            tempTable.Add(key, table[key]);
                        }

                        try
                        {
                            if (targetNode.Equals(Cluster.LocalAddress))
                            {
                                tmpData = Local_RemoveDepKeyList(tempTable, operationContext);
                            }
                            else
                            {
                                tmpData = Clustered_RemoveDepKeyList(targetNode, tempTable, operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            suspectedErrorOccured = true;
                            Thread.Sleep(_serverFailureWaitTime);
                            //we redo the operation
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.RemoveDepKeyList()", targetNode + " left while updating DependencyKeyList. Error " + se.ToString());
                            continue;
                        }
                        catch (Alachisoft.NGroups.SuspectedException se)
                        {
                            suspectedErrorOccured = true;
                            Thread.Sleep(_serverFailureWaitTime);
                            //we redo the operation
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.RemoveDepKeyList()", targetNode + " left while updating DependencyKeyList. Error " + se.ToString());
                            continue;
                        }
                        catch (Runtime.Exceptions.TimeoutException te)
                        {
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.RemoveDepKeyList()", targetNode + " operation timed out. Error: " + te.ToString());
                            if (suspectedErrorOccured)
                            {
                                suspectedErrorOccured = false;
                            }
                            else
                            {
                                IDictionaryEnumerator den = tempTable.GetEnumerator();
                                while (den.MoveNext())
                                {
                                    totalKeys.Remove(den.Key);
                                }
                            }
                            continue;
                        }

                        //list of items which have been transfered to some other node.
                        //so we need to revisit them.
                        ArrayList remainingKeys = null;

                        if (tmpData != null && tmpData.Count > 0)
                        {
                            remainingKeys = new ArrayList();
                            IDictionaryEnumerator ie = tmpData.GetEnumerator();
                            while (ie.MoveNext())
                            {
                                if (ie.Value is StateTransferException)
                                    remainingKeys.Add(ie.Key);
                                else
                                {
                                    totalKeys.Remove(ie.Key);
                                    result[ie.Key] = ie.Value;
                                }
                            }
                        }
                        else
                        {
                            if (keyList != null)
                            {
                                foreach (object key in keyList.Keys)
                                {
                                    totalKeys.Remove(key);
                                }
                            }
                        }

                        if (remainingKeys != null && remainingKeys.Count > 0)
                        {
                            _distributionMgr.Wait(remainingKeys[0], null);
                        }
                    }
                }
                return result;
            }
            finally
            {
            }
        }

        private Hashtable Local_RemoveDepKeyList(Hashtable table, OperationContext operationContext)
        {
            Hashtable retVal = null;
            if (_internalCache != null)
            {
                retVal = _internalCache.RemoveDepKeyList(table, operationContext);
            }
            return retVal;
        }

        private Hashtable handleRemoveDepKeyList(object info)
        {
            try
            {
                object[] objs = (object[])info;
                OperationContext oc = objs[1] as OperationContext;
                return Local_RemoveDepKeyList((Hashtable)objs[0], oc);
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        #endregion



        #region	/                 --- Partitioned ICache.Count ---           /

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        public override long Count
        {
            get
            {
                if (_internalCache == null) throw new InvalidOperationException();
                long count = 0;
                if (IsInStateTransfer())
                {
                    count = Clustered_Count(GetDestInStateTransfer());
                }
                else
                {
                    count = Clustered_Count(this.Servers);
                }
                return count;
            }
        }

       

        /// <summary>
        /// Hanlde cluster-wide Get(key) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleCount()
        {
            try
            {
                return Local_Count();
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return 0;
        }




        #endregion

        #region	/                 --- Partitioned ICache.Contains ---           /

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        public override bool Contains(object key, string group, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Cont", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            bool suspectedErrorOccured = false;
            Address node = null;

            while (true)
            {
                try
                {
                    if (_internalCache == null) throw new InvalidOperationException();

                    node = GetNextNode(key as string, group);
                    if (node != null)
                    {
                        if (node.CompareTo(Cluster.LocalAddress) == 0)
                        {
                            return Local_Contains(key, operationContext);
                        }
                        else
                        {
                            return Clustered_Contains(node, key, operationContext) != null;

                        }
                    }
                    return false;
                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    Thread.Sleep(_serverFailureWaitTime);
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Contains", node + " left while Contains. Error: " + se.ToString());
                    continue;
                }
                catch (Alachisoft.NGroups.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    Thread.Sleep(_serverFailureWaitTime);
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Contains", node + " left while Contains. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Contains", node + " operation timed out. Error: " + te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                    {
                        throw new GeneralFailureException(te.Message, te);
                    }
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, group);
                }
                finally
                {
                }
            }
        }

        /// <summary>
        /// Determines whether the cache contains the given keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>list of available keys from the given key list</returns>
        public override Hashtable Contains(object[] keys, string group, OperationContext operationContext)
        {
            Hashtable targetNodes = null;
            Hashtable result = new Hashtable();
            Hashtable tmpKeyTbl = null;
            Address targetNode = null;

            ArrayList totalFoundKeys = new ArrayList();
            ArrayList totalRremainingKeys = new ArrayList();
            ClusteredArrayList totalKeys = new ClusteredArrayList(keys);

            do
            {
                targetNodes = (Hashtable)GetTargetNodes(totalKeys, group);

                IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                Hashtable keyList = null;
                //We select one node at a time for contain operation.
                while (ide.MoveNext())
                {
                    targetNode = ide.Key as Address;
                    keyList = (Hashtable)ide.Value;

                    if (targetNode != null)
                    {
                        object[] currentKeys = MiscUtil.GetArrayFromCollection(keyList.Keys);

                        try
                        {
                            if (targetNode != null)
                            {
                                if (targetNode.Equals(Cluster.LocalAddress))
                                    tmpKeyTbl = Local_Contains(currentKeys, operationContext);
                                else
                                    tmpKeyTbl = Clustered_Contains(targetNode, currentKeys, operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            totalRremainingKeys.AddRange(currentKeys);
                            Thread.Sleep(_serverFailureWaitTime);
                            //we redo the operation
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PoRServerCache.Contains", targetNode + " left while addition");
                            continue;
                        }
                        catch (Alachisoft.NGroups.SuspectedException se)
                        {
                            totalRremainingKeys.AddRange(currentKeys);
                            Thread.Sleep(_serverFailureWaitTime);
                            //we redo the operation
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PoRServerCache.Contains", targetNode + " left while addition");
                            continue;
                        }
                        catch (Alachisoft.NCache.Common.Exceptions.TimeoutException te)
                        {
                            totalRremainingKeys.AddRange(currentKeys);

                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PoRServerCache.Contains", targetNode + " operation timed out");
                            continue;
                        }

                        if (tmpKeyTbl != null)
                        {
                            //list of items which have been transfered to some other node.
                            //so we need to revisit them.
                            ArrayList transferredKeys = tmpKeyTbl["items-transfered"] as ArrayList;
                            if (transferredKeys != null && transferredKeys.Count > 0)
                            {
                                totalRremainingKeys.AddRange(transferredKeys);
                            }

                            ArrayList foundKeys = tmpKeyTbl["items-found"] as ArrayList;
                            if (foundKeys != null && foundKeys.Count > 0)
                            {
                                totalFoundKeys.AddRange(foundKeys);
                            }
                        }
                    }
                }

                totalKeys = new ClusteredArrayList(totalRremainingKeys);
                totalRremainingKeys.Clear();

            }
            while (totalKeys.Count > 0);

            result["items-found"] = totalFoundKeys;

            return result;
        }

        /// <summary>
        /// Determines whether the local cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        private bool Local_Contains(object key, OperationContext operationContext)
        {
            bool retVal = false;
            if (_internalCache != null)
                retVal = _internalCache.Contains(key, operationContext);
            return retVal;
        }

        /// <summary>
        /// Determines whether the local cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        private Hashtable Local_Contains(object[] keys, OperationContext operationContext)
        {
            Hashtable retVal = new Hashtable();
            if (_internalCache != null)
                retVal = _internalCache.Contains(keys, operationContext);
            return retVal;
        }

        /// <summary>
        /// Hanlde cluster-wide Get(key) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleContains(object info)
        {
            try
            {
                OperationContext operationContext = null;
                object[] args = (object[])info;
                if (args.Length > 1)
                    operationContext = args[1] as OperationContext;
                if (args[0] is object[])
                {
                    return Local_Contains((object[])args[0], operationContext);
                }
                else
                {
                    if (Local_Contains(args[0], operationContext))
                        return true;
                }
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        #endregion

        #region	/                 --- Partitioned ICache.Clear ---           /

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        /// <remarks>
        /// This method invokes <see cref="handleClear"/> on every node in the partition, 
        /// which then fires OnCacheCleared locally. The <see cref="handleClear"/> method on the 
        /// coordinator will also trigger a cluster-wide notification to the clients.
        /// </remarks>
        public override void Clear(CallbackEntry cbEntry, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
        {
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            string taskId = null;
            if (updateOptions == DataSourceUpdateOptions.WriteBehind)
                taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

            if (Servers.Count > 1)
            {
                Clustered_Clear(cbEntry, taskId, false, operationContext);
            }
            else
            {
                handleClear(Cluster.LocalAddress, new object[] { cbEntry, taskId, operationContext });
            }

        }

        /// <summary>
        /// Removes all entries from the local cache only.
        /// </summary>
        private void Local_Clear(Address src, CallbackEntry cbEntry, string taskId, OperationContext operationContext)
        {
            ClearCQManager();            
            if (_internalCache != null)
            {
                _internalCache.Clear(null, DataSourceUpdateOptions.None, operationContext);
                if (taskId != null)
                {
                    CacheEntry entry = new CacheEntry(cbEntry, null, null);
                    if (operationContext.Contains(OperationContextFieldName.ReadThruProviderName))
                    {
                        entry.ProviderName = (string)operationContext.GetValueByField(OperationContextFieldName.ReadThruProviderName);
                    }
                    base.AddWriteBehindTask(src, null, entry, taskId, OpCode.Clear, WriteBehindAsyncProcessor.TaskState.Execute, operationContext);
                }
            }
        }

        private void AbortStateTransfer()
        {
            //Check if this node is involved in any kind of state transfer.
            if (_stateTransferTask != null && _stateTransferTask.IsRunning)
                _stateTransferTask.Stop();
            if (_corresponders != null && _corresponders.Count > 0)
                _corresponders.Clear();
        }

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        /// <remarks>
        /// <see cref="handleClear"/> is called on every server node in a cluster. Therefore in order to ensure 
        /// that only one notification is generated for the cluster, only the coordinator node 
        /// replicates the notification to the clients. 
        /// <para>
        /// <b>Note: </b> The notification to the servers is handled in their <see cref="OnCacheCleared"/> callback.
        /// Therefore the servers generate the notifications locally. Only the clients need to be 
        /// notified.
        /// </para>
        /// </remarks>
        private object handleClear(Address src, object operand)
        {
            try
            {
                object[] args = operand as object[];
                CallbackEntry cbEntry = null;
                string taskId = null;
                OperationContext operationContext = null;

                if (args.Length > 0)
                    cbEntry = args[0] as CallbackEntry;
                if (args.Length > 1)
                    taskId = args[1] as string;
                if (args.Length > 2)
                    operationContext = args[2] as OperationContext;

                Local_Clear(src, cbEntry, taskId, operationContext);
                /// Only the coordinator replicates notification.
                if (IsCacheClearNotifier && Cluster.IsCoordinator &&
                    (ValidMembers.Count - Servers.Count) > 1)
                {
                    /// Send notification to clients only, because this operation is
                    /// performed by all the servers.
                    Cluster.SendNoReplyMessageAsync(new Function((int)OpCodes.NotifyClear, null));
                }
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        #endregion

        #region /                 --- Partitioned Search ---               /

        protected override QueryResultSet Local_Search(string query, IDictionary values, OperationContext operationContext)
        {
            return _internalCache.Search(query, values, operationContext);
        }

        protected override QueryResultSet Local_SearchEntries(string query, IDictionary values, OperationContext operationContext)
        {
            return _internalCache.SearchEntries(query, values, operationContext);
        }

        protected override QueryResultSet Local_SearchCQ(string cmdText, IDictionary values, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters)
        {
            QueryResultSet resultSet = new QueryResultSet();
            ContinuousQuery query = CQManager.GetCQ(cmdText, values);
            if (CQManager.Exists(query))
            {
                CQManager.Update(query, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, datafilters);
                resultSet = _internalCache.SearchCQ(query.UniqueId, operationContext);
                resultSet.CQUniqueId = query.UniqueId;
            }
            else
            {
                if (!Cluster.IsCoordinator)
                {
                    query.UniqueId = "-1";
                }

                resultSet = _internalCache.SearchCQ(query, operationContext);

                if (Cluster.IsCoordinator)
                {
                    CQManager.Register(query, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, datafilters);
                    Cluster_RegisterSearchCQ(Cluster.Servers, query, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, false, operationContext, datafilters);
                    resultSet.CQUniqueId = query.UniqueId;
                }
            }
            return resultSet;
        }

        protected override QueryResultSet Local_SearchEntriesCQ(string cmdText, IDictionary values, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters)
        {
            QueryResultSet resultSet = new QueryResultSet();
            ContinuousQuery query = CQManager.GetCQ(cmdText, values);
            if (CQManager.Exists(query))
            {
                CQManager.Update(query, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, datafilters);
                resultSet = _internalCache.SearchEntriesCQ(query.UniqueId, operationContext);
                resultSet.CQUniqueId = query.UniqueId;
            }
            else
            {
                if (!Cluster.IsCoordinator)
                {
                    query.UniqueId = "-1";
                }
                resultSet = _internalCache.SearchEntriesCQ(query, operationContext);
                if (Cluster.IsCoordinator)
                {
                    CQManager.Register(query, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, datafilters);
                    Cluster_RegisterSearchEnteriesCQ(Cluster.Servers, query, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, false, operationContext, datafilters);
                    resultSet.CQUniqueId = query.UniqueId;
                }
            }
            return resultSet;
        }

        protected void Cluster_RegisterSearchCQ(ArrayList dests, ContinuousQuery query, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, bool excludeSelf, OperationContext operationContext, QueryDataFilters datafilters)
        {
            try
            {
                Function func = new Function((int)OpCodes.SearchCQ, new object[] { query, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters }, excludeSelf);
                RspList results = Cluster.BroadcastToMultiple(dests, func, GroupRequest.GET_ALL, false);
                ClusterHelper.ValidateResponses(results, null, Name);
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

        protected void Clustered_RegisterPollingNotification(short callbackId, OperationContext context)
        {
            try
            {
                if (Cluster.Servers.Count == 1 && Cluster.Servers[0].ToString().Contains(LocalAddress.IpAddress.ToString()))
                    Local_RegisterPollingNotification(callbackId, context);

                Clustered_RegisterPollingNotification(Cluster.Servers, callbackId, context, false);
            }
            catch(Exception)
            {
                Clustered_RegisterPollingNotification(Cluster.Servers, callbackId, context, false);
            }
        }
      
        protected void Clustered_RegisterPollingNotification(ArrayList servers, short callbackId, OperationContext context, bool excludeSelf)
        {
            try
            {
                Function func = new Function((int)OpCodes.RegisterPollingNotification, new object[] { callbackId, context }, excludeSelf);
                RspList results = Cluster.BroadcastToMultiple(Servers, func, GroupRequest.GET_ALL, false);
                ClusterHelper.ValidateResponses(results, null, Name);
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

        protected void Cluster_RegisterSearchEnteriesCQ(ArrayList dests, ContinuousQuery query, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, bool excludeSelf, OperationContext operationContext, QueryDataFilters datafilters)
        {
            try
            {
                Function func = new Function((int)OpCodes.SearchEntriesCQ, new object[] { query, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters }, excludeSelf);
                RspList results = Cluster.BroadcastToMultiple(dests, func, GroupRequest.GET_ALL, false);
                ClusterHelper.ValidateResponses(results, null, Name);
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

        public QueryResultSet handleSearch(object info)
        {
            if (_internalCache != null)
            {
                ArrayList keyList = new ArrayList();
                object[] data = (object[])info;
                return _internalCache.Search(data[0] as string, data[1] as IDictionary, data[2] as OperationContext);                
            }

            return null;
        }

        public QueryResultSet handleSearchEntries(object info)
        {
            if (_internalCache != null)
            {
                Hashtable keyValues = new Hashtable();
                object[] data = (object[])info;
                return _internalCache.SearchEntries(data[0] as string, data[1] as IDictionary, data[2] as OperationContext);
            }

            return null;
        }

        #endregion

        #region	/                 --- Partitioned ICache.Get ---           /

        /// <summary>
        /// Retrieve the object from the cache if found in a specified group and subgroup.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="group">group name.</param>
        /// <param name="subGroup">subgroup name.</param>
        /// <returns>cache entry.</returns>
        public override CacheEntry GetGroup(object key, string group, string subGroup, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            CacheEntry e = null;
            bool suspectedErrorOccured = false;
            Address address = null;

            while (true)
            {
                address = GetNextNode(key as string, group);

                if (address == null)
                {
                    NCacheLog.Error("PartitionedServerCache.GetGroup(): ", "specified key does not map to any node. return.");
                    return null;
                }

                try
                {
                    if (address.CompareTo(Cluster.LocalAddress) == 0)
                    {
                        e = Local_GetGroup(key, group, subGroup, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
                    }
                    else
                    {
                        e = Clustered_GetGroup(address, key, group, subGroup, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
                    }

                    if (e == null)
                    {
                        _stats.BumpMissCount();
                    }
                    else
                    {
                        _stats.BumpHitCount();
                    }
                    return e;
                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    Thread.Sleep(_serverFailureWaitTime);
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.GetGroup", address + " left while addition: " + se.ToString());
                    continue;
                }
                catch (Alachisoft.NGroups.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    Thread.Sleep(_serverFailureWaitTime);
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.GetGroup", address + " left while addition: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.GetGroup", address + " operation timed out: " + te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                    {
                        throw new GeneralFailureException(te.Message, te);
                    }
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, group);
                }
            }
            return e;
        }

        protected override HashVector Local_GetTagData(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            return _internalCache.GetTagData(tags, comparisonType, operationContext);
        }

        public HashVector handleGetTag(object info)
        {
            if (_internalCache != null)
            {
                object[] data = (object[])info;
                HashVector keyValues = _internalCache.GetTagData(data[0] as string[], (TagComparisonType)data[1], data[2] as OperationContext);
                return keyValues;
            }

            return null;
        }

  
        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        public override CacheEntry Get(object key, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType access, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Get", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            Address address = null;
            CacheEntry e = null;
            bool suspectedErrorOccured = false;

            while (true)
            {
                address = GetNextNode(key as string, null);

                if (address == null)
                {
                    NCacheLog.Error("PartitionedServerCache.Get()", "specified key does not map to any node. return.");
                    return null;
                }

                try
                {
                    if (address.CompareTo(Cluster.LocalAddress) == 0)
                    {
                        e = Local_Get(key, ref version, ref lockId, ref lockDate, lockExpiration, access, operationContext);
                    }
                    else
                    {
                        e = Clustered_Get(address, key, ref version, ref lockId, ref lockDate, lockExpiration, access, operationContext);
                    }

                    if (e == null)
                    {
                        _stats.BumpMissCount();
                    }
                    else
                    {
                        _stats.BumpHitCount();
                    }
                    break;
                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    Thread.Sleep(_serverFailureWaitTime);
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Get", address + " left while Get. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Get", address + " operation timed out. Error: " + te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                    {
                        throw new GeneralFailureException(te.Message, te);
                    }
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {                  
                    _distributionMgr.Wait(key, null);
                }
            }
            return e;

        }


        private HashVector OptimizedGet(object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.GetBlk", "");

            HashVector result = new HashVector();

            ClusteredArrayList remainingKeys = new ClusteredArrayList();

            if (_internalCache == null)
                throw new InvalidOperationException();

            result = (HashVector)Local_Get(keys, operationContext);

            if (result != null && result.Count > 0)
            {
                HashVector resultClone = (HashVector)result.Clone();

                IDictionaryEnumerator ie = resultClone.GetEnumerator();
                while (ie.MoveNext())
                {
                    if (ie.Value is StateTransferException)
                    {
                        remainingKeys.Add(ie.Key);
                        result.Remove(ie.Key); // do remove from result
                    }
                }
            }

            IDictionaryEnumerator ine = result.GetEnumerator();
            ClusteredArrayList updateIndiceKeyList = null;
            while (ine.MoveNext())
            {
                CacheEntry e = ine.Value as CacheEntry;
                if (e == null)
                {
                    _stats.BumpMissCount();
                }
                else
                {
                    if (updateIndiceKeyList == null)
                        updateIndiceKeyList = new ClusteredArrayList();

                    _stats.BumpHitCount();

                    if ((e.ExpirationHint != null && e.ExpirationHint.IsVariant))
                    {
                        updateIndiceKeyList.Add(ine.Key);
                    }
                }
            }

            if (updateIndiceKeyList != null && updateIndiceKeyList.Count > 0)
            {
                UpdateIndices(updateIndiceKeyList.ToArray(), true, operationContext);
            }

            if (remainingKeys.Count > 0)
            {
                HashVector tmpResult = ClusteredGet(remainingKeys.ToArray(), operationContext);
                foreach (DictionaryEntry entry in tmpResult)
                {
                    result[entry.Key] = entry.Value;
                }
            }

            return result;
        }

        private HashVector ClusteredGet(object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.GetBlk", "");

            HashVector result = new HashVector();
            Hashtable targetNodes = null;
            HashVector tmpData = null;

            ClusteredArrayList totalKeys = new ClusteredArrayList(keys);
            ClusteredArrayList totalRemainingKeys = new ClusteredArrayList();

            Address targetNode = null;

            if (_internalCache == null)
                throw new InvalidOperationException();

            do
            {
                targetNodes = (Hashtable)GetTargetNodes(totalKeys, null);

                IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                Hashtable keyList = null;
                //We select one node at a time for operation.
                while (ide.MoveNext())
                {
                    targetNode = ide.Key as Address;
                    keyList = (Hashtable)ide.Value;

                    if (targetNode != null)
                    {
                        object[] currentKeys = MiscUtil.GetArrayFromCollection(keyList.Keys);
                        try
                        {
                            if (targetNode.Equals(Cluster.LocalAddress))
                                tmpData = (HashVector)Local_Get(currentKeys, operationContext);
                            else
                            {
                                tmpData = Clustered_Get(targetNode, currentKeys, operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            //we redo the operation
                            Thread.Sleep(_serverFailureWaitTime);
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PoRServerCache.Contains", targetNode + " left while addition");
                            totalRemainingKeys.Add(currentKeys);
                            continue;
                        }
                        catch (Runtime.Exceptions.TimeoutException te)
                        {
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PoRServerCache.Contains", targetNode + " operation timed out");
                            totalRemainingKeys.Add(currentKeys);
                            continue;
                        }

                        if (tmpData != null && tmpData.Count > 0)
                        {
                            IDictionaryEnumerator ie = tmpData.GetEnumerator();
                            while (ie.MoveNext())
                            {
                                if (ie.Value is StateTransferException)
                                    totalRemainingKeys.Add(ie.Key);
                                else
                                {
                                    result[ie.Key] = ie.Value;
                                }
                            }
                        }
                    }
                }

                totalKeys = new ClusteredArrayList(totalRemainingKeys);
                totalRemainingKeys.Clear();
            }
            while (totalKeys.Count > 0);

            IDictionaryEnumerator ine = result.GetEnumerator();
            ClusteredArrayList updateIndiceKeyList = null;
            while (ine.MoveNext())
            {
                CacheEntry e = (CacheEntry)ine.Value;
                if (e == null)
                {
                    _stats.BumpMissCount();
                }
                else
                {
                    if (updateIndiceKeyList == null) updateIndiceKeyList = new ClusteredArrayList();
                    _stats.BumpHitCount();
                    if ((e.ExpirationHint != null && e.ExpirationHint.IsVariant))
                    {
                        updateIndiceKeyList.Add(ine.Key);
                    }

                }
            }
            if (updateIndiceKeyList != null && updateIndiceKeyList.Count > 0)
            {
                UpdateIndices(updateIndiceKeyList.ToArray(), true, operationContext);
            }

            return result;
        } 

        /// <summary>
        /// Retrieve the objects from the cache. An array of keys is passed as parameter.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        public override IDictionary Get(object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.GetBlk", "");

            HashVector result = null;

            if (_internalCache == null)
                throw new InvalidOperationException();

            long clientLastViewId = GetClientLastViewId(operationContext);

            if (clientLastViewId == Cluster.LastViewID && !IsInStateTransfer())
            {
                result = OptimizedGet(keys, operationContext);
            }
            else
            {
                result = ClusteredGet(keys, operationContext);
            }

            return result;
        }

        /// <summary>
        /// Retrieve the objects from the cache. An array of keys is passed as parameter.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        public override Hashtable GetGroup(object[] keys, string group, string subGroup, OperationContext operationContext)
        {
            Hashtable targetNodes = null;
            ArrayList contains = new ArrayList();
            Hashtable result = new Hashtable();
            Hashtable tmpData = null;
            Hashtable totalKeys = null;
            Address targetNode = null;
            bool suspectedErrorOccured = false;

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            try
            {
                if (_internalCache == null) throw new InvalidOperationException();

                totalKeys = new Hashtable();
                for (int i = 0; i < keys.Length; i++)
                {
                    totalKeys.Add(keys[i], null);
                }

                while (totalKeys.Count > 0)
                {
                    ClusteredArrayList _keys = new ClusteredArrayList(totalKeys.Keys);

                    targetNodes = (Hashtable)GetTargetNodes(_keys, group);

                    IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                    Hashtable keyList = null;
                    //We select one node at a time for contain operation.
                    while (ide.MoveNext())
                    {
                        targetNode = ide.Key as Address;
                        keyList = (Hashtable)ide.Value;
                        if (targetNode != null) break;
                    }

                    if (targetNode != null)
                    {
                        object[] currentKeys = new object[keyList.Count];
                        int j = 0;
                        foreach (object key in keyList.Keys)
                        {
                            currentKeys[j] = key;
                            j++;
                        }

                        try
                        {
                            if (targetNode.CompareTo(LocalAddress) == 0)
                            {
                                tmpData = Local_GetGroup(currentKeys, group, subGroup, operationContext);
                            }
                            else
                            {
                                tmpData = Clustered_GetGroup(targetNode, currentKeys, group, subGroup, operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            suspectedErrorOccured = true;
                            Thread.Sleep(_serverFailureWaitTime);
                            //we redo the operation
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.GetGroup", targetNode + " left while GetGroup. Error: " + se.ToString());
                            continue;
                        }
                        catch (Alachisoft.NGroups.SuspectedException se)
                        {
                            suspectedErrorOccured = true;
                            Thread.Sleep(_serverFailureWaitTime);
                            //we redo the operation
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.GetGroup", targetNode + " left while GetGroup. Error: " + se.ToString());
                            continue;
                        }
                        catch (Runtime.Exceptions.TimeoutException te)
                        {
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.GetGroup", targetNode + " operation timed out. Error: " + te.ToString());
                            if (suspectedErrorOccured)
                            {
                                suspectedErrorOccured = false;
                            }
                            else
                            {
                                for (int i = 0; i < currentKeys.Length; i++)
                                {
                                    totalKeys.Remove(currentKeys[i]);
                                }
                            }
                            continue;
                        }

                        //list of items which have been transfered to some other node.
                        //so we need to revisit them.
                        ArrayList remainingKeys = null;

                        if (tmpData != null && tmpData.Count > 0)
                        {
                            remainingKeys = new ArrayList();
                            IDictionaryEnumerator ie = tmpData.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                if (ide.Value is StateTransferException)
                                    remainingKeys.Add(ie.Key);
                                else
                                {
                                    totalKeys.Remove(ie.Key);
                                    result[ide.Key] = ie.Value;
                                }
                            }
                        }

                        if (remainingKeys != null && remainingKeys.Count > 0)
                        {
                            _distributionMgr.Wait(remainingKeys[0], group);
                        }
                    }
                }
                return result;
            }
            finally
            {
            }
        }


        /// <summary>
        /// Retrieve the object from the local cache only. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        internal CacheEntry Local_Get(object key, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            if (_internalCache != null)
                retVal = _internalCache.Get(key, operationContext);
            return retVal;
        }

        /// <summary>
        /// Retrieve the object from the local cache only. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheEntry Local_Get(object key, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType access, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            if (_internalCache != null)
                retVal = _internalCache.Get(key, ref version, ref lockId, ref lockDate, lockExpiration, access, operationContext);
            return retVal;
        }

        /// <summary>
        /// Retrieve the objects from the local cache only. 
        /// </summary>
        /// <param name="keys">keys of the entry.</param>
        /// <returns>cache entries.</returns>
        private IDictionary Local_Get(object[] keys, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.Get(keys, operationContext);

            return null;
        }

        /// <summary>
        /// Retrieve the list of keys fron the cache for the given group or sub group.
        /// </summary>
        private CacheEntry Local_GetGroup(object key, string group, string subGroup, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.GetGroup(key, group, subGroup, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);

            return null;
        }

        protected override ICollection Local_GetTagKeys(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.GetTagKeys(tags, comparisonType, operationContext);

            return null;
        }
        /// <summary>
        /// Retrieve the list of keys fron the cache for the given group or sub group.
        /// </summary>
        private Hashtable Local_GetGroup(object[] keys, string group, string subGroup, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.GetGroup(keys, group, subGroup, operationContext);

            return null;
        }

        /// <summary>
        /// Retrieve the list of keys fron the cache for the given group or sub group.
        /// </summary>
        protected override ArrayList Local_GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.GetGroupKeys(group, subGroup, operationContext);

            return null;
        }


        /// <summary>
        /// Retrieve the list of keys fron the cache for the given group or sub group.
        /// </summary>
        protected override HashVector Local_GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.GetGroupData(group, subGroup, operationContext);

            return null;
        }

        /// <summary>
        /// Hanlde cluster-wide Get(key) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleGet(object info)
        {
            try
            {
                object[] args = info as object[];
                if (args[0] is object[])
                {
                    return Local_Get((object[])args[0], args[1] as OperationContext);
                }
                else
                {
                    object key = args[0];
                    object lockId = args[1];
                    DateTime lockDate = (DateTime)args[2];
                    LockAccessType accessType = (LockAccessType)args[3];
                    ulong version = (ulong)args[4];
                    LockExpiration lockExpiration = (LockExpiration)args[5];
                    OperationContext operationContext = args[6] as OperationContext;

                    CacheEntry entry = Local_Get(key, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
                    OperationResponse opRes = new OperationResponse();
                    object[] response = new object[4];
                    if (entry != null)
                    {
                        if (_context.InMemoryDataFormat.Equals(DataFormat.Binary))
                        {
                            UserBinaryObject ubObject = (UserBinaryObject)(entry.Value is CallbackEntry ? ((CallbackEntry)entry.Value).Value : entry.Value);
                            opRes.UserPayload = ubObject.Data;
                            response[0] = entry.CloneWithoutValue();
                        }
                        else
                        {
                            opRes.UserPayload = null;
                            response[0] = entry.Clone();
                        }
                    }
                    response[1] = lockId;
                    response[2] = lockDate;
                    response[3] = version;
                    opRes.SerializablePayload = response;

                    return opRes;
                }
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        /// <summary>
        /// Hanlde cluster-wide Get(key) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleGetGroup(object info)
        {
            try
            {
                object[] package = (object[])info;
                string group = (string)package[1];
                string subGroup = (string)package[2];
                object lockId = package[3];
                DateTime lockDate = (DateTime)package[4];
                LockAccessType accessType = (LockAccessType)package[5];
                ulong version = (ulong)package[6];
                LockExpiration lockExpiration = (LockExpiration)package[7];
                OperationContext operationContext = null;

                if (package.Length > 4)
                    operationContext = package[8] as OperationContext;
                else
                    operationContext = package[3] as OperationContext;

                if (package[0] is object[])
                {
                    object[] keys = (object[])package[0];

                    return Local_GetGroup(keys, group, subGroup, operationContext);
                }
                else
                {
                    object key = package[0];              
                    OperationResponse opRes = new OperationResponse();
                    object[] response = new object[4];

                    CacheEntry entry = Local_GetGroup(key, group, subGroup, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
                    if (entry != null)
                    {
                        UserBinaryObject ubObject = (UserBinaryObject)(entry.Value is CallbackEntry ? ((CallbackEntry)entry.Value).Value : entry.Value);
                        opRes.UserPayload = ubObject.Data;
                        response[0] = entry.Clone();
                    }
                    response[1] = lockId;
                    response[2] = lockDate;
                    response[3] = version;
                    opRes.SerializablePayload = response;

                    return opRes;
                }
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }
        #endregion

        #region	/                 --- Partitioned ICache.Add ---           /

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleAdd"/> on every server-node in the cluster, 
        /// or invokes <see cref="Local_Add"/> locally. <see cref="Local_Add"/> can only be called on 
        /// one node in the cluster, therefore it triggers a cluster-wide item added notification.
        /// </remarks>
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Add_1", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            Address targetNode = null;
            string taskId = null;
            CacheAddResult result = CacheAddResult.Failure;  
            try
            {
                if (_internalCache == null) throw new InvalidOperationException();

                if (cacheEntry.Flag != null && cacheEntry.Flag.IsBitSet(BitSetConstants.WriteBehind))
                    taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

                #region -- PART I -- Cascading Dependency Operation
                object[] keys = cacheEntry.KeysIAmDependingOn;
                if (keys != null)
                {
                    Hashtable goodKeysTable = Contains(keys, operationContext);

                    if (!goodKeysTable.ContainsKey("items-found"))
                        throw new OperationFailedException("One of the dependency keys does not exist.", false);

                    if (goodKeysTable["items-found"] == null)
                        throw new OperationFailedException("One of the dependency keys does not exist.", false);

                    if (goodKeysTable["items-found"] == null || ((ArrayList)goodKeysTable["items-found"]).Count != keys.Length)
                        throw new OperationFailedException("One of the dependency keys does not exist.", false);

                }
                #endregion

                result = Safe_Clustered_Add(key, cacheEntry, out targetNode, taskId, operationContext);

                #region -- PART II -- Cascading Dependency Operation
                if (result == CacheAddResult.Success)
                {
                    Hashtable ret = null;
                    Hashtable keysTable = new Hashtable();
                    try
                    {
                        keysTable = GetKeysTable(key, cacheEntry.KeysIAmDependingOn);
                        if (keysTable != null)
                        {
                            //Fix for NCache Bug4981
                            object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);
                            if (generateQueryInfo == null)
                            {
                                operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                            }

                            ret = AddDepKeyList(keysTable, operationContext);

                            if (generateQueryInfo == null)
                            {
                                operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                            }
                        
                        }
                    }
                    catch (Exception e)
                    {

                        Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                        throw e;
                    }
                    if (ret != null)
                    {
                        IDictionaryEnumerator en = ret.GetEnumerator();
                        while (en.MoveNext())
                        {
                            if (en.Value is bool && !((bool)en.Value))
                            {
                                Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                                NCacheLog.Info("PartitionedServerCache.Add(): ", "One of the dependency keys does not exist. Key: " + en.Key.ToString());
                                throw new OperationFailedException("One of the dependency keys does not exist.", false);
                            }
                        }
                    }
                }
                #endregion

                return result;
            }
            finally
            {
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public override bool Add(object key, string group, ExpirationHint eh, OperationContext operationContext)
        {
            bool result = false;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Add_3", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            #region -- PART I -- Cascading Dependency Operation
            CacheEntry cacheEntry = new CacheEntry();
            cacheEntry.ExpirationHint = eh;

            object[] keys = cacheEntry.KeysIAmDependingOn;
            if (keys != null)
            {
                Hashtable goodKeysTable = Contains(keys, operationContext);

                if (!goodKeysTable.ContainsKey("items-found"))
                    throw new OperationFailedException("One of the dependency keys does not exist.", false);

                if (goodKeysTable["items-found"] == null)
                    throw new OperationFailedException("One of the dependency keys does not exist.", false);

                if (goodKeysTable["items-found"] == null || ((ArrayList)goodKeysTable["items-found"]).Count != keys.Length)
                    throw new OperationFailedException("One of the dependency keys does not exist.", false);
            }
            #endregion

            bool suspectedErrorOccured = false;
            Address targetNode = null;

            while (true)
            {
                try
                {
                    targetNode = GetNextNode(key as string, group);
                    if (targetNode != null)
                    {
                        if (targetNode.compare(LocalAddress) == 0)
                        {
                            result = Local_Add(key, eh, operationContext);
                            break;
                        }
                        else
                        {
                            result = Clustered_Add(targetNode, key, eh, operationContext);
                            break;
                        }
                    }
                    result = false;
                    break;
                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    Thread.Sleep(_serverFailureWaitTime);
                    suspectedErrorOccured = true;
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Add", targetNode + " left while addition. Error: " + se.ToString());
                    continue;
                }
                catch (Alachisoft.NGroups.SuspectedException se)
                {
                    Thread.Sleep(_serverFailureWaitTime);
                    suspectedErrorOccured = true;
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Add", targetNode + " left while addition. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Add", targetNode + " operation timed out. Error: " + te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                    {
                        throw new GeneralFailureException(te.Message, te);
                    }
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, group);
                }

            }

            #region -- PART II -- Cascading Dependency Operation
            if (result)
            {
                Hashtable ret = null;
                Hashtable keysTable = new Hashtable();
                try
                {
                    keysTable = GetKeysTable(key, cacheEntry.KeysIAmDependingOn);
                    if (keysTable != null)
                        ret = AddDepKeyList(keysTable, operationContext);
                }
                catch (Exception e)
                {

                    Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                    throw e;
                }
                if (ret != null)
                {
                    IDictionaryEnumerator en = ret.GetEnumerator();
                    while (en.MoveNext())
                    {
                        if (en.Value is bool && !((bool)en.Value))
                        {
                            Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                            NCacheLog.Info("PartitionedServerCache.Add(): ", "One of the dependency keys does not exist. Key: " + en.Key.ToString());
                            throw new OperationFailedException("One of the dependency keys does not exist.", false);
                        }
                    }
                }
            }
            #endregion

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public override bool Add(object key, string group, CacheSynchronization.CacheSyncDependency syncDependency, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Add_2", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            bool suspectedErrorOccured = false;
            Address targetNode = null;

            while (true)
            {
                try
                {
                    targetNode = GetNextNode(key as string, group);
                    if (targetNode.CompareTo(LocalAddress) == 0)
                    {
                        return Local_Add(key, syncDependency, operationContext);
                    }
                    else
                    {
                        return Clustered_Add(targetNode, key, syncDependency, operationContext);
                    }
                    return false;
                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    Thread.Sleep(_serverFailureWaitTime);
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Add", targetNode + " left while addition. Error: " + se.ToString());
                    continue;
                }
                catch (Alachisoft.NGroups.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    Thread.Sleep(_serverFailureWaitTime);
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Add", targetNode + " left while addition. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Add", targetNode + " operation timed out. Error: " + te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                    {
                        throw new GeneralFailureException(te.Message, te);
                    }
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, group);
                }
            }
        }


        /// <summary>
        /// Add the object to the local cache. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method can only be called on one node in the cluster. It triggers <see cref="OnItemAdded"/>,
        /// which initiates a cluster-wide item added notification.
        /// </remarks>
        private CacheAddResult Local_Add(object key, CacheEntry cacheEntry, Address src, string taskId, bool notify, OperationContext operationContext)
        {
            CacheAddResult retVal = CacheAddResult.Failure;

            CacheEntry clone = null;
            if (taskId != null && cacheEntry.HasQueryInfo)
                clone = (CacheEntry)cacheEntry.Clone();
            else
                clone = cacheEntry;

            if (_internalCache != null)
            {
                retVal = _internalCache.Add(key, cacheEntry, notify, operationContext);

                if (taskId != null && retVal == CacheAddResult.Success)
                {
                    base.AddWriteBehindTask(src, key as string, clone, taskId, OpCode.Add, WriteBehindAsyncProcessor.TaskState.Execute, operationContext);
                }
            }

            return retVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        private bool Local_Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
            bool retVal = false;
            if (_internalCache != null)
                retVal = _internalCache.Add(key, eh, operationContext);
            return retVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        private bool Local_Add(object key, CacheSynchronization.CacheSyncDependency syncDependency, OperationContext operationContext)
        {
            bool retVal = false;
            if (_internalCache != null)
                retVal = _internalCache.Add(key, syncDependency, operationContext);
            return retVal;
        }

        /// <summary>
        /// A wrapper method that reperform the operations that fail because
        /// of the members suspected during operations.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cacheEntry"></param>
        /// <returns></returns>
        private CacheAddResult Safe_Clustered_Add(object key, CacheEntry cacheEntry, out Address targetNode, string taskId, OperationContext operationContext)
        {
            bool suspectedErrorOccured = false;
            int maxTries = _stats.Nodes.Count > 3 ? 3 : _stats.Nodes.Count - 1;
            CacheAddResult result = CacheAddResult.Failure;
            targetNode = null;
            do
            {
                string group = cacheEntry.GroupInfo == null ? null : cacheEntry.GroupInfo.Group;

                try
                {
                    targetNode = GetNextNode(key as string, group);
                     
                    //possible in case of strict affinity...
                    if (targetNode == null)
                    {
                        throw new Exception("No target node available to accommodate the data.");
                    }

                    if (targetNode.CompareTo(LocalAddress) == 0)
                    {
                        result = Local_Add(key, cacheEntry, Cluster.LocalAddress, taskId, true, operationContext);
                    }
                    else
                    {
                        result = Clustered_Add(targetNode, key, cacheEntry, taskId, operationContext);
                    }
                    return result;
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, group);
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.Clustered_Add()", te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                        throw;
                }
                catch (Runtime.Exceptions.SuspectedException e)
                {
                    suspectedErrorOccured = true;
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.Clustered_Add()", e.ToString());
                    if (maxTries == 0)
                        throw;
                    maxTries--;
                    Thread.Sleep(_serverFailureWaitTime);
                }
            } while (maxTries > 0);
            return result;
        }

        /// <summary>
        /// Hanlde cluster-wide Add(key) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        /// <remarks>
        /// This method can only be called on one node in the cluster. It triggers <see cref="OnItemAdded"/>,
        /// which initiates a cluster-wide item added notification.
        /// </remarks>
        private object handleAdd(Address src, object info, Array userPayload)
        {
            try
            {
                object[] objs = (object[])info;
                OperationContext operationContext = null;
                string taskId = null;
                if (objs.Length > 2)
                    taskId = objs[2] as string;

                if (objs.Length > 3)
                    operationContext = objs[3] as OperationContext;

                if (objs[0] is object[])
                {
                    object[] keys = (object[])objs[0];
                    CacheEntry[] entries = objs[1] as CacheEntry[];
                    return Local_Add(keys, entries, src, taskId, true, operationContext);
                }
                else
                {
                    object key = objs[0];
                    CacheEntry e = objs[1] as CacheEntry;
                    if (userPayload!=null)
                            e.Value = userPayload;
                    CacheAddResult result = Local_Add(key, e, src, taskId, true, operationContext);
                    return result;
                }
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return CacheAddResult.Failure;
        }

        /// <summary>
        /// Hanlde cluster-wide GetKeys(group, subGroup) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleGetKeys(object info)
        {
            try
            {
                object[] objs = (object[])info;
                string group = objs[0] as string;
                string subGroup = objs[1] as string;
                OperationContext operationContext = null;
                if (objs.Length > 2)
                    operationContext = objs[2] as OperationContext;

                return Local_GetGroupKeys(group, subGroup, operationContext);
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        /// <summary>
        /// Hanlde cluster-wide GetData(group, subGroup) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleGetData(object info)
        {
            try
            {
                object[] objs = (object[])info;
                string group = objs[0] as string;
                string subGroup = objs[1] as string;
                OperationContext operationContext = objs[2] as OperationContext;
                return Local_GetGroupData(group, subGroup, operationContext);
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        /// <summary>
        /// Add the expiration hint against the given key
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private object handleAddHint(object info)
        {
            try
            {
                object[] objs = (object[])info;
                object key = objs[0];
                ExpirationHint eh = objs[1] as ExpirationHint;
                OperationContext oc = objs[2] as OperationContext;

                return Local_Add(key, eh, oc);
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return false;
        }

        /// <summary>
        /// Add the expiration hint against the given key
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private object handleAddSyncDependency(object info)
        {
            try
            {
                object[] objs = (object[])info;
                object key = objs[0];
                CacheSynchronization.CacheSyncDependency syncDependency = objs[1] as CacheSynchronization.CacheSyncDependency;
                OperationContext oc = objs[2] as OperationContext;
                return Local_Add(key, syncDependency, oc);
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return false;
        }

        #endregion

        #region /                   --- GetGroupInfo ---                      /

        /// <summary>
        /// Gets the data group info the item.
        /// </summary>
        /// <param name="key">Key of the item</param>
        /// <returns>Data group info of the item</returns>
        public override DataGrouping.GroupInfo GetGroupInfo(object key, OperationContext operationContext)
        {
            _statusLatch.WaitForAny(NodeStatus.Running);

            DataGrouping.GroupInfo info;
            info = Local_GetGroupInfo(key, operationContext);
            if (info == null)
            {
                ClusteredOperationResult result = Clustered_GetGroupInfo(key, operationContext);
                if (result != null)
                {
                    info = result.Result as DataGrouping.GroupInfo;
                }
            }

            return info;
        }

        /// <summary>
        /// Gets data group info the items
        /// </summary>
        /// <param name="keys">Keys of the items</param>
        /// <returns>IDictionary of the data grup info the items</returns>
        public override Hashtable GetGroupInfoBulk(object[] keys, OperationContext operationContext)
        {
            _statusLatch.WaitForAny(NodeStatus.Running);

            Hashtable infoTable;
            infoTable = Local_GetGroupInfoBulk(keys, operationContext);
            if (infoTable == null)
                infoTable = (Hashtable)Clustered_GetGroupInfoBulk(keys, operationContext);

            return infoTable;
        }

        /// <summary>
        /// Gets the data group info the item.
        /// </summary>
        /// <param name="key">Key of the item</param>
        /// <returns>Data group info of the item</returns>
        private DataGrouping.GroupInfo Local_GetGroupInfo(Object key, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.GetGroupInfo(key, operationContext);
            return null;
        }

        /// <summary>
        /// Gets data group info the items
        /// </summary>
        /// <param name="keys">Keys of the items</param>
        /// <returns>IDictionary of the data grup info the items</returns>
        private Hashtable Local_GetGroupInfoBulk(Object[] keys, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.GetGroupInfoBulk(keys, operationContext);
            return null;
        }


        /// <summary>
        /// Handles the request for data group of the item
        /// </summary>
        /// <param name="info">Key(s) of the item(s)</param>
        /// <returns>Data group info of the item(s)</returns>
        private object handleGetGroupInfo(object info)
        {
            object result;
            OperationContext operationContext = null;
            object[] args = (object[])info;
            if (args.Length > 1)
                operationContext = args[1] as OperationContext;
            if (args[0] is object[])
                result = Local_GetGroupInfoBulk((object[])args[0], operationContext);
            else
                result = Local_GetGroupInfo(args[0], operationContext);

            return result;
        }

        #endregion

        #region /                    --- Data Integrity ---                     /

        /// <summary>
        /// Handles the data integrity varification from a new joining node.
        /// </summary>
        /// <param name="info">Data groups of the joining node.</param>
        /// <returns>True, if conflict found, otherwise false</returns>
        /// <remarks>Varifies whether the data groups of the joining node exist on
        /// this node or not. We get the list of all the groups contained by the
        /// cache. Remove the own data affinity groups. From remaining groups, if
        /// the joining node groups exist, we return true.</remarks>
        public object handleVerifyDataIntegrity(object info)
        {
            ArrayList allGroups = null;
            ArrayList otherGroups = (ArrayList)info;
            ArrayList myBindedGroups = null;
            if (_statusLatch.Status.IsBitSet(NodeStatus.Running))
            {
                try
                {
                    allGroups = _internalCache.DataGroupList;
                    if (_stats != null && _stats.LocalNode != null && _stats.LocalNode.DataAffinity != null)
                    {
                        myBindedGroups = _stats.LocalNode.DataAffinity.Groups;
                    }

                    if (allGroups != null)
                    {
                        IEnumerator ie;
                        if (myBindedGroups != null)
                        {
                            ie = myBindedGroups.GetEnumerator();
                            while (ie.MoveNext())
                            {
                                allGroups.Remove(ie.Current);
                            }
                        }

                        ie = allGroups.GetEnumerator();
                        while (ie.MoveNext())
                        {
                            if (otherGroups.Contains(ie.Current))
                            {
                                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.handleVerifyDataIntegrity", "data integrity not varified : group " + ie.Current.ToString() + "alread exists.");
                                return true;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    NCacheLog.Error("PartitionedServerCache.handleVerifyDataIntegrity", e.ToString());
                }
            }
            return false;
        }
        #endregion

        #region	/                 --- Partitioned ICache.BulkAdd ---           /

        /// <summary>
        /// Adds pairs keys and values to the cache. Returns the list of keys that failed to Add.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">cache entries.</param>
        /// <returns>List of keys that are added or that alredy exists in the cache and their status.</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleAdd"/> on every server-node in the cluster, 
        /// or invokes <see cref="Local_Add"/> locally. <see cref="Local_Add"/> can only be called on 
        /// one node in the cluster, therefore it triggers a cluster-wide item added notification.
        /// </remarks>
        private bool IsClusterNodeExist(string ipAddress)
        {
            ArrayList serverIPList = new ArrayList();
            string onlyIP = "";
            foreach (object ipPort in Cluster.Servers)
            {
                onlyIP = ipPort.ToString();
                onlyIP = onlyIP.Remove(onlyIP.IndexOf(':'));
                serverIPList.Add(onlyIP);
            }
            if (serverIPList.Contains(ipAddress))
                return true;
            else
                return false;
        }

        private Hashtable OptimizedAdd(object[] keys, CacheEntry[] cacheEntries, string taskId, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.AddBlk", "");

            Hashtable result = new Hashtable();
            ArrayList goodKeysList = new ArrayList();
            Hashtable totalDepKeys = new Hashtable();
            Hashtable tmpResult = new Hashtable();

            ArrayList totalKeys = new ArrayList(keys);
            ArrayList totalEntries = new ArrayList(cacheEntries);

            Hashtable depResult = new Hashtable();

            ArrayList successfulKeys = new ArrayList();
            ArrayList remainingKeys = new ArrayList();
            
            ArrayList goodEntriesList = new ArrayList();

            #region -- PART I -- Cascading Dependency Operation

            for (int i = 0; i < totalEntries.Count; i++)
            {
                object[] tempKeys = ((CacheEntry)totalEntries[i]).KeysIAmDependingOn;

                if (tempKeys != null)
                {
                    Hashtable goodKeysTable = Contains(tempKeys, operationContext);

                    if (goodKeysTable.ContainsKey("items-found") && goodKeysTable["items-found"] != null && tempKeys.Length == ((ArrayList)goodKeysTable["items-found"]).Count)
                    {
                        goodKeysList.Add(keys[i]);
                        goodEntriesList.Add(cacheEntries[i]);
                    }
                    else
                    {
                        result.Add(keys[i], new OperationFailedException("One of the dependency keys does not exist."));
                    }
                }
                else
                {
                    goodKeysList.Add(keys[i]);
                    goodEntriesList.Add(cacheEntries[i]);
                }
            }

            #endregion

            string[] goodKeys = new string[goodKeysList.Count];
            goodKeysList.CopyTo(goodKeys);

            CacheEntry[] goodEntries = new CacheEntry[goodEntriesList.Count];
            goodEntriesList.CopyTo(goodEntries);

            try
            {
                tmpResult = Local_Add(goodKeys, goodEntries, Cluster.LocalAddress, taskId, notify, operationContext);
            }
            catch (BucketTransferredException ex)
            {
                tmpResult = new Hashtable();
                for (int i = 0; i < goodKeys.Length; i++)
                {
                    tmpResult[goodKeys[i]] = new OperationFailedException(ex.Message, ex);
                }
            }

            if (tmpResult != null && tmpResult.Count > 0)
            {
                IDictionaryEnumerator ie = tmpResult.GetEnumerator();
                while (ie.MoveNext())
                {
                    if (ie.Value is StateTransferException)
                    {
                        remainingKeys.Add(ie.Key);
                    }
                    else
                    {
                        if (ie.Value is Exception)
                        {
                            result[ie.Key] = ie.Value;
                        }
                        else if (ie.Value is CacheAddResult)
                        {
                            CacheAddResult res = (CacheAddResult)ie.Value;
                            switch (res)
                            {
                                case CacheAddResult.Failure:
                                    result[ie.Key] = new OperationFailedException("Generic operation failure; not enough information is available.");
                                    break;
                                case CacheAddResult.NeedsEviction:
                                    result[ie.Key] = new OperationFailedException("The cache is full and not enough items could be evicted.");
                                    break;
                                case CacheAddResult.KeyExists:
                                    result[ie.Key] = new OperationFailedException("The specified key already exists.");
                                    break;
                                case CacheAddResult.Success:
                                    successfulKeys.Add(ie.Key);
                                    int index = totalKeys.IndexOf(ie.Key);
                                    if (index != -1)
                                    {
                                        depResult[ie.Key] = totalEntries[index];
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            if (remainingKeys.Count > 0)
            {
                object[] currentKeys = new object[remainingKeys.Count];
                CacheEntry[] currentValues = new CacheEntry[remainingKeys.Count];

                int j = 0;
                foreach (string key in remainingKeys)
                {
                    int index = totalKeys.IndexOf(key);
                    if (index != -1)
                    {
                        currentKeys[j] = totalKeys[index];
                        currentValues[j] = totalEntries[index] as CacheEntry;
                        j++;
                    }
                }

                tmpResult = ClusteredAdd(currentKeys, currentValues, taskId, notify, operationContext);

                foreach (DictionaryEntry entry in tmpResult)
                {
                    result[entry.Key] = entry.Value;
                }
               
            }

            if (successfulKeys.Count > 0)
            {
                IEnumerator ie = successfulKeys.GetEnumerator();
                while (ie.MoveNext())
                {
                    object key = ie.Current;
                    CacheEntry originalEntry = (CacheEntry)depResult[key];
                    object[] depKeys = originalEntry.KeysIAmDependingOn;
                    if (depKeys != null)
                    {
                        for (int i = 0; i < depKeys.Length; i++)
                        {
                            if (!totalDepKeys.Contains(depKeys[i]))
                            {
                                totalDepKeys.Add(depKeys[i], new ArrayList());
                            }
                            ((ArrayList)totalDepKeys[(depKeys[i])]).Add(key);
                        }
                    }
                }


                object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);

                if (generateQueryInfo == null)
                {
                    operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                }


                Hashtable table2 = AddDepKeyList(totalDepKeys, operationContext);

                if (generateQueryInfo==null)
                {
                    operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                }

                IDictionaryEnumerator ten = table2.GetEnumerator();
                while (ten.MoveNext())
                {
                    if (!(bool)ten.Value)
                    {
                        Remove(((ArrayList)totalDepKeys[ten.Key]).ToArray(), ItemRemoveReason.Removed, false, operationContext);
                    }
                }
            }

            return result;
        }

        private Hashtable ClusteredAdd(object[] keys, CacheEntry[] cacheEntries, string taskId, bool notify, OperationContext operationContext)
        {
            Hashtable targetNodes = null;
            Hashtable result = new Hashtable();
            Hashtable tmpResult = null;
            ArrayList totalKeys = new ArrayList(keys);
            ClusteredArrayList keysToAdd = new ClusteredArrayList(keys);

            Hashtable failedTbl = new Hashtable();
            ArrayList totalEntries = new ArrayList(cacheEntries);
            Address targetNode = null;
            object[] currentKeys = null;
            CacheEntry[] currentValues = null;

            ArrayList goodKeysList = new ArrayList();
            ArrayList goodEntriesList = new ArrayList();
            Dictionary<object, CacheEntry> fullEntrySet = new Dictionary<object, CacheEntry>();

            Hashtable depResult = new Hashtable();
            Hashtable totalDepKeys = new Hashtable();


            Hashtable totalSuccessfullKeys = new Hashtable();
            Hashtable totalRemainingKeys = new Hashtable();


            string group = cacheEntries[0].GroupInfo == null ? null : cacheEntries[0].GroupInfo.Group;

            if (_internalCache == null) throw new InvalidOperationException();

            do
            {
                targetNodes = (Hashtable)GetTargetNodes(keysToAdd, group);

                if (targetNodes != null && targetNodes.Count == 0)
                {
                    foreach (object key in keysToAdd)
                    {
                        result[key] = new OperationFailedException("No target node available to accommodate the data.");
                    }
                    return result;
                }
                IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                Hashtable keyList = null;

                //We select one node at a time for Add operation.
                while (ide.MoveNext())
                {
                    targetNode = ide.Key as Address;
                    keyList = (Hashtable)ide.Value;

                    if (targetNode != null && keyList != null)
                    {
                        currentKeys = new object[keyList.Count];
                        currentValues = new CacheEntry[keyList.Count];

                        int j = 0;
                        foreach (object key in keyList.Keys)
                        {
                            int index = totalKeys.IndexOf(key);
                            if (index != -1)
                            {
                                currentKeys[j] = totalKeys[index];
                                currentValues[j] = totalEntries[index] as CacheEntry;
                                if (!fullEntrySet.ContainsKey((string)totalKeys[index]))
                                    fullEntrySet.Add((string)totalKeys[index], (CacheEntry)totalEntries[index]);
                                j++;
                            }
                        }

                        #region -- PART I -- Cascading Dependency Operation

                        goodKeysList.Clear();
                        goodEntriesList.Clear();

                        for (int i = 0; i < currentValues.Length; i++)
                        {
                            object[] tempKeys = currentValues[i].KeysIAmDependingOn;
                            if (tempKeys != null)
                            {
                                Hashtable goodKeysTable = Contains(tempKeys, operationContext);

                                if (goodKeysTable.ContainsKey("items-found") && goodKeysTable["items-found"] != null && tempKeys.Length == ((ArrayList)goodKeysTable["items-found"]).Count)
                                {
                                    goodKeysList.Add(currentKeys[i]);
                                    goodEntriesList.Add(currentValues[i]);
                                    if (!fullEntrySet.ContainsKey(currentKeys[i]))
                                        fullEntrySet.Add(currentKeys[i], currentValues[i]);
                                }

                                else
                                {
                                    result[currentKeys[i]] = new OperationFailedException("One of the dependency keys does not exist.");
                                }
                            }
                            else
                            {
                                goodKeysList.Add(currentKeys[i]);
                                goodEntriesList.Add(currentValues[i]); 
                                if (!fullEntrySet.ContainsKey(currentKeys[i]))
                                    fullEntrySet.Add(currentKeys[i], currentValues[i]);
                            }

                        }

                        string[] goodKeys = new string[goodKeysList.Count];
                        goodKeysList.CopyTo(goodKeys);

                        CacheEntry[] goodEntries = new CacheEntry[goodEntriesList.Count];
                        goodEntriesList.CopyTo(goodEntries);

                        #endregion

                        try
                        {
                            if (targetNode.Equals(Cluster.LocalAddress))
                            {
                                tmpResult = Local_Add(goodKeys, goodEntries, Cluster.LocalAddress, taskId, notify, operationContext);
                            }
                            else
                            {
                                tmpResult = Clustered_Add(targetNode, goodKeys, goodEntries, taskId, operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            Thread.Sleep(_serverFailureWaitTime);
                            //we redo the operation
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedServerCache.SafeAdd", targetNode + " left while addition");

                            tmpResult = new Hashtable();
                            for (int i = 0; i < goodKeys.Length; i++)
                            {
                                tmpResult[goodKeys[i]] = new GeneralFailureException(se.Message, se);
                            }
                        }
                        catch (Runtime.Exceptions.TimeoutException te)
                        {
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedServerCache.SafeAdd", targetNode + " operation timed out");

                            tmpResult = new Hashtable();
                            for (int i = 0; i < goodKeys.Length; i++)
                            {
                                tmpResult[goodKeys[i]] = new GeneralFailureException(te.Message, te);
                            }
                        }
                        catch (BucketTransferredException ex)
                        {
                            tmpResult = new Hashtable();
                            for (int i = 0; i < goodKeys.Length; i++)
                            {
                                tmpResult[goodKeys[i]] = new OperationFailedException(ex.Message, ex);
                            }
                        }

                        if (tmpResult != null && tmpResult.Count > 0)
                        {
                            IDictionaryEnumerator ie = tmpResult.GetEnumerator();
                            while (ie.MoveNext())
                            {
                                if (ie.Value is StateTransferException)
                                {
                                    totalRemainingKeys[ie.Key] = null;
                                }
                                else
                                {
                                    if (ie.Value is Exception)
                                    {
                                        result[ie.Key] = ie.Value;
                                    }
                                    else if (ie.Value is CacheAddResult)
                                    {
                                        CacheAddResult res = (CacheAddResult)ie.Value;
                                        switch (res)
                                        {
                                            case CacheAddResult.Failure:
                                                result[ie.Key] = new OperationFailedException("Generic operation failure; not enough information is available.");
                                                break;
                                            case CacheAddResult.NeedsEviction:
                                                result[ie.Key] = new OperationFailedException("The cache is full and not enough items could be evicted.");
                                                break;
                                            case CacheAddResult.KeyExists:
                                                result[ie.Key] = new OperationFailedException("The specified key already exists.");
                                                break;
                                            case CacheAddResult.Success:
                                                totalSuccessfullKeys[ie.Key] = null;
                                                int index = totalKeys.IndexOf(ie.Key);
                                                if (index != -1)
                                                {
                                                    depResult[ie.Key] = totalEntries[index];
                                                }
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                keysToAdd = new ClusteredArrayList(totalRemainingKeys.Keys);
                totalRemainingKeys.Clear();
            }
            while (keysToAdd.Count > 0);

            if (totalSuccessfullKeys.Count > 0)
            {
                IEnumerator ie = totalSuccessfullKeys.Keys.GetEnumerator();
                while (ie.MoveNext())
                {
                    object key = ie.Current;
                    if (notify)
                    {
                        // If everything went ok!, initiate local and cluster-wide notifications.
                        EventContext eventContext = CreateEventContextForGeneralDataEvent(Persistence.EventType.ITEM_ADDED_EVENT, operationContext, fullEntrySet[key], null);
                        RaiseItemAddNotifier(key, fullEntrySet[key], operationContext, eventContext);
                        handleNotifyAdd(new object[] { key, operationContext, eventContext });
                    }

                    CacheEntry orignalEntry = (CacheEntry)depResult[key];
                    object[] depKeys = orignalEntry.KeysIAmDependingOn;
                    if (depKeys != null)
                    {
                        for (int i = 0; i < depKeys.Length; i++)
                        {
                            if (!totalDepKeys.Contains(depKeys[i]))
                            {
                                totalDepKeys.Add(depKeys[i], new ArrayList());
                            }
                            ((ArrayList)totalDepKeys[(depKeys[i])]).Add(key);
                        }
                    }
                }

                object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);

                if (generateQueryInfo == null)
                {
                    operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                }

                Hashtable table2 = AddDepKeyList(totalDepKeys, operationContext);

                if (generateQueryInfo == null)
                {
                    operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                }

                IDictionaryEnumerator ten = table2.GetEnumerator();
                while (ten.MoveNext())
                {
                    if (!(bool)ten.Value)
                    {
                        Remove(((ArrayList)totalDepKeys[ten.Key]).ToArray(), ItemRemoveReason.Removed, false, operationContext);
                    }
                }
            }

            return result;
        }

        public override Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.AddBlk", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            Hashtable result = null;

            if (_internalCache == null) throw new InvalidOperationException();

            string taskId = null;
            if (cacheEntries[0].Flag != null && cacheEntries[0].Flag.IsBitSet(BitSetConstants.WriteBehind))
                taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

            long clientLastViewId = GetClientLastViewId(operationContext);

            if (clientLastViewId == Cluster.LastViewID && !IsInStateTransfer())
            {
                result = OptimizedAdd(keys, cacheEntries, taskId, notify, operationContext);
            }
            else
            {
                result = ClusteredAdd(keys, cacheEntries, taskId, notify, operationContext);
            }

            return result;
        }
        
        /// <summary>
        /// Add the objects to the local cache. 
        /// </summary>
        /// <param name="keys">key of the entry.</param>
        /// <returns>list of added keys.</returns>
        /// <remarks>
        /// This method can only be called on one node in the cluster. It triggers <see cref="OnItemAdded"/>,
        /// which initiates a cluster-wide item added notification.
        /// </remarks>
        private Hashtable Local_Add(object[] keys, CacheEntry[] cacheEntries, Address src, string taskId, bool notify, OperationContext operationContext)
        {
            Hashtable added = new Hashtable();

            CacheEntry[] clone = null;
            if (taskId != null)
            {
                clone = new CacheEntry[cacheEntries.Length];
                for (int i = 0; i < cacheEntries.Length; i++)
                {
                    if (cacheEntries[i].HasQueryInfo)
                        clone[i] = (CacheEntry)cacheEntries[i].Clone();
                    else
                        clone[i] = cacheEntries[i];
                }
            }

            if (_internalCache != null)
            {
                added = _internalCache.Add(keys, cacheEntries, notify, operationContext);

                if (taskId != null && added.Count > 0)
                {
                    Hashtable writeBehindTable = new Hashtable();
                    for (int i = 0; i < keys.Length; i++)
                    {
                        object value = added[keys[i]];
                        if (value is CacheAddResult && ((CacheAddResult)value) == CacheAddResult.Success)
                        {
                            writeBehindTable.Add(keys[i], clone[i]);
                        }
                    }
                    if (writeBehindTable.Count > 0)
                    {
                        base.AddWriteBehindTask(src, writeBehindTable, null, taskId, OpCode.Add, WriteBehindAsyncProcessor.TaskState.Execute);
                    }
                }
            }

            return added;
        }


        #endregion

        #region	/                 --- Partitioned ICache.Insert ---           /

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleInsert"/> on any cluster node or invokes 
        /// <see cref="Local_Insert"/> locally. The choice of the server node is determined by the 
        /// <see cref="LoadBalancer"/>.
        /// <see cref="Local_Insert"/> triggers either <see cref="OnItemAdded"/> or <see cref="OnItemUpdated"/>, which
        /// in turn trigger either an item-added or item-updated cluster-wide notification.
        /// </remarks>
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Insert", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            Address targetNode = null;

            if (_internalCache == null) throw new InvalidOperationException();

            string taskId = null;
            if (cacheEntry.Flag != null && cacheEntry.Flag.IsBitSet(BitSetConstants.WriteBehind))
                taskId = taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

            CacheInsResultWithEntry result = new CacheInsResultWithEntry();

            #region -- PART I -- Cascading Dependency Operation
            object[] keys = cacheEntry.KeysIAmDependingOn;
            if (keys != null)
            {
                Hashtable goodKeysTable = Contains(keys, operationContext);
                if (!goodKeysTable.ContainsKey("items-found"))
                    throw new OperationFailedException("One of the dependency keys does not exist.", false);

                if (goodKeysTable["items-found"] == null)
                    throw new OperationFailedException("One of the dependency keys does not exist.", false);

                if (goodKeysTable["items-found"] == null || ((ArrayList)goodKeysTable["items-found"]).Count != keys.Length)
                    throw new OperationFailedException("One of the dependency keys does not exist.", false);

            }
            #endregion

            result = Safe_Clustered_Insert(key, cacheEntry, out targetNode, taskId, lockId, version, accessType, operationContext);

            try
            {
                #region -- PART II -- Cascading Dependency Operation
                if (result.Result == CacheInsResult.Success || result.Result == CacheInsResult.SuccessOverwrite)
                {
                    Hashtable table = null;
                    Hashtable ret = null;
                    Hashtable keysTable = new Hashtable();
                    try
                    {
                        //Fix for NCache Bug4981
                        object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);
                        if (generateQueryInfo == null)
                        {
                            operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                        }

                        if (result.Entry != null && result.Entry.KeysIAmDependingOn != null)
                        {
                            table = GetFinalKeysList(result.Entry.KeysIAmDependingOn, cacheEntry.KeysIAmDependingOn);
                          
                            keysTable = GetKeysTable(key, (object[])table["oldKeys"]);
                            if (keysTable != null)
                                RemoveDepKeyList(keysTable, operationContext);

                            keysTable = GetKeysTable(key, (object[])table["newKeys"]);
                            if (keysTable != null)
                                ret = AddDepKeyList(keysTable, operationContext);                            

                        }
                        else if (cacheEntry.KeysIAmDependingOn != null)
                        {
                            keysTable = GetKeysTable(key, cacheEntry.KeysIAmDependingOn);
                            if (keysTable != null)
                                ret = AddDepKeyList(keysTable, operationContext);                          
                        }

                        if (generateQueryInfo == null)
                        {
                            operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                        }

                    }
                    catch (Exception e)
                    {
                        Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                        throw e;
                    }
                    if (ret != null)
                    {
                        IDictionaryEnumerator en = ret.GetEnumerator();
                        while (en.MoveNext())
                        {
                            if (en.Value is bool && !((bool)en.Value))
                            {
                                Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                                NCacheLog.Info("PartitionedServerCache.Insert", "One of the dependency keys does not exist. Key: " + en.Key.ToString());
                                throw new OperationFailedException("One of the dependency keys does not exist.", false);
                            }
                        }
                    }
                }
                #endregion
            }
            finally
            {
            }

            return result;
        }

        private Hashtable OptimizedInsert(object[] keys, CacheEntry[] cacheEntries, string taskId, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.InsertBlk", "");

            Hashtable result = new Hashtable();

            Hashtable addedKeys = new Hashtable();
            Hashtable insertedKeys = new Hashtable();
            ArrayList remainingKeys = new ArrayList();

            ArrayList totalKeys = new ArrayList(keys);
            ArrayList totalEntries = new ArrayList(cacheEntries);

            Hashtable depResult = new Hashtable();

            Hashtable totalDepKeys = new Hashtable();
            Hashtable oldDepKeys = new Hashtable();
            Hashtable tmpResult = new Hashtable();


            ArrayList goodKeysList = new ArrayList();
            ArrayList goodEntriesList = new ArrayList();

            #region -- PART I -- Cascading Dependency Operation

            for (int i = 0; i < totalEntries.Count; i++)
            {
                object[] tempKeys = ((CacheEntry)totalEntries[i]).KeysIAmDependingOn;
                if (tempKeys != null)
                {
                    Hashtable goodKeysTable = Contains(tempKeys, operationContext);

                    if (goodKeysTable.ContainsKey("items-found") && goodKeysTable["items-found"] != null && tempKeys.Length == ((ArrayList)goodKeysTable["items-found"]).Count)
                    {
                        goodKeysList.Add(keys[i]);
                        goodEntriesList.Add(cacheEntries[i]);
                    }
                    else
                    {
                        result[keys[i]] = new OperationFailedException("One of the dependency keys does not exist.");
                    }
                }
                else
                {
                    goodKeysList.Add(keys[i]);
                    goodEntriesList.Add(cacheEntries[i]);
                }
            }

            #endregion

            string[] goodKeys = new string[goodKeysList.Count];
            goodKeysList.CopyTo(goodKeys);

            CacheEntry[] goodEntries = new CacheEntry[goodEntriesList.Count];
            goodEntriesList.CopyTo(goodEntries);

            try
            {
                tmpResult = Local_Insert(goodKeys, goodEntries, Cluster.LocalAddress, taskId, notify, operationContext);
            }
            catch (BucketTransferredException ex)
            {
                tmpResult = new Hashtable();
                for (int i = 0; i < goodKeys.Length; i++)
                {
                    tmpResult[goodKeys[i]] = new OperationFailedException(ex.Message, ex);
                }
            }

            if (tmpResult != null && tmpResult.Count > 0)
            {
                IDictionaryEnumerator ie = tmpResult.GetEnumerator();
                while (ie.MoveNext())
                {
                    if (ie.Value is StateTransferException)
                    {
                        remainingKeys.Add(ie.Key);
                    }
                    else
                    {
                        if (ie.Value is Exception)
                        {
                            result[ie.Key] = ie.Value;
                        }
                        else if (ie.Value is CacheInsResultWithEntry)
                        {
                            CacheInsResultWithEntry res = ie.Value as CacheInsResultWithEntry;
                            switch (res.Result)
                            {
                                case CacheInsResult.Failure:
                                    result[ie.Key] = new OperationFailedException("Generic operation failure; not enough information is available.");
                                    break;
                                case CacheInsResult.NeedsEviction:
                                    result[ie.Key] = new OperationFailedException("The cache is full and not enough items could be evicted.");
                                    break;
                                case CacheInsResult.IncompatibleGroup:
                                    result[ie.Key] = new OperationFailedException("Data group of the inserted item does not match the existing item's data group");
                                    break;
                                case CacheInsResult.Success:
                                    addedKeys[ie.Key] = null;
                                    int index = totalKeys.IndexOf(ie.Key);
                                    if (index != -1)
                                    {
                                        depResult[ie.Key] = totalEntries[index];
                                        result[ie.Key] = ie.Value;
                                    }
                                    break;
                                case CacheInsResult.SuccessOverwrite:
                                    insertedKeys[ie.Key] = ie.Value;
                                    index = totalKeys.IndexOf(ie.Key);
                                    if (index != -1)
                                    {
                                        depResult[ie.Key] = totalEntries[index];
                                        result[ie.Key] = ie.Value;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            if (remainingKeys.Count > 0)
            {
                object[] currentKeys = new object[remainingKeys.Count];
                CacheEntry[] currentValues = new CacheEntry[remainingKeys.Count];

                int j = 0;
                foreach (string key in remainingKeys)
                {
                    int index = totalKeys.IndexOf(key);
                    if (index != -1)
                    {
                        currentKeys[j] = totalKeys[index];
                        currentValues[j] = totalEntries[index] as CacheEntry;
                        j++;
                    }
                }

                tmpResult = ClusteredInsert(currentKeys, currentValues, taskId, notify, operationContext);
                foreach (DictionaryEntry entry in tmpResult)
                {
                    result[entry.Key] = entry.Value;
                }
            }

            if (addedKeys.Count > 0)
            {
                IEnumerator ie = addedKeys.Keys.GetEnumerator();
                while (ie.MoveNext())
                {
                    object key = ie.Current;
                    CacheEntry originalEntry = (CacheEntry)depResult[key];
                    object[] depKeys = originalEntry.KeysIAmDependingOn;
                    if (depKeys != null)
                    {
                        for (int i = 0; i < depKeys.Length; i++)
                        {
                            if (!totalDepKeys.Contains(depKeys[i]))
                            {
                                totalDepKeys.Add(depKeys[i], new ArrayList());
                            }
                            ((ArrayList)totalDepKeys[(depKeys[i])]).Add(key);
                        }
                    }
                }
            }

            //Fix for NCache Bug4981
            object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);

            if (generateQueryInfo == null)
            {
                operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
            }


            if (insertedKeys.Count > 0)
            {
                IDictionaryEnumerator ide = insertedKeys.GetEnumerator();
                while (ide.MoveNext())
                {
                    object key = ide.Key;
                    CacheInsResultWithEntry insResult = ide.Value as CacheInsResultWithEntry;                  
                    CacheEntry originalEntry = (CacheEntry)depResult[key];
                    object[] depKeys = originalEntry.KeysIAmDependingOn;
                    if (depKeys != null)
                    {
                        for (int i = 0; i < depKeys.Length; i++)
                        {
                            if (!totalDepKeys.Contains(depKeys[i]))
                            {
                                totalDepKeys.Add(depKeys[i], new ArrayList());
                            }
                            ((ArrayList)totalDepKeys[(depKeys[i])]).Add(key);
                        }
                    }
                    CacheEntry oldEntry = insResult.Entry;
                    if (oldEntry != null)
                    {
                        object[] depKeys2 = oldEntry.KeysIAmDependingOn;
                        if (depKeys2 != null)
                        {
                            for (int i = 0; i < depKeys2.Length; i++)
                            {
                                if (!oldDepKeys.Contains(depKeys2[i]))
                                {
                                    oldDepKeys.Add(depKeys2[i], new ArrayList());
                                }
                                ((ArrayList)oldDepKeys[(depKeys2[i])]).Add(key);
                            }
                        }
                    }
                }

                RemoveDepKeyList(oldDepKeys, operationContext);
            }

            Hashtable table = AddDepKeyList(totalDepKeys, operationContext);

            if (generateQueryInfo == null)
            {
                operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
            } 

            IDictionaryEnumerator ten = table.GetEnumerator();
            while (ten.MoveNext())
            {
                if (!(bool)ten.Value)
                {
                    Remove(((ArrayList)totalDepKeys[ten.Key]).ToArray(), ItemRemoveReason.Removed, false, operationContext);
                }
            }

            return result;
        }

        private Hashtable ClusteredInsert(object[] keys, CacheEntry[] cacheEntries, string taskId, bool notify, OperationContext operationContext)
        {
            Hashtable targetNodes = null;
            Hashtable result = new Hashtable();
            Hashtable tmpResult = null;

            ArrayList totalKeys = new ArrayList(keys);
            ArrayList totalEntries = new ArrayList(cacheEntries);
            ClusteredArrayList keysToInsert = new ClusteredArrayList(keys);
            
            Hashtable failedTbl = new Hashtable();
            
            Address targetNode = null;
            object[] currentKeys = null; 
            CacheEntry[] currentValues = null;

            ArrayList goodKeysList = new ArrayList();
            ArrayList goodEntriesList = new ArrayList();

            Hashtable depResult = new Hashtable();
            Hashtable totalDepKeys = new Hashtable();
            Hashtable oldDepKeys = new Hashtable();
            Dictionary<object, CacheEntry> fullEntrySet = new Dictionary<object, CacheEntry>();

            Hashtable totalAddedKeys = new Hashtable();
            Hashtable totalInsertedKeys = new Hashtable();
            Hashtable totalRemainingKeys = new Hashtable();


            string group = cacheEntries[0].GroupInfo == null ? null : cacheEntries[0].GroupInfo.Group;

            if (_internalCache == null) throw new InvalidOperationException();

            do
            {
                targetNodes = (Hashtable)GetTargetNodes(keysToInsert, group);

                if (targetNodes != null && targetNodes.Count == 0)
                {
                    foreach (object key in totalKeys)
                    {
                        result[key] = new OperationFailedException("No target node available to accommodate the data.");
                    }
                    return result;
                }
                IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                Hashtable keyList = null;

                //We select one node at a time for Add operation.
                while (ide.MoveNext())
                {
                    targetNode = ide.Key as Address;
                    keyList = (Hashtable)ide.Value;

                    if (targetNode != null && keyList != null)
                    {
                        currentKeys = new object[keyList.Count];
                        currentValues = new CacheEntry[keyList.Count];

                        int j = 0;
                        foreach (object key in keyList.Keys)
                        {
                            int index = totalKeys.IndexOf(key);
                            if (index != -1)
                            {
                                currentKeys[j] = totalKeys[index];
                                currentValues[j] = totalEntries[index] as CacheEntry;
                                if (!fullEntrySet.ContainsKey((string)totalKeys[index]))
                                    fullEntrySet.Add((string)totalKeys[index], (CacheEntry)totalEntries[index]);
                                j++;
                            }
                        }

                        #region -- PART I -- Cascading Dependency Operation

                        goodKeysList.Clear();
                        goodEntriesList.Clear();

                        for (int i = 0; i < currentValues.Length; i++)
                        {
                            object[] tempKeys = currentValues[i].KeysIAmDependingOn;
                            if (tempKeys != null)
                            {
                                Hashtable goodKeysTable = Contains(tempKeys, operationContext);

                                if (goodKeysTable.ContainsKey("items-found") && goodKeysTable["items-found"] != null && tempKeys.Length == ((ArrayList)goodKeysTable["items-found"]).Count)
                                {
                                    goodKeysList.Add(currentKeys[i]);
                                    goodEntriesList.Add(currentValues[i]); 
                                    if (!fullEntrySet.ContainsKey((string)keys[i]))
                                        fullEntrySet.Add((string)keys[i], cacheEntries[i]);
                                }

                                else
                                {
                                    result[currentKeys[i]] = new OperationFailedException("One of the dependency keys does not exist.");
                                }
                            }
                            else
                            {
                                goodKeysList.Add(currentKeys[i]);
                                goodEntriesList.Add(currentValues[i]);
                                if (!fullEntrySet.ContainsKey((string)keys[i]))
                                    fullEntrySet.Add((string)keys[i], cacheEntries[i]);
                            }

                        }

                        string[] goodKeys = new string[goodKeysList.Count];
                        goodKeysList.CopyTo(goodKeys);

                        CacheEntry[] goodEntries = new CacheEntry[goodEntriesList.Count];
                        goodEntriesList.CopyTo(goodEntries);

                        #endregion

                        try
                        {
                            if (targetNode.Equals(Cluster.LocalAddress))
                            {
                                tmpResult = Local_Insert(goodKeys, goodEntries, Cluster.LocalAddress, taskId, notify, operationContext);
                            }
                            else
                            {
                                tmpResult = Clustered_Insert(targetNode, goodKeys, goodEntries, taskId, operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            //we redo the operation
                            Thread.Sleep(_serverFailureWaitTime);
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedServerCache.SafeAdd", targetNode + " left while addition");

                            tmpResult = new Hashtable();
                            for (int i = 0; i < goodKeys.Length; i++)
                            {
                                tmpResult[goodKeys[i]] = new GeneralFailureException(se.Message, se);
                            }
                        }
                        catch (Runtime.Exceptions.TimeoutException te)
                        {
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedServerCache.SafeAdd", targetNode + " operation timed out");

                            tmpResult = new Hashtable();
                            for (int i = 0; i < goodKeys.Length; i++)
                            {
                                tmpResult[goodKeys[i]] = new GeneralFailureException(te.Message, te);
                            }
                        }
                        catch (BucketTransferredException ex)
                        {
                            tmpResult = new Hashtable();
                            for (int i = 0; i < goodKeys.Length; i++)
                            {
                                tmpResult[goodKeys[i]] = new OperationFailedException(ex.Message, ex);
                            }
                        }

                        if (tmpResult != null && tmpResult.Count > 0)
                        {
                            IDictionaryEnumerator ie = tmpResult.GetEnumerator();
                            while (ie.MoveNext())
                            {
                                if (ie.Value is StateTransferException)
                                {
                                    totalRemainingKeys[ie.Key] = null;
                                }
                                else
                                {
                                    if (ie.Value is Exception)
                                    {
                                        result[ie.Key] = ie.Value;
                                    }
                                    else if (ie.Value is CacheInsResultWithEntry)
                                    {
                                        CacheInsResultWithEntry res = ie.Value as CacheInsResultWithEntry;
                                        switch (res.Result)
                                        {
                                            case CacheInsResult.Failure:
                                                result[ie.Key] = new OperationFailedException("Generic operation failure; not enough information is available.");
                                                break;
                                            case CacheInsResult.NeedsEviction:
                                                result[ie.Key] = new OperationFailedException("The cache is full and not enough items could be evicted.");
                                                break;
                                            case CacheInsResult.IncompatibleGroup:
                                                result[ie.Key] = new OperationFailedException("Data group of the inserted item does not match the existing item's data group");
                                                break;
                                            case CacheInsResult.Success:
                                                totalAddedKeys[ie.Key] = null;
                                                int index = totalKeys.IndexOf(ie.Key);
                                                if (index != -1)
                                                {
                                                    depResult[ie.Key] = totalEntries[index];
                                                }
                                                break;
                                            case CacheInsResult.SuccessOverwrite:
                                                totalInsertedKeys[ie.Key] = ie.Value;
                                                index = totalKeys.IndexOf(ie.Key);
                                                if (index != -1)
                                                {
                                                    depResult[ie.Key] = totalEntries[index];
                                                    result[ie.Key] = ie.Value; 
                                                }
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                keysToInsert = new ClusteredArrayList(totalRemainingKeys.Keys);
                totalRemainingKeys.Clear();
            }
            while (keysToInsert.Count > 0);

            if (totalAddedKeys.Count > 0)
            {
                IEnumerator ie = totalAddedKeys.Keys.GetEnumerator();
                while (ie.MoveNext())
                {
                    object key = ie.Current;
                    if (notify)
                    {
                        // If everything went ok!, initiate local and cluster-wide notifications.
                        EventContext eventContext = CreateEventContextForGeneralDataEvent(Persistence.EventType.ITEM_ADDED_EVENT, operationContext, fullEntrySet[key], null);
                        RaiseItemAddNotifier(key, fullEntrySet[key], operationContext, eventContext);
                        handleNotifyAdd(new object[] { key, operationContext, eventContext });
                    }

                    CacheEntry originalEntry = (CacheEntry)depResult[key];
                    object[] depKeys = originalEntry.KeysIAmDependingOn;
                    if (depKeys != null)
                    {
                        for (int i = 0; i < depKeys.Length; i++)
                        {
                            if (!totalDepKeys.Contains(depKeys[i]))
                            {
                                totalDepKeys.Add(depKeys[i], new ArrayList());
                            }
                            ((ArrayList)totalDepKeys[(depKeys[i])]).Add(key);
                        }
                    }
                }
            }

            //Fix for NCache Bug4981
            object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);
            if (generateQueryInfo == null)
            {
                operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
            }

            if (totalInsertedKeys.Count > 0)
            {
                IDictionaryEnumerator ide = totalInsertedKeys.GetEnumerator();
                while (ide.MoveNext())
                {
                    object key = ide.Key;
                    CacheInsResultWithEntry insResult = ide.Value as CacheInsResultWithEntry;
                    if (notify)
                    {
                        CacheEntry currentEntry = fullEntrySet[(string)ide.Key];
                        object value = insResult.Entry.Value;
                        if (value is CallbackEntry)
                        {
                            
                            RaiseCustomUpdateCalbackNotifier(ide.Key, currentEntry, insResult.Entry, operationContext);
                        }
                        EventContext eventContext = CreateEventContextForGeneralDataEvent(Persistence.EventType.ITEM_UPDATED_EVENT, operationContext, currentEntry, insResult.Entry);
                        RaiseItemUpdateNotifier(key, operationContext, eventContext);
                        handleNotifyUpdate(new object[]{key,operationContext,eventContext});
                    }
                    CacheEntry originalEntry = (CacheEntry)depResult[key];
                    object[] depKeys = originalEntry.KeysIAmDependingOn;
                    if (depKeys != null)
                    {
                        for (int i = 0; i < depKeys.Length; i++)
                        {
                            if (!totalDepKeys.Contains(depKeys[i]))
                            {
                                totalDepKeys.Add(depKeys[i], new ArrayList());
                            }
                            ((ArrayList)totalDepKeys[(depKeys[i])]).Add(key);
                        }
                    }
                    CacheEntry oldEntry = insResult.Entry;
                    if (oldEntry != null)
                    {
                        object[] depKeys2 = oldEntry.KeysIAmDependingOn;
                        if (depKeys2 != null)
                        {
                            for (int i = 0; i < depKeys2.Length; i++)
                            {
                                if (!oldDepKeys.Contains(depKeys2[i]))
                                {
                                    oldDepKeys.Add(depKeys2[i], new ArrayList());
                                }
                                ((ArrayList)oldDepKeys[(depKeys2[i])]).Add(key);
                            }
                        }
                    }
                }

                RemoveDepKeyList(oldDepKeys, operationContext);
            }

            Hashtable table = AddDepKeyList(totalDepKeys, operationContext);

            if (generateQueryInfo == null)
            {
                operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
            }

            IDictionaryEnumerator ten = table.GetEnumerator();
            while (ten.MoveNext())
            {
                if (!(bool)ten.Value)
                {
                    Remove(((ArrayList)totalDepKeys[ten.Key]).ToArray(), ItemRemoveReason.Removed, false, operationContext);
                }
            }

            return result;
        }

        /// <summary>
        /// Adds key and va
        /// lue pairs to the cache. If any of the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="keys">keys of the entry.</param>
        /// <param name="cacheEntries">the cache entries.</param>
        /// <returns>IDictionary of failed items. Keys are the keys of items and values are failure reasons
        /// usually exceptions</returns>
        /// <remarks>
        /// This method either invokes <see cref="handleInsert"/> on any cluster node or invokes 
        /// <see cref="Local_Insert"/> locally. The choice of the server node is determined by the 
        /// <see cref="LoadBalancer"/>.
        /// <see cref="Local_Insert"/> triggers either <see cref="OnItemAdded"/> or <see cref="OnItemUpdated"/>, which
        /// in turn trigger either an item-added or item-updated cluster-wide notification.
        /// </remarks>
        public override Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.InsertBlk", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            Hashtable result = null;
            string taskId = null;

            if (cacheEntries[0].Flag != null && cacheEntries[0].Flag.IsBitSet(BitSetConstants.WriteBehind))
                taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

            long clientLastViewId = GetClientLastViewId(operationContext);

            if (clientLastViewId == Cluster.LastViewID && !IsInStateTransfer())
            {
                result = OptimizedInsert(keys, cacheEntries, taskId, notify, operationContext);
            }
            else
            {
                result = ClusteredInsert(keys, cacheEntries, taskId, notify, operationContext);
            }

            return result;
        }

        private CacheInsResultWithEntry Local_Insert(object key, CacheEntry cacheEntry, Address src, string taskId, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheInsResultWithEntry retVal = new CacheInsResultWithEntry();

            CacheEntry clone = null;
            if (taskId != null && cacheEntry.HasQueryInfo)
                clone = (CacheEntry)cacheEntry.Clone();
            else
                clone = cacheEntry;

            if (_internalCache != null)
            {
                retVal = _internalCache.Insert(key, cacheEntry, notify, lockId, version, accessType, operationContext);

                if (taskId != null && (retVal.Result == CacheInsResult.Success || retVal.Result == CacheInsResult.SuccessOverwrite))
                {
                    base.AddWriteBehindTask(src, key as string, clone, taskId, OpCode.Update, WriteBehindAsyncProcessor.TaskState.Execute, operationContext);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Insert the objects to the local cache. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        private Hashtable Local_Insert(object[] keys, CacheEntry[] cacheEntries, Address src, string taskId, bool notify, OperationContext operationContext)
        {
            Hashtable retVal = new Hashtable();

            CacheEntry[] clone = null;
            if (taskId != null)
            {
                clone = new CacheEntry[cacheEntries.Length];
                for (int i = 0; i < cacheEntries.Length; i++)
                {
                    if (cacheEntries[i].HasQueryInfo)
                        clone[i] = (CacheEntry)cacheEntries[i].Clone();
                    else
                        clone[i] = cacheEntries[i];
                }
            }

            if (_internalCache != null)
            {
                retVal = _internalCache.Insert(keys, cacheEntries, notify, operationContext);

                if (taskId != null && retVal.Count > 0)
                {
                    Hashtable writeBehindTable = new Hashtable();
                    for (int i = 0; i < keys.Length; i++)
                    {
                        CacheInsResultWithEntry value = retVal[keys[i]] as CacheInsResultWithEntry;
                        if (value != null && (value.Result == CacheInsResult.Success || value.Result == CacheInsResult.SuccessOverwrite))
                        {
                            writeBehindTable.Add(keys[i], clone[i]);
                        }
                    }
                    if (writeBehindTable.Count > 0)
                    {
                        base.AddWriteBehindTask(src, writeBehindTable, null, taskId, OpCode.Update, WriteBehindAsyncProcessor.TaskState.Execute);
                    }
                }
            }
            return retVal;
        }

        private CacheInsResultWithEntry Safe_Clustered_Insert(object key, CacheEntry cacheEntry, out Address targetNode, string taskId, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            bool suspectedErrorOccured = false;
            int maxTries = _stats.Nodes.Count > 3 ? 3 : _stats.Nodes.Count - 1;
            CacheInsResultWithEntry retVal = new CacheInsResultWithEntry();

            string group = cacheEntry.GroupInfo == null ? null : cacheEntry.GroupInfo.Group;
            targetNode = null;
            do
            {
                try
                {
                    targetNode = GetNextNode(key as string, group);

                    if (targetNode == null)
                    {
                        throw new Exception("No target node available to accommodate the data.");
                    }

                    if (targetNode.CompareTo(LocalAddress) == 0)
                    {
                        retVal = Local_Insert(key, cacheEntry, Cluster.LocalAddress, taskId, true, lockId, version, accessType, operationContext);
                    }
                    else
                    {
                        retVal = Clustered_Insert(targetNode, key, cacheEntry, taskId, lockId, version, accessType, operationContext);
                    }

                    break;
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, group);
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.Safe_Clustered_Insert()", te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                        throw;
                }
                catch (Runtime.Exceptions.SuspectedException e)
                {
                    suspectedErrorOccured = true;
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.Safe_Clustered_Insert()", e.ToString());
                    if (maxTries == 0)
                        throw;
                    maxTries--;
                    Thread.Sleep(_serverFailureWaitTime);
                }
            } while (maxTries > 0);
            return retVal;
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        /// <remarks>
        /// This method triggers either <see cref="OnItemAdded"/> or <see cref="OnItemUpdated"/>, which
        /// in turn trigger either an item-added or item-updated cluster-wide notification.
        /// </remarks>
        private object handleInsert(Address src, object info, Array userPayload)
        {
            try
            {
                OperationContext operationContext = null;
                object[] objs = (object[])info;

                string taskId = null;
                if (objs.Length > 2)
                    taskId = objs[2] as string;

                if (objs.Length > 4)
                {
                    operationContext = objs[6] as OperationContext;
                }
                else
                {
                    operationContext = objs[3] as OperationContext;
                }

                if (objs[0] is object[])
                {
                    object[] keyArr = (object[])objs[0];
                    CacheEntry[] valArr = (CacheEntry[])objs[1];
                    return Local_Insert(keyArr, valArr, src, taskId, true, operationContext);
                }
                else
                {
                    object key = objs[0];
                    CacheEntry e = objs[1] as CacheEntry;
                    e.Value = userPayload;
                    object lockId = objs[3];
                    LockAccessType accessType = (LockAccessType)objs[4];
                    ulong version = (ulong)objs[5];
                    CacheInsResultWithEntry retVal = Local_Insert(key, e, src, taskId, true, lockId, version, accessType, operationContext);
                    /* send value and entry separate*/
                    OperationResponse opRes = new OperationResponse();
                    if (retVal.Entry != null)
                    {
                        opRes.UserPayload = null;

                        if (retVal.Entry.KeysDependingOnMe == null || retVal.Entry.KeysDependingOnMe.Count == 0)
                            retVal.Entry = null;
                        else
                            retVal.Entry = retVal.Entry.CloneWithoutValue() as CacheEntry;
                    }
                    opRes.SerializablePayload = retVal;
                    return opRes;
                }
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        #endregion

        #region	/                 --- Partitioned ICache.Remove ---           /

        public override object RemoveSync(object[] keys, ItemRemoveReason reason, bool notify, OperationContext operationContext)
        {
            ArrayList depenedentItemList = new ArrayList();
            try
            {

                Hashtable totalRemovedItems = new Hashtable();

                CacheEntry entry = null;
                IDictionaryEnumerator ide = null;

                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.RemoveSync", "Keys = " + keys.Length.ToString());

                for (int i = 0; i < keys.Length; i++)
                {
                    try
                    {
                        if (keys[i] != null)
                            entry = Local_Remove(keys[i], reason, null, null, null, null, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);

                        if (entry != null)
                        {
                            totalRemovedItems.Add(keys[i], entry);
                            if (entry.KeysDependingOnMe != null && entry.KeysDependingOnMe.Count > 0)
                            {
                                depenedentItemList.AddRange(entry.KeysDependingOnMe.Keys);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }

                ArrayList keysOfRemoveNotification = new ArrayList();
                ArrayList entriesOfRemoveNotification = new ArrayList();
                List<EventContext> eventContexts = new List<EventContext>();
                int sizeThreshhold = 30 * 1024;
                int countThreshhold = 50;
                int size = 0;

                ide = totalRemovedItems.GetEnumerator();

                while (ide.MoveNext())
                {
                    try
                    {
                        entry = ide.Value as CacheEntry;
                        if (entry != null)
                        {
                            if (IsItemRemoveNotifier)
                            {
                                EventId eventId = null;
                                OperationID opId = operationContext.OperatoinID;
                                EventContext eventContext = null;

                                //generate event id
                                if (!operationContext.Contains(OperationContextFieldName.EventContext)) //for atomic operations
                                {
                                    eventId = EventId.CreateEventId(opId);
                                }
                                else //for bulk
                                {
                                    eventId = ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                                }

                                eventId.EventType = Alachisoft.NCache.Persistence.EventType.ITEM_REMOVED_EVENT;
                                eventContext = new EventContext();
                                eventContext.Add(EventContextFieldName.EventID, eventId);
                                eventContext.Item = CacheHelper.CreateCacheEventEntry(Runtime.Events.EventDataFilter.DataWithMetadata, entry);

                                size += entry.Size;
                                keysOfRemoveNotification.Add(ide.Key);
                                eventContexts.Add(eventContext);

                                if (size > sizeThreshhold || keysOfRemoveNotification.Count > countThreshhold)
                                {
                                    RaiseAsyncItemRemoveNotifier(keysOfRemoveNotification.ToArray(),null, reason, operationContext,eventContexts.ToArray());
                                    keysOfRemoveNotification.Clear();
                                    entriesOfRemoveNotification.Clear();
                                    eventContexts.Clear();
                                    size = 0;
                                }

                          

                                NotifyItemRemoved(ide.Key, entry, reason, true, operationContext,eventContext);
                            }
                            if (entry.Value is CallbackEntry)
                            {
                                EventId eventId = null;
                                OperationID opId = operationContext.OperatoinID;
                                CallbackEntry cbEtnry = (CallbackEntry)entry.Value;
                                EventContext eventContext = null;
                                
                                if (cbEtnry != null && cbEtnry.ItemRemoveCallbackListener != null && cbEtnry.ItemRemoveCallbackListener.Count > 0)
                                {
                                    //generate event id
                                    if (!operationContext.Contains(OperationContextFieldName.EventContext)) //for atomic operations
                                    {
                                        eventId = EventId.CreateEventId(opId);
                                    }
                                    else //for bulk
                                    {
                                        eventId = ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                                    }

                                    eventId.EventType = Alachisoft.NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK;
                                    eventContext = new EventContext();
                                    eventContext.Add(EventContextFieldName.EventID, eventId);
                                    EventCacheEntry eventCacheEntry = CacheHelper.CreateCacheEventEntry(cbEtnry.ItemRemoveCallbackListener, entry);
                                    eventContext.Item = eventCacheEntry;
                                    eventContext.Add(EventContextFieldName.ItemRemoveCallbackList, cbEtnry.ItemRemoveCallbackListener.Clone());

                                    RaiseAsyncCustomRemoveCalbackNotifier(ide.Key, entry, reason, operationContext, eventContext);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        NCacheLog.Error("PartitionedCache.RemoveSync", "an error occured while raising events. Error :" + ex.ToString());
                    }
                }

                if (keysOfRemoveNotification.Count > 0)
                {
                    RaiseAsyncItemRemoveNotifier(keysOfRemoveNotification.ToArray(), null, reason, operationContext,eventContexts.ToArray());
                }
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }

            return depenedentItemList;
        }

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleRemove"/> on every server node in the cluster. In
        /// a partition only one node can remove an item (due to partitioning of data). Therefore the
        /// <see cref="OnItemsRemoved"/> handler of the node actually removing the item is responsible
        /// for triggering a cluster-wide Item removed notification. 
        /// <para>
        /// <b>Note:</b> Evictions and Expirations are also handled through the 
        /// <see cref="OnItemsRemoved"/> handler.
        /// </para>
        /// </remarks>
        public override CacheEntry Remove(object key, string group, ItemRemoveReason ir, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Remove", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            bool suspectedErrorOccured = false;
            Address targetNode = null;
            CacheEntry entry = null;

            if (_internalCache == null) throw new InvalidOperationException();

            object actualKey = key;
            DataSourceUpdateOptions updateOptions = DataSourceUpdateOptions.None;
            CallbackEntry cbEntry = null;
            string providerName = null;

            if (key is object[])
            {
                object[] package = key as object[];
                actualKey = package[0];
                updateOptions = (DataSourceUpdateOptions)package[1];
                cbEntry = package[2] as CallbackEntry;
                if (package.Length > 3)
                {
                    providerName = package[3] as string;
                }
            }

            string taskId = null;
            if (updateOptions == DataSourceUpdateOptions.WriteBehind)
                taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

            while (true)
            {
                try
                {
                    targetNode = GetNextNode(actualKey as string, group);
                    if (targetNode != null)
                    {
                        if (targetNode.CompareTo(LocalAddress) == 0)
                        {
                            entry = Local_Remove(actualKey, ir, Cluster.LocalAddress, cbEntry, taskId, providerName, notify, lockId, version, accessType, operationContext);
                        }
                        else
                        {
                            entry = Clustered_Remove(targetNode, actualKey, ir, cbEntry, taskId, providerName, notify, lockId, version, accessType, operationContext);
                        }



                    }
                    break;

                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    Thread.Sleep(_serverFailureWaitTime);
                    suspectedErrorOccured = true;
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Remove", targetNode + " left while addition. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.Remove", targetNode + " operation timed out. Error: " + te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                    {
                        throw new GeneralFailureException(te.Message, te);
                    }
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(actualKey, group);
                }
            }
            if (notify && entry != null)
            {
                object value = entry.Value;

                if (value is CallbackEntry)
                {
                    RaiseCustomRemoveCalbackNotifier(actualKey, entry, ir);
                }                
            }

            if (entry != null)
            {
                RemoveDepKeyList(GetKeysTable(actualKey, entry.KeysIAmDependingOn), operationContext);
            }

            return entry;
        }

        private Hashtable OptimizedRemove(IList keys, string group, ItemRemoveReason ir, string taskId, string providerName, CallbackEntry cbEntry, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.RemoveBlk", "");

            Hashtable result = new Hashtable();
            Hashtable totalDepKeys = new Hashtable();

            ArrayList remainingKeys = new ArrayList();

            try
            {
                result = Local_Remove(keys, ir, Cluster.LocalAddress, cbEntry, taskId, providerName, notify, operationContext);
            }
            catch (Exception ex)
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    result[keys[i]] = new OperationFailedException(ex.Message, ex);
                }
            }

            if (result != null)
            {
                Hashtable resultClone = (Hashtable)result.Clone();

                IDictionaryEnumerator ie = resultClone.GetEnumerator();
                while (ie.MoveNext())
                {
                    if (ie.Value is StateTransferException)
                    {
                        remainingKeys.Add(ie.Key);
                        result.Remove(ie.Key); // do remove from result;
                    }
                }
            }

            if (result.Count > 0)
            {
                IDictionaryEnumerator ide = result.GetEnumerator();
                while (ide.MoveNext())
                {
                    object key = ide.Key;
                    CacheEntry entry = ide.Value as CacheEntry;
                    if (entry != null)
                    {
                        if (notify)
                        {

                            object value = entry.Value;
                            if (value is CallbackEntry)
                            {
                                RaiseCustomRemoveCalbackNotifier(key, entry, ir);
                            }

                            if (!notify)
                            {
                                EventContext eventContext = CreateEventContextForGeneralDataEvent(Persistence.EventType.ITEM_REMOVED_EVENT, operationContext, entry, null);
                                object data = new object[] { key, ir, operationContext, eventContext };
                                RaiseItemRemoveNotifier(data);
                                handleNotifyRemoval(data);
                            }
                        }
                        object[] depKeys = entry.KeysIAmDependingOn;
                        if (depKeys != null)
                        {
                            for (int i = 0; i < depKeys.Length; i++)
                            {
                                if (!totalDepKeys.Contains(depKeys[i]))
                                {
                                    totalDepKeys.Add(depKeys[i], new ArrayList());
                                }
                                ((ArrayList)totalDepKeys[(depKeys[i])]).Add(key);
                            }
                        }
                    }
                }

                RemoveDepKeyList(totalDepKeys, operationContext);
            }

            if (remainingKeys.Count > 0)
            {
                Hashtable tmpResult = ClusteredRemove(remainingKeys, group, ir, taskId, providerName, cbEntry, notify, operationContext);
                foreach (DictionaryEntry entry in tmpResult)
                {
                    result[entry.Key] = entry.Value;
                }
            }

            return result;
        }

        private Hashtable ClusteredRemove(IList keys, string group, ItemRemoveReason ir, string taskId, string providerName, CallbackEntry cbEntry, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.RemoveBlk", "");

            Hashtable targetNodes = null;
            Hashtable result = new Hashtable();
            Hashtable tmpResult = null;

            ClusteredArrayList totalKeys = new ClusteredArrayList(keys);
            ArrayList totalRemainingKeys = new ArrayList();

            Hashtable totalDepKeys = new Hashtable();

            Address targetNode = null;

            do
            {
                targetNodes = (Hashtable)GetTargetNodes(totalKeys, group);
                if (targetNodes != null && targetNodes.Count == 0)
                {
                    foreach (object key in totalKeys)
                    {
                        result[key] = new OperationFailedException("No target node available to accommodate the data.");
                    }
                    return result;
                }

                IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                Hashtable keyList = null;

                //We select one node at a time for Add operation.
                while (ide.MoveNext())
                {
                    targetNode = ide.Key as Address;
                    keyList = (Hashtable)ide.Value;

                    if (targetNode != null && keyList != null)
                    {
                        object[] currentKeys = MiscUtil.GetArrayFromCollection(keyList.Keys);
                        try
                        {
                            if (targetNode.Equals(Cluster.LocalAddress))
                            {
                                tmpResult = Local_Remove(currentKeys, ir, Cluster.LocalAddress, cbEntry, taskId, providerName, notify, operationContext);
                            }
                            else
                            {
                                tmpResult = Clustered_Remove(targetNode, currentKeys, ir, cbEntry, taskId, providerName, notify, operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            Thread.Sleep(_serverFailureWaitTime);
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedServerCache.Remove()", targetNode + " left while addition");
                            totalRemainingKeys.AddRange(currentKeys);
                            continue;
                        }
                        catch (Runtime.Exceptions.TimeoutException te)
                        {
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedServerCache.Remove()", targetNode + " operation timed out");
                            totalRemainingKeys.AddRange(currentKeys);
                            continue;
                        }

                        if (tmpResult != null)
                        {
                            IDictionaryEnumerator ie = tmpResult.GetEnumerator();
                            while (ie.MoveNext())
                            {
                                if (ie.Value is StateTransferException)
                                    totalRemainingKeys.Add(ie.Key);
                                else
                                {
                                    if (ie.Value is CacheEntry)
                                        result[ie.Key] = ie.Value;
                                }
                            }
                        }
                    }
                }

                totalKeys = new ClusteredArrayList(totalRemainingKeys);
                totalRemainingKeys.Clear();
            }
            while (totalKeys.Count > 0);

            if (result.Count > 0)
            {
                IDictionaryEnumerator ide = result.GetEnumerator();
                while (ide.MoveNext())
                {
                    object key = ide.Key;
                    CacheEntry entry = (CacheEntry)ide.Value;
                    //Already fired from LocalCacheBase
                    if (notify)
                    {
                        object value = entry.Value;
                        if (value is CallbackEntry)
                        {
                            RaiseCustomRemoveCalbackNotifier(key, entry, ir);
                        }
                    }
                    object[] depKeys = entry.KeysIAmDependingOn;
                    if (depKeys != null)
                    {
                        for (int i = 0; i < depKeys.Length; i++)
                        {
                            if (!totalDepKeys.Contains(depKeys[i]))
                            {
                                totalDepKeys.Add(depKeys[i], new ArrayList());
                            }
                            ((ArrayList)totalDepKeys[(depKeys[i])]).Add(key);
                        }
                    }
                }

                RemoveDepKeyList(totalDepKeys, operationContext);
            }

            return result;
        }
        /// <summary>
        /// Removes the key and value pairs from the cache. The keys are specified as parameter.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>list of removed keys</returns>
        /// <remarks>
        /// This method invokes <see cref="handleRemove"/> on every server node in the cluster. In
        /// a partition only one node can remove an item (due to partitioning of data). Therefore the
        /// <see cref="OnItemsRemoved"/> handler of the node actually removing the item is responsible
        /// for triggering a cluster-wide Item removed notification. 
        /// <para>
        /// <b>Note:</b> Evictions and Expirations are also handled through the 
        /// <see cref="OnItemsRemoved"/> handler.
        /// </para>
        /// </remarks>
        public override Hashtable Remove(IList keys, string group, ItemRemoveReason ir, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.Remove", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            DataSourceUpdateOptions updateOptions = DataSourceUpdateOptions.None;
            CallbackEntry cbEntry = null;
            string providerName = null;

            Hashtable result = new Hashtable();

            if (keys != null && keys.Count > 0)
            {
                if (keys[0] is object[])
                {
                    object[] package = keys[0] as object[];
                    updateOptions = (DataSourceUpdateOptions)package[1];
                    cbEntry = package[2] as CallbackEntry;
                    if (package.Length > 3)
                    {
                        providerName = package[3] as string;
                    }
                    keys[0] = package[0];
                }

                if (_internalCache == null) throw new InvalidOperationException();

                string taskId = null;
                if (updateOptions == DataSourceUpdateOptions.WriteBehind)
                    taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

                long clientLastViewId = GetClientLastViewId(operationContext);

                if (clientLastViewId == Cluster.LastViewID && !IsInStateTransfer())
                {
                    result = OptimizedRemove(keys, group, ir, taskId, providerName, cbEntry, notify, operationContext);
                }
                else
                {
                    result = ClusteredRemove(keys, group, ir, taskId, providerName, cbEntry, notify, operationContext);
                }
            }
            return result;
        }

        /// <summary>
        /// Remove the object from the local cache only. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheEntry Local_Remove(object key, ItemRemoveReason ir, Address src, CallbackEntry cbEntry, string taskId, string providerName, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            if (_internalCache != null)
            {
                retVal = _internalCache.Remove(key, ir, notify, lockId, version, accessType, operationContext);
                if (taskId != null && retVal != null)
                {
                    CacheEntry cloned = retVal;
                    cloned = retVal.Clone() as CacheEntry;
                    cloned.ProviderName = providerName;
                    if (cbEntry != null)
                    {
                        if (cloned.Value is CallbackEntry)
                        {
                            ((CallbackEntry)cloned.Value).WriteBehindOperationCompletedCallback = cbEntry.WriteBehindOperationCompletedCallback;
                        }
                        else
                        {
                            cbEntry.Value = cloned.Value;
                            cloned.Value = cbEntry;
                        }
                    }
                    base.AddWriteBehindTask(src, key as string, cloned, taskId, OpCode.Remove, WriteBehindAsyncProcessor.TaskState.Execute, operationContext);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Remove the objects from the local cache only. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>list of removed keys.</returns>
        private Hashtable Local_Remove(IList keys, ItemRemoveReason ir, Address src, CallbackEntry cbEntry, string taskId, string providerName, bool notify, OperationContext operationContext)
        {
            Hashtable removedKeys = null;


            if (_internalCache != null)
            {
                removedKeys = _internalCache.Remove(keys, ir, notify, operationContext);

                if (taskId != null && removedKeys != null && removedKeys.Count > 0)
                {
                    Hashtable writeBehindTable = new Hashtable();
                    for (int i = 0; i < keys.Count; i++)
                    {
                        CacheEntry entry = removedKeys[keys[i]] as CacheEntry;
                        if (entry != null)
                        {
                            entry.ProviderName = providerName;
                            writeBehindTable.Add(keys[i], entry);
                        }
                    }
                    if (writeBehindTable.Count > 0)
                    {
                        base.AddWriteBehindTask(src, writeBehindTable, cbEntry, taskId, OpCode.Remove, WriteBehindAsyncProcessor.TaskState.Execute);
                    }
                }

            }
            return removedKeys;
        }


        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        public override Hashtable Remove(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            // Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) 
                throw new InvalidOperationException();

            long clientLastViewId = GetClientLastViewId(operationContext);

            if (clientLastViewId != Cluster.LastViewID)
            {
                if (this.Cluster.IsCoordinator)
                {
                    ArrayList list = GetGroupKeys(group, subGroup, operationContext);
                    if (list != null && list.Count > 0)
                    {
                        object[] grpKeys = MiscUtil.GetArrayFromCollection(list);
                        return Remove(grpKeys, ItemRemoveReason.Removed, notify, operationContext);
                    }
                }

                return new Hashtable();
            }
            else
            {
                return Local_RemoveGroup(group, subGroup, notify, operationContext);
            }
        }

        protected override Hashtable Local_RemoveGroup(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            if (_internalCache == null) 
                throw new InvalidOperationException();
            ArrayList list = Local_GetGroupKeys(group, subGroup, operationContext);
            if (list != null && list.Count > 0)
            {
                object[] grpKeys = MiscUtil.GetArrayFromCollection(list);
                return Remove(grpKeys, ItemRemoveReason.Removed, notify, operationContext);
            }
            return null;
        }

        public override Hashtable Remove(string[] tags, TagComparisonType tagComparisonType, bool notify, OperationContext operationContext)
        {
            // Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            long ClientLastViewId = GetClientLastViewId(operationContext);
            if (ClientLastViewId != Cluster.LastViewID)
            {
                if (this.Cluster.IsCoordinator)
                {
                    ICollection list = GetTagKeys(tags, tagComparisonType, operationContext);
                    if (list != null && list.Count > 0)
                    {
                        object[] grpKeys = MiscUtil.GetArrayFromCollection(list);
                        return Remove(grpKeys, ItemRemoveReason.Removed, notify, operationContext);
                    }
                }
                
                return new Hashtable();
            }
            else
            {
                return Local_RemoveTag(tags, tagComparisonType, notify, operationContext);
            }
        }

        protected override Hashtable Local_RemoveTag(string[] tags, TagComparisonType tagComparisonType, bool notify, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();
            ICollection list = Local_GetTagKeys(tags, tagComparisonType, operationContext);
            if (list != null && list.Count > 0)
            {
                object[] grpKeys = MiscUtil.GetArrayFromCollection(list);
                return Remove(grpKeys, ItemRemoveReason.Removed, notify, operationContext);
            }
            return null;
        }

        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        private Hashtable Local_Remove(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.Remove(group, subGroup, notify, operationContext);
            return null;
        }

        /// <summary>
        /// Hanlde cluster-wide Remove(key) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        /// <remarks>
        /// This method triggers <see cref="OnItemsRemoved"/>, which then triggers a cluster-wide 
        /// Item removed notification. 
        /// </remarks>
        private object handleRemove(Address src, object info)
        {
            try
            {
                object[] param = (object[])info;
                CallbackEntry cbEntry = null;
                string taskId = null;
                string providerName = null;
                OperationContext oc = null;

                if (param.Length > 3)
                    cbEntry = param[3] as CallbackEntry;
                if (param.Length > 4)
                    taskId = param[4] as string;
                if (param.Length > 8)
                    providerName = param[8] as string;

                if (param.Length == 3)
                    oc = param[2] as OperationContext;

                if (param.Length == 10)
                    oc = param[9] as OperationContext;

                if (param.Length == 7)
                    oc = param[6] as OperationContext;

                if (param[0] is object[])
                {
                    if (param.Length > 5)
                    {
                        providerName = param[5] as string;
                    }
                    Hashtable table = Local_Remove((object[])param[0], (ItemRemoveReason)param[1], src, cbEntry, taskId, providerName, (bool)param[2], oc);
                    
                    return table;
                }
                else
                {
                    object lockId = param[5];
                    LockAccessType accessType = (LockAccessType)param[6];
                    ulong version = (ulong)param[7];
                    CacheEntry e = Local_Remove(param[0], (ItemRemoveReason)param[1], src, cbEntry, taskId, providerName, (bool)param[2], lockId, version, accessType, oc);
                    OperationResponse opRes = new OperationResponse();
                    if (e != null)
                    {
                        if (_context.InMemoryDataFormat.Equals(DataFormat.Object))
                        {
                            opRes.UserPayload = null;
                            opRes.SerializablePayload = e.Clone();
                        }
                        else
                        {
                            UserBinaryObject ubObject = (UserBinaryObject)(e.Value is CallbackEntry ? ((CallbackEntry)e.Value).Value : e.Value);
                            opRes.UserPayload = ubObject.Data;
                            opRes.SerializablePayload = e.CloneWithoutValue();
                        }
                    }
                    return opRes;
                }                             
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }


        /// <summary>
        /// Hanlde cluster-wide RemoveGroup(group, subGroup) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        /// <remarks>
        /// This method triggers <see cref="OnItemsRemoved"/>, which then triggers a cluster-wide 
        /// Item removed notification. 
        /// </remarks>
        private object handleRemoveGroup(object info)
        {
            try
            {
                object[] param = (object[])info;
                string group = param[0] as string;
                string subGroup = param[1] as string;
                bool notify = (bool)param[2];

                OperationContext operationContext = null;

                if (param.Length > 3)
                    operationContext = param[3] as OperationContext;

                return Local_RemoveGroup(group, subGroup, notify, operationContext);
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }

            return null;
        }

        #endregion

        #region	/                 --- Partitioned ICache.GetEnumerator ---           /

        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        /// <returns>IDictionaryEnumerator enumerator.</returns>
        public override IDictionaryEnumerator GetEnumerator()
        {
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null)
                throw new InvalidOperationException();

            IDictionaryEnumerator localEnumerator = new LazyPartitionedKeysetEnumerator(this, (object[])handleKeyList(), Cluster.LocalAddress, true);

            if (Cluster.Servers.Count == 1)
                return localEnumerator;


            return Clustered_GetEnumerator(Cluster.Servers, localEnumerator);
        }

        public override EnumerationDataChunk GetNextChunk(EnumerationPointer pointer, OperationContext operationContext)
        {
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null)
                throw new InvalidOperationException();

            EnumerationDataChunk nextChunk = null;

            long clientLastViewId = GetClientLastViewId(operationContext);
            string intenededRecepient = GetIntendedRecipient(operationContext);
            Array servers = Array.CreateInstance(typeof(Address), Cluster.Servers.Count);
            Cluster.Servers.CopyTo(servers);
            Address targetNode = null;

            if (clientLastViewId == -1 && !string.IsNullOrEmpty(intenededRecepient))
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
                        nextChunk = InternalCache.GetNextChunk(pointer, operationContext);
                    else
                        nextChunk = Clustered_GetNextChunk(targetNode, pointer, operationContext);
                }
                else
                {
                    nextChunk = new EnumerationDataChunk();
                    nextChunk.Pointer = pointer;
                }
            }
            else
            {
                nextChunk = InternalCache.GetNextChunk(pointer, operationContext);
            }

            return nextChunk;
        }

        /// <summary>
        /// Hanlde cluster-wide KeyList requests.
        /// </summary>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleKeyList()
        {
            try
            {
                return MiscUtil.GetKeyset(_internalCache, Convert.ToInt32(Cluster.Timeout));
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        #endregion



        #region	/                 --- OnCacheCleared ---           /

        /// <summary> 
        /// Fire when the cache is cleared. 
        /// </summary>
        void ICacheEventsListener.OnCacheCleared(OperationContext operationContext, EventContext eventContext)
        {
            // do local notifications only, every node does that, so we get a replicated notification.
            UpdateCacheStatistics();
            handleNotifyCacheCleared();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="data"></param>
        void ICacheEventsListener.OnCustomEvent(object notifId, object data, OperationContext operationContext, EventContext eventContext)
        {
        }

        /// <summary>
        /// Hanlder for clustered cache clear notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private object handleNotifyCacheCleared()
        {
            NotifyCacheCleared(true, null, null);
            return null;
        }

        #endregion

        #region	/                 --- OnItemAdded ---           /

        /// <summary> 
        /// Fired when an item is added to the cache. 
        /// </summary>
        /// <remarks>
        /// Triggers a cluster-wide item added notification.
        /// </remarks>
        void ICacheEventsListener.OnItemAdded(object key, OperationContext operationContext, EventContext eventContext)
        {
            // Handle all exceptions, do not let the effect seep thru
            try
            {
                FilterEventContextForGeneralDataEvents(Runtime.Events.EventType.ItemAdded, eventContext);
                // do not broad cast if there is only one node.
                if (IsItemAddNotifier && ValidMembers.Count > 1)
                {

                    object notification = new Function((int)OpCodes.NotifyAdd, new object[] { key, operationContext, eventContext });
                    RaiseGeneric(notification);
                    handleNotifyAdd(new object[] { key, operationContext, eventContext });
                }
                else
                {
                    handleNotifyAdd(new object[] { key, operationContext, eventContext });
                }
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.OnItemAdded()", "key: " + key.ToString());
            }
            catch (Exception e)
            {
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.OnItemAdded()", e.ToString());
            }
        }


        /// <summary>
        /// Hanlder for clustered item added notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private object handleNotifyAdd(object info)
        {
            object[] args = info as object[];
            NotifyItemAdded(args[0], true, (OperationContext)args[1], (EventContext)args[2]);
            return null;

        }


        #endregion

        #region	/                 --- OnItemUpdated ---           /

        /// <summary> 
        /// handler for item updated event.
        /// </summary>
        void ICacheEventsListener.OnItemUpdated(object key, OperationContext operationContext, EventContext eventContext)
        {
            // Handle all exceptions, do not let the effect seep thru
            try
            {
                FilterEventContextForGeneralDataEvents(Runtime.Events.EventType.ItemRemoved, eventContext);
                object[] packedData = new object[] { key, operationContext, eventContext };
                // do not broad cast if there is only one node.
                if (IsItemUpdateNotifier && ValidMembers.Count > 1)
                {
                    object notification = new Function((int)OpCodes.NotifyUpdate, packedData);
                    RaiseGeneric(notification);
                    handleNotifyUpdate(packedData);
                }
                else
                {
                    handleNotifyUpdate(packedData);
                }
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.OnItemUpdated()", "key: " + key.ToString());
            }
            catch (Exception e)
            {
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.OnItemUpdated()", "Key: " + key.ToString() + " Error: " + e.ToString());
            }
        }

        /// <summary>
        /// Hanlder for clustered item updated notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private object handleNotifyUpdate(object info)
        {
            object[] args = info as object[];
            if (args != null)
            {
                OperationContext opContext = null;
                EventContext evContext = null;
                if (args.Length > 1)
                    opContext = args[1] as OperationContext;
                if (args.Length > 2)
                    evContext = args[2] as EventContext;

                NotifyItemUpdated(args[0], true, opContext, evContext);
            }
            else
            {
                NotifyItemUpdated(info, true, null, null);
            }
            return null;
        }


        #endregion



        void ICacheEventsListener.OnActiveQueryChanged(object key, QueryChangeType changeType, System.Collections.Generic.List<CQCallbackInfo> activeQueries, OperationContext operationContext, EventContext eventContext)
        {

            RaiseCQCallbackNotifier((string)key, changeType, activeQueries, operationContext, eventContext);
        }


        #region	/                 --- OnItemRemoved ---           /

        /// <summary> 
        /// Fired when an item is removed from the cache.
        /// </summary>
        void ICacheEventsListener.OnItemRemoved(object key, object val, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext)
        {
            ((ICacheEventsListener)this).OnItemsRemoved(new object[] { key }, new object[] { val }, reason, operationContext, new EventContext[] { eventContext } );
        }

        /// <summary> 
        /// Fired when multiple items are removed from the cache. 
        /// </summary>
        /// <remarks>
        /// In a partition only one node can remove an item (due to partitioning of data). 
        /// Therefore this handler triggers a cluster-wide Item removed notification.
        /// </remarks>
        void ICacheEventsListener.OnItemsRemoved(object[] keys, object[] values, ItemRemoveReason reason, OperationContext operationContext, EventContext[] eventContext)
        {
            // Handle all exceptions, do not let the effect seep thru
            try
            {
                if (IsItemRemoveNotifier && ValidMembers.Count > 1)
                {
                    object notification = null;

                    CacheEntry entry;
                    for (int i = 0; i < keys.Length; i++)
                    {
                        FilterEventContextForGeneralDataEvents(Runtime.Events.EventType.ItemRemoved, eventContext[i]);
                        object data = new object[] { keys[i], reason, operationContext, eventContext[i] };
                        

                        notification = new Function((int)OpCodes.NotifyRemoval, data);
                        RaiseGeneric(notification);
                    }
                    NotifyItemsRemoved(keys, null, reason, true, operationContext, eventContext);
                }

                else
                {
                    NotifyItemsRemoved(keys, null, reason, true, operationContext, eventContext);
                }
            }
            catch (Exception e)
            {
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ParitionedCache.OnItemsRemoved()", e.ToString());
            }
        }

        /// <summary>
        /// Hanlder for clustered item removal notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private object handleNotifyRemoval(object info)
        {
            object[] objs = (object[])info;
            OperationContext operationContext = null;
           
            EventContext evContext = null;
            if (objs.Length > 2)
                operationContext = objs[2] as OperationContext;
            if (objs.Length > 3)
                evContext = objs[3] as EventContext;

            NotifyItemRemoved(objs[0], null, (ItemRemoveReason)objs[1], true, operationContext, evContext);
            return null;
        }

        /// <summary>
        /// Hanlder for clustered item removal notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private object handleNotifyBulkRemoval(object info)
        {
            OperationContext operationContext = null;
            object[] objs = (object[])info;
            object[] keys = objs[0] as object[];
            object[] values = objs[1] as object[];
            ItemRemoveReason reason = (ItemRemoveReason)objs[2];
            EventContext[] eventContexts = null;
            if (objs.Length > 3)
            {
                operationContext = objs[3] as OperationContext;
                eventContexts = objs[4] as EventContext[];
            }

            if (keys != null)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    NotifyItemRemoved(keys[i], null, reason, true, operationContext,eventContexts[i]);
                }
            }

            return null;
        }

        #endregion

        #region	/                 --- OnCustomUpdateCallback ---           /

        /// <summary> 
        /// handler for item update callback event.
        /// </summary>
        void ICacheEventsListener.OnCustomUpdateCallback(object key, object value, OperationContext operationContext, EventContext eventContext)
        {            
            if (value != null)
            {
                RaiseCustomUpdateCalbackNotifier(key, (ArrayList)value, eventContext);
            }
        }


        #endregion

        #region	/                 --- OnCustomRemoveCallback ---           /

        /// <summary> 
        /// handler for item remove callback event.
        /// </summary>
        void ICacheEventsListener.OnCustomRemoveCallback(object key, object entry, ItemRemoveReason removalReason, OperationContext operationContext, EventContext eventContext)
        {
            bool notifyRemove = false;
            object notifyRemoval = operationContext.GetValueByField(OperationContextFieldName.NotifyRemove);

            if (notifyRemoval != null)
                notifyRemove = (bool)notifyRemoval;

            if ((removalReason == ItemRemoveReason.Removed || removalReason == ItemRemoveReason.DependencyChanged || removalReason == ItemRemoveReason.DependencyInvalid) && !(bool)notifyRemove) return;

            if (entry != null)
            {
                RaiseCustomRemoveCalbackNotifier(key, (CacheEntry)entry, removalReason, operationContext, eventContext);
            }
        }

        #endregion

        /// <summary>
        /// Fire when hasmap changes when 
        /// - new node joins
        /// - node leaves
        /// - manual/automatic load balance
        /// </summary>
        /// <param name="newHashmap">new hashmap</param>
        void ICacheEventsListener.OnHashmapChanged(NewHashmap newHashmap, bool updateClientMap)
        {

        }

        #region	/                 --- OnWriteBehindOperationCompletedCallback ---           /

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operationCode"></param>
        /// <param name="result"></param>
        /// <param name="cbEntry"></param>
        void ICacheEventsListener.OnWriteBehindOperationCompletedCallback(OpCode operationCode, object result, CallbackEntry cbEntry)
        {
        }

        #endregion

        void ICacheEventsListener.OnPollNotify(string clientId, short callbackId, Alachisoft.NCache.Runtime.Events.EventType eventtype)
        {
            try
            {
                RaisePollRequestNotifier(clientId, callbackId, eventtype);
            }
            catch (Exception e)
            {
                Context.NCacheLog.Warn("Partitioned.OnPollNotify", "failed: " + e.ToString());
            }
        }
     

        #region /               --- Key based notification  ---     /

        public override object handleRegisterKeyNotification(object operand)
        {
            object[] operands = operand as object[];
            if (operands != null)
            {
                object Keys = operands[0];
                CallbackInfo updateCallback = operands[1] as CallbackInfo;
                CallbackInfo removeCallback = operands[2] as CallbackInfo;
                OperationContext operationContext = operands[3] as OperationContext;
                if (_internalCache != null)
                {
                    if (Keys is object[])
                    {
                        _internalCache.RegisterKeyNotification((string[])Keys, updateCallback, removeCallback, operationContext);
                    }
                    else
                    {
                        _internalCache.RegisterKeyNotification((string)Keys, updateCallback, removeCallback, operationContext);
                    }
                }
            }
            return null;
        }

        public override PollingResult handlePoll(object operand)
        {
            object[] operands = operand as object[];
            if (operands != null)
            {
                OperationContext operationContext = operands[0] as OperationContext;
                if (_internalCache != null)
                {
                    return _internalCache.Poll(operationContext);
                }
            }
            return null;
        }
  
        public override object handleRegisterPollingNotification(object operand)
        {
            object[] operands = operand as object[];
            if (operands != null)
            {
                short callbackId = (short)operands[0];
                OperationContext operationContext = operands[1] as OperationContext;
                if (_internalCache != null)
                {
                    _internalCache.RegisterPollingNotification(callbackId, operationContext);
                }
            }
            return null;
        }
       
        public override object handleUnregisterKeyNotification(object operand)
        {
            object[] operands = operand as object[];
            if (operands != null)
            {
                object Keys = operands[0];
                CallbackInfo updateCallback = operands[1] as CallbackInfo;
                CallbackInfo removeCallback = operands[2] as CallbackInfo;
                OperationContext operationContext = operands[3] as OperationContext;
                if (_internalCache != null)
                {
                    if (Keys is object[])
                    {
                        _internalCache.UnregisterKeyNotification((string[])Keys, updateCallback, removeCallback, operationContext);
                    }
                    else
                    {
                        _internalCache.UnregisterKeyNotification((string)Keys, updateCallback, removeCallback, operationContext);
                    }
                }
            }
            return null;
        }

        public override Common.Events.PollingResult Poll(OperationContext context)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.Poll", "");

            PollingResult result = null;

            if (_internalCache == null)
                throw new InvalidOperationException();
       
            long clientLastViewId = GetClientLastViewId(context);

            if (clientLastViewId == Cluster.LastViewID && !IsInStateTransfer())
            {
                result = Local_Poll(context);
            }
            else
            {
                result = Clustered_Poll(context);
            }

            return result;

        }

        public override void RegisterPollingNotification(short callbackId, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.RegisterPoll", "");

            if (_internalCache == null)
                throw new InvalidOperationException();

            long clientLastViewId = GetClientLastViewId(context);

            if (clientLastViewId == Cluster.LastViewID && !IsInStateTransfer())
            {
                Local_RegisterPollingNotification(callbackId, context);
            }
            else
            {
                Clustered_RegisterPollingNotification(callbackId, context);
            }
        }

        protected override void Local_RegisterPollingNotification(short callbackId, OperationContext context)
        {
            _internalCache.RegisterPollingNotification(callbackId, context);
        }

        protected override PollingResult Local_Poll(OperationContext context)
        {
            return _internalCache.Poll(context);
        }
       
        public override void RegisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] { key, updateCallback, removeCallback, operationContext };
            Address targetNode = null;
            do
            {
                try
                {
                    targetNode = GetNextNode(key, null);
                    if (targetNode.Equals(Cluster.LocalAddress))
                    {
                        handleRegisterKeyNotification(obj);
                    }
                    else
                    {
                        Function fun = new Function((byte)OpCodes.RegisterKeyNotification, obj, false);
                        object results = Cluster.SendMessage(targetNode, fun, GetFirstResponse, false);
                    }
                    break;
                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    Thread.Sleep(_serverFailureWaitTime);
                    continue;
                }
                catch (Alachisoft.NGroups.SuspectedException se)
                {
                    if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartServerCache.RegisterKeyNotification", targetNode + " left while Registering notification");
                    continue;
                }
                catch (Alachisoft.NCache.Common.Exceptions.TimeoutException te)
                {
                    if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartServerCache.RegisterKeyNotification", targetNode + " operation timed out");
                    throw te;
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, null);
                }
            }
            while (true);
        }

        public override void RegisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = null;
            Hashtable targetNodes = null;
            Hashtable result = new Hashtable();

            ClusteredArrayList totalKeys = new ClusteredArrayList(keys);
            Address targetNode = null;
            string[] currentKeys = null;
            targetNodes = (Hashtable)GetTargetNodes(totalKeys, null);

            if (targetNodes != null && targetNodes.Count != 0)
            {
                IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                Hashtable keyList = null;


                while (ide.MoveNext())
                {
                    targetNode = ide.Key as Address;
                    keyList = (Hashtable)ide.Value;

                    if (targetNode != null && keyList != null)
                    {
                        currentKeys = new string[keyList.Count];
                        int j = 0;
                        foreach (object key in keyList.Keys)
                        {
                            int index = totalKeys.IndexOf(key);
                            if (index != -1)
                            {
                                currentKeys[j] = (string)totalKeys[index];
                                j++;
                            }
                        }

                        try
                        {
                            obj = new object[] { currentKeys, updateCallback, removeCallback, operationContext };
                            if (targetNode.Equals(Cluster.LocalAddress))
                            {
                                handleRegisterKeyNotification(obj);
                            }
                            else
                            {
                                Function fun = new Function((byte)OpCodes.RegisterKeyNotification, obj, false);
                                object rsp = Cluster.SendMessage(targetNode, fun, GetFirstResponse, false);
                            }
                        }
                        catch (Alachisoft.NGroups.SuspectedException se)
                        {
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartServerCache.RegisterKeyNotification", targetNode + " left while Registering notification");
                            throw se;
                        }
                        catch (Alachisoft.NCache.Common.Exceptions.TimeoutException te)
                        {
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartServerCache.RegisterKeyNotification", targetNode + " operation timed out");
                            throw te;
                        }
                        catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                        {
                            throw se;
                        }

                    }
                }
            }
        }

        public override void UnregisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] { key, updateCallback, removeCallback, operationContext };
            Address targetNode = null;
            do
            {
                try
                {
                    targetNode = GetNextNode(key, null);
                    if (targetNode.Equals(Cluster.LocalAddress))
                    {
                        handleUnregisterKeyNotification(obj);
                    }
                    else
                    {
                        Function fun = new Function((byte)OpCodes.UnregisterKeyNotification, obj, false);
                        object results = Cluster.SendMessage(targetNode, fun, GetFirstResponse, false);
                    }
                    break;
                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    Thread.Sleep(_serverFailureWaitTime);
                    continue;
                }
                catch (Alachisoft.NGroups.SuspectedException se)
                {
                    if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartServerCache.UnRegisterKeyNotification", targetNode + " left while Registering notification");
                    continue;
                }
                catch (Alachisoft.NCache.Common.Exceptions.TimeoutException te)
                {
                    if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartServerCache.UnRegisterKeyNotification", targetNode + " operation timed out");
                    throw te;
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, null);
                }
            }
            while (true);
        }

        public override void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = null;
            Hashtable targetNodes = null;

            ClusteredArrayList totalKeys = new ClusteredArrayList(keys);
            Address targetNode = null;
            string[] currentKeys = null;
            targetNodes = (Hashtable)GetTargetNodes(totalKeys, null);

            if (targetNodes != null && targetNodes.Count != 0)
            {
                IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                Hashtable keyList = null;


                while (ide.MoveNext())
                {
                    targetNode = ide.Key as Address;
                    keyList = (Hashtable)ide.Value;

                    if (targetNode != null && keyList != null)
                    {
                        currentKeys = new string[keyList.Count];
                        int j = 0;
                        foreach (object key in keyList.Keys)
                        {
                            int index = totalKeys.IndexOf(key);
                            if (index != -1)
                            {
                                currentKeys[j] = (string)totalKeys[index];
                                j++;
                            }
                        }

                        try
                        {
                            obj = new object[] { currentKeys, updateCallback, removeCallback, operationContext };
                            if (targetNode.Equals(Cluster.LocalAddress))
                            {
                                handleUnregisterKeyNotification(obj);
                            }
                            else
                            {
                                Function fun = new Function((byte)OpCodes.UnregisterKeyNotification, obj, false);
                                object rsp = Cluster.SendMessage(targetNode, fun, GetFirstResponse, false);
                            }
                        }
                        catch (Alachisoft.NGroups.SuspectedException se)
                        {
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartServerCache.UnRegisterKeyNotification", targetNode + " left while Registering notification");
                            throw se;
                        }
                        catch (Alachisoft.NCache.Common.Exceptions.TimeoutException te)
                        {
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartServerCache.UnRegisterKeyNotification", targetNode + " operation timed out");
                            throw te;
                        }
                        catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                        {
                            throw se;
                        }

                    }
                }
            }
        }


        #endregion

        #region IDistributionPolicyMember Members

        public override void EmptyBucket(int bucketId)
        {
            InternalCache.RemoveBucketData(bucketId);
            //Announce all the members that i am the new owner of this bucket....
            ArrayList bucketIds = new ArrayList(1);
            bucketIds.Add(bucketId);
            ReleaseBuckets(bucketIds);
        }

        public override DistributionMaps GetDistributionMaps(DistributionInfoData distInfo)
        {
            NCacheLog.Debug("PartitionedCache.GetHashMap()", "here comes the request for hashmap");
            distInfo.Group = null;
            return this._distributionMgr.GetMaps(distInfo);
        }

        public override ArrayList ActiveServers
        {
            get
            {
                return Servers;
            }
        }

        public override ArrayList HashMap
        {
            get { return _distributionMgr.InstalledHashMap; }
            set { _distributionMgr.InstalledHashMap = value; }
        }

        public override Hashtable BucketsOwnershipMap
        {
            get { return _distributionMgr.BucketsOwnershipMap; }
            set { _distributionMgr.BucketsOwnershipMap = value; }
        }

        public override NewHashmap GetOwnerHashMapTable(out int bucketSize)
        {
            ArrayList membersList = GetClientMappedServers(this.Servers.Clone() as ArrayList);

            return new NewHashmap(Cluster.LastViewID,
                _distributionMgr.GetOwnerHashMapTable(Cluster.Renderers, out bucketSize),
                membersList);
        }

        public override void InstallHashMap(DistributionMaps distributionMaps, ArrayList leftMbrs)
        {
            _distributionMgr.InstallHashMap(distributionMaps, leftMbrs);
        }

        protected override DistributionMaps GetMaps(DistributionInfoData info)
        {
            return _distributionMgr.GetMaps(info);
        }


        #endregion

        #region lock

        private LockOptions Local_Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
            return null;
        }

        private LockOptions handleLock(object info)
        {
            object[] package = info as object[];
            object key = package[0];
            object lockId = package[1];
            DateTime lockDate = (DateTime)package[2];
            LockExpiration lockExpiration = (LockExpiration)package[3];
            OperationContext operationContext = package[4] as OperationContext;

            return Local_Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
        }

        public override LockOptions Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCache.lock", "lock_id :" + lockId);

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            LockOptions lockInfo = null;
            Address address = null;
            bool suspectedErrorOccured = false;

            while (true)
            {
                address = GetNextNode(key as string, null);

                if (address == null)
                {
                    NCacheLog.Error("PartitionedServerCache.lock()", "specified key does not map to any node. return.");
                    return null;
                }

                try
                {
                    if (address.CompareTo(Cluster.LocalAddress) == 0)
                    {
                        lockInfo = Local_Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
                    }
                    else
                    {
                        lockInfo = Clustered_Lock(address, key, lockExpiration, ref lockId, ref lockDate, operationContext);
                    }
                    return lockInfo;
                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    Thread.Sleep(_serverFailureWaitTime);
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.lock", address + " left while trying to lock the key. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.lock", address + " operation timed out. Error: " + te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                    {
                        throw new GeneralFailureException(te.Message, te);
                    }
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, null);
                }
            }
            return lockInfo;
        }

        public override void UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PartCache.Unlock", "lock_id :" + lockId);
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            Address address = null;
            bool suspectedErrorOccured = false;

            while (true)
            {
                address = GetNextNode(key as string, null);

                if (address == null)
                {
                    NCacheLog.Error("PartitionedServerCache.unlock()", "specified key does not map to any node. return.");
                }

                try
                {
                    if (address.CompareTo(Cluster.LocalAddress) == 0)
                    {
                        Local_UnLock(key, lockId, isPreemptive, operationContext);
                        break;
                    }
                    else
                    {
                        Clustered_UnLock(address, key, lockId, isPreemptive, operationContext);
                        break;
                    }
                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    Thread.Sleep(_serverFailureWaitTime);
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.unlock", address + " left while trying to lock the key. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.unlock", address + " operation timed out. Error: " + te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                    {
                        throw new GeneralFailureException(te.Message, te);
                    }
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, null);
                }
            }
        }

        private void Local_UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (_internalCache != null)
                _internalCache.UnLock(key, lockId, isPreemptive, operationContext);
        }

        private void handleUnLock(object info)
        {
            object[] package = info as object[];
            object key = package[0];
            object lockId = package[1];
            bool isPreemptive = (bool)package[2];
            OperationContext operationContext = package[3] as OperationContext;

            Local_UnLock(key, lockId, isPreemptive, operationContext);
        }

        private LockOptions Local_IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.IsLocked(key, ref lockId, ref lockDate, operationContext);
            return null;
        }

        private LockOptions handleIsLocked(object info)
        {
            object[] package = info as object[];
            object key = package[0];
            object lockId = package[1];
            DateTime lockDate = (DateTime)package[2];
            OperationContext operationContext = null;
            if (package.Length > 3)
                operationContext = package[3] as OperationContext;

            return Local_IsLocked(key, ref lockId, ref lockDate, operationContext);
        }

        public override LockOptions IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            LockOptions lockInfo = null;
            Address address = null;
            bool suspectedErrorOccured = false;

            while (true)
            {
                address = GetNextNode(key as string, null);

                if (address == null)
                {
                    NCacheLog.Error("PartitionedServerCache.lock()", "specified key does not map to any node. return.");
                    return null;
                }

                try
                {
                    if (address.CompareTo(Cluster.LocalAddress) == 0)
                    {
                        lockInfo = Local_IsLocked(key, ref lockId, ref lockDate, operationContext);
                    }
                    else
                    {
                        lockInfo = Clustered_IsLocked(address, key, ref lockId, ref lockDate, operationContext);
                    }
                    return lockInfo;
                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    Thread.Sleep(_serverFailureWaitTime);
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.lock", address + " left while trying to lock the key. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedServerCache.lock", address + " operation timed out. Error: " + te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                    {
                        throw new GeneralFailureException(te.Message, te);
                    }
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, null);
                }
            }
            return lockInfo;
        }



        #endregion

        /// <summary>
        /// Gets the node status.
        /// </summary>
        /// <returns></returns>
        protected override CacheNodeStatus GetNodeStatus()
        {
            CacheNodeStatus status = CacheNodeStatus.Running;

            //Check for state transfer.
            if (_stateTransferTask != null && _stateTransferTask.IsRunning)
                status = CacheNodeStatus.InStateTransfer;

            if (_corresponders != null && _corresponders.Count > 0)
                status = CacheNodeStatus.InStateTransfer;

            return status;
        }

        #region /                   --- Stream Operations---                /

        public override bool OpenStream(string key, string lockHandle, StreamModes mode, string group, string subGroup, ExpirationHint hint, EvictionHint evictinHint, OperationContext operationContext)
        {
            bool lockAcquired = false;
            _statusLatch.WaitForAny(NodeStatus.Running);
            int redoCount = 3;
            bool suspectedErrorOccured = false;

            #region -- PART I -- Cascading Dependency Operation
            object[] keys = CacheHelper.GetKeyDependencyTable(hint);
            if (keys != null && mode == StreamModes.Write)
            {
                Hashtable goodKeysTable = Contains(keys, operationContext);
                if (!goodKeysTable.ContainsKey("items-found"))
                    throw new OperationFailedException("One of the dependency keys does not exist.", false);

                if (goodKeysTable["items-found"] == null)
                    throw new OperationFailedException("One of the dependency keys does not exist.", false);

                if (goodKeysTable["items-found"] == null || ((ArrayList)goodKeysTable["items-found"]).Count != keys.Length)
                    throw new OperationFailedException("One of the dependency keys does not exist.", false);

            }
            #endregion

            do
            {
                try
                {
                    Address targetNode = GetNextNode(key, group);
                    OpenStreamResult result = null;
                    if (targetNode != null)
                    {
                        if (targetNode.Equals(Cluster.LocalAddress))
                        {
                            lockAcquired = InternalCache.OpenStream(key, lockHandle, mode, group, subGroup, hint, evictinHint, operationContext);
                        }
                        else
                        {
                            OpenStreamOperation streamOperation = new OpenStreamOperation(key, lockHandle, mode, group, subGroup, hint, evictinHint, operationContext);
                            Function func = new Function((int)OpCodes.OpenStream, streamOperation);

                            result = Cluster.SendMessage(targetNode, func, GroupRequest.GET_ALL) as OpenStreamResult;
                            lockAcquired = result.LockAcquired;
                        }
                    }
                    break;
                }
                catch (StreamException streamExc)
                {
                    throw;
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, group);
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.OpendStream()", te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                        throw;
                }
                catch (Runtime.Exceptions.SuspectedException e)
                {
                    suspectedErrorOccured = true;
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.OpendStream()", e.ToString());
                    if (redoCount == 0)
                        throw;
                    redoCount--;
                }
            } while (redoCount > 0);

            try
            {
                #region -- PART II -- Cascading Dependency Operation
                if (lockAcquired && mode == StreamModes.Write)
                {
                    //Hashtable table = null;
                    Hashtable ret = null;
                    Hashtable keysTable = new Hashtable();
                    try
                    {
                        keysTable = GetKeysTable(key, keys);
                        if (keysTable != null)
                            ret = AddDepKeyList(keysTable, operationContext);
                    }
                    catch (Exception e)
                    {

                        Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                        throw e;
                    }
                    if (ret != null)
                    {
                        IDictionaryEnumerator en = ret.GetEnumerator();
                        while (en.MoveNext())
                        {
                            if (en.Value is bool && !((bool)en.Value))
                            {
                                Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                                NCacheLog.Info("PartitionedServerCache.OpenStream(): ", "One of the dependency keys does not exist. Key: " + en.Key.ToString());
                                throw new OperationFailedException("One of the dependency keys does not exist.", false);
                            }
                        }
                    }
                }
                #endregion
            }
            finally
            {
            }
            return lockAcquired;
        }

        public override void CloseStream(string key, string lockHandle, OperationContext operationContext)
        {
            _statusLatch.WaitForAny(NodeStatus.Running);

            int redoCount = 3;
            bool suspectedErrorOccured = false;
            do
            {
                try
                {
                    Address targetNode = GetNextNode(key, null);

                    if (targetNode != null)
                    {
                        if (targetNode.Equals(Cluster.LocalAddress))
                        {
                            InternalCache.CloseStream(key, lockHandle, operationContext);
                        }
                        else
                        {
                            CloseStreamOperation streamOperation = new CloseStreamOperation(key, lockHandle, operationContext);
                            Function func = new Function((int)OpCodes.CloseStream, streamOperation);
                            Cluster.SendMessage(targetNode, func, GroupRequest.GET_ALL);
                        }
                    }
                    return;
                }
                catch (StreamException streamExc)
                {
                    throw;
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, null);
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.CloseStream()", te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                        throw;
                }
                catch (Runtime.Exceptions.SuspectedException e)
                {
                    suspectedErrorOccured = true;
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.CloseStream()", e.ToString());
                    if (redoCount == 0)
                        throw;
                    redoCount--;
                    Thread.Sleep(_serverFailureWaitTime);
                }
            } while (redoCount > 0);

        }

        public override int ReadFromStream(ref VirtualArray vBuffer, string key, string lockHandle, int offset, int length, OperationContext operationContext)
        {
            _statusLatch.WaitForAny(NodeStatus.Running);
            int redoCount = 3;
            int bytesRead = 0;
            bool suspectedErrorOccured = false;

            do
            {
                try
                {
                    Address targetNode = GetNextNode(key, null);
                    ReadFromStreamResult readResult = null;
                    if (targetNode != null)
                    {
                        if (targetNode.Equals(Cluster.LocalAddress))
                        {
                            bytesRead = InternalCache.ReadFromStream(ref vBuffer, key, lockHandle, offset, length, operationContext);
                        }
                        else
                        {
                            ReadFromStreamOperation streamOperation = new ReadFromStreamOperation(key, lockHandle, offset, length, operationContext);
                            Function func = new Function((int)OpCodes.ReadFromStream, streamOperation);

                            readResult = Cluster.SendMessage(targetNode, func, GroupRequest.GET_ALL) as ReadFromStreamResult;
                            if (readResult != null)
                            {
                                vBuffer = readResult.Buffer;
                                bytesRead = readResult.BytesRead;
                            }
                        }
                    }
                    return bytesRead;
                }
                catch (StreamException streamExc)
                {
                    throw;
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, null);
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.ReadFromStream()", te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                        throw;
                }
                catch (Runtime.Exceptions.SuspectedException e)
                {
                    suspectedErrorOccured = true;
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.ReadFromStream()", e.ToString());
                    if (redoCount == 0)
                        throw;
                    redoCount--;
                    Thread.Sleep(_serverFailureWaitTime);
                }
            } while (redoCount > 0);

            return bytesRead;
        }

        public override void WriteToStream(string key, string lockHandle, VirtualArray vBuffer, int srcOffset, int dstOffset, int length, OperationContext operationContext)
        {
            _statusLatch.WaitForAny(NodeStatus.Running);
            int redoCount = 3;
            bool suspectedErrorOccured = false;
            do
            {
                try
                {
                    Address targetNode = GetNextNode(key, null);

                    if (targetNode != null)
                    {
                        if (targetNode.Equals(Cluster.LocalAddress))
                        {
                            InternalCache.WriteToStream(key, lockHandle, vBuffer, srcOffset, dstOffset, length, operationContext);
                        }
                        else
                        {
                            WriteToStreamOperation streamOperation = new WriteToStreamOperation(key, lockHandle, vBuffer, srcOffset, dstOffset, length, operationContext);
                            Function func = new Function((int)OpCodes.WriteToStream, streamOperation);

                            Cluster.SendMessage(targetNode, func, GroupRequest.GET_ALL);
                        }
                    }
                    return;
                }
                catch (StreamException streamExc)
                {
                    throw;
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, null);
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.WriteToStream()", te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                        throw;
                }
                catch (Runtime.Exceptions.SuspectedException e)
                {
                    suspectedErrorOccured = true;
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.WriteToStream()", e.ToString());
                    if (redoCount == 0)
                        throw;
                    redoCount--;
                    Thread.Sleep(_serverFailureWaitTime);
                }
            } while (redoCount > 0);
        }

        public override long GetStreamLength(string key, string lockHandle, OperationContext operationContext)
        {
            long streamLength = 0;
            _statusLatch.WaitForAny(NodeStatus.Running);

            int redoCount = 3;
            bool suspectedErrorOccured = false;
            do
            {
                try
                {
                    Address targetNode = GetNextNode(key, null);

                    if (targetNode != null)
                    {
                        if (targetNode.Equals(Cluster.LocalAddress))
                        {
                            streamLength = InternalCache.GetStreamLength(key, lockHandle, operationContext);
                        }
                        else
                        {
                            GetStreamLengthOperation streamOperation = new GetStreamLengthOperation(key, lockHandle, operationContext);

                            Function func = new Function((int)OpCodes.GetStreamLength, streamOperation);

                            GetStreamLengthResult result = Cluster.SendMessage(targetNode, func, GroupRequest.GET_ALL) as GetStreamLengthResult;
                            if (result != null)
                                streamLength = result.Length;
                        }
                    }
                    return streamLength;
                }
                catch (StreamException streamExc)
                {
                    throw;
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key, null);
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.GetStreamLength()", te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;
                        continue;
                    }
                    else
                        throw;
                }
                catch (Runtime.Exceptions.SuspectedException e)
                {
                    suspectedErrorOccured = true;
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.GetStreamLength()", e.ToString());
                    if (redoCount == 0)
                        throw;
                    redoCount--;
                    Thread.Sleep(_serverFailureWaitTime);
                }
            } while (redoCount > 0);
            return streamLength;
        }

        #endregion

        public override void UnRegisterCQ(string serverUniqueId, string clientUniqueId, string clientId)
        {
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null)
                throw new InvalidOperationException();

            if (Cluster.Servers.Count > 1)
            {
                Clustered_UnRegisterCQ(serverUniqueId, clientUniqueId, clientId);
            }
            else
            {
                Local_UnRegisterCQ(serverUniqueId, clientUniqueId, clientId);
            }
        }

        private void Clustered_UnRegisterCQ(string serverUniqueId, string clientUniqueId, string clientId)
        {
            Clustered_UnRegisterCQ(Cluster.Servers, serverUniqueId, clientUniqueId, clientId, false);
        }

        public void Local_UnRegisterCQ(string serverUniqueId, string clientUniqueId, string clientId)
        {
            if (CQManager.UnRegister(serverUniqueId, clientUniqueId, clientId))
            {
                _internalCache.UnRegisterCQ(serverUniqueId);
            }
        }

        private EnumerationDataChunk Clustered_GetNextChunk(Address address, EnumerationPointer pointer, OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.GetNextChunk, new object[] { pointer, operationContext });
                object result = Cluster.SendMessage(address,
                    func,
                    GroupRequest.GET_FIRST,
                    Cluster.Timeout);

                EnumerationDataChunk nextChunk = result as EnumerationDataChunk;

                return nextChunk;
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

        private EnumerationDataChunk handleGetNextChunk(Address src, object info)
        {
            object[] package = info as object[];
            EnumerationPointer pointer = package[0] as EnumerationPointer;
            OperationContext operationContext = package[1] as OperationContext;

            EnumerationDataChunk nextChunk = InternalCache.GetNextChunk(pointer, operationContext);
            return nextChunk;
        }

    

        public override string RegisterCQ(string query, IDictionary values, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters)
        {
            string queryId = null;
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null)
                throw new InvalidOperationException();

            if (Cluster.Servers.Count > 1)
            {
                queryId = Clustered_RegisterCQ(query, values, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters);
            }
            else
            {
                queryId = Local_RegisterCQ(query, values, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters);
            }

            return queryId;
        }

        public string Clustered_RegisterCQ(string query, IDictionary values, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters)
        {
            ContinuousQuery cQuery = CQManager.GetCQ(query, values);
            Clustered_RegisterCQ(Cluster.Servers, cQuery, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, false, operationContext, datafilters);
            return cQuery.UniqueId;
        }

        public string Local_RegisterCQ(string query, IDictionary values, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters)
        {
            string queryId = null;
            ContinuousQuery cQuery = CQManager.GetCQ(query, values);
            if (CQManager.Exists(cQuery))
            {
                CQManager.Update(cQuery, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, datafilters);
            }
            else
            {
                _internalCache.RegisterCQ(cQuery, operationContext);
                CQManager.Register(cQuery, clientId, clientUniqueId, notifyAdd, notifyUpdate, notifyRemove, datafilters);          
            }
            queryId = cQuery.UniqueId;
            return queryId;
        }


        private CacheNodeStatus GetClusterStatus()
        {
            if (this._stats != null && this._stats.Nodes != null)
            {
                var nodes = this._stats.Nodes.Clone() as ArrayList;
                foreach (NodeInfo node in nodes)
                {
                    switch (node.CacheNodeStatus)
                    {
                        case CacheNodeStatus.InStateTransfer: return node.CacheNodeStatus;
                    }
                }
            }

            return CacheNodeStatus.Running;
        }

        public override bool IsInStateTransfer()
        {
            bool inTransfer = false;
            Cluster.ViewInstallationLatch.WaitForAny(ViewStatus.COMPLETE);
            if (GetNodeStatus() == CacheNodeStatus.InStateTransfer || GetClusterStatus() == CacheNodeStatus.InStateTransfer)
            {
                inTransfer = true;
            }
            else
            {
                inTransfer = _distributionMgr.InStateTransfer();
            }
            return inTransfer;
        }

        protected override bool VerifyClientViewId(long clientLastViewId)
        {
            return clientLastViewId == Cluster.LastViewID;
        }

        protected override ArrayList GetDestInStateTransfer()
        {
            ArrayList list = _distributionMgr.GetPermanentAddress(this.Servers);
            return list;
        }

        public override bool IsOperationAllowed(object key, AllowedOperationType opType)
        {
            if (base._shutdownServers != null && base._shutdownServers.Count > 0)
            {
                Address targetNode = GetNextNode(key as string, "");


                if (opType == AllowedOperationType.AtomicRead || opType == AllowedOperationType.AtomicWrite)
                {
                    if (base.IsShutdownServer(targetNode))
                        return false;
                    else if (base.IsShutdownServer(LocalAddress))
                        return false;
                }
            }

            return true;
        }

        public override bool IsOperationAllowed(IList keys, AllowedOperationType opType, OperationContext operationContext)
        {
            if (base._shutdownServers != null && base._shutdownServers.Count > 0)
            {
                long clientLastViewId = GetClientLastViewId(operationContext);

                if (clientLastViewId == Cluster.LastViewID && !IsInStateTransfer())
                {
                    if (base.IsShutdownServer(LocalAddress))
                    {
                        return false;
                    }
                }
                else
                {
                    ClusteredArrayList totalKeys = new ClusteredArrayList(keys);
                    Hashtable targetNodes = (Hashtable)GetTargetNodes(totalKeys, "");
                    if (targetNodes != null)
                    {
                        foreach (Address targetNode in targetNodes.Keys)
                        {
                            if (base.IsShutdownServer(targetNode))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public override bool IsOperationAllowed(AllowedOperationType opType, OperationContext operationContext)
        {
            if (base._shutdownServers != null && base._shutdownServers.Count > 0)
            {
                if (operationContext == null)
                {
                    if (opType == AllowedOperationType.ClusterRead)
                    {
                        return false;
                    }
                }

                if (opType == AllowedOperationType.BulkWrite) return false;

                if (opType == AllowedOperationType.BulkRead)
                {
                    long clientLastViewId = GetClientLastViewId(operationContext);
                    if (clientLastViewId == forcedViewId) //Client wants only me to collect data from cluster and return
                    {
                            return false;
                    }
                    else if (clientLastViewId == Cluster.LastViewID)
                    {
                        if (base.IsShutdownServer(LocalAddress))
                            return false;
                    }
                }
            }
            return true;
        }

        public void OnTaskCallback(string taskId, object value, OperationContext operationContext, EventContext eventContext)
        {
            if (value != null)
                RaiseTaskCalbackNotifier(taskId, (IList)value, eventContext);
        }


        internal override void Touch(List<string> keys, OperationContext operationContext)
        {
            List<string> totalRremainingKeys = new List<string>();
            ClusteredArrayList totalKeys = new ClusteredArrayList(keys);
            do
            {
                Hashtable targetNodes = (Hashtable)GetTargetNodes(totalKeys, "");

                IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                Hashtable keyList = null;
                //We select one node at a time for contain operation.
                while (ide.MoveNext())
                {
                    Address targetNode = ide.Key as Address;
                    keyList = (Hashtable)ide.Value;

                    if (targetNode != null)
                    {
                        List<string> list = MiscUtil.GetListFromCollection(keyList.Keys);
                        try
                        {
                            if (targetNode != null)
                            {
                                if (targetNode.Equals(Cluster.LocalAddress))
                                    Local_Touch(list, operationContext);
                                else
                                    Clustered_Touch(targetNode, list, operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            totalRremainingKeys.AddRange(list);
                            Thread.Sleep(_serverFailureWaitTime);
                            //we redo the operation
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PoRServerCache.Contains", targetNode + " left while addition");
                            continue;
                        }
                        catch (Alachisoft.NGroups.SuspectedException se)
                        {
                            totalRremainingKeys.AddRange(list);
                            Thread.Sleep(_serverFailureWaitTime);
                            //we redo the operation
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PoRServerCache.Contains", targetNode + " left while addition");
                            continue;
                        }
                        catch (Alachisoft.NCache.Common.Exceptions.TimeoutException te)
                        {
                            totalRremainingKeys.AddRange(list);

                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PoRServerCache.Contains", targetNode + " operation timed out");
                            continue;
                        }
                    }
                }

                totalKeys = new ClusteredArrayList(totalRremainingKeys);
                totalRremainingKeys.Clear();
            }
            while (totalKeys.Count > 0);
        }

        #region                                ---------------- Messaging -----------------------------

        #region ---------------------- IMessageStore Implemntation ---------------------------------

        public override bool AssignmentOperation(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo, TopicOperationType type, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity)
                ServerMonitor.LogClientActivity("Partitioned.AssignmentOperation", "Begin");

            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null)
                throw new InvalidOperationException();

            bool result = false;

            try
            {
                Address targetNode = GetNextNode(messageInfo.MessageId, "");

                if (targetNode == null)
                    throw new Exception("No target node available to accommodate the data.");

                if (targetNode.Equals(Cluster.LocalAddress))
                {
                    result = _internalCache.AssignmentOperation(messageInfo, subscriptionInfo, type,context);
                }
            }
            catch (StateTransferException x)
            {
                _context.NCacheLog.Error("Partitioned.AssignmentOperation", x + " " + messageInfo.MessageId + " " + _distributionMgr.GetBucketId(messageInfo.MessageId));
                DistributionMgr.Wait(messageInfo.MessageId, "");
            }
            catch (Exception e)
            {
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("Partitioned.AssignmentOperation", e.ToString());
                throw e;
            }
            finally
            {
                if (ServerMonitor.MonitorActivity)
                    ServerMonitor.LogClientActivity("Partitioned.AssignmentOperation", "End");
            }

            return result;
        }

        public override void AcknowledgeMessageReceipt(string clientID, IDictionary<string, IList<string>> topicWiseMessageIds, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PORcache.AcknowledgeMessageReceipt", "Begin");

            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            HashVector<string, IList<string>> messagesToAcknowledge = new HashVector<string, IList<string>>(topicWiseMessageIds);
            HashVector<string, IList<string>> totalRemainingMessages = new HashVector<string, IList<string>>();

            GeneralFailureException exception = null;
            bool suspectedErrorOccured = false;

            do
            {
                HashVector<Address, HashVector<string, IList<string>>> targetNodes = GetTargetNodes(messagesToAcknowledge);

                if (targetNodes == null) return;

                if (targetNodes != null && targetNodes.Count == 0) return;

                foreach (KeyValuePair<Address, HashVector<string, IList<string>>> messageDistribution in targetNodes)
                {
                    Address targetNode = messageDistribution.Key;
                    if (targetNode != null)
                    {
                        try
                        {
                            if (targetNode.Equals(LocalAddress))
                            {
                                _internalCache.AcknowledgeMessageReceipt(clientID, messageDistribution.Value, operationContext);
                            }
                            else
                            {
                                AcknowledgeMessageOperation acknowledgeMessageOperation = new AcknowledgeMessageOperation(clientID, messageDistribution.Value, operationContext);
                                Function func = new Function((int)OpCodes.Message_Acknowldegment, acknowledgeMessageOperation);
                                object rsp = Cluster.SendMessage(targetNode, func, GroupRequest.GET_ALL);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            suspectedErrorOccured = true;
                            Thread.Sleep(_serverFailureWaitTime);
                            //we redo the operation
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedServerCache.SafeAdd", targetNode + " left while addition");

                            PopulateRemainingValue(messageDistribution.Value, ref totalRemainingMessages);
                            continue;
                        }
                        catch (Runtime.Exceptions.TimeoutException te)
                        {
                            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("PartitionedServerCache.SafeAdd", targetNode + " operation timed out");

                            if (suspectedErrorOccured)
                            {
                                suspectedErrorOccured = false;

                                PopulateRemainingValue(messageDistribution.Value, ref totalRemainingMessages);
                                continue;
                            }
                            else
                            {
                                throw new GeneralFailureException(te.Message, te);
                            }
                        }
                        catch (BucketTransferredException ex)
                        {
                            PopulateRemainingValue(messageDistribution.Value, ref totalRemainingMessages);
                            continue;
                        }
                    }
                }

                messagesToAcknowledge = new HashVector<string, IList<string>>(totalRemainingMessages);
                totalRemainingMessages.Clear();

            } while (messagesToAcknowledge.Count > 0);

            if (exception != null)
            {
                throw exception;
            }
        }

        private void PopulateRemainingValue(HashVector<string, IList<string>> topicMessagesIds, ref HashVector<string, IList<string>> totalRemainingMessages)
        {
            foreach (var item in topicMessagesIds)
            {
                if (totalRemainingMessages.ContainsKey(item.Key))
                {
                    IList<string> messageList = totalRemainingMessages[item.Key];
                    foreach (var messages in item.Value)
                    {
                        messageList.Add(messages);
                    }
                }
                else
                {
                    totalRemainingMessages.Add(item.Key, new List<string>(item.Value));
                }
            }
        }

        private HashVector<Address, HashVector<string, IList<string>>> GetTargetNodes(IDictionary<string, IList<string>> topicWiseMessagesIds)
        {
            var targetNodes = new HashVector<Address, HashVector<string, IList<string>>>();

            if (topicWiseMessagesIds != null)
            {
                foreach (var pair in topicWiseMessagesIds)
                {
                    foreach (var message in pair.Value)
                    {
                        Address targetNode = GetNextNode(message, "");
                        if (targetNode != null)
                        {
                            HashVector<string, IList<string>> topicWiseMessageDic;
                            if (targetNodes.ContainsKey(targetNode))
                            {
                                topicWiseMessageDic = targetNodes[targetNode];
                                Populate(topicWiseMessageDic, pair.Key, message);
                            }
                            else
                            {
                                topicWiseMessageDic = new HashVector<string, IList<string>>();
                                Populate(topicWiseMessageDic, pair.Key, message);
                                targetNodes[targetNode] = topicWiseMessageDic;
                            }
                        }
                    }
                }
            }
            return targetNodes;
        }

        private HashVector GetTargetNodes(ClusteredArrayList keys)
        {
            var targetNodes = new HashVector();
            if (keys != null)
            {
                foreach (object key in keys)
                {
                    Address targetNode = GetNextNode(key as string, null);
                    if (targetNode != null)
                    {
                        if (targetNodes.Contains(targetNode))
                        {
                            var keyList = (ClusteredArrayList)targetNodes[targetNode];
                            keyList.Add(key);
                        }
                        else
                        {
                            var keyList = new ClusteredArrayList { key };
                            targetNodes[targetNode] = keyList;
                        }
                    }
                }
            }
            return targetNodes;
        }

        private void Populate(HashVector<string, IList<string>> topicWiseMessageDic, string topicName, string messageId)
        {
            IList<string> messagesList;
            if (topicWiseMessageDic.ContainsKey(topicName))
            {
                messagesList = topicWiseMessageDic[topicName];
                messagesList.Add(messageId);
            }
            else
            {
                messagesList = new List<string>();
                messagesList.Add(messageId);
                topicWiseMessageDic.Add(topicName, messagesList);
            }
        } 

        public override void RemoveMessages(IList<MessageInfo> messagesTobeRemoved, MessageRemovedReason reason, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity)
                ServerMonitor.LogClientActivity("Partitioned.RemoveMessages", "Begin");

            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null)
                throw new InvalidOperationException();

            try
            {
                IList<MessageInfo> localMessages = new List<MessageInfo>();

                foreach (var messageId in messagesTobeRemoved)
                {
                    Address targetNode = GetNextNode(messageId.MessageId, "");

                    if (targetNode != null && targetNode.Equals(Cluster.LocalAddress))
                        localMessages.Add(messageId);
                }

                if (localMessages.Count > 0)
                {
                    _internalCache.RemoveMessages(localMessages,reason,context);
                }
            }
            catch (Exception e)
            {
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("Partitioned.RemoveMessages", e.ToString());
                throw e;
            }
            finally
            {
                if (ServerMonitor.MonitorActivity)
                    ServerMonitor.LogClientActivity("Partitioned.RemoveMessages", "End");
            }
        }

        public override bool StoreMessage(string topic, Messaging.Message message, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.StoreMessage", "begins");

            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            bool suspectedErrorOccured = false;
            int maxTries = _stats.Nodes.Count > 3 ? 3 : _stats.Nodes.Count - 1;

            do
            {
                try
                {
                    Address targetNode = GetNextNode(message.MessageId, "");

                    if (targetNode == null)
                    {
                        throw new Exception("No target node available to accommodate the data.");
                    }

                    if (targetNode.CompareTo(LocalAddress) == 0)
                    {
                        _internalCache.StoreMessage(topic, message, context);
                    }
                    else
                    {
                        StoreMessageOperation messageOperation = new StoreMessageOperation(topic, message, context);
                        Function func = new Function((int)OpCodes.StoreMessage, messageOperation);

                        Cluster.SendMessage(targetNode, func, GroupRequest.GET_ALL);
                    }

                    break;
                }
                catch (StateTransferException)
                {
                    _distributionMgr.Wait(message.MessageId, "");
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.StoreMessage()", te.ToString());
                    if (suspectedErrorOccured)
                    {
                        suspectedErrorOccured = false;

                    }
                    else
                        throw;
                }
                catch (Runtime.Exceptions.SuspectedException e)
                {
                    suspectedErrorOccured = true;
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.StoreMessage()", e.ToString());
                    if (maxTries == 0)
                        throw;
                    maxTries--;
                }
            } while (maxTries > 0);

            return true;
        }
        
        #endregion
        
        #endregion
    }
}



