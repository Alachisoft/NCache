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
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.EventTask
{
    /// <summary>
    /// This event is fired when queue is full
    /// </summary>
    class QueueFullEvent : IEventTask
    {
        private string _clientId;

        /// <summary>
        /// Constructor, sets the client id
        /// </summary>
        /// <param name="clientId"></param>
        public QueueFullEvent(string clientId)
        {
            _clientId = clientId;
        }
        #region IEventTask Members

        public void Process()
        {
            ClientManager clientManager;
            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientId];
            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.QueueFullEventResponse queueFullEvent = new Alachisoft.NCache.Common.Protobuf.QueueFullEventResponse();

                response.queueFullEvent = queueFullEvent;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.QUEUE_FULL_EVENT;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);

            }

        }

        #endregion
    }
}
