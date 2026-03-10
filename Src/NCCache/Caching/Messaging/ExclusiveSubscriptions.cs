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
