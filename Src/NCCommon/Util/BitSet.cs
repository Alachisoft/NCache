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
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling.Extension;


namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// A class that encapsulates a bit set.
    /// </summary>
    [Serializable]
    public class BitSet : BookKeepingLease, ICloneable, IDisposable, Runtime.Serialization.ICompactSerializable
    {
        private byte _bitset;
        public const int Size = 24;

        public BitSet() { }
        public BitSet(byte bitset) { this._bitset = bitset; }

        #region Creating BitSet

        public static BitSet Create(PoolManager poolManager)
        {

            if (poolManager != null)
                return poolManager.GetBitSetPool().Rent(true);
            else
                return new BitSet();

            
        }

        public static BitSet Create(PoolManager poolManager, byte bit)
        {
            var instance = Create(poolManager);
            instance._bitset = bit;
            return instance;
        }

        public static BitSet CreateAndMarkInUse(PoolManager poolManager, int moduleRefId)
        {
            var instance = Create(poolManager);
            instance.MarkInUse(moduleRefId);
            return instance;
        }

        public static BitSet CreateAndMarkInUse(PoolManager poolManager, int moduleRefId, byte bit)
        {
            var instance = Create(poolManager, bit);
            instance.MarkInUse(moduleRefId);
            return instance;
        }

        #endregion

        public byte Data
        {
            get { return _bitset; }
            set { _bitset = value; }
        }

        /// <summary> Bit set specific functions. </summary>
        public bool IsAnyBitSet(byte bit) { return ((_bitset & bit) != 0); }
        public bool IsBitSet(byte bit) { return ((_bitset & bit) == bit); }
        public void SetBit(byte bit) { _bitset |= bit; }
        public void UnsetBit(byte bit) { _bitset &= Convert.ToByte(~bit & 0xff); }
        public void Set(byte bitsToSet, byte bitsToUnset)
        {
            SetBit(bitsToSet);
            UnsetBit(bitsToUnset);
        }

        #region	/                 --- ICloneable ---           /

        public object Clone()
        {
            var    other = new BitSet();
            other._bitset = _bitset;
            return other;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        #region ICompact Serializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _bitset = reader.ReadByte();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_bitset);
        }
        #endregion

        #region ILeasable

        public sealed override void ResetLeasable()
        {
            _bitset = 0;
        }

        public sealed override void ReturnLeasableToPool()
        {
        }

        #endregion

        #region - [Deep Cloning] -

        public BitSet DeepClone(PoolManager poolManager)
        {
            var clonedBitSet = poolManager.GetBitSetPool()?.Rent(true) ?? new BitSet();
            clonedBitSet._bitset = _bitset;

            return clonedBitSet;
        }

        #endregion
    }
}