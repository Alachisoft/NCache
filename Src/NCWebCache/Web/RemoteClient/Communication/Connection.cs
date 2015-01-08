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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Web.Caching.Util;
using Alachisoft.NCache.Web.Statistics;
using System.IO;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Web.Util;
using Alachisoft.NCache.Web.Command;
using Alachisoft.NCache.Web.RemoteClient.Config;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Web.Communication
{
	internal sealed class Connection
	{
        #region --------Constants--------
        internal const int CmdSizeHolderBytesCount = 10;
        internal const int ValSizeHolderBytesCount = 10;
        internal const int TotSizeHolderBytesCount = ValSizeHolderBytesCount + CmdSizeHolderBytesCount;
        #endregion

        private OnCommandRecieved _commandRecieved = null;
        private OnServerLost _serverLost = null;
        private bool _isConnected = true;
		private Socket _primaryClient = null;
        private Socket _secondaryClient = null;
        private IPAddress _address;
        private string _ipAddress = string.Empty;
        private string _intendedRecipientIPAddress = string.Empty;
        private int _port = 0;
        private object _connectionMutex = new object();
        private Latch _connectionStatusLatch = new Latch(ConnectionStatus.Disconnected);
        public static long s_receiveBufferSize = 2048000;
        private int _processID = System.Diagnostics.Process.GetCurrentProcess().Id;
        private string _cacheId;
       
        private Thread _primaryReceiveThread = null;
        private Thread _secondaryReceiveThread = null;
        private bool _notificationsRegistered = false;
        private bool _isReconnecting = false;
       
        
        private bool _forcedDisconnect = false;

        private bool _nagglingEnabled = false;
        private long _nagglingSize = 5 * 100 * 1024; //500k
        private NagglingManager _priNagglingMgr;
        private NagglingManager _secNagglingMgr;
        private Alachisoft.NCache.Common.DataStructures.Queue _msgQueue;
        private bool _supportDualSocket = false;
        private object _syncLock = new object();
        private static IPEndPoint _bindIP;
        private Logs _logger;
        private ResponseIntegrator _responseIntegrator;
        private Address _serverAddress;

        private PerfStatsCollector _perfStatsColl = null;


            private object _socketSelectionMutex = new object();
        private bool _usePrimary = true;


        // function that sets string provided to bindIP
        internal void SetBindIP(string value)
        {
            
            if (value != null && value != string.Empty)
            {
                try
                {
                    _bindIP = new IPEndPoint(IPAddress.Parse(value), 0);
                }
                catch (Exception) { }
            }
           
        }

        public string GetClientLocalIP()
        {
            string ip = string.Empty;
            if (PrimaryClientSocket != null)
            {
                if (this.IsConnected)
                {
                    IPEndPoint add = (IPEndPoint)PrimaryClientSocket.LocalEndPoint;
                    ip = add.Address.ToString();
                }
            }
            return ip;
        }

        internal Connection(OnCommandRecieved commandRecieved, OnServerLost serverLost, Logs logs, PerfStatsCollector perfStatsCollector,ResponseIntegrator rspIntegraotr, string bindIP)
        {
            _commandRecieved = commandRecieved;
            _serverLost = serverLost;
            _logger = logs;
            _responseIntegrator = rspIntegraotr;

            _perfStatsColl = perfStatsCollector;

            SetBindIP(bindIP);
            if (System.Configuration.ConfigurationSettings.AppSettings["EnableNaggling"] != null)
                _nagglingEnabled = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["EnableNaggling"]);

            //read the naggling size from app.config and covert it to bytes.
            if (System.Configuration.ConfigurationSettings.AppSettings["NagglingSize"] != null)
                _nagglingSize = 1024 * Convert.ToInt64(System.Configuration.ConfigurationSettings.AppSettings["NagglingSize"]);

            if (System.Configuration.ConfigurationSettings.AppSettings["EnableDualSockets"] != null)
                _supportDualSocket = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["EnableDualSockets"]);
            
        }

        internal bool Connect(string hostName, int port)
        {

            return Connect(((IPAddress[])Dns.GetHostByName(hostName).AddressList)[0], port);
        }

        private bool DoNaggling
        {
            get { return (_nagglingEnabled && _priNagglingMgr != null); }
        }

        public bool SupportDualSocket
        {
            get { return _supportDualSocket; }
        }
       
        public Socket PrimaryClientSocket
        {
            get { return _primaryClient; }
        }

        public Socket SecondaryClientSocket
        {
            get { return _secondaryClient; }
        }

        public void Dispose()
        {
            if (_msgQueue != null && !_msgQueue.Closed)
            {
                _msgQueue.close(true);
                _msgQueue = null;
            }

            try
            {
                if (_priNagglingMgr != null && _priNagglingMgr.IsAlive)
                {
                    _priNagglingMgr.Abort();
                }

                if (_secNagglingMgr != null && _secNagglingMgr.IsAlive)
                {
                    _secNagglingMgr.Abort();
                }
            }
            catch (Exception) { }
        }

        internal bool Connect(IPAddress ipAddress, int port)
        {
           

            this._ipAddress = ipAddress.ToString();
            this._address = ipAddress;
            this._port = port;
            this._serverAddress = new Address(ipAddress, port);
            lock (_connectionMutex)
            {
                _primaryClient = PrepareToConnect(_primaryClient);

                IPEndPoint endPoint = new IPEndPoint(ipAddress, port);

                try
                {
                    _primaryClient.Connect(endPoint);
                }
                catch (Exception e)
                {
                    if(_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error( "Connection.Connect", " can not connect to " + ipAddress + ":" + port + ". error: " + e.ToString());
                    return false;
                }
            }
            return true;
        }

        public void Init()
        {
            if (_nagglingEnabled)
            {
                _msgQueue = new Alachisoft.NCache.Common.DataStructures.Queue();
                _priNagglingMgr = new NagglingManager(this, this.PrimaryClientSocket, _msgQueue, _nagglingSize, _syncLock);
                _priNagglingMgr.Start();

                if (SupportDualSocket)
                {
                    _secNagglingMgr = new NagglingManager(this, this.SecondaryClientSocket, _msgQueue, _nagglingSize, _syncLock);
                    _secNagglingMgr.Start();
                }
            }

            StartThread();
        }

        internal Latch StatusLatch
        {
            get { return _connectionStatusLatch; }
        }

        private Socket PrepareToConnect(Socket client)
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 131072);
            client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);

            if (_bindIP != null) 
                try 
                { 
                    client.Bind(_bindIP); 
                }
                catch (Exception) { throw new Exception("Invalid bind-ip-address specified in client configuration"); }
            
            _forcedDisconnect = false;
            return client;
        }

        internal void ConnectSecondarySocket(IPAddress address, int port)
        {
            _secondaryClient = PrepareToConnect(_secondaryClient);
            IPEndPoint endPoint = new IPEndPoint(address, port);
            try
            {
                _secondaryClient.Connect(endPoint);
            }
            catch (Exception e)
            {
                if(_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error( "Connection.Connect", " can not connect to " + address + ":" + port + ". error: " + e.ToString());
            }
        }

        internal bool IsConnected
        {
            get { return _connectionStatusLatch.IsAnyBitsSet(ConnectionStatus.Connected); }
        }

        /// <summary>
        /// Get ip address of machine to which connection is made
        /// </summary>
        internal string IpAddress
        {
            get { return this._ipAddress; }
        }

        internal IPAddress Address
        {
            get { return this._address; }
        }

        internal Address ServerAddress
        {
            get { return this._serverAddress; }
        }

        internal string IntendedRecipientIPAddress
        {
            set { this._intendedRecipientIPAddress = value; }
            get { return this._intendedRecipientIPAddress; }
        }
        /// <summary>
        /// Get port on which connection is made
        /// </summary>
        internal int Port
        {
            get { return this._port; }
        }

        internal bool NotifRegistered
        {
            get { return this._notificationsRegistered; }
            set { this._notificationsRegistered = value; }
        }

        internal bool IsReconnecting
        {
            get { return this._isReconnecting; }
            set { this._isReconnecting = value; }
        }

        internal void StartThread()
        {

            _primaryReceiveThread = new Thread(new ParameterizedThreadStart(RecieveThread));
            _primaryReceiveThread.Priority = ThreadPriority.AboveNormal;
            _primaryReceiveThread.IsBackground = true;  //Now application can exit without calling dispose()
            _primaryReceiveThread.Start(_primaryClient);

            if (SupportDualSocket)
            {
                _secondaryReceiveThread = new Thread(new ParameterizedThreadStart(RecieveThread));
                _secondaryReceiveThread.Priority = ThreadPriority.AboveNormal;
                _secondaryReceiveThread.IsBackground = true;  //Now application can exit without calling dispose()
                _secondaryReceiveThread.Start(_secondaryClient);
				
            }

        }

        internal void Disconnect()
        { 
            _forcedDisconnect = true;

           
            if (_primaryReceiveThread != null && _primaryReceiveThread.ThreadState != ThreadState.Aborted && _primaryReceiveThread.ThreadState != ThreadState.AbortRequested)
            {
                try
                {
                    if(_logger.NCacheLog != null) _logger.NCacheLog.Flush();
                    _primaryReceiveThread.Abort();
                }
                catch (System.Threading.ThreadAbortException){ }
               
                _primaryReceiveThread = null;
            }

            if (_primaryClient != null && _primaryClient.Connected)
            {
                try
                {
                    _primaryClient.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException) { }
                catch (ObjectDisposedException) { }
                _primaryClient.Close();
                _primaryClient = null;
            }
           
           
            //dispose the secondary socket
            if (_secondaryReceiveThread != null && _secondaryReceiveThread.ThreadState != ThreadState.Aborted && _secondaryReceiveThread.ThreadState != ThreadState.AbortRequested)
            {
                try
                {
                    if (_logger.NCacheLog != null)  _logger.NCacheLog.Flush();
                    _secondaryReceiveThread.Abort();
                }
                catch (System.Threading.ThreadAbortException) { }
                _secondaryReceiveThread = null;
            }

            if (_secondaryClient != null && _secondaryClient.Connected)
            {
                try
                {
                    _secondaryClient.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException) { }
                catch (ObjectDisposedException) { }
                _secondaryClient.Close();
                _secondaryClient = null;
            }

           
            this._connectionStatusLatch.SetStatusBit(ConnectionStatus.Disconnected, ConnectionStatus.Connected);

     
        }

        private void RecieveThread(object clientSocket)

        {
            while (true)
            {
                try
                {
                    CommandResponse response = null;
                    Socket client = null;


                    if (clientSocket != null && clientSocket is Socket )
                    {
                        client = clientSocket as Socket;
                    }

                    response = RecieveCommandResponse(client);

                    if (_perfStatsColl.IsEnabled)
                        _perfStatsColl.IncrementClientResponsesPerSecStats(1);

                    string serverAddress = ((IPEndPoint)client.RemoteEndPoint).Address.ToString();

                    if(response != null)
                        _commandRecieved(response, _serverAddress);
                }
                catch (ConnectionException ce)
                {
                    if (_forcedDisconnect)
                    {
                        if(_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error( "Connection.ReceivedThread", "Connection with server lost gracefully");
                    }
                    else
                        if(_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error( "Connection.ReceivedThread", "An established connection with the server is lost. Error:" + ce.ToString());

                    if(!_forcedDisconnect) _connectionStatusLatch.SetStatusBit(ConnectionStatus.Disconnected, ConnectionStatus.Connected);
                    _primaryReceiveThread = null;
                    _serverLost(_serverAddress, _forcedDisconnect);
                    break;
                }
                catch (ThreadAbortException te)
                {
                    if (AppDomain.CurrentDomain.IsFinalizingForUnload()) return;
                    if (_forcedDisconnect)
                    {
                        if (_logger.IsErrorLogsEnabled)
                        {
                            _logger.NCacheLog.Error("Connection.ReceivedThread", "Connection with server lost gracefully");
                            _logger.NCacheLog.Flush();
                        }
                    }
                    if (!_forcedDisconnect) _connectionStatusLatch.SetStatusBit(ConnectionStatus.Disconnected, ConnectionStatus.Connected);
                    _serverLost(_serverAddress, _forcedDisconnect);
                    break;

                }
                catch (ThreadInterruptedException tae)
                {
                    if (AppDomain.CurrentDomain.IsFinalizingForUnload()) return;
                    if (_forcedDisconnect)
                    {
                        if (_logger.IsErrorLogsEnabled)
                        {
                            _logger.NCacheLog.Error("Connection.ReceivedThread", "Connection with server lost gracefully");
                            _logger.NCacheLog.Flush();
                        }
                    }
                    if (!_forcedDisconnect) _connectionStatusLatch.SetStatusBit(ConnectionStatus.Disconnected, ConnectionStatus.Connected);
                    _primaryReceiveThread = null;
                    _serverLost(_serverAddress, _forcedDisconnect);
                    break;
                }
                catch (Exception e)
                {
                    if (_logger.IsErrorLogsEnabled)
                    {
                        _logger.NCacheLog.Error("Connection.ReceivedThread", e.ToString());
                        _logger.NCacheLog.Flush();
                    }
                    if (!_forcedDisconnect) _connectionStatusLatch.SetStatusBit(ConnectionStatus.Disconnected, ConnectionStatus.Connected);
                    _serverLost(_serverAddress, _forcedDisconnect);
                }
            }
        }

        public CommandResponse RecieveCommandResponse()
        {
            return RecieveCommandResponse(_primaryClient);
        }

		public CommandResponse RecieveCommandResponse(Socket client)
		{
            //string result = null;
            byte[] value = null;
            CommandResponse cmdRespose = null;
            try
            {
                value = AssureRecieve(client);

                

                ///Deserialize the response
                Alachisoft.NCache.Common.Protobuf.Response response = null;
                using (MemoryStream stream = new MemoryStream(value))
                {
                    response = ProtoBuf.Serializer.Deserialize<Alachisoft.NCache.Common.Protobuf.Response>(stream);
                    stream.Close();
                }

                if (response != null && response.responseType == Alachisoft.NCache.Common.Protobuf.Response.Type.RESPONSE_FRAGMENT)
                {
                    response = _responseIntegrator.AddResponseFragment(this._serverAddress,response.getResponseFragment);
                }

                if (response != null)
                {
                    cmdRespose = new CommandResponse(false, new Address());
                    cmdRespose.Result = response;                    
                }
            }
            catch (SocketException e)
            {
                throw new ConnectionException(e.Message);
            }
            return cmdRespose;
		}

        internal void AssureSend(byte[] buffer, Socket client, bool checkConnected )
        {
            int dataSent = 0, dataLeft = buffer.Length;
            lock (_connectionMutex)
            {

                if (checkConnected && _connectionStatusLatch.IsAnyBitsSet(ConnectionStatus.Disconnected | ConnectionStatus.Connecting))
                {
                    throw new ConnectionException();
                }

                while (dataSent < buffer.Length)
                {
                    try
                    {
                        dataLeft = buffer.Length - dataSent;
                        dataSent += client.Send(buffer, dataSent, dataLeft, SocketFlags.None);

                    }
                    catch (SocketException se)
                    {

                        if (se.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                        {
                        }
                        else
                        {
                            if(_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error( "Connection.AssureSend", se.ToString());
                            _connectionStatusLatch.SetStatusBit(ConnectionStatus.Disconnected, ConnectionStatus.Connected);
                            throw new ConnectionException();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method is used by the naggling manager to send the naggled data.
        /// We pass a fixed sized buffer to this method that contains the naggled data.
        /// One extra argument 'bytesToSent' tells how many bytes we need to send from this buffer.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="buffer"></param>
        /// <param name="bytesToSent"></param>
        /// <param name="checkConnected"></param>
        internal void AssureSend(Socket client, byte[] buffer, int bytesToSent, bool checkConnected)
        {
            int dataSent = 0, dataLeft = bytesToSent;
            lock (_connectionMutex)
            {

                if (checkConnected && _connectionStatusLatch.IsAnyBitsSet(ConnectionStatus.Disconnected | ConnectionStatus.Connecting))
                {
                    throw new ConnectionException();
                }

                while (dataSent < bytesToSent)
                {
                    try
                    {
                        dataLeft = bytesToSent - dataSent;
                        dataSent += client.Send(buffer, dataSent, dataLeft, SocketFlags.None);
                    }
                    catch (SocketException se)
                    {

                        if (se.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                        {
                        }
                        else
                        {
                            if(_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error( "Connection.AssureSend", se.ToString());
                            _connectionStatusLatch.SetStatusBit(ConnectionStatus.Disconnected, ConnectionStatus.Connected);
                            throw new ConnectionException();
                        }
                    }
                }
            }
        }

        private byte[] AssureRecieve(Socket client)
        {
            
            byte[] buffer = new byte[CmdSizeHolderBytesCount];
            AssureRecieve(ref buffer, client);
            int commandSize = 0;
        
            try
            {
                commandSize = HelperFxn.ToInt32(buffer, 0, CmdSizeHolderBytesCount);
               
            }
            catch (InvalidCastException)
            {
                string str = System.Text.UTF8Encoding.UTF8.GetString(buffer);
                if(_logger.IsErrorLogsEnabled) _logger.NCacheLog.Error( "AssureReceive", str);
                throw;
            }
            if (commandSize == 0/* + dataSize == 0*/)
            {
               
                return new byte[0];
            }

            buffer = new byte[commandSize/* + dataSize*/];
            AssureRecieve(ref buffer, client);

            return buffer;
            
           
        }



        private void AssureRecieve(ref byte[] buffer, Socket client)
        {
            int totalBytesRecieved = 0;
            int bytesRecieved =0;
           

            do
            {
                try
                {
                    bytesRecieved = client.Receive(buffer, totalBytesRecieved, buffer.Length - totalBytesRecieved, SocketFlags.None);


                    if (bytesRecieved == 0) throw new SocketException((int)SocketError.ConnectionReset);


                    totalBytesRecieved += bytesRecieved;
                }
                catch (SocketException se)
                {

                    if (se.SocketErrorCode != SocketError.NoBufferSpaceAvailable) throw;

                }
            } while (totalBytesRecieved < buffer.Length);

           
        }

        internal void SendCommand(byte[] commandBytes, bool checkConnected)
        {

            if (_perfStatsColl.IsEnabled)
                _perfStatsColl.IncrementClientRequestsPerSecStats(1);

            if (DoNaggling)
                _msgQueue.add(commandBytes);
            else
            {
                if (SupportDualSocket)
                {
                    Socket selectedSocket = _primaryClient;
                    
                    lock (_socketSelectionMutex)
                    {
                        if (!_usePrimary) selectedSocket = _secondaryClient;
                        _usePrimary = !_usePrimary;
                    }

                    AssureSend(commandBytes, selectedSocket, checkConnected);
                }
                else
                {
                    AssureSend(commandBytes, _primaryClient, checkConnected);
                }
            }
        }

        private string ShowBufferContents(byte[] buffer, int offset, int count)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("[ ");
            if (buffer != null)
            {
                if (count > buffer.Length)
                {
                    count = buffer.Length;
                }
                for (int i = 0; i < count; i++)
                {
                    sb.Append(" " + buffer[i] + " ,");
                }
            }
            sb.Append(" ]");
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            Connection connection = obj as Connection;
            return (connection != null && this.IpAddress == connection.IpAddress);
        }
    }
}
