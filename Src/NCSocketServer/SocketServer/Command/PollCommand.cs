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

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Common.Protobuf;
using System;
using System.Diagnostics;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class PollCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public long ClientLastViewId;
            public int CommandVersion;
        }

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            string exception = null;
            int updateCount = 0;
            int removeCount = 0;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch(System.Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                return;
            }

            try
            {
                NCache cmdExecuter = clientManager.CmdExecuter as NCache;
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                if (cmdInfo.CommandVersion < 1)
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, forcedViewId);
                }
                else // NCache 4.1 or later
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                }

                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                operationContext.Add(OperationContextFieldName.ClientId, clientManager.ClientID);
                PollingResult result = cmdExecuter.Cache.Poll(operationContext);

                Response response = new Response();
                PollResponse pollResponse = new PollResponse();
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);

                pollResponse.removedKeys.AddRange(result.RemovedKeys);
                pollResponse.updatedKeys.AddRange(result.UpdatedKeys);
                updateCount = result.UpdatedKeys.Count;
                removeCount = result.RemovedKeys.Count;

                response.pollResponse = pollResponse;
                response.responseType = Response.Type.POLL;
                response.commandID = command.commandID;
                response.requestId = command.requestID;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (System.Exception exc)
            {
                exception = exc.ToString();
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Management.APILogging.APILogManager.APILogManger != null && Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.Poll.ToLower());
                        log.GeneratePollCommandAPILogItem(1, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), updateCount, removeCount);
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

            Alachisoft.NCache.Common.Protobuf.PollCommand pollCommand = command.pollCommand;

            cmdInfo.RequestId = pollCommand.requestId.ToString();
            cmdInfo.ClientLastViewId = command.clientLastViewId;
            cmdInfo.CommandVersion = command.commandVersion;
            return cmdInfo;
        }
    }
}
