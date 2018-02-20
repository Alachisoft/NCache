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

namespace Alachisoft.NCache.Web.Command
{
    internal class ReadFromStreamCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.ReadFromStreamCommand _readFromStreamCommand;

        public ReadFromStreamCommand(string key, string lockHandle, int offset, int length)
        {
            base.name = "ReadFromStreamCommand";
            _readFromStreamCommand = new Alachisoft.NCache.Common.Protobuf.ReadFromStreamCommand();
            _readFromStreamCommand.key = key;
            _readFromStreamCommand.lockHandle = lockHandle;
            _readFromStreamCommand.offset = offset;
            _readFromStreamCommand.requestId = base.RequestId;
            _readFromStreamCommand.length = length;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.READ_FROM_STREAM; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.readFromStreamCommand = _readFromStreamCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.READ_FROM_STREAM;
        }
    }
}