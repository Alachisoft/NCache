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

        //Cacluate Size of this Attribute Index accumulated size of all stores
        private long _attributeIndexSize;

        //Cacluate Size of indexTable and IndexInformation Associated with Keys
        private long _keyIndexInformationSize;

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
            _type = type;

            Initialize(attribList);
        }

        public AttributeIndex(ArrayList attribList, string cacheName, string type, TypeInfoMap typeMap)
        {
            _indexTable = new Hashtable();
            _cacheName = cacheName;
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
                    Type genericType = typeof(RBStore<>).MakeGenericType(Common.MemoryUtil.GetDataType(storeDataType));
                    store = (IIndexStore)Activator.CreateInstance(genericType, new object[] { _cacheName, storeDataType, attribName});
                    _indexTable.Add(attribName, store);                   
                }
            }
        }

        public virtual void AddToIndex(object key, object value)
        {
            QueryItemContainer container = (QueryItemContainer)value;
            CacheEntry entry = container.Item;
            Hashtable attributeValues = container.ItemArrtributes;
            IDictionaryEnumerator valuesDic = attributeValues.GetEnumerator();
            INodeReference keyNode = null;
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
                        keyNode = (INodeReference)store.Add(val, key);

                    _attributeIndexSize += store.IndexInMemorySize - prev;
                }

                storeName = Common.Util.StringPool.PoolString(storeName);

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

                this._keyIndexInformationSize += info.IndexInMemorySize - prevSize;
                entry.IndexInfo = info;

            }
        }

        public virtual void RemoveFromIndex(object key, object value)
        {
            bool isNodeRemoved = false;
            CacheEntry entry = (CacheEntry)value;
            IndexInformation indexInfo = entry.IndexInfo;
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

            entry.IndexInfo = null;

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
            return null;
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return _indexTable.GetEnumerator();
        }

        public object GetAttributeValue(string key, string attributeName, IndexInformation indexInfo)
        {
            string storeName = attributeName;
            if (!_indexTable.Contains(storeName))
                throw new Exception("Index is not defined for attribute '" + attributeName + "'");

            IndexInformation indexInformation = indexInfo;

            object value = null;
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
            return value;
        }

        public long IndexInMemorySize
        {
            get
            {
                long temp = 0;
                temp += _keyIndexInformationSize;
                temp += _attributeIndexSize;

                return temp;
            }
        }
    }
}
