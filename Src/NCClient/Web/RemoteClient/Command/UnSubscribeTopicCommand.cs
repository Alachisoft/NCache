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
    internal class UnSubscribeTopicCommand : CommandBase
    {
        private readonly Common.Protobuf.UnSubscribeTopicCommand _unSubscribeTopicCommand;

        public UnSubscribeTopicCommand(string topicName, string recepientId, SubscriptionType pubSubType, SubscriptionPolicyType subscriptionPolicy,bool isDispose)
        {
            name = "UnSubscribeTopicCommand";

            _unSubscribeTopicCommand = new Common.Protobuf.UnSubscribeTopicCommand();
            _unSubscribeTopicCommand.topicName = topicName;
            _unSubscribeTopicCommand.recepientId = recepientId;
            _unSubscribeTopicCommand.pubSubType = (int)pubSubType;
            _unSubscribeTopicCommand.subscriptionPolicy = (int)subscriptionPolicy;
            _unSubscribeTopicCommand.isDispose = isDispose;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.UNSUBCRIBE; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        internal override bool IsKeyBased { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _unSubscribeTopicCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.UNSUBSCRIBE_TOPIC;
        }

        protected override void CreateCommand()
        {
            
            _unSubscribeTopicCommand.requestId = base.RequestId;
            _unSubscribeTopicCommand.clientLastViewId = ClientLastViewId;
            _unSubscribeTopicCommand.intendedRecipient = IntendedRecipient;
            _unSubscribeTopicCommand.version = "4200";
            _unSubscribeTopicCommand.commandVersion = 1; // NCache 4.1 Onwards
        }
    }
}
