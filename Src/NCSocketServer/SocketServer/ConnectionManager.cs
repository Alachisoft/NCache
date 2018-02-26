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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Web.Util;
using ProtoCommand = Alachisoft.NCache.Common.Protobuf.Command;
using ICallbackTask = Alachisoft.NCache.SocketServer.CallbackTasks.ICallbackTask;
using IEventTask = Alachisoft.NCache.SocketServer.EventTask.IEventTask;
using HelperFxn = Alachisoft.NCache.SocketServer.Util.HelperFxn;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using System.IO;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Alachisoft.NCache.SocketServer.Statistics;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.SocketServer.Util;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Threading.Tasks;


namespace Alachisoft.NCache.SocketServer
{
    /// <summary>
    /// This class is responsible for maintaining all clients connection with server.
    /// </summary>
    public sealed class ConnectionManager : IConnectionManager, IRequestProcessor
    {
        #region ------------- Constants ---------------

        /// <summary> Number of maximum pending connections</summary>
        private int _maxClient = 300;

        internal const int header = 8;

        /// <summary> Number of bytes that will hold the incoming command size</summary>
        internal const int cmdSizeHolderBytesCount = 10;

        /// <summary> Number of bytes that will hold the incoming data size</summary>
        internal const int valSizeHolderBytesCount = 10;

        /// <summary> Total number of bytes that will hold both command and data size</summary>
        internal const int totSizeHolderBytesCount = cmdSizeHolderBytesCount + valSizeHolderBytesCount;

        /// <summary> Total buffer size used as pinned buffer for asynchronous socket io.</summary>
        internal const int pinnedBufferSize = 200 * 1024;

        /// <summary> Total number of milliseconds after which we check for idle clients and send heart beat to them.</summary>
        internal const int waitIntervalHeartBeat = 30000;

#if NET40
        private static TaskFactory factory = new TaskFactory(TaskCreationOptions.None, TaskContinuationOptions.None);
#endif
        #endregion

        /// <summary> Underlying server socket object</summary>
        private Socket _serverSocket = null;

        /// <summary> Send buffer size of connected client socket</summary>
        private int _clientSendBufferSize = 0;

        /// <summary> Receive buffer size of connected client socket</summary>
        private int _clientReceiveBufferSize = 0;

        /// <summary> Command manager object to process incoming commands</summary>
        private ICommandManager _cmdManager;

        /// <summary> Stores the client connections</summary>
        internal static Hashtable ConnectionTable = new Hashtable(10);

        /// <summary> Thread to send callback and notification responses to client</summary>
        private Thread _callbacksThread = null;

        /// <summary> Reade writer lock instance used to synchronize access to socket</summary>
        private static ReaderWriterLock _readerWriterLock = new ReaderWriterLock();

        /// <summary> Hold server ip address</summary>
        private static string _serverIpAddress = string.Empty;

        /// <summary> Holds server port</summary>
        private static int _serverPort = -1;

        private Thread _eventsThread = null;

        private Logs _logger;

        private static readonly LoggingInfo ClientLogginInfo = new LoggingInfo();

        private static readonly Queue CallBackQueue = new Queue(256);

        private static long _fragmentedMessageId;

        private static int _timeOutInterval = int.MaxValue; // By default this interval is set to Max value i.e. infinity to turn off bad client detection

        private ConnectionManagerType _type = ConnectionManagerType.Management;

        private readonly DistributedQueue _eventsAndCallbackQueue;

        private readonly PerfStatsCollector _perfStatsCollector;

        private static Thread _badClientMonitoringThread;

        private static bool _isClientCommand = false;

        private readonly CommandProcessorPool _processorPool;

        private static bool _enableBadClientDetection = false;

        // Either Received callback  is continued or blocked based on command coming from client
        private bool _isCommandReceivedonService = false;

        private const bool UseCommandProcessorPool = false;

        private AsyncCallback _receiveCallback;

        public ConnectionManager(PerfStatsCollector statsCollector)
        {
            _receiveCallback = new AsyncCallback(RecieveCallback);

            int threadsPerProcessor = ServiceConfiguration.ThreadsPerProcessor;

            if (threadsPerProcessor > 0)
            {
                ThreadPool.SetMaxThreads(Environment.ProcessorCount * 2 * 2 * threadsPerProcessor, Environment.ProcessorCount * threadsPerProcessor);
            }
            _eventsAndCallbackQueue = new DistributedQueue(statsCollector);
            _perfStatsCollector = statsCollector;

            if (UseCommandProcessorPool)
            {
                _processorPool = new CommandProcessorPool(Environment.ProcessorCount, this, statsCollector);
                _processorPool.Start();
            }
        }

        internal static Queue CallbackQueue
        {
            get { return CallBackQueue; }
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

        private static readonly object _client_hearbeat_mutex = new object();

        internal static object client_hearbeat_mutex
        {
            get
            {
                return _client_hearbeat_mutex;
            }
        }

        private ICommandManager GetCommandManager(CommandManagerType cmdMgrType)
        {
            switch (cmdMgrType)
            {
                case CommandManagerType.NCacheClient:
                    return new CommandManager(_perfStatsCollector);

                case CommandManagerType.NCacheManagement:
                case CommandManagerType.NCacheHostManagement:
                    return new ManagementCommandManager();

                case CommandManagerType.NCacheService:
                    return new ServiceCommandManager(_perfStatsCollector);

                default:
                    return new CommandManager(_perfStatsCollector);
            }
        }

        private static int _messageFragmentSize = 80 * 1024;
        private CommandManagerType _cmdManagerType;

        /// <summary>
        /// Start the socket server and start listening for clients
        /// <param name="port">port at which the server will be listening</param>
        /// </summary>
        public void Start(IPAddress bindIP, int port, int sendBuffer, int receiveBuffer, Logs logger, CommandManagerType cmdMgrType, ConnectionManagerType conMgrType)
        {
            _type = conMgrType;
            _cmdManagerType = cmdMgrType;
            _logger = logger;
            _clientSendBufferSize = sendBuffer;
            _clientReceiveBufferSize = receiveBuffer;
            _cmdManager = GetCommandManager(cmdMgrType);

            string maxPendingConnections = "NCache.MaxPendingConnections";
            string enableServerCounters = "NCache.EnableServerCounters";

            _enableBadClientDetection = ServiceConfiguration.EnableBadClientDetection;

            if (_enableBadClientDetection)
            {
                _timeOutInterval = ServiceConfiguration.ClientSocketSendTimeout;
            }

            if (_type == ConnectionManagerType.ServiceClient && ServiceConfiguration.ServiceGCInterval > 0)
            {
                _gcThread = new Thread(new ThreadStart(CollectGarbage));
                _gcThread.IsBackground = true;
                _gcThread.Start();
            }

            try
            {
                _maxClient = ServiceConfiguration.MaxPendingConnections;
            }
            catch (Exception e)
            {
                throw new Exception("Invalid value specified for " + maxPendingConnections + ".");
            }

            try
            {
                SocketServer.IsServerCounterEnabled = ServiceConfiguration.EnableServerCounters;
            }
            catch (Exception e)
            {
                throw new Exception("Invalid value specified for " + enableServerCounters + ".");
            }

            try
            {
                int messageLength = ServiceConfiguration.MaxResponseLength;

                if (messageLength > 1)
                    _messageFragmentSize = messageLength * 1024;
            }
            catch (Exception e)
            {
                throw new Exception("Invalid value specified for NCacheServer.MaxResponseLength.");
            }
            if (ConnectionManagerType.Management == conMgrType || conMgrType == ConnectionManagerType.ServiceClient)
            {
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
                    if (bindIP != null && cmdMgrType != CommandManagerType.NCacheHostManagement)
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
            }
            _serverIpAddress = bindIP.ToString();
            if (cmdMgrType == CommandManagerType.NCacheClient || cmdMgrType == CommandManagerType.NCacheService)
            {
                _serverPort = port;
            }
            _callbacksThread = new Thread(new ThreadStart(this.CallbackThread));
            _callbacksThread.Priority = ThreadPriority.BelowNormal;
            _callbacksThread.IsBackground = true;
            _callbacksThread.Start();

            _eventsThread = new Thread(new ThreadStart(this.SendBulkClientEvents));
            _eventsThread.Name = "ConnectionManager.BulkEventThread";
            _eventsThread.IsBackground = true;
            _eventsThread.Start();
        }

        /// <summary>
        /// Dispose socket server and all the allocated resources
        /// </summary>
        public void Stop()
        {
            DisposeServer();

            if (_processorPool != null)
            {
                _processorPool.Stop();
            }

            if (_gcThread != null && _gcThread.IsAlive)
#if !NETCORE
                _gcThread.Abort();
#else
                _gcThread.Interrupt();
#endif
        }

        /// <summary>
        /// Due to lack of activity in Cache Service, GC kicks in very late causing a lot of memory to be used by service.
        /// A dedicated thread calls GC after every 10 minutes to reduce service memory foot print.
        /// </summary>
        private void CollectGarbage()
        {
            while (true)
            {
                try
                {
                    if (ServiceConfiguration.ServiceGCInterval <= 0) break;

                    Thread.Sleep(new TimeSpan(0, ServiceConfiguration.ServiceGCInterval, 0));

                    GC.Collect(2, GCCollectionMode.Forced);
                    GC.WaitForPendingFinalizers();
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Dispose the client
        /// </summary>
        /// <param name="clientManager">Client manager object representing the client to be disposed</param>
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
                clientSocket.UseOnlyOverlappedIO = true;
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

            // Different network options depends on the underlying OS, which may support them or not
            // therefore we should handle error if they are not supported.
            ClientManager clientManager = new ClientManager(this, clientSocket, totSizeHolderBytesCount, pinnedBufferSize);
            clientManager.ClientDisposed += OnClientDisposed;

            RecieveClientConnection(clientManager);
        }

        internal void EnqueueEvent(Object eventObj, String slaveId)
        {
            EventsAndCallbackQueue.Enqueue(eventObj, slaveId);
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

        private long GetAcknowledgementId(ClientManager clientManager)
        {
            long acknowledgementId = -1;

            if (clientManager.SupportAcknowledgement)
            {
                byte[] acknowledgementBuffer = new byte[20];
                AssureRecieve(clientManager, ref acknowledgementBuffer, acknowledgementBuffer.Length);
                acknowledgementId = HelperFxn.ToInt64(acknowledgementBuffer, 0, acknowledgementBuffer.Length);
            }

            return acknowledgementId;
        }

        internal static void AssureSend(ClientManager clientManager, IList buffer, Alachisoft.NCache.Common.Enum.Priority priority)
        {
            AssureSend(clientManager, buffer, null, priority);
        }

        private static void AssureSend(ClientManager clientManager, IList buffer, Array userpayLoad, Alachisoft.NCache.Common.Enum.Priority priority)
        {
            try
            {
                bool send = false;
                lock (clientManager.SendMutex)
                {
                    if (!clientManager.SendingResponse)
                    {
                        clientManager.SendingResponse = send = true;
                        if (clientManager.IsDisposed)
                        {
                            return;
                        }
                    }
                    else
                    {
                        clientManager.AddPendingQueueOperation(buffer, priority);
                    }
                }

                if (send)
                {
                    AssureSend(clientManager, buffer, 0);
                    return;
                }
            }
            catch (SocketException se)
            {
                DisposeClient(clientManager);
            }
            catch (ObjectDisposedException o)
            {
            }
            catch
            {
            }
        }

        internal static void AssureSendSync(ClientManager clientManager, IList bufferList)
        {
            int bytesSent = 0;
            lock (clientManager.SendMutex)
            {
                foreach (byte[] buffer in bufferList)
                {
                    while (bytesSent < buffer.Length)
                    {
                        try
                        {
                            clientManager.ResetAsyncSendTime();
                            bytesSent += clientManager.ClientSocket.Send(buffer, bytesSent, buffer.Length - bytesSent, SocketFlags.None);
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
                }
                clientManager.ResetAsyncSendTime();
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

                            if (ServiceConfiguration.EnableBadClientDetection)
                                _timeOutInterval = ServiceConfiguration.ClientSocketSendTimeout;

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
                // Client older then 4.1 sp2 private patch 2 does not support message fragmentation.
                if (bMessage.Length <= _messageFragmentSize || !client.IsDotNetClient || client.ClientVersion < 4122)
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

                        Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                        response.requestId = -1;
                        fragmentedResponse.messageId = messageId;
                        fragmentedResponse.totalFragments = noOfFragments;
                        fragmentedResponse.fragmentNo = i;
                        response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.RESPONSE_FRAGMENT;
                        response.getResponseFragment = fragmentedResponse;

                        IList serializedReponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);

                        AssureSend(client, serializedReponse, null, priority);
                    }
                }
            }
        }

        private static void AssureSend(ClientManager clientManager, IList buffer, int count)
        {
            try
            {
                ArraySegment<byte>[] segments;
                clientManager.SendingResponse = true;

                if (clientManager.IsOptimized)
                {
                    using (ClusteredMemoryStream stream = new ClusteredMemoryStream())
                    {
                        byte[] dataSzBytes = new byte[MessageSizeHeader];
                        stream.Write(dataSzBytes, 0, MessageSizeHeader);

                        int len = 0;

                        foreach (byte[] buffBytes in buffer)
                        {
                            stream.Write(buffBytes, 0, buffBytes.Length);
                            len += buffBytes.Length;
                        }

                        byte[] lengthBytes = HelperFxn.ToBytes(len.ToString());

                        stream.Seek(0, SeekOrigin.Begin);
                        stream.Write(lengthBytes, 0, lengthBytes.Length);

                        segments = SocketHelper.GetArraySegments<byte>(stream.GetInternalBuffer());
                    }
                }
                else
                {
                    segments = SocketHelper.GetArraySegments<byte>(buffer);
                }

                BeginAsyncSend(clientManager, segments, 0, 0, segments[0].Array.Length);
                clientManager.ResetAsyncSendTime();

            }
            catch (Exception e)
            {
                if (SocketServer.Logger != null && SocketServer.Logger.IsErrorLogsEnabled)
                    SocketServer.Logger.NCacheLog.Error("ConnectionManager.AssureSend", e.ToString());

                DisposeClient(clientManager);
            }
        }

        #region Aync response to client stuff

        #region Constants

        //Message size bytes.
        internal readonly static int MessageSizeHeader = 10;

        //Discarding buffer.
        internal readonly static int DiscardBufLen = 20;

        //Discarding buffer.
        internal readonly static int AckIdBufLen = 20;

        //Threshold for maximum number of commands in a request.
        internal static readonly int MaxRspThreshold = 100;// int.MaxValue;
        private Thread _gcThread;

        #endregion

        private static void BeginAsyncSend(ClientManager clientManager, ArraySegment<byte>[] buffers, int buffIndex, int offset, int count)
        {
            if (clientManager.SendEventArgs == null)
            {
                clientManager.SendEventArgs = new SocketAsyncEventArgs();
                clientManager.SendEventArgs.UserToken = new SendContextServer();
                ((SendContextServer)clientManager.SendEventArgs.UserToken).Client = clientManager;
                clientManager.SendEventArgs.Completed += OnCompleteAsyncSend;
            }

            SendContextServer sendStruct = (SendContextServer)clientManager.SendEventArgs.UserToken;
            sendStruct.CurrbuffIndex = buffIndex;
            sendStruct.DataToSend = count;
            sendStruct.Buffers = buffers;
            if (!clientManager.ClientSocket.Connected) throw new Exception("socket closed");
            clientManager.SendEventArgs.SetBuffer(buffers[buffIndex].Array, offset, count);

            if (!clientManager.ClientSocket.SendAsync(clientManager.SendEventArgs))
            {
                SendDataAsync(clientManager.SendEventArgs);
            }
        }

        private static void OnCompleteAsyncSend(object obj, SocketAsyncEventArgs args)
        {
            SendDataAsync(args);
        }

        private static void SendDataAsync(object obj)
        {
            SocketAsyncEventArgs args = (SocketAsyncEventArgs)obj;
            SendContextServer sendStruct = (SendContextServer)args.UserToken;
            ClientManager clientManager = sendStruct.Client;

            try
            {
                if (clientManager.ClientSocket == null)
                {
                    return;
                }

                int bytesSent = args.BytesTransferred;

                clientManager.AddToClientsBytesSent(bytesSent);

                if (bytesSent == 0)
                {
                    DisposeClient(clientManager);
                    return;
                }

                if (args.SocketError != SocketError.Success)
                {
                    DisposeClient(clientManager);
                    return;
                }

                if (bytesSent < sendStruct.DataToSend)
                {
                    int newDataToSend = sendStruct.DataToSend - bytesSent;
                    int newOffset = sendStruct.Buffers[sendStruct.CurrbuffIndex].Array.Length - newDataToSend;
                    BeginAsyncSend(clientManager, sendStruct.Buffers, sendStruct.CurrbuffIndex, newOffset, newDataToSend);
                    return;
                }

                if (sendStruct.CurrbuffIndex < sendStruct.Buffers.Length - 1)
                {
                    sendStruct.CurrbuffIndex++;
                    int newDataToSend = sendStruct.Buffers[sendStruct.CurrbuffIndex].Array.Length;
                    BeginAsyncSend(clientManager, sendStruct.Buffers, sendStruct.CurrbuffIndex, 0, newDataToSend);
                    return;
                }

                ClusteredArrayList response;

                lock (clientManager.SendMutex)
                {
                    if (!clientManager.AreResponsesPending)
                    {
                        clientManager.SendingResponse = false;
                        return;
                    }
                }

                response = clientManager.CompositeResponse;
                clientManager.ResetAsyncSendTime();

                if (response != null)
                {
                    ArraySegment<byte>[] segments = SocketHelper.GetArraySegments<byte>(response);
                    BeginAsyncSend(clientManager, segments, 0, 0, segments[0].Array.Length);
                }
            }
            catch (SocketException ex)
            {
                if (ServerMonitor.MonitorActivity)
                {
                    ServerMonitor.LogClientActivity("ClntMgr.SendClbk", "Error :" + ex);
                }

                DisposeClient(clientManager);
            }
            catch (Exception ex)
            {
                if (ServerMonitor.MonitorActivity)
                {
                    ServerMonitor.LogClientActivity("ClntMgr.SendClbk", "Error :" + ex);
                }
                if (SocketServer.Logger.IsErrorLogsEnabled)
                {
                    SocketServer.Logger.NCacheLog.Error("ClientManager.SendCallback", "Error :" + ex);
                }

                DisposeClient(clientManager);
            }
        }

        #endregion

        private void AssureRecieve(ClientManager clientManager, out Object command, out byte[] value, out long tranSize)
        {
            value = null;
            int commandSize = HelperFxn.ToInt32(clientManager.Buffer, 0, cmdSizeHolderBytesCount, "Command");
            tranSize = commandSize;
            using (Stream str = new ClusteredMemoryStream())
            {
                while (commandSize >= 0)
                {
                    int apparentSize = commandSize < 81920 ? commandSize : 81920;
                    byte[] buffer = clientManager.GetPinnedBuffer((int)apparentSize);

                    AssureRecieve(clientManager, ref buffer, (int)apparentSize);
                    str.Write(buffer, 0, apparentSize);
                    commandSize -= 81920;
                }
                str.Position = 0;

                command = _cmdManager.Deserialize(str);
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
                catch (ObjectDisposedException) { }
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
                catch (ObjectDisposedException) { }
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
                                        IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);
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
                                    IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);
                                    ConnectionManager.AssureSend(client, serializedResponse, Alachisoft.NCache.Common.Enum.Priority.Low);
                                }
                            }
                            catch (SocketException se)
                            {
                                client.Dispose();
                            }
                            catch (InvalidOperationException io)
                            {
                                // Thrown when iterator is modified
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
                    try
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
            catch (Exception e)
            {
            }
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
#if !NETCORE
                _badClientMonitoringThread.Abort();
#else
                _badClientMonitoringThread.Interrupt();
#endif

            if (_eventsThread != null)
#if !NETCORE
                _eventsThread.Abort();
#else
                _eventsThread.Interrupt();
#endif

            if (_callbacksThread != null)
            {
#if !NETCORE
                _callbacksThread.Abort();
#else
                _callbacksThread.Interrupt();
#endif
            }
            if (ConnectionTable != null)
            {
                lock (ConnectionTable)
                {
                    Hashtable cloneTable = ConnectionTable.Clone() as Hashtable;
                    if (cloneTable != null)
                    {
                        IDictionaryEnumerator tableEnu = cloneTable.GetEnumerator();
                        while (tableEnu.MoveNext()) ((ClientManager)tableEnu.Value).Dispose();
                    }
                }
                ConnectionTable = null;
            }
            if (_cmdManager != null && _cmdManager.RequestLogger != null) _cmdManager.RequestLogger.Dispose();
        }

        /// <summary>
        /// Set client logging info
        /// </summary>
        /// <param name="type"></param>
        /// <param name="status"></param>
        public static bool SetClientLoggingInfo(LoggingInfo.LoggingType type, LoggingInfo.LogsStatus status)
        {
            lock (ClientLogginInfo)
            {
                if (ClientLogginInfo.GetStatus(type) != status)
                {
                    ClientLogginInfo.SetStatus(type, status);
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
            lock (ClientLogginInfo)
            {
                return ClientLogginInfo.GetStatus(type);
            }
        }

        public static void UpdateClients()
        {
            bool errorOnly = false;
            bool detailed = false;

            lock (ClientLogginInfo)
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

        internal Alachisoft.NCache.Common.DataStructures.RequestStatus GetRequestStatus(string clientId, long requestId, long commandId)
        {
            return _cmdManager.GetRequestStatus(clientId, requestId, commandId);
        }

        /// <summary>
        /// ////////////////////////// changing here for overload of OnclientConnected to be called from somewhere :D
        /// </summary>
        /// <param name="socketInformation"></param>
        /// <param name="transferCommand"></param>
        public void OnClientConnected(ClientManager clientManager, byte[] transferCommand)
        {
            clientManager.ClientDisposed += OnClientDisposed;
            try
            {
                TransferableCommandThread(clientManager, transferCommand);

                RecieveClientConnection(clientManager);
            }
            catch (Exception ex)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled)
                    SocketServer.Logger.NCacheLog.Error(
                        "ConnectionManager.OnClientConnected",
                        "Problem occured processing newly connected client, error: " + ex);

                clientManager.RaiseClientDisconnectEvent = false;
                clientManager.Dispose();
            }
        }

        public void OnClientConnected(SocketInformation socketInformation, byte[] transferCommand)
        {
            var duplicateSocket = new Socket(socketInformation);
            var clientManager = new ClientManager(this, duplicateSocket, totSizeHolderBytesCount, pinnedBufferSize);
            OnClientConnected(clientManager, transferCommand);
        }

        private void RecieveClientConnection(ClientManager clientManager)
        {
            try
            {
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

                if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.AcceptCallback", "accepted client : " + clientManager.ClientSocket.RemoteEndPoint.ToString());
#if !NETCORE
                if (_type != ConnectionManagerType.ServiceClient && clientManager.ClientSocket.UseOnlyOverlappedIO)
#else
                if(_type != ConnectionManagerType.ServiceClient)
#endif
                {
                    SocketAsyncEventArgs eventArg = new SocketAsyncEventArgs();
                    RecContextServer reciveStruct = new RecContextServer();
                    reciveStruct.State = clientManager.IsOptimized ? ReceivingState.ReceivingDataLength : ReceivingState.ReceivingDiscBuff;
                    reciveStruct.Client = clientManager;
                    reciveStruct.StreamBuffer = new ClusteredMemoryStream(10);
                    reciveStruct.RequestId = 1;

                    eventArg.RemoteEndPoint = clientManager.ClientSocket.RemoteEndPoint;
                    eventArg.UserToken = reciveStruct;
                    eventArg.Completed += OnCompleteAsyncReceive;
                    byte[] dataBuffer = clientManager.IsOptimized ? new byte[10] : new byte[20];
                    clientManager.ReceiveEventArgs = eventArg;
                    BeginAsyncReceive(eventArg, dataBuffer.Length, dataBuffer, 0, dataBuffer.Length, false);
                }
                else
                {
                    clientManager.ClientSocket.BeginReceive(clientManager.Buffer, 0, clientManager.discardingBuffer.Length, SocketFlags.None, _receiveCallback, clientManager);
                }
            }
            catch (Exception e)
            {
                AppUtil.LogEvent(e.ToString() + " process id : " + System.Diagnostics.Process.GetCurrentProcess().Id, System.Diagnostics.EventLogEntryType.Error);
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.AcceptCallback", "can not set async receive callback Error " + e.ToString());
            }
        }

        void OnCompleteAsyncReceive(object sender, SocketAsyncEventArgs e)
        {
            ReceiveCommandAndDataAsync(e);
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
                UsageStats stats = new UsageStats();
                stats.BeginSample();

                if (clientManager.ClientSocket == null) return;
                bytesRecieved = clientManager.ClientSocket.EndReceive(result);

                clientManager.AddToClientsBytesRecieved(bytesRecieved);

                if (SocketServer.IsServerCounterEnabled) PerfStatsColl.IncrementBytesReceivedPerSecStats(bytesRecieved);

                if (bytesRecieved == 0)
                {
                    DisposeClient(clientManager);
                    return;
                }
                long acknowledgementId = 0;

                command = ReceiveCommandSynchronously(clientManager, bytesRecieved, out acknowledgementId);

                Alachisoft.NCache.Common.Protobuf.Command cmd = command as Alachisoft.NCache.Common.Protobuf.Command;

                if (RegisterCallBack(cmd))
                {
                    clientManager.ClientSocket.BeginReceive(
                        clientManager.discardingBuffer,
                        0,
                        clientManager.discardingBuffer.Length,
                        SocketFlags.None,
                        _receiveCallback,
                        clientManager);

                    if (ServerMonitor.MonitorActivity)
                    {
                        ServerMonitor.RegisterClient(clientManager.ClientID, clientManager.ClientSocketId);
                        ServerMonitor.StartClientActivity(clientManager.ClientID);
                        ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "enter");
                    }
                }

                clientManager.AddToClientsRequest(1);

                if (SocketServer.IsServerCounterEnabled) PerfStatsColl.IncrementRequestsPerSecStats(1);

                clientManager.StartCommandExecution();

                _cmdManager.ProcessCommand(clientManager, command, acknowledgementId, stats);

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

        private object ReceiveCommandSynchronously(ClientManager clientManager, int bytesRecieved, out long acknowledgementId)
        {
            Object command = null;
            byte[] value = null;
            long transactionSize = 0;
            int discardingLength = 20;

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

            acknowledgementId = GetAcknowledgementId(clientManager);
            AssureRecieve(clientManager, ref clientManager.Buffer, cmdSizeHolderBytesCount);
            bytesRecieved += cmdSizeHolderBytesCount;

            AssureRecieve(clientManager, out command, out value, out transactionSize);

            transactionSize += bytesRecieved;

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "cmd_size :" + transactionSize);

            clientManager.ReinitializeBuffer();

            return command;
        }

        private void ReceiveCommandAndDataAsync(Object obj)
        {
            SocketAsyncEventArgs args = (SocketAsyncEventArgs)obj;
            RecContextServer receiveStruct = (RecContextServer)args.UserToken;
            ClientManager clientManager = receiveStruct.Client;
            object command = null;

            try
            {
                UsageStats stats = new UsageStats();
                stats.BeginSample();

                if (clientManager.ClientSocket == null)
                {
                    return;
                }

                int bytesReceived = args.BytesTransferred;
                clientManager.AddToClientsBytesRecieved(bytesReceived);

                if (SocketServer.IsServerCounterEnabled)
                {
                    PerfStatsColl.IncrementBytesReceivedPerSecStats(bytesReceived);
                }

                if (bytesReceived == 0)
                {
                    DisposeClient(clientManager);
                    return;
                }

                if (args.SocketError != SocketError.Success)
                {
                    DisposeClient(clientManager);
                    return;
                }

                // Dump that chunk to the stream... and recaluculate the remaining command size.
                receiveStruct.RequestSize -= bytesReceived;
                receiveStruct.StreamBuffer.Write(receiveStruct.Buffer.Array, receiveStruct.Buffer.Offset, bytesReceived);

                // For checking of the receival of current chunk...
                if (bytesReceived < receiveStruct.ChunkToReceive)
                {
                    int newDataToReceive = receiveStruct.ChunkToReceive - bytesReceived;
                    int newOffset = receiveStruct.Buffer.Array.Length - newDataToReceive;
                    BeginAsyncReceive(args, receiveStruct.RequestSize, receiveStruct.Buffer.Array, newOffset, newDataToReceive, false);
                    return;
                }

                // Check if there is any more command/request left to receive...
                if (receiveStruct.RequestSize > 0)
                {
                    int dataToRecieve = MemoryUtil.GetSafeByteCollectionCount(receiveStruct.RequestSize);

                    BeginAsyncReceive(args, receiveStruct.RequestSize, new byte[dataToRecieve], 0, dataToRecieve, false);
                    return;
                }

                switch (receiveStruct.State)
                {
                    case ReceivingState.ReceivingDiscBuff:

                        if (_type == ConnectionManagerType.Management)
                        {
                            receiveStruct.StreamBuffer.Seek(0, SeekOrigin.Begin);
                            byte[] memstr = ReadDiscardingBuffer(receiveStruct.StreamBuffer);
                            byte[] mangBuff = new byte[3];
                            Array.Copy(memstr, mangBuff, 3);
                            string k = System.Text.Encoding.ASCII.GetString(mangBuff);

                            // Means its a client command that has been recieved here so set a property to be true
                            if (!System.Text.Encoding.ASCII.GetString(mangBuff).Equals("MNG"))
                                receiveStruct.Client.IsMarkedAsClient = true;
                        }
                        if (!clientManager.SupportAcknowledgement)
                        {
                            // Reading the message size bytes...
                            receiveStruct.State = ReceivingState.ReceivingDataLength;
                            receiveStruct.StreamBuffer = new ClusteredMemoryStream(MessageSizeHeader);
                            receiveStruct.AcknowledgmentId = 0;
                            BeginAsyncReceive(args, MessageSizeHeader, new byte[MessageSizeHeader], 0, MessageSizeHeader, false);
                            return;
                        }

                        receiveStruct.IsOptimized = false;
                        receiveStruct.State = ReceivingState.ReceivingAckId;
                        receiveStruct.StreamBuffer = new ClusteredMemoryStream(AckIdBufLen);
                        receiveStruct.AcknowledgmentId = 0;
                        BeginAsyncReceive(args, AckIdBufLen, new byte[AckIdBufLen], 0, AckIdBufLen, false);
                        break;

                    case ReceivingState.ReceivingAckId:
                        receiveStruct.IsOptimized = false;
                        receiveStruct.AcknowledgmentId = ReadAcknowledgementId(receiveStruct.StreamBuffer);

                        // Reading the message size bytes...
                        receiveStruct.State = ReceivingState.ReceivingDataLength;
                        receiveStruct.StreamBuffer = new ClusteredMemoryStream(MessageSizeHeader);
                        BeginAsyncReceive(args, MessageSizeHeader, new byte[MessageSizeHeader], 0, MessageSizeHeader, false);
                        break;

                    case ReceivingState.ReceivingDataLength:
                        // Reading request size...
                        byte[] reqSizeBytes = new byte[MessageSizeHeader];
                        receiveStruct.StreamBuffer.Seek(0, SeekOrigin.Begin);
                        receiveStruct.StreamBuffer.Read(reqSizeBytes, 0, MessageSizeHeader);

                        int requestSize = HelperFxn.ToInt32(reqSizeBytes, 0, reqSizeBytes.Length);

                        // Doing the chunk to receive work...
                        int dataToRecieve = MemoryUtil.GetSafeByteCollectionCount(requestSize);
                        receiveStruct.State = ReceivingState.ReceivingData;
                        receiveStruct.StreamBuffer = new ClusteredMemoryStream((int)requestSize);
                        BeginAsyncReceive(args, requestSize, new byte[dataToRecieve], 0, dataToRecieve, false);
                        break;

                    case ReceivingState.ReceivingData:
                        using (ClusteredMemoryStream streamBuffer = receiveStruct.StreamBuffer)
                        {
                            bool isOptimized = receiveStruct.IsOptimized;
                            uint requestId = receiveStruct.RequestId;
                            long ackId = receiveStruct.AcknowledgmentId;

                            // Start listening for the new request...
                            if (receiveStruct.RequestId >= uint.MaxValue)
                            {
                                receiveStruct.RequestId = 0;
                            }

                            receiveStruct.RequestId++;
                            streamBuffer.Seek(0, SeekOrigin.Begin);

                            if (SocketServer.HostClientConnectionManager != null && receiveStruct.Client.IsMarkedAsClient) // if this is true than it must be a client command that need to be transfered
                            {
                                var sendBytes = streamBuffer.ToArray();
                                receiveStruct.Client.IsMarkedAsClient = false;
                                var hostConMgr = (ConnectionManager)SocketServer.HostClientConnectionManager;
                                receiveStruct.Client.ClientDisposed -= OnClientDisposed;

                                hostConMgr.OnClientConnected(clientManager, sendBytes);
                                return;
                            }

                            if (clientManager.IsOptimized)
                            {
                                // Listen for new request...
                                receiveStruct.StreamBuffer = new ClusteredMemoryStream(MessageSizeHeader);
                                receiveStruct.State = ReceivingState.ReceivingDataLength;
                                BeginAsyncReceive(args, MessageSizeHeader, new byte[MessageSizeHeader], 0, MessageSizeHeader, false);

                                while (streamBuffer.Position < streamBuffer.Length)
                                {

                                    if (clientManager.SupportAcknowledgement)
                                    {
                                        receiveStruct.AcknowledgmentId = ReadAcknowledgementId(streamBuffer);
                                    }

                                    int commandSize = ReadCommandSize(streamBuffer);

                                    object cmd = GetProtoCommand(streamBuffer, commandSize);

                                    ProcessClientCommand(cmd, clientManager, requestId, ackId, stats);

                                    if (SocketServer.IsServerCounterEnabled)
                                    {
                                        PerfStatsColl.IncrementRequestsPerSecStats(1);
                                    }
                                }
                            }
                            else
                            {
                                object cmd = GetProtoCommand(streamBuffer, (int)streamBuffer.Length);

                                ProcessClientCommand(cmd, clientManager, requestId, ackId, stats);

                                if (SocketServer.IsServerCounterEnabled)
                                {
                                    PerfStatsColl.IncrementRequestsPerSecStats(1);
                                }

                                // If the client manager is marked optimized...
                                if (clientManager.IsOptimized)
                                {
                                    receiveStruct.IsOptimized = true;
                                    receiveStruct.StreamBuffer = new ClusteredMemoryStream(MessageSizeHeader);
                                    receiveStruct.State = ReceivingState.ReceivingDataLength;
                                    BeginAsyncReceive(args, MessageSizeHeader, new byte[MessageSizeHeader], 0, MessageSizeHeader, false);
                                    return;
                                }

                                if (RegisterCallBack(cmd as ProtoCommand))
                                {
                                    receiveStruct.StreamBuffer = new ClusteredMemoryStream(DiscardBufLen);
                                    receiveStruct.State = ReceivingState.ReceivingDiscBuff;
                                    BeginAsyncReceive(args, DiscardBufLen, new byte[DiscardBufLen], 0, DiscardBufLen, false);
                                }
                            }
                        }
                        break;
                }
            }
            catch (SocketException so_ex)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + so_ex.ToString());

                DisposeClient(clientManager);

            }
            catch (Exception e)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + e.ToString());
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.ReceiveCallback", clientManager.ToString() + command + " Error " + e.ToString());

                try
                {
                    if (Management.APILogging.APILogManager.APILogManger != null && Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder();
                        log.GenerateConnectionManagerLog(clientManager, e.ToString());
                    }
                }
                catch
                {

                }

                DisposeClient(clientManager);

            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.StopClientActivity(clientManager.ClientID);
            }
        }

        private void ProcessClientCommand(object command, ClientManager clientManager, uint feed, long ackId, UsageStats stats)
        {
            if (UseCommandProcessorPool && CommandHelper.Queable(command))
            {
                ProcCommand procCommand = new ProcCommand();
                procCommand.ClientManager = clientManager;
                procCommand.Acknowledgementid = ackId;
                procCommand.Stats = stats;
                procCommand.CommandInst = command;

                _processorPool.EnqueuRequest(procCommand, feed++);
            }
            else
            {
                _cmdManager.ProcessCommand(clientManager, command, ackId, stats);
            }
        }

        private object GetProtoCommand(Stream stream, int size)
        {
            using (ClusteredMemoryStream cmdStream = new ClusteredMemoryStream(size))
            {

                int bytesRead = 0;

                while (bytesRead < size)
                {
                    int bytesToRead = MemoryUtil.GetSafeByteCollectionCount(size - bytesRead);
                    byte[] byteChunk = new byte[bytesToRead];
                    stream.Read(byteChunk, 0, bytesToRead);
                    cmdStream.Write(byteChunk, 0, bytesToRead);
                    bytesRead += bytesToRead;
                }

                cmdStream.Seek(0, SeekOrigin.Begin);
                return _cmdManager.Deserialize(cmdStream);
            }
        }

        private byte[] ReadDiscardingBuffer(Stream stream)
        {
            byte[] disBuff = new byte[DiscardBufLen];
            stream.Read(disBuff, 0, DiscardBufLen);
            return disBuff;
        }

        private long ReadAcknowledgementId(Stream stream)
        {
            byte[] akcIdBuff = new byte[AckIdBufLen];
            stream.Read(akcIdBuff, 0, AckIdBufLen);
            return HelperFxn.ToInt64(akcIdBuff, 0, AckIdBufLen);
        }

        private int ReadCommandSize(Stream stream)
        {
            byte[] commandSize = new byte[MessageSizeHeader];
            stream.Read(commandSize, 0, MessageSizeHeader);
            return HelperFxn.ToInt32(commandSize, 0, MessageSizeHeader);
        }

        private void BeginAsyncReceive(SocketAsyncEventArgs args, long requestSize,
           byte[] buffer, int offset, int count, bool runOnThreadPool)
        {
            RecContextServer receiveStruct = (RecContextServer)args.UserToken;
            receiveStruct.RequestSize = requestSize;
            receiveStruct.ChunkToReceive = count;
            receiveStruct.Buffer = new ArraySegment<byte>(buffer, offset, count);

            args.SetBuffer(buffer, offset, count);

            if (receiveStruct.Client.ClientSocket.ReceiveAsync(args))
            {
                return;
            }

            if (runOnThreadPool)
            {
                ThreadPool.QueueUserWorkItem(ReceiveCommandAndDataAsync, args);
                return;
            }

            ReceiveCommandAndDataAsync(args);
        }

        private void TransferableCommandThread(ClientManager clientManager, byte[] transferCommand)
        {
            object command;
            clientManager.Buffer = transferCommand;
            long acknowledgementId = GetAcknowledgementId(clientManager);

            using (MemoryStream str = new MemoryStream(clientManager.Buffer))
            {
                command = _cmdManager.Deserialize(str);
            }

            clientManager.StartCommandExecution();
            _cmdManager.ProcessCommand(clientManager, command, acknowledgementId, null);
        }


        public void Process(ProcCommand procCommand)
        {
            _cmdManager.ProcessCommand(procCommand.ClientManager, procCommand.CommandInst,
                procCommand.Acknowledgementid, procCommand.Stats);
        }

        private bool RegisterCallBack(ProtoCommand cmd)
        {
            if (_cmdManagerType == CommandManagerType.NCacheService)
            {
                if (cmd.type == ProtoCommand.Type.GET_LC_DATA || cmd.type == ProtoCommand.Type.GET_SERVER_MAPPING || cmd.type == ProtoCommand.Type.GET_CACHE_MANAGEMENT_PORT)
                {
                    return true;
                }
                return false;
            }
            return true;
        }
    }
}
