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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching.Queries;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
    /// internal class and must not be used from outside
    /// </summary>
    class InprocCacheEventsListener : MarshalByRefObject, IDisposable
    {
        /// <summary> Underlying implementation of NCache. </summary>
        private CacheEventsListenerBase _listener;


        /// <summary> Underlying implementation of NCache. </summary>
        private Alachisoft.NCache.Caching.Cache _nCache;

        private ItemAddedCallback _itemAdded;
        private ItemUpdatedCallback _itemUpdated;
        private ItemRemovedCallback _itemRemoved;
        private Alachisoft.NCache.Caching.CacheClearedCallback _cacheCleared;
        private Alachisoft.NCache.Caching.CustomNotificationCallback _customNotification;
        private CustomRemoveCallback _customRemoveCallback;
        private CustomUpdateCallback _customUpdateCallback;
        private Alachisoft.NCache.Caching.CacheStoppedCallback _cacheStopped;
        private string _serializationContext;
        private PollRequestCallback _pollRequested;

        private ActiveQueryCallback _queryChanged;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="nCache"></param>
        internal InprocCacheEventsListener(CacheEventsListenerBase parent, Alachisoft.NCache.Caching.Cache nCache)
        {
            _listener = parent;
            _nCache = nCache;


            _itemAdded = new ItemAddedCallback(this.OnItemAdded);
            _itemUpdated = new ItemUpdatedCallback(this.OnItemUpdated);
            _itemRemoved = new ItemRemovedCallback(this.OnItemRemoved);
            _cacheCleared = new Alachisoft.NCache.Caching.CacheClearedCallback(this.OnCacheCleared);
            _customNotification =
                new Alachisoft.NCache.Caching.CustomNotificationCallback(this.OnCustomNotification);
            _cacheStopped = new Alachisoft.NCache.Caching.CacheStoppedCallback(this.OnCacheStopped);
            _queryChanged = new ActiveQueryCallback(this.OnActiveQueryChanged);
            _pollRequested = OnPollNotified;

            _nCache.ItemAdded += _itemAdded;
            _nCache.ItemUpdated += _itemUpdated;
            _nCache.ItemRemoved += _itemRemoved;
            _nCache.CacheCleared += _cacheCleared;
            _nCache.CustomNotif += _customNotification;
            _nCache.CacheStopped += _cacheStopped;


            _customRemoveCallback = new CustomRemoveCallback(this.OnCustomRemoveCallback);
            _customUpdateCallback = new CustomUpdateCallback(this.OnCustomUpdateCallback);


            _nCache.CustomRemoveCallbackNotif += _customRemoveCallback;
            _nCache.CustomUpdateCallbackNotif += _customUpdateCallback;

            _nCache.ActiveQueryCallbackNotif += _queryChanged;
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public void Dispose()
        {
            try
            {
                _nCache.ItemAdded -= _itemAdded;
                _nCache.ItemUpdated -= _itemUpdated;
                _nCache.ItemRemoved -= _itemRemoved;
                _nCache.CacheCleared -= _cacheCleared;
                _nCache.CustomNotif -= _customNotification;
                _nCache.CacheStopped -= _cacheStopped;

                _nCache.CustomRemoveCallbackNotif -= _customRemoveCallback;
                _nCache.CustomUpdateCallbackNotif -= _customUpdateCallback;
            }
            catch
            {
            }
        }

        #endregion


        public void OnItemAdded(object key, EventContext eventContext)
        {
            try
            {
                BitSet flag = new BitSet(0);
                EventCacheItem item = EventUtil.ConvertToItem(eventContext.Item);

                if (_listener != null)
                    _listener.OnItemAdded((string)key, true, item, flag);
            }
            catch
            {
            }
        }

        public void OnPollNotified(string clientId, short callbackId, EventType eventType)
        {
            if (_listener != null)
                _listener.OnPollNotified(callbackId, eventType);
        }

        public void OnItemUpdated(object key, EventContext eventContext)
        {
            EventCacheItem oldItem = null;
            EventCacheItem item = null;
            try
            {
                BitSet flag = new BitSet(0);
                if (eventContext != null)
                {
                    if (eventContext.OldItem != null)
                        oldItem = EventUtil.ConvertToItem(eventContext.OldItem);
                    if (eventContext.Item != null)
                        item = EventUtil.ConvertToItem(eventContext.Item);

                    if (_listener != null)
                        _listener.OnItemUpdated((string)key, true, item, oldItem, flag);
                }
            }
            catch
            {
            }
        }

        public void OnCacheCleared(EventContext eventContext)
        {
            try
            {
                if (_listener != null) _listener.OnCacheCleared(true);
            }
            catch
            {
            }
        }

        public void OnItemRemoved(object key, object value, ItemRemoveReason reason, BitSet Flag,
            EventContext eventContext)
        {
            EventCacheItem item = null;
            try
            {
                BitSet flag = new BitSet(0);

                if (eventContext.Item != null)
                    item = EventUtil.ConvertToItem(eventContext.Item);

                if (_listener != null)
                    _listener.OnItemRemoved((string)key,
                        value,
                        reason,
                        Flag, true, item);
            }
            catch
            {
            }
        }

        public void OnCustomNotification(object notifId, object data, EventContext eventContext)
        {
            try
            {
                if (_listener != null)
                    _listener.OnCustomNotification(notifId, data, true);
            }
            catch
            {
            }
        }

        public void OnCacheStopped(string cacheId, EventContext eventContext)
        {
            _listener.OnCacheStopped(cacheId, true);
        }

        public void OnActiveQueryChanged(object key, Alachisoft.NCache.Caching.Queries.QueryChangeType changeType,
            List<CQCallbackInfo> activeQueries, EventContext eventContext)
        {
            try
            {
                if (_listener != null)
                {
                    foreach (CQCallbackInfo queryInfo in activeQueries)
                    {
                        EventDataFilter datafilter = EventDataFilter.None;
                        foreach (EventDataFilter df in queryInfo.DataFilters.Values)
                        {
                            datafilter = df;
                            break;
                        }

                        _listener.OnActiveQueryChanged(queryInfo.CQId,
                            (QueryChangeType)Convert.ToInt32(changeType), (string)key, true, null, null, null,
                            datafilter);
                    }
                }
            }
            catch
            {
            }
        }


        public void OnCustomRemoveCallback(object key, object value, ItemRemoveReason reason, BitSet Flag,
            EventContext eventContext)
        {
            EventCacheItem item = EventUtil.ConvertToItem(eventContext.Item);
            CacheItemRemovedReason removeReason = EventUtil.ConvertToCIRemoveReason(reason);

            _listener.OnCustomRemoveCallback((string)key, value, removeReason, Flag, true, item);
        }

        public void OnCustomUpdateCallback(object key, object value, EventContext eventContext)
        {
            EventCacheItem item = EventUtil.ConvertToItem(eventContext.Item);
            EventCacheItem oldItem = EventUtil.ConvertToItem(eventContext.OldItem);
            _listener.OnCustomUpdateCallback((string)key, value, true, item, oldItem, null);
        }
    }
}
