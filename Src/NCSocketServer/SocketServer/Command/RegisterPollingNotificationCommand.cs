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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                    //_commandBytes = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                }
                return;
            }

            try
            {


                NCache nCache = clientManager.CmdExecuter as NCache;
                OperationContext context = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                context.Add(OperationContextFieldName.ClientId, clientManager.ClientID);
                CommandsUtil.PopulateClientIdInContext(ref context, clientManager.ClientAddress);
                nCache.Cache.RegisterPollingNotification(cmdInfo.PollingCallbackId, context);

                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.RegisterPollNotifResponse registerNotifResponse = new Alachisoft.NCache.Common.Protobuf.RegisterPollNotifResponse();

                if (clientManager.ClientVersion >= 5000)
                {
                    Common.Util.ResponseHelper.SetResponse(registerNotifResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(registerNotifResponse, Common.Protobuf.Response.Type.REGISTER_POLL_NOTIF));
                }
                else
                {
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    response.registerPollNotifResponse = registerNotifResponse;
                    Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.REGISTER_POLL_NOTIF);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }

            }
            catch (Exception exc)
            {
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
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
