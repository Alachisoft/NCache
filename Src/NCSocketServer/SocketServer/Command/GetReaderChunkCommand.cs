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
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;
using Alachisoft.NCache.Common.DataReader;
using System.Diagnostics;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class GetReaderChunkCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string ReaderId;
            public string NodeIp;
            public int nextIndex;
            public OperationContext OperationContext;
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            int overload;
            int count = 0;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                overload = 1;
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
                ReaderResultSet reader = nCache.Cache.GetReaderChunk(cmdInfo.ReaderId, cmdInfo.nextIndex, false, cmdInfo.OperationContext);
                stopWatch.Stop();
                ReaderResponseBuilder.BuildReaderChunkResponse(reader, cmdInfo.RequestId, _serializedResponsePackets, command.commandID, clientManager.ClientVersion < 4620, out count);
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
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetReaderChunkCommand.ToLower());
                        log.GenerateGetReaderChunkCommand(cmdInfo.ReaderId, count, overload, exception, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }
        }

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();
            Alachisoft.NCache.Common.Protobuf.GetReaderNextChunkCommand getNextChunkCommand = command.getReaderNextChunkCommand;
            cmdInfo.RequestId = getNextChunkCommand.requestId.ToString();
            cmdInfo.nextIndex = getNextChunkCommand.nextIndex;
            cmdInfo.ReaderId = getNextChunkCommand.readerId;
            string intendedRecepient = getNextChunkCommand.nodeIP;
            long lastViewId = command.clientLastViewId;

            cmdInfo.OperationContext = new OperationContext();
            cmdInfo.OperationContext.Add(OperationContextFieldName.IntendedRecipient, intendedRecepient);
            cmdInfo.OperationContext.Add(OperationContextFieldName.ClientLastViewId, lastViewId);

            return cmdInfo;
        }
    }
}