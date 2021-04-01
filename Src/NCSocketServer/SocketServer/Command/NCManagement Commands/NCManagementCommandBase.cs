//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.RPCFramework;

namespace Alachisoft.NCache.SocketServer.Command
{
    abstract class NCManagementCommandBase : CommandBase
    {

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            //No need to implement this
        }

        abstract public void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.ManagementCommand command);

        protected object[] GetTargetMethodParameters(byte[] graph)
        {
            TargetMethodParameter parameters = SerializationUtil.CompactBinaryDeserialize(graph, null) as TargetMethodParameter;
            return parameters.ParameterList.ToArray();
        }

        protected byte[] SerializeResponse(object result)
        {
            return SerializationUtil.CompactBinarySerialize(result, null);
        }

    }
}
