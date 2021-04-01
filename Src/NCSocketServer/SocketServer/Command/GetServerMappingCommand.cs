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
using Alachisoft.NCache.Config;
namespace Alachisoft.NCache.SocketServer.Command
{
    class GetServerMappingCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
#if !DEVELOPMENT
            try
            {

                ServerMapping serverMapping = Management.MappingConfiguration.MappingConfigurationManager.GetMappingConfiguration().ClientIPMapping;
                Mapping[] mappedServers = serverMapping.MappingServers;
                
                Common.Protobuf.Response response = new Common.Protobuf.Response();
                Common.Protobuf.GetServerMappingResponse getServerMappingResponse = new Common.Protobuf.GetServerMappingResponse();

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
                response.responseType = Common.Protobuf.Response.Type.GET_SERVER_MAPPING;
                response.requestId = command.requestID;
                response.commandID = command.commandID;
               
                _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(response));
                              
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
            }
#endif
        }
    }
}
