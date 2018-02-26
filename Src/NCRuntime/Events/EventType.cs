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
namespace Alachisoft.NCache.Runtime.Events
{
    /// <summary>
    /// Type of event
    /// </summary>
    [Serializable]
    [Flags]
    public enum EventType
    {
        /// <summary>
        /// When an item is added to cache
        /// </summary>
        ItemAdded = 0x001,
        /// <summary>
        /// When an item is updated in cache
        /// </summary>
        ItemUpdated = 0x002,
        /// <summary>
        /// When an item is removed from cache
        /// </summary>
        ItemRemoved = 0x004,

        /// For pub-sub Poll based notfication. THis event type is internal.
        /// </summary>
        PubSub = 0x10
    }
}
