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

using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Web.Command
{
    internal class UnSubscribeTopicCommand : CommandBase
    {
        private readonly Common.Protobuf.UnSubscribeTopicCommand _unSubscribeTopicCommand;

        public UnSubscribeTopicCommand(string topicName, string recepientId, SubscriptionType pubSubType)
        {
            name = "UnSubscribeTopicCommand";

            _unSubscribeTopicCommand = new Common.Protobuf.UnSubscribeTopicCommand();
            _unSubscribeTopicCommand.topicName = topicName;
            _unSubscribeTopicCommand.recepientId = recepientId;
            _unSubscribeTopicCommand.pubSubType = (int) pubSubType;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.UNSUBCRIBE; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        internal override bool IsKeyBased
        {
            get { return false; }
        }

        protected override void CreateCommand()
        {
            _command = new Common.Protobuf.Command();
            _command.requestID = base.RequestId;
            _command.unSubscribeTopicCommand = _unSubscribeTopicCommand;
            _command.type = Common.Protobuf.Command.Type.UNSUBSCRIBE_TOPIC;
            _command.clientLastViewId = ClientLastViewId;
            _command.intendedRecipient = IntendedRecipient;
            _command.version = "4200";
            _command.commandVersion = 1; // NCache 4.1 Onwards
        }
    }
}