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

using System.Collections;
using System;

namespace Alachisoft.NCache.Caching.Queries
{
    internal class VirtualQueryIndex : IQueryIndex
    {
        private Topologies.Local.LocalCacheBase _cache;
        private readonly object _mutex = new object();
        public VirtualQueryIndex(Topologies.Local.LocalCacheBase cache)
        {
            _cache = cache;
        }

        //only needed in case of AttributeIndex
        public void AddToIndex(object key, object value) { }

        public void RemoveFromIndex(object key, object value) { }

        public void RemoveFromIndex(object key) { }

        public void Clear() { }
        public IndexInformation GetIndexInformation(object key) { return null; }

        public IDictionaryEnumerator GetEnumerator(string typeName)
        {
            if (_cache == null)
                return null;

            lock (_mutex)
            {
                if (typeName == "*")
                    return GetEnumerator();
                else
                {
                    IDictionaryEnumerator en = _cache.GetEnumerator() as IDictionaryEnumerator;
                    Hashtable tbl = new Hashtable();

                    while (en.MoveNext())
                    {
                        object obj = ((CacheEntry)en.Value).DeflattedValue(_cache.Context.CacheImpl.Name);

                        if (obj.GetType().FullName == typeName)
                        {
                            tbl[en.Key] = en.Value;
                        }
                    }

                    return tbl.GetEnumerator();
                } 
            }
        }

        private IDictionaryEnumerator GetEnumerator()
        {
            lock (_mutex)
            {
                if (_cache != null)
                    return _cache.GetEnumerator() as IDictionaryEnumerator; 
            }
            return null;
        }

        public long IndexInMemorySize
        {
            get { throw new NotImplementedException("VirtualQueryIndex.IndexMemorySize"); }
        }
    }
}
