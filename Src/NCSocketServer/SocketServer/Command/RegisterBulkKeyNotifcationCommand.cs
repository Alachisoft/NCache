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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.SocketServer.Util;

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
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                    //   _commandBytes = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
                }
                return;
            }

            try
            {

                CallbackInfo cbUpdate = null;
                CallbackInfo cbRemove = null;

                CallbackType callbackType = CallbackType(cmdInfo.callbackType);

                if (cmdInfo.dataFilter != -1) //Default value in protbuf set to -1
                {
                    EventDataFilter datafilter = (EventDataFilter)cmdInfo.dataFilter;

                    cbUpdate = new CallbackInfo(!string.IsNullOrEmpty(cmdInfo.SurrogateClientID) ? cmdInfo.SurrogateClientID : clientManager.ClientID, cmdInfo.UpdateCallbackId, datafilter, callbackType);
                    cbRemove = new CallbackInfo(!string.IsNullOrEmpty(cmdInfo.SurrogateClientID) ? cmdInfo.SurrogateClientID : clientManager.ClientID, cmdInfo.RemoveCallbackId, datafilter, cmdInfo.NotifyOnExpiration, callbackType);
                }
                else
                {
                    cbUpdate = new CallbackInfo(!string.IsNullOrEmpty(cmdInfo.SurrogateClientID) ? cmdInfo.SurrogateClientID : clientManager.ClientID, cmdInfo.UpdateCallbackId, EventDataFilter.None, callbackType);
                    cbRemove = new CallbackInfo(!string.IsNullOrEmpty(cmdInfo.SurrogateClientID) ? cmdInfo.SurrogateClientID : clientManager.ClientID, cmdInfo.RemoveCallbackId, EventDataFilter.None, cmdInfo.NotifyOnExpiration, callbackType);
                }

                NCache nCache = clientManager.CmdExecuter as NCache;

                OperationContext context = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                context.Add(OperationContextFieldName.ClientId, !string.IsNullOrEmpty(cmdInfo.SurrogateClientID) ? cmdInfo.SurrogateClientID : clientManager.ClientID);
                context.Add(OperationContextFieldName.ClientOperationTimeout, clientManager.RequestTimeout);
                context.CancellationToken = CancellationToken;
                CommandsUtil.PopulateClientIdInContext(ref context, clientManager.ClientAddress);
                nCache.Cache.RegisterKeyNotificationCallback(cmdInfo.Keys,
                    cbUpdate, cbRemove, context);
                if (clientManager.ClientVersion < 5000 && !clientManager.CreateEventSubscription)
                {
                    Util.EventHelper.SubscribeEvents(clientManager.ClientID, TopicConstant.ItemLevelEventsTopic, nCache, context);
                    clientManager.CreateEventSubscription = true;
                }

                stopWatch.Stop();

                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.RegisterBulkKeyNotifResponse registerBulkKeyNotifResponse = new Alachisoft.NCache.Common.Protobuf.RegisterBulkKeyNotifResponse();


                if (clientManager.ClientVersion >= 5000)
                {
                    Common.Util.ResponseHelper.SetResponse(registerBulkKeyNotifResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(registerBulkKeyNotifResponse, Common.Protobuf.Response.Type.REGISTER_BULK_KEY_NOTIF));
                }
                else
                {
                    //PROTOBUF:RESPONSE
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    response.registerBulkKeyNotifResponse = registerBulkKeyNotifResponse;
                    Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.REGISTER_BULK_KEY_NOTIF);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (OperationCanceledException ex)
            {
                exception = ex.ToString();
                Dispose();

            }
            catch (Exception exc)
            {
                exception = exc.ToString();

                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
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
            //HACK:notifMask
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