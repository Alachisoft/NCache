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
    internal sealed class UnlockCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.UnlockCommand _unlockCommand;
        private int _methodOverload;

        public UnlockCommand(string key, int methodOverload)
        {
            base.name = "UnlockCommand";
            base.key = key;
            _methodOverload = methodOverload;
            _unlockCommand = new Alachisoft.NCache.Common.Protobuf.UnlockCommand();
            _unlockCommand.key = key;
            _unlockCommand.preemptive = true;
        }

        public UnlockCommand(string key, object lockId, int methodOverload)
        {
            base.name = "UnlockCommand";
            _methodOverload = methodOverload;
            _unlockCommand = new Alachisoft.NCache.Common.Protobuf.UnlockCommand();
            _unlockCommand.key = key;
            if (lockId == null)
                _unlockCommand.preemptive = true;
            else
                _unlockCommand.preemptive = false;
            _unlockCommand.lockId = lockId == null ? "" : lockId.ToString();
        }

        internal override CommandType CommandType
        {
            get { return CommandType.UNLOCK; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _unlockCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.UNLOCK;
        }

        protected override void CreateCommand()
        {
            _unlockCommand.requestId = base.RequestId;
           _unlockCommand.MethodOverload = _methodOverload;
        }
    }
}
