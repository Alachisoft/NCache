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
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Runtime.Events;
using System.Collections.Generic;
using System.Collections;


namespace Alachisoft.NCache.SocketServer.CallbackTasks
{
    internal sealed class PollRequestCallback : ICallbackTask
    {
        private int _callbackId;
        private string _clientId;
        private readonly EventTypeInternal _eventType;


        internal PollRequestCallback(string clientId, int callbackId, EventTypeInternal eventType)
        {
            _clientId = clientId;
            _callbackId = callbackId;
            _eventType = eventType;
        }

        public void Process()
        {
            ClientManager clientManager = null;
            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientId];
            if (clientManager != null)
            {
                Common.Protobuf.Response response = new Common.Protobuf.Response();
                response.pollNotifyEventResponse = new Common.Protobuf.PollNotifyEventResponse();
                response.pollNotifyEventResponse.callbackId = _callbackId;
                response.pollNotifyEventResponse.eventType = (int)_eventType;
                response.responseType = Common.Protobuf.Response.Type.POLL_NOTIFY_CALLBACK;
                IList serializedResponse = Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.POLL_NOTIFY_CALLBACK);
                ConnectionManager.AssureSend(clientManager, serializedResponse, false);
            }
        }
    }
}
