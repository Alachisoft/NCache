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
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetLCCommand : CommandBase
    {
        private struct CommandInfo
        {
            public long requestId;
            public int opCode;
            public int editionID;
        }
        private OperationResult _lcDataResult = OperationResult.Success;
        internal override OperationResult OperationResult
        {
            get
            {
                return _lcDataResult;
            }
        }
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            byte[] data = null;
            try
            {
                cmdInfo = ParseCommand(command, clientManager);
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.LCDataResponse lCDResponse = new Alachisoft.NCache.Common.Protobuf.LCDataResponse();
                lCDResponse.lcData = data;
                lCDResponse.opCode = cmdInfo.opCode;
                response.requestId =cmdInfo.requestId;
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.LC_DATA;
                response.LcDataResponse = lCDResponse;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));

            }
            catch (Exception exc)
            {
                _lcDataResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                return;
            }
 

        }
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetLCCommand getCommand = command.getLCCommand;
            cmdInfo.requestId = Convert.ToInt64(getCommand.requestId);
            cmdInfo.opCode = Convert.ToInt32(getCommand.opCode);
            cmdInfo.editionID =Convert.ToInt32(getCommand.editionId);
            return cmdInfo;
        }

    }

}
