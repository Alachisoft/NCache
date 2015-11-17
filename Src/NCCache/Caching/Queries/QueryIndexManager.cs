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
using System.Text;
using System.Collections;
using System.Reflection;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Serialization.Surrogates;
using System.Collections.Generic;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Caching.Queries
{
    internal class QueryIndexManager : ISizableIndex
    {
        static bool disableException = false;
        //in case of DisableException is true, exception will not be thrown, and return new attribute index. 

        public static bool DisableException
        {
            get
            {
                String str = System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.DisableIndexNotDefinedException"];
                if (!string.IsNullOrEmpty(str))
                    disableException = Convert.ToBoolean(str);

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
        private Topologies.Local.IndexedLocalCache _cache;
        private IDictionary _props;
      
        protected string _cacheName;

        protected TypeInfoMap _typeMap;
        protected Hashtable _indexMap;
        protected long _queryIndexMemorySize;

        public QueryIndexManager(IDictionary props, Topologies.Local.IndexedLocalCache cache, string cacheName)
        {
            _indexMap = new Hashtable();
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

        public Hashtable IndexMap
        {
            get { return _indexMap; }
        }

        internal virtual bool Initialize()
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
                                _indexMap[typename] = new AttributeIndex(attribList, _cacheName, typename, _typeMap);
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

        public virtual void AddToIndex(object key, object value, OperationContext operationContext)
        {
            CacheEntry entry = (CacheEntry)value;
            if(entry==null ) return;
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
                _asyncProcessor.Enqueue(new IndexAddTask(this, key, value, operationContext));
            }
        }

        //public virtual void RemoveFromIndex(object key, string value)
        //{
        //    if (value == null)
        //    {
        //        return;
        //    }
        //    lock (_indexMap.SyncRoot)
        //    {
        //        string type = value.ToString();
        //        if (_indexMap.Contains(type))
        //        {
        //            IQueryIndex index = (IQueryIndex)_indexMap[type];
        //            long prevSize = index.IndexInMemorySize;
        //            index.RemoveFromIndex(key);
        //            this._queryIndexMemorySize += index.IndexInMemorySize - prevSize;
        //        }
        //    }
        //}

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
            CacheEntry entry = (CacheEntry)value;
            if (entry.ObjectType == null)
                return queryInfo;
            IQueryIndex index = (IQueryIndex)_indexMap[entry.ObjectType];
            IndexInformation indexInformation = _cache.GetInternal(key).IndexInfo;
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
            return queryInfo;
        }

        #region ISizable Impelementation

        public long IndexInMemorySize
        {
            get { return _queryIndexMemorySize; }
        }

        #endregion      
    }
}
