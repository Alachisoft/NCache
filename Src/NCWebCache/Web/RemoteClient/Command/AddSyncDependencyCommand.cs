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

using Alachisoft.NCache.Web.Caching;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class AddSyncDependencyCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.AddSyncDependencyCommand _addSyncDependencyCommand;
        private CacheSyncDependency _syncDependency;
        private int _methodOverload;

        public AddSyncDependencyCommand(string key, CacheSyncDependency syncDependency, int methodOverload)
        {
            base.name = "AddSyncDependencyCommand";
            _addSyncDependencyCommand = new Alachisoft.NCache.Common.Protobuf.AddSyncDependencyCommand();

            _addSyncDependencyCommand.key = key;
            _addSyncDependencyCommand.requestId = base.RequestId;

            if (syncDependency != null)
            {
                _addSyncDependencyCommand.syncDependency = new Alachisoft.NCache.Common.Protobuf.SyncDependency();
                _addSyncDependencyCommand.syncDependency.key = syncDependency.Key;
                _addSyncDependencyCommand.syncDependency.cacheId = syncDependency.CacheId;
                _addSyncDependencyCommand.syncDependency.server = syncDependency.Server;
                _addSyncDependencyCommand.syncDependency.port = syncDependency.Port;
            }

            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.ADD_SYNC_DEPENDENCY; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.addSyncDependencyCommand = _addSyncDependencyCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_SYNC_DEPENDENCY;
            base._command.MethodOverload = _methodOverload;
        }
    }
}