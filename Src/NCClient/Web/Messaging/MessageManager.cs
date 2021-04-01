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
using System.Collections.Generic;
using System.Threading;
using Alachisoft.NCache.Runtime.Events;
using System.Collections.Concurrent;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Runtime.Caching.Messaging;
using System.Text.RegularExpressions;
using Alachisoft.NCache.Management.Statistics;
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Common.ErrorHandling;
namespace Alachisoft.NCache.Client
{
    internal class MessageManager : IDisposable
    {
        private readonly ConcurrentDictionary<TopicIdentity, Topic> _topicsDic;
        private PollNotificationCallback _pollNotificationCallback;
        private ReregisteredTopicCallback _reregisteredTopicCallback;
        private readonly EventManager _eventManager;
        private CacheImplBase _cacheImpl;
        private Thread _pollingThread;
        private bool _poll;
        private DateTime _lastPoll = DateTime.Now;
        private const int _pollingInterval = 10;  // polling interval is in seconds; i.e. 10 seconds
        private readonly object _lockObj = new object();
        private readonly StatisticsCounter _perfStatsCollector;
        private bool _isNotificationRegister = false;
        private long _version;

        internal MessageManager(EventManager eventManager, StatisticsCounter perfStatsCollector)
        {
            _topicsDic = new ConcurrentDictionary<TopicIdentity, Topic>();
            _eventManager = eventManager;
            _perfStatsCollector = perfStatsCollector;
        }

        internal void Initialize()
        {
            _pollNotificationCallback = OnPollNotified;
            _eventManager.RegisterPollingEvent(_pollNotificationCallback, EventTypeInternal.PubSub);
            _reregisteredTopicCallback = ReregisterTopic;

        }

        internal ReregisteredTopicCallback ReregisterTopicCallBack
        {
            get { return _reregisteredTopicCallback; }
        }
      
        internal ITopic GetOrCreateTopic(TopicIdentity topicPair, TopicOperationType type,bool internalOperation)
        {
            if (string.IsNullOrEmpty(topicPair.TopicName))
                throw new ArgumentException("TopicName is null or empty string");


            if(!internalOperation && IsDefaultTopicName(topicPair.TopicName))
                throw new OperationFailedException(ErrorCodes.PubSub.DEFAULT_TOPICS,ErrorMessages.GetErrorMessage(ErrorCodes.PubSub.DEFAULT_TOPICS));

            if (_cacheImpl == null)
                throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            Topic topic = null;

            if (_cacheImpl.GetOrCreate(topicPair.TopicName, type))
            {
                lock (this)
                {
                    if (_topicsDic.TryGetValue(topicPair, out topic))
                    {
                        if (topic.IsClosed)
                        {
                            _topicsDic.TryRemove(topicPair, out topic);
                            TopicSearchOptions searchOptions = topic.SearchOptions;
                            topic = new Topic(topicPair.TopicName, _cacheImpl, _perfStatsCollector, this);
                            topic.SearchOptions = searchOptions;
                        }

                        topic.IncrementRefCount();
                        return topic;
                    }

                    topic = new Topic(topicPair.TopicName, _cacheImpl, _perfStatsCollector, this);

                    topic.IncrementRefCount();
                    _topicsDic.TryAdd(topicPair, topic);
                }
            }
            return topic;
        }

        internal void StopPollingIfRequired(Topic topic)
        {
            bool keepPollingOn = false;

            if (topic == null || topic.ActiveSubscriptions <= 0)
            {
                lock (this)
                {
                    foreach (var registeredTopcs in _topicsDic.Values)
                    {
                        if (registeredTopcs.ActiveSubscriptions > 0 || registeredTopcs.HasFailureDeliveryNotification)
                        {
                            keepPollingOn = true;
                            break;
                        }
                    }


                    if (!keepPollingOn)
                        StopPolling();
                }
            }
        }

        private void StopPolling()
        {

            try
            {
                lock (this)
                {
                    _poll = false;
                    _isNotificationRegister = false;
                    if (_pollingThread != null)
#if !NETCORE
                        _pollingThread.Abort();
#elif NETCORE
                        _pollingThread.Interrupt();
#endif
                    _pollingThread = null;
                }
            }
            catch (Exception)
            {
            }
        }

        internal void OnSubscriptionCreated(Topic topic, TopicSubscription topicSubscription)
        {
            StartPolling();
        }

        internal void StartPolling()
        {
            lock (this)
            {
                if (!_isNotificationRegister)
                {
                    StartPollForMessage();
                    _isNotificationRegister = true;
                }
            }
        }

        private void StartPollForMessage()
        {
            _poll = true;
            _pollingThread = new Thread(PollForMessage);
            _pollingThread.IsBackground = true;
            _pollingThread.Name = _cacheImpl.Name + ":TopicPollingThread";
            _pollingThread.Start();
        }

        private void PollForMessage()
        {
            try
            {
                long currentVersion = -1;
                while (_poll)
                {
                    try
                    {
                        lock (_lockObj)
                        {
                            //wait only if there is no change event fired from server side causing version update
                            if (currentVersion == _version)
                                Monitor.Wait(_lockObj, _pollingInterval * 1000);
                        }
                        TimeSpan diff = DateTime.Now.Subtract(_lastPoll);
                        currentVersion = _version;
                        Poll();

                    }
                    catch (ThreadAbortException)
                    {
                        break;
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void Poll()
        {
             BitSet flagMap = new BitSet();
            ReceivedMessages assignedResponse = (ReceivedMessages)_cacheImpl.GetMessageData(flagMap);
            IDictionary<string, IList<MessageItem>> table = assignedResponse.AssignedMessages;
            if (assignedResponse != null)
            {
                //1: Send acknowlgement of messages to server side. 
                SendMessageAcknowledgements(assignedResponse);

                //2:  raise event OnMesssageRecieved or OnMessageDeliveryFailure based on message type to subsriber or publishers. 
                DeleiverValidMessagesToClient(assignedResponse);

                //3: If GetMessageData calls return dictionary with topic name and empty list. Then in this case verify topic existance on client side then either reregister topic or delete topic on client side
                // if user delete topic on server side from any publisher then subscriber must known about the delete topic call otherwise subscriber will always listening for upcoming messages
                // if subscriber client discoonected from server for short time period. While during this, any publisher recreated topic agian then after subscriber reconnect on the basic of poll command information registered topic again on server and clietn side.       
                TopicValidation(assignedResponse);
                
            }

            _lastPoll = DateTime.Now;
        }

        public void Dispose()
        {
            StopPolling();

            foreach (Topic messageTopic in _topicsDic.Values)
            {
                messageTopic.DisposeInternal(false);
            }
            _topicsDic.Clear();

            _cacheImpl = null;


        }

        internal void OnPollNotified()
        {
            lock (_lockObj)
            {
                _version++;
                Monitor.PulseAll(_lockObj);
            }
        }

        internal void ReregisterTopic()
        {
            foreach (Topic topic in _topicsDic.Values)
            {
                if(topic.SearchOptions==TopicSearchOptions.ByName)
                    _cacheImpl.GetOrCreate(topic.Name, TopicOperationType.Create);
                topic.ReregisterSubscribers(topic.SearchOptions);
            }
        }

        internal CacheImplBase CacheImpl
        {
            set { _cacheImpl = value; }
        }

        internal void TopicDisposeItself(Topic topic)
        {
            lock (this)
            {
                TopicIdentity topicPair = new TopicIdentity(topic.Name, topic.SearchOptions);
                Topic existingtopic;
                if (_topicsDic.TryGetValue(topicPair, out existingtopic) && existingtopic == topic)
                {
                    _topicsDic.TryRemove(topicPair, out existingtopic);
                }
            }
        }

        internal void DeleteTopic(string topicName)
        {

            if (_cacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (IsDefaultTopicName(topicName))
                return;
            
            TopicIdentity topicPair = new TopicIdentity(topicName, TopicSearchOptions.ByName);
            Topic topic;

            if (_topicsDic.TryRemove(topicPair, out topic))
            {
                if (topic.SearchOptions == TopicSearchOptions.ByName)
                {
                    topic.DisposeInternal(true);
                    StopPollingIfRequired(topic);
                }
            }
            _cacheImpl.RemoveTopic(topicName, true);
        }

       private void SendMesasgeAcknowledgment(Dictionary<string, IList<string>> acknowledgmentIdList)
        {
            _cacheImpl.AcknowledgeMessageReceipt(acknowledgmentIdList);
        }



        private void SendMessageAcknowledgements(ReceivedMessages assignedResponse)
        {
            var ackIdList = new Dictionary<string, IList<string>>();

            foreach (KeyValuePair<string, IList<MessageItem>> pair in assignedResponse.AssignedMessages)
            {
                IList<string> messageIds = new List<string>();
                foreach (MessageItem messageItem in pair.Value)
                {
                    messageIds.Add(messageItem.MessageId);
                }

                if (messageIds.Count > 0) ackIdList.Add(pair.Key, messageIds);
            }

            if (ackIdList.Count > 0) SendMesasgeAcknowledgment(ackIdList);
        }

        private void DeleiverValidMessagesToClient(ReceivedMessages assignedResponse)
        {
            int count = 0;

            foreach (KeyValuePair<string, IList<MessageItem>> pair in assignedResponse.AssignedMessages)
            {
                Topic selectedTopic = null;
                foreach (var topic in _topicsDic.Values)
                {
                    selectedTopic = GetValidTopic(topic, pair.Key);
                    if (selectedTopic != null)
                    {
                        if (pair.Value != null && pair.Value.Count > 0)
                        {
                           
                            count += pair.Value.Count;
                            selectedTopic.UpdateSyncData(pair.Value, pair.Key);
                        }
                       
                    }
                }
            }

            if (_perfStatsCollector != null)
            {
                _perfStatsCollector.IncrementMessageDeliverPerSec(count);
            }

        }


        private void TopicValidation(ReceivedMessages assignedResponse)
        {
            List<string> removeList = new List<string>();

            foreach (Topic topic in _topicsDic.Values)
            {
                TopicIdentity topicPair = new TopicIdentity(topic.Name, topic.SearchOptions);
                
                if (!assignedResponse.AssignedMessages.ContainsKey(topic.Name) && !assignedResponse.RegisteredPatterns.Contains(topic.Name))
                {
                    if (topic.HasFailureDeliveryNotification || topic.ActiveSubscriptions > 0)
                    {
                       
                        if (topic.SearchOptions == TopicSearchOptions.ByName)
                        {
                            if (GetOrCreateTopic(topicPair, TopicOperationType.Get, true) != null)
                                topic.ReregisterSubscribers(topic.SearchOptions);
                        }
                       
                        else
                        {
                            removeList.Add(topic.Name);
                            
                        }
                    }
                }
            }
        }


        private Topic GetValidTopic(Topic topic, string ServerSideTopicName)
        {
            switch (topic.SearchOptions)
            {
                case TopicSearchOptions.ByName:
                    if (topic.Name.Equals(ServerSideTopicName))
                        return topic;
                break;
            }

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


    }
}
