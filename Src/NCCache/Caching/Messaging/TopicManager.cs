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
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Monitoring;
using System.Threading;

namespace Alachisoft.NCache.Caching.Messaging
{
    class TopicManager:ITopicEventListener,ISizableIndex
    {
        private CacheRuntimeContext _context;
        private IDictionary<string, Topic> _topics = new Dictionary<string, Topic>(StringComparer.InvariantCultureIgnoreCase);
        private long _size;
        private long _count;
        private object _mutex = new object();
        private RoundRobinLoadbalancer<string> _topicSelector = new RoundRobinLoadbalancer<string>();
        private RoundRobinLoadbalancer<string> _deliveryTopicSelector = new RoundRobinLoadbalancer<string>();
        private ITopicEventListener _listener;
        private ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public long IndexInMemorySize
        {
            get
            {
                return _size;
            }
        }

        public long Count
        {
            get { return _count; }
        }


        public TopicManager(CacheRuntimeContext context)
        {
            _context = context;
        }

        public void RegisterTopicListener(ITopicEventListener listener)
        {
            _listener = listener;
        }

        public void CreateTopic(string topic)
        {
            try
            {
                _rwLock.EnterWriteLock();
                if (!_topics.ContainsKey(topic))
                {
                    Topic instance = new Topic(topic, _context);
                    instance.RegisterEventListener(this);
                    _topics.Add(topic, instance);
                    _topicSelector.Add(topic);
                    _deliveryTopicSelector.Add(topic);

                    if (_context.PerfStatsColl != null)
                        _context.PerfStatsColl.SetPubSubCounter(_topics.Count, CounterNames.TopicCount);
                }
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public bool RemoveTopic(string topic)
        {
            try
            {
                _rwLock.EnterWriteLock();
                if (_topics.ContainsKey(topic))
                {
                    Topic topicInstance = _topics[topic];
                    _topics.Remove(topic);
                    _topicSelector.Remove(topic);
                    _deliveryTopicSelector.Remove(topic);
                    topicInstance.Dispose();

                    if (_context.PerfStatsColl != null)
                        _context.PerfStatsColl.SetPubSubCounter(_topics.Count, CounterNames.TopicCount);

                    return true;
                }
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }

            return false;
        }

        public Topic GetTopic(string topic)
        {
            Topic registeredTopic = null;

            try
            {
                _rwLock.EnterReadLock();

                if (string.IsNullOrEmpty(topic))
                    return null;
                _topics.TryGetValue(topic, out registeredTopic);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            return registeredTopic;
        }

        public void OnSubscriptionCreated(Topic topic)
        {
            if (_listener != null) _listener.OnSubscriptionCreated(topic);
        }

        public void OnSubscriptionRemoved(Topic topic)
        {
            if (_listener != null) _listener.OnSubscriptionRemoved(topic);
        }

        public void OnMessageArrived(Topic topic)
        {
            if (_listener != null) _listener.OnMessageArrived(topic);
        }

        public void OnMessageDelivered(Topic topic)
        {
            if (_listener != null) _listener.OnMessageDelivered(topic);
        }

        public void OnSizeIncrement(long sizeChange)
        {
            lock (_mutex)
            {
                _size += sizeChange;
                if (_context.PerfStatsColl != null) _context.PerfStatsColl.SetPubSubCounter(_size, CounterNames.MessageStoreSize);
            }
        }

        public void OnCountIncrement(long count)
        {
            lock (_mutex)
            {
                _count += count;
                if (_context.PerfStatsColl != null) _context.PerfStatsColl.SetPubSubCounter(_count, CounterNames.MessageCount);
            }
        }

        public void OnSizeDecrement(long sizeChange)
        {
            lock (_mutex)
            {
                _size -= sizeChange;
                if (_context.PerfStatsColl != null) _context.PerfStatsColl.SetPubSubCounter(_size, CounterNames.MessageStoreSize);
            }
        }

        public void OnCountDecrement(long count)
        {
            lock (_mutex)
            {
                _count -= count;
                if (_context.PerfStatsColl != null) _context.PerfStatsColl.SetPubSubCounter(_count, CounterNames.MessageCount);
            }
        }

        internal void OnClientDisconnected(string client)
        {
        }

        internal MessageInfo GetNextUnassignedMessage(TimeSpan timeout, OperationContext context)
        {
            MessageInfo message = null;

            string firstselectedTopic = null;
            string selectedTopic = null;
            while (true)
            {
                selectedTopic = _topicSelector.GetNext();

                if (string.IsNullOrEmpty(selectedTopic)) return message;

                //breaks the loop
                if (firstselectedTopic != null && String.Compare(selectedTopic, firstselectedTopic, true) == 0)
                    return null;

                if (firstselectedTopic == null) firstselectedTopic = selectedTopic;

                Topic topic = GetTopic(selectedTopic);

                if (topic != null)
                {
                    message = topic.GetNextUnassignedMessage(timeout, context);

                    if (message != null)
                    {
                        if (!topic.HasSubscriptions(SubscriptionType.Subscriber))
                        {
                            message = null;
                            continue;
                        }
                        break;
                    }
                }

            }

            return message;
        }

        internal MessageInfo GetNextUndeliveredMessage(OperationContext context)
        {
            MessageInfo message = null;

            string firstselectedTopic = null;
            string selectedTopic = null;
            while (true)
            {
                selectedTopic = _deliveryTopicSelector.GetNext();

                if (string.IsNullOrEmpty(selectedTopic)) return message;

                //breaks the loop
                if (firstselectedTopic != null && string.Compare(selectedTopic, firstselectedTopic, true) == 0)
                    return null;

                if (firstselectedTopic == null) firstselectedTopic = selectedTopic;

                Topic topic = GetTopic(selectedTopic);

                if (topic != null)
                {
                    message = topic.GetNextUndeliveredMessage(context);

                    if (message != null)
                    {
                        break;
                    } 
                }

            }

            return message;
        }

        internal IDictionary<string, IList<object>> GetAssignedMessages(SubscriptionInfo subscriptionInfo)
        {
            IDictionary<string, IList<object>> messageTable = new Dictionary<string, IList<object>>();
            try
            {
                _rwLock.EnterReadLock();

                foreach (Topic topic in _topics.Values)
                {
                    var messages = topic.GetAssignedMessages(subscriptionInfo);

                    if (messages != null)
                        messageTable.Add(topic.Name, messages);
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return messageTable;
        }

        internal void AcknowledgeMessageReceipt(string clientId, IDictionary<string, IList<string>> topicWiseMessageIds, OperationContext operationContext)
        {
            foreach(KeyValuePair<string,IList<string>> pair in topicWiseMessageIds)
            {
                Topic topic = GetTopic(pair.Key);

                if(topic != null)
                {
                    topic.AcknowledgeMessageReceipt(clientId, pair.Value);
                }
            }
        }

        internal void AcknowledgeMessageReceipt(string clientId, string topic,string messagId, OperationContext operationContext)
        {
            Topic topicInstance = GetTopic(topic);

            if (topicInstance != null)
            {
                topicInstance.AcknowledgeMessageReceipt(clientId, messagId);
            }
            
        }

        internal IList<MessageInfo> GetDeliveredMessages()
        {
            List<MessageInfo> messageList = new List<MessageInfo>();
            try
            {
                _rwLock.EnterReadLock();
                foreach (Topic topic in _topics.Values)
                {
                    var message = topic.GetDeliveredMessage(TimeSpan.MaxValue, null);
                    if (message != null) messageList.AddRange(message);
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return messageList;
        }

        internal IList<MessageInfo> GetUnAcknowledgedMessages(TimeSpan timeAfterAssignment)
        {
            List<MessageInfo> messageList = new List<MessageInfo>();
            try
            {
                _rwLock.EnterReadLock();
                foreach (Topic topic in _topics.Values)
                {
                    var messages = topic.GetNeverAcknowledgedMessages(timeAfterAssignment);
                    if (messages != null) messageList.AddRange(messages);
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return messageList;
        }

        internal IList<MessageInfo> GetExpiredMessages()
        {
            List<MessageInfo> messageList = new List<MessageInfo>();
            try
            {
                _rwLock.EnterReadLock();
                foreach (Topic topic in _topics.Values)
                {
                    var messages = topic.GetExpiredMessages();

                    if (messages != null)
                        messageList.AddRange(messages);
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return messageList;
        }

        internal IList<string> GetNotifiableClients()
        {
            List<string> clients = new List<string>();
            HashSet<string> duplicateCheck = new HashSet<string>();

            try
            {
                _rwLock.EnterReadLock();
                foreach (Topic topic in _topics.Values)
                {
                    var notifiableClients = topic.GetNotifiableClients();

                    if (notifiableClients != null)
                    {
                        foreach (string client in notifiableClients)
                        {
                            if (!duplicateCheck.Contains(client))
                            {
                                duplicateCheck.Add(client);
                                clients.Add(client);
                            }
                        }
                    }
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return clients;
        }

        internal IList<MessageInfo> GetEvicatableMessages(long sizeToEvict)
        {
            List<MessageInfo> messageList = new List<MessageInfo>();

            try
            {
                _rwLock.EnterReadLock();

                long totalEvictedSize = 0;

                if (_size <= 0) return messageList;

                foreach (var topic in _topics.Values)
                {
                    double topicRatio = (double)topic.IndexInMemorySize / (double)_size;

                    long evictionPerTopic = (long)Math.Ceiling(topicRatio * sizeToEvict);

                    if (evictionPerTopic > 0)
                    {
                        long evictedSize;
                        var messages = topic.GetEvicatableMessages(evictionPerTopic, out evictedSize);

                        if (messages != null)
                            messageList.AddRange(messages);

                        totalEvictedSize += evictedSize;

                        if (totalEvictedSize >= sizeToEvict)
                            break;
                    }
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return messageList;
        }
        
        internal IList<Message> RemoveMessages(IList<MessageInfo> messagesTobeRemoved,MessageRemovedReason reason)
        {
            IList<Message> removedMessages = new List<Message>(messagesTobeRemoved.Count);
            try
            {
                _rwLock.EnterReadLock();
                foreach (MessageInfo message in messagesTobeRemoved)
                {
                    Topic topic = GetTopic(message.Topic);

                    if (topic != null)
                    {
                        Message removed = topic.RemoveMessage(message.MessageId, reason);
                        if (removed != null) removedMessages.Add(removed);
                    }
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return removedMessages;
        }

        internal Message RemoveMessage(MessageInfo message, MessageRemovedReason reason)
        {
            Topic topic = GetTopic(message.Topic);

            if (topic != null)
            {
                return topic.RemoveMessage(message.MessageId, reason);
            }
            return null;
        }

        public void Clear()
        {
            try
            {
                _rwLock.EnterReadLock();
                foreach (Topic topic in _topics.Values)
                {
                    topic.Clear();
                }

                lock (_mutex)
                {
                    _count = _size = 0;
                    if (_context.PerfStatsColl != null)
                    {
                        _context.PerfStatsColl.SetPubSubCounter(_size, CounterNames.MessageStoreSize);
                        _context.PerfStatsColl.SetPubSubCounter(_count, CounterNames.MessageCount);
                    }
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }


        internal IDictionary<string,IList<string>> GetInActiveClientSubscriptions(TimeSpan inactivityThreshold)
        {
            IDictionary<string, IList<string>> topicWiseInactiveClients = new Dictionary<string, IList<string>>();
            try
            {
                _rwLock.EnterReadLock();

                foreach (Topic topic in _topics.Values)
                {
                    IList<string> inactiveClients = topic.GetInActiveClientSubscriptions(inactivityThreshold);

                    if (inactiveClients != null && inactiveClients.Count > 0)
                        topicWiseInactiveClients.Add(topic.Name, inactiveClients);
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return topicWiseInactiveClients;
        }

        internal IDictionary<string, IList<string>> GetActiveClientSubscriptions(TimeSpan inactivityThreshold)
        {
            IDictionary<string, IList<string>> topicWiseActiveClients = new Dictionary<string, IList<string>>();
            try
            {
                _rwLock.EnterReadLock();
                foreach (Topic topic in _topics.Values)
                {
                    IList<string> inactiveClients = topic.GetActiveClientSubscriptions(inactivityThreshold);

                    if (inactiveClients != null && inactiveClients.Count > 0)
                        topicWiseActiveClients.Add(topic.Name, inactiveClients);
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return topicWiseActiveClients;
        }

        #region /                       ---- State transfer related stuff ---                           /

        public ArrayList GetTopicsState()
        {
            ArrayList topicStates = new ArrayList();

            try
            {
                _rwLock.EnterReadLock();
                foreach (var topic in _topics)
                {
                      topicStates.Add(topic.Value.GetTopicState());
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            return topicStates;
        }

        public void SetTopicsState(ArrayList topicStates)
        {
            if(topicStates != null)
            {
                foreach(Topic.State topicState in topicStates)
                {
                    try
                    {
                        _rwLock.EnterWriteLock();
                        Topic topic = null;
                        CreateTopic(topicState.Topic);

                        if (_topics.TryGetValue(topicState.Topic, out topic))
                        {
                            topic.SetTopicState(topicState);
                        }
                    }
                    finally
                    {
                        _rwLock.ExitWriteLock();
                    }
                }
            }
        }

        internal TransferrableMessage GetTransferrableMessage(string topic,string messageId)
        {
            Topic topicInstance = GetTopic(topic);

            if (topicInstance != null)
            {
                return topicInstance.GetTransferrableMessage(messageId);
            }
            return null;
        }


        internal bool StoreTransferrableMessage(string topic,TransferrableMessage message)
        {
            Topic topicInstance = GetTopic(topic);

            if (topicInstance != null)
            {
                topicInstance.StoreTransferrableMessage(message);
                return true;
            }

            return false;
        }

        internal OrderedDictionary GetMessageList()
        {
            OrderedDictionary topicWiseMessageList = new OrderedDictionary();

            try
            {
                _rwLock.EnterReadLock();
                foreach (var topic in _topics)
                {
                    topicWiseMessageList.Add(topic.Key,topic.Value.GetMessageList());
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            return topicWiseMessageList;
        }

        internal Dictionary<string,  TopicStats> GetTopicsStats()
        {
            Dictionary<string ,TopicStats> topicStatsList = new Dictionary<string, TopicStats>();
            try
            {
                _rwLock.EnterReadLock();

                foreach (var topic in _topics.Values)
                {
                    topicStatsList.Add(topic.Name, topic.GetTopicsStats());
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return topicStatsList;
        }

        #endregion
    }
}
