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

using System;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class GetCacheItemCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetCacheItemCommand _getCacheItemCommand;

        private string _group;
        private string _subGroup;
        private LockAccessType _accessType;
        private object _lockId;
        private TimeSpan _lockTimeout;
        private int _methodOverload;

        public GetCacheItemCommand(string key, BitSet flagMap, string group, string subGroup, LockAccessType accessType,
            object lockId, TimeSpan lockTimeout, ulong version, string providerName, int methodOverload)
        {
            base.name = "GetCacheItemCommand";
            base.key = key;

            _getCacheItemCommand = new Alachisoft.NCache.Common.Protobuf.GetCacheItemCommand();
            _getCacheItemCommand.key = key;
            _getCacheItemCommand.requestId = base.RequestId;
            _getCacheItemCommand.group = group;
            _getCacheItemCommand.subGroup = subGroup;
            _getCacheItemCommand.flag = flagMap.Data;

            _getCacheItemCommand.lockInfo = new Alachisoft.NCache.Common.Protobuf.LockInfo();
            _getCacheItemCommand.lockInfo.lockAccessType = (int) accessType;
            if (lockId != null) _getCacheItemCommand.lockInfo.lockId = lockId.ToString();
            _getCacheItemCommand.lockInfo.lockTimeout = lockTimeout.Ticks;
            _methodOverload = methodOverload;
            _getCacheItemCommand.version = version;
            _getCacheItemCommand.providerName = providerName;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_CACHE_ITEM; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.getCacheItemCommand = _getCacheItemCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_CACHE_ITEM;
            base._command.MethodOverload = _methodOverload;
        }
    }
}