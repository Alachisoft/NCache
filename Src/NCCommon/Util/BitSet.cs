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

namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// A class that encapsulates a bit set.
    /// </summary>
    [Serializable]
    public class BitSet : ICloneable, IDisposable, Runtime.Serialization.ICompactSerializable
    {
        private byte _bitset;
        public const int Size = 24;


        public BitSet() { }
        public BitSet(byte bitset) { this._bitset = bitset; }

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
            BitSet other = new BitSet();
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
    }
}
