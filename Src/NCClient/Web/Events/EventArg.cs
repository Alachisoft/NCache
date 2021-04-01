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

using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Parent of <see cref="CQEventArg"/> and <see cref="CacheEventArg"/>
    /// Contains the necessary information related to the event being raised
    /// </summary>
    public abstract class EventArg
    {
        private string _cacheName;
        private EventType _eventType;
        private EventCacheItem _item; //Internal value will be null if fetch data was off, will be completely null if no data upon return was specified	
        private EventCacheItem _oldItem; //For insert only

        /// <summary>
        /// Name of the cache the event is raised against
        /// </summary>
        public string CacheName
        {
            get { return _cacheName; }
        }

        /// <summary>
        /// Event Type the event is raised against
        /// </summary>
        public EventType EventType
        {
            get { return _eventType; }
        }

      
        public EventCacheItem Item
        {
            get { return _item; }
        }

        /// <summary>
        /// Only applicable for <see cref="EventTypeInternal.ItemUpdated"/>
        /// Otherwise it will be null
        /// </summary>
        public EventCacheItem OldItem
        {
            get { return _oldItem; }
            internal set { _oldItem = value; }
        }

        internal EventArg(string cacheName, EventType eventType, EventCacheItem item)
        {
            _cacheName = cacheName;
            _eventType = eventType;
            _item = item;
        }

        internal EventArg(string cacheName, EventType eventType, EventCacheItem item, EventCacheItem oldItem)
        {
            _cacheName = cacheName;
            _eventType = eventType;
            _item = item;
            _oldItem = oldItem;
        }
    }
}
