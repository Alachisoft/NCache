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
    internal sealed class GetCacheItemCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetCacheItemCommand _getCacheItemCommand;

        private LockAccessType _accessType;
        private object _lockId;
        private TimeSpan _lockTimeout;
        private int _methodOverload;


        public GetCacheItemCommand(string key, BitSet flagMap, LockAccessType accessType, object lockId, TimeSpan lockTimeout,  int methodOverload)
        {
            base.name = "GetCacheItemCommand";
            base.key = key;

            _getCacheItemCommand = new Alachisoft.NCache.Common.Protobuf.GetCacheItemCommand();
            _getCacheItemCommand.key = key;
            _getCacheItemCommand.requestId = base.RequestId;
            _getCacheItemCommand.flag = flagMap.Data;
            _getCacheItemCommand.lockInfo = new Alachisoft.NCache.Common.Protobuf.LockInfo();
            _getCacheItemCommand.lockInfo.lockAccessType = (int)accessType;
            if (lockId != null) _getCacheItemCommand.lockInfo.lockId = lockId.ToString();
            _getCacheItemCommand.lockInfo.lockTimeout = lockTimeout.Ticks;
            _methodOverload = methodOverload;
       
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_CACHE_ITEM; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _getCacheItemCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.GET_CACHE_ITEM;
        }

        protected override void CreateCommand()
        {

            _getCacheItemCommand.requestId = base.RequestId;
            _getCacheItemCommand.MethodOverload = _methodOverload;
        }
    }
}