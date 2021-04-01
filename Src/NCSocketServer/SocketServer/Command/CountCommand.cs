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
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Alachisoft.NCache.SocketServer.Command
{
    class CountCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            long result = 0;

            try
            {
                overload = command.MethodOverload;
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                result = nCache.Cache.Count;
                stopWatch.Stop();

                Common.Protobuf.CountResponse countResponse = new Common.Protobuf.CountResponse();
                countResponse.count = result;

                if (clientManager.ClientVersion >= 5000)
                {
                    ResponseHelper.SetResponse(countResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(countResponse, Common.Protobuf.Response.Type.COUNT));
                }
                else
                {
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    response.count = countResponse;
                    ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.COUNT);

                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (Exception exc)
            {
                exception = exc.ToString();
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.Count.ToLower());
                        log.GenerateCacheCountAPILogItem(1, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), result);
                    }
                }
                catch { }
            }
        }

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.CountCommand countCommand = command.countCommand;

            cmdInfo.RequestId = countCommand.requestId.ToString();

            return cmdInfo;
        }

    }
}
