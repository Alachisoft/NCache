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
using System;

namespace Alachisoft.NCache.Runtime.Caching
{
    /// <summary>
    /// A message contains the actual data object which is sent by the publisher and delivered to the 
    /// interested subscribers for the topic. NCache provides the Message class which implements the IMessage 
    /// interface to create a message.
    /// </summary>

    public interface IMessage
    {
        /// <summary>
        /// Auto generated ID for the message, as same messages can be stored on different topics.
        /// </summary>
        string MessageId { get; }
        /// <summary>
        /// 	ExpirationTime of TimeSpan type after which the message is expired from the topic. 
        /// 	This can also accept null value, which will ensure that the message is not expired from the topic. 
        /// 	In case of no expiration time specified, null is considered as default.
        /// </summary>
        TimeSpan? ExpirationTime { get; set; }
        /// <summary>
        /// The actual data object of interest for subscribers, for example, Order.
        /// </summary>
        object Payload { get;  }
        /// <summary>
        /// Creation time in DateTime for the message.
        /// </summary>
        DateTime CreationTime { get; }

    }
}
