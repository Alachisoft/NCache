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
using Alachisoft.NCache.Common.Pooling.Stats;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Common.Pooling.Internal
{
    internal sealed class StringPool : IStringPool
    {
        private readonly int _initialCapacity;
        private IDictionary<string, string> _pool;
        private int _padding = 10000; // Padding of 10k

        public string PoolName
        {
            get => "StringPool";
        }

        public StringPool(int initialCapacity)
        {
            _initialCapacity = initialCapacity;
            _pool = new HashVector<string, string>(initialCapacity);
        }

        public string GetString(string key)
        {
            if (key == null)
                return null;

            if (key.Equals(string.Empty))
                return string.Empty;

            string value;

            lock (_pool)
            {
                if (!_pool.TryGetValue(key, out value))
                {
                    _pool[key] = value = key;
                }
            }
            return value;
        }

        public void ResetPool()
        {
            lock (_pool)
            {
                _pool = new HashVector<string, string>(_initialCapacity);
            }
        }

        public void TrimPool(long count)
        {
            ClusteredArrayList interimKeyArray;
            lock (_pool)
            {
                // No trimming needed if StringPool size is less than CacheCount
                if (_pool.Count <= count)
                    return;

                // No trimming needed if the difference in size lies within padding range so no need of trimming
                if (_pool.Count - count < _padding)
                    return;

                // Calculate the number of keys that need to be truncated
                long truncatingCount = _pool.Count - (count + _padding);

                // Populate an array from the StringPool equal to the number of keys that need to be truncated
                interimKeyArray = new ClusteredArrayList();

                int i = 0;
                IEnumerator<KeyValuePair<string, string>> enumerator = _pool.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    if (i == truncatingCount)
                        break;

                    interimKeyArray.Add(enumerator.Current.Key);
                    i++;
                }

                //Use the array to remove those keys from the StringPool
                for (i = 0; i < interimKeyArray.Count; i++)
                {
                    _pool.Remove(interimKeyArray[i] as string);
                }
            }
        }

        public StringPoolStats GetStats()
        {
            lock (_pool)
            {
                return new StringPoolStats
                {
                    StringsPooled = _pool.Count
                };
            }
        }
    }
}
