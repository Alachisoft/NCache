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

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching
{
    [System.Serializable]
    public abstract class CallbackInfoBase : ICompactSerializable, ISizable
    {
        #region -------------------- Fields --------------------

        private string _client;
        private object _callback;
        private CallbackType _callbackType;
        private bool _notifyOnItemExpiration;

        #endregion

        #region ------------------ Properties ------------------

        /// <summary>
        /// Gets/sets the client intended to listen for the event.
        /// </summary>
        public string Client
        {
            get { return _client; }
            set { _client = value; }
        }

        /// <summary>
        /// Gets/sets the callback.
        /// </summary>
        public object Callback
        {
            get { return _callback; }
            set { _callback = value; }
        }

        /// <summary>
        /// Gets the flag which indicates whether client is interested in itemRemovedNotification
        /// when expired due to time-based expiration.
        /// </summary>
        public bool NotifyOnExpiration
        {
            get { return _notifyOnItemExpiration; }
        }

        /// <summary>
        /// Gets the type of the notification for callback.
        /// </summary>
        public CallbackType CallbackType
        {
            get { return _callbackType; }
        }

        #endregion

        #region ----------------- Constructors -----------------

        protected CallbackInfoBase(string client, object callback)
            : this(client, callback, true)
        {
        }

        protected CallbackInfoBase(string client, object callback, bool notifyOnItemExpiration)
            : this(client, callback, notifyOnItemExpiration, CallbackType.PushBasedNotification)
        {
        }

        protected CallbackInfoBase(string client, object callback, bool notifyOnItemExpiration, CallbackType callbackType)
        {
            _client = client;
            _callback = callback;
            _callbackType = callbackType;
            _notifyOnItemExpiration = notifyOnItemExpiration;
        }

        #endregion

        #region ------------------- ISizable -------------------

        public int Size
        {
            get
            {
                int size = 0;
                size += MemoryUtil.GetStringSize(_client);   // For _client
                size += MemoryUtil.NetReferenceSize;         // For _callback
                size += MemoryUtil.NetEnumSize;              // For _callbackType
                size += MemoryUtil.NetByteSize;              // For _notifyOnItemExpiration
                return size;
            }
        }

        public int InMemorySize
        {
            get
            {
                return MemoryUtil.GetInMemoryInstanceSize(Size);
            }
        }

        #endregion

        #region ------------- ICompactSerializable -------------

        public virtual void Deserialize(CompactReader reader)
        {
            _client = (string)reader.ReadObject();
            _callback = reader.ReadObject();
            _notifyOnItemExpiration = reader.ReadBoolean();
            _callbackType = (CallbackType)reader.ReadObject();
        }

        public virtual void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_client);
            writer.WriteObject(_callback);
            writer.Write(_notifyOnItemExpiration);
            writer.WriteObject(_callbackType);
        }

        #endregion

        #region ------------- Equals() - ToString() ------------

        /// <summary>
        /// Compares on Callback.
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as CallbackInfoBase;

            if (other == null)
                return false;

            if (other.Client != _client)
                return false;

            var thisCallback = _callback as short?;
            var otherCallback = other.Callback as short?;

            if (otherCallback != null && thisCallback != null)
                return otherCallback == thisCallback;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            string client = _client ?? "NULL";
            string callback = _callback != null ? _callback.ToString() : "NULL";
            return client + ":" + callback;
        }

        #endregion
    }
}
