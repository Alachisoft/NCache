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
    /// This object is received when an even is raised and delegate <see cref=" CacheDataNotificationCallback"/> is executed
    /// CacheEventArg contains necessary information to identify the event and perform necessary actions accordingly.
    /// It inherits <see cref=" EventArg"/> which is also a parent of <see cref=" CQEventArg"/>
    /// This class is consistent for both selective and general events
    /// </summary>
    public class CacheEventArg : EventArg
    {
        private CacheItemRemovedReason _reason; //For remove only
        private CacheEventDescriptor _descriptor;

        /// <summary>
        /// Only applicable for general events otherwise it will be null
        /// </summary>
        public CacheEventDescriptor Descriptor
        {
            get { return _descriptor; }
            internal set { _descriptor = value; }
        }

        /// <summary>
        /// Only applicable for <see cref="EventType.ItemRemoved"/>
        /// </summary>
        public CacheItemRemovedReason CacheItemRemovedReason
        {
            get { return _reason; }
            internal set { _reason = value; }
        }

        internal CacheEventArg(string key, string cachename, EventType eventType, EventCacheItem item, CacheEventDescriptor discriptor)
            : base(cachename, eventType, item)
        {
            _descriptor = discriptor;
        }

        internal CacheEventArg(string key, string cachename, EventType eventType, EventCacheItem item, CacheEventDescriptor discriptor, EventCacheItem olditem)
            : base(cachename, eventType, item, olditem)
        {
            _descriptor = discriptor;
        }

        internal CacheEventArg(string key, string cachename, EventType eventType, EventCacheItem item, CacheEventDescriptor discriptor, CacheItemRemovedReason reason)
            : base(cachename, eventType, item)
        {
            _reason = reason;
            _descriptor = discriptor;
        }
    }
}
