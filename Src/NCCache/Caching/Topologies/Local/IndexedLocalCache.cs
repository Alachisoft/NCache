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

using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.AutoExpiration;

using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Caching.Queries.Filters;

using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Caching.Enumeration;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Queries.Filters;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    /// <summary>
    /// This class provides the local storage options i.e. the actual storage of objects. It is used 
    /// by the Cache Manager, replicated cache and partitioned cache.
    /// </summary>

	internal class IndexedLocalCache : LocalCache
    {


        /// <summary> The underlying local cache used. </summary>
        private GroupIndexManager _grpIndexManager;
        private QueryIndexManager _queryIndexManager;
        private EnumerationIndex _enumerationIndex;


        /// <summary>
        /// Overloaded constructor. Takes the properties as a map.
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        /// <param name="listener">cache events listener</param>
        /// <param name="timeSched">scheduler to use for periodic tasks</param>
        public IndexedLocalCache(IDictionary cacheClasses, CacheBase parentCache, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context

, ActiveQueryAnalyzer activeQueryAnalyzer

)
            : base(cacheClasses, parentCache, properties, listener, context

, activeQueryAnalyzer

)
        {
            _grpIndexManager = new GroupIndexManager();


            IDictionary props = null;
            if (properties.Contains("indexes"))
            {
                props = properties["indexes"] as IDictionary;
            }

            _queryIndexManager = new NamedTagIndexManager(props, this, _context.CacheRoot.Name);

            if (!_queryIndexManager.Initialize(null)) _queryIndexManager = null;

            _cacheStore.ISizableQueryIndexManager = _queryIndexManager;
            _cacheStore.ISizableGroupIndexManager =  _grpIndexManager;
            _cacheStore.ISizableEvictionIndexManager = _evictionPolicy;
            _cacheStore.ISizableExpirationIndexManager =  _context.ExpiryMgr;

            _stats.MaxCount = _cacheStore.MaxCount;
            _stats.MaxSize = _cacheStore.MaxSize;
            
            if (_context.PerfStatsColl != null)
            {
                if (_queryIndexManager != null)
                    _context.PerfStatsColl.SetQueryIndexSize(_queryIndexManager.IndexInMemorySize);

                _context.PerfStatsColl.SetGroupIndexSize(_grpIndexManager.IndexInMemorySize);
            }

        }


        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            if (_queryIndexManager != null)
            {
                _queryIndexManager.Dispose();
                _queryIndexManager = null;
            }
            if (_grpIndexManager != null)
            {
                _grpIndexManager.Dispose();
                _grpIndexManager = null;
            }
        }

        #endregion

        public QueryIndexManager IndexManager
        {
            get { return _queryIndexManager; }
        }

        public sealed override TypeInfoMap TypeInfoMap
        {
            get
            {
                if (_queryIndexManager != null)
                    return _queryIndexManager.TypeInfoMap;
                else
                    return null;
            }
        }

        #region	/                 --- CacheBase ---           /

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        internal override void ClearInternal()
        {
            base.ClearInternal();
            _grpIndexManager.Clear();
           

            if (_queryIndexManager != null)
            {
                _queryIndexManager.Clear();
            }

            if (_context.PerfStatsColl != null)
            {
                _context.PerfStatsColl.SetGroupIndexSize(_grpIndexManager.IndexInMemorySize);
                
                if (_queryIndexManager != null)
                    _context.PerfStatsColl.SetQueryIndexSize(_queryIndexManager.IndexInMemorySize);
            }

        }


        /// <summary>
        ///         /// returns the keylist fullfilling the specified criteria.
        /// </summary>
        /// <param name="queryString">a string describing the search criteria.</param>
        /// <returns>a list of keys.</returns>
        internal override QueryContext SearchInternal(Predicate pred, IDictionary values, Boolean includeFilters = false)
        {
            QueryContext queryContext = new QueryContext(this);
            queryContext.AttributeValues = values;
            queryContext.CacheContext = _context.CacheRoot.Name;

            try
            {
                if (includeFilters)
                {
                    queryContext.KeyFilter = GetBucketFilter();
                    queryContext.CompoundFilter = GetCompoundFilter();
                }
                pred.Execute(queryContext, null);
                return queryContext;
            }
            catch (Exception)
            {
                throw;
                //return null;
            }
        }

        public virtual IKeyFilter GetBucketFilter()
        {
            return null;
        }

        public virtual IKeyFilter GetCompoundFilter()
        {
            return null;
        }

        /// <summary>
        ///         /// returns the keylist fullfilling the specified criteria.
        /// </summary>
        /// <param name="queryString">a string describing the search criteria.</param>
        /// <returns>a list of keys.</returns>
        internal override QueryContext DeleteQueryInternal(Predicate pred, IDictionary values)
        {
            QueryContext queryContext = new QueryContext(this);
            queryContext.AttributeValues = values;
            queryContext.CacheContext = _context.CacheRoot.Name;

            try
            {
                pred.Execute(queryContext, null);
                return queryContext;
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal CacheEntry GetInternal(object key)
        {
            return base.GetInternal(key, false, null);
        }

        internal override CacheEntry GetInternal(object key, bool isUserOperation, OperationContext operationContext)
        {
            
                CacheEntry entry = base.GetInternal(key, isUserOperation, operationContext);

                if (entry != null)
                {
                    if (operationContext != null)
                    {
                        if (operationContext.Contains(OperationContextFieldName.GenerateQueryInfo))
                        {
                            if (entry.ObjectType != null)
                            {
                                CacheEntry clone = (CacheEntry)entry.Clone();
                                clone.QueryInfo = _queryIndexManager.GetQueryInfo(key, entry);
                                return clone;
                            }
                        }
                    }

                }

                return entry;
        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        public override ArrayList GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            return _grpIndexManager.GetGroupKeys(group, subGroup);
        }

        public override CacheEntry GetGroup(object key, string group, string subGroup, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            if (_grpIndexManager.KeyExists(key, group, subGroup))
                return Get(key, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);

            return null;
        }

        public override Hashtable GetGroup(object[] keys, string group, string subGroup, OperationContext operationContext)
        {
            Hashtable result = new Hashtable();
            for (int i = 0; i < keys.Length; i++)
            {
                try
                {
                    if (_grpIndexManager.KeyExists(keys[i], group, subGroup))
                    {
                        result[keys[i]] = Get(keys[i], operationContext);
                    }
                }
                catch (StateTransferException se)
                {
                    result[keys[i]] = se;
                }
            }
            return result;
        }

        public override ArrayList DataGroupList
        {
            get
            {
                return _grpIndexManager != null ? _grpIndexManager.DataGroupList : null;
            }
        }

        /// <summary>
        /// Gets the Data group info of the item.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Group info</returns>
        public override GroupInfo GetGroupInfo(object key, OperationContext operationContext)
        {
            CacheEntry entry = Get(key, operationContext);
            GroupInfo info = null;
            if (entry != null)
            {
                if (entry.GroupInfo != null)
                    info = new GroupInfo(entry.GroupInfo.Group, entry.GroupInfo.SubGroup);
                else
                    info = new GroupInfo(null, null);
            }

            return info;
        }

        /// <summary>
        /// Gets the data groups of the items.
        /// </summary>
        /// <param name="keys">Keys of the items</param>
        /// <returns>Hashtable containing key of the item as 'key' and GroupInfo as 'value'</returns>
        public override Hashtable GetGroupInfoBulk(object[] keys, OperationContext operationContext)
        {
            Hashtable infoTahle = new Hashtable();
            HashVector entries = (HashVector)Get(keys, operationContext);
            CacheEntry currentEntry;
            GroupInfo info;
            if (entries != null)
            {
                IDictionaryEnumerator ide = entries.GetEnumerator();
                while (ide.MoveNext())
                {
                    info = null;
                    currentEntry = (CacheEntry)ide.Value;
                    if (currentEntry != null)
                    {
                        info = currentEntry.GroupInfo;
                        if (info == null) info = new GroupInfo(null, null);
                    }
                    infoTahle.Add(ide.Key, info);
                }
            }
            return infoTahle;
        }

        internal override ICollection GetTagKeys(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        { 
            switch (comparisonType)
            {
                case TagComparisonType.BY_TAG:
                    return ((NamedTagIndexManager)_queryIndexManager).GetByTag(tags[0]);

                case TagComparisonType.ANY_MATCHING_TAG:
                    return ((NamedTagIndexManager)_queryIndexManager).GetAnyMatchingTag(tags);

                case TagComparisonType.ALL_MATCHING_TAGS:
                    return ((NamedTagIndexManager)_queryIndexManager).GetAllMatchingTags(tags);
            }
            return null;
        }

        /// <summary>
        /// Remove the objects from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        public override Hashtable Remove(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            ArrayList list = _grpIndexManager.GetGroupKeys(group, subGroup);
            object[] keys = new object[list.Count];

            int i = 0;
            foreach (object key in list)
            {
                keys[i] = key;
                i++;
            }
            Hashtable table = new Hashtable();
            table = Remove(keys, ItemRemoveReason.Removed, notify, operationContext);

            if (_context.PerfStatsColl != null)
            {
                if (_queryIndexManager != null)
                    _context.PerfStatsColl.SetQueryIndexSize(_queryIndexManager.IndexInMemorySize);

                _context.PerfStatsColl.SetGroupIndexSize(_grpIndexManager.IndexInMemorySize);

            }

            return table;
        }


        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        internal override CacheAddResult AddInternal(object key, CacheEntry cacheEntry, bool isUserOperation, OperationContext operationContext)
        {
            CacheAddResult result = base.AddInternal(key, cacheEntry, isUserOperation,operationContext);
            if (result == CacheAddResult.Success || result == CacheAddResult.SuccessNearEviction)
            {
                _grpIndexManager.AddToGroup(key, cacheEntry.GroupInfo);

                 
                if (_queryIndexManager != null && cacheEntry.QueryInfo != null)
                {
                    _queryIndexManager.AddToIndex(key, cacheEntry,operationContext);
                }
            }

            if (_context.PerfStatsColl != null)
            {
                if (_queryIndexManager != null)
                    _context.PerfStatsColl.SetQueryIndexSize(_queryIndexManager.IndexInMemorySize);

                _context.PerfStatsColl.SetGroupIndexSize(_grpIndexManager.IndexInMemorySize);

            }
            return result;
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        internal override CacheInsResult InsertInternal(object key, CacheEntry cacheEntry, bool isUserOperation, CacheEntry oldEntry, OperationContext operationContext, bool updateIndex)
        {
            if (oldEntry != null)
            {
                if (!Util.CacheHelper.CheckDataGroupsCompatibility(cacheEntry.GroupInfo, oldEntry.GroupInfo))
                {
                    return CacheInsResult.IncompatibleGroup;// throw new Exception("Data group of the inserted item does not match the existing item's data group");
                }
            }

            CacheInsResult result = base.InsertInternal(key, cacheEntry, isUserOperation, oldEntry, operationContext, updateIndex);
            if (result == CacheInsResult.Success || result == CacheInsResult.SuccessNearEvicition)
            {
                _grpIndexManager.AddToGroup(key, cacheEntry.GroupInfo);

                 
                if (_queryIndexManager != null && cacheEntry.QueryInfo != null)
                {
                    _queryIndexManager.AddToIndex(key, cacheEntry,operationContext);
                }
            }
            else if ((result == CacheInsResult.SuccessOverwrite || result == CacheInsResult.SuccessOverwriteNearEviction)&& updateIndex)
            {
                if (oldEntry != null) _grpIndexManager.RemoveFromGroup(key, oldEntry.GroupInfo);
                _grpIndexManager.AddToGroup(key, cacheEntry.GroupInfo);

                 
                if (_queryIndexManager != null)
                {
                    if (oldEntry != null && oldEntry.ObjectType != null)
                    {
                        _queryIndexManager.RemoveFromIndex(key, oldEntry);
                    }

                    if (cacheEntry.QueryInfo != null)
                        _queryIndexManager.AddToIndex(key, cacheEntry,operationContext);
                }
            }

            if (_context.PerfStatsColl != null)
            {
                if (_queryIndexManager != null)
                    _context.PerfStatsColl.SetQueryIndexSize(_queryIndexManager.IndexInMemorySize);

                _context.PerfStatsColl.SetGroupIndexSize(_grpIndexManager.IndexInMemorySize);

            }

            return result;
        }


        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>item value</returns>
        internal override CacheEntry RemoveInternal(object key, ItemRemoveReason removalReason, bool isUserOperation, OperationContext operationContext)
        {
            CacheEntry e = base.RemoveInternal(key, removalReason, isUserOperation, operationContext);
            if (e != null)
            {
                _grpIndexManager.RemoveFromGroup(key, e.GroupInfo);

                 
                if (_queryIndexManager != null && e.ObjectType != null)
                {
                    _queryIndexManager.RemoveFromIndex(key, e);
                }
            }
            if (_context.PerfStatsColl != null)
            {
                if (_queryIndexManager != null)
                    _context.PerfStatsColl.SetQueryIndexSize(_queryIndexManager.IndexInMemorySize);

                _context.PerfStatsColl.SetGroupIndexSize(_grpIndexManager.IndexInMemorySize);

            }
            return e;
        }

        public override EnumerationDataChunk GetNextChunk(EnumerationPointer pointer, OperationContext operationContext)
        {
            if (_enumerationIndex == null)
                _enumerationIndex = new EnumerationIndex(this);

            EnumerationDataChunk nextChunk = _enumerationIndex.GetNextChunk(pointer);

            return nextChunk;
        }

        public override bool HasEnumerationPointer(EnumerationPointer pointer)
        {
            if (_enumerationIndex == null)
                return false;

            return _enumerationIndex.Contains(pointer);
        }

        #endregion

      

   
       
    }
}

