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
using System.Threading;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;
using Alachisoft.NCache.Common.Stats;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway
{
    public class TcpNetworkGateway : NetworkGateway
    {
        private TcpListener _listener;
        private ProtocolType _protocol;


        public TcpNetworkGateway(string hostName, int port, ProtocolType protocol)
        {
            _listener = new TcpListener(System.Net.IPAddress.Parse(hostName), port);
            _protocol = protocol;
        }

        public override void StartListenForClients()
        {
            try
            {
                _listener.Start();
                _listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnected), null);
            }
            catch (Exception e)
            {
                LogManager.Logger.Fatal("TcpNetworkGateway.StartListenForClients()", "\t Failed to start asynchronously accept TcpClients. " + e.Message);
                throw;
            }
        }

        private void OnClientConnected(IAsyncResult ar)
        {
            TcpClient client = null;
            try
            {
                client = _listener.EndAcceptTcpClient(ar);

                try
                {
                    _listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnected), null);
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    LogManager.Logger.Fatal("TcpNetworkGateway.OnClientConnected()", "\t Failed to accept TcpClients. " + e.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    LogManager.Logger.Fatal("TcpNetworkGateway.OnClientConnected()", "\t Failed to accept TcpClients. " + ex.Message);
                    throw;
                }

                MemTcpClient clientHandler = new MemTcpClient(client, _protocol);
                lock(_clients)
                    _clients.Add(clientHandler);
                clientHandler.Start();
            }
            catch (Exception e)
            {
                string clientName = "";
                if (client != null)
                {
                    clientName = client.Client.RemoteEndPoint.ToString();
                    clientName = " for " + clientName;
                }
                LogManager.Logger.Fatal("TcpNetworkGateway.OnClientConnected()", "\t Failed to initialize MemTcpClient " + clientName + e.Message+e.StackTrace);

                try
                {
                    client.Close();
                }
                catch (Exception)
                { }
            }
            

        }

        public override void StopListenForClients()
        {
            _listener.Stop();
        }


    }
}
