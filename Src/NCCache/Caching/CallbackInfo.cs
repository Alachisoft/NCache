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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching
{
    [Serializable]
    public class CallbackInfo : CallbackInfoBase
    {
        #region -------------------- Fields --------------------

        private EventDataFilter _dataFilter = EventDataFilter.None;

        #endregion

        #region ------------------ Properties ------------------

        public EventDataFilter DataFilter
        {
            get { return _dataFilter; }
            set { _dataFilter = value; }
        }

        #endregion

        #region ----------------- Constructors -----------------

        [Obsolete("data filter required", true)]
        public CallbackInfo()
            : this(default(string), default(object))
        {
        }

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
            : base(client, callback, notifyOnItemExpiration, callbackType)
        {
            _dataFilter = datafilter;
        }

        #endregion

        #region ------------------- ISizable -------------------

        public new int Size
        {
            get
            {
                int size = base.Size;
                size += MemoryUtil.NetEnumSize;     // For _dataFilter
                return size;
            }
        }

        #endregion

        #region ------------- ICompactSerializable -------------

        public override void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _dataFilter = (EventDataFilter)reader.ReadByte();
        }

        public override void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write((byte)_dataFilter);
        }

        #endregion

        #region ------------------- ToString() -----------------

        public override string ToString()
        {
            return base.ToString() + ":" + _dataFilter;
        }

        #endregion
    }
}
