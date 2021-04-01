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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Runtime.Events;
using System.Collections.Generic;
using System.Collections;


namespace Alachisoft.NCache.SocketServer.CallbackTasks
{
    internal sealed class ItemRemoveCallback : ICallbackTask
    {
        private short _id;
        private string _key;
        private UserBinaryObject _value;
        private BitSet _flag;
        private ItemRemoveReason _reason;
        private EventContext _eventContext;
        private EventDataFilter _dataFilter = EventDataFilter.None;

        private string _clientID;

        internal ItemRemoveCallback(short id, string key, object value, ItemRemoveReason reason, string clientId, BitSet Flag, EventContext eventContext,EventDataFilter dataFilter)
        {
            _id = id;
            _key = key;
            _value = value as UserBinaryObject;
            _flag = Flag;
            _reason = reason;
            _clientID = clientId;
            _eventContext = eventContext;
            _dataFilter = dataFilter;
        }

        public void Process()
        {
            ClientManager clientManager = null;
            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientID];
            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();

                response.itemRemovedCallback = Alachisoft.NCache.SocketServer.Util.EventHelper.GetItemRemovedCallbackResponse(_eventContext,_id, _key, _value, _flag, _reason,_dataFilter);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.ITEM_REMOVED_CALLBACK;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.ITEM_REMOVED_CALLBACK);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);
                
                //ConnectionManager.AssureSend(clientManager, clientManager.ReplyPacket("REMOVECALLBACK \"" + _id + "\"" + _key + "\"" + ((int)_reason).ToString() + "\"" + _flag.Data.ToString() + "\"", _value.Size),_value.Data);
            }

        }
    }
}