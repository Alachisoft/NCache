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
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Operations
{
    public class AssignmentOperation : ICompactSerializable
    {
        private MessageInfo _messageInfo;
        private SubscriptionInfo _subscriptionInfo;
        private TopicOperationType _type;
        private OperationContext _context;

        public AssignmentOperation(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo, TopicOperationType type,OperationContext context)
        {
            _messageInfo = messageInfo;
            _subscriptionInfo = subscriptionInfo;
            _type = type;
            _context = context;
        }

        public MessageInfo MessageInfo
        {
            get
            {
                return _messageInfo;
            }
        }

        public SubscriptionInfo SubscriptionInfo
        {
            get
            {
                return _subscriptionInfo;
            }
        }

        public TopicOperationType Type
        {
            get
            {
                return _type;
            }
        }

        public OperationContext Context
        {
            get { return _context; }
            set { _context = value; }
        }

        public void Deserialize(CompactReader reader)
        {
            _messageInfo = reader.ReadObject() as MessageInfo;
            _subscriptionInfo = reader.ReadObject() as SubscriptionInfo;
            _type = (TopicOperationType)reader.ReadInt32();
            _context = reader.ReadObject() as OperationContext;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_messageInfo);
            writer.WriteObject(_subscriptionInfo);
            writer.Write((int)_type);
            writer.WriteObject(_context);
        }

        public override string ToString()
        {
            return " Topic Name: " + _messageInfo.Topic + " Message ID: " + _messageInfo.MessageId + " Topic Opertion: " + _type.ToString();
        }
    }
}
