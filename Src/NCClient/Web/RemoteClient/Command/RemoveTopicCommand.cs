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
    internal class RemoveTopicCommand : CommandBase
    {
        private readonly Common.Protobuf.RemoveTopicCommand _removeTopicCommand;

        public RemoveTopicCommand(string topicName, bool forcefully)
        {
            name = "RemoveTopicCommand";

            _removeTopicCommand = new Common.Protobuf.RemoveTopicCommand();
            _removeTopicCommand.topicName = topicName;
            _removeTopicCommand.forcefully = forcefully;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.REMOVE_TOPIC; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        internal override bool IsKeyBased { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _removeTopicCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.REMOVE_TOPIC;
        }

        protected override void CreateCommand()
        {
            
            _removeTopicCommand.requestId = base.RequestId;
            _removeTopicCommand.clientLastViewId = ClientLastViewId;
            _removeTopicCommand.intendedRecipient = IntendedRecipient;
            _removeTopicCommand.version = "4200";
            _removeTopicCommand.commandVersion = 1; // NCache 4.1 Onwards
            
        }
    }
}
