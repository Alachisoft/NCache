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


namespace Alachisoft.NCache.Runtime.Events
{
    /// <summary>
    /// NCache provides an EventType enum which specifies the type of event to be registered by the user. Event types are specified at the time of notification registration.
    /// </summary>
    /// <remarks>
    /// One or more event types can be specified. 
    /// </remarks>
    /// <example>
    /// To specify multiple events at the time of event registration you can use the following code:
    /// <code>
    /// cache.MessagingService.RegisterCacheNotification(key, onCacheDataModifiedCallback, EventType.ItemAdded | EventType.ItemRemoved, EventDataFilter.DataWithMetadata);
    /// </code>
    /// </example>
    [Serializable]
    [Flags]
    public enum EventType
    {
        /// <summary>
        /// User receives a notification when an item is added in cache.
        /// </summary>
        ItemAdded = 0x001,
        /// <summary>
        ///  when an item is updated in cache.
        /// </summary>
        ItemUpdated = 0x002,
        /// <summary>
        /// User receives a notification when an item is removed from cache
        /// </summary>
        ItemRemoved = 0x004,
    }
}
