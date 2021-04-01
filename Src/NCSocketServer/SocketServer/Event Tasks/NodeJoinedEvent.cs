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
using System.Text;
using Alachisoft.NCache.Common.Net;
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.EventTask
{
	internal sealed class NodeJoinedEvent : IEventTask
	{
		private string _cacheId;
		private Address _clusterAddress;
        private Address _serverAddress;
        private string _clientId;
        private bool _reconnect;

		internal NodeJoinedEvent(string cacheId, Address clusterAddress, Address serverAddress, string clientid, bool reconn)
		{
			_cacheId = cacheId;
            _clusterAddress = clusterAddress;
            _serverAddress = serverAddress;
            _clientId = clientid;
            _reconnect = reconn;
		}

        public void Process()
        {
            ClientManager clientManager = null;


            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientId];
            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.NodeJoinedEventResponse nodeJoined = new Alachisoft.NCache.Common.Protobuf.NodeJoinedEventResponse();

                nodeJoined.clusterIp = _clusterAddress.IpAddress.ToString();
                nodeJoined.clusterPort = _clusterAddress.Port.ToString();
                nodeJoined.serverIp = _serverAddress.IpAddress.ToString();
                nodeJoined.serverPort = _serverAddress.Port.ToString();
                nodeJoined.reconnect = _reconnect;

                response.nodeJoined = nodeJoined;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.NODE_JOINED_EVENT;

                IList serializedResponse = null;

                serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response, Common.Protobuf.Response.Type.NODE_JOINED_EVENT);
                

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);
            }
        }
	}
}
