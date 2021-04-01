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

using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Events;
using System;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Caching.Messaging;
using Alachisoft.NCache.Management.Statistics;
using Alachisoft.NCache.Client.Services;
using Alachisoft.NCache.Caching.Events;

namespace Alachisoft.NCache.Client
{
    internal class MessagingService : IMessagingService
    {
        internal MessageManager PubSubManager { get; private set; }

        internal Cache CacheContainer { get; set; }

        private EventManager EventManager { get; set; }

        internal MessagingService(EventManager eventManager, StatisticsCounter perfStatsCollector,Cache cache)
        {
            PubSubManager = new MessageManager(eventManager, perfStatsCollector);
            PubSubManager.Initialize();
            CacheContainer = cache;
        }

        #region Topic Related Operations 

        public ITopic CreateTopic(string topicName)
        {
            if (string.IsNullOrEmpty(topicName))
                throw new ArgumentException("TopicName is null or empty string");
            TopicIdentity topicPair = new TopicIdentity(topicName, TopicSearchOptions.ByName);
            return PubSubManager.GetOrCreateTopic(topicPair, TopicOperationType.Create, false);
        }

        public void DeleteTopic(string topicName)
        {
            PubSubManager.DeleteTopic(topicName);
        }


        public ITopic GetTopic(string topicName)

        {
            TopicSearchOptions searchOptions = TopicSearchOptions.ByName;
            if (string.IsNullOrEmpty(topicName))
            {
                throw new ArgumentException("TopicName is null or empty string");
            }
           TopicIdentity topicPair = new TopicIdentity(topicName, TopicSearchOptions.ByName);
           return PubSubManager.GetOrCreateTopic(topicPair,  TopicOperationType.Get ,false);
           
        }

        internal ITopic GetTopic(string topicName, bool flag)
        {
            TopicIdentity topicPair = new TopicIdentity(topicName,TopicSearchOptions.ByName);
            return PubSubManager.GetOrCreateTopic(topicPair, TopicOperationType.Get, flag);
        }

        internal void OnReregisterTopic()
        {
            try
            {
                if (PubSubManager != null && PubSubManager.ReregisterTopicCallBack != null)
                {
                    PubSubManager.ReregisterTopicCallBack();
                }
            }
            catch { }
        }

        #endregion

        #region Register Notification Operations


      
        public virtual void RegisterCacheNotification(string key, CacheDataNotificationCallback callback, EventType eventType)
        {
            if (key == null || key.Length == 0) throw new ArgumentNullException("key");

            if (callback == null) throw new ArgumentException("callback");
            EventDataFilter datafilter = EventDataFilter.None;
            CacheContainer. RegisterCacheNotificationInternal(key, callback, eventType, datafilter, true);
        }

        public void RegisterCacheNotification(IEnumerable<string> keys, CacheDataNotificationCallback callback, EventType eventType)
        {
            if (keys == null) throw new ArgumentNullException("key");

            string[] keysList = new List<string>(keys).ToArray();

            for (int i = 0; i < keysList.Length; i++)
            {
                if (string.IsNullOrEmpty(keysList[i])) throw new ArgumentNullException("key can't be null or empty");
            }

            if (callback == null) throw new ArgumentException("callback");
            EventDataFilter datafilter = EventDataFilter.None;
            CacheContainer.RegisterCacheDataNotificationCallback(keysList, callback, eventType, datafilter, true);
        }



        #endregion

        #region Un-Register Notification Operations


        public void UnRegisterCacheNotification(string key, CacheDataNotificationCallback callback, EventType EventType)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (callback == null) throw new ArgumentNullException("callback");
            CacheContainer.UnRegisterCacheNotification(key, callback, EventsUtil.GetEventTypeInternal(EventType));
        }

        public void UnRegisterCacheNotification(IEnumerable<string> keys, CacheDataNotificationCallback callback, EventType EventType)
        {
            if (keys == null) throw new ArgumentNullException("key");

            string[] keysList = new List<string>(keys).ToArray();

            for (int i = 0; i < keysList.Length; i++)
            {
                if (string.IsNullOrEmpty(keysList[i])) throw new ArgumentNullException("key can't be null or empty");
            }

            if (callback == null) throw new ArgumentException("callback");

            CacheContainer.UnRegisterCacheNotification(keysList, callback, EventsUtil.GetEventTypeInternal(EventType));
        }

        #endregion

        #region IDisposable Impl
        internal void Dispose()
        {
            PubSubManager.Dispose();
        }
        #endregion

    }
}
