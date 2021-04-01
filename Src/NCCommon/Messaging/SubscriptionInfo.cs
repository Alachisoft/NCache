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
using System;

namespace Alachisoft.NCache.Common
{
    public class SubscriptionInfo : ICompactSerializable
    {
        private string _subscriptionId;
        private string _clientId;
        private SubscriptionType _type;
        private SubscriptionPolicyType _subscriptionPolicy;
        private long _expiration;
        private long _creationTime;

        public const string EventsSubscriptionName = "$EventsSubscription$";

        public string SubscriptionId
        {
            get { return _subscriptionId; }
            set { _subscriptionId = value; }
        }

        public string ClientId
        {
            get { return _clientId; }
            set { _clientId = value; }
        }

        public SubscriptionType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        internal SubscriptionPolicyType SubPolicyType
        {
            get { return _subscriptionPolicy; }
            set { _subscriptionPolicy = value; }
        }

        public long Expiration
        {
            get { return _expiration; }
            set { _expiration = value; }
        }

        public long CreationTime
        {
            get { return _creationTime; }
            set { _creationTime = value; }
        }


        public DateTime? InitializeExpiration()
        {
            AbsoluteExpiratoinTime = DateTime.UtcNow.Add(new TimeSpan(Expiration));
            return AbsoluteExpiratoinTime;
        }

        public bool ExpirationCheck()
        {
            long expCheck = TimeSpan.MaxValue.Ticks;
            if (expCheck != _expiration)
                return true;
            else
                return false;
        }

        public DateTime? AbsoluteExpiratoinTime
        {
            get;
            private set;
        }

        public void Deserialize(CompactReader reader)
        {
            _subscriptionId = reader.ReadObject() as string;
            _clientId = reader.ReadObject() as string;
            _type = (SubscriptionType)reader.ReadByte();
            _subscriptionPolicy = (SubscriptionPolicyType)reader.ReadByte();
            _expiration = reader.ReadInt64();
            _creationTime = reader.ReadInt64();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_subscriptionId);
            writer.WriteObject(_clientId);
            writer.Write((byte)_type);
            writer.Write((byte)_subscriptionPolicy);
            writer.Write(_expiration);
            writer.Write(_creationTime);
        }
    }
}