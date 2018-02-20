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

using Alachisoft.NCache.Runtime.Caching;

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// This interface contains properties and methods required for a Messaging Service. 
    /// It is implemented by MessagingService.
    /// </summary>
    public interface IMessagingService
    {
        /// <summary>
        /// Retrieves the specified topic instance.
        /// </summary>
        /// <param name="topicName"></param>
        /// <returns>Returns the topic instance, null if it does not exist.</returns>
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
        /// </code>        
        /// 
        /// </example>
        ITopic GetTopic(string topicName);

        /// <summary>
        /// Creates a topic with the specified name.
        /// </summary>
        /// <param name="topicName"></param>
        /// <returns>Returns the created topic instance</returns>
        /// <example>The following example demonstrates how to create a topic. 
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
        /// Then create topic on messagingService
        /// <code>
        /// ITopic topic=messagingService.CreateTopic("mytopic");
        /// </code>        
        /// 
        /// </example>
        ITopic CreateTopic(string topicName);

        /// <summary>
        /// Deletes the specified topic.
        /// </summary>
        /// <param name="topicName"></param>
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
    }
}