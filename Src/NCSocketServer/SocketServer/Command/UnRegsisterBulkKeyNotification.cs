// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.SocketServer.Command
{
    class UnRegsisterBulkKeyNotification : CommandBase
    {
        protected struct CommandInfo
        {
            public int PackageSize;

            public string RequestId;
            public string[] Keys;
            public short RemoveCallbackId;
            public short UpdateCallbackId;
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
                if (!base.immatureId.Equals("-2")) _serializedResponsePackets.Add(clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc)));
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                nCache.Cache.UnregisterKeyNotificationCallback(cmdInfo.Keys
                    , new CallbackInfo(clientManager.ClientID, cmdInfo.UpdateCallbackId, EventDataFilter.None) //DataFilter not required while unregistration
                    , new CallbackInfo(clientManager.ClientID, cmdInfo.RemoveCallbackId, EventDataFilter.None) //DataFilter not required while unregistration
                    , new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.UnregisterBulkKeyNotifResponse unregResponse = new Common.Protobuf.UnregisterBulkKeyNotifResponse();
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.UNREGISTER_BULK_KEY_NOTIF;
                response.requestId = command.requestID;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc)));
            }
            
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.UnRegisterBulkKeyNotifCommand unRegisterBulkKeyNotifCommand = command.unRegisterBulkKeyNotifCommand;

            cmdInfo.Keys = unRegisterBulkKeyNotifCommand.keys.ToArray();
            cmdInfo.RemoveCallbackId = (short)unRegisterBulkKeyNotifCommand.removeCallbackId;
            cmdInfo.RequestId = unRegisterBulkKeyNotifCommand.requestId.ToString();
            cmdInfo.UpdateCallbackId = (short)unRegisterBulkKeyNotifCommand.updateCallbackId;

            return cmdInfo;
        }
    }
}