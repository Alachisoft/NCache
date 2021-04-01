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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Messaging;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Messaging
{  
    internal class ClientSubscriptionManager : ISubscription, IDisposable
    {
        private HashSet<SubscriptionIdentifier> _refreshSubscriptions = new HashSet<SubscriptionIdentifier>();
        private IDictionary<SubscriptionIdentifier, Subscribers> _messageSubscriptions = new Dictionary<SubscriptionIdentifier, Subscribers>();
        private IDictionary<SubscriptionIdentifier, Subscribers> _deliverySubscriptions = new Dictionary<SubscriptionIdentifier, Subscribers>();
        private OrderedDictionary _messages = new OrderedDictionary(StringComparer.InvariantCultureIgnoreCase);
        private OrderedDictionary _delivryNotificationMessages = new OrderedDictionary(StringComparer.InvariantCultureIgnoreCase);
        private RoundRobinLoadbalancer<SubscriptionIdentifier> _messageSubscriptionLoadbalancer = new RoundRobinLoadbalancer<SubscriptionIdentifier>(new SubscriptionIdentifierCompararer());
        private RoundRobinLoadbalancer<SubscriptionIdentifier> _deliverSubscriptionLoadbalancer = new RoundRobinLoadbalancer<SubscriptionIdentifier>(new SubscriptionIdentifierCompararer());
        private DateTime _lastActivityTime;
        private DateTime _updateTime;
        private DateTime _pollTime = DateTime.Now;
        private TimeSpan _idleTimeout = TimeSpan.FromSeconds(30);
        private Topic _topic;
        private bool _dipsoed;
        private ISubscriptionEventListener _subscriptionListener;
       

        public string ClientID { get; set; }

        public bool HasActiveSubscriptions { get { return _messageSubscriptions.Count > 0|| _deliverySubscriptions.Count >0; } }

        internal TimeSpan InactivityInterval
        {
            get
            {
               return DateTime.Now - _lastActivityTime;
            }
        }

        public bool IsActive
        {
            get
            {
                return InactivityInterval < _idleTimeout ;
            }
        }

        public bool HasNewMessages
        {
            get
            {
                return _messages.Count > 0 || _delivryNotificationMessages.Count > 0;
            }
        }

        public ClientSubscriptionManager(Topic topic,string clientid)
        {
            this._topic = topic;
            this.ClientID = clientid;
            this._subscriptionListener = topic;
            this._messages.LazyRemoval = true;
            this._delivryNotificationMessages.LazyRemoval = true;
        }

        public void AddSubscription(SubscriptionInfo subscriptionInfo)
        {
            if (String.Compare(subscriptionInfo.ClientId, ClientID, StringComparison.OrdinalIgnoreCase) != 0)
                return;

            SubscriptionIdentifier subscriptionIdentifier = new SubscriptionIdentifier(subscriptionInfo.SubscriptionId,subscriptionInfo.SubPolicyType);
            
            lock(this)
            {
                _lastActivityTime = _updateTime = DateTime.Now;
                switch(subscriptionInfo.Type)
                {
                    //add subscriptionIdentifier instead
                    case Common.Enum.SubscriptionType.Subscriber:
                        if (!_messageSubscriptions.ContainsKey(subscriptionIdentifier))
                        {
                            _messageSubscriptions.Add(subscriptionIdentifier, new Subscribers(subscriptionInfo.SubscriptionId, subscriptionInfo.Type));
                            _messageSubscriptionLoadbalancer.Add(subscriptionIdentifier);
                            if (subscriptionInfo.ExpirationCheck())
                            {
                                _refreshSubscriptions.Add(subscriptionIdentifier);
                            }
                        }
                        break;
                   
                    case Common.Enum.SubscriptionType.Publisher:
                        if (!_deliverySubscriptions.ContainsKey(subscriptionIdentifier))
                        {
                            _deliverySubscriptions.Add(subscriptionIdentifier, new Subscribers(subscriptionInfo.SubscriptionId, subscriptionInfo.Type));
                            _deliverSubscriptionLoadbalancer.Add(subscriptionIdentifier);
                        }
                        break;

                }
            }
        }

        public bool RemoveSubscription(SubscriptionInfo subscriptionInfo)
        {
            //use subscription IDentifier
            if (string.Compare(subscriptionInfo.ClientId, ClientID, true) != 0)
                return false;

            SubscriptionIdentifier subscriptionIdentifier = new SubscriptionIdentifier(subscriptionInfo.SubscriptionId, subscriptionInfo.SubPolicyType);
            List<Message> removedMessages = new List<Message>();
            bool result = false;
            lock (this)
            {
                _lastActivityTime = _updateTime = DateTime.Now;
                switch (subscriptionInfo.Type)
                {
                    
                    case Common.Enum.SubscriptionType.Subscriber:
                        if (_messageSubscriptions.ContainsKey(subscriptionIdentifier))
                        {
                            result = _messageSubscriptions.Remove(subscriptionIdentifier);
                            _messageSubscriptionLoadbalancer.Remove(subscriptionIdentifier);
                        }
                        break;
                  
                    case Common.Enum.SubscriptionType.Publisher:
                        if (_deliverySubscriptions.ContainsKey(subscriptionIdentifier))
                        {
                            result = _deliverySubscriptions.Remove(subscriptionIdentifier);
                            _deliverSubscriptionLoadbalancer.Remove(subscriptionIdentifier);
                        }
                        break;

                }

                foreach (Message message in _messages.Values)
                {
                    if (message.MessageMetaData.DeliveryOption == Runtime.Caching.DeliveryOption.Any)
                    {
                        message.MessageMetaData.RemoveRecepient(subscriptionInfo.SubscriptionId);
                        message.MessageMetaData.RemoveFromReciepientList(subscriptionIdentifier);
                    }

                    message.MessageMetaData.UnregisterSubscription(this);
                    if (message.MessageMetaData.IsRemovable)
                        removedMessages.Add(message);
                }

                foreach (Message message in _delivryNotificationMessages.Values)
                {
                    message.MessageMetaData.UnregisterSubscription(this);
                    message.MessageMetaData.RemoveRecepient(subscriptionInfo.SubscriptionId);
                    message.MessageMetaData.RemoveFromReciepientList(subscriptionIdentifier);
                    if (message.MessageMetaData.IsRemovable)
                        removedMessages.Add(message);
                }
            }

            foreach (Message message in removedMessages)
            {
                if (message.MessageMetaData.SubscriptionType == Common.Enum.SubscriptionType.Publisher)
                    _delivryNotificationMessages.Remove(message.MessageId);
                else
                    _messages.Remove(message.MessageId);

                _topic.OnSubscriptonRemoved(message, subscriptionInfo);
            }
            _refreshSubscriptions.Remove(subscriptionIdentifier);
            return result;
        }

        internal void Refesh()
        {
            _lastActivityTime = DateTime.Now;
        }

        internal void ClientDisconnected(SubscriptionInfo subscriptionInfo)
        {
            lock (this)
            {
                SubscriptionIdentifier subscriptionIdentifier = new SubscriptionIdentifier(subscriptionInfo.SubscriptionId, subscriptionInfo.SubPolicyType);
                SubscriptionIdentifier[] messageSubscriptionKey = new SubscriptionIdentifier[_messageSubscriptions.Count];
                SubscriptionIdentifier[] deliverySubscriptionKeys = new SubscriptionIdentifier[_deliverySubscriptions.Count];
                _deliverySubscriptions.Keys.CopyTo(deliverySubscriptionKeys, 0);

                _messageSubscriptions.Keys.CopyTo(messageSubscriptionKey, 0);
                foreach (var item in messageSubscriptionKey)
                {
                    subscriptionInfo.SubscriptionId = item.SubscriptionName;
                    subscriptionInfo.Type = Common.Enum.SubscriptionType.Subscriber;
                    subscriptionInfo.SubPolicyType = item.SubscriptionPolicy;
                    RemoveSubscription(subscriptionInfo);
                }

                foreach (var item in deliverySubscriptionKeys)
                {
                    subscriptionInfo.SubscriptionId = item.SubscriptionName;
                    subscriptionInfo.SubPolicyType = item.SubscriptionPolicy;
                    subscriptionInfo.Type = Common.Enum.SubscriptionType.Publisher;
                    RemoveSubscription(subscriptionInfo);
                }

                if (_subscriptionListener != null) { _subscriptionListener.OnSubscriptionInstanceRemoved(messageSubscriptionKey, this.ClientID); }
            }
        }


        public bool AssignMessageToSubscription(Message message,SubscriptionInfo subscriptionInfo)
        {
            lock(this)
            {
                SubscriptionIdentifier subscriptionIdentifier = new SubscriptionIdentifier(subscriptionInfo.SubscriptionId, subscriptionInfo.SubPolicyType);

                if (_dipsoed) return false;
                if (message.MessageMetaData.SubscriptionType != subscriptionInfo.Type) return false;

                Subscribers subscription = null;
                switch (message.MessageMetaData.SubscriptionType)
                {
                    case Common.Enum.SubscriptionType.Subscriber:
                        if (subscriptionInfo.SubscriptionId != null)
                        {
                            if (!_messageSubscriptions.TryGetValue(subscriptionIdentifier, out subscription))
                                return false;

                        }

                        if (_messageSubscriptions.Count > 0 && !_messages.Contains(message.MessageId))
                            _messages.Add(message.MessageId, message);
                        break;

                    case Common.Enum.SubscriptionType.Publisher:
                        if (!_deliverySubscriptions.TryGetValue(subscriptionIdentifier, out subscription))
                            return false;

                        if (!_delivryNotificationMessages.Contains(message.MessageId))
                            _delivryNotificationMessages.Add(message.MessageId, message);
                        break;
                }
                message.MessageMetaData.RegisterSubscription(this);
                //in case of single subscriber assignment; we add subscriber id to the message
                bool any = message.MessageMetaData.DeliveryOption == Runtime.Caching.DeliveryOption.Any
                    || message.MessageMetaData.SubscriptionType == Common.Enum.SubscriptionType.Publisher;

                if (any && subscription != null)
                {
                    message.MessageMetaData.AddRecepient(subscription.ID);
                    message.MessageMetaData.AddToRecepientList(subscriptionIdentifier);
                }

            }
            return true;
        }

        internal IList<object> GetAssignedMessages()
        {
            List<object> messages = new List<object>();
            lock (this)
            {
                if (_dipsoed) return messages;

                _lastActivityTime = _pollTime = DateTime.Now;

                if (_messages.Count > 0)
                {
                    foreach(Message message in _messages.Values)
                    {
                        messages.Add(message);
                    }
                }

                if (_delivryNotificationMessages.Count > 0)
                {
                    foreach (Message message in _delivryNotificationMessages.Values)
                    {
                        messages.Add(message);
                    }
                }
            }
            foreach (var subscriptions in _refreshSubscriptions)
            {
                if (_subscriptionListener != null) { _subscriptionListener.OnSubscritionRefresh(subscriptions); }

            }
            return messages;
        }

        public IList<Message> GetNeverAcknowledgedMessages(TimeSpan timeAfterAssignment)
        {
            IList<Message> messages = new List<Message>();
            lock (this)
            {
                if (_dipsoed) return messages;

                DateTime now = DateTime.UtcNow;
                foreach (Message message in _messages.Values)
                {
                    
                        if (message.MessageMetaData.AssigmentTime == null || message.MessageMetaData.AssigmentTime.Value.Add(timeAfterAssignment) < now)
                        {
                            messages.Add(message);
                        }
                   
                   
                }

                foreach (Message message in _delivryNotificationMessages.Values)
                {
                    if (message.MessageMetaData.AssigmentTime != null)
                    {
                        if (message.MessageMetaData.AssigmentTime.Value.Add(timeAfterAssignment) < now)
                        {
                            messages.Add(message);
                        }
                    }
                }
            }

            return messages;
        }

        internal void AcknowledgeMessageRecepit(Message message)
        {
            lock (this)
            {
                if (_dipsoed) return;
                _lastActivityTime = _updateTime = DateTime.Now;

                Message storedMessage = RemoveMessage(message.MessageId);

                if(storedMessage != null)
                {
                    storedMessage.MessageMetaData.UnregisterSubscription(this);
                    storedMessage.MessageMetaData.EverAcknowledged = true;

                    if (storedMessage.MessageMetaData.SubscriptionType == Common.Enum.SubscriptionType.Subscriber)
                    {
                        foreach (SubscriptionIdentifier subscrption in _messageSubscriptions.Keys)
                        {
                            storedMessage.MessageMetaData.RemoveRecepient(subscrption.SubscriptionName);
                            storedMessage.MessageMetaData.RemoveFromReciepientList(subscrption);
                            
                        }
                    }
                    else
                    {
                        foreach (SubscriptionIdentifier subscrption in _deliverySubscriptions.Keys)
                        {
                            storedMessage.MessageMetaData.RemoveRecepient(subscrption.SubscriptionName);
                            storedMessage.MessageMetaData.RemoveFromReciepientList(subscrption);
                        }
                    }
                }
            }
        }

        public void OnMessageRemoved(string messageId)
        {
            lock(this)
            {
                RemoveMessage(messageId);
            }
        }

        private Message RemoveMessage(string messageId)
        {
            Message message = null;

            if (_dipsoed) return null;

            if (_messages.Contains(messageId))
            {
               message =  _messages[messageId] as Message;
                _messages.Remove(messageId);
            }
            else if(_delivryNotificationMessages.Contains(messageId))
            {
                message = _delivryNotificationMessages[messageId] as Message;
                _delivryNotificationMessages.Remove(messageId);
            }
            return message;
        }

        public bool HasMessageSubscriptons(Common.Enum.SubscriptionType type)
        {
            if (type == Common.Enum.SubscriptionType.Publisher)
                return _deliverySubscriptions.Count > 0;
            else
                return _messageSubscriptions.Count > 0;
        }
        public void Dispose()
        {
            lock(this)
            {
                _dipsoed = true;
            }
            foreach(Message message in _messages.Values)
            {
                message.MessageMetaData.UnregisterSubscription(this);
                if (!message.MessageMetaData.HasSubscriptions)
                    _topic.OnSubscriptonRemoved(message, null);
            }

            foreach (Message message in _delivryNotificationMessages.Values)
            {
                message.MessageMetaData.UnregisterSubscription(this);
                if (!message.MessageMetaData.HasSubscriptions)
                    _topic.OnSubscriptonRemoved(message, null);
            }
        }

        public void Clear()
        {
            lock (this)
            {
                _messages.Clear();
                _delivryNotificationMessages.Clear();
            }
        }

        internal void GetClientStats(ref int subscriberCount, ref int publisherCount)
        {
            lock(this)
            {
                subscriberCount = _messageSubscriptions.Count;
                publisherCount = _deliverySubscriptions.Count;
            }
        }

        internal bool HasPollingOnlyActivityPerformed(TimeSpan activityInterval)
        {
            TimeSpan diff = DateTime.Now - _updateTime;

            if (diff <= activityInterval)
                return false;

            diff = DateTime.Now - _pollTime;

            if (diff <= activityInterval)
                return true;

            return false;
        }

        public void OnSubscriptionCreated(Topic topic)
        {
            throw new NotImplementedException();
        }

        public void OnSubscriptionRemoved(Topic topic)
        {
            throw new NotImplementedException();
        }

        public void OnMessageArrived(Topic topic)
        {
            throw new NotImplementedException();
        }

        public void OnMessageDelivered(Topic topic)
        {
            throw new NotImplementedException();
        }


        #region /                               --- State ---                                 /

        internal class State :ICompactSerializable
        {
            private List<SubscriptionInfo> _subscriptions = new List<SubscriptionInfo>();

            public IList<SubscriptionInfo> Subscriptions { get { return _subscriptions; } }

            public void AddSubscriptions(SubscriptionInfo subscription)
            {
                _subscriptions.Add(subscription);
            }

            public void Deserialize(CompactReader reader)
            {
                _subscriptions = SerializationUtility.DeserializeList<SubscriptionInfo>(reader);
            }

            public void Serialize(CompactWriter writer)
            {
                SerializationUtility.SerializeList<SubscriptionInfo>(_subscriptions, writer);
            }
        }

       

        #endregion
    }



}