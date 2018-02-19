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

using System;
using System.Collections;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.SocketServer.Util;
using Alachisoft.NCache.Web.Util;
using Alachisoft.NCache.Common.DataStructures;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetHashmapCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2")) 
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                int bucketSize = 0;
                NewHashmap hashmap = nCache.Cache.GetOwnerHashMap(out bucketSize);
                byte[] buffer = new byte[0];                              

                if (!nCache.IsDotnetClient)
                {
                    if (hashmap != null)
                    {
                    }
                }

                //TODO:Incomplete conversion
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.GetHashmapResponse getHashmapResponse = new Alachisoft.NCache.Common.Protobuf.GetHashmapResponse();
                
                response.getHashmap = getHashmapResponse;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_HASHMAP;
                response.requestId = command.requestID;

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
                        Alachisoft.NCache.Common.Protobuf.KeyValuePair keyValue =
                            new Alachisoft.NCache.Common.Protobuf.KeyValuePair();
                        keyValue.key = entry.Key.ToString();
                        keyValue.value = entry.Value.ToString();

                        getHashmapResponse.keyValuePair.Add(keyValue);
                    }
                }
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetHashmapCommand getHashmapCommand = command.getHashmapCommand;
            cmdInfo.RequestId = getHashmapCommand.requestId.ToString();

            return cmdInfo;
        }
    }
}
