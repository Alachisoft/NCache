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
using System.IO;
using System.Text;
using Alachisoft.NCache.Common.Protobuf.Util;

namespace Alachisoft.NCache.Client
{
    internal sealed class GetSerializationFormatCommand : CommandBase
    {
        private Common.Protobuf.GetSerializationFormatCommand _getSerializationFormatCommand;

        internal GetSerializationFormatCommand(bool isAsync)
        {
            name = "GetSerializationFormatCommand";
            base.isAsync = isAsync;

            _getSerializationFormatCommand = new Common.Protobuf.GetSerializationFormatCommand();
            _getSerializationFormatCommand.requestId = base.RequestId;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_SERIALIZATION_FORMAT; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        internal override bool IsKeyBased { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _getSerializationFormatCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.GET_SERIALIZATION_FORMAT;
        }

        protected override void CreateCommand()
        {

            _getSerializationFormatCommand.requestId = RequestId;
        }
    }
}
