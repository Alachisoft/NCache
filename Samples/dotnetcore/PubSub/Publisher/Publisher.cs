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
    public class Publisher : IDisposable
    {
        private ITopic _topic;
        private Cache _cache;
        private bool _isStarted;

        /// <summary>
        /// Event subscriber/unsubscriber for the event of message delivery failure.  
        /// </summary>
        public event MessageDeliveryFailureCallback MessageDeliveryFailure
        {
            add
            {
                if (!_isStarted) throw new Exception("Publisher is not started.");
                _topic.MessageDeliveryFailure += value ;
            }
            remove
            {
                if (!_isStarted) throw new Exception("Publisher is not started.");
                _topic.MessageDeliveryFailure -= value;
            }
        }

        /// <summary>
        /// This methods starts the specified cache and creates the specified topic on it.
        /// </summary>
        /// <param name="cacheName">The cache's name.</param>
        /// <param name="topicName">The topic's name.</param>
        public void Start(string cacheName, string topicName)
        {
            // Initialize cache.
            _cache = Web.Caching.NCache.InitializeCache(cacheName);

            // Create messaging topic.
            _topic = _cache.MessagingService.CreateTopic(topicName); 

            // Marking it as started.
            _isStarted = true;
        }

        /// <summary>
        /// This method publishes a message, which is created by the provided arguments. 
        /// </summary>
        /// <param name="payload">The payload for the message.</param>
        /// <param name="timeSpan">Optional and nullable message expiry time.</param>
        public void Publish(object payload, TimeSpan? timeSpan = default(TimeSpan?))
        {
            if (payload == null) throw new ArgumentNullException("payload");
            if (!_isStarted) throw new Exception("Publisher is not started.");

            // Creating new message instance with the specified expiration TimeSpan.
            Message message = new Message(payload, timeSpan);

            // Publishing the above created message.
            _topic.Publish(message, DeliveryOption.All, true);
        }

        /// <summary>
        /// This method deletes the specified 'Topic' in the argumnet.
        /// </summary>
        /// <param name="topicName">Name of the topic to be deleted.</param>
        public void DeleteTopic(string topicName)
        {
            if (topicName == null) throw new ArgumentNullException("topicName");
            if (!_isStarted) throw new Exception("Publisher is not started.");

            // Deleting the topic.
            _cache.MessagingService.DeleteTopic(topicName);
        }

        /// <summary>
        /// This methods releases the resource by the instance.
        /// </summary>
        public void Dispose()
        {
            _isStarted = false;

            //Disposing the acquired resources.
            if (_topic != null) _topic.Dispose();
            if (_cache != null) _cache.Dispose();
        }
    }
}
