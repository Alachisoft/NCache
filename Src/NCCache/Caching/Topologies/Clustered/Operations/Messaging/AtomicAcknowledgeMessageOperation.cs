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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Operations
{
    public class AtomicAcknowledgeMessageOperation : ICompactSerializable
    {
        private string _clientId;
        private string _topic;
        private string _messageId;
        private OperationContext _operationContext;

        public string Topic
        {
            get
            {
                return _topic;
            }
        }

        public string MessageId
        {
            get { return _messageId; }
        }
        public OperationContext OperationContext
        {
            get
            {
                return _operationContext;
            }
        }

        public string ClientId
        {
            get
            {
                return _clientId;
            }
        }

        public AtomicAcknowledgeMessageOperation(string clientID,string topic,string messageId, OperationContext operationContext)
        {
            _clientId = clientID;
            _topic = topic;
            _messageId = messageId;
            _operationContext = operationContext;
        }

        public void Deserialize(CompactReader reader)
        {
            _clientId = reader.ReadObject() as string;
            _topic = reader.ReadObject() as string;
            _messageId = reader.ReadObject() as string;
            _operationContext = reader.ReadObject() as OperationContext;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_clientId);
            writer.WriteObject(_topic);
            writer.WriteObject(_messageId);
            writer.WriteObject(_operationContext);
        }
    }
}