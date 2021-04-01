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
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetTypeInfoMap : CommandBase
    {

        protected struct CommandInfo
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
                    //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                return;
            }

            try
            {
                string typeInfoMap = "";
                NCache nCache = clientManager.CmdExecuter as NCache;
                
                if (nCache.Cache.GetTypeInfoMap() != null)
                    typeInfoMap = nCache.Cache.GetTypeInfoMap().ToProtocolString();

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.GetTypeMapResponse getTypeMapResponse = new Alachisoft.NCache.Common.Protobuf.GetTypeMapResponse();
                getTypeMapResponse.map = typeInfoMap;
                response.getTypemap = getTypeMapResponse;
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);

                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_TYPEINFO_MAP;

                if (clientManager.ClientVersion >= 5000)
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response, Common.Protobuf.Response.Type.GET_TYPEINFO_MAP));

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

     

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetTypeInfoMapCommand getTypeInfoMapCommand = command.getTypeInfoMapCommand;

            cmdInfo.RequestId = getTypeInfoMapCommand.requestId.ToString();

            return cmdInfo;
        }

       
    }
}
