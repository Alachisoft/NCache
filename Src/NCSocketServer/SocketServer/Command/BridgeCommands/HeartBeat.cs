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
using Alachisoft.NCache.SocketServer.EventTask;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.Command
{
    /// <summary>
    /// This heart beat is send to the client when a client is idle for too long
    /// and this hearbeat is used to verify whethercclient is still up or down.
    /// </summary>
    class HeartBeat :IEventTask
    {
        private string _clientId;

        /// <summary>
        /// Constructor, sets the client id
        /// </summary>
        /// <param name="clientId"></param>
        public HeartBeat(string clientId)
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
                Alachisoft.NCache.Common.Protobuf.HeartBeatResponse heartBeatResponse = new Alachisoft.NCache.Common.Protobuf.HeartBeatResponse();

                response.heartBeatResponse = heartBeatResponse;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.HEART_BEAT;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);

                //ConnectionManager.AssureSend(clientManager, clientManager.ReplyPacket("QUEUEFULLNOTIF \"", new byte[0]));
            }

        }

        #endregion
    }
}
