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
    /// Instance of this class holds the link to the registered delegate
    /// Keep it safe and use it to unregister the registered delegate when required.
    /// The bool <see cref=" IsRegistered"/> returns false when the descriptor has been consumed to unregister the delegate.
    /// Then this instance can then be disposed of. Upon re-registering for the interested event, a new descriptor will be created.
    /// </summary>
    public sealed class CacheEventDescriptor
    {
        internal CacheEventDescriptor() { }

        private EventType _eventType;
        private EventHandle _handle = null;
        private string _cacheName;
        private bool _isRegistered;
        private CacheDataNotificationCallback _cacheDataNotificationCallback;
        private EventDataFilter _datafilter;

        private object _syncblk = new object();

        //Creation is CacheDesriptor's responsibility
        static internal CacheEventDescriptor CreateCacheDiscriptor(EventType eventType, string cacheName, CacheDataNotificationCallback callback, EventDataFilter datafilter)
        {
            CacheEventDescriptor descriptor = new CacheEventDescriptor();
            descriptor.RegisteredAgainst = eventType;
            descriptor.CacheName = cacheName;
            descriptor.CacheDataNotificationCallback = callback;
            descriptor.IsRegistered = true;
            descriptor.DataFilter = datafilter;
            return descriptor;

        }

        /// <summary>
        /// Sets data filter.
        /// </summary>
        public EventDataFilter DataFilter
        {
            get { return _datafilter; }
            internal set { _datafilter = value; }
        }

        /// <summary>
        /// Returns true if the linked event delegate is registered, returns false when the descriptor has been consumed
        /// This property is ThreadSafe
        /// </summary>
        public bool IsRegistered
        {
            get { lock (_syncblk) return _isRegistered; }
            internal set { lock (_syncblk) _isRegistered = value; }
        }

        /// <summary>
        /// Name of the cache registered against
        /// </summary>
	    public string CacheName
        {
            get { return _cacheName; }
            internal set { _cacheName = value; }
        }

        /// <summary>
        /// Event Types registered against. Can be ORed to check registration types
        /// </summary>
        public EventType RegisteredAgainst
        {
            get { return _eventType; }
            set { _eventType = value; }
        }

        /// <summary>
        /// Delegate that triggers when any data change in cache.
        /// </summary>
        public CacheDataNotificationCallback CacheDataNotificationCallback
        {
            get { return _cacheDataNotificationCallback; }
            internal set { _cacheDataNotificationCallback = value; }
        }


        /// <summary>
        /// Cannot reset once created
        /// </summary>
        internal EventHandle Handle
        {
            get { return _handle; }
            set
            {
                if (_handle == null)
                {
                    IsRegistered = true;
                    _handle = value;
                }
            }
        }

        //TODO: Need to write overloads for equality and stuff
    }
}
