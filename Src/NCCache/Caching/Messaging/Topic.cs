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
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Messaging;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.ErrorHandling;
using System;
using System.Linq;
using Alachisoft.NCache.Runtime.Exceptions;
using System.Threading;
using Alachisoft.NCache.Common.FeatureUsageData;

namespace Alachisoft.NCache.Caching.Messaging
{
    internal class Topic : ISizableIndex, IDisposable,ISubscriptionEventListener, ICloneable
    {
        
        private OptimizedQueue<string, Message> _messages = new OptimizedQueue<string, Message>(100, StringComparer.InvariantCultureIgnoreCase);
        private IDictionary<string, ClientSubscriptionManager> _subscribers = new HashVector<string, ClientSubscriptionManager>(StringComparer.InvariantCultureIgnoreCase);
        private MessageExpirationIndex _expirationIndex = new MessageExpirationIndex();
        private IDictionary<SubscriptionIdentifier, Subscriptions> _subscriptions = new Dictionary<SubscriptionIdentifier, Subscriptions>();
        private RoundRobinLoadbalancer<SubscriptionIdentifier> _subscriptionLoadBalancer = new RoundRobinLoadbalancer<SubscriptionIdentifier>(new SubscriptionIdentifierCompararer());
        private OptimizedQueue<string, Message> _unassignedMessages = new OptimizedQueue<string, Message>();
        private OptimizedQueue<string, Message> _deliveredMessages = new OptimizedQueue<string, Message>();
        private CacheRuntimeContext _context;
        private long _size;
        private ITopicEventListener _listener = null;
        private object _mutex = new object();
        public string Name { get; set; }

        public long IndexInMemorySize
        {
            get
            {
                return _size;
            }
        }

        public long Count
        {
            get
            {
                return _messages.Count;
            }
        }

        public bool HasSubscriptions(SubscriptionType type)
        {
            lock (_mutex)
            {
               
                foreach (var subscription in _subscriptions)
                {
                    if (subscription.Value.GetSubscriptionType == type)
                        return true;
                }


            }

            return false;
        }

        public Topic(string name, CacheRuntimeContext context)
        {
            this.Name = name;
            _context = context;
           
        }



        public bool AddSubscription(SubscriptionInfo subscriptionInfo, SubscriptionIdentifier subscriptionIdentifier,bool isUserOperation=true)
        {
            Subscriptions subscription;
            bool created = true;

            lock (_mutex)
            {
                if (_subscriptions.TryGetValue(subscriptionIdentifier, out subscription))
                {
                    if (subscription.SubscriptionPolicyType == SubscriptionPolicyType.NonDurableExclusiveSubscription && isUserOperation)
                    {
                        created = false;
                    }
                    else
                    {
                        if (subscriptionInfo.SubPolicyType != SubscriptionPolicyType.NonDurableExclusiveSubscription)
                           created= subscription.AddSubscriber(subscriptionInfo.ClientId);
                    }

                }
                else
                {
                    switch (subscriptionInfo.SubPolicyType)
                    {
                        case SubscriptionPolicyType.NonDurableExclusiveSubscription:
                            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.data_sharing).UpdateUsageTime();
                            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.pubsub, FeatureEnum.data_sharing).UpdateUsageTime();
                            FeatureUsageCollector.Instance.GetFeature(FeatureEnum.non_durable_subscription, FeatureEnum.pubsub).UpdateUsageTime();

                            if (subscriptionInfo.Type != SubscriptionType.Publisher)
                                subscription = new ExclusiveSubscriptions(subscriptionInfo.ClientId, subscriptionInfo.SubscriptionId, subscriptionInfo.SubPolicyType, subscriptionInfo.Expiration, _context);
                            else
                            {
                                subscription = new ExclusiveSubscriptions(subscriptionInfo.ClientId, subscriptionInfo.SubscriptionId, subscriptionInfo.SubPolicyType, subscriptionInfo.Expiration, _context, subscriptionInfo.Type);

                                FeatureUsageCollector.Instance.GetFeature(FeatureEnum.data_sharing).UpdateUsageTime();
                                FeatureUsageCollector.Instance.GetFeature(FeatureEnum.pubsub, FeatureEnum.data_sharing).UpdateUsageTime();
                                FeatureUsageCollector.Instance.GetFeature(FeatureEnum.delivery_failure_notificatiion, FeatureEnum.pubsub).UpdateUsageTime();
                            }

                            break;
                        case SubscriptionPolicyType.EventSubscription:
                            subscription = new EventSubscriptions(subscriptionInfo.ClientId, subscriptionInfo.SubscriptionId, subscriptionInfo.SubPolicyType, subscriptionInfo.Expiration, _context, subscriptionInfo.Type);
                            break;
                    }
                    _subscriptions.Add(subscriptionIdentifier, subscription);
                }
                return created;
            }
        }

     

        public void CreateSubscription(SubscriptionInfo subscriptionInfo, bool isUserOperation=true)
        {
            if (subscriptionInfo.SubPolicyType != SubscriptionPolicyType.EventSubscription)
            {
                FeatureUsageCollector.Instance.GetFeature(FeatureEnum.data_sharing).UpdateUsageTime();
                FeatureUsageCollector.Instance.GetFeature(FeatureEnum.pubsub, FeatureEnum.data_sharing).UpdateUsageTime();

            }

            SubscriptionIdentifier subscriptionIdentifier = new SubscriptionIdentifier(subscriptionInfo.SubscriptionId, subscriptionInfo.SubPolicyType);
            ClientSubscriptionManager clientSubscriptionManger = null;


            if (AddSubscription(subscriptionInfo, subscriptionIdentifier,isUserOperation))
            {
                lock (_mutex)
                {
                    if (subscriptionInfo.SubPolicyType != SubscriptionPolicyType.EventSubscription)
                        _subscriptionLoadBalancer.Add(subscriptionIdentifier);

                    //while state transfer if any durable subscription has no active subscriber
                    if (subscriptionInfo.ClientId != null)
                    {
                        if (!_subscribers.TryGetValue(subscriptionInfo.ClientId, out clientSubscriptionManger))
                        {
                            clientSubscriptionManger = new ClientSubscriptionManager(this, subscriptionInfo.ClientId);
                            _subscribers.Add(subscriptionInfo.ClientId, clientSubscriptionManger);
                        }

                        clientSubscriptionManger.AddSubscription(subscriptionInfo);
                    }
                }

                if (_listener != null) _listener.OnSubscriptionCreated(this);
            }
        }

        public void RemoveFromSubscriptions(SubscriptionIdentifier[] keys, string clientId)
        {

            lock (_mutex)
            {
                foreach (SubscriptionIdentifier subscriptionIdentifier in keys)
                {
                    Subscriptions subscriptions;

                    if (subscriptionIdentifier.SubscriptionPolicy == SubscriptionPolicyType.NonDurableExclusiveSubscription)
                    {
                        _subscriptions.Remove(subscriptionIdentifier);
                        _subscriptionLoadBalancer.Remove(subscriptionIdentifier);
                    }
                    else
                    {
                        if (_subscriptions.TryGetValue(subscriptionIdentifier, out subscriptions))
                        {
                            subscriptions.Remove(clientId);
                           
                        }
                    }
                }
            }
        }

        public bool RemoveSubscriber(SubscriptionInfo subscriptionInfo, bool clientDisconnected = false)
        {
            bool result = false;
            lock (_mutex)
            {
                ClientSubscriptionManager clientSubscriptionManger = null;
                if (_subscribers.TryGetValue(subscriptionInfo.ClientId, out clientSubscriptionManger))
                {
                    if (clientDisconnected || subscriptionInfo.SubscriptionId == null)
                    {
                        clientSubscriptionManger.ClientDisconnected(subscriptionInfo);
                        result = true;
                    }
                    else
                    {
                        result = clientSubscriptionManger.RemoveSubscription(subscriptionInfo);
                    }

                    if (!clientSubscriptionManger.HasActiveSubscriptions)
                    {
                        result |= _subscribers.Remove(subscriptionInfo.ClientId);
                        clientSubscriptionManger.Dispose();
                    }
                }
            }

            if (_listener != null && result) _listener.OnSubscriptionRemoved(this);
            return result;
        }


        private bool RemoveSubscription(SubscriptionIdentifier subIdentifier,SubscriptionInfo subInfo,bool isDispose=false)
        {
            Subscriptions subscription;
            lock (_mutex)
            {
                if (_subscriptions.TryGetValue(subIdentifier, out subscription))
                {
                    if (subIdentifier.SubscriptionPolicy == SubscriptionPolicyType.NonDurableExclusiveSubscription)
                    {
                        _subscriptions.Remove(subIdentifier);
                    }
                    else
                    {
                        subscription.Remove(subInfo.ClientId);

                        if (!subscription.IsActive()&& isDispose==false)
                        {
                            _subscriptions.Remove(subIdentifier);
                            //_subscriptionExpirationIndex.Remove(subIdentifier);
                        }
                    }
                }
            }

           return RemoveSubscriber(subInfo);
        }


        public bool UnSubscribeSubscription(SubscriptionInfo subscriptionInfo, bool clientDisconnected = false,bool isDispose=false)
        {
            SubscriptionIdentifier subscriptionIdentifier = new SubscriptionIdentifier(subscriptionInfo.SubscriptionId, subscriptionInfo.SubPolicyType);

            return RemoveSubscription(subscriptionIdentifier,subscriptionInfo,isDispose);
        }

        public void RefreshSubscription(SubscriptionInfo subscriptionInfo)
        {
            lock (_mutex)
            {
                ClientSubscriptionManager clientSubscriptionManger = null;
                if (_subscribers.TryGetValue(subscriptionInfo.ClientId, out clientSubscriptionManger))
                {
                    clientSubscriptionManger.Refesh();
                }
            }
        }



        public SubscriptionInfo GetSubscriberForAssignment(SubscriptionType type, OperationContext context)
        {
            lock (_mutex)
            {
                if (_subscriptionLoadBalancer.IsValid)
                {
                    SubscriptionIdentifier firstSelectedIdentifier = null;
                    SubscriptionIdentifier selectedIdentifier = null;
                    Subscriptions subscription;
                    SubscriptionInfo subscriptionInfo=new SubscriptionInfo();

                    while (true)
                    {
                        selectedIdentifier = _subscriptionLoadBalancer.GetNext();

                        if (firstSelectedIdentifier != null && selectedIdentifier.Equals(firstSelectedIdentifier))
                            return null;

                        if (firstSelectedIdentifier == null) firstSelectedIdentifier = selectedIdentifier;

                        if (_subscriptions.TryGetValue(selectedIdentifier, out subscription))
                        {
                            if (subscription.GetSubscriptionType == type)
                            {
                                subscriptionInfo.ClientId =  GetActiveSubscriber(subscription);
                                subscriptionInfo.SubscriptionId = selectedIdentifier.SubscriptionName;
                                subscriptionInfo.SubPolicyType = subscription.SubscriptionPolicyType;
                                subscriptionInfo.Type = type;
                                return subscriptionInfo;
                            }
                        }
                    }
                }
                return null;
            }
        }

    
        private string GetActiveSubscriber(Subscriptions subscription)
        {
            string activeSbuscriber = null;

            string subscriber = null;
            string firstSubscriber = null;

            ClientSubscriptionManager client = null;

            while (activeSbuscriber == null)
            {
                subscriber = subscription.GetSubscriber();

                if (firstSubscriber == null)
                {
                    if (subscriber == null) break;
                    firstSubscriber = subscriber;
                }

                if (subscriber != null && _subscribers.TryGetValue(subscriber, out client))
                {
                    if (client.IsActive)
                    {
                        activeSbuscriber = subscriber;
                        break;
                    }
                }

                if (subscriber == firstSubscriber) break;
            }

            if (activeSbuscriber == null && firstSubscriber != null) activeSbuscriber = firstSubscriber;

            return activeSbuscriber;
        }

        public void RegisterEventListener(ITopicEventListener listener)
        {
            _listener = listener;
        }

        public IList<SubscriptionInfo> GetAllSubscriber(OperationContext context)
        {
            return null;
        }

        internal bool IsDefaultTopicName(string topicName)
        {
            bool isDefaultTopicName = false;

            if (!string.IsNullOrEmpty(topicName))
            {
                isDefaultTopicName = isDefaultTopicName || topicName.Equals(TopicConstant.ItemLevelEventsTopic);
                isDefaultTopicName = isDefaultTopicName || topicName.Equals(TopicConstant.CollectionEventsTopic);
            }
            return isDefaultTopicName;
        }

        public bool StoreMessage(Message message, OperationContext context)
        {
            
                bool messageStored = false;
                lock (_mutex)
                {
                    if (IsDefaultTopicName(message.MessageMetaData.TopicName) && _subscribers.Count == 0)
                    {
                        return false;
                    }
                    else
                    {
                        if (!_messages.ContainsKey(message.MessageId))
                        {
                            _messages.Enqueue(message.MessageId, message);
                            AddToUnassignedMessageQueue(message);

                            if (message.MessageMetaData.ExpirationTime != TimeSpan.MaxValue.Ticks)
                            {
                                if (message.MessageMetaData.AbsoluteExpiratoinTime == null)
                                    message.MessageMetaData.InitializeExpiration();

                                _expirationIndex.Add(message.MessageId, message.MessageMetaData.AbsoluteExpiratoinTime.Value);
                            }
                            _size += message.Size;
                            if (_listener != null)
                            {
                                _listener.OnSizeIncrement(message.Size);
                                _listener.OnCountIncrement(1);
                            }
                            messageStored = true;
                        }
                        else
                        {
                            if (!OperationContext.IsReplicationOperation && !IsDefaultTopicName(message.MessageMetaData.TopicName))
                                throw new OperationFailedException(ErrorCodes.PubSub.MESSAGE_ID_ALREADY_EXISTS, ErrorMessages.GetErrorMessage(ErrorCodes.PubSub.MESSAGE_ID_ALREADY_EXISTS));
                        }
                    }

                    if (_listener != null) _listener.OnMessageArrived(this);
                    return messageStored;
                }

                
        }

        private void AddToUnassignedMessageQueue(Message message)
        {
            lock (_mutex)
            {
                _unassignedMessages.Enqueue(message.MessageId, message);
            }
        }

        public void RemoveMessage(IList<string> messageIds, MessageRemovedReason reason)
        {
            foreach (string messageid in messageIds)
                RemoveMessage(messageid, reason);
        }

        /// <summary>
        /// Removes message
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="reason"></param>
        /// <returns>Retruns message object</returns>
        public Message RemoveMessage(string messageId, MessageRemovedReason reason)
        {
            Message message = null;
            bool removed = true;
            lock (_mutex)
            {

                if (_messages.TryGetValue(messageId, out message))
                {
                        _messages.Remove(messageId);
                      
                        _size -= message.Size;
                        if (_listener != null)
                        {
                            _listener.OnSizeDecrement(message.Size);
                            _listener.OnCountDecrement(1);
                        }
                   
                    message.MessageMetaData.OnMessageRemoved();
                    _unassignedMessages.Remove(messageId);
                    _deliveredMessages.Remove(messageId);

                    if (message.MessageMetaData.AbsoluteExpiratoinTime.HasValue)
                        _expirationIndex.Remove(messageId);
                }
            }
            if (_context.IsStartedAsMirror && message == null) return null;

            if (removed && reason == MessageRemovedReason.Delivered &&  message?.MessageMetaData.SubscriptionType == SubscriptionType.Subscriber)
            {
                if (_context.PerfStatsColl != null && !_context.IsStartedAsMirror)
                    _context.PerfStatsColl.IncrementMessageDeliverPerSec();
            }
            //only return message if it is removed from _messages
            return removed ? message : null;
        }



        public MessageInfo GetNextUnassignedMessage(TimeSpan timeout, OperationContext context)
        {
            Message message = null;
          
                lock (_mutex)
                {
                    if (_unassignedMessages.Count > 0)
                    {
                        _unassignedMessages.Peek(out message);
                    }
                }

            return message != null ? GetMessageInfo(message) : null; ;
        }

       

        public IList<MessageInfo> GetDeliveredMessage(TimeSpan timeout, OperationContext context)
        {
            IList<MessageInfo> delieveredMessages = null;
            lock (_mutex)
            {
                if (_deliveredMessages.Count > 0)
                {
                    delieveredMessages = new List<MessageInfo>((int)_deliveredMessages.Count);
                    IEnumerator<Message> e = _deliveredMessages.GetEnumerator();
                    while (e.MoveNext())
                    {
                        if (e.Current != null )
                        {
                            delieveredMessages.Add(GetMessageInfo(e.Current));
                        }
                    }
                }
            }
            return delieveredMessages;
        }

        private MessageInfo GetMessageInfo(Message message)
        {
            return new MessageInfo()
            {
                MessageId = message.MessageId,
                NotifyOnDeliveryFailure = message.MessageMetaData.IsNotify,
                DeliveryOption = message.MessageMetaData.DeliveryOption,
                Topic = this.Name,
                IsEventMessage = message is EventMessage
            };
        }

        public IList<object> GetAssignedMessages(SubscriptionInfo subscriptionInfo)
        {
            IList<object> messages = null;
            ClientSubscriptionManager client = null;

            lock (_mutex)
            {
                if (_subscribers.TryGetValue(subscriptionInfo.ClientId, out client))
                {
                    messages = client.GetAssignedMessages();
                }
            }

            return messages;
        }
       

        void RemoveSubscribers(SubscriptionIdentifier subscriptionIdentifier)
        {
            Subscriptions subscriptions;
            SubscriptionInfo subscriptionInfo = new SubscriptionInfo();
            if (_subscriptions.TryGetValue(subscriptionIdentifier, out subscriptions))
            {
                List<string> subscriberList = subscriptions.GetSubscriberList();
                if (subscriberList.Count > 0 && subscriberList != null)
                {
                    subscriptionInfo.SubscriptionId = subscriptionIdentifier.SubscriptionName;
                    subscriptionInfo.SubPolicyType = subscriptionIdentifier.SubscriptionPolicy;
                    subscriptionInfo.Type = SubscriptionType.Subscriber;
                    foreach (var clientId in subscriberList)
                    {
                        subscriptionInfo.ClientId = clientId;
                        UnSubscribeSubscription(subscriptionInfo);
                        
                    }
                }
            }
        }

        internal void RemoveExpiredSubscription(SubscriptionIdentifier subscriptionIdentifier)
        {
          
        }
       

        public IList<MessageInfo> GetExpiredMessages()
        {
            IList<MessageInfo> expiredMessages = new List<MessageInfo>();
            ICollection expiredList = null;
            lock (_mutex)
            {
                expiredList = _expirationIndex.GetExpiredKeys();
            }

            if (expiredList != null && expiredList.Count > 0)
            {
                Message message = null;
                foreach (string messageId in expiredList)
                {
                    if (_messages.TryGetValue(messageId, out message))
                    {
                        expiredMessages.Add(GetMessageInfo(message));
                    }
                }
            }

            return expiredMessages;
        }

        public IList<MessageInfo> GetEvicatableMessages(long sizeToEvict, out long evictedSize)
        {
            evictedSize = 0;
            IList<MessageInfo> evictableMessages = new List<MessageInfo>();
            lock (_mutex)
            {
                //our selection is pending message first then undelivered (notification messages)
                IEnumerator<Message> e = _messages.GetEnumerator();
                long totalSize = 0;
                while (e.MoveNext())
                {
                    if (e.Current != null)
                    {
                        totalSize += e.Current.Size;
                        evictableMessages.Add(GetMessageInfo(e.Current));
                        evictedSize = totalSize;
                        if (totalSize >= sizeToEvict)
                            break;
                    }
                }
            }
            return evictableMessages;
        }

        public IList<MessageInfo> GetNeverAcknowledgedMessages(TimeSpan timeAfterAssignment)
        {
            IList<MessageInfo> unacknowledgedMessages = new List<MessageInfo>();
            HashVector durableSubscriptions = new HashVector(StringComparer.InvariantCultureIgnoreCase);

            lock (_mutex)
            {
                foreach (ClientSubscriptionManager client in _subscribers.Values)
                {
                    IList<Message> unacknowledgedMessageList = null;

                    unacknowledgedMessageList = client.GetNeverAcknowledgedMessages(timeAfterAssignment);

                    if (unacknowledgedMessageList != null)
                    {
                        foreach (Message message in unacknowledgedMessageList)
                        {
                            if (!durableSubscriptions.ContainsKey(message.MessageId))
                            {
                                unacknowledgedMessages.Add(GetMessageInfo(message));
                                durableSubscriptions.Add(message.MessageId, null);
                            }

                        }
                    }
                }
            }

            return unacknowledgedMessages;
        }

        public bool AssignSubscription(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo)
        {
            lock (_mutex)
            {
                Message message = null;

                if (!_messages.TryGetValue(messageInfo.MessageId, out message))
                    return false;

                bool assigned = false;
                //subscription id is null when assigned to a single subscriber
                if (subscriptionInfo.SubscriptionId != null)
                    assigned = AssignMessageToSubscriber(subscriptionInfo, message);
                else
                    assigned = AssignMessageToAllSubscriptions(subscriptionInfo, message);

                if (assigned)
                    RemoveFromPendingQueues(message);

                return assigned;
            }

        }

        internal ClusteredArrayList GetMessageList()
        {
            ClusteredArrayList messages = new ClusteredArrayList();

            lock (_mutex)
            {
                IEnumerator<Message> e = _messages.GetEnumerator();
                while (e.MoveNext())
                {
                    messages.Add(e.Current.MessageId);
                }
            }
            return messages;
        }

        private bool AssignMessageToSubscriber(SubscriptionInfo subscriptionInfo, Message message)
        {
            HashSet<SubscriptionIdentifier> subscriptionIdentifierList;
            ClientSubscriptionManager client;
            SubscriptionIdentifier subscriptionIdentity = new SubscriptionIdentifier(subscriptionInfo.SubscriptionId, subscriptionInfo.SubPolicyType);
            Subscriptions subscriptions;
            
            _subscriptions.TryGetValue(subscriptionIdentity, out subscriptions);

            if (subscriptions == null)
                return false;

            if (/*client.IsActive &&*/ subscriptions.IsActive())
            {
                lock (_mutex)
                {

                    if (_subscribers.TryGetValue(subscriptionInfo.ClientId, out client))
                    {
                        if (client.AssignMessageToSubscription(message, subscriptionInfo))
                        {

                            SubscriptionIdentifier subIdentifier = new SubscriptionIdentifier(subscriptionInfo.SubscriptionId, subscriptionInfo.SubPolicyType);

                            HashSet<SubscriptionIdentifier> subscriptionId;
                            

                        }
                        return true;
                    }
                }
            }

            return false;
        }

        internal TopicStats GetTopicsStats()
        {
            TopicStats topicStats = new TopicStats();
            topicStats.TopicName = Name;

            lock (_mutex)
            {
                topicStats.CurrentMessageCount = _messages.Count;
            }

            int subscriberCount = 0;
            int publisherCount = 0;
            int durableSharedCount = 0;
            int durableExclCount = 0;
            int nonDurableCount = 0;
            lock (_mutex)
            {
                foreach (ClientSubscriptionManager client in _subscribers.Values)
                {
                    client.GetClientStats(ref subscriberCount, ref publisherCount);
                    topicStats.SubscriberCount += subscriberCount;
                    topicStats.PublisherCount += publisherCount;
                }
                foreach (KeyValuePair<SubscriptionIdentifier,Subscriptions> subscription in _subscriptions)
                {
                   
                    if(subscription.Key.SubscriptionPolicy == SubscriptionPolicyType.NonDurableExclusiveSubscription)
                    {
                        nonDurableCount++;
                    }
                }
                topicStats.DurableSharedCount = durableSharedCount;
                topicStats.DurableExclusiveCount = durableExclCount;
                topicStats.NonDurableSubCount = nonDurableCount; 

            }
            return topicStats;
        }



        private bool AssignMessageToAllSubscriptions(SubscriptionInfo subscriptionInfo, Message message)
        {
            bool assigned = false;
            Subscriptions subscriptions;
            ClientSubscriptionManager clientManager;
            lock (_mutex)
            {
                if (subscriptionInfo.SubPolicyType != SubscriptionPolicyType.EventSubscription)
                {
                    foreach (var subscription in _subscriptions)
                    {
                        HashSet<SubscriptionIdentifier> subscriptionIdentifierList;

                        if (subscription.Value.IsActive() && subscription.Value.SubscriptionPolicyType == SubscriptionPolicyType.NonDurableExclusiveSubscription)
                        {
                            string clientId = subscription.Value.GetSubscriber();
                            if (!String.IsNullOrEmpty(clientId))
                            {
                                if (_subscribers.TryGetValue(clientId, out clientManager))
                                {
                                    if (clientManager.HasMessageSubscriptons(SubscriptionType.Subscriber))
                                        assigned |= clientManager.AssignMessageToSubscription(message, subscriptionInfo);
                                }
                            }


                        }
                    }
                }
                else
                {
                    if (message.IsMulticast)
                    {
                        MultiCastMessage multicastMessage = (MultiCastMessage)message;
                        if (multicastMessage.SpecificReciepients.Count > 0 || multicastMessage.SpecificReciepients != null)
                        {
                            List<SubscriptionInfo> subinfolist = multicastMessage.SpecificReciepients;
                            foreach (var subInfo in subinfolist)
                            {
                                if (_subscribers.TryGetValue(subInfo.ClientId, out clientManager))
                                {
                                    assigned = clientManager.AssignMessageToSubscription(message, subscriptionInfo);
                                }
                            }
                        }
                    }
                    else
                    {
                        SubscriptionIdentifier subscriptionIdentifier = new SubscriptionIdentifier();
                        subscriptionIdentifier.SubscriptionName = SubscriptionInfo.EventsSubscriptionName;
                        subscriptionIdentifier.SubscriptionPolicy = SubscriptionPolicyType.EventSubscription;
                        if (_subscriptions.TryGetValue(subscriptionIdentifier, out subscriptions))
                        {
                            List<string> subscribedClients = subscriptions.GetSubscriberList();
                            if (subscriptions.IsActive() && (subscribedClients.Count > 0))
                            {
                                foreach (var clientId in subscribedClients)
                                {
                                    if (_subscribers.TryGetValue(clientId, out clientManager))
                                    {
                                        if (clientManager.HasMessageSubscriptons(SubscriptionType.Subscriber))
                                            assigned |= clientManager.AssignMessageToSubscription(message, subscriptionInfo);
                                    }
                                }
                            }
                        }
                    }

                }
            }
            return assigned;
        }




        private void RemoveFromPendingQueues(Message message)
        {
            if (message.MessageMetaData.SubscriptionType == SubscriptionType.Subscriber)
            {
                _unassignedMessages.Remove(message.MessageId);
            }
           
        }

        public void RevokeAssignment(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo)
        {
            bool messageRequed = false;

            ClientSubscriptionManager client = null;

            lock (_mutex)
            {
                Message message = null;

                if (!_messages.TryGetValue(messageInfo.MessageId, out message))
                    return;

                    if (subscriptionInfo == null) //revoke all subscriptions
                    {
                        message.MessageMetaData.RevokeSubscriptions();
                    }
                    else
                    {
                        if (_subscribers.TryGetValue(subscriptionInfo.ClientId, out client))
                        {
                            message.MessageMetaData.RevokeSubscriptions(client);
                        }
                    }

                    SubscriptionIdentifier subscriptonId = null;
                    if (subscriptionInfo != null)
                    {
                        subscriptonId = new SubscriptionIdentifier();
                        subscriptonId.SubscriptionName = subscriptionInfo.SubscriptionId;
                        subscriptonId.SubscriptionPolicy = subscriptionInfo.SubPolicyType;
                    }

                    messageRequed = RequeMessage(message, subscriptonId);
            }

            if (messageRequed && _listener != null) _listener.OnMessageArrived(this);
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        private bool RequeMessage(Message message,SubscriptionIdentifier subscriptionIdentifier)
        {
            HashSet<SubscriptionIdentifier> assignedSubscriptions;
           

            if (!message.MessageMetaData.EverAcknowledged && message.MessageMetaData.IsAssigned && !message.MessageMetaData.HasSubscriptions)
            {
                message.MessageMetaData.IsAssigned = false;
                message.MessageMetaData.AssigmentTime = null;

                //agains; enque for assignment
                switch (message.MessageMetaData.SubscriptionType)
                {
                    case SubscriptionType.Subscriber:
                        _unassignedMessages.Enqueue(message.MessageId, message);
                        break;
                }
                return true;
            }
            else if (message.MessageMetaData.Delivered)
            {
                _deliveredMessages.Enqueue(message.MessageId, message);
                if (_listener != null) _listener.OnMessageDelivered(this);
            }

            return false;
        }


        public void AcknowledgeMessageReceipt(string clientId, string messageId)
        {
            try
            {
                bool delivered = false;
                ClientSubscriptionManager client;
                Message message = null;

                lock (_mutex)
                {
                    if (!_subscribers.TryGetValue(clientId, out client))
                        return;

                    if(_messages.TryGetValue(messageId, out message))
                        client.AcknowledgeMessageRecepit(message);

                    if (message==null ) return;
                    if (message.MessageMetaData.Delivered)
                    {
                        _deliveredMessages.Enqueue(message.MessageId, message);
                        delivered = true;
                    }
                }

                if (delivered && _listener != null) _listener.OnMessageDelivered(this);
            }
            catch (Exception ex)
            {

            }

        }


        public void OnSubscriptonRemoved(Message message, SubscriptionInfo subscriptionInfo)
        {
            bool messageRequed = false;
            SubscriptionIdentifier subscriptionIdentifier = new SubscriptionIdentifier(subscriptionInfo.SubscriptionId, subscriptionInfo.SubPolicyType);
            HashSet<SubscriptionIdentifier> subscriptionIdentifierList = new HashSet<SubscriptionIdentifier>();
            lock (_mutex)
            {
                
                message.MessageMetaData.RemoveFromReciepientList(subscriptionIdentifier);

                if (!_messages.TryGetValue(message.MessageId, out message))
                    return;

                messageRequed = RequeMessage(message,subscriptionIdentifier);
            }

            if (messageRequed && _listener != null) _listener.OnMessageArrived(this);
        }

        public IList<string> GetNotifiableClients()
        {
            IList<string> clients = null;

            lock (_mutex)
            {
                foreach (ClientSubscriptionManager client in _subscribers.Values)
                {
                    if (client.HasNewMessages)
                    {
                        if (clients == null) clients = new List<string>();
                        clients.Add(client.ClientID);
                    }
                }
            }

            return clients;
        }

     


        public TransferrableMessage GetTransferrableMessage(string messageId)
        {
            TransferrableMessage transferrableMessage = null;

            lock (_mutex)
            {
                Message message = null;
                if (_messages.TryGetValue(messageId, out message))
                {
                    message = message.Clone() as Message;
                    transferrableMessage = new TransferrableMessage(message);

                    foreach (ClientSubscriptionManager client in message.MessageMetaData.Subscriptions)
                    {
                        transferrableMessage.AddSubscribedClientId(client.ClientID);
                    }
                }
            }

            return transferrableMessage;
        }

        public void StoreTransferrableMessage(TransferrableMessage transferrableMessage)
        {
            if (transferrableMessage != null)
            {
                Message message = transferrableMessage.Message;
                lock (_mutex)
                {
                    if (_messages.ContainsKey(message.MessageId)) return; //message already transferred

                    StoreMessage(transferrableMessage.Message, null);

                    //restore subscriptions
                    foreach (var clientSubscription in transferrableMessage.SubscribedClients)
                    {
                        SubscriptionInfo subscriptionInfo = new SubscriptionInfo();
                        subscriptionInfo.ClientId = clientSubscription;
                        subscriptionInfo.Type = message.MessageMetaData.SubscriptionType;

                        //we assume that if receptionlist is not empty then message is assigned to single client
                        if (message.MessageMetaData.RecepientList != null && message.MessageMetaData.RecepientList.Count > 0)
                        {
                            subscriptionInfo.SubscriptionId = message.MessageMetaData.RecepientList.First<string>();
                        }

                        if (AssignMessageToSubscriber(subscriptionInfo, message))
                            RemoveFromPendingQueues(message);
                    }
                }
            }
        }

        public void Clear()
        {
            lock (_mutex)
            {
                _size = 0;
                _messages.Clear();
                _expirationIndex.Clear();
                _unassignedMessages.Clear();
                _deliveredMessages.Clear();

                foreach (ClientSubscriptionManager client in _subscribers.Values)
                {
                    client.Clear();
                }
            }
        }

        public IList<string> GetInActiveClientSubscriptions(TimeSpan inactivityPeriodThreshold)
        {
            IList<string> inactiveClients = new List<string>();
            
            lock (_mutex)
            {
                foreach (ClientSubscriptionManager client in _subscribers.Values)
                {
                    if (client.InactivityInterval > inactivityPeriodThreshold)
                    {
                         inactiveClients.Add(client.ClientID);
                      
                    }
                }
            }

            return inactiveClients;
        }

        public IList<string> GetActiveClientSubscriptions(TimeSpan interval)
        {
            IList<string> activeClients = new List<string>();

            lock (_mutex)
            {
                foreach (ClientSubscriptionManager client in _subscribers.Values)
                {
                    if (client.HasPollingOnlyActivityPerformed(interval))
                    {
                        activeClients.Add(client.ClientID);
                    }
                }
            }

            return activeClients;
        }

        #region /                       ---- State transfer related stuff ---                           /

        public State GetTopicState()
        {
           
            State state = new State(this.Name);

            lock (_mutex)
            {
                
                foreach (var subscription in _subscriptions)
                {
                    state.AddSubscriptions(subscription);
                }
            }
            return state;
        }

        public void SetTopicState(State state)
        {
           
            if (state != null)
            {
                foreach (var subscriptions in state.Subscriptions)
                {
                    if (subscriptions.Key.SubscriptionPolicy == SubscriptionPolicyType.NonDurableExclusiveSubscription)
                        CreateSubscription(new SubscriptionInfo() { ClientId = subscriptions.Value.GetSubscriber(), SubscriptionId = subscriptions.Key.SubscriptionName, Type = subscriptions.Value.GetSubscriptionType, SubPolicyType = subscriptions.Key.SubscriptionPolicy }, false);
                    else
                    {
                        List<string> clients = subscriptions.Value.GetSubscriberList();
                        if (clients != null && clients.Count > 0)
                        {
                            foreach (var clientId in clients)
                            {
                                CreateSubscription(new SubscriptionInfo() { ClientId = subscriptions.Value.GetSubscriber(), SubscriptionId = subscriptions.Key.SubscriptionName, Type = subscriptions.Value.GetSubscriptionType, SubPolicyType = subscriptions.Key.SubscriptionPolicy, Expiration = subscriptions.Value.ExpirationTime });
                            }
                        }
                        else
                        {
                            CreateSubscription(new SubscriptionInfo() { ClientId = subscriptions.Value.GetSubscriber(), SubscriptionId = subscriptions.Key.SubscriptionName, Type = subscriptions.Value.GetSubscriptionType, SubPolicyType = subscriptions.Key.SubscriptionPolicy, Expiration = subscriptions.Value.ExpirationTime });

                        }
                    }
                   
                }
            }
        }

        public void Dispose()
        {
            lock (_mutex)
            {
                if (_listener != null)
                {
                    _listener.OnSizeDecrement(IndexInMemorySize);
                    _listener.OnCountDecrement(Count);
                    _listener = null;
                }
            }
        }

        public void OnSubscriptionInstanceRemoved(SubscriptionIdentifier[] keys,string clientId)
        {
            RemoveFromSubscriptions(keys, clientId);
        }

        public void OnSubscritionRefresh(SubscriptionIdentifier subscriptionId)
        {
            Subscriptions subscriptions;
            if (_subscriptions.TryGetValue(subscriptionId, out subscriptions))
                subscriptions.ResetExpiration();
        }



        #region /                               --- Topic State ---                                 /

        internal class State : ICompactSerializable
        {
            private string _topic;
            private IDictionary<SubscriptionIdentifier, Subscriptions> _subscriptions = new Dictionary<SubscriptionIdentifier, Subscriptions>();

            private IDictionary<string,HashSet< SubscriptionIdentifier>> _messagesAssigned = new Dictionary<string,HashSet<SubscriptionIdentifier>>();

            public IDictionary<SubscriptionIdentifier, Subscriptions> Subscriptions { get{ return _subscriptions; } }

            public IDictionary<string, HashSet<SubscriptionIdentifier>> MessageAssigned
            {
                get { return _messagesAssigned; }
            }
            public string Topic { get { return _topic; } }

            public State(string topic) { _topic = topic; }

            public void AddSubscriptions(KeyValuePair<SubscriptionIdentifier,Subscriptions> subscription)
            {
                if (!_subscriptions.ContainsKey(subscription.Key))
                {
                    _subscriptions.Add(subscription);
                }
            }

            internal void AddMessagesAssigned(string messageId,HashSet<SubscriptionIdentifier> assignedSubscriptions)
            {
                if (!_messagesAssigned.ContainsKey(messageId))
                {
                    _messagesAssigned.Add(messageId,assignedSubscriptions);
                }
            }

            public void Deserialize(CompactReader reader)
            {
                _topic = reader.ReadObject() as string;
                _subscriptions = SerializationUtility.DeserializeDictionary<SubscriptionIdentifier, Subscriptions>(reader);
                _messagesAssigned = SerializationUtility.DeserializeDictionaryHashSet<string, SubscriptionIdentifier>(reader);
              
                
            }

            public void Serialize(CompactWriter writer)
            {
                writer.WriteObject(_topic);
                SerializationUtility.SerializeDictionary<SubscriptionIdentifier, Subscriptions>(_subscriptions, writer);
                SerializationUtility.SerializeDictionaryHashSet(_messagesAssigned, writer);
            }


        }

        #endregion
        #endregion
    }
}


