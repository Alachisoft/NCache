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
using System.Collections;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching
{
    public class CompactCacheEntry : ICompactSerializable, IRentableObject
    {
        private object _key;
        private object _value;
        private BitSet _flag;
        private ExpirationHint _dependency;
        private long _expiration;
        private byte _options;
        private object _itemRemovedCallback;
        private int _rentId;
        private Hashtable _queryInfo;
        private object _lockId;
        private LockAccessType _accessType;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="dependency"></param>
        /// <param name="expiration"></param>
        /// <param name="options"></param>
        /// <param name="itemRemovedCallback"></param>
        /// <param name="queryInfo"></param>
        /// <param name="Flag"></param>
        /// <param name="lockId"></param>
        /// <param name="accessType"></param>
        /// <param name="providername"></param>
        /// <param name="resyncProviderName"></param>
        /// <param name="exh"></param>
        /// <param name="priority"></param>
        [CLSCompliant(false)]
        public CompactCacheEntry(object key, object value, ExpirationHint dependency, 
            long expiration, 
            byte options, object itemRemovedCallback, Hashtable queryInfo, BitSet Flag, object lockId, LockAccessType accessType)
        {
            _key = key;
            _flag = Flag;
            _value = value;
            _dependency = dependency;
            _expiration = expiration;
            _options = options;
            _itemRemovedCallback = itemRemovedCallback;
            _queryInfo = queryInfo;
            _lockId = lockId;
            _accessType = accessType;
        }

        public CompactCacheEntry() { }

        /// <summary>
        /// 
        /// </summary>
        public object Key { get { return _key; } }

        /// <summary>
        /// 
        /// </summary>
        public object Value { get { return _value; } }

        /// <summary>
        /// 
        /// </summary>
        public BitSet Flag { get { return _flag; } }
        /// <summary>
        /// 
        /// </summary>
        public long Expiration { get { return _expiration; } }

        public object LockId
        {
            get { return _lockId; }
        }

        public LockAccessType LockAccessType
        {
            get { return _accessType; }
        }

        /// <summary>
        /// 
        /// </summary>
        public byte Options { get { return _options; } }

        /// <summary>
        /// 
        /// </summary>
        public object Callback { get { return _itemRemovedCallback; } }


        public Hashtable QueryInfo { get { return _queryInfo; } }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _key = reader.ReadObject();
            _value = reader.ReadObject();
            _expiration = reader.ReadInt64();
            _dependency = ExpirationHint.ReadExpHint(reader);
            _options = reader.ReadByte();
            _itemRemovedCallback = reader.ReadObject();
            _queryInfo = (Hashtable)reader.ReadObject();
        }

        public void Serialize(CompactWriter writer)
        {
            try
            {
                writer.WriteObject(_key);
                writer.WriteObject(_value);
                writer.Write(_expiration);
                ExpirationHint.WriteExpHint(writer, _dependency);
                writer.Write(_options);
                writer.WriteObject(_itemRemovedCallback);
                writer.WriteObject(_queryInfo);
            }
            catch (Exception) { throw; }
        }

        #endregion

        #region IRentableObject Members

        public int RentId
        {
            get
            {
                return _rentId;
            }
            set
            {
                _rentId = value;
            }
        }

        public void Reset()
        {
            _key = null;
            _value = null;
            _dependency = null;
            _expiration = 0;
            _options = 0;
            _itemRemovedCallback = null;
            _queryInfo = null;

        }
        #endregion
    }
}
