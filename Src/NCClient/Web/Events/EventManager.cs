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

using System;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Logger;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.Events;
using static Alachisoft.NCache.Client.EventUtil;
using System.Threading.Tasks;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Has the responsibility of creating <see cref=" CacheEventDescriptor"/> and registering it agains a ResourcePool
    /// </summary>
    internal class EventManager
    {
        public const short REFSTART = -1;
        public const short SELECTIVEREFSTARTRemove = 8000;
        public const short SELECTIVEREFSTARTUpdate = 9000;
        public const short SELECTIVEREFSTARTPolling = 10000;

        public const short COLLECTIONREFSTARTAdd = 11000;
        public const short COLLECTIONREFSTARTUpdate = 12000;
        public const short COLLECTIONREFSTARTRemove = 13000;

        System.Threading.WaitCallback waitC = new System.Threading.WaitCallback(Fire);

        string _cacheName = null;
        private NCacheLogger _logger;

        private object sync_lock_collection = new object();
        private object sync_lock_selective = new object();
        private object sync_lock_general = new object();
        
        private PollNotificationCallback _pollPubSubCallback;


        private static AsyncCallback asyn = new System.AsyncCallback(EndAsyncCallback);
        private Cache _cache;

        private int addCallbacks = -1;
        private int removeCallbacks = -1;
        private int updateCallbacks = -1;

        #region Resource Pools
        private ResourcePool _addEventPool = null;
        private EventDataFilter _addDataFilter = EventDataFilter.None;

        private ResourcePool _removeEventPool = null;
        private EventDataFilter _removeDataFilter = EventDataFilter.None;

        private ResourcePool _updateEventPool = null;
        private EventDataFilter _updateDataFilter = EventDataFilter.None;

        private short _addEventRegistrationSequence = REFSTART; //Significant difference from old callback numbers
        private short _updateEventRegisrationSequenceId = REFSTART; //Significant difference from old callback numbers
        private short _removeEventRegistrationSequenceId = REFSTART; //Significant difference from old callback numbers

        private ResourcePool _selectiveRemoveEventPool = null;
        private ResourcePool _selectiveRemoveEventIDPool = null;

        private ResourcePool _oldSelectiveCallbackPool = new ResourcePool();
        private ResourcePool _oldSelectiveMappingCallbackPool = new ResourcePool();
        private short _selectveRemoveCallbackRef = SELECTIVEREFSTARTRemove;

        private ResourcePool _selectiveUpdateEventPool = null;
        private ResourcePool _selectiveUpdateEventIDPool = null;
        private short _selectiveUpdateCallbackRef = SELECTIVEREFSTARTUpdate;
        private EventDataFilter _generalAddDataFilter = EventDataFilter.None;
        private EventDataFilter _generalUpdateDataFilter = EventDataFilter.None;
        private EventDataFilter _generalRemoveDataFilter = EventDataFilter.None;

        private short _pollingNotificationCallbackRef = SELECTIVEREFSTARTPolling;

        private TopicSubscription _selectiveEventsSubscription;
        private TopicSubscription _collectionEventsSubscription;

        #region -- Collection Events Specific Fields --
        private ResourcePool _collectionAddEventPool = null;
        private ResourcePool _collectionAddEventIdPool = null;
        private short _collectionAddCallbackRef = COLLECTIONREFSTARTAdd;

        private ResourcePool _collectionUpdateEventPool = null;
        private ResourcePool _collectionUpdateEventIdPool = null;
        private short _collectionUpdateCallbackRef = COLLECTIONREFSTARTUpdate;

        private ResourcePool _collectionRemoveEventPool = null;
        private ResourcePool _collectionRemoveEventIdPool = null;
        private short _collectionRemoveCallbackRef = COLLECTIONREFSTARTRemove;
        #endregion

        #endregion

        internal EventManager(string cacheName, NCacheLogger logger, Cache cache)
        {
            _cacheName = cacheName;
            _logger = logger;
            _cache = cache;
        }

        internal short AddSequenceNumber { get { return _addEventRegistrationSequence; } }

        internal short UpdateSequenceNumber { get { return _updateEventRegisrationSequenceId; } }

        internal short RemoveSequenceNumber { get { return _removeEventRegistrationSequenceId; } }

        internal object SyncLockGeneral { get { return sync_lock_general; } }
        internal object SyncLockSelective { get { return sync_lock_selective; } }
        internal object SyncLockCollection { get { return sync_lock_collection; } }

        /// <summary>
        /// Provide 
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        internal short GeneralEventRefCountAgainstEvent(EventTypeInternal eventType)
        {
            if ((eventType & EventTypeInternal.ItemAdded) != 0)
                return _addEventRegistrationSequence;
            if ((eventType & EventTypeInternal.ItemRemoved) != 0)
                return _removeEventRegistrationSequenceId;
            if ((eventType & EventTypeInternal.ItemUpdated) != 0)
                return _updateEventRegisrationSequenceId;

            return -1;
        }

        /// <summary>
        /// Returns the filter type of the eventType
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        internal EventDataFilter MaxFilterAgainstEvent(EventTypeInternal eventType)
        {
            if ((eventType & EventTypeInternal.ItemAdded) != 0)
                return _addDataFilter;
            if ((eventType & EventTypeInternal.ItemRemoved) != 0)
                return _removeDataFilter;
            if ((eventType & EventTypeInternal.ItemUpdated) != 0)
                return _updateDataFilter;

            return EventDataFilter.None;
        }

        /// <summary>
        /// Registeres the callback sepeartely and returns short values of registeredCallbacks
        /// </summary>
        /// <param name="key"></param>
        /// <param name="callback"></param>
        /// <param name="eventType"></param>
        /// <param name="datafilter"></param>
        /// <returns>short array,<para>1st element is updated callbackRef</para><para>2st element is removed callbackRef</para></returns>
        internal short[] RegisterSelectiveEvent(CacheDataNotificationCallback callback, EventTypeInternal eventType, EventDataFilter datafilter, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            if (callback != null)
            {
                //Avoiding new ResourcePool(inside = new Hashtable) at constructor level
                if (_selectiveUpdateEventPool == null)
                {
                    _selectiveUpdateEventPool = new ResourcePool();
                    _selectiveUpdateEventIDPool = new ResourcePool();
                }
                if (_selectiveRemoveEventPool == null)
                {
                    _selectiveRemoveEventPool = new ResourcePool();
                    _selectiveRemoveEventIDPool = new ResourcePool();
                }
                return RegisterSelectiveDiscriptor(callback, eventType,callbackType);
            }
            else
                return null;
        }


        

        internal short RegisterPollingEvent(PollNotificationCallback callback, EventTypeInternal eventType)
        {
            // Only one poll callback can be configured.
            // No need to use pools.

            if ((eventType & EventTypeInternal.PubSub) != 0)
            {
                _pollPubSubCallback = callback;

            }

            return 10001;
        }

        internal CacheEventDescriptor RegisterGeneralEvents(CacheDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter)
        {
            if (callback != null)
            {
                //Avoiding new ResourcePool(inside = new Hashtable) at constructor level
                if (_addEventPool == null)
                {
                    _addEventPool = new ResourcePool();
                }
                if (_removeEventPool == null)
                {
                    _removeEventPool = new ResourcePool();
                }
                if (_updateEventPool == null)
                {
                    _updateEventPool = new ResourcePool();
                }

                CacheEventDescriptor discriptor = CacheEventDescriptor.CreateCacheDiscriptor(eventType, _cacheName, callback, datafilter);

                //Registers the handl)
                bool registeredDescriptor = RegisterGeneralDiscriptor(discriptor, EventsUtil.GetEventTypeInternal(eventType));
                if (!registeredDescriptor)
                    return null;

                return discriptor;

            }
            else
                return null;
        }


        internal void UnregisterAll()
        {
        }

        /// <summary>
        /// TheadSafe and no locks internally
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eventType">Should contain one type i.e. should not be used as a flag.
        /// Every EventType should be executed from another thread</param>
        /// <param name="item"></param>
        /// <param name="oldItem"></param>
        /// <param name="reason"></param>
        /// <param name="notifyAsync"></param>
        internal void RaiseGeneralCacheNotification(string key, EventType eventType, EventCacheItem item, EventCacheItem oldItem, CacheItemRemovedReason reason, bool notifyAsync)
        {
            try
            {
                object[] registeredDiscriptors = null;

                ResourcePool eventPool = GetEventPool(EventsUtil.GetEventTypeInternal(eventType));
                if (eventPool != null)
                    registeredDiscriptors = eventPool.GetAllResourceKeys();

                if (registeredDiscriptors != null && registeredDiscriptors.Length > 0)
                {
                    for (int i = 0; i < registeredDiscriptors.Length; i++)
                    {
                        CacheEventDescriptor discriptor = registeredDiscriptors[i] as CacheEventDescriptor;

                        if (discriptor == null)
                            continue;

                        var bitSet = new BitSet();

                        if (_cache.SerializationFormat == Common.Enum.SerializationFormat.Json)
                            bitSet.SetBit(BitSetConstants.JsonData);

                        if (item != null)
                            item.SetValue(_cache.SafeDeserialize<object>(item.GetValue<object>(), _cache.SerializationContext, bitSet, UserObjectType.CacheItem));

                        if (oldItem != null)
                            oldItem.SetValue(_cache.SafeDeserialize<object>(oldItem.GetValue<object>(), _cache.SerializationContext, bitSet, UserObjectType.CacheItem));

                        var arg = CreateCacheEventArgument(discriptor.DataFilter, key, _cacheName, eventType, item, oldItem, reason);
                        arg.Descriptor = discriptor;

                        if (notifyAsync)
                        {
#if !NETCORE
                            discriptor.CacheDataNotificationCallback.BeginInvoke(key, arg, asyn, null);
#elif NETCORE
                            //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                            TaskFactory factory = new TaskFactory();
                            Task task = factory.StartNew(() => discriptor.CacheDataNotificationCallback(key, arg));
#endif
                        }
                        else
                            discriptor.CacheDataNotificationCallback.Invoke(key, arg);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_logger != null && _logger.IsErrorEnabled) _logger.CriticalInfo(ex.ToString());
            }
        }

        internal void RaisePollNotification(short callbackId, EventTypeInternal eventType)
        {
            try
            {
                // just invoke the callback if not null.
                // callbackId is of no use here.
                PollNotificationCallback _pollCallback = null;
                
                if ((eventType & EventTypeInternal.PubSub) != 0)
                {
                    _pollCallback = _pollPubSubCallback;
                }
                if (_pollCallback != null)
                    _pollCallback.Invoke();
            }
            catch (Exception ex)
            {
                if (_logger != null && _logger.IsErrorEnabled) _logger.CriticalInfo(ex.ToString());
            }

        }

        private CacheEventArg CreateCacheEventArgument(EventDataFilter dataFilter, string key, string cacheName, EventType eventType, EventCacheItem item, EventCacheItem oldItem, CacheItemRemovedReason removedReason)
        {
            EventCacheItem cloneItem = null;
            EventCacheItem cloneOldItem = null;

            CacheEventArg eventArg = new CacheEventArg(key, cacheName, eventType, cloneItem, null, removedReason);
            if (eventType == EventType.ItemUpdated) eventArg.OldItem = cloneOldItem;

            return eventArg;
        }

   
        /// <summary>
        /// TheadSafe and no locks internally
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eventType">Should contain one type i.e. should not be used as a flag.
        /// Every EventType should be executed from another thread</param>
        /// <param name="item"></param>
        /// <param name="oldItem"></param>
        /// <param name="reason"></param>
        /// <param name="_notifyAsync"></param>
        /// <param name="eventhandle"></param>
        internal void RaiseSelectiveCacheNotification(string key, EventType eventType, EventCacheItem item, EventCacheItem oldItem, CacheItemRemovedReason reason, bool _notifyAsync, EventHandle eventhandle, EventDataFilter dataFilter)
        {
            try
            {
                ResourcePool poolID = null;
                CacheEventArg arg = null;
                var bitSet = new BitSet();

                if (_cache.SerializationFormat == Common.Enum.SerializationFormat.Json)
                    bitSet.SetBit(BitSetConstants.JsonData);

                if (item != null)
                    item.SetValue(_cache.SafeDeserialize<object>(item.GetValue<object>(), _cache.SerializationContext, bitSet, UserObjectType.CacheItem));
                if (oldItem != null)
                    oldItem.SetValue(_cache.SafeDeserialize<object>(oldItem.GetValue<object>(), _cache.SerializationContext, bitSet, UserObjectType.CacheItem));
                if ((eventType & EventType.ItemUpdated) != 0)
                {
                    poolID = _selectiveUpdateEventIDPool;
                }
                else if ((eventType & EventType.ItemRemoved) != 0)
                {
                    poolID = _selectiveRemoveEventIDPool;
                }

                arg = CreateCacheEventArgument(dataFilter, key, _cacheName, eventType, item, oldItem, reason);

                if (poolID == null)
                    return;

                CacheDataNotificationCallback callback = poolID.GetResource((short)eventhandle.Handle) as CacheDataNotificationCallback;

                if (callback == null) //Can occur if Unregistered concurrently
                    return;

                if (_notifyAsync)
                    System.Threading.ThreadPool.QueueUserWorkItem(waitC, new object[] { callback, key, arg }); //Faster and better
                else
                    callback.Invoke(key, arg);
            }
            catch (Exception ex)
            {
                if (_logger != null && _logger.IsErrorEnabled) _logger.CriticalInfo(ex.ToString());
            }
        }

        /// <summary>
        /// TheadSafe and no locks internally.
        /// </summary>
       
        private static void Fire(object obj)
        {
            try
            {
                object[] objArray = (object[])obj;
                ((CacheDataNotificationCallback)objArray[0]).Invoke((string)objArray[1], (CacheEventArg)objArray[2]);
            }
            catch (Exception)
            {

            }
        }


        internal short RegisterSelectiveCallback(CacheItemRemovedCallback removedCallback, CallbackType callbackType)
        {
            if (removedCallback == null)
                return -1;
            lock (SyncLockSelective)
            {
                SelectiveRemoveCallbackWrapper callbackWrapper = null;
                // callback is not already registered with the same method, so add
                if (_oldSelectiveCallbackPool.GetResource(removedCallback) == null)
                {
                    callbackWrapper = new SelectiveRemoveCallbackWrapper(removedCallback);
                    _oldSelectiveCallbackPool.AddResource(removedCallback, callbackWrapper);
                    _oldSelectiveMappingCallbackPool.AddResource(callbackWrapper, removedCallback);
                }
                // already present against the same method, so no need to add again.
                else
                {
                    callbackWrapper = (SelectiveRemoveCallbackWrapper)_oldSelectiveCallbackPool.GetResource(removedCallback);
                    _oldSelectiveCallbackPool.AddResource(removedCallback, callbackWrapper);//just to increment the refCount
                }

                short[] callbackIds = RegisterSelectiveEvent(callbackWrapper.MappingCallback, EventTypeInternal.ItemRemoved, EventDataFilter.None, callbackType);
                return callbackIds[1];
            }
        }

        internal short UnRegisterSelectiveCallback(CacheItemRemovedCallback removedCallback)
        {
            if (removedCallback == null)
                return -1;

            SelectiveRemoveCallbackWrapper callbackWrapper = null;
            // callback is not already registered with the same method, so add
            lock (SyncLockSelective)
            {
                //a callback more then one time or against wrong items.
                callbackWrapper = (SelectiveRemoveCallbackWrapper)_oldSelectiveCallbackPool.GetResource(removedCallback);

                if (callbackWrapper != null)
                {
                    short[] callbackIds = UnregisterSelectiveNotification(callbackWrapper.MappingCallback, EventTypeInternal.ItemRemoved);
                    return callbackIds[1];
                }
                return -1;
            }
        }
        internal short RegisterSelectiveCallback(CacheItemUpdatedCallback updateCallback, CallbackType callbackType)
        {
            if (updateCallback == null)
                return -1;

            SelectiveUpdateCallbackWrapper callbackWrapper = null;
            lock (SyncLockSelective)
            {
                // callback is not already registered with the same method, so add
                if (_oldSelectiveCallbackPool.GetResource(updateCallback) == null)
                {
                    callbackWrapper = new SelectiveUpdateCallbackWrapper(updateCallback);
                    _oldSelectiveCallbackPool.AddResource(updateCallback, callbackWrapper);
                    _oldSelectiveMappingCallbackPool.AddResource(callbackWrapper, updateCallback);
                }
                // already present against the same method, so no need to add again.
                else
                {
                    callbackWrapper = (SelectiveUpdateCallbackWrapper)_oldSelectiveCallbackPool.GetResource(updateCallback);
                    _oldSelectiveCallbackPool.AddResource(updateCallback, callbackWrapper);//to increment the refcount
                }

                short[] callbackIds = RegisterSelectiveEvent(callbackWrapper.MappingCallback, EventTypeInternal.ItemUpdated, EventDataFilter.None,callbackType);
                return callbackIds[0];
            }
        }


        internal short UnRegisterSelectiveCallback(CacheItemUpdatedCallback updateCallback)
        {
            if (updateCallback == null)
                return -1;
            lock (SyncLockSelective)
            {
                SelectiveUpdateCallbackWrapper callbackWrapper = (SelectiveUpdateCallbackWrapper)_oldSelectiveCallbackPool.GetResource(updateCallback);
                //a callback more then one time or against wrong items.
                if (callbackWrapper != null)
                {
                    short[] callbackIds = RegisterSelectiveEvent(callbackWrapper.MappingCallback, EventTypeInternal.ItemUpdated, EventDataFilter.None);
                    return callbackIds[0];
                }
                return -1;
            }
        }
        /// <summary>
        /// Returning Negative value means operation not successfull
        /// </summary>
        /// <param name="discriptor"></param>
        /// <param name="eventType"></param>
        /// <returns>short array <para>1st value is Update callbackRef</para> <para>nd value is removeRef</para></returns>
        private short[] RegisterSelectiveDiscriptor(CacheDataNotificationCallback callback, EventTypeInternal eventType, CallbackType callbackType)
        {
            if (callback == null)
                return null; //FAIL CONDITION
            short[] returnValue = new short[] { -1, -1 }; //First value update callback ref & sencond is remove callbackref

            foreach (EventTypeInternal type in Enum.GetValues(typeof(EventTypeInternal)))
            {
                if (type == EventTypeInternal.ItemAdded) //ItemAdded not supported Yet
                    continue;

                lock (SyncLockSelective)
                {
                    ResourcePool pool = null;
                    ResourcePool poolID = null;

                    #region pool selection

                    if (type == EventTypeInternal.ItemRemoved && (eventType & EventTypeInternal.ItemRemoved) != 0)
                    {
                        pool = _selectiveRemoveEventPool;
                        poolID = _selectiveRemoveEventIDPool;
                    }
                    else if (type == EventTypeInternal.ItemUpdated && (eventType & EventTypeInternal.ItemUpdated) != 0)
                    {
                        pool = _selectiveUpdateEventPool;
                        poolID = _selectiveUpdateEventIDPool;
                    }

                    if (pool == null)
                        continue;
                    #endregion

                    while (true)
                    {
                        int i = type == EventTypeInternal.ItemUpdated ? 0 : 1;
                        if (pool.GetResource(callback) == null)
                        {

                            returnValue[i] = type == EventTypeInternal.ItemUpdated ? ++_selectiveUpdateCallbackRef : ++_selectveRemoveCallbackRef;
                            pool.AddResource(callback, returnValue[i]);
                            poolID.AddResource(returnValue[i], callback);
                            break;
                        }
                        else
                        {
                            try
                            {
                                short cref = (short)pool.GetResource(callback);
                                if (cref < 0)
                                    break; //FAIL CONDITION

                                //add it again into the table for updating ref count.
                                pool.AddResource(callback, cref);
                                poolID.AddResource(cref, callback);
                                returnValue[i] = cref;
                                break;
                            }
                            catch (NullReferenceException)
                            {
                                //Legacy code: can create an infinite loop
                                //Recomendation of returning a negative number instead of continue
                                continue;
                            }
                        }
                    }
                }
            }

            if (_selectiveEventsSubscription == null && callbackType != CallbackType.PullBasedCallback)
            {
                Topic topic = (Topic)_cache._messagingService.GetTopic(TopicConstant.ItemLevelEventsTopic, true);
                _selectiveEventsSubscription = (TopicSubscription)topic.CreateEventSubscription(OnSelectiveEventMessageReceived);
            }

            return returnValue;
        }

      
        private bool RegisterGeneralDiscriptor(CacheEventDescriptor discriptor, EventTypeInternal eventType)
        {
            if (discriptor == null)
                return false; //FAIL CONDITION

            EventHandle handle = null;

            foreach (EventTypeInternal type in Enum.GetValues(typeof(EventTypeInternal)))
            {
                ResourcePool pool = null;
                bool registrationUpdated = false;

                #region Pool selection

                if ((type & eventType) != 0)
                    pool = GetEventPool(type);

                if (pool == null)
                    continue;

                #endregion
                short registrationSequenceId = -1;

                lock (SyncLockGeneral)
                {
                    pool.AddResource(discriptor, 1); // Everytime a new Discriptor is forcefully created

                    //Keeps a sequence number

                    switch (type)
                    {
                        case EventTypeInternal.ItemAdded:
                            if (discriptor.DataFilter > _generalAddDataFilter || _addEventRegistrationSequence == REFSTART)
                            {
                                registrationUpdated = true;
                                registrationSequenceId = ++_addEventRegistrationSequence;
                                _generalAddDataFilter = discriptor.DataFilter;
                            }
                            else
                                registrationSequenceId = _addEventRegistrationSequence;
                            break;
                        case EventTypeInternal.ItemRemoved:
                            if (discriptor.DataFilter > _generalRemoveDataFilter || _removeEventRegistrationSequenceId == REFSTART)
                            {
                                registrationUpdated = true;
                                registrationSequenceId = ++_removeEventRegistrationSequenceId;
                                _generalRemoveDataFilter = discriptor.DataFilter;
                            }
                            else
                                registrationSequenceId = _removeEventRegistrationSequenceId;
                            break;
                        case EventTypeInternal.ItemUpdated:
                            if (discriptor.DataFilter > _generalUpdateDataFilter || _updateEventRegisrationSequenceId == REFSTART)
                            {
                                registrationUpdated = true;
                                registrationSequenceId = ++_updateEventRegisrationSequenceId;
                                _generalUpdateDataFilter = discriptor.DataFilter;
                            }
                            else
                                registrationSequenceId = _updateEventRegisrationSequenceId;
                            break;
                    }

                    //Although the handle doesnt matter in general events
                    if (handle == null) handle = new EventHandle(registrationSequenceId);
                }

                if (_cache != null && registrationSequenceId != -1)
                    _cache.RegisterCacheNotificationDataFilter(type, discriptor.DataFilter, registrationSequenceId);
            }

      

            discriptor.IsRegistered = true;
            discriptor.Handle = handle;
            return true;
        }

        /// <summary>
        /// Unregisters CacheDataNotificationCallback
        /// <para>Flag based unregistration</para>
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="key"></param>
        /// <param name="eventType"></param>
        internal short[] UnregisterSelectiveNotification(CacheDataNotificationCallback callback, EventTypeInternal eventType)
        {

            if (callback == null)
                return null;

            short[] returnValue = new short[] { -1, -1 }; //First value update callback ref & sencond is remove callbackref


            foreach (EventTypeInternal type in Enum.GetValues(typeof(EventTypeInternal)))
            {
                if (type == EventTypeInternal.ItemAdded) //ItemAdded not supported Yet
                    continue;

                object id = -1;

                lock (SyncLockSelective)
                {
                    ResourcePool pool = null;
                    ResourcePool poolID = null;

                    #region pool selection

                    if (type == EventTypeInternal.ItemRemoved && (eventType & EventTypeInternal.ItemRemoved) != 0)
                    {
                        pool = _selectiveRemoveEventPool;
                        poolID = _selectiveRemoveEventIDPool;
                        if (pool == null)
                            removeCallbacks = 0;
                    }
                    else if (type == EventTypeInternal.ItemUpdated && (eventType & EventTypeInternal.ItemUpdated) != 0)
                    {
                        pool = _selectiveUpdateEventPool;
                        poolID = _selectiveUpdateEventIDPool;
                        if (pool == null)
                            updateCallbacks = 0;
                    }

                    if(removeCallbacks == 0 && updateCallbacks == 0)
                    {
                        _selectiveEventsSubscription.UnSubscribeEventTopic();
                        _selectiveEventsSubscription = null;
                    }

                    if (pool == null)
                        continue;
                    #endregion

                    int i = type == EventTypeInternal.ItemUpdated ? 0 : 1;
                    id = pool.GetResource(callback);
                    if (id is short)
                    {
                        returnValue[i] = (short)id;
                    }

                }
            }

            return returnValue;
        }

        
        internal EventHandle UnregisterDiscriptor(CacheEventDescriptor discriptor)
        {
            if (discriptor == null || !discriptor.IsRegistered)
                return null;

            foreach (EventType type in Enum.GetValues(typeof(EventType)))
            {
                ResourcePool pool = null;

                #region Pool selection

                if ((type & discriptor.RegisteredAgainst) != 0)
                    pool = GetEventPool(EventsUtil.GetEventTypeInternal(type));

                if (pool == null)
                    continue;
                #endregion

                short registrationSequenceId = -1;
                bool unregisterNotification = false;
                EventDataFilter maxDataFilter = EventDataFilter.None;

                lock (SyncLockGeneral)
                {
                    object retVal = pool.RemoveResource(discriptor);

                    if (retVal == null) continue;
                    unregisterNotification = pool.Count == 0;

                    if (!unregisterNotification)
                    {
                        object[] pooledDescriptors = pool.GetAllResourceKeys();

                        if (pooledDescriptors != null)
                        {
                            for (int i = 0; i < pooledDescriptors.Length; i++)
                            {
                                CacheEventDescriptor pooledDescriptor = pooledDescriptors[i] as CacheEventDescriptor;

                                if (pooledDescriptor.DataFilter > maxDataFilter)
                                    maxDataFilter = pooledDescriptor.DataFilter;

                            }
                        }
                    }


                    discriptor.IsRegistered = false;

                    //keeps a sequence number
                    switch (type)
                    {
                        case EventType.ItemAdded:
                            //Data filter is being updated
                            if (maxDataFilter != _generalAddDataFilter)
                            {
                                _generalAddDataFilter = maxDataFilter;
                                registrationSequenceId = ++_addEventRegistrationSequence;
                            }
                            if (unregisterNotification) _generalAddDataFilter = EventDataFilter.None;
                            break;
                        case EventType.ItemRemoved:
                            if (maxDataFilter != _generalRemoveDataFilter)
                            {
                                _generalRemoveDataFilter = maxDataFilter;
                                registrationSequenceId = ++_removeEventRegistrationSequenceId;
                            }
                            if (unregisterNotification) _generalAddDataFilter = EventDataFilter.None;
                            break;
                        case EventType.ItemUpdated:
                            if (maxDataFilter != _generalUpdateDataFilter)
                            {
                                _generalUpdateDataFilter = maxDataFilter;
                                registrationSequenceId = ++_updateEventRegisrationSequenceId;
                            }
                            if (unregisterNotification) _generalAddDataFilter = EventDataFilter.None;

                            break;
                    }
                }

                if (_cache != null)
                {
                    if (unregisterNotification)
                    {
                        //client is no more interested in event, therefore unregister it from server
                        _cache.UnregiserGeneralCacheNotification(EventsUtil.GetEventTypeInternal(type));
                   
                    }
                    else if (registrationSequenceId != -1)
                    {
                        //only caused update of data filter either upgrade or downgrade
                        _cache.RegisterCacheNotificationDataFilter(EventsUtil.GetEventTypeInternal(type), maxDataFilter, registrationSequenceId);
                    }
                }
            }
            return null;

        }

        public EventRegistrationInfo[] GetEventRegistrationInfo()
        {
            List<EventRegistrationInfo> registeredEvents = new List<EventRegistrationInfo>();

            lock (SyncLockGeneral)
            {
                if (_addEventPool != null && _addEventPool.Count > 0)
                {
                    registeredEvents.Add(new EventRegistrationInfo(EventTypeInternal.ItemAdded, _generalAddDataFilter, _addEventRegistrationSequence));
                }
                if (_updateEventPool != null && _updateEventPool.Count > 0)
                {
                    registeredEvents.Add(new EventRegistrationInfo(EventTypeInternal.ItemUpdated, _generalUpdateDataFilter, _updateEventRegisrationSequenceId));
                }
                if (_removeEventPool != null && _removeEventPool.Count > 0)
                {
                    registeredEvents.Add(new EventRegistrationInfo(EventTypeInternal.ItemRemoved, _generalRemoveDataFilter, _removeEventRegistrationSequenceId));
                }
            }
            return registeredEvents.ToArray();
        }

        private ResourcePool GetEventPool(EventTypeInternal eventType)
        {
            ResourcePool pool = null;

            if ((eventType & EventTypeInternal.ItemAdded) != 0)
                pool = _addEventPool;
            else if ((eventType & EventTypeInternal.ItemRemoved) != 0)
                pool = _removeEventPool;
            else if ((eventType & EventTypeInternal.ItemUpdated) != 0)
                pool = _updateEventPool;

            return pool;
        }

        private static void EndAsyncCallback(IAsyncResult arr)
        {
            CacheDataNotificationCallback subscribber = (CacheDataNotificationCallback)arr.AsyncState;

            try
            {
                subscribber.EndInvoke(arr);
            }
            catch (Exception e)
            {
            }
        }

        #region Event Message received handlers

        /// <summary>
        /// Handles what to do when a general event message is received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        internal void OnGeneralEventMessageReceived(object sender, MessageEventArgs args)
        {
            MessageItemType type = GetMessageItemType(args);

            switch (type)
            {
                case MessageItemType.MessageEventItem:
                    OnGeneralEventMessageReceived((MessageEventItem)args.Message.Payload);
                    break;
                case MessageItemType.MessageEventItems:
                    MessageEventItem[] messages = args.Message.Payload as MessageEventItem[];
                    if (messages != null)
                    {
                        foreach (MessageEventItem item in messages)
                        {
                            OnGeneralEventMessageReceived(item);
                        }
                    }
                    break;
            }
            
        }
        internal void OnGeneralEventMessageReceived(MessageEventItem eventMessage)
        {

            string key = eventMessage.Key;
            switch (eventMessage.EventType)
            {
                case Alachisoft.NCache.Persistence.EventType.ITEM_ADDED_EVENT:
                    RaiseGeneralCacheNotification(key, EventType.ItemAdded, eventMessage.Item, null, CacheItemRemovedReason.Underused, false);
                    break;
                case Alachisoft.NCache.Persistence.EventType.ITEM_UPDATED_EVENT:
                    RaiseGeneralCacheNotification(key, EventType.ItemUpdated, eventMessage.Item, eventMessage.OldItem, CacheItemRemovedReason.Underused, false);
                    break;
                case Alachisoft.NCache.Persistence.EventType.ITEM_REMOVED_EVENT:
                    RaiseGeneralCacheNotification(key, EventType.ItemRemoved, eventMessage.Item, null, eventMessage.Reason, false);
                    break;
            }
        }
        /// <summary>
        /// Handles what to do when a selective event message is received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        internal void OnSelectiveEventMessageReceived(object sender, MessageEventArgs args)
        {
            MessageItemType type  = GetMessageItemType(args);

            switch (type)
            {
                case MessageItemType.MessageEventItem:
                    OnSelectiveEventsMessageRecieved((MessageEventItem)args.Message.Payload);
                    break;
                case MessageItemType.MessageEventItems:
                    MessageEventItem[] messages = args.Message.Payload as MessageEventItem[];
                    if (messages !=null)
                    {
                        foreach (MessageEventItem item in messages)
                        {
                            OnSelectiveEventsMessageRecieved(item);
                        }
                    }
                    break;
            } 
        }

       

        internal void OnSelectiveEventsMessageRecieved(MessageEventItem eventMessage)
        {
            
            string key = eventMessage.Key;
            CallbackInfo cbInfo = new CallbackInfo(null, eventMessage.Callback, eventMessage.DataFilter);
            EventHandle handle = null;

            if (cbInfo != null)
            {
                short handler = (short)cbInfo.Callback;
                handle = new EventHandle(handler);
            }

            switch (eventMessage.EventType)
            {
                case Alachisoft.NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK:
                    RaiseSelectiveCacheNotification(key, EventType.ItemUpdated, eventMessage.Item, eventMessage.OldItem, CacheItemRemovedReason.Underused, false, handle, cbInfo.DataFilter);
                    break;
                case Alachisoft.NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK:
                    RaiseSelectiveCacheNotification(key, EventType.ItemRemoved, eventMessage.Item, null, eventMessage.Reason, false, handle, cbInfo.DataFilter);
                    break;
            }
            
        }

       
        #endregion

        #region /                       --- Inner Classes ---                                               /

        class SelectiveRemoveCallbackWrapper
        {
            CacheItemRemovedCallback _callback;
            CacheDataNotificationCallback _mappingCallback;

            public SelectiveRemoveCallbackWrapper(CacheItemRemovedCallback callback)
            {
                _callback = callback;
            }

            public void OnCacheDataNotification(string key, CacheEventArg arg)
            {
                if (_callback != null)
                    _callback(key, arg.Item.GetValue<object>(), arg.CacheItemRemovedReason);
            }

            public CacheDataNotificationCallback MappingCallback
            {
                get
                {
                    if (_mappingCallback == null) _mappingCallback = new CacheDataNotificationCallback(OnCacheDataNotification);
                    return _mappingCallback;
                }
            }
        }

        class SelectiveUpdateCallbackWrapper
        {
            CacheItemUpdatedCallback _callback;
            CacheDataNotificationCallback _mappingCallback;

            public SelectiveUpdateCallbackWrapper(CacheItemUpdatedCallback callback)
            {
                _callback = callback;
            }

            public void OnCacheDataNotification(string key, CacheEventArg arg)
            {
                if (_callback != null)
                    _callback(key);
            }

            public CacheDataNotificationCallback MappingCallback
            {
                get
                {
                    if (_mappingCallback == null) _mappingCallback = new CacheDataNotificationCallback(OnCacheDataNotification);
                    return _mappingCallback;
                }
            }
        }

        internal class EventRegistrationInfo
        {
            private EventTypeInternal _eventType;
            private EventDataFilter _filter;
            private short _registrationSequence;

            public EventRegistrationInfo() { }

            public EventRegistrationInfo(EventTypeInternal eventTYpe, EventDataFilter filter, short sequenceId)
            {
                _eventType = eventTYpe;
                _filter = filter;
                _registrationSequence = sequenceId;
            }

            public EventTypeInternal EventTYpe
            {
                get { return _eventType; }
                set { _eventType = value; }
            }

            public EventDataFilter DataFilter
            {
                get { return _filter; }
                set { _filter = value; }
            }

            public short RegistrationSequence
            {
                get { return _registrationSequence; }
                set { _registrationSequence = value; }
            }
        }
        #endregion
    }
}
