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

using System.Collections;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class MessagePublishCommand : CommandBase
    {
        private readonly Common.Protobuf.MessagePublishCommand _publishMessageCommand;

        public MessagePublishCommand(string messageId, byte[] payLoad, long creationTime, long expirationTime,
            Hashtable metadata, BitSet flagMap)
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

        internal override bool IsSafe
        {
            get { return false; }
        }

        protected override void CreateCommand()
        {
            _command = new Common.Protobuf.Command();
            _command.requestID = RequestId;
            _command.messagePublishCommand = _publishMessageCommand;
            _command.type = Common.Protobuf.Command.Type.MESSAGE_PUBLISH;
            _command.clientLastViewId = ClientLastViewId;
            _command.intendedRecipient = IntendedRecipient;
            _command.version = "4200";
            _command.commandVersion = 1;
        }
    }
}