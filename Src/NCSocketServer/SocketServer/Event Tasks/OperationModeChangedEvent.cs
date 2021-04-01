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

using System.Collections;

namespace Alachisoft.NCache.SocketServer.EventTask
{
    /// <summary>
    /// This event is fired when an OperationM Mode Changed 
    /// </summary>
    internal sealed class OperationModeChangedEvent : IEventTask
    {
        private readonly string _clientId;

        internal OperationModeChangedEvent(string clientId)
        {
            _clientId = clientId;
         
        }

        public void Process()
        {
            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable)
                clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientId];

            if (clientManager != null)
            {
                Common.Protobuf.Response response = new Common.Protobuf.Response();

                Common.Protobuf.OperationModeChangeEventResponse operationModeResponse = new Common.Protobuf.OperationModeChangeEventResponse();
                operationModeResponse.serverIP = clientManager.ClientIP.ToString();
              
                response.operationModeChangeEventResponse = operationModeResponse;
                response.responseType = Common.Protobuf.Response.Type.OPERATIONCHANGEDEVNET;

                IList serializedResponse = Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.OPERATIONCHANGEDEVNET);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);
            }


        }
    }
}
