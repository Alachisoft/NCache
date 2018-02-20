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
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Caching.Queries
{
    internal class QueryIndexManager:ISizableIndex
    {
        string TAG_INDEX_KEY = "$Tag$";
        string NAMED_TAG_PREFIX = "$NamedTagAttribute$";
        static bool disableException = false;
        //in case of DisableException is true, exception will not be thrown, and return new attribute index. 

        public static bool DisableException
        {
            get
            {
                disableException = ServiceConfiguration.DisableIndexNotDefinedException;
                return disableException;
            }
        }

        class IndexAddTask : AsyncProcessor.IAsyncTask
        {
            private object _key;
            private CacheEntry _entry;
            private QueryIndexManager _indexManager;
            private OperationContext _operationContext;

            public IndexAddTask(QueryIndexManager indexManager, object key, CacheEntry value, OperationContext operationContext)
            {
                _key = key;
                _entry = value;
                _indexManager = indexManager;
                _operationContext = operationContext;
            }

            void AsyncProcessor.IAsyncTask.Process()
            {
                _indexManager.AddToIndex(_key, _entry, _operationContext);
            }
        }

        class IndexRemoveTask : AsyncProcessor.IAsyncTask
        {
            private object _key;
            private CacheEntry _entry;
            private QueryIndexManager _indexManager;

            public IndexRemoveTask(QueryIndexManager indexManager, object key, CacheEntry value)
            {
                _key = key;
                _entry = value;
                _indexManager = indexManager;
            }

            void AsyncProcessor.IAsyncTask.Process()
            {
                _indexManager.RemoveFromIndex(_key, _entry);
            }
        }

        private AsyncProcessor _asyncProcessor;
        private bool _indexForAll;
        protected Topologies.Local.IndexedLocalCache _cache;
        private IDictionary _props;
        protected string _cacheName;

        protected TypeInfoMap _typeMap;
        protected HashVector _indexMap;
        protected Dictionary<String, AttributeIndex> _sharedAttributeIndex;
        protected long _queryIndexMemorySize;

              

        public QueryIndexManager(IDictionary props, Topologies.Local.IndexedLocalCache cache, string cacheName)
        {
            _indexMap = new HashVector();
            _cache = cache;
            _props = props;
            _cacheName = cacheName;
        }
        

        public TypeInfoMap TypeInfoMap
        {
            get { return _typeMap; }
        }

        public AsyncProcessor AsyncProcessor
        {
            get { return _asyncProcessor; }
        }

        public bool IndexForAll
        {
            get { return _indexForAll; }
        }

        public HashVector IndexMap
        {
            get { return _indexMap; }
        }

        public Boolean CheckForSameClassNames(Hashtable knownSharedClasses) 
        {           
            ICollection coll=knownSharedClasses.Values;

            Array values = Array.CreateInstance(typeof(System.Object), coll.Count);
            
            coll.CopyTo(values,0);

            for (int outer = 0; outer < values.Length-1; outer++)
            {
                Hashtable outerValue = (Hashtable)values.GetValue(outer);
                
                string name = (string)outerValue["name"];
                string[] temp = name.Split(':');
                string outerTypeName = temp[0];

                for (int inner = outer + 1; inner < values.Length; inner++)
                {
                    Hashtable innerValue = (Hashtable)values.GetValue(inner);
                    
                    string name1 = (string)innerValue["name"];
                    string[] temp1 = name1.Split(':');
                    string innerTypeName = temp1[0];

                    if (outerTypeName.Equals(innerTypeName))
                        return false;
                }
            }
                return true;
        }

        public void createSharedTypeAttributeIndex(Hashtable knownSharedClasses, Hashtable indexedClasses)
        {
            Hashtable commonRBStore = new Hashtable();
            Dictionary<string, AttributeIndex> sharedAttributeIndexMap = new Dictionary<string, AttributeIndex>();
            IEnumerator iteouterSharedTypes = knownSharedClasses.GetEnumerator();
            Type genericType = typeof(RBStore<>).MakeGenericType(Common.MemoryUtil.GetDataType(Common.MemoryUtil.Net_System_String));
            IIndexStore store = (IIndexStore)Activator.CreateInstance(genericType, new object[] { _cacheName, Common.MemoryUtil.Net_System_String, this.TAG_INDEX_KEY });

            commonRBStore.Add(this.TAG_INDEX_KEY, store);

            while (iteouterSharedTypes.MoveNext())
            {
                DictionaryEntry outerEntry = (DictionaryEntry)iteouterSharedTypes.Current;
                Hashtable outerEntryValue = (Hashtable)outerEntry.Value;
                string name = (string)outerEntryValue["name"];
                string[] temp = StringHelperClass.StringSplit(name, ":", true);
                string outerTypeName = temp[0];
                //Create Attribute Index even if not queryindexed
                sharedAttributeIndexMap.Add(outerTypeName, new AttributeIndex(_cacheName, outerTypeName));

                sharedAttributeIndexMap[outerTypeName].TypeMap = _typeMap;

                if (indexedClasses.Count > 0 && isQueryindexed(outerTypeName, indexedClasses))
                {
                    Hashtable outerTypeAttributes = (Hashtable)outerEntryValue["attribute"];
                    if (outerTypeAttributes != null)
                    {
                        IEnumerator iteOuterTypeAttribute = outerTypeAttributes.GetEnumerator();
                        while (iteOuterTypeAttribute.MoveNext())
                        {
                            DictionaryEntry tempEntry = (DictionaryEntry)iteOuterTypeAttribute.Current;
                            Hashtable outerAttributeMeta = (Hashtable)tempEntry.Value;

                            string outerOrderNo = (string)outerAttributeMeta["order"];
                            string outerAttributeName = (string)outerAttributeMeta["name"];
                            if (isQueryindexedAttribute(outerTypeName, outerAttributeName, indexedClasses))
                            {
                                IEnumerator iteInnerSharedTypes = knownSharedClasses.GetEnumerator();
                                while (iteInnerSharedTypes.MoveNext())
                                {
                                    DictionaryEntry innerEntry = (DictionaryEntry)iteInnerSharedTypes.Current;

                                    Hashtable innerEntryValue = (Hashtable)innerEntry.Value;
                                    string name1 = (string)innerEntryValue["name"];
                                    string[] temp1 = StringHelperClass.StringSplit(name1, ":", true);
                                    string innerTypeName = temp1[0];
                                    if (!outerTypeName.Equals(innerTypeName) && isQueryindexed(innerTypeName, indexedClasses))
                                    {
                                        Hashtable innerTypeAttributes = (Hashtable)((Hashtable)innerEntry.Value)["attribute"];

                                        IEnumerator iteInnerTypeAttribute = innerTypeAttributes.GetEnumerator();
                                        while (iteInnerTypeAttribute.MoveNext())
                                        {

                                            DictionaryEntry tempEntry1 = (DictionaryEntry)iteInnerTypeAttribute.Current;
                                            Hashtable innerAttributeMeta = (Hashtable)tempEntry1.Value;

                                            string innerorderNo = (string)innerAttributeMeta["order"];
                                            string innerAttributeName = (string)innerAttributeMeta["name"];
                                            if (innerorderNo.Equals(outerOrderNo) && isQueryindexedAttribute(innerTypeName, innerAttributeName, indexedClasses))
                                            {
                                                if (commonRBStore.ContainsKey(outerTypeName + ":" + outerAttributeName))
                                                {
                                                    if (!commonRBStore.ContainsKey(innerTypeName + ":" + innerAttributeName))
                                                    {
                                                        IIndexStore commonRB = (IIndexStore)commonRBStore[outerTypeName + ":" + outerAttributeName];
                                                        commonRBStore.Add(innerTypeName + ":" + innerAttributeName, commonRB);
                                                    }
                                                    break;
                                                }
                                                else
                                                {
                                                    String storeDataType = TypeInfoMap.GetAttributeType(innerTypeName, innerAttributeName);
                                                    genericType = typeof(RBStore<>).MakeGenericType(Common.MemoryUtil.GetDataType(storeDataType));
                                                    IIndexStore commonRB = (IIndexStore)Activator.CreateInstance(genericType, new object[] { _cacheName, storeDataType, innerAttributeName });                    
                        
                                                    commonRBStore.Add(innerTypeName + ":" + innerAttributeName, commonRB);
                                                    commonRBStore.Add(outerTypeName + ":" + outerAttributeName, commonRB);
                                                    break;

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


            if (sharedAttributeIndexMap.Count > 0)
            {
                IEnumerator iteSharedIndexMap = sharedAttributeIndexMap.GetEnumerator();
                while (iteSharedIndexMap.MoveNext())
                {
                    List<AttributeIndex> sharedTypes = new List<AttributeIndex>();
                    System.Collections.Generic.KeyValuePair<string, AttributeIndex> outerEntry = (System.Collections.Generic.KeyValuePair<string, AttributeIndex>)iteSharedIndexMap.Current;
                    string outerTypeName = (string)outerEntry.Key;
                    AttributeIndex outerSharedIndex = (AttributeIndex)outerEntry.Value;
                    foreach (System.Collections.Generic.KeyValuePair<string,AttributeIndex> innerEntry in sharedAttributeIndexMap)
                    {
                        string innerTypeName = (string)innerEntry.Key;
                        if (!innerTypeName.Equals(outerTypeName))
                        {
                            AttributeIndex innerSharedIndex = (AttributeIndex)innerEntry.Value;
                            sharedTypes.Add(innerSharedIndex);
                        }
                    }
                    outerSharedIndex.CommonRBStores = commonRBStore;
                    _sharedAttributeIndex.Add(outerTypeName, outerSharedIndex);
                }
            }
        }

        public bool isQueryindexed(string typeName, Hashtable indexedClasses)
        {
            IEnumerator ie = indexedClasses.GetEnumerator();
            while (ie.MoveNext())
            {
                DictionaryEntry current_1 = (DictionaryEntry)ie.Current;
                Hashtable innerProps = (Hashtable)((current_1.Value is Hashtable) ? current_1.Value : null);
                string queryIndexedTypename = "";

                if (innerProps != null)
                {
                    queryIndexedTypename = (string)innerProps["id"];
                    if (typeName.Equals(queryIndexedTypename))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool isQueryindexedAttribute(string typeName, string attributeName, Hashtable indexedClasses)
        {
            IEnumerator ie = indexedClasses.GetEnumerator();
            while (ie.MoveNext())
            {
                DictionaryEntry current_1 = (DictionaryEntry)ie.Current;
                Hashtable innerProps = (Hashtable)((current_1.Value is Hashtable) ? current_1.Value : null);
                string queryIndexedTypeName = "";
                if (innerProps != null)
                {
                    queryIndexedTypeName = (string)innerProps["id"];
                    if (typeName.Equals(queryIndexedTypeName))
                    {
                        ArrayList attribList = new ArrayList();
                        IEnumerator en = innerProps.GetEnumerator();
                        while (en.MoveNext())
                        {
                            DictionaryEntry current_2 = (DictionaryEntry)en.Current;
                            Hashtable attribs = (Hashtable)((current_2.Value is Hashtable) ? current_2.Value : null);
                            if (attribs != null)
                            {
                                IEnumerator ide = attribs.GetEnumerator();
                                while (ide.MoveNext())
                                {
                                    DictionaryEntry current_3 = (DictionaryEntry)ide.Current;
                                    Hashtable attrib = (Hashtable)((current_3.Value is Hashtable) ? current_3.Value : null);
                                    if (attrib != null)
                                    {
                                        string tempAttrib = (string)attrib["id"];
                                        if (attributeName.Equals(tempAttrib))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        //----------------------------------------------------------------------------------------
        //	Copyright � 2007 - 2012 Tangible Software Solutions Inc.
        //    This class can be used by anyone provided that the copyright notice remains intact.

        //    This class is used to replace most calls to the Java String.split method.
        //----------------------------------------------------------------------------------------
        internal static class StringHelperClass
        {
            //------------------------------------------------------------------------------------
            //	This method is used to replace most calls to the Java String.split method.
            //------------------------------------------------------------------------------------
            internal static string[] StringSplit(string source, string regexDelimiter, bool trimTrailingEmptyStrings)
            {
                string[] splitArray = System.Text.RegularExpressions.Regex.Split(source, regexDelimiter);

                if (trimTrailingEmptyStrings)
                {
                    if (splitArray.Length > 1)
                    {
                        for (int i = splitArray.Length; i > 0; i--)
                        {
                            if (splitArray[i - 1].Length > 0)
                            {
                                if (i < splitArray.Length)
                                    System.Array.Resize(ref splitArray, i);

                                break;
                            }
                        }
                    }
                }

                return splitArray;
            }
        }


        internal virtual bool Initialize(Hashtable _dataSharingKnownTypes)
        {
            
            bool indexedDefined = false;
            if (_props != null)
            {
                if (_props.Contains("index-for-all"))
                {
                    _indexForAll = Convert.ToBoolean(_props["index-for-all"]);
                    indexedDefined = _indexForAll;
                }

                if (_props.Contains("index-classes"))
                {
                    Hashtable indexClasses = _props["index-classes"] as Hashtable;
                    _typeMap = new TypeInfoMap(indexClasses);

                    IDictionaryEnumerator ie = indexClasses.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        Hashtable innerProps = ie.Value as Hashtable;
                        string typename = "";

                        if (innerProps != null)
                        {
                            typename = (string)innerProps["id"];
                            ArrayList attribList = new ArrayList();
                            IDictionaryEnumerator en = innerProps.GetEnumerator();
                            while (en.MoveNext())
                            {
                                Hashtable attribs = en.Value as Hashtable;
                                if (attribs != null)
                                {
                                    IDictionaryEnumerator ide = attribs.GetEnumerator();
                                    while (ide.MoveNext())
                                    {
                                        Hashtable attrib = ide.Value as Hashtable;
                                        if (attrib != null)
                                        {
                                            attribList.Add(attrib["id"] as string);
                                        }
                                    }
                                }
                            }

                            //attrib level index.
                            if (attribList.Count > 0)
                            {
                                AttributeIndex index = new AttributeIndex(attribList, _cacheName, typename, _typeMap);
                                 _indexMap[typename] = index;
                                
                            }
                            //just a key level index.
                            else
                                _indexMap[typename] = new TypeIndex(typename, _indexForAll);
                            indexedDefined = true;
                        }

                    }
                }
            }
            else
            {
                _indexMap["default"] = new VirtualQueryIndex(_cache);
            }
            if (indexedDefined)
            {
                _asyncProcessor = new AsyncProcessor(_cache.Context.NCacheLog);
                _asyncProcessor.Start();
            }
            return indexedDefined;
        }

        public void Dispose()
        {
            if (_asyncProcessor != null)
            {
                _asyncProcessor.Stop();
                _asyncProcessor = null;
            }
            if (_indexMap != null)
            {
                _indexMap.Clear();
                _indexMap = null;
            }
            _cache = null;
        }

        public virtual void AddToIndex(object key, object value,OperationContext operationContext)
        {
          
            CacheEntry entry = (CacheEntry)value;
            Hashtable queryInfo = entry.QueryInfo["query-info"] as Hashtable;
            if (queryInfo == null) return;


            lock (_indexMap.SyncRoot)
            {
                IDictionaryEnumerator queryInfoEnumerator = queryInfo.GetEnumerator();

                while (queryInfoEnumerator.MoveNext())
                {
                    int handleId = (int)queryInfoEnumerator.Key;
                    string type = _typeMap.GetTypeName(handleId);
                    if (_indexMap.Contains(type))
                    {
                        Hashtable indexAttribs = new Hashtable();
                        Hashtable metaInfoAttribs = new Hashtable();

                        ArrayList values = (ArrayList)queryInfoEnumerator.Value;

                        ArrayList attribList = _typeMap.GetAttribList(handleId);
                        for (int i = 0; i < attribList.Count; i++)
                        {
                            string attribute = attribList[i].ToString();
                            string val = _typeMap.GetAttributes(handleId)[attribList[i]] as string;
                            if (Common.Util.JavaClrTypeMapping.JavaToClr(val)!=null)
                            {
                                val = Common.Util.JavaClrTypeMapping.JavaToClr(val);
                            }
                            Type t1 =Type.GetType(val, true, true);

                            object obj = null;

                            if (values[i] != null)
                            {
                                try
                                {
                                    if (t1 == typeof(System.DateTime))
                                    {
                                        obj = new DateTime(Convert.ToInt64(values[i]));
                                    }
                                    else
                                    {
                                        obj = Convert.ChangeType(values[i], t1);
                                    }
                                }
                                catch (Exception)
                                {
                                    throw new System.FormatException("Cannot convert '" + values[i] + "' to " + t1.ToString());
                                }

                                indexAttribs.Add(attribute, obj);
                            }
                            else
                                indexAttribs.Add(attribute, null);


                            metaInfoAttribs.Add(attribute, obj);
                        }

                        MetaInformation metaInformation = new MetaInformation(metaInfoAttribs);                        
                        metaInformation.CacheKey = key as string;
                        metaInformation.Type = _typeMap.GetTypeName(handleId);

                        operationContext.Add(OperationContextFieldName.IndexMetaInfo,metaInformation);
                        
                        entry.ObjectType = _typeMap.GetTypeName(handleId);

                        IQueryIndex index = (IQueryIndex)_indexMap[type];
                        long prevSize = index.IndexInMemorySize;
                        index.AddToIndex(key, new QueryItemContainer(entry, indexAttribs));

                        this._queryIndexMemorySize += index.IndexInMemorySize - prevSize;
                    }
                }
            }


        }

        public void AsyncAddToIndex(object key, CacheEntry value, OperationContext operationContext)
        {
            lock (_asyncProcessor)
            {
                _asyncProcessor.Enqueue(new IndexAddTask(this, key, value,operationContext));
            }
        }

 
        public virtual void RemoveFromIndex(object key, object value)
        {
            if (value == null)
                return;
            CacheEntry entry = (CacheEntry)value;
            string type = entry.ObjectType;
            lock (_indexMap.SyncRoot)
            {
                IQueryIndex index = (IQueryIndex)_indexMap[type];
                long prevSize = index.IndexInMemorySize;
                index.RemoveFromIndex(key, value);
                this._queryIndexMemorySize += index.IndexInMemorySize - prevSize;
            }
        }


        public void AsyncRemoveFromIndex(object key, CacheEntry value)
        {
            lock (_asyncProcessor)
            {
                _asyncProcessor.Enqueue(new IndexRemoveTask(this, key, value));
            }
        }

        public void Clear()
        {
            if (_indexMap != null)
            {
                lock (_indexMap.SyncRoot)
                {
                    IDictionaryEnumerator e = _indexMap.GetEnumerator();
                    while (e.MoveNext())
                    {
                        IQueryIndex index = e.Value as IQueryIndex;
                        index.Clear();
                    }
                    this._queryIndexMemorySize = 0;
                }
            }
        }

        public virtual Hashtable GetQueryInfo(object key, object value)
        {
            Hashtable queryInfo = new Hashtable();
            Hashtable queryIndex = new Hashtable();
            Hashtable namedTagInfo = new Hashtable();
            Hashtable namedTagsList = new Hashtable();

            Hashtable tagInfo = new Hashtable();
            ArrayList tagsList = new ArrayList();
            CacheEntry entry = (CacheEntry)value;
            if (entry.ObjectType == null)
                return queryInfo;
            IQueryIndex index = (IQueryIndex)_indexMap[entry.ObjectType];
            IndexInformation indexInformation = entry.IndexInfo;
            lock (_indexMap.SyncRoot)
            {
                if (indexInformation != null)
                {
                    if (_typeMap != null)
                    {
                        int handleId = _typeMap.GetHandleId(entry.ObjectType);
                        if (handleId > -1)
                        {
                            ArrayList attributes = _typeMap.GetAttribList(handleId);

                            ArrayList attributeValues = new ArrayList();

                            for (int i = 0; i < attributes.Count; i++)
                            {
                                foreach (IndexStoreInformation indexStoreInfo in indexInformation.IndexStoreInformations)
                                {

                                    if (attributes[i].ToString() == indexStoreInfo.StoreName)
                                    {
                                        if (indexStoreInfo.IndexPosition == null)
                                            attributeValues.Add(null);
                                        else
                                        {
                                            object val = indexStoreInfo.IndexPosition.GetKey();

                                            string objValue = null;

                                            if (val is DateTime)
                                            {
                                                objValue = ((DateTime)val).Ticks.ToString();
                                            }
                                            else
                                            {
                                                objValue = val.ToString();
                                            }

                                            attributeValues.Add(objValue);
                                        }
                                        break;
                                    }
                                }

                            }
                            queryIndex.Add(handleId, attributeValues);
                            queryInfo["query-info"] = queryIndex;
                        }
                    }
                }


                if (indexInformation != null)
                {
                    foreach (IndexStoreInformation indexStoreinfo in indexInformation.IndexStoreInformations)
                    {
                        if (AttributeIndex.IsNamedTagKey(indexStoreinfo.StoreName))
                        {
                            if (indexStoreinfo.IndexPosition != null)
                                namedTagsList.Add(ConvertToNamedTag(indexStoreinfo.StoreName.ToString()), indexStoreinfo.IndexPosition.GetKey());
                        }
                        else if (indexStoreinfo.StoreName.Equals(TAG_INDEX_KEY))
                        {
                            if (indexStoreinfo.IndexPosition != null)
                                tagsList.Add(indexStoreinfo.IndexPosition.GetKey());
                        }

                    }
                }
                namedTagInfo["type"] = entry.ObjectType;
                namedTagInfo["named-tags-list"] = namedTagsList;
                queryInfo["named-tag-info"] = namedTagInfo;

                tagInfo["type"] = entry.ObjectType;
                tagInfo["tags-list"] = tagsList;
                queryInfo["tag-info"] = tagInfo;
            }
            return queryInfo;

        }

        public string ConvertToNamedTag(string indexKey)
        {
            string namedTagKey = indexKey.Replace(NAMED_TAG_PREFIX, "");
            return namedTagKey;
        }

        #region ISizable Impelementation

        public long IndexInMemorySize
        {
            get { return _queryIndexMemorySize; }
        }
       
        #endregion       
    }
}
