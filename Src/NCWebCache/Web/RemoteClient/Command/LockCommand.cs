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
using System.IO;

using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Web.Caching.Util;

using Alachisoft.NCache.Web.Communication;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class LockCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.LockCommand _lockCommand;

        public LockCommand(string key, TimeSpan lockTimeout)
        {
            base.name = "LockCommand";
            base.key = key;

            _lockCommand = new Alachisoft.NCache.Common.Protobuf.LockCommand();
            _lockCommand.key = key;
            _lockCommand.lockTimeout = lockTimeout.Ticks;
            _lockCommand.requestId = base.RequestId;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.LOCK; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.lockCommand = _lockCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.LOCK;

        }
    }
}
