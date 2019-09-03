//  Copyright (c) 2019 Alachisoft
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
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Caching.AutoExpiration;
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
#if SERVER 
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
using Alachisoft.NCache.Caching.Topologies.Clustered.Results;
using System.Collections.Generic;
using System.Net;
#endif

using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NGroups.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Resources;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Common.ErrorHandling;


namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// This class provides the partitioned cluster cache primitives. 
    /// </summary>
    internal class PartitionedServerCache : PartitionedCacheBase, ICacheEventsListener
    {
        /// <summary> The periodic update task. </summary>
        private PeriodicPresenceAnnouncer _taskUpdate;

        /// <summary>The data groups allowed for this node</summary>
        private IDictionary _dataAffinity;

        private AutomaticDataLoadBalancer _autoBalancingTask;

        private StateTransferTask _stateTransferTask;

        private new object _txfrTaskMutex = new object();

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
            RequiresModuleStateTransfer = true;
            //Initialize(cacheClasses, properties, userId, password);
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

        /// <summary> 
        /// Returns the cache local to the node, i.e., internal cache.
        /// </summary>
        protected internal override CacheBase InternalCache
        {
            get { return _internalCache; }
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
              
                if (cacheType.CompareTo("local-cache") == 0)
                {
                    if (properties.Contains("notifications"))
                    {
                        frontCacheProps.Add("notifications", properties["notifications"]);
                    }
                    _internalCache = CacheBase.Synchronized(new HashedLocalCache(cacheClasses, this, frontCacheProps, this, _context, false));
                }
                else
                {
                    throw new ConfigurationException("invalid or non-local class specified in partitioned cache");
                }

                if (properties.Contains("data-affinity"))
                {
                    _dataAffinity = (IDictionary)properties["data-affinity"];
                }

                DistributionMgr = new DistributionManager(_autoBalancingThreshold, _internalCache.MaxSize);
                DistributionMgr.NCacheLog = Context.NCacheLog;

                InternalCache.BucketSize = DistributionMgr.BucketSize;

                _stats.Nodes = new ArrayList(2);

                _initialJoiningStatusLatch = new Latch();

                InitializeCluster(properties, Name, MCAST_DOMAIN, new Identity(true, (_context.Render != null ? _context.Render.Port : 0), (_context.Render != null ? _context.Render.IPAddress : null)), twoPhaseInitialization, false);
                _stats.GroupName = Cluster.ClusterName;
                DistributionMgr.LocalAddress = LocalAddress;

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
            _stateTransferTask.DoStateTransfer(DistributionMgr.GetBucketsList(Cluster.LocalAddress), false);

        }

        private int GetBucketId(string key)
        {
            //int hashCode = key.GetHashCode();
            int hashCode = AppUtil.GetHashCode(key);
            int bucketId = hashCode / DistributionMgr.BucketSize;

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
                NotifyHashmapChanged(Cluster.LastViewID, DistributionMgr.GetOwnerHashMapTable(Cluster.Renderers,false), GetClientMappedServers(Cluster.Servers.Clone() as ArrayList), true, true);
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

         
            //coordinator is responsible for carrying out the automatic load
            //balancing...
            if (Cluster.IsCoordinator)
            {
                if (_isAutoBalancingEnabled)
                {
                   
                }
            }

            if (_context.MessageManager != null) _context.MessageManager.StartMessageProcessing();
         

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


            NodeInfo info = new NodeInfo(address as Address);
            if (identity.RendererAddress != null)
                info.RendererAddress = new Address(identity.RendererAddress, identity.RendererPort);
            info.IsInproc = identity.RendererPort == 0;
            info.SubgroupName = identity.SubGroupName;
            lock (_stats.Nodes.SyncRoot)
            {
                _stats.Nodes.Add(info);
            }
            DistributionMgr.OnMemberJoined(address, identity);

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

            NodeInfo info = _stats.GetNode(address as Address);

            if (_context.ConnectedClients != null)
            {
                var disconnectedClients = _context.ConnectedClients.ClientsDisconnected(info.ConnectedClients, info.Address, DateTime.Now);
               
            }

            lock (_stats.Nodes.SyncRoot)
            {
                _stats.Nodes.Remove(info);
            }
            DistributionMgr.OnMemberLeft(address, identity);

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
                    
                case (int)OpCodes.Remove:
                    return handleRemove(src, func.Operand);

                case (int)OpCodes.Clear:
                    return handleClear(src, func.Operand);

                case (int)OpCodes.KeyList:
                    return handleKeyList();

                case (int)OpCodes.NotifyAdd:
                    return handleNotifyAdd(func.Operand);

                case (int)OpCodes.NotifyOldAdd:
                    return handleOldNotifyAdd(func.Operand);

                case (int)OpCodes.NotifyUpdate:
                    return handleNotifyUpdate(func.Operand);

                case (int)OpCodes.NotifyOldUpdate:
                    return handleOldNotifyUpdate(func.Operand);

                case (int)OpCodes.NotifyRemoval:
                    return handleNotifyRemoval(func.Operand);

                case (int)OpCodes.NotifyOldRemoval:
                    return handleOldNotifyRemoval(func.Operand);

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

               
                case (int)OpCodes.RemoveByTag:
                    return handleRemoveByTag(func.Operand);
                    
                case (int)OpCodes.GetNextChunk:
                    return handleGetNextChunk(src, func.Operand);

                case (int)OpCodes.GetMessageCount:
                    return handleMessageCount(func.Operand);


                case (int)OpCodes.GetAttribs:
                    return HandleGetEntryAttributes(func.Operand);

            }
            return base.HandleClusterMessage(src, func);
        }

        private object HandleGetEntryAttributes(object operand)
        {
            try
            {
                object[] args = operand as object[];
                return _internalCache.GetEntryAttributeValues(args[0], (List<string>)args[1], args[2] as OperationContext);
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        private object handleRemoveByTag(object info)
        {
            if (_internalCache != null)
            {
                object[] data = (object[])info;
                var operationContext = data[3] as OperationContext;
                if (operationContext != null) operationContext.UseObjectPool = false;

                Hashtable removed = Local_RemoveTag(data[0] as string[], (TagComparisonType)data[1], (bool)data[2], operationContext);
                return removed;
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
                DistributionMgr.UpdateBucketStats(other);
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
            if (updateBucketstats) DistributionMgr.UpdateBucketStats(_stats.LocalNode);
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

        private IDictionary GetTargetNodes(ClusteredArrayList keys, string group)
        {
            Hashtable targetNodes = new Hashtable();
            Address targetNode = null;

            if (keys != null)
            {
                foreach (object key in keys)
                {
                    targetNode = GetTargetNode(key as string, group);
                    if (targetNode != null)
                    {
                        if (targetNodes.Contains(targetNode))
                        {
                            Hashtable keyList = (Hashtable)targetNodes[targetNode];
                            if (!keyList.Contains(key))
                            {
                                keyList.Add(key, null);
                            }
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
                    result = DistributionMgr.LockBuckets(bucketIds, owner);
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
            DistributionMgr.ChangeBucketStatusToStateTransfer(bucketIds, src);
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
                    corresponder = new StateTxfrCorresponder(this, DistributionMgr, sender, transferType);
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
                DistributionMgr.ReleaseBuckets(bucketIds, src);
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
                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

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
                        catch (NGroups.SuspectedException se)
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
                            DistributionMgr.Wait(remainingKeys[0], null);
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
                            //tempTable = new Hashtable();
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
                        catch (NGroups.SuspectedException se)
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
                            DistributionMgr.Wait(remainingKeys[0], null);
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
        /// Returns the number of messages published for a certain topic.
        /// </summary>
        /// <param name="topicName">The name of the topic whose message count is to be determined.</param>
        /// <returns>The count of messages for the topic whose name is topicName.</returns>
        public override long GetMessageCount(string topicName)
        {
            if (_internalCache == null)
            {
                throw new InvalidOperationException();
            }
            long messageCount = 0;

            if (IsInStateTransfer())
            {
                messageCount = Clustered_MessageCount(GetDestInStateTransfer(), topicName);
            }
            else
            {
                messageCount = Clustered_MessageCount(this.Servers, topicName);
            }
            return messageCount;
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

                    node = GetTargetNode(key as string, group);
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
                catch (NGroups.SuspectedException se)
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
                    DistributionMgr.Wait(key, group);
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
        public override Hashtable Contains(IList keys, string group, OperationContext operationContext)
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
                        catch (NGroups.SuspectedException se)
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
        public override void Clear(Caching.Notifications notification, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
        {
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            string taskId = null;
            if (updateOptions == DataSourceUpdateOptions.WriteBehind)
                taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

            if (Servers.Count > 1)
            {
                Clustered_Clear(notification, taskId, false, operationContext);
            }
            else
            {
                handleClear(Cluster.LocalAddress, new object[] { notification, taskId, operationContext });
            }

        }

        /// <summary>
        /// Removes all entries from the local cache only.
        /// </summary>
        private void Local_Clear(Address src, Caching.Notifications notification, string taskId, OperationContext operationContext)
        {
            CacheEntry entry = null;
            try
            {
                
                if (_internalCache != null)
                {
                    _internalCache.Clear(null, DataSourceUpdateOptions.None, operationContext);
                   
                }
            }
            finally
            {
                if (entry != null)
                    entry.MarkFree(NCModulesConstants.Topology);
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
                Caching.Notifications notification = null;
                string taskId = null;
                OperationContext operationContext = null;

                if (args.Length > 0)
                    notification = args[0] as Caching.Notifications;
                if (args.Length > 1)
                    taskId = args[1] as string;
                if (args.Length > 2)
                    operationContext = args[2] as OperationContext;

                Local_Clear(src, notification, taskId, operationContext);
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

    

        protected new void Clustered_RegisterPollingNotification(short callbackId, OperationContext context)
        {
            try
            {
                if (Cluster.Servers.Count == 1 && Cluster.Servers[0].ToString().Contains(LocalAddress.IpAddress.ToString()))
                    Local_RegisterPollingNotification(callbackId, context);

                Clustered_RegisterPollingNotification(Cluster.Servers, callbackId, context, false);
            }
            catch (Exception)
            {
                Clustered_RegisterPollingNotification(Cluster.Servers, callbackId, context, false);
            }
        }

        protected new void Clustered_RegisterPollingNotification(ArrayList servers, short callbackId, OperationContext context, bool excludeSelf)
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
                address = GetTargetNode(key as string, group);

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
                catch (NGroups.SuspectedException se)
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
                    //nTrace.error("PartitionedServerCache.Clustered_GetGroup", "bucket transfered. Error: " + se.ToString());
                    DistributionMgr.Wait(key, group);
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
                var operationContext = data[2] as OperationContext;
                operationContext.UseObjectPool = false;

                HashVector keyValues = _internalCache.GetTagData(data[0] as string[], (TagComparisonType)data[1], operationContext);
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
            //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Get", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            Address address = null;
            CacheEntry e = null;
            bool suspectedErrorOccured = false;

            while (true)
            {
                address = GetTargetNode(key as string, null);

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
                    DistributionMgr.Wait(key, null);
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
                    CacheEntry e = result[entry.Key] as CacheEntry;
                    e?.MarkFree(NCModulesConstants.Global);

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
            bool clusterCall = false;
            do
            {
                targetNodes = (Hashtable)GetTargetNodes(totalKeys, null);

                IDictionaryEnumerator ide = targetNodes.GetEnumerator();
                Hashtable keyList = null;
                //We select one node at a time for operation.
                while (ide.MoveNext())
                {
                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

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
                                clusterCall = true;
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
                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                CacheEntry e = (CacheEntry)ine.Value;
                if (e == null)
                {
                    _stats.BumpMissCount();
                }
                else
                {
                    if (clusterCall )
                        ((CacheEntry)(ine.Value)).MarkInUse(NCModulesConstants.Global);
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

        public override IDictionary GetEntryAttributeValues(object key, IList<string> columns, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.GetAttribs", "");
            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            Address address = null;
            Hashtable result = null;
            bool suspectedErrorOccured = false;

            while (true)
            {
                address = GetTargetNode(key as string, null);

                if (address == null)
                {
                    NCacheLog.Error("PartitionedServerCache.GetEntryAttributeValues()", "specified key does not map to any node. return.");
                    return null;
                }

                try
                {
                    if (address.CompareTo(Cluster.LocalAddress) == 0)
                    {
                        if (_internalCache != null)
                            result = (Hashtable)_internalCache.GetEntryAttributeValues(key, columns, operationContext);
                        else
                            result = null;
                    }
                    else
                    {
                        result = (Hashtable)Clustered_GetEntryAttributes(address, key, columns, operationContext);
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
                    DistributionMgr.Wait(key, null);
                }
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
        private CacheEntry Local_Get(object key, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType access, OperationContext operationContext)
        {
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.Topology);

                CacheEntry retVal = null;
                if (_internalCache != null)
                    retVal = _internalCache.Get(key, ref version, ref lockId, ref lockDate, lockExpiration, access, operationContext);
                return retVal;
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.Topology);
            }
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
            try
            {
                operationContext?.MarkInUse(NCModulesConstants.Topology);

                if (_internalCache != null)
                    return _internalCache.GetGroup(key, group, subGroup, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);

                return null;
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.Topology);
            }
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
                    var operationContext = args[1] as OperationContext;
                    operationContext.UseObjectPool = false;

                    return Local_Get((object[])args[0], operationContext);
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
                    operationContext.UseObjectPool = false;

                    CacheEntry entry = Local_Get(key, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
                    OperationResponse opRes = new OperationResponse();
                    object[] response = new object[4];
                    if (entry != null)
                    {
                        if (_context.InMemoryDataFormat.Equals(DataFormat.Binary))
                        {
                            UserBinaryObject ubObject = (UserBinaryObject)(entry.Value);
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

                if(operationContext != null) operationContext.UseObjectPool = false;

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

                    CacheEntry entry = null;
                    try
                    {
                        entry = Local_GetGroup(key, group, subGroup, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
                        if (entry != null)
                        {
                            UserBinaryObject ubObject = (UserBinaryObject)(entry.Value);
                            opRes.UserPayload = ubObject.Data;
                            response[0] = entry.Clone();
                        }
                        response[1] = lockId;
                        response[2] = lockDate;
                        response[3] = version;
                        opRes.SerializablePayload = response;

                        return opRes;
                    }
                    finally
                    {
                        if (entry != null) entry.MarkFree(NCModulesConstants.Global);
                    }
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
            //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Add_1", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            Address targetNode = null;
            string taskId = null;
            CacheAddResult result = CacheAddResult.Failure;
            try
            {
                if (_internalCache == null) throw new InvalidOperationException();
                if (cacheEntry != null) cacheEntry.MarkInUse(NCModulesConstants.Topology);
                if (operationContext != null)
                    operationContext.MarkInUse(NCModulesConstants.Topology);
                if (cacheEntry.Flag != null && cacheEntry.Flag.IsBitSet(BitSetConstants.WriteBehind))
                    taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

                #region -- PART I -- Cascading Dependency Operation
                object[] keys = cacheEntry.KeysIAmDependingOn;
                if (keys != null)
                {
                    Hashtable goodKeysTable = Contains(keys, operationContext);

                    if (!goodKeysTable.ContainsKey("items-found"))
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND),false);

                    if (goodKeysTable["items-found"] == null)
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND),false);

                    if (goodKeysTable["items-found"] == null || ((ArrayList)goodKeysTable["items-found"]).Count != keys.Length)
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND),false);

                }
                #endregion

                result = Safe_Clustered_Add(key, cacheEntry, out targetNode, taskId, operationContext);

                #region -- PART II -- Cascading Dependency Operation
                if (result == CacheAddResult.Success)
                {
                    Hashtable ret = null;
                    // Hashtable keysTable = new Hashtable();
                    Hashtable keyDepInfoTable = new Hashtable();
                    try
                    {
                        // keysTable = GetKeysTable(key, cacheEntry.KeysIAmDependingOn);
                        keyDepInfoTable = GetKeyDependencyInfoTable(key, cacheEntry);

                        // if (keysTable != null)
                        if (keyDepInfoTable != null)
                        {
                            //Fix for NCache Bug4981
                            object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);
                            if (generateQueryInfo == null)
                            {
                                operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                            }

                            // ret = AddDepKeyList(keysTable, operationContext);
                            ret = AddDepKeyList(keyDepInfoTable, operationContext);

                            if (generateQueryInfo == null)
                            {
                                operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        var removedEntry = Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);

                        // If Adding dependency keys failed due to some exception, the added entry is 
                        // removed and hence return that entry to pool
                        if (removedEntry != null)
                            MiscUtil.ReturnEntryToPool(removedEntry, Context.TransactionalPoolManager);

                        throw e;
                    }
                    if (ret != null)
                    {
                        IDictionaryEnumerator en = ret.GetEnumerator();
                        while (en.MoveNext())
                        {
                            if (en.Value is bool && !((bool)en.Value))
                            {
                                var removedEntry = Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);

                                // If Adding dependency keys failed due to some reason, the added entry is removed and hence return that entry to pool
                                if (removedEntry != null)
                                    MiscUtil.ReturnEntryToPool(removedEntry, Context.TransactionalPoolManager);

                                NCacheLog.Info("PartitionedServerCache.Add(): ", "One of the dependency keys does not exist. Key: " + en.Key.ToString());
                                throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND),false);
                            }
                        }
                    }
                }
                #endregion

                return result;
            }
            finally
            {
                if (cacheEntry != null) cacheEntry.MarkFree(NCModulesConstants.Topology);
                if (operationContext != null)
                    operationContext.MarkFree(NCModulesConstants.Topology);
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
            CacheEntry cacheEntry = null;
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Add_3", "");

                /// Wait until the object enters the running status
                _statusLatch.WaitForAny(NodeStatus.Running);

                if (_internalCache == null) throw new InvalidOperationException();

                #region -- PART I -- Cascading Dependency Operation
                cacheEntry = CacheEntry.CreateCacheEntry(Context.TransactionalPoolManager);
                cacheEntry.ExpirationHint = eh;
                cacheEntry.MarkInUse(NCModulesConstants.Topology);
                object[] keys = cacheEntry.KeysIAmDependingOn;
                if (keys != null)
                {
                    Hashtable goodKeysTable = Contains(keys, operationContext);

                    if (!goodKeysTable.ContainsKey("items-found"))
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND),false);

                    if (goodKeysTable["items-found"] == null)
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND), false);

                    if (goodKeysTable["items-found"] == null || ((ArrayList)goodKeysTable["items-found"]).Count != keys.Length)
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND), false);
                }
                #endregion

                bool suspectedErrorOccured = false;
                Address targetNode = null;

                while (true)
                {
                    try
                    {
                        targetNode = GetTargetNode(key as string, group);
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
                    catch (NGroups.SuspectedException se)
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
                        DistributionMgr.Wait(key, group);
                    }

                }

                #region -- PART II -- Cascading Dependency Operation
                if (result)
                {
                    Hashtable ret = null;
                    // Hashtable keysTable = new Hashtable();
                    Hashtable keyDepInfoTable = new Hashtable();

                    try
                    {
                        // keysTable = GetKeysTable(key, cacheEntry.KeysIAmDependingOn);
                        keyDepInfoTable = GetKeyDependencyInfoTable(key, cacheEntry);
                        // if (keysTable != null)
                        if (keyDepInfoTable != null)
                            // ret = AddDepKeyList(keysTable, operationContext);
                            ret = AddDepKeyList(keyDepInfoTable, operationContext);
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
                                throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND), false);
                            }
                        }
                    }
                }
                #endregion

                return result;
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);

                MiscUtil.ReturnEntryToPool(cacheEntry, Context.TransactionalPoolManager);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public override bool Add(object key, string group,  OperationContext operationContext)
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
                    targetNode = GetTargetNode(key as string, group);
                    if (targetNode.CompareTo(LocalAddress) == 0)
                    {
                        return Local_Add(key, operationContext);
                    }
                    else
                    {
                        return Clustered_Add(targetNode, key, operationContext);
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
                catch (NGroups.SuspectedException se)
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
                    DistributionMgr.Wait(key, group);
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
            CacheEntry clone = null;
            CacheAddResult retVal = CacheAddResult.Failure;

            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);
                if (taskId != null)
                {
                    clone = cacheEntry.DeepClone(Context.TransactionalPoolManager);
                    clone.MarkInUse(NCModulesConstants.Topology);
                }
                else
                    clone = cacheEntry;

                if (_internalCache != null)
                {
                    operationContext?.MarkInUse(NCModulesConstants.Topology);

                    retVal = _internalCache.Add(key, cacheEntry, notify, operationContext);

                   
                        // Add operation failed so Write-Behind is skipped. Return the clone to pool thus.
                        if (!ReferenceEquals(cacheEntry, clone))
                            MiscUtil.ReturnEntryToPool(clone, Context.TransactionalPoolManager);
                   
                }

            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.Topology);

                operationContext?.MarkFree(NCModulesConstants.Topology);
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
        private bool Local_Add(object key,  OperationContext operationContext)
        {
            bool retVal = false;
            if (_internalCache != null)
                retVal = _internalCache.Add(key, operationContext);
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
            int maxTries = _stats.Nodes.Count > 3 ? 3 : _stats.Nodes.Count;
            CacheAddResult result = CacheAddResult.Failure;
            targetNode = null;
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);
                if (operationContext != null)
                    operationContext.MarkInUse(NCModulesConstants.Topology);
                do
                {
                    string group = cacheEntry.GroupInfo == null ? null : cacheEntry.GroupInfo.Group;

                    try
                    {
                        targetNode = GetTargetNode(key as string, group);

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
                        DistributionMgr.Wait(key, group);
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
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);
                if (operationContext != null)
                    operationContext.MarkFree(NCModulesConstants.Topology);
            }
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
                    if (userPayload != null)
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
                operationContext.UseObjectPool = false;

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

            Hashtable infoTable= new Hashtable();

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
            {
                operationContext = args[1] as OperationContext;
                operationContext.UseObjectPool = false;
            }
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

            CacheEntry[] goodEntries = null;
            CacheEntry[] currentValues = null;
            CacheEntry originalEntry = null;
            Hashtable removedValues = null;
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.Topology);

                #region -- PART I -- Cascading Dependency Operation

                for (int i = 0; i < totalEntries.Count; i++)
                {
                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

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
                            result.Add(keys[i],  new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND)));
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

                goodEntries = new CacheEntry[goodEntriesList.Count];
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
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);

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
                    currentValues = new CacheEntry[remainingKeys.Count];

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
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                        object key = ie.Current;

                        originalEntry = (CacheEntry)depResult[key];
                        KeyDependencyInfo[] keyDepInfos = originalEntry.KeysIAmDependingOnWithDependencyInfo;
                        if (keyDepInfos != null)
                        {
                            for (int i = 0; i < keyDepInfos.Length; i++)
                            {
                                if (!totalDepKeys.Contains(keyDepInfos[i].Key))
                                {
                                    totalDepKeys.Add(keyDepInfos[i].Key, new ArrayList());
                                }
                                ((ArrayList)totalDepKeys[keyDepInfos[i].Key]).Add(new KeyDependencyInfo(key.ToString()));
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
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                        if (!(bool)ten.Value)
                        {
                            try
                            {
                                ArrayList relevantList = (ArrayList)totalDepKeys[ten.Key];
                                string[] relevantKeys = new string[relevantList.Count];

                                for (int i = 0; i < relevantKeys.Length; i++)
                                {
                                    relevantKeys[i] = ((KeyDependencyInfo)relevantList[i]).Key;
                                }
                                removedValues = Remove(relevantKeys, ItemRemoveReason.Removed, false, operationContext);
                            }
                            finally
                            {

                                if (removedValues != null && removedValues.Values != null)
                                {
                                    foreach (object e in removedValues.Values)
                                    {
                                        CacheEntry entry = e as CacheEntry;
                                        entry?.MarkFree(NCModulesConstants.Global);

                                        if (entry != null)
                                            MiscUtil.ReturnEntryToPool(entry, Context.TransactionalPoolManager);
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);
                if (originalEntry != null)
                    originalEntry.MarkFree(NCModulesConstants.Topology);
                if (currentValues != null)
                    currentValues.MarkFree(NCModulesConstants.Topology);
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);
            }
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
            CacheEntry[] goodEntries = null;
            CacheEntry orignalEntry = null;
            Hashtable totalSuccessfullKeys = new Hashtable();
            Hashtable totalRemainingKeys = new Hashtable();
            Hashtable removedValues = null;

            string group = cacheEntries[0].GroupInfo == null ? null : cacheEntries[0].GroupInfo.Group;

            if (_internalCache == null) throw new InvalidOperationException();
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.Topology);
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
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                        }
                        targetNode = ide.Key as Address;
                        keyList = (Hashtable)ide.Value;


                        if (targetNode != null && keyList != null)
                        {
                            currentKeys = new object[keyList.Count];
                            currentValues = new CacheEntry[keyList.Count];

                            int j = 0;
                            foreach (object key in keyList.Keys)
                            {
                                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

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
                                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

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
                                        result[currentKeys[i]] = new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));
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

                            goodEntries = new CacheEntry[goodEntriesList.Count];
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
                                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

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
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                        object key = ie.Current;
                       

                        orignalEntry = (CacheEntry)depResult[key];
                        KeyDependencyInfo[] keyDepInfos = orignalEntry.KeysIAmDependingOnWithDependencyInfo;

                        if (keyDepInfos != null)
                        {
                            for (int i = 0; i < keyDepInfos.Length; i++)
                            {
                                if (!totalDepKeys.Contains(keyDepInfos[i].Key))
                                {
                                    totalDepKeys.Add(keyDepInfos[i].Key, new ArrayList());
                                }
                                ((ArrayList)totalDepKeys[keyDepInfos[i].Key]).Add(new KeyDependencyInfo(key.ToString()));
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
                            try
                            {
                                ArrayList relevantList = (ArrayList)totalDepKeys[ten.Key];
                                string[] relevantKeys = new string[relevantList.Count];

                                for (int i = 0; i < relevantKeys.Length; i++)
                                {
                                    relevantKeys[i] = ((KeyDependencyInfo)relevantList[i]).Key;
                                }
                                removedValues =Remove(relevantKeys, ItemRemoveReason.Removed, false, operationContext);
                            }
                            finally
                            {

                                if (removedValues != null && removedValues.Values != null)
                                {
                                    foreach (object e in removedValues.Values)
                                    {
                                        CacheEntry entry = e as CacheEntry;
                                        entry?.MarkFree(NCModulesConstants.Global);

                                        if (entry != null)
                                            MiscUtil.ReturnEntryToPool(entry, Context.TransactionalPoolManager);
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);
                if (currentValues != null)
                    currentValues.MarkFree(NCModulesConstants.Topology);
                if (fullEntrySet != null && fullEntrySet.Values != null)
                    fullEntrySet.Values.MarkFree(NCModulesConstants.Topology);
                if (goodEntries != null)
                    goodEntries.MarkFree(NCModulesConstants.Topology);

            }
        }

        public override Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.Topology);

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
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);

            }
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
            CacheEntry[] clone = null;

            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.Topology);

                Hashtable added = new Hashtable();

                if (taskId != null)
                {
                    clone = new CacheEntry[cacheEntries.Length];
                    for (int i = 0; i < cacheEntries.Length; i++)
                    {
                        clone[i] = cacheEntries[i].DeepClone(Context.TransactionalPoolManager);
                        clone[i].MarkInUse(NCModulesConstants.Topology);
                    }
                }

                if (_internalCache != null)
                {
                    added = _internalCache.Add(keys, cacheEntries, notify, operationContext);

                   
                        // 'added' is probably empty so keys failed to add
                        // Return all clones to pool thus
                        if (clone != null)
                            MiscUtil.ReturnEntriesToPool(clone, Context.TransactionalPoolManager);
                   
                }

                return added;
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.Topology);
            }
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
            CacheEntry rollBack = null;
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);
                /// Wait until the object enters the running status
                _statusLatch.WaitForAny(NodeStatus.Running);
                Address targetNode = null;

                if (_internalCache == null) throw new InvalidOperationException();

                string taskId = null;
                if (cacheEntry.Flag != null && cacheEntry.Flag.IsBitSet(BitSetConstants.WriteBehind))
                    taskId = taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

                CacheInsResultWithEntry result = null;//new CacheInsResultWithEntry();

                #region -- PART I -- Cascading Dependency Operation
                object[] keys = cacheEntry.KeysIAmDependingOn;
                if (keys != null)
                {
                    Hashtable goodKeysTable = Contains(keys, operationContext);
                    if (!goodKeysTable.ContainsKey("items-found"))
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND), false);

                    if (goodKeysTable["items-found"] == null)
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND), false);

                    if (goodKeysTable["items-found"] == null || ((ArrayList)goodKeysTable["items-found"]).Count != keys.Length)
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND), false);

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
                        // Hashtable keysTable = new Hashtable();
                        Hashtable keyDepInfos = new Hashtable();

                        try
                        {
                            //Fix for NCache Bug4981
                            object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);
                            if (generateQueryInfo == null)
                            {
                                //  operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                            }

                            if (result.Entry != null && result.Entry.KeysIAmDependingOn != null)
                            {
                                table = GetFinalKeysListWithDependencyInfo(result.Entry, cacheEntry);

                                keyDepInfos = GetKeysTable(key, (KeyDependencyInfo[])table["oldKeys"]);
                                if (keyDepInfos != null)
                                    RemoveDepKeyList(keyDepInfos, operationContext);

                                keyDepInfos = GetKeyDependencyInfoTable(key, (KeyDependencyInfo[])table["newKeys"]);
                                if (keyDepInfos != null)
                                    ret = AddDepKeyList(keyDepInfos, operationContext);
                            }
                            else if (cacheEntry.KeysIAmDependingOn != null)
                            {
                                keyDepInfos = GetKeyDependencyInfoTable(key, cacheEntry);
                                if (keyDepInfos != null)
                                    ret = AddDepKeyList(keyDepInfos, operationContext);
                            }
                            if (generateQueryInfo == null)
                            {
                                operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                            }
                        }
                        catch (Exception e)
                        {
                            rollBack = Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                            throw e;
                        }
                        if (ret != null)
                        {
                            IDictionaryEnumerator en = ret.GetEnumerator();
                            rollBack?.MarkFree(NCModulesConstants.Global);
                            while (en.MoveNext())
                            {
                                try
                                {
                                    if (en.Value is bool && !((bool)en.Value))
                                    {
                                        rollBack =Remove(key, null, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                                        NCacheLog.Info("PartitionedServerCache.Insert", "One of the dependency keys does not exist. Key: " + en.Key.ToString());
                                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND), false);
                                    }
                                }
                                finally
                                {
                                    rollBack?.MarkFree(NCModulesConstants.Global);
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
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);
                rollBack?.MarkFree(NCModulesConstants.Global);

                if (rollBack != null)
                    MiscUtil.ReturnEntryToPool(rollBack, Context.TransactionalPoolManager);
            }
        }

        private Hashtable OptimizedInsert(object[] keys, CacheEntry[] cacheEntries, string taskId, bool notify, OperationContext operationContext)
        {

            Hashtable result = new Hashtable();

            Hashtable addedKeys = new Hashtable();
            Hashtable insertedKeys = new Hashtable();
            ArrayList remainingKeys = new ArrayList();

            ArrayList totalKeys = new ArrayList(keys);
            ArrayList totalEntries = new ArrayList(cacheEntries);

            Hashtable depResult = new Hashtable();

            Hashtable totalKeyDepInfos = new Hashtable();
            Hashtable oldDepKeys = new Hashtable();

            Hashtable tmpResult = new Hashtable();

            ArrayList goodKeysList = new ArrayList();
            ArrayList goodEntriesList = new ArrayList();
            CacheEntry[] currentValues = null;
            CacheEntry[] goodEntries = null;
            try
            {
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
                            result[keys[i]] =  new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));
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

                goodEntries = new CacheEntry[goodEntriesList.Count];
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
                    currentValues = new CacheEntry[remainingKeys.Count];

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
                        KeyDependencyInfo[] keyDepInfos = originalEntry.KeysIAmDependingOnWithDependencyInfo;

                        if (keyDepInfos != null)
                        {
                            for (int i = 0; i < keyDepInfos.Length; i++)
                            {
                                if (!totalKeyDepInfos.Contains(keyDepInfos[i].Key))
                                {
                                    totalKeyDepInfos.Add(keyDepInfos[i].Key, new ArrayList());
                                }
                                ((ArrayList)totalKeyDepInfos[keyDepInfos[i].Key]).Add(new KeyDependencyInfo(key.ToString()));
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
                        KeyDependencyInfo[] keyDepInfos = originalEntry.KeysIAmDependingOnWithDependencyInfo;

                        if (keyDepInfos != null)
                        {
                            for (int i = 0; i < keyDepInfos.Length; i++)
                            {
                                if (!totalKeyDepInfos.Contains(keyDepInfos[i].Key))
                                {
                                    totalKeyDepInfos.Add(keyDepInfos[i].Key, new ArrayList());
                                }
                                ((ArrayList)totalKeyDepInfos[keyDepInfos[i].Key]).Add(new KeyDependencyInfo(key.ToString()));
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

                Hashtable table = AddDepKeyList(totalKeyDepInfos, operationContext);

                if (generateQueryInfo == null)
                {
                    operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                }

                IDictionaryEnumerator ten = table.GetEnumerator();
                while (ten.MoveNext())
                {
                    if (!(bool)ten.Value)
                    {
                        ArrayList relevantList = (ArrayList)totalKeyDepInfos[ten.Key];
                        string[] relevantKeys = new string[relevantList.Count];

                        for (int i = 0; i < relevantKeys.Length; i++)
                        {
                            relevantKeys[i] = ((KeyDependencyInfo)relevantList[i]).Key;
                        }

                        Hashtable rollbackEntries = null;

                        try
                        {
                            rollbackEntries = Remove(relevantKeys, ItemRemoveReason.Removed, false, operationContext);
                        }
                        finally
                        {
                            if (rollbackEntries?.Values?.Count > 0)
                            {
                                foreach (var value in rollbackEntries.Values)
                                {
                                    if (value is CacheEntry removedCacheEntry)
                                    {
                                        MiscUtil.ReturnEntryToPool(removedCacheEntry, Context.TransactionalPoolManager);
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);

                if (goodEntries != null)
                    goodEntries.MarkFree(NCModulesConstants.Topology);

                if (currentValues != null)
                    currentValues.MarkFree(NCModulesConstants.Topology);
            }
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
            Hashtable totalKeyDepInfos = new Hashtable();
            Hashtable oldDepKeys = new Hashtable();

            Dictionary<object, CacheEntry> fullEntrySet = new Dictionary<object, CacheEntry>();

            Hashtable totalAddedKeys = new Hashtable();
            Hashtable totalInsertedKeys = new Hashtable();
            Hashtable totalRemainingKeys = new Hashtable();
            CacheEntry originalEntry = null;
            CacheEntry oldEntry = null;

            string group = cacheEntries[0].GroupInfo == null ? null : cacheEntries[0].GroupInfo.Group;

            if (_internalCache == null) throw new InvalidOperationException();
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.Topology);

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

                    while (ide.MoveNext())
                    {
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);

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
                                        result[currentKeys[i]] = new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));
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
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                        object key = ie.Current;
                       

                        originalEntry = (CacheEntry)depResult[key];
                        KeyDependencyInfo[] keyDepInfos = originalEntry.KeysIAmDependingOnWithDependencyInfo;
                        
                        if (totalKeyDepInfos != null)
                        {
                            for (int i = 0; i < keyDepInfos.Length; i++)
                            {
                                if (!totalKeyDepInfos.Contains(keyDepInfos[i].Key))
                                {
                                    totalKeyDepInfos.Add(keyDepInfos[i].Key, new ArrayList());
                                }
                                ((ArrayList)totalKeyDepInfos[keyDepInfos[i].Key]).Add(new KeyDependencyInfo(key.ToString()));
                            }
                        }
                    }
                }
                
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
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                        object key = ide.Key;
                        CacheInsResultWithEntry insResult = ide.Value as CacheInsResultWithEntry;
                      
                        originalEntry = (CacheEntry)depResult[key];
                        KeyDependencyInfo[] keyDepInfos = originalEntry.KeysIAmDependingOnWithDependencyInfo;
                        
                        if (keyDepInfos != null)
                        {
                            for (int i = 0; i < keyDepInfos.Length; i++)
                            {
                                if (!totalKeyDepInfos.Contains(keyDepInfos[i].Key))
                                {
                                    totalKeyDepInfos.Add(keyDepInfos[i].Key, new ArrayList());
                                }
                                ((ArrayList)totalKeyDepInfos[keyDepInfos[i].Key]).Add(new KeyDependencyInfo(key.ToString()));
                            }
                        }
                        oldEntry = insResult.Entry;
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

                Hashtable table = AddDepKeyList(totalKeyDepInfos, operationContext);

                if (generateQueryInfo == null)
                {
                    operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                }

                IDictionaryEnumerator ten = table.GetEnumerator();
                while (ten.MoveNext())
                {
                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    if (!(bool)ten.Value)
                    {
                        ArrayList relevantList = (ArrayList)totalKeyDepInfos[ten.Key];
                        string[] relevantKeys = new string[relevantList.Count];

                        for (int i = 0; i < relevantKeys.Length; i++)
                        {
                            relevantKeys[i] = ((KeyDependencyInfo)relevantList[i]).Key;
                        }

                        Hashtable rollbackEntries = null;

                        try
                        {
                            rollbackEntries = Remove(relevantKeys, ItemRemoveReason.Removed, false, operationContext);
                        }
                        finally
                        {
                            if (rollbackEntries?.Values?.Count > 0)
                            {
                                foreach (var value in rollbackEntries.Values)
                                {
                                    if (value is CacheEntry removedCacheEntry)
                                    {
                                        MiscUtil.ReturnEntryToPool(removedCacheEntry, Context.TransactionalPoolManager);
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);
                if (oldEntry != null)
                    oldEntry.MarkFree(NCModulesConstants.Topology);
                if (originalEntry != null)
                    originalEntry.MarkFree(NCModulesConstants.Topology);
                if (fullEntrySet != null && fullEntrySet.Values != null)
                    fullEntrySet.Values.MarkFree(NCModulesConstants.Topology);
                if (currentValues != null)
                    currentValues.MarkFree(NCModulesConstants.Topology);


            }
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
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.Topology);
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
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);
            }
        }

        private CacheInsResultWithEntry Local_Insert(object key, CacheEntry cacheEntry, Address src, string taskId, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheInsResultWithEntry retVal = null;  
            CacheEntry clone = null;

            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);

                if (taskId != null)
                {
                    clone = cacheEntry.DeepClone(Context.TransactionalPoolManager);
                    clone.MarkInUse(NCModulesConstants.Topology);
                }
                else
                    clone = cacheEntry;

                if (_internalCache != null)
                {
                    operationContext?.MarkInUse(NCModulesConstants.Topology);
                    retVal = _internalCache.Insert(key, cacheEntry, notify, lockId, version, accessType, operationContext);
                    if (retVal == null) retVal = CacheInsResultWithEntry.CreateCacheInsResultWithEntry(_context.TransactionalPoolManager);
                   
                        // Insert operation failed so Write-Behind is skipped. Return the clone to pool thus.
                        if (!ReferenceEquals(cacheEntry, clone))
                            MiscUtil.ReturnEntryToPool(clone, Context.TransactionalPoolManager);
                   
                }


            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.Topology);

                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.Topology);
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
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.Topology);

                if (taskId != null)
                {
                    clone = new CacheEntry[cacheEntries.Length];
                    for (int i = 0; i < cacheEntries.Length; i++)
                    {
                        clone[i] = cacheEntries[i].DeepClone(Context.TransactionalPoolManager);
                        clone[i].MarkInUse(NCModulesConstants.Topology);
                    }
                }

                if (_internalCache != null)
                {
                    retVal = _internalCache.Insert(keys, cacheEntries, notify, operationContext);

                   
                    
                        // 'retVal' is probably empty so keys failed to insert
                        // Return all clones to pool thus
                        if (clone != null)
                            MiscUtil.ReturnEntriesToPool(clone, Context.TransactionalPoolManager);
                   
                }
                return retVal;
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.Topology);
            }
        }

        private CacheInsResultWithEntry Safe_Clustered_Insert(object key, CacheEntry cacheEntry, out Address targetNode, string taskId, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            try
            {
                
                bool suspectedErrorOccured = false;
                int maxTries = _stats.Nodes.Count > 3 ? 3 : _stats.Nodes.Count;
                CacheInsResultWithEntry retVal = null;//new CacheInsResultWithEntry();

                string group = cacheEntry.GroupInfo == null ? null : cacheEntry.GroupInfo.Group;
                targetNode = null;
                do
                {
                    try
                    {
                        targetNode = GetTargetNode(key as string, group);

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
                        DistributionMgr.Wait(key, group);
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
                        maxTries--;
                        if (maxTries == 0)
                            throw;

                        Thread.Sleep(_serverFailureWaitTime);
                    }
                } while (maxTries > 0);
                return retVal;
            }
            finally
            {
               
            }
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

                operationContext.UseObjectPool = false;

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
                    OperationResponse opRes = new OperationResponse();
                    if (retVal.Entry != null) { 
                        retVal.Entry = retVal.Entry.CloneWithoutValue() as CacheEntry;
                        retVal.Entry.MarkInUse(NCModulesConstants.Global);
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
            Hashtable totalRemovedItems = new Hashtable();

            try
            {
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

            
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }

            finally
            {
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
            //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("PrtCache.Remove", "");

            /// Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Running);
            bool suspectedErrorOccured = false;
            Address targetNode = null;
            CacheEntry entry = null;

            if (_internalCache == null) throw new InvalidOperationException();

            object actualKey = key;
            DataSourceUpdateOptions updateOptions = DataSourceUpdateOptions.None;
            Caching.Notifications notification = null;
            string providerName = null;

            if (key is object[])
            {
                object[] package = key as object[];
                actualKey = package[0];
                updateOptions = (DataSourceUpdateOptions)package[1];
                notification = package[2] as Caching.Notifications;
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
                    targetNode = GetTargetNode(actualKey as string, group);
                    if (targetNode != null)
                    {
                        if (targetNode.CompareTo(LocalAddress) == 0)
                        {
                            entry = Local_Remove(actualKey, ir, Cluster.LocalAddress, notification, taskId, providerName, notify, lockId, version, accessType, operationContext);
                        }
                        else
                        {
                            entry = Clustered_Remove(targetNode, actualKey, ir, notification, taskId, providerName, notify, lockId, version, accessType, operationContext);
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
                    DistributionMgr.Wait(actualKey, group);
                }
            }

            if (entry != null)
            {
                RemoveDepKeyList(GetKeysTable(actualKey, entry.KeysIAmDependingOn), operationContext);
            }

            return entry;
        }

        private Hashtable OptimizedRemove(IList keys, string group, ItemRemoveReason ir, string taskId, string providerName, Caching.Notifications notification, bool notify, OperationContext operationContext)
        {

            Hashtable result = new Hashtable();
            Hashtable totalDepKeys = new Hashtable();

            ArrayList remainingKeys = new ArrayList();

            try
            {
                result = Local_Remove(keys, ir, Cluster.LocalAddress, notification, taskId, providerName, notify, operationContext);
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
                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

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
                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    object key = ide.Key;
                    CacheEntry entry = ide.Value as CacheEntry;
                    if (entry != null)
                    {
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
                Hashtable tmpResult = ClusteredRemove(remainingKeys, group, ir, taskId, providerName, notification, notify, operationContext);
                foreach (DictionaryEntry entry in tmpResult)
                {
                    result[entry.Key] = entry.Value;
                }
            }

            return result;
        }

        private Hashtable ClusteredRemove(IList keys, string group, ItemRemoveReason ir, string taskId, string providerName, Caching.Notifications notification, bool notify, OperationContext operationContext)
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
                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    targetNode = ide.Key as Address;
                    keyList = (Hashtable)ide.Value;

                    if (targetNode != null && keyList != null)
                    {
                        object[] currentKeys = MiscUtil.GetArrayFromCollection(keyList.Keys);
                        try
                        {
                            if (targetNode.Equals(Cluster.LocalAddress))
                            {
                                tmpResult = Local_Remove(currentKeys, ir, Cluster.LocalAddress, notification, taskId, providerName, notify, operationContext);
                            }
                            else
                            {
                                tmpResult = Clustered_Remove(targetNode, currentKeys, ir, notification, taskId, providerName, notify, operationContext);
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
                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    object key = ide.Key;
                    CacheEntry entry = (CacheEntry)ide.Value;
                   
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
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            DataSourceUpdateOptions updateOptions = DataSourceUpdateOptions.None;
            Caching.Notifications notification = null;
            string providerName = null;

            Hashtable result = new Hashtable();

            if (keys != null && keys.Count > 0)
            {
                if (keys[0] is object[])
                {
                    object[] package = keys[0] as object[];
                    updateOptions = (DataSourceUpdateOptions)package[1];
                    notification = package[2] as Caching.Notifications;
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
                    result = OptimizedRemove(keys, group, ir, taskId, providerName, notification, notify, operationContext);
                }
                else
                {
                    result = ClusteredRemove(keys, group, ir, taskId, providerName, notification, notify, operationContext);
                }
            }
            return result;
        }

        /// <summary>
        /// Remove the object from the local cache only. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheEntry Local_Remove(object key, ItemRemoveReason ir, Address src, Caching.Notifications notification, string taskId, string providerName, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = null;

            CacheEntry cloned = null;
            try
            {
                if (_internalCache != null)
                {
                    operationContext?.MarkInUse(NCModulesConstants.Topology);

                    retVal = _internalCache.Remove(key, ir, notify, lockId, version, accessType, operationContext);
                  
                }

            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.Topology);

                if (cloned != null)
                    cloned.MarkFree(NCModulesConstants.Topology);
            }
            return retVal;

        }

        /// <summary>
        /// Remove the objects from the local cache only. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>list of removed keys.</returns>
        private Hashtable Local_Remove(IList keys, ItemRemoveReason ir, Address src, Caching.Notifications notification, string taskId, string providerName, bool notify, OperationContext operationContext)
        {
            Hashtable removedKeys = null;


            if (_internalCache != null)
            {
                removedKeys = _internalCache.Remove(keys, ir, notify, operationContext);
                CacheEntry entry = null;
                entry?.MarkFree(NCModulesConstants.Global);
               

            }
            return removedKeys;
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
            CacheEntry e = null;
            try
            {
                object[] param = (object[])info;
                Caching.Notifications notification = null;
                string taskId = null;
                string providerName = null;
                OperationContext oc = null;

                if (param.Length > 3)
                    notification = param[3] as Caching.Notifications;
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

                if (oc != null)
                    oc.UseObjectPool = false;

                if (param[0] is object[])
                {
                    if (param.Length > 5)
                    {
                        providerName = param[5] as string;
                    }
                    Hashtable table = Local_Remove((object[])param[0], (ItemRemoveReason)param[1], src, notification, taskId, providerName, (bool)param[2], oc);

                    return table;
                }
                else
                {
                    object lockId = param[5];
                    LockAccessType accessType = (LockAccessType)param[6];
                    ulong version = (ulong)param[7];
                    e = Local_Remove(param[0], (ItemRemoveReason)param[1], src, notification, taskId, providerName, (bool)param[2], lockId, version, accessType, oc);
                    OperationResponse opRes = new OperationResponse();
                    if (e != null)
                    {
                        if (_context.InMemoryDataFormat.Equals(DataFormat.Object))
                        {
                            opRes.UserPayload = null;
                            CacheEntry cacheEntry = (CacheEntry)e.Clone();
                            cacheEntry.MarkInUse(NCModulesConstants.Global);
                            opRes.SerializablePayload = (object)cacheEntry;
                        }
                        else
                        {
                            UserBinaryObject ubObject = (UserBinaryObject)(e.Value);
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
            finally
            {
                if (e != null)
                    e.MarkFree(NCModulesConstants.Global);
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
                {
                    operationContext = param[3] as OperationContext;
                    operationContext.UseObjectPool = false;
                }

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

        #endregion

        #region	/                 --- ICacheEventsListener ---           /

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
                FilterEventContextForGeneralDataEvents(Caching.Events.EventTypeInternal.ItemAdded, eventContext);
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
        private object handleOldNotifyAdd(object info)
        {
            object[] args = info as object[];
            NotifyOldItemAdded(args[0], true, (OperationContext)args[1], (EventContext)args[2]);
            return null;

        }
        #region	/                 --- OnItemUpdated ---           /

        /// <summary> 
        /// handler for item updated event.
        /// </summary>
        void ICacheEventsListener.OnItemUpdated(object key, OperationContext operationContext, EventContext eventContext)
        {
            // Handle all exceptions, do not let the effect seep thru
            try
            {
                FilterEventContextForGeneralDataEvents(Caching.Events.EventTypeInternal.ItemRemoved, eventContext);
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
        private object handleOldNotifyUpdate(object info)
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

                NotifyOldItemUpdated(args[0], true, opContext, evContext);
            }
            else
            {
                NotifyOldItemUpdated(info, true, null, null);
            }
            return null;
        }


        #region	/                 --- OnItemRemoved ---           /

        /// <summary> 
        /// Fired when an item is removed from the cache.
        /// </summary>
        void ICacheEventsListener.OnItemRemoved(object key, object val, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext)
        {
            ((ICacheEventsListener)this).OnItemsRemoved(new object[] { key }, new object[] { val }, reason, operationContext, new EventContext[] { eventContext });
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

                    for (int i = 0; i < keys.Length; i++)
                    {
                        FilterEventContextForGeneralDataEvents(Caching.Events.EventTypeInternal.ItemRemoved, eventContext[i]);
                        object data = new object[] { keys[i], /*values[i],*/ reason, operationContext, eventContext[i] };

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


        private object handleOldNotifyRemoval(object info)
        {
            object[] objs = (object[])info;

            NotifyOldItemRemoved(objs[0], null, ItemRemoveReason.Removed, true, (OperationContext)objs[1], (EventContext)objs[2]);
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
                    NotifyItemRemoved(keys[i], null, reason, true, operationContext, eventContexts[i]);
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
            //change made after discussion with 20-04-2011 (fix for two notification on single key removel)
            // do not notify if explicitly removed by Remove()
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
        /// <param name="notification"></param>
        void ICacheEventsListener.OnWriteBehindOperationCompletedCallback(OpCode operationCode, object result, Caching.Notifications notification)
        {
        }

        #endregion

        void ICacheEventsListener.OnPollNotify(string clientId, short callbackId, Alachisoft.NCache.Caching.Events.EventTypeInternal eventtype)
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
                if (operationContext != null) operationContext.UseObjectPool = false;

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



        public override void RegisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] { key, updateCallback, removeCallback, operationContext };
            Address targetNode = null;
            do
            {
                try
                {
                    targetNode = GetTargetNode(key, null);
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
                catch (NGroups.SuspectedException se)
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
                    DistributionMgr.Wait(key, null);
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
                                fun.Cancellable = true;
                                object rsp = Cluster.SendMessage(targetNode, fun, GetFirstResponse, false);
                            }
                        }
                        catch (NGroups.SuspectedException se)
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
                    targetNode = GetTargetNode(key, null);
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
                catch (NGroups.SuspectedException se)
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
                    DistributionMgr.Wait(key, null);
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
                                fun.Cancellable = true;
                                object rsp = Cluster.SendMessage(targetNode, fun, GetFirstResponse, false);
                            }
                        }
                        catch (NGroups.SuspectedException se)
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
            return this.DistributionMgr.GetMaps(distInfo);
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
            get { return DistributionMgr.InstalledHashMap; }
            set { DistributionMgr.InstalledHashMap = value; }
        }

        public override Hashtable BucketsOwnershipMap
        {
            get { return DistributionMgr.BucketsOwnershipMap; }
            set { DistributionMgr.BucketsOwnershipMap = value; }
        }

        public override NewHashmap GetOwnerHashMapTable(out int bucketSize)
        {
            ArrayList membersList = GetClientMappedServers(this.Servers.Clone() as ArrayList);

            return new NewHashmap(Cluster.LastViewID,
                DistributionMgr.GetOwnerHashMapTable(Cluster.Renderers, false, out bucketSize),
                membersList);
        }

        public override void InstallHashMap(DistributionMaps distributionMaps, ArrayList leftMbrs)
        {
            DistributionMgr.InstallHashMap(distributionMaps, leftMbrs);
        }

        protected override DistributionMaps GetMaps(DistributionInfoData info)
        {
            return DistributionMgr.GetMaps(info);
        }

       


        internal override void AutoLoadBalance()
        {
            if (DistributionMgr.CandidateNodesForBalance.Count > 0)
            {
                DetermineClusterStatus();
                ArrayList candidateNodes = DistributionMgr.CandidateNodesForBalance;
                if (candidateNodes != null && candidateNodes.Count > 0)
                {
                    DistributionMaps maps = null;
                    DistributionManager.CandidateNodeForLoadBalance candidateNode = candidateNodes[0] as DistributionManager.CandidateNodeForLoadBalance;
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.AutoLoadBalance", "candidate node count: " + candidateNodes.Count + " candidate node :" + candidateNode.Node + " above avg(%) :" + candidateNode.PercentageAboveAverage);
                    PartNodeInfo nodeInfo = new PartNodeInfo(candidateNode.Node as Address, null, false);
                    DistributionInfoData distInfo = new DistributionInfoData(DistributionMode.Manual, ClusterActivity.None, nodeInfo, false);
                    maps = DistributionMgr.GetMaps(distInfo);

                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.AutoLoadBalance", "result :" + maps.BalancingResult);

                    if (maps.BalancingResult == BalancingResult.Default)
                    {
                        PublishMaps(maps);
                    }
                }
                else
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PartitionedCache.AutoLoadBalance", "No need to load balance");
            }
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
                address = GetTargetNode(key as string, null);

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
                    DistributionMgr.Wait(key, null);
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
                address = GetTargetNode(key as string, null);

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
                    DistributionMgr.Wait(key, null);
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
                address = GetTargetNode(key as string, null);

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
                    DistributionMgr.Wait(key, null);
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
                    throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND), false);

                if (goodKeysTable["items-found"] == null)
                    throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND), false);

                if (goodKeysTable["items-found"] == null || ((ArrayList)goodKeysTable["items-found"]).Count != keys.Length)
                    throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND), false);

            }
            #endregion

            do
            {
                try
                {
                    Address targetNode = GetTargetNode(key, group);
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
                    //nTrace.error("PartitionedServerCache.Clustered_Add", "bucket under state txfr " + key + " Error: " + se.ToString());
                    DistributionMgr.Wait(key, group);
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
                    Hashtable ret = null;
                    Hashtable keyDepInfoTable = new Hashtable();
                    try
                    {
                        keyDepInfoTable = GetKeyDependencyInfoTable(key, (KeyDependencyInfo[])CacheHelper.GetKeyDependencyInfoTable(hint));

                        if (keyDepInfoTable != null)
                            ret = AddDepKeyList(keyDepInfoTable, operationContext);
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
                                throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND), false);
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
                    Address targetNode = GetTargetNode(key, null);

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
                    DistributionMgr.Wait(key, null);
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
                    Address targetNode = GetTargetNode(key, null);
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
                    DistributionMgr.Wait(key, null);
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
                    Address targetNode = GetTargetNode(key, null);

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
                    DistributionMgr.Wait(key, null);
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
                    Address targetNode = GetTargetNode(key, null);

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
                    DistributionMgr.Wait(key, null);
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

       

      

        private EnumerationDataChunk Clustered_GetNextChunk(Address address, EnumerationPointer pointer, OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.GetNextChunk, new object[] { pointer, operationContext });
                object result = Cluster.SendMessage(address, func, GroupRequest.GET_FIRST, Cluster.Timeout);

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

     

      

#if SERVER

        

      

        private object handleContinueReplication(object info)
        {
            object[] data = (object[])info;
            bool continueReplication = Convert.ToBoolean(data[0]);

            return null;
        }
        
#endif
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
                inTransfer = DistributionMgr.InStateTransfer();
            }
            return inTransfer;
        }

        protected override bool VerifyClientViewId(long clientLastViewId)
        {
            return clientLastViewId == Cluster.LastViewID;
        }

        protected override ArrayList GetDestInStateTransfer()
        {
            ArrayList list = DistributionMgr.GetPermanentAddress(this.Servers);
            return list;
        }

        public override bool IsOperationAllowed(object key, AllowedOperationType opType)
        {
            if (base._shutdownServers != null && base._shutdownServers.Count > 0)
            {
                Address targetNode = GetTargetNode(key as string, "");

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
                        catch (NGroups.SuspectedException se)
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
                Address targetNode = GetTargetNode(messageInfo.MessageId, "");

                if (targetNode == null)
                    throw new Exception("No target node available to accommodate the data.");

                if (targetNode.Equals(Cluster.LocalAddress))
                {
                    result = _internalCache.AssignmentOperation(messageInfo, subscriptionInfo, type, context);
                }
            }
            catch (StateTransferException x)
            {
                _context.NCacheLog.Error("Partitioned.AssignmentOperation", x + " " + messageInfo.MessageId + " " + DistributionMgr.GetBucketId(messageInfo.MessageId));
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
                        Address targetNode = GetTargetNode(message, "");
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
                    Address targetNode = GetTargetNode(key as string, null);
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
                    Address targetNode = GetTargetNode(messageId.MessageId, "");

                    if (targetNode != null && targetNode.Equals(Cluster.LocalAddress))
                        localMessages.Add(messageId);
                }

                if (localMessages.Count > 0)
                {
                    _internalCache.RemoveMessages(localMessages, reason, context);
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
            int maxTries = _stats.Nodes.Count > 3 ? 3 : _stats.Nodes.Count;

            do
            {
                try
                {
                    Address targetNode = GetTargetNode(message.MessageId, "");

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
                    DistributionMgr.Wait(message.MessageId, "");
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

        public void OnOperationModeChanged(OperationMode mode)
        {

        }

        #endregion


        /// <summary>
        /// Handle cluster-wide MessageCount request.
        /// </summary>
        /// <param name="info">
        /// The object containing parameters for this operation. 
        /// It actually contains the topic name whose message count is to be determined.
        /// </param>
        /// <returns>
        /// Object to be sent back to the requester. 
        /// This object is actually the message count against the topic name passed to this method.
        /// </returns>
        private object handleMessageCount(object info)
        {
            if (info != null)
            {
                try
                {
                    object[] operands = (object[])info;
                    string topicName = operands[0] as string;
                    return Local_MessageCount(topicName);
                }
                catch (Exception)
                {
                    if (_clusteredExceptions)
                    {
                        throw;
                    }
                }
            }
            return 0;
        }

        #endregion

       
    }
}



