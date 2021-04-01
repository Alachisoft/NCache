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

using System.IO;

namespace Alachisoft.NCache.Client
{
    class UnRegisterBulkKeyNotificationCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.UnRegisterBulkKeyNotifCommand _unregisterBulkKeyNotifCommand;

        public UnRegisterBulkKeyNotificationCommand(string[] keys, short updateCallbackid, short removeCallbackid)
        {
            name = "UnRegisterBulkKeyNotificationCommand";

            _unregisterBulkKeyNotifCommand = new Alachisoft.NCache.Common.Protobuf.UnRegisterBulkKeyNotifCommand();
            _unregisterBulkKeyNotifCommand.keys.AddRange(keys);
            _unregisterBulkKeyNotifCommand.removeCallbackId = removeCallbackid;
            _unregisterBulkKeyNotifCommand.updateCallbackId = updateCallbackid;
            _unregisterBulkKeyNotifCommand.requestId = base.RequestId;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.UNREGISTER_BULK_KEY_NOTIF; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.KeyBulkWrite; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _unregisterBulkKeyNotifCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.UNREGISTER_BULK_KEY_NOTIF;
        }

        protected override void CreateCommand()
        {
            _unregisterBulkKeyNotifCommand.requestId = base.RequestId;
        }
    }
}