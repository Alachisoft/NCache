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
    /// NCache provides ITopicSubscription interface which is returned against the desired topic, 
    /// containing information for topic subscriptions.
    /// </summary>
    public interface ITopicSubscription
    {
        /// <summary>
        /// ITopic instance containing information about the topic. 
        /// </summary>
        /// <example>
        /// Example prints topic name of the subscription
        /// <code>
        /// ITopicSubscription topicSubscription = _topic.CreateSubscription(messageReceivedCallback);
        ///
        /// Console.WriteLine("Topic Name : {0}", topicSubscription.Topic.Name);
        /// </code>
        /// </example>
        ITopic Topic { get; }

        /// <summary>
        /// Unsubscribes topic. 
        /// </summary>
        /// <remarks>
        /// You can use this method to allow the subscriber to unregister from the topic, i.e., 
        /// the subscriber will not receive any messages in the future.
        /// </remarks>
        /// <example>
        /// Example unsubscribes the subscription
        /// <code>
        /// ITopicSubscription topicSubscription = _topic.CreateSubscription(messageReceivedCallback);
        ///
        /// topicSubscription.UnSubscribe();
        /// </code>
        /// </example>
        void UnSubscribe();

        /// <summary>
        /// Event to register subscriber against the topic so that it can receive the published messages.
        /// </summary>
        /// <example>
        /// Example registers message received callback on topic subscription
        /// <code>
        /// topicSubscription.OnMessageRecieved += messageReceivedCallback;
        /// </code>
        /// </example>
        event MessageReceivedCallback OnMessageRecieved;
    }
}
