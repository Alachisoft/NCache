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
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Caching.Pooling;

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// Internal class used to hold the object as well as 
    /// eviction and expiration data.
    /// </summary>

    /// <summary>
    /// CallbackEntry represents an item with callback.
    /// </summary>
    [Serializable]
    public class Notifications : BookKeepingLease, ICompactSerializable, ICloneable, ISizable
    {
        private object _onAsyncOperationCompleteCallback;
        private object _onWriteBehindOperationCompletedCallback;
        
        protected ArrayList _itemRemovedListener = ArrayList.Synchronized(new ArrayList(2));
        protected ArrayList _itemUpdateListener = ArrayList.Synchronized(new ArrayList(2));

        public Notifications() { }

        /// <summary>
        /// Creates a CallBackEntry.
        /// </summary>
        /// <param name="callerid">Caller id i.e. Clietn application id</param>
        /// <param name="value">Actual data</param>
        /// <param name="onCacheItemRemovedCallback">OnCacheItemRemovedCallback</param>
        /// <param name="onCacheItemUpdateCallback">OnCacheItemUpdateCallback</param>
        public Notifications(string clientid, int reqId, short onCacheItemRemovedCallback, short onCacheItemUpdateCallback, short onAsyncOperationCompleteCallback, short onWriteBehindOperationCompletedCallback, EventDataFilter updateDatafilter, EventDataFilter removeDatafilter, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            if (onCacheItemUpdateCallback != -1)
            {
                _itemUpdateListener.Add(new CallbackInfo(clientid, onCacheItemUpdateCallback, updateDatafilter, callbackType));
            }
            if (onCacheItemRemovedCallback != -1)
            {
                _itemRemovedListener.Add(new CallbackInfo(clientid, onCacheItemRemovedCallback, removeDatafilter, callbackType));
            }
            if (onAsyncOperationCompleteCallback != -1)
            {
                _onAsyncOperationCompleteCallback = new AsyncCallbackInfo(reqId, clientid, onAsyncOperationCompleteCallback);
            }
            if (onWriteBehindOperationCompletedCallback != -1)
            {
                _onWriteBehindOperationCompletedCallback = new AsyncCallbackInfo(reqId, clientid, onWriteBehindOperationCompletedCallback);
            }
        }

        /// <summary>
        /// Creates a CallBackEntry.
        /// </summary>
        /// <param name="callerid">Caller id i.e. Clietn application id</param>
        /// <param name="value">Actual data</param>
        /// <param name="onCacheItemRemovedCallback">OnCacheItemRemovedCallback</param>
        /// <param name="onCacheItemUpdateCallback">OnCacheItemUpdateCallback</param>
        public Notifications(string clientid, int reqId, short onCacheItemRemovedCallback, short onCacheItemUpdateCallback, short onAsyncOperationCompleteCallback, EventDataFilter updateDatafilter, EventDataFilter removeDatafilter/*, short onWriteBehindOperationCompletedCallback*/)
        {
            if (onCacheItemUpdateCallback != -1)
            {
                _itemUpdateListener.Add(new CallbackInfo(clientid, onCacheItemUpdateCallback, updateDatafilter));
            }
            if (onCacheItemRemovedCallback != -1)
            {
                _itemRemovedListener.Add(new CallbackInfo(clientid, onCacheItemRemovedCallback, removeDatafilter));
            }
            if (onAsyncOperationCompleteCallback != -1)
            {
                _onAsyncOperationCompleteCallback = new AsyncCallbackInfo(reqId, clientid, onAsyncOperationCompleteCallback);
            }
        }
        /// <summary>
        /// Creates a CallBackEntry.
        /// </summary>
        /// <param name="callerid">Caller id i.e. Clietn application id</param>
        /// <param name="value">Actual data</param>
        /// <param name="onCacheItemRemovedCallback">OnCacheItemRemovedCallback</param>
        /// <param name="onCacheItemUpdateCallback">OnCacheItemUpdateCallback</param>
        public Notifications(CallbackInfo onCacheItemRemovedCallback, CallbackInfo onCacheItemUpdateCallback, AsyncCallbackInfo onAsyncOperationCompleteCallback, AsyncCallbackInfo onWriteBehindOperationCompletedCallback)
        {
            if (onCacheItemRemovedCallback != null) _itemRemovedListener.Add(onCacheItemRemovedCallback);
            if (onCacheItemUpdateCallback != null) _itemUpdateListener.Add(onCacheItemUpdateCallback);

            _onAsyncOperationCompleteCallback = onAsyncOperationCompleteCallback;
            _onWriteBehindOperationCompletedCallback = onWriteBehindOperationCompletedCallback;
        }

        public void AddItemRemoveCallback(string clientid, object callback, EventDataFilter datafilter)
        {
            AddItemRemoveCallback(new CallbackInfo(clientid, callback, datafilter));
        }
        public void AddItemRemoveCallback(CallbackInfo cbInfo,bool keepOldFilter = false)
        {
            if (_itemRemovedListener != null)
            {
                int indexOfCallback = _itemRemovedListener.IndexOf(cbInfo);
                if (indexOfCallback != -1)
                {
                    //update the data filter only
                    CallbackInfo oldCallback = _itemRemovedListener[indexOfCallback] as CallbackInfo;
                    if(!keepOldFilter) oldCallback.DataFilter = cbInfo.DataFilter;
                }
                else
                {
                    _itemRemovedListener.Add(cbInfo);
                }
            }
        }
        public void RemoveItemRemoveCallback(CallbackInfo cbInfo)
        {
            if (_itemRemovedListener != null && _itemRemovedListener.Contains(cbInfo))
            {
                _itemRemovedListener.Remove(cbInfo);
            }
        }
        public void AddItemUpdateCallback(string clientid, object callback, EventDataFilter datafilter)
        {
            AddItemUpdateCallback(new CallbackInfo(clientid, callback, datafilter));
        }

        public void AddItemUpdateCallback(CallbackInfo cbInfo,bool keepOldFilter =false)
        {
            if (_itemUpdateListener != null)
            {
                int indexOfCallback = _itemUpdateListener.IndexOf(cbInfo);
                if (indexOfCallback != -1)
                {
                    //update the data filter only
                    CallbackInfo oldCallback = _itemUpdateListener[indexOfCallback] as CallbackInfo;
                    if(!keepOldFilter) oldCallback.DataFilter = cbInfo.DataFilter;
                }
                else
                {
                    _itemUpdateListener.Add(cbInfo);
                }
            }

        }

        public void RemoveItemUpdateCallback(CallbackInfo cbInfo)
        {
            if (_itemUpdateListener != null && _itemUpdateListener.Contains(cbInfo))
            {
                _itemUpdateListener.Remove(cbInfo);
            }
        }

        public ArrayList ItemUpdateCallbackListener
        {
            get { return _itemUpdateListener; }
        }
        public ArrayList ItemRemoveCallbackListener
        {
            get { return _itemRemovedListener; }
            set { _itemRemovedListener = value; }
        }

        /// <summary>
        /// Gets Caller id i.e. Clietn application id
        /// </summary>
    
        public object AsyncOperationCompleteCallback
        {
            get { return _onAsyncOperationCompleteCallback; }
            set { _onAsyncOperationCompleteCallback = value; }
        }

        public object WriteBehindOperationCompletedCallback
        {
            get { return _onWriteBehindOperationCompletedCallback; }
            set { _onWriteBehindOperationCompletedCallback = value; }
        }
      
        #region ICompactSerializable Members
        /// <summary>
        /// Deserializes the CallbackEntry.
        /// </summary>
        /// <param name="reader"></param>
        public void Deserialize(CompactReader reader)
        {
            ArrayList list = reader.ReadObject() as ArrayList;
            if(list != null)
                _itemUpdateListener = ArrayList.Synchronized(list);
            list = reader.ReadObject() as ArrayList;
            if(list != null)
                _itemRemovedListener = ArrayList.Synchronized(list);

            _onAsyncOperationCompleteCallback = reader.ReadObject();
            _onWriteBehindOperationCompletedCallback = reader.ReadObject();
        }

        /// <summary>
        /// Serializes the CallbackEntry
        /// </summary>
        /// <param name="writer"></param>
        public void Serialize(CompactWriter writer)
        {
            lock (_itemUpdateListener.SyncRoot)
            {
                writer.WriteObject(_itemUpdateListener);
            }
            lock (_itemRemovedListener.SyncRoot)
            {
                writer.WriteObject(_itemRemovedListener);
            }
            writer.WriteObject(_onAsyncOperationCompleteCallback);
            writer.WriteObject(_onWriteBehindOperationCompletedCallback);
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            Notifications cloned = new Notifications();
            cloned._itemRemovedListener = this._itemRemovedListener.Clone() as ArrayList;
            cloned._itemUpdateListener = this._itemUpdateListener.Clone() as ArrayList;
            cloned._onAsyncOperationCompleteCallback = this._onAsyncOperationCompleteCallback;
            cloned._onWriteBehindOperationCompletedCallback = this._onWriteBehindOperationCompletedCallback;
            return cloned;
        }

        #endregion

        #region ISizable Members
        public int Size
        {
            get 
            {
                return CallbackEntrySize;
            }
        }

        public int InMemorySize
        {
            get 
            {
                return Common.MemoryUtil.GetInMemoryInstanceSize(this.Size);
            }
        }

        private int CallbackEntrySize
        {
            get
            {
                int temp = 0;                
                temp += Common.MemoryUtil.NetReferenceSize; // for _onAsyncOperationCompleteCallback
                temp += Common.MemoryUtil.NetReferenceSize; // for _onWriteBehindOperationCompletedCallback
                temp += Common.MemoryUtil.NetReferenceSize; // for _itemRemovedLisetner
                temp += Common.MemoryUtil.NetReferenceSize; // for _itemUpdatedLisetner
              
                if (_itemRemovedListener != null)
                {
                    temp += _itemRemovedListener.Count * Common.MemoryUtil.NetListOverHead;
                    foreach (CallbackInfoBase cbInfo in _itemRemovedListener)
                    {
                        temp += cbInfo.Size;
                    }
                }
                if (_itemUpdateListener != null)
                {
                    temp += _itemRemovedListener.Count * Common.MemoryUtil.NetListOverHead;
                    foreach (CallbackInfoBase cbInfo in _itemUpdateListener)
                    {
                        temp += cbInfo.Size;
                    }
                }
                return temp;
            }
        }
        #endregion

        #region ILeasable

        public override void ResetLeasable()
        {
           
        }

        public override void ReturnLeasableToPool()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region - [Deep Cloning] -

        public Notifications DeepClone(PoolManager poolManager)
        {
            var clonedNotifications = poolManager.GetNotificationsPool()?.Rent(false) ?? new Notifications();
            clonedNotifications._itemRemovedListener = _itemRemovedListener.Clone() as ArrayList;
            clonedNotifications._itemUpdateListener = _itemUpdateListener.Clone() as ArrayList;
            clonedNotifications._onAsyncOperationCompleteCallback = _onAsyncOperationCompleteCallback;
            clonedNotifications._onWriteBehindOperationCompletedCallback = _onWriteBehindOperationCompletedCallback;
            return clonedNotifications;
        }

        #endregion
    }
}