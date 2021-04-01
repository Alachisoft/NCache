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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common
{
    public class SubscriptionOperation : TopicOperation, ICompactSerializable
    {
        private SubscriptionInfo _subscriptionInfo;
        private bool _dispose = false;

        public SubscriptionInfo SubscriptionInfo
        {
            get { return _subscriptionInfo; }
            set { _subscriptionInfo = value; }
        }

        public bool IsDispose
        {
            get { return _dispose; }
            set { _dispose = value; }
        }

        public SubscriptionOperation(string topic, TopicOperationType topicOperationType, SubscriptionInfo subscriptionInfo,bool isDispose=false)
            : base(topic, topicOperationType)
        {
            _subscriptionInfo = subscriptionInfo;
            _dispose = isDispose;
        }
        

        public override void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _subscriptionInfo = reader.ReadObject() as SubscriptionInfo;
            IsDispose= reader.ReadBoolean();
        }

        public override void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.WriteObject(_subscriptionInfo);
            writer.Write(IsDispose);
        }
    }
}
