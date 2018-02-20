// Copyright (c) 2018 Alachisoft
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
using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Queries
{
    public class TagIndex : IQueryIndex
    {
        private HashVector _indexTable;
        public const string TAG_INDEX_KEY = "$Tag$";
        private readonly object _mutex = new object();

        public TagIndex()
        {
            Initialize();
        }

        /// <summary>
        /// Gets the size of the attribute index;
        /// </summary>
        public int Count
        {
            get
            {
                if (_indexTable != null)
                    return _indexTable.Count;
                else
                    return 0;
            }
        }

        private void Initialize()
        {
            _indexTable = new HashVector();
            IIndexStore store = new HashStore();
            _indexTable[TAG_INDEX_KEY] = store;
        }



        public virtual void AddToIndex(object key, object value)
        {
            lock (_mutex)
            {
                IDictionaryEnumerator valuesDic = ((Hashtable) value).GetEnumerator();

                while (valuesDic.MoveNext())
                {
                    string indexKey = (string) valuesDic.Key;
                    IIndexStore store = _indexTable[indexKey] as IIndexStore;

                    if (store == null && indexKey == TAG_INDEX_KEY)
                    {
                        store = new HashStore();
                        _indexTable[indexKey] = store;
                    }

                    if (store != null)
                    {
                        object val = valuesDic.Value;

                        if (val != null)
                            store.Add(val, key);
                        else
                            store.Add("null", key);
                    }
                }
            }
        }

        public virtual void RemoveFromIndex(object key, object value)
        {
            lock (_mutex)
            {
                IDictionaryEnumerator valuesDic = ((Hashtable) value).GetEnumerator();

                while (valuesDic.MoveNext())
                {
                    string indexKey = (string) valuesDic.Key;

                    if (_indexTable.Contains(indexKey))
                    {
                        IIndexStore store = _indexTable[indexKey] as IIndexStore;
                        object val = valuesDic.Value;

                        if (val != null)
                            store.Remove(val, key);
                        else
                            store.Remove("null", key);

                        if (store.Count == 0 && indexKey == TAG_INDEX_KEY)
                        {
                            _indexTable.Remove(indexKey);
                        }
                    }
                }
            }
        }

        public IIndexStore GetIndex(string attrib)
        {
            if (_indexTable.Contains(attrib))
                return _indexTable[attrib] as IIndexStore;

            return null;
        }

        public void Clear()
        {
            lock (_mutex)
            {
                IDictionaryEnumerator e = _indexTable.GetEnumerator();
                while (e.MoveNext())
                {
                    IIndexStore index = e.Value as IIndexStore;
                    index.Clear();
                }
            }
        }

        public IDictionaryEnumerator GetEnumerator(string typeName, bool forTag)
        {
            if (_indexTable.Contains(TAG_INDEX_KEY))
            {
                IIndexStore store = _indexTable[TAG_INDEX_KEY] as IIndexStore;
                return store.GetEnumerator();
            }

            return null;
        }

        //only needed in case of AttributeIndex
        public IndexInformation GetIndexInformation(object key) { return null; }

        public void RemoveFromIndex(object key) { }

        public long IndexInMemorySize
        {
            get { throw new NotImplementedException("TagIndex.IndexMemorySize"); }
        }
    }
}