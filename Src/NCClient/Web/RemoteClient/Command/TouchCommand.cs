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

using System.Collections.Generic;
using System.IO;
using Alachisoft.NCache.Client.Caching;

namespace Alachisoft.NCache.Client
{
    internal sealed class TouchCommand : CommandBase
    {
        private Common.Protobuf.TouchCommand _touchCommand;
        private int _methodOverload;

        internal TouchCommand(List<string> keys)
        {
            name = "TouchCommand";
            BulkKeys = keys.ToArray();
            _touchCommand = new Common.Protobuf.TouchCommand();
            _touchCommand.requestId = base.RequestId;
            _touchCommand.keys.AddRange(keys);
            _methodOverload = TargetMethodAttribute.MethodOverload;

        }

        internal override CommandType CommandType
        {
            get { return CommandType.TOUCH; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _touchCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.TOUCH;
        }

        protected override void CreateCommand()
        {
            _touchCommand.requestId = RequestId;
            _touchCommand.MethodOverload = _methodOverload;
        }
    }
}