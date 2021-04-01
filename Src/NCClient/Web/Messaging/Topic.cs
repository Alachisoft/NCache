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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Runtime.Caching;
using IMessage = Alachisoft.NCache.Runtime.Caching.IMessage;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Caching.Messaging;
using Alachisoft.NCache.Management.Statistics;
using static Alachisoft.NCache.Client.EventUtil;
using System.Threading.Tasks;
using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// 
    /// </summary>
    internal class Topic : ITopic, ITopicSubscription
    {
        #region Properties
        private readonly string _topicName;
        private readonly CacheImplBase _cacheImpl;
        private readonly StatisticsCounter _perfStatsCollector;
        private readonly string _publisherId;
        private readonly MessageManager _parent;
        private TimeSpan _expiration = TimeSpan.MaxValue;

        private int _refCount;
        private ReaderWriterLock _readerWriterLock;
        private int _refPublisherCount;
        private Dictionary<SubscriptionIdentifier, TopicSubscription> _subscriptions;
        internal bool _isDisposed;
        private event MessageDeliveryFailureCallback _messageDeliveryFailure;
        public event MessageReceivedCallback OnMessageRecieved;

      
        private TopicSearchOptions _searchOptions = TopicSearchOptions.ByName;

        private bool _eventRegistered = false;
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="cacheImpl"></param>
        /// <param name="perfStatsCollector"></param>
        /// <param name="parent"></param>
        internal Topic(string topicName, CacheImplBase cacheImpl, StatisticsCounter perfStatsCollector, MessageManager parent)
        {
            _topicName = topicName;
            _subscriptions = new Dictionary<SubscriptionIdentifier, TopicSubscription>();
            _cacheImpl = cacheImpl;
            _perfStatsCollector = perfStatsCollector;
            _parent = parent;
            _publisherId = new Shorter().GuidString;
            _readerWriterLock = new ReaderWriterLock();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Create subscription against the Topic. After creation, topic react as subscriber, now it receive messages from server. 
        /// </summary>
        /// <param name="onMessageReceivedCallback"> Callback for message received from topic</param>
        /// <returns></returns>
        public ITopicSubscription CreateSubscription(MessageReceivedCallback onMessageReceivedCallback)
        {

            if (_isDisposed) throw new OperationFailedException(ErrorCodes.PubSub.TOPIC_DISPOSED,ErrorMessages.GetErrorMessage(ErrorCodes.PubSub.TOPIC_DISPOSED,Name));
            if (onMessageReceivedCallback == null) throw new ArgumentNullException("onMessageReceivedCallback");
            string subscriptionName = new Shorter().GuidString;
            return RegisterSubscriptions(subscriptionName, onMessageReceivedCallback);

        }
        public ITopicSubscription CreateEventSubscription(MessageReceivedCallback onMessageReceivedCallback)
        {

            if (_isDisposed) throw new OperationFailedException(ErrorCodes.PubSub.TOPIC_DISPOSED, ErrorMessages.GetErrorMessage(ErrorCodes.PubSub.TOPIC_DISPOSED, Name));
            if (onMessageReceivedCallback == null) throw new ArgumentNullException("onMessageReceivedCallback");
            string subscriptionName = SubscriptionInfo.EventsSubscriptionName;
            return RegisterSubscriptions(subscriptionName, onMessageReceivedCallback,SubscriptionPolicyType.EventSubscription);

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
        /// Number of messages published for this topic.
        /// </summary>
        /// <remarks>
        /// This property returns value for a specific topic. Count of other topics play no role.
        /// <para>
        /// <b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.
        /// </para>
        /// </remarks>
        /// <value>The number of messages published for a specific topic.</value>
        public long MessageCount
        {
            get
            {
                if (IsClosed)
                {
                    throw new ObjectDisposedException("Topic +  : " + _topicName);
                }
                if (_cacheImpl != null)
                {
                    return _cacheImpl.GetMessageCount(_topicName);
                }
                return 0;
            }
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
        /// Expiration time of Topic, That is if user not defined message expiry then topic expiration is used for messages.  
        /// </summary>
        public TimeSpan ExpirationTime { get { return _expiration; } set { _expiration = value; } }

        /// <summary>
        /// Publishes the messages.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="deliverOption"></param>
        /// <param name="notifyDeliveryFailure"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Publish(Message message, DeliveryOption deliverOption, bool notifyDeliveryFailure = false)
        {
            if (TopicSearchOptions.ByName != _searchOptions) throw new OperationFailedException(ErrorCodes.PubSub.PATTERN_BASED_PUBLISHING_NOT_ALLOWED,ErrorMessages.GetErrorMessage(ErrorCodes.PubSub.PATTERN_BASED_PUBLISHING_NOT_ALLOWED));
            try
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);

                if (message == null) throw new ArgumentNullException("message");
                if (_isDisposed) throw new OperationFailedException(ErrorCodes.PubSub.TOPIC_DISPOSED, ErrorMessages.GetErrorMessage(ErrorCodes.PubSub.TOPIC_DISPOSED, Name));

                UsageStats stats = new UsageStats();
                stats.BeginSample();
                BitSet flagMap = new BitSet();
                long size = 0;
                object value = message.Payload;


                value = _cacheImpl.SafeSerialize(value, _cacheImpl.Name, ref flagMap, _cacheImpl, ref size, UserObjectType.CacheItem);


                if (_perfStatsCollector != null && value != null && value is byte[])
                    _perfStatsCollector.IncrementAvgItemSize(((byte[])value).Length);

                Hashtable metaInfo = new Hashtable();
                metaInfo.Add(TopicConstant.TopicName, TopicConstant.TopicTag + _topicName);
                metaInfo.Add(TopicConstant.DeliveryOption, ((int)deliverOption).ToString());
                metaInfo.Add(TopicConstant.NotifyOption, notifyDeliveryFailure.ToString());

                long expiration = GetExpirationTime(message);

                _cacheImpl.PublishMessage(message.MessageId, value, message.CreationTime.Ticks, expiration, metaInfo, flagMap);

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




        #endregion

        #region internal Methods

        internal int ActiveSubscriptions
        {
            get { return _subscriptions == null ? 0 : _subscriptions.Count; }
        }

        internal void Subscribe(TopicSubscription subscription,SubscriptionPolicyType subscriptionPolicy=SubscriptionPolicyType.NonDurableExclusiveSubscription)
        {
            try
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);

                if (_isDisposed) return;
                 _cacheImpl.Subscribe(Name, subscription.SubscriptionName,   SubscriptionType.Subscriber ,  subscription.CreationTime, subscription.Expiration, subscriptionPolicy);
            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

        internal void UnSubscribe(TopicSubscription subscription, bool remove = false,bool dispose=false)
        {
            try
            {
                SubscriptionIdentifier subscriptionIdentifier = new SubscriptionIdentifier(subscription.SubscriptionName, subscription.GetSubscriptionPolicyType);
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                if (_isDisposed) return;
                
                _cacheImpl.UnSubscribe(Name, subscription.SubscriptionName,subscription.GetSubscriptionPolicyType,  SubscriptionType.Subscriber,dispose);

                if (remove)
                {
                    lock (_subscriptions)
                    {
                     _subscriptions.Remove(subscriptionIdentifier);
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
        internal bool HasFailureDeliveryNotification { get { return _messageDeliveryFailure != null; } }

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
                            foreach (KeyValuePair<SubscriptionIdentifier, TopicSubscription> pair in _subscriptions)
                            {
                                pair.Value.Dispose();
                            }
                            _subscriptions.Clear();
                        }
                    }
                    if (HasFailureDeliveryNotification)
                        _cacheImpl.UnSubscribe(_topicName, _publisherId,SubscriptionPolicyType.NonDurableExclusiveSubscription, SubscriptionType.Publisher);
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

        internal void ReregisterSubscribers(TopicSearchOptions searchOption)
        {
                      try
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);

                if (_isDisposed) return;
             
                lock (_subscriptions)
                {
                    switch (searchOption)
                    {
                       
                        case TopicSearchOptions.ByName:
                         
                            foreach (KeyValuePair<SubscriptionIdentifier, TopicSubscription> subscriptions in _subscriptions)
                            {
                                _cacheImpl.Subscribe(Name, subscriptions.Value.SubscriptionName, SubscriptionType.Subscriber, subscriptions.Value.CreationTime, subscriptions.Value.Expiration, subscriptions.Value.GetSubscriptionPolicyType);
                            }
                            break;

                    }

                }

            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

        internal void UpdateSyncData(IList<MessageItem> messageList,string topicName)
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
                        List<SubscriptionIdentifier> subscriptionReciepientList=null;
                        if (messageItem.SubscriptionType == SubscriptionType.Publisher || messageItem.DeliveryOption == DeliveryOption.Any)
                        {
                            subscriptionReciepientList = messageItem.SubscriptionIdentifierList;
                        }
                        else
                        {
                            lock (_subscriptions)
                            {
                                subscriptionReciepientList = new List<SubscriptionIdentifier>(this._subscriptions.Keys);
                            }
                        }

                        foreach (var info in subscriptionReciepientList)
                        {
                            switch (messageItem.SubscriptionType)
                            {
                                case SubscriptionType.Subscriber:
                                    if (_subscriptions.ContainsKey(info))
                                    {
                                        _subscriptions[info].OnMessageRecieved(GetMessageEventArgs(messageItem, topicName));
                                    }
                                    break;
                               
                                case SubscriptionType.Publisher:
                                    if (!_publisherId.Equals(info.SubscriptionName)) continue;
                                    OnMessageDeliveryFailure(GetMessageFailureEventArgs(messageItem));
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

        internal void UnSubscribeEventTopic(TopicSubscription subscription, bool remove = false)
        {
            try
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                UnSubscribe(subscription, remove);
                _eventRegistered = false;
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
            return new MessageFailedEventArgs(message, messageItem.DeliveryOption, this, messageItem.MessageFailureReason);
        }

        private MessageEventArgs GetMessageEventArgs(MessageItem messageItem, string topicName)
        {
            IMessage message = GetMessage(messageItem);
            return new MessageEventArgs(message, messageItem.DeliveryOption, this, topicName);
        }

        private IMessage GetMessage(MessageItem messageItem)
        {
            var message = new Message(messageItem.Payload, messageItem.ExpirationTime);
            message.CreationTime = messageItem.CreationTime;
            message.MessageId = messageItem.MessageId;

            return message;
        }

        private void GetMessagePayLoad(MessageItem messageItem)
        {
            if (messageItem!=null && messageItem.Payload!=null)
            {
                MessageItemType type = GetMessageItemType(messageItem);

                switch (type) {

                    case MessageItemType.MessageItems:
                        GetMessageItemPayLoad(messageItem);
                        break;

                    case MessageItemType.MessageEventItem:
                        messageItem.Payload =  GetMessageEventItemPayLoad((MessageEventItem)messageItem.Payload);
                        break;
                    case MessageItemType.MessageEventItems:
                        MessageEventItem[] items = (MessageEventItem[])messageItem.Payload;
                     
                        if (items!=null)
                        {
                            MessageEventItem[] payLoads = new MessageEventItem[items.Length];

                            for(int count=0; count<items.Length; count++)
                            {
                               payLoads[count] = GetMessageEventItemPayLoad(items[count]);
                            }
                            messageItem.Payload = payLoads;
                        }
                        break;
                  
                  
                }

            }
           
        }

     
        private MessageEventItem GetMessageEventItemPayLoad(MessageEventItem eventItem)
        {
            if (eventItem != null && eventItem.Item != null)
            {
                eventItem.Item.SetValue(_cacheImpl.SafeDeserialize<object>(eventItem.Item.GetValue<object>(), _cacheImpl.Name, eventItem.Item.Flagmap, _cacheImpl, UserObjectType.CacheItem));
                
            }
            return eventItem;

        }

        private void GetMessageItemPayLoad(MessageItem messageItem)
        {
            if (messageItem != null && !messageItem.Deserialized)
            {
                if (messageItem.Payload != null)
                    if (messageItem.Flag.IsBitSet(BitSetConstants.Compressed))
                        messageItem.Payload = CompressionUtil.Decompress((byte[])messageItem.Payload);

                messageItem.Payload = _cacheImpl.SafeDeserialize<object>(messageItem.Payload, _cacheImpl.Name, messageItem.Flag, _cacheImpl, UserObjectType.CacheItem);
                messageItem.Deserialized = true;
            }
        }
        

        private void UnRegisterPublisher(string publisherId)
        {
            _cacheImpl.UnSubscribe(_topicName, publisherId,SubscriptionPolicyType.NonDurableExclusiveSubscription,  SubscriptionType.Publisher );
            _parent.StopPollingIfRequired(this);
        }

        private void RegisterPublisher(string publisherId)
        {
            long expieration = TimeSpan.MaxValue.Ticks;
            _cacheImpl.Subscribe(_topicName, publisherId,SubscriptionType.Publisher ,DateTime.Now.Ticks,expieration);
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
                            //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                            TaskFactory factory = new TaskFactory();
                            Task task = factory.StartNew(() => cb(this, args));
#endif
                        }

                    }
                }
            }
            catch (Exception)
            { }
        }

        internal ITopicSubscription RegisterSubscriptions(string subscriptionName, MessageReceivedCallback onMessageReceivedCallback, SubscriptionPolicyType subscriptionPolicy = SubscriptionPolicyType.NonDurableExclusiveSubscription, TimeSpan? timeSpan = default(TimeSpan?))
        {
            try
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                SubscriptionIdentifier subscriptionIdentifier = new SubscriptionIdentifier(subscriptionName, subscriptionPolicy);
                TopicSubscription topicSubscription = GetExistingSubscription(subscriptionIdentifier,subscriptionPolicy);

                if(topicSubscription == null)
                {
                    topicSubscription = new TopicSubscription(this, subscriptionName, subscriptionPolicy, onMessageReceivedCallback);
                    DateTime creationTime = DateTime.Now;
                    topicSubscription.CreationTime = creationTime.Ticks;
                    topicSubscription.SetSubscriptionPolicy(subscriptionPolicy);

                    if (timeSpan == null)
                        topicSubscription.Expiration = TimeSpan.MaxValue.Ticks;
                    else
                        topicSubscription.Expiration = timeSpan.Value.Ticks;

                    Subscribe(topicSubscription, subscriptionPolicy);

                    lock (_subscriptions)
                    {
                        var existingSubscription = GetExistingSubscription(subscriptionIdentifier, subscriptionPolicy);

                        if (existingSubscription == null)
                        {
                            _subscriptions.Add(subscriptionIdentifier, topicSubscription);
                        }
                        else
                            return existingSubscription;
                       
                    }

                    _parent.OnSubscriptionCreated(this, topicSubscription);
                    
                }
                return topicSubscription;
            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

        private TopicSubscription GetExistingSubscription(SubscriptionIdentifier subscriptionIdentifier, SubscriptionPolicyType subscriptionPolicy)
        {
            TopicSubscription topicSubscription = null;
            if (_subscriptions.TryGetValue(subscriptionIdentifier, out topicSubscription))
            {
                if ((subscriptionPolicy!=SubscriptionPolicyType.EventSubscription))
                    throw new OperationFailedException(ErrorCodes.PubSub.SUBSCRIPTION_EXISTS,ErrorMessages.GetErrorMessage(ErrorCodes.PubSub.SUBSCRIPTION_EXISTS));
                else
                    topicSubscription.UpdateCount();
            }
            return topicSubscription;
        }

        public void UnSubscribe()
        {
            throw new NotImplementedException();
        }


     internal TopicSearchOptions SearchOptions
        {
            get { return _searchOptions; }
            set { this._searchOptions = value; }
        }



        public string SubscriptionName
        {
            get
            {
                throw new NotImplementedException();
            }
        }



        ITopic ITopicSubscription.Topic
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public SubscriptionPolicy SubscriptionPolicy
        {
            get
            {
                throw new NotImplementedException();
            }
        }

      
        #endregion

    }
}
