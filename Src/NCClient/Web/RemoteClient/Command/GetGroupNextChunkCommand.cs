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

using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Util;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal class GetGroupNextChunkCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetGroupNextChunkCommand _getNextChunkCommand;

        internal GetGroupNextChunkCommand(GroupEnumerationPointer pointer)
        {
            base.name = "GetGroupNextChunkCommand";

            _getNextChunkCommand = new Alachisoft.NCache.Common.Protobuf.GetGroupNextChunkCommand();
            _getNextChunkCommand.groupEnumerationPointer = EnumerationPointerConversionUtil.ConvertToProtobufGroupEnumerationPointer(pointer);
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _getNextChunkCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.GET_GROUP_NEXT_CHUNK;
        }

        protected override void CreateCommand()
        {
            _getNextChunkCommand.requestId = base.RequestId;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GETGROUP_NEXT_CHUNK; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.ChunkRead; }
        }

        internal override bool IsKeyBased { get { return false; } }
    }
}