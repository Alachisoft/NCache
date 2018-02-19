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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Events;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.SocketServer.Command
{
    class ClearCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public BitSet FlagMap;
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
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                }
                return;
            }
            NCache nCache = clientManager.CmdExecuter as NCache;
                try
                {
                    CallbackEntry cbEntry = null;
                    OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                    nCache.Cache.Clear(cmdInfo.FlagMap, cbEntry, operationContext);

                    Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                    Alachisoft.NCache.Common.Protobuf.ClearResponse clearResponse = new Alachisoft.NCache.Common.Protobuf.ClearResponse();
                    response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                    response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.CLEAR;
                    response.clearResponse = clearResponse;

                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
                catch (Exception exc)
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                }
            }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.ClearCommand clearCommand= command.clearCommand;
            cmdInfo.FlagMap = new BitSet((byte)clearCommand.flag);
            cmdInfo.RequestId = clearCommand.requestId.ToString();

            return cmdInfo;
        }
    }
}
