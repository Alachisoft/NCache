//  Copyright (c) 2018 Alachisoft
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
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NGroups;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Util;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Resources;
using System.Net;
using Alachisoft.NCache.Common.Monitoring;
using System.Collections.Generic;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Caching.Pooling;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// This class provides the partitioned cluster cache primitives. 
    /// </summary>
    internal class MirrorCache : MirrorCacheBase, IPresenceAnnouncement, ICacheEventsListener
    {
        /// <summary> The periodic update task. </summary>
        private PeriodicPresenceAnnouncer _taskUpdate;

        private PeriodicStatsUpdater _localStatsUpdater;

        private bool threadRunning = true;

        /// <summary> Asynchronous Item replicator.</summary>
        public AsyncItemReplicator _asyncReplicator;


        protected IPAddress _srvrJustLeft = null;
        private SubscriptionRefresherTask _subscriptionTask;

        private ReplicaStateTxfrCorresponder _corresponder = null;

        internal ClusteredArrayList _collectionsLoggedOperations = null;

        internal object _flagMutex = new object();

        protected StateTransferTask stateTransferTask = null;

        private OptimizedQueue _replicationQueue;
        private Thread _executionThread;
        private long _replicationAutoKey;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        /// <param name="listener">cache events listener</param>
        public MirrorCache(IDictionary cacheClasses, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(properties, listener, context)
        {
            _stats.ClassName = "mirror-server";
            Initialize(cacheClasses, properties);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheClasses"></param>
        /// <param name="properties"></param>
        /// <param name="listener"></param>
        /// <param name="context"></param>
        /// <param name="clusterListener"></param>
        /// <param name="userId"></param>
        /// <param name="password"></param>
        public MirrorCache(IDictionary cacheClasses, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, IClusterEventsListener clusterListener)
            : base(properties, listener, context, clusterListener)
        {
            _replicationQueue = new OptimizedQueue(Context);
            _stats.ClassName = "mirror-server";
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (_executionThread != null)
            {
#if !NETCORE
                _executionThread.Abort();
#elif NETCORE
                _executionThread.Interrupt();
#endif
                _executionThread = null;
            }

            if (_taskUpdate != null)
            {
                _taskUpdate.Cancel();
                _taskUpdate = null;
            }

            if (_localStatsUpdater != null)
            {
                _localStatsUpdater.Cancel();
                _localStatsUpdater = null;
            }

            if (_internalCache != null)
            {
                _internalCache.Dispose();
                _internalCache = null;
            }
            threadRunning = false;

            if (_subscriptionTask != null)
            {
                _subscriptionTask.Cancle();
                _subscriptionTask = null;
            }         

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
        protected override void Initialize(IDictionary cacheClasses, IDictionary properties)
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
                    _internalCache = CacheBase.Synchronized(new LocalCache(cacheClasses, this, frontCacheProps, this, _context));
                }
                else
                {
                    throw new ConfigurationException("invalid or non-local class specified in mirror cache");
                }

                _stats.Nodes = new ArrayList(2);

                InitializeCluster(properties, Name, MCAST_DOMAIN, new Identity(true, (_context.Render != null ? _context.Render.Port : 0), (_context.Render != null ? _context.Render.IPAddress : null)));
                _stats.GroupName = Cluster.ClusterName;
                                
                postInstrumentatedData(_stats, Name);
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
                    _internalCache = CacheBase.Synchronized(new LocalCache(cacheClasses, this, frontCacheProps, this, _context));
                }
                else
                {
                    throw new ConfigurationException("invalid or non-local class specified in mirror cache");
                }

                _stats.Nodes = new ArrayList(2);

                _asyncReplicator = new AsyncItemReplicator(Context, new TimeSpan(0, 0, 2));
                InitializeCluster(properties, Name, MCAST_DOMAIN, new Identity(true, (_context.Render != null ? _context.Render.Port : 0), (_context.Render != null ? _context.Render.IPAddress : null)), twoPhaseInitialization, false);
                _stats.GroupName = Cluster.ClusterName;

                postInstrumentatedData(_stats, Name);
                

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


        #region	/                 --- Overrides for ClusteredCache ---           /

        public override void WindUpReplicatorTask()
        {
            if (_asyncReplicator != null)
            {
                _asyncReplicator.WindUpTask();
            }
        }

        public override void WaitForReplicatorTask(long interval)
        {
            if (_asyncReplicator != null)
            {
                _asyncReplicator.WaitForShutDown(interval);
            }
        }


        /// <summary>
        /// Called after the membership has been changed. Lets the members do some
        /// member oriented tasks.
        /// </summary>
        public override void OnAfterMembershipChange()
        {
            base.OnAfterMembershipChange();
            _context.ExpiryMgr.AllowClusteredExpiry = Cluster.IsCoordinator;

            Cluster.ViewInstallationLatch.SetStatusBit(ViewStatus.COMPLETE, ViewStatus.INPROGRESS | ViewStatus.NONE);
            if (_taskUpdate == null)
            {
                _taskUpdate = new PeriodicPresenceAnnouncer(this, _statsReplInterval);
                _context.TimeSched.AddTask(_taskUpdate);

                StartStateTransfer();
            }

            if (_localStatsUpdater == null)
            {
                _localStatsUpdater = new PeriodicStatsUpdater(this);
                _context.TimeSched.AddTask(_localStatsUpdater);
            }

            if (Cluster.IsCoordinator)
            {               
                if (Cluster.Servers.Count > 1)
                {
                    _asyncReplicator.Start();

                    if (_subscriptionTask == null)
                    {
                        _subscriptionTask = new SubscriptionRefresherTask(this, _context);
                        _context.TimeSched.AddTask(_subscriptionTask);
                    }
                }
                else
                {
                    if (_subscriptionTask != null)
                    {
                        _subscriptionTask.Cancle();
                        _subscriptionTask = null;
                    }
                }
            }
            else
            {
                _asyncReplicator.Clear();
                _asyncReplicator.Stop(false);
            }

            if (Cluster.IsCoordinator)
            {
                if (_context.MessageManager != null) _context.MessageManager.StartMessageProcessing();
            }

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
            // POTeam Precautional locking
            lock (_stats.Nodes.SyncRoot)
            {
                _stats.Nodes.Add(info);
            }
            if (LocalAddress.CompareTo(address) == 0)
            {
                _stats.LocalNode = info;
            }
            if (!info.IsInproc)
                AddServerInformation(address, identity.RendererPort, info.ConnectedClients.Count);

            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("MirrorCache.OnMemberJoined()", "Replication increased: " + address);
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
            // POTeam Precautional locking
            lock (_stats.Nodes.SyncRoot)
            {
                _stats.Nodes.Remove(info);
            }
            
            if (!info.IsInproc)
                RemoveServerInformation(address, identity.RendererPort);

            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("MirrorCache.OnMemberLeft()", "Replica Removed: " + address);
            return true;
        }

        /// <summary>
        /// Handles the function requests.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public override object HandleClusterMessage(Address src, Function func)
        {
                object[] objs = null;
                OperationContext oc = null;
                switch (func.Opcode)
                {
                    case (int)OpCodes.PeriodicUpdate:
                        return handlePresenceAnnouncement(src, func.Operand);

                    case (int)OpCodes.ReqStatus:
                        return this.handleReqStatus();

                    case (int)OpCodes.GetCount:
                        return handleCount();

                    case (int)OpCodes.Remove:
                        return handleRemove(src, func.Operand);

                    case (int)OpCodes.RemoveRange:
                        return handleRemoveRange(func.Operand);

                    case (int)OpCodes.Clear:
                        return handleClear(src, func.Operand);

                    case (int)OpCodes.KeyList:
                        return handleKeyList();

                    case (int)OpCodes.Get:
                        return handleGet(func.Operand);

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

                    case (int)OpCodes.RemoveGroup:
                        return handleRemoveGroup(func.Operand);

                    case (int)OpCodes.UpdateIndice:
                        return handleUpdateIndice(func.Operand);

                    case (int)OpCodes.ReplicateOperations:
                        return handleReplicateOperations(src, func.Operand, func.UserPayload);

                    case (int)OpCodes.LockKey:
                        return handleLock(func.Operand);

                    case (int)OpCodes.UnLockKey:
                        handleUnLock(func.Operand);
                        break;

                    case (int)OpCodes.IsLocked:
                        return handleIsLocked(func.Operand);

                    case (int)OpCodes.GetMessageCount:
                        return handleMessageCount(func.Operand);

                    case (int)OpCodes.TransferEntries:
                        return HandleTransferEntries(src, func.Operand);
						
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

        #endregion

        private object HandleTransferEntries(Address src, object info)
        {
            if (_corresponder == null)
                _corresponder = new ReplicaStateTxfrCorresponder(this, src);

            ReplicaStateTxfrInfo data = _corresponder.GetData();

            if (data.transferCompleted)
            {
                _corresponder.Dispose();
                _corresponder = null;
            }

            return data;
        }



        #region	/                 --- Statistics Replication ---           /

        /// <summary>
        /// Periodic update (PULL model), i.e. on demand fetch of information from every node.
        /// </summary>
        private bool DetermineClusterStatus()
        {
            try
            {
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("MirrorCache.DetermineClusterStatus", " determine cluster status");
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
          
            ///No .. this was previously maintained as it is i.e. using exception as flow control
            ///Null can be rooted down to MirrorManager.GetGroupInfo where null is intentionally thrown and here instead of providing a check ...
            ///This flow can be found in every DetermineCluster function of every topology
            catch (NullReferenceException) { }
            catch (Exception e)
            {
                Context.NCacheLog.Error("MirrorCache.DetermineClusterStatus()", e.ToString());
            }
            return false;
        }

        /// <summary>
        /// Handler for Periodic update (PULL model), i.e. on demand fetch of information 
        /// from every node.
        /// </summary>
        private object handleReqStatus()
        {
            if (_stats.LocalNode != null)
            {
                NodeInfo localStats = _stats.LocalNode;
                localStats.StatsReplicationCounter++;
                return localStats.Clone() as NodeInfo;
            }
            return null;
        }

        /// <summary>
        /// Periodic update (PUSH model), i.e., Publish cache statisitcs so that every node in 
        /// the cluster gets an idea of the state of every other node.
        /// </summary>
        public new bool AnnouncePresence(bool urgent)
        {
            try
            {
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("MirrorCache.AnnouncePresence()", " announcing presence ;urget " + urgent);

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
                Context.NCacheLog.Error("MirrorCache.AnnouncePresence()", e.ToString());
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
            NodeInfo info;
            lock (Servers.SyncRoot)
            {
                NodeInfo other = obj as NodeInfo;
                info = _stats.GetNode(sender as Address);
                if (other != null && info != null)
                {
                    if (!IsReplicationSequenced(info, other)) return null;
                    Context.NCacheLog.Debug("Mirror.handlePresenceAnnouncement()",
                        "sender = " + sender + " stats = " + other.Statistics);
                    info.Statistics = other.Statistics;
                    info.NodeGuid = other.NodeGuid;
                    info.StatsReplicationCounter = other.StatsReplicationCounter;
                   
                    info.ConnectedClients = other.ConnectedClients;
                    info.Status = other.Status;
                }
            }
            UpdateCacheStatistics();
            return null;
        }

        public override CacheStatistics CombineClusterStatistics(ClusterCacheStatistics s)
        {
            CacheStatistics c = ClusterHelper.CombineReplicatedStatistics(s);
            return c;
        }


        /// <summary>
        /// Update the list of the clients connected to this node
        /// and replicate it over the entire cluster.
        /// </summary>
        /// <param name="client"></param>
        public override void ClientConnected(string client, bool isInproc, ClientInfo clientInfo)
        {
            base.ClientConnected(client, isInproc, clientInfo);
            if (_context.ConnectedClients != null) UpdateClientStatus(client, true, clientInfo);
            AnnouncePresence(false);
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);
        }

        /// <summary>
        /// Update the list of the clients connected to this node
        /// and replicate it over the entire cluster.
        /// </summary>
        /// <param name="client"></param>
        public override void ClientDisconnected(string client, bool isInproc, ClientInfo clientInfo)
        {
            base.ClientDisconnected(client, isInproc, clientInfo);
            if (_context.ConnectedClients != null) UpdateClientStatus(client, false, null);
            AnnouncePresence(false);
        }

        public override ArrayList DetermineClientConnectivity(ArrayList clients)
        {
            if (clients == null) return null;
            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Client-Death-Detection.DetermineClientConnectivity()", "going to determine client connectivity in cluster");
            try
            {
                DetermineClusterStatus();//updating stats
                ArrayList result = new ArrayList();
                foreach (NodeInfo node in _stats.Nodes)
                {
                    if (node.Statistics != null && node.IsActive)
                    {
                        foreach (string client in clients)
                        {
                            if (!node.ConnectedClients.Contains(client))
                            {
                                result.Add(client);
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("Client-Death-Detection.DetermineClientConnectivity()", e.ToString());
            }
            finally
            {
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Client-Death-Detection.DetermineClientConnectivity()", "determining client connectivity in cluster completed");
            }
            return null;
        }

        public override CacheStatistics Statistics
        {
            get
            {
                CacheStatistics stats = _stats.Clone() as CacheStatistics;
                long maxSize = 0;
                foreach (NodeInfo nodeInfo in _stats.Nodes)
                {
                    if (nodeInfo.Statistics != null && nodeInfo.IsActive)
                    {
                        maxSize += nodeInfo.Statistics.MaxSize;
                    }
                }
                stats.MaxSize = maxSize;
                return stats;

            }
        }

        #endregion

        #region	/                 --- State Transfer ---           /

        #region	/                 --- StateTransferTask ---           /

        /// <summary>
        /// Asynchronous state tranfer job.
        /// </summary>
        protected class StateTransferTask : AsyncProcessor.IAsyncTask
        {
            /// <summary> The partition base class </summary>
            private MirrorCache _parent = null;

            bool _stateTransferEventLogged = false;

            /// <summary> A promise object to wait on. </summary>
            private Promise _promise = null;

            private NewTrace nTrace = null;

            string _cacheserver = "NCache";

            Hashtable itemsHavingKeyDependency = new Hashtable();

            ArrayList keysHavingdependency = null;

            string _collectionUnderStateTransfer = null;

            object collectionLock = new object();

            protected virtual string Name
            {
                get { return "MirrorStateTransferTask"; }
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="parent"></param>
            public StateTransferTask(MirrorCache parent)
            {
                _parent = parent;

                _promise = new Promise();
            }

            /// <summary>
            /// Wait until a result is available for the state transfer task.
            /// </summary>
            /// <param name="timeout"></param>
            /// <returns></returns>
            public object WaitUntilCompletion(long timeout)
            {
                return _promise.WaitResult(timeout);
            }

            /// <summary>
            /// Signal the end of state transfer.
            /// </summary>
            /// <param name="result">transfer result</param>
            /// <returns></returns>
            private void SignalEndOfTransfer(object result)
            {
                _parent.EndStateTransfer(_parent.Local_Count());
                _promise.SetResult(result);
            }


            /// <summary>
            /// Do the state transfer now.
            /// </summary>
            void AsyncProcessor.IAsyncTask.Process()
            {
                object result = null;
                bool logEvent = false;

                try
                {
                    _parent.DetermineClusterStatus();
                    _parent._statusLatch.SetStatusBit(NodeStatus.Initializing, NodeStatus.Running);
                    _parent.TransferTopicState();
                    
                    while (true)
                    {
                        ReplicaStateTxfrInfo data = _parent.Clustered_GetEntries(_parent.Cluster.Coordinator);

                        if (data != null)
                        {
                            if (data.transferCompleted)
                                break;

                            AddDataToCache(data);
                        }
                    }

                    result = _parent.Local_Count();
                    TransferMessages();
                }
                catch (Exception e)
                {
                    result = e;
                }
                finally
                {
                    
                    SignalEndOfTransfer(result);

                    if (logEvent)
                    {
                        _parent.Context.NCacheLog.CriticalInfo("MirrorCache.StateTransfer", "State transfer has ended");
                        if (result is System.Exception)
                        {
                            AppUtil.LogEvent(_cacheserver, "\"" + _parent._context.SerializationContext + "\"" + " has ended state transfer prematurely.", System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.StateTransferError);                          
                        }
                        else
                        {                            
                            AppUtil.LogEvent(_cacheserver, "\"" + _parent._context.SerializationContext + "\"" + " has completed state transfer.", System.Diagnostics.EventLogEntryType.Information, EventCategories.Information, EventID.StateTransferStop);
                        }
                    }
                }
            }

            private void AddDataToCache(ReplicaStateTxfrInfo info)
            {
                if (info != null)
                {
                    try
                    {
                        OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                        if (info.key != null && info.data != null)
                        {
                            switch (info.DataType)
                            {
                                case DataType.CacheItems:
                                    
                                    AddEntryToCache(info.key, info.data as CacheEntry, operationContext);

                                    break;                                
                            }
                        }
                        else
                        {
                            if (info.key != null)
                                _parent.InternalCache.Remove(info.key, ItemRemoveReason.Removed, false, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                        }
                    }
                    catch (StateTransferException se)
                    {
                        _parent.Context.NCacheLog.Error(Name + ".AddDataToCache", " Can not add/remove key = " + info.key + " : value is " + ((info.data == null) ? "null" : " not null") + " : " + se.Message);
                    }
                    catch (Exception e)
                    {
                        _parent.Context.NCacheLog.Error(Name + ".AddDataToCache", " Can not add/remove key = " + info.key + " : value is " + ((info.data == null) ? "null" : " not null") + " : " + e.Message);
                    }

                }
            }

            private void AddEntryToCache(string key, CacheEntry val, OperationContext operationContext)
            {
                try
                {
                    if (val != null)
                        val.MarkInUse(NCModulesConstants.Topology);
                    // if the object is already there, skip a network call
                    if (_parent.Local_Contains(key, operationContext /*new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation)*/))
                    {
                        return;
                    }

                    if (val != null)
                    {
                        if (val.KeysIAmDependingOn != null && val.KeysIAmDependingOn.Length > 0)
                        {
                            itemsHavingKeyDependency.Add(key, val);
                            return;
                        }
                        try
                        {
                            if (val != null && val.EvictionHint != null)
                            {
                                //We deliberately remove the eviction hint so that it is reset according to the this node.
                                if (val.EvictionHint is Alachisoft.NCache.Caching.EvictionPolicies.TimestampHint
                                    || val.EvictionHint is Alachisoft.NCache.Caching.EvictionPolicies.CounterHint)
                                {
                                    val.EvictionHint = null;
                                }
                            }

                            // doing an Add ensures that the object is not updated 
                            // if it had already been added while we were fetching it.
                            CacheAddResult opResult = _parent.Local_Add(key, val, null, null, true, operationContext);

                            _parent.Context.PerfStatsColl.IncrementStateTxfrPerSecStats();
                        }
                        catch (Exception e)
                        {
                            // object already there so skip it.
                            if (_parent.Context.NCacheLog.IsErrorEnabled)
                                _parent.Context.NCacheLog.Error("MirrorCache.StateTransferTask", e.ToString());
                        }
                    }

                    keysHavingdependency = new ArrayList(itemsHavingKeyDependency.Keys);
                    while (keysHavingdependency.Count != 0)
                    {
                        for (int i = 0; i < keysHavingdependency.Count; i++)
                        {
                            CacheEntry valueToAdd = itemsHavingKeyDependency[keysHavingdependency[i]] as CacheEntry;
                            if (valueToAdd != null)
                            {
                                object[] dependentKeys = valueToAdd.KeysIAmDependingOn;
                                int Count = 0;
                                for (int x = 0; x < dependentKeys.Length; x++)
                                {
                                    if (_parent.Local_Contains(dependentKeys[x].ToString(), new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation)))
                                        Count++;
                                }
                                if (dependentKeys.Length == Count)
                                {
                                    try
                                    {
                                        string keyToBeAdded = keysHavingdependency[i].ToString();
                                        keysHavingdependency.Remove(keyToBeAdded);

                                        if (valueToAdd != null && valueToAdd.EvictionHint != null)
                                        {
                                            //We deliberately remove the eviction hint so that it is reset according to the this node.
                                            if (valueToAdd.EvictionHint is Alachisoft.NCache.Caching.EvictionPolicies.TimestampHint
                                                || valueToAdd.EvictionHint is Alachisoft.NCache.Caching.EvictionPolicies.CounterHint)
                                            {
                                                valueToAdd.EvictionHint = null;
                                            }
                                        }
                                        CacheAddResult opResult = _parent.Local_Add(keyToBeAdded, valueToAdd, null, null, true, operationContext);

                                        _parent.Context.PerfStatsColl.IncrementStateTxfrPerSecStats();

                                    }
                                    catch (Exception e)
                                    {
                                        // object already there so skip it.
                                        if (_parent.Context.NCacheLog.IsInfoEnabled)
                                            _parent.Context.NCacheLog.Info("MirrorCache.StateTransferTask", e.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
                finally {
                    if (val != null)
                        val.MarkFree(NCModulesConstants.Topology);
                }
            }

            private void TransferMessages()
            {
                OrderedDictionary topicWiseMessagees = _parent.GetMessageList(0,true);

                if (_parent.NCacheLog.IsInfoEnabled)
                    _parent.NCacheLog.Info("StateTransferTask.TransferMessages", " message transfer started");

                if (topicWiseMessagees != null)
                {
                    foreach (DictionaryEntry topicWiseMessage in topicWiseMessagees)
                    {
                        ClusteredArrayList messageList = topicWiseMessage.Value as ClusteredArrayList;
                        if (_parent.NCacheLog.IsInfoEnabled)
                            _parent.NCacheLog.Info("StateTransferTask.TransferMessages", " topic : " + topicWiseMessage.Key + " messaeg count : " + messageList.Count);

                        foreach (string messageId in messageList)
                        {
                            try
                            {
                                TransferrableMessage message = _parent.GetTransferrableMessage(topicWiseMessage.Key as string, messageId);

                                if (message != null)
                                {
                                    _parent.InternalCache.StoreTransferrableMessage(topicWiseMessage.Key as string, message);
                                }
                            }
                            catch (Exception e)
                            {
                                _parent.NCacheLog.Error("StateTransferTask.TransferMessages", e.ToString());
                            }


                        }

                    }
                }

                if (_parent.NCacheLog.IsInfoEnabled)
                    _parent.NCacheLog.Info("StateTransferTask.TransferMessages", " message transfer ended");

            }

            private void LogStateTranferStartedEvent()
            {
                if (!_stateTransferEventLogged)
                {
                    //EmailNotifier
                    if (_parent.alertPropagator != null)
                    {
                        _parent.alertPropagator.RaiseAlert(EventID.StateTransferStart, _cacheserver, "\"" + _parent.Context.SerializationContext + "(" + _parent.Cluster.LocalAddress.ToString() + ")\"" + " has started state transfer.");
                    }
                    //
                    AppUtil.LogEvent(_cacheserver, "\"" + _parent.Context.SerializationContext + "(" + _parent.Cluster.LocalAddress.ToString() + ")\"" + " has started state transfer.", System.Diagnostics.EventLogEntryType.Information, EventCategories.Information, EventID.StateTransferStart);
                    //_parent.Context.NCacheLog.Error(Name + ".Process", " State transfer has started");
                    _stateTransferEventLogged = true;
                }
            }

            private void InsertEntry(object key, CacheEntry entry, OperationContext operationContext)
            {
                try {
                    if (entry != null)
                        entry.MarkInUse(NCModulesConstants.Topology);

                    if (entry != null && entry.EvictionHint != null)
                    {
                        //We deliberately remove the eviction hint so that it is reset according to the this node.
                        if (entry.EvictionHint is EvictionPolicies.TimestampHint
                            || entry.EvictionHint is EvictionPolicies.CounterHint)
                        {
                            entry.EvictionHint = null;
                        }
                    }

                    CacheInsResultWithEntry result = _parent.InternalCache.Insert(key, entry, false, false, null, entry.Version, LockAccessType.PRESERVE_VERSION, operationContext);

                    if (result != null && result.Result == CacheInsResult.NeedsEviction)
                    {
                        _parent.Context.NCacheLog.Info("Failed to insert key : " + key + "Result : " + result.Result.ToString());
                    }
                }
                finally
                {
                    if (entry != null)
                        entry.MarkFree(NCModulesConstants.Topology);
                }
            }
            }


        #endregion

        /// <summary>
        /// Fetch state from a cluster member. If the node is the coordinator there is
        /// no need to do the state transfer.
        /// </summary>
        protected void StartStateTransfer()
        {
            if (!Cluster.IsCoordinator)
            {
                /// Tell everyone that we are not fully-functional, i.e., initilizing.
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("MirrorCache.StartStateTransfer()", "Requesting state transfer " + LocalAddress);

                /// Start the initialization(state trasfer) task.
                stateTransferTask = new StateTransferTask(this);
                _context.AsyncProc.Enqueue(stateTransferTask);

                /// Un-comment the following line to do it synchronously.
                /// object v = stateTransferTask.WaitUntilCompletion(-1);
            }
            else
            {
                _statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);
                UpdateCacheStatistics();
                AnnouncePresence(true);
            }
        }

        /// <summary>
        /// Fetch state from a cluster member. If the node is the coordinator there is
        /// no need to do the state transfer.
        /// </summary>
        protected void EndStateTransfer(object result)
        {
            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("MirrorCache.EndStateTransfer()", "State Txfr ended: " + result);
            if (result is Exception)
            {
                /// What to do? if we failed the state transfer?. Proabably we'll keep
                /// servicing in degraded mode? For the time being we don't!
            }

            /// Set the status to fully-functional (Running) and tell everyone about it.
            _statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);
            UpdateCacheStatistics();
            AnnouncePresence(true);
        }

        #endregion

        #region	/                 --- ICache ---           /

        #region	/                 --- Mirror ICache.Count ---           /

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        public override long Count
        {
            get
            {
                /// Wait until the object enters any running status
                _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

                if (_internalCache == null) throw new InvalidOperationException();

                return Local_Count();
            }
        }


        /// <summary>
        /// Returns the count of local cache items only.
        /// </summary>
        /// <returns>count of items.</returns>
        private long Local_Count()
        {
            if (_internalCache != null)
                return _internalCache.Count;
            return 0;
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

        #region	/                 --- Mirror ICache.Contains ---           /

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        public override bool Contains(object key, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            bool retVal = Local_Contains(key, operationContext);

            return retVal;
        }


        /// <summary>
        /// Determines whether the cache contains the specified keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        public override Hashtable Contains(IList keys, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            Hashtable result = new Hashtable();
            Hashtable tbl = Local_Contains(keys, operationContext);
            ArrayList list = null;

            if (tbl != null && tbl.Count > 0)
            {
                list = (ArrayList)tbl["items-found"];
            }

            result["items-found"] = list;
            return result;
        }


        /// <summary>
        /// Determines whether the local cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        internal bool Local_Contains(object key, OperationContext operationContext)
        {
            bool retVal = false;
            if (_internalCache != null)
                retVal = _internalCache.Contains(key, operationContext);
            return retVal;
        }

        /// <summary>
        /// Determines whether the local cache contains the specified keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>List of keys available in cache</returns>
        private Hashtable Local_Contains(IList keys, OperationContext operationContext)
        {
            Hashtable tbl = new Hashtable();
            if (_internalCache != null)
            {
                tbl = _internalCache.Contains(keys, operationContext);
            }
            return tbl;
        }

        #endregion

 
        #region	/                 --- Mirror ICache.Clear ---           /

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        /// <remarks>
        /// This method invokes <see cref="handleClear"/> on every node in the cluster, 
        /// which then fires OnCacheCleared locally. The <see cref="handleClear"/> method on the 
        /// coordinator will also trigger a cluster-wide notification to the clients.
        /// </remarks>
        public override void Clear(Caching.Notifications notification, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            string taskId = null;
            if (updateOptions == DataSourceUpdateOptions.WriteBehind)
                taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

            Local_Clear(Cluster.LocalAddress, notification, taskId, operationContext);

        }

        /// <summary>
        /// Clears the local cache only. 
        /// </summary>
        private void Local_Clear(Address src, Caching.Notifications notification, string taskId, OperationContext operationContext)
        {
            CacheEntry entry = null;
            try
            {
                if (_internalCache != null)
                {
                    _internalCache.Clear(null, DataSourceUpdateOptions.None, taskId, operationContext);
                    if (taskId != null)
                    {
                        entry = CacheEntry.CreateCacheEntry(Context.FakeObjectPool, notification, null, null);                      
                    }
                    UpdateCacheStatistics();
                }
            }
            finally
            {
                if (entry != null)
                    entry.MarkFree(NCModulesConstants.Topology);
            }
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
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        #endregion

        internal override void EnqueueForReplication(object key, int opCode, object data, int size, Array userPayLoad, long payLoadSize)
        {
            OperationContext operationContext = null;
            if (data is object[])
            {
                object[] dataArray = (object[])data;

                operationContext = dataArray[dataArray.Length - 1] as OperationContext;

                if (operationContext != null)
                {
                    operationContext = (OperationContext)operationContext.Clone();

                    if (operationContext.Contains(OperationContextFieldName.RaiseCQNotification))
                    {
                        operationContext.RemoveValueByField(OperationContextFieldName.RaiseCQNotification);
                    }

                    dataArray[dataArray.Length - 1] = operationContext;
                }
            }

            EnqueOperationForReplication(key, opCode, data, size, userPayLoad, payLoadSize, operationContext);
        }

        internal override void EnqueueForReplication(object key, int opCode, object data)
        {
            OperationContext operationContext = null;
            if (data is object[])
            {
                object[] dataArray = (object[])data;

                operationContext = dataArray[dataArray.Length - 1] as OperationContext;

                if (operationContext != null)
                {
                    operationContext = (OperationContext)operationContext.Clone();

                    if (operationContext.Contains(OperationContextFieldName.RaiseCQNotification))
                    {
                        operationContext.RemoveValueByField(OperationContextFieldName.RaiseCQNotification);
                    }

                    dataArray[dataArray.Length - 1] = operationContext;
                    
                }                                 
            }
            if (opCode == (int)ClusterCacheBase.OpCodes.Clear)
                EnqueClearOperationForReplication(opCode, data, operationContext);
            else
                EnqueOperationForReplication(key, opCode, data, 20, null, 0, operationContext);

        }

        private void EnqueOperationForReplication(object key, int opCode, object data, int size, Array userPayLoad, long payLoadSize, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            if (Cluster.IsCoordinator && Cluster.Servers.Count > 1)
            {
                ReplicationOperation operation = GetReplicationOperation(opCode, data, size, userPayLoad, payLoadSize);
                _asyncReplicator.EnqueueOperation(key, operation);
            }
        }

        private void EnqueClearOperationForReplication(int opCode, object data, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            ReplicationOperation operation = GetClearReplicationOperation(opCode, data);
            _asyncReplicator.EnqueueClear(operation);
        }

        internal override bool RequiresReplication
        {
            get
            {
                return _asyncReplicator != null && Cluster.IsCoordinator && Cluster.Servers.Count > 1;
            }
        }

        #region	/                 --- Mirror ICache.Get ---           /

        public override CacheEntry GetGroup(object key, string group, string subGroup, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            /// If we are in state transfer, we check locally first and then 
            /// to make sure we do a clustered call and fetch from some other 
            /// functional node.
            CacheEntry e = Local_GetGroup(key, group, subGroup, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);

            if (e == null)
            {
                _stats.BumpMissCount();
            }
            else
            {
                _stats.BumpHitCount();
                // update the indexes on other nodes in the cluster
                if ((e.ExpirationHint != null && e.ExpirationHint.IsVariant) /*|| (e.EvictionHint !=null && e.EvictionHint.IsVariant)*/ )
                {
                    UpdateIndices(key, true, operationContext);
                }
            }
            return e;
        }

        /// <summary>
        /// Retrieve the object from the cache for the given group or sub group.
        /// </summary>
        private CacheEntry Local_GetGroup(object key, string group, string subGroup, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.GetGroup(key, group, subGroup, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);

            return null;
        }

        public override HashVector GetTagData(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            return _internalCache.GetTagData(tags, comparisonType, operationContext);
        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        public override CacheEntry Get(object key, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("MirrCache.Get", "");

            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            /// If we are in state transfer, we check locally first and then 
            /// to make sure we do a clustered call and fetch from some other 
            /// functional node.
            CacheEntry e = Local_Get(key, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);

            if (e == null)
            {
                _stats.BumpMissCount();
            }
            else
            {
                _stats.BumpHitCount();
                // update the indexes on other nodes in the cluster
                if ((e.ExpirationHint != null && e.ExpirationHint.IsVariant) /*|| (e.EvictionHint !=null && e.EvictionHint.IsVariant)*/ )
                {
                    UpdateIndices(key, true, operationContext);
                }
            }
            return e;
        }

        public override IDictionary GetEntryAttributeValues(object key, IList<string> columns, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();
            
            return _internalCache.GetEntryAttributeValues(key, columns, operationContext);
        }

        /// <summary>
        /// Retrieve the objects from the cache. An array of keys is passed as parameter.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        public override IDictionary Get(object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("MirrCache.GetBlk", "");

            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            /// If we are in state transfer, we check locally first and then 
            /// to make sure we do a clustered call and fetch from some other 
            /// functional node.
            HashVector table = (HashVector)Local_Get(keys, operationContext);

            ClusteredArrayList updateIndiceKeyList = null;
            IDictionaryEnumerator ine = table.GetEnumerator();
            while (ine.MoveNext())
            {
                if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                CacheEntry e = (CacheEntry)ine.Value;
                if (e == null)
                {
                    _stats.BumpMissCount();
                }
                else
                {
                    _stats.BumpHitCount();
                    // update the indexes on other nodes in the cluster
                    if ((e.ExpirationHint != null && e.ExpirationHint.IsVariant))
                    {
                        if (updateIndiceKeyList == null) updateIndiceKeyList = new ClusteredArrayList();
                        updateIndiceKeyList.Add(ine.Key);
                    }
                }
            }

            if (updateIndiceKeyList != null && updateIndiceKeyList.Count > 0)
            {
                UpdateIndices(updateIndiceKeyList.ToArray(), true, operationContext);
            }

            return table;
        }

        /// <summary>
        /// Retrieve the keys from the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>list of keys.</returns>
        public override ArrayList GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            ArrayList list = Local_GetKeys(group, subGroup, operationContext);
            return list;
        }

        public override void RegisterPollingNotification(short callbackId, OperationContext operationContext)
        {
            Local_RegisterPollingNotification(callbackId, operationContext);
        }


        public override PollingResult Poll(OperationContext operationContext)
        {
            PollingResult result = null;
            result = Local_Poll(operationContext);

            if (Cluster.Servers.Count > 1  )
            {
                DryPoll(operationContext);
            }
            return result;
        }

        /// <summary>
        /// Retrieve the keys from the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>key and value pairs.</returns>
        public override HashVector GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            return Local_GetData(group, subGroup, operationContext);
        }

        /// <summary>
        /// Retrieve the list of keys fron the cache for the given group or sub group.
        /// </summary>
        private ArrayList Local_GetKeys(string group, string subGroup, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.GetGroupKeys(group, subGroup, operationContext);

            return null;
        }

        private PollingResult Local_Poll(OperationContext context)
        {
            if (_internalCache != null)
                return _internalCache.Poll(context);
            return null;
        }

        private void Local_RegisterPollingNotification(short callbackId, OperationContext operationContext)
        {
            if (_internalCache != null)
                _internalCache.RegisterPollingNotification(callbackId, operationContext);
        }

        /// <summary>
        /// Retrieve the list of keys fron the cache for the given group or sub group.
        /// </summary>
        private HashVector Local_GetData(string group, string subGroup, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.GetGroupData(group, subGroup, operationContext);

            return null;
        }

        /// <summary>
        /// Perform a dummy get operation on cluster that triggers index updates on all
        /// the nodes in the cluster, has no other particular purpose.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        public override void UpdateIndices(object key, OperationContext operationContext)
        {
            try
            {
                if (Cluster.Servers != null && Cluster.Servers.Count > 1)
                {
                    Function func = new Function((int)OpCodes.UpdateIndice, new object[] { key, operationContext });
                    Cluster.Multicast(Cluster.OtherServers, func, GroupRequest.GET_ALL, false);
                }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("MirrorCache.UpdateIndices()", e.ToString());
            }
        }
        private void RemoveUpdateIndexOperation(object key, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            if (key != null && RequiresReplication)
            {
                try
                {
                    if (_asyncReplicator != null) _asyncReplicator.RemoveUpdateIndexKey(key);
                }
                catch (Exception) { }
            }
        }
        protected override void UpdateIndices(object key, bool async, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            if (RequiresReplication)
            {
                try
                {
                    if (_asyncReplicator != null) _asyncReplicator.AddUpdateIndexKey(key);
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Hanlde cluster-wide Get(key) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleGet(object info)
        {
            bool isUserOperation = true;
            try
            {
                object[] args = info as object[];
                OperationContext operationContext = null;

                if (args.Length == 2)
                    operationContext = args[1] as OperationContext;

                if (args.Length > 3)
                    operationContext = args[6] as OperationContext;

                if (args.Length > 2)
                {
                    operationContext = args[1] as OperationContext;
                    isUserOperation = (bool)args[2];
                }
                if (operationContext != null) operationContext.UseObjectPool = false;

                if (args[0] is object[])
                {
                    return Local_Get((object[])args[0], isUserOperation, operationContext);
                }
                else
                {
                    object lockId = null;
                    DateTime lockDate = DateTime.UtcNow;
                    LockAccessType accessType = LockAccessType.IGNORE_LOCK;
                    ulong version = 0;
                    CacheEntry entry = Local_Get(args[0], isUserOperation, ref version, ref lockId, ref lockDate, null, accessType, operationContext);
                    /* send value and entry seperaty*/
                    OperationResponse opRes = null;
                    if (entry != null)
                    {
                        opRes = new OperationResponse();
                        if (_context.InMemoryDataFormat.Equals(DataFormat.Binary))
                        {
                            UserBinaryObject ubObject = (UserBinaryObject)(entry.Value);
                            opRes.UserPayload = ubObject.Data;
                            opRes.SerializablePayload = entry.CloneWithoutValue();
                        }
                        else
                        {
                            opRes.UserPayload = null;
                            opRes.SerializablePayload = entry.Clone();
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

        #region	/                 --- Replicated ICache.GetEnumerator ---           /

        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        /// <returns>IDictionaryEnumerator enumerator.</returns>
        public override IDictionaryEnumerator GetEnumerator()
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            return new LazyKeysetEnumerator(this, (object[])handleKeyList(), false);
        }

        public override EnumerationDataChunk GetNextChunk(EnumerationPointer pointer, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null)
                throw new InvalidOperationException();


            if (pointer.ChunkId > 0 && !InternalCache.HasEnumerationPointer(pointer))
            {
                throw new OperationFailedException("Enumeration Has been Modified");
            }

            EnumerationDataChunk nextChunk = null;
            if (Cluster.Servers.Count > 1)
            {
                if (pointer.ChunkId < 0) //only perform a clustered operation for intialization and dispose call on passive node
                    Clustered_GetNextChunk(Cluster.Servers, pointer, operationContext);

                nextChunk = InternalCache.GetNextChunk(pointer, operationContext);
            }
            else
            {
                nextChunk = InternalCache.GetNextChunk(pointer, operationContext);
            }

            return nextChunk;
        }

        internal override void CacheBecomeActive()
        {
            
        }
        /// <summary>
        /// Hanlde cluster-wide KeyList requests.
        /// </summary>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleKeyList()
        {
            try
            {
                return MiscUtil.GetKeys(_internalCache, Convert.ToInt32(Cluster.Timeout));
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        #endregion

        private CacheEntry Local_Get(object key, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.Get(key, operationContext);

            return null;
        }

        private CacheEntry Local_Get(object key, bool isUserOperation, OperationContext operationContext)
        {
            Object lockId = null;
            DateTime lockDate = DateTime.UtcNow;
            ulong version = 0;

            CacheEntry retVal = null;
            if (_internalCache != null)
                retVal = _internalCache.Get(key, isUserOperation, ref version, ref lockId, ref lockDate, null, LockAccessType.IGNORE_LOCK, operationContext);

            return retVal;
        }

        private CacheEntry Local_Get(object key, bool isUserOperation, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.Get(key, isUserOperation, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);

            return null;
        }



        /// <summary>
        /// Retrieve the object from the local cache only. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheEntry Local_Get(object key, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.Get(key, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);

            return null;
        }

        /// <summary>
        /// Retrieve the objects from the local cache only. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        private IDictionary Local_Get(object[] keys, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.Get(keys, operationContext);

            return null;
        }

        #endregion

        #region /                   --- ReplicatedServer.GetGroupInfo ---                      /

        /// <summary>
        /// Gets the data group info the item.
        /// </summary>
        /// <param name="key">Key of the item</param>
        /// <returns>Data group info of the item</returns>
        public override DataGrouping.GroupInfo GetGroupInfo(object key, OperationContext operationContext)
        {
            _statusLatch.WaitForAny(NodeStatus.Running | NodeStatus.Running);
            return Local_GetGroupInfo(key, operationContext);
        }

        /// <summary>
        /// Gets data group info the items
        /// </summary>
        /// <param name="keys">Keys of the items</param>
        /// <returns>IDictionary of the data grup info the items</returns>
        public override Hashtable GetGroupInfoBulk(object[] keys, OperationContext operationContext)
        {
            _statusLatch.WaitForAny(NodeStatus.Running);
            return Local_GetGroupInfoBulk(keys, operationContext);
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

        #endregion

        #region	/                 --- Mirror ICache.Add ---           /

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAdd"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// Moreover the node initiating this request (this method) also triggers a cluster-wide item-add 
        /// notificaion.
        /// </remarks>
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
        {
            CacheEntry rollbackEntry = null;

            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);
                operationContext?.MarkInUse(NCModulesConstants.Topology);
                /// Wait until the object enters any running status
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("MirrCache.Add", "");

                _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

                if (_internalCache == null) throw new InvalidOperationException();

                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("MirrorCache.Add()", "Key = " + key);

                if (Local_Contains(key, operationContext)) return CacheAddResult.KeyExists;
                CacheAddResult result = CacheAddResult.Success;
                Exception thrown = null;

                string taskId = null;
                if (cacheEntry.Flag != null && cacheEntry.Flag.IsBitSet(BitSetConstants.WriteBehind))
                    taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

                try
                {
                    // Add locally
                    result = Local_Add(key, cacheEntry, Cluster.LocalAddress, taskId, true, operationContext);
                    if (result == CacheAddResult.KeyExists)
                    {
                        return result;
                    }
                }
                catch (Exception e)
                {
                    thrown = e;
                }

                if (result != CacheAddResult.Success || thrown != null)
                {
                    try
                    {
                        rollbackEntry = Local_Remove(key, ItemRemoveReason.Removed, null, null, null, null, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                    }
                    catch (Exception) { }

                    if (thrown != null) throw thrown;
                }
               
                return result;
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);
                operationContext?.MarkFree(NCModulesConstants.Topology);

                if (rollbackEntry != null)
                    MiscUtil.ReturnEntryToPool(rollbackEntry, Context.TransactionalPoolManager);
            }
        }

        /// <summary>
        /// Add ExpirationHint against the given key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public override bool Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("MirrorCache.Add()", "Key = " + key);

            if (Local_Contains(key, operationContext) == false) return false;
            bool result = false;
            try
            {
                result = Local_Add(key, eh, operationContext);
            }
            catch (Exception e)
            {
                throw;
            }

            return result;
        }

        /// <summary>
        /// Add ExpirationHint against the given key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public override bool Add(object key,  OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("MirrorCache.Add()", "Key = " + key);

            if (Local_Contains(key, operationContext) == false) return false;
            bool result = false;
            try
            {
                result = Local_Add(key, operationContext);
            }
            catch (Exception e)
            {
                throw;
            }
            return result;
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAdd"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// Moreover the node initiating this request (this method) also triggers a cluster-wide item-add 
        /// notificaion.
        /// </remarks>
        public override Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            CacheEntry[] entries = null;
            Hashtable failedOperations = null;
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.Topology);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("MirrCache.AddBlk", "");

                /// Wait until the object enters any running status
                _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

                if (_internalCache == null) throw new InvalidOperationException();

                object[] failedKeys = null;
                Hashtable addResult = new Hashtable();
                Hashtable tmp = new Hashtable();
                Dictionary<object, CacheEntry> fullEntrySet = new Dictionary<object, CacheEntry>();

                // keys and entries to replicate to passive node. we populate this table with all the keys and then remove only fialed one.
                // so in the end only successful keys on local node are queued for replication. 


                Hashtable existingKeys = Local_Contains(keys, operationContext);
                ArrayList list = new ArrayList();
                if (existingKeys != null && existingKeys.Count > 0)
                    list = existingKeys["items-found"] as ArrayList;

                int failCount = list.Count;
                if (failCount > 0)
                {
                    IEnumerator ie = list.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        addResult[ie.Current] = new OperationFailedException(ErrorCodes.BasicCacheOperations.KEY_ALREADY_EXISTS, ErrorMessages.GetErrorMessage(ErrorCodes.BasicCacheOperations.KEY_ALREADY_EXISTS));
                    }




                    // all keys failed, so return.
                    if (failCount == keys.Length)
                    {
                        return addResult;
                    }

                    object[] newKeys = new object[keys.Length - failCount];
                    entries = new CacheEntry[keys.Length - failCount];

                    int i = 0;
                    int j = 0;

                    IEnumerator im = keys.GetEnumerator();
                    while (im.MoveNext())
                    {
                        object key = im.Current;
                        if (!list.Contains(key))
                        {
                            newKeys[j] = key;
                            entries[j] = cacheEntries[i];
                            j++;
                        }
                        i++;
                    }
                    keys = newKeys;
                    cacheEntries = entries;
                }

                string taskId = null;
                if (cacheEntries[0].Flag != null && cacheEntries[0].Flag.IsBitSet(BitSetConstants.WriteBehind))
                    taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

                Exception thrown = null;
                try
                {
                    tmp = Local_Add(keys, cacheEntries, Cluster.LocalAddress, taskId, true, operationContext);
                }
                catch (Exception inner)
                {
                    Context.NCacheLog.Error("Mirror.Clustered_Add()", inner.ToString());
                    for (int i = 0; i < keys.Length; i++)
                    {
                        tmp[keys[i]] = new OperationFailedException(inner.Message, inner);
                    }
                    thrown = inner;
                }

                if (thrown != null)
                {
                    failedOperations = Local_Remove(keys, ItemRemoveReason.Removed, null, null, null, false, operationContext);
                }
                else
                {
                    failCount = 0;
                    ArrayList failKeys = new ArrayList();
                    IDictionaryEnumerator ide = tmp.GetEnumerator();
                    Hashtable writeBehindTable = new Hashtable();

                    while (ide.MoveNext())
                    {
                        if (ide.Value is CacheAddResult)
                        {
                            CacheAddResult res = (CacheAddResult)ide.Value;
                            switch (res)
                            {
                                case CacheAddResult.Failure:
                                case CacheAddResult.KeyExists:
                                case CacheAddResult.NeedsEviction:
                                    failCount++;
                                    failKeys.Add(ide.Key);
                                    addResult[ide.Key] = ide.Value;
                                    break;

                                case CacheAddResult.Success:
                                    addResult[ide.Key] = ide.Value;
                                    writeBehindTable.Add(ide.Key, null);
                                    break;
                            }
                        }
                        else //it means value is exception
                        {
                            failCount++;
                            failKeys.Add(ide.Key);
                            addResult[ide.Key] = ide.Value;
                        }
                    }

                    if (failCount > 0)
                    {
                        object[] keysToRemove = new object[failCount];
                        failKeys.CopyTo(keysToRemove, 0);

                        failedOperations = Local_Remove(keysToRemove, ItemRemoveReason.Removed, null, null, null, false, operationContext);
                    }


                }

                return addResult;
            }
            finally
            {
                if (entries != null)
                    entries.MarkFree(NCModulesConstants.Topology);

                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);
                if (failedOperations != null && failedOperations.Values != null)
                {
                    foreach (object e in failedOperations.Values)
                    {
                        CacheEntry entry = e as CacheEntry;
                        entry?.MarkFree(NCModulesConstants.Global);

                        if (entry != null)
                            MiscUtil.ReturnEntryToPool(entry, Context.TransactionalPoolManager);
                    }
                }
            }
        }

        /// <summary>
        /// Add the object to the local cache. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// Does an Add locally, however the generated notification is discarded, since it is specially handled
        /// in <see cref="Add"/>.
        /// </remarks>
        internal CacheAddResult Local_Add(object key, CacheEntry cacheEntry, Address src, string taskId, bool notify, OperationContext operationContext)
        {
            CacheEntry clone = null;
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("MirrCache.LocalAdd", "");

                CacheAddResult retVal = CacheAddResult.Failure;

                if (taskId != null)
                {
                    clone = cacheEntry.DeepClone(Context.TransactionalPoolManager);
                    clone.MarkInUse(NCModulesConstants.Topology);
                }
                else
                    clone = cacheEntry;

                if (_internalCache != null)
                {
                    object[] keys = null;
                    try
                    {
                        #region -- PART I -- Cascading Dependency Operation
                        keys = cacheEntry.KeysIAmDependingOn;
                        if (keys != null)
                        {
                            Hashtable goodKeysTable = _internalCache.Contains(keys, operationContext);

                            if (!goodKeysTable.ContainsKey("items-found"))
                                throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND,ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                            if (goodKeysTable["items-found"] == null)
                                throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                            if (goodKeysTable["items-found"] == null || (((ArrayList)goodKeysTable["items-found"]).Count != keys.Length))
                                throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                        }
                        #endregion

                        retVal = _internalCache.Add(key, cacheEntry, notify, taskId, operationContext);
                    }
                    catch (Exception e)
                    {
                        throw;
                    }

                    #region -- PART II -- Cascading Dependency Operation
                    if (retVal == CacheAddResult.Success && keys != null)
                    {
                        Hashtable table = new Hashtable();
                        Hashtable keyDepInfoTable = GetKeyDependencyInfoTable(key, cacheEntry);
                        try
                        {
                            //Fix for NCache-SP3 Bug4981
                            object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);
                            if (generateQueryInfo == null)
                            {
                                operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                            }

                            table = _internalCache.AddDepKeyList(keyDepInfoTable, operationContext);

                            if (generateQueryInfo == null)
                            {
                                operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                            }

                        }
                        catch (Exception e)
                        {
                            throw e;
                        }

                        if (table != null)
                        {
                            IDictionaryEnumerator en = table.GetEnumerator();
                            while (en.MoveNext())
                            {
                                if (en.Value is bool && !((bool)en.Value))
                                {
                                    throw new OperationFailedException("One of the dependency keys does not exist.");
                                }
                            }
                        }
                    }
                    #endregion
                }

                if (!ReferenceEquals(cacheEntry, clone))
                    MiscUtil.ReturnEntryToPool(clone, Context.TransactionalPoolManager);
                
                return retVal;
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.Topology);
            }
        }

        /// <summary>
        /// Add the ExpirationHint against the given key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hint"></param>
        /// <returns></returns>
        private bool Local_Add(object key, ExpirationHint hint, OperationContext operationContext)
        {
            CacheEntry cacheEntry = null;
            try
            {
                bool retVal = false;
                if (_internalCache != null)
                {
                    #region -- PART I -- Cascading Dependency Operation
                    cacheEntry = CacheEntry.CreateCacheEntry(Context.TransactionalPoolManager);


                    cacheEntry.ExpirationHint = hint;
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);
                    object[] keys = cacheEntry.KeysIAmDependingOn;

                    if (keys != null)
                    {
                        Hashtable goodKeysTable = Contains(keys, operationContext);


                    if (!goodKeysTable.ContainsKey("items-found"))
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                    if (goodKeysTable["items-found"] == null)
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                    if (((ArrayList)goodKeysTable["items-found"]).Count != keys.Length)
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                    }
                    #endregion

                    try
                    {
                        retVal = _internalCache.Add(key, hint, operationContext);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }

                    #region -- PART II -- Cascading Dependency Operation
                    if (retVal && keys != null)
                    {
                        Hashtable table = new Hashtable();
                        Hashtable keyDepInfoTable = GetKeyDependencyInfoTable(key, cacheEntry);
                        try
                        {
                            table = _internalCache.AddDepKeyList(keyDepInfoTable, operationContext);
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }

                        if (table != null)
                        {
                            IDictionaryEnumerator en = table.GetEnumerator();
                            while (en.MoveNext())
                            {

                                if (en.Value is bool && !((bool)en.Value))
                                {
                                    throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));
                                }


                            }
                        }
                    }
                    #endregion

                }
                return retVal;
            }

            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);

                MiscUtil.ReturnEntryToPool(cacheEntry, Context.TransactionalPoolManager);
            }
        }

        /// <summary>
        /// Add the ExpirationHint against the given key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hint"></param>
        /// <returns></returns>
        private bool Local_Add(object key,  OperationContext operationContext)
        {
            bool retVal = false;
            if (_internalCache != null)
            {
                retVal = _internalCache.Add(key, operationContext);
            }
            return retVal;
        }

        /// <summary>
        /// Add the object to the local cache. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// Does an Add locally, however the generated notification is discarded, since it is specially handled
        /// in <see cref="Add"/>.
        /// </remarks>
        private Hashtable Local_Add(object[] keys, CacheEntry[] cacheEntries, Address src, string taskId, bool notify, OperationContext operationContext)
        {
            CacheEntry[] clone = null;
            CacheEntry[] goodEntries = null;

            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.Topology);

                Hashtable table = new Hashtable();

                ArrayList goodKeysList = new ArrayList();
                ArrayList goodEntriesList = new ArrayList();

                ArrayList badKeysList = new ArrayList();

                
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
                    #region -- PART I -- Cascading Dependency Operation
                    for (int i = 0; i < cacheEntries.Length; i++)
                    {
                        object[] tempKeys = cacheEntries[i].KeysIAmDependingOn;
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
                                badKeysList.Add(keys[i]);
                            }
                        }
                        else
                        {
                            goodKeysList.Add(keys[i]);
                            goodEntriesList.Add(cacheEntries[i]);
                        }
                    }
                    #endregion

                    goodEntries = new CacheEntry[goodEntriesList.Count];
                    goodEntriesList.CopyTo(goodEntries);

                    table = _internalCache.Add(goodKeysList.ToArray(), goodEntries, notify, taskId, operationContext);

                    #region --Part II-- Cascading Dependency Operations
                    object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);
                    if (generateQueryInfo == null)
                    {
                        operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                    }

                    for (int i = 0; i < goodKeysList.Count; i++)
                    {
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                        CacheAddResult retVal = (CacheAddResult)table[goodKeysList[i]];
                        KeyDependencyInfo[] keyDepInfos = goodEntries[i].KeysIAmDependingOnWithDependencyInfo;

                        if (retVal == CacheAddResult.Success && keyDepInfos != null)
                        {
                            Hashtable keyDepInfoTable = GetKeyDependencyInfoTable(goodKeysList[i], keyDepInfos);
                            Hashtable tempTable = null;
                            try
                            {
                                tempTable = _internalCache.AddDepKeyList(keyDepInfoTable, operationContext);
                            }
                            catch (Exception e)
                            {
                                throw e;
                            }

                            if (tempTable != null)
                            {
                                IDictionaryEnumerator en = tempTable.GetEnumerator();
                                while (en.MoveNext())
                                {
                                    if (en.Value is bool && !((bool)en.Value))
                                    {
                                        table[goodKeysList[i]] = new OperationFailedException("Error setting up key dependency.");
                                    }
                                }
                            }
                        }
                    }

                    if (generateQueryInfo == null)
                    {
                        operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                    }


                    for (int i = 0; i < badKeysList.Count; i++)
                    {
                        table.Add(badKeysList[i], new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND)));
                    }
                    #endregion


                }
             
                // 'table' is probably null so keys failed to add
                // Return all clones to pool thus
                if (clone != null)
                    MiscUtil.ReturnEntriesToPool(clone, Context.TransactionalPoolManager);
                

                return table;
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.Topology);
                if (goodEntries != null)
                    goodEntries.MarkFree(NCModulesConstants.Topology);

            }
        }

        private object handleUpdateIndice(object info)
        {

            object[] objs = (object[])info;
            object[] keys = null;
            object key = null;
            OperationContext operationContext = null;

            if (objs.Length > 1)
                operationContext = objs[1] as OperationContext;

             if (operationContext == null) operationContext = OperationContext.Create(Context.FakeObjectPool);
            if (operationContext != null) operationContext.UseObjectPool = false;

            if (objs[0] is object[])
            {
                keys = (object[])objs[0];
                handleGet(new object[] { keys, operationContext });
            }
            else
            {
                key = objs[0];
                handleGet(new object[] { key, operationContext });
            }


            //we do a get operation on the item so that its relevent index in epxiration/eviction
            //is updated.
            handleGet(new object[] { key, operationContext });
            return null;
        }

        /// <summary>
        /// Hanlde cluster-wide Add(key) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleAdd(Address src, object info, Array userPayload)
        {
            try
            {
                object[] objs = (object[])info;

                string taskId = null;
                if (objs.Length > 2)
                    taskId = objs[2] != null ? objs[2] as string : null;

                OperationContext operationContext = null;
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
                    {
                        if (e.Value == null)
                            e.Value = userPayload;
                    }

                    return Local_Add(key, e, src, taskId, false, operationContext);
                }
            }
            catch (Exception e)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        /// <summary>
        /// Hanlde cluster-wide AddHint(key) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleAddHint(Address src, object info)
        {
            try
            {
                object[] objs = (object[])info;
                object key = objs[0];
                ExpirationHint eh = objs[1] as ExpirationHint;
                OperationContext operationContext = null;
                if (objs.Length > 2)
                    operationContext = objs[2] as OperationContext;
                Local_Add(key, eh, operationContext);
            }
            catch (Exception e)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        #endregion

        #region	/                 --- Mirror ICache.Insert ---           /

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// Moreover the node initiating this request (this method) also triggers a cluster-wide 
        /// item-update notificaion.
        /// </remarks>
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry entry = null;
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);
                operationContext?.MarkInUse(NCModulesConstants.Topology);


                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("MirrCache.Insert", "");

                /// Wait until the object enters any running status
                _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

                if (_internalCache == null) throw new InvalidOperationException();


                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Mirror.Insert()", "Key = " + key);

                CacheInsResultWithEntry retVal = null;
                Exception thrown = null;

                string taskId = null;
                if (cacheEntry.Flag != null && cacheEntry.Flag.IsBitSet(BitSetConstants.WriteBehind))
                    taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

                try
                {
                
                    retVal = Local_Insert(key, cacheEntry, Cluster.LocalAddress, taskId, true, lockId, version, accessType, operationContext);
                }
                catch (Exception e)
                {
                    thrown = e;
                }
                if (retVal == null) retVal = CacheInsResultWithEntry.CreateCacheInsResultWithEntry(_context.TransactionalPoolManager);
                // Try to insert to the local node and the cluster.
                if ((retVal.Result == CacheInsResult.NeedsEviction || retVal.Result == CacheInsResult.Failure) || thrown != null)
                {
                    Context.NCacheLog.Warn("Mirror.Insert()", "rolling back, since result was " + retVal.Result);
                    /// failed on the cluster, so remove locally as well.
                    entry=  Local_Remove(key, ItemRemoveReason.Removed, null, null, null, null, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                    if (thrown != null) throw thrown;
                }

                return retVal;
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.Topology);
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);
                entry?.MarkFree(NCModulesConstants.Global);

                if (entry != null)
                    MiscUtil.ReturnEntryToPool(entry, Context.TransactionalPoolManager);
            }
        }

        /// <summary>
        /// Adds key and value pairs to the cache. If any of the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">the cache entries.</param>
        /// <returns>the list of keys that failed to add.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// Moreover the node initiating this request (this method) also triggers a cluster-wide 
        /// item-update notificaion.
        /// </remarks>
        public override Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            CacheEntry[] validEnteries =null;
             
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.Topology);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("MirrCache.InsertBlk", "");

                _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

                if (_internalCache == null) throw new InvalidOperationException();

                Hashtable insertResults = null;

                string taskId = null;
                if (cacheEntries[0].Flag != null && cacheEntries[0].Flag.IsBitSet(BitSetConstants.WriteBehind))
                    taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

                HashVector pEntries = null;

                pEntries = (HashVector)Get(keys, operationContext); //dont remove

                Hashtable existingItems;
                Hashtable jointTable = new Hashtable();
                Hashtable failedTable = new Hashtable();
                Hashtable insertable = new Hashtable();
                ArrayList inserted = new ArrayList();
                ArrayList added = new ArrayList();
                Dictionary<object, CacheEntry> fullEntrySet = new Dictionary<object, CacheEntry>();

                ClusteredOperationResult opResult;
                object[] validKeys;
                object[] failedKeys;
                int index = 0;
                object key;
                Address node = null;

                for (int i = 0; i < keys.Length; i++)
                {
                    jointTable.Add(keys[i], cacheEntries[i]);
                }

                existingItems = Local_GetGroupInfoBulk(keys, operationContext);
                if (existingItems != null && existingItems.Count > 0)
                {
                    insertable = CacheHelper.GetInsertableItems(existingItems, jointTable);
                    IDictionaryEnumerator ide;
                    if (insertable != null)
                    {
                        index = 0;
                        validKeys = new object[insertable.Count];
                        validEnteries = new CacheEntry[insertable.Count];
                        ide = insertable.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            key = ide.Key;
                            validKeys[index] = key;
                            validEnteries[index] = (CacheEntry)ide.Value;
                            jointTable.Remove(key);
                            inserted.Add(key);
                            index += 1;

                            if (!fullEntrySet.ContainsKey((string)ide.Key))
                                fullEntrySet.Add((string)ide.Key, (CacheEntry)ide.Value);
                        }


                        if (validKeys.Length > 0)
                        {
                            try
                            {
                                insertResults = Local_Insert(validKeys, validEnteries, Cluster.LocalAddress, taskId, true, operationContext);
                            }
                            catch (Exception e)
                            {
                                Context.NCacheLog.Error("PartitionedServerCache.Insert(Keys)", e.ToString());
                                for (int i = 0; i < validKeys.Length; i++)
                                {
                                    failedTable.Add(validKeys[i], e);
                                    inserted.Remove(validKeys[i]);
                                }

                                Hashtable removedEntries = null;

                                try
                                {
                                    Local_Remove(validKeys, ItemRemoveReason.Removed, null, null, null, false, operationContext);
                                }
                                finally
                                {
                                    if (removedEntries?.Values?.Count > 0)
                                    {
                                        foreach (var value in removedEntries.Values)
                                        {
                                            if (value is CacheEntry removedCacheEntry)
                                            {
                                                MiscUtil.ReturnEntryToPool(removedCacheEntry, Context.TransactionalPoolManager);
                                            }
                                        }
                                    }
                                }
                            }
                            #region modifiedcode
                            if (insertResults != null)
                            {
                                IDictionaryEnumerator ie = insertResults.GetEnumerator();
                                while (ie.MoveNext())
                                {
                                    if (ie.Value is CacheInsResultWithEntry)
                                    {

                                        CacheInsResultWithEntry res = ie.Value as CacheInsResultWithEntry;
                                        switch (res.Result)
                                        {
                                            case CacheInsResult.Failure:
                                                failedTable[ie.Key] = new OperationFailedException("Generic operation failure; not enough information is available.");
                                                break;
                                            case CacheInsResult.NeedsEviction:
                                                failedTable[ie.Key] = new OperationFailedException(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED, ErrorMessages.GetErrorMessage(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED));
                                                break;
                                            case CacheInsResult.IncompatibleGroup:
                                                failedTable[ie.Key] = new OperationFailedException("Data group of the inserted item does not match the existing item's data group");
                                                break;
                                            case CacheInsResult.DependencyKeyNotExist:
                                                failedTable[ie.Key] = new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));
                                                break;
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                    ide = existingItems.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        key = ide.Key;
                        if (jointTable.Contains(key))
                        {
                            failedTable.Add(key, new OperationFailedException("Data group of the inserted item does not match the existing item's data group"));
                            jointTable.Remove(key);
                        }
                    }
                }

                Hashtable localInsertResult = null;
                if (jointTable.Count > 0)
                {
                    index = 0;
                    validKeys = new object[jointTable.Count];
                    validEnteries = new CacheEntry[jointTable.Count];
                    added = new ArrayList();
                    IDictionaryEnumerator ide = jointTable.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        key = ide.Key;
                        validKeys[index] = key;
                        validEnteries[index] = (CacheEntry)ide.Value;
                        added.Add(key);
                        if (!fullEntrySet.ContainsKey((string)validKeys[index]))
                            fullEntrySet.Add((string)validKeys[index], (CacheEntry)validEnteries[index]);
                        index += 1;
                    }
                    localInsertResult = Local_Insert(validKeys, validEnteries, Cluster.LocalAddress, taskId, notify, operationContext);
                }

                if (localInsertResult != null)
                {
                    IDictionaryEnumerator ide = localInsertResult.GetEnumerator();
                    CacheInsResultWithEntry result = null;// CacheInsResult.Failure;
                    while (ide.MoveNext())
                    {
                        if (ide.Value is Exception)
                        {

                            failedTable.Add(ide.Key, new OperationFailedException(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED, ErrorMessages.GetErrorMessage(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED)));
                            added.Remove(ide.Key);
                        }
                        else if (ide.Value is CacheInsResultWithEntry)
                        {

                            result = (CacheInsResultWithEntry)ide.Value;

                            if (result.Result == CacheInsResult.NeedsEviction)
                            {
                                failedTable.Add(ide.Key, new OperationFailedException("The cache is full and not enough items could be evicted."));
                                added.Remove(ide.Key);
                            }
                            else if (result.Result == CacheInsResult.DependencyKeyNotExist)
                            {
                                failedTable.Add(ide.Key, new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND)));
                                added.Remove(ide.Key);
                            }
                        }
                    }

                    insertResults = localInsertResult;
                }


                //This function is expected to only return Failed Keys therfore removing Dependencies within the insetBulk call
                _context.CacheImpl.RemoveCascadingDependencies(insertResults, operationContext, true);

                if (taskId != null && insertResults != null)
                {
                    Hashtable writeBehindTable = new Hashtable();
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (!insertResults.ContainsKey(keys[i]))
                        {
                            writeBehindTable.Add(keys[i], cacheEntries[i]);
                        }
                    }
                }

                // Bugfix 12070 - Insertbulk with groups and subgroups on existing key
                // erging result with failed result

                if (failedTable != null && failedTable.Count > 0)
                {
                    //If insertRsult is null return failed table else merge results
                    if (insertResults == null)
                        return failedTable;
                    foreach (var failedKey in failedTable.Keys)
                    {
                        // Bugfix 12532 - MIRROR TOPOLOGY: InsertBulk few objects with Key based dependency on another non-existing objects in the cache it gives already exist exception.
                        // The behaviour here is hybrid and unclear the stack trace above expects failed+successfull keys where as sometimes the same key is peresent in both 
                        // dictionaries with same/different exception. thus throwing one of those exceptions instead of a proper fix as the code is in freeze state NCache 4.8 Release.
                        if (!insertResults.ContainsKey(failedKey))
                            insertResults.Add(failedKey, failedTable[failedKey]);
                    }
                }
                return insertResults;
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);

                if (validEnteries != null)
                    validEnteries.MarkFree(NCModulesConstants.Topology);

            }
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// Does an Insert locally, however the generated notification is discarded, since it is 
        /// specially handled in <see cref="Insert"/>.
        /// </remarks>
        private CacheInsResultWithEntry Local_Insert(object key, CacheEntry cacheEntry, Address src, string taskId, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry clone = null;
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);

                CacheInsResultWithEntry retVal = null;

                if (taskId != null)
                {
                    clone = cacheEntry.DeepClone(Context.TransactionalPoolManager);
                    clone.MarkInUse(NCModulesConstants.Topology);
                }
                else
                    clone = cacheEntry;

                try
                {
                    if (_internalCache != null)
                    {


                        #region -- PART I -- Cascading Dependency Operation
                        object[] dependingKeys = cacheEntry.KeysIAmDependingOn;
                        if (dependingKeys != null)
                        {
                            Hashtable goodKeysTable = Contains(dependingKeys, operationContext);


                        if (!goodKeysTable.ContainsKey("items-found"))
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                        if (goodKeysTable["items-found"] == null)
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                        if (dependingKeys.Length != ((ArrayList)goodKeysTable["items-found"]).Count)
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                        }
                        #endregion
                      
                        retVal = _internalCache.Insert(key, cacheEntry, notify, taskId, lockId, version, accessType, operationContext);

                        #region -- PART II -- Cascading Dependency Operation

                        object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);
                        if (generateQueryInfo == null)
                        {
                            operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                        }

                        if (retVal.Result == CacheInsResult.Success || retVal.Result == CacheInsResult.SuccessOverwrite)
                        {
                            RemoveUpdateIndexOperation(key, operationContext);
                            Hashtable table = null;
                            Hashtable keyDepInfoTable = null;
                            if (retVal.Entry != null && retVal.Entry.KeysIAmDependingOn != null)
                            {
                                keyDepInfoTable = GetFinalKeysListWithDependencyInfo(retVal.Entry, cacheEntry);

                             
                                Hashtable oldKeysTable = GetKeysTable(key, (KeyDependencyInfo[])keyDepInfoTable["oldKeys"]);
                                _internalCache.RemoveDepKeyList(oldKeysTable, operationContext);
                                Hashtable keyDepInfos = GetKeyDependencyInfoTable(key, (KeyDependencyInfo[])keyDepInfoTable["newKeys"]);
                                table = _internalCache.AddDepKeyList(keyDepInfos, operationContext);
                            }
                            else if (cacheEntry.KeysIAmDependingOn != null)
                            {
                                Hashtable newKeyDepInfoTable = GetKeyDependencyInfoTable(key, cacheEntry);
                                table = _internalCache.AddDepKeyList(newKeyDepInfoTable, operationContext);
                            }

                            if (table != null)
                            {
                                IDictionaryEnumerator en = table.GetEnumerator();
                                while (en.MoveNext())
                                {
                                    if (en.Value is bool && !((bool)en.Value))
                                    {
                                        throw new OperationFailedException("Error setting up key depenedncy.");
                                    }
                                }
                            }
                        }
                        if (generateQueryInfo == null)
                        {
                            operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                        }

                        #endregion
                    }
                }
                catch (Exception e)
                {
                    if (_clusteredExceptions) throw;
                }
                if (retVal == null) retVal = CacheInsResultWithEntry.CreateCacheInsResultWithEntry(_context.TransactionalPoolManager);
               
                // Insert operation failed so Write-Behind is skipped. Return the clone to pool thus.
                if (!ReferenceEquals(cacheEntry, clone))
                    MiscUtil.ReturnEntryToPool(clone, Context.TransactionalPoolManager);
                

                return retVal;
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);
                if (clone != null)
                    clone.MarkFree(NCModulesConstants.Topology);
            }
        }

        /// <summary>
        /// Adds key and value pairs to the cache. If any of the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        /// <remarks>
        /// Does an Insert locally, however the generated notification is discarded, since it is 
        /// specially handled in <see cref="Insert"/>.
        /// </remarks>
        private Hashtable Local_Insert(object[] keys, CacheEntry[] cacheEntries, Address src, string taskId, bool notify, OperationContext operationContext)
        {
            CacheEntry[] clone = null;
            CacheEntry[] goodEntries = null;
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.Topology);

                Hashtable retVal = null;
                Exception thrown = null;

                Hashtable badEntriesTable = new Hashtable();
                ArrayList goodKeysList = new ArrayList();
                ArrayList goodEntriesList = new ArrayList();
                ArrayList badKeysList = new ArrayList();

           
                if (taskId != null)
                {
                    clone = new CacheEntry[cacheEntries.Length];
                    for (int i = 0; i < cacheEntries.Length; i++)
                    {
                        clone[i] = cacheEntries[i].DeepClone(Context.TransactionalPoolManager);
                        clone[i].MarkInUse(NCModulesConstants.Topology);
                    }
                }

                try
                {
                    if (_internalCache != null)
                    {
                        #region -- PART I -- Cascading Dependency Operation
                        for (int i = 0; i < cacheEntries.Length; i++)
                        {
                            object[] tempKeys = cacheEntries[i].KeysIAmDependingOn;
                            if (tempKeys != null)
                            {
                                Hashtable goodKeysTable = Contains(tempKeys, operationContext);

                                if (goodKeysTable.ContainsKey("items-found") && goodKeysTable["items-found"] != null && tempKeys.Length == ((ArrayList)goodKeysTable["items-found"]).Count)
                                {
                                    RemoveUpdateIndexOperation(keys[i], operationContext);
                                    goodKeysList.Add(keys[i]);
                                    goodEntriesList.Add(cacheEntries[i]);
                                }
                                else
                                {
                                    badKeysList.Add(keys[i]);
                                }
                            }
                            else
                            {
                                goodKeysList.Add(keys[i]);
                                goodEntriesList.Add(cacheEntries[i]);
                            }
                        }
                        #endregion


                        goodEntries = new CacheEntry[goodEntriesList.Count];
                        goodEntriesList.CopyTo(goodEntries);

                        retVal = _internalCache.Insert(goodKeysList.ToArray(), goodEntries, notify, taskId, operationContext);

                        #region -- PART II -- Cascading Dependency Operation

                        //Fix for NCache Bug4981
                        object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);
                        if (generateQueryInfo == null)
                        {
                            operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                        }

                        for (int i = 0; i < goodKeysList.Count; i++)
                        {
                            if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                                throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                            CacheInsResultWithEntry result = retVal[goodKeysList[i]] as CacheInsResultWithEntry;

                            if (result != null)
                            {
                                if (result.Result == CacheInsResult.Success || result.Result == CacheInsResult.SuccessOverwrite)
                                {
                                    Hashtable table = null;
                                    Hashtable keyDepInfoTable = null;
                                    if (result.Entry != null && result.Entry.KeysIAmDependingOn != null)
                                    {
                                        keyDepInfoTable = GetFinalKeysListWithDependencyInfo(result.Entry, goodEntries[i]);

                                        Hashtable oldKeysTable = GetKeysTable(goodKeysList[i], (KeyDependencyInfo[])keyDepInfoTable["oldKeys"]);
                                        _internalCache.RemoveDepKeyList(oldKeysTable, operationContext);

                                        
                                        Hashtable keyDepInfos = GetKeyDependencyInfoTable(goodKeysList[i], (KeyDependencyInfo[])keyDepInfoTable["newKeys"]);
                                        table = _internalCache.AddDepKeyList(keyDepInfos, operationContext);
                                    }
                                    else if (goodEntries[i].KeysIAmDependingOn != null)
                                    {
                                        Hashtable newKeyDepInfoTable = GetKeyDependencyInfoTable(goodKeysList[i], goodEntries[i]);
                                        table = _internalCache.AddDepKeyList(newKeyDepInfoTable, operationContext);
                                    }

                                    if (table != null)
                                    {
                                        IDictionaryEnumerator en = table.GetEnumerator();
                                        while (en.MoveNext())
                                        {
                                            if (en.Value is bool && !((bool)en.Value))
                                            {
                                                CacheInsResultWithEntry resultWithEntry = CacheInsResultWithEntry.CreateCacheInsResultWithEntry(_context.TransactionalPoolManager);
                                                resultWithEntry.Result = CacheInsResult.DependencyKeyError;

                                                retVal[goodKeysList[i]] = resultWithEntry;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (generateQueryInfo == null)
                        {
                            operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                        }

                        for (int i = 0; i < badKeysList.Count; i++)
                        {
                            CacheInsResultWithEntry resultWithEntry = CacheInsResultWithEntry.CreateCacheInsResultWithEntry(_context.TransactionalPoolManager);
                            resultWithEntry.Result = CacheInsResult.DependencyKeyNotExist;

                            retVal.Add(badKeysList[i], resultWithEntry);
                        }
                        #endregion
                    }
                }
                catch (Exception e)
                {
                    if (_clusteredExceptions) throw;
                }
                
                // 'retVal' is probably null so keys failed to insert
                // Return all clones to pool thus
                if (clone != null)
                    MiscUtil.ReturnEntriesToPool(clone, Context.TransactionalPoolManager);
                

                return retVal;
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.Topology);

                if (clone != null)
                    clone.MarkFree(NCModulesConstants.Topology);

                if (goodEntries != null)
                    goodEntries.MarkFree(NCModulesConstants.Topology);

            }
        }


        /// <summary>
        /// updates a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleInsert(Address src, object info, Array userPayload)
        {
            try
            {
                object[] objs = (object[])info;
                //bool returnEntry = false;
                string taskId = null;
                ulong version = 0;
                OperationContext operationContext = null;

                if (objs.Length > 2)
                    taskId = objs[2] != null ? objs[2] as string : null;

                if (objs.Length > 3)
                    if (objs[3] is OperationContext)
                        operationContext = objs[3] as OperationContext;
                if (objs.Length > 4)
                    if (objs[4] is ulong)
                        version = (ulong)objs[4];

                if (operationContext != null) operationContext.UseObjectPool = false;

                if (objs[0] is object[])
                {
                    object[] keys = (object[])objs[0];
                    CacheEntry[] entries = objs[1] as CacheEntry[];
                    Local_Insert(keys, entries, src, taskId, false, operationContext);
                }
                else
                {
                    object key = objs[0];
                    CacheEntry e = objs[1] as CacheEntry;

                    if (userPayload != null)
                    {
                        if (e.Value == null)
                            e.Value = userPayload;
                    }
                    object lockId = null;
                    LockAccessType accessType = LockAccessType.IGNORE_LOCK;
                    if (objs.Length == 6)
                    {
                        lockId = objs[3];
                        accessType = (LockAccessType)objs[4];
                        //version is never passed in case of POR and also before addition of operation context
                        //the object array use to contain only 6 objects so this condition was never true and was
                        //always by passed previously.Now operation context is passed in the as 7th element of object array.

                        operationContext = objs[5] as OperationContext;
                    }

                    if (operationContext != null) operationContext.UseObjectPool = false;

                    Local_Insert(key, e, src, taskId, true, null, version, LockAccessType.IGNORE_LOCK, operationContext);
                }
            }
            catch (Exception e)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        #endregion

        #region	/                 --- Mirror ICache.Remove ---           /

        public override object RemoveSync(object[] keys, ItemRemoveReason reason, bool notify, OperationContext operationContext)
        {
            object result = null;
            try
            {
                result = handleRemoveRange(new object[] { keys, reason, operationContext });
            }
            catch (Exception e)
            {
                throw;
            }
            return result;
        }

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// Remove notifications in a repicated cluster are handled differently. If there is an 
        /// explicit request for Remove, the node initiating the request triggers the notifications.
        /// Expirations and Evictions are replicated and again the node initiating the replication
        /// triggers the cluster-wide notification.
        /// </remarks>
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("MirrCache.Remove", "");

            try
            {
                /// Wait until the object enters any running status
                _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

                if (_internalCache == null) throw new InvalidOperationException();
                operationContext?.MarkInUse(NCModulesConstants.Topology);
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

                CacheEntry e = null;

                string taskId = null;
                if (updateOptions == DataSourceUpdateOptions.WriteBehind)
                    taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

                e = Local_Remove(actualKey, ir, Cluster.LocalAddress, notification, taskId, providerName, true, lockId, version, accessType, operationContext);


                return e;
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.Topology);
            }
        }

        /// <summary>
        /// Removes the objects and key pairs from the cache. The keys are specified as parameter.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>keys that actually removed from the cache</returns>
        /// <remarks>
        /// Remove notifications in a repicated cluster are handled differently. If there is an 
        /// explicit request for Remove, the node initiating the request triggers the notifications.
        /// Expirations and Evictions are replicated and again the node initiating the replication
        /// triggers the cluster-wide notification.
        /// </remarks>
        public override Hashtable Remove(IList keys, ItemRemoveReason ir, bool notify, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("MirrCache.RemoveBlk", "");

            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            DataSourceUpdateOptions updateOptions = DataSourceUpdateOptions.None;
            Caching.Notifications notification = null;

            if (keys[0] is object[])
            {
                object[] package = keys[0] as object[];
                keys[0] = package[0];
                updateOptions = (DataSourceUpdateOptions)package[1];
                notification = package[2] as Caching.Notifications;
            }

            string taskId = null;
            if (updateOptions == DataSourceUpdateOptions.WriteBehind)
                taskId = Cluster.LocalAddress.ToString() + ":" + NextSequence().ToString();

            Hashtable removed = null;
            removed = Local_Remove(keys, ir, Cluster.LocalAddress, notification, taskId,notify, operationContext);

            if (removed.Count > 0)
            {
                IDictionaryEnumerator ide = removed.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    object key = ide.Key;
                    CacheEntry e = ide.Value as CacheEntry;
                    if (e != null)
                    {
                        RemoveUpdateIndexOperation(key, operationContext);
                    }
                }
            }

            return removed;
        }

        /// <summary>
        /// Remove the object from the local cache only. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="ir"></param>
        /// <param name="notify"></param>
        /// <returns>cache entry.</returns>
        private CacheEntry Local_Remove(object key, ItemRemoveReason ir, Address src, Caching.Notifications notification, string taskId, string providerName, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            CacheEntry cloned = null;
            if (_internalCache != null)
            {
                retVal = _internalCache.Remove(key, ir, notify, taskId, lockId, version, accessType, operationContext);

                if (retVal != null)
                {
                    Object[] oldKeys = retVal.KeysIAmDependingOn;
                    if (oldKeys != null)
                    {
                        Hashtable oldKeysTable = new Hashtable();
                        for (int i = 0; i < oldKeys.Length; i++)
                        {
                            if (!oldKeysTable.Contains(oldKeys[i]))
                            {
                                oldKeysTable.Add(oldKeys[i], new ArrayList());
                            }
                            ((ArrayList)oldKeysTable[oldKeys[i]]).Add(key);
                        }
                        _internalCache.RemoveDepKeyList(oldKeysTable, operationContext);
                    }
                }

            }
            try
            {
                if (retVal != null && taskId != null)
                {
                    cloned = retVal.DeepClone(Context.TransactionalPoolManager);
                    cloned.ProviderName = providerName;
                    cloned.MarkInUse(NCModulesConstants.Topology);
                    if (notification != null)
                    {
                        if (cloned.Notifications != null)
                        {
                            cloned.Notifications.WriteBehindOperationCompletedCallback = notification.WriteBehindOperationCompletedCallback;
                        }
                        else
                        {
                            cloned.Notifications = notification;
                        }
                    }                    
                }
            }
            finally
            {
                if (cloned != null)
                    cloned.MarkFree(NCModulesConstants.Topology);
            }
            return retVal;
        }

        private Hashtable Local_Cascaded_Remove(object key, CacheEntry e, ItemRemoveReason ir, Address src, bool notify, OperationContext operationContext)
        {
            // 'false' means the call is from remove and hence remove the cache items anyways
            return Local_Cascaded_Remove(key, e, ir, src, notify, operationContext);
        }

        /// <summary>
        /// Remove the objects from the local cache only. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="ir"></param>
        /// <param name="notify"></param>
        /// <returns>keys and values that actualy removed from the cache</returns>
        private Hashtable Local_Remove(IList keys, ItemRemoveReason ir, Address src, Caching.Notifications notification, string taskId, bool notify, OperationContext operationContext)
        {
            Hashtable retVal = null;
            if (_internalCache != null)
            {
                retVal = _internalCache.Remove(keys, ir, notify, taskId, operationContext);

                for (int i = 0; i < keys.Count; i++)
                {
                    if (operationContext.CancellationToken !=null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    if (retVal[keys[i]] is CacheEntry)
                    {
                        CacheEntry entry = (CacheEntry)retVal[keys[i]];
                        _internalCache.RemoveDepKeyList(GetKeysTable(keys[i], entry.KeysIAmDependingOn), operationContext);                       
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        public override Hashtable Remove(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            // Wait until the object enters the running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            ArrayList list = GetGroupKeys(group, subGroup, operationContext);
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
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            ICollection list = GetTagKeys(tags, tagComparisonType, operationContext);
            if (list != null && list.Count > 0)
            {
                object[] grpKeys = MiscUtil.GetArrayFromCollection(list);
                return Remove(grpKeys, ItemRemoveReason.Removed, notify, operationContext);

            }

            return null;

        }

        internal override ICollection GetTagKeys(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {

            if (_internalCache != null)
                return _internalCache.GetTagKeys(tags, comparisonType, operationContext);

            return null;
        }

        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        public virtual Hashtable Local_Remove(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            Hashtable retVal = null;
            if (_internalCache != null)
            {
                retVal = _internalCache.Remove(group, subGroup, notify, operationContext);
            }
            return retVal;
        }

        /// <summary>
        /// Hanlde cluster-wide Remove(key) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        /// <remarks>
        /// Removes an item locally; however without generating notifications. 
        /// <para>
        /// <b>Note:</b> When a client invokes <see cref="handleRemove"/>; it is the clients reponsibility
        /// to actaully initiate the notification.
        /// </para>
        /// </remarks>
        private object handleRemove(Address src, object info)
        {
            try
            {
                object result = null;

                if (info is object[])
                {
                    object[] args = info as Object[];
                    string taskId = null;
                    Caching.Notifications notification = null;
                    OperationContext oc = null;

                    if (args.Length > 3)
                        notification = args[3] as Caching.Notifications;
                    if (args.Length > 4)
                        taskId = args[4] as string;

                    if (args.Length == 3)
                        oc = args[2] as OperationContext;

                    if (args.Length == 10)
                        oc = args[9] as OperationContext;

                    if (args.Length == 7)
                        oc = args[6] as OperationContext;

                    if (oc != null) oc.UseObjectPool = false;

                    if (args != null && args.Length > 0)
                    {
                        object tmp = args[0];
                        if (tmp is Object[])
                        {
                            Local_Remove((object[])tmp, ItemRemoveReason.Removed, src, notification, taskId, false, oc);

                        }
                        else
                        {
                            Local_Remove(tmp, ItemRemoveReason.Removed, src, notification, taskId, null, true, null, 0, LockAccessType.IGNORE_LOCK, oc);
                        }
                    }
                }
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        /// <summary>
        /// Hanlde cluster-wide Remove(key[]) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        /// <remarks>
        /// Removes a range of items locally; however without generating notifications.
        /// <para>
        /// <b>Note:</b> When a client invokes <see cref="handleRemoveRange"/>; it is the clients 
        /// responsibility to actaully initiate the notification.
        /// </para>
        /// </remarks>
        private object handleRemoveRange(object info)
        {
            try
            {
                object[] objs = (object[])info;
                OperationContext oc = null;
                if (objs[0] is object[])
                {
                    object[] keys = (object[])objs[0];
                    ItemRemoveReason ir = (ItemRemoveReason)objs[1];
                    if (objs.Length > 3)
                        oc = objs[3] as OperationContext;
                    else
                        oc = objs[2] as OperationContext;

                    Hashtable totalRemovedItems = new Hashtable();
                    CacheEntry entry = null;
                    IDictionaryEnumerator ide = null;

                    if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("MirrorCache.handleRemoveRange()", "Keys = " + keys.Length.ToString());

                    if (oc != null) oc.UseObjectPool = false;
                    //Raise events on active node only
                    Hashtable removedItems = Local_Remove(keys, ir, null, null, null, Cluster.IsCoordinator, oc);
                    if (removedItems != null)
                    {
                        totalRemovedItems = removedItems;
                        Hashtable cascRemovedItems = InternalCache.Cascaded_remove(removedItems, ir, Cluster.IsCoordinator, false, oc);
                        if (cascRemovedItems != null && cascRemovedItems.Count > 0)
                        {
                            ide = cascRemovedItems.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                RemoveUpdateIndexOperation(ide.Key, oc);
                                if (!totalRemovedItems.Contains(ide.Key))
                                    totalRemovedItems.Add(ide.Key, ide.Value);
                            }
                        }
                    }
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
                var operationContext = param[3] as OperationContext;
                if (operationContext != null) operationContext.UseObjectPool = false;

                Local_Remove((string)param[0], (string)param[1], (bool)param[2],operationContext);
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }

        #endregion

        private Hashtable handleAddDepKeyList(object info)
        {
            try
            {
                object[] objs = (object[])info;
                return _internalCache.AddDepKeyList((Hashtable)objs[0], objs[1] as OperationContext);
            }
            catch (Exception)
            {
                if (_clusteredExceptions) throw;
            }
            return null;
        }
        #endregion

        #region /               --- Key based notification  ---     /

        /// <summary>
        /// Sends a cluster wide request to resgister the key based notifications.
        /// </summary>
        /// <param name="key">key agains which notificaiton is to be registered.</param>
        /// <param name="updateCallback"></param>
        /// <param name="removeCallback"></param>
        public override void RegisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] { key, updateCallback, removeCallback, operationContext };
            handleRegisterKeyNotification(obj);
        }

        public override void RegisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] { keys, updateCallback, removeCallback, operationContext };
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
            handleUnregisterKeyNotification(obj);
        }

        public override void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            object[] obj = new object[] { keys, updateCallback, removeCallback, operationContext };
            handleUnregisterKeyNotification(obj);
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

        public override void HandleDryPoll(object operand)
        {
            object[] operands = operand as object[];
            if (operands != null)
            {
                OperationContext operationContext = operands[0] as OperationContext;
                if (_internalCache != null)
                {
                    _internalCache.Poll(operationContext);
                }
            }
        }

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

        #endregion
       

        public override bool IsEvictionAllowed
        {
            get
            {
                return Cluster.IsCoordinator;
            }
            set
            {
                base.IsEvictionAllowed = value;
            }
        }

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
        void ICacheEventsListener.OnItemAdded(object key, OperationContext operationContext, EventContext eventContext)
        {
            NotifyItemAdded(key, false, operationContext, eventContext);
        }

        /// <summary>
        /// Hanlder for clustered item added notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private new object handleNotifyAdd(object info)
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
        #region /          --- OnCustomEvent ---           /

        void ICacheEventsListener.OnCustomEvent(object notifId, object data, OperationContext operationContext, EventContext eventContext)
        {
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

        #region /          --- OnWriteBehindTaskCompletedEvent ---           /

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

        #region	/                 --- OnItemUpdated ---           /

        /// <summary> 
        /// handler for item updated event.
        /// </summary>
        void ICacheEventsListener.OnItemUpdated(object key, OperationContext operationContext, EventContext eventContext)
        {
            NotifyItemUpdated(key, false, operationContext, eventContext);
        }

        /// <summary>
        /// Hanlder for clustered item updated notification.
        /// </summary>
        /// <param name="info">packaged information</param>
        /// <returns>null</returns>
        private new object handleNotifyUpdate(object info)
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
                NotifyItemUpdated(info, true, null, null);

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
                NotifyOldItemUpdated(info, true, null, null);

            return null;
        }
        #region	/                 --- OnItemRemoved ---           /

        /// <summary> 
        /// Fired when an item is removed from the cache.
        /// </summary>
        void ICacheEventsListener.OnItemRemoved(object key, object val, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext)
        {
                
            NotifyItemRemoved(key, val, reason, false, operationContext, eventContext);
            
            ((ICacheEventsListener)this).OnItemsRemoved(new object[] { key }, new object[] { val }, reason, operationContext, new EventContext[] { eventContext });
        }

        /// <summary> 
        /// Fired when multiple items are removed from the cache. 
        /// </summary>
        /// <remarks>
        /// If the items are removed due to evictions or expiration, this method replicates the
        /// removals, i.e., makes sure those items get removed from every node in the cluster. It
        /// then also triggers the cluster-wide item remove notification.
        /// </remarks>
        void ICacheEventsListener.OnItemsRemoved(object[] keys, object[] values, ItemRemoveReason reason, OperationContext operationContext, EventContext[] eventContexts)
        {
            bool notifyRemove = false;
            try
            {
                object notifyRemoval = operationContext.GetValueByField(OperationContextFieldName.NotifyRemove);
                if (notifyRemoval != null)
                    notifyRemove = (bool)notifyRemoval;
                // do not notify if explicitly removed by Remove()
                if ((reason == ItemRemoveReason.Removed || reason == ItemRemoveReason.DependencyChanged) && !(bool)notifyRemove) return;

                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Mirror.OnItemsRemoved()", "items evicted/expired, now replicating");

                ///// If it is not due to eviction we replicate! Eviction is supposed to
                ///// occur on all the nodes simultaneously (since we assume that in replicated
                ///// clusters configuration is exact same at every node).
                CacheEntry entry;
                for (int i = 0; i < keys.Length; i++)
                {
                    if (values[i] == null) continue;
                    entry = (CacheEntry)values[i];
                    object value = entry.Value;

                    object data = new object[] { keys[i], reason, operationContext, eventContexts[i] };

                    handleNotifyRemoval(data);
                }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Warn("Mirror.OnItemsRemoved", "failed: " + e.ToString());
            }
            finally
            {
                UpdateCacheStatistics();
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

        #endregion


        private object handleOldNotifyRemoval(object info)
        {
            object[] objs = (object[])info;
            NotifyOldItemRemoved(objs[0], objs[1], (ItemRemoveReason)objs[2], true, (OperationContext)objs[1], (EventContext)objs[2]);
            return null;
        }
        void ICacheEventsListener.OnPollNotify(string clientId, short callbackId, Alachisoft.NCache.Caching.Events.EventTypeInternal eventtype)
        {
            try
            {
                RaisePollRequestNotifier(clientId, callbackId, eventtype);
            }
            catch (Exception e)
            {
                Context.NCacheLog.Warn("Mirror.OnPollNotify", "failed: " + e.ToString());
            }
        }


        #region	/                 --- OnCustomUpdateCallback ---           /

        /// <summary> 
        /// handler for item update callback event.
        /// </summary>
        void ICacheEventsListener.OnCustomUpdateCallback(object key, object value, OperationContext operationContext, EventContext eventContext)
        {
            // specially handled in Add.
        }


        #endregion

        #region	/                 --- OnCustomRemoveCallback ---           /

        /// <summary> 
        /// handler for item remove callback event.
        /// </summary>
        void ICacheEventsListener.OnCustomRemoveCallback(object key, object value, ItemRemoveReason removalReason, OperationContext operationContext, EventContext eventContext)
        {
            bool notifyRemove = false;
            try
            {
                object notifyRemoval = operationContext.GetValueByField(OperationContextFieldName.NotifyRemove);
                if (notifyRemoval != null)
                    notifyRemove = (bool)notifyRemoval;
                // do not notify if explicitly removed by Remove()
                if ((removalReason == ItemRemoveReason.Removed || removalReason == ItemRemoveReason.DependencyChanged) && !notifyRemove) return;

                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Mirror.OnCustomRemoveCallback()", "items evicted/expired, now replicating");

                if (value != null && value is CacheEntry)
                {
                    CacheEntry entry = value as CacheEntry;
                    RaiseCustomRemoveCalbackNotifier(key, entry, removalReason, operationContext, eventContext);
                }

            }
            catch (Exception e)
            {
                Context.NCacheLog.Warn("Mirror.OnItemsRemoved", "failed: " + e.ToString());
            }
            finally
            {
                UpdateCacheStatistics();
            }
        }

        #endregion



        #endregion

        #region /           ---- Async Replication handlers ---         /
        /// <summary>
        /// Replicates the data to the passive node in the Mirror Cache.
        /// </summary>
        /// <param name="opCodes">The operation codes for corresponding dataArray1 and dataArray2.</param>
        /// <param name="dataArray1">Array of data. This array may contain keys, group string or any other data based upon the operation.</param>
        /// <param name="dataArray2">Array of data. This array may contain entries, subGroup string or any other data based upon the operation. It may also be null for remove operations.</param>
        public override void ReplicateOperations(IList opCodes, IList info, IList userPayLoads, IList compilationInfo, ulong seqId, long viewId)
        {
            try
            {
                if (Cluster.Servers.Count > 1)
                {
                    Function func = new Function((int)OpCodes.ReplicateOperations, new object[] { opCodes, info, compilationInfo }, false);
                    func.UserPayload = ((ClusteredArrayList)userPayLoads).ToArray();
                    Cluster.SendMessage((Address)Cluster.OtherServers[0], func, GroupRequest.GET_FIRST, false);// sequence less operation
                }
            }
            catch (CacheException e)
            {
                throw;
            }
            catch (Exception e)
            {
                //NCacheLog.Error(Context.CacheName, "MirrorCache.ReplicateInvalidatedItem()", e.ToString());
                throw new GeneralFailureException(e.Message, e);
            }
        }

        private static Hashtable GetAllPayLoads(Array userPayLoad, ArrayList compilationInfo)
        {
            Hashtable result = new Hashtable();
            int arrayIndex = 0;
            int readIndex = 0;

            VirtualArray payLoadArray = new VirtualArray(userPayLoad);
            VirtualIndex virtualIndex = new VirtualIndex();

            for (int i = 0; i < compilationInfo.Count; i++)
            {
                if ((long)compilationInfo[i] == 0)
                {
                    result[i] = null;
                }
                else
                {
                    VirtualArray atomicPayLoadArray = new VirtualArray((long)compilationInfo[i]);
                    VirtualIndex atomicVirtualIndex = new VirtualIndex();

                    VirtualArray.CopyData(payLoadArray, virtualIndex, atomicPayLoadArray, atomicVirtualIndex, (int)atomicPayLoadArray.Size);
                    virtualIndex.IncrementBy((int)atomicPayLoadArray.Size);
                    result[i] = atomicPayLoadArray.BaseArray;
                }
            }
            return result;
        }

        private object handleReplicateOperations(Address src, object info, Array userPayLoad)
        {
            lock (_replicationQueue)
            {
                if (_executionThread == null)
                {
                    _executionThread = new Thread(new ThreadStart(handleReplicateAysnc));
                    _executionThread.Name = "Replication.ExecutionThread";
                    _executionThread.IsBackground = true;
                    _executionThread.Start();
                }

                long nextKey = _replicationAutoKey++;
                ReplicationOperation replicationOperation = new ReplicationOperation(info);
                replicationOperation.UserPayLoad = userPayLoad;

                _replicationQueue.Enqueue(nextKey.ToString(), replicationOperation);
             //   _context.PerfStatsColl.IncrementMirrorQueueSizeStats(_replicationQueue.Count);

                Monitor.Pulse(_replicationQueue);
            }

            return null;
        }

        private void handleReplicateAysnc()
        {
            while (true)
            {
                try
                {
                    IOptimizedQueueOperation operation = null;

                    lock (_replicationQueue)
                    {
                        if (_replicationQueue.Count > 0)
                        {
                            operation = _replicationQueue.Dequeue();
                           // _context.PerfStatsColl.IncrementMirrorQueueSizeStats(_replicationQueue.Count);
                        }
                        else
                        {
                            Monitor.Wait(_replicationQueue);
                            continue;
                        }
                    }

                    if (operation != null)
                    {
                        Object[] objs = (Object[])operation.Data;
                        IList opCodes = (IList)objs[0];
                        IList infos = (IList)objs[1];
                        IList compilationInfo = (IList)objs[2];

                        ExecuteReplicationOperation(opCodes, infos, operation.UserPayLoad, compilationInfo, true);
                    }

                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
                catch (Exception e)
                {
                    if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("Mirror.handleReplicateAsync", e.ToString());
                }
            }
        }

        protected object ExecuteReplicationOperation(IList opCodes, IList infos, IList userPayLoad, IList compilationInfo, Boolean isAtomic = false)
        {
            try
            {
                HashVector tbl = null;
                Address src = Cluster.Coordinator;
                if (userPayLoad != null && userPayLoad.Count > 0)
                    tbl = GetAllPayLoads(userPayLoad, compilationInfo);

                OperationContext.IsReplicationOperation = true;

                for (int i = 0; i < opCodes.Count; i++)
                {
                    try
                    {
                        Array payLoad = null;
                        if (tbl != null)
                        {
                            payLoad = tbl[i] != null ? tbl[i] as Array : null;
                        }
                        object result = null;
                        switch ((int)opCodes[i])
                        {
                            case (int)OpCodes.Insert:
                                result = handleInsert(src, infos[i], payLoad);
                                ClusterHelper.ReleaseCacheInsertResult(result);
                                break;

                            case (int)OpCodes.Add:
                                handleAdd(src, infos[i], payLoad);
                                break;

                            case (int)OpCodes.AddHint:
                                handleAddHint(src, infos[i]);
                                break;                           

                            case (int)OpCodes.Remove:
                                result = handleRemove(src, infos[i]);
                                ClusterHelper.ReleaseCacheEntry(result);
                                break;

                            case (int)OpCodes.RemoveRange:

                                result = handleRemoveRange(infos[i]);
                                ClusterHelper.ReleaseCacheEntry(result);
                                break;

                            case (int)OpCodes.Clear:
                                handleClear(src, infos[i]);
                                break;

                            case (int)OpCodes.NotifyAdd:
                                handleNotifyAdd(infos[i]);
                                break;

                            case (int)OpCodes.NotifyOldAdd:
                                handleOldNotifyAdd(infos[i]);
                                break;

                            case (int)OpCodes.NotifyUpdate:
                                handleNotifyUpdate(infos[i]);
                                break;

                            case (int)OpCodes.NotifyOldUpdate:
                                handleOldNotifyUpdate(infos[i]);
                                break;

                            case (int)OpCodes.NotifyRemoval:
                                handleNotifyRemoval(infos[i]);
                                break;

                            case (int)OpCodes.NotifyOldRemoval:
                                handleOldNotifyRemoval(infos[i]);
                                break;

                            case (int)OpCodes.RemoveGroup:
                                result = handleRemoveGroup(infos[i]);
                                ClusterHelper.ReleaseCacheEntry(result);
                                break;

                            case (int)OpCodes.UpdateIndice:
                                handleUpdateIndice(infos[i]);
                                break;                            

                            case (int)OpCodes.UpdateLockInfo:
                                handleUpdateLockInfo(infos[i]);
                                break;

                            case (int)OpCodes.RegisterKeyNotification:
                                handleRegisterKeyNotification(infos[i]);
                                break;

                            case (int)OpCodes.UnregisterKeyNotification:
                                handleUnregisterKeyNotification(infos[i]);
                                break;

                            case (int)OpCodes.OpenStream:
                                handleOpenStreamOperation(src, (OpenStreamOperation)infos[i]);
                                break;

                            case (int)OpCodes.CloseStream:
                                handleCloseStreamOperation(src, (CloseStreamOperation)infos[i]);
                                break;

                            case (int)OpCodes.WriteToStream:
                                handleWriteToStreamOperation(src, (WriteToStreamOperation)infos[i]);
                                break;

                            case (int)OpCodes.AddDepKeyList:
                                handleAddDepKeyList(infos[i]);
                                break;

                            case (int)OpCodes.Message_Acknowldegment:
                                HandleAcknowledgeMessageReceipt((AcknowledgeMessageOperation)infos[i]);
                                break;
                            case (int)OpCodes.AssignmentOperation:
                                HandleAssignSubscription((AssignmentOperation)infos[i]);
                                break;

                            case (int)OpCodes.RemoveMessages:
                                HandleRemoveMessages((RemoveMessagesOperation)infos[i]);
                                break;
                            case (int)OpCodes.StoreMessage:
                                HandleStoreMessage((StoreMessageOperation)infos[i]);
                                break;                            
                        }
                    }
                    catch (Exception e)
                    {
                        if (Context.NCacheLog.IsErrorEnabled) Context.NCacheLog.Error("MirrorCache.handleReplicateInvalidatedItems", e.ToString());

                    }
                }
            }
            catch (Exception e)
            {
                if (Context.NCacheLog.IsErrorEnabled) Context.NCacheLog.Error("MirrorCache.handleReplicateInvalidatedItems", e.ToString());
            }
            return null;
        }
     

            #endregion

        internal override void StopServices()
        {
            _statusLatch.SetStatusBit(0, NodeStatus.Initializing | NodeStatus.Running);
            if (InternalCache != null)
                InternalCache.IsEvictionAllowed = false;
            if (_asyncReplicator != null)
                _asyncReplicator.Dispose();

            base.StopServices();
        }

        #region lock

        public override LockOptions Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("MirrCache.lock", "lock_id :" + lockId);
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            /// If we are in state transfer, we check locally first and then 
            /// to make sure we do a clustered call and fetch from some other 
            /// functional node.
            LockOptions lockInfo = Local_Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);

            return lockInfo;
        }

        private LockOptions Local_Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
            return null;
        }

        private LockOptions handleLock(object info)
        {
            object[] args = info as object[];
            object key = args[0];
            object lockId = args[1];
            DateTime lockDate = DateTime.SpecifyKind((DateTime)args[2], DateTimeKind.Utc);
            LockExpiration lockExpiration = (LockExpiration)args[3];
            OperationContext operationContext = null;

            if (args.Length > 4)
                operationContext = args[4] as OperationContext;

            return Local_Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
        }

        public override void UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("MirrCache.Unlock", "lock_id :" + lockId);
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            /// If we are in state transfer, we check locally first and then 
            /// to make sure we do a clustered call and fetch from some other 
            /// functional node.
            Local_UnLock(key, lockId, isPreemptive, operationContext);
        }

        private void Local_UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (_internalCache != null)
                _internalCache.UnLock(key, lockId, isPreemptive, operationContext);
        }

        private void handleUnLock(object info)
        {
            object[] args = info as object[];
            object key = args[0];
            object lockId = args[1];
            bool isPreemptive = (bool)args[2];
            OperationContext operationContext = null;
            if (args.Length > 3)
                operationContext = args[3] as OperationContext;

            Local_UnLock(key, lockId, isPreemptive, operationContext);
        }

        public override LockOptions IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            /// If we are in state transfer, we check locally first and then 
            /// to make sure we do a clustered call and fetch from some other 
            /// functional node.
            return Local_IsLocked(key, ref lockId, ref lockDate, operationContext);
        }

        private LockOptions Local_IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.IsLocked(key, ref lockId, ref lockDate, operationContext);
            return null;
        }

        private LockOptions handleIsLocked(object info)
        {
            object[] args = info as object[];
            object key = args[0];
            object lockId = args[1];
            DateTime lockDate = DateTime.SpecifyKind((DateTime)args[2], DateTimeKind.Utc);
            OperationContext operationContext = null;
            if (args.Length > 3)
                operationContext = args[3] as OperationContext;

            return Local_IsLocked(key, ref lockId, ref lockDate, operationContext);
        }

        #endregion

        protected override CacheNodeStatus GetNodeStatus()
        {
            CacheNodeStatus status = CacheNodeStatus.Running;

            if (_statusLatch.IsAnyBitsSet(NodeStatus.Initializing))
                status = CacheNodeStatus.InStateTransfer;

            return status;
        }

        public override List<CacheNodeStatistics> GetCacheNodeStatistics()
        {
            List<CacheNodeStatistics> statistics = base.GetCacheNodeStatistics();

            foreach (CacheNodeStatistics stats in statistics)
            {
                stats.Node.IsReplica = !Cluster.IsCoordinator;	//Set node replica attribute to true.
            }

            return statistics;
        }

        #region /               --- Stream Operation ---                    /

        public override bool OpenStream(string key, string lockHandle, Alachisoft.NCache.Common.Enum.StreamModes mode, string group, string subGroup, ExpirationHint hint, Alachisoft.NCache.Caching.EvictionPolicies.EvictionHint evictinHint, OperationContext operationContext)
        {
            return InternalCache.OpenStream(key, lockHandle, mode, group, subGroup, hint, evictinHint, operationContext);
        }

        public override void CloseStream(string key, string lockHandle, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            InternalCache.CloseStream(key, lockHandle, operationContext);
        }

        public override int ReadFromStream(ref VirtualArray vBuffer, string key, string lockHandle, int offset, int length, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            return InternalCache.ReadFromStream(ref vBuffer, key, lockHandle, offset, length, operationContext);
        }

        public override void WriteToStream(string key, string lockHandle, VirtualArray vBuffer, int srcOffset, int dstOffset, int length, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            InternalCache.WriteToStream(key, lockHandle, vBuffer, srcOffset, dstOffset, length, operationContext);
        }

        public override long GetStreamLength(string key, string lockHandle, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();

            return InternalCache.GetStreamLength(key, lockHandle, operationContext);
        }

        protected override Alachisoft.NCache.Caching.Topologies.Clustered.Results.OpenStreamResult handleOpenStreamOperation(Address source, Alachisoft.NCache.Caching.Topologies.Clustered.Operations.OpenStreamOperation operation)
        {
            Alachisoft.NCache.Caching.Topologies.Clustered.Results.OpenStreamResult result = null;

            object[] keys = null;
            #region -- PART I -- Cascading Dependency Operation

            if (operation.Mode == Alachisoft.NCache.Common.Enum.StreamModes.Write)
            {
                keys = CacheHelper.GetKeyDependencyTable(operation.ExpirationHint);
                if (keys != null)
                {
                    Hashtable goodKeysTable = _internalCache.Contains(keys, operation.OperationContext);

                    if (!goodKeysTable.ContainsKey("items-found"))
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                    if (goodKeysTable["items-found"] == null)
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                    if (goodKeysTable["items-found"] == null || (((ArrayList)goodKeysTable["items-found"]).Count != keys.Length))
                        throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                }
            }
            #endregion

            result = base.handleOpenStreamOperation(source, operation);

            #region -- PART II -- Cascading Dependency Operation
            if (keys != null && result != null && result.LockAcquired && operation.Mode == Alachisoft.NCache.Common.Enum.StreamModes.Write)
            {
                Hashtable table = new Hashtable();
                
                Hashtable keyDepInfoTable = GetKeyDependencyInfoTable(operation.Key, (KeyDependencyInfo[])CacheHelper.GetKeyDependencyInfoTable(operation.ExpirationHint));
                try
                {
                    table = _internalCache.AddDepKeyList(keyDepInfoTable, operation.OperationContext);
                }
                catch (Exception e)
                {
                    throw e;
                }

                if (table != null)
                {
                    IDictionaryEnumerator en = table.GetEnumerator();
                    while (en.MoveNext())
                    {
                        if (en.Value is bool && !((bool)en.Value))
                        {
                            Local_Remove(operation.Key, ItemRemoveReason.Removed, source, null, null, null, false, null, 0, LockAccessType.IGNORE_LOCK, operation.OperationContext);
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));
                        }
                    }
                }

            }
            #endregion
            return result;
        }
        #endregion


        public override void LogBackingSource()
        {
            
        }

        #region --------------------------Touch ------------------------------------

        internal override void Touch(List<string> keys, OperationContext operationContext)
        {
            _statusLatch.WaitForAny(NodeStatus.Running);
            if (_internalCache == null) throw new InvalidOperationException();
            _internalCache.Touch(keys, operationContext);
        }

        #endregion

        #region                ------------- Messaging -----------------------------

        public override void AcknowledgeMessageReceipt(string clientId, IDictionary<string, IList<string>> topicWiseMessageIds, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity)
                ServerMonitor.LogClientActivity("MirrorCache.MessageAcknowledgement", "Begins");

            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null)
                throw new InvalidOperationException();

            try
            {
                _internalCache.AcknowledgeMessageReceipt(clientId, topicWiseMessageIds, operationContext);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity)
                    ServerMonitor.LogClientActivity("MirrorCache.MessageAcknowledgement", "Ends");
            }
        }


        public override bool AssignmentOperation(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo, TopicOperationType type, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity)
                ServerMonitor.LogClientActivity("MirrorCache.AssignmentOperation", "Begins");

            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null)
                throw new InvalidOperationException();

            if (Context.NCacheLog.IsInfoEnabled)
                Context.NCacheLog.Info("Mirror.AssignmentOperation()", "MessageId = " + messageInfo.MessageId);

            try
            {
                return _internalCache.AssignmentOperation(messageInfo, subscriptionInfo, type, context);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity)
                    ServerMonitor.LogClientActivity("MirrorCache.AssignmentOperation", "Ends");
            }
        }

        public override void RemoveMessages(IList<MessageInfo> messagesTobeRemoved, MessageRemovedReason reason, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity)
                ServerMonitor.LogClientActivity("MirrorCache.RemoveMessages", "Begins");

            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null)
                throw new InvalidOperationException();
            try
            {
                _internalCache.RemoveMessages(messagesTobeRemoved, reason, context);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity)
                    ServerMonitor.LogClientActivity("MirrorCache.RemoveMessages", "Ends");
            }
        }


        public override bool StoreMessage(string topic, Messaging.Message message, OperationContext context)
        {
            if (ServerMonitor.MonitorActivity)
                ServerMonitor.LogClientActivity("MirrorCache.StoreMessage", "Begins");

            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null)
                throw new InvalidOperationException();

            if (Context.NCacheLog.IsInfoEnabled)
                Context.NCacheLog.Info("Mirror.StoreMessage()", "MessageId = " + message.MessageId);

            try
            {
                _internalCache.StoreMessage(topic, message, context);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity)
                    ServerMonitor.LogClientActivity("MirrorCache.StoreMessage", "Ends");
            }
            return true;
        }

        public void OnOperationModeChanged(OperationMode mode)
        {
        }
        /// <summary>
        /// Number of messages published for this topic.
        /// </summary>
        /// <remarks>
        /// This property returns value for a specific topic. Count of other topics play no role.
        /// </remarks>
        public override long GetMessageCount(string topicName, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null)
            {
                throw new InvalidOperationException();
            }
            return Local_GetMessageCount(topicName, operationContext);
        }

        private long Local_GetMessageCount(string topicName, OperationContext operationContext)
        {
            if (_internalCache != null)
            {
                return _internalCache.GetMessageCount(topicName, operationContext);
            }
            return 0;
        }

        private object handleMessageCount(object info)
        {
            if (info != null)
            {
                try
                {
                    object[] operands = (object[])info;
                    string topicName = operands[0] as string;
                    return Local_MessageCount(topicName, null);
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
