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
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Logger;
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.MapReduce;
using Alachisoft.NCache.Web.MapReduce;
using System.Threading.Tasks;

namespace Alachisoft.NCache.Web.Caching
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

        public short MAPREDUCELISTENER = 7000;
        System.Threading.WaitCallback waitC = new System.Threading.WaitCallback(Fire);

        string _cacheName = null;
        private NCacheLogger _logger;

        private object sync_lock_selective = new object();
        private object sync_lock_general = new object();
        private object _syncMapReduce = new object();

        private PollNotificationCallback _pollPubSubCallback;


        private static AsyncCallback asyn = new System.AsyncCallback(EndAsyncCallback);
        private Cache _cache;

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
        private ResourcePool _mapReduceListenerPool = null;
        private ResourcePool _mapReduceListenerIDPool = null;
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

        #endregion

        internal EventManager(string cacheName, NCacheLogger logger, Cache cache)
        {
            _cacheName = cacheName;
            _logger = logger;
            _cache = cache;
        }

        internal short AddSequenceNumber
        {
            get { return _addEventRegistrationSequence; }
        }

        internal short UpdateSequenceNumber
        {
            get { return _updateEventRegisrationSequenceId; }
        }

        internal short RemoveSequenceNumber
        {
            get { return _removeEventRegistrationSequenceId; }
        }

        internal object SyncLockGeneral
        {
            get { return sync_lock_general; }
        }

        internal object SyncLockSelective
        {
            get { return sync_lock_selective; }
        }
        
        internal short GeneralEventRefCountAgainstEvent(EventType eventType)
        {
            if ((eventType & EventType.ItemAdded) != 0)
                return _addEventRegistrationSequence;
            if ((eventType & EventType.ItemRemoved) != 0)
                return _removeEventRegistrationSequenceId;
            if ((eventType & EventType.ItemUpdated) != 0)
                return _updateEventRegisrationSequenceId;

            return -1;
        }

        /// <summary>
        /// Returns the filter type of the eventType
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        internal EventDataFilter MaxFilterAgainstEvent(EventType eventType)
        {
            if ((eventType & EventType.ItemAdded) != 0)
                return _addDataFilter;
            if ((eventType & EventType.ItemRemoved) != 0)
                return _removeDataFilter;
            if ((eventType & EventType.ItemUpdated) != 0)
                return _updateDataFilter;

            return EventDataFilter.DataWithMetadata;
        }

        /// <summary>
        /// Registeres the callback sepeartely and returns short values of registeredCallbacks
        /// </summary>
        /// <param name="key"></param>
        /// <param name="callback"></param>
        /// <param name="eventType"></param>
        /// <param name="datafilter"></param>
        /// <returns>short array,<para>1st element is updated callbackRef</para><para>2st element is removed callbackRef</para></returns>
        internal short[] RegisterSelectiveEvent(CacheDataNotificationCallback callback, EventType eventType,
            EventDataFilter datafilter)
        {
            if (callback != null)
            {
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

                return RegisterSelectiveDiscriptor(callback, eventType);
            }
            else
                return null;
        }

        #region ----- MapReduce -----

        internal object SyncMapReduce
        {
            get { return _syncMapReduce; }
        }

        internal short RegisterMapReduceEvent(TaskListener listener)
        {
            if (listener != null)
            {
                if (_mapReduceListenerPool == null)
                {
                    _mapReduceListenerPool = new ResourcePool();
                    _mapReduceListenerIDPool = new ResourcePool();
                }

                return RegisterMRListenerDescriptor(listener);
            }

            return 0;
        }

        private short RegisterMRListenerDescriptor(TaskListener listener)
        {
            if (listener == null)
                return 0;

            short returnValue = 0;

            lock (SyncMapReduce)
            {
                ResourcePool pool = _mapReduceListenerPool;
                ResourcePool poolID = _mapReduceListenerIDPool;

                if (pool.GetResource(listener) == null)
                {
                    returnValue = ++MAPREDUCELISTENER;
                    pool.AddResource(listener, returnValue);
                    poolID.AddResource(returnValue, listener);
                }
                else
                {
                    short val = (short) pool.GetResource(listener);
                    if (val >= 0)
                    {
                        pool.AddResource(listener, returnValue);
                        poolID.AddResource(returnValue, listener);
                        returnValue = val;
                    }
                }
            }

            return returnValue;
        }

        internal void FireMapReduceCallback(string taskId, int taskstatus, string taskFailureReason, short callbackId)
        {
            TaskCompletionStatus status = TaskCompletionStatus.Success;
            switch (taskstatus)
            {
                case 0:
                    status = TaskCompletionStatus.Success;
                    break;
                case 1:
                    status = TaskCompletionStatus.Failure;
                    break;
                case 2:
                    status = TaskCompletionStatus.Cancelled;
                    break;
            }

            TaskResult resp = new TaskResult(status, taskId, callbackId, taskFailureReason);

            if (_mapReduceListenerIDPool != null)
            {
                ResourcePool poole = _mapReduceListenerIDPool;
                TaskListener callback = (TaskListener) poole.GetResource(callbackId);

                if (callback != null)
                    callback.Invoke(resp);
            }
        }

        #endregion

        internal short RegisterPollingEvent(PollNotificationCallback callback, EventType eventType)
        {
            if ((eventType & EventType.PubSub) != 0)
            {
                _pollPubSubCallback = callback;
            }

            return 10001;
        }

        internal CacheEventDescriptor RegisterGeneralEvents(CacheDataNotificationCallback callback, EventType eventType,
            EventDataFilter datafilter)
        {
            if (callback != null)
            {
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

                CacheEventDescriptor discriptor =
                    CacheEventDescriptor.CreateCacheDiscriptor(eventType, _cacheName, callback, datafilter);

                if (!RegisterGeneralDiscriptor(discriptor, eventType))
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
        internal void RaiseGeneralCacheNotification(string key, EventType eventType, EventCacheItem item,
            EventCacheItem oldItem, CacheItemRemovedReason reason, bool notifyAsync)
        {
            try
            {
                

                object[] registeredDiscriptors = null;

                ResourcePool eventPool = GetEventPool(eventType);
                if (eventPool != null)
                    registeredDiscriptors = eventPool.GetAllResourceKeys();

                if (registeredDiscriptors != null && registeredDiscriptors.Length > 0)
                {
                    for (int i = 0; i < registeredDiscriptors.Length; i++)
                    {
                        CacheEventDescriptor discriptor = registeredDiscriptors[i] as CacheEventDescriptor;

                        if (discriptor == null)
                            continue;

                        var arg = CreateCacheEventArgument(discriptor.DataFilter, key, _cacheName, eventType, item, oldItem,
                            reason);
                        arg.Descriptor = discriptor;

                        if (notifyAsync)
                        {
#if !NETCORE
                            discriptor.CacheDataNotificationCallback.BeginInvoke(key, arg, asyn, null);
#elif NETCORE
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

        internal void RaisePollNotification(short callbackId, EventType eventType)
        {
            try
            {
                // just invoke the callback if not null.
                // callbackId is of no use here.
                PollNotificationCallback _pollCallback = null;
                if ((eventType & EventType.PubSub) != 0)
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

        private CacheEventArg CreateCacheEventArgument(EventDataFilter dataFilter, string key, string cacheName,
            EventType eventType, EventCacheItem item, EventCacheItem oldItem, CacheItemRemovedReason removedReason)
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
        internal void RaiseSelectiveCacheNotification(string key, EventType eventType, EventCacheItem item,
            EventCacheItem oldItem, CacheItemRemovedReason reason, bool _notifyAsync, EventHandle eventhandle,
            EventDataFilter dataFilter)
        {
            try
            {
                ResourcePool poolID = null;

                CacheEventArg arg = null;

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

                CacheDataNotificationCallback callback =
                    poolID.GetResource((short) eventhandle.Handle) as CacheDataNotificationCallback;

                if (callback == null)
                    return;

                if (_notifyAsync)
                    System.Threading.ThreadPool.QueueUserWorkItem(waitC,
                        new object[] {callback, key, arg}); //Faster and better
                else
                    callback.Invoke(key, arg);
            }
            catch (Exception ex)
            {
                if (_logger != null && _logger.IsErrorEnabled) _logger.CriticalInfo(ex.ToString());
            }
        }

        private static void Fire(object obj)
        {
            try
            {
                object[] objArray = (object[]) obj;
                ((CacheDataNotificationCallback) objArray[0]).Invoke((string) objArray[1], (CacheEventArg) objArray[2]);
            }
            catch (Exception)
            {
            }
        }


        internal short RegisterSelectiveCallback(CacheItemRemovedCallback removedCallback)
        {
            if (removedCallback == null)
                return -1;
            lock (SyncLockSelective)
            {
                SelectiveRemoveCallbackWrapper callbackWrapper = null;
                if (_oldSelectiveCallbackPool.GetResource(removedCallback) == null)
                {
                    callbackWrapper = new SelectiveRemoveCallbackWrapper(removedCallback);
                    _oldSelectiveCallbackPool.AddResource(removedCallback, callbackWrapper);
                    _oldSelectiveMappingCallbackPool.AddResource(callbackWrapper, removedCallback);
                }
                else
                {
                    callbackWrapper =
                        (SelectiveRemoveCallbackWrapper) _oldSelectiveCallbackPool.GetResource(removedCallback);
                    _oldSelectiveCallbackPool.AddResource(removedCallback, callbackWrapper);
                }

                short[] callbackIds = RegisterSelectiveEvent(callbackWrapper.MappingCallback, EventType.ItemRemoved,
                    EventDataFilter.DataWithMetadata);
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
                //For selective callback, we dont remove the callback as it can create chaos if user try to unregister
                //a callback more then one time or against wrong items.
                callbackWrapper =
                    (SelectiveRemoveCallbackWrapper) _oldSelectiveCallbackPool.GetResource(removedCallback);

                if (callbackWrapper != null)
                {
                    short[] callbackIds =
                        UnregisterSelectiveNotification(callbackWrapper.MappingCallback, EventType.ItemRemoved);
                    return callbackIds[1];
                }

                return -1;
            }
        }

        internal short RegisterSelectiveCallback(CacheItemUpdatedCallback updateCallback)
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
                    callbackWrapper =
                        (SelectiveUpdateCallbackWrapper) _oldSelectiveCallbackPool.GetResource(updateCallback);
                    _oldSelectiveCallbackPool.AddResource(updateCallback, callbackWrapper); //to increment the refcount
                }

                short[] callbackIds = RegisterSelectiveEvent(callbackWrapper.MappingCallback, EventType.ItemUpdated,
                    EventDataFilter.None);
                return callbackIds[0];
            }
        }


        internal short UnRegisterSelectiveCallback(CacheItemUpdatedCallback updateCallback)
        {
            if (updateCallback == null)
                return -1;
            lock (SyncLockSelective)
            {
                SelectiveUpdateCallbackWrapper callbackWrapper =
                    (SelectiveUpdateCallbackWrapper) _oldSelectiveCallbackPool.GetResource(updateCallback);
                // For selective callback, we dont remove the callback from resource pool as it can create chaos if user try to unregister
                //a callback more then one time or against wrong items.
                if (callbackWrapper != null)
                {
                    short[] callbackIds = RegisterSelectiveEvent(callbackWrapper.MappingCallback, EventType.ItemUpdated,
                        EventDataFilter.None);
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
        private short[] RegisterSelectiveDiscriptor(CacheDataNotificationCallback callback, EventType eventType)
        {
            if (callback == null)
                return null; //FAIL CONDITION
            short[]
                returnValue = new short[] {-1, -1}; //First value update callback ref & sencond is remove callbackref

            foreach (EventType type in Enum.GetValues(typeof(EventType)))
            {
                if (type == EventType.ItemAdded) //ItemAdded not supported Yet
                    continue;

                lock (SyncLockSelective)
                {
                    ResourcePool pool = null;
                    ResourcePool poolID = null;

                    #region pool selection

                    if (type == EventType.ItemRemoved && (eventType & EventType.ItemRemoved) != 0)
                    {
                        pool = _selectiveRemoveEventPool;
                        poolID = _selectiveRemoveEventIDPool;
                    }
                    else if (type == EventType.ItemUpdated && (eventType & EventType.ItemUpdated) != 0)
                    {
                        pool = _selectiveUpdateEventPool;
                        poolID = _selectiveUpdateEventIDPool;
                    }

                    if (pool == null)
                        continue;

                    #endregion

                    while (true)
                    {
                        int i = type == EventType.ItemUpdated ? 0 : 1;
                        if (pool.GetResource(callback) == null)
                        {
                            returnValue[i] = type == EventType.ItemUpdated
                                ? ++_selectiveUpdateCallbackRef
                                : ++_selectveRemoveCallbackRef;
                            pool.AddResource(callback, returnValue[i]);
                            poolID.AddResource(returnValue[i], callback);
                            break;
                        }
                        else
                        {
                            try
                            {
                                short cref = (short) pool.GetResource(callback);
                                if (cref < 0)
                                    break;

                                //add it again into the table for updating ref count.
                                pool.AddResource(callback, cref);
                                poolID.AddResource(cref, callback);
                                returnValue[i] = cref;
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

            return returnValue;
        }

        private bool RegisterGeneralDiscriptor(CacheEventDescriptor discriptor, EventType eventType)
        {
            if (discriptor == null)
                return false; //FAIL CONDITION

            EventHandle handle = null;

            foreach (EventType type in Enum.GetValues(typeof(EventType)))
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
                        case EventType.ItemAdded:
                            if (discriptor.DataFilter > _generalAddDataFilter ||
                                _addEventRegistrationSequence == REFSTART)
                            {
                                registrationUpdated = true;
                                registrationSequenceId = ++_addEventRegistrationSequence;
                                _generalAddDataFilter = discriptor.DataFilter;
                            }
                            else
                                registrationSequenceId = _addEventRegistrationSequence;

                            break;
                        case EventType.ItemRemoved:
                            if (discriptor.DataFilter > _generalRemoveDataFilter ||
                                _removeEventRegistrationSequenceId == REFSTART)
                            {
                                registrationUpdated = true;
                                registrationSequenceId = ++_removeEventRegistrationSequenceId;
                                _generalRemoveDataFilter = discriptor.DataFilter;
                            }
                            else
                                registrationSequenceId = _removeEventRegistrationSequenceId;

                            break;
                        case EventType.ItemUpdated:
                            if (discriptor.DataFilter > _generalUpdateDataFilter ||
                                _updateEventRegisrationSequenceId == REFSTART)
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
        internal short[] UnregisterSelectiveNotification(CacheDataNotificationCallback callback, EventType eventType)
        {
            if (callback == null)
                return null;

            short[]
                returnValue = new short[] {-1, -1}; //First value update callback ref & sencond is remove callbackref


            foreach (EventType type in Enum.GetValues(typeof(EventType)))
            {
                if (type == EventType.ItemAdded) //ItemAdded not supported Yet
                    continue;

                object id = -1;

                lock (SyncLockSelective)
                {
                    ResourcePool pool = null;
                    ResourcePool poolID = null;

                    #region pool selection

                    if (type == EventType.ItemRemoved && (eventType & EventType.ItemRemoved) != 0)
                    {
                        pool = _selectiveRemoveEventPool;
                        poolID = _selectiveRemoveEventIDPool;
                    }
                    else if (type == EventType.ItemUpdated && (eventType & EventType.ItemUpdated) != 0)
                    {
                        pool = _selectiveUpdateEventPool;
                        poolID = _selectiveUpdateEventIDPool;
                    }

                    if (pool == null)
                        continue;

                    #endregion

                    // For selective callback, we dont remove the callback as it can create chaos if user try to unregister
                    //a callback more then one time or against wrong items.

                    int i = type == EventType.ItemUpdated ? 0 : 1;
                    id = pool.GetResource(callback);
                    if (id is short)
                    {
                        returnValue[i] = (short) id;
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
                    pool = GetEventPool(type);

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

                                if (maxDataFilter == EventDataFilter.DataWithMetadata) break;
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
                        _cache.UnregiserGeneralCacheNotification(type);
                    }
                    else if (registrationSequenceId != -1)
                    {
                        //only caused update of data filter either upgrade or downgrade
                        _cache.RegisterCacheNotificationDataFilter(type, maxDataFilter, registrationSequenceId);
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
                    registeredEvents.Add(new EventRegistrationInfo(EventType.ItemAdded, _generalAddDataFilter,
                        _addEventRegistrationSequence));
                }

                if (_updateEventPool != null && _updateEventPool.Count > 0)
                {
                    registeredEvents.Add(new EventRegistrationInfo(EventType.ItemUpdated, _generalUpdateDataFilter,
                        _updateEventRegisrationSequenceId));
                }

                if (_removeEventPool != null && _removeEventPool.Count > 0)
                {
                    registeredEvents.Add(new EventRegistrationInfo(EventType.ItemRemoved, _generalRemoveDataFilter,
                        _removeEventRegistrationSequenceId));
                }
            }

            return registeredEvents.ToArray();
        }

        private ResourcePool GetEventPool(EventType eventType)
        {
            ResourcePool pool = null;

            if ((eventType & EventType.ItemAdded) != 0)
                pool = _addEventPool;
            else if ((eventType & EventType.ItemRemoved) != 0)
                pool = _removeEventPool;
            else if ((eventType & EventType.ItemUpdated) != 0)
                pool = _updateEventPool;

            return pool;
        }

        private static void EndAsyncCallback(IAsyncResult arr)
        {
            CacheDataNotificationCallback subscribber = (CacheDataNotificationCallback) arr.AsyncState;

            try
            {
                subscribber.EndInvoke(arr);
            }
            catch (Exception e)
            {
            }
        }
    }
}