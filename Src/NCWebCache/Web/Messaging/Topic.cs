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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Web.Statistics;
using IMessage = Alachisoft.NCache.Runtime.Caching.IMessage;
using Alachisoft.NCache.Common.Enum;
#if NETCORE
using System.Threading.Tasks;
#endif

namespace Alachisoft.NCache.Web.Caching
{
    internal class Topic : ITopic
    {
#region Properties

        private readonly string _topicName;
        private readonly CacheImplBase _cacheImpl;
        private readonly PerfStatsCollector2 _perfStatsCollector;
        private readonly string _publisherId;
        private readonly MessageManager _parent;
        private TimeSpan _expiration = TimeSpan.MaxValue;

        private int _refCount;
        private ReaderWriterLock _readerWriterLock;
        private int _refPublisherCount;
        private Dictionary<string, TopicSubscription> _subscriptions;
        internal bool _isDisposed;
        private event MessageDeliveryFailureCallback _messageDeliveryFailure;
        private TopicDeletedCallback _topicDeletedCallBack;

#endregion

#region Constructor

        internal Topic(string topicName, CacheImplBase cacheImpl, PerfStatsCollector2 perfStatsCollector,
            MessageManager parent)
        {
            _topicName = topicName;
            _subscriptions = new Dictionary<string, TopicSubscription>(StringComparer.InvariantCultureIgnoreCase);
            _cacheImpl = cacheImpl;
            _perfStatsCollector = perfStatsCollector;
            _parent = parent;
            _publisherId = new Shorter().GuidString;
            _readerWriterLock = new ReaderWriterLock();
        }

#endregion

#region Public Methods

        /// <summary>
        /// Create subscription against the Topic. After creation, topic react as subscriber, now it recieve messages from server. 
        /// </summary>
        /// <param name="onMessageReceivedCallback"> Callback for message recceived from topic</param>
        /// <returns></returns>
        public ITopicSubscription CreateSubscription(MessageReceivedCallback onMessageReceivedCallback)
        {
            try
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                if (_isDisposed) throw new Exception(string.Format("Topic '{0}' is disposed.", Name));
                if (onMessageReceivedCallback == null) throw new ArgumentNullException("onMessageReceivedCallback");

                TopicSubscription topicSubscription = new TopicSubscription(this, onMessageReceivedCallback);
                Subscribe(topicSubscription);

                lock (_subscriptions)
                {
                    _subscriptions.Add(topicSubscription.SubscriberId, topicSubscription);
                }

                _parent.OnSubscriptionCreated(this, topicSubscription);

                return topicSubscription;
            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Register failure event against topic, when a topic failed to send message to any subscriber due to any reason. 
        /// That particular message will be fired if this event is registed. 
        /// reason of failure can be message expiration (could not delivery messsage to any subscriber within its expiry time), 
        /// or eviction occur upon cache full (message could not publish on cache store). 
        /// </summary>
        public event MessageDeliveryFailureCallback MessageDeliveryFailure
        {
            add
            {
                _messageDeliveryFailure += value;

                if (++_refPublisherCount == 1)
                {
                    RegisterPublisher(_publisherId);
                }
            }
            remove
            {
                _messageDeliveryFailure -= value;

                if (--_refPublisherCount == 0)
                {
                    UnRegisterPublisher(_publisherId);
                }
            }
        }

        public bool IsClosed
        {
            get
            {
                try
                {
                    _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                    return _isDisposed;
                }
                finally
                {
                    _readerWriterLock.ReleaseReaderLock();
                }
            }
        }

        /// <summary>
        /// Topic Name 
        /// </summary>
        public string Name
        {
            get { return _topicName; }
        }

        /// <summary>
        /// topoc  disposed, dispose any resource against topic. such as subscirbation. 
        /// </summary>
        public void Dispose()
        {
            DisposeInternal(false);
            _parent.TopicDisposeItself(this);
            _parent.StopPollingIfRequired(this);
        }

        /// <summary>
        /// Expiration time of Topic, That is if user not defiend message expiry then topic expiration is used for messages.  
        /// </summary>
        public TimeSpan ExpirationTime
        {
            get { return _expiration; }
            set { _expiration = value; }
        }
        
        public void Publish(Message message, DeliveryOption deliverOption, bool notifyDeliveryFailure = false)
        {
            try
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);

                if (message == null) throw new ArgumentNullException("message");
                if (_isDisposed) throw new Exception(string.Format("Topic '{0}' is disposed.", Name));

                UsageStats stats = new UsageStats();
                stats.BeginSample();
                BitSet flagMap = new BitSet();
                long size = 0;
                object value = message.Payload;


                value = _cacheImpl.SafeSerialize(value, _cacheImpl.Name, ref flagMap, _cacheImpl, ref size);


                if (_perfStatsCollector != null && value != null && value is byte[])
                    _perfStatsCollector.IncrementAvgItemSize(((byte[]) value).Length);


                Hashtable metaInfo = new Hashtable();
                metaInfo.Add(TopicConstant.TopicName, TopicConstant.TopicTag + _topicName);
                metaInfo.Add(TopicConstant.DeliveryOption, ((int) deliverOption).ToString());
                metaInfo.Add(TopicConstant.NotifyOption, notifyDeliveryFailure.ToString());

                long expiration = GetExpirationTime(message);

                _cacheImpl.PublishMessage(message.MessageId, value, message.CreationTime.Ticks, expiration, metaInfo,
                    flagMap);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerMessagePublish(stats.Current);
                    _perfStatsCollector.IncrementMessagePublishedPerSec();
                }
            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

        public TopicDeletedCallback OnTopicDeleted
        {
            set { _topicDeletedCallBack = value; }
        }

        internal void FireDeleteNotification()
        {
            if (_topicDeletedCallBack != null)
            {
#if !NETCORE
                _topicDeletedCallBack.BeginInvoke(this, new TopicDeleteEventArgs(Name), null, null);
#elif NETCORE
                TaskFactory factory = new TaskFactory();
                Task task = factory.StartNew(() => _topicDeletedCallBack(this, new TopicDeleteEventArgs(Name)));
#endif
            }
        }

#endregion

#region internal Methods

        internal int ActiveSubscriptions
        {
            get { return _subscriptions == null ? 0 : _subscriptions.Count; }
        }

        internal void Subscribe(TopicSubscription subscription)
        {
            try
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                if (_isDisposed) return;

                _cacheImpl.Subscribe(Name, subscription.SubscriberId, SubscriptionType.Subscriber);
            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

        internal void UnSubscribe(TopicSubscription subscription, bool remove = false)
        {
            try
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                if (_isDisposed) return;

                _cacheImpl.UnSubscribe(Name, subscription.SubscriberId, SubscriptionType.Subscriber);

                if (remove)
                {
                    lock (_subscriptions)
                    {
                        if (_subscriptions.ContainsKey(subscription.SubscriberId))
                            _subscriptions.Remove(subscription.SubscriberId);
                    }
                }

                _parent.StopPollingIfRequired(this);
            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Returns true if delivery notification is registered
        /// </summary>
        internal bool HasFailureDeliveryNotification
        {
            get { return _messageDeliveryFailure != null; }
        }

        internal void DisposeInternal(bool removeTopic)
        {
            try
            {
                _readerWriterLock.AcquireWriterLock(Timeout.Infinite);
                if (_isDisposed) return;

                if (_refCount > 0) Interlocked.Decrement(ref _refCount);
                if (removeTopic) Interlocked.Exchange(ref _refCount, 0);

                if (_refCount == 0 && !_isDisposed)
                {
                    if (_subscriptions != null)
                    {
                        lock (_subscriptions)
                        {
                            foreach (KeyValuePair<string, TopicSubscription> pair in _subscriptions)
                            {
                                pair.Value.Dispose();
                            }

                            _subscriptions.Clear();
                        }
                    }

                    if (HasFailureDeliveryNotification)
                        _cacheImpl.UnSubscribe(_topicName, _publisherId, SubscriptionType.Publisher);
                    _messageDeliveryFailure = null;
                    _subscriptions = null;
                    _isDisposed = true;
                }
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }
        }

        internal void IncrementRefCount()
        {
            Interlocked.Increment(ref _refCount);
        }

        internal void ReregisterSubscribers()
        {
            try
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                if (_isDisposed) return;

                lock (_subscriptions)
                {
                    foreach (string subscriberId in _subscriptions.Keys)
                    {
                        _cacheImpl.Subscribe(Name, subscriberId, SubscriptionType.Subscriber);
                    }
                }

                _cacheImpl.Subscribe(Name, _publisherId, SubscriptionType.Publisher);
            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

        internal void UpdateSyncData(IList<MessageItem> messageList)
        {
            try
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                if (_isDisposed) return;

                foreach (MessageItem messageItem in messageList)
                {
                    try
                    {
                        GetMessagePayLoad(messageItem);

                        ICollection<string> recipient = null;

                        if (messageItem.SubscriptionType == SubscriptionType.Publisher ||
                            messageItem.DeliveryOption == DeliveryOption.Any)
                        {
                            recipient = messageItem.RecipientList;
                        }
                        else
                        {
                            lock (_subscriptions)
                            {
                                recipient = new string[_subscriptions.Count];
                                _subscriptions.Keys.CopyTo((string[]) recipient, 0);
                            }
                        }

                        foreach (string info in recipient)
                        {
                            switch (messageItem.SubscriptionType)
                            {
                                case SubscriptionType.Subscriber:
                                    if (_subscriptions.ContainsKey(info))
                                        _subscriptions[info].OnMessageRecieved(GetMessageEventArgs(messageItem));
                                    break;

                                case SubscriptionType.Publisher:
                                    if (!_publisherId.Equals(info)) continue;
                                    OnMessageDeliveryFailure(GetMessageFailureEventArgs(messageItem));
                                    break;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

#endregion

#region Private Methods 

        private long GetExpirationTime(Message message)
        {
            long expiration = 0;
            if (message.ExpirationTime.HasValue)
            {
                expiration = message.ExpirationTime.Value.Ticks;
            }
            else if (ExpirationTime != null)
            {
                expiration = ExpirationTime.Ticks;
            }

            return expiration;
        }

        private MessageFailedEventArgs GetMessageFailureEventArgs(MessageItem messageItem)
        {
            IMessage message = GetMessage(messageItem);
            return new MessageFailedEventArgs(message, messageItem.DeliveryOption, this,
                messageItem.MessageFailureReason);
        }

        private MessageEventArgs GetMessageEventArgs(MessageItem messageItem)
        {
            IMessage message = GetMessage(messageItem);
            return new MessageEventArgs(message, messageItem.DeliveryOption, this);
        }

        private IMessage GetMessage(MessageItem messageItem)
        {
            var message = new Message(messageItem.Payload, messageItem.ExpirationTime);
            message.CreationTime  = messageItem.CreationTime;
            message.MessageId = messageItem.MessageId;

            return message;
        }

        private void GetMessagePayLoad(MessageItem messageItem)
        {
            if (messageItem != null)
            {
                if (messageItem.Payload is CallbackEntry)
                {
                    CallbackEntry e = messageItem.Payload as CallbackEntry;
                    messageItem.Payload = e.Value;
                }


                if (_perfStatsCollector != null && messageItem.Payload != null && messageItem.Payload is byte[])
                    _perfStatsCollector.IncrementAvgItemSize(((byte[]) messageItem.Payload).Length);


                messageItem.Payload =
                    _cacheImpl.SafeDeserialize(messageItem.Payload, _cacheImpl.Name, messageItem.Flag, _cacheImpl);
            }
        }

        private void UnRegisterPublisher(string publisherId)
        {
            _cacheImpl.UnSubscribe(_topicName, publisherId, SubscriptionType.Publisher);
            _parent.StopPollingIfRequired(this);
        }

        private void RegisterPublisher(string publisherId)
        {
            _cacheImpl.Subscribe(_topicName, publisherId, SubscriptionType.Publisher);
            _parent.StartPolling();
        }

        private void OnMessageDeliveryFailure(MessageFailedEventArgs args)
        {
            try
            {
                if (_messageDeliveryFailure != null)
                {
                    Delegate[] list = _messageDeliveryFailure.GetInvocationList();
                    foreach (Delegate t in list)
                    {
                        MessageDeliveryFailureCallback cb = t as MessageDeliveryFailureCallback;
                        if (cb != null)
                        {
#if !NETCORE
                            cb.BeginInvoke(this, args, null, null);
#elif NETCORE
                            TaskFactory factory = new TaskFactory();
                            Task task = factory.StartNew(() => cb(this, args));
#endif
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

#endregion
    }
}