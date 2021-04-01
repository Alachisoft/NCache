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
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.SocketServer.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Alachisoft.NCache.SocketServer.Command
{
    class RaiseCustomNotifCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            
            byte[] data = null;
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            
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
                    //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
                }
                return;
            }
            Alachisoft.NCache.Common.Protobuf.RaiseCustomEventCommand notifcommand = command.raiseCustomEventCommand;

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                OperationContext operationContext = null;
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                nCache.Cache.SendNotification(notifcommand.notifIf, notifcommand.data, operationContext);

                Alachisoft.NCache.Common.Protobuf.RaiseCustomEventResponse raiseCustomEventResponse = new Alachisoft.NCache.Common.Protobuf.RaiseCustomEventResponse();

                if (clientManager.ClientVersion >= 5000)
                {
                    ResponseHelper.SetResponse(raiseCustomEventResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(raiseCustomEventResponse, Common.Protobuf.Response.Type.RAISE_CUSTOM_EVENT));
                }
                else
                {
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    response.raiseCustomEventResponse = raiseCustomEventResponse;
                    ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.RAISE_CUSTOM_EVENT);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }

            }
            catch (Exception exc)
            {
                exception = exc.ToString();
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                stopWatch.Stop();
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.RaiseCustomEvent.ToLower());
                        log.GenerateraiseCustomAPILogItem(notifcommand.notifIf.Length, notifcommand.data.Length, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());

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
            Alachisoft.NCache.Common.Protobuf.RaiseCustomEventCommand raiseCustomEventCommand = command.raiseCustomEventCommand;
            cmdInfo.RequestId = raiseCustomEventCommand.requestId.ToString();

            return cmdInfo;
        }


      
    }
}
