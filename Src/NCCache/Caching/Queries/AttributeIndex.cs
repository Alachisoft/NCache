// Copyright (c) 2015 Alachisoft
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
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Queries
{
    [Serializable]
    public class AttributeIndex : IQueryIndex
    {
        protected Hashtable _indexTable;
        protected string _cacheName;
        protected string _type;
        protected Hashtable _keyIndexTable;

        //Cacluate Size of this Attribute Index accumulated size of all stores
        private long _attributeIndexSize;

        //Cacluate Size of indexTable and IndexInformation Associated with Keys
        private long _keyIndexInformationSize;
        private long _keyIndexTableMaxCount;

        [NonSerialized]
        private TypeInfoMap _typeMap = null;

        public string TypeName 
        {

            get { return _type; }

        }

        public virtual IIndexStore getRBStore(string attrib)
        {
            return (IIndexStore)((_indexTable[attrib] is IIndexStore) ? _indexTable[attrib] : null);
        }

        public AttributeIndex(ArrayList attribList, string cacheName, string type)
        {
            _indexTable = new Hashtable();
            _cacheName = cacheName;
            _keyIndexTable = new Hashtable();
            _type = type;

            Initialize(attribList);
        }

        public AttributeIndex(ArrayList attribList, string cacheName, string type, TypeInfoMap typeMap)
        {
            _indexTable = new Hashtable();
            _cacheName = cacheName;
            _keyIndexTable = new Hashtable();
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
                if (_indexTable != null)
                    return _indexTable.Count;
                else
                    return 0;
            }
        }
        public virtual void Initialize(ArrayList attribList)
        {
            IIndexStore store = null;
            if (attribList != null && attribList.Count > 0)
            {

                IEnumerator e = attribList.GetEnumerator();

                while (e.MoveNext())
                {
                    string attribName = e.Current.ToString();
                    
                    String storeDataType = String.Empty;
                    if (this._typeMap != null)
                        storeDataType = _typeMap.GetAttributeType(this.TypeName, attribName);

                    store = new RBStore(_cacheName, storeDataType);
                    _indexTable.Add(attribName, store);                   
                }
            }
        }

        public virtual void AddToIndex(object key, object value)
        {
            Hashtable attributeValues = value as Hashtable;
            IDictionaryEnumerator valuesDic = attributeValues.GetEnumerator();
            RedBlackNodeReference keyNode = null;
            while (valuesDic.MoveNext())
            {
                string indexKey = (string)valuesDic.Key;
                string storeName = indexKey;
                IIndexStore store = _indexTable[indexKey] as IIndexStore;
                keyNode = null;

                if (store != null)
                {
                    long prev = store.IndexInMemorySize;
                    object val = valuesDic.Value;

                    if (val != null)
                        keyNode = (RedBlackNodeReference)store.Add(val, key);

                    _attributeIndexSize += store.IndexInMemorySize - prev;
                }

                storeName = Common.Util.StringPool.PoolString(storeName);

                IndexInformation info;

                if (_keyIndexTable.Contains(key))
                {
                    info = _keyIndexTable[key] as IndexInformation;
                }
                else
                {
                    info = new IndexInformation();
                }

                long prevSize = info.IndexInMemorySize;

                info.Add(storeName, store, keyNode);
                _keyIndexTable[key] = info;

                this._keyIndexInformationSize += info.IndexInMemorySize - prevSize;
                if (_keyIndexTable.Count > _keyIndexTableMaxCount)
                    _keyIndexTableMaxCount = _keyIndexTable.Count;

            }
        }

        public virtual void RemoveFromIndex(object key)
        {
            bool isNodeRemoved = false;
            IndexInformation indexInfo = (IndexInformation)_keyIndexTable[key];
            if (indexInfo != null)
            {
                foreach (IndexStoreInformation indexStoreInfo in indexInfo.IndexStoreInformations)
                {
                    isNodeRemoved = false;
                    IIndexStore store = indexStoreInfo.Store;
                    if (indexStoreInfo.IndexPosition != null)
                    {
                        long prevSize = store.IndexInMemorySize;
                        isNodeRemoved = store.Remove(key, indexStoreInfo.IndexPosition);
                        _attributeIndexSize += store.IndexInMemorySize - prevSize;
                    }
                }
                _keyIndexInformationSize -= indexInfo.IndexInMemorySize;
            }

            _keyIndexTable.Remove(key);

        }

        public virtual void RemoveFromIndex(object key, object value)
        {
            Hashtable attributeValues = value as Hashtable;
            IDictionaryEnumerator valuesDic = attributeValues.GetEnumerator();

            while (valuesDic.MoveNext())
            {
                string indexKey = (string)valuesDic.Key;

                if (_indexTable.Contains(indexKey))
                {
                    IIndexStore store = _indexTable[indexKey] as IIndexStore;
                    object val = valuesDic.Value;

                    long prev = store.IndexInMemorySize;

                    if (val != null)
                        store.Remove(val, key);
                    else
                        store.Remove("null", key);

                    if (store.Count == 0)
                    {
                        String storeDataType = ((RBStore)store).StoreDataType;
                        _indexTable[indexKey] = new RBStore(_cacheName, storeDataType);
                    }
                    _attributeIndexSize += store.IndexInMemorySize - prev;
                }
            }
        }

        public IIndexStore GetStore(string attrib)
        {
            bool disacleException = QueryIndexManager.DisableException;
            IIndexStore store = null;

            if (_indexTable.Contains(attrib))
                store = _indexTable[attrib] as IIndexStore;
            else
            {
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
            IDictionaryEnumerator e = _indexTable.GetEnumerator();

            while (e.MoveNext())
            {
                IIndexStore store = e.Value as IIndexStore;
                store.Clear();
            }

            _keyIndexTable = new Hashtable();

            _attributeIndexSize = 0;

        }

        public IDictionaryEnumerator GetEnumerator(string type)
        {
            IDictionaryEnumerator en = _indexTable.GetEnumerator();
 
            while (en.MoveNext())
            {
                IIndexStore store = en.Value as IIndexStore;
                return store.GetEnumerator();
            }
           

            return null;
        }

        public IndexInformation GetIndexInformation(object key)
        {
            return (IndexInformation)_keyIndexTable[key];
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return _indexTable.GetEnumerator();
        }

        public object GetAttributeValue(string key, string attributeName)
        {
            string storeName = attributeName;
            if (!_indexTable.Contains(storeName))
                throw new Exception("Index is not defined for attribute '" + attributeName + "'");

            IndexInformation indexInformation = GetIndexInformation(key);

            IComparable value = null;
            foreach (IndexStoreInformation indexStoreInfo in indexInformation.IndexStoreInformations)
            {
                if (indexStoreInfo.StoreName == storeName)
                {
                    if (indexStoreInfo.IndexPosition != null)
                        value = indexStoreInfo.IndexPosition.RBReference.Key;
                    else
                        return null;
                    break;
                }
            }
            return value;
        }

        public long IndexInMemorySize
        {
            get
            {
                long temp = 0;
                temp += _keyIndexInformationSize;
                temp += _attributeIndexSize;
                temp += (this._keyIndexTableMaxCount * Common.MemoryUtil.NetHashtableOverHead);

                return temp;
            }
        }
    }
}
