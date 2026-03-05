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
