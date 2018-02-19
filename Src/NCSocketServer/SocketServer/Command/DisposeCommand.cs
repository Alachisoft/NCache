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

namespace Alachisoft.NCache.SocketServer.Command
{
    class DisposeCommand : CommandBase
    {

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            if (clientManager != null)
            {
                clientManager._leftGracefully = true;
                clientManager.CmdExecuter.Dispose();

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                response.requestId = command.requestID;                
                response.disposeResponse = new Alachisoft.NCache.Common.Protobuf.DisposeResponse();
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.DISPOSE;

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
        }
    }
}
