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
using System.Collections;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Caching.Queries
{
    /// <summary>
    /// special purpose single key store. this store is used in ActiveQueryIndex, an extension of AttributeIndex, 
    /// that is passed to re-evaluate an active query.
    /// </summary>
    [Serializable]
    public class HashStore : IIndexStore, Runtime.Serialization.ICompactSerializable
    {
        private Hashtable _store;

        public HashStore()
        {
            _store = new Hashtable();
        }

        public object Add(object key, object value)
        {
            if (_store != null)
                _store[key] = value;
            return null;
        }

        public bool Remove(object key, object value)
        {
            if (_store != null && _store.Contains(key))
                _store.Remove(key);

            return true;
        }

        public void Clear()
        {
            if (_store != null)
                _store.Clear();
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            if (_store != null)
                return _store.GetEnumerator();

            return (new Hashtable()).GetEnumerator();
        }

        public int Count
        {
            get { return _store != null ? _store.Count : 0; }
        }

        public void GetData(object key, ComparisonType comparisonType, IQueryResult result, CollectionOperation op)
        {
            IComparable keyToCompare = key as IComparable;

            if (_store != null)
            {
                switch (comparisonType)
                {
                    case ComparisonType.EQUALS:
                        if (_store.Contains(key))
                            result.AddObject(_store[key], op);

                        break;

                    case ComparisonType.NOT_EQUALS:
                        foreach (object storedKey in _store.Keys)
                        {
                            if (((IComparable)storedKey).CompareTo(keyToCompare) != 0)
                                result.AddObject(_store[storedKey], op);
                        }

                        break;

                    case ComparisonType.LESS_THAN:
                        foreach (object storedKey in _store.Keys)
                        {
                            if (((IComparable)storedKey).CompareTo(keyToCompare) < 0)
                                result.AddObject(_store[storedKey], op);
                        }
                        break;

                    case ComparisonType.GREATER_THAN:
                        foreach (object storedKey in _store.Keys)
                        {
                            if (((IComparable)storedKey).CompareTo(keyToCompare) > 0)
                                result.AddObject(_store[storedKey], op);
                        }
                        break;

                    case ComparisonType.LESS_THAN_EQUALS:
                        foreach (object storedKey in _store.Keys)
                        {
                            if (((IComparable)storedKey).CompareTo(keyToCompare) <= 0)
                                result.AddObject(_store[storedKey], op);
                        }
                        break;

                    case ComparisonType.GREATER_THAN_EQUALS:
                        foreach (object storedKey in _store.Keys)
                        {
                            if (((IComparable)storedKey).CompareTo(keyToCompare) >= 0)
                                result.AddObject(_store[storedKey], op);
                        }
                        break;

                    case ComparisonType.LIKE:
                        foreach (object storedKey in _store.Keys)
                        {
                            string pattern = key as string;
                            WildcardEnabledRegex regex = new WildcardEnabledRegex(pattern);

                            if (storedKey is string)
                            {
                                if (regex.IsMatch((string)storedKey))
                                    result.AddObject(_store[storedKey], op);
                            }
                        }

                        break;

                    case ComparisonType.NOT_LIKE:
                        foreach (object storedKey in _store.Keys)
                        {
                            string pattern = key as string;
                            WildcardEnabledRegex regex = new WildcardEnabledRegex(pattern);

                            if (storedKey is string)
                            {
                                if (!regex.IsMatch((string)storedKey))
                                    result.AddObject(_store[storedKey], op);
                            }
                        }

                        break;
                }
                result.Mark(op);
            }

        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _store = (Hashtable)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_store);
        }

        public long IndexInMemorySize
        {
            get { throw new NotImplementedException("HashStore.IndexInMemorySize"); }
        }

        public string StoreDataType
        {
            get { throw new NotImplementedException(); }
        }


        public string Name
        {
            get { throw new NotImplementedException(); }
        }
    }
}
