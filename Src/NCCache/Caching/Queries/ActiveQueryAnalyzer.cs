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
using System.Collections.Generic;
using System.Collections;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Queries.Continuous;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Queries
{
    public class ActiveQueryAnalyzer : IQueryOperationsObserver
    {

        string TAG_INDEX_KEY = "$Tag$";
        /// <summary>
        /// ICacheEventsListener to be notified of changes resulting in query result.
        /// </summary>
        private ICacheEventsListener _queryChangesListener;

        /// <summary>
        /// A list of all active predicates for which NCache is responsible to keep 
        /// up to date for a particular type of object.
        /// </summary>
        private HashVector _typeSpecificRegisteredPredicates;

        /// <summary>
        /// A list of all type specific attribute indexes used for evaluation.
        /// </summary>
        private HashVector _typeSpecificEvalIndexes;

        /// <summary>
        /// 1. string based object type as key i.e. "Application.Data.Employee"
        /// 2. Dicationary for which
        ///     a. key is the cache key
        ///     b. value is predicate holder
        /// 
        /// storing the predicates in this form will make it easy for us to know on 
        /// each operation (identified by a cache key) result sets for which registered predicates can 
        /// change.
        /// </summary>

        private HashVector _typeSpecificPredicates;


        /// <summary>
        /// Storing the SharedType in this form will make it easy for us to know on
        /// each operation (identified by a cache key) result sets for which
        /// registered predicates can change.
        /// </summary>
        protected internal Hashtable _dataSharingTypesMap;
        /// <summary>
        /// Storing the SharedType Attribindex in this form will make it easy for us
        /// to know on each operation (identified by a cache key) result sets for
        /// which registered predicates can change.
        /// </summary>
        protected internal Dictionary<string, AttributeIndex> _sharedAttributeIndex;
        protected internal string _cacheContext;


        public AsyncProcessor _queryEvaluationProcessor;
        public AsyncProcessor _notificationAsyncProcessor;
        private CacheRuntimeContext _context = new CacheRuntimeContext();
        private static bool _isPersistEnabled = false;
        private Object _syncLock = new object();

        class AsynchronousNotificationTask : AsyncProcessor.IAsyncTask
        {
            private object _key;
            private QueryChangeType _changeType;
            private OperationContext _operationContext;
            private EventContext _eventContext;
            private List<CQCallbackInfo> _callbacks;
            private ActiveQueryAnalyzer _parent;

            public AsynchronousNotificationTask(ActiveQueryAnalyzer parent, object key,QueryChangeType changeType,List<CQCallbackInfo> callabacks,OperationContext opContext,EventContext eventContext)
            {
                _parent = parent;
                _key = key;
                _changeType = changeType;
                _operationContext = opContext;
                _eventContext = eventContext;
                _callbacks = callabacks;
            }

            public void Process()
            {
                if (_parent != null)
                {
                    _parent.RaiseQueryChangeNotification(_key, _changeType, _callbacks, _operationContext, _eventContext);
                }
            }
        }

        class AsyncTask : AsyncProcessor.IAsyncTask
        {
            private object _key;
            private MetaInformation _metaInfo;
            LocalCacheBase _cache;
            string _cacheContext;
            bool _notify;
            ActiveQueryAnalyzer _queryAnalyzer;
            QueryChangeType _taskType;
            OperationContext _operationContext;
            EventContext _eventContext;

            public AsyncTask(ActiveQueryAnalyzer queryAnalyzer, object key, MetaInformation metaInfo, LocalCacheBase cache, string cacheContext, bool notify, QueryChangeType taskType, OperationContext operationContext, EventContext eventContext)
            {
                _key = key;
                _metaInfo = metaInfo;
                _cache = cache;
                _cacheContext = cacheContext;
                _notify = notify;
                _taskType = taskType;
                _queryAnalyzer = queryAnalyzer;
                _operationContext = operationContext;
                _eventContext = eventContext;

            }

            void AsyncProcessor.IAsyncTask.Process()
            {
                _queryAnalyzer.NotifyModifiedQueries((string)_key, _metaInfo, _cache, _cacheContext, _taskType, _notify, _operationContext, _eventContext);
            }
        }

        class ClearAsyncTask : AsyncProcessor.IAsyncTask
        {
            private object _key;
            List<CQCallbackInfo> _modifiedQueries;
            ActiveQueryAnalyzer _queryAnalyzer;
            OperationContext _operationContext;
            EventContext _eventContext;

            public ClearAsyncTask(ActiveQueryAnalyzer queryAnalyzer, object key, List<CQCallbackInfo> modifiedQueries, OperationContext operationContext, EventContext eventContext)
            {
                _queryAnalyzer = queryAnalyzer;
                _key = key;
                _modifiedQueries = modifiedQueries;
                _operationContext = operationContext;
                _eventContext = eventContext;
            }

            void AsyncProcessor.IAsyncTask.Process()
            {
                _queryAnalyzer._queryChangesListener.OnActiveQueryChanged((string)_key, QueryChangeType.Remove, _modifiedQueries, _operationContext, _eventContext);
            }
        }




        internal HashVector TypeSpecificRegisteredPredicates
        {
            get { return _typeSpecificRegisteredPredicates; }
            set { _typeSpecificRegisteredPredicates = value; }
        }

        internal HashVector TypeSpecificEvalIndexes
        {
            get { return _typeSpecificEvalIndexes; }
            set { _typeSpecificEvalIndexes = value; }
        }

        private Object SyncLock { get { return _syncLock; } }

        internal HashVector TypeSpecificPredicates
        {
            get { return _typeSpecificPredicates; }
            set { _typeSpecificPredicates = value; }
        }

        static ActiveQueryAnalyzer()
        {
            _isPersistEnabled = ServiceConfiguration.EventsPersistence;
        }

        internal ActiveQueryAnalyzer(ICacheEventsListener queryChangeListener, IDictionary indexedTypes, string cacheContext, CacheRuntimeContext context, Hashtable dataSharingKnownTypes)
        {
            this._queryChangesListener = queryChangeListener;
            _typeSpecificPredicates = new HashVector();
            _typeSpecificRegisteredPredicates = new HashVector();
            _typeSpecificEvalIndexes = new HashVector();
            _context = context;
            _cacheContext = cacheContext;
            _dataSharingTypesMap = dataSharingKnownTypes;
            if (_dataSharingTypesMap != null && _dataSharingTypesMap.Count > 0)
            {
                _sharedAttributeIndex = new Dictionary<String, AttributeIndex>();
            }
            _queryEvaluationProcessor = new AsyncProcessor(context.NCacheLog);
            _queryEvaluationProcessor.Start();

            _notificationAsyncProcessor = new AsyncProcessor(2,context.NCacheLog);
            _notificationAsyncProcessor.Start();

            if (indexedTypes.Contains("indexes"))
            {
                InitializeEvalIndexes(indexedTypes["indexes"] as IDictionary, _cacheContext);
            }
        }

        
        #region Datasharing Code Change

        public Boolean CheckForSameClassNames(Hashtable knownSharedClasses)
        {
            ICollection coll = knownSharedClasses.Values;

            Array values = Array.CreateInstance(typeof(System.Object), coll.Count);

            coll.CopyTo(values, 0);

            for (int outer = 0; outer < values.Length - 1; outer++)
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


        public void manipulateDataSharing(Hashtable indexClasses)
        {
            if (_dataSharingTypesMap != null && _dataSharingTypesMap.Count > 0 && indexClasses != null)
            {
                IEnumerator ieDataSharingTypeMap = _dataSharingTypesMap.GetEnumerator();
                while (ieDataSharingTypeMap.MoveNext())
                {
                    DictionaryEntry current_1 = (DictionaryEntry)ieDataSharingTypeMap.Current;
                    Hashtable typeHashMap = (Hashtable)((current_1.Value is Hashtable) ? current_1.Value : null);
                    if (typeHashMap != null && typeHashMap.ContainsKey("known-classes"))
                    {
                        Hashtable knownSharedClasses = (Hashtable)typeHashMap["known-classes"];
                        if (CheckForSameClassNames(knownSharedClasses))
                            createSharedTypeAttributeIndex(knownSharedClasses, indexClasses);
                    }
                }
            }
        }

        public void createSharedTypeAttributeIndex(Hashtable knownSharedClasses, Hashtable indexedClasses)
        {
            Hashtable commonRBStore = new Hashtable();
            Dictionary<string, ActiveQueryEvaluationIndex> sharedAttributeIndexMap = new Dictionary<string, ActiveQueryEvaluationIndex>();
            IEnumerator iteouterSharedTypes = knownSharedClasses.GetEnumerator();

            commonRBStore.Add(this.TAG_INDEX_KEY, new HashStore());

            while (iteouterSharedTypes.MoveNext())
            {
                DictionaryEntry outerEntry = (DictionaryEntry)iteouterSharedTypes.Current;
                Hashtable outerEntryValue = (Hashtable)outerEntry.Value;
                string name = (string)outerEntryValue["name"];
                string[] temp = name.Split(':');
                string outerTypeName = temp[0];
                //Create Attribute Index even if not queryindexed
                sharedAttributeIndexMap.Add(outerTypeName, new ActiveQueryEvaluationIndex(_cacheContext, outerTypeName));

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
                                    string[] temp1 = name1.Split(':');
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
                                                        HashStore commonRB = (HashStore)commonRBStore[outerTypeName + ":" + outerAttributeName];
                                                        commonRBStore.Add(innerTypeName + ":" + innerAttributeName, commonRB);
                                                    }
                                                    break;
                                                }
                                                else
                                                {
                                                    HashStore commonRB = new HashStore();
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
                    System.Collections.Generic.KeyValuePair<string, ActiveQueryEvaluationIndex> outerEntry = (System.Collections.Generic.KeyValuePair<string, ActiveQueryEvaluationIndex>)iteSharedIndexMap.Current;
   
                    string outerTypeName = (string)outerEntry.Key;
                    AttributeIndex outerSharedIndex = (ActiveQueryEvaluationIndex)outerEntry.Value;
                    foreach (System.Collections.Generic.KeyValuePair<string, ActiveQueryEvaluationIndex> innerEntry in sharedAttributeIndexMap)
                    {
                        string innerTypeName = (string)innerEntry.Key;
                        if (!innerTypeName.Equals(outerTypeName))
                        {
                            ActiveQueryEvaluationIndex innerSharedIndex = (ActiveQueryEvaluationIndex)innerEntry.Value;
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
        #endregion




        private void InitializeEvalIndexes(IDictionary indexedTypes, string cacheContext)
        {
            if (indexedTypes != null)
            {
                if (indexedTypes.Contains("index-classes"))
                {
                    Hashtable indexClasses = indexedTypes["index-classes"] as Hashtable;
                    manipulateDataSharing(indexClasses);
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
                           lock (SyncLock)
                           {
                                if (attribList.Count > 0)
                                {
                                    if (_sharedAttributeIndex != null && _sharedAttributeIndex.ContainsKey(typename))
                                    {
                                        AttributeIndex index = _sharedAttributeIndex[typename];
                                        index.Initialize(attribList);
                                        TypeSpecificEvalIndexes[typename] = (ActiveQueryEvaluationIndex)index;
                                    }
                                    else
                                    {
                                        ActiveQueryEvaluationIndex index = new ActiveQueryEvaluationIndex(attribList, cacheContext, typename);
                                        TypeSpecificEvalIndexes[typename] = index;
                                    }
                                }
                            }                           
                        }
                    }
                }
            }
        }


        /// <summary>
        /// return continuous query state on this node.
        /// </summary>
        /// <returns></returns>
        internal ContinuousQueryStateInfo GetStateInfo()
        {
            ContinuousQueryStateInfo stateInfo = new ContinuousQueryStateInfo();
            stateInfo.IsPartial = false;
            //stateInfo.TypeSpecificPredicates = TypeSpecificRegisteredPredicates;
            stateInfo.TypeSpecificPredicates = TypeSpecificPredicates;
            stateInfo.TypeSpecificRegisteredPredicates = TypeSpecificRegisteredPredicates;
            stateInfo.TypeSpecificEvalIndexes = TypeSpecificEvalIndexes;

            return stateInfo;
        }


        internal IList<PredicateHolder> GetPredicatesForType(string type)
        {
            IList<PredicateHolder> result = null;
            lock (SyncLock)
            {
                if (TypeSpecificRegisteredPredicates.ContainsKey(type))
                    result = (IList<PredicateHolder>)TypeSpecificRegisteredPredicates[type];
            }
            return result;
        }

        internal void RegisterPredicate(string type, string commandText, IDictionary queryValues, Alachisoft.NCache.Caching.Queries.Filters.Predicate predicate, string queryId, ClusteredArrayList keys)
        {
            PredicateHolder holder = new PredicateHolder();
            holder.QueryId = queryId;
            holder.Predicate = predicate;
            holder.AttributeValues = queryValues;
            holder.CommandText = commandText;
            holder.ObjectType = type;

            lock (SyncLock)
            {
                if (TypeSpecificRegisteredPredicates.ContainsKey(type))
                {
                    IList<PredicateHolder> holders = (IList<PredicateHolder>)TypeSpecificRegisteredPredicates[type];
                    holders.Add(holder);
                }
                else
                {
                    ClusteredList<PredicateHolder> holders = new ClusteredList<PredicateHolder>();
                    holders.Add(holder);
                    TypeSpecificRegisteredPredicates[type] = holders;

                }
                if (TypeSpecificPredicates.ContainsKey(type))
                {
                    HashVector predicateKeys = (HashVector)TypeSpecificPredicates[type];
                    foreach (string key in keys)
                    {
                        if (predicateKeys.ContainsKey(key))
                        {
                            IList<PredicateHolder> predicates = (IList<PredicateHolder>)predicateKeys[key];
                            predicates.Add(holder);
                        }
                        else
                        {
                            IList<PredicateHolder> predicates = new ClusteredList<PredicateHolder>();
                            predicates.Add(holder);
                            predicateKeys[key] = predicates;
                        }
                    }
                }
                else
                {
                    HashVector predicateKeys = new HashVector();
                    foreach (string key in keys)
                    {
                        ClusteredList<PredicateHolder> predicates = new ClusteredList<PredicateHolder>();
                        predicates.Add(holder);
                        predicateKeys[key] = predicates;
                    }
                    TypeSpecificPredicates[type] = predicateKeys;
                }
            }
        }

        /// <summary>
        /// Called by underlying cache for unregistering predicates.
        /// </summary>
        /// <param name="predicate"></param>
        internal void UnRegisterPredicate(string queryId)
        {
            //Create a holder to check for equality
            PredicateHolder holder = new PredicateHolder();
            holder.QueryId = queryId;

            List<string> typePredicatesKeysToRemove = new List<string>();
            lock (SyncLock)
            {
                IDictionaryEnumerator ide = _typeSpecificRegisteredPredicates.GetEnumerator();
                while (ide.MoveNext())
                {
                    int index = ((IList)ide.Value).IndexOf(holder);
                    if (index != -1)
                    {
                        string objectType = ((PredicateHolder)((IList)ide.Value)[index]).ObjectType;

                        if (TypeSpecificPredicates.ContainsKey(objectType))
                        {
                            HashVector typeSpecificPredicates = (HashVector)TypeSpecificPredicates[objectType];
                            IDictionaryEnumerator iden = typeSpecificPredicates.GetEnumerator();
                            ClusteredArrayList toBeRemoved = new ClusteredArrayList();
                            while (iden.MoveNext())
                            {
                                IList list = (IList)iden.Value;
                                list.Remove(holder);
                                if (list.Count == 0)
                                {
                                    toBeRemoved.Add(iden.Key);
                                }
                            }
                            foreach (object key in toBeRemoved)
                                typeSpecificPredicates.Remove(key);


                            if (typeSpecificPredicates.Count == 0)
                            {
                                TypeSpecificPredicates.Remove(objectType);
                            }
                        }
                        IList typeList = (IList)ide.Value;
                        typeList.RemoveAt(index);
                        if (typeList.Count == 0)
                        {
                            typePredicatesKeysToRemove.Add(ide.Key.ToString());
                        }
                        break;

                    }
                }
                foreach (string key in typePredicatesKeysToRemove)
                {
                    _typeSpecificRegisteredPredicates.Remove(key);
                }
            }
        }

        #region IQueryOperationsObserver Members

        void IQueryOperationsObserver.OnItemAdded(object key, MetaInformation metaInfo, LocalCacheBase cache, string cacheContext, bool notify, OperationContext operationContext, EventContext eventContext)
        {
            if (_isPersistEnabled)
                NotifyModifiedQueries((string)key, metaInfo, cache, cacheContext, QueryChangeType.Add, notify, operationContext, eventContext);
            else
            {
                lock (_queryEvaluationProcessor)
                {
                    _queryEvaluationProcessor.Enqueue(new AsyncTask(this, key, metaInfo, cache, cacheContext, notify, QueryChangeType.Add, operationContext, eventContext));
                }
            }
        }

        void IQueryOperationsObserver.OnItemUpdated(object key, MetaInformation metaInfo, LocalCacheBase cache, string cacheContext, bool notify, OperationContext operationContext, EventContext eventContext)
        {
            if (_isPersistEnabled)
                NotifyModifiedQueries((string)key, metaInfo, cache, cacheContext, QueryChangeType.Update, notify, operationContext, eventContext);
            else
            {
                lock (_queryEvaluationProcessor)
                {
                    _queryEvaluationProcessor.Enqueue(new AsyncTask(this, key, metaInfo, cache, cacheContext, notify, QueryChangeType.Update, operationContext, eventContext));
                }
            }
        }

        void IQueryOperationsObserver.OnItemRemoved(object key, MetaInformation metaInfo, LocalCacheBase cache, string cacheContext, bool notify, OperationContext operationContext, EventContext eventContext)
        {
            if (_isPersistEnabled)
                NotifyModifiedQueries((string)key, metaInfo, cache, cacheContext, QueryChangeType.Remove, notify, operationContext, eventContext);
            else
            {
                lock (_queryEvaluationProcessor)
                {
                    _queryEvaluationProcessor.Enqueue(new AsyncTask(this, key, metaInfo, cache, cacheContext, notify, QueryChangeType.Remove, operationContext, eventContext));
                }
            }
        }

        #endregion

        private void NotifyModifiedQueries(string key, MetaInformation metaInfo, LocalCacheBase cache, string cacheContext, QueryChangeType changeType, bool notify, OperationContext operationContext, EventContext eventContext)
        {
            if (metaInfo == null) return;
            if (metaInfo.Type == null) return;

            string baseType = metaInfo.Type;
            System.Collections.Generic.List<String> types = new List<string>();
            types.Add(baseType);           
            foreach (String type in types)
            {
                ActiveQueryEvaluationIndex index = null;
                try
                {
                    List<CQCallbackInfo> modifiedQueries = new List<CQCallbackInfo>();
                    Hashtable indexedAttributes;
                    if (changeType == QueryChangeType.Add || changeType == QueryChangeType.Update)
                    {
                        indexedAttributes = metaInfo.AttributeValues;
                        if (TypeSpecificEvalIndexes.ContainsKey(type))
                        {
                            index =(ActiveQueryEvaluationIndex)TypeSpecificEvalIndexes[type];
                        }
                        else
                        {
                            index = new ActiveQueryEvaluationIndex(new ArrayList(indexedAttributes.Keys), cacheContext, type);
                            lock (SyncLock)
                            {
                                TypeSpecificEvalIndexes.Add(type, index);
                            }
                        }
                        lock (SyncLock)
                        {
                            index.AddToIndex(key, indexedAttributes);
                        }
                    }
                    EventId eventId = null;
                    if (eventContext != null)
                    {
                        eventId = eventContext.EventID;
                    }

                    if (changeType == QueryChangeType.Add)
                    {
                        if (eventId != null)
                            eventId.QueryChangeType = QueryChangeType.Add;
                        modifiedQueries.AddRange(GetInclusion(key, type, index, cache, cacheContext, changeType));
                    }
                    else if (changeType == QueryChangeType.Update)
                    {
                        List<CQCallbackInfo> excludedQueries = GetExclusion(key, type, index, cache, cacheContext);
                        if (excludedQueries.Count > 0 && notify)
                        {
                            if (eventId != null)
                            {
                                eventId = (EventId)eventId.Clone();
                                eventContext = (EventContext)eventContext.Clone();
                                eventId.QueryChangeType = QueryChangeType.Remove;
                            }

                            try
                            {
                               RaiseQueryChangeNotificationInternal((string)key, QueryChangeType.Remove, excludedQueries, operationContext, eventContext);
                            }
                            catch (Exception e)
                            {
                                if (_context.NCacheLog != null && _context.NCacheLog.IsErrorEnabled)
                                {
                                    _context.NCacheLog.Error("ActiveQueryAnalyzer.NotifyModifiedQueries", "Error occurred while raising notification. " + e.ToString());
                                    _context.NCacheLog.CriticalInfo("ActiveQueryAnalyzer.NotifyModifiedQueries", "cache-key :" + key + " change_type :" + changeType);
                                }
                            }
                        }
                        List<CQCallbackInfo> retainedQueries = GetRetention(key, type, index, cache, cacheContext);
                        if (retainedQueries.Count > 0 && notify)
                        {
                            if (eventId != null)
                            {
                                eventId = (EventId)eventId.Clone();
                                eventContext = (EventContext)eventContext.Clone();
                                eventId.QueryChangeType = QueryChangeType.Update;
                            }

                            try
                            {
                                RaiseQueryChangeNotificationInternal((string)key, QueryChangeType.Update, retainedQueries, operationContext, eventContext);
                            }
                            catch (Exception e)
                            {
                                if (_context.NCacheLog != null && _context.NCacheLog.IsErrorEnabled)
                                {
                                    _context.NCacheLog.Error("ActiveQueryAnalyzer.NotifyModifiedQueries", "Error occurred while raising notification. " + e.ToString());
                                    _context.NCacheLog.CriticalInfo("ActiveQueryAnalyzer.NotifyModifiedQueries", "cache-key :" + key + " change_type :" + changeType);
                                }
                            }
                        }
                        List<CQCallbackInfo> includedQueries = GetInclusion(key, type, index, cache, cacheContext, changeType);
                        if (includedQueries.Count > 0 && notify)
                        {
                            if (eventId != null)
                            {
                                eventId = (EventId)eventId.Clone();
                                eventContext = (EventContext)eventContext.Clone();
                                eventId.QueryChangeType = QueryChangeType.Add;
                            }
                            try
                            {
                                RaiseQueryChangeNotificationInternal((string)key, QueryChangeType.Add, includedQueries, operationContext, eventContext);
                            }
                            catch (Exception e)
                            {
                                if (_context.NCacheLog != null && _context.NCacheLog.IsErrorEnabled)
                                {
                                    _context.NCacheLog.Error("ActiveQueryAnalyzer.NotifyModifiedQueries", "Error occurred while raising notification. " + e.ToString());
                                    _context.NCacheLog.CriticalInfo("ActiveQueryAnalyzer.NotifyModifiedQueries", "cache-key :" + key + " change_type :" + changeType);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (TypeSpecificPredicates.ContainsKey(type))
                        {
                            HashVector predicateKeys = (HashVector)TypeSpecificPredicates[type];
                            lock (SyncLock)
                            {
                                if (predicateKeys.ContainsKey(key))
                                {
                                    foreach (PredicateHolder existingholder in ((IList)predicateKeys[key]))
                                    {
                                        if (existingholder != null)
                                        {
                                            CQCallbackInfo info = new CQCallbackInfo();
                                            info.CQId = existingholder.QueryId;
                                            modifiedQueries.Add(info);
                                        }
                                    }
                                    predicateKeys.Remove(key);
                                }
                            }
                            if (eventId != null)
                            {
                                eventId.QueryChangeType = QueryChangeType.Remove;
                            }
                        }
                    }

                    if (modifiedQueries.Count > 0 && notify)
                    {
                        try
                        {
                            RaiseQueryChangeNotificationInternal((string)key, changeType, modifiedQueries, operationContext, eventContext);
                        }
                        catch (Exception e)
                        {
                            if (_context.NCacheLog != null && _context.NCacheLog.IsErrorEnabled)
                            {
                                _context.NCacheLog.Error("ActiveQueryAnalyzer.NotifyModifiedQueries", "Error occurred while raising notification. " + e.ToString());
                                _context.NCacheLog.CriticalInfo("ActiveQueryAnalyzer.NotifyModifiedQueries", "cache-key :" + key + " change_type :" + changeType);
                            }
                        }
                    }
                }
                finally
                {
                    if (index != null)
                    {
                        index.Clear();
                    }
                }
            }
        }

        private void RaiseQueryChangeNotificationInternal(object key, QueryChangeType changeType, List<CQCallbackInfo> activeQueries, OperationContext operationContext, EventContext eventContext)
        {
            if (_isPersistEnabled)
                RaiseQueryChangeNotification(key, changeType, activeQueries, operationContext, eventContext);
            else
            {
                lock (_notificationAsyncProcessor)
                {
                    _notificationAsyncProcessor.Enqueue(new AsynchronousNotificationTask(this,key,changeType,activeQueries,operationContext,eventContext));
                }
            }
        }

        public void RaiseQueryChangeNotification(object key, QueryChangeType changeType, List<CQCallbackInfo> activeQueries, OperationContext operationContext, EventContext eventContext)
        {
            if (_queryChangesListener != null)
                _queryChangesListener.OnActiveQueryChanged(key, changeType, activeQueries, operationContext, eventContext);
        }

        public void PrintResultSet()
        {
            Console.WriteLine("Printing Result set : " + TypeSpecificPredicates == null);
            if (TypeSpecificPredicates != null)
            {
               IDictionaryEnumerator ide = TypeSpecificPredicates.GetEnumerator();

                while (ide.MoveNext())
                {

                    IDictionary predicateKeys = (IDictionary)ide.Key;

                    Console.WriteLine("Typexxx :" + ide.Key.ToString() + " Predicate Keys count :" + predicateKeys.Count);
                }
            }
        }
        private List<CQCallbackInfo> GetInclusion(string key, string type, AttributeIndex index, LocalCacheBase cache, string cacheContext, QueryChangeType changeType)
        {
            List<CQCallbackInfo> modifiedQueries = new List<CQCallbackInfo>();
           
            if (TypeSpecificRegisteredPredicates.ContainsKey(type))
            {
                ClusteredList<PredicateHolder> predicateHolders = (ClusteredList<PredicateHolder>)TypeSpecificRegisteredPredicates[type];
                PredicateHolder[] holdersArray = null;
                if (predicateHolders != null)
                {
                    lock (SyncLock)
                    {
                        holdersArray = predicateHolders.ToArray();
                    }
                }

                if (holdersArray != null)
                {

                    foreach (PredicateHolder holder in holdersArray)
                    {
                        if (holder == null)
                        {
                            lock(SyncLock)
                            {
                                predicateHolders.Remove(holder);
                            }
                            continue;
                        }
                        try
                        {
                            ClusteredArrayList keys;
                            keys = holder.Predicate.ReEvaluate(index, cache, holder.AttributeValues, cacheContext);
                            if (keys.Count > 0)
                            {
                                CQCallbackInfo info = new CQCallbackInfo();
                                info.CQId = holder.QueryId;
                                lock(SyncLock)
                                {
                                    if (TypeSpecificPredicates.ContainsKey(type))
                                    {
                                        HashVector predicateKeys = (HashVector)TypeSpecificPredicates[type];
                                        if (predicateKeys.ContainsKey(key))
                                        {
                                            IList existingholders =  (IList)predicateKeys[key];
                                            if (!existingholders.Contains(holder))
                                            {
                                                existingholders.Add(holder);
                                                modifiedQueries.Add(info);
                                            }
                                        }
                                        else
                                        {
                                            ClusteredList<PredicateHolder> newHolders = new ClusteredList<PredicateHolder>();
                                            newHolders.Add(holder);
                                            lock (SyncLock)
                                            {
                                                predicateKeys.Add(key, newHolders);
                                            }
                                            modifiedQueries.Add(info);
                                        }
                                    }
                                    else
                                    {
                                        HashVector predicateKeys = new HashVector();
                                        ClusteredList<PredicateHolder> holders = new ClusteredList<PredicateHolder>();
                                        holders.Add(holder);
                                        predicateKeys.Add(key, holders);
                                        lock (SyncLock)
                                        {
                                            TypeSpecificPredicates.Add(type, predicateKeys);
                                        }
                                        modifiedQueries.Add(info);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (_context.NCacheLog != null && _context.NCacheLog.IsErrorEnabled)
                            {
                                _context.NCacheLog.Error("ActiveQueryAnalyzer.GetInclusion", e.ToString());
                                if (holder != null)
                                    _context.NCacheLog.Error("ActiveQueryAnalyzer.GetInclusion", "query :" + holder.CommandText);
                            }
                        }
                    }
                }
            
            }
            return modifiedQueries;
        }

        private List<CQCallbackInfo> GetExclusion(string key, string type, AttributeIndex index, LocalCacheBase cache, string cacheContext)
        {
            List<CQCallbackInfo> removedQueries = new List<CQCallbackInfo>();
            Exception exception = null;
            if (TypeSpecificPredicates.ContainsKey(type))
            {
                HashVector predicateKeys = (HashVector)TypeSpecificPredicates[type];
                if (predicateKeys.ContainsKey(key))
                {
                    IList existingholders = (IList)predicateKeys[key];
                    IList existingholdersTemp = null;
                    ClusteredList<PredicateHolder> holdersToRemove = new ClusteredList<PredicateHolder>();
                    //to avoid collection modified exception
                    lock (SyncLock)
                    {
                        if (existingholders != null)
                        {
                            existingholdersTemp = new ClusteredList<PredicateHolder>(existingholders.Count);
                            foreach (object holder in existingholders)
                                existingholdersTemp.Add(holder);
                        }
                    }

                    foreach (PredicateHolder existingholder in existingholdersTemp)
                    {
                        if (existingholder == null) continue;
                        try
                        {
                            ClusteredArrayList keys = existingholder.Predicate.ReEvaluate(index, cache, existingholder.AttributeValues, cacheContext);
                            if (keys.Count == 0) //  key not part of this predicate anymore
                            {
                                CQCallbackInfo info = new CQCallbackInfo();
                                info.CQId = existingholder.QueryId;
                                removedQueries.Add(info);

                                holdersToRemove.Add(existingholder);
                            }
                        }
                        catch (Exception e)
                        {
                            exception = null;
                            if (_context.NCacheLog != null && _context.NCacheLog.IsErrorEnabled)
                            {
                                _context.NCacheLog.Error("ActiveQueryAnalyzer.GetExclusion", e.ToString());
                                if (existingholder != null)
                                    _context.NCacheLog.Error("ActiveQueryAnalyzer.GetExclusion", "query :" + existingholder.CommandText);
                            }
                        }
                    }

                    lock (SyncLock)
                    {
                        foreach (PredicateHolder holder in holdersToRemove)
                        {
                            existingholders.Remove(holder);
                        }

                        if (existingholders.Count == 0)
                        {
                            predicateKeys.Remove(key);
                        }

                        if (predicateKeys.Count == 0)
                        {
                            TypeSpecificPredicates.Remove(type);
                        }
                    }
                }
            }

            return removedQueries;
        }

        private List<CQCallbackInfo> GetRetention(string key, string type, AttributeIndex index, LocalCacheBase cache, string cacheContext)
        {
            List<CQCallbackInfo> updatedQueries = new List<CQCallbackInfo>();
            
            if (TypeSpecificPredicates.ContainsKey(type))
            {
                HashVector predicateKeys = (HashVector)TypeSpecificPredicates[type];
                if (predicateKeys.ContainsKey(key))
                {
                    ClusteredList<PredicateHolder> existingholders = (ClusteredList<PredicateHolder>)predicateKeys[key];
                    //Copy to avoid enumeration modified exception
                    lock (SyncLock)
                    {
                        if (existingholders != null)
                        {
                            existingholders = (ClusteredList<PredicateHolder>)existingholders.Clone();
                        }
                    }
                    foreach (PredicateHolder existingholder in existingholders)
                    {
                        if (existingholder == null) continue;
                        try
                        {
                            ClusteredArrayList keys = existingholder.Predicate.ReEvaluate(index, cache, existingholder.AttributeValues, cacheContext);
                            if (keys.Count > 0) //  key updated for this predicate
                            {
                                CQCallbackInfo info = new CQCallbackInfo();
                                info.CQId = existingholder.QueryId;
                                updatedQueries.Add(info);
                            }
                        }
                        catch (Exception e)
                        {
                            if (_context.NCacheLog != null && _context.NCacheLog.IsErrorEnabled)
                            {
                                _context.NCacheLog.Error("ActiveQueryAnalyzer.GetRetension", e.ToString());
                                if (existingholder != null)
                                    _context.NCacheLog.Error("ActiveQueryAnalyzer.GetRetension", "query :" + existingholder.CommandText);
                            }
                        }
                    }
                    while (existingholders.Remove(null)) ;
                }
            }
            return updatedQueries;
        }


        public ClusteredArrayList Search(string queryId)
        {
            ClusteredArrayList keys = new ClusteredArrayList();

            //Create a holder to check for equality
            PredicateHolder holder = new PredicateHolder();
            holder.QueryId = queryId;

            lock (SyncLock)
            {
                foreach (IList<PredicateHolder> holders in TypeSpecificRegisteredPredicates.Values)
                {
                    if (holders != null)
                    {
                        int index = -1;
                        lock (holders)
                        {
                            index = holders.IndexOf(holder);
                        }
                        if (index != -1)
                        {
                            string objectType = holders[index].ObjectType;
                            if (TypeSpecificPredicates.ContainsKey(objectType))
                            {
                                IDictionaryEnumerator ide = ((IDictionary)TypeSpecificPredicates[objectType]).GetEnumerator();
                                {
                                    while (ide.MoveNext())
                                    {
                                        IList value = (IList)ide.Value;
                                        if(value.Contains(holder))
                                            keys.Add(ide.Key);
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            return keys;
        }

        public void Clear(OperationContext operationContext, EventContext eventContext)
        {
            if (_typeSpecificRegisteredPredicates != null)
            {
                lock (SyncLock)
                {
                    IDictionaryEnumerator ide = TypeSpecificPredicates.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        if (ide.Value != null)
                        {
                            IDictionary dict = (IDictionary)ide.Value;
                            dict.Clear();
                        }

                    }
                }



            }
        }

        public void Dispose()
        {
            Clear(null, null);


            if (_typeSpecificRegisteredPredicates != null)
            {
                _typeSpecificRegisteredPredicates = null;
            }
            if (_typeSpecificPredicates != null)
            {
                _typeSpecificPredicates = null;
            }
            if (_typeSpecificEvalIndexes != null)
            {
                _typeSpecificEvalIndexes.Clear();
                _typeSpecificEvalIndexes = null;
            }

            if (_queryEvaluationProcessor != null)
            {
                _queryEvaluationProcessor.Stop();
                _queryEvaluationProcessor = null;
            }

            if (_notificationAsyncProcessor != null)
            {
                _notificationAsyncProcessor.Stop();
                _notificationAsyncProcessor = null;
            }
        }

        public bool IsRegistered(object key, MetaInformation metaInfo)
        {
            if (metaInfo == null) return false;
            if (metaInfo.Type == null) return false;
            string baseType = metaInfo.Type;
            lock (SyncLock)
            {
                if (TypeSpecificRegisteredPredicates.ContainsKey(baseType))
                {
                    return true;
                }
           }
            return false;
        }
    }
}
