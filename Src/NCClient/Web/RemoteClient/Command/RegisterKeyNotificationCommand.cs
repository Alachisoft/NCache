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

using Alachisoft.NCache.Runtime.Events;
using System.IO;

namespace Alachisoft.NCache.Client
{
    class RegisterKeyNotificationCommand : CommandBase
    {
        Alachisoft.NCache.Common.Protobuf.RegisterKeyNotifCommand _registerKeyNotifCommand;
        short _updateCallbackId;
        short _removeCallabackId;

        public RegisterKeyNotificationCommand(string key, short updateCallbackid, short removeCallbackid, bool notifyOnItemExpiration, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            base.name = "RegisterKeyNotificationCommand";
            base.key = key;

            _registerKeyNotifCommand = new Alachisoft.NCache.Common.Protobuf.RegisterKeyNotifCommand();
            _registerKeyNotifCommand.key = key;

            _registerKeyNotifCommand.removeCallbackId = removeCallbackid;
            _registerKeyNotifCommand.updateCallbackId = updateCallbackid;
            _registerKeyNotifCommand.notifyOnExpiration = notifyOnItemExpiration;
            _registerKeyNotifCommand.callbackType = (int)callbackType;

            _registerKeyNotifCommand.requestId = base.RequestId;
        }

        public RegisterKeyNotificationCommand(string key, short update, short remove, EventDataFilter dataFilter, bool notifyOnItemExpiration, CallbackType callbackType = CallbackType.PushBasedNotification)
            : this(key, update, remove, notifyOnItemExpiration, callbackType)

        {
            _registerKeyNotifCommand.datafilter = (int)dataFilter;
        }


        internal override CommandType CommandType
        {
            get { return CommandType.REGISTER_KEY_NOTIF; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _registerKeyNotifCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.REGISTER_KEY_NOTIF;
        }

        protected override void CreateCommand()
        {
            //base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            //base._command.requestID = base.RequestId;
            //base._command.registerKeyNotifCommand = _registerKeyNotifCommand;
            //base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_KEY_NOTIF;
            _registerKeyNotifCommand.requestId = base.RequestId;
        }
    }
}
