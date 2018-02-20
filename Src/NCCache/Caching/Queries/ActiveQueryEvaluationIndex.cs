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
    [Serializable]
    public class ActiveQueryEvaluationIndex : AttributeIndex, Runtime.Serialization.ICompactSerializable
    {

        private readonly  object _mutex= new object();

        public ActiveQueryEvaluationIndex(string cacheName, string type)
            : base(cacheName, type)
        {
        }

        public ActiveQueryEvaluationIndex(ArrayList attribList, string cacheName, string type): base(attribList, cacheName, type)
        {
        }
      
        public override void Initialize(ArrayList attribList)
        {
            IIndexStore store = null;

            if (attribList != null && attribList.Count > 0)
            {

                IEnumerator e = attribList.GetEnumerator();

                while (e.MoveNext())
                {
                    string attribName = e.Current.ToString();
                    if (commonRbStores != null && commonRbStores.ContainsKey(_type + ":" + attribName))
                    {
                        HashStore commonStore = (HashStore)commonRbStores[_type + ":" + attribName];
                        IndexTable.Add(attribName, commonStore);
                    }
                    else
                    {
                        store = new HashStore();
                        IndexTable.Add(attribName, store);
                    }
                }
                if (commonRbStores != null && commonRbStores.ContainsKey(TAG_INDEX_KEY))
                {
                    store = (HashStore)commonRbStores[TAG_INDEX_KEY];
                    IndexTable.Add(TAG_INDEX_KEY, store);
                }
                else
                {
                    store = new HashStore();
                    IndexTable.Add(TAG_INDEX_KEY, store);
                }
            }

            if (!IndexTable.ContainsKey(TAG_INDEX_KEY) && commonRbStores != null && commonRbStores.ContainsKey(TAG_INDEX_KEY))
            {
                store = (HashStore)commonRbStores[TAG_INDEX_KEY];
                IndexTable.Add(TAG_INDEX_KEY, store);
            }
        }

        public override void AddToIndex(object key, object value)
        {
            lock (_mutex)
            {

                Hashtable attributeValues = value as Hashtable;
                IDictionaryEnumerator valuesDic = attributeValues.GetEnumerator();

                while (valuesDic.MoveNext())
                {
                    string indexKey = (string)valuesDic.Key;
                    IIndexStore store = IndexTable[indexKey] as IIndexStore;

                    if (store == null)
                    {
                        if (indexKey == TAG_INDEX_KEY)
                        {
                            store = new HashStore();
                            IndexTable[indexKey] = store;
                        }
                        else
                        {
                            string namedTagIndexKey = ConvertToNamedTagKey(indexKey);
                            store = IndexTable[namedTagIndexKey] as IIndexStore;

                            if (store == null)
                            {
                                store = new HashStore();
                                IndexTable[namedTagIndexKey] = store;
                            }
                        }
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

        public override void RemoveFromIndex(object key, object value)
        {
            lock (_mutex)
            {
                Hashtable attributeValues = value as Hashtable;
                IDictionaryEnumerator valuesDic = attributeValues.GetEnumerator();

                while (valuesDic.MoveNext())
                {
                    string indexKey = (string)valuesDic.Key;

                    if (IndexTable.Contains(indexKey) || IndexTable.Contains(indexKey = ConvertToNamedTagKey(indexKey)))
                    {
                        IIndexStore store = IndexTable[indexKey] as IIndexStore;
                        object val = valuesDic.Value;

                        if (val != null)
                            store.Remove(val, key);
                        else
                            store.Remove("null", key);

                        if (store.Count == 0)
                        {
                            if (indexKey == TAG_INDEX_KEY || IsNamedTagKey(indexKey))
                                IndexTable.Remove(indexKey);
                            else
                                IndexTable[indexKey] = new HashStore();
                        }
                    }
                } 
            }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            IndexTable = (HashVector)reader.ReadObject();
            CacheName = (string)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(IndexTable);
            writer.WriteObject(CacheName);
        }
    }
}