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
using System.Collections;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.ResponseSerialization;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetHashmapCommand : CommandBase
    {
        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
#if !DEVELOPMENT

            try
            {
                int bucketSize = 0;
                byte[] buffer = new byte[0];

                NCache nCache = clientManager.CmdExecuter as NCache;
                NewHashmap hashmap = nCache.Cache.GetOwnerHashMap(out bucketSize);
               
                Common.Protobuf.Response response = new Common.Protobuf.Response();
                Common.Protobuf.GetHashmapResponse getHashmapResponse = new Common.Protobuf.GetHashmapResponse();

                response.responseType = Common.Protobuf.Response.Type.GET_HASHMAP;
                response.getHashmap = getHashmapResponse;
                response.requestId = command.requestID;
                response.commandID = command.commandID;

                if (hashmap != null)
                {
                    getHashmapResponse.viewId = hashmap.LastViewId;
                    getHashmapResponse.bucketSize = bucketSize;

                    foreach (string member in hashmap.Members)
                    {
                        getHashmapResponse.members.Add(member);
                    }

                    if (hashmap.ServerMapping != null)
                    {
                        foreach (DictionaryEntry entry in hashmap.ServerMapping)
                        {
                            Common.Protobuf.KeyValuePair serverMapped = new Common.Protobuf.KeyValuePair();
                            serverMapped.key = entry.Key.ToString();
                            serverMapped.value = entry.Value.ToString();

                            getHashmapResponse.serverMapping.Add(serverMapped);
                        }
                    }

                    foreach (DictionaryEntry entry in hashmap.Map)
                    {
                        Common.Protobuf.KeyValuePair keyValue = new Common.Protobuf.KeyValuePair();
                        keyValue.key = entry.Key.ToString();
                        keyValue.value = entry.Value.ToString();

                        getHashmapResponse.keyValuePair.Add(keyValue);
                    }
                }

                ResponseOptions responseOptions = new ResponseOptions()
                {
                    Response = response,
                    ResponseType = Common.Protobuf.Response.Type.GET_HASHMAP
                };
                _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildResponse(responseOptions));

            }
            catch (Exception exc)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("GetHashmapCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " : "+exc.ToString());

                ResponseOptions responseOptions = new ResponseOptions()
                {
                    Exception = exc,
                    RequestId = command.requestID,
                    CommandId = command.commandID,
                };

                _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildExceptionResponse(responseOptions));
            }
#endif
        }
    }
}
