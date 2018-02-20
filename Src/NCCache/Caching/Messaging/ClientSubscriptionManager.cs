// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Messaging;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Messaging
{
    internal class ClientSubscriptionManager:ISubscription,IDisposable
    {
        private IDictionary<string, Subscription> _messageSubscriptions = new Dictionary<string, Subscription>(StringComparer.InvariantCultureIgnoreCase);
        private IDictionary<string, Subscription> _deliverySubscriptions = new Dictionary<string, Subscription>(StringComparer.InvariantCultureIgnoreCase);
        private OrderedDictionary _messages = new OrderedDictionary(StringComparer.InvariantCultureIgnoreCase);
        private OrderedDictionary _delivryNotificationMessages = new OrderedDictionary(StringComparer.InvariantCultureIgnoreCase);

        private RoundRobinLoadbalancer<string> _messageSubscriptionLoadbalancer = new RoundRobinLoadbalancer<string>(StringComparer.InvariantCultureIgnoreCase);
        private RoundRobinLoadbalancer<string> _deliverSubscriptionLoadbalancer = new RoundRobinLoadbalancer<string>(StringComparer.InvariantCultureIgnoreCase);
        private DateTime _lastActivityTime;
        private DateTime _updateTime;
        private DateTime _pollTime = DateTime.Now;
        private TimeSpan _idleTimeout = TimeSpan.FromSeconds(30);
        private Topic _topic;
        private bool _dipsoed;

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
        }

        public void AddSubscription(SubscriptionInfo subscriptionInfo)
        {
            if (String.Compare(subscriptionInfo.ClientId, this.ClientID, true) != 0)
                return;

            lock(this)
            {
                _lastActivityTime = _updateTime = DateTime.Now;
                switch(subscriptionInfo.Type)
                {
                    case SubscriptionType.Subscriber:
                        if (!_messageSubscriptions.ContainsKey(subscriptionInfo.SubscriptionId))
                        {
                            _messageSubscriptions.Add(subscriptionInfo.SubscriptionId, new Subscription(subscriptionInfo.SubscriptionId, subscriptionInfo.Type));
                            _messageSubscriptionLoadbalancer.Add(subscriptionInfo.SubscriptionId);
                        }
                        break;

                    case SubscriptionType.Publisher:
                        if (!_deliverySubscriptions.ContainsKey(subscriptionInfo.SubscriptionId))
                        {
                            _deliverySubscriptions.Add(subscriptionInfo.SubscriptionId, new Subscription(subscriptionInfo.SubscriptionId, subscriptionInfo.Type));
                            _deliverSubscriptionLoadbalancer.Add(subscriptionInfo.SubscriptionId);
                        }
                        break;

                }
            }
        }

        public bool RemoveSubscription(SubscriptionInfo subscriptionInfo)
        {
            if (string.Compare(subscriptionInfo.ClientId, ClientID, true) != 0)
                return false;

            List<Message> removedMessages = new List<Message>();
            bool result = false;
            lock (this)
            {
                _lastActivityTime = _updateTime = DateTime.Now;
                switch (subscriptionInfo.Type)
                {
                    case SubscriptionType.Subscriber:
                        if (_messageSubscriptions.ContainsKey(subscriptionInfo.SubscriptionId))
                        {
                            result = _messageSubscriptions.Remove(subscriptionInfo.SubscriptionId);
                            _messageSubscriptionLoadbalancer.Remove(subscriptionInfo.SubscriptionId);
                        }
                        break;

                    case SubscriptionType.Publisher:
                        if (_deliverySubscriptions.ContainsKey(subscriptionInfo.SubscriptionId))
                        {
                            result = _deliverySubscriptions.Remove(subscriptionInfo.SubscriptionId);
                            _deliverSubscriptionLoadbalancer.Remove(subscriptionInfo.SubscriptionId);
                        }
                        break;

                }

                foreach (Message message in _messages.Values)
                {
                    if (message.MessageMetaData.DeliveryOption == Runtime.Caching.DeliveryOption.Any)
                    {
                        message.MessageMetaData.RemoveRecepient(subscriptionInfo.SubscriptionId);
                    }

                    message.MessageMetaData.UnregisterSubscription(this);
                    if (message.MessageMetaData.IsRemovable)
                        removedMessages.Add(message);
                }

                foreach (Message message in _delivryNotificationMessages.Values)
                {
                    message.MessageMetaData.UnregisterSubscription(this);
                    message.MessageMetaData.RemoveRecepient(subscriptionInfo.SubscriptionId);
                    if (message.MessageMetaData.IsRemovable)
                        removedMessages.Add(message);
                }
            }

            foreach (Message message in removedMessages)
            {
                if (message.MessageMetaData.SubscriptionType == SubscriptionType.Publisher)
                    _delivryNotificationMessages.Remove(message.MessageId);
                else
                    _messages.Remove(message.MessageId);

                _topic.OnSubscriptonRemoved(message, subscriptionInfo);
            }

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
                string[] keys = new string[_messageSubscriptions.Count];
                _messageSubscriptions.Keys.CopyTo(keys, 0);
                foreach (var item in keys)
                {
                    subscriptionInfo.SubscriptionId = item;
                    subscriptionInfo.Type = SubscriptionType.Subscriber;
                    RemoveSubscription(subscriptionInfo);
                }
                keys = new string[_deliverySubscriptions.Count];
                _deliverySubscriptions.Keys.CopyTo(keys, 0);
                foreach (var item in keys)
                {
                    subscriptionInfo.SubscriptionId = item;
                    subscriptionInfo.Type = SubscriptionType.Publisher;
                    RemoveSubscription(subscriptionInfo);
                }
            }
        }

        public SubscriptionInfo GetNextAvailableSubscription(SubscriptionType type)
        {
            Subscription subscription = null;
            lock (this)
            {
                if (_dipsoed) return null;

                switch (type)
                {
                    case SubscriptionType.Subscriber:
                        if(_messageSubscriptionLoadbalancer.IsValid)
                            _messageSubscriptions.TryGetValue(_messageSubscriptionLoadbalancer.GetNext(), out subscription);
                        break;

                    case SubscriptionType.Publisher:
                        if(_deliverSubscriptionLoadbalancer.IsValid)
                            _deliverySubscriptions.TryGetValue(_deliverSubscriptionLoadbalancer.GetNext(), out subscription);
                        break;

                }
            }

            if (subscription != null)
                return new SubscriptionInfo() { ClientId = this.ClientID, SubscriptionId = subscription.ID, Type = type };

            return null;
        }

        public bool AssignMessageToSubscription(Message message,SubscriptionInfo subscriptionInfo)
        {
            lock(this)
            {
                if (_dipsoed) return false;
                if (message.MessageMetaData.SubscriptionType != subscriptionInfo.Type) return false;

                Subscription subscription = null;
                switch (message.MessageMetaData.SubscriptionType)
                {
                    case SubscriptionType.Subscriber:
                        if (subscriptionInfo.SubscriptionId != null)
                        {
                            if (!_messageSubscriptions.TryGetValue(subscriptionInfo.SubscriptionId, out subscription))
                                return false;

                        }

                        if (_messageSubscriptions.Count > 0 && !_messages.Contains(message.MessageId))
                            _messages.Add(message.MessageId, message);
                        break;

                    case SubscriptionType.Publisher:
                        if (!_deliverySubscriptions.TryGetValue(subscriptionInfo.SubscriptionId, out subscription))
                            return false;

                        if (!_delivryNotificationMessages.Contains(message.MessageId))
                            _delivryNotificationMessages.Add(message.MessageId, message);
                        break;
                }
                message.MessageMetaData.RegisterSubscription(this);
                //in case of single subscriber assignment; we add subscriber id to the message
                bool any = message.MessageMetaData.DeliveryOption == Runtime.Caching.DeliveryOption.Any 
                    || message.MessageMetaData.SubscriptionType == SubscriptionType.Publisher;

                if (any && subscription != null)
                    message.MessageMetaData.AddRecepient(subscription.ID);
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
            return messages;
        }

        public IList<Message> GetNeverAcknowledgedMessages(TimeSpan timeAfterAssignment)
        {
            IList<Message> messages = new List<Message>();
            lock(this)
            {
                if (_dipsoed) return messages;

                DateTime now = DateTime.UtcNow;
                foreach(Message message in _messages.Values)
                {
                    if (message.MessageMetaData.AssigmentTime.Value.Add(timeAfterAssignment) < now)
                        {
                            messages.Add(message);
                        }                    
                }

                foreach (Message message in _delivryNotificationMessages.Values)
                {
                        if (message.MessageMetaData.AssigmentTime.Value.Add(timeAfterAssignment) < now)
                        {
                            messages.Add(message);
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

                    if (storedMessage.MessageMetaData.SubscriptionType == SubscriptionType.Subscriber)
                    {
                        foreach (string subscrption in _messageSubscriptions.Keys)
                        {
                            storedMessage.MessageMetaData.RemoveRecepient(subscrption);
                        }
                    }
                    else
                    {
                        foreach (string subscrption in _deliverySubscriptions.Keys)
                        {
                            storedMessage.MessageMetaData.RemoveRecepient(subscrption);
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

        public bool HasMessageSubscriptons(SubscriptionType type)
        {
            if (type == SubscriptionType.Publisher)
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

        internal State GetState()
        {
            State state = new State();
            lock(this)
            {
                foreach(var subscription in _messageSubscriptions)
                {
                    state.AddSubscriptions(new SubscriptionInfo() { SubscriptionId = subscription.Value.ID, Type = SubscriptionType.Subscriber });
                }

                foreach (var subscription in _deliverySubscriptions)
                {
                    state.AddSubscriptions(new SubscriptionInfo() { SubscriptionId = subscription.Value.ID, Type = SubscriptionType.Publisher });
                }
            }
            return state;
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