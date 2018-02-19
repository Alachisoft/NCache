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

using Alachisoft;
using Alachisoft.NGroups;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Stack;
using Alachisoft.NGroups.Util;

using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Util;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Caching.EvictionPolicies;

using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.Queries;
using System.Net;
using Alachisoft.NCache.Common.Enum;
using System.Collections.Generic;
using Runtime = Alachisoft.NCache.Runtime;

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

        private AutomaticDataLoadBalancer _autoBalancingTask;

        private StateTransferTask _stateTransferTask;

        private object _txfrTaskMutex = new object();

        private Hashtable _corresponders;

        private long _clusterItemCount;

        private bool threadRunning = true;
        private int confirmClusterStartUP = 3;

        protected IPAddress _srvrJustLeft = null;

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
       
        #region /                 --- Constructor ---           /

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        /// <param name="listener">cache events listener</param>
        public PartitionedServerCache(IDictionary cacheClasses, IDictionary properties, ICacheEventsListener listener,
            CacheRuntimeContext context)
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
        public PartitionedServerCache(IDictionary cacheClasses, IDictionary properties, ICacheEventsListener listener,
            CacheRuntimeContext context, IClusterEventsListener clusterListener)
            : base(properties, listener, context, clusterListener)
        {
            _stats.ClassName = "partitioned-server";
        }
        
        #endregion
        
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
            if (_autoBalancingTask != null)
            {
                _autoBalancingTask.Cancel();
                _autoBalancingTask = null;
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

            threadRunning = false;
            base.Dispose();
        }

        #endregion

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
                if (cacheType.CompareTo("local-cache") == 0)
                {
                    _internalCache =
                        CacheBase.Synchronized(new HashedLocalCache(cacheClasses, this, frontCacheProps, this, _context,
                            false));
                }
                else
                {
                    throw new ConfigurationException("invalid or non-local class specified in partitioned cache");
                }

                if (properties.Contains("data-affinity"))
                {
                    _dataAffinity = (IDictionary) properties["data-affinity"];
                }

                _distributionMgr = new DistributionManager(_autoBalancingThreshold, _internalCache.MaxSize);
                _distributionMgr.NCacheLog = Context.NCacheLog;

                InternalCache.BucketSize = _distributionMgr.BucketSize;

                _stats.Nodes = ArrayList.Synchronized(new ArrayList());

                _initialJoiningStatusLatch = new Latch();

                InitializeCluster(properties, Name, MCAST_DOMAIN,
                    new Identity(true, (_context.Render != null ? _context.Render.Port : 0),
                        (_context.Render != null ? _context.Render.IPAddress : null)), twoPhaseInitialization, false);
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
            if (NCacheLog.IsInfoEnabled)
                NCacheLog.Info("PartitionedCache.StartStateTransfer()", "Requesting state transfer " + LocalAddress);
            if (_stateTransferTask == null)
                _stateTransferTask = new StateTransferTask(this, Cluster.LocalAddress);
            _stateTransferTask.IsBalanceDataLoad = isBalanceDataLoad;
            _stateTransferTask.DoStateTransfer(_distributionMgr.GetBucketsList(Cluster.LocalAddress), false);
        }

        protected override bool VerifyClientViewId(long clientLastViewId)
        {
            return clientLastViewId == Cluster.LastViewID;
        }

        private int GetBucketId(string key)
        {
            int hashCode = AppUtil.GetHashCode(key);
            int bucketId = hashCode/_distributionMgr.BucketSize;

            if (bucketId < 0)
                bucketId *= -1;
            return bucketId;
        }

        public override bool IsRetryOnSuspected
        {
            get { return true; }
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
                NotifyHashmapChanged(Cluster.LastViewID, _distributionMgr.GetOwnerHashMapTable(Cluster.Renderers), GetClientMappedServers(Cluster.Servers.Clone() as ArrayList),true, true);
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

           StartStateTransfer(false);

            UpdateCacheStatistics();
            //We announces our status aa uninitalized
        }

        /// <summary>
        /// Called when a new member joins the group.
        /// </summary>
        /// <param name="address">address of the joining member</param>
        /// <param name="identity">additional identity information</param>
        /// <returns>true if the node joined successfuly</returns>
        public override bool OnMemberJoined(Address address, NodeIdentity identity)
        {
            if (!base.OnMemberJoined(address, identity) || !((Identity) identity).HasStorage)
                return false;

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
                                ArrayList nodeList = (ArrayList) _stats.PartitionsHavingDatagroup[ie.Current];
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

            if (NCacheLog.IsInfoEnabled)
                NCacheLog.Info("PartitionedCache.OnMemberJoined()", "Partition extended: " + address);
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

            NodeInfo info = _stats.GetNode(address as Address);
            _stats.Nodes.Remove(info);
            _distributionMgr.OnMemberLeft(address, identity);

            if (_stats.DatagroupsAtPartition.Contains(address))
            {
                ArrayList datagroups = (ArrayList) _stats.DatagroupsAtPartition[address];
                if (datagroups != null && datagroups.Count > 0)
                {
                    IEnumerator ie = datagroups.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        if (_stats.PartitionsHavingDatagroup.Contains(ie.Current))
                        {
                            ArrayList nodeList = (ArrayList) _stats.PartitionsHavingDatagroup[ie.Current];
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

            if (NCacheLog.IsInfoEnabled)
                NCacheLog.Info("PartitionedCache.OnMemberLeft()", "Partition shrunk: " + address);
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
                case (int) OpCodes.PeriodicUpdate:
                    return handlePresenceAnnouncement(src, func.Operand);

                case (int) OpCodes.ReqStatus:
                    return this.handleReqStatus();

                case (int) OpCodes.GetCount:
                    return handleCount();

                case (int) OpCodes.Get:
                    return handleGet(func.Operand);

                case (int) OpCodes.Insert:
                    return handleInsert(src, func.Operand, func.UserPayload);

                case (int) OpCodes.Contains:
                    return handleContains(func.Operand);

                case (int) OpCodes.Add:
                    return handleAdd(src, func.Operand, func.UserPayload);

                case (int)OpCodes.AddHint:
                    return handleAddHint(func.Operand);

                case (int) OpCodes.Remove:
                    return handleRemove(src, func.Operand);

                case (int) OpCodes.Clear:
                    return handleClear(src, func.Operand);

                case (int) OpCodes.KeyList:
                    return handleKeyList();

               case (int)OpCodes.Search:
                    return handleSearch(func.Operand);

                case (int) OpCodes.SearchEntries:
                    return handleSearchEntries(func.Operand);

               case (int) OpCodes.LockBuckets:
                    return handleLockBuckets(func.Operand);

                case (int) OpCodes.ReleaseBuckets:
                    handleReleaseBuckets(func.Operand, src);
                    break;

                case (int) OpCodes.TransferBucket:
                    return handleTransferBucket(src, func.Operand);

                case (int) OpCodes.AckStateTxfr:
                    handleAckStateTxfr(func.Operand, src);
                    break;

                case (int) OpCodes.AnnounceStateTransfer:
                    handleAnnounceStateTransfer(func.Operand, src);
                    break;

                case (int) OpCodes.SignalEndOfStateTxfr:
                    handleSignalEndOfStateTxfr(src);
                    break;

                case (int) OpCodes.LockKey:
                    return handleLock(func.Operand);

                case (int) OpCodes.UnLockKey:
                    handleUnLock(func.Operand);
                    break;

                case (int) OpCodes.IsLocked:
                    return handleIsLocked(func.Operand);

                case (int) OpCodes.GetNextChunk:
                    return handleGetNextChunk(src, func.Operand);
            }
            return base.HandleClusterMessage(src, func);
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
                Function func = new Function((int) OpCodes.ReqStatus, null);
                RspList results = Cluster.BroadcastToServers(func, GroupRequest.GET_ALL, false);

                for (int i = 0; i < results.size(); i++)
                {
                    Rsp rsp = (Rsp) results.elementAt(i);
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
            catch (NullReferenceException ne)
            {
                Context.NCacheLog.Error("PoRServer.DetermineClusterStatus()", ne.ToString());
            }
            catch (Exception e)
            {
                NCacheLog.Error("ParitionedCache.DetermineClusterStatus()", e.ToString());
            }
            return false;
        }

        /// <summary>
        /// Handler for Periodic update (PULL model), i.e. on demand fetch of information 
        /// from every node.
        /// </summary>
        private object handleReqStatus()
        {
            return _stats.LocalNode != null ? _stats.LocalNode.Clone() : null;
        }

        /// <summary>
        /// Handler for Periodic update (PUSH model).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        private object handlePresenceAnnouncement(Address sender, object obj)
        {
            if (NCacheLog.IsInfoEnabled)
                NCacheLog.Info("PartitionedServer.handlePresenceAnnouncement", "sender :" + sender);
            NodeInfo other = null;
            NodeInfo info = null;
            lock (Servers.SyncRoot)
            {
                other = obj as NodeInfo;
                info = _stats.GetNode(sender as Address);
                if (other != null && info != null)
                {
                    info.Statistics = other.Statistics;
                    info.Status = other.Status;
                    info.ConnectedClients = other.ConnectedClients;
                    info.DataAffinity = other.DataAffinity;

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
                                ArrayList nodeList = (ArrayList) _stats.PartitionsHavingDatagroup[ie.Current];
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
        public override void ClientConnected(string client, bool isInproc)
        {
            base.ClientConnected(client, isInproc);
            PublishStats(false);
            if (_context.ClientDeathDetection != null) UpdateClientStatus(client, true);
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
            PublishStats(false);
            if (_context.ClientDeathDetection != null) UpdateClientStatus(client, true);
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

        #region IPresenceAnnouncement Members

        public bool PublishStats(bool urgent)
        {
            try
            {
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("ClusteredCacheBase.AnnouncePresence()",
                        " announcing presence ;urget " + urgent);
                if (this.ValidMembers.Count > 1)
                {
                    Function func = new Function((int) OpCodes.PeriodicUpdate, _stats.LocalNode.Clone());
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

        #region /                 --- data distribution + state transfer ---           /

        public override void BalanceDataLoad()
        {
            Clustered_BalanceDataLoad(Cluster.Coordinator, Cluster.LocalAddress);
        }

        /// <summary>
        /// Return the next node in load balancing order.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private Address GetNextNode(string key)
        {
            return _distributionMgr.SelectNode(key);
        }

        private Hashtable GetTargetNodes(ArrayList keys)
        {
            Hashtable targetNodes = new Hashtable();
            Address targetNode = null;

            if (keys != null)
            {
                foreach (object key in keys)
                {
                    targetNode = GetNextNode(key as string);
                    if (targetNode != null)
                    {
                        if (targetNodes.Contains(targetNode))
                        {
                            Hashtable keyList = (Hashtable) targetNodes[targetNode];
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
                    NCacheLog.Error("PartitionedServerCache.handleLockBuckets",
                        "i am not coordinator but i have received bucket lock request.");
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
            byte transferType = (byte) pack[1];
            bool sparsedBuckets = (bool) pack[2];
            int expectedTxfrId = (int) pack[3];
            bool isBalanceDataLoad = (bool) pack[4];

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

            Alachisoft.NCache.Caching.Topologies.Clustered.StateTxfrInfo transferInfo = corresponder.TransferBucket(bucketIds, sparsedBuckets, expectedTxfrId);
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
            try
            {
                ArrayList bucketIds = (ArrayList) info;
                IEnumerator ie = bucketIds.GetEnumerator();
                while (ie.MoveNext())
                {
                    //remove this bucket from the local buckets.
                    //this bucket has been transfered to some other node.
                    InternalCache.RemoveBucket((int) ie.Current);
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
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.handleSignalEndOfTxfr",
                            requestingNode.ToString() + " Corresponder removed.");
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


        #region	/                 --- ICache ---           /


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

  
        public override int ServersCount
        {
            get { return Cluster.ValidMembers.Count; }
        }

        public override bool IsServerNodeIp(Address clientAddress)
        {
            foreach (Address addr in Cluster.Servers)
            {
                if (addr.IpAddress.Equals(clientAddress.IpAddress))
                    return true;
            }
            return false;
        }

        #endregion

        #region	/                 --- Partitioned ICache.Contains ---           /

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        public override bool Contains(object key, OperationContext operationContext)
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

                    node = GetNextNode(key as string);
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
                catch (Alachisoft.NGroups.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.Contains",
                            node + " left while Contains. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.Contains",
                            node + " operation timed out. Error: " + te.ToString());
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
                    _distributionMgr.Wait(key);
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
        public override Hashtable Contains(object[] keys, OperationContext operationContext)
        {
            Hashtable targetNodes = null;
            Hashtable result = new Hashtable();
            Hashtable tmpKeyTbl = null;
            Address targetNode = null;

            ArrayList totalFoundKeys = new ArrayList();
            ArrayList totalRremainingKeys = new ArrayList();
            ArrayList totalKeys = new ArrayList(keys);

            do
            {
                targetNodes = GetTargetNodes(totalKeys);

                IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                Hashtable keyList = null;
                //We select one node at a time for contain operation.
                while (ide.MoveNext())
                {
                    targetNode = ide.Key as Address;
                    keyList = (Hashtable) ide.Value;

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
                        catch (Alachisoft.NGroups.SuspectedException se)
                        {
                            totalRremainingKeys.AddRange(currentKeys);

                            //we redo the operation
                            if (Context.NCacheLog.IsInfoEnabled)
                                Context.NCacheLog.Info("PoRServerCache.Contains", targetNode + " left while addition");
                            continue;
                        }
                        catch (Alachisoft.NCache.Common.Exceptions.TimeoutException te)
                        {
                            totalRremainingKeys.AddRange(currentKeys);

                            if (Context.NCacheLog.IsInfoEnabled)
                                Context.NCacheLog.Info("PoRServerCache.Contains", targetNode + " operation timed out");
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

                totalKeys = new ArrayList(totalRremainingKeys);
                totalRremainingKeys.Clear();

            } while (totalKeys.Count > 0);

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
                object[] args = (object[]) info;
                if (args.Length > 1)
                    operationContext = args[1] as OperationContext;
                if (args[0] is object[])
                {
                    return Local_Contains((object[]) args[0], operationContext);
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
        public override void Clear(CallbackEntry cbEntry, OperationContext operationContext)
        {
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();
            
            if (Servers.Count > 1)
            {
                Clustered_Clear(cbEntry, false, operationContext);
            }
            else
            {
                handleClear(Cluster.LocalAddress, new object[] {cbEntry, operationContext});
            }

        }

        /// <summary>
        /// Removes all entries from the local cache only.
        /// </summary>
        private void Local_Clear(Address src, CallbackEntry cbEntry, OperationContext operationContext)
        {
            if (_internalCache != null)
            {
                _internalCache.Clear(null, operationContext);
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
                OperationContext operationContext = null;

                if (args.Length > 0)
                    cbEntry = args[0] as CallbackEntry;
                if (args.Length > 1)
                    operationContext = args[1] as OperationContext;

                Local_Clear(src, cbEntry, operationContext);
               
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        #endregion

        #region /                 --- Partitioned Search ---               /

        protected override QueryResultSet Local_Search(string query, IDictionary values,
            OperationContext operationContext)
        {
            return _internalCache.Search(query, values, operationContext);
        }

        protected override QueryResultSet Local_SearchEntries(string query, IDictionary values,
            OperationContext operationContext)
        {
            return _internalCache.SearchEntries(query, values, operationContext);
        }

        public QueryResultSet handleSearch(object info)
        {
            if (_internalCache != null)
            {
                ArrayList keyList = new ArrayList();
                object[] data = (object[]) info;
                return _internalCache.Search(data[0] as string, data[1] as IDictionary, data[2] as OperationContext);
            }

            return null;
        }

        public QueryResultSet handleSearchEntries(object info)
        {
            if (_internalCache != null)
            {
                Hashtable keyValues = new Hashtable();
                object[] data = (object[]) info;
                return _internalCache.SearchEntries(data[0] as string, data[1] as IDictionary,
                    data[2] as OperationContext);
            }

            return null;
        }

        #endregion

        #region	/                 --- Partitioned ICache.Get ---           /




        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="lockId"></param>
        /// <param name="lockDate"></param>
        /// <param name="lockExpiration"></param>
        /// <param name="access"></param>
        /// <param name="operationContext"></param>
        /// <returns>cache entry.</returns>
        public override CacheEntry Get(object key, ref object lockId, ref DateTime lockDate,
            LockExpiration lockExpiration, LockAccessType access, OperationContext operationContext)
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
                address = GetNextNode(key as string);

                if (address == null)
                {
                    NCacheLog.Error("PartitionedServerCache.Get()", "specified key does not map to any node. return.");
                    return null;
                }

                try
                {
                    if (address.CompareTo(Cluster.LocalAddress) == 0)
                    {
                        e = Local_Get(key, ref lockId, ref lockDate, lockExpiration, access, operationContext);
                    }
                    else
                    {
                        e = Clustered_Get(address, key, ref lockId, ref lockDate, lockExpiration, access,
                            operationContext);

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
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.Get",
                            address + " left while Get. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.Get",
                            address + " operation timed out. Error: " + te.ToString());
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
                    _distributionMgr.Wait(key);
                }
            }
            return e;

        }


        private Hashtable OptimizedGet(object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.GetBlk", "");

            Hashtable result = new Hashtable();

            ArrayList remainingKeys = new ArrayList();

            if (_internalCache == null)
                throw new InvalidOperationException();

            result = Local_Get(keys, operationContext);

            if (result != null && result.Count > 0)
            {
                IDictionaryEnumerator ie = result.GetEnumerator();
                while (ie.MoveNext())
                {
                    if (ie.Value is StateTransferException)
                    {
                        remainingKeys.Add(ie.Key);
                    }
                }
            }

            IDictionaryEnumerator ine = result.GetEnumerator();
            ArrayList updateIndiceKeyList = null;
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
                        updateIndiceKeyList = new ArrayList();

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
                Hashtable tmpResult = ClusteredGet(remainingKeys.ToArray(), operationContext);
                foreach (DictionaryEntry entry in tmpResult)
                {
                    result[entry.Key] = entry.Value;
                }
            }

            return result;
        }

        private Hashtable ClusteredGet(object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.GetBlk", "");

            Hashtable result = new Hashtable();
            Hashtable targetNodes = null;
            Hashtable tmpData = null;

            ArrayList totalKeys = new ArrayList(keys);
            ArrayList totalRemainingKeys = new ArrayList();

            Address targetNode = null;

            if (_internalCache == null)
                throw new InvalidOperationException();

            do
            {
                targetNodes = GetTargetNodes(totalKeys);

                IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                Hashtable keyList = null;
                //We select one node at a time for operation.
                while (ide.MoveNext())
                {
                    targetNode = ide.Key as Address;
                    keyList = (Hashtable) ide.Value;

                    if (targetNode != null)
                    {
                        object[] currentKeys = MiscUtil.GetArrayFromCollection(keyList.Keys);
                        try
                        {
                            if (targetNode.Equals(Cluster.LocalAddress))
                                tmpData = Local_Get(currentKeys, operationContext);
                            else
                            {
                                tmpData = Clustered_Get(targetNode, currentKeys, operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            //we redo the operation
                            if (Context.NCacheLog.IsInfoEnabled)
                                Context.NCacheLog.Info("PoRServerCache.Contains", targetNode + " left while addition");
                            totalRemainingKeys.Add(currentKeys);
                            continue;
                        }
                        catch (Runtime.Exceptions.TimeoutException te)
                        {
                            if (Context.NCacheLog.IsInfoEnabled)
                                Context.NCacheLog.Info("PoRServerCache.Contains", targetNode + " operation timed out");
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

                totalKeys = new ArrayList(totalRemainingKeys);
                totalRemainingKeys.Clear();
            } while (totalKeys.Count > 0);

            IDictionaryEnumerator ine = result.GetEnumerator();
            ArrayList updateIndiceKeyList = null;
            while (ine.MoveNext())
            {
                CacheEntry e = (CacheEntry) ine.Value;
                if (e == null)
                {
                    _stats.BumpMissCount();
                }
                else
                {
                    if (updateIndiceKeyList == null) updateIndiceKeyList = new ArrayList();
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
        public override Hashtable Get(object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.GetBlk", "");

            Hashtable result = null;

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
        private CacheEntry Local_Get(object key, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration,
            LockAccessType access, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            if (_internalCache != null)
                retVal = _internalCache.Get(key, ref lockId, ref lockDate, lockExpiration, access, operationContext);
            return retVal;
        }

        /// <summary>
        /// Retrieve the objects from the local cache only. 
        /// </summary>
        /// <param name="keys">keys of the entry.</param>
        /// <returns>cache entries.</returns>
        private Hashtable Local_Get(object[] keys, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.Get(keys, operationContext);

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
                    Hashtable table = Local_Get((object[]) args[0], args[1] as OperationContext);
                    return table;
                }
                else
                {
                    object key = args[0];
                    object lockId = args[1];
                    DateTime lockDate = (DateTime) args[2];
                    LockAccessType accessType = (LockAccessType) args[3];
                    LockExpiration lockExpiration = (LockExpiration) args[4];
                    OperationContext operationContext = args[5] as OperationContext;

                    CacheEntry entry = Local_Get(key, ref lockId, ref lockDate, lockExpiration, accessType,
                        operationContext);
                    OperationResponse opRes = new OperationResponse();
                    object[] response = new object[4];
                    if (entry != null)
                    {
                        UserBinaryObject ubObject =
                            (UserBinaryObject)
                                (entry.Value is CallbackEntry ? ((CallbackEntry) entry.Value).Value : entry.Value);
                        opRes.UserPayload = ubObject.Data;
                        response[0] = entry.CloneWithoutValue();
                    }
                    response[1] = lockId;
                    response[2] = lockDate;
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
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify,
            OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Add_1", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            Address targetNode = null;
            CacheAddResult result = CacheAddResult.Failure;
            if (_internalCache == null) throw new InvalidOperationException();


            result = Safe_Clustered_Add(key, cacheEntry, out targetNode, operationContext);
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public override bool Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
            bool result = false;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Add_3", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();


            bool suspectedErrorOccured = false;
            Address targetNode = null;

            while (true)
            {
                try
                {
                    targetNode = GetNextNode(key as string);
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
                catch (Alachisoft.NGroups.SuspectedException se)
                {
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
                    _distributionMgr.Wait(key);
                }

            }

            return result;
        }


        /// <summary>
        /// Add the object to the local cache. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry"></param>
        /// <param name="src"></param>
        /// <param name="notify"></param>
        /// <param name="operationContext"></param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method can only be called on one node in the cluster. It triggers <see cref="OnItemAdded"/>,
        /// which initiates a cluster-wide item added notification.
        /// </remarks>
        private CacheAddResult Local_Add(object key, CacheEntry cacheEntry, Address src, bool notify, OperationContext operationContext)
        {
            CacheAddResult retVal = CacheAddResult.Failure;
            if (_internalCache != null)
            {
                retVal = _internalCache.Add(key, cacheEntry, notify, operationContext);
            }
            return retVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <param name="operationContext"></param>
        /// <returns></returns>
        private bool Local_Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
            bool retVal = false;
            if (_internalCache != null)
                retVal = _internalCache.Add(key, eh, operationContext);
            return retVal;
        }

        /// <summary>
        /// A wrapper method that reperform the operations that fail because
        /// of the members suspected during operations.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cacheEntry"></param>
        /// <param name="targetNode"></param>
        /// <param name="operationContext"></param>
        /// <returns></returns>
        private CacheAddResult Safe_Clustered_Add(object key, CacheEntry cacheEntry, out Address targetNode, OperationContext operationContext)
        {
            bool suspectedErrorOccured = false;
            int maxTries = _stats.Nodes.Count > 3 ? 3 : _stats.Nodes.Count - 1;
            CacheAddResult result = CacheAddResult.Failure;
            targetNode = null;
            do
            {

                try
                {
                    targetNode = GetNextNode(key as string);
                    //possible in case of strict affinity...
                    if (targetNode == null)
                    {
                        throw new Exception("No target node available to accommodate the data.");
                    }

                    if (targetNode.CompareTo(LocalAddress) == 0)
                    {
                        result = Local_Add(key, cacheEntry, Cluster.LocalAddress, true, operationContext);
                    }
                    else
                    {
                        result = Clustered_Add(targetNode, key, cacheEntry, operationContext);
                    }
                    return result;
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key);
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
                }
            } while (maxTries > 0);
            return result;
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
                object[] objs = (object[]) info;
                OperationContext operationContext = null;

                if (objs.Length == 3)
                    operationContext = objs[2] as OperationContext;

                if (objs[0] is object[])
                {
                    object[] keys = (object[]) objs[0];
                    CacheEntry[] entries = objs[1] as CacheEntry[];
                    return Local_Add(keys, entries, src, true, operationContext);
                }
                else
                {
                    object key = objs[0];
                    CacheEntry e = objs[1] as CacheEntry;
                    e.Value = userPayload;
                    CacheAddResult result = Local_Add(key, e, src, true, operationContext);
                    return result;
                }
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return CacheAddResult.Failure;
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

        private Hashtable OptimizedAdd(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.AddBlk", "");

            Hashtable result = new Hashtable();
            Hashtable tmpResult = new Hashtable();

            ArrayList totalKeys = new ArrayList(keys);
            ArrayList totalEntries = new ArrayList(cacheEntries);

            ArrayList successfulKeys = new ArrayList();
            ArrayList remainingKeys = new ArrayList();

            try
            {
                tmpResult = Local_Add(keys, cacheEntries, Cluster.LocalAddress, notify, operationContext);
            }
            catch (BucketTransferredException ex)
            {
                tmpResult = new Hashtable();
                for (int i = 0; i < keys.Length; i++)
                {
                    tmpResult[keys[i]] = new OperationFailedException(ex.Message, ex);
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
                            CacheAddResult res = (CacheAddResult) ie.Value;
                            switch (res)
                            {
                                case CacheAddResult.Failure:
                                    result[ie.Key] =
                                        new OperationFailedException(
                                            "Generic operation failure; not enough information is available.");
                                    break;
                                case CacheAddResult.NeedsEviction:
                                    result[ie.Key] =
                                        new OperationFailedException(
                                            "The cache is full and not enough items could be evicted.");
                                    break;
                                case CacheAddResult.KeyExists:
                                    result[ie.Key] = new OperationFailedException("The specified key already exists.");
                                    break;
                                case CacheAddResult.Success:
                                    successfulKeys.Add(ie.Key);
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

                tmpResult = ClusteredAdd(currentKeys, currentValues, notify, operationContext);

                foreach (DictionaryEntry entry in tmpResult)
                {
                    result[entry.Key] = entry.Value;
                }

            }

            return result;
        }

        private Hashtable ClusteredAdd(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            Hashtable targetNodes = null;
            Hashtable result = new Hashtable();
            Hashtable tmpResult = null;
            ArrayList totalKeys = new ArrayList(keys);
            ArrayList keysToAdd = new ArrayList(keys);


            ArrayList totalEntries = new ArrayList(cacheEntries);
            Address targetNode = null;
            object[] currentKeys = null;
            CacheEntry[] currentValues = null;

            Dictionary<object, CacheEntry> fullEntrySet = new Dictionary<object, CacheEntry>();

            Hashtable totalSuccessfullKeys = new Hashtable();
            Hashtable totalRemainingKeys = new Hashtable();


            if (_internalCache == null) throw new InvalidOperationException();

            do
            {
                targetNodes = GetTargetNodes(keysToAdd);

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
                    keyList = (Hashtable) ide.Value;

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
                                if (!fullEntrySet.ContainsKey((string) totalKeys[index]))
                                    fullEntrySet.Add((string) totalKeys[index], (CacheEntry) totalEntries[index]);
                                j++;
                            }
                        }

                        try
                        {
                            if (targetNode.Equals(Cluster.LocalAddress))
                            {
                                tmpResult = Local_Add(currentKeys, currentValues, Cluster.LocalAddress, notify, operationContext);
                            }
                            else
                            {
                                tmpResult = Clustered_Add(targetNode, currentKeys, currentValues,
                                    operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            //we redo the operation
                            if (Context.NCacheLog.IsInfoEnabled)
                                Context.NCacheLog.Info("PartitionedServerCache.SafeAdd",
                                    targetNode + " left while addition");

                            tmpResult = new Hashtable();
                            for (int i = 0; i < currentKeys.Length; i++)
                            {
                                tmpResult[currentKeys[i]] = new GeneralFailureException(se.Message, se);
                            }
                        }
                        catch (Runtime.Exceptions.TimeoutException te)
                        {
                            if (Context.NCacheLog.IsInfoEnabled)
                                Context.NCacheLog.Info("PartitionedServerCache.SafeAdd",
                                    targetNode + " operation timed out");

                            tmpResult = new Hashtable();
                            for (int i = 0; i < currentKeys.Length; i++)
                            {
                                tmpResult[currentKeys[i]] = new GeneralFailureException(te.Message, te);
                            }
                        }
                        catch (BucketTransferredException ex)
                        {
                            tmpResult = new Hashtable();
                            for (int i = 0; i < currentKeys.Length; i++)
                            {
                                tmpResult[currentKeys[i]] = new OperationFailedException(ex.Message, ex);
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
                                        CacheAddResult res = (CacheAddResult) ie.Value;
                                        switch (res)
                                        {
                                            case CacheAddResult.Failure:
                                                result[ie.Key] =
                                                    new OperationFailedException(
                                                        "Generic operation failure; not enough information is available.");
                                                break;
                                            case CacheAddResult.NeedsEviction:
                                                result[ie.Key] =
                                                    new OperationFailedException(
                                                        "The cache is full and not enough items could be evicted.");
                                                break;
                                            case CacheAddResult.KeyExists:
                                                result[ie.Key] =
                                                    new OperationFailedException("The specified key already exists.");
                                                break;
                                            case CacheAddResult.Success:
                                                totalSuccessfullKeys[ie.Key] = null;
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                keysToAdd = new ArrayList(totalRemainingKeys.Keys);
                totalRemainingKeys.Clear();
            } while (keysToAdd.Count > 0);

             return result;
        }

        public override Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify,
            OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.AddBlk", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            Hashtable result = null;

            if (_internalCache == null) throw new InvalidOperationException();
            
            long clientLastViewId = GetClientLastViewId(operationContext);

            if (clientLastViewId == Cluster.LastViewID && !IsInStateTransfer())
            {
                result = OptimizedAdd(keys, cacheEntries, notify, operationContext);
            }
            else
            {
                result = ClusteredAdd(keys, cacheEntries, notify, operationContext);
            }

            return result;
        }

        /// <summary>
        /// Add the objects to the local cache. 
        /// </summary>
        /// <param name="keys">key of the entry.</param>
        /// <param name="cacheEntries"></param>
        /// <param name="src"></param>
        /// <param name="notify"></param>
        /// <param name="operationContext"></param>
        /// <returns>list of added keys.</returns>
        /// <remarks>
        /// This method can only be called on one node in the cluster. It triggers <see cref="OnItemAdded"/>,
        /// which initiates a cluster-wide item added notification.
        /// </remarks>
        private Hashtable Local_Add(object[] keys, CacheEntry[] cacheEntries, Address src, bool notify, OperationContext operationContext)
        {
            Hashtable added = new Hashtable();

            if (_internalCache != null)
            {
                added = _internalCache.Add(keys, cacheEntries, notify, operationContext);
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
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId,
            LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Insert", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            Address targetNode = null;

            if (_internalCache == null) throw new InvalidOperationException();
            
            CacheInsResultWithEntry result = new CacheInsResultWithEntry();
            result = Safe_Clustered_Insert(key, cacheEntry, out targetNode, lockId, accessType, operationContext);
            return result;
        }

        private Hashtable OptimizedInsert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.InsertBlk", "");

            Hashtable result = new Hashtable();

            Hashtable addedKeys = new Hashtable();
            Hashtable insertedKeys = new Hashtable();
            ArrayList remainingKeys = new ArrayList();

            ArrayList totalKeys = new ArrayList(keys);
            ArrayList totalEntries = new ArrayList(cacheEntries);

            Hashtable tmpResult = new Hashtable();


            try
            {
                tmpResult = Local_Insert(keys, cacheEntries, Cluster.LocalAddress, notify, operationContext);
            }
            catch (BucketTransferredException ex)
            {
                tmpResult = new Hashtable();
                for (int i = 0; i < keys.Length; i++)
                {
                    tmpResult[keys[i]] = new OperationFailedException(ex.Message, ex);
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
                                    result[ie.Key] =
                                        new OperationFailedException(
                                            "Generic operation failure; not enough information is available.");
                                    break;
                                case CacheInsResult.NeedsEviction:
                                    result[ie.Key] =
                                        new OperationFailedException(
                                            "The cache is full and not enough items could be evicted.");
                                    break;
                                case CacheInsResult.Success:
                                    addedKeys[ie.Key] = null;
                                    break;
                                case CacheInsResult.SuccessOverwrite:
                                    insertedKeys[ie.Key] = ie.Value;
                                    result[ie.Key] = ie.Value;
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

                tmpResult = ClusteredInsert(currentKeys, currentValues, notify, operationContext);
                foreach (DictionaryEntry entry in tmpResult)
                {
                    result[entry.Key] = entry.Value;
                }
            }
            return result;
        }

        private Hashtable ClusteredInsert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            Hashtable targetNodes = null;
            Hashtable result = new Hashtable();
            Hashtable tmpResult = null;

            ArrayList totalKeys = new ArrayList(keys);
            ArrayList totalEntries = new ArrayList(cacheEntries);
            ArrayList keysToInsert = new ArrayList(keys);

            Address targetNode = null;
            object[] currentKeys = null;
            CacheEntry[] currentValues = null;

            Dictionary<object, CacheEntry> fullEntrySet = new Dictionary<object, CacheEntry>();

            Hashtable totalAddedKeys = new Hashtable();
            Hashtable totalInsertedKeys = new Hashtable();
            Hashtable totalRemainingKeys = new Hashtable();

            if (_internalCache == null) throw new InvalidOperationException();

            do
            {
                targetNodes = GetTargetNodes(keysToInsert);

                if (targetNodes != null && targetNodes.Count == 0)
                {
                    foreach (object key in keysToInsert)
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
                    keyList = (Hashtable) ide.Value;

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
                                if (!fullEntrySet.ContainsKey((string) totalKeys[index]))
                                    fullEntrySet.Add((string) totalKeys[index], (CacheEntry) totalEntries[index]);
                                j++;
                            }
                        }

                        try
                        {
                            if (targetNode.Equals(Cluster.LocalAddress))
                            {
                                tmpResult = Local_Insert(currentKeys, currentValues, Cluster.LocalAddress, notify, operationContext);
                            }
                            else
                            {
                                tmpResult = Clustered_Insert(targetNode, currentKeys, currentValues,
                                    operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
                            //we redo the operation
                            if (Context.NCacheLog.IsInfoEnabled)
                                Context.NCacheLog.Info("PartitionedServerCache.SafeAdd",
                                    targetNode + " left while addition");

                            tmpResult = new Hashtable();
                            for (int i = 0; i < currentKeys.Length; i++)
                            {
                                tmpResult[currentKeys[i]] = new GeneralFailureException(se.Message, se);
                            }
                        }
                        catch (Runtime.Exceptions.TimeoutException te)
                        {
                            if (Context.NCacheLog.IsInfoEnabled)
                                Context.NCacheLog.Info("PartitionedServerCache.SafeAdd",
                                    targetNode + " operation timed out");

                            tmpResult = new Hashtable();
                            for (int i = 0; i < currentKeys.Length; i++)
                            {
                                tmpResult[currentKeys[i]] = new GeneralFailureException(te.Message, te);
                            }
                        }
                        catch (BucketTransferredException ex)
                        {
                            tmpResult = new Hashtable();
                            for (int i = 0; i < currentKeys.Length; i++)
                            {
                                tmpResult[currentKeys[i]] = new OperationFailedException(ex.Message, ex);
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
                                                result[ie.Key] =
                                                    new OperationFailedException(
                                                        "Generic operation failure; not enough information is available.");
                                                break;
                                            case CacheInsResult.NeedsEviction:
                                                result[ie.Key] =
                                                    new OperationFailedException(
                                                        "The cache is full and not enough items could be evicted.");
                                                break;
                                            case CacheInsResult.Success:
                                                totalAddedKeys[ie.Key] = null;
                                                break;
                                            case CacheInsResult.SuccessOverwrite:
                                                totalInsertedKeys[ie.Key] = ie.Value;
                                                result[ie.Key] = ie.Value;
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                keysToInsert = new ArrayList(totalRemainingKeys.Keys);
                totalRemainingKeys.Clear();
            } while (keysToInsert.Count > 0);

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
                        CacheEntry currentEntry = fullEntrySet[(string) ide.Key];
                        object value = insResult.Entry.Value;
                        if (value is CallbackEntry)
                        {

                            RaiseCustomUpdateCalbackNotifier(ide.Key, currentEntry, insResult.Entry, operationContext);
                        }
                    }
                }

            }



            if (generateQueryInfo == null)
            {
                operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
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
        public override Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify,
            OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.InsertBlk", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            Hashtable result = null;

           
            long clientLastViewId = GetClientLastViewId(operationContext);

            if (clientLastViewId == Cluster.LastViewID && !IsInStateTransfer())
            {
                result = OptimizedInsert(keys, cacheEntries, notify, operationContext);
            }
            else
            {
                result = ClusteredInsert(keys, cacheEntries, notify, operationContext);
            }

            return result;
        }

        private CacheInsResultWithEntry Local_Insert(object key, CacheEntry cacheEntry, Address src, bool notify, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            CacheInsResultWithEntry retVal = new CacheInsResultWithEntry();
           
            if (_internalCache != null)

            {
                retVal = _internalCache.Insert(key, cacheEntry, notify, lockId, accessType, operationContext);

             }
            return retVal;
        }

        /// <summary>
        /// Insert the objects to the local cache. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries"></param>
        /// <param name="src"></param>
        /// <param name="notify"></param>
        /// <param name="operationContext"></param>
        /// <returns>cache entries.</returns>
        private Hashtable Local_Insert(object[] keys, CacheEntry[] cacheEntries, Address src, bool notify, OperationContext operationContext)
        {
            Hashtable retVal = new Hashtable();

            if (_internalCache != null)
            {
                retVal = _internalCache.Insert(keys, cacheEntries, notify, operationContext);

            }
            return retVal;
        }

        private CacheInsResultWithEntry Safe_Clustered_Insert(object key, CacheEntry cacheEntry, out Address targetNode, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            bool suspectedErrorOccured = false;
            int maxTries = _stats.Nodes.Count > 3 ? 3 : _stats.Nodes.Count - 1;
            CacheInsResultWithEntry retVal = new CacheInsResultWithEntry();
            targetNode = null;
            do
            {
                try
                {
                    targetNode = GetNextNode(key as string);

                    if (targetNode == null)
                    {
                        throw new Exception("No target node available to accommodate the data.");
                    }

                    if (targetNode.CompareTo(LocalAddress) == 0)
                    {
                        retVal = Local_Insert(key, cacheEntry, Cluster.LocalAddress, true, lockId, accessType,
                            operationContext);
                    }
                    else
                    {
                        retVal = Clustered_Insert(targetNode, key, cacheEntry, lockId, accessType,
                            operationContext);
                    }

                    break;
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key);
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedCache.Safe_Clustered_Insert()", te.ToString());
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
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedCache.Safe_Clustered_Insert()", e.ToString());
                    if (maxTries == 0)
                        throw;
                    maxTries--;
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
                object[] objs = (object[]) info;

                if (objs.Length == 3)
                {
                    operationContext = objs[2] as OperationContext;
                }
                else if (objs.Length == 5)
                {
                    operationContext = objs[4] as OperationContext;
                }

                if (objs[0] is object[])
                {
                    object[] keyArr = (object[])objs[0];
                    CacheEntry[] valArr = (CacheEntry[])objs[1];
                    return Local_Insert(keyArr, valArr, src, true, operationContext);
                }
                else
                {
                    object key = objs[0];
                    CacheEntry e = objs[1] as CacheEntry;
                    e.Value = userPayload;
                    object lockId = objs[2];
                    LockAccessType accessType = (LockAccessType) objs[3];
                    CacheInsResultWithEntry retVal = Local_Insert(key, e, src, true, lockId, accessType,
                        operationContext);
                    /* send value and entry seperaty*/
                    OperationResponse opRes = new OperationResponse();
                    if (retVal.Entry != null)
                    {
                        opRes.UserPayload = null; 
                        retVal.Entry = null;
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

        public override object RemoveSync(object[] keys, ItemRemoveReason reason, bool notify,
            OperationContext operationContext)
        {
            try
            {
                Hashtable totalRemovedItems = new Hashtable();
                CacheEntry entry = null;
                IDictionaryEnumerator ide = null;

                if (NCacheLog.IsInfoEnabled)
                    NCacheLog.Info("PartitionedCache.RemoveSync", "Keys = " + keys.Length.ToString());

                for (int i = 0; i < keys.Length; i++)
                {
                    try
                    {
                        if (keys[i] != null)
                            entry = Local_Remove(keys[i], reason, null, null, false, null, LockAccessType.IGNORE_LOCK, operationContext);

                        if (entry != null)
                        {
                            totalRemovedItems.Add(keys[i], entry);
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
                ide = totalRemovedItems.GetEnumerator();

                while (ide.MoveNext())
                {
                    try
                    {
                        entry = ide.Value as CacheEntry;
                        if (entry != null)
                        {
                            if (entry.Value is CallbackEntry)
                            {
                                EventId eventId = null;
                                OperationID opId = operationContext.OperatoinID;
                                CallbackEntry cbEtnry = (CallbackEntry) entry.Value;
                                EventContext eventContext = null;

                                if (cbEtnry != null && cbEtnry.ItemRemoveCallbackListener != null &&
                                    cbEtnry.ItemRemoveCallbackListener.Count > 0)
                                {
                                    //generate event id
                                    if (!operationContext.Contains(OperationContextFieldName.EventContext))
                                        //for atomic operations
                                    {
                                        eventId = EventId.CreateEventId(opId);
                                    }
                                    else //for bulk
                                    {
                                        eventId =
                                            ((EventContext)
                                                operationContext.GetValueByField(OperationContextFieldName.EventContext))
                                                .EventID;
                                    }

                                    eventId.EventType = Alachisoft.NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK;
                                    eventContext = new EventContext();
                                    eventContext.Add(EventContextFieldName.EventID, eventId);
                                    EventCacheEntry eventCacheEntry =
                                        CacheHelper.CreateCacheEventEntry(cbEtnry.ItemRemoveCallbackListener, entry);
                                    eventContext.Item = eventCacheEntry;
                                    eventContext.Add(EventContextFieldName.ItemRemoveCallbackList,
                                        cbEtnry.ItemRemoveCallbackListener.Clone());

                                    RaiseAsyncCustomRemoveCalbackNotifier(ide.Key, entry, reason, operationContext,
                                        eventContext);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="group"></param>
        /// <param name="ir"></param>
        /// <param name="notify"></param>
        /// <param name="lockId"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
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
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, object lockId,
            LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Remove", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            bool suspectedErrorOccured = false;
            Address targetNode = null;
            CacheEntry entry = null;

            if (_internalCache == null) throw new InvalidOperationException();

            object actualKey = key;
            CallbackEntry cbEntry = null;
            
            if (key is object[])
            {
                object[] package = key as object[];
                actualKey = package[0];
                cbEntry = package[1] as CallbackEntry;
            }
            while (true)
            {
                try
                {
                    targetNode = GetNextNode(actualKey as string);
                    if (targetNode != null)
                    {
                        if (targetNode.CompareTo(LocalAddress) == 0)
                        {
                            entry = Local_Remove(actualKey, ir, Cluster.LocalAddress, cbEntry, notify, lockId, accessType, operationContext);
                        }
                        else
                        {
                            entry = Clustered_Remove(targetNode, actualKey, ir, cbEntry, notify, lockId, accessType, operationContext);
                        }
                    }
                    break;

                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.Remove",
                            targetNode + " left while addition. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.Remove",
                            targetNode + " operation timed out. Error: " + te.ToString());
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
                    _distributionMgr.Wait(actualKey);
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

            return entry;
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
        public override Hashtable Remove(IList keys, ItemRemoveReason ir, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.Remove", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();
            CallbackEntry cbEntry = null;

            Hashtable result = new Hashtable();

            if (keys != null && keys.Count > 0)
            {
                if (_internalCache == null) throw new InvalidOperationException();
                
                long clientLastViewId = GetClientLastViewId(operationContext);

                if (clientLastViewId == Cluster.LastViewID && !IsInStateTransfer())
                {
                    result = OptimizedRemove(keys, ir, cbEntry, notify, operationContext);
                }
                else
                {
                    result = ClusteredRemove(keys, ir, cbEntry, notify, operationContext);
                }
            }
            return result;
        }

        private Hashtable ClusteredRemove(IList keys, ItemRemoveReason ir, CallbackEntry cbEntry, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.RemoveBlk", "");

            Hashtable targetNodes = null;
            Hashtable result = new Hashtable();
            Hashtable tmpResult = null;

            ArrayList totalKeys = new ArrayList(keys);
            ArrayList totalRemainingKeys = new ArrayList();

            Hashtable totalDepKeys = new Hashtable();

            Address targetNode = null;

            do
            {
                targetNodes = GetTargetNodes(totalKeys);
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
                                tmpResult = Local_Remove(currentKeys, ir, Cluster.LocalAddress, cbEntry, notify, operationContext);
                            }
                            else
                            {
                                tmpResult = Clustered_Remove(targetNode, currentKeys, ir, cbEntry, notify, operationContext);
                            }
                        }
                        catch (Runtime.Exceptions.SuspectedException se)
                        {
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

                totalKeys = new ArrayList(totalRemainingKeys);
                totalRemainingKeys.Clear();
            }
            while (totalKeys.Count > 0);

            if (result.Count > 0)
            {
                IDictionaryEnumerator ide = result.GetEnumerator();
                while (ide.MoveNext())
                {
                    object key = ide.Key;
                    CacheEntry entry = (CacheEntry) ide.Value;
                    // Already fired from LocalCacheBase
                    if (notify)
                    {
                        object value = entry.Value;
                        if (value is CallbackEntry)
                        {
                            RaiseCustomRemoveCalbackNotifier(key, entry, ir);
                        }
                    }
                }
            }


            return result;
        }

        private Hashtable OptimizedRemove(IList keys, ItemRemoveReason ir, CallbackEntry cbEntry, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PoRCache.RemoveBlk", "");

            Hashtable result = new Hashtable();
            Hashtable totalDepKeys = new Hashtable();

            ArrayList remainingKeys = new ArrayList();

            try
            {
                result = Local_Remove(keys, ir, Cluster.LocalAddress, cbEntry, notify, operationContext);
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
                IDictionaryEnumerator ie = result.GetEnumerator();
                while (ie.MoveNext())
                {
                    if (ie.Value is StateTransferException)
                    {
                        remainingKeys.Add(ie.Key);
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
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Remove the object from the local cache only. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheEntry Local_Remove(object key, ItemRemoveReason ir, Address src, CallbackEntry cbEntry, bool notify, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            if (_internalCache != null)
            {
                retVal = _internalCache.Remove(key, ir, notify, lockId, accessType, operationContext);
            }
            return retVal;
        }

        /// <summary>
        /// Remove the objects from the local cache only. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="ir"></param>
        /// <param name="src"></param>
        /// <param name="cbEntry"></param>
        /// <param name="notify"></param>
        /// <param name="operationContext"></param>
        /// <returns>list of removed keys.</returns>
        private Hashtable Local_Remove(IList keys, ItemRemoveReason ir, Address src, CallbackEntry cbEntry, bool notify, OperationContext operationContext)
        {
            Hashtable removedKeys = null;


            if (_internalCache != null)
            {
                removedKeys = _internalCache.Remove(keys, ir, notify, operationContext);

            }
            return removedKeys;
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
                object[] param = (object[]) info;
                CallbackEntry cbEntry = null;
                OperationContext oc = null;

                if (param.Length > 3)
                    cbEntry = param[3] as CallbackEntry;
               if (param.Length == 2)
                    oc = param[1] as OperationContext;

                if (param.Length == 7)
                    oc = param[6] as OperationContext;

                if (param.Length == 5)
                    oc = param[4] as OperationContext;

                if (param[0] is object[])
                {
                    Hashtable table = Local_Remove((object[])param[0], (ItemRemoveReason)param[1], src, cbEntry, (bool)param[2], oc);
                    return table;
                }
                else
                {
                    object lockId = param[4];
                    LockAccessType accessType = (LockAccessType)param[5];
                    CacheEntry e = Local_Remove(param[0], (ItemRemoveReason)param[1], src, cbEntry, (bool)param[2], lockId, accessType, oc);
                    OperationResponse opRes = new OperationResponse();
                    if (e != null)
                    {
                        UserBinaryObject ubObject =
                            (UserBinaryObject) (e.Value is CallbackEntry ? ((CallbackEntry) e.Value).Value : e.Value);
                        opRes.UserPayload = ubObject.Data;

                        opRes.SerializablePayload = e.CloneWithoutValue();
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

            IDictionaryEnumerator localEnumerator = new LazyPartitionedKeysetEnumerator(this, (object[]) handleKeyList(),
                Cluster.LocalAddress, true);

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
            Array servers = Array.CreateInstance(typeof (Address), Cluster.Servers.Count);
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

        #endregion

        #region	/                 --- ICacheEventsListener ---           /

        #region	/                 --- OnCustomUpdateCallback ---           /

        /// <summary> 
        /// handler for item update callback event.
        /// </summary>
        void ICacheEventsListener.OnCustomUpdateCallback(object key, object value, OperationContext operationContext,
            EventContext eventContext)
        {
            if (value != null)
            {
                RaiseCustomUpdateCalbackNotifier(key, (ArrayList) value, eventContext);
            }
        }



        #endregion

        #region	/                 --- OnCustomRemoveCallback ---           /

        /// <summary> 
        /// handler for item remove callback event.
        /// </summary>
        void ICacheEventsListener.OnCustomRemoveCallback(object key, object entry, ItemRemoveReason removalReason,
            OperationContext operationContext, EventContext eventContext)
        {
            bool notifyRemove = false;
            // do not notify if explicitly removed by Remove()
            object notifyRemoval = operationContext.GetValueByField(OperationContextFieldName.NotifyRemove);

            if (notifyRemoval != null)
                notifyRemove = (bool) notifyRemoval;

            if ((removalReason == ItemRemoveReason.Removed) && !(bool) notifyRemove) return;

            if (entry != null)
            {
                RaiseCustomRemoveCalbackNotifier(key, (CacheEntry) entry, removalReason, operationContext, eventContext);
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

        

        #endregion

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
                        _internalCache.RegisterKeyNotification((string[]) Keys, updateCallback, removeCallback,
                            operationContext);
                    }
                    else
                    {
                        _internalCache.RegisterKeyNotification((string) Keys, updateCallback, removeCallback,
                            operationContext);
                    }
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
                        _internalCache.UnregisterKeyNotification((string[]) Keys, updateCallback, removeCallback,
                            operationContext);
                    }
                    else
                    {
                        _internalCache.UnregisterKeyNotification((string) Keys, updateCallback, removeCallback,
                            operationContext);
                    }
                }
            }
            return null;
        }

        public override void RegisterKeyNotification(string key, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] {key, updateCallback, removeCallback, operationContext};
            Address targetNode = null;
            do
            {
                try
                {
                    targetNode = GetNextNode(key);
                    if (targetNode.Equals(Cluster.LocalAddress))
                    {
                        handleRegisterKeyNotification(obj);
                    }
                    else
                    {
                        Function fun = new Function((byte) OpCodes.RegisterKeyNotification, obj, false);
                        object results = Cluster.SendMessage(targetNode, fun, GetFirstResponse, false);
                    }
                    break;
                }
                catch (Alachisoft.NGroups.SuspectedException se)
                {
                    if (Context.NCacheLog.IsInfoEnabled)
                        Context.NCacheLog.Info("PartServerCache.RegisterKeyNotification",
                            targetNode + " left while Registering notification");
                    throw se;
                }
                catch (Alachisoft.NCache.Common.Exceptions.TimeoutException te)
                {
                    if (Context.NCacheLog.IsInfoEnabled)
                        Context.NCacheLog.Info("PartServerCache.RegisterKeyNotification",
                            targetNode + " operation timed out");
                    throw te;
                }
                catch (Alachisoft.NCache.Caching.Exceptions.StateTransferException se)
                {
                    _distributionMgr.Wait(key);
                }
            } while (true);
        }

        public override void RegisterKeyNotification(string[] keys, CallbackInfo updateCallback,
            CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = null;
            Hashtable targetNodes = null;
            Hashtable result = new Hashtable();

            ArrayList totalKeys = new ArrayList(keys);
            Address targetNode = null;
            string[] currentKeys = null;
            targetNodes = GetTargetNodes(totalKeys);

            if (targetNodes != null && targetNodes.Count != 0)
            {
                IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                Hashtable keyList = null;


                while (ide.MoveNext())
                {
                    targetNode = ide.Key as Address;
                    keyList = (Hashtable) ide.Value;

                    if (targetNode != null && keyList != null)
                    {
                        currentKeys = new string[keyList.Count];
                        int j = 0;
                        foreach (object key in keyList.Keys)
                        {
                            int index = totalKeys.IndexOf(key);
                            if (index != -1)
                            {
                                currentKeys[j] = (string) totalKeys[index];
                                j++;
                            }
                        }

                        try
                        {
                            obj = new object[] {currentKeys, updateCallback, removeCallback, operationContext};
                            if (targetNode.Equals(Cluster.LocalAddress))
                            {
                                handleRegisterKeyNotification(obj);
                            }
                            else
                            {
                                Function fun = new Function((byte) OpCodes.RegisterKeyNotification, obj, false);
                                object rsp = Cluster.SendMessage(targetNode, fun, GetFirstResponse, false);
                            }
                        }
                        catch (Alachisoft.NGroups.SuspectedException se)
                        {
                            if (Context.NCacheLog.IsInfoEnabled)
                                Context.NCacheLog.Info("PartServerCache.RegisterKeyNotification",
                                    targetNode + " left while Registering notification");
                            throw se;
                        }
                        catch (Alachisoft.NCache.Common.Exceptions.TimeoutException te)
                        {
                            if (Context.NCacheLog.IsInfoEnabled)
                                Context.NCacheLog.Info("PartServerCache.RegisterKeyNotification",
                                    targetNode + " operation timed out");
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
         
            return new NewHashmap(Cluster.LastViewID,_distributionMgr.GetOwnerHashMapTable(Cluster.Renderers, out bucketSize), membersList);
        }

        public override void InstallHashMap(DistributionMaps distributionMaps, ArrayList leftMbrs)
        {
            _distributionMgr.InstallHashMap(distributionMaps, leftMbrs);
        }

        protected override DistributionMaps GetMaps(DistributionInfoData info)
        {
            return _distributionMgr.GetMaps(info);
        }

        internal override void AutoLoadBalance()
        {
            if (_distributionMgr.CandidateNodesForBalance.Count > 0)
            {
                DetermineClusterStatus();
                ArrayList candidateNodes = _distributionMgr.CandidateNodesForBalance;
                if (candidateNodes != null && candidateNodes.Count > 0)
                {
                    DistributionMaps maps = null;
                    DistributionManager.CandidateNodeForLoadBalance candidateNode =
                        candidateNodes[0] as DistributionManager.CandidateNodeForLoadBalance;
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedCache.AutoLoadBalance",
                            "candidate node count: " + candidateNodes.Count + " candidate node :" + candidateNode.Node +
                            " above avg(%) :" + candidateNode.PercentageAboveAverage);
                    PartNodeInfo nodeInfo = new PartNodeInfo(candidateNode.Node as Address, null, false);
                    DistributionInfoData distInfo = new DistributionInfoData(DistributionMode.Manual,
                        ClusterActivity.None, nodeInfo);
                    maps = _distributionMgr.GetMaps(distInfo);

                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedCache.AutoLoadBalance", "result :" + maps.BalancingResult);

                    if (maps.BalancingResult == BalancingResult.Default)
                    {
                        PublishMaps(maps);
                    }
                }
                else if (NCacheLog.IsInfoEnabled)
                    NCacheLog.Info("PartitionedCache.AutoLoadBalance", "No need to load balance");
            }
        }

        #endregion

        #region lock

        private LockOptions Local_Lock(object key, LockExpiration lockExpiration, ref object lockId,
            ref DateTime lockDate, OperationContext operationContext)
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
            DateTime lockDate = (DateTime) package[2];
            LockExpiration lockExpiration = (LockExpiration) package[3];
            OperationContext operationContext = package[4] as OperationContext;

            return Local_Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
        }

        public override LockOptions Lock(object key, LockExpiration lockExpiration, ref object lockId,
            ref DateTime lockDate, OperationContext operationContext)
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
                address = GetNextNode(key as string);

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
                        lockInfo = Clustered_Lock(address, key, lockExpiration, ref lockId, ref lockDate,
                            operationContext);
                    }
                    return lockInfo;
                }
                catch (Runtime.Exceptions.SuspectedException se)
                {
                    suspectedErrorOccured = true;
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.lock",
                            address + " left while trying to lock the key. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.lock",
                            address + " operation timed out. Error: " + te.ToString());
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
                    _distributionMgr.Wait(key);
                }
            }
        }

        public override void UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity)
                ServerMonitor.LogClientActivity("PartCache.Unlock", "lock_id :" + lockId);
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            Address address = null;
            bool suspectedErrorOccured = false;

            while (true)
            {
                address = GetNextNode(key as string);

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
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.unlock",
                            address + " left while trying to lock the key. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.unlock",
                            address + " operation timed out. Error: " + te.ToString());
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
                    _distributionMgr.Wait(key);
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
            bool isPreemptive = (bool) package[2];
            OperationContext operationContext = package[3] as OperationContext;

            Local_UnLock(key, lockId, isPreemptive, operationContext);
        }

        private LockOptions Local_IsLocked(object key, ref object lockId, ref DateTime lockDate,
            OperationContext operationContext)
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
            DateTime lockDate = (DateTime) package[2];
            OperationContext operationContext = null;
            if (package.Length > 3)
                operationContext = package[3] as OperationContext;

            return Local_IsLocked(key, ref lockId, ref lockDate, operationContext);
        }

        public override LockOptions IsLocked(object key, ref object lockId, ref DateTime lockDate,
            OperationContext operationContext)
        {
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            LockOptions lockInfo = null;
            Address address = null;
            bool suspectedErrorOccured = false;

            while (true)
            {
                address = GetNextNode(key as string);

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
                    //we redo the operation
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.lock",
                            address + " left while trying to lock the key. Error: " + se.ToString());
                    continue;
                }
                catch (Runtime.Exceptions.TimeoutException te)
                {
                    if (NCacheLog.IsInfoEnabled)
                        NCacheLog.Info("PartitionedServerCache.lock",
                            address + " operation timed out. Error: " + te.ToString());
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
                    _distributionMgr.Wait(key);
                }
            }
        }

        #endregion

        private EnumerationDataChunk Clustered_GetNextChunk(Address address, EnumerationPointer pointer,
            OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int) OpCodes.GetNextChunk, new object[] {pointer, operationContext});
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

    }
}



