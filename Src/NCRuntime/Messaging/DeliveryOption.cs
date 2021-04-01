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
    /// NCache provides a DeliveryOption enum which specifies how the message should be delivered to any registered subscribers. 
    /// The delivery option is specified during message publishing phase. It contains the following options:
    /// </summary>
    public enum DeliveryOption: byte
    {
        /// <summary>
        /// Delivers message to all registered subscribers, if no subscriber has been registered,
        /// it will return without any failure acknowledgment. Message 
        /// will be sent to any subscriber when it registers on topic; unless message expiration has not occurred.
        /// </summary>
        All,
        /// <summary>
        /// Delivers message to any one of the registered subscribers. 
        /// If acknowledgement is not received, the message is reassigned to another subscriber 
        /// till it reaches its expiration time limit.
        /// </summary>
        Any
    }
}
