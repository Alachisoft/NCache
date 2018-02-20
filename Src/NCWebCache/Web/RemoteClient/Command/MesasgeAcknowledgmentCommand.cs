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

using System.Collections.Generic;
using System.Collections;
using Alachisoft.NCache.Common.Protobuf;

namespace Alachisoft.NCache.Web.Command
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
                var keyValue = new KeyValue {key = item.Key.ToLower()};

                var set = item.Value as List<string>;
                if (set != null)
                {
                    foreach (string value in set)
                    {
                        var valueWithType = new ValueWithType {value = value};
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

        protected override void CreateCommand()
        {
            _command = new Common.Protobuf.Command();
            _command.requestID = base.RequestId;
            _command.mesasgeAcknowledgmentCommand = _mesasgeAckCommand;
            _command.type = Common.Protobuf.Command.Type.MESSAGE_ACKNOWLEDGMENT;
            _command.clientLastViewId = ClientLastViewId;
            _command.intendedRecipient = IntendedRecipient;
            _command.version = "4200";
            _command.commandVersion = 1;
        }
    }
}