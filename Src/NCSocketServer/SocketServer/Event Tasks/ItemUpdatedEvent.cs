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
using System.Collections;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Runtime.Events;
using System.Collections.Generic;


namespace Alachisoft.NCache.SocketServer.EventTask
{
    /// <summary>
    /// This event is fired when an item is updated in cache
    /// </summary>
    internal sealed class ItemUpdatedEvent : IEventTask
    {
        private string _key;
        private string _cacheId;
        private string _clientId;
        private EventContext _eventContext;

        internal ItemUpdatedEvent(string key, string cacheId, string clientId, EventContext eventContext)
        {
            _key = key;
            _cacheId = cacheId;
            _clientId = clientId;
            _eventContext = eventContext;
        }

        public void Process()
        {
            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientId];

            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                response.itemUpdated = Alachisoft.NCache.SocketServer.Util.EventHelper.GetItemUpdatedEventResponse(_eventContext, _key, null);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.ITEM_UPDATED_EVENT;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.ITEM_UPDATED_EVENT);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);
            }
        }
    }
}
