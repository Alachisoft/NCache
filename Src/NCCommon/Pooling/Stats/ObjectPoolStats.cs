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

using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common.Pooling.Stats
{
    public sealed class ObjectPoolStats : ICompactSerializable
    {
        public int Top
        {
            get; set;
        }

        public long Hits
        {
            get; set;
        }

        public int Count
        {
            get; set;
        }

        public long Misses
        {
            get; set;
        }

        public int Capacity
        {
            get; set;
        }

        public long TotalRequests
        {
            get; set;
        }

        public float ServingCapabilityBorderline
        {
            get; set;
        }

        public bool IsSimpleObjectPool
        {
            get;set;
        }

        #region ---------------------------- [ ICompactSerialization ] ----------------------------

        public void Serialize(CompactWriter writer)
        {
            writer.Write(Top);
            writer.Write(Hits);
            writer.Write(Count);
            writer.Write(Misses);
            writer.Write(Capacity);
            writer.Write(TotalRequests);
            writer.Write(ServingCapabilityBorderline);
        }

        public void Deserialize(CompactReader reader)
        {
            Top = reader.ReadInt32();
            Hits = reader.ReadInt64();
            Count = reader.ReadInt32();
            Misses = reader.ReadInt64();
            Capacity = reader.ReadInt32();
            TotalRequests = reader.ReadInt64();
            ServingCapabilityBorderline = reader.ReadSingle();
        }

        #endregion
    }
}
