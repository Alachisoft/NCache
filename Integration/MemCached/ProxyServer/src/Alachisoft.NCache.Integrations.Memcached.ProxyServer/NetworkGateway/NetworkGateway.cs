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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway
{
    public abstract class NetworkGateway
    {
        public abstract void StartListenForClients();
        public abstract void StopListenForClients();

        protected static HashSet<MemTcpClient> _clients = new HashSet<MemTcpClient>();

        public static void DisposeClient(MemTcpClient client)
        {
            if (client == null)
                return;
            try
            {
                bool exists = false;

                lock (_clients)
                {
                    exists = _clients.Remove(client);
                }

                if (exists)
                {
                    client.LogManager.Info("NetworkGateway.DisposeClient()","\tDisposing " + client.Protocol + "client");
                    client.Dispose();
                }
            }
            catch (Exception e)
            {
                LogManager.Logger.Fatal("NetworkGateway.DisposeClient()", "\tFailed to dispose MemTcpClient. " + e.Message);
            }
        }
    }
}
