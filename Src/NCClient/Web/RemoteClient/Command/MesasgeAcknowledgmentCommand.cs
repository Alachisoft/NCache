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

using System.Collections.Generic;
using System.Collections;
using Alachisoft.NCache.Common.Protobuf;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal class MesasgeAcknowledgmentCommand : CommandBase
    {
        readonly Common.Protobuf.MesasgeAcknowledgmentCommand _mesasgeAckCommand;

        public MesasgeAcknowledgmentCommand(IDictionary<string, IList<string>> topicWiseMessageIds)
        {
            name = "MesasgeAcknowledgmentCommand";
            _mesasgeAckCommand = new Common.Protobuf.MesasgeAcknowledgmentCommand();
            PopulateValues(topicWiseMessageIds, _mesasgeAckCommand.values);
        }

        private void PopulateValues(IDictionary<string, IList<string>> from, List<KeyValue> to)
        {
            IEnumerator ide = from.GetEnumerator();
            foreach (var item in from)
            {
                var keyValue = new KeyValue { key = item.Key.ToLower() };

                var set = item.Value as List<string>;
                if (set != null)
                {
                    foreach (string value in set)
                    {
                        var valueWithType = new ValueWithType { value = value };
                        keyValue.value.Add(valueWithType);
                    }
                }
                to.Add(keyValue);
            }

        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.KeyBulkRead; }
        }

        internal override CommandType CommandType
        {
            get { return CommandType.MESSAGE_ACKNOWLEDGMENT; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _mesasgeAckCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.MESSAGE_ACKNOWLEDGMENT;
        }

        protected override void CreateCommand()
        {
            _mesasgeAckCommand.requestId = base.RequestId;
            _mesasgeAckCommand.clientLastViewId = ClientLastViewId;
            _mesasgeAckCommand.intendedRecipient = IntendedRecipient;
            _mesasgeAckCommand.version = "4200";
            _mesasgeAckCommand.commandVersion = 1; // NCache 4.1 Onwards

        }
    }
}
