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

using System.Collections;
using Alachisoft.NCache.Common;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Util;
using System.Threading;
using System;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered;
#endif
using Alachisoft.NCache.Common.Net;
using System.Linq;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Caching.Messaging
{
    internal class Topic : ISizableIndex, IDisposable
    {
        private OptimizedQueue<string, Message> _messages = new OptimizedQueue<string, Message>(100, StringComparer.InvariantCultureIgnoreCase);
        private IDictionary<string, ClientSubscriptionManager> _subscriptions = new HashVector<string, ClientSubscriptionManager>(StringComparer.InvariantCultureIgnoreCase);
        private MessageExpirationIndex _expirationIndex = new MessageExpirationIndex();

        private RoundRobinLoadbalancer<string> _subscriptionLoadBalancer = new RoundRobinLoadbalancer<string>(StringComparer.InvariantCultureIgnoreCase);
        private OptimizedQueue<string, Message> _unassignedMessages = new OptimizedQueue<string, Message>();
        private OptimizedQueue<string, Message> _undeliveredMessages = new OptimizedQueue<string, Message>();
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
                foreach (ClientSubscriptionManager subscription in _subscriptions.Values)
                {
                    if (subscription.HasMessageSubscriptons(type) && subscription.IsActive) return true;
                }
            }

            return false;
        }

        public Topic(string name, CacheRuntimeContext context)
        {
            this.Name = name;
            _context = context;
        }

        public void CreateSubscription(SubscriptionInfo subscriptionInfo)
        {
            lock (_mutex)
            {
                ClientSubscriptionManager clientSubscriptionManger = null;
                if (!_subscriptions.TryGetValue(subscriptionInfo.ClientId, out clientSubscriptionManger))
                {
                    clientSubscriptionManger = new ClientSubscriptionManager(this, subscriptionInfo.ClientId);
                    _subscriptions.Add(subscriptionInfo.ClientId, clientSubscriptionManger);
                    _subscriptionLoadBalancer.Add(subscriptionInfo.ClientId);
                }

                clientSubscriptionManger.AddSubscription(subscriptionInfo);
            }

            if (_listener != null) _listener.OnSubscriptionCreated(this);
        }

        public bool RemoveSubscription(SubscriptionInfo subscriptionInfo, bool clientDisconnected = false)
        {
            bool result = false;
            lock (_mutex)
            {
                ClientSubscriptionManager clientSubscriptionManger = null;
                if (_subscriptions.TryGetValue(subscriptionInfo.ClientId, out clientSubscriptionManger))
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
                        result |= _subscriptions.Remove(subscriptionInfo.ClientId);
                        _subscriptionLoadBalancer.Remove(subscriptionInfo.ClientId);
                        clientSubscriptionManger.Dispose();
                    }
                }
            }

            if (_listener != null) _listener.OnSubscriptionRemoved(this);

            return result;
        }

        public void RefrshSubscription(SubscriptionInfo subscriptionInfo)
        {
            lock (_mutex)
            {
                ClientSubscriptionManager clientSubscriptionManger = null;
                if (_subscriptions.TryGetValue(subscriptionInfo.ClientId, out clientSubscriptionManger))
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
                    string firstselectedClientId = null;
                    string selectedClient = null;
                    while (true)
                    {
                        selectedClient = _subscriptionLoadBalancer.GetNext();

                        //breaks the loop
                        if (firstselectedClientId != null && string.Compare(selectedClient, firstselectedClientId, true) == 0)
                            return null;

                        if (firstselectedClientId == null) firstselectedClientId = selectedClient;
                        ClientSubscriptionManager clientManager = _subscriptions[selectedClient];

                        if (clientManager.IsActive)
                            return clientManager.GetNextAvailableSubscription(type);

                    }
                }
            }
            return null;
        }

        public void RegisterEventListener(ITopicEventListener listener)
        {
            _listener = listener;
        }

        public IList<SubscriptionInfo> GetAllSubscriber(OperationContext context)
        {
            return null;
        }

        public bool StoreMessage(Message message, OperationContext context)
        {
            bool messageStored = false;
            lock (_mutex)
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
                    if (!OperationContext.IsReplicationOperation)
                        throw new OperationFailedException("The specified message id already exists.");
                }
            }

            if (_listener != null) _listener.OnMessageArrived(this);
            return messageStored;
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
                    if (reason == MessageRemovedReason.Expired && message.MessageMetaData.IsNotify
                        && !message.MessageMetaData.EverAcknowledged)
                    {
                        message.MessageMetaData.SubscriptionType = SubscriptionType.Publisher;
                        message.MessageMetaData.DeliveryFailed = true;
                        message.MessageMetaData.IsAssigned = true;
                        message.MessageMetaData.AssigmentTime = null;
                        message.MessageMetaData.MessgeFailureReason = MessgeFailureReason.Expired;
                        //moved to unldeivered queue
                        _undeliveredMessages.Enqueue(message.MessageId, message);
                        removed = false;
                    }
                    else
                    {
                        _messages.Remove(messageId);
                        _undeliveredMessages.Remove(messageId);
                        _size -= message.Size;
                        if (_listener != null)
                        {
                            _listener.OnSizeDecrement(message.Size);
                            _listener.OnCountDecrement(1);
                        }
                    }


                    message.MessageMetaData.OnMessageRemoved();

                    _unassignedMessages.Remove(messageId);
                    _deliveredMessages.Remove(messageId);

                    if (message.MessageMetaData.AbsoluteExpiratoinTime.HasValue)
                        _expirationIndex.Remove(messageId);
                }
            }

            if (removed && reason == MessageRemovedReason.Delivered && message.MessageMetaData.SubscriptionType == SubscriptionType.Subscriber)
            {
                if (_context.PerfStatsColl != null )
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

        public MessageInfo GetNextUndeliveredMessage(OperationContext context)
        {
            Message message = null;
            lock (this)
            {
                if (_undeliveredMessages.Count > 0)
                {
                    _undeliveredMessages.Peek(out message);
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
                        if (e.Current != null)
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
                Topic = this.Name
            };
        }

        public IList<object> GetAssignedMessages(SubscriptionInfo subscriptionInfo)
        {
            IList<object> messages = null;
            ClientSubscriptionManager client = null;

            lock (_mutex)
            {
                if (_subscriptions.TryGetValue(subscriptionInfo.ClientId, out client))
                {
                    messages = client.GetAssignedMessages();
                }
            }

            return messages;
        }

        public IList<MessageInfo> GetExpiredMessages()
        {
            IList<MessageInfo> expiredMessages = new List<MessageInfo>();
            ICollection expiredList = null;
            lock (_mutex)
            {
                expiredList = _expirationIndex.GetExpiredKeys();
            }

            if (expiredList != null)
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
            HashVector _duplicationCheck = new HashVector(StringComparer.InvariantCultureIgnoreCase);

            lock (_mutex)
            {
                foreach (ClientSubscriptionManager client in _subscriptions.Values)
                {
                    IList<Message> unacknowledgedMessageList = null;

                    unacknowledgedMessageList = client.GetNeverAcknowledgedMessages(timeAfterAssignment);

                    if (unacknowledgedMessageList != null)
                    {
                        foreach (Message message in unacknowledgedMessageList)
                        {
                            if (!_duplicationCheck.ContainsKey(message.MessageId))
                            {
                                unacknowledgedMessages.Add(GetMessageInfo(message));
                                _duplicationCheck.Add(message.MessageId, null);
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
            ClientSubscriptionManager client;

            if (!_subscriptions.TryGetValue(subscriptionInfo.ClientId, out client))
                return false;

            lock (_mutex)
            {
                return client.AssignMessageToSubscription(message, subscriptionInfo);
            }
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

            lock (_mutex)
            {
                foreach (ClientSubscriptionManager client in _subscriptions.Values)
                {
                    client.GetClientStats(ref subscriberCount, ref publisherCount);
                    topicStats.SubscriberCount += subscriberCount;
                    topicStats.PublisherCount += publisherCount;
                }

            }
            return topicStats;
        }

        private bool AssignMessageToAllSubscriptions(SubscriptionInfo subscriptionInfo, Message message)
        {
            bool assigned = false;
            lock (_mutex)
            {

                foreach (var clientManager in _subscriptions.Values)
                {
                    if (clientManager.IsActive && clientManager.HasMessageSubscriptons(SubscriptionType.Subscriber))
                        assigned |= clientManager.AssignMessageToSubscription(message, subscriptionInfo);
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
            else
            {
                _undeliveredMessages.Remove(message.MessageId);
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
                    throw new Exception("Message with given id does not exist");

                if (subscriptionInfo == null) //revoke all subscriptions
                {
                    message.MessageMetaData.RevokeSubscriptions();
                }
                else
                {
                    if (_subscriptions.TryGetValue(subscriptionInfo.ClientId, out client))
                    {

                        message.MessageMetaData.RevokeSubscriptions(client);
                    }
                }

                messageRequed = RequeMessage(message);
            }

            if (messageRequed && _listener != null) _listener.OnMessageArrived(this);
        }

        private bool RequeMessage(Message message)
        {
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
                    case SubscriptionType.Publisher:
                        _undeliveredMessages.Enqueue(message.MessageId, message);
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

        public void AcknowledgeMessageReceipt(string clientId, IList<string> messageIds)
        {
            foreach (string messageId in messageIds)
            {
                AcknowledgeMessageReceipt(clientId, messageId);
            }
        }

        public void AcknowledgeMessageReceipt(string clientId, string messageId)
        {
            bool delivered = false;

            ClientSubscriptionManager client;

            lock (_mutex)
            {
                if (!_subscriptions.TryGetValue(clientId, out client))
                    return;

                Message message = null;
                if (_messages.TryGetValue(messageId, out message))
                {
                    client.AcknowledgeMessageRecepit(message);

                    if (message.MessageMetaData.Delivered)
                    {
                        _deliveredMessages.Enqueue(message.MessageId, message);
                        delivered = true;
                    }
                }
            }

            if (delivered && _listener != null) _listener.OnMessageDelivered(this);
        }

        public void OnSubscriptonRemoved(Message message, SubscriptionInfo subscriptionInfo)
        {
            bool messageRequed = false;
            lock (_mutex)
            {
                if (!_messages.TryGetValue(message.MessageId, out message))
                    return;

                messageRequed = RequeMessage(message);
            }

            if (messageRequed && _listener != null) _listener.OnMessageArrived(this);
        }

        public IList<string> GetNotifiableClients()
        {
            IList<string> clients = null;

            lock (_mutex)
            {
                foreach (ClientSubscriptionManager client in _subscriptions.Values)
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
                _undeliveredMessages.Clear();
                _deliveredMessages.Clear();

                foreach (ClientSubscriptionManager client in _subscriptions.Values)
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
                foreach (ClientSubscriptionManager client in _subscriptions.Values)
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
                foreach (ClientSubscriptionManager client in _subscriptions.Values)
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
                foreach (var clientSubscripton in _subscriptions)
                {
                    state.AddClientSubscirption(clientSubscripton.Key, clientSubscripton.Value.GetState());
                }
            }
            return state;
        }

        public void SetTopicState(State state)
        {
            if (state != null)
            {
                foreach (var clientSubscriptoin in state.ClientSubscriptions)
                {
                    if (clientSubscriptoin.Value.Subscriptions != null)
                    {
                        foreach (var subscripton in clientSubscriptoin.Value.Subscriptions)
                        {
                            CreateSubscription(new SubscriptionInfo() { ClientId = clientSubscriptoin.Key, SubscriptionId = subscripton.SubscriptionId, Type = subscripton.Type });
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

        #region /                               --- Topic State ---                                 /

        internal class State : ICompactSerializable
        {
            private string _topic;
            private IDictionary<string, ClientSubscriptionManager.State> _subscriptions = new Dictionary<string, ClientSubscriptionManager.State>(StringComparer.InvariantCultureIgnoreCase);

            public IDictionary<string, ClientSubscriptionManager.State> ClientSubscriptions { get { return _subscriptions; } }

            public string Topic { get { return _topic; } }

            public State(string topic) { _topic = topic; }

            public void AddClientSubscirption(string clientId, ClientSubscriptionManager.State clientState)
            {
                if (!_subscriptions.ContainsKey(clientId))
                {
                    _subscriptions.Add(clientId, clientState);
                }
            }

            public void Deserialize(CompactReader reader)
            {
                _topic = reader.ReadObject() as string;
                _subscriptions = SerializationUtility.DeserializeDictionary<string, ClientSubscriptionManager.State>(reader);
            }

            public void Serialize(CompactWriter writer)
            {
                writer.WriteObject(_topic);
                SerializationUtility.SerializeDictionary<string, ClientSubscriptionManager.State>(_subscriptions, writer);
            }
        }

        #endregion
        #endregion
    }
}
