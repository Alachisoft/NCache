// Copyright (c) 2018 Alachisoft
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

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Runtime.Events;

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
               Common.Protobuf.Response response = new Common.Protobuf.Response();

                response.itemRemovedCallback = Util.EventHelper.GetItemRemovedCallbackResponse(_eventContext,_id, _key, _value, _flag, _reason,_dataFilter);
                response.responseType = Common.Protobuf.Response.Type.ITEM_REMOVED_CALLBACK;

                byte[] serializedResponse = Common.Util.ResponseHelper.SerializeResponse(response);

                ConnectionManager.AssureSend(clientManager, serializedResponse,Alachisoft.NCache.Common.Enum.Priority.Low);
            }

        }
    }
}
