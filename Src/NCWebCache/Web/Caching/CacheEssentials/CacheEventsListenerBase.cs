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
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Caching;
#if NETCORE
using System.Threading.Tasks;
#endif

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
    /// internal class and must not be used from outside
    /// </summary>
    internal class CacheEventsListenerBase : MarshalByRefObject, IDisposable
    {
        /// <summary> Underlying implementation of NCache. </summary>
        private Cache _parent;

        private EventManager _eventManager;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        internal CacheEventsListenerBase(Cache parent, EventManager eventManager)
        {
            _parent = parent;
            _eventManager = eventManager;
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public virtual void Dispose()
        {
            try
            {
            }
            catch
            {
            }
        }

        #endregion

        public virtual void OnCacheStopped(string cacheId, bool notifyAsync)
        {
            try
            {
                if (_parent.CacheStoppedEventHandle != null)
                {
                    Delegate[] list = _parent.CacheStoppedEventHandle.GetInvocationList();
                    CacheStoppedCallback cb;
                    for (int i = 0; i < list.Length; i++)
                    {
                        cb = list[i] as CacheStoppedCallback;

                        if (notifyAsync)
                        {
#if !NETCORE
                            cb.BeginInvoke(cacheId, null, null);
#elif NETCORE
                                TaskFactory factory = new TaskFactory();
                                Task task = factory.StartNew(() => cb(cacheId));
#endif
                        }
                        else
                            cb(cacheId);
                    }
                }
            }
            catch
            {
            }
        }

        public virtual void OnCustomRemoveCallback(string key, object value, ItemRemoveReason reason, BitSet Flag,
            bool notifyAsync)
        {
            try
            {
                object[] args = value as object[];
                if (args != null)
                {
                    object val = args[0];

                    if (value is UserBinaryObject)
                        value = ((UserBinaryObject)value).GetFullObject();

                    CallbackInfo cbInfo = args[1] as CallbackInfo;
                    if (cbInfo != null)
                    {
                        val = _parent.SafeDeserialize(val, _parent._serializationContext, Flag);
                        int processid = System.Diagnostics.Process.GetCurrentProcess().Id;

                        CacheItemRemovedCallback cb =
                            (CacheItemRemovedCallback)_parent.CallbackIDsMap.GetResource(cbInfo.Callback);
                        if (cb != null)
                        {
                            if (notifyAsync)
                            {
#if !NETCORE
                                cb.BeginInvoke(key, val, WebCacheHelper.GetWebItemRemovedReason(reason), null,
                                    null);
#elif NETCORE
                                    TaskFactory factory = new TaskFactory();
                                    Task task = factory.StartNew(() => cb(key, val, WebCacheHelper.GetWebItemRemovedReason(reason)));
#endif
                            }
                            else
                            {
                                cb(key, value, WebCacheHelper.GetWebItemRemovedReason(reason));
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public virtual void OnCustomRemoveCallback(string key, object value, CacheItemRemovedReason reason,
            BitSet flag, bool notifyAsync, EventCacheItem item)
        {
            try
            {
                object[] args = value as object[];
                if (args != null)
                {
                    object val = args[0];


                    CallbackInfo cbInfo = args[1] as CallbackInfo;
                    if (cbInfo != null)
                    {
                        if (_parent._perfStatsCollector != null)
                            _parent._perfStatsCollector.IncrementEventsProcessedPerSec();

                        if (item != null) item.Value = GetObject(item.Value, flag);
                        EventHandle handle = new EventHandle((short)cbInfo.Callback);
                        _parent.EventManager.RaiseSelectiveCacheNotification(key, EventType.ItemRemoved, item, null,
                            reason, notifyAsync, handle, cbInfo.DataFilter);
                    }
                }
            }
            catch
            {
            }
        }

        public virtual void OnCacheCleared(bool notifyAsync)
        {
            try
            {
                if (_parent.CallbackIDsMap != null)
                    _parent.CallbackIDsMap.RemoveAllResources();

                if (_parent.CallbacksMap != null)
                    _parent.CallbacksMap.RemoveAllResources();

                if (_parent.CacheClearedEventHandle != null)
                {
                    Delegate[] list = _parent.CacheClearedEventHandle.GetInvocationList();
                    CacheClearedCallback cb;
                    for (int i = 0; i < list.Length; i++)
                    {
                        cb = list[i] as CacheClearedCallback;

                        if (notifyAsync)
                        {
#if !NETCORE
                            cb.BeginInvoke(null, null);
#elif NETCORE
                                Task task = Task.Factory.StartNew(()=> cb());
#endif
                        }
                        else
                            cb();

                        if (_parent._perfStatsCollector != null)
                            _parent._perfStatsCollector.IncrementEventsProcessedPerSec();
                    }
                }
            }
            catch
            {
            }
        }

        public virtual void OnCustomUpdateCallback(string key, object value, bool notifyAsync, EventCacheItem item,
            EventCacheItem oldItem, BitSet flag)
        {
            try
            {
                CallbackInfo cbInfo = value as CallbackInfo;
                if (cbInfo != null)
                {
                    if (item != null) item.Value = GetObject(item.Value, flag);
                    if (oldItem != null) oldItem.Value = GetObject(oldItem.Value, flag);

                    if (_parent._perfStatsCollector != null)
                        _parent._perfStatsCollector.IncrementEventsProcessedPerSec();

                    EventHandle handle = new EventHandle((short)cbInfo.Callback);
                    this._eventManager.RaiseSelectiveCacheNotification(key, EventType.ItemUpdated, item, oldItem,
                        CacheItemRemovedReason.Underused, notifyAsync, handle, cbInfo.DataFilter);
                }
            }
            catch
            {
            }
        }


        public virtual void OnItemAdded(object key, bool notifyAsync)
        {
            try
            {
                String keyString = key as string;
                if (key != null)
                {
                    if (_parent.ItemAddedEventHandle != null)
                    {
                        try
                        {
                            Delegate[] dltList = _parent.ItemAddedEventHandle.GetInvocationList();
                            for (int i = dltList.Length - 1; i >= 0; i--)
                            {
                                CacheItemAddedCallback subscriber = (CacheItemAddedCallback)dltList[i];
                                try
                                {
                                    if (notifyAsync)
                                    {
#if !NETCORE
                                        subscriber.BeginInvoke(keyString, null, null);
#elif NETCORE
                                            TaskFactory factory = new TaskFactory();
                                            Task task = factory.StartNew(() => subscriber(keyString));
#endif
                                    }
                                    else
                                        subscriber(keyString);

                                    if (_parent._perfStatsCollector != null)
                                        _parent._perfStatsCollector.IncrementEventsProcessedPerSec();
                                }
                                catch (Exception e)
                                {
                                }
                            }
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public virtual void OnItemAdded(object key, bool notifyAsync, EventCacheItem item, BitSet flag)
        {
            try
            {
                String keyString = key as string;
                if (key != null)
                {
                    if (item != null && item.Value != null)
                        item.Value = GetObject(item.Value, flag);
                    _eventManager.RaiseGeneralCacheNotification(keyString, EventType.ItemAdded, item, null,
                        CacheItemRemovedReason.Underused, notifyAsync);
                }
            }
            catch
            {
            }
        }

        public virtual void OnItemUpdated(object key, bool notifyAsync)
        {
            try
            {
                string ketString = key as string;
                if (ketString != null)
                {
                    if (_parent.ItemUpdatedEventHandle != null)
                    {
                        try
                        {
                            Delegate[] dltList = _parent.ItemUpdatedEventHandle.GetInvocationList();
                            for (int i = dltList.Length - 1; i >= 0; i--)
                            {
                                CacheItemUpdatedCallback subscriber = (CacheItemUpdatedCallback)dltList[i];
                                try
                                {
                                    if (notifyAsync)
                                    {
#if !NETCORE
                                        subscriber.BeginInvoke(ketString, null, null);
#elif NETCORE
                                            TaskFactory factory = new TaskFactory();
                                            Task task = factory.StartNew(() => subscriber(ketString));
#endif
                                    }
                                    else
                                        subscriber(ketString);

                                    if (_parent._perfStatsCollector != null)
                                        _parent._perfStatsCollector.IncrementEventsProcessedPerSec();
                                }
                                catch (Exception e)
                                {
                                }
                            }
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public virtual void OnItemUpdated(object key, bool notifyAsync, EventCacheItem item, EventCacheItem oldItem,
            BitSet flag)
        {
            try
            {
                string ketString = key as string;
                if (ketString != null)
                {
                    if (item != null && item.Value != null)
                        item.Value = GetObject(item.Value, flag);
                    if (oldItem != null && oldItem.Value != null)
                        oldItem.Value = GetObject(oldItem.Value, flag);
                    this._eventManager.RaiseGeneralCacheNotification(ketString, EventType.ItemUpdated, item,
                        oldItem, CacheItemRemovedReason.Underused, notifyAsync);
                }
            }
            catch (Exception e)
            {
            }
        }

        public virtual void OnItemRemoved(string key, object value, CacheItemRemovedReason reason, bool notifyAsync)
        {
            try
            {
                if (_parent.ItemRemovedEventHandle != null)
                {
                    Delegate[] dltList = _parent.ItemRemovedEventHandle.GetInvocationList();
                    for (int i = dltList.Length - 1; i >= 0; i--)
                    {
                        CacheItemRemovedCallback subscriber = (CacheItemRemovedCallback)dltList[i];
                        try
                        {
                            if (notifyAsync)
                            {
#if !NETCORE
                                subscriber.BeginInvoke((string)key, value, reason, null, null);
#elif NETCORE
                                    TaskFactory factory = new TaskFactory();
                                    Task task = factory.StartNew(() => subscriber(key, value, reason));
#endif
                            }
                            else
                                subscriber((string)key, value, reason);
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public virtual void OnItemRemoved(string key, object value, ItemRemoveReason reason, BitSet Flag,
            bool notifyAsync, EventCacheItem item)
        {
            try
            {
                if (item != null && item.Value != null)
                {
                    item.Value = value = GetObject(value, Flag);
                }

                this._eventManager.RaiseGeneralCacheNotification(key, EventType.ItemRemoved, item, null,
                    WebCacheHelper.GetWebItemRemovedReason(reason), notifyAsync);
            }
            catch
            {
            }
        }

        public virtual void OnItemRemoved(string key, object value, CacheItemRemovedReason reason, BitSet Flag,
            bool notifyAsync, EventCacheItem item)
        {
            try
            {
                if (item != null && value != null)
                    item.Value = value = GetObject(value, Flag);
                this._eventManager.RaiseGeneralCacheNotification(key, EventType.ItemRemoved, item, null, reason,
                    notifyAsync);
            }
            catch
            {
            }
        }

        public virtual void OnCustomNotification(object notifId, object data, bool notifyAsync)
        {
            try
            {
                BitSet flag = new BitSet();
                notifId = _parent.SafeDeserialize(notifId, _parent._serializationContext, flag);
                data = _parent.SafeDeserialize(data, _parent._serializationContext, flag);
                if (_parent.CustomEventHandle != null)
                {
                    Delegate[] dltList = _parent.CustomEventHandle.GetInvocationList();
                    for (int i = dltList.Length - 1; i >= 0; i--)
                    {
                        CustomEventCallback subscriber = (CustomEventCallback)dltList[i];
                        try
                        {
                            if (notifyAsync)
                            {
#if !NETCORE
                                subscriber.BeginInvoke(notifId, data, null, null);
#elif NETCORE
                                    TaskFactory factory = new TaskFactory();
                                    Task task = factory.StartNew(() => subscriber(notifId, data));
#endif
                            }
                            else
                                subscriber(notifId, data);

                            if (_parent._perfStatsCollector != null)
                                _parent._perfStatsCollector.IncrementEventsProcessedPerSec();
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
            }

            catch
            {
            }
        }

        public virtual void OnActiveQueryChanged(string queryId, QueryChangeType changeType, string key,
            bool notifyAsync, EventCacheItem item, EventCacheItem oldItem, BitSet flag, EventDataFilter datafilter)
        {
            if (item != null && item.Value != null)
                item.Value = GetObject(item.Value, flag);
            if (oldItem != null && oldItem.Value != null)
                oldItem.Value = GetObject(oldItem.Value, flag);

            ContinuousQueryManager.Notify(queryId, changeType, key, notifyAsync, item, oldItem, flag,
                this._parent.CacheImpl.Name, datafilter);
            if (_parent._perfStatsCollector != null)
                _parent._perfStatsCollector.IncrementEventsProcessedPerSec();
        }

        public virtual void OnPollNotified(short callbackId, EventType eventType)
        {
            _eventManager.RaisePollNotification(callbackId, eventType);
        }


        private object GetObject(object value, BitSet Flag)
        {
            try
            {
                if (value is CallbackEntry)
                {
                    value = ((CallbackEntry)value).Value;
                }

                if (value is UserBinaryObject)
                    value = ((UserBinaryObject)value).GetFullObject();


                return _parent.SafeDeserialize(value, _parent._serializationContext, Flag);
            }
            catch (Exception ex)
            {
                return value;
            }
        }

        public virtual void OnTaskCompletedCallback(string taskId, short taskStatus, string failureReason,
            short callbackId)
        {
            if (taskId != null || !string.IsNullOrEmpty(taskId))
            {
                _eventManager.FireMapReduceCallback(taskId, taskStatus, failureReason, callbackId);
            }
        }

        public virtual void OnClientConnectivityChange(string cacheId, Runtime.Caching.ClientInfo clientInfo,
            ConnectivityStatus status)
        {
            if (_parent._cacheClientConnectivityChanged != null)
            {
                _parent._cacheClientConnectivityChanged.Invoke(cacheId, clientInfo, status);
            }
        }

        internal void OnReregisterTopic()
        {
            _parent._messagingService.OnReregisterTopic();
        }
    }
}
