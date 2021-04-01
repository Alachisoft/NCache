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
    /// Delivery failure notification is raised to any publisher which has registered message failure event on that topic.
    /// </summary>
    /// <param name="sender">Sender's information</param>
    /// <param name="args">Event Arguments</param>
    /// <example>The following example demonstrates how to use the <see cref="MessageDeliveryFailureCallback"/> class to notify 
    /// an application about message delivery failure. 
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
    /// if(topic==null)  //If topic not exists create it.
    /// {
    ///   topic=messagingService.CreateTopic("mytopic");
    /// }
    /// </code>     
    /// 
    /// Then register for message delivery failure events.
    /// <code>
    ///	public void MessageDeliveryFailure(object sender, MessageFailedEventArgs args)
    ///	{
    ///		// ...
    ///	}
    /// 
    /// topic.MessageDeliveryFailure += MessageDeliveryFailure;
    /// </code>
    /// 
    /// </example>
    internal delegate void MessageDeliveryFailureCallback(object sender, MessageFailedEventArgs args);
}