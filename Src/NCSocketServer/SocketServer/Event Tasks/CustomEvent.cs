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
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.EventTask
{
    internal sealed class CustomEvent : IEventTask
    {
        private byte[] _key;        
        private byte[] _value;
        private string _cacheId;
        private string _clientId;

        internal CustomEvent(string cacheId, byte[] key, byte[] value,string clientid)
        {
            _key = key;
            _cacheId = cacheId;
            _value = value;
            _clientId = clientid;
        }

        public void Process()
        {
            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientId];

            if (clientManager != null)
            {
 

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();

                response.customEvent = Alachisoft.NCache.SocketServer.Util.EventHelper.GetCustomEventResponse(_key, _value);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.CUSTOM_EVENT;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.CUSTOM_EVENT);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);
            }
        }
    }
}