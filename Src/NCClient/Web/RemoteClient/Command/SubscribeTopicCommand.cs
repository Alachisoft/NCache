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
using Alachisoft.NCache.Runtime.Caching;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal class SubscribeTopicCommand : CommandBase
    {
        private readonly Common.Protobuf.SubscribeTopicCommand _subscribeTopicCommand;

        public SubscribeTopicCommand(string topicName, string subscriptionName, SubscriptionType pubSubType, long creationTime, long expiration, SubscriptionPolicyType subscriptionPolicy)
        {
            name = "SubscribeTopicCommand";

            _subscribeTopicCommand = new Common.Protobuf.SubscribeTopicCommand();
            _subscribeTopicCommand.topicName = topicName;
            _subscribeTopicCommand.subscriptionName = subscriptionName;
            _subscribeTopicCommand.pubSubType = (int)pubSubType;
            _subscribeTopicCommand.subscriptionPolicy = (int)subscriptionPolicy;
            _subscribeTopicCommand.creationTime = creationTime;
            _subscribeTopicCommand.expirationTime = expiration;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.SUBCRIBE; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        internal override bool IsKeyBased { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _subscribeTopicCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.SUBSCRIBE_TOPIC;
        }

        protected override void CreateCommand()
        {
            _subscribeTopicCommand.requestId = base.RequestId;
            _subscribeTopicCommand.clientLastViewId = ClientLastViewId;
            _subscribeTopicCommand.intendedRecipient = IntendedRecipient;
            _subscribeTopicCommand.version = "4200";
            _subscribeTopicCommand.commandVersion = 1; // NCache 4.1 Onwards
        }
    }
}
