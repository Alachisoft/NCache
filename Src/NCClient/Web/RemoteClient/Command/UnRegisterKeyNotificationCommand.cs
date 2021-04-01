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

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _unregisterKeyNotifCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.UNREGISTER_KEY_NOTIF;
        }

        protected override void CreateCommand()
        {

            _unregisterKeyNotifCommand.requestId = base.RequestId;
        }
    }
}
