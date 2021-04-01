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
using Alachisoft.NCache.Common.Net;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.EventTask
{
    internal sealed class NodeLeftEvent : IEventTask
    {
        private string _cacheId;
        private Address _clusterAddress;
        private Address _serverAddress;
        private string _clientId;

        internal NodeLeftEvent(string cacheId, Address clusterAddress, Address serverAddress, string clientid)
        {
            _cacheId = cacheId;
            _clusterAddress = clusterAddress;
            _serverAddress = serverAddress;
            _clientId = clientid;
        }

        public void Process()
        {
            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientId];
            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.NodeLeftEventResponse nodeLeft = new Alachisoft.NCache.Common.Protobuf.NodeLeftEventResponse();

                nodeLeft.clusterIp = _clusterAddress.IpAddress.ToString();
                nodeLeft.clusterPort = _clusterAddress.Port.ToString();
                nodeLeft.serverIp = _serverAddress.IpAddress.ToString();
                nodeLeft.serverPort = _serverAddress.Port.ToString();

                response.nodeLeft = nodeLeft;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.NODE_LEFT_EVENT;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.NODE_LEFT_EVENT);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);                
            }
        }
    }
}