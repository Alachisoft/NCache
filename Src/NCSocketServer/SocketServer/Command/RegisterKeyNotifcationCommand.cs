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
    class RegisterKeyNotifcationCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public short RemoveCallbackId;
            public short UpdateCallbackId;
            public bool  NotifyOnExpiration;
            public int callbackType;

            public int dataFilter;
        }

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
                if (!base.immatureId.Equals("-2"))
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                }
                return;
            }

            try
            {
                CallbackInfo cbUpdate = null;
                CallbackInfo cbRemove = null;

                CallbackType callbackType = (CallbackType)cmdInfo.callbackType;

                if(cmdInfo.dataFilter != -1) // Default value in protbuf set to -1
                {
                    EventDataFilter datafilter = (EventDataFilter)cmdInfo.dataFilter;

                    cbUpdate = new CallbackInfo(clientManager.ClientID, cmdInfo.UpdateCallbackId, datafilter, callbackType);
                    cbRemove = new CallbackInfo(clientManager.ClientID, cmdInfo.RemoveCallbackId, datafilter, cmdInfo.NotifyOnExpiration, callbackType);
                }
                else
                {
                    cbUpdate = new CallbackInfo(clientManager.ClientID, cmdInfo.UpdateCallbackId, EventDataFilter.None, callbackType);
                    cbRemove = new CallbackInfo(clientManager.ClientID, cmdInfo.RemoveCallbackId, EventDataFilter.DataWithMetadata, cmdInfo.NotifyOnExpiration, callbackType);
                }


                NCache nCache = clientManager.CmdExecuter as NCache;
                OperationContext context = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                context.Add(OperationContextFieldName.ClientId, clientManager.ClientID);

                nCache.Cache.RegisterKeyNotificationCallback(cmdInfo.Key, cbUpdate, cbRemove
                  , context);

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.RegisterKeyNotifResponse registerKeyNotifResponse = new Alachisoft.NCache.Common.Protobuf.RegisterKeyNotifResponse();
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.REGISTER_KEY_NOTIF;
                response.registerKeyNotifResponse = registerKeyNotifResponse;
                response.requestId = command.registerKeyNotifCommand.requestId;
                response.commandID = command.commandID;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                exception = exc.ToString();

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.RegisterKeyNotificationCallback.ToLower());
                        log.GenerateKeyNotificationCallback(1, cmdInfo.UpdateCallbackId, cmdInfo.RemoveCallbackId, 1, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }

        }

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
            cmdInfo.callbackType = registerKeyNotifCommand.callbackType;

            return cmdInfo;
        }
    }
}
