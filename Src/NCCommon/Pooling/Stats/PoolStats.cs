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

using System.Collections.Generic;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common.Pooling.Stats
{
    public sealed class PoolStats : ICompactSerializable
    {
        public StringPoolStats StringPoolStats
        {
            get; set;
        }

        public IDictionary<ArrayPoolType, ArrayPoolStats> ArrayPoolStats
        {
            get; private set;
        } = new Dictionary<ArrayPoolType, ArrayPoolStats>();

        public IDictionary<ObjectPoolType, ObjectPoolStats> ObjectPoolStats
        {
            get; private set;
        } = new Dictionary<ObjectPoolType, ObjectPoolStats>();

        public IDictionary<ObjectPoolType, ObjectPoolStats> SimpleObjectPoolStats
        {
            get; private set;
        } = new Dictionary<ObjectPoolType, ObjectPoolStats>();

        public void MergeStats(PoolStats otherPoolStats)
        {
            if (otherPoolStats == null)
                return;

            if (StringPoolStats == null)
                StringPoolStats = otherPoolStats.StringPoolStats;

            else
                StringPoolStats.StringsPooled += otherPoolStats.StringPoolStats?.StringsPooled ?? 0;

            foreach (var stats in otherPoolStats.ArrayPoolStats)
                ArrayPoolStats[stats.Key] = stats.Value;

            foreach (var stats in otherPoolStats.ObjectPoolStats)
                ObjectPoolStats[stats.Key] = stats.Value;

            foreach (var stats in otherPoolStats.SimpleObjectPoolStats)
                SimpleObjectPoolStats[stats.Key] = stats.Value;
        }

        #region ---------------------------- [ ICompactSerialization ] ----------------------------

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(StringPoolStats);
            SerializationUtility.SerializeDictionary(ArrayPoolStats, writer);
            SerializationUtility.SerializeDictionary(ObjectPoolStats, writer);
            SerializationUtility.SerializeDictionary(SimpleObjectPoolStats, writer);

        }

        public void Deserialize(CompactReader reader)
        {
            StringPoolStats = reader.ReadObject() as StringPoolStats;
            ArrayPoolStats = SerializationUtility.DeserializeDictionary<ArrayPoolType, ArrayPoolStats>(reader);
            ObjectPoolStats = SerializationUtility.DeserializeDictionary<ObjectPoolType, ObjectPoolStats>(reader);
            SimpleObjectPoolStats = SerializationUtility.DeserializeDictionary<ObjectPoolType, ObjectPoolStats>(reader);

        }

        #endregion
    }
}
