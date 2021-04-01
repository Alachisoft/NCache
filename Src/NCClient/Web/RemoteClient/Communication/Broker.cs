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

using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Caching.Statistics.CustomCounters;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Management.Statistics;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Serialization.Formatters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ConfigurationException = Alachisoft.NCache.Runtime.Exceptions.ConfigurationException;
using EncryptionUtil = Alachisoft.NCache.Common.EncryptionUtil;
using EventId = Alachisoft.NCache.Caching.EventId;
using Exception = System.Exception;

namespace Alachisoft.NCache.Client
{
    internal sealed class Broker
    {
        internal RemoteCache _cache = null;
        internal ClientConfiguration _clientConfig;
        private OnCommandRecieved _commandReieved = null;
        private OnServerLost _serverLost = null;
       

#if !DEVELOPMENT
        private delegate void OnNewHashmapRecieved(NewHashmap newHashmap, int bucketSize);
        private OnNewHashmapRecieved _onNewHashmapRecieved = null;
#endif

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
        private ReaderWriterLock _lock = new ReaderWriterLock();
        private int _connectionMutexTimeout = Timeout.Infinite;
        private int _connectionRetries = 5;
        private int _retryInterval = 1000;
        internal int RetryInterval
        {
            get { return  _retryInterval; }
        }
        private int _connectionTimeout = 5000;

        
        private bool _retryConnection = true;
        private DateTime _retryConnectionStartTime = DateTime.Now;

        private string _cacheId;
        private Address _serverIP;
        private string _licenceCode;
        private int _port;
        private int _priority = 1;
        private int _itemSizeThreshHold = 0;

        private int _operationTimeout = 90000;//default 90 sec.
        private Logs _logger = new Logs();
        public Logs Logger
        {
            get { return  _logger; }
        }


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
        private static long _requestsPerSecond = 50; // for lowering number of requests for develop[er edition
        private RequestModerator requestModerator = new RequestModerator();

        
        internal StatisticsCounter _perfStatsColl;

        public bool _perfStatsEnabled = false;
        
        private bool _isDisposing = false;
        private bool _connectingFirstTime = true;
        private WaitCallback _bulkEventCallback;
        object _hashmapUpdateMutex = new object();
     
        private bool _isPersistEnabled = false;
        private int _persistenceInterval;

        private Hashtable _shutdownServers = new Hashtable();
        private long _shutdownTimeout = 180;

        private MutableQueue<Address> connectionQueue = new MutableQueue<Address>();
        private ThrottlingManager _throttleManager = new ThrottlingManager(100);
        private AddressUtil _addressUtil;
        private bool _isLocalAddress = false;
        private static IDictionary<string, long> _requestDic = new Dictionary<string, long>();
        object _requestsLock = new object();
        long _allowedRequests = 200000;
        private Runtime.Caching.ClientInfo _clientInfo;
        Latch _hashMapStatus = new Latch(HashMapStatus.UNINITIALIZE);
        private InitCommand _command;
        private IDictionary<Address, ReconnectTask> _reconnectTasks = new Dictionary<Address, ReconnectTask>();
        
        public bool IsPersistenceEnabled
        {
            get { return  _isPersistEnabled; }
        }

        public int PersistInterval
        {
            get { return  _persistenceInterval; }
        }

        internal int ItemSizeThreshHold
        {
            get { return  _itemSizeThreshHold; }
        }

        private long RequestId
        {
            get { return  Interlocked.Increment(ref _requestId); }
        }

        public static int ForcedViewId
        {
            get { return  forcedViewId; }
        }

        internal long ClientLastViewId
        {
            get { return  this._pool.LastViewId; }
        }

        public int OperationTimeOut
        {
            get { return  _operationTimeout; }
        }
		
		
        internal Broker(RemoteCache cache,  CacheConnectionOptions cacheConnectionOptions, StatisticsCounter statsCol, Runtime.Caching.ClientInfo clientInfo)
            : this(cache, true, statsCol, cacheConnectionOptions)
        {
            _clientInfo = clientInfo;
        }

        public IPAddress NodeIP
        {
            get { return  _nodeIP; }
            set
            {
                _nodeIP = value;
            }
        }

        public int NewServerPort
        {
            get { return  _newServerPort; }
            set { _newServerPort = value; }
        }


        private void CheckPendingRequests(string cacheId)
        {
            long val = 0;
            lock (_requestsLock)
            {
                if (_requestDic.ContainsKey(cacheId.ToLower()))
                {
                    _requestDic.TryGetValue(cacheId.ToLower(), out val);
                    if (val >= 0)
                    {
                        if (val >= _allowedRequests)
                        {
                            throw new LicensingException("Clients running under DEV license cannot send more than 200,000 requests to remote cache.");
                        }

                        val += 1;
                        _requestDic[cacheId.ToLower()] = val;

                    }

                }
                else
                {
                    _requestDic.Add(cacheId.ToLower(), 1);
                }

            }
        }
        private Broker(RemoteCache cache, bool importHashMap, StatisticsCounter perfStatsColl, CacheConnectionOptions cacheConnectionOptions)
        {
            _bulkEventCallback = new WaitCallback(RaiseBulkEvent);
            _clientConfig = new ClientConfiguration(cache.CacheId, cacheConnectionOptions);
            _cache = cache;
            _licenceCode = "LicenceCode";
            _balanceNode = _clientConfig.BalanceNodes;
            _importHashmap = _clientConfig.ImportHashmap;

            _operationTimeout = _clientConfig.ClientRequestTimeout;
            _connectionTimeout = _clientConfig.ConnectionTimeout;
            _connectionRetries = 3;
            _retryInterval = 3;
         
            _perfStatsColl = perfStatsColl;


            int pid = AppUtil.CurrentProcess.Id;
            string instanceName = "Client." + cache.CacheId + "." + pid;

            if (_perfStatsColl == null || !_perfStatsColl.InstanceName.Equals(instanceName))
            {
                _perfStatsColl = new PerfStatsCollector(instanceName, 0);
            }

            _itemSizeThreshHold = _clientConfig.ItemSizeThreshHold;

            this._commandReieved = new OnCommandRecieved(CommandReceived);
            this._serverLost = new OnServerLost(ServerLost);
            this._requestTable = HashVector.Synchronized(new HashVector(10000, 0.75f));
            this._pool = new ConnectionPool();
                     
        }

        internal ServerInfo GetInitialServer()
        {
            ServerInfo serverInfo = new ServerInfo();
            if (_clientConfig._cacheConnectionOptions.ServerList.Count > 0)
            {
                serverInfo = _clientConfig._cacheConnectionOptions.ServerList[0];
            }
            return serverInfo;

        }

        internal void StartServices(string cacheId, string server, int port)
        {
            try
            {
                this._cacheId = cacheId;


                if (!string.IsNullOrEmpty(System.Configuration.ConfigurationSettings.AppSettings.Get("NCacheClient.AsynchronousEventNotification")))
                {
                    try
                    {
                        _notifyAsync = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings.Get("NCacheClient.AsynchronousEventNotification"));
                    }
                    catch (Exception ex)
                    {
                        throw new OperationFailedException(ErrorCodes.Common.INVALID_VALUE_ASYNC_EVENT_NOTIF, ErrorMessages.GetErrorMessage(ErrorCodes.Common.INVALID_VALUE_ASYNC_EVENT_NOTIF));
                    }

                    if (!_notifyAsync)
                    {
                        if (!string.IsNullOrEmpty(System.Configuration.ConfigurationSettings.AppSettings.Get("NCacheClient.NumberofEventProccesingThreads")))
                        {
                            try
                            {
                                _asyncProccesorThreadCount = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings.Get("NCacheClient.NumberofEventProccesingThreads"));
                            }
                            catch (Exception e)
                            {
                                throw new OperationFailedException(ErrorCodes.Common.NUMBER_OF_EVENTS_PROCESSING_THREADS, ErrorMessages.GetErrorMessage(ErrorCodes.Common.NUMBER_OF_EVENTS_PROCESSING_THREADS));
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
                { }

                bool enable_logs = false;
                bool detailed_logs = false;

                if (System.Configuration.ConfigurationSettings.AppSettings.Get("enableNCWebLogs") != null)
                {
                    enable_logs = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["enableNCWebLogs"]);
                }
                else
                    enable_logs = _clientConfig.EnableClientLogs;

                if (System.Configuration.ConfigurationSettings.AppSettings.Get("enableDetailedNCWebLogs") != null)
                {
                    detailed_logs = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["enableDetailedNCWebLogs"]);
                }
                else
                    detailed_logs = _clientConfig.EnableDetailedClientLogs;

                InitializeLogs(enable_logs, detailed_logs);

                if (System.Configuration.ConfigurationSettings.AppSettings.Get("enablePerfStats") != null)
                {
                    _perfStatsEnabled = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["enablePerfStats"]);
                }

                if (_perfStatsEnabled)
                {
                    _perfStatsColl.InitializePerfCounters(false);
                }


                int conTimeout = _connectionRetries * (_connectionTimeout + _retryInterval);
                if (conTimeout > 0) _connectionMutexTimeout = conTimeout;
                if (_operationTimeout < 60000) _operationTimeout = 60000; //minimum timeout is 60 seconds.

                _connection = new Connection(this, _commandReieved, _serverLost, _logger, _perfStatsColl, _responseIntegrator, _clientConfig.BindIP, this._cacheId);

                ServerInfo remoteServer = new ServerInfo(server, port);

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

                    catch (SecurityException se)
                    {
                        if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StartServices", se.ToString());
                    }

                    catch (OperationNotSupportedException oe)
                    {
                        if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StartServices", oe.ToString());
                    }
                    catch (InternalCommandException ex)
                    {
                        if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StartServices", ex.ToString());
                    }
                }
                if (!IsConnected)
                {
                    try
                    {
                        TryNextServer();
                    }

                    catch (SecurityException ex)
                    {
                        if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StartServices", ex.ToString());
                        throw;
                    }

                    catch (OperationNotSupportedException oe)
                    {
                        if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StartServices", oe.ToString());
                        throw;
                    }

                }

                if (_clientConfig.EnableKeepAlive)
                {
                    try
                    {
                        _connectionPinger = new ConnectionKeepAlive(this, _clientConfig.KeepAliveInterval);
                        _connectionPinger.Start();
                    }
                    catch (Exception ex)
                    {
                        if (_logger.IsErrorLogsEnabled)
                            _logger.NCacheLog.Error("Broker.StartServices", "Problem occured while starting 'ConnectionKeepAlive'" + ex.ToString());
                    }
                }

                // Console.WriteLine("EnablePipelining: "+ _clientConfig.EnablePipelining);
               
            }
            catch(Exception)
            {
                StopWriter();
                throw;
            }
        }

        ConnectionKeepAlive _connectionPinger;

        private void InitializeLogs(bool enable_logs, bool detailed_logs)
        {
            if (enable_logs)
            {
                Logs localLogger = new Logs();
                localLogger.IsErrorLogsEnabled = enable_logs;
                if (localLogger.IsErrorLogsEnabled)
                    localLogger.IsDetailedLogsEnabled = detailed_logs;

                int pid = AppUtil.CurrentProcess.Id;
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

        int serverMissingEventCount;
        int missingEventCount;
        int duplicates;
        int peristentDuplicates;
        int eventsAlreadyArrived;
        Hashtable eventsHistory = new Hashtable();
        List<EventId> missingEvents = new List<EventId>();
        private bool _tryPool;
        private bool _hashmapError;
        private int _poolSize;
        private bool _connectionRectified;
        private Thread _dedicatedWriter;

   
        private Alachisoft.NCache.Persistence.EventType ConvertEventType(EventInfo.EventType protoEventType)
        {
            switch (protoEventType)
            {
                case Common.Protobuf.EventInfo.EventType.CACHE_CLEARED_EVENT:
                    return Alachisoft.NCache.Persistence.EventType.CACHE_CLEARED_EVENT;

               
                case Common.Protobuf.EventInfo.EventType.ITEM_ADDED_EVENT:
                    return Alachisoft.NCache.Persistence.EventType.ITEM_ADDED_EVENT;

                case Common.Protobuf.EventInfo.EventType.ITEM_REMOVED_CALLBACK:
                    return Alachisoft.NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK;

                case Common.Protobuf.EventInfo.EventType.ITEM_REMOVED_EVENT:
                    return Alachisoft.NCache.Persistence.EventType.ITEM_REMOVED_EVENT;

                case Common.Protobuf.EventInfo.EventType.ITEM_UPDATED_CALLBACK:
                    return Alachisoft.NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK;

                case Common.Protobuf.EventInfo.EventType.ITEM_UPDATED_EVENT:
                    return Alachisoft.NCache.Persistence.EventType.ITEM_UPDATED_EVENT;
            }
            return Alachisoft.NCache.Persistence.EventType.ITEM_UPDATED_EVENT;
        }

        internal void InitializeCache(Connection connection, IPAddress address, int port, bool balanceNodes)
        {
            _command = new InitCommand(_cache.ClientID, _cacheId, connection.GetClientLocalIP(), connection.Address, _clientInfo, _operationTimeout);
            Request request = new Request(false, _operationTimeout);
            request.AddCommand(connection.ServerAddress, _command);
            ExecuteRequest(request, connection, false, false);
            connection.Optimized = true;
            CommandResponse res = connection.RecieveCommandResponse();

            switch (res.CacheType)
            {
                case "mirror-server":
                    this._balanceNode = false;
                    this._importHashmap = false;
                    break;

                case "local-cache":
                    this._balanceNode = false;
                    this._importHashmap = false;

                    break;                
            }
            
            connection.RequestInquiryEnabled = res.RequestInquiryEnabled;
            if (res != null)
                res.ParseResponse();


            if (res.ProtobufResponse.initCache.isShutDownProcessEnabled)
            {
                foreach (Alachisoft.NCache.Common.Protobuf.ShutDownServerInfo sInfo in res.ProtobufResponse.initCache.shutDownServerInfo)
                {
                    Address blockedServer = new Address(sInfo.serverIP, sInfo.port);
                    if (!_shutdownServers.Contains(blockedServer))
                    {
                        ShutDownServerInfo shutdownServer = new ShutDownServerInfo();
                        shutdownServer.BlockInterval = sInfo.timeoutInterval;
                        shutdownServer.BlockServerAddress = blockedServer;
                        shutdownServer.UniqueBlockingId = sInfo.uniqueKey;
                        _shutdownServers[blockedServer] = shutdownServer;
                    }
                }
            }
        }

    

        internal void GetLoggingInfo(Connection connection)
        {
            GetLogginInfoCommand command = new GetLogginInfoCommand();

            Request request = new Request(false, _operationTimeout);
            Address ipAddress = connection.ServerAddress;
            request.AddCommand(ipAddress, command);

            ExecuteRequest(request, connection, true, true);

            CommandResponse res = request.Response;
        }

        internal void InitializeSecondarySocket(Connection connection, IPAddress address, int port)
        {
            connection.ConnectSecondarySocket(address, port);
            InitSecondarySocketCommand command = new InitSecondarySocketCommand(_cache.ClientID);
            DoSendCommand(connection, command, false, false);
            CommandResponse res = connection.RecieveCommandResponse(true);
            if (res != null) res.ParseResponse();
        }

        internal void GetHashmap()
        {
            GetHashmap(null);
        }

        internal void GetHashmap(Connection connection)
        {
#if !DEVELOPMENT
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
                    if (this._processor != null) this._processor.Stop();
                }
                catch (ThreadAbortException) { }
                catch (ThreadInterruptedException) { }

                this._importHashmap = false;
                return;
            }

#if !NETCORE
            if (this._onNewHashmapRecieved == null) this._onNewHashmapRecieved = new OnNewHashmapRecieved(NewHashmapRecieved);
            this._onNewHashmapRecieved.BeginInvoke(hashmap, res.BucketSize, new AsyncCallback(NewHashmapRecievedCompleted), null);
#elif NETCORE
            //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
            TaskFactory factory = new TaskFactory();
            Task task = factory.StartNew(NewHashmapRecieved, new { BucketSize = res.BucketSize, NewHashmap = hashmap });
#endif
#endif
        }

        public bool IsConnected
        {
            get { return  _connection != null ? _connection.IsConnected : false; }
        }

        internal bool PoolHasAllServers
        {
            get
            {
                return _clientConfig.ServerCount == _pool.Servers.Count;
            }
        }

        internal bool PoolFullyConnected
        {
            get
            {
                lock (_hashmapUpdateMutex)
                {
                    var poolFullyConnected = _pool.FullyConnnected;

                    if (!poolFullyConnected)
                    {
                        if (_pool.Connections != null)
                        {
                            try
                            {
                                //as pool is fully disconnected,let's start reconnection task
                                foreach (Connection connection in _pool.Connections.Values)
                                {
                                    if (connection != null && !connection.IsConnected)
                                    {
                                        ReconectInBackground(connection.ServerAddress, connection);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                //enumeration exception can occur
                            }
                        }
                    }

                    if (_shutdownServers.Count > 1)
                        return false;
                    return poolFullyConnected;
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
                                    if (connection != null)
                                    {
                                        ReconectInBackground(connection.ServerAddress, connection);
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
            get
            {
                return _importHashmap;
            }
        }

        internal ArrayList ClientServerList
        {
            get
            {
                return _clientConfig.ServerList;
            }
        }

        internal byte[] Value
        {
            get { return  _value; }
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
			
			Address clusterAddress = null;
            Address serverAddress = null;

            lock (_requestTable.SyncRoot)
            {
                request = (Request)_requestTable[response.RequestId];

                if (!response.NeedsDeserialization && request != null)
                {
                    //The async Add/Insert/Remove complete events need to verify the command type to raise events specific to a commmand
                    try
                    {
                        if (request.Commands.Count > 0)
                            command = (CommandBase)request.Commands[remoteServerAddress];
                    }
                    catch (KeyNotFoundException)
                    {
                        //incase if request is sent to some other server for server down or dedicated call scenerio
                        foreach (var cmd in request.Commands)
                        {
                            if (cmd.Value.FinalDestinationAddress.Equals(remoteServerAddress))
                            {
                                command = cmd.Value;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }


            response.CacheId = _cacheId;
            if (command != null)
                response.CommandID = command.CommandID;

            switch (response.Type)
            {
                case Response.Type.INIT:
                case Response.Type.ADD:
                case Response.Type.REMOVE:
                case Response.Type.GET:
                case Response.Type.INSERT:
                case Response.Type.CLEAR:
                case Response.Type.COUNT:
                case Response.Type.REGISTER_NOTIF:
                case Response.Type.GET_OPTIMAL_SERVER:
                case Response.Type.GET_ENUMERATOR:
                case Response.Type.ADD_BULK:
                case Response.Type.INSERT_BULK:
                case Response.Type.GET_BULK:
                case Response.Type.BULK_GET_CACHEITEM:
                case Response.Type.REMOVE_BULK:
                case Response.Type.CONTAINS:
                case Response.Type.CONTAINS_BULK:
                case Response.Type.GET_CACHE_ITEM:
                case Response.Type.RAISE_CUSTOM_EVENT:
                case Response.Type.REMOVE_GROUP:
                case Response.Type.SEARCH:
                case Response.Type.SEARCH_ENTRIES:
                case Response.Type.REGISTER_KEY_NOTIF:
                case Response.Type.REGISTER_BULK_KEY_NOTIF:
                case Response.Type.UNREGISTER_KEY_NOTIF:
                case Response.Type.UNREGISTER_BULK_KEY_NOTIF:
                case Response.Type.GET_TYPEINFO_MAP:
                case Response.Type.GET_HASHMAP:
                case Response.Type.GET_COMPACT_TYPES:
                case Response.Type.UNLOCK:
                case Response.Type.LOCK:
                case Response.Type.ISLOCKED:
                case Response.Type.GET_LOGGING_INFO:
                case Response.Type.DISPOSE:
                case Response.Type.UNREGISTER_CQ:
                case Response.Type.REGISTER_CQ:
                case Response.Type.DELETE:
                case Response.Type.DELETE_BULK:
                case Response.Type.EXCEPTION:
                case Response.Type.GET_GROUP_NEXT_CHUNK:
                case Response.Type.GET_NEXT_CHUNK:
                case Response.Type.SEARCH_CQ:
                case Response.Type.SEARCH_ENTRIES_CQ:
                case Response.Type.ADD_ATTRIBUTE:
                case Response.Type.SYNC_EVENTS:
                case Response.Type.DELETE_QUERY:
                case Response.Type.REMOVE_QUERY:
                case Response.Type.GET_SERVER_MAPPING:
                case Response.Type.INQUIRY_REQUEST_RESPONSE:
                case Response.Type.EXECUTE_READER:
                case Response.Type.EXECUTE_READER_CQ:
                case Response.Type.DISPOSE_READER:
                case Response.Type.GET_READER_CHUNK:
                case Response.Type.EXPIRATION_RESPONSE:
                case Response.Type.POLL:
                case Response.Type.REGISTER_POLL_NOTIF:
                case Response.Type.GET_CONNECTED_CLIENTS:
                case Response.Type.TOUCH:
                case Response.Type.GET_TOPIC:
                case Response.Type.REMOVE_TOPIC:
                case Response.Type.SUBSCRIBE_TOPIC:
                case Response.Type.UNSUBSCRIBE_TOPIC:
                case Response.Type.GET_MESSAGE:
                case Response.Type.MESSAGE_PUBLISH:
                case Response.Type.MESSAGE_ACKNOWLEDGEMENT:
                case Response.Type.PING:
                case Response.Type.MESSAGE_COUNT:
                case Response.Type.GET_SERIALIZATION_FORMAT:
				case Response.Type.MODULE:
                    ProcessRawResponse(request, response, remoteServerAddress);
                    break;
                case Response.Type.SURROGATE:
                    response.IsSurrogate = true;
                    ProcessRawResponse(request, response, remoteServerAddress);
                    break;
                default:
                    ProcessInternalResponse(response, remoteServerAddress,command);
                    break;

            }
        }

        private void ProcessRawResponse(Request request, CommandResponse response, Address remoteServerAddress)
        {
            if (request == null)
                return;
            lock (request)
            {
                request.AddResponse(remoteServerAddress, response);
                request.CacheId = _cacheId;
                Monitor.PulseAll(request);
            }
            if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.Debug("Broker.ProcessResponse", "RequestID : " + request.RequestId + " " + request.Name + " received response from server " + remoteServerAddress + ". Seq # " + response.SequenceId);
        }

        //For Internal Responses and Notifications Callback
        private void ProcessInternalResponse(CommandResponse response, Address remoteServerAddress, CommandBase command)
        {
            Address clusterAddress = null;
            Address serverAddress = null;

            //Deserializing response incase of InternalCommands and CallBacks
            response.DeserializeResponse();
            switch (response.Type)
            {
                case Response.Type.ITEM_REMOVED_CALLBACK:
                  

                    if (_cache != null && _cache.AsyncEventHandler != null)
                    {
                        _cache.EventListener.OnCustomRemoveCallback(response.CallbackId, response.Key, response.Value, response.Reason, response.FlagValueEntry.Flag, true, null, response.DataFilter);
                    }

                    break;

                case Response.Type.ITEM_UPDATED_CALLBACK:
                   
                    if (_cache != null && _cache.AsyncEventHandler != null)
                    {
                        _cache.EventListener.OnCustomUpdateCallback(response.CallbackId, response.Key, true, null, null, null, response.DataFilter);
                    }

                    break;
                case Common.Protobuf.Response.Type.POLL_NOTIFY_CALLBACK:
                  
                    if (_cache != null && _cache.AsyncEventHandler != null)
                    {
                        _cache.EventListener.OnPollNotified(response.CallbackId, response.EventType);
                    }
                    break;

              

                case Response.Type.BULK_EVENT:
                    if (response.EventList.Count > 0)
                    {
                        ThreadPool.QueueUserWorkItem(_bulkEventCallback, new object[] { remoteServerAddress, response.EventList });
                        break;
                    }
                    break;


                case Response.Type.ASYNC_OP_COMPLETED_CALLBACK:
                    if (_cache != null && _cache.AsyncEventHandler != null)
                    {
                        if (command is AddCommand)
                        {
                            _cache.AsyncEventHandler.OnAsyncAddCompleted(response.Key, ((AddCommand)command).AsycItemAddedOpComplete, response.AsyncOpResult, true);
                            break;
                        }
                        else if (command is InsertCommand)
                        {
                            _cache.AsyncEventHandler.OnAsyncInsertCompleted(response.Key, ((InsertCommand)command).AsycItemUpdatedOpComplete, response.AsyncOpResult, true);
                            break;
                        }
                        else if (command is RemoveCommand)
                        {
                            _cache.AsyncEventHandler.OnAsyncRemoveCompleted(response.Key, ((RemoveCommand)command).AsyncItemRemovedOpComplete, response.AsyncOpResult, true);
                            break;
                        }
                        else if (command is ClearCommand)
                        {
                            _cache.AsyncEventHandler.OnAsyncClearCompleted(((ClearCommand)command).AsyncCacheClearedOpComplete, response.AsyncOpResult, true);
                            break;
                        }
                    }
                    break;

             

                case Response.Type.ITEM_ADDED_EVENT:
                   

                    _cache.EventListener.OnItemAdded(response.Key, true, null, null);

                    break;

                case Response.Type.ITEM_UPDATED_EVENT:
                 
                    _cache.EventListener.OnItemUpdated(response.Key, true, null, null, null);

                    break;

                case Response.Type.ITEM_REMOVED_EVENT:
                  
                    _cache.EventListener.OnItemRemoved(response.Key, response.Value, response.Reason, response.FlagValueEntry.Flag, true, null);

                    break;

                case Response.Type.CUSTOM_EVENT:
                    _cache.EventListener.OnCustomNotification(CompactBinaryFormatter.FromByteBuffer(response.NotifId, _cacheId), CompactBinaryFormatter.FromByteBuffer(response.Value, _cacheId), true);
                    break;

                case Response.Type.NODE_LEFT_EVENT:
                    ServerInfo serverLeft = new ServerInfo();

                    if (response.ServerPort > 0)
                    {
                        //Get Mapped Server will return the same "IP and port" incase of non-existance of the map
                        ServerInfo rm = _clientConfig.GetMappedServer(response.ServerIPAddress.ToString(), response.ServerPort);
                        serverLeft = rm;
                        _clientConfig.RemoveServer(serverLeft);
                    }

                    clusterAddress = new Address(serverLeft.IP, serverLeft.Port);
                    serverAddress = new Address(serverLeft.IP, serverLeft.Port);
                 
                    break;

                case Response.Type.CACHE_STOPPED_EVENT:
                    break;

                case Response.Type.NODE_JOINED_EVENT:
                    ThreadPool.QueueUserWorkItem(new WaitCallback(StartBalancingClients), response);
                    break;

                case Response.Type.HASHMAP_CHANGED_EVENT:
                    ThreadPool.QueueUserWorkItem(new WaitCallback(UpdateHashmapAsync), response.Value);
                    break;

                case Response.Type.CONFIG_MODIFIED_EVENT:
                    break;

                case Response.Type.COMPACT_TYPE_REGISTER_EVENT:
                    ThreadPool.QueueUserWorkItem(new WaitCallback(UpdateCompactTypesAsync), response.Value);
                    break;

                case Response.Type.LOGGING_INFO_MODIFIED_EVENT:
                    this.InitializeLogs(response.EnableErrorLogs, response.EnableDetailedLogs);
                    break;

               

                case Response.Type.BLOCK_ACTIVITY:

                    ShutDownServerInfo ssinfo = new ShutDownServerInfo();
                    ssinfo.UniqueBlockingId = response.ProtobufResponse.blockActivityEvent.uniqueKey;
                    ssinfo.BlockServerAddress = new Address(response.ProtobufResponse.blockActivityEvent.serverIP, response.ProtobufResponse.blockActivityEvent.port);
                    ssinfo.BlockInterval = response.ProtobufResponse.blockActivityEvent.timeoutInterval;

                    ssinfo.StartBlockingTime = DateTime.Now;

                    if (!_shutdownServers.Contains(ssinfo.BlockServerAddress))
                    {
                        _shutdownServers[ssinfo.BlockServerAddress] = ssinfo;

                        long maxTimeout = 0;

                        foreach (ShutDownServerInfo sInfo in _shutdownServers.Values)
                        {
                            if (maxTimeout == 0)
                                maxTimeout = sInfo.BlockInterval;
                            if (maxTimeout < sInfo.BlockInterval)
                                maxTimeout = sInfo.BlockInterval;
                        }

                        Double additionaltime = maxTimeout * 0.05f;
                        maxTimeout = (maxTimeout + (int)additionaltime) * 1000;
                        _shutdownTimeout = maxTimeout;
                        try
                        {
                            IEnumerator iter = null;
                            lock (_requestTable.SyncRoot)
                            {
                                iter = _requestTable.Values.GetEnumerator();
                                while (iter.MoveNext())
                                {
                                    Request req = (Request)iter.Current;
                                    lock (req)
                                    {
                                        if (!req.IsRequestTimeoutReset)
                                        {
                                            req.RequestTimeout = req.RequestTimeout + maxTimeout;
                                            req.IsRequestTimeoutReset = true;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            NCacheLog.Error("Broker.ProcessResponse", ex.ToString());
                        }
                    }
                    else
                    {
                        ShutDownServerInfo oldInfo = (ShutDownServerInfo)_shutdownServers[ssinfo.BlockServerAddress];
                        if (!oldInfo.UniqueBlockingId.Equals(ssinfo.UniqueBlockingId))
                        {
                            long startTime = (oldInfo.StartBlockingTime.Ticks - 621355968000000000) / 10000;
                            int timeout = Convert.ToInt32(oldInfo.BlockInterval * 1000) - (int)((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - startTime);
                            if (timeout <= 0)
                            {
                                _shutdownServers[oldInfo.BlockServerAddress] = ssinfo;
                            }
                        }
                    }

                    break;

                case Response.Type.UNBLOCK_ACTIVITY:
                    Address blockServer = new Address(response.ProtobufResponse.unblockActivityEvent.serverIP, response.ProtobufResponse.unblockActivityEvent.port);
                    if (_shutdownServers.ContainsKey(blockServer))
                    {
                        ShutDownServerInfo ssInfo = (ShutDownServerInfo)_shutdownServers[blockServer];
                        if (ssInfo != null)
                        {
                            if (ssInfo.UniqueBlockingId.Equals(response.ProtobufResponse.unblockActivityEvent.uniqueKey))
                            {
                                Connection shutdownCon = null;
                                if (_pool.Contains(blockServer))
                                {
                                    shutdownCon = _pool[blockServer];
                                    _pool.Remove(blockServer);
                                }
                                if (_connection.ServerAddress.Equals(blockServer))
                                {
                                    Connection con = _pool.GetAnyConnection(); //TryPool or GetAnyConnection here?
                                    if (con != null)
                                        _connection = con;
                                }

                                if (_connection != null)
                                {
                                    ReconectInBackground(_connection.ServerAddress, _connection);
                                }

                                lock (ssInfo.WaitForBlockedActivity)
                                {
                                    _shutdownServers.Remove(blockServer);
                                    Monitor.PulseAll(ssInfo.WaitForBlockedActivity);

                                }

                                if (shutdownCon != null)
                                    shutdownCon.Disconnect();

                            }
                        }
                    }

                    break;

                case Response.Type.OPERATIONCHANGEDEVNET:
                    Address address = Address.Parse(response.ProtobufResponse.operationModeChangeEventResponse.serverIP);
                    Disconnection(address);
                    break;
            }
        }

        private void Disconnection(Address address)
        {
            if (_connection != null)
            {
                _connection.Disconnect();
                _connection.Dispose();
                ResetBroker(Address.Parse(_connection.IpAddress));
            }
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                if (_pool != null && _pool.Connections != null)
                {
                    foreach (DictionaryEntry entry in _pool.Connections)
                    {
                        Address ip = entry.Key as Address;
                        Connection connection = entry.Value as Connection;
                        connection.Disconnect();
                        connection.Dispose();
                        ResetBroker(ip);
                    }

                }
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }

        }

        private void RaiseBulkEvent(object arg)
        {
            try
            {
                Alachisoft.NCache.Caching.EventId eventId;
                Address remoteServerAddress = null;
                if (arg == null || ((object[])arg).Length != 2) return;
                remoteServerAddress = ((object[])arg)[0] as Address;
                List<BulkEventItemResponse> bulkEvents = ((object[])arg)[1] as List<BulkEventItemResponse>;

                if (bulkEvents == null) return;

                foreach (BulkEventItemResponse eventItem in bulkEvents)
                {
                    try
                    {
                        if (_perfStatsColl != null)
                        {
                            _perfStatsColl.IncrementEventsTriggeredPerSeconds();
                        }

                        eventId = new Alachisoft.NCache.Caching.EventId();

                        switch (eventItem.eventType)
                        {


                            case BulkEventItemResponse.EventType.ITEM_ADDED_EVENT:
                                {
                                    eventId.EventUniqueID = eventItem.itemAddedEvent.eventId.eventUniqueId;
                                    eventId.EventCounter = eventItem.itemAddedEvent.eventId.eventCounter;
                                    eventId.OperationCounter = eventItem.itemAddedEvent.eventId.operationCounter;
                                    eventId.EventType = Persistence.EventType.ITEM_ADDED_EVENT;

                                    BitSet flag = new BitSet((byte)eventItem.itemAddedEvent.flag);

                                    EventCacheItem item = EventUtil.ConvertToEventEntry(eventItem.itemAddedEvent.eventId.item);

                                    if (_notifyAsync)
                                        _cache.EventListener.OnItemAdded(eventItem.itemAddedEvent.key, _notifyAsync, item, flag);
                                    else
                                        _eventProcessor.Enqueue(new ItemAddedTask(this, eventItem.itemAddedEvent.key, _notifyAsync, item, flag));
                                }
                                break;

                            case Common.Protobuf.BulkEventItemResponse.EventType.ITEM_UPDATED_EVENT:
                                {
                                    eventId.EventUniqueID = eventItem.itemUpdatedEvent.eventId.eventUniqueId;
                                    eventId.EventCounter = eventItem.itemUpdatedEvent.eventId.eventCounter;
                                    eventId.OperationCounter = eventItem.itemUpdatedEvent.eventId.operationCounter;
                                    eventId.EventType = Persistence.EventType.ITEM_UPDATED_EVENT;

                                    BitSet flag = new BitSet((byte)eventItem.itemUpdatedEvent.flag);

                                    EventCacheItem item = EventUtil.ConvertToEventEntry(eventItem.itemUpdatedEvent.eventId.item);
                                    EventCacheItem oldItem = EventUtil.ConvertToEventEntry(eventItem.itemUpdatedEvent.eventId.oldItem);


                                    if (_notifyAsync)
                                        _cache.EventListener.OnItemUpdated(eventItem.itemUpdatedEvent.key, _notifyAsync, item, oldItem, flag);
                                    else
                                        _eventProcessor.Enqueue(new ItemUpdatedTask(this, eventItem.itemUpdatedEvent.key, _notifyAsync, item, oldItem, flag));
                                }
                                break;


                            case Common.Protobuf.BulkEventItemResponse.EventType.ITEM_UPDATED_CALLBACK:
                                {
                                    eventId.EventUniqueID = eventItem.ItemUpdatedCallback.eventId.eventUniqueId;
                                    eventId.EventCounter = eventItem.ItemUpdatedCallback.eventId.eventCounter;
                                    eventId.OperationCounter = eventItem.ItemUpdatedCallback.eventId.operationCounter;
                                    eventId.EventType = Persistence.EventType.ITEM_UPDATED_CALLBACK;

                                    BitSet flag = new BitSet((byte)eventItem.ItemUpdatedCallback.flag);

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
                                            item.SetValue(value);
                                        }
                                    }

                                    eventId.EventUniqueID = eventItem.itemRemoveCallback.eventId.eventUniqueId;
                                    eventId.EventCounter = eventItem.itemRemoveCallback.eventId.eventCounter;
                                    eventId.OperationCounter = eventItem.itemRemoveCallback.eventId.operationCounter;
                                    eventId.EventType = Persistence.EventType.ITEM_REMOVED_CALLBACK;

                                    if (_notifyAsync)
                                    {
                                        if (_cache != null && _cache.AsyncEventHandler != null)
                                            _cache.EventListener.OnCustomRemoveCallback((Int16)eventItem.itemRemoveCallback.callbackId, eventItem.itemRemoveCallback.key, value, (CacheItemRemovedReason)eventItem.itemRemoveCallback.itemRemoveReason, flagValueEntry.Flag, _notifyAsync, item, (EventDataFilter)eventItem.itemRemoveCallback.dataFilter);
                                    }
                                    else
                                        _eventProcessor.Enqueue(new ItemRemoveCallBackTask(this, eventItem.itemRemoveCallback.key, (Int16)eventItem.itemRemoveCallback.callbackId, value, (CacheItemRemovedReason)eventItem.itemRemoveCallback.itemRemoveReason, flagValueEntry.Flag, _notifyAsync, item, (EventDataFilter)eventItem.itemRemoveCallback.dataFilter));
                                }
                                break;

                            case Common.Protobuf.BulkEventItemResponse.EventType.POLL_NOTIFY_EVENT:
                                _cache.EventListener.OnPollNotified((short)eventItem.pollNotifyEvent.callbackId, (EventTypeInternal)eventItem.pollNotifyEvent.eventType);
                                break;

                            case Common.Protobuf.BulkEventItemResponse.EventType.ITEM_REMOVED_EVENT:
                                {
                                    CompressedValueEntry flagValueEntry = new CompressedValueEntry();
                                    CacheItemRemovedReason reason = (CacheItemRemovedReason)eventItem.itemRemovedEvent.itemRemoveReason;
                                    flagValueEntry.Flag = new BitSet((byte)eventItem.itemRemovedEvent.flag);

                                    EventCacheItem item = EventUtil.ConvertToEventEntry(eventItem.itemRemovedEvent.eventId.item);
                                    byte[] value = null;
                                    if (eventItem.itemRemovedEvent.value != null && eventItem.itemRemovedEvent.value.Count > 0)
                                    {
                                        UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(eventItem.itemRemovedEvent.value.ToArray());
                                        value = ubObject.GetFullObject();

                                        if (item != null)
                                        {
                                            item.SetValue(value);
                                        }
                                    }

                                    eventId.EventUniqueID = eventItem.itemRemovedEvent.eventId.eventUniqueId;
                                    eventId.EventCounter = eventItem.itemRemovedEvent.eventId.eventCounter;
                                    eventId.OperationCounter = eventItem.itemRemovedEvent.eventId.operationCounter;
                                    eventId.EventType = Persistence.EventType.ITEM_REMOVED_EVENT;

                                    

                                    if (_notifyAsync)
                                        _cache.EventListener.OnItemRemoved(eventItem.itemRemovedEvent.key, value, reason, flagValueEntry.Flag, _notifyAsync, item);
                                    else
                                        _eventProcessor.Enqueue(new ItemRemoveEventTask(this, eventItem.itemRemovedEvent.key, value, reason, flagValueEntry.Flag, _notifyAsync, item));
                                }
                                break;

                          

                            case Common.Protobuf.BulkEventItemResponse.EventType.RAISE_CUSTOM_EVENT:
                                {
                                    if (_notifyAsync)
                                        _cache.EventListener.OnCustomNotification(CompactBinaryFormatter.FromByteBuffer(eventItem.CustomEvent.key, _cacheId), CompactBinaryFormatter.FromByteBuffer(eventItem.CustomEvent.value, _cacheId), _notifyAsync);
                                    else
                                        _eventProcessor.Enqueue(new CustomEventTask(this, eventItem.CustomEvent.key, _cacheId, eventItem.CustomEvent.value, _notifyAsync));
                                }
                                break;

                            case Common.Protobuf.BulkEventItemResponse.EventType.ASYNC_OP_COMPLETED_EVENT:
                                {
                                    Object asyncOpResult;
                                    CommandBase command = null;
                                    if (eventItem.asyncOperationCompletedCallback.success)
                                        asyncOpResult = Alachisoft.NCache.Caching.AsyncOpResult.Success;
                                    else
                                        asyncOpResult = new System.Exception(eventItem.asyncOperationCompletedCallback.exc.exception);

                                    Request request = (Request)_requestTable[eventItem.asyncOperationCompletedCallback.requestId];

                                    if (request != null)
                                    {
                                        RemoveRequestFromRequestTable(request);
                                        if (request.Commands.Count > 0)
                                            command = (CommandBase)request.Commands[remoteServerAddress];
                                    }

                                    if (_notifyAsync)
                                    {
                                        if (_cache != null && _cache.AsyncEventHandler != null)
                                        {
                                            if (command is AddCommand)
                                            {
                                                _cache.AsyncEventHandler.OnAsyncAddCompleted(eventItem.asyncOperationCompletedCallback.key, ((AddCommand)command).AsycItemAddedOpComplete, asyncOpResult, _notifyAsync);
                                                break;
                                            }
                                            else if (command is InsertCommand)
                                            {
                                                _cache.AsyncEventHandler.OnAsyncInsertCompleted(eventItem.asyncOperationCompletedCallback.key, ((InsertCommand)command).AsycItemUpdatedOpComplete, asyncOpResult, _notifyAsync);
                                                break;
                                            }
                                            else if (command is RemoveCommand)
                                            {
                                                _cache.AsyncEventHandler.OnAsyncRemoveCompleted(eventItem.asyncOperationCompletedCallback.key, ((RemoveCommand)command).AsyncItemRemovedOpComplete, asyncOpResult, _notifyAsync);
                                                break;
                                            }
                                            else if (command is ClearCommand)
                                            {
                                                _cache.AsyncEventHandler.OnAsyncClearCompleted(((ClearCommand)command).AsyncCacheClearedOpComplete, asyncOpResult, _notifyAsync);
                                                break;
                                            }
                                        }
                                    }
                                    else
                                        _eventProcessor.Enqueue(new AsyncOperationCompletedEventTask(this, command, eventItem.asyncOperationCompletedCallback.key, asyncOpResult, _notifyAsync));

                                }
                                break;


                        }
                    }
                    catch (Exception ex)
                    {
                        if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.RaiseBulkEvent", "An error occurred while raising bulk event of type : " + eventItem.eventType + ". Error :" + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            { }
        }
        private void UpdateHashmapAsync(object state)
        {
            byte[] serializedMap = state as byte[];
            try
            {
#if !DEVELOPMENT
                if (serializedMap != null)
                {
                    NewHashmap map = NewHashmap.Deserialize(serializedMap, this._cacheId);
                    NewHashmapRecieved(map);
                }
#endif
            }
            catch (Exception e)
            {
                try
                {
                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.UpdateHashmapAsync", "An error occurred while installing updated hashmap. Error :" + e.ToString());
                }
                catch (Exception) { }
            }
        }

        private void UpdateCompactTypesAsync(object state)
        {
            byte[] serializedMap = state as byte[];
            try
            {
#if !DEVELOPMENT
                if (serializedMap != null)
                {
                    Hashtable updatedCompactTypesConfigFromByteBuffer = CompactBinaryFormatter.FromByteBuffer(serializedMap, this._cacheId) as Hashtable;
                    Hashtable updatedCompactTypesConfig = null;

                    if (updatedCompactTypesConfigFromByteBuffer != null)
                        updatedCompactTypesConfig = new Hashtable(updatedCompactTypesConfigFromByteBuffer, new EqualityComparer());

                    Hashtable updatedCompactTypes = null;
                    if (updatedCompactTypesConfig != null) updatedCompactTypes = Alachisoft.NCache.Util.SerializationUtil.GetCompactTypes(updatedCompactTypesConfig, false, this._cacheId);
                    if (updatedCompactTypes != null)
                    {
                        IDictionaryEnumerator ide = updatedCompactTypes.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            Hashtable handleNonCompactFields = (Hashtable)ide.Value;
                            short typeHandle = (short)handleNonCompactFields["handle"];
                            Hashtable nonCompactFields = null;
                            if (handleNonCompactFields.Contains("non-compact-fields"))
                                nonCompactFields = (Hashtable)handleNonCompactFields["non-compact-fields"];

                            Serialization.CompactFormatterServices.RegisterCustomCompactType((Type)ide.Key, (short)typeHandle, this._cacheId.ToLower(),
                            Util.SerializationUtil.GetSubTypeHandle(this._cacheId, ((short)typeHandle).ToString(), (Type)ide.Key),
                            Util.SerializationUtil.GetAttributeOrder(this._cacheId),
                            Util.SerializationUtil.GetPortibilaty((short)typeHandle, this._cacheId), nonCompactFields);

                            //Also register array type for custom types.

                            typeHandle += Util.SerializationUtil.UserdefinedArrayTypeHandle;    //Same handle is used at server side.
                            Type arrayType = ((Type)ide.Key).MakeArrayType();

                            Serialization.CompactFormatterServices.RegisterCustomCompactType(arrayType, (short)typeHandle, _cacheId.ToLower(),
                                Util.SerializationUtil.GetSubTypeHandle(this._cacheId, ((short)typeHandle).ToString(), arrayType),
                                Util.SerializationUtil.GetAttributeOrder(this._cacheId),
                                Util.SerializationUtil.GetPortibilaty((short)typeHandle, this._cacheId), nonCompactFields);
                        }
                    }
                }
#endif
            }
            catch (Exception e)
            {
                try
                {
                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.UpdateCompactTypesAsync", "An error occurred while installing updated compact types. Error :" + e.ToString());
                }
                catch (Exception) { }
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
                //
                //Copied entire logic from ProcessResponse->Node_Joined_Event to this method because of Azure Common Client.
                //
                ServerInfo newServerJoined = new ServerInfo();

                if (response.ServerPort > 0)
                {

                    if (_clientConfig.IPMappingConfigured)
                    {
                        this.GetServerMapping(null, false);
                        ServerInfo rm = _clientConfig.GetMappedServer(response.ServerIPAddress.ToString(), response.ServerPort);
                        newServerJoined = rm;
                    }
                    else
                    {
                        newServerJoined = new ServerInfo(response.ServerIPAddress, response.ServerPort);
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
                        if (!ConnectRemoteServer(this._connection, NodeIP, NewServerPort, false, false, true, ref exception))
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
            catch (Exception e)
            {
                if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StartBalancingClients", "Error: " + e);
            }

        }

        /// <summary>
        /// New hashmap recieved. 
        /// </summary>
        /// <param name="buffer"></param>
        private void NewSerializedMapRecieved(byte[] buffer)
        {
#if !DEVELOPMENT
            this.NewHashmapRecieved(NewHashmap.Deserialize(buffer, this._cacheId));
#endif
        }
#if !DEVELOPMENT

#if !NETCORE
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
#elif NETCORE
        private void NewHashmapRecieved(object args)
        {
            dynamic arguments = args;
            if (arguments.NewHashmap == null) return;
            this._pool.BucketSize = arguments.BucketSize;
            NewHashmapRecieved(arguments.NewHashmap);
        }
#endif

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

            try
            {
                //we are acquring this lock first to avoid deadlock caused during connection establishment with new servers
                this._lock.AcquireWriterLock(Timeout.Infinite);

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
                            if (newHashmap.ForcefulUpdate)
                            {
                                if (_logger != null && _logger.NCacheLog != null)
                                    _logger.NCacheLog.CriticalInfo("Broker.NewHashmapReceived", "Hashmap is same as current pool. Pool " + this._pool.ToString() + " New Hashmap " + newHashmap.ToString() + " ... is being installed forcefully");

                            }
                            else
                            {
                                if (_logger != null && _logger.NCacheLog != null)
                                    _logger.NCacheLog.CriticalInfo("Broker.NewHashmapReceived", "Hashmap is same as current pool. Pool " + this._pool.ToString() + " New Hashmap " + newHashmap.ToString() + " ... returning");
                                return;
                            }
                        }

                        if (_clientConfig.IPMappingConfigured)
                        {
                            this.GetServerMapping(null, false);
                        }
                        for (int i = 0; i < newHashmap.Members.Count; i++)
                        {
                            string ip = (string)newHashmap.Members[i];
                            int serverPort = this._port;

                            //+MOiz: Azure remote client task
                            if (_clientConfig.IPMappingConfigured)
                            {
                                ServerInfo server = _clientConfig.GetMappedServer(ip, serverPort);
                                ip = server.Name;
                                serverPort = server.Port;
                                newHashmap.Members[i] = ip + ":" + serverPort.ToString();
                            }
                            //-MOiz

                            Address addr = new Address(ip, serverPort);

                            if (!this._pool.Contains(addr))
                            {
                                IPAddress address = default(IPAddress) ;
                                Connection connection = default(Connection);
                                try
                                {
                                    address = IPAddress.Parse(ip);
                                    connection = new Connection(this, this._commandReieved, this._serverLost, this._logger, _perfStatsColl, _responseIntegrator, _clientConfig.BindIP, this._cacheId);

                                    Exception exception = null;

                                    if (ConnectRemoteServer(connection, address, serverPort, false, false, false, ref exception))
                                    {
                                        this._pool.Add(addr, connection);
                                        this._clientConfig.AddServer(new ServerInfo(address, serverPort));
                                        if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.Debug("Broker.NewHashmapRecieved", "Connection made to " + ip + ", and added to pool");
                                    }
                                    else
                                    {
                                        if (exception != null && _logger.IsErrorLogsEnabled)
                                        {
                                            _logger.NCacheLog.Error("Broker.NewHashmapRecieved", "Could not connect to " + ip + ". " + exception.ToString());
                                        }
                                    }
                                }
                                catch (SecurityException sxc)
                                {
                                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.NewHashmapRecieved", "You do not have permissions to perform the operation on " + ip + ". " + sxc.ToString());
                                }
                                catch (InternalCommandException ex)
                                {
                                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StartServices", ex.ToString());
                                }
                                catch(LicensingException ex)
                                {

                                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StartServices", ex.ToString());
                                }
                                catch (Exception exc)
                                {
                                    this._pool.Add(addr, connection);
                                    this._clientConfig.AddServer(new ServerInfo(address, serverPort));
                                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.NewHashmapRecieved", exc.ToString());
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
                                        TryConnecting(connection, ref exception,0,false);
                                    }
                                    catch (Exception exc)
                                    {
                                        if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.NewHashmapRecieved", exc.ToString());
                                    }
                                }
                                else
                                {
                                    if (_logger.IsDetailedLogsEnabled)
                                    {
                                        _logger.NCacheLog.Debug("Broker.NewHashmapRecieved", "Already connected to " + ip + " in the pool");
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
                                    if (!newHashmap.Members.Contains(ipAddress.IpAddress.ToString() + ":" + ipAddress.Port.ToString()))
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

                                if (_connection != null && _connection.Address.Equals(ip.IpAddress))
                                {
                                    //Let's reset default connection from existing pool
                                    _connection = _pool.GetAnyConnection();
                                    if (_connection == null)
                                    {
                                        _hashmapError = true;
                                        _poolSize = _pool.Count;
                                    }
                                }

                                if (_shutdownServers.ContainsKey(ip))
                                {
                                    ShutDownServerInfo ssInfo = (ShutDownServerInfo)_shutdownServers[ip];

                                    lock (ssInfo.WaitForBlockedActivity)
                                    {

                                        Monitor.PulseAll(ssInfo.WaitForBlockedActivity);
                                        _shutdownServers.Remove(ip);
                                    }

                                }
                                if (_logger.IsDetailedLogsEnabled)
                                {
                                    _logger.NCacheLog.Debug("Broker.NewHashmapRecieved", "Disconnected from " + ip + ", and removed from pool");
                                }
                            }
                        }



                        ServerInfo srvr = new ServerInfo();
                        string add = null;
                        for (int key = 0; key < newHashmap.Map.Count; key++)
                        {
                            add = (string)newHashmap.Map[key];

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
                }
            }
            finally
            {
                this._lock.ReleaseWriterLock();
            }
            _hashMapStatus.SetStatusBit(HashMapStatus.INITIALIZE, HashMapStatus.UNINITIALIZE);
        }
#endif
        /// <summary>
        /// Called when NewHashmapRecieved callback is executed
        /// </summary>
        /// <param name="result"></param>
        private void NewHashmapRecievedCompleted(IAsyncResult result)
        {
#if !DEVELOPMENT
            this._onNewHashmapRecieved.EndInvoke(result);
#endif
        }
        
        internal bool GetKeysDistributionMap(string[] keys, CacheItem[] items,
            ref Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap)
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
                    Connection conn = _pool[serverAddress]; //for balanced connection
                    Address adrs = serverAddress;

                    if (conn != null && !conn.IsConnected)
                    {
                        Address loadbaanced = GetLoadBalancedAddress();
                        if (loadbaanced != null) adrs = loadbaanced;
                    }

                    if (keysDistributionMap.ContainsKey(adrs))
                    {
                        KeyValuePair<string[], CacheItem[]> newtmp = keysDistributionMap[adrs];

                        tmp = MergeDistributioonMap(tmp, newtmp, itemsAvailable);
                        keysDistributionMap[adrs] = tmp;
                        result = true;
                    }
                    else
                    {
                        keysDistributionMap.Add(adrs, tmp);
                    }
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

            return new KeyValuePair<string[], CacheItem[]>(distributedKeys, distributedItems);
        }

        internal Request CreateRequest(CommandBase command)
        {
            Request request = null;

            switch (command.CommandType)
            {
                case CommandType.GET_GROUP:
                case CommandType.GET_TAG:
                case CommandType.SEARCH:
                case CommandType.SEARCH_CQ:
                case CommandType.GET_KEYS_TAG:
                case CommandType.GETGROUP_NEXT_CHUNK:
                case CommandType.GET_NEXT_CHUNK:
                case CommandType.REMOVE_BY_TAG:
                case CommandType.REMOVE_GROUP:
                case CommandType.DELETEQUERY:
                case CommandType.EXECUTE_READER:
                case CommandType.EXECUTE_READER_CQ:
                case CommandType.DISPOSE_READER:
                case CommandType.GET_READER_CHUNK:
                case CommandType.POLL:
                case CommandType.GETMESSAGE:
                case CommandType.REGISTER_NOTIF:

                    if (_importHashmap)
                    {
                        if (PoolFullyDisConnected)
                        {
                            throw new OperationFailedException(ErrorCodes.Common.NO_SERVER_AVAILABLE,ErrorMessages.GetErrorMessage(ErrorCodes.Common.NO_SERVER_AVAILABLE));
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
                    if (ipAddress == null)
                    {
                        ipAddress = _connection.ServerAddress;
                    }
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
            command.ResetCommand();
            if (_importHashmap)
            {
                request = new Request(true, _operationTimeout);
                command.ClientLastViewId = ForcedViewId;

                Address server = GetLoadBalancedAddress();
                if (server != null)
                {
                    request.AddCommand(server, command);
                }
                else
                {
                    throw new OperationFailedException(ErrorCodes.Common.NO_SERVER_AVAILABLE, ErrorMessages.GetErrorMessage(ErrorCodes.Common.NO_SERVER_AVAILABLE));
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

        public Address GetLoadBalancedAddress()
        {
            if (_pool != null)
                return _pool.GetNextAddress();
            return null;
        }

        internal Connection GetAnyConnection()
        {
            return _pool.GetAnyConnection();
        }

        /// <summary>
        /// Called when a command is failed while sending
        /// </summary>
        /// <param name="command">Command failed to be sent</param>
        private void RetrySendCommand(CommandBase command)
        {
            //Try to resend command to any other available server
            int tries = 1;
            bool retry = true;
            while (retry)
            {


                retry = tries < 5;

                try
                {
                    if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.CriticalInfo("RetrySendCommand",
                        "Command: " + command.CommandType + " retried. RequestId: " + command.Parent.RequestId +
                        ", Sent as dedicated. command.ClientLastViewId == ForcedViewId => " + (command.ClientLastViewId == ForcedViewId));
                    command.IsRetry = true;
                    SendCommand(command, true);
                    retry = false;
                }
                catch (ConnectionException e)
                {
                    if (_logger.IsErrorLogsEnabled)
                        _logger.NCacheLog.Error("Broker.ExecuteRequest",
                            "RequestID :" + command.RequestId + " " + command.CommandName + " can not sent to server " +
                            (new Address(e.IPAddress, e.Port)).ToString() + e.ToString());

                    if (retry)
                    {
                        tries++;

                        if (5 != 0)
                            Thread.Sleep(5000);

                    }
                    else
                    {
                        throw new OperationFailedException(ErrorCodes.Common.NO_SERVER_AVAILABLE, ErrorMessages.GetErrorMessage(ErrorCodes.Common.NO_SERVER_AVAILABLE));
                    }
                }
                catch (ActivityBlockedException e)
                {
                    if (_logger.IsErrorLogsEnabled)
                        _logger.NCacheLog.Error("Broker.ExecuteRequest",
                            "RequestID :" + command.RequestId + " " + command.CommandName + " can not sent to server " +
                            e.BlockedServerIp + e.ToString());

                    if (retry)
                    {
                        tries++;

                        if (1 != 0)
                            Thread.Sleep( 1000);

                    }
                    else
                    {
                        throw new OperationFailedException(ErrorCodes.Common.NO_SERVER_AVAILABLE, ErrorMessages.GetErrorMessage(ErrorCodes.Common.NO_SERVER_AVAILABLE));
                    }
                }
                catch (Exception e)
                {
                    if (_logger.IsErrorLogsEnabled)
                        _logger.NCacheLog.Error("Broker.ExecuteRequest",
                            "RequestID :" + command.RequestId + " " + command.CommandName + " can not sent to server. " +
                            e.ToString());
                    throw new OperationFailedException(e.Message, e);
                }
            }
        }

        /// <summary>
        /// Called when a bulk command is failed while sending
        /// </summary>
        /// <param name="command">Command failed to be sent</param>
        /// <returns>True if command was resent as dedicated command (no need to send pending commands)</returns>
        private bool RetrySendBulkCommand(CommandBase command)
        {
            CommandBase resendCommand = null;
            bool isDedicatedResend = false;

            //Only key bulk write commands, when parallel 
            if (command.CommandRequestType == RequestType.KeyBulkWrite && !command.Parent.IsDedicatedRequest)
            {
                //key based unsafe parallel commands-- command can be sent to any other- server wil re-route according to keys
                resendCommand = command;
                command.Parent.RemoveResponse(command.FinalDestinationAddress, command.CommandID);
                command.ResetCommand();
            }
            else
            {
                //if request is dedicated or command is safe, a dedicated request can be executed on any other server
                //command is parallel and unsafe
                //if command is non-key bulk write e.g. delete query etc, create a dedicated request and execute and merge responses with already present responses
                resendCommand = CommandBase.GetDedicatedCommand(command.Parent.Commands.Values);
                isDedicatedResend = true;
                resendCommand.Parent.ClearResponses();
            }

            RetrySendCommand(resendCommand);
            return isDedicatedResend;
        }

        private void RetryBulkRequest(IEnumerable<KeyValuePair<Address, CommandBase>> faliedCommands)
        {
            foreach (KeyValuePair<Address, CommandBase> pair in faliedCommands)
            {
                Address address = pair.Key;
                CommandBase command = pair.Value;

                if (command.SendError == null) continue;

                ErrorType errorType = command.SendError.Type;
                Exception exception = command.SendError.Exception;
                switch (errorType)
                {
                    case ErrorType.ActivityBlocked:
                        if (command.CommandRequestType == RequestType.ChunkRead)
                            throw new OperationFailedException("Enumeration has been modified");
                        if (!command.IsInternalCommand)
                        {
                            bool dedicatedResend = RetrySendBulkCommand(command);
                            //if resent as dedicated command, no need to execute remaining commands
                            if (dedicatedResend)
                                break;
                        }
                        else throw new InternalCommandException(exception.Message, exception);
                        break;
                    case ErrorType.ConnectionException:
                        if (command.CommandRequestType == RequestType.ChunkRead)
                            throw new OperationFailedException("Enumeration has been modified");
                        if (!command.IsInternalCommand)
                        {
                            bool dedicatedResend = RetrySendBulkCommand(command);
                            //if resent as dedicated command, no need to execute remaining commands
                            if (dedicatedResend)
                                break;
                        }
                        else throw new InternalCommandException(exception.Message, exception);
                        break;
                    case ErrorType.Exception:
                        if (_logger.IsErrorLogsEnabled)
                            _logger.NCacheLog.Error("Broker.ExecuteRequest",
                                "RequestID :" + command.RequestId + " " + command.CommandName +
                                " can not sent to server " + address + exception.ToString());
                        throw new OperationFailedException(exception.Message, exception);
                }
            }  
        }

        private void RetryNonBulkRequest(IEnumerable<KeyValuePair<Address, CommandBase>> faliedCommands)
        {
            foreach (KeyValuePair<Address, CommandBase> pair in faliedCommands)
            {
                CommandBase command = pair.Value;

                if (command.SendError == null) continue;

                ErrorType errorType = command.SendError.Type;
                Exception exception = command.SendError.Exception;

                switch (errorType)
                {
                    case ErrorType.ActivityBlocked:
                        //should check for request type or not?
                        if (!command.IsInternalCommand)
                            RetrySendCommand(command);
                        else throw new InternalCommandException(exception.Message, exception);
                        break;
                    case ErrorType.ConnectionException:
                        if (!command.IsInternalCommand)
                            RetrySendCommand(command);
                        else throw new InternalCommandException(exception.Message, exception);
                        break;
                    case ErrorType.Exception:
                        throw exception;
                }
            }
        }

        /// <summary>
        /// Ensure execution of request on remote cache server.
        /// </summary>
        /// <param name="request">Request to be executed</param>
        /// <param name="connection">Preferred connection to execute the request</param>
        /// <param name="checkConnected">If True, checks connection status before execution.</param>
        /// <param name="waitForResponse">If True, this function will be a blocking call till response is recieved</param>
        /// <exception cref=" Alachisoft.NCache.Client.Communication.ParallelRequsetFailedException">Must be handled by bulk operations.</exception>
        internal void ExecuteRequest(Request request, Connection connection, bool checkConnected, bool waitForResponse)
        {

            if (waitForResponse && _shutdownServers.Count > 0)
            {
                if (request.RequestTimeout == _operationTimeout)
                {
                    request.RequestTimeout = request.RequestTimeout + _shutdownTimeout;
                }
            }

            try
            {
                //1. Add request to request table
                if (waitForResponse)
                {
                    AddRequestToRequestTable(request);
                }

                //2. send each command. This method not only takes care of the specific connection for 
                //sending command, it also intializes the response for the connection.
                //This case will have only one command, since IsBulk returns true for more than one commands
                //In case of send failure for only one command, command can be resend to any other available connection
                if (!request.IsBulk)
                {
                    foreach (CommandBase command in request.Commands.Values)
                    {
                        bool nullConnection = connection == null;
                        try
                        {
                            if (nullConnection)
                                SendCommand(command, false);
                            else
                                SendCommand(connection, command, checkConnected, false);
                        }
                        catch (ActivityBlockedException ex)
                        {
                            //should check for request type or not?
                            if (!command.IsInternalCommand)
                                RetrySendCommand(command);
                            else throw new InternalCommandException(ex.Message, ex);
                        }
                        catch (ConnectionException ex)
                        {
                            if (!command.IsInternalCommand)
                                RetrySendCommand(command);
                            else throw new InternalCommandException(ex.Message, ex);
                        }
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
                        connection = VerifyServerConnectivity(ip, optimizeConnection);
                        if (connection == null && command.CommandType == CommandType.GET_NEXT_CHUNK)
                            throw new OperationFailedException(ErrorCodes.Common.ENUMERATION_MODIFIED,ErrorMessages.GetErrorMessage(ErrorCodes.Common.ENUMERATION_MODIFIED));


                        try
                        {
                            
                            SendCommand(connection, command, true, false);
                        }
                        catch (ActivityBlockedException ex)
                        {
                            if (command.CommandRequestType == RequestType.ChunkRead)
                                throw new OperationFailedException(ErrorCodes.Common.ENUMERATION_MODIFIED, ErrorMessages.GetErrorMessage(ErrorCodes.Common.ENUMERATION_MODIFIED));
                            if (!command.IsInternalCommand)
                            {
                                bool dedicatedResend = RetrySendBulkCommand(command);
                                //if resent as dedicated command, no need to execute remaining commands
                                if (dedicatedResend)
                                    break;
                            }
                            else throw new InternalCommandException(ex.Message, ex);
                        }
                        catch (ConnectionException ex)
                        {

                            if (command.CommandRequestType == RequestType.ChunkRead)
                                throw new OperationFailedException(ErrorCodes.Common.ENUMERATION_MODIFIED, ErrorMessages.GetErrorMessage(ErrorCodes.Common.ENUMERATION_MODIFIED));
                            if (!command.IsInternalCommand)
                            {
                                
                                    bool dedicatedResend = RetrySendBulkCommand(command);
                                    //if resent as dedicated command, no need to execute remaining commands
                                    if (dedicatedResend)
                                        break;
                                
                            }
                            else throw new InternalCommandException(ex.Message, ex);
                        }
                        catch (Exception e)
                        {
                            if (_logger.IsErrorLogsEnabled)
                                _logger.NCacheLog.Error("Broker.ExecuteRequest",
                                    "RequestID :" + command.RequestId + " " + command.CommandName +
                                    " can not sent to server " + connection.Address + e.ToString());
                            throw new OperationFailedException(e.Message, e);
                        }
                    }
                }

                if (waitForResponse)
                {
                    bool reacquiredLock = true;
                    int timeout = Convert.ToInt32(request.RequestTimeout);
                    long startTime = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
                    bool recheckCommandStatus = false;

                    try
                    {
                        while (timeout > 0)
                        {
                            recheckCommandStatus = false;
                            if (request.IsAsync)
                                break;

                   

                            if (request.IsCompleteResponseReceived)
                            {
                                if (request.HasFailedResponses)
                                    continue;

                                if (request.FailedCommands.Count > 0)
                                {
                                    if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.CriticalInfo("ExecuteRequest",
                                        "failed to receive response(s) from " + request.FailedCommands.Count +
                                        " command(s) for requestId: " + request.RequestId);
                                    CommandBase[] failedCommands = request.FailedCommands.ToArray();
                                    request.FailedCommands.Clear();
                                    //All commands will be of same type as of first command e.g. IsSafe,CommandRequestType etc

                                    if (failedCommands[0].IsInternalCommand)
                                        throw new InternalCommandException("Could not receive response for internal command. ");
                                    //There will be at-max one command for dedicated requests
                                    if (failedCommands[0].IsSafe)
                                    {
                                        // Retrying safe command, consider all parallel commands as failed
                                        RetrySafeCommand(request, request.Commands.Values);
                                    }
                                    else
                                    {
                                        recheckCommandStatus = RetryUnsafeCommand(request, failedCommands, startTime, failedCommands[0].CommandRequestType == RequestType.NonKeyBulkWrite);
                                    }

                                }
                                else
                                    break;
                            }
                            lock (request)
                            {

                                if (recheckCommandStatus)
                                    continue;
                                timeout = Convert.ToInt32(request.RequestTimeout) -
                                          (int)((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - startTime);
                                if (!request.IsCompleteResponseReceived && timeout > 0) // fix for non-negative value exception
                                    reacquiredLock = Monitor.Wait(request, timeout);

                                if (!reacquiredLock)
                                {
                                    if (request.IsRequestTimeoutReset)
                                    {
                                        timeout = (Convert.ToInt32(request.RequestTimeout) -
                                                   (int)
                                                       ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 -
                                                        startTime));
                                        if (timeout > 0)
                                        {
                                            reacquiredLock = true;
                                            reacquiredLock = Monitor.Wait(request, timeout);
                                        }
                                    }
                                }

                                if (!reacquiredLock && !request.IsCompleteResponseReceived)
                                {


                                    if (_perfStatsColl != null) _perfStatsColl.DecrementRequestQueueSizeStats();

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
                        if (_perfStatsColl != null) _perfStatsColl.DecrementRequestQueueSizeStats();

                    }
                }

            }
            finally
            {
                // Async requests are removed from table when Async completion response is recieved
                if (waitForResponse && !request.IsAsync)
                    RemoveRequestFromRequestTable(request);
                requestModerator.UnRegisterRequest(request.RequestId);
            }
        }
		
		
        private void RetrySafeCommand(Request request, IEnumerable<CommandBase> failedCommands)
        {
            request.ClearResponses();
            //Create dedicated command
            CommandBase dedicatedCommand =
                CommandBase.GetDedicatedCommand(failedCommands);
            //Set retry flag true
            dedicatedCommand.IsRetry = true;
            RetrySendCommand(dedicatedCommand);
        }
        /// <summary>
        ///This method id for unsafe parallel commands
        ///Keybased parallel unsafe commands
        ///Remove failed response
        ///Inquiry each command seperatly
        ///re-execute each command(whose inquiry is failed) else add response recieved by inquiry
        ///For non-key based parallel unsafe commands e.g. DeleteQuery
        ///Remove failed response
        ///Inquiry each command seperatly
        ///add response recieved by inquiry
        /// </summary>
        /// <param name="request"></param>
        /// <param name="failedCommands"></param>
        /// <param name="executionStartTime"></param>
        /// <param name="sendDedicatedRequest"></param>
        /// <returns></returns>
        private bool RetryUnsafeCommand(Request request, CommandBase[] failedCommands, long executionStartTime, bool sendDedicatedRequest)
        {
            bool checkRequestStatus = false;
            bool retrySend = false;
            bool retryDedicated = false;

            if (failedCommands == null || failedCommands.Length == 0) return false;

            foreach (CommandBase command in failedCommands)
            {
                retrySend = false;
                request.RemoveResponse(command.FinalDestinationAddress,
                                                               command.CommandID);

                //Inquiry command
                int timeout = Convert.ToInt32(request.RequestTimeout) -
                          (int)
                              ((System.DateTime.Now.Ticks - 621355968000000000) /
                               10000 - executionStartTime);
                if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.CriticalInfo("ExecuteRequest",
                    "Inquiring for command " + command.CommandType +
                    " of RequestId: " + command.Parent.RequestId);

                CommandResponse inquiryResponse = Inquire(command,
                    timeout);
                //If inquiry fails, re-execute command, else add command resposne(returned by inquiry)
                if (inquiryResponse.InquiryResponse != null)
                {
                    if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.CriticalInfo("ExecuteRequest",
                     "Inquiry Result for command " +
                     command.CommandType + ": " +
                     inquiryResponse.InquiryResponse.status.ToString());

                    switch (inquiryResponse.InquiryResponse.status)
                    {
                        case
                            Alachisoft.NCache.Common.Enum.RequestStatus
                                .RECEIVED_AND_EXECUTED:
                            Response response
                               =
                               null;
                            using (MemoryStream stream = new MemoryStream())
                            {
                                foreach (
                                    byte[] bytes in
                                        inquiryResponse.InquiryResponse.value)
                                {
                                    stream.Write(bytes, 0, bytes.Length);
                                }
                                stream.Seek(0, SeekOrigin.Begin);
                                response =
                                    Common.Util.ResponseHelper
                                        .DeserializeResponse(
                                            stream.ToArray());
                                stream.Close();
                            }
                            CommandResponse cmdResponse = new CommandResponse(
                                false, command.FinalDestinationAddress);
                            cmdResponse.Result = response;
                            request.InitializeResponse(
                                command.FinalDestinationAddress, command);
                            request.AddResponse(command.FinalDestinationAddress,
                                cmdResponse);
                            break;
                        case
                            Alachisoft.NCache.Common.Enum.RequestStatus.NOT_RECEIVED:
                        case
                            Alachisoft.NCache.Common.Enum.RequestStatus.RECEIVED_WITH_ERROR:
                        case
                            Alachisoft.NCache.Common.Enum.RequestStatus.NODE_DOWN:
                            retrySend = true;
                            break;
                        case
                            Alachisoft.NCache.Common.Enum.RequestStatus
                                .RECEIVED_AND_INEXECUTION:
                            //If command is in execution on server, add again to failed commands, will be inquired again in next cycle
                            request.FailedCommands.Add(command);
                            request.InitializeFailedResponse(
                                command.FinalDestinationAddress, command);
                            checkRequestStatus = true;
                            break;
                    }
                }
                else
                    retrySend = true;

                if (retrySend)
                {
                    if (sendDedicatedRequest)
                        retryDedicated |= retrySend;
                    else
                        RetrySendCommand(command);
                }
            }

            if (retryDedicated)
            {
                CommandBase dedicatedCommmand = null;
                if (sendDedicatedRequest)
                {
                    dedicatedCommmand = CommandBase.GetDedicatedCommand(failedCommands);
                    RetrySendCommand(dedicatedCommmand);
                }
            }
            return checkRequestStatus;
        }


        internal CommandResponse Inquire(CommandBase command, long timeout)
        {
            Request inquiryRequest = new Request(false, timeout);
            InquiryRequestCommand inquiryCommand = new InquiryRequestCommand(command.RequestId, command.CommandID,
                command.FinalDestinationAddress.IpAddress.ToString());
            Address ipAddress = GetConnectionIP(command);
            inquiryRequest.AddCommand(ipAddress, inquiryCommand);
            ExecuteRequest(inquiryRequest);
            return inquiryRequest.Response;
        }

        /// <summary>
        /// Ensure execution of request on remote cache server. This is a blocking function call, till the response from server is recieved.
        /// </summary>
        /// <param name="request">Request to be executed</param>
        /// <exception cref=" Alachisoft.NCache.Client.Communication.ParallelRequsetFailedException">Must be handled by bulk operations.</exception>

        internal void ExecuteRequest(Request request)
        {
            ExecuteRequest(request, null, true, true);
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

        internal void SendCommand(CommandBase command, bool waitUntilSend)
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
                connection = TryPool();

                if (connection != null && connection.ServerAddress != null)
                    _connection = connection;

                if (_connection == null) _tryPool = true;
            }

            //If not POR or Partitioned and connection is not connected, try to get another connection before sending command 
            if (!this._importHashmap && connection != null && !connection.IsConnected)
            {
                TryNextServer();
                connection = _connection;
            }

            SendCommand(connection, command, true, waitUntilSend);
        }

        internal void SendCommand(Connection connection, CommandBase command, bool checkConnected, bool waitUntilSend)
        {
            Address ip = connection.ServerAddress;
            if (checkConnected)
            {
                connection.StatusLatch.WaitForAny(ConnectionStatus.Connected | ConnectionStatus.Disconnected | ConnectionStatus.LoadBalance);
            }
            try
            {
                DoSendCommand(connection, command, checkConnected, waitUntilSend);
                if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.Debug("Broker.SendCommand", "RequestID : " + command.RequestId + " " + command.CommandName + " sent to server " + connection.IpAddress);
            }
            catch (OperationFailedException e)
            {
                if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.SendCommand", "RequestID :" + command.RequestId + " " + command.CommandName + " can not sent to server " + connection.IpAddress + " " + e.ToString());

                throw;
            }
            catch (ActivityBlockedException ex)
            {
                throw ex;
            }
            catch (Exception e)
            {
                if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.SendCommand", "RequestID :" + command.RequestId + " " + command.CommandName + " can not sent to server " + connection.IpAddress + " " + e.ToString());
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

            if (_connectionPinger != null) _connectionPinger.Stop();

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
                }



                _perfStatsColl.Dispose();

                connection.Dispose();
            }

            try
            {
                if (this._processor != null) this._processor.Stop();
                if (_eventProcessor != null) _eventProcessor.Stop();
            }
            catch (ThreadAbortException) { }
            catch (ThreadInterruptedException) { }

            try
            {
                if (_logger.NCacheLog != null)
                {
                    _logger.NCacheLog.Flush();
                    _logger.NCacheLog.Close();
                }
            }
            catch (Exception exception) { }

            StopWriter();
        }

        private void StopWriter()
        {
            if (_dedicatedWriter != null && _dedicatedWriter.IsAlive)
            {
#if NETCORE
                _dedicatedWriter.Interrupt();
#else
                _dedicatedWriter.Abort();
#endif

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

        internal void DoSendCommand(Connection connection, CommandBase command, bool checkConnected,bool waitUntilSend)
        {
            if (_shutdownServers.Count > 0)
            {
                bool reacquiredLock = true;

                if (command.CommandRequestType != RequestType.InternalCommand)
                {
                    ShutDownServerInfo ssInfo = (ShutDownServerInfo)_shutdownServers[connection.ServerAddress];
                    if (ssInfo != null)
                    {
                        lock (ssInfo.WaitForBlockedActivity)
                        {
                            if (_shutdownServers.ContainsKey(connection.ServerAddress))
                            {
                                long startTime = (ssInfo.StartBlockingTime.Ticks - 621355968000000000) / 10000;
                                int timeout = Convert.ToInt32(ssInfo.BlockInterval * 1000) - (int)((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - startTime);
                                if (timeout > 0)
                                {
                                    reacquiredLock = Monitor.Wait(ssInfo.WaitForBlockedActivity, (int)timeout);
                                    throw new ActivityBlockedException(ErrorCodes.Common.REQUEST_TIMEOUT,ErrorMessages.GetErrorMessage(ErrorCodes.Common.REQUEST_TIMEOUT), ssInfo.BlockServerAddress);
                                }
                            }
                        }
                    }
                }
            }

            if (command.CommandType == CommandType.INQUIRY_REQUEST)
            {
                if (!connection.RequestInquiryEnabled)
                    throw new OperationFailedException(ErrorCodes.Common.OPERATION_INTERRUPTED,ErrorMessages.GetErrorMessage(ErrorCodes.Common.OPERATION_INTERRUPTED));
            }


            InitializeResponse(connection, command);
            command._cacheId = _cacheId;

            try
            {
                long acknowledgement = -1;
                if (connection.RequestInquiryEnabled && !command.IsSafe)
                {
                    acknowledgement = requestModerator.RegisterRequest(connection.IpAddress, command.RequestId);
                    command.AcknowledgmentId = acknowledgement;
                }

                //Following code is to be refactored, it's written poorly for the reason, to keep the newly introduced
                //secure-communication path as isolated it can be from the old communication path.
                if (!connection.Optimized)
                {
                  
                        
                        connection.AssureSendDirect(command.ToByte(acknowledgement, connection.RequestInquiryEnabled), connection.PrimaryClientSocket, checkConnected);
                    
                }
                else if (command is InitSecondarySocketCommand)
                {
                   
                        connection.AssureSendDirect(command.ToByte(acknowledgement, connection.RequestInquiryEnabled), connection.SecondaryClientSocket, checkConnected);
                   
                }
                else
                {
                   
                        connection.SendCommand(command.ToByte(acknowledgement, connection.RequestInquiryEnabled), checkConnected);
                    
                }
            }
            catch (ConnectionException e)
            {
                if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.Info("Broker.DoSendCommand", e.ToString());
                RemoveResponse(connection, command);
                throw;
            }
            catch (Exception e)
            {
                if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.Info("Broker.DoSendCommand", e.ToString());
                RemoveResponse(connection, command);
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
                if (_perfStatsColl != null)
                    _perfStatsColl.IncrementRequestQueueSizeStats();

            }
        }

        internal Request GetRequest(long requestId)
        {
            Request request = null;
            lock (_requestTable.SyncRoot)
            {
                if (_requestTable.ContainsKey(requestId))
                    request = (Request)_requestTable[requestId];
            }

            return request;
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
            if (_perfStatsColl != null && requestExistedInTable) _perfStatsColl.DecrementRequestQueueSizeStats();

        }



        /// <summary>
        /// Adds request to request table. Depending on type of type is request id is set here.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="command"></param>
        private void InitializeResponse(Connection connection, CommandBase command)
        {
            command.Parent.InitializeResponse(connection.ServerAddress, command);
        }

        private void RemoveResponse(Connection connection, CommandBase command)
        {
            command.Parent.RemoveResponse(connection.ServerAddress, command.CommandID);
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
                            if (connection != null)
                            {
                                ReconectInBackground(connection.ServerAddress, connection);
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
                    if (_shutdownServers.ContainsKey(ip))
                    {
                        ShutDownServerInfo ssInfo = (ShutDownServerInfo)_shutdownServers[ip];

                        lock (ssInfo.WaitForBlockedActivity)
                        {

                            Monitor.PulseAll(ssInfo.WaitForBlockedActivity);
                            _shutdownServers.Remove(ip);
                        }
                    }
                }

                ResetBroker(ip);
                this._clientConfig.RemoveServer(new ServerInfo(ip.IpAddress, ip.Port));

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

                    // raise this event if client once disconnected and then was not able to connect again.
                    try
                    {
                        if (!_connection.IsConnected )
                        {
                            TryPool();
                           
                        }
                        else if (_connection.IsConnected)
                        {
                            _cache.EventListener.OnReregisterTopic();
                        }
                    }
                    catch (Exception) { }

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

                    //[Ata] No need, it would already be registered when 
                    //connection is created

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
                        Request request = (Request)iter.Current;
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
            ServerInfo startingServer = null;
            ArrayList deniedServers = null;
            Hashtable invalidServers = new Hashtable();
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
                                if (!_clientConfig.BalanceNodes)
                                    _clientConfig.CurrentServerIndex = 0;
                                ServerInfo nextServer = _clientConfig.NextServer;
                                startingServer = nextServer;
                                bool triedWithParamPort = true;
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
                                                    connected = ConnectRemoteServer(this._connection, nextServer.IP,
                                                        nextServer.Port + i, this._balanceNode, this._importHashmap, true, ref exception);

                                                }
                                                if (connected)
                                                {
                                                    break;
                                                }
                                            }


                                            catch (SecurityException)
                                            {
                                                if (deniedServers == null)
                                                    deniedServers = new ArrayList();
                                                if (!deniedServers.Contains(nextServer.Name))
                                                    deniedServers.Add(nextServer.Name);
                                            }
                                            //if free clients try to connect with licensed servers
                                            catch (LicensingException ex)
                                            {
                                                if (!invalidServers.Contains(nextServer.Name))
                                                    invalidServers.Add(nextServer.Name, ex);
                                            }
                                            catch (InternalCommandException)
                                            { }
                                            catch (OperationNotSupportedException operationException)
                                            {
                                                exceptionThrown = operationException;
                                            }
                                        }
                                    }

                                    if (!connected)
                                    {
                                        if (triedWithParamPort && _clientConfig.IsDifferentParamPort(nextServer.Port))
                                        {
                                            triedWithParamPort = false;
                                            nextServer.Port = _clientConfig.ConfigServerPort;
                                        }
                                        else
                                        {
                                            triedWithParamPort = true;
                                            nextServer = _clientConfig.NextServer;
                                            if (startingServer.Equals(nextServer)) break;
                                        }
                                    }
                                }
                                //muds:
                                //if the connection is established, exit the outer loop.
                                //otherwise sleep for the sleep interval and retry.
                                if (connected) break;
                                System.Threading.Thread.Sleep(_retryInterval);
                                continue;
                            }
                            else
                                throw new ConfigurationException(ErrorCodes.CacheInit.SERVER_INFO_NOT_FOUND,ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.SERVER_INFO_NOT_FOUND));
                        }
                        else
                            connected = true;

                    }
                    catch (Exception) { throw; }
                }
            }
            catch (Exception) { throw; }
            finally
            {
                //set the connection status
                byte setStatus = connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
                byte unsetStatus = (byte)((!connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected) | ConnectionStatus.Connecting);
                _connection.StatusLatch.SetStatusBit(setStatus, unsetStatus);

                _retryConnection = connected;

                //release the lock
                _lock.ReleaseWriterLock();

                //muds:
                //if security exception was thrown from some server(s), then we
                //must pass it to the client application.
                if (!connected)
                {
                    if (deniedServers != null && deniedServers.Count > 0)
                    {
                        bool first = true;
                        System.Text.StringBuilder sb = new System.Text.StringBuilder("You do not have permissions to perform the operation on : ");
                        IEnumerator ie = deniedServers.GetEnumerator();
                        while (ie.MoveNext())
                        {
                            if (!first)
                            {
                                sb.Append(" ,");
                                first = false;
                            }
                            sb.Append("\'" + ie.Current as string + "\'");
                        }

                        throw new SecurityException(sb.ToString());

                    }
                    //if free clients try to connect with licensed servers
                    if (invalidServers != null && invalidServers.Count > 0)
                    {
                        bool first = true;
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        IDictionaryEnumerator ie = invalidServers.GetEnumerator();
                        while (ie.MoveNext())
                        {
                            if (!first)
                            {
                                sb.Append(" ,");
                                first = false;
                            }
                            sb.Append(ie.Value);
                        }

                        throw new LicensingException(sb.ToString());

                    }

                    if (exceptionThrown != null)
                        throw exceptionThrown;
                }
            }
        }

        private Connection ReconnectServer(Connection connection, ServerInfo nextServer)
        {
            bool connected = false;
            ArrayList deniedServers = null;
            ArrayList invalidServers = null;
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
               

                while (retries-- > 0 && !_isDisposing)
                {
                    if (!connection.IsConnected)
                    {
                        connection.StatusLatch.SetStatusBit(ConnectionStatus.Connecting,
                            ConnectionStatus.Connected | ConnectionStatus.Disconnected);

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

                                        connected = ConnectRemoteServer(connection, nextServer.IP, nextServer.Port + i,
                                            this._balanceNode, this._importHashmap, true, ref exception);


                                    }
                                    if (connected)
                                    {
                                        if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.CriticalInfo("ReconnectServer", "Reconnected with: " + connection.IpAddress);
                                        break;
                                    }
                                }

                                catch (SecurityException)
                                {
                                    if (deniedServers == null)
                                        deniedServers = new ArrayList();
                                    if (!deniedServers.Contains(nextServer.Name))
                                        deniedServers.Add(nextServer.Name);
                                }
                                catch (LicensingException)
                                {
                                    if (invalidServers == null)
                                        invalidServers = new ArrayList();
                                    if (!invalidServers.Contains(nextServer.Name))
                                        invalidServers.Add(nextServer.Name);
                                }

                                catch (OperationNotSupportedException operationException)
                                {
                                    exceptionThrown = operationException;
                                }
                                catch (InternalCommandException ex)
                                {
                                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StartServices", ex.ToString());
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
            }
            catch (Exception) { throw; }
            finally
            {
                byte setStatus = connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
                byte unsetStatus = (byte)((!connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected) | ConnectionStatus.Connecting);
                connection.StatusLatch.SetStatusBit(setStatus, unsetStatus);

                _retryConnection = connected; //[KS : Connection is up again so we can retry ]

                _lock.ReleaseWriterLock();

                //muds:
                //if security exception was thrown from some server(s), then we
                //must pass it to the client application.
                if (!connected)
                {
                    if (deniedServers != null && deniedServers.Count > 0)
                    {
                        bool first = true;
                        System.Text.StringBuilder sb = new System.Text.StringBuilder("You do not have permissions to perform the operation on : ");
                        IEnumerator ie = deniedServers.GetEnumerator();
                        while (ie.MoveNext())
                        {
                            if (!first)
                            {
                                sb.Append(" ,");
                                first = false;
                            }
                            sb.Append("\'" + ie.Current as string + "\'");
                        }

                        throw new SecurityException(sb.ToString());

                    }
                    //if free clients try to connect with licensed servers
                    if (invalidServers != null && invalidServers.Count > 0)
                    {
                        bool first = true;
                        System.Text.StringBuilder sb = new System.Text.StringBuilder("You do not have valid license to communicate with : ");
                        IEnumerator ie = invalidServers.GetEnumerator();
                        while (ie.MoveNext())
                        {
                            if (!first)
                            {
                                sb.Append(" ,");
                                first = false;
                            }
                            sb.Append("\'" + ie.Current as string + "\'");
                        }

                        throw new LicensingException(sb.ToString());

                    }
                    if (exceptionThrown != null)
                        throw exceptionThrown;
                }
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
                        ReconectInBackground(connection.ServerAddress, connection);
                    }
                }
                else return connection;
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
        /// <returns>True if connection was successful, false otherwise</returns>
        internal bool TryConnecting(Connection connection, ref Exception exception, int connectionWaitInterval = 0, bool importHashmap = true )
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


                        connection.StatusLatch.SetStatusBit(ConnectionStatus.Connecting, ConnectionStatus.Connected | ConnectionStatus.Disconnected);

                        if (connectionWaitInterval > 0) Thread.Sleep(connectionWaitInterval);

                        connected = ConnectRemoteServer(connection, connection.Address, this._port, false, importHashmap, NeedsNotifRegistration(), ref exception);

                        if (connected)
                        {
                            if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.Info("Broker.TryConnecting", "Connection established with " + connection.IpAddress);
                        }
                    }
                    catch (LicensingException)
                    {
                        if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.TryConnecting", "Client does not have a valid license to communicate with the cache server : " + connection.IpAddress);
                    }
                    catch (SecurityException)
                    {
                        if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.TryConnecting", "You do not have permissions to perform the operation on : " + connection.IpAddress);
                    }


                    catch (OperationNotSupportedException)
                    {
                    }
                    catch (InternalCommandException ex)
                    {
                        if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.StartServices", ex.ToString());
                    }
                    finally
                    {
                        byte setStatus = connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
                        byte unsetStatus = (byte)((!connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected) | ConnectionStatus.Connecting);
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
            if (!_clientConfig.BalanceNodes)
                _clientConfig.CurrentServerIndex = 0;

            int index = 0;
            ServerInfo nextServer = _clientConfig.GetServerAt(index);
            ServerInfo startingServer = new ServerInfo(nextServer.IP.ToString(), nextServer.Port);

            while (true && !_isDisposing)
            {
                if (_clientConfig.ServerCount > 0 && index < _clientConfig.ServerCount)
                {
                    connection = this._pool[new Address(nextServer.IP.ToString(), nextServer.Port)];

                    if (connection != null && connection.IsConnected) break;

                    else if (connection != null && !connection.IsReconnecting)
                    {
                        ReconectInBackground(connection.ServerAddress, connection);
                        //connection = null;
                    }
                    else
                    {
                        nextServer = _clientConfig.GetServerAt(++index);
                        //   nextServer = _clientConfig.NextServer;
                        if (startingServer.Equals(nextServer)) break;
                    }
                }
                else
                {
                    if (index >= _clientConfig.ServerCount)
                        nextServer = startingServer;
                    break;
                }
            }
              
            if (connection == null || !connection.IsConnected)
            {
                index = 0;
                nextServer = _clientConfig.GetServerAt(index);

                while (true && !_isDisposing)
                {
                    if (_clientConfig.ServerCount > 0 && index < _clientConfig.ServerCount)
                    {
                        bool found = false;
                        if (this._pool.Contains(new Address(nextServer.IP.ToString(), nextServer.Port)))
                        {
                            connection = this._pool[new Address(nextServer.IP.ToString(), nextServer.Port)];
                            found = true;
                        }
                        else
                        {
                            connection = new Connection(this, this._commandReieved, this._serverLost, this._logger, _perfStatsColl, _responseIntegrator, _clientConfig.BindIP, this._cacheId);
                        }  

                        if (!connection.IsConnected)
                        {
                            if (!connection.IsReconnecting)
                            {
                                connection = ReconnectServer(connection, nextServer);

                                //in case we could not acquire lock and no call to connect is made
                                if (connection.ServerAddress == null)
                                {
                                    connection.ServerAddress = new Address(nextServer.IP, nextServer.Port);
                                }

                                if (connection.IsConnected)
                                {
                                    if (!this._pool.Contains(new Address(nextServer.IP.ToString(), nextServer.Port)))
                                        this._pool[new Address(nextServer.IP.ToString(), nextServer.Port)] = connection;

                                    break;
                                }
                            }

                            nextServer = _clientConfig.GetServerAt(++index);
                            if (startingServer.Equals(nextServer)) break;
                            
                        }
                        else
                            break;
                    }
                    else {
                        if (index >= _clientConfig.ServerCount)
                            nextServer = startingServer;
                        break; }
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
        /// <param name="securityParams">security parameters</param>
        /// <param name="registerNotifs">if re registring of events is needed</param>
        /// <returns>true if connecion is established, false otherwise</returns>
        private bool ConnectRemoteServer(Connection connection, ServerInfo server, bool registerNotifs)
        {
            bool connected = false;
            Exception exception = null;

            if (server != null && server.IP != null)
            {
                connected = ConnectRemoteServer(connection, server.IP, server.Port, this._balanceNode, this._importHashmap, registerNotifs, ref exception);
            }

            return connected;
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
        /// <param name="securityParams">security parameters</param>
        /// <returns>true if connecion is established, false otherwise</returns>
        private bool ConnectRemoteServer(Connection connection, IPAddress addr, int port, bool balanceNodes, bool importHashmap, bool registerNotifs, ref Exception exception)
        {
            bool connected = connection.Connect(addr, port);
            //remove old entry from message integrator if exist
            _responseIntegrator.RemoveServer(new Address(addr, port));
            if (connected)
            {
                try
                {
                    Dictionary<string, int> runningServers;
#if !(DEVELOPMENT || PROF_DEV)
                    //Populating the server list at initialization time.
                    if (_clientConfig.IPMappingConfigured)
                    {
                        GetServerMapping(connection, true);
                    }
                    // connect with cache host
                    connected = ConnectWithCacheHost(connection, addr, port);

                    if (balanceNodes)
                    {
                        CommandResponse response = IsOptimalServer(connection, addr, port);
                        ServerInfo rm = _clientConfig.GetMappedServer(response.ServerIPAddress.ToString(), response.ServerPort);

                        if (response != null && (addr.ToString() != rm.Name || port != rm.Port))
                            connected = TryConnectTo(connection, rm);
                    }
#endif
                    if (connected)
                    {
                        if (_logger.IsDetailedLogsEnabled)
                            _logger.NCacheLog.Info("Broker.ConnectRemoteServer", "[Local : (" +
                            (connection.PrimaryClientSocket != null ?
                                (connection.PrimaryClientSocket.LocalEndPoint != null ?
                                    connection.PrimaryClientSocket.LocalEndPoint.ToString() : "localendpointNULL") :
                                    "primaryclientsocketNULL")
                                    + ") Server : (" + addr.ToString() + ":" + port + ")] connected successfully");


                        InitializeCache(connection, addr, port, balanceNodes);
                      
                        if (connection.SupportDualSocket) InitializeSecondarySocket(connection, addr, port);

                        runningServers =  GetRunningServers(connection, addr, port);

                        connection.Init();

                        if (runningServers != null)
                        {
                            int outPort;

                            foreach (string str in runningServers.Keys)
                            {
                                ServerInfo rServer = new ServerInfo();

                                runningServers.TryGetValue(str, out outPort);
                                rServer.Name = str;
                                rServer.IP = System.Net.IPAddress.Parse(str);
                                rServer.Port = outPort;
                                rServer.Priority = short.MaxValue;
                                _clientConfig.AddServer(rServer);
                            }
                        }


                        if (_logger.IsDetailedLogsEnabled) _logger.NCacheLog.Info("Broker.ConnectRemoteServer", "[Local : (" + connection.PrimaryClientSocket.LocalEndPoint.ToString() + ") Server : (" + addr.ToString() + ":" + port + ")] initialized cache successfully");
                    }
                }

                catch (SecurityException sec_ex)
                {
                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.ConnectRemoteServer", sec_ex.ToString());
                    connection.Disconnect();
                    connected = false;
                    throw;
                }
                catch (LicensingException lic)
                {
                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.ConnectRemoteServer", lic.ToString());
                    connection.Disconnect();
                    connected = false;
                    throw;
                }

                catch (OperationNotSupportedException ons)
                {
                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.ConnectRemoteServer", ons.ToString());
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
                if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.ConnectRemoteServer", "Could not connect to server (" + addr.ToString() + ":" + port + ")");
            }
            if (connected)
            {
                if (_processor != null)
                    _processor.Start();

                connection.StatusLatch.SetStatusBit(ConnectionStatus.Connected, ConnectionStatus.Disconnected | ConnectionStatus.Connecting);

                _serverIP = connection.ServerAddress;
                _port = connection.Port;

                _pool.Add(connection.ServerAddress, connection);

                if (importHashmap)
                {
                    GetHashmap(connection);
                }
                if (registerNotifs)
                {
                    _cache.ReRegisterGeneralNotification(connection);
                    connection.NotifRegistered = true;
                }
                if (_importHashmap)
                {
                    _cache.RegisterHashmapChangedEvent(connection);
                }

                _cache.GetTypeInfoMap(connection);
                _cache.GetSerializationFormat(connection);

                //Not to re-register 
                if (importHashmap)
                {
                    _cache.GetCompactTypesFromServer(connection);
                }

               
                _connectingFirstTime = false;

                //Last moment fix for _connection becomes null or it's server address get's null
                if (_connection == null || _connection.ServerAddress == null)
                {
                    //DONT REMOVE IT; used for dump analysis
                    _connectionRectified = true;

                    if (_logger != null && _logger.IsErrorLogsEnabled)
                        _logger.NCacheLog.Error("Broker.ConnectRemoteServer", "_connection anamoly detected connection ? " + (_connection == null));

                    _connection = connection;
                }
            }
            return connected;
        }

        private bool TryConnectTo(Connection connection, ServerInfo remoteServer)
        {
            int mainPort = connection.Port;
            string mainIp = connection.IpAddress;
            bool mainDisconnected = false, connected = true;

            try
            {
                connection.Disconnect();
                mainDisconnected = true;
                if (connected = connection.Connect(remoteServer.IpString, remoteServer.Port))
                {
                    connected = ConnectWithCacheHost(connection, remoteServer.IP, remoteServer.Port);
                }
                else
                {
                    if (_logger.IsErrorLogsEnabled)
                        _logger.NCacheLog.Error(
                            "Broker.ConnectRemoteServer",
                            "Unable to connect to [" + remoteServer.IpString + ":" + remoteServer.Port + "], restoring existing connection with [" + mainIp + ":" + mainPort + "]");

                    try { connection.Disconnect(); } catch { }

                    if (connected = connection.Connect(mainIp, mainPort))
                        connected = ConnectWithCacheHost(connection, IPAddress.Parse(mainIp), mainPort);
                    else
                        if (_logger.IsErrorLogsEnabled)
                            _logger.NCacheLog.Error("Broker.ConnectRemoteServer", "Unable to restoring connection to [" + mainIp.ToString() + ":" + mainPort + "]");
                }
            }
            catch (Exception e)
            {
                if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.ConnectRemoteServer", "Error connecting. Error: " + e.ToString());

                if (mainDisconnected)
                {
                    try { connection.Disconnect(); } catch { }

                    if (connected = connection.Connect(mainIp, mainPort))
                        connected = ConnectWithCacheHost(connection, IPAddress.Parse(mainIp), mainPort);
                    else
                        if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.ConnectRemoteServer", "Unable to restoring connection to ["
                            + mainIp + ":" + mainPort + "]");
                }
            }
            return connected;
        }

        private bool ConnectWithCacheHost(Connection connection, IPAddress address, int port)
        {
            bool connectedWithHost = true;

            // getting management port to transfer connection 
            int cachePort = GetCachePort(connection);

            //.Net core does not support socket transfer from one process to other. In this case, client needs to directly connect to the cache host.
            //However for .net framework, service will transfer connection to the cache host.
            if (cachePort != port)
            {
                connection.Disconnect(false);

                connectedWithHost = connection.SwitchTo(this, this._commandReieved, this._serverLost, this._logger, _perfStatsColl, _responseIntegrator, _clientConfig.BindIP, this._cacheId, address, cachePort);
                if (!connectedWithHost)
                {
                    if (_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error("Broker.ConnectWithCacheHost", "Failed to connect with cache host directly on port :" + cachePort);
                }
            }
            return connectedWithHost;
        }

        private Dictionary<string, int> GetRunningServers(Connection conn, IPAddress coonectedServerAddress, int port)
        {
            GetRunningServersCommand command = new GetRunningServersCommand(_cacheId, null, null);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="connectedServerAddress"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private int GetCachePort(Connection connection)
        {
            GetCacheHostPort command = new GetCacheHostPort(_cacheId, null, null);
            Request request = new Request(false, _operationTimeout);
            request.AddCommand(connection.ServerAddress, command);

            ExecuteRequest(request, connection, false, false);
            CommandResponse managementPortRes = connection.RecieveCommandResponse();

            if (managementPortRes != null)
                managementPortRes.ParseResponse();

            return managementPortRes.CacheMangementPort;
        }

        private CommandResponse IsOptimalServer(Connection connection, IPAddress connectedServerAddress, int port)
        {
            GetOptimalServerCommand command = new GetOptimalServerCommand(_cacheId, null, null);
            Request request = new Request(false, _operationTimeout);
            request.AddCommand(connection.ServerAddress, command);

            ExecuteRequest(request, connection, false, false);

            CommandResponse balanceNodeRes = connection.RecieveCommandResponse();

            if (balanceNodeRes != null) balanceNodeRes.ParseResponse();
            return balanceNodeRes;
        }

        internal bool GetMessageDistribution(IDictionary<string, IList<string>> topicWiseMessageIds, out Dictionary<Address, Dictionary<string, IList<string>>> messageDistributionMap)
        {
            bool result = false;
            messageDistributionMap = new Dictionary<Address, Dictionary<string, IList<string>>>();

            foreach (KeyValuePair<string, IList<string>> pair in topicWiseMessageIds)
            {
                string topicName = pair.Key;
                IList<string> messageList = pair.Value;

                foreach (var messageId in messageList)
                {
                    Address address;

                    lock (_hashmapUpdateMutex)
                    {
                        address = _pool.GetIp(messageId);
                    }

                    Connection conn = _pool[address]; //for balanced connection
                    Address adrs = address;

                    if (conn != null && !conn.IsConnected)
                    {
                        Address loadbaanced = GetLoadBalancedAddress();
                        if (loadbaanced != null) adrs = loadbaanced;
                    }

                    Dictionary<string, IList<string>> topicDic;
                    if (messageDistributionMap.ContainsKey(adrs))
                    {
                        topicDic = messageDistributionMap[adrs];

                        PopulateList(topicDic, topicName, messageId);

                        result = true;
                    }
                    else
                    {
                        topicDic = new Dictionary<string, IList<string>>();

                        PopulateList(topicDic, topicName, messageId);

                        messageDistributionMap[adrs] = topicDic;
                    }
                }

            }
            return result;
        }

        private void PopulateList(Dictionary<string, IList<string>> topicDic, string topicName, string messageId)
        {
            if (topicDic.ContainsKey(topicName))
            {
                IList<string> list = topicDic[topicName];
                list.Add(messageId);
            }
            else
            {
                IList<string> list = new List<string>();
                list.Add(messageId);
                topicDic[topicName] = list;
            }
        }

        private void ReconectInBackground(Address address,Connection connection)
        { 
            lock(_reconnectTasks)
            {
                if(!_reconnectTasks.ContainsKey(address))
                {
                    if (!connection.IsReconnecting)
                    {
                        var task = new ReconnectTask(this, connection);
                        _reconnectTasks.Add(address, task);
                        this._processor.Enqueue(task);
                    }
                }
            }
        }

        private void UnregisterReconnectTask(Address address)
        {
            lock (_reconnectTasks)
            {
                if (_reconnectTasks.ContainsKey(address))
                {
                    _reconnectTasks.Remove(address);
                }
            }
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
                        try
                        {
                            Exception exception = null;

                            if (this._parent.TryConnecting(this._connection, ref exception,2000))
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
                                //Cache might be restarted
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
                            if (_parent.Logger.IsErrorLogsEnabled) _parent.Logger.NCacheLog.Error("ReconnectTask.Process", e.ToString());
                            break;
                        }
                    }
                }
                finally
                {
                    _parent.UnregisterReconnectTask(_connection.ServerAddress);
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

        private sealed class ConnectionKeepAlive
        {
            private readonly int _interval;
            private readonly Broker _encloser;
            private readonly HashSet<Connection> _idleConnections;

            private Thread _activityPerformer;
            private CancellationTokenSource _stopToken;

            internal ConnectionKeepAlive(Broker encloser, int interval)
            {
                _encloser = encloser;
                _interval = interval;
                _stopToken = new CancellationTokenSource();
                _idleConnections = new HashSet<Connection>();
            }

            internal void Start()
            {
                if (_activityPerformer != null) return;
                _activityPerformer = new Thread(Run) { IsBackground = true, Name = string.Concat(_encloser._cache.Name, ":KeepAlivePerformer") };
                _activityPerformer.Start();
                _stopToken = new CancellationTokenSource();
            }

            internal void Stop()
            {
                if (_activityPerformer == null) return;
                _activityPerformer.Interrupt();
                _stopToken.Cancel();
                _stopToken = null;
                _activityPerformer = null;
            }

            private void Run()
            {
                try
                {
                    while (!_stopToken.IsCancellationRequested)
                    {
                        MarkConnectionsIdle();

                        Thread.Sleep(_interval * 1000);

                        PopulateIdleList();
                        PingIdleList();
                    }
                }
                catch (ThreadInterruptedException)
                {
                    if (_encloser._logger != null && _encloser._logger.IsDetailedLogsEnabled)
                        _encloser._logger.NCacheLog.Info("Broker.ConnectionKeepAlive.Run", " The activity performing thread is stopping...");
                }
                catch (Exception ex)
                {
                    if (_encloser._logger != null && _encloser._logger.IsErrorLogsEnabled)
                        _encloser._logger.NCacheLog.Error("Broker.ConnectionKeepAlive.Run", " The activity performing thread has been terminated due to the problem: " + ex);
                }
            }

            private void MarkConnectionsIdle()
            {
                if (_encloser._connection != null)
                    _encloser._connection.IsIdle = true;

                if (_encloser._pool != null)
                    _encloser._pool.MarkConnectionsIdle();
            }

            private void PopulateIdleList()
            {
                if (_encloser._connection != null && _encloser._connection.IsIdle)
                    _idleConnections.Add(_encloser._connection);

                if (_encloser._pool != null)
                    foreach (var connection in _encloser._pool.GetIdleConnections())
                        if (!_idleConnections.Contains(connection))
                            _idleConnections.Add(connection);
            }

            private void PingIdleList()
            {
                foreach (var connection in _idleConnections)
                    SendPing(connection);

                _idleConnections.Clear();
            }

            private void SendPing(Connection connection, bool waitforReponse = false, bool hasResponse = true, bool isAsync = false)
            {
                try
                {
                    if (_encloser._logger != null && _encloser._logger.IsDetailedLogsEnabled)
                        _encloser._logger.NCacheLog.Info("Broker.ConnectionKeepAlive.SendPing", "Sending PingCommand to Server: " + connection.IpAddress);

                    var command = new PingCommand(hasResponse, isAsync);
                    var request = new Request(false, _encloser._operationTimeout);

                    request.AddCommand(connection.ServerAddress, command);
                    _encloser.ExecuteRequest(request, connection, true, waitforReponse);

                    if (waitforReponse)
                    {
                        CommandResponse reponse = request.Response;
                        reponse.ParseResponse();
                    }
                }
                catch (Exception ex)
                {
                    if (_encloser._logger != null && _encloser._logger.IsErrorLogsEnabled)
                        _encloser._logger.NCacheLog.Error("Broker.ConnectionKeepAlive.SendPing", "Problem occured: " + ex);
                }
            }
        }
		
	
        
    }
}
