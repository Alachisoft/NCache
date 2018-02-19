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

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));

                return;
            }

           try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                ReaderResultSet reader = nCache.Cache.GetReaderChunk(cmdInfo.ReaderId, cmdInfo.nextIndex, false, cmdInfo.OperationContext);

                ReaderResponseBuilder.BuildReaderChunkResponse(reader, cmdInfo.RequestId, _serializedResponsePackets);
            }
             catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
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


