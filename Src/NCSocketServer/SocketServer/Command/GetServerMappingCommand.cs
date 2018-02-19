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

using Alachisoft.NCache.Config;
namespace Alachisoft.NCache.SocketServer.Command
{
    class GetServerMappingCommand : CommandBase
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
                //Fetch mapped servers

                ServerMapping serverMapping = Management.MappingConfiguration.MappingConfigurationManager.GetMappingConfiguration().ClientIPMapping;
                Mapping[] mappedServers = serverMapping.MappingServers;
                
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.GetServerMappingResponse getServerMappingResponse = new Alachisoft.NCache.Common.Protobuf.GetServerMappingResponse();

                if (mappedServers != null)
                {
                    for (int i = 0; i < mappedServers.Length; i++)
                    {
                        Common.Protobuf.ServerMapping mappingObject = new Common.Protobuf.ServerMapping();
                        //Map the server list to protobuf object
                        mappingObject.privateIp = mappedServers[i].PrivateIP;
                        mappingObject.privatePort = mappedServers[i].PrivatePort;
                        mappingObject.publicIp = mappedServers[i].PublicIP;
                        mappingObject.publicPort = mappedServers[i].PublicPort;

                        //Adding to list to be sent as a response
                        getServerMappingResponse.serverMapping.Add(mappingObject);

                    }
                }
                else
                    SocketServer.Logger.NCacheLog.Error("Server Mapping is null");

                response.getServerMappingResponse = getServerMappingResponse;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_SERVER_MAPPING;
                response.requestId = command.requestID;
               
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                              
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }

        }


        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetServerMappingCommand getServerMappingCommand = command.getServerMappingCommand;
            cmdInfo.RequestId = getServerMappingCommand.requestId.ToString();

            return cmdInfo;
        }
    }
}
