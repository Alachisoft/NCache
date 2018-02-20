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
    internal sealed class GetGroupCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetGroupCommand _getGroupCommand;
        private int _methodOverload;

        public GetGroupCommand(string group, string subGroup, bool getGroupKeys, int methodOverload)
        {
            base.name = getGroupKeys ? "GetGroupKeysCommand" : "GetGroupDataCommand";

            _getGroupCommand = new Alachisoft.NCache.Common.Protobuf.GetGroupCommand();
            _getGroupCommand.group = group;
            _getGroupCommand.subGroup = subGroup;
            _getGroupCommand.requestId = base.RequestId;
            _getGroupCommand.getGroupKeys = getGroupKeys;
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_GROUP; }
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
            base._command.getGroupCommand = _getGroupCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_GROUP;
            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.commandVersion = 1;
            base._command.MethodOverload = _methodOverload;
        }
    }
}