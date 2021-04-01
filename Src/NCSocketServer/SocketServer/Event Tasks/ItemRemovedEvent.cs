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

using Alachisoft.NCache.Common;

using Alachisoft.NCache.Caching;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Caching;

namespace Alachisoft.NCache.SocketServer.EventTask
{
    /// <summary>
    /// This event is fired when an item is removed from cache
    /// </summary>
    internal sealed class ItemRemovedEvent : IEventTask
    {
        private string _key;
        private string _cacheId;
        private ItemRemoveReason _reason;
        private UserBinaryObject _value;
        private string _clientId;
        private BitSet _flag;
        private EventContext _eventContext;

        internal ItemRemovedEvent(string key, string cacheId, ItemRemoveReason reason, UserBinaryObject value, string clientId, BitSet Flag, EventContext eventContext)
        {
            _key = key;
            _cacheId = cacheId;
            _reason = reason;
            _value = value;
            _clientId = clientId;
            _flag = Flag;
            _eventContext = eventContext;
        }

        public void Process()
        {
            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientId];
            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                response.itemRemoved = Alachisoft.NCache.SocketServer.Util.EventHelper.GetItemRemovedEventResponse(_eventContext, _key, null, _flag, _reason, _value);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.ITEM_REMOVED_EVENT;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.ITEM_REMOVED_EVENT);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);

            }
        }
    }
}
