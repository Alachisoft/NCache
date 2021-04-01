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
using System.Collections.Generic;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common.Pooling.Stats
{
    public sealed class ArrayPoolStats : IEnumerable<KeyValuePair<int, ObjectPoolStats>>, ICompactSerializable
    {
        private IDictionary<int, ObjectPoolStats> _arrayPoolBucketStats = new Dictionary<int, ObjectPoolStats>();

        public ObjectPoolStats WrapperPoolStats
        {
            get; set;
        }

        public ObjectPoolStats this[int bucketId]
        {
            get => _arrayPoolBucketStats.TryGetValue(bucketId, out var stats) ? stats : null;
            set => _arrayPoolBucketStats[bucketId] = value ?? throw new ArgumentNullException(nameof(value));
        }

        #region --------------------------------- [ IEnumerable ] ---------------------------------

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<int, ObjectPoolStats>> GetEnumerator()
        {
            return _arrayPoolBucketStats.GetEnumerator();
        }

        #endregion

        #region ---------------------------- [ ICompactSerialization ] ----------------------------

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(WrapperPoolStats);
            SerializationUtility.SerializeDictionary(_arrayPoolBucketStats, writer);
        }

        public void Deserialize(CompactReader reader)
        {
            WrapperPoolStats = reader.ReadObject() as ObjectPoolStats;
            _arrayPoolBucketStats = SerializationUtility.DeserializeDictionary<int, ObjectPoolStats>(reader);
        }

        #endregion
    }
}
