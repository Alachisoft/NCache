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

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Protobuf;
using System.Collections;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class MessagePublishCommand : CommandBase
    {
        private readonly Common.Protobuf.MessagePublishCommand _publishMessageCommand;

        public MessagePublishCommand(string messageId, byte[] payLoad, long creationTime, long expirationTime, Hashtable metadata, BitSet flagMap)
        {
            name = "PublishMessageCommand";

            _publishMessageCommand = new Common.Protobuf.MessagePublishCommand();
            _publishMessageCommand.messageId = messageId;
            key = messageId;
            UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(payLoad);
            _publishMessageCommand.data.AddRange(ubObject.DataList);

            _publishMessageCommand.flag = flagMap.Data;
            _publishMessageCommand.expiration = expirationTime;
            _publishMessageCommand.creationTime = creationTime;
            _publishMessageCommand.requestId = RequestId;
            _publishMessageCommand.isAsync = isAsync;

            foreach (DictionaryEntry entry in metadata)
            {
                KeyValuePair keyValue = new KeyValuePair();
                keyValue.key = entry.Key.ToString();
                keyValue.value = entry.Value.ToString();

                _publishMessageCommand.keyValuePair.Add(keyValue);
            }
        }

        internal override CommandType CommandType
        {
            get { return CommandType.PUBLISHMESSAGE; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        internal override bool IsSafe { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _publishMessageCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.MESSAGE_PUBLISH;
        }

        protected override void CreateCommand()
        {
            _publishMessageCommand.requestId = RequestId;
            _publishMessageCommand.clientLastViewId = ClientLastViewId;
            _publishMessageCommand.intendedRecipient = IntendedRecipient;
            _publishMessageCommand.version = "4200";
            _publishMessageCommand.commandVersion = 1;
        }
    }
}
