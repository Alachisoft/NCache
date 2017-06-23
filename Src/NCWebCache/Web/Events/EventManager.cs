// Copyright (c) 2017 Alachisoft
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
        System.Threading.WaitCallback waitC = new System.Threading.WaitCallback(Fire);

        string _cacheName = null;
        private  NCacheLogger _logger;

        private object sync_lock_selective = new object();
        private object sync_lock_general = new object();

        private static AsyncCallback asyn = new System.AsyncCallback(EndAsyncCallback);
        private Cache _cache;

        #region Resource Pools

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

        #endregion

        internal EventManager(string cacheName, NCacheLogger logger,Cache cache)
        {
            _cacheName = cacheName;
            _logger = logger;
            _cache = cache;
        }

        internal short AddSequenceNumber    { get { return _addEventRegistrationSequence; } }

        internal short UpdateSequenceNumber { get { return _updateEventRegisrationSequenceId; } }

        internal short RemoveSequenceNumber { get { return _removeEventRegistrationSequenceId; } }

        internal object SyncLockGeneral   { get { return sync_lock_general; } }
        internal object SyncLockSelective { get { return sync_lock_selective; } }


        /// <summary>
        /// Registeres the callback sepeartely and returns short values of registeredCallbacks
        /// </summary>
        /// <param name="key"></param>
        /// <param name="callback"></param>
        /// <param name="eventType"></param>
        /// <param name="datafilter"></param>
        /// <returns>short array,<para>1st element is updated callbackRef</para><para>2st element is removed callbackRef</para></returns>
        internal short[] RegisterSelectiveEvent(CacheDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter)
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
                
                
                return RegisterSelectiveDiscriptor(callback, eventType);

            }
            else
                return null;
        }

 


        internal void UnregisterAll()
        {
        }

        private CacheEventArg CreateCacheEventArgument(EventDataFilter dataFilter, string key,string cacheName,EventType eventType,EventCacheItem item,EventCacheItem oldItem,CacheItemRemovedReason removedReason)
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
        internal void RaiseSelectiveCacheNotification(string key, EventType eventType, EventCacheItem item, EventCacheItem oldItem, CacheItemRemovedReason reason, bool _notifyAsync, EventHandle eventhandle,EventDataFilter dataFilter)
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
                
                CacheDataNotificationCallback callback = poolID.GetResource((short)eventhandle.Handle) as CacheDataNotificationCallback;

                if (callback == null) //Can occur if Unregistered concurrently
                    return;

                if (_notifyAsync)
                    System.Threading.ThreadPool.QueueUserWorkItem(waitC, new object[] { callback, key, arg}); //Faster and better
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
                object[] objArray = (object[])obj;
                ((CacheDataNotificationCallback)objArray[0]).Invoke((string)objArray[1], (CacheEventArg)objArray[2]);
            }
            catch (Exception)
            {

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
            short[] returnValue = new short[] { -1, -1 }; //First value update callback ref & sencond is remove callbackref
            
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
                            
                            returnValue[i] = type == EventType.ItemUpdated ? ++_selectiveUpdateCallbackRef : ++_selectveRemoveCallbackRef;
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
            return returnValue;
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

            short[] returnValue = new short[] { -1, -1 }; //First value update callback ref & sencond is remove callbackref


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
                        returnValue[i] = (short)id;
                    }

                }
            }
            return returnValue;
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

        #region /                       --- Inner Classes ---                                               /

        internal class EventRegistrationInfo
        {
            private EventType _eventType;
            private EventDataFilter _filter;
            private short _registrationSequence;

            public EventRegistrationInfo() { }
           
            public EventRegistrationInfo(EventType eventTYpe,EventDataFilter filter,short sequenceId)
            {
                _eventType = eventTYpe;
                _filter = filter;
                _registrationSequence = sequenceId;
            }

            public EventType EventTYpe
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
