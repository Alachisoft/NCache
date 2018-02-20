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
// limitations under the License.

using System;
using Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// This is a stripped down version of <see cref=" CacheItem"/>
    /// Contains basic information of an item present in the cache
    /// Will be provided in <see cref="QueryDataNotificationCallback"/> or <see cref="CacheItemRemovedCallback"/>
    /// but only when the event is registered against <see cref="EventDataFilter.Metadata"/> or <see cref="EventDataFilter.DataWithMetadata"/>
    /// </summary>
    public class EventCacheItem : ICloneable
    {
        /// <summary> The actual object provided by the client application </summary>
        private object _value;

        private CacheItemPriority _cacheItemPriority;

        //Callbacks

        private bool _resyncExpiredItems;
        private string _resyncProviderName;
        private string _group;
        private string _subGroup;
        private CacheItemVersion _version;

        /// <summary>
        /// Will contain the value present in the cache but only if the event was registered against
        /// <see cref="EventDataFilter.Metadata"/> or <see cref="EventDataFilter.DataWithMetadata"/>
        /// otherwise it will be null
        /// </summary>
        public object Value
        {
            get { return _value; }
            internal set { _value = value; }
        }

        /// <summary>
        /// CacheItemPriority of the item present in the cache
        /// </summary>
        public CacheItemPriority CacheItemPriority
        {
            get { return _cacheItemPriority; }
            internal set { _cacheItemPriority = value; }
        }

        /// <summary>
        /// If items are to be ReSynced at expiry
        /// </summary>
        public bool ResyncExpiredItems
        {
            get { return _resyncExpiredItems; }
            internal set { _resyncExpiredItems = value; }
        }

        /// <summary>
        /// Readthrough provider name when item will be resynced at expiry
        /// </summary>
        public string ResyncProviderName
        {
            get { return _resyncProviderName; }
            internal set { _resyncProviderName = value; }
        }

        /// <summary>
        /// Group of the item
        /// </summary>
        public string Group
        {
            get { return _group; }
            internal set { _group = value; }
        }

        /// <summary>
        /// SubGroup of the item
        /// </summary>
        public string SubGroup
        {
            get { return _subGroup; }
            internal set { _subGroup = value; }
        }


        /// <summary>
        /// Item version of the item
        /// </summary>
        public CacheItemVersion CacheItemVersion
        {
            get { return _version; }
            internal set { _version = value; }
        }


        internal EventCacheItem()
        {
        }


        public object Clone()
        {
            EventCacheItem clone = new EventCacheItem();
            clone._group = _group;
            clone._subGroup = _subGroup;
            clone._version = _version;
            clone._resyncExpiredItems = _resyncExpiredItems;
            clone._resyncProviderName = _resyncProviderName;
            clone._cacheItemPriority = _cacheItemPriority;
            clone._value = _value;

            return clone;
        }
    }
}