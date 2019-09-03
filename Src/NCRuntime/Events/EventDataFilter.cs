//  Copyright (c) 2018 Alachisoft
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
using System.Collections.Generic;
using System.Text;


namespace Alachisoft.NCache.Runtime.Events
{
    /// <summary>
    /// NCache provides an enum EventDataFilter to specify how much data should be retrieved from cache when a notification is raised. This is specified when user registers a notification.
    /// </summary>
    /// <remarks>
    /// Only one type of data filter can be specified at the time of registration. 
    /// </remarks>
    /// <example>
    /// To get complete data with meta data whenever a notification is raised you can use the following code:
    /// <code>
    /// cache.MessagingService.RegisterCacheNotification(key, onCacheDataModifiedCallback, EventType.ItemAdded | EventType.ItemRemoved, EventDataFilter.DataWithMetadata);
    /// </code>
    /// </example>
    public enum EventDataFilter
    {
        /// <summary>
        /// This specifies that no data or meta data is required when an event notification is raised.
        /// </summary>
        None = 0x0,

        
    }
}
