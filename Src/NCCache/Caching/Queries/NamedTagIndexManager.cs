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
using System.Collections.Generic;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Queries
{
    internal class NamedTagIndexManager : TagIndexManager
    {
        internal NamedTagIndexManager(IDictionary props, Topologies.Local.IndexedLocalCache cache, string cacheName)
            : base(props, cache, cacheName)
        {
        }

        internal override bool Initialize(Hashtable _knownTypes)
        {
            if (base.Initialize(_knownTypes)) return true;
            //initialize the index map to put the tags information on 
            _indexMap = new HashVector();
            return true;
        }

        public override void AddToIndex(object key, object value, OperationContext operationContext)
        {
            base.AddToIndex(key, value,operationContext);

            CacheEntry entry = (CacheEntry)value;
            Hashtable queryInfo = entry.QueryInfo;

            if (queryInfo.Contains("named-tag-info"))
            {
                Hashtable namedTags = queryInfo["named-tag-info"] as Hashtable;
                if (namedTags!=null)
                    AddNamedTag(key, namedTags, entry, operationContext);
            }

            entry.QueryInfo = null;
        }


        private void AddNamedTag(object key, Hashtable value, CacheEntry entry, OperationContext operationContext)
        {

            string type = value["type"] as string;
            Hashtable tagsList = ((Hashtable) value["named-tags-list"]).Clone() as Hashtable;

            if (tagsList != null && type!=null)
            {
                Hashtable metaInfoAttribs = (Hashtable) tagsList.Clone();
                IQueryIndex index;

                lock (_indexMap.SyncRoot)
                {
                    if (!_indexMap.Contains(type))
                    {
                        if (_sharedAttributeIndex != null && _sharedAttributeIndex.ContainsKey(type))
                        {
                            AttributeIndex tempAttrib = _sharedAttributeIndex[type];
                            tempAttrib.Initialize(new ArrayList());
                            _indexMap.Add(type, tempAttrib);
                        }
                        else
                        {
                            _indexMap[type] = new AttributeIndex(null, _cacheName, type, true);
                        }
                    }
                    
                    index= _indexMap[type] as IQueryIndex;

                    if (index != null)
                    {
                        long prevSize = index.IndexInMemorySize;

                        index.AddToIndex(key, new QueryItemContainer(entry, tagsList));

                        _queryIndexMemorySize += index.IndexInMemorySize - prevSize;
                    }
                }

                if (operationContext.Contains(OperationContextFieldName.IndexMetaInfo))
                {
                    MetaInformation metaInformation =
                        (MetaInformation) operationContext.GetValueByField(OperationContextFieldName.IndexMetaInfo);
                    metaInformation.Add(metaInfoAttribs);
                }
                else
                {
                    MetaInformation metaInformation = new MetaInformation(metaInfoAttribs);
                    metaInformation.CacheKey = key as string;
                    metaInformation.Type = value["type"] as string;
                    operationContext.Add(OperationContextFieldName.IndexMetaInfo, metaInformation);
                    entry.ObjectType = value["type"] as string;
                }
            }
        }
    }
}