// Copyright (c) 2015 Alachisoft
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
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.SocketServer.Command
{
    class RegisterKeyNotifcationCommand : CommandBase
    {

        protected struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public short RemoveCallbackId;
            public short UpdateCallbackId;
            public bool  NotifyOnExpiration;

            public int dataFilter;
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

            try
            {
                CallbackInfo cbUpdate = null;
                CallbackInfo cbRemove = null;


                if(cmdInfo.dataFilter != -1) //Default value in protbuf set to -1
                {
                    EventDataFilter datafilter = (EventDataFilter)cmdInfo.dataFilter;

                    cbUpdate = new CallbackInfo(clientManager.ClientID, cmdInfo.UpdateCallbackId, datafilter);
                    cbRemove = new CallbackInfo(clientManager.ClientID, cmdInfo.RemoveCallbackId, datafilter, cmdInfo.NotifyOnExpiration);
                }
                else
                {
                    cbUpdate = new CallbackInfo(clientManager.ClientID, cmdInfo.UpdateCallbackId, EventDataFilter.None);
                    cbRemove = new CallbackInfo(clientManager.ClientID, cmdInfo.RemoveCallbackId,EventDataFilter.DataWithMetadata, cmdInfo.NotifyOnExpiration);
                }


                NCache nCache = clientManager.CmdExecuter as NCache;

                nCache.Cache.RegisterKeyNotificationCallback(cmdInfo.Key, cbUpdate, cbRemove
                  , new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.RegisterKeyNotifResponse registerKeyNotifResponse = new Alachisoft.NCache.Common.Protobuf.RegisterKeyNotifResponse();
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.REGISTER_KEY_NOTIF;
                response.registerKeyNotifResponse = registerKeyNotifResponse;
                response.requestId = command.registerKeyNotifCommand.requestId;
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
            cmdInfo.NotifyOnExpiration = true;
                 
            Alachisoft.NCache.Common.Protobuf.RegisterKeyNotifCommand registerKeyNotifCommand = command.registerKeyNotifCommand;
            cmdInfo.Key = registerKeyNotifCommand.key;
            cmdInfo.RemoveCallbackId = (short)registerKeyNotifCommand.removeCallbackId;
            cmdInfo.RequestId = registerKeyNotifCommand.requestId.ToString();
            cmdInfo.UpdateCallbackId = (short)registerKeyNotifCommand.updateCallbackId;
            cmdInfo.NotifyOnExpiration = registerKeyNotifCommand.notifyOnExpiration;

            cmdInfo.dataFilter = registerKeyNotifCommand.datafilter;

            return cmdInfo;
        }      
    }
}
