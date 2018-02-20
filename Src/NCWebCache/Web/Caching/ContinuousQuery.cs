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
using System.Collections;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common;
#if NETCORE
using System.Threading.Tasks;
#endif

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Class to hold Object query and values, intended for notifications.
    /// </summary>
    public class ContinuousQuery
    {
        public const short CQREFSTARTAdd = 2000;
        public const short CQREFSTARTRemove = 3000;
        public const short CQREFSTARTUpdate = 4000;

        private event ContinuousQueryItemAddedCallback _itemAdded;
        private event ContinuousQueryItemRemovedCallback _itemRemoved;
        private event ContinuousQueryItemUpdatedCallback _itemUpdated;
        private event ContinuousQueryClearCallback _cacheCleared;
        bool _isClearRegistered = false;

        private int refadd = 0;
        QueryDataNotificationCallback addCallback;
        private int refupdate = 0;
        QueryDataNotificationCallback updateCallback;
        private int refremove = 0;
        QueryDataNotificationCallback removeCallback;

        private ItemCallbacksWrapper _notificationsWrapper;

#region ResourcePools

        private ResourcePool _cqAddEventPool = null;
        private ResourcePool _cqAddEventDataFilter = null;
        private short _cqAddCallbackRef = CQREFSTARTAdd;
        private EventDataFilter _cqAddDF = EventDataFilter.None;

        private ResourcePool _cqRemoveEventPool = null;
        private ResourcePool _cqRemoveEventDataFilter = null;
        private short _cqRemoveCallbackRef = CQREFSTARTRemove;
        private EventDataFilter _cqRemoveDF = EventDataFilter.None;

        private ResourcePool _cqUpdateEventPool = null;
        private ResourcePool _cqUpdateEventDataFilter = null;
        private short _cqUpdateCallbackRef = CQREFSTARTUpdate;
        private EventDataFilter _cqUpdateDF = EventDataFilter.None;

        private object syncLock = new object();

        private static AsyncCallback asyn = new System.AsyncCallback(EndAsyncCallback);

#endregion

        string query;
        Hashtable values;
        string serverUniqueId;
        string clientUniqueId;

        /// <summary>
        /// Query text.
        /// </summary>
        public string Query
        {
            get { return query; }
            set { query = value; }
        }

        /// <summary>
        /// Query values.
        /// </summary>
        public Hashtable Values
        {
            get { return values; }
            set { values = value; }
        }

        internal string ServerUniqueID
        {
            get { return this.serverUniqueId; }

            set { this.serverUniqueId = value; }
        }

        internal string ClientUniqueId
        {
            get { return clientUniqueId; }
        }

        internal EventDataFilter MaxFilter(EventType eventype)
        {
            switch (eventype)
            {
                case EventType.ItemAdded:
                    return _cqAddDF;
                case EventType.ItemRemoved:
                    return _cqRemoveDF;
                case EventType.ItemUpdated:
                    return _cqUpdateDF;
                default:
                    return EventDataFilter.None;
            }
        }

        /// <summary>
        /// Initializes a new instance of the ContinuousQuery class.
        /// </summary>
        /// <param name="query">Query text</param>
        /// <param name="values">Query values</param>
        public ContinuousQuery(string query, Hashtable values)
        {
            this.query = query;
            this.values = values;
            this.clientUniqueId = Guid.NewGuid().ToString();
            _notificationsWrapper = new ItemCallbacksWrapper(this);
        }


        internal bool IsClearRegistered
        {
            get { return _isClearRegistered; }
            set { _isClearRegistered = value; }
        }


        internal ContinuousQueryClearCallback CacheCleared
        {
            get { return _cacheCleared; }
        }

        /// <summary>
        /// This method registers a custom callback that is fired if dataset of a continous query is cleared
        /// </summary>
        /// <param name="callback">A delegate to register your custom method with</param>
        /// <example>
        /// /// First create an CacheClearedCallback
        /// <code>
        ///  public static void CacheCleared()
        ///   {
        ///     ...
        ///   }
        /// </code>
        /// Then declare your continous query
        /// <code>
        /// ContinuousQuery cQ=new ContinuousQuery(query,Hashtable vals);
        /// </code>
        /// Then register your notification callback
        /// <code>
        /// cQ.RegisterClearNotification(new ContinuousQueryClearCallback(CacheCleared));
        /// </code>
        /// </example>
        public void RegisterClearNotification(ContinuousQueryClearCallback callback)
        {
            if (callback != null)
            {
                _cacheCleared += callback;
            }
        }

        /// <summary>
        /// This method Unregisters the clear callback
        /// </summary>
        /// <param name="callback">A delegate to register your custom method with</param>
        /// <example>
        /// Lets consider we registered a ClearNotification
        /// <code>
        ///  public static void CacheCleared()
        ///   {
        ///     ...
        ///   }
        /// </code>
        /// Then unregister your notification callback
        /// <code>
        /// cQ.UnRegisterClearNotification(new ContinuousQueryClearCallback(CacheCleared));
        /// </code>
        /// </example>
        public void UnRegisterClearNotification(ContinuousQueryClearCallback callback)
        {
            if (callback != null)
            {
                _cacheCleared -= callback;
            }
        }


        internal void OnCacheCleared()
        {
            if (_cacheCleared != null)
            {
                Delegate[] dltList = _cacheCleared.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    ContinuousQueryClearCallback subscriber = (ContinuousQueryClearCallback) dltList[i];

                    try
                    {
                        subscriber();
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        internal void OnItemAdded(string key, bool notifyAsync)
        {
            if (_itemAdded != null)
            {
                Delegate[] dltList = _itemAdded.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    ContinuousQueryItemAddedCallback subscriber = (ContinuousQueryItemAddedCallback) dltList[i];

                    try
                    {
                        if (notifyAsync)
                        {
#if !NETCORE
                            subscriber.BeginInvoke(key, new System.AsyncCallback(ItemAddedAsyncCallbackHandler),
                                subscriber);
#elif NETCORE
                            TaskFactory factory = new TaskFactory();
                            Task task = factory.StartNew(() => subscriber(key));
#endif
                        }
                        else
                            subscriber(key);
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        internal void OnItemAdded(string key, bool notifyAsync, EventCacheItem item)
        {
            try
            {
                this.FireCQEvents(key, EventType.ItemAdded, item, null, notifyAsync, "", null,
                    EventDataFilter.None); //Supressess all exceptions
            }
            catch (Exception)
            {
            }
        }


        private void ItemAddedAsyncCallbackHandler(IAsyncResult ar)
        {
            ContinuousQueryItemAddedCallback subscribber = (ContinuousQueryItemAddedCallback) ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (Exception e)
            {
            }
        }

        internal void OnItemUpdated(string key, bool notifyAsync)
        {
            if (_itemUpdated != null)
            {
                Delegate[] dltList = _itemUpdated.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    ContinuousQueryItemUpdatedCallback subscriber = (ContinuousQueryItemUpdatedCallback) dltList[i];

                    try
                    {
                        if (notifyAsync)
                        {
#if !NETCORE
                            subscriber.BeginInvoke(key, new System.AsyncCallback(ItemUpdatedAsyncCallbackHandler),
                                subscriber);
#elif NETCORE
                            TaskFactory factory = new TaskFactory();
                            Task task = factory.StartNew(() => subscriber(key));
#endif
                        }
                        else
                            subscriber(key);
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        internal void OnItemUpdated(string key, bool notifyAsync, EventCacheItem item, EventCacheItem oldItem)
        {
            try
            {
                this.FireCQEvents(key, EventType.ItemUpdated, item, oldItem, notifyAsync, "", null,
                    EventDataFilter.None); //Supressess all exceptions
            }
            catch (Exception)
            {
                //Logger
            }
        }


        private void ItemUpdatedAsyncCallbackHandler(IAsyncResult ar)
        {
            ContinuousQueryItemUpdatedCallback subscribber = (ContinuousQueryItemUpdatedCallback) ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (Exception e)
            {
            }
        }

        internal void OnItemRemoved(string key, bool notifyAsync)
        {
            if (_itemRemoved != null)
            {
                Delegate[] dltList = _itemRemoved.GetInvocationList();
                for (int i = dltList.Length - 1; i >= 0; i--)
                {
                    ContinuousQueryItemRemovedCallback subscriber = (ContinuousQueryItemRemovedCallback) dltList[i];

                    try
                    {
                        if (notifyAsync)
                        {
#if !NETCORE
                            subscriber.BeginInvoke(key, new System.AsyncCallback(ItemRemovedAsyncCallbackHandler),
                                subscriber);
#elif NETCORE
                            TaskFactory factory = new TaskFactory();
                            Task task = factory.StartNew(() => subscriber(key));
#endif
                        }
                        else
                            subscriber(key);
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        internal void OnItemRemoved(string key, bool notifyAsync, EventCacheItem item)
        {
            try
            {
                this.FireCQEvents(key, EventType.ItemRemoved, item, null, notifyAsync, "", null,
                    EventDataFilter.DataWithMetadata); //Supressess all exceptions
            }
            catch (Exception)
            {
            }
        }


        private void ItemRemovedAsyncCallbackHandler(IAsyncResult ar)
        {
            ContinuousQueryItemRemovedCallback subscribber = (ContinuousQueryItemRemovedCallback) ar.AsyncState;

            try
            {
                subscribber.EndInvoke(ar);
            }
            catch (Exception e)
            {
            }
        }

        /// <summary>
        /// Registers notification for item added in ContinuousQuery resultset.
        /// </summary>
        /// <param name="itemAddedCallback"></param>
        [Obsolete(
            "Use RegisterCQ(QueryDataNotificationCallback callback,EventType eventType,EventDataFilter datafilter")]
        public void RegisterAddNotification(ContinuousQueryItemAddedCallback itemAddedCallback)
        {
            if (itemAddedCallback != null)
            {
                _itemAdded += itemAddedCallback;

                lock (syncLock)
                {
                    if (++refadd == 1)
                    {
                        addCallback = new QueryDataNotificationCallback(_notificationsWrapper.OnQueryChangeNotifiation);
                        this.RegisterNotification(addCallback, EventType.ItemAdded, EventDataFilter.None);
                    }
                }
            }
            else
            {
                _itemAdded = null;

                lock (syncLock)
                {
                    if (refadd > 0)
                    {
                        refadd = 0;
                        this.UnRegisterNotification(addCallback, EventType.ItemAdded);
                    }
                }
            }
        }

        /// <summary>
        /// Registers notification for item updated in ContinuousQuery resultset.
        /// </summary>
        /// <param name="itemUpdatedCallback"></param>
        [Obsolete(
            "Use RegisterNotification(QueryDataNotificationCallback callback,EventType eventType,EventDataFilter datafilter")]
        public void RegisterUpdateNotification(ContinuousQueryItemUpdatedCallback itemUpdatedCallback)
        {
            if (itemUpdatedCallback != null)
            {
                _itemUpdated += itemUpdatedCallback;

                lock (syncLock)
                {
                    if (++refupdate == 1)
                    {
                        updateCallback =
                            new QueryDataNotificationCallback(_notificationsWrapper.OnQueryChangeNotifiation);
                        this.RegisterNotification(updateCallback, EventType.ItemUpdated, EventDataFilter.None);
                    }
                }
            }
            else
            {
                _itemUpdated = null;

                lock (syncLock)
                {
                    if (refupdate > 0)
                    {
                        refupdate = 0;
                        this.UnRegisterNotification(updateCallback, EventType.ItemUpdated);
                    }
                }
            }
        }

        /// <summary>
        /// Registers notification for item removed from ContinuousQuery resultset.
        /// </summary>
        /// <param name="itemRemovedCallback"></param>
        [Obsolete(
            "Use RegisterCQ(QueryDataNotificationCallback callback,EventType eventType,EventDataFilter datafilter")]
        public void RegisterRemoveNotification(ContinuousQueryItemRemovedCallback itemRemovedCallback)
        {
            if (itemRemovedCallback != null)
            {
                _itemRemoved += itemRemovedCallback;

                lock (syncLock)
                {
                    if (++refremove == 1)
                    {
                        removeCallback =
                            new QueryDataNotificationCallback(_notificationsWrapper.OnQueryChangeNotifiation);
                        this.RegisterNotification(removeCallback, EventType.ItemRemoved,
                            EventDataFilter.DataWithMetadata);
                    }
                }
            }
            else
            {
                _itemRemoved = null;

                lock (syncLock)
                {
                    if (refremove > 0)
                    {
                        refremove = 0;
                        this.UnRegisterNotification(removeCallback, EventType.ItemRemoved);
                    }
                }
            }
        }

        /// <summary>
        /// This method registers a custom callback that is fired on change in dataset of a continous query
        /// </summary>
        /// <param name="callback">A delegate to register your custom method with</param>
        /// <param name="eventType">Describes whether the event is to be raised on Item Added, Updated or Removed</param>
        /// <param name="datafilter">This enum is to describe when registering an event, upon raise how much data is 
        /// retrieved from cache when the event is raised</param>
        /// <example>
        /// /// First create an ItemCallback
        /// <code>
        /// ItemCallback(string key, CacheEventArg e)
        /// {
        ///    ...
        /// }
        /// </code>
        /// Then declare your continous query
        /// <code>
        /// ContinuousQuery cQ=new ContinuousQuery(query,Hashtable vals);
        /// </code>
        /// Then register your notification callback
        /// <code>
        /// cQ.RegisterNotification(new QueryDataNotificationCallback(ItemCallback),EventType.ItemAdded, EventDataFilter.None);
        /// </code>
        /// </example>
        public void RegisterNotification(QueryDataNotificationCallback callback, EventType eventType,
            EventDataFilter datafilter)
        {
            if (callback != null)
            {
                //Avoiding new ResourcePool(inside = new Hashtable) at constructor level
                if (_cqAddEventPool == null && (eventType & EventType.ItemAdded) != 0)
                {
                    _cqAddEventPool = new ResourcePool();
                    _cqAddEventDataFilter = new ResourcePool();
                }

                if (_cqRemoveEventPool == null && (eventType & EventType.ItemRemoved) != 0)
                {
                    _cqRemoveEventPool = new ResourcePool();
                    _cqRemoveEventDataFilter = new ResourcePool();
                }

                if (_cqUpdateEventPool == null && (eventType & EventType.ItemUpdated) != 0)
                {
                    _cqUpdateEventPool = new ResourcePool();
                    _cqUpdateEventDataFilter = new ResourcePool();
                }

                RegisterCQ(callback, eventType, datafilter);
            }
        }

        /// <summary>
        /// This method Unregisters a custom callback that is fired on change in dataset of a continous query
        /// </summary>
        /// <param name="callback">A delegate to register your custom method with</param>
        /// <param name="eventType">Describes whether the event is to be raised on Item Added, Updated or Removed</param>
        /// <example>
        /// Lets consider we created an ItemCallback
        /// <code>
        /// ItemCallback(string key, CacheEventArg e)
        /// {
        ///    ...
        /// }
        /// </code>
        /// Uregister your notification callback
        /// <code>
        /// cQ.RegisterNotification(new QueryDataNotificationCallback(ItemCallback),EventType.ItemAdded);
        /// </code>
        /// </example>
        public void UnRegisterNotification(QueryDataNotificationCallback callback, EventType eventType)
        {
            //BY LEGACY DESIGN THERE IS NO UNREGISTRATION PROCESS

            if (callback == null)
                throw new ArgumentNullException("callback");

            object id = -1;

            foreach (EventType type in Enum.GetValues(typeof(EventType)))
            {
                lock (syncLock)
                {
                    ResourcePool pool = null;
                    ResourcePool poolDF = null;

#region pool selection

                    if (type == EventType.ItemAdded && (eventType & EventType.ItemAdded) != 0)
                    {
                        pool = _cqAddEventPool;
                        poolDF = _cqAddEventDataFilter;
                    }
                    else if (type == EventType.ItemRemoved && (eventType & EventType.ItemRemoved) != 0)
                    {
                        pool = _cqRemoveEventPool;
                        poolDF = _cqRemoveEventDataFilter;
                    }
                    else if (type == EventType.ItemUpdated && (eventType & EventType.ItemUpdated) != 0)
                    {
                        pool = _cqUpdateEventPool;
                        poolDF = _cqUpdateEventDataFilter;
                    }

                    if (pool == null)
                        continue;

#endregion

                    object temp = pool.GetResource(callback);
                    short index = -1;
                    index = Convert.ToInt16(temp);


                    if (index > -1)
                    {
                        EventDataFilter datafilter = (EventDataFilter) poolDF.GetResource(index);

                        object retVal = poolDF.RemoveResource(index);
                        pool.RemoveResource(callback);

                        if (retVal == null) continue;
                        bool unregisterNotification = poolDF.Count == 0;
                        EventDataFilter maxDataFilter = EventDataFilter.None;


                        if (!unregisterNotification)
                        {
                            object[] callbackRefs = poolDF.GetAllResourceKeys();

                            if (callbackRefs != null)
                            {
                                for (int i = 0; i < callbackRefs.Length; i++)
                                {
                                    EventDataFilter df = (EventDataFilter) callbackRefs[i];

                                    if (df > maxDataFilter)
                                        maxDataFilter = df;

                                    if (maxDataFilter == EventDataFilter.DataWithMetadata) break;
                                }
                            }
                        }

                        if (type == EventType.ItemAdded)
                        {
                            _cqAddDF = maxDataFilter;
                        }
                        else if (type == EventType.ItemRemoved)
                        {
                            _cqRemoveDF = maxDataFilter;
                        }
                        else
                        {
                            _cqUpdateDF = maxDataFilter;
                        }
                    }
                }
            }
        }

        private void RegisterCQ(QueryDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");


            foreach (EventType type in Enum.GetValues(typeof(EventType)))
            {
                lock (syncLock)
                {
                    ResourcePool pool = null;
                    ResourcePool poolDF = null;

#region pool selection

                    if (type == EventType.ItemAdded && (eventType & EventType.ItemAdded) != 0)
                    {
                        pool = _cqAddEventPool;
                        poolDF = _cqAddEventDataFilter;
                    }
                    else if (type == EventType.ItemRemoved && (eventType & EventType.ItemRemoved) != 0)
                    {
                        pool = _cqRemoveEventPool;
                        poolDF = _cqRemoveEventDataFilter;
                    }
                    else if (type == EventType.ItemUpdated && (eventType & EventType.ItemUpdated) != 0)
                    {
                        pool = _cqUpdateEventPool;
                        poolDF = _cqUpdateEventDataFilter;
                    }

                    if (pool == null)
                        continue;

#endregion

                    while (true)
                    {
                        if (pool.GetResource(callback) == null)
                        {
                            short refcallback = -1;
                            if (type == EventType.ItemAdded)
                            {
                                refcallback = ++_cqAddCallbackRef;
                                _cqAddDF = _cqAddDF < datafilter ? datafilter : _cqAddDF;
                            }
                            else if (type == EventType.ItemRemoved)
                            {
                                refcallback = ++_cqRemoveCallbackRef;
                                _cqRemoveDF = _cqRemoveDF < datafilter ? datafilter : _cqRemoveDF;
                            }
                            else
                            {
                                refcallback = ++_cqUpdateCallbackRef;
                                _cqUpdateDF = _cqUpdateDF < datafilter ? datafilter : _cqUpdateDF;
                            }

                            pool.AddResource(callback, refcallback);
                            poolDF.AddResource(refcallback, datafilter);
                            break;
                        }
                        else
                        {
                            try
                            {
                                short cref = (short) pool.GetResource(callback);
                                if (cref < 0)
                                    break; //FAIL CONDITION

                                poolDF.RemoveResource(cref);
                                pool.RemoveResource(callback);

                                //add it again into the table for updating ref count.
                                pool.AddResource(callback, cref);
                                poolDF.AddResource(cref, datafilter);
                                break;
                            }
                            catch (NullReferenceException)
                            {
                                continue;
                            }
                        }
                    }
                }
            }
        }


        internal void FireCQEvents(string key, EventType eventType, EventCacheItem item, EventCacheItem oldItem,
            bool notifyAsync, string cacheName, BitSet flag, EventDataFilter datafilter)
        {
            try
            {
                CQEventArg arg = null;

                ICollection collection = null;
                ResourcePool pool = null;
                ResourcePool filterPool = null;
                if ((eventType & EventType.ItemAdded) != 0 && _cqAddEventPool != null)
                {
                    pool = _cqAddEventPool;
                    collection = _cqAddEventPool.Keys;
                    filterPool = _cqAddEventDataFilter;
                }
                else if ((eventType & EventType.ItemUpdated) != 0 && _cqUpdateEventPool != null)
                {
                    pool = _cqUpdateEventPool;
                    collection = _cqUpdateEventPool.Keys;
                    filterPool = _cqUpdateEventDataFilter;
                }
                else if ((eventType & EventType.ItemRemoved) != 0 && _cqRemoveEventPool != null)
                {
                    pool = _cqRemoveEventPool;
                    collection = _cqRemoveEventPool.Keys;
                    filterPool = _cqRemoveEventDataFilter;
                }
                else
                    return;


                if (collection != null && collection.Count > 0)
                {
                    QueryDataNotificationCallback[] disc = null;
                    lock (syncLock)
                    {
                        disc = new QueryDataNotificationCallback[collection.Count];
                        collection.CopyTo(disc, 0); //to avoid locking 
                    }

                    for (int i = 0; i < disc.Length; i++)
                    {
                        short index = -1;
                        object obj = pool.GetResource(disc[i]);
                        index = Convert.ToInt16(obj);

                        if (index > -1)
                        {
                            //Not to fire event if datafilter recieved is less than requried OR noDF present
                            EventDataFilter queryDataFilter = (EventDataFilter) filterPool.GetResource(index);

                            if ((eventType & EventType.ItemAdded) != 0)
                                arg = CreateCQEventArgument(queryDataFilter, key, cacheName, EventType.ItemAdded, item,
                                    oldItem);
                            else if ((eventType & EventType.ItemUpdated) != 0)
                                arg = CreateCQEventArgument(queryDataFilter, key, cacheName, EventType.ItemUpdated,
                                    item, oldItem);
                            else if ((eventType & EventType.ItemRemoved) != 0)
                                arg = CreateCQEventArgument(queryDataFilter, key, cacheName, EventType.ItemRemoved,
                                    item, oldItem);
                            else
                                return;

                            arg.ContinuousQuery = this;

                            if (notifyAsync)
                            {
#if !NETCORE
                                disc[i].BeginInvoke(key, arg, asyn, disc[i]);
#elif NETCORE
                                TaskFactory factory = new TaskFactory();
                                int temp = i;
                                Task task = factory.StartNew(() => disc[temp](key, arg));
#endif
                            }
                            else
                                disc[i].Invoke(key, arg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private CQEventArg CreateCQEventArgument(EventDataFilter dataFilter, string key, string cacheName,
            EventType eventType, EventCacheItem item, EventCacheItem oldItem)
        {
            EventCacheItem cloneItem = null;
            EventCacheItem cloneOldItem = null;

            if (dataFilter != EventDataFilter.None && item != null)
            {
                cloneItem = item.Clone() as EventCacheItem;

                if (dataFilter == EventDataFilter.Metadata)
                    cloneItem.Value = null;
            }

            if (dataFilter != EventDataFilter.None && oldItem != null)
            {
                cloneOldItem = oldItem.Clone() as EventCacheItem;

                if (dataFilter == EventDataFilter.Metadata)
                    cloneOldItem.Value = null;
            }

            CQEventArg eventArg = new CQEventArg(cacheName, eventType, cloneItem, null);
            if (eventType == EventType.ItemUpdated) eventArg.OldItem = cloneOldItem;

            return eventArg;
        }

        private static void EndAsyncCallback(IAsyncResult arr)
        {
            QueryDataNotificationCallback subscribber = (QueryDataNotificationCallback) arr.AsyncState;

            try
            {
                if (subscribber != null)
                    subscribber.EndInvoke(arr);
            }
            catch (Exception e)
            {
            }
        }

        internal bool NotifyAdd
        {
            get
            {
                if (_itemAdded != null || (_cqAddEventPool != null && _cqAddEventPool.Count > 0))
                {
                    return true;
                }

                return false;
            }
        }

        internal bool NotifyUpdate
        {
            get
            {
                if (_itemUpdated != null || (_cqUpdateEventPool != null && _cqUpdateEventPool.Count > 0))
                {
                    return true;
                }

                return false;
            }
        }

        internal bool NotifyRemove
        {
            get
            {
                if (_itemRemoved != null || (_cqRemoveEventPool != null && _cqRemoveEventPool.Count > 0))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Compares two instances of ContinuousQuery for equality.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns> true if both ContinuousQuery instances are the same. Otherwise false. </returns>
        public override bool Equals(object obj)
        {
            ContinuousQuery other = obj as ContinuousQuery;
            if (other != null)
            {
                if (this.clientUniqueId.Equals(other.clientUniqueId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Compoutes the hash code for the current object.
        /// </summary>
        /// <returns> Hash code for the current object </returns>
        public override int GetHashCode()
        {
            if (this.clientUniqueId != null)
            {
                return this.clientUniqueId.GetHashCode();
            }
            else
            {
                return base.GetHashCode();
            }
        }
    }
}