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
using System.Threading;
using System.Reflection;
using System.IO;


using Alachisoft.NCache.Storage;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Caching.AutoExpiration;


using Alachisoft.NCache.Parser;
using Alachisoft.NCache.Caching.Queries.Filters;


using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Caching.Enumeration;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    /// <summary>
    /// This class provides the local storage options i.e. the actual storage of objects. It is used 
    /// by the Cache Manager and  partitioned cache.
    /// </summary>
	internal class IndexedLocalCache : LocalCache
    {


        /// <summary> The underlying local cache used. </summary>
        private QueryIndexManager _queryIndexManager;
        private EnumerationIndex _enumerationIndex;


        /// <summary>
        /// Overloaded constructor. Takes the properties as a map.
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        /// <param name="listener">cache events listener</param>
        /// <param name="timeSched">scheduler to use for periodic tasks</param>
        public IndexedLocalCache(IDictionary cacheClasses, CacheBase parentCache, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base(cacheClasses, parentCache, properties, listener, context)
        {
            IDictionary props = null;
            if (properties.Contains("indexes"))
            {
                props = properties["indexes"] as IDictionary;
            }

            _queryIndexManager = new QueryIndexManager(props, this, _context.CacheRoot.Name);
            if (!_queryIndexManager.Initialize()) _queryIndexManager = null;


            //+Numan16122014
            _cacheStore.ISizableQueryIndexManager = _queryIndexManager;            
            _cacheStore.ISizableEvictionIndexManager = _evictionPolicy;
            _cacheStore.ISizableExpirationIndexManager = _context.ExpiryMgr;

            _stats.MaxCount = _cacheStore.MaxCount;
            _stats.MaxSize = _cacheStore.MaxSize;

            //+Numan16122014

            if (_context.PerfStatsColl != null)
            {
                if (_queryIndexManager != null)
                    _context.PerfStatsColl.SetQueryIndexSize(_queryIndexManager.IndexInMemorySize);                
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
            if (_queryIndexManager != null)
            {
                _queryIndexManager.Clear();
                _context.PerfStatsColl.SetQueryIndexSize(_queryIndexManager.IndexInMemorySize);
            }
        }


        /// <summary>
        ///         /// returns the keylist fullfilling the specified criteria.
        /// </summary>
        /// <param name="queryString">a string describing the search criteria.</param>
        /// <returns>a list of keys.</returns>
        internal override QueryContext SearchInternal(Predicate pred, IDictionary values)
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
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        internal override CacheAddResult AddInternal(object key, CacheEntry cacheEntry, bool isUserOperation)
        {
            CacheAddResult result = base.AddInternal(key, cacheEntry, isUserOperation);
            if (result == CacheAddResult.Success || result == CacheAddResult.SuccessNearEviction)
            {
                //muds:
                if (_queryIndexManager != null && cacheEntry.QueryInfo != null)
                {
                    _queryIndexManager.AddToIndex(key, cacheEntry);
                }

                if (_context.PerfStatsColl != null && _queryIndexManager!=null)
                {                    
                        _context.PerfStatsColl.SetQueryIndexSize(_queryIndexManager.IndexInMemorySize);
                }

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
        internal override CacheInsResult InsertInternal(object key, CacheEntry cacheEntry, bool isUserOperation, CacheEntry oldEntry, OperationContext operationContext)
        {

            CacheInsResult result = base.InsertInternal(key, cacheEntry, isUserOperation, oldEntry, operationContext);
            if (result == CacheInsResult.Success || result == CacheInsResult.SuccessNearEvicition)
            {

                //muds:
                if (_queryIndexManager != null && cacheEntry.QueryInfo != null)
                {
                    _queryIndexManager.AddToIndex(key, cacheEntry);
                }
            }
            else if (result == CacheInsResult.SuccessOverwrite || result == CacheInsResult.SuccessOverwriteNearEviction)
            {

                //muds:
                if (_queryIndexManager != null)
                {
                    if (oldEntry != null && oldEntry.ObjectType != null)
                    {
                        _queryIndexManager.RemoveFromIndex(key, oldEntry.ObjectType);
                    }

                    if (cacheEntry.QueryInfo != null)
                        _queryIndexManager.AddToIndex(key, cacheEntry);
                }
            }


            if (_context.PerfStatsColl != null && _queryIndexManager!=null)
            {
                _context.PerfStatsColl.SetQueryIndexSize(_queryIndexManager.IndexInMemorySize);
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

                //muds:
                if (_queryIndexManager != null && e.ObjectType != null)
                {
                    _queryIndexManager.RemoveFromIndex(key, e.ObjectType);
                }
            }

            if (_context.PerfStatsColl != null && _queryIndexManager != null)
            {
                _context.PerfStatsColl.SetQueryIndexSize(_queryIndexManager.IndexInMemorySize);
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

