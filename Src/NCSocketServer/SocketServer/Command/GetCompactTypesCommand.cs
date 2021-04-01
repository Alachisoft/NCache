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
using Alachisoft.NCache.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetCompactTypesCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public bool isDotNetClient;
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            //TODO
            byte[] data = null;

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2")) 
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                return;
            }

            data = null;
            NCache nCache = clientManager.CmdExecuter as NCache;
            
            try
            {
                string compactTypesString = "";
                
                if (nCache.Cache.CompactRegisteredTypesForDotNet != null && cmdInfo.isDotNetClient)
                    compactTypesString = SerializationUtil.GetProtocolStringFromTypeMap(nCache.Cache.CompactRegisteredTypesForDotNet);
                else if (nCache.Cache.CompactRegisteredTypesForJava != null && !cmdInfo.isDotNetClient)
                    compactTypesString = SerializationUtil.GetProtocolStringFromTypeMap(nCache.Cache.CompactRegisteredTypesForJava);


                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.GetCompactTypeResponse getCompactTypesResponse = new Alachisoft.NCache.Common.Protobuf.GetCompactTypeResponse();
                getCompactTypesResponse.compactTypeString = compactTypesString;
                response.getCompactTypes = getCompactTypesResponse;
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_COMPACT_TYPES;

                //PROTOBUF:RESPONSE
                if (clientManager.ClientVersion >= 5000)
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response, Common.Protobuf.Response.Type.GET_COMPACT_TYPES));
                }
                else
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
        }

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetCompactTypesCommand getCompactTypesCommand = command.getCompactTypesCommand;

            cmdInfo.RequestId = getCompactTypesCommand.requestId.ToString();
            cmdInfo.isDotNetClient = getCompactTypesCommand.isDotnetClient;

            return cmdInfo;
        }
    }
}
