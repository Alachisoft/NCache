// Copyright (c) 2015 Alachisoft
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
//using System.Linq;
using System.Text;
using Alachisoft.NCache.Common.Communication;
using Alachisoft.NCache.Common.Protobuf;
using System.IO;
using Alachisoft.NCache.Serialization.Formatters;

namespace Alachisoft.NCache.Management.RPC
{
    class ManagementChannelFormatter : IChannelFormatter
    {
        public byte[] Serialize(object graph)
        {
            byte[] bufffer = null;
            ManagementCommand command = graph as ManagementCommand;

            command.arguments = CompactBinaryFormatter.ToByteBuffer(command.Parameters, null);

            using (MemoryStream stream = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize<ManagementCommand>(stream,command);
                bufffer = stream.ToArray();
            }

            return bufffer;
        }

        public object Deserialize(byte[] buffer)
        {
            ManagementResponse response = null;
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                response = ProtoBuf.Serializer.Deserialize<ManagementResponse>(stream);
            }

            if (response != null)
            {
                if (response.returnVal != null)
                {
                    response.ReturnValue = CompactBinaryFormatter.FromByteBuffer(response.returnVal, null);
                }
            }
            return response;
        }
    }
}
