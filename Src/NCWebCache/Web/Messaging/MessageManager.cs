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
using System.Collections.Generic;
using System.Threading;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Web.Statistics;
using System.Collections.Concurrent;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common;
using System.Collections;

namespace Alachisoft.NCache.Web.Caching
{
    internal class MessageManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, Topic> _topicsDic;
        private PollNotificationCallback _pollNotificationCallback;
        private ReregisteredTopicCallback _reregisteredTopicCallback;
        private readonly EventManager _eventManager;
        private CacheImplBase _cacheImpl;
        private Thread _pollingThread;
        private bool _poll;
        private DateTime _lastPoll = DateTime.Now;
        private const int _pollingInterval = 10; // polling interval is in seconds; i.e. 10 seconds
        private readonly object _lockObj = new object();
        private readonly PerfStatsCollector2 _perfStatsCollector;
        private bool _isNotificationRegister = false;
        private long _version;

        internal MessageManager(EventManager eventManager, PerfStatsCollector2 perfStatsCollector)
        {
            _topicsDic = new ConcurrentDictionary<string, Topic>(StringComparer.InvariantCultureIgnoreCase);
            _eventManager = eventManager;
            _perfStatsCollector = perfStatsCollector;
        }

        internal void Initialize()
        {
            _pollNotificationCallback = OnPollNotified;
            _eventManager.RegisterPollingEvent(_pollNotificationCallback, EventType.PubSub);
            _reregisteredTopicCallback = ReregisterTopic;
        }

        internal ReregisteredTopicCallback ReregisterTopicCallBack
        {
            get { return _reregisteredTopicCallback; }
        }

        internal ITopic GetOrCreateTopic(string topicName, TopicOperationType type)
        {
            if (string.IsNullOrEmpty(topicName))
                throw new ArgumentException("TopicName is null or empty string");

            Topic topic = null;

            if (_cacheImpl.GetOrCreate(topicName, type))
            {
                lock (this)
                {
                    if (_topicsDic.TryGetValue(topicName, out topic))
                    {
                        if (topic.IsClosed)
                        {
                            _topicsDic.TryRemove(topic.Name, out topic);

                            topic = new Topic(topicName, _cacheImpl, _perfStatsCollector, this);
                            _topicsDic.TryAdd(topicName, topic);
                        }

                        topic.IncrementRefCount();
                        return topic;
                    }

                    topic = new Topic(topicName, _cacheImpl, _perfStatsCollector, this);
                    topic.IncrementRefCount();
                    _topicsDic.TryAdd(topicName, topic);
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
#else
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
                            //wait only if there is no change event fired from server side causing verion update
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
                    catch (Exception)
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
            IDictionary<string, IList<MessageItem>> table =
                (IDictionary<string, IList<MessageItem>>) _cacheImpl.GetMessageData(flagMap);

            if (table != null)
            {
                var ackIdList = new Dictionary<string, IList<string>>();

                foreach (KeyValuePair<string, IList<MessageItem>> pair in table)
                {
                    IList<string> messageIds = new List<string>();
                    foreach (MessageItem messageItem in pair.Value)
                    {
                        messageIds.Add(messageItem.MessageId);
                    }

                    if (messageIds.Count > 0) ackIdList.Add(pair.Key, messageIds);
                }

                if (ackIdList.Count > 0) SendMesasgeAcknowledgment(ackIdList);

                int count = 0;
                foreach (KeyValuePair<string, IList<MessageItem>> pair in table)
                {
                    Topic topic;
                    if (_topicsDic.TryGetValue(pair.Key, out topic))
                    {
                        if (pair.Value != null && pair.Value.Count > 0)
                        {
                            count += pair.Value.Count;
                            topic.UpdateSyncData(pair.Value);
                        }
                    }
                }

                if (_perfStatsCollector != null)
                {
                    _perfStatsCollector.IncrementMessageDeliverPerSec(count);
                }

                List<string> removeList = new List<string>();
                foreach (Topic topic in _topicsDic.Values)
                {
                    if (!table.ContainsKey(topic.Name))
                    {
                        if (topic.HasFailureDeliveryNotification || topic.ActiveSubscriptions > 0)
                        {
                            if (GetOrCreateTopic(topic.Name, TopicOperationType.Get) != null)
                            {
                                topic.ReregisterSubscribers();
                            }
                            else
                            {
                                removeList.Add(topic.Name);
                                topic.FireDeleteNotification();
                            }
                        }
                    }
                }

                foreach (string topicName in removeList)
                {
                    if (_topicsDic.ContainsKey(topicName))
                    {
                        Topic topic;
                        _topicsDic.TryRemove(topicName, out topic);
                        topic.Dispose();
                    }
                }
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
                _cacheImpl.GetOrCreate(topic.Name, TopicOperationType.Create);
                topic.ReregisterSubscribers();
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
                Topic existingtopic;
                if (_topicsDic.TryGetValue(topic.Name, out existingtopic) && existingtopic == topic)
                {
                    _topicsDic.TryRemove(topic.Name, out existingtopic);
                }
            }
        }

        internal void DeleteTopic(string topicName)
        {
            Topic topic;
            if (_topicsDic.TryRemove(topicName, out topic))
            {
                topic.DisposeInternal(true);
                topic.FireDeleteNotification();
                StopPollingIfRequired(topic);
            }

            _cacheImpl.RemoveTopic(topicName, true);
        }

        private void SendMesasgeAcknowledgment(Dictionary<string, IList<string>> acknowledgmentIdList)
        {
            _cacheImpl.AcknowledgeMessageReceipt(acknowledgmentIdList);
        }
    }
}