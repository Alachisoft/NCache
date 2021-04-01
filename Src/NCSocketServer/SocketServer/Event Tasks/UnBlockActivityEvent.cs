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
using System.Collections;
using Alachisoft.NCache.Caching;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.EventTask
{
    /// <summary>
    /// This event is fired when an item is added to cache
    /// </summary>
    internal sealed class UnBlockActivityEvent : IEventTask
    {
        private string _uniquekey;
        private string _cacheId;
        private string _clientid;
        private string _serverIP;
        private int _port;
    

        internal UnBlockActivityEvent(string key, string cacheId, string clientid, string serverIP, int port)
        {
            _uniquekey = key;
            _cacheId = cacheId;
            _clientid = clientid;
            _serverIP = serverIP;
            _port = port;
         
        }

        public void Process()
        {
            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientid];
            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();

                Alachisoft.NCache.Common.Protobuf.UnBlockActivityEventResponse unBlockActivityResponse = new Alachisoft.NCache.Common.Protobuf.UnBlockActivityEventResponse();
                unBlockActivityResponse.uniqueKey = _uniquekey;
                unBlockActivityResponse.serverIP = _serverIP;
                unBlockActivityResponse.port = _port;

                response.unblockActivityEvent = unBlockActivityResponse;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.UNBLOCK_ACTIVITY;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.UNBLOCK_ACTIVITY);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);
               
            }
        }
    }
}
