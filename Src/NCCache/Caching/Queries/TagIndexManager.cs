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

using System.Collections;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Caching.Queries.Filters;
using Alachisoft.NCache.Common.Queries.Filters;

namespace Alachisoft.NCache.Caching.Queries
{
    internal class TagIndexManager : QueryIndexManager
    {
        internal TagIndexManager(IDictionary props, Topologies.Local.IndexedLocalCache cache, string cacheName)
            : base(props, cache, cacheName)
        {
        }

        internal override bool Initialize(Hashtable _knownTypes)
        {
            if (base.Initialize(_knownTypes)) return true;

             
            //initialize the index map to put the tags information on runtime.
            _indexMap = new HashVector();
            return true;
        }

        private void AddTag(object key, QueryItemContainer value)
        {
            if (value != null)
            {
                Hashtable tagAttribs = value.ItemArrtributes;
                string type = tagAttribs["type"] as string;
                ArrayList tagsList = tagAttribs["tags-list"] as ArrayList;

                if (type != null)
                {
                    IQueryIndex index;
                    lock (_indexMap.SyncRoot)
                    {
                        if (!_indexMap.Contains(type))
                        {
                            _indexMap[type] = new AttributeIndex(null, _cacheName, type, true);
                        }
                        index = _indexMap[type] as IQueryIndex;

                        if (index != null)
                        {
                            long prevSize = index.IndexInMemorySize;
                            if (tagsList != null)
                            {
                                foreach (string tag in tagsList)
                                {
                                    Hashtable tmp = new Hashtable();
                                    tmp["$Tag$"] = tag;//.ToLower();
                                    value.ItemArrtributes = tmp;
                                    index.AddToIndex(key, value);
                                }
                            }
                            _queryIndexMemorySize += index.IndexInMemorySize - prevSize;
                        }
                    }
                }
            }
        }

        public override void AddToIndex(object key, object value, OperationContext operationContext)
        {
            CacheEntry entry = (CacheEntry)value;
            Hashtable queryInfo = entry.QueryInfo;

            if (queryInfo.Contains("query-info")) base.AddToIndex(key, entry,operationContext);
            if (queryInfo.Contains("tag-info")&&queryInfo["tag-info"]!=null)
            {
                AddTag(key, new QueryItemContainer(entry,queryInfo["tag-info"] as Hashtable));
                if (queryInfo["tag-info"] != null)
                    entry.ObjectType = ((Hashtable)(queryInfo["tag-info"]))["type"].ToString();
            }
        }

        private void GetCombinedKeysFromEveryType(string tag, HashVector finalResult)
        {
            if (_indexMap == null)
                return;
            IDictionaryEnumerator typeEnumerator;
            lock (_indexMap.SyncRoot)
            {
                typeEnumerator = _indexMap.GetEnumerator(); 
            }

            while (typeEnumerator.MoveNext())
            {
                AttributeIndex index = typeEnumerator.Value as AttributeIndex;
                IIndexStore store = index.GetStore("$Tag$");

                if (store != null)
                {
                    IKeyFilter keyFilter = _cache != null ? _cache.GetBucketFilter() : null;
                    IKeyFilter compoundFilter = _cache != null ? _cache.GetCompoundFilter() : null;
                    IQueryResult result = new ListQueryResult(keyFilter,compoundFilter);
                    store.GetData(tag.ToLower(), ComparisonType.EQUALS, result, CollectionOperation.Union);
                    foreach (string key in result)
                        finalResult[key] = null;
                }
            }            
        }

        public ICollection GetAllMatchingTags(string[] tags)
        {
            HashVector finalResult = new HashVector();
            GetCombinedKeysFromEveryType(tags[0],finalResult);

            for (int i = 1; i < tags.Length; i++)
            {
                HashVector shiftTable = new HashVector();
                HashVector temp = new HashVector();
                GetCombinedKeysFromEveryType(tags[i],temp);

                HashVector smaller, larger;
                if (temp.Count > finalResult.Count)
                {
                    smaller = finalResult;
                    larger = temp;
                }
                else
                {
                    smaller = temp;
                    larger = finalResult;
                }

                foreach (object key in smaller.Keys)
                {
                    if(larger.ContainsKey(key))
                        shiftTable[key] = null;
                }
                finalResult = shiftTable;
                
            }

            return new ClusteredArrayList(finalResult.Keys);
        }

        public ICollection GetByTag(string tag)
        {
            HashVector finalResult = new HashVector();
            GetCombinedKeysFromEveryType(tag,finalResult);
            return new ClusteredArrayList(finalResult.Keys);
        }

        public ICollection GetAnyMatchingTag(string[] tags)
        {
            HashVector finalResult = new HashVector();
            GetCombinedKeysFromEveryType(tags[0],finalResult);           

            for (int i = 1; i < tags.Length; i++)
            {
                GetCombinedKeysFromEveryType(tags[i],finalResult);
            }
            return new ClusteredArrayList(finalResult.Keys);
        }
    }
}