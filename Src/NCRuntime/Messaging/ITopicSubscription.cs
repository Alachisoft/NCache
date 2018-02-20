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


namespace Alachisoft.NCache.Runtime.Caching
{
    /// <summary>
    /// This interface contains properties and methods of created subscription. 
    /// It is implemented by TopicSubscription.
    /// </summary>
    public interface ITopicSubscription
    {
        /// <summary>
        /// Topic of subscription.  
        /// </summary>
        ITopic Topic { get; }

        /// <summary>
        /// Unsubscribes topic. 
        /// </summary>
        /// <remarks>
        /// You can use this method to unsbscribe the subscription created on a topic.
        /// For more information on how to create a subscription see the documentation for <see cref="ITopic"/>.
        /// </remarks>
        void UnSubscribe();

        /// <summary>
        /// Message is delivered through this callback.
        /// </summary>
        event MessageReceivedCallback OnMessageRecieved;
    }
}
