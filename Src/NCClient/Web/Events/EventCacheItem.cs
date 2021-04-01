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

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.JSON;
using System;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// This is a stripped down version of <see cref=" CacheItem"/>
    /// Contains basic information of an item present in the cache
    /// Will be provided in <see cref="QueryDataNotificationCallback"/> or <see cref="CacheItemRemovedCallback"/>
    /// but only when the event is registered against 
    /// </summary>
    public class EventCacheItem : ICloneable
    {
        /// <summary> The actual object provided by the client application </summary>
        private object _value;

        private CacheItemPriority _cacheItemPriority;

        //Callbacks


       
        private string _group;
        private CacheItemVersion _version;
        private EntryType _entryType;
        private string _subGroup;
        private BitSet _flagMap;
        /// <summary>
        /// CacheItemPriority of the item present in the cache
        /// </summary>
        public CacheItemPriority CacheItemPriority
        {
            get { return _cacheItemPriority; }
            internal set { _cacheItemPriority = value; }
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
        /// Group of the item
        /// </summary>
        internal string SubGroup
        {
            get { return _subGroup; }
             set { _subGroup = value; }
        }

        /// <summary>
        /// Item version of the item
        /// </summary>
        public CacheItemVersion CacheItemVersion
        {
            get { return _version; }
            internal set { _version = value; }
        }

        /// <summary>
        /// Entry type of the item.
        /// </summary>
        public EntryType EntryType
        {
            get { return _entryType; }
            internal set { _entryType = value; }
        }

        internal BitSet Flagmap
        {
            get { return _flagMap; }
            set { _flagMap = value; }
        }


        internal EventCacheItem() { }

        /// <summary>
        /// Will return the value present in the cache but only if the event was registered against
        /// <see cref="Alachisoft.NCache.Runtime.Events.EventDataFilter.Metadata"/> or <see cref="Alachisoft.NCache.Runtime.Events.EventDataFilter.None"/>
        /// otherwise it will be null.
        /// </summary>
        public T GetValue<T>()
        {
            return JsonUtil.GetValueAs<T>(_value);
        }

        internal void SetValue(object value)
        {
            _value = value;
        }

        /// <summary>
        /// Clones the Event cache item instance.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            EventCacheItem clone = new EventCacheItem();
            clone._group = _group;
            clone._version = _version;
            clone._cacheItemPriority = _cacheItemPriority;
            clone._value = _value;
            clone._entryType = _entryType;
            clone._subGroup = _subGroup;
            clone._flagMap = _flagMap;
            return clone;
        }
    }
}
