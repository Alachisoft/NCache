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
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;

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
            int overload;
            string exception = null;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

            try
            {
                overload = command.MethodOverload;
                stopWatch.Start();
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
                    , new CallbackInfo(clientManager.ClientID, cmdInfo.UpdateCallbackId, EventDataFilter.None) // DataFilter not required while unregistration
                    , new CallbackInfo(clientManager.ClientID, cmdInfo.RemoveCallbackId, EventDataFilter.None) // DataFilter not required while unregistration
                    , new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                stopWatch.Stop();
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.UnregisterBulkKeyNotifResponse unregResponse = new Common.Protobuf.UnregisterBulkKeyNotifResponse();
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.UNREGISTER_BULK_KEY_NOTIF;
                response.requestId = command.requestID;
                response.commandID = command.commandID;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc)));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.UnRegisterKeyNotificationCallback.ToLower());
                        log.GenerateKeyNotificationCallback(cmdInfo.Keys.Length, cmdInfo.UpdateCallbackId, cmdInfo.RemoveCallbackId, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
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