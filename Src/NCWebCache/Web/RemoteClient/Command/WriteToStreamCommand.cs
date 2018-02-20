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

using Alachisoft.NCache.Caching;

namespace Alachisoft.NCache.Web.Command
{
    class WriteToStreamCommand : CommandBase
    {
        public string lockHandle;
        public int srcOffset;
        public int dstOffset;
        public int length;
        public byte[] buffer;
        private Alachisoft.NCache.Common.Protobuf.WriteToStreamCommand _writeToStreamCommand;

        public WriteToStreamCommand(string key, string lockHandle, int srcOffset, int dstOffset, int length,
            byte[] buffer)
        {
            base.name = "WriteToStreamCommand";
            _writeToStreamCommand = new Alachisoft.NCache.Common.Protobuf.WriteToStreamCommand();
            _writeToStreamCommand.key = key;
            _writeToStreamCommand.lockHandle = lockHandle;
            _writeToStreamCommand.srcOffSet = srcOffset;
            _writeToStreamCommand.dstOffSet = dstOffset;
            _writeToStreamCommand.length = length;
            if (buffer != null)
            {
                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(buffer);
                _writeToStreamCommand.buffer.AddRange(ubObject.DataList);
            }
        }

        internal override CommandType CommandType
        {
            get { return CommandType.WRITE_TO_STREAM; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.writeToStreamCommand = _writeToStreamCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.WRITE_TO_STREAM;
        }
    }
}