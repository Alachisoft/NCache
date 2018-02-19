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

using Alachisoft.NCache.Common.Protobuf;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class GetRecordSetNextChunk : CommandBase
    {
        private GetReaderNextChunkCommand _readerNextChunkCommand;

        public GetRecordSetNextChunk(string readerID, string nodeIp, int nextIndex)
        {
            name = "GetReaderNextChunkCommand";
            _readerNextChunkCommand = new GetReaderNextChunkCommand();
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
            get { return RequestType.BulkRead; }
        }

        protected override void CreateCommand()
        {
            _command = new Common.Protobuf.Command();
            _command.requestID = RequestId;
            _command.getReaderNextChunkCommand = _readerNextChunkCommand;
            _command.type = Common.Protobuf.Command.Type.GET_READER_CHUNK;
            _command.commandVersion = 1;
            _command.clientLastViewId = ClientLastViewId;
        }
    }
}
