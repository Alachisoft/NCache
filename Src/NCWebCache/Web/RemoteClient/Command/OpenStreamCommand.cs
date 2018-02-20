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
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Dependencies;

namespace Alachisoft.NCache.Web.Command
{
    class OpenStreamCommand : CommandBase
    {
        int _methodOverlaod;
        private Alachisoft.NCache.Common.Protobuf.OpenStreamCommand _openStreamCommand;

        public OpenStreamCommand(string key, StreamModes mode, string group, string subgroup, DateTime absolute,
            TimeSpan sliding, CacheDependency dependency, Runtime.CacheItemPriority priority, int methodOverload)
        {
            base.name = "OpenStreamCommand";
            _openStreamCommand = new Alachisoft.NCache.Common.Protobuf.OpenStreamCommand();
            _openStreamCommand.key = key;
            _openStreamCommand.streamMode = (int) mode;
            _openStreamCommand.group = group;
            _openStreamCommand.subGroup = subgroup;
            _methodOverlaod = methodOverload;

            if (absolute != Alachisoft.NCache.Web.Caching.Cache.NoAbsoluteExpiration)
                _openStreamCommand.absoluteExpiration = absolute.Ticks;

            if (sliding != Alachisoft.NCache.Web.Caching.Cache.NoSlidingExpiration)
                _openStreamCommand.slidingExpiration = sliding.Ticks;

            _openStreamCommand.priority = (int) priority;
            _openStreamCommand.requestId = base.RequestId;

            if (dependency != null)
                _openStreamCommand.dependency =
                    Alachisoft.NCache.Common.Util.DependencyHelper.GetProtoBufDependency(dependency);
        }

        internal override CommandType CommandType
        {
            get { return CommandType.OPEN_STREAM; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.openStreamCommand = _openStreamCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.OPEN_STREAM;
            base._command.MethodOverload = _methodOverlaod;
        }
    }
}