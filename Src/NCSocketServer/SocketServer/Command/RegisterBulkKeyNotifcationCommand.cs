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
    class RegisterBulkKeyNotifcationCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public int PackageSize;

            public string RequestId;
            public string[] Keys;
            public short RemoveCallbackId;
            public short UpdateCallbackId;
            public bool NotifyOnExpiration;
            public int callbackType;

            public int dataFilter;

            public string SurrogateClientID;
        }

        private CallbackType CallbackType(int type)
        {
            if (type == 0)
                return Runtime.Events.CallbackType.PullBasedCallback;
            else if (type == 1)
                return Runtime.Events.CallbackType.PushBasedNotification;
            else
                return Runtime.Events.CallbackType.PullBasedCallback;
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

                CallbackType callbackType = CallbackType(cmdInfo.callbackType);

                if (cmdInfo.dataFilter != -1) // Default value in protbuf set to -1
                {
                    EventDataFilter datafilter = (EventDataFilter)cmdInfo.dataFilter;

                    cbUpdate = new CallbackInfo(!string.IsNullOrEmpty(cmdInfo.SurrogateClientID) ? cmdInfo.SurrogateClientID : clientManager.ClientID, cmdInfo.UpdateCallbackId, datafilter, callbackType);
                    cbRemove = new CallbackInfo(!string.IsNullOrEmpty(cmdInfo.SurrogateClientID) ? cmdInfo.SurrogateClientID : clientManager.ClientID, cmdInfo.RemoveCallbackId, datafilter, cmdInfo.NotifyOnExpiration, callbackType);
                }
                else
                {
                    cbUpdate = new CallbackInfo(!string.IsNullOrEmpty(cmdInfo.SurrogateClientID) ? cmdInfo.SurrogateClientID : clientManager.ClientID, cmdInfo.UpdateCallbackId, EventDataFilter.None, callbackType);
                    cbRemove = new CallbackInfo(!string.IsNullOrEmpty(cmdInfo.SurrogateClientID) ? cmdInfo.SurrogateClientID : clientManager.ClientID, cmdInfo.RemoveCallbackId, EventDataFilter.DataWithMetadata, cmdInfo.NotifyOnExpiration, callbackType);
                }

                NCache nCache = clientManager.CmdExecuter as NCache;

                OperationContext context = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                context.Add(OperationContextFieldName.ClientId, !string.IsNullOrEmpty(cmdInfo.SurrogateClientID) ? cmdInfo.SurrogateClientID : clientManager.ClientID);

                nCache.Cache.RegisterKeyNotificationCallback(cmdInfo.Keys,
                    cbUpdate, cbRemove, context);
                stopWatch.Stop();

                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.RegisterBulkKeyNotifResponse registerBulkKeyNotifResponse = new Alachisoft.NCache.Common.Protobuf.RegisterBulkKeyNotifResponse();
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.REGISTER_BULK_KEY_NOTIF;
                response.registerBulkKeyNotifResponse = registerBulkKeyNotifResponse;
                response.requestId = command.registerBulkKeyNotifCommand.requestId;
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
            Alachisoft.NCache.Common.Protobuf.RegisterBulkKeyNotifCommand registerBulkKeyNotifCommand = command.registerBulkKeyNotifCommand;
            cmdInfo.Keys = registerBulkKeyNotifCommand.keys.ToArray();
            cmdInfo.PackageSize = registerBulkKeyNotifCommand.keys.Count;
            cmdInfo.RemoveCallbackId = (short)registerBulkKeyNotifCommand.removeCallbackId;
            cmdInfo.RequestId = registerBulkKeyNotifCommand.requestId.ToString();
            cmdInfo.UpdateCallbackId = (short)registerBulkKeyNotifCommand.updateCallbackId;
            cmdInfo.dataFilter = registerBulkKeyNotifCommand.datafilter;
            cmdInfo.NotifyOnExpiration = registerBulkKeyNotifCommand.notifyOnExpiration;
            cmdInfo.callbackType = registerBulkKeyNotifCommand.callbackType;
            cmdInfo.SurrogateClientID = registerBulkKeyNotifCommand.surrogateClientID;
            return cmdInfo;
        }
    }
}