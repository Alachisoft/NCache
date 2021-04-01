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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Messaging
{
    class EventSubscriptions :Subscriptions, ICompactSerializable
    {

        List<string> _connectedClients;

        public EventSubscriptions(string clientId, string subscriptionId, SubscriptionPolicyType subscriptionPolicy, long expirationTime, CacheRuntimeContext context, SubscriptionType subscriptionType = SubscriptionType.Subscriber) : base(clientId, subscriptionId, subscriptionPolicy, expirationTime, context, subscriptionType)
        {
            _connectedClients = new List<string>();
            AddSubscriber(clientId);
        }
        

        public override bool AddSubscriber(string clientId)
        {
            if (!string.IsNullOrEmpty(clientId))
            {
                _connectedClients.Add(clientId);
                return true;
            }
            else
                return false;
        }

        public override bool IsActive()
        {
            return (_connectedClients.Count > 0);
        }

        public override List<string> GetSubscriberList()
        {
            return _connectedClients;
        }


        public override void Remove(string clientId)
        {
            if (_connectedClients.Contains(clientId))
            {
                _connectedClients.Remove(clientId);
            }
        }
        public new void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _connectedClients = SerializationUtility.DeserializeList<string>(reader);
        }

        public new void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            SerializationUtility.SerializeList<string>(_connectedClients, writer);
        }
    }
}
