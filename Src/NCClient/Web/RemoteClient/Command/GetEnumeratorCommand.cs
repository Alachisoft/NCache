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
    internal sealed class GetEnumeratorCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetEnumeratorCommand _getEnumeratorCommand;
        private int _methodOverload;

        internal GetEnumeratorCommand(int methodOverload)
        {
            base.name = "GetEnumeratorCommand";

            _getEnumeratorCommand = new Alachisoft.NCache.Common.Protobuf.GetEnumeratorCommand();
            _getEnumeratorCommand.requestId = base.RequestId;
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_ENUMERATOR; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        internal override bool IsKeyBased { get { return false; } }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.getEnumeratorCommand = _getEnumeratorCommand; ;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_ENUMERATOR;
            base._command.MethodOverload = _methodOverload;
        }
    }
}
