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

namespace Alachisoft.NCache.Web.Command
{
    [Serializable]
    class CloseStreamCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.CloseStreamCommand _closeStreamCommand;
        public string lockHandle;
        private int _methodOverload;

        public CloseStreamCommand(string key, string lockHandle, int methodOverload)
        {
            base.name = "CloseStreamCommand";
            _closeStreamCommand = new Alachisoft.NCache.Common.Protobuf.CloseStreamCommand();
            _closeStreamCommand.key = key;
            _closeStreamCommand.lockHandle = lockHandle;
            _closeStreamCommand.requestId = base.RequestId;
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.CLOSE_STREAM; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.closeStreamCommand = _closeStreamCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.CLOSE_STREAM;
            base._command.MethodOverload = _methodOverload;
        }
    }
}