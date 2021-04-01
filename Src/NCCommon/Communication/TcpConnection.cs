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
using System;
using System.Net.Sockets;
using System.Net;
using Alachisoft.NCache.Common.Communication.Exceptions;

namespace Alachisoft.NCache.Common.Communication
{
    public class TcpConnection :IConnection
    {
        private Socket _socket;
        private bool _connected;
        private object _sync_lock = new object();
        private IPAddress _bindIP;

        public bool Connect(string serverIP, int port)
        {
            bool connected = false;

            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                
                if(_bindIP != null)
                    socket.Bind(new IPEndPoint(_bindIP,0));
                IPAddress ip = IPAddress.Parse(serverIP);
                socket.Connect(ip, port);
                connected = true;
                _socket = socket;
            }
            catch (Exception e)
            {
                throw new ConnectionException("[" + serverIP + "] " +  e.Message,e);
            }

            _connected = connected;
            return connected;
        }

        public void Disconnect()
        {
            try
            {
                if (_socket != null && _socket.Connected)
                {
                    
                        _socket.Shutdown(SocketShutdown.Both);
                        _socket.Close();
                }
            }
            catch (Exception) { }
        }

        public bool Send(byte[] buffer, int offset, int count)
        {
            bool sent = false;

            lock (_sync_lock)
            {
                if (_connected)
                {
                    int dataSent = 0;

                    while (count > 0)
                    {
                        try
                        {
                            dataSent = _socket.Send(buffer, offset, count, SocketFlags.None);
                            offset += dataSent;
                            count = count - dataSent;
                        }
                        catch (SocketException se)
                        {
                            _connected = false;
                            throw new ConnectionException(se.Message,se);
                        }
                    }
                    sent = true;
                }
                else
                    throw new ConnectionException();
            }

            return sent;
        }

        public bool Receive(byte[] buffer, int count)
        {
            bool received = false;
            {
                if (_connected)
                {
                    int receivedCount = 0;
                    int offset = 0;
                    while (count > 0)
                    {
                        try
                        {
                            receivedCount = _socket.Receive(buffer, offset, count, SocketFlags.None);
                            
                            if (receivedCount == 0) throw new SocketException((int)SocketError.ConnectionReset);
                            
                            offset += receivedCount;
                            count = count - receivedCount;
                        }
                        catch (SocketException se)
                        {
                            _connected = false;
                            throw new ConnectionException(se.Message,se);
                        }
                    }
                    received = true;
                }
                else
                    throw new ConnectionException();
            }
            return received;
        }

        public bool IsConnected
        {
            get
            {
                if (_connected)
                    _connected = _socket.Connected;

                return _connected; 
            }
        }

        public void Bind(string address)
        {
            if (address == null)
                throw new ArgumentNullException("address");

            _bindIP = IPAddress.Parse(address);
        }
    }
}
