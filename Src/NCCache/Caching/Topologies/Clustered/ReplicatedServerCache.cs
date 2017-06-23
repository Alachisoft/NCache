using System;
using System.Collections;
using System.Data;
using System.Threading;

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
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Stats;


using Alachisoft.NCache.Common.Monitoring;

using Alachisoft.NCache.Caching.Topologies.Clustered.Results;
using System.Net;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;

using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Enum;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.DataReader;


namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
     /// <summary>
    /// This class provides the partitioned cluster cache primitives. 
    /// </summary>
    internal class ReplicatedServerCache :
        ReplicatedCacheBase,
        IPresenceAnnouncement,
        ICacheEventsListener
    {
        /// <summary> Call balancing helper object. </summary>
        private IActivityDistributor _callBalancer;

        /// <summary> The periodic update task. </summary>
        private PeriodicPresenceAnnouncer _taskUpdate;

        private PeriodicStatsUpdater _localStatsUpdater;

        private StateTransferTask _stateTransferTask = null;

        /// <summary> keeps track of all server members excluding itself </summary>
        protected ArrayList _otherServers = ArrayList.Synchronized(new ArrayList(11));
        private ArrayList _nodesInStateTransfer = new ArrayList();
        private bool _allowEventRaiseLocally;
        private Latch _stateTransferLatch = new Latch((byte)ReplicatedStateTransferStatus.UNDER_STATE_TRANSFER);

        private AsyncItemReplicator _asyncReplicator = null;

  
        private ClientsManager _clientsMgr;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        /// <param name="listener">cache events listener</param>
        public ReplicatedServerCache(IDictionary cacheClasses, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(properties, listener, context)
        {
            _stats.ClassName = "replicated-server";
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
        public ReplicatedServerCache(IDictionary cacheClasses, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, IClusterEventsListener clusterListener)
            : base(properties, listener, context, clusterListener)
        {
            _stats.ClassName = "replicated-server";
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (_stateTransferTask != null)
            {
                _stateTransferTask.StopProcessing();
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

                _internalCache = CacheBase.Synchronized(new LocalCache(cacheClasses, this, cacheClasses, this, _context));

                _stats.Nodes = ArrayList.Synchronized(new ArrayList());
                _callBalancer = new CallBalancer();

                InitializeCluster(properties, Name, MCAST_DOMAIN, new Identity(true, (_context.Render != null ? _context.Render.Port : 0), (_context.Render != null ? _context.Render.IPAddress : null)));
                _stats.GroupName = Cluster.ClusterName;
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

        public override void Initialize(IDictionary cacheClasses, IDictionary properties,  bool twoPhaseInitialization)
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
                    _internalCache = CacheBase.Synchronized(new IndexedLocalCache(cacheClasses, this, frontCacheProps, this, _context));
                }
                else
                {
                    throw new ConfigurationException("invalid or non-local class specified in partitioned cache");
                }

                _stats.Nodes = ArrayList.Synchronized(new ArrayList());
                _callBalancer = new CallBalancer();

                InitializeCluster(properties, Name, MCAST_DOMAIN, new Identity(true, (_context.Render != null ? _context.Render.Port : 0), (_context.Render != null ? _context.Render.IPAddress : null)), twoPhaseInitialization, false);
                _stats.GroupName = Cluster.ClusterName;


                _clientsMgr = new ClientsManager(Cluster);

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
        /// Return the next node in call balacing order.
        /// </summary>
        /// <returns></returns>
        private Address GetNextNode()
        {
            NodeInfo node = _callBalancer.SelectNode(_stats, null);
            return node == null ? null : node.Address;
        }


        #region	/                 --- Overrides for ClusteredCache ---           /

        /// <summary>
        /// Called after the membership has been changed. Lets the members do some
        /// member oriented tasks.
        /// </summary>
        public override void OnAfterMembershipChange()
        {
            base.OnAfterMembershipChange();
            _context.ExpiryMgr.AllowClusteredExpiry = Cluster.IsCoordinator;

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

            Context.NCacheLog.Info("OnAfterMembershipChange", "bridge replicator started with source cache unique id: " + ((ClusterCacheBase)this).BridgeSourceCacheId);

            //async replicator is used to replicate the update index operations to other replica nodes.
            if (Cluster.Servers.Count > 1)
            {
                if (_asyncReplicator == null) _asyncReplicator = new AsyncItemReplicator(Context, new TimeSpan(0, 0, 2));
                _asyncReplicator.Start();
                Context.NCacheLog.CriticalInfo("OnAfterMembershipChange", "async-replicator started.");
            }
            else
            {
                if (_asyncReplicator != null)
                {
                    _asyncReplicator.Stop(false);
                    _asyncReplicator = null;
                    Context.NCacheLog.CriticalInfo("OnAfterMembershipChange", "async-replicator stopped.");
                }
            }

            UpdateCacheStatistics();
        }


        /// <summary>
        /// Called when a new member joins the group.
        /// </summary>
        /// 		/// <param name="address">address of the joining member</param>
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
            _stats.Nodes.Add(info);


            if (LocalAddress.CompareTo(address) == 0)
            {
                _stats.LocalNode = info;
            }
            else
            {
                lock (_nodesInStateTransfer)
                {
                    if (!_nodesInStateTransfer.Contains(address))
                        _nodesInStateTransfer.Add(address);
                }
                //add into the list of other servers.
                if (!_otherServers.Contains(address))
                    _otherServers.Add(address);
            }
            if (!info.IsInproc)
            {
                AddServerInformation(address, identity.RendererPort, info.ConnectedClients.Count);

                if (_clientsMgr != null)
                {
                    _clientsMgr.OnMemberJoined(address);
                }
            }

            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedCache.OnMemberJoined()", "Replication increased: " + address);
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

            //remove into the list of other servers.
            _otherServers.Remove(address);

            if (!info.IsInproc)
            {
                RemoveServerInformation(address, identity.RendererPort);

                if (_clientsMgr != null)
                {
                    _clientsMgr.OnMemberLeft(address);
                }
            }

            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedCache.OnMemberLeft()", "Replica Removed: " + address);
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

                case (int)OpCodes.Contains:
                    return handleContains(func.Operand);

                case (int)OpCodes.Get:
                    return handleGet(func.Operand);

                case (int)OpCodes.Insert:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    return handleInsert(src, func.Operand, func.UserPayload);

                case (int)OpCodes.Add:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    return handleAdd(src, func.Operand, func.UserPayload);

                case (int)OpCodes.AddHint:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    return handleAddHint(src, func.Operand);

                case (int)OpCodes.Remove:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    return handleRemove(src, func.Operand);

                case (int)OpCodes.RemoveRange:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    return handleRemoveRange(func.Operand);

                case (int)OpCodes.Clear:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    return handleClear(src, func.Operand);

                case (int)OpCodes.KeyList:
                    return handleKeyList();

                case (int)OpCodes.Search:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    return handleSearch(func.Operand);

                case (int)OpCodes.SearchEntries:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    return handleSearchEntries(func.Operand);

                case (int)OpCodes.UpdateIndice:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    return handleUpdateIndice(func.Operand);

                case (int)OpCodes.LockKey:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    return handleLock(func.Operand);

                case (int)OpCodes.UnLockKey:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    handleUnLockKey(func.Operand);
                    break;

                case (int)OpCodes.IsLocked:
                    return handleIsLocked(func.Operand);

                case (int)OpCodes.ReplicateOperations:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    return handleReplicateOperations(src, func.Operand, func.UserPayload);

                case (int)OpCodes.GetNextChunk:
                    _stateTransferLatch.WaitForAny((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED);
                    return handleGetNextChunk(src, func.Operand);

                case (int)OpCodes.NotifyCustomRemoveCallback:
                    return handleNotifyRemoveCallback(func.Operand);
                
                case (int)OpCodes.NotifyCustomUpdateCallback:
                    return handleNotifyUpdateCallback(func.Operand);

            }
            return base.HandleClusterMessage(src, func);
        }

        #endregion

        #region	/                 --- Statistics Replication ---           /

        /// <summary>
        /// Periodic update (PULL model), i.e. on demand fetch of information from every node.
        /// </summary>
        private bool DetermineClusterStatus()
        {
            try
            {
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedServerCache.DetermineClusterStatus", " determine cluster status");
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
                Context.NCacheLog.Error("ReplicatedCache.DetermineClusterStatus()", e.ToString());
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
        /// Periodic update (PUSH model), i.e., Publish cache statisitcs so that every node in 
        /// the cluster gets an idea of the state of every other node.
        /// </summary>
        public bool AnnouncePresence(bool urgent)
        {
            try
            {
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedCache.AnnouncePresence()", " announcing presence ;urget " + urgent);

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
                Context.NCacheLog.Error("ReplicatedCache.AnnouncePresence()", e.ToString());
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
            lock (Servers.SyncRoot)
            {
                NodeInfo other = obj as NodeInfo;
                NodeInfo info = _stats.GetNode(sender as Address);
                if (other != null && info != null)
                {
                    Context.NCacheLog.Debug("Replicated.handlePresenceAnnouncement()", "sender = " + sender + " stats = " + other.Statistics);
                    info.Statistics = other.Statistics;
                    info.ConnectedClients = other.ConnectedClients;
                    info.Status = other.Status;
                }

                if (object.Equals(Cluster.Coordinator, sender))
                {
                    _internalCache.UpdateClientsList(other.Statistics.ClientsList);
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
        public override void ClientConnected(string client, bool isInproc)
        {
            base.ClientConnected(client, isInproc);
            AnnouncePresence(false);
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
            AnnouncePresence(false);
            if (_context.ClientDeathDetection != null) UpdateClientStatus(client, true);
        }

        public override ArrayList DetermineClientConnectivity(ArrayList clients)
        {
            ArrayList result = null;
            if (clients == null) return null;
            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Client-Death-Detection.DetermineClientConnectivity()", "going to determine client connectivity in cluster");
            try
            {
                DetermineClusterStatus();//updating stats
                result = clients;
                foreach (string client in clients)
                {
                    foreach (NodeInfo node in _stats.Nodes)
                    {
                        if (node.ConnectedClients.Contains(client))
                        {
                            if (result.Contains(client))
                                result.Remove(client);
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("Client-Death-Detection.DetermineClientConnectivity()", e.ToString());
            }
            finally
            {
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Client-Death-Detection.DetermineClientConnectivity()", "determining client connectivity in cluster completed");
            }
            return result;
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
            private ReplicatedServerCache _parent = null;

            /// <summary> A promise object to wait on. </summary>
            private Promise _promise = null;

            private ILogger _ncacheLog;
            string _cacheserver="NCache";

            ILogger NCacheLog
            {
                get { return _ncacheLog; }
            }

            private bool _stopProcessing = false;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="parent"></param>
            public StateTransferTask(ReplicatedServerCache parent)
            {
                _parent = parent;
                _ncacheLog = parent.Context.NCacheLog;

                _promise = new Promise();
            }

            public void StopProcessing()
            {
                _stopProcessing = true;
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
                    _parent._statusLatch.SetStatusBit(NodeStatus.Initializing, NodeStatus.Running);
                    _parent.DetermineClusterStatus();

                    // Fetch the list of keys from coordinator and open an enumerator
                    IDictionaryEnumerator ie = _parent.Clustered_GetEnumerator(_parent.Cluster.Coordinator);
                    if (ie == null)
                    {
                        _parent._stateTransferLatch.SetStatusBit((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED, (byte)ReplicatedStateTransferStatus.UNDER_STATE_TRANSFER);
                        return;
                    }

                    Hashtable keysTable = new Hashtable();

                    while (ie.MoveNext())
                    {
                        keysTable.Add(ie.Key,null);
                    }
                    if (NCacheLog.IsErrorEnabled) NCacheLog.Info("ReplicatedServerCache.StateTransfer", "Transfered keys list"+keysTable.Count);
                    _parent._internalCache.SetStateTransferKeyList(keysTable);
                    _parent._stateTransferLatch.SetStatusBit((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED, (byte)ReplicatedStateTransferStatus.UNDER_STATE_TRANSFER);

                    ie.Reset();

                   
                    bool loggedonce = false;
                    while (ie.MoveNext())
                    {
                        if (!loggedonce)
                        {
                            NCacheLog.CriticalInfo("ReplicatedServerCache.StateTransfer", "State transfer has started");
                            AppUtil.LogEvent(_cacheserver, "\"" + _parent._context.SerializationContext + "\"" + " has started state transfer.", System.Diagnostics.EventLogEntryType.Information, EventCategories.Information, EventID.StateTransferStart);
                            loggedonce = logEvent = true;
                        }

                        if (!_stopProcessing)
                        {
                            object key = ie.Key;
                            // if the object is already there, skip a network call
                            if (_parent.Local_Contains(key, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation)))
                            {
                                continue;
                            }
                            // fetches the object remotely
                            CacheEntry val = ie.Value as CacheEntry;
                            if (val != null)
                            {
                                try
                                {
                                    // doing an Add ensures that the object is not updated
                                    // if it had already been added while we were fetching it.
                                    if (!_stopProcessing)
                                    {
                                        OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                                        _parent.Local_Add(key, val, null, false, operationContext);

                                        _parent.Context.PerfStatsColl.IncrementStateTxfrPerSecStats();
                                    }
                                    else
                                    {
                                        result = _parent.Local_Count();
                                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ReplicatedServerCache.StateTransfer", "  state transfer was stopped by the parent.");

                                        return;
                                    }
                                }
                                catch (Exception)
                                {
                                    // object already there so skip it.
                                }
                            }
                        }
                        else
                        {
                            result = _parent.Local_Count();
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ReplicatedServerCache.StateTransfer", "  state transfer was stopped by the parent.");
                            return;
                        }
                    }

                    if(_stopProcessing)
                    {
                        result = _parent.Local_Count();
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ReplicatedServerCache.StateTransfer", "  state transfer was stopped by the parent.");
                        return;
                    }
                    result = _parent.Local_Count();

                }
                catch (Exception e)
                {
                    result = e;
                }
                finally
                {
                    _parent._stateTransferLatch.SetStatusBit((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED, (byte)ReplicatedStateTransferStatus.UNDER_STATE_TRANSFER);
                    _parent._internalCache.UnSetStateTransferKeyList();

                    SignalEndOfTransfer(result);
                    if (logEvent)
                    {
                        NCacheLog.CriticalInfo("ReplicatedServerCache.StateTransfer", "State transfer has ended");

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
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedCache.StartStateTransfer()", "Requesting state transfer " + LocalAddress);

                /// Start the initialization(state trasfer) task.
                if (_stateTransferTask == null) _stateTransferTask = new StateTransferTask(this);
                _context.AsyncProc.Enqueue(_stateTransferTask);
            }
            else
            {
                _stateTransferLatch.SetStatusBit((byte)ReplicatedStateTransferStatus.STATE_TRANSFER_COMPLETED, (byte)ReplicatedStateTransferStatus.UNDER_STATE_TRANSFER);
                _allowEventRaiseLocally = true;
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
            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("ReplicatedCache.EndStateTransfer()", "State Txfr ended: " + result);
            if (result is Exception)
            {
                /// What to do? if we failed the state transfer?. Proabably we'll keep
                /// servicing in degraded mode? For the time being we don't!
            }

            /// Set the status to fully-functional (Running) and tell everyone about it.
            _statusLatch.SetStatusBit(NodeStatus.Running, NodeStatus.Initializing);

            Function fun = new Function((int)OpCodes.SignalEndOfStateTxfr, new object(),false);
            if (Cluster != null) Cluster.BroadcastToServers(fun, GroupRequest.GET_ALL, true);

            UpdateCacheStatistics();
            AnnouncePresence(true);
        }

        #endregion

        #region	/                 --- ICache ---           /

        #region	/                 --- Replicated ICache.Count ---           /

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
                    count = Local_Count();
                }
                return count;
            }
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

        #region	/                 --- Replicated ICache.Contains ---           /

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        public override bool Contains(object key, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCache.Cont", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            bool contains = false;

            if (IsInStateTransfer())
            {
                contains = Safe_Clustered_Contains(key, operationContext) != null;
            }
            else
            {
                contains = Local_Contains(key, operationContext);
            }

            return contains;
        }


        /// <summary>
        /// Determines whether the cache contains the specified keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
        public override Hashtable Contains(object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCache.ContBlk", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            Hashtable result = new Hashtable();
            Hashtable tbl = Local_Contains(keys, operationContext);
            ArrayList list = null;

            if (tbl != null && tbl.Count > 0)
            {
                list = (ArrayList)tbl["items-found"];
            }

            /// If we failed and during state transfer, we check from some other 
            /// functional node as well.
            if (list != null && list.Count < keys.Length && (_statusLatch.IsAnyBitsSet(NodeStatus.Initializing)))
            {
                object[] rKeys = new object[keys.Length - list.Count];
                int i = 0;
                IEnumerator ir = keys.GetEnumerator();
                while (ir.MoveNext())
                {
                    object key = ir.Current;
                    if (list.Contains(key) == false)
                    {
                        rKeys[i] = key;
                        i++;
                    }
                }

                Hashtable clusterTbl = Clustered_Contains(rKeys, operationContext);
                ArrayList clusterList = null;

                if (clusterTbl != null && clusterTbl.Count > 0)
                {
                    clusterList = (ArrayList)clusterTbl["items-found"];
                }

                IEnumerator ie = clusterList.GetEnumerator();
                while (ie.MoveNext())
                {
                    list.Add(ie.Current);
                }
            }
            result["items-found"] = list;
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
        /// Determines whether the local cache contains the specified keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>List of keys available in cache</returns>
        private Hashtable Local_Contains(object[] keys, OperationContext operationContext)
        {
            Hashtable tbl = new Hashtable();
            if (_internalCache != null)
            {
                tbl = _internalCache.Contains(keys, operationContext);
            }
            return tbl;
        }
        
        /// <summary>
        /// Determines whether the cluster contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>address of the node that contains the specified key; otherwise, null.</returns>
        private Address Clustered_Contains(object key, OperationContext operationContext)
        {
            Address targetNode = GetNextNode();
            if (targetNode == null) return null;
            return Clustered_Contains(targetNode, key, operationContext);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private Address Safe_Clustered_Contains(object key, OperationContext operationContext)
        {
            try
            {
                return Clustered_Contains(key, operationContext);
            }
            catch (Alachisoft.NGroups.SuspectedException e)
            {
                return Clustered_Contains(key, operationContext);
            }
            catch (Runtime.Exceptions.TimeoutException e)
            {
                return Clustered_Contains(key, operationContext);
            }
        }
        /// <summary>
        /// Determines whether the cluster contains the specified keys.
        /// </summary>
        /// <param name="key">The keys to locate in the cache.</param>
        /// <returns>list of keys and their addresses</returns>
        private Hashtable Clustered_Contains(object[] keys, OperationContext operationContext)
        {
            Address targetNode = GetNextNode();
            return Clustered_Contains(targetNode, keys, operationContext);
        }


        /// <summary>
        /// Hanlde cluster-wide Contain(key(s)) requests.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleContains(object info)
        {
            try
            {
                OperationContext operationContext = null;
                if (info is object[])
                {
                    object[] objs = info as object[];
                    if (objs[0] is object[] && objs.Length > 1)
                        operationContext = objs[1] as OperationContext;

                    if (objs[0] is object[])
                    {
                        return Local_Contains((object[])objs[0], operationContext);
                    }
                    else
                    {
                        if (Local_Contains(objs[0], operationContext))
                            return true;
                    }

                }
                else
                {
                    if (Local_Contains(info, operationContext))
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

        #region /         --- Replicated Cache Search ---          /

        protected override QueryResultSet Local_Search(string queryText, IDictionary values, OperationContext operationContext)
        {
            QueryResultSet resultSet = null;

            if (_internalCache == null)
                throw new InvalidOperationException();

            resultSet = _internalCache.Search(queryText, values, operationContext);

            return resultSet;
        }

        protected override QueryResultSet Local_SearchEntries(string queryText, IDictionary values, OperationContext operationContext)
        {
            QueryResultSet resultSet = null;

            if (_internalCache == null)
                throw new InvalidOperationException();

            resultSet = _internalCache.SearchEntries(queryText, values, operationContext);

            return resultSet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public QueryResultSet handleSearchEntries(object info)
        {
            if (_internalCache != null)
            {
                object[] data = (object[])info;
                OperationContext operationContext = null;
                if (data.Length > 2)
                    operationContext = data[2] as OperationContext;

                return _internalCache.SearchEntries(data[0] as string, data[1] as IDictionary, operationContext);
            }
            return null;
        }

        public QueryResultSet Clustered_SearchEntries(string queryText, IDictionary values, OperationContext operationContext)
        {
            return Clustered_SearchEntries(Cluster.Coordinator, queryText, values, true, operationContext);
        }

        #endregion
               
        #region	/                 --- Replicated ICache.Clear ---           /

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        /// <remarks>
        /// This method invokes <see cref="handleClear"/> on every node in the cluster, 
        /// which then fires OnCacheCleared locally. The <see cref="handleClear"/> method on the 
        /// coordinator will also trigger a cluster-wide notification to the clients.
        /// </remarks>
        public override void Clear(CallbackEntry cbEntry, OperationContext operationContext)
        {
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_statusLatch.IsAnyBitsSet(NodeStatus.Initializing))
            {
                if (_stateTransferTask != null) _stateTransferTask.StopProcessing();
                _statusLatch.WaitForAny(NodeStatus.Running);
            }

            if (_internalCache == null) throw new InvalidOperationException();

            if (Cluster.Servers.Count > 1)
                Clustered_Clear(cbEntry, false, operationContext);
            else
                handleClear(Cluster.LocalAddress, new object[] { cbEntry, operationContext });

          
        }

        /// <summary>
        /// Clears the local cache only. 
        /// </summary>
        private void Local_Clear(Address src, CallbackEntry cbEntry,  OperationContext operationContext)
        {
            if (_internalCache != null)
            {
                _internalCache.Clear(null, operationContext);
                
                UpdateCacheStatistics();
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

        #region	/                 --- Replicated ICache.Get ---           /

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        public override CacheEntry Get(object key, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType access, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCache.Get", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();
            CacheEntry e = null;

           
            if (access == LockAccessType.ACQUIRE || access == LockAccessType.DONT_ACQUIRE)
            {
                if (Cluster.Servers.Count > 1)
                {
                    e = Local_Get(key, false, operationContext);
                    if (e != null)
                    {
                        if (access == LockAccessType.DONT_ACQUIRE)
                        {
                            if (e.IsItemLocked() && !e.CompareLock(lockId))
                            {
                                lockId = e.LockId;
                                lockDate = e.LockDate;
                                e = null;
                            }
                            else
                            {
                                lockDate = e.LockDate; //compare lock does not set the lockdate internally.
                            }
                        }
                        else if (!e.IsLocked(ref lockId, ref lockDate))
                        {
                            if (Clustered_Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext))
                            {
                                e = Local_Get(key,  ref lockId, ref lockDate, lockExpiration, LockAccessType.IGNORE_LOCK, operationContext);
                            }
                        }
                        else
                        {
                            //dont send the entry back if it is locked.
                            e = null;
                        }
                    }
                    else if (_statusLatch.IsAnyBitsSet(NodeStatus.Initializing))
                    {
                        if (access == LockAccessType.ACQUIRE)
                        {
                            if (Clustered_Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext))
                            {
                                e = Clustered_Get(key, ref lockId, ref lockDate, access, operationContext);
                            }
                        }
                        else
                        {
                            e = Clustered_Get(key, ref lockId, ref lockDate, access, operationContext);
                        }
                    }
                }
                else
                {
                    e = Local_Get(key,  ref lockId, ref lockDate, lockExpiration, access, operationContext);
                }
            }
            else
            {
                e = Local_Get(key, operationContext);

                if (e == null && _statusLatch.IsAnyBitsSet(NodeStatus.Initializing))
                {
                    e = Clustered_Get(key, ref lockId, ref lockDate, LockAccessType.IGNORE_LOCK, operationContext);
                }
            }

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
                    Local_Get(key, operationContext); //to update the index locally.
                }
            }
            return e;
        }

        /// <summary>
        /// Retrieve the objects from the cache. An array of keys is passed as parameter.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        public override Hashtable Get(object[] keys, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCache.GetBlk", "");

            if (_internalCache == null) throw new InvalidOperationException();

            Hashtable table = null;

            if (IsInStateTransfer())
            {
                ArrayList dests = GetDestInStateTransfer();
                table = Clustered_Get(dests[0] as Address, keys, operationContext);
            }
            else
            {
                table = Local_Get(keys, operationContext);
            }

            if (table != null)
            {

                ArrayList updateIndiceKeyList = null;
                IDictionaryEnumerator ine = table.GetEnumerator();
                while (ine.MoveNext())
                {
                    CacheEntry e = (CacheEntry)ine.Value;
                    if (e == null)
                    {
                        _stats.BumpMissCount();
                    }
                    else
                    {
                        if (updateIndiceKeyList == null) updateIndiceKeyList = new ArrayList();
                        _stats.BumpHitCount();
                        // update the indexes on other nodes in the cluster
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
            }

            return table;
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
                    Cluster.Multicast(_otherServers, func, GroupRequest.GET_ALL, false);
                }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ReplicatedCache.UpdateIndices()", e.ToString());
            }
        }

        protected override void UpdateIndices(object key, bool async, OperationContext operationContext)
        {
            if (_asyncReplicator != null && Cluster.Servers.Count > 1)
            {
                try
                {
                    _asyncReplicator.AddUpdateIndexKey(key);
                }
                catch (Exception) { }
            }
        }

        protected void UpdateIndices(object[] keys, bool async, OperationContext operationContext)
        {
            if (_asyncReplicator != null && Cluster.Servers.Count > 1)
            {
                try
                {
                    foreach(object key in keys)
                    {
                        UpdateIndices(key, async, operationContext);
                    }
                }
                catch (Exception) { }
            }
        }

        private void RemoveUpdateIndexOperation(object key)
        {
            if (key != null)
            {
                try
                {
                    if (_asyncReplicator != null) _asyncReplicator.RemoveUpdateIndexKey(key);

                }
                catch (Exception) { }
            }
        }

        public override void ReplicateOperations(Array opCodes, Array info, Array userPayLoads, ArrayList compilationInfo, ulong seqId, long viewId)
        {
            try
            {
                if (Cluster.Servers != null && Cluster.Servers.Count > 1)
                {
                    Function func = new Function((int)OpCodes.ReplicateOperations, new object[] { opCodes, info, compilationInfo }, false);
                    func.UserPayload = userPayLoads;
                    Cluster.Multicast(_otherServers, func, GroupRequest.GET_ALL, false);
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

        public object handleReplicateOperations(Address src, object info, Array userPayLoad)
        {
            try
            {
                object[] objs = (Object[])info;
                object[] opCodes = (object[])objs[0];
                object[] keys = (object[])objs[1];
                OperationContext operationContext = null;

                for (int i = 0; i < opCodes.Length; i++)
                {
                    switch ((int)opCodes[i])
                    {
                        case (int)OpCodes.UpdateIndice:
                            object[] data = (Object[])info;
                            if (data != null && data.Length > 3)
                                operationContext = data[3] as OperationContext;
                            return handleUpdateIndice(new object[] { keys[i], operationContext });
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
            return null;
        }

        /// <summary>
        /// Retrieve the object from the cluster. Used during state trasfer, when the cache
        /// is loading state from other members of the cluster.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        private CacheEntry Clustered_Get(object key, ref object lockId, ref DateTime lockDate, LockAccessType accessType, OperationContext operationContext)
        {
            /// Fetch address of a fully functional node. There should always be one
            /// fully functional node in the cluster (coordinator is alway fully-functional).
            Address targetNode = GetNextNode();
            if (targetNode != null)
                return Clustered_Get(targetNode, key, ref lockId, ref lockDate, accessType, operationContext);
            return null;
        }

        private CacheEntry Safe_Clustered_Get(object key, ref object lockId, ref DateTime lockDate, LockAccessType accessType, OperationContext operationContext)
        {
            try
            {
                return Clustered_Get(key, ref lockId, ref lockDate, accessType, operationContext);
            }
            catch (Alachisoft.NGroups.SuspectedException e)
            {
                return Clustered_Get(key, ref lockId, ref lockDate, accessType, operationContext);
            }
            catch (Runtime.Exceptions.TimeoutException e)
            {
                return Clustered_Get(key, ref lockId, ref lockDate, accessType, operationContext);
            }

        }


        /// <summary>
        /// Retrieve the object from the local cache only. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheEntry Local_Get(object key, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            if (_internalCache != null)
                retVal = _internalCache.Get(key, operationContext);
            return retVal;
        }

        private CacheEntry Local_Get(object key, bool isUserOperation, OperationContext operationContext)
        {
            Object lockId = null;
            DateTime lockDate = DateTime.Now;
            CacheEntry retVal = null;
            if (_internalCache != null)
                retVal = _internalCache.Get(key, isUserOperation, ref lockId, ref lockDate, null, LockAccessType.IGNORE_LOCK, operationContext);

            return retVal;
        }

        /// <summary>
        /// Retrieve the object from the local cache only. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        private CacheEntry Local_Get(object key,  ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType access, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            if (_internalCache != null)
                retVal = _internalCache.Get(key, ref lockId, ref lockDate, lockExpiration, access, operationContext);
            return retVal;
        }

        /// <summary>
        /// Retrieve the objects from the local cache only. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        private Hashtable Local_Get(object[] keys, OperationContext operationContext)
        {
            Hashtable retVal = null;
            if (_internalCache != null)
                retVal = _internalCache.Get(keys, operationContext);
            return retVal;
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
                object[] objs = info as object[];
                OperationContext operationContext = null;
                bool isUserOperation = true;
                if (objs.Length > 1) operationContext = objs[1] as OperationContext;
                if (objs.Length > 2) isUserOperation = (bool)objs[2];

                if (objs[0] is object[]) return Local_Get((object[])objs[0], operationContext);
                else
                {
                    CacheEntry entry = Local_Get(objs[0],isUserOperation, operationContext);
                    /* send value and entry seperaty*/
                    OperationResponse opRes = new OperationResponse();
                    if (entry != null)
                    {
                        UserBinaryObject ubObject = (UserBinaryObject)(entry.Value is CallbackEntry ? ((CallbackEntry)entry.Value).Value : entry.Value);
                        opRes.UserPayload = ubObject.Data;
                        opRes.SerializablePayload = entry.CloneWithoutValue();
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


        #region	/                 --- Replicated ICache.Add ---           /


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
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCache.Add_1", "");

            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();
            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Replicated.Add()", "Key = " + key);

            if (Local_Contains(key, operationContext)) return CacheAddResult.KeyExists;
            CacheAddResult result = CacheAddResult.Success;
            Exception thrown = null;
            try
            {
                if (Cluster.Servers.Count > 1)
                {
                    // Try to add to the local node and the cluster.
                    result = Clustered_Add(key, cacheEntry,  operationContext);
                    if (result == CacheAddResult.KeyExists)
                    {
                        return result;
                    }
                }
                else
                    result = Local_Add(key, cacheEntry, Cluster.LocalAddress,  true, operationContext);
            }
            catch (Exception e)
            {
                thrown = e;
            }

            if (result != CacheAddResult.Success || thrown != null)
            {
                bool timeout = false;
                bool rollback = true;
                try
                {
                    if (result == CacheAddResult.FullTimeout)
                    {
                        timeout = true;
                        rollback = false;
                    }
                    if (result == CacheAddResult.PartialTimeout)
                    {
                        timeout = true;
                    }
                    if (rollback)
                    {
                        if (Cluster.Servers.Count > 1)
                        {
                            Clustered_Remove(key, ItemRemoveReason.Removed, null,  false, null, LockAccessType.IGNORE_LOCK, operationContext);
                        }
                        else
                        {
                            Local_Remove(key, ItemRemoveReason.Removed, null, null,  true, null, LockAccessType.IGNORE_LOCK, operationContext);
                        }
                    }
                }
                catch (Exception) { }

                //throw actual exception that was caused due to add operation.
                if (thrown != null) throw thrown;
                if (timeout)
                {
                    throw new Alachisoft.NCache.Common.Exceptions.TimeoutException("Operation timeout.");
                }
            }

            return result;
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

            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Replicated.Add()", "Key = " + key);

            if (Local_Contains(key, operationContext) == false) return false;
            bool result;
             try
            {
                if (Cluster.Servers.Count > 1)
                {
                    // Try to add to the local node and the cluster.
                    result = Clustered_Add(key, eh, operationContext);
                }
                else
                    result = Local_Add(key, eh, operationContext);
            }
            catch (Exception e)
            {
                throw e;
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
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCache.AddBlk", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();
            
            Hashtable addResult = new Hashtable();
            Hashtable tmp = new Hashtable();
           
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
                    addResult[ie.Current] = new OperationFailedException("The specified key already exists.");
                }

                // all keys failed, so return.
                if (failCount == keys.Length)
                {
                    return addResult;
                }

                object[] newKeys = new object[keys.Length - failCount];
                CacheEntry[] entries = new CacheEntry[keys.Length - failCount];

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

            Exception thrown = null;
            try
            {
                if (Cluster.Servers.Count > 1)
                {
                    // Try to add to the local node and the cluster.
                    tmp = Clustered_Add(keys, cacheEntries, operationContext);
                }
                else
                {
                    tmp = Local_Add(keys, cacheEntries, Cluster.LocalAddress,true, operationContext);
                }
            }
            catch (Exception inner)
            {
                Context.NCacheLog.Error("Replicated.Clustered_Add()", inner.ToString());
                for (int i = 0; i < keys.Length; i++)
                {
                    tmp[keys[i]] = new OperationFailedException(inner.Message, inner);
                }
                thrown = inner;
            }


            if (thrown != null)
            {
                if (Cluster.Servers.Count > 1)
                {
                    Clustered_Remove(keys, ItemRemoveReason.Removed, operationContext);
                }
                else
                {
                    Local_Remove(keys, ItemRemoveReason.Removed, null, null, false, operationContext);
                }
                
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

                    if (Cluster.Servers.Count > 1)
                    {
                        Clustered_Remove(keysToRemove, ItemRemoveReason.Removed, null, false, operationContext);
                    }
                    else
                    {
                        Local_Remove(keysToRemove, ItemRemoveReason.Removed, null, null, false, operationContext);
                    }
                }
          
            
            }

            return addResult;
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
        private CacheAddResult Local_Add(object key, CacheEntry cacheEntry, Address src,  bool notify, OperationContext operationContext)
        {
            CacheAddResult retVal = CacheAddResult.Failure;

            if (_internalCache != null)
            {
                try
                {
                    retVal = _internalCache.Add(key, cacheEntry, notify, operationContext);
                }
                catch (Exception e)
                {
                    throw;
                }
            }

            return retVal;
        }


        /// <summary>
        /// Add the ExpirationHint against the given key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hint"></param>
        /// <returns></returns>
        private bool Local_Add(object key, ExpirationHint hint, OperationContext operationContext)
        {
            bool retVal = false;
            if (_internalCache != null)
            {
                try
                {
                    retVal = _internalCache.Add(key, hint, operationContext);
                }
                catch (Exception e)
                {
                    throw e;
                }
                           
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
        private Hashtable Local_Add(object[] keys, CacheEntry[] cacheEntries, Address src,  bool notify, OperationContext operationContext)
        {
            Hashtable table = new Hashtable();
            
            if (_internalCache != null)
            {
                table = _internalCache.Add(keys, cacheEntries, notify, operationContext);
            }

           return table;
        }

        /// <summary>
        /// Add the object to the cluster. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAdd"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// </remarks>
        private CacheAddResult Clustered_Add(object key, CacheEntry cacheEntry,  OperationContext operationContext)
        {
            return Clustered_Add(Cluster.Servers, key, cacheEntry,  operationContext);
        }

        /// <summary>
        /// Add the ExpirationHint to the cluster. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAddHint"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// </remarks>
        private bool Clustered_Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
            return Clustered_Add(Cluster.Servers, key, eh, operationContext);
        }

        /// <summary>
        /// Add the object to the cluster. 
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        /// <remarks>
        /// This method invokes <see cref="handleAdd"/> on every server-node in the cluster. If the operation
        /// fails on any one node the whole operation is considered to have failed and is rolled-back.
        /// </remarks>
        private Hashtable Clustered_Add(object[] keys, CacheEntry[] cacheEntries,  OperationContext operationContext)
        {
            return Clustered_Add(Cluster.Servers, keys, cacheEntries,  operationContext);
        }

        private object handleUpdateIndice(object key)
        {
            //we do a get operation on the item so that its relevent index in epxiration/eviction
            //is updated.
            handleGet(key);
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
                OperationContext oc = null;

                if (objs.Length > 2)
                    oc = objs[2] as OperationContext;

                if (objs[0] is object[])
                {
                    object[] keys = (object[])objs[0];
                    CacheEntry[] entries = objs[1] as CacheEntry[];
                        
                    Hashtable results = Local_Add(keys, entries, src, true, oc);

                    return results;
                }
                else
                {
                    CacheAddResult result = CacheAddResult.Failure;
                    object key = objs[0];
                    
                    CacheEntry e = objs[1] as CacheEntry;
                    e.Value = userPayload;
                    result = Local_Add(key, e, src,  true, oc);
                    return result;
                }
            }
            catch (Exception e)
            {
                if (_clusteredExceptions) throw;
            }
            return CacheAddResult.Failure;
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
                OperationContext oc = objs[2] as OperationContext;
                return Local_Add(key, eh, oc);
            }
            catch (Exception e)
            {
                if (_clusteredExceptions) throw;
            }
            return false;
        }

        

        #endregion

        #region	/                 --- Replicated ICache.Insert ---           /

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
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId,  LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCache.Insert", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Replicated.Insert()", "Key = " + key);

            CacheEntry pEntry = null;
            CacheInsResultWithEntry retVal = new CacheInsResultWithEntry();
            Exception thrown = null;

            try
            {
                // We get the actual item to raise custom call back with the item.
                //Get internally catters for the state-transfer scenarios. 
                pEntry = Get(key, operationContext);
                retVal.Entry = pEntry;

                if (pEntry != null)
                {
                    if (accessType != LockAccessType.IGNORE_LOCK)
                    {
                        if (pEntry.IsItemLocked() && !pEntry.CompareLock(lockId))
                        {
                            //throw new LockingException("Item is locked.");
                            retVal.Entry = null;
                            retVal.Result = CacheInsResult.ItemLocked;
                            return retVal;
                        }
                    }
                }
                if (Cluster.Servers.Count > 1)
                {
                    // Try to add to the local node and the cluster.
                    retVal = Clustered_Insert(key, cacheEntry, lockId, accessType, operationContext);

                    //if coordinator has sent the previous entry, use that one...
                    //otherwise send back the localy got previous entry...
                    if (retVal.Entry != null)
                        pEntry = retVal.Entry;
                    else
                        retVal.Entry = pEntry;
                }
                else
                    retVal = Local_Insert(key, cacheEntry, Cluster.LocalAddress,  true, lockId,  accessType, operationContext);
            }
            catch (Exception e)
            {
                thrown = e;
            }

            // Try to insert to the local node and the cluster.
            if ((retVal.Result == CacheInsResult.NeedsEviction || retVal.Result == CacheInsResult.Failure || retVal.Result == CacheInsResult.FullTimeout || retVal.Result == CacheInsResult.PartialTimeout) || thrown != null)
            {
                Context.NCacheLog.Warn("Replicated.Insert()", "rolling back, since result was " + retVal.Result);
                bool rollback = true;
                bool timeout = false;
                if (retVal.Result == CacheInsResult.PartialTimeout)
                {
                    timeout = true;
                }
                else if (retVal.Result == CacheInsResult.FullTimeout)
                {
                    timeout = true;
                    rollback = false;
                }

                if (rollback)
                {
                    Thread.Sleep(2000);
                    /// failed on the cluster, so remove locally as well.
                    if (Cluster.Servers.Count > 1)
                    {
                        Clustered_Remove(key, ItemRemoveReason.Removed, null, false, null,  LockAccessType.IGNORE_LOCK, operationContext);
                    }
                }
                if (timeout)
                {
                    throw new Runtime.Exceptions.TimeoutException("Operation timeout.");

                }
                if (thrown != null) throw thrown;
            }
            if (notify && retVal.Result == CacheInsResult.SuccessOverwrite)
            {
                 RemoveUpdateIndexOperation(key);
            }

          return retVal;
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
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCache.InsertBlk", "");
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            Hashtable insertResults = null;
                      
            if (Cluster.Servers.Count > 1)
                insertResults = Clustered_Insert(keys, cacheEntries,  notify, operationContext);
            else
            {
                Hashtable pEntries = null;

                pEntries = Get(keys, operationContext); //dont remove

                Hashtable existingItems;
                Hashtable jointTable = new Hashtable();
                Hashtable failedTable = new Hashtable();
                ArrayList inserted = new ArrayList();
                ArrayList added = new ArrayList();
               
                object[] validKeys;
                CacheEntry[] validEnteries;
                int index = 0;
                object key;
       

                for (int i = 0; i < keys.Length; i++)
                {
                    jointTable.Add(keys[i], cacheEntries[i]);
                }

                existingItems = Local_Get(keys, operationContext);
                if (existingItems != null && existingItems.Count > 0)
                {
                    IDictionaryEnumerator ide;
                    if (existingItems != null)
                    {
                        index = 0;
                        validKeys = new object[existingItems.Count];
                        validEnteries = new CacheEntry[existingItems.Count];
                        ide = existingItems.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            key = ide.Key;
                            validKeys[index] = key;
                            validEnteries[index] = (CacheEntry)jointTable[key];
                            jointTable.Remove(key);
                            inserted.Add(key);
                            index += 1;
                        }

                        if (validKeys.Length > 0)
                        {
                            try
                            {
                                insertResults = Local_Insert(validKeys, validEnteries, Cluster.LocalAddress, true, operationContext);
                            }
                            catch (Exception e)
                            {
                                Context.NCacheLog.Error("ReplicatedServerCache.Insert(Keys)", e.ToString());
                                for (int i = 0; i < validKeys.Length; i++)
                                {
                                    failedTable.Add(validKeys[i], e);
                                    inserted.Remove(validKeys[i]);
                                }
                                Clustered_Remove(validKeys, ItemRemoveReason.Removed, null, false, operationContext);
                            }

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
                                                failedTable[ie.Key] = new OperationFailedException("The cache is full and not enough items could be evicted.");
                                                break;
                                        }
                                    }
                                }        
                            }
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
                        index += 1;
                    }
                    localInsertResult = Local_Insert(validKeys, validEnteries, Cluster.LocalAddress,  notify, operationContext);
                }

                if (localInsertResult != null)
                {
                    IDictionaryEnumerator ide = localInsertResult.GetEnumerator();
                    CacheInsResultWithEntry result = null;
                    while (ide.MoveNext())
                    {
                        if (ide.Value is Exception)
                        {
                            failedTable.Add(ide.Key, ide.Value);
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
                        }
                    }
                }
              
                if (notify)
                {
                    IEnumerator ideInsterted = inserted.GetEnumerator();
                    while (ideInsterted.MoveNext())
                    {
                        key = ideInsterted.Current;
                        RemoveUpdateIndexOperation(key);
                    }
                }
                insertResults = failedTable;
            }
            return insertResults;
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
        private CacheInsResultWithEntry Local_Insert(object key, CacheEntry cacheEntry, Address src,  bool notify, object lockId,  LockAccessType accessType, OperationContext operationContext)
        {
            CacheInsResultWithEntry retVal = new CacheInsResultWithEntry();
            try
            {
                if (_internalCache != null)
                {
                    retVal = _internalCache.Insert(key, cacheEntry, notify, lockId,accessType, operationContext);
                }
            }
            catch (Exception e)
            {
                if (_clusteredExceptions) throw;
            }
            return retVal;
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
        private Hashtable Local_Insert(object[] keys, CacheEntry[] cacheEntries, Address src,  bool notify, OperationContext operationContext)
        {
            Hashtable retVal = null;
            try
            {
                if (_internalCache != null)
                {
                    retVal = _internalCache.Insert(keys, cacheEntries, notify, operationContext);
                }
            }
            catch (Exception e)
            {
                if (_clusteredExceptions) throw;
            }
            return retVal;
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
        private CacheInsResultWithEntry Clustered_Insert(object key, CacheEntry cacheEntry, object lockId, LockAccessType accesssType, OperationContext operationContext)
        {
            return Clustered_Insert(Cluster.Servers, key, cacheEntry, lockId, accesssType, operationContext);
        }

        /// <summary>
        /// Add the objects to the cluster. 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="cacheEntries"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method invokes <see cref="handleInsert"/> on every server-node in the cluster. If 
        /// the operation fails on any one node the whole operation is considered to have failed 
        /// and is rolled-back.
        /// </remarks>
        protected override Hashtable Clustered_Insert(object[] keys, CacheEntry[] cacheEntries, OperationContext operationContext)
        {
            return Clustered_Insert(Cluster.Servers, keys, cacheEntries, operationContext);
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="info">the object containing parameters for this operation.</param>
        /// <returns>object to be sent back to the requestor.</returns>
        private object handleInsert(Address src, object info, Array userPayload)
        {
            try
            {
                object[] objs = (object[])info;
                bool returnEntry = false;
                OperationContext operationContext = null;

                // operation Context
                if (objs.Length == 3)
                {
                    operationContext = objs[2] as OperationContext;
                }
                
                //if client node is requesting for the previous cache entry
                //then cluster coordinator must send it back...
                if (objs.Length == 6)
                {
                    returnEntry = (bool)objs[2] && Cluster.IsCoordinator;
                    operationContext = objs[5] as OperationContext;
                }

                if (objs[0] is object[])
                {
                    object[] keys = (object[])objs[0];
                    CacheEntry[] entries = objs[1] as CacheEntry[];
                    return Local_Insert(keys, entries, src,  true, operationContext);
                }
                else
                {
                    object key = objs[0];
                    CacheEntry e = objs[1] as CacheEntry;
                    e.Value = userPayload;
                    object lockId = null;
                    LockAccessType accessType = LockAccessType.IGNORE_LOCK;
                
                    if (objs.Length == 6)
                    {
                        lockId = objs[3];
                        accessType = (LockAccessType)objs[4];
                    }
                    CacheInsResultWithEntry resultWithEntry = Local_Insert(key, e, src, true, lockId,  accessType, operationContext);


                    /* send value and entry seperately*/
                    OperationResponse opRes = new OperationResponse();
                    if (resultWithEntry.Entry != null)
                    {
                        if (resultWithEntry.Entry.Value is CallbackEntry)
                        {
                            opRes.UserPayload = null;
                        }
                        else
                        {
                            //we need not to send this entry back... it is needed only for custom notifications and/or key dependencies...
                            opRes.UserPayload = null;
                        }

                        if (returnEntry)
                            resultWithEntry.Entry = resultWithEntry.Entry.CloneWithoutValue() as CacheEntry;
                        else
                            resultWithEntry.Entry = null;
                    }

                    opRes.SerializablePayload = resultWithEntry;
                    return opRes;
                }
            }
            catch (Exception e)
            {
                if (_clusteredExceptions) throw;
            }
            return CacheInsResult.Failure;
        }

        #endregion

        #region	/                 --- Replicated ICache.Remove ---           /

        public override object RemoveSync(object[] keys, ItemRemoveReason reason, bool notify, OperationContext operationContext)
        {
            object result = null;
            try
            {
                if (Cluster.Servers.Count > 1)
                    result = Clustered_Remove(keys, reason, operationContext);
                else
                    result = handleRemoveRange(new object[] { keys, reason, operationContext });
            }
            catch (Runtime.Exceptions.TimeoutException)
            {
                //we retry the operation.
                Thread.Sleep(2000);
                if (Cluster.Servers.Count > 1)
                    result = Clustered_Remove(keys, reason, operationContext);
                else
                    result = handleRemoveRange(new object[] { keys, reason, operationContext });
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ReplicatedCache.RemoveSync", e.ToString());
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
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, object lockId,  LockAccessType accessType, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCache.Remove", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();

            object actualKey = key;
            CallbackEntry cbEntry = null;
           
            if (key is object[])
            {
                object[] package = key as object[];
                actualKey = package[0];
                cbEntry = package[2] as CallbackEntry;
            }

            CacheEntry e = null;

            if (accessType != LockAccessType.IGNORE_LOCK)
            {
                //Get internally catters for the state-transfer scenarios.
                e = Get(key, operationContext);
                if (e != null)
                {
                    if (e.IsItemLocked() && !e.CompareLock(lockId))
                    {
                        //this exception directly goes to user. 
                        throw new LockingException("Item is locked.");
                    }
                }
            }       
            try
            {
                if (Cluster.Servers.Count > 1)
                    e = Clustered_Remove(actualKey, ir, cbEntry, false, lockId, accessType, operationContext);
                else
                    e = Local_Remove(actualKey, ir, Cluster.LocalAddress, cbEntry,  true, lockId, accessType, operationContext);
            }
            catch (Runtime.Exceptions.TimeoutException)
            {
                Thread.Sleep(2000);

                if (Cluster.Servers.Count > 1)
                    e = Clustered_Remove(actualKey, ir, cbEntry,  false, lockId,  accessType, operationContext);
                else
                    e = Local_Remove(actualKey, ir, Cluster.LocalAddress, cbEntry,  true, lockId, accessType, operationContext);
            }
          
            if (e != null && notify)
            {
           
                RemoveUpdateIndexOperation(key);
            }

           return e;
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
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCache.RemoveBlk", "");
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null) throw new InvalidOperationException();
                      
            CallbackEntry cbEntry = null;
         

            if (keys[0] is object[])
            {
                object[] package = keys[0] as object[];
                keys[0] = package[0];
                
                cbEntry = package[2] as CallbackEntry;
            }     

            Hashtable removed = null;
            if (Cluster.Servers.Count > 1)
                removed = Clustered_Remove(keys, ir, cbEntry,  false, operationContext);
            else
                removed = Local_Remove(keys, ir, Cluster.LocalAddress, cbEntry, true, operationContext);

            if (removed.Count > 0)
            {
                IDictionaryEnumerator ide = removed.GetEnumerator();
                while (ide.MoveNext())
                {
                    object key = ide.Key;
                    CacheEntry e = (CacheEntry)ide.Value;
                    if (e != null)
                    {
                        RemoveUpdateIndexOperation(ide.Key);
                      
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
        private CacheEntry Local_Remove(object key, ItemRemoveReason ir, Address src, CallbackEntry cbEntry, bool notify, object lockId,  LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = null;
            if (_internalCache != null)
            {
                retVal = _internalCache.Remove(key, ir, notify, lockId,  accessType, operationContext);
            }
            return retVal;
        }


        /// <summary>
        /// Remove the objects from the local cache only. 
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="ir"></param>
        /// <param name="notify"></param>
        /// <returns>keys and values that actualy removed from the cache</returns>
        private Hashtable Local_Remove(IList keys, ItemRemoveReason ir, Address src, CallbackEntry cbEntry, bool notify, OperationContext operationContext)
        {
            Hashtable retVal = null;
            if (_internalCache != null)
            {
                retVal = _internalCache.Remove(keys, ir, notify, operationContext);
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
                    CallbackEntry cbEntry = null;
                    OperationContext operationContext = null;

                    if (args.Length > 3)
                        cbEntry = args[3] as CallbackEntry;

                    if (args.Length > 6)
                        operationContext = args[6] as OperationContext;
                    else if (args.Length > 4)
                        operationContext = args[4] as OperationContext;
                    else if (args.Length > 2)
                        operationContext = args[2] as OperationContext;
                    if (args != null && args.Length > 0)
                    {
                        object tmp = args[0];
                        if (tmp is Object[])
                        {
                            result = Local_Remove((object[])tmp, ItemRemoveReason.Removed, src, cbEntry,  true, operationContext);
                        }
                        else
                        {
                            object lockId = args[4];
                            LockAccessType accessType = (LockAccessType)args[5];

                            CacheEntry entry = Local_Remove(tmp, ItemRemoveReason.Removed, src, cbEntry,  true, lockId,  accessType, operationContext);
                            /* send value and entry seperaty*/
                            OperationResponse opRes = new OperationResponse();
                            if (entry != null)
                            {
                                opRes.UserPayload = (entry.Value is CallbackEntry ? ((CallbackEntry)entry.Value).UserData : entry.UserData);
                                opRes.SerializablePayload = entry.CloneWithoutValue();
                            }
                            result = opRes;
                        }
                    }
                }

                /// Only the coordinator returns the object, this saves a lot of traffic because
                /// only one reply actually contains the object other replies are simply dummy objects.
                if (Cluster.IsCoordinator)
                    return result;
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
                OperationContext operationContext = null;
                if (objs.Length > 2)
                    operationContext = objs[2] as OperationContext;

                if (objs[0] is object[])
                {
                    object[] keys = (object[])objs[0];
                    ItemRemoveReason ir = (ItemRemoveReason)objs[1];

                    Hashtable totalRemovedItems = new Hashtable();
                    IDictionaryEnumerator ide = null;

                    if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Replicated.handleRemoveRange()", "Keys = " + keys.Length.ToString());

                    Hashtable removedItems = Local_Remove(keys, ir, null, null, true, operationContext);

                    if (removedItems != null)
                    {
                        totalRemovedItems = removedItems;
                        Hashtable cascRemovedItems = InternalCache.Cascaded_remove(removedItems, ir, true, false, operationContext);
                        if (cascRemovedItems != null && cascRemovedItems.Count > 0)
                        {
                            ide = cascRemovedItems.GetEnumerator();
                            while (ide.MoveNext())
                            {
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

        #endregion

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

            if ((_statusLatch.IsAnyBitsSet(NodeStatus.Initializing)))
                return Clustered_GetEnumerator(Cluster.Coordinator.Clone() as Address);

            return new LazyKeysetEnumerator(this, (object[])handleKeyList(), false);
        }

        public override EnumerationDataChunk GetNextChunk(EnumerationPointer pointer, OperationContext operationContext)
        {
            /* All clustered operation have been removed as they use to return duplicate keys because the snapshots
            created on all the nodes of replicated for a particular enumerator were not sorted and in case of node up and down we might 
            get duplicate keys when a request is routed to another client. As per discussion with iqbal sahab it has been decided that 
            whenever a node leaves the cluster we will throw Enumeration has been modified exception to the client.*/
           
            /// Wait until the object enters any running status
            _statusLatch.WaitForAny(NodeStatus.Initializing | NodeStatus.Running);

            if (_internalCache == null)
                throw new InvalidOperationException();

            EnumerationDataChunk nextChunk = null;

            if (Cluster.Servers.Count > 1)
            {

                //Enumeration Pointer came to this node on rebalance of clients and node is currently in state transfer or pointer came to this node and node is intitialized but doesnt have snapshot
                if ((_statusLatch.IsAnyBitsSet(NodeStatus.Initializing) && (pointer.ChunkId > 0 || !InternalCache.HasEnumerationPointer(pointer)))
                    || (!_statusLatch.IsAnyBitsSet(NodeStatus.Initializing) && pointer.ChunkId > 0 && !InternalCache.HasEnumerationPointer(pointer)))
                {
                    throw new OperationFailedException("Enumeration Has been Modified");
                }
                else //Node is initialized with data and has snapshot should return the next chunk locally
                {
                    nextChunk = InternalCache.GetNextChunk(pointer, operationContext);
                }

                //Dispose the pointer on all nodes as this is the last chunk for this particular enumeration pointer
                if (pointer.IsSocketServerDispose && nextChunk == null)
                {                    
                    pointer.isDisposable = true;
                    InternalCache.GetNextChunk(pointer, operationContext);
                }
                else if (nextChunk.IsLastChunk)
                {
                    pointer = nextChunk.Pointer;
                    pointer.isDisposable = true;
                    InternalCache.GetNextChunk(pointer, operationContext);
                }
            }
            else if (pointer.ChunkId > 0 && !InternalCache.HasEnumerationPointer(pointer))
            {
                throw new OperationFailedException("Enumeration Has been Modified");
            }
            else
            {
                nextChunk = InternalCache.GetNextChunk(pointer, operationContext);
            }

            return nextChunk;
        }

        private EnumerationDataChunk Clustered_GetNextChunk(ArrayList dests, EnumerationPointer pointer, OperationContext operationContext)
        {
            try
            {
                Function func = new Function((int)OpCodes.GetNextChunk, new object[] { pointer, operationContext }, false);

                RspList results = Cluster.BroadcastToMultiple(dests,
                   func,
                   GroupRequest.GET_ALL, false);

                ClusterHelper.ValidateResponses(results, typeof(EnumerationDataChunk), Name);

                /// Get a single resulting enumeration data chunk from a request
                EnumerationDataChunk nextChunk = ClusterHelper.FindAtomicEnumerationDataChunkReplicated(results) as EnumerationDataChunk;

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

        private EnumerationDataChunk handleGetNextChunk(Address src, object info)
        {
            object[] package = info as object[];
            EnumerationPointer pointer = package[0] as EnumerationPointer;
            OperationContext operationContext = package[1] as OperationContext;

            //if a request for a an enumerator whos in middle of its enumeration comes to coordinator
            //and we cannot find it we will throw enumeration modified exception.
            if (this.Cluster.IsCoordinator && pointer.ChunkId > 0 && !pointer.IsSocketServerDispose && !InternalCache.HasEnumerationPointer(pointer))
            {
                throw new OperationFailedException("Enumeration Has been Modified");
            }

            EnumerationDataChunk nextChunk = InternalCache.GetNextChunk(pointer, operationContext);
            return nextChunk;
        }

        #endregion

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

        public override object handleUnregisterKeyNotification(object operand)
        {
            OperationContext operationContext = null;
            object[] operands = operand as object[];
            if (operands != null)
            {
                object Keys = operands[0];
                CallbackInfo updateCallback = operands[1] as CallbackInfo;
                CallbackInfo removeCallback = operands[2] as CallbackInfo;
                if (operands.Length > 2)
                    operationContext = operands[3] as OperationContext;

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

        #region	/                 --- OnCustomUpdateCallback ---           /

        /// <summary> 
        /// handler for item update callback event.
        /// </summary>
        void ICacheEventsListener.OnCustomUpdateCallback(object key, object value, OperationContext operationContext, EventContext eventContext)
        {
            // specially handled in Add.
            try
            {
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Replicated.OnCustomUpdateCallback()", "");

                if (value != null && value is ArrayList)
                {
                    ArrayList itemUpdateCallbackListener =(ArrayList)value;

                    if (Cluster.IsCoordinator && _nodesInStateTransfer.Count > 0)
                    {
                        Object data = new object[] { key, itemUpdateCallbackListener.Clone(),operationContext,eventContext };
                        RaiseUpdateCallbackNotifier((ArrayList)_nodesInStateTransfer.Clone(), data);
                    }

                    if (_allowEventRaiseLocally)
                    {
                        NotifyCustomUpdateCallback(key, itemUpdateCallbackListener, true, operationContext, eventContext);
                    }
                }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Warn("Replicated.OnCustomUpdated", "failed: " + e.ToString());
            }
            finally
            {
                UpdateCacheStatistics();
            }
        }

        private void RaiseUpdateCallbackNotifier(ArrayList servers, Object data)
        {
            try
            {
                if (Cluster.Servers != null && Cluster.Servers.Count > 1)
                {
                    Function func = new Function((int)OpCodes.NotifyCustomUpdateCallback, data);
                    Cluster.Multicast(servers, func, GroupRequest.GET_NONE, false, Cluster.Timeout);
                }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ReplicatedCache.CustomUpdateCallback()", e.ToString());
            }
        }

        private object handleNotifyUpdateCallback(object info)
        {
            object[] objs = (object[])info;
            ArrayList callbackListeners = objs[1] as ArrayList;
            NotifyCustomUpdateCallback(objs[0], callbackListeners, true, (OperationContext)objs[2], (EventContext)objs[3]);
            return null;
        }
        #endregion

        #region	/                 --- OnCustomRemoveCallback ---           /

        /// <summary> 
        /// handler for item remove callback event.
        /// </summary>
        void ICacheEventsListener.OnCustomRemoveCallback(object key, object value, ItemRemoveReason removalReason, OperationContext operationContext, EventContext eventContext)
        {
            try
            {
                // do not notify if explicitly removed by Remove()
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Replicated.OnCustomRemoveCallback()", "items evicted/expired, now replicating");

                CacheEntry entry = value as CacheEntry;
                CallbackEntry cbEntry = entry != null ? entry.Value as CallbackEntry : null;
                if (cbEntry != null )
                {


                    if (Cluster.IsCoordinator && _nodesInStateTransfer.Count > 0)
                    {
                        Object data = new object[] { key, removalReason, operationContext, eventContext };
                        RaiseRemoveCallbackNotifier((ArrayList)_nodesInStateTransfer.Clone(), data);
                    }

                    if (_allowEventRaiseLocally)
                    {
                        NotifyCustomRemoveCallback(key, entry, removalReason, true, operationContext, eventContext);
                    }
                }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Warn("Replicated.OnItemsRemoved", "failed: " + e.ToString());
            }
            finally
            {
                UpdateCacheStatistics();
            }
        }


        private void RaiseRemoveCallbackNotifier(ArrayList servers, Object data)
        {
            try
            {
                if (Cluster.Servers != null && Cluster.Servers.Count > 1)
                {
                    Function func = new Function((int)OpCodes.NotifyCustomRemoveCallback, data);
                    Cluster.Multicast(servers, func, GroupRequest.GET_NONE, false, Cluster.Timeout);
                }
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("ReplicatedCache.CustomRemoveCallback()", e.ToString());
            }
        }

        private object handleNotifyRemoveCallback(object info)
        {
            object[] objs = (object[])info;
            NotifyCustomRemoveCallback(objs[0], null, (ItemRemoveReason)objs[1], true, (OperationContext)objs[2], (EventContext)objs[3]);
            return null;
        }
        #endregion

        #endregion


        #region Lock

        private bool Local_CanLock(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            CacheEntry e = Get(key, operationContext);
            if (e != null)
            {
                return !e.IsLocked(ref lockId, ref lockDate);
            }
            else
            {
                lockId = null;
            }
            return false;
        }

        public override LockOptions Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCache.Lock", "");
            if (Cluster.Servers.Count > 1)
            {
                Clustered_Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
                return new LockOptions(lockId, lockDate);
            }
            else
            {
                return Local_Lock(key, lockExpiration, lockId, lockDate, operationContext);
            }
        }

        private LockOptions handleLock(object info)
        {
            object[] param = (object[])info;
            OperationContext operationContext = null;
            if (param.Length > 4)
                operationContext = param[4] as OperationContext;

            return Local_Lock(param[0], (LockExpiration)param[3], param[1], (DateTime)param[2], operationContext);
        }

        private LockOptions Local_Lock(object key, LockExpiration lockExpiration, object lockId, DateTime lockDate, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
            return null;
        }

        public override void UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RepCache.Unlock", "");
            if (Cluster.Servers.Count > 1)
            {
                Clustered_UnLock(key, lockId, isPreemptive, operationContext);
            }
            else
            {
                Local_UnLock(key, lockId, isPreemptive, operationContext);
            }
        }

        private void Local_UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            if (_internalCache != null)
                _internalCache.UnLock(key, lockId, isPreemptive, operationContext);
        }

        private void handleUnLockKey(object info)
        {
            object[] package = info as object[];
            if (package != null)
            {
                object key = package[0];
                object lockId = package[1];
                bool isPreemptive = (bool)package[2];
                OperationContext operationContext = null;
                if (package.Length > 3)
                    operationContext = package[3] as OperationContext;

                Local_UnLock(key, lockId, isPreemptive, operationContext);
            }
        }

        public override LockOptions IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            if (Cluster.Servers.Count > 1)
            {
                return Clustered_IsLocked(key, ref lockId, ref lockDate, operationContext);
            }
            else
            {
                return Local_IsLocked(key, lockId, lockDate, operationContext);
            }
        }

        private LockOptions handleIsLocked(object info)
        {
            object[] param = (object[])info;
            OperationContext operationContext = null;
            if (param.Length > 3)
                operationContext = param[3] as OperationContext;

            return Local_IsLocked(param[0], param[1], (DateTime)param[2], operationContext);
        }

        private LockOptions Local_IsLocked(object key, object lockId, DateTime lockDate, OperationContext operationContext)
        {
            if (_internalCache != null)
                return _internalCache.IsLocked(key, ref lockId, ref lockDate, operationContext);
            return null;
        }
        #endregion


        internal override void StopServices()
        {
            _statusLatch.SetStatusBit(0, NodeStatus.Initializing | NodeStatus.Running);
            if (_asyncReplicator != null)
                _asyncReplicator.Dispose();
            base.StopServices();
        }

        /// <summary>
        /// Retrieve the list of keys from the cache based on the specified query.
        /// </summary>
        public override QueryResultSet Search(string query, IDictionary values, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            QueryResultSet result = null;
            ArrayList dests = new ArrayList();

            if (IsInStateTransfer())
            {
                result = Clustered_Search(GetDestInStateTransfer(), query, values, operationContext, false);
            }
            else
            {
                result = Local_Search(query, values, operationContext);
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

            
            if (IsInStateTransfer())
            {
                result = Clustered_SearchEntries(GetDestInStateTransfer(), query, values, operationContext,false);
            }
            else
            {
                result = Local_SearchEntries(query, values, operationContext);
            }

            if (Cluster.Servers.Count > 1)
            {

                if (result != null)
                {
                    if (result.UpdateIndicesKeys != null)
                    {
                        UpdateIndices(result.UpdateIndicesKeys.ToArray(), true, operationContext);
                    }
                }

            }

            return result;
        }


        public override bool IsInStateTransfer()
        {
            return _statusLatch.IsAnyBitsSet(NodeStatus.Initializing);
        }

        protected override bool VerifyClientViewId(long clientLastViewId)
        {
            return true;
        }

        protected override ArrayList GetDestInStateTransfer()
        {
            ArrayList list = new ArrayList();
            list.Add(this.Cluster.Coordinator);
            return list;
        }

        #region--------------------------------Cache Data Reader----------------------------------------------
        /// <summary>
        /// Open reader based on the query specified.
        /// </summary>
        public override ClusteredList<ReaderResultSet> ExecuteReader(string query, IDictionary values, bool getData, int chunkSize, bool isInproc, OperationContext operationContext)
        {
            if (_internalCache == null) throw new InvalidOperationException();

            ClusteredList<ReaderResultSet> resultList = new ClusteredList<ReaderResultSet>();


            if (IsInStateTransfer())//open reader on cordinator node
            {
                resultList = Clustered_ExecuteReader(GetDestInStateTransfer(), query, values, getData, chunkSize, operationContext);
            }
            else
            {
                ReaderResultSet result = _internalCache.Local_ExecuteReader(query, values, getData, chunkSize, isInproc, operationContext);
                resultList.Add(result);
            }
            return resultList;
        }

        #endregion
       
    }

}


