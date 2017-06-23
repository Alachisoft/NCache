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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Statistics
{
    [Serializable]
    public class BucketStatistics : ICompactSerializable
    {
        private long _count;
        private long _dataSize;

        public BucketStatistics() { }

        public long Count
        {
            get { return _count; }
            set { _count = value; }
        }

        public long DataSize
        {
            get { return _dataSize; }
            set { _dataSize = value; }
        }

        public void Increment(long dataSize)
        {
            lock (this)
            {
                _count++;
                _dataSize += dataSize;
            }
        }

        public void Decrement(long dataSize)
        {
            lock (this)
            {
                _count--;
                _dataSize -= dataSize;
            }
        }

        public void Clear()
        {
            lock (this)
            {
                _count = 0;
                _dataSize = 0;
            }
        }

        public void SerializeLocal(CompactWriter writer)
        {
            writer.Write(_count);
            writer.Write(_dataSize);
        }

        public void DeserializeLocal(CompactReader reader)
        {
            _count = reader.ReadInt64();
            _dataSize = reader.ReadInt64();
        }

        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            _count = reader.ReadInt64();
            _dataSize = reader.ReadInt64();
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            writer.Write(_count);
            writer.Write(_dataSize);
        }

        #endregion
    }
}
