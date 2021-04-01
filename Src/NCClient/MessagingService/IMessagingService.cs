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
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Caching.Messaging;
using Alachisoft.NCache.Runtime.Events;
using System.Collections.Generic;

namespace Alachisoft.NCache.Client.Services
{
    /// <summary>
    /// This interface contains properties and methods required for Messaging Service. 
    /// </summary>
    public interface IMessagingService
    {
        #region Topic Related 

        /// <summary>
        /// </summary>
        /// <param name="topicName">Name or pattern to identify topic.</param>
        /// <returns>Returns the topic instance, null if it does not exist.</returns>
        /// <example>The following example demonstrates how to get topic by name.
        /// First initialize cache.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// </code>
        /// Then get topic from MessagingService
        /// <code>
        /// ITopic topic = cache.MessagingService.GetTopic("mytopic");
        /// TopicSearch Option is an optional paramater which has default value of ByName 
        /// </code>        
        /// 
        /// </example>
        ITopic GetTopic(string topicName);

        /// <summary>
        /// Retrieves the specified topic instance.
        /// </summary>
        /// <param name="topicName">Name or pattern to identify topic.</param>
        /// <returns>Returns the topic instance, null if it does not exist.</returns>
        /// 
        /// <example>The following example demonstrates how to get a topic. 
        /// 
        /// First initialize cache.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// </code>
        /// 
        /// Then get messaging service from cache.
        /// <code>
        /// IMessagingService messagingService=cache.MessagingService;
        /// </code>
        /// 
        /// Then get topic from messagingService
        /// <code>
        /// ITopic topic=messagingService.GetTopic("mytopic");
        /// TopicSearch Option is an optional paramater which has default value of ByName 
        /// </code>        
        /// 
        /// </example>
        ITopic CreateTopic(string topicName);
        /// <summary>
        /// Deletes the specified topic.
        /// </summary>
        /// <param name="topicName">Name or pattern to identify topic.</param>
        /// <example>The following example demonstrates how to get a topic. 
        /// 
        /// First initialize cache.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// </code>
        /// 
        /// Then get messaging service from cache.
        /// <code>
        /// IMessagingService messagingService=cache.MessagingService;
        /// </code>
        /// 
        /// Then delete topic from messagingService
        /// <code>
        /// messagingService.DeleteTopic("mytopic");
        /// </code>        
        /// 
        /// </example>        
        void DeleteTopic(string topicName);

        #endregion

        #region Register Notification Operations



        /// <summary>
        /// Registers cache notification <see cref="EventType"/> of type Item Added, Updated or Removed against specified key in cache.
        /// </summary>
        /// <param name="key">Unique key to identify the cache item.</param>
        /// <param name="callback">The <see cref="CacheDataNotificationCallback"/> that is invoked when specified <see cref="EventType"/> is triggered against specified key in cache.</param>
        /// <param name="eventType">Tells whether the event is to be raised on Item Added, Updated or Removed.</param>
        /// <param name="eventDataFilter">Tells whether to receive metadata, data with metadata or none when a notification is triggered.</param>
        /// <example>Following example demonstrates how to register Item added, updated or removed notification against a key in cache.
        /// First create an ItemCallback
        /// <code>
        /// ItemCallback(string key, CacheEventArg cacheEventArgs)
        /// {
        ///    ...
        /// }
        /// </code>
        /// Then register the Key Notification
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// cache.MessagingService.RegisterCacheNotification(key, new CacheDataNotificationCallback(ItemCallback), EventType.ItemUpdated, EventDataFilter.DataWithMetadata);
        /// </code>
        /// </example>
        void RegisterCacheNotification(string key, CacheDataNotificationCallback callback, EventType eventType/*, EventDataFilter eventDataFilter = EventDataFilter.None*/);

        /// <summary>
        /// Registers cache notification <see cref="EventType"/> of type Item Added, Updated or Removed against specified keys in cache.
        /// </summary>
        /// <param name="keys">IEnumerable list of keys to identify the cache item.</param>
        /// <param name="callback">The <see cref="CacheDataNotificationCallback"/> that is invoked when specified <see cref="EventType"/> is triggered against specified keys in cache.</param>
        /// <param name="eventType">Tells whether the event is to be raised on Item Added, Updated or Removed.</param>
        /// <param name="eventDataFilter">Tells whether to receive metadata, data with metadata or none when a notification is triggered.</param>
        /// <remarks>
        /// Only single notification is registered against duplicate keys in <paramref name="keys"/>.
        /// </remarks>
        /// <example>Following example demonstrates how to register Item added, updated or removed notification against multiple keys in cache.
        /// First create an ItemCallback
        /// <code>
        /// ItemCallback(string key, CacheEventArg cacheEventArgs)
        /// {
        ///    ...
        /// }
        /// </code>
        /// Then register the Key Notification
        /// <code>
        /// List&lt;string&gt; keys = new List&lt;string&gt;()
        /// {
        /// 	"Product0",
        /// 	"Product1",
        /// 	"Product2"
        /// };
        /// 
        /// cache.MessagingService.RegisterCacheNotification(keys, new CacheDataNotificationCallback(ItemAdded), EventType.ItemAdded, EventDataFilter.DataWithMetadata);
        /// </code>
        /// </example>
        void RegisterCacheNotification(IEnumerable<string> keys, CacheDataNotificationCallback callback, EventType eventType/*, EventDataFilter eventDataFilter = EventDataFilter.None*/);


        #endregion

        #region Un-Register Notification Operations


        /// <summary>
        /// Unregisters the <see cref="CacheItemUpdatedCallback"/> already registered for the specified key.
        /// </summary>
        /// <param name="key">Unique key to identify the cache item.</param>
        /// <param name="callback">The <see cref="CacheDataNotificationCallback"/> that is invoked when specified <see cref="EventType"/> is triggered in cache.</param>
        /// <param name="EventType">Tells whether the event is to be raised on Item Added, Updated or Removed.</param>
        /// <example>Example registers and unregisters update and remove notification against a key
        /// /// First create an ItemCallback
        /// <code>
        /// ItemCallback(string key, CacheEventArg cacheEventArgs)
        /// {
        ///    ...
        /// }
        /// </code>
        /// Then register the Key Notification
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// cache.MessagingService.RegisterCacheNotification(key, new CacheDataNotificationCallback(ItemCallback), EventType.ItemUpdated, EventDataFilter.DataWithMetadata);
        /// </code>
        /// Now, Unregister this event.
        /// <code>
        /// cache.MessagingService.UnRegisterCacheNotification(key, new CacheDataNotificationCallback(ItemCallback), EventType.ItemUpdated);
        /// </code>
        /// </example>
        void UnRegisterCacheNotification(string key, CacheDataNotificationCallback callback, EventType EventType);

        /// <summary>
        /// Unregisters cache notification against specified keys in cache.
        /// </summary>
        /// <param name="keys">IEnumerable list of keys to identify the cache item.</param>
        /// <param name="callback">The <see cref="CacheDataNotificationCallback"/> that is invoked when specified <see cref="EventType"/> is triggered against specified keys in cache.</param>
        /// <param name="EventType">Tells whether the event is to be raised on Item Added, Updated or Removed.</param>
        /// <example>Following example demonstrates how to unregister Item added, updated or removed notification against multiple keys in cache.
        /// First create an ItemCallback
        /// <code>
        /// ItemCallback(string key, CacheEventArg cacheEventArgs)
        /// {
        ///    ...
        /// }
        /// </code>
        /// Then register the Key Notification
        /// <code>
        /// List&lt;string&gt; keys = new List&lt;string&gt;()
        /// {
        /// 	"Product0",
        /// 	"Product1",
        /// 	"Product2"
        /// };
        /// 
        /// cache.MessagingService.RegisterCacheNotification(keys, new CacheDataNotificationCallback(ItemAdded), EventType.ItemAdded, EventDataFilter.DataWithMetadata);
        /// </code>
        /// Now, Unregister this event.
        /// <code>
        /// cache.MessagingService.UnRegisterCacheNotification(keys, new CacheDataNotificationCallback(ItemAdded), EventType.ItemAdded);
        /// </code>
        /// </example>
        void UnRegisterCacheNotification(IEnumerable<string> keys, CacheDataNotificationCallback callback, EventType EventType);

        #endregion
    }
}
