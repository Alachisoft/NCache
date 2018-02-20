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
using System.Collections.Generic;
using Alachisoft.NCache.Web.Command;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.DataStructures;

namespace Alachisoft.NCache.SocketServer.Command
{
    class ReadFromStreamCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            Alachisoft.NCache.Common.Protobuf.ReadFromStreamCommand readFromStreamCommand = command.readFromStreamCommand;
            ReadFromStreamCommandInfo cmdInfo = new ReadFromStreamCommandInfo();
            cmdInfo.key = readFromStreamCommand.key;
            cmdInfo.length = readFromStreamCommand.length;
            cmdInfo.lockHandle = readFromStreamCommand.lockHandle;
            cmdInfo.offset = readFromStreamCommand.offset;
            cmdInfo.requestId = readFromStreamCommand.requestId;

            int bytesRead =0;
            List<byte[]> buffer = null;

            VirtualArray vBuffer = null;
            try
            {
                bytesRead = ((NCache)clientManager.CmdExecuter).Cache.ReadFromStream(ref vBuffer, cmdInfo.key, cmdInfo.lockHandle, cmdInfo.offset, cmdInfo.length, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
            }
            catch (Exception e)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(e, command.requestID, command.commandID));
                return;
            }

            Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
            Alachisoft.NCache.Common.Protobuf.ReadFromStreamResponse readFromStreamResponse = new Alachisoft.NCache.Common.Protobuf.ReadFromStreamResponse();

            if (bytesRead > 0)
            {
                foreach (byte[] buffersegment in vBuffer.BaseArray)
                {
                    readFromStreamResponse.buffer.Add(buffersegment);
                }
            }
            readFromStreamResponse.bytesRead = bytesRead;
            response.requestId = cmdInfo.requestId;
            response.commandID = command.commandID;
            response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.READ_FROM_STREAM;
            response.readFromStreamResponse = readFromStreamResponse;
         
            _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
        }
    }
}
