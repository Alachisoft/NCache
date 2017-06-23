// Copyright (c) 2017 Alachisoft
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Commands;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.ExecutionManagement;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Parsing;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.ResponseManagement;
using System.Threading;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway
{
    public class MemTcpClient
    {
        private LogManager _logManager;

        private ProtocolParser _parser;
        private ResponseManager _responseManager;
        protected ExecutionManager _executionManager;

        protected TcpClient _client;
        protected NetworkStream _stream;
        protected byte[] _inputBuffer;
        protected const int MAX_BUFFER_SIZE = 10240;
        protected DataStream _inputDataStream;

        private bool _disposed = false;

        private ProtocolType _protocol;
        public ProtocolType Protocol
        {
            get { return _protocol; }
        }

        public LogManager LogManager
        {
            get { return _logManager; }
        }

        /// <summary>
        /// Initialize and start new TcpClientHandler
        /// </summary>
        /// <param name="client">TcpClient to Handle</param>
        public MemTcpClient(TcpClient client, ProtocolType protocol)
        {
            _logManager = new LogManager(client.Client.RemoteEndPoint.ToString());
            _logManager.Info("MemTcpClient", "\tNew client connected on protocol :" + protocol.ToString());
            
            _client = client;
            _stream = _client.GetStream();

            _executionManager = new SequentialExecutionManager(_logManager);

            _inputBuffer = new byte[MAX_BUFFER_SIZE];
            _inputDataStream = new DataStream();

            _protocol = protocol;
            if (protocol == ProtocolType.Text)
            {
                _parser = new TextProtocolParser(_inputDataStream, this, _logManager);
                _responseManager = new TextResponseManager(_stream, _logManager);
            }
            else
            {
                _parser = new BinaryProtocolParser(_inputDataStream, this, _logManager);
                _responseManager = new BinaryResponseManager(_stream, _logManager);
            }
            _responseManager.MemTcpClient = this;

            _parser.CommandConsumer = _executionManager;
            _executionManager.CommandConsumer = _responseManager;
        }

        public void Start()
        {
            _stream.BeginRead(_inputBuffer, 0, MAX_BUFFER_SIZE, new AsyncCallback(AsyncReadCallback), null);
            _logManager.Info("MemTcpClient.Start()", "\tNew MemTcpClient started for protocol:" + _protocol.ToString());
        }

        public void AsyncReadCallback(IAsyncResult ar)
        {
            try
            {

                int bytesRecieved = 0;

                if (_disposed)
                    return;
                try
                {
                    bytesRecieved = this._stream.EndRead(ar);
                }
                catch (ObjectDisposedException e)
                {
                    return;
                }

                if (bytesRecieved == 0)
                {
                    TcpNetworkGateway.DisposeClient(this);
                    return;
                }
                _inputDataStream.Write(_inputBuffer, 0, bytesRecieved);

                _stream.BeginRead(_inputBuffer, 0, MAX_BUFFER_SIZE, new AsyncCallback(AsyncReadCallback), null);

                lock (_parser)
                {
                    if (_parser.Alive)
                        return;
                    _parser.Alive = true;
                }
                _parser.StartParser();

            }
            catch (Exception e)
            {
                _logManager.Error("TcpClientHandler.AsyncReadCallback()", "\tError in client handler." + e.Message);
                TcpNetworkGateway.DisposeClient(this);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _parser.Disposed = true;
            _executionManager.Disposed = true;
            _responseManager.Disposed = true;

            if(_stream!=null)
                _stream.Dispose();
            if (_client != null)
                _client.Close();
        }

        ~MemTcpClient()
        {
        }
    }
}
