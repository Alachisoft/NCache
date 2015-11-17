// Copyright (c) 2015 Alachisoft
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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Alachisoft.NCache.Web.Util;
using Alachisoft.NCache.Caching;
using ICallbackTask = Alachisoft.NCache.SocketServer.CallbackTasks.ICallbackTask;
using IEventTask = Alachisoft.NCache.SocketServer.EventTask.IEventTask;
using CallbackEntry = Alachisoft.NCache.Caching.CallbackEntry;
using HelperFxn = Alachisoft.NCache.SocketServer.Util.HelperFxn;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using System.IO;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.SocketServer;
using System.Collections.Generic;
using Alachisoft.NCache.SocketServer.Statistics;
using Alachisoft.NCache.Common.DataStructures.Clustered;
#if !NET20
using System.Threading.Tasks;
using Alachisoft.NCache.Common.DataStructures.Clustered;
#endif


namespace Alachisoft.NCache.SocketServer
{
    /// <summary>
    /// This class is responsible for maitaining all clients connection with server.
    /// </summary>
    public sealed class ConnectionManager
    {
        #region ------------- Constants ---------------
        /// <summary> Number of maximam pending connections</summary>
        private int _maxClient = 100;

        /// <summary> Number of bytes that will hold the incomming command size</summary>
        internal const int cmdSizeHolderBytesCount = 10;

        /// <summary> Number of bytes that will hold the incomming data size</summary>
        internal const int valSizeHolderBytesCount = 10;

        /// <summary> Total number of bytes that will hold both command and data size</summary>
        internal const int totSizeHolderBytesCount = cmdSizeHolderBytesCount + valSizeHolderBytesCount;

        /// <summary> Total buffer size used as pinned buffer for asynchronous socket io.</summary>
        internal const int pinnedBufferSize = 200 * 1024;

        /// <summary> Total number of milliseconds after which we check for idle clients and send heart beat to them.</summary>
        internal const int waitIntervalHeartBeat = 30000;

        #endregion

        /// <summary> Underlying server socket object</summary>
        private Socket _serverSocket = null;

        /// <summary> Send buffer size of connected client socket</summary>
        private int _clientSendBufferSize = 0;

        /// <summary> Receive buffer size of connected client socket</summary>
        private int _clientReceiveBufferSize = 0;

        /// <summary> Command manager object to process incomming commands</summary>
        private ICommandManager cmdManager;

        /// <summary> Stores the client connections</summary>
        internal static Hashtable ConnectionTable = new Hashtable(10);

        /// <summary> Thread to send callback and notification responces to client</summary>
        private Thread _callbacksThread = null;

        /// <summary> Reade writer lock instance used to synchronize access to socket</summary>
        private static ReaderWriterLock _readerWriterLock = new ReaderWriterLock();

        /// <summary> Hold server ip address</summary>
        private static string _serverIpAddress = string.Empty;

        /// <summary> Holds server port</summary>
        private static int _serverPort = -1;

        private Thread _eventsThread = null;

        private Logs _logger;

        private static LoggingInfo _clientLogginInfo = new LoggingInfo();

        private static Queue _callbackQueue = new Queue(256);
        private static long _fragmentedMessageId;

        private static Thread _badClientMonitoringThread;
        private static int _timeOutInterval = int.MaxValue; //by default this interval is set to Max value i.e. infinity to turn off bad client detection
        private static bool _enableBadClientDetection = false;

        private DistributedQueue _eventsAndCallbackQueue;

        private PerfStatsCollector _perfStatsCollector;

        public ConnectionManager(PerfStatsCollector statsCollector)
        {
            _eventsAndCallbackQueue = new DistributedQueue(statsCollector);
            _perfStatsCollector = statsCollector;
        }

        internal static Queue CallbackQueue
        {
            get { return _callbackQueue; }
        }

        
        public PerfStatsCollector PerfStatsColl
        {
            get { return _perfStatsCollector; }
        }

        internal DistributedQueue EventsAndCallbackQueue
        {
            get { return _eventsAndCallbackQueue; }
        }


        /// <summary>
        /// Gets the fragmented unique message id
        /// </summary>
        private static long NextFragmentedMessageID { get { return Interlocked.Increment(ref _fragmentedMessageId); } }

        private static object _client_hearbeat_mutex = new object();
        internal static object client_hearbeat_mutex
        {
            get
            {
                return _client_hearbeat_mutex;
            }
        }

        internal ICommandManager GetCommandManager(CommandManagerType cmdMgrType)
        {
            ICommandManager cmdMgr;
            switch (cmdMgrType)
            {
                case CommandManagerType.NCacheClient:
                    cmdMgr = new CommandManager(_perfStatsCollector);
                    break;
                case CommandManagerType.NCacheManagement:
                    cmdMgr = new ManagementCommandManager();
                    break;
                default:
                    cmdMgr = new CommandManager(_perfStatsCollector);
                    break;
            }
            return cmdMgr;
        }
        private static int _messageFragmentSize = 512 * 1024;

        /// <summary>
        /// Start the socket server and start listening for clients
        /// <param name="port">port at which the server will be listening</param>
        /// </summary>
        /// 
        public void Start(IPAddress bindIP, int port, int sendBuffer, int receiveBuffer, Logs logger, CommandManagerType cmdMgrType)
        {
            _logger = logger;
            _clientSendBufferSize = sendBuffer;
            _clientReceiveBufferSize = receiveBuffer;
            cmdManager = GetCommandManager(cmdMgrType);
            string maxPendingConnections="NCache.MaxPendingConnections";
            string enableServerCounters = "NCache.EnableServerCounters";

            if (!string.IsNullOrEmpty(System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.EnableBadClientDetection"]))
            {
                try
                {
                    _enableBadClientDetection = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.EnableBadClientDetection"]);
                }
                catch (Exception e)
                {
                    throw new Exception("Invalid value specified for NCacheServer.EnableBadClientDetection.");
                }

                if (_enableBadClientDetection)
                {
                    if (!string.IsNullOrEmpty(System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.ClientSocketSendTimeOut"]))
                    {
                        try
                        {
                            _timeOutInterval = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.ClientSocketSendTimeOut"]);

                        }
                        catch (Exception e)
                        { throw new Exception("Invalid value specified for NCacheServer.ClientSocketSendTimeOut."); }
                    }

                    if (_timeOutInterval < 5)
                        _timeOutInterval = 5;
                }
            }
            string maxPendingCon = System.Configuration.ConfigurationSettings.AppSettings[maxPendingConnections];
            if (maxPendingCon != null && maxPendingCon != String.Empty)
            {
                try
                {
                    _maxClient = Convert.ToInt32(maxPendingCon);
                }
                catch (Exception e)
                {
                    throw new Exception("Invalid value specified for " + maxPendingConnections + ".");
                }
            }
            string enablePerfCounters = System.Configuration.ConfigurationSettings.AppSettings[enableServerCounters];
            if (enablePerfCounters != null && enablePerfCounters != String.Empty)
            {
                try
                {
                    SocketServer.IsServerCounterEnabled = Convert.ToBoolean(enablePerfCounters);
                }
                catch (Exception e)
                {
                    throw new Exception("Invalid value specified for " + enableServerCounters + ".");
                }
            }

            string maxRspLength = System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.MaxResponseLength"];
            if (maxRspLength != null && maxRspLength != String.Empty)
            {
                try
                {
                    int messageLength = Convert.ToInt32(maxRspLength);

                    if (messageLength > 1)
                        _messageFragmentSize = messageLength * 1024;

                }
                catch (Exception e)
                {
                    throw new Exception("Invalid value specified for NCacheServer.MaxResponseLength.");
                }
            }

            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            if (bindIP == null)
            {
                try
                {
                    String hostName = Dns.GetHostName();
                    IPHostEntry ipEntry = Dns.GetHostByName(hostName);
                    bindIP = ipEntry.AddressList[0];
                }
                catch (Exception e)
                {
                }
            }

            try
            {
                if (bindIP != null)
                    _serverSocket.Bind(new IPEndPoint(bindIP, port));
                else
                {
                    _serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                }
            }
            catch (System.Net.Sockets.SocketException se)
            {
                switch (se.ErrorCode)
                {
                    // 10049 --> address not available.
                    case 10049:
                        throw new Exception("The address " + bindIP + " specified for NCacheServer.BindToIP is not valid");
                    default:
                        throw;
                }
            }

            _serverSocket.Listen(_maxClient);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), _serverSocket);

            _serverIpAddress = ((IPEndPoint)_serverSocket.LocalEndPoint).Address.ToString();
            if (cmdMgrType == CommandManagerType.NCacheClient)
                _serverPort = ((IPEndPoint)_serverSocket.LocalEndPoint).Port;

            _callbacksThread = new Thread(new ThreadStart(this.CallbackThread));
            _callbacksThread.Priority = ThreadPriority.BelowNormal;
            _callbacksThread.Start();

            _eventsThread = new Thread(new ThreadStart(this.SendBulkClientEvents));
            _eventsThread.Name = "ConnectionManager.BulkEventThread";
            _eventsThread.Start();
        }

        /// <summary>
        /// Dipose socket server and all the allocated resources
        /// </summary>
        public void Stop()
        {
            DisposeServer();
        }

        /// <summary>
        /// Dispose the client
        /// </summary>
        /// <param name="clientManager">Client manager object representing the client to be diposed</param>
        internal static void DisposeClient(ClientManager clientManager)
        {
            try
            {
                if (clientManager != null)
                {
                    if (clientManager._leftGracefully)
                    {
                        if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.ReceiveCallback", clientManager.ToString() + " left gracefully");
                    }
                    else
                    {
                        if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.ReceiveCallback", "Connection lost with client (" + clientManager.ToString() + ")");
                    }

                    if (clientManager.ClientID != null)
                    {
                        lock (ConnectionTable)
                            ConnectionTable.Remove(clientManager.ClientID);
                    }

                    clientManager.Dispose();
                    clientManager = null;
                }
            }
            catch (Exception) { }
        }

        private void AcceptCallback(IAsyncResult result)
        {
            Socket clientSocket = null;
            bool objectDisposed = false;
            try
            {
                clientSocket = ((Socket)result.AsyncState).EndAccept(result);
            }
            catch (Exception e)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.AcceptCallback", "An error occurred on EndAccept. " + e.ToString());
                return;
            }
            finally
            {
                try
                {
                    if (_serverSocket != null)
                        _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), _serverSocket);
                }
                catch (ObjectDisposedException)
                {
                    objectDisposed = true;
                }
            }

            if (objectDisposed) return;

            try
            {
                ClientManager clientManager = new ClientManager(this,clientSocket, totSizeHolderBytesCount, pinnedBufferSize);
                clientManager.ClientDisposed += new ClientDisposedCallback(OnClientDisposed);

                //Different network options depends on the underlying OS, which may support them or not
                //therefore we should handle error if they are not supported.
                try
                {
                    clientManager.ClientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, _clientSendBufferSize);
                }
                catch (Exception e)
                {
                    if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.AcceptCallback", "Can not set SendBuffer value. " + e.ToString());
                }
                try
                {
                    clientManager.ClientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, _clientReceiveBufferSize);
                }
                catch (Exception e)
                {
                    if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.AcceptCallback", "Can not set ReceiveBuffer value. " + e.ToString());
                }
                try
                {
                    clientManager.ClientSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
                }
                catch (Exception e)
                {
                    if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.AcceptCallback", "Can not set NoDelay option. " + e.ToString());
                }
                if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.AcceptCallback", "accepted client : " + clientSocket.RemoteEndPoint.ToString());

                clientManager.ClientSocket.BeginReceive(clientManager.Buffer,
                    0,
                    clientManager.discardingBuffer.Length,
                    SocketFlags.None,
                    new AsyncCallback(RecieveCallback),
                    clientManager);
            }
            catch (Exception e)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.AcceptCallback", "can not set async receive callback Error " + e.ToString());
                return;
            }

        }

        private static void UpdateContext(SendContext context, int dataSendOrReceived)
        {
            int remainingBytes = context.expectedSize - dataSendOrReceived;
            context.expectedSize = remainingBytes;
            context.offset += dataSendOrReceived;
        }


        internal void EnqueueEvent(Object eventObj, String slaveId)
        {
            {
                EventsAndCallbackQueue.Enqueue(eventObj, slaveId);
                if (SocketServer.IsServerCounterEnabled) PerfStatsColl.SetEventQueueCountStats(EventsAndCallbackQueue.Count);
            }
        }


        private void ReceiveDiscardLength(IAsyncResult result)
        {
            SendContext context = (SendContext)result.AsyncState;
            ClientManager clientManager = context.clientManager;

            int bytesRecieved = 0;
            try
            {
                if (clientManager.ClientSocket == null) return;
                bytesRecieved = clientManager.ClientSocket.EndReceive(result);

                clientManager.AddToClientsBytesRecieved(bytesRecieved);

                if (SocketServer.IsServerCounterEnabled) PerfStatsColl.IncrementBytesReceivedPerSecStats(bytesRecieved);

                if (bytesRecieved == 0)
                {
                    DisposeClient(clientManager);
                    return;
                }
                clientManager.MarkActivity();
                if (bytesRecieved > clientManager.discardingBuffer.Length)
                    if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionMgr.ReceiveCallback", " data read is more than the buffer");


                if (bytesRecieved < context.expectedSize)
                {
                    UpdateContext(context, bytesRecieved);
                    clientManager.ClientSocket.BeginReceive(context.buffer, context.offset, context.expectedSize, SocketFlags.None, new AsyncCallback(ReceiveDiscardLength), context);
                }

                context.buffer = clientManager.Buffer;
                if (bytesRecieved == context.expectedSize)
                {
                    context.offset = 0;
                    context.expectedSize = cmdSizeHolderBytesCount;
                    clientManager.ClientSocket.BeginReceive(context.buffer, context.offset, context.expectedSize, SocketFlags.None, new AsyncCallback(Receivelength), context);
                }
            }
            catch (SocketException so_ex)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + so_ex.ToString());

                DisposeClient(clientManager);
                return;
            }
            catch (Exception e)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + e.ToString());

                DisposeClient(clientManager);
                return;
            }
        }

        private void Receivelength(IAsyncResult result)
        {
            SendContext context = (SendContext)result.AsyncState;
            ClientManager clientManager = context.clientManager;
            int bytesRecieved = 0;

            try
            {
                if (clientManager.ClientSocket == null) return;
                bytesRecieved = clientManager.ClientSocket.EndReceive(result);

                clientManager.AddToClientsBytesRecieved(bytesRecieved);
                if (SocketServer.IsServerCounterEnabled) PerfStatsColl.IncrementBytesReceivedPerSecStats(bytesRecieved);

                if (bytesRecieved == 0)
                {
                    DisposeClient(clientManager);
                    return;
                }

                if (bytesRecieved < context.expectedSize)
                {
                    UpdateContext(context, bytesRecieved);
                    clientManager.ClientSocket.BeginReceive(context.buffer, context.offset, context.expectedSize, SocketFlags.None, new AsyncCallback(Receivelength), context);
                }

                if (bytesRecieved == context.expectedSize)
                {
                    long commandSize = 0;
                    CommandContext(clientManager, out commandSize);
                    int expectedCommandSize = (int)commandSize;
                    context.buffer = clientManager.GetPinnedBuffer((int)expectedCommandSize);
                    context.expectedSize = expectedCommandSize;
                    context.offset = 0;
                    context.totalExpectedSize = expectedCommandSize;

                    clientManager.ClientSocket.BeginReceive(context.buffer, context.offset, context.expectedSize, SocketFlags.None, new AsyncCallback(ReceiveCommmand), context);

                }
            }
            catch (SocketException so_ex)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + so_ex.ToString());

                DisposeClient(clientManager);
                return;

            }
            catch (Exception e)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + e.ToString());

                DisposeClient(clientManager);
                return;
            }
        }

        private void ReceiveCommmand(IAsyncResult result)
        {
            SendContext context = (SendContext)result.AsyncState;
            ClientManager clientManager = context.clientManager;
            Alachisoft.NCache.Common.Protobuf.Command command = null;

            int bytesRecieved = 0;
            try
            {
                if (clientManager.ClientSocket == null) return;
                bytesRecieved = clientManager.ClientSocket.EndReceive(result);

                clientManager.AddToClientsBytesRecieved(bytesRecieved);
                if (SocketServer.IsServerCounterEnabled) PerfStatsColl.IncrementBytesReceivedPerSecStats(bytesRecieved);

                if (bytesRecieved == 0)
                {
                    DisposeClient(clientManager);
                    return;
                }

                if (bytesRecieved < context.expectedSize)
                {
                    UpdateContext(context, bytesRecieved);
                    clientManager.ClientSocket.BeginReceive(context.buffer, context.offset, context.expectedSize, SocketFlags.None, new AsyncCallback(ReceiveCommmand), context);
                }

                if (bytesRecieved == context.expectedSize)
                {
                    byte[] buffer = context.buffer;
                    using (MemoryStream stream = new MemoryStream(buffer, 0, (int)context.totalExpectedSize))
                    {
                        command = ProtoBuf.Serializer.Deserialize<Alachisoft.NCache.Common.Protobuf.Command>(stream);
                        stream.Close();
                    }

                    clientManager.ReinitializeBuffer();

                    context = new SendContext();
                    context.clientManager = clientManager;
                    context.buffer = clientManager.Buffer;
                    context.expectedSize = clientManager.discardingBuffer.Length;

                    clientManager.ClientSocket.BeginReceive(context.buffer,
                        context.offset,
                        context.expectedSize,
                        SocketFlags.None,
                        new AsyncCallback(ReceiveDiscardLength),
                        context);

                    if (ServerMonitor.MonitorActivity)
                    {
                        ServerMonitor.RegisterClient(clientManager.ClientID, clientManager.ClientSocketId);
                        ServerMonitor.StartClientActivity(clientManager.ClientID);
                        ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "enter");
                    }
                    if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " COMMAND to be executed : " + command.type.ToString() + " RequestId :" + command.requestID);

                    clientManager.AddToClientsRequest(1);
                    if (SocketServer.IsServerCounterEnabled) PerfStatsColl.IncrementRequestsPerSecStats(1);

                    clientManager.StartCommandExecution();
                    cmdManager.ProcessCommand(clientManager, command);

                    if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " after executing COMMAND : " + command.type.ToString() + " RequestId :" + command.requestID);
                }
            }
            catch (SocketException so_ex)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + so_ex.ToString());

                DisposeClient(clientManager);
                return;

            }
            catch (Exception e)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + e.ToString());
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.ReceiveCallback", clientManager.ToString() + command + " Error " + e.ToString());

                DisposeClient(clientManager);
                return;
            }
            finally
            {
                clientManager.StopCommandExecution();
                if (ServerMonitor.MonitorActivity) ServerMonitor.StopClientActivity(clientManager.ClientID);
            }
        }

        private void CommandContext(ClientManager clientManager, out long tranSize)
        {
            int commandSize = HelperFxn.ToInt32(clientManager.Buffer, 0, cmdSizeHolderBytesCount, "Command");
            tranSize = commandSize;

        }

        private void RecieveCallback(IAsyncResult result)
        {
            ClientManager clientManager = (ClientManager)result.AsyncState;

            int bytesRecieved = 0;
            long transactionSize = 0;
            Object command = null;
            byte[] value = null;

            int discardingLength = 20;
            try
            {
                if (clientManager.ClientSocket == null) return;
                bytesRecieved = clientManager.ClientSocket.EndReceive(result);

                clientManager.AddToClientsBytesRecieved(bytesRecieved);
                if (SocketServer.IsServerCounterEnabled) PerfStatsColl.IncrementBytesReceivedPerSecStats(bytesRecieved);

                if (bytesRecieved == 0)
                {
                    DisposeClient(clientManager);
                    return;
                }
                clientManager.MarkActivity();
                if (bytesRecieved > discardingLength)
                    if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionMgr.ReceiveCallback", " data read is more than the buffer");


                if (bytesRecieved < discardingLength)
                {
                    byte[] interimBuffer = new byte[discardingLength - bytesRecieved];
                    AssureRecieve(clientManager, ref interimBuffer);
                    for (int i = 0; i < interimBuffer.Length; i++)
                        clientManager.discardingBuffer[bytesRecieved + i] = interimBuffer[i];

                    bytesRecieved += interimBuffer.Length;
                }

                AssureRecieve(clientManager, ref clientManager.Buffer, cmdSizeHolderBytesCount);
                bytesRecieved += cmdSizeHolderBytesCount;

                Command.CommandBase cmd = null;
                AssureRecieve(clientManager, out command, out value, out transactionSize);

                transactionSize += bytesRecieved;

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "cmd_size :" + transactionSize);

                clientManager.ReinitializeBuffer();

                clientManager.ClientSocket.BeginReceive(
                    clientManager.discardingBuffer,
                    0,
                    clientManager.discardingBuffer.Length,
                    SocketFlags.None,
                    new AsyncCallback(RecieveCallback),
                    clientManager);

                if (ServerMonitor.MonitorActivity)
                {
                    ServerMonitor.RegisterClient(clientManager.ClientID, clientManager.ClientSocketId);
                    ServerMonitor.StartClientActivity(clientManager.ClientID);
                    ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "enter");
                }

                clientManager.AddToClientsRequest(1);
                if (SocketServer.IsServerCounterEnabled) PerfStatsColl.IncrementRequestsPerSecStats(1);
                clientManager.StartCommandExecution();
                cmdManager.ProcessCommand(clientManager, command);
            }
            catch (SocketException so_ex)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + so_ex.ToString());

                DisposeClient(clientManager);
                return;

            }
            catch (Exception e)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + e.ToString());
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.ReceiveCallback", clientManager.ToString() + command + " Error " + e.ToString());

                DisposeClient(clientManager);

                return;
            }
            finally
            {
                clientManager.StopCommandExecution();
                if (ServerMonitor.MonitorActivity) ServerMonitor.StopClientActivity(clientManager.ClientID);
            }
        }

        internal void OnClientDisposed(string clientSocketId)
        {
            if (ConnectionTable != null && clientSocketId != null)
            {
                lock (ConnectionTable)
                {
                    ConnectionTable.Remove(clientSocketId);
                }
            }
        }

        internal static void AssureSend(ClientManager clientManager, byte[] buffer, Alachisoft.NCache.Common.Enum.Priority priority)
        {
            SendFragmentedResponse(clientManager, buffer, priority);
        }

        private static void AssureSend(ClientManager clientManager, byte[] buffer, Array userpayLoad, Alachisoft.NCache.Common.Enum.Priority priority)
        {
            SendContext context = new SendContext();
            context.clientManager = clientManager;
            context.buffer = buffer;
            context.expectedSize = buffer.Length;

            try
            {
                bool doOperation = true;
                if (clientManager != null)
                {
                    lock (clientManager)
                    {
                        doOperation = clientManager.MarkOperationInProcess();
                        if (!doOperation)
                        {
                            if (clientManager.IsDisposed) return;
                            clientManager.PendingSendOperationQueue.add(context, priority);

                            if (SocketServer.IsServerCounterEnabled) clientManager.ConnectionManager.PerfStatsColl.IncrementResponsesQueueCountStats();
                            if (SocketServer.IsServerCounterEnabled) clientManager.ConnectionManager.PerfStatsColl.IncrementResponsesQueueSizeStats(buffer.Length);

                            return;
                        }
                    }

                    AssureSend(clientManager, buffer, buffer.Length, context);
                }
            }
            catch (SocketException se)
            {
                DisposeClient(clientManager);
            }
            catch (ObjectDisposedException o)
            {
                return;
            }
        }

        internal void MonitorBadClients()
        {
            double sendTimeTaken;
            List<ClientManager> clients = new List<ClientManager>();

            while (true)
            {
                try
                {
                    clients.Clear();

                    lock (ConnectionManager.ConnectionTable)
                    {
                        ClientManager clientManager = null;
                        IDictionaryEnumerator ide = ConnectionManager.ConnectionTable.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            clientManager = (ClientManager)ide.Value;
                            clients.Add(clientManager);
                        }
                    }

                    foreach (ClientManager client in clients)
                    {
                        if (client != null)
                        {
                            sendTimeTaken = client.TimeTakenByOperation();

                            if (sendTimeTaken > _timeOutInterval)
                            {
                                if (client.OperationInProgress)
                                {
                                    ICommandExecuter cmdExecuter = client.CmdExecuter;
                                    if (cmdExecuter != null) cmdExecuter.OnClientForceFullyDisconnected(client.ClientID);
                                    client.ClientSocket.Disconnect(true);
                                }
                            }
                        }
                    }

                    Thread.Sleep(5000);
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                }
            }
        }

        private static void SendFragmentedResponse(ClientManager client, byte[] bMessage, Alachisoft.NCache.Common.Enum.Priority priority)
        {
            if (bMessage != null)
            {
                //client older then 4.1 sp2 private patch 2 does not support message fragmentation.
                if (bMessage.Length <= _messageFragmentSize  ||  client.ClientVersion < 4122)
                {
                    AssureSend(client, bMessage, null, priority);
                }
                else
                {
                    long messageId = NextFragmentedMessageID;
                    int messageLength = bMessage.Length - cmdSizeHolderBytesCount;

                    int noOfFragments = messageLength / _messageFragmentSize;
                    if (bMessage.Length % _messageFragmentSize > 0)
                        noOfFragments++;

                    int srcOffset = cmdSizeHolderBytesCount;
                    int remainingBytes = messageLength;

                    Alachisoft.NCache.Common.Protobuf.FragmentedResponse fragmentedResponse = new Alachisoft.NCache.Common.Protobuf.FragmentedResponse();

                    for (int i = 0; i < noOfFragments; i++)
                    {
                        int chunkSize = remainingBytes > _messageFragmentSize ? _messageFragmentSize : remainingBytes;

                        if (fragmentedResponse.message == null || fragmentedResponse.message.Length != chunkSize)
                            fragmentedResponse.message = new byte[chunkSize];

                        Buffer.BlockCopy(bMessage, srcOffset, fragmentedResponse.message, 0, chunkSize);

                        remainingBytes -= chunkSize;
                        srcOffset += chunkSize;
                        //fragmentedResponses.Add(fragmentedResponse);

                        Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                        response.requestId = -1;
                        fragmentedResponse.messageId = messageId;
                        fragmentedResponse.totalFragments = noOfFragments;
                        fragmentedResponse.fragmentNo = i;
                        response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.RESPONSE_FRAGMENT;
                        response.getResponseFragment = fragmentedResponse;

                        byte[] serializedReponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);

                        AssureSend(client, serializedReponse, null, priority);
                    }
                }
            }
        }

        private static void Send(ClientManager clientManager, byte[] buffer, int count)
        {
            int bytesSent = 0;
            lock (clientManager.SendMutex)
            {
                while (bytesSent < count)
                {
                    try
                    {
                        clientManager.ResetAsyncSendTime();

                        bytesSent += clientManager.ClientSocket.Send(buffer, bytesSent, count - bytesSent, SocketFlags.None);

                        clientManager.AddToClientsBytesSent(bytesSent);
                        if (SocketServer.IsServerCounterEnabled) clientManager.ConnectionManager.PerfStatsColl.IncrementBytesSentPerSecStats(bytesSent);
                    }
                    catch (SocketException e)
                    {

                        if (e.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                            continue;
                        else throw;
                    }
                }

                clientManager.ResetAsyncSendTime();
            }
        }

        private static void AssureSend(ClientManager clientManager, byte[] buffer, int count, SendContext context)
        {
            try
            {
                Send(clientManager, buffer, count);

                lock (clientManager)
                {
                    if (clientManager.PendingSendOperationQueue.Count == 0)
                    {
                        clientManager.DeMarkOperationInProcess();
                        return;
                    }
                }

#if !NET20
                try
                {
                    Task t = null;
                    //as there are times in the queue; let's start sending pending responses in separate thread.
                    TaskFactory factory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.LongRunning);
                    t = factory.StartNew(() => ProccessResponseQueue(clientManager), TaskCreationOptions.LongRunning);

                }
                catch (AggregateException aggEx)
                {
                }

#else
                ThreadPool.QueueUserWorkItem(new WaitCallback(ProccessResponseQueue), clientManager);
#endif

            }
            catch (Exception e)
            {
                if (SocketServer.Logger != null && SocketServer.Logger.IsErrorLogsEnabled)
                    SocketServer.Logger.NCacheLog.Error("ConnectionManager.AssureSend", e.ToString());

                DisposeClient(clientManager);
            }
        }

        private static void ProccessResponseQueue(object arg)
        {
            ClientManager clientManager = arg as ClientManager;
            try
            {
                
                SendContext opContext = null;
                do
                {
                    opContext = null;

                    lock (clientManager)
                    {
                        if (clientManager.PendingSendOperationQueue.Count > 0)
                        {
                            object operation = clientManager.PendingSendOperationQueue.remove();
                            opContext = (SendContext)operation;
                            clientManager.ResetAsyncSendTime();

                            if (SocketServer.IsServerCounterEnabled) clientManager.ConnectionManager.PerfStatsColl.DecrementResponsesQueueCountStats();
                            if (SocketServer.IsServerCounterEnabled) clientManager.ConnectionManager.PerfStatsColl.DecrementResponsesQueueSizeStats(opContext.expectedSize);
                        }
                    }

                    if (opContext != null)
                    {
                        Send(opContext.clientManager, opContext.buffer, opContext.expectedSize);
                    }

                    lock (clientManager)
                    {
                        if (clientManager.PendingSendOperationQueue.Count == 0)
                        {
                            clientManager.DeMarkOperationInProcess();
                            return;
                        }
                    }

                } while (clientManager.PendingSendOperationQueue.Count > 0);
            }
            catch (Exception e)
            {
                if (SocketServer.Logger != null && SocketServer.Logger.IsErrorLogsEnabled)
                    SocketServer.Logger.NCacheLog.Error("ConnectionManager.AssureSend", e.ToString());

                DisposeClient(clientManager);
            }
        }

        private static void AssureSendOld(ClientManager clientManager, byte[] buffer, int count, SendContext context)
        {
            TimeSpan tempTaken;

            try
            {
                Send(clientManager, buffer, count);
                SendContext opContext = null;

                do
                {
                    opContext = null;
                    lock (clientManager)
                    {
                        if (clientManager.PendingSendOperationQueue.Count > 0)
                        {
                            object operation = clientManager.PendingSendOperationQueue.remove();
                            opContext = (SendContext)operation;
                            clientManager.ResetAsyncSendTime();

                            if (SocketServer.IsServerCounterEnabled) clientManager.ConnectionManager.PerfStatsColl.DecrementResponsesQueueCountStats();
                            if (SocketServer.IsServerCounterEnabled) clientManager.ConnectionManager.PerfStatsColl.DecrementResponsesQueueSizeStats(opContext.expectedSize);
                        }
                    }

                    if (opContext != null)
                    {
                        Send(opContext.clientManager, opContext.buffer, opContext.expectedSize);
                    }

                    lock (clientManager)
                    {
                        if (clientManager.PendingSendOperationQueue.Count == 0)
                        {
                            clientManager.DeMarkOperationInProcess();
                            return;
                        }
                    }

                } while (clientManager.PendingSendOperationQueue.Count > 0);

            }
            catch (Exception e)
            {
                if (SocketServer.Logger != null && SocketServer.Logger.IsErrorLogsEnabled)
                    SocketServer.Logger.NCacheLog.Error("ConnectionManager.AssureSend", e.ToString());

                DisposeClient(clientManager);
            }
        }


        private void AssureRecieve(ClientManager clientManager, out Object command, out byte[] value, out long tranSize)
        {
            value = null;

            int commandSize = HelperFxn.ToInt32(clientManager.Buffer, 0, cmdSizeHolderBytesCount, "Command");
            tranSize = commandSize;
            using (Stream str = new ClusteredMemoryStream())
            {
                //byte[] buffer = new byte[commandSize + dataSize];
                while (commandSize >= 0)
                {
                    int apparentSize = commandSize < 81920 ? commandSize : 81920;
                    byte[] buffer = clientManager.GetPinnedBuffer((int)apparentSize);

                    AssureRecieve(clientManager, ref buffer, (int)apparentSize);
                    str.Write(buffer, 0, apparentSize);
                    commandSize -= 81920;
                }
                str.Position = 0;
                command = cmdManager.Deserialize(str);
            }
        }

        /// <summary>
        /// Receives data equal to the buffer length.
        /// </summary>
        /// <param name="clientManager"></param>
        /// <param name="buffer"></param>
        private static void AssureRecieve(ClientManager clientManager, ref byte[] buffer)
        {
            int bytesRecieved = 0;
            int totalBytesReceived = 0;
            do
            {
                try
                {
                    bytesRecieved = clientManager.ClientSocket.Receive(buffer, totalBytesReceived, buffer.Length - totalBytesReceived, SocketFlags.None);
                    totalBytesReceived += bytesRecieved;

                    clientManager.AddToClientsBytesRecieved(bytesRecieved);
                    if (SocketServer.IsServerCounterEnabled) clientManager.ConnectionManager.PerfStatsColl.IncrementBytesReceivedPerSecStats(bytesRecieved);
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.NoBufferSpaceAvailable) continue;
                    else throw;
                }
                catch (ObjectDisposedException)
                { }
            } while (totalBytesReceived < buffer.Length && bytesRecieved > 0);
        }

        /// <summary>
        /// The Receives data less than the buffer size. i.e buffer length and size may not be equal. (used to avoid pinning of unnecessary byte buffers.)
        /// </summary>
        /// <param name="clientManager"></param>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        private static void AssureRecieve(ClientManager clientManager, ref byte[] buffer, int size)
        {
            int bytesRecieved = 0;
            int totalBytesReceived = 0;

            do
            {
                try
                {
                    bytesRecieved = clientManager.ClientSocket.Receive(buffer, totalBytesReceived, size - totalBytesReceived, SocketFlags.None);
                    totalBytesReceived += bytesRecieved;

                    clientManager.AddToClientsBytesRecieved(bytesRecieved);
                    if (SocketServer.IsServerCounterEnabled) clientManager.ConnectionManager.PerfStatsColl.IncrementBytesReceivedPerSecStats(bytesRecieved);
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.NoBufferSpaceAvailable) continue;
                    else throw;
                }
                catch (ObjectDisposedException)
                { }
            } while (totalBytesReceived < size && bytesRecieved > 0);
        }



        private void SendBulkClientEvents()
        {
            Alachisoft.NCache.Common.Protobuf.Response response = null;
            QueuedItem item = null;
            string clienId = null;
            ClientManager clientManager = null;

            while (true)
            {
                try
                {
                    item = (QueuedItem)_eventsAndCallbackQueue.Dequeue();

                    if (item != null)
                    {
                        

                        if (SocketServer.IsServerCounterEnabled && PerfStatsColl != null)
                            PerfStatsColl.SetEventQueueCountStats(_eventsAndCallbackQueue.Count);

                        response = (Alachisoft.NCache.Common.Protobuf.Response)item.Item;
                        clienId = item.RegisteredClientId;

                        if (response != null)
                        {
                            if (clienId != null)
                            {
                                lock (ConnectionManager.ConnectionTable)
                                {
                                    clientManager = (ClientManager)ConnectionManager.ConnectionTable[clienId];
                                }

                                if (clientManager != null && clientManager.ClientVersion >= 4124)
                                {
                                    try
                                    {
                                        byte[] serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);
                                        ConnectionManager.AssureSend(clientManager, serializedResponse, Alachisoft.NCache.Common.Enum.Priority.Low);
                                    }
                                    catch (SocketException se)
                                    {
                                        clientManager.Dispose();
                                    }
                                    catch (System.Exception ex)
                                    {
                                        if (SocketServer.IsServerCounterEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.EventThread", ex.ToString());
                                    }
                                }
                            }
                            else
                            {
                                if (SocketServer.IsServerCounterEnabled) SocketServer.Logger.NCacheLog.CriticalInfo("ConnectionManager.EventThread client information not found ");
                            }

                        }
                    }

                }
                catch (ThreadAbortException ta)
                {
                    break;
                }
                catch (ThreadInterruptedException ti)
                {
                    break;
                }
                catch (Exception e)
                {
                }
            }
        }


        private void SendBulkEvents()
        {
            Alachisoft.NCache.Common.Protobuf.Response response;
            List<ClientManager> clients = new List<ClientManager>();

            while (true)
            {
                try
                {
                    clients.Clear();
                    bool takeBreak = true;
                    lock (ConnectionManager.ConnectionTable)
                    {
                        ClientManager clientManager = null;
                        IDictionaryEnumerator ide = ConnectionManager.ConnectionTable.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            clientManager = (ClientManager)ide.Value;
                            if (clientManager.ClientVersion >= 4124)
                            {
                                clients.Add(clientManager);
                            }
                        }
                    }

                    foreach (ClientManager client in clients)
                    {
                        if (client != null)
                        {
                            try
                            {
                                bool hasMessages = false;
                                response = client.GetEvents(out hasMessages);

                                if (hasMessages && takeBreak)
                                {
                                    takeBreak = false;
                                }

                                if (response != null)
                                {
                                    byte[] serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);
                                    ConnectionManager.AssureSend(client, serializedResponse, Alachisoft.NCache.Common.Enum.Priority.Low);
                                }
                            }
                            catch (SocketException se)
                            {
                                client.Dispose();
                            }
                            catch (InvalidOperationException io)
                            {
                                //thrown when iterator is modified
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (SocketServer.IsServerCounterEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.EventThread", ex.ToString());
                            }

                        }
                    }
                    Thread.Sleep(500);
                }
                catch (ThreadAbortException ta)
                {
                    break;
                }
                catch (ThreadInterruptedException ti)
                {
                    break;
                }
                catch (Exception e)
                {
                    if (SocketServer.IsServerCounterEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.EventThread", e.ToString());
                }
            }
        }

        private void CallbackThread()
        {
            object returnVal = null;

            try
            {
                while (true)
                {
                    while (CallbackQueue.Count > 0)
                    {
                        lock (CallbackQueue) returnVal = CallbackQueue.Dequeue();

                        try
                        {
                            if (returnVal is ICallbackTask) ((ICallbackTask)returnVal).Process();
                            else if (returnVal is IEventTask) ((IEventTask)returnVal).Process();
                        }
                        catch (SocketException) { break; }
                        catch (Exception) { }
                    }

                    lock (CallbackQueue) { Monitor.Wait(CallbackQueue); }
                }
            }
            catch (Exception) { }
        }

        public void StopListening()
        {
            if (_serverSocket != null)
            {
                _serverSocket.Close();
                _serverSocket = null;
            }
        }

        private void DisposeServer()
        {
            StopListening();
            if (_badClientMonitoringThread != null)
                _badClientMonitoringThread.Abort();

            if (_eventsThread != null)
                _eventsThread.Abort();

            if (_callbacksThread != null)
            {
                if (_callbacksThread.ThreadState != ThreadState.Aborted && _callbacksThread.ThreadState != ThreadState.AbortRequested)
                    _callbacksThread.Abort();
            }

            if (ConnectionTable != null)
            {
                lock (ConnectionTable)
                {
                    Hashtable cloneTable = ConnectionTable.Clone() as Hashtable;
                    IDictionaryEnumerator tableEnu = cloneTable.GetEnumerator();
                    while (tableEnu.MoveNext()) ((ClientManager)tableEnu.Value).Dispose();
                }
                ConnectionTable = null;
            }
        }

        /// <summary>
        /// Set client logging info
        /// </summary>
        /// <param name="type"></param>
        /// <param name="status"></param>
        public static bool SetClientLoggingInfo(LoggingInfo.LoggingType type, LoggingInfo.LogsStatus status)
        {
            lock (_clientLogginInfo)
            {
                if (_clientLogginInfo.GetStatus(type) != status)
                {
                    _clientLogginInfo.SetStatus(type, status);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Get client logging information
        /// </summary>
        public static LoggingInfo.LogsStatus GetClientLoggingInfo(LoggingInfo.LoggingType type)
        {
            lock (_clientLogginInfo)
            {
                return _clientLogginInfo.GetStatus(type);
            }
        }
        public static void UpdateClients()
        {
            bool errorOnly = false;
            bool detailed = false;

            lock (_clientLogginInfo)
            {
                errorOnly = GetClientLoggingInfo(LoggingInfo.LoggingType.Error) == LoggingInfo.LogsStatus.Enable;
                detailed = GetClientLoggingInfo(LoggingInfo.LoggingType.Detailed) == LoggingInfo.LogsStatus.Enable;
            }

            UpdateClients(errorOnly, detailed);

        }

        /// <summary>
        /// Update logging info on all connected clients
        /// </summary>
        private static void UpdateClients(bool errorOnly, bool detailed)
        {
            ICollection clients = null;
            lock (ConnectionTable)
            {
                clients = ConnectionTable.Values;
            }

            try
            {
                foreach (object obj in clients)
                {
                    ClientManager client = obj as ClientManager;
                    if (client != null)
                    {
                        NCache executor = client.CmdExecuter as NCache;
                        if (executor != null)
                        {
                            executor.OnLoggingInfoModified(errorOnly, detailed, client.ClientID);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                throw;
            }
        }

        /// <summary>
        /// Get the ip address of server
        /// </summary>
        internal static string ServerIpAddress
        {
            get { return _serverIpAddress; }
        }

        /// <summary>
        /// Get the port at which server is running
        /// </summary>
        internal static int ServerPort
        {
            get { return _serverPort; }
        }
    }
}
