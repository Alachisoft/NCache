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
using System.Net;
using System.Threading;
using System.Collections;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Web.Util;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Web.Command;
using Alachisoft.NCache.Web.RemoteClient.Config;
using Alachisoft.NCache.Web.Statistics;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Web.AsyncTask;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Web.Communication
{
    internal sealed class Broker
    {
        internal RemoteCache _cache = null;
        internal ClientConfiguration _clientConfig;
        private OnCommandRecieved _commandReieved = null;
        private OnServerLost _serverLost = null;
        private delegate void OnNewHashmapRecieved(NewHashmap newHashmap, int bucketSize);
        private OnNewHashmapRecieved _onNewHashmapRecieved = null;
        private delegate void OnNewSerializedMap(byte[] buffer);
        private OnNewSerializedMap _onNewSerializedMap = null;
        /// <summary> Object</summary>
        private byte[] _value;
        private ConnectionPool _pool = null;
        private Connection _connection = null;
        private long _requestId = -1;
        private static int forcedViewId = -5;
        private HashVector _requestTable = null;
        internal Hashtable serverMap = null;
        private object _connectionMutex = new object();
        private ReaderWriterLock _lock = new ReaderWriterLock();
        private int _connectionMutexTimeout = Timeout.Infinite;
        private int _connectionRetries = 5;
        private int _retryInterval = 1;
        private int _connectionTimeout = 5000;
        private int _processID = System.Diagnostics.Process.GetCurrentProcess().Id;
        private bool _retryConnection = true;
        private DateTime _retryConnectionStartTime = DateTime.Now;
        private string _cacheId;
        private Address _serverIP;
        private int _port;
        private int _priority = 1;
        private int _itemSizeThreshHold = 0;
        private int _operationTimeout = 90000; //default 90 sec.
        private Logs _logger = new Logs();
        private bool _balanceNode;
        private bool _importHashmap = true;
        private IPAddress _nodeIP;
        private int _newServerPort;
        private ResponseIntegrator _responseIntegrator = new ResponseIntegrator();
        //this processor will be used to reconnect with connection who are disconnected.
        private AsyncProcessor _processor = null;
        //this is for bulk events
        private AsyncProcessor _eventProcessor = null;
        private int _asyncProccesorThreadCount = 1;
        private bool _notifyAsync = true;
        private PerfStatsCollector _perfStatsColl = null;
        internal PerfStatsCollector2 _perfStatsColl2;
        public bool _perfStatsEnabled = false;
        private bool _isDisposing = false;
        private bool _connectingFirstTime = true;
        private WaitCallback _bulkEventCallback;
        private object _hashmapUpdateMutex = new object();
        private bool _isPersistEnabled = false;
        private int _persistenceInterval;
        Latch _hashMapStatus = new Latch(HashMapStatus.UNINITIALIZE);


        internal int RetryInterval
        {
            get { return _retryInterval; }
        }

        public Logs Logger
        {
            get { return _logger; }
        }

        public bool IsPersistenceEnabled
        {
            get { return _isPersistEnabled; }
        }

        public int PersistInterval
        {
            get { return _persistenceInterval; }
        }

        internal int ItemSizeThreshHold
        {
            get { return _itemSizeThreshHold; }
        }

        private long RequestId
        {
            get { return Interlocked.Increment(ref _requestId); }
        }

        public static int ForcedViewId
        {
            get { return forcedViewId; }
        }

        internal long ClientLastViewId
        {
            get { return  this._pool.LastViewId; }
        }

        public int OperationTimeOut
        {
            get { return _operationTimeout; }
        }

        public IPAddress NodeIP
        {
            get { return _nodeIP; }
            set { _nodeIP = value; }
        }

        public int NewServerPort
        {
            get { return _newServerPort; }
            set { _newServerPort = value; }
        }

        internal Broker(RemoteCache cache, CacheInitParams initParams, PerfStatsCollector2 statsCol)
            : this(cache, true, statsCol, initParams)
        {
        }

        private Broker(RemoteCache cache, bool importHashMap, PerfStatsCollector2 perfStatsColl,
            CacheInitParams initParams)
        {
            _bulkEventCallback = new WaitCallback(RaiseBulkEvent);
            _clientConfig = new ClientConfiguration(cache.CacheId, initParams);
            _cache = cache;
           
            _balanceNode = _clientConfig.BalanceNodes;
            _importHashmap = _clientConfig.ImportHashmap;

            _operationTimeout = _clientConfig.Timeout;
            _connectionTimeout = _clientConfig.ConnectionTimeout;
            _connectionRetries = _clientConfig.ConnectionRetries;
            _retryInterval = _clientConfig.RetryInterval;

            _perfStatsColl2 = perfStatsColl;

            int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
            string instanceName = "Client." + cache.CacheId + "." + pid;

            if (_perfStatsColl == null || !_perfStatsColl.InstanceName.Equals(instanceName))
            {
                _perfStatsColl = new PerfStatsCollector(instanceName, 0);
            }
            _commandReieved = new OnCommandRecieved(CommandReceived);
            _serverLost = new OnServerLost(ServerLost);

            _requestTable = HashVector.Synchronized(new HashVector(10000, 0.75f));
            _pool = new ConnectionPool();
        }

        // function to return the initial server and port to connect with
        internal RemoteServer GetInitialServer()
        {
            RemoteServer serverInfo = new RemoteServer();
            if (_clientConfig.initParam.ServerList.Length > 0)
            {
                serverInfo = _clientConfig.initParam.ServerList[0].ServerInfo;
                return serverInfo;
            }
            return serverInfo;

        }

        internal void StartServices(string cacheId, string server, int port)
        {
            _cacheId = cacheId;

            if (!string.IsNullOrEmpty(System.Configuration.ConfigurationSettings.AppSettings.Get("NCacheClient.AsynchronousEventNotification")))
            {
                try
                {
                    _notifyAsync =Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings.Get("NCacheClient.AsynchronousEventNotification"));
                }
                catch (Exception ex)
                {
                    throw new Exception("Invalid value specified for NCacheClient.AsynchronousEventNotification.");
                }

                if (!_notifyAsync)
                {
                    if (!string.IsNullOrEmpty(System.Configuration.ConfigurationSettings.AppSettings.Get("NCacheClient.NumberofEventProccesingThreads")))
                    {
                        try
                        {
                            _asyncProccesorThreadCount =Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings.Get("NCacheClient.NumberofEventProccesingThreads"));
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Invalid value specified for NCacheClient.NumberofEventProccesingThreads.");
                        }
                    }

                    if (_asyncProccesorThreadCount <= 0)
                        _asyncProccesorThreadCount = 1;

                    if (_asyncProccesorThreadCount > 5)
                        _asyncProccesorThreadCount = 5;

                    _eventProcessor = new AsyncProcessor(_asyncProccesorThreadCount);
                    _eventProcessor.Start();
                }
            }

            //Check the type of license the application 
            try
            {
                _clientConfig.LoadConfiguration();
            }
            catch (Exception)
            {
            }

            bool enable_logs = false;
            bool detailed_logs = false;

            if (System.Configuration.ConfigurationSettings.AppSettings.Get("enableNCWebLogs") != null)
            {
                enable_logs =
                    Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["enableNCWebLogs"]);
            }
            else
                enable_logs = _clientConfig.EnableClientLogs;

            if (System.Configuration.ConfigurationSettings.AppSettings.Get("enableDetailedNCWebLogs") != null)
            {
                detailed_logs =
                    Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["enableDetailedNCWebLogs"]);
            }
            else
                detailed_logs = _clientConfig.EnableDetailedClientLogs;

            InitializeLogs(enable_logs, detailed_logs);

            if (System.Configuration.ConfigurationSettings.AppSettings.Get("enablePerfStats") != null)
            {
                _perfStatsEnabled =
                    Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["enablePerfStats"]);
            }

            if (_perfStatsEnabled)
            {
                _perfStatsColl.InitializePerfCounters();
            }


            int conTimeout = _connectionRetries*(_connectionTimeout + _retryInterval);
            if (conTimeout > 0) _connectionMutexTimeout = conTimeout;
            if (_operationTimeout < 60000) _operationTimeout = 60000; //minimum timeout is 60 seconds.

            _connection = new Connection(this, _commandReieved, _serverLost, _logger, _perfStatsColl, _responseIntegrator, _clientConfig.BindIP, this._cacheId);

            RemoteServer remoteServer = new RemoteServer(server, port);

            if (this._importHashmap)
            {
                this._processor = new AsyncProcessor();
            }

            if (remoteServer.IP != null)
            {
                remoteServer.IsUserProvided = true;


                _clientConfig.AddServer(remoteServer);

                try
                {
                    ConnectRemoteServer(_connection, remoteServer, true);
                }

                catch (OperationNotSupportedException oe)
                {
                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StartServices", oe.ToString());
                }
            }
            if (!IsConnected)
            {
                try
                {

                    TryNextServer();
                }

                catch (OperationNotSupportedException oe)
                {
                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StartServices", oe.ToString());
                    throw;
                }

            }
        }

        private void InitializeLogs(bool enable_logs, bool detailed_logs)
        {
            if (enable_logs)
            {
                Logs localLogger = new Logs();
                localLogger.IsErrorLogsEnabled = enable_logs;
                if(localLogger.IsErrorLogsEnabled)
                    localLogger.IsDetailedLogsEnabled = detailed_logs;

                int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                localLogger.NCacheLog = new NCacheLogger();
                localLogger.NCacheLog.Initialize(LoggerNames.ClientLogs, _cacheId);
                if (detailed_logs)
                    localLogger.NCacheLog.SetLevel("all");
                else
                {
                    localLogger.NCacheLog.SetLevel("criticalinfo");
                }

                localLogger.NCacheLog.Info("Broker.InitializeLogs", "PID :" + pid + " ClientID : " + _cache.ClientID);
                _logger = localLogger;
            }
            else
            {
                if (_logger.NCacheLog != null)
                {
                    _logger.NCacheLog.Flush();
                    _logger.NCacheLog.SetLevel("OFF");
                }
            }
        }


        private Persistence.EventType ConvertEventType( Common.Protobuf.EventInfo.EventType protoEventType)
        {
            switch (protoEventType)
            {

                case Common.Protobuf.EventInfo.EventType.CQ_CALLBACK:
                    return Persistence.EventType.CQ_CALLBACK;

                case Common.Protobuf.EventInfo.EventType.ITEM_REMOVED_CALLBACK:
                    return Persistence.EventType.ITEM_REMOVED_CALLBACK;

                case Common.Protobuf.EventInfo.EventType.ITEM_UPDATED_CALLBACK:
                    return Persistence.EventType.ITEM_UPDATED_CALLBACK;

            }
            return Persistence.EventType.ITEM_UPDATED_CALLBACK;
        }

        internal void InitializeCache(Connection connection, IPAddress address, int port, bool balanceNodes)
        {
           
            InitCommand command = new InitCommand(_cache.ClientID, _cacheId);
           
            Request request = new Request(false, _operationTimeout);
            request.AddCommand(connection.ServerAddress, command);
            ExecuteRequest(request, connection, false, false);
           
            CommandResponse res = connection.RecieveCommandResponse();                       

            switch (res.CacheType)
            {
                case "partitioned-server":
                    _balanceNode = false;
                    break;
                case "local-cache":
                    _balanceNode = false;
                    _importHashmap = false;
                    break;
                case "replicated-server":
                    _importHashmap = false;
                    break;
            }
            lock (_requestTable.SyncRoot)
            {
                _requestTable.Remove(request.RequestId);
            }

            if (_perfStatsColl2 != null) _perfStatsColl2.DecrementRequestQueueSizeStats();

            if (res != null)
                res.ParseResponse();
      }

        internal void GetLoggingInfo(Connection connection)
        {
            GetLogginInfoCommand command = new GetLogginInfoCommand();
           
            Request request = new Request(false, _operationTimeout);
            Address ipAddress = connection.ServerAddress;
            request.AddCommand(ipAddress, command);

            ExecuteRequest(request, connection, true, true);
            CommandResponse res = request.Response;
            InitializeLogs(res.EnableErrorLogs, res.EnableDetailedLogs);
        }

        internal void InitializeSecondarySocket(Connection connection, IPAddress address, int port)
        {
            connection.ConnectSecondarySocket(address, port);
            InitSecondarySocketCommand command = new InitSecondarySocketCommand(_cache.ClientID);
            DoSendCommand(connection, command, false);
            CommandResponse res = connection.RecieveCommandResponse(connection.SecondaryClientSocket);
            if (res != null) res.ParseResponse();
        }

        internal void GetHashmap()
        {
            GetHashmap(null);
        }

        internal void GetHashmap(Connection connection)
        {
            GetHashmapCommand command = new GetHashmapCommand();
            Request request = new Request(false, _operationTimeout);
            Address ipAddress = connection == null ? _connection.ServerAddress : connection.ServerAddress;
            request.AddCommand(ipAddress, command);

            if (connection != null)
                ExecuteRequest(request, connection, true, true);
            else
                ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            NewHashmap hashmap = res.Hashmap; 
            
            if (hashmap == null)
            {

                try
                {
                    if (_processor != null) _processor.Stop();
                }
                catch (ThreadAbortException)
                {
                }
                _importHashmap = false;
                return;
            }
            if (_onNewHashmapRecieved == null)
                _onNewHashmapRecieved = new OnNewHashmapRecieved(NewHashmapRecieved);
            _onNewHashmapRecieved.BeginInvoke(hashmap, res.BucketSize, new AsyncCallback(NewHashmapRecievedCompleted), null);

        }

        public bool IsConnected
        {
            get { return _connection != null ? _connection.IsConnected : false; }
        }

        internal bool PoolHasAllServers
        {
            get { return _clientConfig.ServerCount == _pool.Servers.Count; }
        }

        internal bool PoolFullyConnected
        {
            get
            {
                lock (_hashmapUpdateMutex)
                {
                    return _pool.FullyConnnected;
                }
            }
        }

        internal bool PoolFullyDisConnected
        {
            get
            {
                lock (_hashmapUpdateMutex)
                {
                    if (_pool.FullyDisConnnected)
                    {
                        if (_pool.Connections != null)
                        {
                            try
                            {
                                //as pool is fully disconnected,let's start reconnection task
                                foreach (Connection connection in _pool.Connections.Values)
                                {
                                    if (connection != null && !connection.IsReconnecting)
                                    {
                                        this._processor.Enqueue(new ReconnectTask(this, connection));
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                //enumeration exception can occur
                            }
                        }
                        return true;
                    }
                }
                return false;
            }
        }


        internal bool ImportHashmap
        {
            get { return _importHashmap; }
        }

        internal ArrayList ClientServerList
        {
            get { return _clientConfig.ServerList; }
        }

        internal byte[] Value
        {
            get { return _value; }
            set
            {
                if (value != null)
                {
                    _value = new byte[value.Length];
                    value.CopyTo(_value, 0);
                }
            }
        }

        /// <summary>event handler for the Receive CommandBase Event.
        /// gets the command from teh _requestTable and if this command was async
        /// then this thread goes on to notify the initiator of this request. if
        /// the command was synchronous then the thread which is waiting on this
        /// command object is notified and command object is Remove from the _requestTable.
        /// 
        /// </summary>
        /// <param name="result">CommandResponse indicating the response/command received from the client.
        /// </param>
        internal void CommandReceived(CommandResponse result, Address serverAddress)
        {
            ProcessResponse(result, serverAddress);
        }

        internal void ProcessResponse(CommandResponse response, Address remoteServerAddress)
        {
            CommandBase command = null;
            Request request = null;
            lock (_requestTable.SyncRoot)
            {
                request = (Request) _requestTable[response.RequestId];
                if (request != null)
                {
                    try
                    {
                        if (request.Commands.Count > 0)
                            command = request.Commands[remoteServerAddress];
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

            response.CacheId = _cacheId;

            switch (response.Type)
            {
                case Common.Protobuf.Response.Type.INIT:
                case Common.Protobuf.Response.Type.ADD:
                case Common.Protobuf.Response.Type.REMOVE:
                case Common.Protobuf.Response.Type.GET:
                case Common.Protobuf.Response.Type.INSERT:
                case Common.Protobuf.Response.Type.CLEAR:
                case Common.Protobuf.Response.Type.COUNT:
                case Common.Protobuf.Response.Type.REGISTER_NOTIF:
                case Common.Protobuf.Response.Type.GET_ENUMERATOR:
                case Common.Protobuf.Response.Type.ADD_BULK:
                case Common.Protobuf.Response.Type.INSERT_BULK:
                case Common.Protobuf.Response.Type.GET_OPTIMAL_SERVER:
                case Common.Protobuf.Response.Type.GET_BULK:
                case Common.Protobuf.Response.Type.REMOVE_BULK:
                case Common.Protobuf.Response.Type.CONTAINS:
                case Common.Protobuf.Response.Type.GET_CACHE_ITEM:
                case Common.Protobuf.Response.Type.SEARCH:
                case Common.Protobuf.Response.Type.SEARCH_ENTRIES:
                case Common.Protobuf.Response.Type.REGISTER_KEY_NOTIF:
                case Common.Protobuf.Response.Type.REGISTER_BULK_KEY_NOTIF:
                case Common.Protobuf.Response.Type.UNREGISTER_KEY_NOTIF:
                case Common.Protobuf.Response.Type.UNREGISTER_BULK_KEY_NOTIF:
                case Common.Protobuf.Response.Type.GET_TYPEINFO_MAP:
                case Common.Protobuf.Response.Type.GET_HASHMAP:
                case Common.Protobuf.Response.Type.UNLOCK:
                case Common.Protobuf.Response.Type.LOCK:
                case Common.Protobuf.Response.Type.ISLOCKED:
                case Common.Protobuf.Response.Type.GET_LOGGING_INFO:
                case Common.Protobuf.Response.Type.DISPOSE:
                case Common.Protobuf.Response.Type.DELETE:
                case Common.Protobuf.Response.Type.DELETE_BULK:
                case Common.Protobuf.Response.Type.EXCEPTION:
                case Common.Protobuf.Response.Type.GET_NEXT_CHUNK:
                case Common.Protobuf.Response.Type.ADD_ATTRIBUTE:
                case Common.Protobuf.Response.Type.GET_SERVER_MAPPING:
                case Common.Protobuf.Response.Type.EXECUTE_READER:
                case Common.Protobuf.Response.Type.DISPOSE_READER:
                case Common.Protobuf.Response.Type.GET_READER_CHUNK:
                    
                    if (request == null)
                        return;

                    lock (request)
                    {
                        request.AddResponse(remoteServerAddress, response);
                        Monitor.PulseAll(request);
                    }
                    if (_logger.IsDetailedLogsEnabled)
                        _logger.NCacheLog.Debug("Broker.ProcessResponse", "RequestID : " + request.RequestId + " " + request.Name + " received response from server " + remoteServerAddress + ". Seq # " + response.SequenceId);
                    break;

                case Common.Protobuf.Response.Type.ITEM_REMOVED_CALLBACK:
                    if (_cache != null && _cache.AsyncEventHandler != null)
                    {
                        _cache.EventListener.OnCustomRemoveCallback(response.CallbackId, response.Key, response.Value, response.Reason, response.FlagValueEntry.Flag, true, null, response.DataFilter);
                    }
                    break;

                case Common.Protobuf.Response.Type.NODE_JOINED_EVENT:
                    ThreadPool.QueueUserWorkItem(new WaitCallback(StartBalancingClients), response);
                    break;

                case Common.Protobuf.Response.Type.HASHMAP_CHANGED_EVENT:
                    ThreadPool.QueueUserWorkItem(new WaitCallback(UpdateHashmapAsync), response.Value);
                    break;

                case Common.Protobuf.Response.Type.NODE_LEFT_EVENT:
                    RemoteServer serverLeft = new RemoteServer();

                    if (response.ServerPort > 0)
                    {
                        //Get Mapped Server will return the same "IP and port" incase of non-existance of the map
                        RemoteServer rm = _clientConfig.GetMappedServer(response.ServerIPAddress.ToString(), response.ServerPort);
                        serverLeft = rm;
                        _clientConfig.RemoveServer(serverLeft);
                    }
                    if (_importHashmap)
                        _cache.InvalidateReaders(response.ServerIPAddress.ToString());
                    break;

                case Common.Protobuf.Response.Type.ITEM_UPDATED_CALLBACK:
                    if (_cache != null && _cache.AsyncEventHandler != null)
                    {
                        _cache.EventListener.OnCustomUpdateCallback(response.CallbackId, response.Key, true, null, null, null, response.DataFilter);
                    }
                    break;

                case Common.Protobuf.Response.Type.BULK_EVENT:
                    if (response.EventList.Count > 0)
                    {
                        ThreadPool.QueueUserWorkItem(_bulkEventCallback, new object[] {remoteServerAddress, response.EventList});
                        break;
                    }
                    break;
            }

            if (request != null)
            {
                if (_perfStatsColl2 != null)
                    _perfStatsColl2.DecrementRequestQueueSizeStats();
            }
        }

        private void RaiseBulkEvent(object arg)
        {
            try
            {
                EventId eventId;
                Address remoteServerAddress = null;
                if (arg == null || ((object[]) arg).Length != 2) return;
                remoteServerAddress = ((object[]) arg)[0] as Address;
                List<Common.Protobuf.BulkEventItemResponse> bulkEvents = ((object[]) arg)[1] as List<Common.Protobuf.BulkEventItemResponse>;

                if (bulkEvents == null) return;

                foreach (Common.Protobuf.BulkEventItemResponse eventItem in bulkEvents)
                {
                    try
                    {
                        if (_perfStatsColl2 != null)
                        {
                            _perfStatsColl2.IncrementEventsTriggeredPerSeconds();
                        }

                        eventId = new EventId();

                        switch (eventItem.eventType)
                        {

                            case Common.Protobuf.BulkEventItemResponse.EventType.ITEM_UPDATED_CALLBACK:
                            {
                                eventId.EventUniqueID = eventItem.ItemUpdatedCallback.eventId.eventUniqueId;
                                eventId.EventCounter = eventItem.ItemUpdatedCallback.eventId.eventCounter;
                                eventId.OperationCounter = eventItem.ItemUpdatedCallback.eventId.operationCounter;
                                eventId.EventType = NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK;

                                BitSet flag = new BitSet((byte) eventItem.ItemUpdatedCallback.flag);

                                    EventCacheItem item = EventUtil.ConvertToEventEntry(eventItem.ItemUpdatedCallback.eventId.item);
                                    EventCacheItem oldItem = EventUtil.ConvertToEventEntry(eventItem.ItemUpdatedCallback.eventId.oldItem);

                                    if (_notifyAsync)
                                    {
                                        if (_cache != null && _cache.AsyncEventHandler != null)
                                        {
                                            _cache.EventListener.OnCustomUpdateCallback((Int16)eventItem.ItemUpdatedCallback.callbackId, eventItem.ItemUpdatedCallback.key, _notifyAsync, item, oldItem, flag, (EventDataFilter)eventItem.ItemUpdatedCallback.dataFilter);
                                        }
                                    }
                                    else
                                        _eventProcessor.Enqueue(new ItemUpdateCallbackTask(this, eventItem.ItemUpdatedCallback.key, (Int16)eventItem.ItemUpdatedCallback.callbackId, _notifyAsync, item, oldItem, flag, (EventDataFilter)eventItem.ItemUpdatedCallback.dataFilter));
                                }
                                break;

                            case Common.Protobuf.BulkEventItemResponse.EventType.ITEM_REMOVED_CALLBACK:
                                {
                                    CompressedValueEntry flagValueEntry = new CompressedValueEntry();
                                    flagValueEntry.Flag = new BitSet((byte)eventItem.itemRemoveCallback.flag);

                                    EventCacheItem item = EventUtil.ConvertToEventEntry(eventItem.itemRemoveCallback.eventId.item);
                                    byte[] value = null;
                                    if (item != null && eventItem.itemRemoveCallback.value != null && eventItem.itemRemoveCallback.value.Count > 0)
                                    {
                                        UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(eventItem.itemRemoveCallback.value.ToArray());
                                        value = ubObject.GetFullObject();
                                        if (item != null)
                                        {
                                            item.Value = value;
                                        }
                                    }

                                    eventId.EventUniqueID = eventItem.itemRemoveCallback.eventId.eventUniqueId;
                                    eventId.EventCounter = eventItem.itemRemoveCallback.eventId.eventCounter;
                                    eventId.OperationCounter = eventItem.itemRemoveCallback.eventId.operationCounter;
                                    eventId.EventType = NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK;

                                    if (_notifyAsync)
                                    {
                                        if (_cache != null && _cache.AsyncEventHandler != null)
                                            _cache.EventListener.OnCustomRemoveCallback((Int16)eventItem.itemRemoveCallback.callbackId, eventItem.itemRemoveCallback.key, value, (CacheItemRemovedReason)eventItem.itemRemoveCallback.itemRemoveReason, flagValueEntry.Flag, _notifyAsync, item, (EventDataFilter)eventItem.itemRemoveCallback.dataFilter);
                                    }
                                    else
                                        _eventProcessor.Enqueue(new ItemRemoveCallBackTask(this, eventItem.itemRemoveCallback.key, (Int16)eventItem.itemRemoveCallback.callbackId, value, (CacheItemRemovedReason)eventItem.itemRemoveCallback.itemRemoveReason, flagValueEntry.Flag, _notifyAsync, item, (EventDataFilter)eventItem.itemRemoveCallback.dataFilter));
                                }
                                break;

                        }
                    }
                    catch (Exception ex)
                    {
                        if (_logger.IsErrorLogsEnabled)
                            _logger.NCacheLog.Error("Broker.RaiseBulkEvent", "An error occurred while raising bulk event of type : " + eventItem.eventType + ". Error :" + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void UpdateHashmapAsync(object state)
        {
            byte[] serializedMap = state as byte[];
            try
            {
                if (serializedMap != null)
                {
                    NewHashmap map = NewHashmap.Deserialize(serializedMap, this._cacheId);
                    NewHashmapRecieved(map);
                }

            }
            catch (Exception e)
            {
                try
                {
                    if (_logger.IsErrorLogsEnabled)
                        _logger.NCacheLog.Error("Broker.UpdateHashmapAsync", "An error occurred while installing updated hashmap. Error :" + e.ToString());
                }
                catch (Exception)
                {
                }
            }
        }

        private int NextWaitInterval(ref int totalTimeToWait, int timeSlice)
        {
            if (totalTimeToWait == 0)
            {
                timeSlice = 0;
            }
            else if (timeSlice > totalTimeToWait)
            {
                timeSlice = totalTimeToWait;
                totalTimeToWait = 0;
            }
            else
            {
                totalTimeToWait -= timeSlice;
            }
            return timeSlice;
        }

        private void StartBalancingClients(object state)
        {
            try
            {
                Address clusterAddress = null;
                Address serverAddress = null;
                CommandResponse response = (CommandResponse)state;
                RemoteServer newServerJoined = new RemoteServer();

                if (response.ServerPort > 0)
                {

                    if (_clientConfig.IPMappingConfigured)
                    {
                        this.GetServerMapping(null, false);
                        RemoteServer rm = _clientConfig.GetMappedServer(response.ServerIPAddress.ToString(), response.ServerPort);
                        newServerJoined = rm;
                    }
                    else
                    {
                        newServerJoined = new RemoteServer(response.ServerIPAddress, response.ServerPort);
                    }

                    _clientConfig.AddServer(newServerJoined);
                }

                clusterAddress = new Address(newServerJoined.IP, newServerJoined.Port);
                serverAddress = new Address(newServerJoined.IP, newServerJoined.Port);
                NodeIP = serverAddress.IpAddress;
                NewServerPort = serverAddress.Port;

                if (response.ReconnectClients && _clientConfig.BalanceNodes)
                {
                    bool isReleaseLock = false;
                    try
                    {
                        this._lock.AcquireWriterLock(Timeout.Infinite);
                        isReleaseLock = true;

                        this._connection.StatusLatch.SetStatusBit(ConnectionStatus.Connecting | ConnectionStatus.LoadBalance, ConnectionStatus.Connected | ConnectionStatus.Disconnected);
                        int totalTimeToWait = _operationTimeout; 
                        int timeSlice = 2000;
                        int nextInterval = NextWaitInterval(ref totalTimeToWait, timeSlice);

                        do
                        {
                            if (_logger.IsDetailedLogsEnabled)
                            {
                                _logger.NCacheLog.Info("Broker.StartBalancingClients", "total wait time remaining: " + totalTimeToWait.ToString());
                                _logger.NCacheLog.Info("Broker.StartBalancingClients", "current wait interval: " + nextInterval.ToString());
                            }

                            Thread.Sleep(nextInterval);

                            lock (this._requestTable.SyncRoot)
                            {
                                if (this._requestTable.Count == 0)
                                {
                                    break;
                                }
                                if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.Info("Broker.StartBalancingClients", "Responses remaining: " + this._requestTable.Count);
                            }

                            nextInterval = NextWaitInterval(ref totalTimeToWait, timeSlice);
                        } while (nextInterval > 0);


                        ResetBroker(this._serverIP);
                        _connection.StatusLatch.SetStatusBit(ConnectionStatus.Connecting, ConnectionStatus.LoadBalance);
                        _connection.Disconnect();
                        Thread.Sleep(5000);
                        Exception exception = null;
                        if (!ConnectRemoteServer(this._connection, NodeIP, NewServerPort, false, false, true,  ref exception))
                        {
                            this._lock.ReleaseWriterLock();
                            isReleaseLock = false;
                            TryNextServer();
                        }
                        else
                            this._connection.StatusLatch.SetStatusBit(ConnectionStatus.Connected, ConnectionStatus.Connecting);
                    }
                    finally
                    {
                        if (isReleaseLock) this._lock.ReleaseWriterLock();
                    }
                }
            }
            catch (Exception e) { }
        }

        /// <summary>
        /// New hashmap recieved. 
        /// </summary>
        /// <param name="buffer"></param>
        private void NewSerializedMapRecieved(byte[] buffer)
        {
            this.NewHashmapRecieved(NewHashmap.Deserialize(buffer, this._cacheId));
        }

        /// <summary>
        /// New hashmap recieved. Depending on new and old hashmap, some connections are
        /// disposed and some new connections are formed(not always as in some cases only buckets have
        /// transfered between servers). This method should be called asynchronously so the recieve thread
        /// will be free to recieve other command responces.
        /// </summary>
        /// <param name="newHashmap">new hashmap returned from primary server</param>
        /// <param name="bucketSize">bucket size</param>
        private void NewHashmapRecieved(NewHashmap newHashmap, int bucketSize)
        {
            if (newHashmap == null) return;
            this._pool.BucketSize = bucketSize;
            NewHashmapRecieved(newHashmap);
        }

        /// <summary>
        /// New hashmap recieved. Depending on new and old hashmap, some connections are
        /// disposed and some new connections are formed(not always as in some cases only buckets have
        /// transfered between servers). This method should be called asynchronously so the recieve thread
        /// will be free to recieve other command responces.
        /// </summary>
        /// <param name="newHashmap">new hashmap returned from primary server</param>
        private void NewHashmapRecieved(NewHashmap newHashmap)
        {
            if (newHashmap == null)
            {
                if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.CriticalInfo("Broker.NewHashmapReceived", "Hashmap is null... returning");
                return;
            }

            lock (_hashmapUpdateMutex)
            {
                try
                {
                    if (_logger.IsDetailedLogsEnabled)
                    {
                        _logger.NCacheLog.Debug("Broker.NewHashmapReceived", "Hashmap " + newHashmap.ToString());
                    }

                    long oldId = _pool != null ? _pool.LastViewId : -2;

                    if (newHashmap.LastViewId == this._pool.LastViewId)
                    {
                        if (_logger != null && _logger.NCacheLog != null)
                            _logger.NCacheLog.CriticalInfo("Broker.NewHashmapReceived", "Hashmap is same as current pool. Pool " + this._pool.ToString() + " New Hashmap " + newHashmap.ToString() + " ... returning");
                        return;
                    }

                    if (_clientConfig.IPMappingConfigured)
                    {
                        this.GetServerMapping(null, false);
                    }
                    for (int i = 0; i < newHashmap.Members.Count; i++)
                    {
                        string ip = (string) newHashmap.Members[i];

                        int serverPort = this._port;

                        // Azure remote client task
                        if (_clientConfig.IPMappingConfigured)
                        {
                            RemoteServer server = _clientConfig.GetMappedServer(ip, serverPort);
                            ip = server.Name;
                            serverPort = server.Port;
                            newHashmap.Members[i] = ip + ":" + serverPort.ToString();
                        }

                        Address addr = new Address(ip, serverPort);

                        if (!this._pool.Contains(addr))
                        {
                            try
                            {
                                IPAddress address = IPAddress.Parse(ip);

                                Connection connection = new Connection(this, this._commandReieved, this._serverLost, this._logger, _perfStatsColl, _responseIntegrator, _clientConfig.BindIP, _cacheId);
                                
                                Exception exception = null;

                                if (ConnectRemoteServer(connection, address, serverPort, false, false, false, ref exception))
                                {
                                    this._pool.Add(addr, connection);
                                    this._clientConfig.AddServer(new RemoteServer(address, serverPort));
                                    if (_logger.IsDetailedLogsEnabled)
                                        _logger.NCacheLog.Debug("Broker.NewHashmapRecieved", "Connection made to " + ip + ", and added to pool");
                                }
                                else
                                {
                                    if (exception != null && _logger.IsErrorLogsEnabled)
                                    {
                                        _logger.NCacheLog.Error("Broker.NewHashmapRecieved", "Could not connect to " + ip + ". " + exception.ToString());
                                    }
                                }
                            }

                            catch (Exception exc)
                            {
                                if (_logger.IsErrorLogsEnabled)
                                    _logger.NCacheLog.Error("Broker.NewHashmapRecieved", exc.ToString());
                            }
                        }
                        else
                        {
                            Connection connection = this._pool[addr];

                            if (connection != null && !connection.IsConnected)
                            {
                                if (_logger.IsDetailedLogsEnabled)
                                {
                                    _logger.NCacheLog.Debug("Broker.NewHashmapRecieved", "Not connected to " + ip + " in the pool");
                                }
                                try
                                {
                                    Exception exception = null;
                                    TryConnecting(connection, ref exception);
                                }
                                catch (Exception exc)
                                {
                                    if (_logger.IsErrorLogsEnabled)
                                        _logger.NCacheLog.Error("Broker.NewHashmapRecieved", exc.ToString());
                                }
                            }
                            else
                            {
                                if (_logger.IsDetailedLogsEnabled)
                                {
                                    _logger.NCacheLog.Debug("Broker.NewHashmapRecieved",
                                        "Already connected to " + ip + " in the pool");
                                }
                            }
                        }
                    }

                    if (this._importHashmap)
                    {
                        //List of Connection ip's which as per new hashmap are no more valid
                        List<Address> invalidIPConnection = new List<Address>();

                        foreach (Address ipAddress in this._pool.Servers)
                        {

                            if (_clientConfig.IPMappingConfigured)
                            {
                                if (!newHashmap.Members.Contains(ipAddress.IpAddress.ToString() + ":" +ipAddress.Port.ToString()))
                                {
                                    invalidIPConnection.Add(ipAddress);
                                }
                            }
                            else
                            {
                                if (!newHashmap.Members.Contains(ipAddress.IpAddress.ToString()))
                                {
                                    invalidIPConnection.Add(ipAddress);
                                }
                            }
                        }

                        foreach (Address ip in invalidIPConnection)
                        {
                            this._pool[ip].Disconnect();
                            this._pool.Remove(ip);
                            if (_logger.IsDetailedLogsEnabled)
                            {
                                _logger.NCacheLog.Debug("Broker.NewHashmapRecieved", "Disconnected from " + ip + ", and removed from pool");
                            }
                        }
                    }
                    RemoteServer srvr = new RemoteServer();
                    string add = null;
                    for (int key = 0; key < newHashmap.Map.Count; key++)
                    {
                        add = (string) newHashmap.Map[key];
                        srvr = _clientConfig.GetMappedServer(add, this._port);
                        newHashmap.Map[key] = new Address(srvr.Name, srvr.Port);

                    }

                    this._pool.SetHashmap(newHashmap);

                    if (_logger.IsDetailedLogsEnabled)
                    {
                        _logger.NCacheLog.Debug("Broker.NewHashmapReceived", "Hashmap applied " + newHashmap.ToString() + " Pool " + this._pool.ToString());
                    }
                }
                catch (Exception exc)
                {
                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.NewHashmapRecieved", exc.Message);
                }
                _hashMapStatus.SetStatusBit(HashMapStatus.INITIALIZE, HashMapStatus.UNINITIALIZE);
            }
        }

        /// <summary>
        /// Called when NewHashmapRecieved callback is executed
        /// </summary>
        /// <param name="result"></param>
        private void NewHashmapRecievedCompleted(IAsyncResult result)
        {

            this._onNewHashmapRecieved.EndInvoke(result);

        }

        /// <summary>
        /// Incase of multiple connection we have to select the connection to which the 
        /// events are registered
        /// </summary>
        /// <param name="command"></param>
        internal void SendUnregisterCommand(CommandBase command)
        {
            if (!this._importHashmap) SendCommand(command);
            else
            {
                Hashtable pool = this._pool.CloneConnectionTable();
                Connection connection = null;
                foreach (DictionaryEntry entry in pool)
                {
                    connection = entry.Value as Connection;
                    if (connection != null && connection.NotifRegistered)
                    {
                        SendCommand(connection, command, true);
                        break;
                    }
                }
            }
        }

        internal bool GetKeysDistributionMap(string[] keys, CacheItem[] items,ref Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap)
        {
            bool result = false;
            bool itemsAvailable = items != null;

            if (_importHashmap)
            {
                Dictionary<Address, Hashtable> keysDistributionList = new Dictionary<Address, Hashtable>();
                Hashtable keysAndItems = null;
                string key = String.Empty;
                CacheItem item = null;

                for (int i = 0; i < keys.Length; i++)
                {
                    key = keys[i];
                    if (itemsAvailable)
                        item = items[i];

                    Address address;
                    _hashMapStatus.WaitForAny(HashMapStatus.INITIALIZE);
                    lock (_hashmapUpdateMutex)
                    {
                        address = _pool.GetIp(key);
                    }

                    if (keysDistributionList.ContainsKey(address))
                    {
                        keysAndItems = keysDistributionList[address];
                        keysAndItems[key] = item;
                    }
                    else
                    {
                        keysAndItems = new Hashtable();
                        keysAndItems[key] = item;
                        keysDistributionList[address] = keysAndItems;
                    }
                }

                    KeyValuePair<string[], CacheItem[]> tmp;
                    Address serverAddress; 

                    foreach (KeyValuePair<Address, Hashtable> pair in keysDistributionList)
                    {
                        int index = 0;
                        serverAddress = pair.Key;
                        keysAndItems = pair.Value;

                        string[] distributedKeys = new string[keysAndItems.Count];
                        CacheItem[] distributedItems = null;
                        if (itemsAvailable)
                            distributedItems = new CacheItem[keysAndItems.Count];

                        IDictionaryEnumerator ide = keysAndItems.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            distributedKeys[index] = ide.Key as string;
                            if (itemsAvailable)
                                distributedItems[index] = ide.Value as CacheItem;
                            index++;
                        }

                        tmp = new KeyValuePair<string[], CacheItem[]>(distributedKeys, distributedItems);
                        keysDistributionMap.Add(serverAddress, tmp);
                    }
                }

            return result;
        }

        public KeyValuePair<string[], CacheItem[]> MergeDistributioonMap(KeyValuePair<string[], CacheItem[]> map1, KeyValuePair<string[], CacheItem[]> map2, bool itemsAvailable)
        {

            string[] distributedKeys = new string[map1.Key.Length + map2.Key.Length];
            CacheItem[] distributedItems = null;
            if (itemsAvailable)
                distributedItems = new CacheItem[map1.Value.Length + map2.Value.Length];

            int size = map1.Key.Length;

            for (int i = 0; i < map1.Key.Length; i++)
            {
                distributedKeys[i] = map1.Key[i];
                if (itemsAvailable)
                    distributedItems[i] = map1.Value[i];
            }

            for (int i = 0; i < map2.Key.Length; i++)
            {
                distributedKeys[size + i] = map2.Key[i];
                if (itemsAvailable)
                    distributedItems[size + i] = map2.Value[i];
            }

            return new KeyValuePair<string[], Caching.CacheItem[]>(distributedKeys, distributedItems);
        }

        internal Request CreateRequest(CommandBase command)
        {
            Request request = null;

            switch (command.CommandType)
            {
                case CommandType.SEARCH:
                case CommandType.GET_NEXT_CHUNK:
                case CommandType.EXECUTE_READER:
                case CommandType.DISPOSE_READER:
                case CommandType.GET_READER_CHUNK:
               
                    if (_importHashmap)
                    {
                        if (PoolFullyDisConnected)
                        {
                            throw new OperationFailedException("No server is available to process the request");
                        }

                        if (!PoolFullyConnected)
                        {
                            request = CreateDedicatedRequest(command);
                        }
                        else
                        {
                            request = new Request(true, _operationTimeout);
                            lock (_hashmapUpdateMutex)
                            {
                                command.ClientLastViewId = this.ClientLastViewId;
                                foreach (Address server in _pool.Servers)
                                {
                                    request.AddCommand(server, command);
                                }
                            }
                        }
                    }
                    else
                    {
                        request = new Request(false, _operationTimeout);
                        request.AddCommand(_connection.ServerAddress, command);
                    }
                    request.IsAsync = command.isAsync;
                    request.IsAyncCallbackSpecified = command.asyncCallbackSpecified;
                    break;
                default:
                    request = new Request(false, _operationTimeout);
                    request.IsAsync = command.isAsync;
                    request.IsAyncCallbackSpecified = command.asyncCallbackSpecified;
                    Address ipAddress = GetConnectionIP(command);
                    request.AddCommand(ipAddress, command);
                    break;
            }

            return request;
        }

        internal Request CreateRequestOnServer(string nodeAddress, CommandBase command)
        {
            Request request = null;
            request = new Request(true, this._operationTimeout);
            Connection conn = null;
            lock (_hashmapUpdateMutex)
            {
                conn = _pool.GetConnection(nodeAddress);
            }
            if (conn != null)
                request.AddCommand(conn.ServerAddress, command);
            else
                return CreateDedicatedRequest(command);

            return request;
        }

        internal Request CreateDedicatedRequest(CommandBase command)
        {
            Request request = null;
            if (_importHashmap)
            {
                request = new Request(true, _operationTimeout);
                command.ClientLastViewId = ForcedViewId;
                Connection conn = _pool.GetAnyConnection();
                if (conn != null)
                {
                    request.AddCommand(conn.ServerAddress, command);
                    
                }
                else
                {
                    throw new OperationFailedException("No server is available to process the request");
                }
            }
            else
            {
                request = new Request(false, _operationTimeout);
                command.ClientLastViewId = ForcedViewId;
                request.AddCommand(_connection.ServerAddress, command);
            }
            request.IsAsync = command.isAsync;
            request.IsAyncCallbackSpecified = command.asyncCallbackSpecified;

            return request;
        }

        internal Connection GetAnyConnection()
        {
            return _pool.GetAnyConnection();
        }



        internal void ExecuteRequest(Request request, Connection connection, bool checkConnected, bool waitForResponse)
        {
            //1. Add request to request table

            AddRequestToRequestTable(request);
            request.CacheId = _cacheId;
            //2. send each command. This method not only takes care of the specific connection for 
            //sending command, it also intializes the response for the connection.
            if (!request.IsBulk)
            {
                foreach (CommandBase command in request.Commands.Values)
                {
                    SendCommand(connection, command, checkConnected);
                }
            }

            if (waitForResponse)
            {
                bool reacquiredLock = true;
                int timeout = Convert.ToInt32(request.RequestTimeout);
                long startTime = (System.DateTime.Now.Ticks - 621355968000000000)/10000;

                try
                {
                    lock (request)
                    {
                        while (timeout > 0)
                        {
                            if (request.IsAsync)
                                break;

                            if (request.Responses)
                            {
                                break;
                            }


                            timeout = Convert.ToInt32(request.RequestTimeout) -
                                      (int) ((System.DateTime.Now.Ticks - 621355968000000000)/10000 - startTime);
                            reacquiredLock = Monitor.Wait(request, timeout);

                            if (!reacquiredLock)
                            {
                                if (request.IsRequestTimeoutReset)
                                {
                                    timeout = (Convert.ToInt32(request.RequestTimeout) -
                                               (int)
                                                   ((System.DateTime.Now.Ticks - 621355968000000000)/10000 - startTime));
                                    if (timeout > 0)
                                    {
                                        reacquiredLock = true;
                                        reacquiredLock = Monitor.Wait(request, timeout);
                                    }
                                }
                            }

                            if (!reacquiredLock && !request.Responses)
                            {

                                if (_perfStatsColl2 != null) _perfStatsColl2.DecrementRequestQueueSizeStats();

                                if (_logger.IsErrorLogsEnabled)
                                {
                                    _logger.NCacheLog.Error("Broker.SendCommand", request.TimeoutMessage);
                                    _logger.NCacheLog.Error("Broker.ExecuteRequest->timeout",
                                        request.RequestId.ToString());
                                }
                                throw new OperationFailedException(request.TimeoutMessage);
                            }
                        }
                    }
                }
                finally
                {
                    lock (_requestTable.SyncRoot)
                    {
                        if (!request.IsAsync && _requestTable.ContainsKey(request.RequestId))
                            _requestTable.Remove(request.RequestId);

                    }

                    if (_perfStatsColl2 != null) _perfStatsColl2.DecrementRequestQueueSizeStats();

                }
            }

        }

        internal void ExecuteRequest(Request request)
        {
            //1. Add request to request table
            AddRequestToRequestTable(request);

            CommandBase requestCommand = null;
            //2. send each command. This method not only takes care of the specific connection for 
            //sending command, it also intializes the response for the connection.
            if (!request.IsBulk)
            {
                foreach (CommandBase command in request.Commands.Values)
                {

                    SendCommand(command);
                    requestCommand = command;


                }
            }
            else
            {
                foreach (KeyValuePair<Address, CommandBase> pair in request.Commands)
                {
                    Address ip = pair.Key;
                    CommandBase command = pair.Value;
                    bool optimizeConnection = (command.CommandType != CommandType.GET_NEXT_CHUNK) ||
                                              (command.CommandType == CommandType.GET_NEXT_CHUNK &&
                                               (!string.IsNullOrEmpty(command.intendedRecipient) || !_importHashmap));
                    Connection connection = VerifyServerConnectivity(ip, optimizeConnection);
                    if (connection == null && command.CommandType == CommandType.GET_NEXT_CHUNK)
                        throw new OperationFailedException("Enumeration has been modified");

                    SendCommand(connection, command, true);
                    requestCommand = command;
                }
            }

            bool reacquiredLock = true;
            int timeout = Convert.ToInt32(request.RequestTimeout);

            long startTime = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
            try
            {
                lock (request)
                {
                    while (timeout > 0)
                    {
                        if (request.IsAsync)
                            break;

                        if (request.Responses)
                        {
                            break;
                        }

                        timeout = Convert.ToInt32(request.RequestTimeout) - (int)((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - startTime);
                        reacquiredLock = Monitor.Wait(request, timeout);

                        if (!reacquiredLock)
                        {
                            if (request.IsRequestTimeoutReset)
                            {
                                timeout = (Convert.ToInt32(request.RequestTimeout) - (int)((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - startTime));
                                if (timeout > 0)
                                {
                                    reacquiredLock = true;
                                    reacquiredLock = Monitor.Wait(request, timeout);
                                }
                            }
                        }

                        if (!reacquiredLock && !request.Responses)
                        {
                            if (_perfStatsColl2 != null) _perfStatsColl2.DecrementRequestQueueSizeStats();

                            if (_logger.IsErrorLogsEnabled)
                            {
                                _logger.NCacheLog.Error("Broker.SendCommand", request.TimeoutMessage);
                            }
                            throw new OperationFailedException(request.TimeoutMessage);
                        }
                    }
                }
            }
            finally
            {
                lock (_requestTable.SyncRoot)
                {
                    if (!request.IsAsync && _requestTable.ContainsKey(request.RequestId))

                        _requestTable.Remove(request.RequestId);
                }

                if (_perfStatsColl2 != null) _perfStatsColl2.DecrementRequestQueueSizeStats();

            }
        }

        internal Address GetConnectionIP(CommandBase command)
        {
            Connection connection = _connection;
            if (command.key != null && command.key != string.Empty && this._importHashmap)
            {
                Address ip; 
                lock (_hashmapUpdateMutex)
                {
                    ip = this._pool.GetIp(command.key);
                }
                if (ip != null)
                {
                    connection = GetConnection(ip, true);
                    if (connection == null || (connection != null && !connection.IsConnected))
                    {
                        connection = TryPool();

                    }
                }
                //done everything to get a working connection.
            }

            return connection.ServerAddress;
        }

        internal void SendCommand(CommandBase command)
        {
            Connection connection = _connection;

            if (connection != null)
                connection.IntendedRecipientIPAddress = string.Empty;

            if (command.key != null && command.key != string.Empty && this._importHashmap)
            {
                Address ip; 

                lock (_hashmapUpdateMutex)
                {
                    ip = this._pool.GetIp(command.key);
                }

                if (ip != null)
                {
                    connection = GetConnection(ip, true);
                    if (connection == null || (connection != null && !connection.IsConnected))
                    {
                        connection = TryPool(); 
                    }
                }
                //done everything to get a working connection.
            }
            else if (this._importHashmap && !connection.IsConnected)
            {
                connection = _connection = TryPool();
            }

            //If not POR or Partitioned and connection is not connected, try to get another connection before sending command 
            if (!this._importHashmap && connection != null && !connection.IsConnected)
            {
                TryNextServer();
                connection = _connection;
            }
            SendCommand(connection, command, true);
        }

        internal void SendCommand(Connection connection, CommandBase command, bool checkConnected)
        {

            Address ip = connection.ServerAddress;
            if (checkConnected)
            {
                connection.StatusLatch.WaitForAny(ConnectionStatus.Connected | ConnectionStatus.Disconnected | ConnectionStatus.LoadBalance);
            }
            try
            {
                DoSendCommand(connection, command, checkConnected);
                if (_logger.IsDetailedLogsEnabled)
                    _logger.NCacheLog.Debug("Broker.SendCommand", "RequestID : " + command.RequestId + " " + command.CommandName + " sent to server " + connection.IpAddress);
            }
            catch (ConnectionException)
            {

                lock (_requestTable.SyncRoot)
                {
                    Request request = command.Parent;
                    if (!this._importHashmap)
                    {
                        if (request.RemoveResponse(ip))
                        {
                            _requestTable.Remove(request.RequestId);
                            if (_perfStatsColl2 != null) _perfStatsColl2.DecrementRequestQueueSizeStats();
                        }
                    }
                    else
                        request.ResetFailedResponse(connection.ServerAddress);
                }

                if (_perfStatsColl2 != null) _perfStatsColl2.DecrementRequestQueueSizeStats();


                if (!this._importHashmap)
                {
                    TryNextServer();
                }

                connection = this._connection;
                if (this._importHashmap && !connection.IsConnected)
                {
                    connection = GetConnection(ip, false);
                }

                try
                {
                    if (!this._importHashmap)
                    {
                        command.ResetBytes();
                        AddRequestToRequestTable(command.Parent);
                        DoSendCommand(connection, command, checkConnected);
                    }
                }
                catch (ConnectionException ce)
                {
                    if (_logger.IsErrorLogsEnabled)
                        _logger.NCacheLog.Error("Broker.SendCommand", "RequestID :" + command.RequestId + " " + command.CommandName + " can not sent to server " + connection.IpAddress);
                    if (!this._importHashmap)
                        throw new OperationFailedException("No server is available to process the request ");
                }
                catch (Exception e)
                {
                    if (_logger.IsErrorLogsEnabled)
                        _logger.NCacheLog.Error("Broker.SendCommand", "RequestID :" + command.RequestId + " " + command.CommandName + " can not sent to server " + connection.IpAddress + " " + e.ToString());
                    throw new OperationFailedException(e.Message, e);
                }
            }
            catch (OperationFailedException e)
            {
                if (_logger.IsErrorLogsEnabled)
                    _logger.NCacheLog.Error("Broker.SendCommand", "RequestID :" + command.RequestId + " " + command.CommandName + " can not sent to server " +connection.IpAddress + " " + e.ToString());

                throw;
            }
            catch (Exception e)
            {
                if (_logger.IsErrorLogsEnabled)
                    _logger.NCacheLog.Error("Broker.SendCommand", "RequestID :" + command.RequestId + " " + command.CommandName + " can not sent to server " + connection.IpAddress + " " + e.ToString());
                throw new OperationFailedException(e.Message, e);
            }
        }

        public void Dispose()
        {
            Dispose(false);
        }

        public void Dispose(bool disposingGracefully)
        {
            _isDisposing = true;

            try
            {
                if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.Dispose", "disposing...");
            }
            catch (Exception)
            {
            }

            Hashtable connections = this._pool.CloneConnectionTable();

            foreach (DictionaryEntry entry in connections)
            {
                Connection connection = entry.Value as Connection;
                if (connection != null)
                {
                    if (connection.IsConnected && disposingGracefully)
                    {
                        DisposeCommand command = new DisposeCommand();
                        Request request = this.CreateRequest(command);
                        this.ExecuteRequest(request, connection, true, false);
                    }

                    connection.Disconnect();
                    this._pool.Remove(connection.ServerAddress);
                    try
                    {
                        if (_logger.IsErrorLogsEnabled)
                            _logger.NCacheLog.Error("Broker.Dispose", connection.IpAddress + " disconnected");
                    }
                    catch (Exception)
                    {
                    }
                }
                _perfStatsColl.Dispose();
                connection.Dispose();
            }

            try
            {
                if (this._processor != null) this._processor.Stop();
                if (_eventProcessor != null) _eventProcessor.Stop();
            }
            catch (ThreadAbortException)
            {
            }

            try
            {
                if (_logger.NCacheLog != null)
                {
                    _logger.NCacheLog.Flush();
                    _logger.NCacheLog.Close();
                }
            }
            catch (Exception exception)
            {
            }
        }

        internal Connection VerifyServerConnectivity(Address serverIP, bool optimizeConnection)
        {
            Connection con = null;
            if (optimizeConnection)
            {
                con = GetConnection(serverIP, false);
                if (!con.IsConnected)
                {
                    con = TryPool();
                }
            }
            else
            {
                con = GetConnection(serverIP, true);
            }
            return con;
        }

        private void DoSendCommand(Connection connection, CommandBase command, bool checkConnected)
        {
            InitializeResponse(connection, command);
            command._cacheId = _cacheId;
            try
            {
                if (command is InitSecondarySocketCommand)
                {
                    connection.AssureSend(command.ToByte(), connection.SecondaryClientSocket, checkConnected);
                }
                else
                {
                    connection.AssureSend(command.ToByte(), connection.PrimaryClientSocket, checkConnected);
                }
            }
            catch (Exception e)
            {
                if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.Info("Broker.DoSendCommand", e.ToString());
                lock (_requestTable.SyncRoot)
                {
                    _requestTable.Remove(command.RequestId);
                }
                if (_perfStatsColl2 != null) _perfStatsColl2.DecrementRequestQueueSizeStats();

                if (e is ConnectionException)
                {
                    throw;
                }
                else
                    throw;
            }
        }

        private void AddRequestToRequestTable(Request request)
        {

            if (!(request.IsAsync && !request.IsAyncCallbackSpecified))
            {
                request.RequestId = this.RequestId;

                lock (_requestTable.SyncRoot)
                    _requestTable.Add(request.RequestId, request);

                if (_perfStatsColl2 != null)
                    _perfStatsColl2.IncrementRequestQueueSizeStats();

            }
        }

        private void RemoveRequestFromRequestTable(Request request)
        {
            bool requestExistedInTable = false;
            lock (_requestTable.SyncRoot)
            {
                requestExistedInTable = _requestTable.ContainsKey(request.RequestId);
                if (requestExistedInTable)
                    _requestTable.Remove(request.RequestId);
            }
            if (_perfStatsColl2 != null && requestExistedInTable) _perfStatsColl2.DecrementRequestQueueSizeStats();

        }

        /// <summary>
        /// Adds request to request table. Depending on type of type is request id is set here.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="command"></param>
        private void InitializeResponse(Connection connection, /*Request request*/ CommandBase command)
        {
            lock (_requestTable.SyncRoot)
            {
                command.Parent.InitializeResponse(connection.ServerAddress);
            }
        }

        private void RemoveResponse(Connection connection, /*Request request*/ CommandBase command)
        {
            command.Parent.RemoveResponse(connection.ServerAddress);
        }

        private void StopServices()
        {
            if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StopService", "stopping services...");
            Hashtable connections = this._pool.CloneConnectionTable();
            foreach (DictionaryEntry entry in connections)
            {
                Connection connection = entry.Value as Connection;
                if (connection != null && connection.IsConnected)
                {
                    connection.Disconnect();
                    this._pool.Remove(connection.ServerAddress);
                }

            }
        }

        public void ServerLost(Address ip, bool forcedDisconnected)
        {
            if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.CriticalInfo("ServerLost", "Server lost " + ip + "; forcedDisconnected = " + forcedDisconnected);
            try
            {
                if (this._importHashmap)
                {
                    if (!forcedDisconnected)
                    {
                        try
                        {
                            ReregisterEvents(ip);
                            Connection connection = this._pool[ip];
                            if (connection != null && !connection.IsReconnecting)
                            {
                                this._processor.Enqueue(new ReconnectTask(this, connection));
                            }
                        }
                        catch (OperationFailedException exc)
                        {
                            if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.ServerLost", exc.Message);
                        }
                    }
                }
                else
                {
                    this._pool.Remove(ip);
                }

                ResetBroker(ip);
                this._clientConfig.RemoveServer(new RemoteServer(ip.IpAddress, ip.Port));

                if (forcedDisconnected || _connection.StatusLatch.IsAnyBitsSet(ConnectionStatus.Connecting)) return;
                try
                {
                    if (!this._connection.IsConnected && !this._importHashmap)
                    {
                        try
                        {
                            TryNextServer();
                        }
                        catch (Exception ex)
                        {
                            if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.ServerLost", ex.ToString());
                        }
                    }
                }
                finally
                {

                }
            }
            catch (Exception e)
            {
                if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.ServerLost", e.ToString());
            }
        }

        private void ReregisterEvents(Address ip)
        {
            ReregisterEvents(this._pool[ip]);
        }

        private void ReregisterEvents(Connection connection)
        {
            if (connection != null && connection.NotifRegistered)
            {
                connection.NotifRegistered = false;

                Connection selected = this._connection;

                if (!selected.IsConnected)
                {
                    selected = TryPool();
                }

                if (selected != null && selected.IsConnected)
                {
                    this._cache.ReRegisterGeneralNotification(selected);

                    selected.NotifRegistered = true;
                }


            }
        }

        /// <summary>
        /// Reset request table. only those command from table are removed who are sent to the provided
        /// ip address
        /// </summary>
        /// <param name="ip">ip address of server to which the connection is lost</param>
        private void ResetBroker(Address ip)
        {
            try
            {
                IEnumerator iter = null;

                lock (_requestTable.SyncRoot)
                {
                    iter = _requestTable.Values.GetEnumerator();
                    ArrayList pulsableRequests = new ArrayList();

                    while (iter.MoveNext())
                    {
                        Request request = (Request) iter.Current;
                        if (request.ExpectingResponseFrom(ip))
                        {
                            lock (request)
                            {
                                request.Reset(ip);
                                Monitor.PulseAll(request);
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                NCacheLog.Error("Broker.ResetBroker", ex.ToString());
            }
        }



        /// <summary>
        /// Try to connect with next server in config
        /// </summary>
        /// <returns>new connection</returns>
        private void TryNextServer()
        {

            bool connected = false;
            RemoteServer startingServer = null;
            int retries = this._connectionRetries;
            OperationNotSupportedException exceptionThrown = null;

            try
            {
                _lock.AcquireWriterLock(_connectionMutexTimeout);
            }
            catch (Exception)
            {
                return; //lock could not be granted before the timeout expires.
            }

            try
            {
                CheckRetryConnectionDelay(); //[ Checking if retry connection Interval is over or not]
                if (!_retryConnection)
                    return;

                while (retries-- > 0 && !_isDisposing)
                {
                    try
                    {
                        if (!this._connection.IsConnected)
                        {
                            this._connection.StatusLatch.SetStatusBit(ConnectionStatus.Connecting, ConnectionStatus.Connected | ConnectionStatus.Disconnected);

                            if (_clientConfig == null)
                                _clientConfig = new ClientConfiguration(_cacheId);

                            int nretries = 3;
                            while (true)
                            {
                                try
                                {
                                    _clientConfig.LoadConfiguration();
                                    break;
                                }
                                catch (System.IO.IOException ie)
                                {
                                    if (--nretries == 0) throw ie;
                                    System.Threading.Thread.Sleep(500);
                                }
                            }

                            if (_clientConfig.ServerCount > 0)
                            {
                                RemoteServer nextServer = _clientConfig.NextServer;
                                startingServer = nextServer;
                                while (!connected)
                                {
                                    if (nextServer == null) break;
                                    if (nextServer.IP != null)
                                    {
                                        for (int i = 0; i < nextServer.PortRange; i++)
                                        {
                                            try
                                            {
                                                if (!connected)
                                                {
                                                    Exception exception = null;
                                                    connected = ConnectRemoteServer(this._connection, nextServer.IP, nextServer.Port + i, _clientConfig.BalanceNodes, this._importHashmap, true, ref exception);
                                                }
                                                if (connected) break;
                                            }
                                            catch (OperationNotSupportedException operationException)
                                            {
                                                exceptionThrown = operationException;
                                            }
                                        }
                                    }
                                    if (!connected)
                                    {
                                        nextServer = _clientConfig.NextServer;
                                        if (startingServer.Equals(nextServer)) break;
                                    }
                                }
                                //if the connection is established, exit the outer loop.
                                //otherwise sleep for the sleep interval and retry.
                                if (connected) break;
                                System.Threading.Thread.Sleep(_retryInterval);
                                continue;
                            }
                            else
                            {
                                throw new ConfigurationException("'client.ncconf' not found or does not contain server information");
                            }
                        }
                        else{
                            connected = true;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                //set the connection status
                byte setStatus = connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
                byte unsetStatus = (byte) ((!connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected) | ConnectionStatus.Connecting);
                _connection.StatusLatch.SetStatusBit(setStatus, unsetStatus);

                _retryConnection = connected;
                //release the lock
                _lock.ReleaseWriterLock();

                if (!connected)
                {
                    if (exceptionThrown != null)
                        throw exceptionThrown;
                }
            }
        }

        private Connection ReconnectServer(Connection connection, RemoteServer nextServer)
        {
            bool connected = false;
            int retries = this._connectionRetries;
            OperationNotSupportedException exceptionThrown = null;
            if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.CriticalInfo("ReconnectServer", "Trying to reconnect to :" + connection.IpAddress);
            try
            {
                _lock.AcquireWriterLock(_connectionMutexTimeout);
            }
            catch (Exception)
            {
                return connection; //lock could not be granted before the timeout expires.
            }

            try
            {
                CheckRetryConnectionDelay(); //[ Checking if retry connection Interval is over or not]
                if (!_retryConnection)
                    return connection;

                while (retries-- > 0 && !_isDisposing)
                {
                    try
                    {
                        if (!connection.IsConnected)
                        {
                            connection.StatusLatch.SetStatusBit(ConnectionStatus.Connecting, ConnectionStatus.Connected | ConnectionStatus.Disconnected);

                            if (nextServer == null) break;
                            if (nextServer.IP != null)
                            {
                                for (int i = 0; i < nextServer.PortRange; i++)
                                {
                                    try
                                    {
                                        if (!connected)
                                        {
                                            Exception exception = null;
                                            connected = ConnectRemoteServer(connection, nextServer.IP, nextServer.Port + i, this._balanceNode, this._importHashmap, true, ref exception);
                                        }
                                        if (connected)
                                        {
                                            break;
                                        }
                                    }

                                    catch (OperationNotSupportedException operationException)
                                    {
                                        exceptionThrown = operationException;
                                    }
                                }
                            }
                            System.Threading.Thread.Sleep(_retryInterval);
                            continue;
                        }
                        else
                        {
                            connected = true;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                byte setStatus = connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
                byte unsetStatus = (byte) ((!connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected) | ConnectionStatus.Connecting);
                connection.StatusLatch.SetStatusBit(setStatus, unsetStatus);

                _retryConnection = connected; //[ Connection is up again so we can retry ]

                _lock.ReleaseWriterLock();

            }

            return connection;
        }

        /// <summary>
        /// Get connection from pool. If disconnected, the function tries to make connection again.
        /// If strict match is set to false, any present connection will be returned from pool
        /// </summary>
        /// <param name="ip">expected connection's ip</param>
        /// <param name="strictMatch">specifies if ip is strictly matched or any connection from pool
        /// is returned incase connection not found or disconnected</param>
        /// <returns>connection. it may be connected or disconnected</returns>
        private Connection GetConnection(Address ip, bool strictMatch)
        {
            Connection connection = this._pool[ip];
            if (connection != null)
            {
                if (!connection.IsConnected)
                {
                    if (connection.NotifRegistered) ReregisterEvents(connection);
                    if (!connection.IsReconnecting)
                    {
                        this._processor.Enqueue(new ReconnectTask(this, connection));
                    }
                }
                else
                    return connection;
            }
            if (this._importHashmap && !strictMatch)
            {
                connection = TryPool();
            }
            if (connection != null)
                connection.IntendedRecipientIPAddress = string.Empty;
            return connection;
        }

        private bool NeedsNotifRegistration()
        {
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                Hashtable connections = this._pool.Connections;
                lock (connections.SyncRoot)
                {
                    foreach (DictionaryEntry entry in connections)
                    {
                        Connection connection = entry.Value as Connection;
                        if (connection.IsConnected) return false;
                    }
                    return true;
                }
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Try connecting to the connection provided
        /// </summary>
        /// <returns>True is connection was successful, false otherwise</returns>
        internal bool TryConnecting(Connection connection, ref Exception exception)
        {
            bool connected = false;
            try
            {
                this._lock.AcquireWriterLock(Timeout.Infinite);

                connected = (connection != null && connection.IsConnected);
                if (!connected)
                {
                    try
                    {

                        CheckRetryConnectionDelay(); //[ Checking if retry connection Interval is over or not]
                        if (!_retryConnection)
                            return false;

                        connection.StatusLatch.SetStatusBit(ConnectionStatus.Connecting,
                            ConnectionStatus.Connected | ConnectionStatus.Disconnected);

                        connected = ConnectRemoteServer(connection, connection.Address, this._port, false, false,
                            NeedsNotifRegistration(), ref exception);

                        if (connected)
                        {
                            if (_logger.IsDetailedLogsEnabled)
                                _logger.NCacheLog.Info("Broker.TryConnecting", "Connection established with " + connection.IpAddress);
                        }

                    }

                    catch (OperationNotSupportedException)
                    {
                    }
                    finally
                    {
                        byte setStatus = connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
                        byte unsetStatus =
                            (byte)
                                ((!connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected) |
                                 ConnectionStatus.Connecting);
                        connection.StatusLatch.SetStatusBit(setStatus, unsetStatus);

                        _retryConnection = connected;
                    }
                }
            }
            finally
            {
                this._lock.ReleaseWriterLock();
            }
            return connected;
        }


        private Connection TryPool()
        {
            Connection connection = null;

            if (_clientConfig == null)
                _clientConfig = new ClientConfiguration(_cacheId);

            int retries = 3;
            while (true && !_isDisposing)
            {
                try
                {
                    _clientConfig.LoadConfiguration();
                    break;
                }
                catch (System.IO.IOException ie)
                {
                    if (--retries == 0) throw ie;
                    System.Threading.Thread.Sleep(500);
                }
            }

            RemoteServer nextServer = _clientConfig.NextServer;
            RemoteServer startingServer = nextServer;

            while (true && !_isDisposing)
            {
                if (_clientConfig.ServerCount > 0)
                {
                    connection = this._pool[new Address(nextServer.IP.ToString(), nextServer.Port)];

                    if (connection != null && connection.IsConnected) break;
                    else
                    {
                        nextServer = _clientConfig.NextServer;
                        if (startingServer.Equals(nextServer)) break;
                    }
                }
                else break;
            }

            if (connection == null)
            {
                while (true && !_isDisposing)
                {
                    if (_clientConfig.ServerCount > 0)
                    {
                        if (this._pool.Contains(new Address(nextServer.IP.ToString(), nextServer.Port)))
                        {
                            connection = this._pool[new Address(nextServer.IP.ToString(), nextServer.Port)];
                        }
                        else
                        {
                            connection = new Connection(this, this._commandReieved, this._serverLost, this._logger,  _perfStatsColl, _responseIntegrator, _clientConfig.BindIP, _cacheId);
                        }

                        if (!connection.IsConnected)
                        {
                            connection = ReconnectServer(connection, nextServer);

                            if (connection.IsConnected)
                            {
                                if (!this._pool.Contains(new Address(nextServer.IP.ToString(), nextServer.Port)))
                                    this._pool[new Address(nextServer.IP.ToString(), nextServer.Port)] = connection;
                                break;
                            }
                            else
                            {
                                nextServer = _clientConfig.NextServer;
                                if (startingServer.Equals(nextServer)) break;
                            }
                        }
                        else
                            break;
                    }
                    else break;
                }
            }

            return connection;
        }

        /// <summary>
        /// Try to connect with the server. The connection object will be used to connect with the ip
        /// address on port provided
        /// </summary>
        /// <param name="connection">This object will be used to connect with server</param>
        /// <param name="server">remote server information</param>

        /// <param name="registerNotifs">if re registring of events is needed</param>
        /// <returns>true if connecion is established, false otherwise</returns>
        private bool ConnectRemoteServer(Connection connection, RemoteServer server, bool registerNotifs)
        {
            bool connected = false;
            Exception exception = null;

            if (server != null && server.IP != null)
            {
                connected = ConnectRemoteServer(connection, server.IP, server.Port, this._balanceNode, this._importHashmap, registerNotifs,
                    ref exception);
            }

            return connected;
        }

        /// <summary>
        /// Checks if the retry connection interval is over and sets retry connection flag to true.
        /// </summary>        
        private void CheckRetryConnectionDelay()
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan span = currentTime.Subtract(_retryConnectionStartTime);
            if (span.TotalMinutes >= _clientConfig.RetryConnectionDelay)
            {
                _retryConnectionStartTime = DateTime.Now;
                _retryConnection = true;
            }
        }

        /// <summary>
        /// Try to connect with the server. The connection object will be used to connect with the ip
        /// address on port provided
        /// </summary>
        /// <param name="connection">This object will be used to connect with server</param>
        /// <param name="addr">ip address of server</param>
        /// <param name="port">port on which socket server is running</param>
        /// <param name="balanceNodes">balance clients load on servers</param>
        /// <param name="importHashmap">if hashmap is to be fetched from connecting server</param>
        /// <param name="registerNotifs">if re registring of events is needed</param>
        /// <returns>true if connecion is established, false otherwise</returns>
        private bool ConnectRemoteServer(Connection connection, IPAddress addr, int port, bool balanceNodes, bool importHashmap,
            bool registerNotifs, ref Exception exception)
        {
            bool connected = connection.Connect(addr, port);
            //remove old entry from message integrator if exist
            _responseIntegrator.RemoveServer(new Address(addr, port));
            if (connected)
            {
                try
                {
                    Dictionary<string, int> runningServers = null;
                    //Populating the server list at initialization time.
                    if (_clientConfig.IPMappingConfigured)
                    {
                        GetServerMapping(connection, true);
                    }

                    if (balanceNodes)
                    {
                        CommandResponse response = IsOptimalServer(connection, addr, port);
                        RemoteServer rm = _clientConfig.GetMappedServer(response.ServerIPAddress.ToString(), response.ServerPort);
                        if (response != null && (addr.ToString() != rm.Name || port != rm.Port))
                        {
                            connection.Disconnect();
                            connection.Connect(rm.Name, rm.Port);
                        }
                    }
                   
                    if (_logger.IsDetailedLogsEnabled)
                        _logger.NCacheLog.Info("Broker.ConnectRemoteServer",  "[Local : (" + connection.PrimaryClientSocket.LocalEndPoint.ToString() + ") Server : (" + addr.ToString() + ":" + port + ")] connected successfully");

                    InitializeCache(connection, addr, port, balanceNodes);
                    if (connection.SupportDualSocket) InitializeSecondarySocket(connection, addr, port);
                    runningServers = GetRunningServers(connection, addr, port);
                    connection.Init();

                    if (runningServers != null)
                    {
                        int outPort;

                        foreach (string str in runningServers.Keys)
                        {
                            RemoteServer rServer = new RemoteServer();
                            runningServers.TryGetValue(str, out outPort);
                            rServer.Name = str;
                            rServer.IP = System.Net.IPAddress.Parse(str);
                            rServer.Port = outPort;
                            rServer.Priority = short.MaxValue;
                            _clientConfig.AddServer(rServer);
                        }
                    }

                    if (_logger.IsDetailedLogsEnabled)
                        _logger.NCacheLog.Info("Broker.ConnectRemoteServer","[Local : (" + connection.PrimaryClientSocket.LocalEndPoint.ToString() + ") Server : (" + addr.ToString() + ":" + port + ")] initialized cache successfully");
                }

                catch (OperationNotSupportedException ons)
                {
                    if (_logger.IsErrorLogsEnabled)
                        _logger.NCacheLog.Error("Broker.ConnectRemoteServer", ons.ToString());
                    connection.Disconnect();
                    connected = false;
                    throw;
                }
                catch (Exception e)
                {
               
                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.ConnectRemoteServer", e.ToString());
                    connection.Disconnect();
                    connected = false;

                    exception = e;
                }
            }
            else
            {
                if (_logger.IsErrorLogsEnabled)
                    _logger.NCacheLog.Error("Broker.ConnectRemoteServer","Could not connect to server (" + addr.ToString() + ":" + port + ")");
            }
            if (connected)
            {
                if (_processor != null) this._processor.Start();

                connection.StatusLatch.SetStatusBit(ConnectionStatus.Connected, ConnectionStatus.Disconnected | ConnectionStatus.Connecting);

                this._serverIP = connection.ServerAddress;
                this._port = connection.Port;

                this._pool.Add(connection.ServerAddress, connection);

                if (importHashmap)
                {
                    this.GetHashmap(connection);
                }

                if (registerNotifs)
                {
                    _cache.ReRegisterGeneralNotification(connection);
                    connection.NotifRegistered = true;
                }

                if (this._importHashmap)
                {
                    _cache.RegisterHashmapChangedEvent(connection);
                }
                _cache.GetTypeInfoMap(connection);
            }
            return connected;
        }
        private Dictionary<string, int> GetRunningServers(Connection conn, IPAddress coonectedServerAddress, int port)
        {
            GetRunningServersCommand command = new GetRunningServersCommand(_cacheId);
            Request request = new Request(false, _operationTimeout);
            request.AddCommand(conn.ServerAddress, command);

            ExecuteRequest(request, conn, false, false);
            CommandResponse runningServers = conn.RecieveCommandResponse();

            if (runningServers != null)
            {
                runningServers.ParseResponse();
                return runningServers.RunningServer;
            }

            return null;

        }
        private void GetServerMapping(Connection connection, bool initialRequest)
        {
            GetServerMappingCommand command = new GetServerMappingCommand();
            CommandResponse serverMapRes = null;
            Request request = CreateRequest(command);

            try
            {
                if (initialRequest)
                {
                    ExecuteRequest(request, connection, false, false);
                    serverMapRes = connection.RecieveCommandResponse();
                    lock (_requestTable.SyncRoot)
                    {
                        _requestTable.Remove(request.RequestId);
                    }
                    if (_perfStatsColl2 != null) _perfStatsColl2.DecrementRequestQueueSizeStats();
                }
                else
                {
                    ExecuteRequest(request);
                    serverMapRes = request.Response;
                }
                if (serverMapRes != null)
                {
                    serverMapRes.ParseResponse();
                }
                _clientConfig.AddMappedServers(serverMapRes.ServerMappingList);
            }
            catch (Exception exp)
            {
                if (_logger.IsDetailedLogsEnabled)
                {
                    _logger.NCacheLog.Debug(exp.Message);
                }
            }
        }
        
        private CommandResponse IsOptimalServer(Connection connection, IPAddress connectedServerAddress, int port)
        {
            GetOptimalServerCommand command = new GetOptimalServerCommand(_cacheId);
            Request request = new Request(false, _operationTimeout);
            request.AddCommand(connection.ServerAddress, command);

            ExecuteRequest(request, connection, false, false);
            CommandResponse balanceNodeRes = connection.RecieveCommandResponse();

            lock (_requestTable.SyncRoot)
            {
                _requestTable.Remove(request.RequestId);
            }

            if (_perfStatsColl2 != null) _perfStatsColl2.DecrementRequestQueueSizeStats();

            if (balanceNodeRes != null)
                balanceNodeRes.ParseResponse();

            return balanceNodeRes;
        }

        private sealed class ReconnectTask : AsyncProcessor.IAsyncTask
        {
            private short _retries = 3;
            private Connection _connection;
            private Broker _parent;

            public ReconnectTask(Broker parent, Connection connection)
            {
                connection.IsReconnecting = true;
                this._parent = parent;
                this._connection = connection;
            }

            #region IAsyncTask Members

            public void Process()
            {
                try
                {
                    if (this._connection == null) return;
                    if (this._connection.IsConnected) return;
                    while (_retries-- > 0)
                    {
                        Thread.Sleep(2000); //waite for 2 seconds before retrying
                        try
                        {
                            Exception exception = null;

                            if (this._parent.TryConnecting(this._connection, ref exception))
                                break;

                            if (exception != null)
                            {
                                if (exception.Message.StartsWith("System.Exception: Cache is not registered"))
                                {
                                    Connection connection = this._parent.TryPool();
                                    if (connection != null && connection.IsConnected)
                                    {
                                        this._parent.GetHashmap(connection);
                                        if (this._parent.Logger.IsErrorLogsEnabled)
                                        {
                                            this._parent.Logger.NCacheLog.Error("ReconnectTask.Process", "Connection [" + this._connection.IpAddress + "] Exception->" + exception.ToString());
                                        }
                                    }
                                    break;
                                }
                                if (exception.Message.StartsWith("System.Exception: Cache is not running") && _retries == 0) // then wait till the retries
                                {
                                    Connection connection = this._parent.TryPool();
                                    if (connection != null && connection.IsConnected)
                                    {
                                        this._parent.GetHashmap(connection);
                                        if (this._parent.Logger.IsErrorLogsEnabled)
                                        {
                                            this._parent.Logger.NCacheLog.Error("ReconnectTask.Process", "Connection [" + this._connection.IpAddress + "] Exception->" + exception.ToString());
                                        }
                                    }
                                    break;
                                }
                                if (this._parent.Logger.IsErrorLogsEnabled)
                                {
                                    this._parent.Logger.NCacheLog.Error("ReconnectTask.Process", "Connection [" + this._connection.IpAddress + "] Exception " + exception.ToString());
                                }
                            }
                        }
                        catch (OperationNotSupportedException ons)
                        {
                            break;
                        }
                        catch (Exception e)
                        {
                            if (_parent.Logger.IsErrorLogsEnabled)
                                _parent.Logger.NCacheLog.Error("ReconnectTask.Process", e.ToString());
                            break;
                        }
                    }
                }
                finally
                {
                    if (_parent.Logger.NCacheLog != null) _parent.Logger.NCacheLog.Flush();
                    if (this._connection != null) this._connection.IsReconnecting = false;
                }
            }

            #endregion
        }

        private class HashMapStatus
        {
            internal const byte INITIALIZE = 1;
            internal const byte UNINITIALIZE = 2;
        }
        
    }
}
