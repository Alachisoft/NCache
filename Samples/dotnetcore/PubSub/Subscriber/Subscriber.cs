// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Web.Caching;
using System;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that provides the functionality of the publisher
    /// </summary>
    public class Subscriber : IDisposable
    {
        private ITopic _topic;
        private Cache _cache;

        /// <summary>
        /// This method initializes the specified cache and subscribes to the specified topic for the published messages on it.
        /// </summary>
        /// <param name="cacheName">The name of the cache to initialize.</param>
        /// <param name="topicName">The name of the topic to subscribe to.</param>
        /// <param name="messageReceivedCallback">The callback which will be invoked when a message is published on the topic.</param>
        public void Subscribe(string cacheName, string topicName, MessageReceivedCallback messageReceivedCallback)
        {
            // Initializes the cache.
            _cache = Web.Caching.NCache.InitializeCache(cacheName);
            
            // Gets the topic.
            _topic = _cache.MessagingService.GetTopic(topicName);

            // Creates the topic if it doesn't exist.
            if (_topic == null)
                _topic = _cache.MessagingService.CreateTopic(topicName);

            // Subscribes to the topic.
            _topic.CreateSubscription(messageReceivedCallback);
        }

        /// <summary>
        /// This property lets the user to subscribe for the topic's deletion event.
        /// </summary>
        public TopicDeletedCallback OnTopicDeleted
        {
            set { _topic.OnTopicDeleted = value; }
        }

        /// <summary>
        /// This methods releases the resource by the instance.
        /// </summary>
        public void Dispose()
        {
            if (_topic != null) _topic.Dispose();
            if (_cache != null) _cache.Dispose();
        }
    }
}
