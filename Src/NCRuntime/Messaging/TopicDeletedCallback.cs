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
namespace Alachisoft.NCache.Runtime.Caching
{
    /// <summary>
    ///  In case a topic is deleted, it is a forceful deletion and deletes all messages 
    ///  and related meta-info form the cache. Hence, the subscriber and publisher must be notified of
    ///  this deletion through TopicDeleteCallback method for notifying application about topic deletion.
    /// </summary>
    /// <param name="sender">Sender's information</param>
    /// <param name="args">Event Arguments</param>
    /// <example>The following example demonstrates how to use the <see cref="TopicDeletedCallback"/> class to notify 
    /// an application about topic deletion.
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
    /// if(topic==null)  //Create the topic if it doesnt exist.
    /// {
    ///   topic=messagingService.CreateTopic("mytopic");
    /// }
    /// </code>     
    /// 
    /// Then register for topic deletion events.
    /// <code>
    ///	public void TopicDeletedCallback(object sender, TopicDeleteEventArgs args)
    ///	{
    ///		// ...
    ///	}
    /// 
    /// topic.OnTopicDeleted = TopicDeletedCallback;
    /// </code>
    /// 
    /// </example>

}