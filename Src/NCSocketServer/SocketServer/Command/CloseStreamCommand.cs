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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class CloseStreamCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            Alachisoft.NCache.Common.Protobuf.CloseStreamCommand closeStreamCommand = command.closeStreamCommand;
            string lockHandle = null;
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            overload = command.MethodOverload;

            try
            {
                if (clientManager != null)
                {
                    ((NCache)clientManager.CmdExecuter).Cache.CloseStream(closeStreamCommand.key, closeStreamCommand.lockHandle, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                    stopWatch.Stop();
                }
            }
            catch (Exception e)
            {
                //PROTOBUF:RESPONSE
                exception = e.ToString();
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(e, command.requestID,command.commandID));
                return;
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.CloseStream.ToLower());
                        log.GenerateCloseStreamAPILogItem(closeStreamCommand.key, closeStreamCommand.lockHandle, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }

            Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
            Alachisoft.NCache.Common.Protobuf.CloseStreamResponse closeStreamResponse = new Alachisoft.NCache.Common.Protobuf.CloseStreamResponse();
            response.requestId = closeStreamCommand.requestId;
            response.commandID = command.commandID;
            response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.CLOSE_STREAM;
            response.closeStreamResponse = closeStreamResponse;

            _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
        }
    }
}
