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
    internal sealed class DisposeReaderCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.DisposeReaderCommand _disposeCommand;
        private int _methodOverload;

        internal DisposeReaderCommand(string readerId, string nodeIp, int methodOverload)
        {
            base.name = "DisposeReaderCommand";
            _disposeCommand = new Alachisoft.NCache.Common.Protobuf.DisposeReaderCommand();
            _disposeCommand.readerId = readerId;
            _disposeCommand.nodeIP = nodeIp;
            _disposeCommand.requestId = base.RequestId;
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.DISPOSE_READER; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.disposeReaderCommand = _disposeCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.DISPOSE_READER;
            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.MethodOverload = _methodOverload;
        }
    }
}