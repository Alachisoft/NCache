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

using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Web.Command
{
    class RegisterBulkKeyNotificationCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.RegisterBulkKeyNotifCommand _registerBulkKeyNotifCommand;
        
        public RegisterBulkKeyNotificationCommand(string[] keys, short updateCallbackid, short removeCallbackid)
        {
            name = "RegisterBulkKeyNotificationCommand";

            _registerBulkKeyNotifCommand = new Alachisoft.NCache.Common.Protobuf.RegisterBulkKeyNotifCommand();
            _registerBulkKeyNotifCommand.keys.AddRange(keys);
            _registerBulkKeyNotifCommand.removeCallbackId = removeCallbackid;
            _registerBulkKeyNotifCommand.updateCallbackId = updateCallbackid;
            _registerBulkKeyNotifCommand.requestId = base.RequestId;

        }

        public RegisterBulkKeyNotificationCommand(string[] key, short update, short remove, EventDataFilter dataFilter, bool notifyOnItemExpiration)
            : this(key, update, remove)
        {
            _registerBulkKeyNotifCommand.datafilter = (int)dataFilter;
            _registerBulkKeyNotifCommand.notifyOnExpiration = notifyOnItemExpiration;
        }


        internal override CommandType CommandType
        {
            get { return CommandType.REGISTER_BULK_KEY_NOTIF; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.BulkWrite; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.registerBulkKeyNotifCommand = _registerBulkKeyNotifCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_BULK_KEY_NOTIF;

        }

    }
}
