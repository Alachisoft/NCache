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
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using Alachisoft.NCache.Util;

namespace Alachisoft.NCache.Caching
{
    public class CompressedValueEntry: SimpleLease, ICompactSerializable
    {
        private BitSet _flag;
        private object _value;
        private CacheEntry _cacheEntry;

        public EntryType Type
        {
            get; set;
        }

        public CacheEntry Entry
        {
            get { return _cacheEntry; }
            set { _cacheEntry = value; }
        }

        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (ReferenceEquals(_value, value))
                    return;

                (_value as BookKeepingLease)?.MarkFree(NCModulesConstants.CompressedValueEntry);
                _value = value;
                (_value as BookKeepingLease)?.MarkInUse(NCModulesConstants.CompressedValueEntry);
            }
        }

        public BitSet Flag
        {
            get
            {
                return _flag;
            }
            set
            {
                if (ReferenceEquals(this._flag, value))
                    return;

                if (_flag != null) MiscUtil.ReturnBitsetToPool(_flag, _flag.PoolManager);
                _flag = value;
            }
        }

        public CompressedValueEntry()
        {
            Type = EntryType.CacheItem;
        }

        public static CompressedValueEntry CreateCompressedCacheEntry(PoolManager poolmanger)
        {
            return poolmanger.GetCompressedValueEntryPool()?.Rent(true) ?? new CompressedValueEntry();
        }

        public static CompressedValueEntry CreateCompressedCacheEntry(PoolManager poolmanger, object value, BitSet flag, EntryType entryType)
        {
            CompressedValueEntry entry = CreateCompressedCacheEntry(poolmanger);
            entry.Value = value;
            entry.Flag = flag;
            entry.Type = entryType;
            return entry;
        }

        public static CompressedValueEntry CreateCompressedCacheEntry(PoolManager poolmanger, CacheEntry entry)
        {
            CompressedValueEntry compressedEntry = CreateCompressedCacheEntry(poolmanger, entry.Value, entry.Flag, entry.Type);
            compressedEntry.Entry = entry;
            return compressedEntry;
        }

        #region ILeasable

        public override void ResetLeasable()
        {
            Type = EntryType.CacheItem;
            Value = default(object);
            Flag = default(BitSet);
            Entry = null;
        }

        public override void ReturnLeasableToPool()
        {
            // Nothing to return since everything populated here 
            // is returned with the CacheEntry here explicitly returned

            //Flag?.MarkFree(NCModulesConstants.CompressedValueEntry);
            //(Value as UserBinaryObject)?.MarkFree(NCModulesConstants.CompressedValueEntry);

            //PoolManager.GetCompressedValueEntryPool().Return(this);
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            Value = reader.ReadObject();
            Flag = reader.ReadObject() as BitSet;
            Type = (EntryType)reader.ReadByte();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(Value);
            writer.WriteObject(Flag);
            writer.Write((byte)Type);
        }

        #endregion
    }
}