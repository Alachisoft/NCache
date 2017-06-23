// Copyright (c) 2017 Alachisoft
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
using System.Text;

using Alachisoft.NCache.Web.Caching;
using System.IO;
using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Web.Caching.Util;

namespace Alachisoft.NCache.Web.Command
{
    class UnRegisterKeyNotificationCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.UnRegisterKeyNotifCommand _unregisterKeyNotifCommand;

        public UnRegisterKeyNotificationCommand(string key, short updateCallbackid, short removeCallbackid)
        {
            base.name = "UnRegisterKeyNotificationCommand";
            base.key = key;

            _unregisterKeyNotifCommand = new Alachisoft.NCache.Common.Protobuf.UnRegisterKeyNotifCommand();
            _unregisterKeyNotifCommand.key = key;
            _unregisterKeyNotifCommand.removeCallbackId = removeCallbackid;
            _unregisterKeyNotifCommand.updateCallbackId = updateCallbackid;
            _unregisterKeyNotifCommand.requestId = base.RequestId;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.UNREGISTER_KEY_NOTIF; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.unRegisterKeyNotifCommand = _unregisterKeyNotifCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.UNREGISTER_KEY_NOTIF;

        }
    }
}
