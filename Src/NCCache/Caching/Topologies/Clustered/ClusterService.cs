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
using System.IO;

using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NGroups;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Stack;
using Alachisoft.NGroups.Util;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.Mirroring;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// Hold cluster stats
    /// </summary>
    

    /// <summary>
    /// A class to serve as the base for all clustered cache implementations.
    /// </summary>
    internal class ClusterService : RequestHandler, MembershipListener, MessageListener, MessageResponder, IDisposable
    {
        #region	/                 --- Inner/Nested Classes ---           /

        /// <summary>
        /// Asynchronous broadcast event.
        /// </summary>
        internal class AsyncBroadCast : AsyncProcessor.IAsyncTask
        {
            /// <summary> The partition base class </summary>
            private ClusterService _disp = null;

            /// <summary> Message to broadcast </summary>
            private object _data = null;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="disp"></param>
            /// <param name="data"></param>
            public AsyncBroadCast(ClusterService disp, object data)
            {
                _disp = disp;
                _data = data;
            }
            /// <summary>
            /// Implementation of message sending.
            /// </summary>
            void AsyncProcessor.IAsyncTask.Process()
            {
                _disp.SendNoReplyMessage(_data);
            }
        }

        /// <summary>
        /// Asynchronous unicast event.
        /// </summary>
        internal class AsyncUnicasCast : AsyncProcessor.IAsyncTask
        {
            /// <summary> The partition base class </summary>
            private ClusterService _disp = null;

            /// <summary> Message to broadcast </summary>
            private object _data = null;

            /// <summary> Destination of the message</summary>
            private Address _dest = null;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="dest">destination</param>
            /// <param name="disp"></param>
            /// <param name="data"></param>
            public AsyncUnicasCast(Address dest, ClusterService disp, object data)
            {
                _disp = disp;
                _data = data;
                _dest = dest;
            }
            /// <summary>
            /// Implementation of message sending.
            /// </summary>
            void AsyncProcessor.IAsyncTask.Process()
            {
                _disp.SendNoReplyMessage(_dest, _data);
            }
        }

        /// <summary>
        /// Asynchronous unicast event.
        /// </summary>
        internal class AsyncMulticastCast : AsyncProcessor.IAsyncTask
        {
            /// <summary> The partition base class </summary>
            private ClusterService _disp = null;

            /// <summary> Message to broadcast </summary>
            private object _data = null;

            /// <summary> Destination of the message</summary>
            private ArrayList _dests = null;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="dest">destination</param>
            /// <param name="disp"></param>
            /// <param name="data"></param>
            public AsyncMulticastCast(ArrayList dest, ClusterService disp, object data)
            {
                _disp = disp;
                _data = data;
                _dests = dest;
            }
            /// <summary>
            /// Implementation of message sending.
            /// </summary>
            void AsyncProcessor.IAsyncTask.Process()
            {
                _disp.SendNoReplyMulticastMessage(_dests, _data);
            }
        }

        #endregion

        private OnClusterConfigUpdate _onClusterConfigUpdate;

        /// <summary> The listner of the cluster events like node joined / node left.</summary>
        private IClusterEventsListener _listener;

        /// <summary> The default operation timeout, to be specified in the configuration. </summary>
        protected string _subgroupid;

        /// <summary> The runtime context associated with the current cache. </summary>
        protected CacheRuntimeContext _context;

        /// <summary> The underlying communication channel to be used. </summary>
        protected Channel _channel;

        /// <summary> The uderlying message dispatcher object. </summary>
        protected MsgDispatcher _msgDisp;

        /// <summary> The listener of cluster events.</summary>
        protected IClusterParticipant _participant;

        protected IDistributionPolicyMember _distributionPolicyMbr;

        /// <summary> keeps track of all group members </summary>
        private IDictionary _subgroups = Hashtable.Synchronized(new Hashtable());

        /// <summary> keeps track of all group members </summary>
        protected ArrayList _members = ArrayList.Synchronized(new ArrayList(11));

        /// <summary> keeps track of all group members </summary>
        protected ArrayList _groupCoords = ArrayList.Synchronized(new ArrayList(11));

        /// <summary> keeps track of all recognized group members </summary>
        protected ArrayList _validMembers = ArrayList.Synchronized(new ArrayList(11));

        /// <summary> keeps track of all server members </summary>
        protected ArrayList _servers = ArrayList.Synchronized(new ArrayList(11));

        protected ArrayList _otherServers = null;

        /// <summary> The default operation timeout, to be specified in the configuration. </summary>
        private long _defOpTimeout = 60000;

        /// <summary>controls the priority of the events </summary>
        protected Priority _eventPriority = Priority.Normal;

        private ClusterOperationSynchronizer _asynHandler;

        private Hashtable _membersRenders = Hashtable.Synchronized(new Hashtable());

        private long _lastViewId;

        private object viewMutex = new object();
        private const int MAX_CLUSTER_MBRS = 2;
        internal string localIp = "";
        private bool _stopServices;

       


        private ILogger _ncacheLog;

        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

      
        string _cacheserver="NCache";

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public ClusterService(CacheRuntimeContext context, IClusterParticipant part, IDistributionPolicyMember distributionMbr)//, IMirrorManagementMember mirrorManagementMbr)
        {
            _context = context;
            _ncacheLog = context.NCacheLog;
            
            _participant = part;
            _distributionPolicyMbr = distributionMbr;

           

            _asynHandler = new ClusterOperationSynchronizer(this);
        }

        public void InitializeClusterPerformanceCounters(string instancename)
        {
            if (_channel != null)
            {
                try
                {
                    ((GroupChannel)_channel).InitializePerformanceCounter(instancename);
                }
                catch (Exception e)
                {
                    NCacheLog.Error("ClusterService.InitializeCLusterCounters",   e.ToString());
                }
            }
        }

        /// <summary>
        /// a cluster-wide unique identifier that is used by each of the bridge source cache while 
        /// connecting to bridge.
        /// </summary>
        private string _bridgeSourceCacheId;

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public ClusterService(CacheRuntimeContext context, IClusterParticipant part, IDistributionPolicyMember distributionMbr, IClusterEventsListener listener)
        {
            _context = context;
            _participant = part;
            _distributionPolicyMbr = distributionMbr;
            _asynHandler = new ClusterOperationSynchronizer(this);
            _listener = listener;

        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_msgDisp != null)
            {
                _msgDisp.stop();
                _msgDisp = null;
            }
            if (_channel != null)
            {
                _channel.close();
                _channel = null;
            }
            if (_asynHandler != null)
            {
                _asynHandler.Dispose();
            }            
        }

        #endregion

        /// <summary> Returns the root cluster name. (Main cluster) </summary>
        public string ClusterName { get { return _channel.ChannelName; } }
        /// <summary> Returns the sub-cluster name. (Secondary cluster) </summary>
        public string SubClusterName
        {
            get { return _subgroupid; }
        }

        public string BridgeSourceCacheId
        {
            get { return _bridgeSourceCacheId; }
        }
       

        /// <summary> Get a named subgroup from the list of sub-groups. </summary>
        public SubCluster CurrentSubCluster { get { return GetSubCluster(_subgroupid); } }

        /// <summary>
        /// The default operation timeout, to be specified in the configuration.
        /// </summary>
        public long Timeout
        {
            get { return _defOpTimeout; }
            set { _defOpTimeout = value; }
        }

        /// <summary>
        /// Listener of the cluster events like node joined / node left etc.
        /// </summary>
        public IClusterEventsListener ClusterEventsListener
        {
            get { return _listener; }
            set { _listener = value; }
        }

        /// <summary>
        /// Get the last view id
        /// </summary>
        public long LastViewID
        {
            get { return this._lastViewId; }
        }

        public void NotifyLeaving()
        {
            if (_channel != null) _channel.down(new Event(Event.NOTIFY_LEAVING));
        }


        /// <summary>
        /// Get a named subgroup from the list of sub-groups.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SubCluster GetSubCluster(string name)
        {
            if (name == null) return null;
            return (SubCluster)_subgroups[name];
        }
        public SubCluster GetSubCluster(Address address)
        {
            if (address == null) return null;
            lock (_subgroups.SyncRoot)
            {
                for (IEnumerator i = _subgroups.Values.GetEnumerator(); i.MoveNext(); )
                {
                    SubCluster group = (SubCluster)i.Current;
                    if (group.IsMember(address)) return group;
                }
            }
            return null;
        }

        /// <summary> The hashtable that contains members and their info.</summary>
        public ArrayList Members { get { return _members; } }
        public ArrayList ValidMembers { get { return _validMembers; } }
        public ArrayList Servers { get { return _servers; } }
        public ArrayList SubCoordinators { get { return _groupCoords; } }

        /// <summary>Address of all other servers in cluster.</summary>
        public ArrayList OtherServers { get { return _otherServers; } }

        public Hashtable Renderers { get { return this._membersRenders; } }

        /// <summary>
        /// returns true if the node is operating in coordinator mode. 
        /// </summary>
        public bool IsCoordinator
        {
            get
            {
                Address address = Coordinator;
                if (address != null && LocalAddress.CompareTo(address) == 0)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// check if the given address exists in this cluster
        /// </summary>
        public bool IsMember(Address node)
        {
            return _members.Contains(node);
        }

        /// <summary>
        /// returns the coordinator in the cluster. 
        /// </summary>
        public Address Coordinator
        {
            get
            {
                lock (_servers.SyncRoot)
                {
                    if (_servers.Count > 0)
                        return _servers[0] as Address;
                }
                return null;
            }
        }

        /// <summary>
        /// returns the next coordinator in the cluster. 
        /// </summary>
        public Address NextCoordinator(bool isPOR)
        {
           
            lock (_servers.SyncRoot)
            {
                if (isPOR)
                {
                    if (_servers.Count > 2)
                        return _servers[2] as Address;
                }
                else
                {
                    if (_servers.Count > 1)
                        return _servers[1] as Address;
                }
            }
            return null;
           
        }

        /// <summary> The local address of this instance. </summary>
        public Address LocalAddress { get { return _channel.LocalAddress; } }

        /// <summary>
        /// Method that allows the object to initialize itself. Passes the property map down 
        /// the object hierarchy so that other objects may configure themselves as well..
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        public void Initialize(IDictionary properties, string channelName,
            string domain, NodeIdentity identity)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                if (properties.Contains("op-timeout"))
                {
                    long val = Convert.ToInt64(properties["op-timeout"]);
                    if (val < 60) val = 60;
                    val = val * 1000;
                    Timeout = val;
                }
                if (properties.Contains("notification-priority"))
                {
                    string priority = Convert.ToString(properties["notification-priority"]);
                    if (priority.ToLower() == "normal")
                        _eventPriority = Priority.Normal;
                }

                IDictionary clusterProps = properties["cluster"] as IDictionary;
                string channelProps = ConfigHelper.GetClusterPropertyString(clusterProps, Timeout);

                string name = channelName != null ? channelName.ToLower() : null;
                if (clusterProps.Contains("group-id"))
                    name = Convert.ToString(clusterProps["group-id"]);
                if (clusterProps.Contains("sub-group-id"))
                {
                    _subgroupid = Convert.ToString(clusterProps["sub-group-id"]);
                    if (_subgroupid != null) _subgroupid = _subgroupid.ToLower();
                    identity.SubGroupName = _subgroupid;
                }
                // =======================================
                else
                    _subgroupid = name;
                // =======================================

                if (name != null) name = name.ToLower();
                if (_subgroupid != null) _subgroupid = _subgroupid.ToLower();

                //A property or indexer may not be passed as an out or ref parameter.
                _channel = new GroupChannel(channelProps, _context.NCacheLog);

                Hashtable config = new Hashtable();
                config["additional_data"] = CompactBinaryFormatter.ToByteBuffer(identity, _context.SerializationContext);
                _channel.down(new Event(Event.CONFIG, config));

                _msgDisp = new MsgDispatcher(_channel, this, this, this, this, false, true);
                _channel.connect(name + domain, _subgroupid, identity.IsStartedAsMirror,false);

                localIp = LocalAddress.IpAddress.ToString();
                _msgDisp.start();
            }
            catch (Exception e)
            {
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }

        public void Initialize(IDictionary properties, string channelName,
            string domain, NodeIdentity identity,
            bool twoPhaseInitialization,bool isPor)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");
            
            try
            {
                if (properties.Contains("op-timeout"))
                {
                    long val = Convert.ToInt64(properties["op-timeout"]);
                    if (val < 60) val = 60;
                    val = val * 1000;
                    Timeout = val;
                }
                if (properties.Contains("notification-priority"))
                {
                    string priority = Convert.ToString(properties["notification-priority"]);
                    if (priority.ToLower() == "normal")
                        _eventPriority = Priority.Normal;
                }

                IDictionary clusterProps = properties["cluster"] as IDictionary;
                string channelProps = ConfigHelper.GetClusterPropertyString(clusterProps, Timeout, isPor);

                string name = channelName != null? channelName.ToLower(): null ;
                if (clusterProps.Contains("group-id"))
                    name = Convert.ToString(clusterProps["group-id"]);
                if (clusterProps.Contains("sub-group-id"))
                {
                    _subgroupid = Convert.ToString(clusterProps["sub-group-id"]);
                    if (_subgroupid != null) _subgroupid = _subgroupid.ToLower();
                    identity.SubGroupName = _subgroupid;
                }
                // =======================================
                else
                    _subgroupid = name;
                // =======================================


                if (name != null) name = name.ToLower();
                if (_subgroupid != null) _subgroupid = _subgroupid.ToLower();
                //A property or indexer may not be passed as an out or ref parameter.
                _channel = new GroupChannel(channelProps, _context.NCacheLog);

                Hashtable config = new Hashtable();
                config["additional_data"] = CompactBinaryFormatter.ToByteBuffer(identity, _context.SerializationContext);
                _channel.down(new Event(Event.CONFIG, config));

                _msgDisp = new MsgDispatcher(_channel, this, this, this, this, false, true);
                _channel.connect(name + domain, _subgroupid, identity.IsStartedAsMirror,twoPhaseInitialization);
                localIp = LocalAddress.IpAddress.ToString();
                _msgDisp.start();
            }
            catch (Exception e)
            {
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }

        public void ConfirmClusterStartUP(bool isPOR, int retryNumber)
        {
            object[] arg = new object[2];
            arg[0] = isPOR;
            arg[1] = retryNumber;
            _channel.down(new Event(Event.CONFIRM_CLUSTER_STARTUP, arg));

        }

        internal void HasStarted()
        {
            _channel.down(new Event(Event.HAS_STARTED));
        }

        public void InitializePhase2()
        {
            if (_channel != null)
            {
                _channel.connectPhase2();
            }
        }
        #region	/                 --- Messages ---           /

        /// <summary>
        /// Sends message to every member of the group. Returns its reponse as well.
        /// </summary>
        public RspList Broadcast(object msg, byte mode)
        {
            return BroadcastToMultiple((ArrayList)null, msg, mode, _defOpTimeout);
        }
        public RspList Broadcast(object msg, byte mode, bool isSeqRequired,Priority msgPriority)
        {
            return BroadcastToMultiple((ArrayList)null, msg, mode, _defOpTimeout, isSeqRequired,"",msgPriority);
        }
       

        public RspList Multicast(ArrayList dests, object msg, byte mode)
        {
            return Multicast(dests, msg, mode, true);
        }

        public RspList Multicast(ArrayList dests, object msg, byte mode, bool isSeqRequired)
        {
            return Multicast(dests, msg, mode, isSeqRequired, _defOpTimeout);
        }

        /// <summary>
        /// Sends message to every member of the group. Returns its reponse as well.
        /// </summary>
        public RspList BroadcastToCoordinators(object msg, byte mode)
        {
            return BroadcastToMultiple(_groupCoords, msg, mode, _defOpTimeout);
        }
        
        /// <summary>
        /// Sends message to every member of the group. Returns its reponse as well.
        /// </summary>
        public RspList BroadcastToServers(object msg, byte mode)
        {
            return BroadcastToMultiple(_servers, msg, mode, _defOpTimeout);
        }
        /// <summary>
        /// Sends message to every member of the group without worrying about the sequence. Returns its reponse as well.
        /// </summary>
        public RspList BroadcastToServers(object msg, byte mode, bool isSeqRequired)
        {
            return BroadcastToMultiple(_servers, msg, mode, _defOpTimeout, isSeqRequired);
        }
       
     
        /// <summary>
        /// Sends message to every member of the group. Returns its reponse as well.
        /// </summary>
        public RspList BroadcastToMultiple(ArrayList dests, object msg, byte mode)
        {
            return BroadcastToMultiple(dests, msg, mode, _defOpTimeout);
        }
        /// <summary>
        /// Sends message to every member of the group without worrying about the sequence. Returns its reponse as well.
        /// </summary>
        public RspList BroadcastToMultiple(ArrayList dests, object msg, byte mode, bool isSeqRequired)
        {
            return BroadcastToMultiple(dests, msg, mode, _defOpTimeout, isSeqRequired, "",Priority.Normal);
        }
        public RspList BroadcastToMultiple(ArrayList dests, object msg, byte mode, long timeout, bool isSeqRequired)
        {
            return BroadcastToMultiple(dests, msg, mode, timeout, isSeqRequired, "", Priority.Normal);
        }
      
        public RspList BroadcastToMultiple(ArrayList dests, object msg, byte mode, long timeout)
        {
            return BroadcastToMultiple(dests, msg, mode, timeout, true, "", Priority.Normal);
        }
        private RspList BroadcastToMultiple(ArrayList dests, object msg, byte mode, long timeout, bool isSeqRequired, string traceMsg,Priority priority)
        {
            ///TODO: remove this conditional compilation

            byte[] serializedMsg = SerializeMessage(msg);
            Message m = new Message(null, null, serializedMsg);
            if (msg is Function)
            {
                m.Payload = ((Function)msg).UserPayload;
                m.responseExpected = ((Function)msg).ResponseExpected;
            }

            m.setBuffer(serializedMsg);
            m.IsSeqRequired = isSeqRequired;
            m.Priority = priority;
            RspList rspList = null;

            try
            {
                rspList = _msgDisp.castMessage(dests, m, mode, timeout);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustService.BcastToMultiple", "completed");

            }
            if (rspList.size() == 0)
            {
                return null;
            }
            Rsp rsp;
            for (int i = 0; i < rspList.size(); i++)
            {
                rsp = (Rsp)rspList.elementAt(i);
                rsp.Deflate(_context.SerializationContext);
            }
            return rspList;
        }

        public RspList BroadcastToMultiple(ArrayList dests, object msg, byte mode, long timeout, string traceMsg, bool handleAsync)
        {

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustService.BcastToMultiple", "");

            byte[] serializedMsg = SerializeMessage(msg);
            Message m = new Message(null, null, serializedMsg);
            m.HandledAysnc = handleAsync;
            m.setBuffer(serializedMsg);
            m.IsUserMsg = true;
            RspList rspList = null;

            try
            {
                rspList = _msgDisp.castMessage(dests, m, mode, timeout);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustService.BcastToMultiple", "completed");

            }
            if (rspList.size() == 0)
            {
                return null;
            }
            Rsp rsp;
            for (int i = 0; i < rspList.size(); i++)
            {
                rsp = (Rsp)rspList.elementAt(i);
                rsp.Deflate(_context.SerializationContext);
            }
            return rspList;
        }

        public RspList Multicast(ArrayList dests, object msg, byte mode, bool isSeqRequired, long timeout)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustService.Mcast", "");

            byte[] serializedMsg = SerializeMessage(msg);
            Message m = new Message(null, null, serializedMsg);
            if (msg is Function)
                m.Payload = ((Function)msg).UserPayload;
            m.setBuffer(serializedMsg);
            m.Dests = dests;
            m.IsSeqRequired = isSeqRequired;

            RspList rspList = null;

            try
            {
                rspList = _msgDisp.castMessage(dests, m, mode, timeout);
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustService.Mcast", "completed");

            }
            if (rspList.size() == 0)
            {
                return null;
            }
            Rsp rsp;
            for (int i = 0; i < rspList.size(); i++)
            {
                rsp = (Rsp)rspList.elementAt(i);
                rsp.Deflate(_context.SerializationContext);
            }

            return rspList;
        }

        public void SendResponse(Address dest, object result, long reqId)
        {
            byte[] serializedMsg = SerializeMessage(result);
            Message response = new Message(dest, null, serializedMsg);

            try
            {
                _msgDisp.SendResponse(reqId, response);
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {

            }

        }
        /// <summary> 
        /// Sends a message to a single member (destination = msg.dest) and returns 
        /// the response. The message's destination must be non-zero !
        /// </summary>
        protected internal object SendMessage(Address dest, object msg, byte mode)
        {
            return SendMessage(dest, msg, mode, _defOpTimeout);
        }

        protected internal object SendMessage(Address dest, object msg, byte mode, bool isSeqRequired)
        {
            return SendMessage(dest, msg, mode, isSeqRequired, _defOpTimeout);
        }

        private byte[] SerializeMessage(object msg)
        {
            byte[] serializedMsg = null;
            serializedMsg = CompactBinaryFormatter.ToByteBuffer(msg, _context.SerializationContext);
            return serializedMsg;
        }

        protected internal object SendMessage(Address dest, object msg, byte mode, long timeout)
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustService.SendMsg", "dest_addr :" + dest);

                object result;
                byte[] serializedMsg = SerializeMessage(msg);
                Message m = new Message(dest, null, serializedMsg);
                if (msg is Function)
                {
                    m.Payload = ((Function)msg).UserPayload;
                    m.responseExpected = ((Function)msg).ResponseExpected;
                }
                result = _msgDisp.sendMessage(m, mode, timeout);
                if (result is OperationResponse)
                {
                    if (((OperationResponse)result).SerilizationStream != null)
                    {
                        CompactBinaryFormatter.Serialize(((OperationResponse)result).SerilizationStream, ((OperationResponse)result).SerializablePayload, _context.SerializationContext, false);
                    }
                    else
                    {
                        ((OperationResponse)result).SerializablePayload = CompactBinaryFormatter.FromByteBuffer((byte[])((OperationResponse)result).SerializablePayload, _context.SerializationContext);
                    }
                }
                else if (result is byte[])
                {
                    result = CompactBinaryFormatter.FromByteBuffer((byte[])result, _context.SerializationContext);
                }

                if (result != null && result is Exception) throw (Exception)result;
                return result;
            }
            catch (Alachisoft.NGroups.SuspectedException e)
            {
                throw new Runtime.Exceptions.SuspectedException("operation failed because the group member was suspected " + e.suspect);
            }
            catch (Runtime.Exceptions.TimeoutException e)
            {
                throw;
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustService.SendMsg", "completed");
            }
        }

        protected internal object SendMessage(Address dest, object msg, byte mode, bool isSeqRequired, long timeout) 
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustService.SendMsg", "dest_addr :" + dest);

               
                byte[] serializedMsg = SerializeMessage(msg);
                Message m = new Message(dest, null, serializedMsg);
                if (msg is Function)
                    m.Payload = ((Function)msg).UserPayload;
                m.IsSeqRequired = isSeqRequired;

                object result = _msgDisp.sendMessage(m, mode, timeout);
                if (result is OperationResponse)
                {
                    ((OperationResponse)result).SerializablePayload = CompactBinaryFormatter.FromByteBuffer((byte[])((OperationResponse)result).SerializablePayload, _context.SerializationContext);
                }
                else if (result is byte[])
                {
                    result = CompactBinaryFormatter.FromByteBuffer((byte[])result, _context.SerializationContext);
                }

                if (result != null && result is Exception) throw (Exception)result;
                return result;
            }
            catch (Alachisoft.NGroups.SuspectedException e)
            {
                throw new Runtime.Exceptions.SuspectedException("operation failed because the group member was suspected " + e.suspect);
            }
            catch (Runtime.Exceptions.TimeoutException e)
            {
                throw;
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustService.SendMsg", "completed");
            }
        }

        protected internal object SendMessage(Address dest, object msg, byte mode, long timeout, bool handleAsync)
        {
            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustService.SendMsg", "dest_addr :" + dest);

                byte[] serializedMsg = SerializeMessage(msg);
                Message m = new Message(dest, null, serializedMsg);
                m.HandledAysnc = handleAsync;
                m.IsUserMsg = true;
                object result = _msgDisp.sendMessage(m, mode, timeout);
                if (result is byte[])
                    result = CompactBinaryFormatter.FromByteBuffer((byte[])result, _context.SerializationContext);
                return result;
            }
            catch (Alachisoft.NGroups.SuspectedException e)
            {
                throw new Runtime.Exceptions.SuspectedException("operation failed because the group member was suspected " + e.suspect);
            }
            catch (Runtime.Exceptions.TimeoutException e)
            {
                throw;
            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustService.SendMsg", "completed");
            }
        }

        /// <summary>
        /// Send a broadcast no reply message to a specific partition
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="part"></param>
        protected internal void SendNoReplyMessage(Object msg)
        {
            SendNoReplyMessage(msg, _eventPriority);
        }

        /// <summary>
        /// Send a broadcast no reply message to a specific partition
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="msg"></param>
        /// <param name="part"></param>
        protected internal void SendNoReplyMessage(Address dest, Object msg)
        {
            SendNoReplyMessage(dest, msg, _eventPriority, false);
        }
        /// <summary>
        /// Send a broadcast no reply message to a specific partition
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="part"></param>
        protected internal void SendNoReplyMessage(Object msg, Priority priority)
        {
            SendNoReplyMessage(null, msg, priority, false);
        }

        /// <summary>
        /// Send a broadcast no reply message to a specific partition without sequence
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="part"></param>
        protected internal void SendNoReplyMessage(Object msg, Priority priority, bool isSeqRequired)
        {
            SendNoReplyMessage(null, msg, priority, isSeqRequired);
        }

        /// <summary>
        /// Send a broadcast no reply message to a specific node
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="msg"></param>
        /// <param name="part"></param>
        protected internal void SendNoReplyMessage(Address dest, Object msg, Priority priority, bool isSeqRequired)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ClustService.SendNoRepMsg", "dest_addr :" + dest);

            byte[] serializedMsg = SerializeMessage(msg);
            Message m = new Message(dest, null, serializedMsg);
            m.IsSeqRequired = isSeqRequired;
            m.Priority = priority;
            _channel.send(m);
        }

        /// <summary>
        /// Sends a configuration event on the channel of the cluster.
        /// </summary>
        /// <param name="configurationEvent"></param>
        public void ConfigureLocalCluster(Event configurationEvent)
        {
            if (_channel != null)
                _channel.down(configurationEvent);
        }

        /// <summary>
        /// Send a multicast no reply message 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="msg"></param>
        /// <param name="part"></param>
        public void SendNoReplyMulticastMessage(ArrayList dest, Object msg)
        {
            SendNoReplyMulticastMessage(dest, msg, _eventPriority, false);

        }
        /// <summary>
        /// Send a multicast no reply message 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="msg"></param>
        /// <param name="part"></param>
        public void SendNoReplyMulticastMessage(ArrayList dest, Object msg, bool isSeqRequired)
        {
            SendNoReplyMulticastMessage(dest, msg, _eventPriority, isSeqRequired);

        }
        /// <summary>
        /// Send a multicast no reply message 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="msg"></param>
        /// <param name="part"></param>
        protected internal void SendNoReplyMulticastMessage(ArrayList dest, Object msg, Priority priority, bool isSeqRequired)
        {
            byte[] serializedMsg = SerializeMessage(msg);
            Message m = new Message(null, null, serializedMsg);
            m.IsSeqRequired = isSeqRequired;
            m.Priority = priority;
            m.Dests = dest;
            _channel.send(m);
        }

        /// <summary>
        /// Send a multicast no reply message 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="part"></param>
        protected internal virtual void SendNoReplyMcastMessageAsync(ArrayList dests, Object msg)
        {
            _context.AsyncProc.Enqueue(new AsyncMulticastCast(dests, this, msg));
        }
        /// <summary>
        /// Send a broadcast no reply message to a specific partition excluding self
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="part"></param>
        protected internal virtual void SendNoReplyMessageAsync(Object msg)
        {
            _context.AsyncProc.Enqueue(new AsyncBroadCast(this, msg));
        }

        /// <summary>
        /// Send a broadcast no reply message to a specific partition
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="msg"></param>
        /// <param name="part"></param>
        protected internal virtual void SendNoReplyMessageAsync(Address dest, Object msg)
        {
            _context.AsyncProc.Enqueue(new AsyncUnicasCast(dest, this, msg));
        }

        #endregion

        #region	/                 --- Cluster Membership ---           /

        /// <summary>
        /// Authenticate the client and see if it is allowed to join the list of valid members.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="identity"></param>
        /// <returns>true if the node is valid and belongs to the scheme's cluster</returns>
        private bool AuthenticateNode(Address address, NodeIdentity identity)
        {
            return _participant.AuthenticateNode(address, identity);
        }

        /// <summary>
        /// Called after the membership has been changed. Lets the members do some
        /// member oriented tasks.
        /// </summary>
        private void OnAfterMembershipChange()
        {
            _groupCoords = new ArrayList();
            lock (_subgroups.SyncRoot)
            {
                for (IEnumerator i = _subgroups.Values.GetEnumerator(); i.MoveNext(); )
                {
                    SubCluster group = (SubCluster)i.Current;
                    if (group.Coordinator != null)
                        _groupCoords.Add(group.Coordinator);
                }
            }
            _otherServers = (ArrayList)_servers.Clone();
            _otherServers.Remove(LocalAddress);

            _participant.OnAfterMembershipChange();
        }

        /// <summary>
        /// Called when a new member joins the group.
        /// </summary>
        /// <param name="address">address of the joining member</param>
        /// <param name="identity">additional identity information</param>
        /// <returns>true if the node joined successfuly</returns>
        private bool OnMemberJoined(Address address, NodeIdentity identity, ArrayList joiningNowList)
        {
            try
            {
                if (!AuthenticateNode(address, identity))
                {
                    NCacheLog.Warn("ClusterService.OnMemberJoined()", "A non-server attempted to join cluster -> " + address);
                    _validMembers.Remove(address);
                    _servers.Remove(address);
                    return false;
                }

                SubCluster group = null;
                if (identity.HasStorage && identity.SubGroupName != null)
                {
                    lock (_subgroups.SyncRoot)
                    {
                        group = GetSubCluster(identity.SubGroupName);
                        if (group == null)
                        {
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ClusterService.OnMemberJoined()", "Formed new sub-cluster -> " + identity.SubGroupName);
                            group = new SubCluster(identity.SubGroupName, this);
                            _subgroups[identity.SubGroupName] = group;
                        }
                        group.OnMemberJoined(address, identity);
                    }
                }
                bool joined = _participant.OnMemberJoined(address, identity);
                if (!joined && group != null)
                {
                    group.OnMemberLeft(address, _distributionPolicyMbr.BucketsOwnershipMap);
                }

                if (joined)
                {
                    NCacheLog.CriticalInfo("ClusterService.OnMemberJoined()", "Member joined: " + address);

                    Address renderer = new Address(identity.RendererAddress, identity.RendererPort);

                    string mirrorExplaination = identity.IsStartedAsMirror ? " (replica)" : "";

                    if (joiningNowList.Contains(address) && !_context.IsStartedAsMirror && !address.Equals(LocalAddress))
                    {
                        AppUtil.LogEvent(_cacheserver, "Node \"" + address + mirrorExplaination + "\" has joined to \"" + _context.CacheRoot.Name + "\".", System.Diagnostics.EventLogEntryType.Information, EventCategories.Information, EventID.NodeJoined);
                    }

                    if (!_membersRenders.Contains(address))
                    {
                        _membersRenders.Add(address, renderer);

                        if (_listener != null && !identity.IsStartedAsMirror)
                        {
                            _listener.OnMemberJoined(address, renderer);
                        }
                    }
                }

                return joined;
            }
            catch (Exception exception)
            {
                NCacheLog.Error("ClusterService.OnMemberJoined", exception.ToString());
            }
            return false;
        }

        /// <summary>
        /// Called when an existing member leaves the group.
        /// </summary>
        /// <param name="address">address of the joining member</param>
        /// <returns>true if the node left successfuly</returns>
        private bool OnMemberLeft(Address address, NodeIdentity identity)
        {
            if (_validMembers.Contains(address))
            {
                NCacheLog.CriticalInfo("ClusterService.OnMemberLeft()", "Member left: " + address);
                if (identity.HasStorage && identity.SubGroupName != null)
                {
                    lock (_subgroups.SyncRoot)
                    {
                        SubCluster group = GetSubCluster(identity.SubGroupName);
                        if (group != null)
                        {
                            if (group.OnMemberLeft(address, _distributionPolicyMbr.BucketsOwnershipMap) < 1)
                            {
                                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ClusterService.OnMemberLeft()",   "Destroyed sub-cluster -> " + identity.SubGroupName);
                                _subgroups.Remove(identity.SubGroupName);
                            }
                        }
                    }
                }
                if (_membersRenders.Contains(address))
                {
                    Address renderer = (Address)_membersRenders[address];
                    _membersRenders.Remove(address);

                    if (_listener != null && !identity.IsStartedAsMirror) // invisible replica's don't raise events.
                        _listener.OnMemberLeft(address, renderer);
                }

                string mirrorExplaination = identity.IsStartedAsMirror ? " (replica)" : "";

                if (!_context.IsStartedAsMirror)
                {
                    AppUtil.LogEvent(_cacheserver, "Node \"" + address + mirrorExplaination + "\" has left \"" + _context.CacheRoot.Name + "\".", System.Diagnostics.EventLogEntryType.Warning, EventCategories.Warning, EventID.NodeLeft);
                }

                return _participant.OnMemberLeft(address, identity);
            }
            return false;
        }

        #endregion

        #region	/                 --- MembershipListener Interface ---           /

        /// <summary>
        /// Notify the target object of a change of membership.
        /// </summary>
        /// <param name="new_view">New view of group</param>
        void MembershipListener.viewAccepted(View newView)
        {
            System.Collections.ArrayList joined_mbrs, left_mbrs, tmp;
            ArrayList joining_mbrs = new ArrayList();

            lock (viewMutex)
            {
                object tmp_mbr;

                if (newView == null)
                    return;

                NCacheLog.CriticalInfo("ClusterService.ViewAccepted", newView.ToString());
                tmp = newView.Members;

                if (newView.Vid != null)
                {
                    this._lastViewId = newView.Vid.Id;
                }

                // get new members
                joined_mbrs = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));

                for (int i = 0; i < tmp.Count; i++)
                {
                    tmp_mbr = tmp[i];
                    if (!_members.Contains(tmp_mbr))
                        joined_mbrs.Add(tmp_mbr);
                }
                int localIndex = 0;

                if (joined_mbrs.Contains(LocalAddress))
                    localIndex = joined_mbrs.IndexOf(LocalAddress);

                for (int i = localIndex; i < joined_mbrs.Count; i++)
                {
                    joining_mbrs.Add(joined_mbrs[i]);
                }

                // get members that left
                left_mbrs = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
                for (int i = 0; i < _members.Count; i++)
                {
                    tmp_mbr = _members[i];
                    if (!tmp.Contains(tmp_mbr))
                        left_mbrs.Add(tmp_mbr);
                }

                // adjust our own membership
                _members.Clear();
                _members.AddRange(tmp);

                //pick the map from the view and send it to cache.
                //if i am the only member, i can build the map locally.
                if (newView.DistributionMaps == null && newView.Members.Count == 1)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ClusterService.viewAccepted()", "I am the only member in the view so, building map myself");
                    PartNodeInfo affectedNode = new PartNodeInfo(LocalAddress, _subgroupid, true);
                    DistributionInfoData info = new DistributionInfoData(DistributionMode.OptimalWeight, ClusterActivity.NodeJoin, affectedNode);
                    DistributionMaps maps = _distributionPolicyMbr.GetDistributionMaps(info);
                    if (maps != null)
                    {
                        _distributionPolicyMbr.HashMap = maps.Hashmap;
                        _distributionPolicyMbr.BucketsOwnershipMap = maps.BucketsOwnershipMap;
                    }
                }
                else
                {

                    if (newView.MirrorMapping != null)
                    {
                        _distributionPolicyMbr.InstallMirrorMap(newView.MirrorMapping);
                        NCacheLog.Info("ClusterService.viewAccepted()", "New MirrorMap installed.");
                    }

                    if (newView.DistributionMaps != null)
                    {
                        _distributionPolicyMbr.InstallHashMap(newView.DistributionMaps, left_mbrs);
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ClusterService.viewAccepted()", "New hashmap installed");
                    }
                }

                lock (_servers.SyncRoot)
                {
                    if (left_mbrs.Count > 0)
                    {
                        for (int i = left_mbrs.Count - 1; i >= 0; i--)
                        {
                            Address ipAddr = (Address)((Address)left_mbrs[i]);
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ClusterService.viewAccepted", ipAddr.AdditionalData.Length.ToString());
                            ipAddr = (Address)ipAddr.Clone();
                            if (_servers.Contains(ipAddr))
                                _servers.Remove(ipAddr);

                            OnMemberLeft(ipAddr, CompactBinaryFormatter.FromByteBuffer(ipAddr.AdditionalData, _context.SerializationContext) as NodeIdentity);
                            ipAddr.AdditionalData = null;
                        }
                    }

                    _validMembers = (ArrayList)_members.Clone();
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ClusterService.viewAccepted", joining_mbrs.Count.ToString());

                    if (joined_mbrs.Count > 0)
                    {
                        for (int i = 0; i < joined_mbrs.Count; i++)
                        {
                            Address ipAddr = (Address)joined_mbrs[i];
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ClusterService.viewAccepted", ipAddr.AdditionalData.Length.ToString());
                            ipAddr = (Address)ipAddr.Clone();

                            if (OnMemberJoined(ipAddr, CompactBinaryFormatter.FromByteBuffer(ipAddr.AdditionalData, _context.SerializationContext) as NodeIdentity, joining_mbrs))
                            {
                                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ClusterServices.ViewAccepted", ipAddr.ToString() + " is added to _servers list.");
                                _servers.Add(ipAddr);
                            }
                            ipAddr.AdditionalData = null;
                        }
                    }
                }

                if (String.IsNullOrEmpty(_bridgeSourceCacheId))
                    _bridgeSourceCacheId = newView.BridgeSourceCacheId;
                OnAfterMembershipChange();
            }
        }

        /// <summary>
        /// Notify the target object of a suspected member
        /// </summary>
        /// <param name="suspected_mbr"></param>
        void MembershipListener.suspect(Address suspected_mbr)
        {
        }

        /// <summary>Block sending and receiving of messages until viewAccepted() is called </summary>
        void MembershipListener.block()
        {
        }

        /// <summary>Whether to allow join of new node </summary>
        bool MembershipListener.AllowJoin()
        {
            return !_participant.IsInStateTransfer();
        }

        #endregion

        #region	/                 --- MessageListener Interface ---           /

        /// <summary>
        /// Notify the target object of a received Message.
        /// </summary>
        /// <param name="msg">Received Message</param>
        void MessageListener.receive(Message msg)
        {
            ((RequestHandler)this).handle(msg);
        }

        #endregion

        #region	/                 --- RequestHandler Interface ---           /

        /// <summary>
        /// Handles the function requests.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public object handleFunction(Address src, Function func)
        {
            return _participant.HandleClusterMessage(src, func);
        }

        public object handleFunction(Address src, Function func, out Address destination, out Message replicationMsg)
        {
            return _participant.HandleClusterMessage(src, func, out destination, out replicationMsg);
        }

        /// <summary>
        ///	Called by the message dispactcher when a message request is received.
        /// </summary>
        /// <param name="msg">message request.</param>
        /// <returns>the reply to be sent to the requester.</returns>
        object RequestHandler.handle(Message req)
        {
            if (req == null || req.Length == 0)
            {
                return null;
            }
            try
            {
                bool isLocalReq = LocalAddress.CompareTo(req.Src) == 0;
                object body = req.getFlatObject();
                try
                {
                    if (body is Byte[])
                    {
                        body = CompactBinaryFormatter.FromByteBuffer((byte[])body, _context.SerializationContext);
                    }
                }
                catch (Exception e)
                {                   
                    return e;
                }
                object result = null;
                if (body is Function)
                {
                    Function func = (Function)body;
                    func.UserPayload = req.Payload;
                    if (isLocalReq && func.ExcludeSelf)
                    {
                        if (req.HandledAysnc && req.RequestId > 0)
                            SendResponse(req.Src, null, req.RequestId);
                        return null;
                    }
                    if (req.HandledAysnc)
                    {
                        AsyncRequst asyncReq = new AsyncRequst(func, func.SyncKey);
                        asyncReq.Src = req.Src;
                        asyncReq.RequsetId = req.RequestId;
                        _asynHandler.HandleRequest(asyncReq);
                        return null;
                    }
                    else
                    {
                        result = handleFunction(req.Src, func);
                    }
                }
                else if (body is AggregateFunction)
                {
                    AggregateFunction funcs = (AggregateFunction)body;
                    object[] results = new object[funcs.Functions.Length];
                    for (int i = 0; i < results.Length; i++)
                    {
                        Function func = (Function)funcs.Functions.GetValue(i);
                        if (isLocalReq && func.ExcludeSelf)
                        {
                            if (req.HandledAysnc && req.RequestId > 0)
                            {
                                SendResponse(req.Src, null, req.RequestId);
                                continue;
                            }
                            results[i] = null;
                        }
                        else
                        {
                            if (req.HandledAysnc)
                            {
                                AsyncRequst asyncReq = new AsyncRequst(func, func.SyncKey);
                                asyncReq.Src = req.Src;
                                asyncReq.RequsetId = req.RequestId;
                                _asynHandler.HandleRequest(asyncReq);
                                continue;
                            }
                            results[i] = handleFunction(req.Src, func);
                        }
                    }
                    result = results;
                }

                if (result is OperationResponse)
                {
                    if (((OperationResponse)result).SerilizationStream != null)
                    {
                        CompactBinaryFormatter.Serialize(((OperationResponse)result).SerilizationStream, ((OperationResponse)result).SerializablePayload, _context.SerializationContext, false);
                    }
                    else
                    {
                        ((OperationResponse)result).SerializablePayload = CompactBinaryFormatter.ToByteBuffer(((OperationResponse)result).SerializablePayload, _context.SerializationContext);
                    }
                }
                else
                {
                    result = CompactBinaryFormatter.ToByteBuffer(result, _context.SerializationContext);
                }
                return result;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        object RequestHandler.handleNHopRequest(Message req, out Address destination, out Message replicationMsg)
        {
            destination = null;
            replicationMsg = null;

            if (req == null || req.Length == 0)
                return null;

            try
            {
                bool isLocalReq = LocalAddress.CompareTo(req.Src) == 0;
                object body = req.getFlatObject();
                try
                {
                    if (body is Byte[])
                        body = CompactBinaryFormatter.FromByteBuffer((byte[])body, _context.SerializationContext);
                }
                catch (Exception e)
                {
                    return e;
                }

                object result = null;
                if (body is Function)
                {
                    Function func = (Function)body;
                    func.UserPayload = req.Payload;
                    if (isLocalReq && func.ExcludeSelf)
                    {
                        if (req.HandledAysnc && req.RequestId > 0)
                            SendResponse(req.Src, null, req.RequestId);
                        return null;
                    }
                    if (req.HandledAysnc)
                    {
                        AsyncRequst asyncReq = new AsyncRequst(func, func.SyncKey);
                        asyncReq.Src = req.Src;
                        asyncReq.RequsetId = req.RequestId;
                        _asynHandler.HandleRequest(asyncReq);
                        return null;
                    }
                    else
                    {
                        result = handleFunction(req.Src, func, out destination, out replicationMsg);
                    }
                }
                else if (body is AggregateFunction)
                {
                    AggregateFunction funcs = (AggregateFunction)body;
                    object[] results = new object[funcs.Functions.Length];
                    for (int i = 0; i < results.Length; i++)
                    {
                        Function func = (Function)funcs.Functions.GetValue(i);
                        if (isLocalReq && func.ExcludeSelf)
                        {
                            if (req.HandledAysnc && req.RequestId > 0)
                            {
                                SendResponse(req.Src, null, req.RequestId);
                                continue;
                            }
                            results[i] = null;
                        }
                        else
                        {
                            if (req.HandledAysnc)
                            {
                                AsyncRequst asyncReq = new AsyncRequst(func, func.SyncKey);
                                asyncReq.Src = req.Src;
                                asyncReq.RequsetId = req.RequestId;
                                _asynHandler.HandleRequest(asyncReq);
                                continue;
                            }
                            results[i] = handleFunction(req.Src, func);
                        }
                    }
                    result = results;
                }

                if (result is OperationResponse)
                {

                    if (((OperationResponse)result).SerilizationStream != null)
                    {
                       CompactBinaryFormatter.Serialize(((OperationResponse)result).SerilizationStream, ((OperationResponse)result).SerializablePayload, _context.SerializationContext, false);
                    }
                    else
                    {
                        ((OperationResponse)result).SerializablePayload = CompactBinaryFormatter.ToByteBuffer(((OperationResponse)result).SerializablePayload, _context.SerializationContext);
                    }

                }
                else
                {
                    result = CompactBinaryFormatter.ToByteBuffer(result, _context.SerializationContext);
                }

                return result;
            }
            catch (Exception e)
            {
                return e;
            }
        }


        #endregion
        public void StopServices()
        {
            if (_msgDisp != null) _msgDisp.StopReplying();
        }

        public void MarkClusterInStateTransfer()
        {
            _channel.down(new Event(Event.MARK_CLUSTER_IN_STATETRANSFER));
        }

        public void MarkClusterStateTransferCompleted()
        {
            _channel.down(new Event(Event.MARK_CLUSTER_STATETRANSFER_COMPLETED));
        }

        #region MessageResponder members


        public object GetDistributionAndMirrorMaps(object data)
        {
            NCacheLog.Debug("MessageResponder.GetDistributionAndMirrorMaps()",   "here comes the request for hashmap");

            object[] package = data as object[];
            ArrayList members = package[0] as ArrayList;
            bool isJoining = (bool)package[1];
            string subGroup = (string)package[2];
            bool isStartedAsMirror = (bool)package[3];

            ClusterActivity activity = ClusterActivity.None;
            activity = isJoining ? ClusterActivity.NodeJoin : ClusterActivity.NodeLeave;

            DistributionMaps maps = null;

            // if the joining node is mirror and its coordinator/active exists
            {
                PartNodeInfo affectedNode = new PartNodeInfo((Address)members[0], subGroup, !isStartedAsMirror);
                DistributionInfoData info = new DistributionInfoData(DistributionMode.OptimalWeight, activity, affectedNode);
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ClusterService.GetDistributionMaps",   "NodeAddress: " + info.AffectedNode.NodeAddress.ToString() + " subGroup: " + subGroup + " isMirror: " + isStartedAsMirror.ToString() + " " + (isJoining ? "joining" : "leaving"));
                maps = _distributionPolicyMbr.GetDistributionMaps(info);
            }

            CacheNode[] mirrors = _distributionPolicyMbr.GetMirrorMap();
            NCacheLog.Debug("MessageResponder.GetDistributionAndMirrorMaps()",   "sending hashmap response back...");

           
            return new object[] { maps, mirrors };
        }

        #endregion

      
        
      
    }
}
