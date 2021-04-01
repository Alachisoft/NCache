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
using System.Collections;
using Alachisoft.NCache.Common.DataStructures;
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

                    foreach (DictionaryEntry entry in hashmap.Map)
                    {
                        Common.Protobuf.KeyValuePair keyValue = new Common.Protobuf.KeyValuePair();
                        keyValue.key = entry.Key.ToString();
                        keyValue.value = entry.Value.ToString();

                        //nCache.Cache.NCacheLog.CriticalInfo("GetHashmapCommand", string.Format("Bucket id : {0} , Server : {1}", keyValue.key, keyValue.value));

                        getHashmapResponse.keyValuePair.Add(keyValue);
                    }
                }

                if (clientManager.ClientVersion >= 5000)
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response, Common.Protobuf.Response.Type.GET_HASHMAP));
                }
                else
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }

            }
            catch (Exception exc)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("GetHashmapCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " : "+exc.ToString());

                //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
#endif
        }
    }
}
