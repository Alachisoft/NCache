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
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;

namespace Alachisoft.NCache.SocketServer.Command
{
    class ContainsCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Key;
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            byte[] data = null;
            
            try
            {
                overload = command.MethodOverload; 
                cmdInfo = ParseCommand(command, clientManager);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ContCmd.Exec", "cmd parsed");
            }
            catch (Exception exc)
            {

                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                return;
            }

            NCache nCache = clientManager.CmdExecuter as NCache;
            
            try
            {
                bool exists = nCache.Cache.Contains(cmdInfo.Key, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                stopWatch.Stop();
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.ContainResponse containsResponse = new Alachisoft.NCache.Common.Protobuf.ContainResponse();
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                containsResponse.exists = exists;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.CONTAINS;
                response.contain = containsResponse;
                
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
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.Contains.ToLower());
                        log.GenerateContainsCommandAPILogItem(cmdInfo.Key, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ContCmd.Exec", "cmd executed on cache");
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.ContainsCommand containsCommand = command.containsCommand;

            cmdInfo.Key = containsCommand.key;
            cmdInfo.RequestId = containsCommand.requestId.ToString();

            return cmdInfo;
        }
    }
}
