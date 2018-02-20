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
// limitations under the License

using System;

namespace Alachisoft.NCache.Runtime.Caching
{
    /// <summary>
    /// Contains properties and methods required for a topic. 
    /// It is implemented by Topic.
    /// </summary>
    public interface ITopic: IDisposable
    {
        /// <summary>
        /// Topic's name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Default Expiry time of messsage for this topic.
        /// Its default value is TimeSpan.MaxValue.
        /// </summary>
        TimeSpan ExpirationTime { get; set; }

        /// <summary>
        /// Is topic closed or not?
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Subscribe for Message delivery failure events
        /// </summary>
        /// <remarks>
        /// You can use this event to perform the tasks when message delivery fails.
        /// For more information on how to use this callback see the documentation for <see cref="MessageDeliveryFailureCallback"/>.
        /// </remarks>
        event MessageDeliveryFailureCallback MessageDeliveryFailure;

        /// <summary>
        /// Topic deletion events are delivered through this callback.
        /// </summary>
        /// <remarks>
        /// You can use this event to perform the tasks when a topic is deleted.
        /// For more information on how to use this callback see the documentation for <see cref="TopicDeletedCallback"/>.
        /// </remarks>
        TopicDeletedCallback OnTopicDeleted { set; }

        /// <summary>
        /// Subscribes for receiving messages of a particular topic.
        /// </summary>
        /// <param name="messageReceivedCallback"> Message is delivered through this callback </param> 
        /// <returns>Returns the created topic subscription</returns>
        /// <example>The following example demonstrates how to create a subscription on topic. 
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
        /// 
        /// <code>
        /// ITopic topic=messagingService.GetTopic("mytopic");  
        /// if(topic==null)  //If topic not exists create it.
        /// {
        ///   topic=messagingService.CreateTopic("mytopic");
        /// }
        /// </code>     
        /// 
        /// Then create subscription
        /// <code>
        /// void MessageReceivedCallback(object sender, MessageEventArgs args)
        /// {
        ///   
        /// }
        /// 
        /// topic.CreateSubscription(MessageReceivedCallback);
        /// </code>
        /// </example>
        ITopicSubscription CreateSubscription(MessageReceivedCallback messageReceivedCallback);

        /// <summary>
        /// Method used to publish message on topic
        /// </summary>
        /// <param name="message">Message to be published</param>
        /// <param name="deliverOption"> Delivery Option </param>
        /// <param name="notifyDeliveryFailure">Is MessageDeliveryFailure event required for this message</param>
        /// <example>The following example demonstrates how to publish message on a topic. 
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
        /// Then publish message on topic.
        /// <code>
        /// object payload = "mymessage"; 
        /// Message message = new Message(payload);   //creating message
        /// topic.Publish(message, DeliveryOption.All);
        /// </code>
        /// </example>
        void Publish(Message message, DeliveryOption deliverOption, bool notifyDeliveryFailure = false);
    }
}
