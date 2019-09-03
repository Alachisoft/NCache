
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common
{

    public class SubscriptionIdentifier: ICompactSerializable,ICloneable
    {
        private string _subscriptionName;
        private SubscriptionPolicyType _subscriptionPolicy;

        public SubscriptionIdentifier() { }

        internal SubscriptionIdentifier(string subscriptionName, SubscriptionPolicyType subscriptionPolicy)
        {
            _subscriptionName = subscriptionName;
            _subscriptionPolicy = subscriptionPolicy;
        }

        public string SubscriptionName
        {
            set { _subscriptionName = value; }
            get { return _subscriptionName; }
        }

        internal SubscriptionPolicyType SubscriptionPolicy
        {
            set { _subscriptionPolicy = value; }
            get { return _subscriptionPolicy; }
        }

        public override int GetHashCode()
        {
            var hashcode = SubscriptionName + SubscriptionPolicy;
            return hashcode.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is SubscriptionIdentifier)
            {
                var other = obj as SubscriptionIdentifier;
                if (string.Compare(this.SubscriptionName, other.SubscriptionName, true) == 0 && this.SubscriptionPolicy == other.SubscriptionPolicy)
                    return true;
            }
            return false;
        }

        public void Deserialize(CompactReader reader)
        {
            _subscriptionName = reader.ReadObject() as string;
            _subscriptionPolicy = (SubscriptionPolicyType)reader.ReadByte();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_subscriptionName);
            writer.Write((byte)_subscriptionPolicy);
        }

        public object Clone()
        {
            SubscriptionIdentifier subscriptionId = new SubscriptionIdentifier();
            subscriptionId.SubscriptionName = this._subscriptionName;
            subscriptionId.SubscriptionPolicy = this._subscriptionPolicy;
            return subscriptionId;
        }
    }


}
