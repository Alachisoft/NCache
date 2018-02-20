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
    internal sealed class RemoveByTagCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.RemoveByTagCommand _removeTagCommand;
        private int _methodOverload;

        public RemoveByTagCommand(string[] tags, TagComparisonType comparisonType, int methodOverload)
        {
            base.name = "RemoveByTagCommand";

            _removeTagCommand = new Alachisoft.NCache.Common.Protobuf.RemoveByTagCommand();
            _removeTagCommand.tags.AddRange(tags);
            _removeTagCommand.tagComparisonType = (int) comparisonType;
            _removeTagCommand.requestId = base.RequestId;
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.REMOVE_BY_TAG; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.NonKeyBulkWrite; }
        }

        internal override bool IsKeyBased
        {
            get { return false; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.removeByTagCommand = _removeTagCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.REMOVE_BY_TAG;
            base._command.clientLastViewId = base.clientLastViewId;
            base._command.MethodOverload = _methodOverload;
        }
    }
}