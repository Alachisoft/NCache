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
    class RegisterBulkKeyNotificationCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.RegisterBulkKeyNotifCommand _registerBulkKeyNotifCommand;

        public RegisterBulkKeyNotificationCommand(string[] keys, short updateCallbackid, short removeCallbackid, string clientId, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            name = "RegisterBulkKeyNotificationCommand";

            _registerBulkKeyNotifCommand = new Alachisoft.NCache.Common.Protobuf.RegisterBulkKeyNotifCommand();
            _registerBulkKeyNotifCommand.keys.AddRange(keys);
            _registerBulkKeyNotifCommand.removeCallbackId = removeCallbackid;
            _registerBulkKeyNotifCommand.updateCallbackId = updateCallbackid;
            _registerBulkKeyNotifCommand.requestId = base.RequestId;
            _registerBulkKeyNotifCommand.callbackType = CallbackType(callbackType);
            _registerBulkKeyNotifCommand.surrogateClientID = clientId;
        }

        private int CallbackType(CallbackType type)
        {
            if (type == Runtime.Events.CallbackType.PullBasedCallback)
                return 0;
            else if (type == Runtime.Events.CallbackType.PushBasedNotification)
                return 1;
            else
                return 0;
        }

        public RegisterBulkKeyNotificationCommand(string[] key, short update, short remove, EventDataFilter dataFilter, bool notifyOnItemExpiration, CallbackType callbackType = Runtime.Events.CallbackType.PullBasedCallback)
            : this(key, update, remove, null, callbackType)

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
            get { return RequestType.KeyBulkWrite; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _registerBulkKeyNotifCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.REGISTER_BULK_KEY_NOTIF;
        }

        protected override void CreateCommand()
        {
            _registerBulkKeyNotifCommand.requestId = base.RequestId;
        }
    }
}
