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
    internal sealed class GetTagCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetTagCommand _getTagCommand;
        private int _methodOverload;

        public GetTagCommand(string[] tags, TagComparisonType comparisonType, int methodOverload)
        {
            base.name = "GetTagCommand";

            _getTagCommand = new Alachisoft.NCache.Common.Protobuf.GetTagCommand();
            _getTagCommand.tagComparisonType = (int) comparisonType;
            _getTagCommand.tags.AddRange(tags);
            _getTagCommand.requestId = base.RequestId;
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_TAG; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.NonKeyBulkRead; }
        }


        internal override bool IsKeyBased
        {
            get { return false; }
        }


        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.getTagCommand = _getTagCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_TAG;
            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.commandVersion = 1;
            base._command.MethodOverload = _methodOverload;
        }
    }
}