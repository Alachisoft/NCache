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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Caching
{
    [Serializable]
    public class CallbackInfo : ICompactSerializable, ISizable 
    {
        protected string theClient;
        protected object theCallback;
        protected bool notifyOnItemExpiration = true;

        protected EventDataFilter _dataFilter = EventDataFilter.None;
        protected CallbackType _callbackType = CallbackType.PushBasedNotification;

        [Obsolete("data filter required", true)]
        public CallbackInfo() { }

        [Obsolete("data filter required", true)]
        public CallbackInfo(string client, object callback, CallbackType callbackType = CallbackType.PushBasedNotification)
            : this(client, callback, true, callbackType)

        {
            
        }

        [Obsolete("data filter required", true)]
        public CallbackInfo(string client, object callback, bool notifyOnItemExpiration, CallbackType callbackType = CallbackType.PushBasedNotification)
            : this(client, callback, EventDataFilter.None, notifyOnItemExpiration, callbackType)


        {
        }

        public CallbackInfo(string client, object callback, EventDataFilter datafilter, CallbackType callbackType = CallbackType.PushBasedNotification)
            : this(client, callback, datafilter, true, callbackType)

        {
        }

        public CallbackInfo(string client, object callback, EventDataFilter datafilter, bool notifyOnItemExpiration, CallbackType callbackType = CallbackType.PushBasedNotification)

        {
            this.theClient = client;
            this.theCallback = callback;
            this.notifyOnItemExpiration = notifyOnItemExpiration;
            this._dataFilter = datafilter;
            this._callbackType = callbackType;

        }


        public EventDataFilter DataFilter
        {
            get { return _dataFilter; }
            set { _dataFilter = value; }
        }

        /// <summary>
        /// Gets/sets the client intended to listen for the event
        /// </summary>
        public string Client
        {
            get { return theClient; }
            set { theClient = value; }
        }

        /// <summary>
        /// Gets/sets the callback.
        /// </summary>
        public object Callback
        {
            get { return theCallback; }
            set { theCallback = value; }
        }

        /// <summary>
        /// Gets the flag which indicates whether client is interested in itemRemovedNotification
        /// when expired due to Time based expiration.
        /// </summary>
        public bool NotifyOnExpiration
        {
            get { return notifyOnItemExpiration; }
        }

        /// <summary>
        /// Gets the type of the notification for callback
        /// </summary>
        public CallbackType CallbackType
        {
            get { return _callbackType; }
        }


        /// <summary>
        /// Compares on Callback and DataFilter
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is CallbackInfo)
            {
                CallbackInfo other = obj as CallbackInfo;
                if (other.Client != theClient) return false;
                if (other.Callback is short && theCallback is short)
                {
                    if ((short)other.Callback == (short)theCallback)
                    {
                        return true;                        
                    }
                    else
                        return false;

                }
                else if (other.Callback == theCallback)
                {                    
                    return true;                    
                }
                else
                    return true;
            }
            return false;
        }

        public override string ToString()
        {
            string cnt = theClient != null ? theClient : "NULL";
            string cback = theCallback != null ? theCallback.ToString() : "NULL";
            string dataFilter = _dataFilter.ToString();
            return cnt + ":" + cback + ":" + dataFilter;
        }
        
        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            theClient = (string)reader.ReadObject();
            theCallback = reader.ReadObject();
            notifyOnItemExpiration = reader.ReadBoolean();
            _dataFilter = (EventDataFilter)reader.ReadByte();
            _callbackType = (CallbackType)reader.ReadObject();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(theClient);
            writer.WriteObject(theCallback);
            writer.Write(notifyOnItemExpiration);
            writer.Write((byte)_dataFilter);
            writer.WriteObject(_callbackType);
        }

        #endregion

        public int Size
        {
            get { return CallbackInfoSize; }
        }

        public int InMemorySize
        {
            get { return Common.MemoryUtil.GetInMemoryInstanceSize(this.Size);}
        }

        private int CallbackInfoSize
        {
            get 
            {
                int temp = 0;
                temp += Common.MemoryUtil.GetStringSize(theClient); // for theClient
                temp += Common.MemoryUtil.NetReferenceSize; // for theCallback
                temp += Common.MemoryUtil.NetByteSize; // for notifyOnItemExpiration
                temp += Common.MemoryUtil.NetEnumSize;  //for _dataFilter
                return temp;
            }
        }
    }
}