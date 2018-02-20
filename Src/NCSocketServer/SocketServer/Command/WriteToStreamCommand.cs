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
using Alachisoft.NCache.Common.DataStructures;

namespace Alachisoft.NCache.SocketServer.Command
{
    class WriteToStreamCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            Alachisoft.NCache.Common.Protobuf.WriteToStreamCommand writeToStreamCommand = command.writeToStreamCommand;

            try
            {
                ((NCache)clientManager.CmdExecuter).Cache.WriteToStream(writeToStreamCommand.key, writeToStreamCommand.lockHandle, new VirtualArray(writeToStreamCommand.buffer), writeToStreamCommand.srcOffSet, writeToStreamCommand.dstOffSet, writeToStreamCommand.length, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
            }
            catch (Exception e)
            {
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(e, command.requestID, command.commandID));
                return;
            }

            Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
            Alachisoft.NCache.Common.Protobuf.WriteToStreamResponse writeToStreamResponse = new Alachisoft.NCache.Common.Protobuf.WriteToStreamResponse();
            response.requestId = writeToStreamCommand.requestId;
            response.commandID = command.commandID;
            response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.WRITE_TO_STREAM;
            response.writeToStreamResponse = writeToStreamResponse;
            _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
        }
    }
}
