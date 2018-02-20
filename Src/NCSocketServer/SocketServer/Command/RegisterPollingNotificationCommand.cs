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

using Alachisoft.NCache.Caching;
using System;

namespace Alachisoft.NCache.SocketServer.Command
{
    class RegisterPollingNotificationCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public short PollingCallbackId;
        }
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
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
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                }
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                OperationContext context = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                context.Add(OperationContextFieldName.ClientId, clientManager.ClientID);

                nCache.Cache.RegisterPollingNotification(cmdInfo.PollingCallbackId, context);

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.RegisterPollNotifResponse registerKeyNotifResponse = new Alachisoft.NCache.Common.Protobuf.RegisterPollNotifResponse();
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.REGISTER_POLL_NOTIF;
                response.registerPollNotifResponse = registerKeyNotifResponse;
                response.requestId = command.registerPollNotifCommand.requestId;
                response.commandID = command.commandID;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
        }
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.RegisterPollingNotificationCommand registerKeyNotifCommand = command.registerPollNotifCommand;
            cmdInfo.RequestId = registerKeyNotifCommand.requestId.ToString();
            cmdInfo.PollingCallbackId = (short)registerKeyNotifCommand.callbackId;

            return cmdInfo;
        }
    }
}
