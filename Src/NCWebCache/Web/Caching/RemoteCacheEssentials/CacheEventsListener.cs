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
using Alachisoft.NCache.Runtime.Caching;
using Exception = System.Exception;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Providers handlers for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
    /// internal class and must not be used from outside
    /// </summary>
    public class CacheEventsListener : MarshalByRefObject, IDisposable
    {
        private CacheEventsListenerBase _listener;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        internal CacheEventsListener(CacheEventsListenerBase parent)
        {
            _listener = parent;
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
        }

        #endregion


        public void OnItemAdded(string key, bool notifyAsync, EventCacheItem item, BitSet flag)
        {
            try
            {
                if (_listener != null)
                    _listener.OnItemAdded(key, notifyAsync, item, flag);
            }
            catch
            {
            }
        }


        public void OnItemUpdated(string key, bool notifyAsync, EventCacheItem item, EventCacheItem oldItem,
            BitSet flag)
        {
            try
            {
                if (_listener != null)
                    _listener.OnItemUpdated(key, notifyAsync, item, oldItem, flag);
            }
            catch
            {
            }
        }

        public void OnPollNotified(short callbackId, EventType eventType)
        {
            try
            {
                if (_listener != null)
                    _listener.OnPollNotified(callbackId, eventType);
            }
            catch
            {
            }
        }

        public void OnItemRemoved(string key, object value, CacheItemRemovedReason reason, BitSet flag,
            bool notifyAsync, EventCacheItem item)
        {
            try
            {
                if (_listener != null)
                    _listener.OnItemRemoved(key, value, reason, flag, notifyAsync, item);
            }
            catch (Exception e)
            {
            }
        }

        public void OnCustomNotification(object notifId, object data, bool notifyAsync)
        {
            try
            {
                if (_listener != null)
                    _listener.OnCustomNotification(notifId, data, notifyAsync);
            }
            catch
            {
            }
        }


        public void OnCacheCleared(bool notifyAsync)
        {
            try
            {
                if (_listener != null)
                    _listener.OnCacheCleared(notifyAsync);
            }
            catch
            {
            }
        }

        public void OnCustomRemoveCallback(short callbackId, string key, object value,
            CacheItemRemovedReason reason, BitSet Flag, bool notifyAsync, EventCacheItem item,
            EventDataFilter dataFilter)
        {
            object[] val = new object[] { value, new CallbackInfo(null, callbackId, dataFilter) };
            _listener.OnCustomRemoveCallback(key, val, reason, Flag, notifyAsync, item);
        }

        public void OnCustomUpdateCallback(short callbackId, string key, bool notifyAsync, EventCacheItem item,
            EventCacheItem oldItem, BitSet flag, EventDataFilter dataFilter)
        {
            CallbackInfo cbInfo = new CallbackInfo(null, callbackId, dataFilter);
            _listener.OnCustomUpdateCallback(key, cbInfo, notifyAsync, item, oldItem, flag);
        }

        public void OnCacheStopped(string cacheId, bool notifyAsync)
        {
            try
            {
                if (_listener != null) _listener.OnCacheStopped(cacheId, notifyAsync);
            }
            catch
            {
            }
        }


        public void OnActiveQueryChanged(string queryId, QueryChangeType changeType, string key, bool notifyAsync,
            EventCacheItem item, EventCacheItem oldItem, BitSet flag, EventDataFilter datafilter)
        {
            try
            {
                if (_listener != null)
                    _listener.OnActiveQueryChanged(queryId, changeType, key, notifyAsync, item, oldItem, flag,
                        datafilter);
            }
            catch
            {
            }
        }

        public void OnTaskCompletedCallback(string taskId, short taskStatus, string taskFailureReason,
            short callbackId)
        {
            try
            {
                if (_listener != null)
                    _listener.OnTaskCompletedCallback(taskId, taskStatus, taskFailureReason, callbackId);
            }
            catch
            {
            }
        }

        public virtual void OnClientConnectivityChange(string cacheId, Runtime.Caching.ClientInfo clientInfo,
            ConnectivityStatus status)
        {
            if (_listener != null)
            {
                _listener.OnClientConnectivityChange(cacheId, clientInfo, status);
            }
        }


        public void OnReregisterTopic()
        {
            try
            {
                if (_listener != null) _listener.OnReregisterTopic();
            }
            catch
            {
            }
        }
    }
}
