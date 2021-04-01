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

using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Client
{
    internal sealed class RegisterNotificationCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.RegisterNotifCommand _registerNotificationCommand;
        private NotificationsType _notifMask;

        public RegisterNotificationCommand(NotificationsType notifMask, EventDataFilter datafilter, short sequenceNumber)
        {
            base.name = "RegisterNotificationCommand";
            _registerNotificationCommand = new Alachisoft.NCache.Common.Protobuf.RegisterNotifCommand();
            _registerNotificationCommand.notifMask = (int)notifMask;
            _registerNotificationCommand.requestId = base.RequestId;
            _registerNotificationCommand.datafilter = (int)datafilter;
            _registerNotificationCommand.sequence = sequenceNumber;
        }

        public RegisterNotificationCommand(NotificationsType notifMask, short sequenceNumber)
            : this(notifMask, EventDataFilter.None, sequenceNumber)
        {
        }

        [System.Obsolete]
        public RegisterNotificationCommand(NotificationsType notifMask)
            : this(notifMask, -1)
        {
        }

        internal override CommandType CommandType
        {
            get { return CommandType.REGISTER_NOTIF; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        internal override bool IsKeyBased { get { return false; } }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.registerNotifCommand = _registerNotificationCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_NOTIF;
        }
    }
}
