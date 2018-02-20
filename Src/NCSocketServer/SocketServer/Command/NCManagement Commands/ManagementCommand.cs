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
using Alachisoft.NCache.Common.Util;

using Alachisoft.NCache.Runtime.Exceptions;


namespace Alachisoft.NCache.SocketServer.Command
{
    class ManagementCommand : NCManagementCommandBase
    {
        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.ManagementCommand command)
        {
            object result = null;
            try
            {
               
                if (command.objectName == ManagementUtil.ManagementObjectName.CacheServer)
                {
                    result = CacheProvider.ManagementRpcService.InvokeMethodOnTarget(command.methodName,
                        command.overload,
                        GetTargetMethodParameters(command.arguments));
                }

                Alachisoft.NCache.Common.Protobuf.ManagementResponse response = new Alachisoft.NCache.Common.Protobuf.ManagementResponse();                
                response.methodName = command.methodName;
                response.version = command.commandVersion;
                response.requestId = command.requestId;
                response.returnVal = SerializeResponse(result);

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {                
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, Convert.ToInt32(command.requestId),-1));
            }
        }
    }
}
