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
    internal sealed class PollCommand : CommandBase
    {
        Alachisoft.NCache.Common.Protobuf.PollCommand _pollCommand;

        public PollCommand()
        {
            _pollCommand = new Common.Protobuf.PollCommand();
            _pollCommand.requestId = base.RequestId;
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        internal override CommandType CommandType
        {
            get { return CommandType.POLL; }
        }

        protected override void CreateCommand()
        {
            base._command = new Common.Protobuf.Command();
            base._command.pollCommand = _pollCommand;
            base._command.requestID = base.RequestId;
            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.commandVersion = 1;
            base._command.type = Common.Protobuf.Command.Type.POLL;
        }
    }
}