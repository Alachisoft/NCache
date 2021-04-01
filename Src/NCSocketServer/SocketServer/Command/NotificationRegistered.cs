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
using System.Collections.Generic;
using System.Globalization;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class NotificationRegistered : CommandBase
    {

        private struct CommandInfo
        {
            public string RequestId;
            public int RegNotifs;
            public int datafilter;
            public int sequence;
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            int overload;
            string exception = null;
            NotificationsType notif=0;
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
                }
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                OperationContext context = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                context.Add(OperationContextFieldName.ClientId, clientManager.ClientID);
                notif = (NotificationsType)cmdInfo.RegNotifs;

                if (command.commandVersion < 1)
                {
                    context.Add(OperationContextFieldName.ClientLastViewId, forcedViewId);
                }
                else //NCache 4.1 SP1 or later
                {
                    context.Add(OperationContextFieldName.ClientLastViewId, command.clientLastViewId.ToString(CultureInfo.InvariantCulture));
                }

                //Will only register those which are == null i.e. not initialized
                nCache.RegisterNotification(notif);              
              
                //Execute only if successfull
                if ((notif & NotificationsType.RegAddNotif) != 0 || (notif & NotificationsType.RegUpdateNotif) != 0 || (notif & NotificationsType.RegRemoveNotif) != 0)
                    nCache.MaxEventRequirement((Runtime.Events.EventDataFilter)cmdInfo.datafilter, notif, (short)cmdInfo.sequence);
                stopWatch.Stop();

                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.RegisterNotifResponse registerNotifResponse = new Alachisoft.NCache.Common.Protobuf.RegisterNotifResponse();
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.REGISTER_NOTIF;
                response.registerNotifResponse = registerNotifResponse;

                if (clientManager.ClientVersion >= 5000)
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response, Common.Protobuf.Response.Type.REGISTER_NOTIF));
                }
                else
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        string methodNme = null;
                    
                        if (notif == NotificationsType.UnregAddNotif || notif == NotificationsType.UnregAddNotif || notif == NotificationsType.UnregRemoveNotif)
                            methodNme = MethodsName.UnRegisterCacheNotification.ToLower();
                        else
                            methodNme = MethodsName.RegisterCacheNotification.ToLower();
                        APILogItemBuilder log = new APILogItemBuilder(methodNme);
                        log.GenerateRegisterCacheNotificationCallback ( 0,  notif.ToString(),cmdInfo.datafilter.ToString(),  overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
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
            Alachisoft.NCache.Common.Protobuf.RegisterNotifCommand registerNotifCommand = command.registerNotifCommand;
            cmdInfo.RegNotifs = registerNotifCommand.notifMask;
            cmdInfo.RequestId = registerNotifCommand.requestId.ToString();
            cmdInfo.datafilter = registerNotifCommand.datafilter;
            cmdInfo.sequence = registerNotifCommand.sequence;
            return cmdInfo;
        }       
    }
}
