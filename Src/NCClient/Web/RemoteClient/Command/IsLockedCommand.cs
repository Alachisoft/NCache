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
    internal sealed class IsLockedCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.IsLockedCommand _isLockedCommand;

        public IsLockedCommand(string key, object lockId)
        {
            base.name = "IsLockedCommand";

            _isLockedCommand = new Alachisoft.NCache.Common.Protobuf.IsLockedCommand();
            _isLockedCommand.key = key;
            if (lockId != null) _isLockedCommand.lockId = lockId.ToString();
            _isLockedCommand.requestId = base.RequestId;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.ISLOCKED; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _isLockedCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.ISLOCKED;
        }

        protected override void CreateCommand()
        {
            _isLockedCommand.requestId = base.RequestId;
        }
    }
}
