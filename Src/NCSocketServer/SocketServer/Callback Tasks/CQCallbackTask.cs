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

using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching;

using Alachisoft.NCache.Runtime.Events;
using System.Collections;


namespace Alachisoft.NCache.SocketServer.CallbackTasks
{
    internal sealed class CQCallbackTask : ICallbackTask
    {
        private string _queryId;
        private string _key;
        private QueryChangeType _changeType;
        private EventContext _eventContext;
        private string _clientID;
        private EventDataFilter _datafilter;

        internal CQCallbackTask(string queryId, string key, QueryChangeType changeType, string clientId, EventContext eventContext, EventDataFilter datafilter)
        {
            _queryId = queryId;
            _key = key;
            _changeType = changeType;
            _clientID = clientId;
            _eventContext = eventContext;
            _datafilter = datafilter;
        }

        public void Process()
        {
            ClientManager clientManager = null;
            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientID];
            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();

                response.cQCallbackResponse = Alachisoft.NCache.SocketServer.Util.EventHelper.GetCQCallbackResponse(_eventContext, _key, _queryId, _changeType, _datafilter);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.CQ_CALLBACK;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);

                ConnectionManager.AssureSend(clientManager, serializedResponse, Alachisoft.NCache.Common.Enum.Priority.Low);
            }
        }
    }
}

