
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
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Collections.Generic;

namespace Alachisoft.NCache.Common
{
    public class ExpireSubscriptionOperation : TopicOperation, ICompactSerializable
    {
        private IList<SubscriptionIdentifier> _subscriptionIdentifier;

        public IList<SubscriptionIdentifier> SubscriptionIdentifier
        {
            get { return _subscriptionIdentifier; }
            set { _subscriptionIdentifier = value; }
        }

        public ExpireSubscriptionOperation(string topic, TopicOperationType topicOperationType, IList<SubscriptionIdentifier> subscriptionIdentifier)
            : base(topic, topicOperationType)
        {
            _subscriptionIdentifier = subscriptionIdentifier;
        }


        public override void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _subscriptionIdentifier = SerializationUtility.DeserializeList<SubscriptionIdentifier>(reader);
        }

        public override void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            SerializationUtility.SerializeList(_subscriptionIdentifier, writer);
        }
    }
}
