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

namespace Alachisoft.NCache.Client
{
    internal sealed class GetCompactTypesCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetCompactTypesCommand _getCompactTypesCommand;

        internal GetCompactTypesCommand(bool isAsync)
        {
            base.name = "GetCompactTypesCommand";
            base.isAsync = isAsync;

            _getCompactTypesCommand = new Alachisoft.NCache.Common.Protobuf.GetCompactTypesCommand();
            _getCompactTypesCommand.requestId = base.RequestId;
            _getCompactTypesCommand.isDotnetClient = true;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_COMPACT_TYPES; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        internal override bool IsKeyBased { get { return false; } }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.getCompactTypesCommand = _getCompactTypesCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_COMPACT_TYPES;
        }
    }
}