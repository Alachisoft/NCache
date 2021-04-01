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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Caching;

namespace Alachisoft.NCache.Caching.Messaging
{
    public class MultiCastMessage : Message , ICompactSerializable
    {
        private List<SubscriptionInfo> specificReciepients = new List<SubscriptionInfo>();
       
        public MultiCastMessage(string messageId) : base(messageId)
        {
           
        }

        public List<SubscriptionInfo> SpecificReciepients { get { return specificReciepients; } set { specificReciepients = value; } }

        public void AddSpecificReciepients(List<string> clientIds)
        {
            foreach (string clientId in clientIds)
            {
                SubscriptionInfo receipient = new SubscriptionInfo();
                receipient.ClientId = clientId;
                receipient.SubscriptionId = SubscriptionInfo.EventsSubscriptionName;
                receipient.Type = SubscriptionType.Subscriber;
                receipient.SubPolicyType = SubscriptionPolicyType.EventSubscription;
                specificReciepients.Add(receipient);
            }
        }

        #region	ICompactSerializable Impl

        public override void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            SpecificReciepients = SerializationUtility.DeserializeList<SubscriptionInfo>(reader);
        }

        public override void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            SerializationUtility.SerializeList(SpecificReciepients, writer);
        }

        #endregion
    }
}
