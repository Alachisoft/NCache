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
using System.Collections;

namespace Alachisoft.NCache.Common.Util
{
    public class RoundRobinLoadbalancer<T>
    {
        private System.Collections.Specialized.OrderedDictionary _items;
        private int _index = -1;

        public bool IsValid { get { return _items.Count > 0; } }

        public RoundRobinLoadbalancer()
        {
            _items = new System.Collections.Specialized.OrderedDictionary();
        }

        public RoundRobinLoadbalancer(IEqualityComparer comparer)
        {
            _items = new System.Collections.Specialized.OrderedDictionary(comparer);
        }

        public void Add(T item)
        {
            lock(this)
            {
                if (!_items.Contains(item))
                    _items.Add(item,item);
            }
        }

        public void Remove(T item)
        {
            lock (this)
            {
                if (_items.Contains(item))
                    _items.Remove(item);
            }
        }

        public T GetNext()
        {
            lock(this)
            {
                if (!IsValid) return default(T);

                _index++;
                if (_index >= _items.Count)
                    _index = 0;

                return (T) _items[_index];
            }
        }
    }
}
