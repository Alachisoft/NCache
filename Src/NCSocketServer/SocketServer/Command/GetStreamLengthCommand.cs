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

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetStreamLengthCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            Alachisoft.NCache.Common.Protobuf.GetStreamLengthCommand getStreamLengthCommand = command.getStreamLengthCommand;

            try
            {
                long streamLength = ((NCache)clientManager.CmdExecuter).Cache.GetStreamLength(getStreamLengthCommand.key, getStreamLengthCommand.lockHandle, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();

                Alachisoft.NCache.Common.Protobuf.GetStreamLengthResponse getStreamLengthResponse = new Alachisoft.NCache.Common.Protobuf.GetStreamLengthResponse();
                response.requestId = getStreamLengthCommand.requestId;
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_STREAM_LENGTH;
                getStreamLengthResponse.streamLength = streamLength;
                response.getStreamLengthResponse = getStreamLengthResponse;

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception e)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(e, command.requestID,command.commandID));
            }
        }
    }
}
