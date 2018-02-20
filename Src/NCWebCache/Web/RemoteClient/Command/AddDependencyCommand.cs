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

using Alachisoft.NCache.Runtime.Dependencies;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class AddDependencyCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.AddDependencyCommand _addDependencyCommand;
        private int _methodOverload;

        public AddDependencyCommand(string key, CacheDependency dependency, bool isResync, int methodOverload)
        {
            base.name = "AddDependencyCommand";
            base.key = key;

            _addDependencyCommand = new Alachisoft.NCache.Common.Protobuf.AddDependencyCommand();

            _addDependencyCommand.key = key;
            _addDependencyCommand.isResync = isResync;
            _addDependencyCommand.requestId = base.RequestId;
            _addDependencyCommand.dependency =
                Alachisoft.NCache.Common.Util.DependencyHelper.GetProtoBufDependency(dependency);
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.ADD_DEPENDENCY; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void CreateCommand()
        {
            _command = new Alachisoft.NCache.Common.Protobuf.Command();
            _command.requestID = base.RequestId;
            _command.addDependencyCommand = _addDependencyCommand;
            _command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_DEPENDENCY;
            base._command.MethodOverload = _methodOverload;
        }
    }
}