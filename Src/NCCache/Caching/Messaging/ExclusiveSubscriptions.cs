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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Messaging
{
  internal  class ExclusiveSubscriptions : Subscriptions, ICompactSerializable
    {
       public ExclusiveSubscriptions(string clientId, string subscriptionId, SubscriptionPolicyType subscriptionPolicy, long expirationTime, CacheRuntimeContext context, SubscriptionType subscriptionType = SubscriptionType.Subscriber) : base(clientId, subscriptionId, subscriptionPolicy, expirationTime, context, subscriptionType) { }

        public override bool AddSubscriber(string clientid)
        {
            Hint.Reset(_context);
            if (string.IsNullOrEmpty(ClientID))
            {
                ClientID = clientid;
                return true;
            }
            else
            {
                return false;
            }
            
        }

        public override void Remove(string clientId)
        {
            if (ClientID == clientId)
            {
                Hint.Reset(_context);
                ClientID = null;
            }
        }

        public override bool IsActive()
        {
            return (SubscriptionPolicyType == SubscriptionPolicyType.NonDurableExclusiveSubscription) && ClientID != null;
        }

        public string ConnectedClient
        {
            get { return ClientID; }
        }

        public new void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
        }

        public new void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
        }
    }
}
