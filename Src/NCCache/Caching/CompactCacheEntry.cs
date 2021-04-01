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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.Locking;

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
        private string _group;
        private string _subgroup;
        private int _rentId;
        private Hashtable _queryInfo;
        private ArrayList _keysDependingOnMe;
        private object _lockId;
        private LockAccessType _accessType;
        private ulong _version;
        private string _providerName;
        private string _resyncProviderName;
        private Notifications _callbackEntry;
        private string _type;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="exh"></param>
        /// <param name="priority"></param>
        /// <param name="itemRemovedCallback"></param>
        /// <param name="group"></param>
        /// <param name="subgroup"></param>
        [CLSCompliant(false)]
        public CompactCacheEntry(object key, object value, ExpirationHint dependency,
            long expiration,
            byte options, object itemRemovedCallback, string group, string subgroup, Hashtable queryInfo, BitSet Flag, object lockId, ulong version, LockAccessType accessType, string providername, string resyncProviderName, Notifications callbackEntry)
        {
            _key = key;
            _flag = Flag;
            _value = value;

            _dependency = dependency;
            _expiration = expiration;
            _options = options;
            _itemRemovedCallback = itemRemovedCallback;
            if (group != null)
            {
                _group = group;
                if (subgroup != null)
                    _subgroup = subgroup;
            }
            _queryInfo = queryInfo;

            _lockId = lockId;
            _accessType = accessType;
            _version = version;
            _providerName = providername;
            _resyncProviderName = resyncProviderName;
            _callbackEntry = callbackEntry;
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

        public string ProviderName
        {
            get { return _providerName; }
        }

        public string ResyncProviderName
        {
            get { return _resyncProviderName; }
        }

        [CLSCompliant(false)]
        public ulong Version
        {
            get { return _version; }
        }

        /// <summary>
        /// 
        /// </summary>
        public ExpirationHint Dependency { get { return _dependency; } }
       
        /// <summary>
        /// 
        /// </summary>
        public byte Options { get { return _options; } }

        /// <summary>
        /// 
        /// </summary>
        public object Callback { get { return _itemRemovedCallback; } }

        /// <summary>
        /// 
        /// </summary>
        public string Group { get { return _group; } }

        /// <summary>
        /// 
        /// </summary>
        public string SubGroup { get { return _subgroup; } }

        public Hashtable QueryInfo { get { return _queryInfo; } }

        public Notifications CallbackEntry { get { return _callbackEntry; } }

        public ArrayList KeysDependingOnMe
        {
            get { return _keysDependingOnMe; }
            set { _keysDependingOnMe = value; }
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _key = reader.ReadObject();
            _value = reader.ReadObject();
            _expiration = reader.ReadInt64();
            _dependency = ExpirationHint.ReadExpHint(reader, null);
            _options = reader.ReadByte();
            _itemRemovedCallback = reader.ReadObject();
            _group = (string)reader.ReadObject();
            _subgroup = (string)reader.ReadObject();
            _queryInfo = (Hashtable)reader.ReadObject();
            _keysDependingOnMe = (ArrayList)reader.ReadObject();
            _callbackEntry = reader.ReadObject() as Notifications;
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
                writer.WriteObject(_group);
                writer.WriteObject(_subgroup);
                writer.WriteObject(_queryInfo);
                writer.WriteObject(_keysDependingOnMe);
                writer.WriteObject(_callbackEntry);
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
            _group = null;
            _subgroup = null;
            _queryInfo = null;
            _callbackEntry = null;
        }
        #endregion
    }
}