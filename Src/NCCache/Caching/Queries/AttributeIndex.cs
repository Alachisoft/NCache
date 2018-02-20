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
using System.Collections.Generic;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Queries
{
    [Serializable]
    public class AttributeIndex : IQueryIndex
    {
        protected HashVector IndexTable;
        protected string CacheName;
        protected string _type;

        protected const string TAG_INDEX_KEY = "$Tag$";
        protected const string NAMED_TAG_PREFIX = "$NamedTagAttribute$";
        protected internal List<AttributeIndex> _sharedTypes;
        protected internal Hashtable commonRbStores;
        
        //Cacluate Size of this Attribute Index accumulated size of all stores
        private long _attributeIndexSize;

        //Cacluate Size of indexTable and IndexInformation Associated with Keys
        private long _keyIndexInformationSize;
        private readonly bool _haveDuplicateKeys;
        private readonly object _mutex = new object();

        [NonSerialized]
        private TypeInfoMap _typeMap;

        public string TypeName 
        {

            get { return _type; }

        }
        public virtual List<AttributeIndex> SharedTypes
        {
            set
            {
                _sharedTypes = value;
            }
        }

        public virtual Hashtable CommonRBStores
        {
            set
            {
                commonRbStores = value;
            }
        }

        
        public virtual IIndexStore getRBStore(string attrib)
        {
            return (IIndexStore)((IndexTable[attrib] is IIndexStore) ? IndexTable[attrib] : null);
        }

        public AttributeIndex(string cacheName, string type)
        {
            IndexTable = new HashVector();
            CacheName = cacheName;
            _type = type;
        }        

        public AttributeIndex(ArrayList attribList, string cacheName, string type)
        {
            IndexTable = new HashVector();
            CacheName = cacheName;
            _type = type;

            Initialize(attribList);
        }
        public AttributeIndex(ArrayList attribList, string cacheName, string type, bool haveDuplicateKeys)
        {
            IndexTable = new HashVector();
            CacheName = cacheName;
            _type = type;
            _haveDuplicateKeys = haveDuplicateKeys;

            Initialize(attribList);
        }
        public AttributeIndex(ArrayList attribList, string cacheName, string type,TypeInfoMap typeMap)
        {
            IndexTable = new HashVector();
            CacheName = cacheName;
            _type = type;
            _typeMap = typeMap;

            Initialize(attribList);
        }

        /// <summary>
        /// Gets the size of the attribute index;
        /// </summary>
        public int Count
        {
            get
            {
                if (IndexTable != null)
                    return IndexTable.Count;
                return 0;
            }
        }        

        public TypeInfoMap TypeMap
        {
            get { return _typeMap; }
            set { _typeMap = value; }
        }


        public virtual void Initialize(ArrayList attribList)
        {
            IIndexStore store;
            if (attribList != null && attribList.Count > 0)
            {

                IEnumerator e = attribList.GetEnumerator();

                while (e.MoveNext())
                {
                    string attribName = e.Current.ToString();
                    if (commonRbStores != null && commonRbStores.ContainsKey(_type + ":" + attribName))
                    {
                        IIndexStore commonStore = (IIndexStore)commonRbStores[_type + ":" + attribName];                       
                        IndexTable.Add(attribName, commonStore);
                    }
                    else
                    {
                        String storeDataType=String.Empty;
                        if (_typeMap != null)
                            storeDataType = _typeMap.GetAttributeType(this.TypeName, attribName);
                        Type genericType = typeof(RBStore<>).MakeGenericType(Common.MemoryUtil.GetDataType(storeDataType));
                        store = (IIndexStore)Activator.CreateInstance(genericType, new object[] { CacheName, storeDataType, attribName, _haveDuplicateKeys });
                        IndexTable.Add(attribName, store);
                    }
                }
                if (commonRbStores != null && commonRbStores.ContainsKey(TAG_INDEX_KEY))
                {
                    store = (IIndexStore)commonRbStores[TAG_INDEX_KEY];
                    IndexTable.Add(TAG_INDEX_KEY, store);
                }
                else
                {
                    Type genericType = typeof(RBStore<>).MakeGenericType(Common.MemoryUtil.GetDataType(Common.MemoryUtil.Net_System_String));
                    store = (IIndexStore)Activator.CreateInstance(genericType, new object[] { CacheName, Common.MemoryUtil.Net_System_String, TAG_INDEX_KEY, _haveDuplicateKeys });
                    IndexTable.Add(TAG_INDEX_KEY, store);
                }
            }

            if (!IndexTable.ContainsKey(TAG_INDEX_KEY) && commonRbStores != null && commonRbStores.ContainsKey(TAG_INDEX_KEY))
            {
                store = (IIndexStore)commonRbStores[TAG_INDEX_KEY];
                IndexTable.Add(TAG_INDEX_KEY, store);
            }

        }

        public virtual void AddToIndex(object key, object value)
        {
            lock (_mutex)
            {
                QueryItemContainer container = (QueryItemContainer) value;
                CacheEntry entry = container.Item;
                Hashtable attributeValues = container.ItemArrtributes;
                IDictionaryEnumerator valuesDic = attributeValues.GetEnumerator();
                INodeReference keyNode;
                while (valuesDic.MoveNext())
                {
                    string indexKey = (string) valuesDic.Key;
                    object val = valuesDic.Value;

                    string storeName = indexKey;
                    IIndexStore store = IndexTable[indexKey] as IIndexStore;
                    keyNode = null;

                    if (store == null)
                    {
                        if (indexKey == TAG_INDEX_KEY)
                        {
                            Type genericType =
                                typeof (RBStore<>).MakeGenericType(
                                    Common.MemoryUtil.GetDataType(Common.MemoryUtil.Net_System_String));
                            store =
                                (IIndexStore)
                                    Activator.CreateInstance(genericType,
                                        new object[]
                                        {CacheName, Common.MemoryUtil.Net_System_String, indexKey, _haveDuplicateKeys});
                            IndexTable[indexKey] = store;
                        }
                        else
                        {
                            string namedTagIndexKey = ConvertToNamedTagKey(indexKey);
                            storeName = namedTagIndexKey;
                            store = IndexTable[namedTagIndexKey] as IIndexStore;


                            if (store == null)
                            {
                                String storeDataType = String.Empty;
                                if (val != null)
                                {
                                    Type type = val.GetType();
                                    storeDataType = type.FullName;
                                }
                                Type genericType =
                                    typeof (RBStore<>).MakeGenericType(Common.MemoryUtil.GetDataType(storeDataType));
                                store =
                                    (IIndexStore)
                                        Activator.CreateInstance(genericType,
                                            new object[]
                                            {CacheName, storeDataType, namedTagIndexKey, _haveDuplicateKeys});

                                IndexTable.Add(namedTagIndexKey, store);

                            }
                        }
                    }

                    if (store != null)
                    {
                        long prev = store.IndexInMemorySize;

                        if (val != null)
                            keyNode = (INodeReference) store.Add(val, key);
                        _attributeIndexSize += store.IndexInMemorySize - prev;
                    }

                    storeName = StringPool.PoolString(storeName);

                    IndexInformation info;

                    if (entry.IndexInfo != null)
                    {
                        info = entry.IndexInfo;
                    }
                    else
                    {
                        info = new IndexInformation();
                    }
                    long prevSize = info.IndexInMemorySize;

                    info.Add(storeName, store, keyNode);

                    _keyIndexInformationSize += info.IndexInMemorySize - prevSize;
                    entry.IndexInfo = info;
                }
            }
        }

        public virtual void RemoveFromIndex(object key, object value)
        {
            lock (_mutex)
            {
                CacheEntry entry = (CacheEntry)value;
                IndexInformation indexInfo = entry.IndexInfo;
                if (indexInfo != null && indexInfo.IndexStoreInformations != null)
                {
                    foreach (IndexStoreInformation indexStoreInfo in indexInfo.IndexStoreInformations)
                    {
                        IIndexStore store = indexStoreInfo.Store;
                        if (indexStoreInfo.IndexPosition != null)
                        {
                            long prevSize = store.IndexInMemorySize;
                            store.Remove(key, indexStoreInfo.IndexPosition);
                            _attributeIndexSize += store.IndexInMemorySize - prevSize;
                            string possibleNamedTagKey = ConvertToNamedTagKey(store.Name);
                            if (store.Count == 0 && IndexTable.ContainsKey(possibleNamedTagKey))
                                IndexTable.Remove(possibleNamedTagKey);
                        }
                    }
                    _keyIndexInformationSize -= indexInfo.IndexInMemorySize;
                }
                entry.IndexInfo = null;
            }
        }

        
        public IIndexStore GetStore(string attrib)
        {
            bool disacleException = QueryIndexManager.DisableException;
            IIndexStore store = null;

            if (IndexTable.Contains(attrib))
                store = IndexTable[attrib] as IIndexStore;
            else
            {
                string namedTagKey = ConvertToNamedTagKey(attrib);

                if (IndexTable.Contains(namedTagKey))
                {
                    store = IndexTable[namedTagKey] as IIndexStore;
                }              

                if (disacleException)
                {
                    if (store == null)
                    {
                        store = new HashStore();
                    }
                }
            }

            return store;
        }

        public void Clear()
        {
            lock (_mutex)
            {
                IDictionaryEnumerator e = IndexTable.GetEnumerator();

                while (e.MoveNext())
                {
                    IIndexStore store = e.Value as IIndexStore;
                    if (store != null) store.Clear();
                }
            }

            _attributeIndexSize = 0;
        }

        public IDictionaryEnumerator GetEnumerator(string type, bool forTag)
        {
            lock (_mutex)
            {
                IDictionaryEnumerator  en = IndexTable.GetEnumerator();
                if (!forTag)
                {
                    while (en.MoveNext())
                    {
                        IIndexStore store = en.Value as IIndexStore;
                        if ((string)en.Key != TAG_INDEX_KEY)
                            if (store != null) return store.GetEnumerator();
                    }
                }
                else
                {
                    if (IndexTable.Contains(TAG_INDEX_KEY))
                    {
                        IIndexStore store = IndexTable[TAG_INDEX_KEY] as IIndexStore;
                        if (store != null) return store.GetEnumerator();
                    }
                }
            }

            return null;
        }

        public IndexInformation GetIndexInformation(object key)
        {
            return null;
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return IndexTable.GetEnumerator();
        }

        protected string ConvertToNamedTagKey(string indexKey)
        {
            string namedTagKey = String.Concat(NAMED_TAG_PREFIX, indexKey);
            return namedTagKey;
        }

        public static bool IsNamedTagKey(string indexKey)
        {
            bool result = indexKey.StartsWith(NAMED_TAG_PREFIX);
            return result;
        }

        public object GetAttributeValue(string key, string attributeName, IndexInformation indexInfo)
        {
            string storeName = attributeName;
            if (!IndexTable.Contains(storeName) && !IndexTable.Contains(storeName = ConvertToNamedTagKey(attributeName)))
                throw new Exception("Index is not defined for attribute '" + attributeName + "'");

            IndexInformation indexInformation = indexInfo;
            object value = null;
            lock (_mutex)
            {
                if (indexInformation != null && indexInformation.IndexStoreInformations != null)
                {
                    foreach (IndexStoreInformation indexStoreInfo in indexInformation.IndexStoreInformations)
                    {
                        if (indexStoreInfo.StoreName == storeName)
                        {
                            if (indexStoreInfo.IndexPosition != null)
                                value = indexStoreInfo.IndexPosition.GetKey();
                            else
                                return null;
                            break;
                        }
                    }
                }
            }

            return value;

        }

        public long IndexInMemorySize
        {
            get 
            {                
                return _keyIndexInformationSize + _attributeIndexSize;
            }
        }
    }
}