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
using Alachisoft.NCache.Runtime.Caching.Messaging;
using System;

namespace Alachisoft.NCache.Runtime.Caching
{
    /// <summary>
    /// The ITopic interface facilitates creating subscription and publishing of messages against the topic. 
    /// This also provides event registrations for message delivery failure, receiving messages and deleting topics.
    /// </summary>
    public interface ITopic : IDisposable
    {
        /// <summary>
        /// Topic's name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Number of messages published for this topic.
        /// </summary>
        /// <remarks>
        /// This property returns value for a specific topic. Count of other topics play no role.
        /// </remarks>
        long MessageCount { get; }

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
        /// This method is used to register against topic on cache if topic exists, otherwise throw exception.
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
        /// This method is used to Publish the message in the cache with specified DeliveryOption. 
        /// And the option to notify the publisher if the message has failed to deliver because of expiration, eviction or internal system issue.
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
