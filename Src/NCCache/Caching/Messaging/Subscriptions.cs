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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Messaging
{
    public abstract class Subscriptions: ICompactSerializable
    {
        private SubscriptionPolicyType _subscriptionPolicy;
        private string _clientId;
        private string _subscriptionId;
        private IdleExpiration _expiration;
        internal CacheRuntimeContext _context;
        private SubscriptionType _subscriptionType;
        private long _expirationTime;

        internal Subscriptions(string clientId, string subscriptionId, SubscriptionPolicyType subscriptionPolicy, long expirationTime, CacheRuntimeContext context, SubscriptionType subscriptionType = SubscriptionType.Subscriber)
        {
            _clientId = clientId;
            _subscriptionPolicy = subscriptionPolicy;
            _context = context;
            _expiration = IdleExpiration.Create(null, new TimeSpan(expirationTime));

            _subscriptionType = subscriptionType;
            _expirationTime = expirationTime;
        }

        public string SubscriptionId
        {
            get { return _subscriptionId; }
            set { _subscriptionId = value; }
        }

        public long ExpirationTime
        {
            get { return _expirationTime; }
        }

        public SubscriptionType GetSubscriptionType
        {
            get { return _subscriptionType; }
        }
        public string ClientID
        {
            get { return _clientId; }
            set { this._clientId = value; }
        }

        public IdleExpiration Hint
        {
            get { return _expiration; }
        }

        public void ResetExpiration()
        {
            Hint.Reset(_context);
        }

        internal SubscriptionPolicyType SubscriptionPolicyType
        {
            get { return _subscriptionPolicy; }

        }

        public virtual string GetSubscriber()
        {
            return ClientID;
        }

        public virtual void Remove(string clientId)
        {
            if (string.Compare(clientId, this.ClientID) == 0)
                this.ClientID = null;
        }

        public virtual bool AddSubscriber(string clientid)
        {
            if (string.IsNullOrEmpty(_clientId))
            {
                _clientId = clientid;
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual bool IsActive()
        {
            return this.ClientID != null;
        }

        public virtual List<string> GetSubscriberList()
        {
            var subscriberList = new List<string>();

            if (this.ClientID != null)
                subscriberList.Add(ClientID);

            return subscriberList;
        }

        public void Deserialize(CompactReader reader)
        {
            _clientId = reader.ReadObject() as string;
            _subscriptionId = reader.ReadObject() as string;
            _subscriptionPolicy = (SubscriptionPolicyType)reader.ReadByte();
            _expirationTime = reader.ReadInt64();
            _subscriptionType = (SubscriptionType)reader.ReadByte();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_clientId);
            writer.WriteObject(_subscriptionId);
            writer.Write((byte)_subscriptionPolicy);
            writer.Write(_expirationTime);
            writer.Write((byte)_subscriptionType);
        }
    }
}
