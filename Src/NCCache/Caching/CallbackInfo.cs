// Copyright (c) 2015 Alachisoft
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

namespace Alachisoft.NCache.Caching
{
    [Serializable]
    public class CallbackInfo : ICompactSerializable
    {
        protected string theClient;
        protected object theCallback;
        protected bool notifyOnItemExpiration = true;

        protected EventDataFilter _dataFilter = EventDataFilter.None;


        public CallbackInfo(string client, object callback, EventDataFilter datafilter)
            : this(client, callback, datafilter, true)
        {
        }

        public CallbackInfo(string client, object callback, EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
            this.theClient = client;
            this.theCallback = callback;
            this.notifyOnItemExpiration = notifyOnItemExpiration;
            this._dataFilter = datafilter;
        }


        public EventDataFilter DataFilter
        {
            get { return _dataFilter; }
            set { _dataFilter = value; }
        }

        /// <summary>
        /// Gets/sets the client inteded to listen for the event
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
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(theClient);
            writer.WriteObject(theCallback);
            writer.Write(notifyOnItemExpiration);
            writer.Write((byte)_dataFilter);
        }

        #endregion
    }
}
