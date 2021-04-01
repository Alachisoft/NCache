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
using System.Collections.Generic;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using System.Globalization;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class RegisterKeyNotifcationCommand : CommandBase
    {
        private bool subscribed;

        protected struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public short RemoveCallbackId;
            public short UpdateCallbackId;
            public bool  NotifyOnExpiration;
            public int callbackType;

            public int dataFilter;
            public long clientLastViewId;
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
                CallbackInfo cbUpdate = null;
                CallbackInfo cbRemove = null;

                CallbackType callbackType = (CallbackType)cmdInfo.callbackType;

                if (cmdInfo.dataFilter != -1) //Default value in protbuf set to -1
                {
                    EventDataFilter datafilter = (EventDataFilter)cmdInfo.dataFilter;

                    cbUpdate = new CallbackInfo(clientManager.ClientID, cmdInfo.UpdateCallbackId, datafilter, callbackType);
                    cbRemove = new CallbackInfo(clientManager.ClientID, cmdInfo.RemoveCallbackId, datafilter, cmdInfo.NotifyOnExpiration, callbackType);

                }
                else
                {
                    cbUpdate = new CallbackInfo(clientManager.ClientID, cmdInfo.UpdateCallbackId, EventDataFilter.None, callbackType);
                    cbRemove = new CallbackInfo(clientManager.ClientID, cmdInfo.RemoveCallbackId, EventDataFilter.None, cmdInfo.NotifyOnExpiration, callbackType);

                }


                NCache nCache = clientManager.CmdExecuter as NCache;
                OperationContext context = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                context.Add(OperationContextFieldName.ClientId, clientManager.ClientID);

                if (command.commandVersion < 1)
                {
                    context.Add(OperationContextFieldName.ClientLastViewId, forcedViewId);
                }
                else //NCache 4.1 SP1 or later
                {
                    context.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.clientLastViewId.ToString(CultureInfo.InvariantCulture));
                }
                CommandsUtil.PopulateClientIdInContext(ref context, clientManager.ClientAddress);
                nCache.Cache.RegisterKeyNotificationCallback(cmdInfo.Key, cbUpdate, cbRemove
                  , context);
                stopWatch.Stop();
                if (clientManager.ClientVersion < 5000 && !clientManager.CreateEventSubscription)
                {
                    Util.EventHelper.SubscribeEvents(clientManager.ClientID, TopicConstant.ItemLevelEventsTopic, nCache, context);
                    clientManager.CreateEventSubscription = true;
                }

                //PROTOBUF:RESPONSE
               // Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.RegisterKeyNotifResponse registerKeyNotifResponse = new Alachisoft.NCache.Common.Protobuf.RegisterKeyNotifResponse();
                //response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.REGISTER_KEY_NOTIF;
                //response.registerKeyNotifResponse = registerKeyNotifResponse;
                //response.requestId = command.registerKeyNotifCommand.requestId;
                //response.commandID = command.commandID;
                //_serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.REGISTER_KEY_NOTIF));

                if (clientManager.ClientVersion >= 5000)
                {
                    Common.Util.ResponseHelper.SetResponse(registerKeyNotifResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(registerKeyNotifResponse, Common.Protobuf.Response.Type.REGISTER_KEY_NOTIF));
                }
                else
                {
                    //PROTOBUF:RESPONSE
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    response.registerKeyNotifResponse = registerKeyNotifResponse;
                    Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.REGISTER_KEY_NOTIF);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }

                //_commandBytes = clientManager.ReplyPacket("REGKEYNOTIFRESULT \"" + cmdInfo.RequestId + "\"", new byte[0]);
            }
            catch (Exception exc)
            {
                exception = exc.ToString();

                //_commandBytes = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
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
                        log.GenerateKeyNotificationCallback(1, cmdInfo.UpdateCallbackId, cmdInfo.RemoveCallbackId, 1, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());



                        // Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpirationHint, cmdInfo.queryInfo);
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
            cmdInfo.NotifyOnExpiration = true;
                 
            Alachisoft.NCache.Common.Protobuf.RegisterKeyNotifCommand registerKeyNotifCommand = command.registerKeyNotifCommand;
            cmdInfo.Key = registerKeyNotifCommand.key;
            cmdInfo.RemoveCallbackId = (short)registerKeyNotifCommand.removeCallbackId;
            cmdInfo.RequestId = registerKeyNotifCommand.requestId.ToString();
            cmdInfo.UpdateCallbackId = (short)registerKeyNotifCommand.updateCallbackId;
            cmdInfo.NotifyOnExpiration = registerKeyNotifCommand.notifyOnExpiration;

            cmdInfo.dataFilter = registerKeyNotifCommand.datafilter;
            cmdInfo.callbackType = registerKeyNotifCommand.callbackType;

            cmdInfo.clientLastViewId = command.clientLastViewId;

            return cmdInfo;
        }

       
    }
}
