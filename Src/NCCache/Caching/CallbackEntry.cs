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
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

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
    public class CallbackEntry : ICompactSerializable, ICloneable, ISizable
    {
        private object _value;
        private BitSet _flag;
        
        private ArrayList _itemRemovedListener = ArrayList.Synchronized(new ArrayList(2));
        private ArrayList _itemUpdateListener = ArrayList.Synchronized(new ArrayList(2));

        public CallbackEntry() { }

        /// <summary>
        /// Creates a CallBackEntry.
        /// </summary>
        /// <param name="clientid"></param>
        /// <param name="reqId"></param>
        /// <param name="value">Actual data</param>
        /// <param name="onCacheItemRemovedCallback">OnCacheItemRemovedCallback</param>
        /// <param name="onCacheItemUpdateCallback">OnCacheItemUpdateCallback</param>
        /// <param name="Flag"></param>
        /// <param name="updateDatafilter"></param>
        /// <param name="removeDatafilter"></param>
        /// <param name="callerid">Caller id i.e. Clietn application id</param>
        public CallbackEntry(string clientid, int reqId, object value, short onCacheItemRemovedCallback, short onCacheItemUpdateCallback, BitSet Flag, EventDataFilter updateDatafilter, EventDataFilter removeDatafilter)
        {
            _value = value;
            _flag = Flag;
            if (onCacheItemUpdateCallback != -1)
            {
                _itemUpdateListener.Add(new CallbackInfo(clientid, onCacheItemUpdateCallback, updateDatafilter));
            }
            if (onCacheItemRemovedCallback != -1)
            {
                _itemRemovedListener.Add(new CallbackInfo(clientid, onCacheItemRemovedCallback, removeDatafilter));
            }
        }

        /// <summary>
        /// Creates a CallBackEntry.
        /// </summary>
        /// <param name="clientid"></param>
        /// <param name="reqId"></param>
        /// <param name="value">Actual data</param>
        /// <param name="onCacheItemRemovedCallback">OnCacheItemRemovedCallback</param>
        /// <param name="onCacheItemUpdateCallback">OnCacheItemUpdateCallback</param>
        /// <param name="updateDatafilter"></param>
        /// <param name="removeDatafilter"></param>
        /// <param name="callerid">Caller id i.e. Clietn application id</param>
        public CallbackEntry(string clientid, int reqId, object value, short onCacheItemRemovedCallback, short onCacheItemUpdateCallback, EventDataFilter updateDatafilter, EventDataFilter removeDatafilter)
        {
            _value = value;
            if (onCacheItemUpdateCallback != -1)
            {
                _itemUpdateListener.Add(new CallbackInfo(clientid, onCacheItemUpdateCallback, updateDatafilter));
            }
            if (onCacheItemRemovedCallback != -1)
            {
                _itemRemovedListener.Add(new CallbackInfo(clientid, onCacheItemRemovedCallback, removeDatafilter));
            }
        }

        public void AddItemRemoveCallback(string clientid, object callback, EventDataFilter datafilter)
        {
            AddItemRemoveCallback(new CallbackInfo(clientid, callback, datafilter));
        }
        public void AddItemRemoveCallback(CallbackInfo cbInfo)
        {
            if (_itemRemovedListener != null)
            {
                int indexOfCallback = _itemRemovedListener.IndexOf(cbInfo);
                if (indexOfCallback != -1)
                {
                    //update the data filter only
                    CallbackInfo oldCallback = _itemRemovedListener[indexOfCallback] as CallbackInfo;
                    oldCallback.DataFilter = cbInfo.DataFilter;
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

        public void AddItemUpdateCallback(CallbackInfo cbInfo)
        {
            if (_itemUpdateListener != null)
            {
                int indexOfCallback = _itemUpdateListener.IndexOf(cbInfo);
                if (indexOfCallback != -1)
                {
                    //update the data filter only
                    CallbackInfo oldCallback = _itemUpdateListener[indexOfCallback] as CallbackInfo;
                    oldCallback.DataFilter = cbInfo.DataFilter;
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
        }

        /// <summary>
        /// Gets Caller id i.e. Clietn application id
        /// </summary>
       
        /// <summary>
        /// Gets/Sets the actual object.
        /// </summary>
        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public Array UserData
        {
            get
            {
                Array userData = null;
                if (_value != null)
                {
                    userData = ((UserBinaryObject)_value).Data;
                }
                return userData;
            }
        }

        public BitSet Flag
        {
            get { return _flag; }
            set { _flag = value; }
        }

      
        #region ICompactSerializable Members
        /// <summary>
        /// Deserializes the CallbackEntry.
        /// </summary>
        /// <param name="reader"></param>
        public void Deserialize(CompactReader reader)
        {
            _value = reader.ReadObject();
            _flag = reader.ReadObject() as BitSet;
            ArrayList list = reader.ReadObject() as ArrayList;
            if(list != null)
                _itemUpdateListener = ArrayList.Synchronized(list);
            list = reader.ReadObject() as ArrayList;
            if(list != null)
                _itemRemovedListener = ArrayList.Synchronized(list);
        }

        /// <summary>
        /// Serializes the CallbackEntry
        /// </summary>
        /// <param name="writer"></param>
        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_value);
            writer.WriteObject(_flag);
            lock (_itemUpdateListener.SyncRoot)
            {
                writer.WriteObject(_itemUpdateListener);
            }
            lock (_itemRemovedListener.SyncRoot)
            {
                writer.WriteObject(_itemRemovedListener);
            }
           
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            CallbackEntry cloned = new CallbackEntry();
            cloned._flag = this._flag;
            cloned._value = this._value;
            cloned._itemRemovedListener = this._itemRemovedListener.Clone() as ArrayList;
            cloned._itemUpdateListener = this._itemUpdateListener.Clone() as ArrayList;
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
                temp += Common.MemoryUtil.NetReferenceSize; // for _value
                temp += Common.MemoryUtil.NetReferenceSize; // for _onAsyncOperationCompleteCallback
                temp += Common.MemoryUtil.NetReferenceSize; // for _onWriteBehindOperationCompletedCallback
                temp += Common.MemoryUtil.NetReferenceSize; // for _itemRemovedLisetner
                temp += Common.MemoryUtil.NetReferenceSize; // for _itemUpdatedLisetner

                temp += BitSet.Size; // for _flag

                if (_itemRemovedListener != null)
                {
                    temp += _itemRemovedListener.Count * Common.MemoryUtil.NetListOverHead;
                    foreach (CallbackInfo cbInfo in _itemRemovedListener)
                    {
                        temp += cbInfo.Size;
                    }
                }
                if (_itemUpdateListener != null)
                {
                    temp += _itemUpdateListener.Count * Common.MemoryUtil.NetListOverHead;
                    foreach (CallbackInfo cbInfo in _itemUpdateListener)
                    {
                        temp += cbInfo.Size;
                    }
                }
                return temp;
            }
        }
        #endregion
    }
}
