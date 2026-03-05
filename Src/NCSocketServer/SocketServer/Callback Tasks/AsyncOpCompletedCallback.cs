//  Copyright (c) 2026 Alachisoft
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

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.Protobuf;
using System.Collections.Generic;
using System.Collections;
using Alachisoft.NCache.Common.ResponseSerialization;

namespace Alachisoft.NCache.SocketServer.CallbackTasks
{
    internal sealed class AsyncOpCompletedCallback : ICallbackTask
    {
        private object _opCode;
        private object _result;
        private string _cacheContext;

        internal AsyncOpCompletedCallback(object opCode, object result, string cacheContext)
        {
            _opCode = opCode;
            _result = result;
            _cacheContext = cacheContext;
        }

        public void Process()
        {
            object[] package = null;
            package = (object[])SerializationUtil.CompactDeserialize(_result, _cacheContext);

            string key = (string)package[0];
            AsyncCallbackInfo cbInfo = (AsyncCallbackInfo)package[1];
            object opResult = package[2];
          
            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable) 
                clientManager = (ClientManager)ConnectionManager.ConnectionTable[cbInfo.Client];

            if (clientManager != null)
            {
                Response response = new Response
                {
                    requestId = cbInfo.RequestID,
                    asyncOpCompletedCallback = Util.EventHelper.GetAsyncOpCompletedResponse(clientManager, cbInfo, opResult, _opCode, key),
                    responseType = Response.Type.ASYNC_OP_COMPLETED_CALLBACK
                };

                var configValues = new ResponseOptions()
                {
                    Response = response,
                    ResponseType = Response.Type.ASYNC_OP_COMPLETED_CALLBACK
                };

                IList serializedResponse = clientManager.ResponseBuilder.BuildResponse(configValues);

                ConnectionManager.AssureSend(clientManager, serializedResponse, false);
            }
        }
    }
}
