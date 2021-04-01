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

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Runtime.Caching;
using System;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class GetCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetCommand _getCommand;
        private int _methodOverload;

        public GetCommand(string key, BitSet flagMap, string group, string subGroup, LockAccessType accessType, object lockId, TimeSpan lockTimeout,   int threadId, int methodOverload)
        {
            base.name = "GetCommand";
            base.key = key;

            _getCommand = new Alachisoft.NCache.Common.Protobuf.GetCommand();
            _getCommand.key = key;
            _getCommand.flag = flagMap.Data;

            _getCommand.lockInfo = new Alachisoft.NCache.Common.Protobuf.LockInfo();
            _getCommand.lockInfo.lockAccessType = (int)accessType;
            if (lockId != null) _getCommand.lockInfo.lockId = lockId.ToString();
            _getCommand.lockInfo.lockTimeout = lockTimeout.Ticks;
            _getCommand.requestId = base.RequestId;
            _getCommand.threadId = threadId;
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _getCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.GET;
        }

        protected override void CreateCommand()
        {

            _getCommand.commandID = this._commandID;
            _getCommand.requestId = base.RequestId;
            _getCommand.MethodOverload = _methodOverload;

        }
    }
}
