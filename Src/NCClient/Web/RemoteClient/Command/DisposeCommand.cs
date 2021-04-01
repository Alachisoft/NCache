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

using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class DisposeCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.DisposeCommand _disposeCommand;

        internal DisposeCommand()
        {
            base.name = "DisposeCommand";
            _disposeCommand = new Alachisoft.NCache.Common.Protobuf.DisposeCommand();
            _disposeCommand.requestId = base.RequestId;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.DISPOSE; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        internal override bool IsKeyBased { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _disposeCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.DISPOSE;
        }

        protected override void CreateCommand()
        {

            _disposeCommand.requestId = base.RequestId;
        }
    }
}
