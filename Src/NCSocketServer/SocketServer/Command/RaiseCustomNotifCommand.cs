//  Copyright (c) 2026 Alachisoft
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
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.ResponseSerialization;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
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
                    ResponseOptions responseOptions = new ResponseOptions()
                    {
                        Exception = exc,
                        RequestId = command.requestID,
                        CommandId = command.commandID,
                    };

                    _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildExceptionResponse(responseOptions));
                }
                return;
            }
            Alachisoft.NCache.Common.Protobuf.RaiseCustomEventCommand notifcommand = command.raiseCustomEventCommand;

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                nCache.Cache.SendNotification(notifcommand.notifIf, notifcommand.data);

                Alachisoft.NCache.Common.Protobuf.RaiseCustomEventResponse raiseCustomEventResponse = new Alachisoft.NCache.Common.Protobuf.RaiseCustomEventResponse();

                if (clientManager.ClientVersion >= 5000)
                {
                    ResponseHelper.SetResponse(raiseCustomEventResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(raiseCustomEventResponse, Common.Protobuf.Response.Type.RAISE_CUSTOM_EVENT));
                    ResponseOptions responseOptions = new ResponseOptions()
                    {
                        Response = raiseCustomEventResponse,
                        ResponseType = Common.Protobuf.Response.Type.RAISE_CUSTOM_EVENT
                    };
                    _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildResponse(responseOptions));
                }
                else
                {
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    response.raiseCustomEventResponse = raiseCustomEventResponse;
                    ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.RAISE_CUSTOM_EVENT);
                    ResponseOptions responseOptions = new ResponseOptions()
                    {
                        Response = response,
                        ResponseType = Common.Protobuf.Response.Type.RAISE_CUSTOM_EVENT
                    };
                    _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildResponse(responseOptions));
                }

            }
            catch (Exception exc)
            {
                exception = exc.ToString();
                //PROTOBUF:RESPONSE
                ResponseOptions responseOptions = new ResponseOptions()
                {
                    Exception = exc,
                    RequestId = command.requestID,
                    CommandId = command.commandID,
                };

                _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildExceptionResponse(responseOptions));
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
            Alachisoft.NCache.Common.Protobuf.RaiseCustomEventCommand raiseCustomEventCommand = command.raiseCustomEventCommand;
            cmdInfo.RequestId = raiseCustomEventCommand.requestId.ToString();

            return cmdInfo;
        }
    }
}
