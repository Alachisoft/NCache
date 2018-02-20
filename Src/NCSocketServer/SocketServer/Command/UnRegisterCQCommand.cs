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

using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System;
using System.Diagnostics;

namespace Alachisoft.NCache.SocketServer.Command
{
    class UnRegisterCQCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public string serverUniqueId;
            public string clientUniqueId;
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                overload = command.MethodOverload;
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                nCache.Cache.UnRegisterCQ(cmdInfo.serverUniqueId, cmdInfo.clientUniqueId, clientManager.ClientID);
                stopWatch.Stop();
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.UNREGISTER_CQ;

                Alachisoft.NCache.Common.Protobuf.UnRegisterCQResponse unRegisterCQResponse = new Alachisoft.NCache.Common.Protobuf.UnRegisterCQResponse();
                response.unRegisterCQResponse = unRegisterCQResponse;

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
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.UnRegisterCQ.ToLower());
                        log.GenerateUnRegisterCQAPILogItem(overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }
        }

        protected CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.UnRegisterCQCommand unRegCQCommand = command.unRegisterCQCommand;

            cmdInfo.RequestId = unRegCQCommand.requestId.ToString();
            cmdInfo.serverUniqueId = unRegCQCommand.serverUniqueId;
            cmdInfo.clientUniqueId = unRegCQCommand.clientUniqueId;

            return cmdInfo;
        }
    }
}
