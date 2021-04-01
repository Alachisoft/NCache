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

namespace Alachisoft.NCache.Client
{
    internal sealed class GetRecordSetNextChunk : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetReaderNextChunkCommand _readerNextChunkCommand;

        public GetRecordSetNextChunk(string readerID, string nodeIp, int nextIndex)
        {
            base.name = "GetReaderNextChunkCommand";
            _readerNextChunkCommand = new Alachisoft.NCache.Common.Protobuf.GetReaderNextChunkCommand();
            _readerNextChunkCommand.readerId = readerID;
            _readerNextChunkCommand.nextIndex = nextIndex;
            _readerNextChunkCommand.nodeIP = nodeIp;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_READER_CHUNK; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.NonKeyBulkRead; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _readerNextChunkCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.GET_READER_CHUNK;
        }

        protected override void CreateCommand()
        {

            _readerNextChunkCommand.commandID = this._commandID;
            _readerNextChunkCommand.requestId = base.RequestId;
            _readerNextChunkCommand.commandVersion = 1;
            _readerNextChunkCommand.clientLastViewId = base.ClientLastViewId;
        }
    }
}
