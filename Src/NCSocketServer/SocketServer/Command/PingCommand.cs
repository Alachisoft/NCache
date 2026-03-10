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
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System;
using System.Diagnostics;

namespace Alachisoft.NCache.SocketServer.Command
{
    class PingCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public long RequestId;
            public bool HasResponse;
        }

        private CommandInfo _cmdInfo;

        private CommandInfo ParseCommand(Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo = new CommandInfo();
            Common.Protobuf.PingCommand pingCommand = command.pingCommand;
            cmdInfo.RequestId = pingCommand.requestId;
            return cmdInfo;
        }

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            string exception = null;
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                _cmdInfo = ParseCommand(command);
            }
            catch (Exception exc)
            {
                if (!immatureId.Equals("-2"))
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

            try
            {
                stopWatch.Stop();

                Common.Protobuf.PingResponse pingResponse = new Common.Protobuf.PingResponse();

                if (_cmdInfo.HasResponse)
                {
                    if (clientManager.ClientVersion >= 5000)
                    {
                        Common.Util.ResponseHelper.SetResponse(pingResponse, command.requestID, command.commandID);
                        ResponseOptions responseOptions = new ResponseOptions()
                        {
                            Response = pingResponse,
                            ResponseType = Common.Protobuf.Response.Type.PING
                        };
                        _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildResponse(responseOptions));
                    }
                    else
                    {
                        Common.Protobuf.Response response = new Common.Protobuf.Response();
                        response.pingResponse = pingResponse;
                        Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.PING);
                        ResponseOptions responseOptions = new ResponseOptions()
                        {
                            Response = response,
                            ResponseType = Common.Protobuf.Response.Type.PING
                        };
                        _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildResponse(responseOptions));
                    }
                }
            }
            catch (Exception exc)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("PingCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " : " + exc.ToString());
                exception = exc.ToString();

                if (_cmdInfo.HasResponse)
                {
                    ResponseOptions responseOptions = new ResponseOptions()
                    {
                        Exception = exc,
                        RequestId = command.requestID,
                        CommandId = command.commandID,
                    };

                    _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildExceptionResponse(responseOptions));
                }
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Management.APILogging.APILogManager.APILogManger != null && Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.Ping.ToLower());
                        log.GeneratePingCommandAPILogItem(1, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }
        }
    }
}
