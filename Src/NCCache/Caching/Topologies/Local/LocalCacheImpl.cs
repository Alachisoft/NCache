//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Collections;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.DataStructures;
using System.Collections.Generic;
using Alachisoft.NCache.Persistence;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Resources;
using Alachisoft.NCache.Runtime.Events;
using EventType = Alachisoft.NCache.Persistence.EventType;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.ErrorHandling;


namespace Alachisoft.NCache.Caching.Topologies.Local
{
    internal class LocalCacheImpl : CacheBase, ICacheEventsListener
    {
        /// <summary> The en-wrapped instance of cache. </summary>
        private CacheBase _cache;
        

        public LocalCacheImpl() { }

        public LocalCacheImpl(CacheRuntimeContext context)
        {
            _context = context;
        
        }
        /// <summary>
        /// Default constructor.
        /// </summary>
        public LocalCacheImpl(CacheBase cache)
        {
            if (cache == null)
                throw new ArgumentNullException("cache");
            _cache = cache;
            _context = cache.InternalCache.Context;
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (_cache != null)
            {
                _cache.Dispose();
                _cache = null;
            }
            base.Dispose();
        }

        #endregion

        /// <summary>
        /// get/set listener of Cache events. 'null' implies no listener.
        /// </summary>
        public CacheBase Internal
        {
            get { return _cache; }
            set
            {
                _cache = value;
                _context = value.InternalCache.Context;
            }
        }

        /// <summary> 
        /// Returns the cache local to the node, i.e., internal cache.
        /// </summary>
        protected internal override CacheBase InternalCache
        {
            get { return _cache; }
        }

        public override TypeInfoMap TypeInfoMap
        {
            get
            {
                return _cache.TypeInfoMap;
            }
        }

        /// <summary>
        /// get/set the name of the cache.
        /// </summary>
        public override string Name
        {
            get { return Internal.Name; }
            set { Internal.Name = value; }
        }

        /// <summary>
        /// get/set listener of Cache events. 'null' implies no listener.
        /// </summary>
        public override ICacheEventsListener Listener
        {
            get { return Internal.Listener; }
            set { Internal.Listener = value; }
        }

        /// <summary>
        /// Notifications are enabled.
        /// </summary>
        public override Notifications Notifiers
        {
            get { return Internal.Notifiers; }
            set { Internal.Notifiers = value; }
        }

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        public override long Count
        {
            get
            {
                return Internal.Count;
            }
        }

        /// <summary>
        /// returns the statistics of the Clustered Cache. 
        /// </summary>
        public override CacheStatistics Statistics
        {
            get
            {
                return Internal.Statistics;
            }
        }

        /// <summary>
        /// returns the statistics of the Clustered Cache. 
        /// </summary>
        internal override CacheStatistics ActualStats
        {
            get
            {
                return Internal.ActualStats;
            }
        }

        #region	/                 --- ICache ---           /

        /// Removes all entries from the store.
        /// </summary>
        public override void Clear(Caching.Notifications notification, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
        {
     
            Internal.Clear(notification, updateOptions, operationContext);
        }

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element 
        /// with the specified key; otherwise, false.</returns>
        public override bool Contains(object key, OperationContext operationContext)
        {
            return Internal.Contains(key, operationContext);
        }

        /// <summary>
        /// Determines whether the cache contains the specified keys.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>list of existing keys.</returns>
        public override Hashtable Contains(IList  keys, OperationContext operationContext)
        {
            return Internal.Contains(keys, operationContext);
        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        public override CacheEntry Get(object key, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry entry = Internal.Get(key, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
            if (entry != null && KeepDeflattedValues)
            {
                entry.KeepDeflattedValue(_context.SerializationContext);
            }
            return entry;
        }

        public override HashVector GetTagData(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            return Internal.GetTagData(tags, comparisonType, operationContext);
        }

        public override Hashtable Remove(string[] tags, TagComparisonType tagComparisonType, bool notify, OperationContext operationContext)
        {
            return Internal.Remove(tags, tagComparisonType, notify, operationContext);
        }

        internal override ICollection GetTagKeys(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            return Internal.GetTagKeys(tags, comparisonType, operationContext);
        }

        public override IDictionary GetEntryAttributeValues(object key, IList<string> columns, OperationContext operationContext)
        {
            return Internal.GetEntryAttributeValues(key, columns, operationContext);
        }

        /// <summary>
        /// Retrieve the objects from the cache. An array of keys is passed as parameter.
        /// </summary>
        /// <param name="key">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        public override IDictionary Get(object[] keys, OperationContext operationContext)
        {
            HashVector data = (HashVector)Internal.Get(keys, operationContext);
            if (data != null && KeepDeflattedValues)
            {
                IDictionaryEnumerator ide = data.GetEnumerator();
                CacheEntry entry;

                while (ide.MoveNext())
                {
                    if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                    entry = ide.Value as CacheEntry;
                    if (entry != null)
                    {
                        entry.KeepDeflattedValue(_context.SerializationContext);
                    }
                }
            }
            return data;
        }

        public override PollingResult Poll(OperationContext operationContext)
        {
            return Internal.Poll(operationContext);
        }

        /// <summary>
        /// Retrieve the keys from the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>list of keys.</returns>
        public override ArrayList GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            return Internal.GetGroupKeys(group, subGroup, operationContext);
        }

        /// <summary>
        /// Retrieve the keys from the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>list of keys.</returns>
        public override CacheEntry GetGroup(object key, string group, string subGroup, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return null;
            CacheEntry entry = Internal.GetGroup(key, group, subGroup, ref version, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
            if (entry != null && KeepDeflattedValues)
            {
                entry.KeepDeflattedValue(_context.SerializationContext);
            }
            return entry;
        }

        /// <summary>
        /// Gets the data group information of the item.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override Alachisoft.NCache.Caching.DataGrouping.GroupInfo GetGroupInfo(object key, OperationContext operationContext)
        {
            return Internal.GetGroupInfo(key, operationContext);
        }

        /// <summary>
        /// Gets the data groups of the items.
        /// </summary>
        /// <param name="keys">Keys of the items</param>
        /// <returns>Hashtable containing key of the item as 'key' and GroupInfo as 'value'</returns>
        public override Hashtable GetGroupInfoBulk(object[] keys, OperationContext operationContext)
        {
            return Internal.GetGroupInfoBulk(keys, operationContext);
        }
        /// <summary>
        /// Retrieve the keys from the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>key and value pairs.</returns>
        public override HashVector GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            HashVector data = Internal.GetGroupData(group, subGroup, operationContext);
            if (data != null && KeepDeflattedValues)
            {
                IDictionaryEnumerator ide = data.GetEnumerator();
                CacheEntry entry;
                while (ide.MoveNext())
                {
                    entry = ide.Value as CacheEntry;
                    if (entry != null)
                    {
                        entry.KeepDeflattedValue(_context.SerializationContext);
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// Gets/sets the list of data groups contained in the cache.
        /// </summary>
        public override ArrayList DataGroupList
        {
            get
            {
                return Internal.DataGroupList;
            }
        }
        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
        {
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.CacheImpl);
                operationContext?.MarkInUse(NCModulesConstants.CacheImpl);
                CacheAddResult result = CacheAddResult.Failure;
                if (Internal != null)
                {
                    #region -- PART I -- Cascading Dependency Operation
                    object[] keys = cacheEntry.KeysIAmDependingOn;
                    if (keys != null)
                    {
                        Hashtable goodKeysTable = Contains(keys, operationContext);

                        if (!goodKeysTable.ContainsKey("items-found"))
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                        if (goodKeysTable["items-found"] == null)
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                        if (goodKeysTable["items-found"] == null || (((ArrayList)goodKeysTable["items-found"]).Count != keys.Length))
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                    }
                    #endregion

                    result = Internal.Add(key, cacheEntry, notify, operationContext);

                    #region -- PART II -- Cascading Dependency Operation

                    KeyDependencyInfo[] keyDepInfos = cacheEntry.KeysIAmDependingOnWithDependencyInfo;

                    if (result == CacheAddResult.Success && keyDepInfos != null)
                    {
                        Hashtable keyDepInfoTable = new Hashtable();
                        foreach (KeyDependencyInfo keyDepInfo in keyDepInfos)
                        {
                            if (keyDepInfoTable[keyDepInfo.Key] == null)
                            {
                                keyDepInfoTable.Add(keyDepInfo.Key, new ArrayList());
                            }
                            ((ArrayList)keyDepInfoTable[keyDepInfo.Key]).Add(new KeyDependencyInfo(key.ToString()));
                        }

                        object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);

                        if (generateQueryInfo == null)
                        {
                            operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                        }

                        #region OLD KEY DEPENDENCY CODE
                        // Internal.AddDepKeyList(table, operationContext); 
                        #endregion
                        Internal.AddDepKeyList(keyDepInfoTable, operationContext);

                        if (generateQueryInfo == null)
                        {
                            operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                        }
                    }
                    #endregion
                }
                return result;
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.CacheImpl);
                operationContext?.MarkFree(NCModulesConstants.CacheImpl);
            }
        }

        /// <summary>
        /// Add ExpirationHint against the given key
        /// Key must already exists in the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        /// <returns></returns>
        public override bool Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
      
            CacheEntry cacheEntry = null;
            try
            {
                bool result = false;
                if (Internal != null)
                {
                    #region -- PART I -- Cascading Dependency Operation
                    cacheEntry = CacheEntry.CreateCacheEntry(Context.TransactionalPoolManager);
                    cacheEntry.ExpirationHint = eh;
                    cacheEntry.MarkInUse(NCModulesConstants.CacheImpl);
                    object[] keys = cacheEntry.KeysIAmDependingOn;

                    if (keys != null)
                    {
                        Hashtable goodKeysTable = Contains(keys, operationContext);

                        if (!goodKeysTable.ContainsKey("items-found"))
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                        if (goodKeysTable["items-found"] == null)
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                        if (((ArrayList)goodKeysTable["items-found"]).Count != keys.Length)
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));
                    }
                    #endregion

                    result = Internal.Add(key, eh, operationContext);

                    #region -- PART II -- Cascading Dependency Operation

                    KeyDependencyInfo[] keyDepInfos = cacheEntry.KeysIAmDependingOnWithDependencyInfo;

                    if (result && keyDepInfos != null)
                    {
                        Hashtable keyDepInfoTable = new Hashtable();
                        foreach (KeyDependencyInfo keyDepInfo in keyDepInfos)
                        {
                            if (keyDepInfoTable[keyDepInfo.Key] == null)
                            {
                                keyDepInfoTable.Add(keyDepInfo.Key, new ArrayList());
                            }
                            ((ArrayList)keyDepInfoTable[keyDepInfo.Key]).Add(new KeyDependencyInfo(key.ToString()));
                        }
                        try
                        {
                            #region OLD KEY DEPENDENCY CODE
                            #endregion
                            keyDepInfoTable = Internal.AddDepKeyList(keyDepInfoTable, operationContext);
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }

                        #region OLD KEY DEPENDENCY CODE
                        // if (table != null) 
                        #endregion
                        if (keyDepInfoTable != null)
                        {
                            #region OLD KEY DEPENDENCY CODE
                            #endregion
                            IDictionaryEnumerator en = keyDepInfoTable.GetEnumerator();
                            while (en.MoveNext())
                            {
                                if (en.Value is bool && !((bool)en.Value))
                                {
                                    throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));
                                }
                            }
                        }
                    }
                    #endregion
                }
                return result;
            }
            finally
            {
                if (cacheEntry != null)
                {
                    MiscUtil.ReturnEntryToPool(cacheEntry, Context.TransactionalPoolManager);
                    cacheEntry.MarkFree(NCModulesConstants.CacheImpl);
                }
            }
        }

        public override bool Add(object key, OperationContext operationContext)
        {
            return Internal.Add(key, operationContext);
        }

        /// <summary>
        /// Adds key and value pairs to the cache. Throws an exception or returns the
        /// list of keys that already exists in the cache.
        /// </summary>
        /// <param name="keys">key of the entry.</param>
        /// <param name="cacheEntries">the cache entry.</param>
        /// <returns>List of keys that are added or that alredy exists in the cache and their status</returns>
        public override Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            Hashtable table = new Hashtable();

            ArrayList goodKeysList = new ArrayList();
            ArrayList badKeysList = new ArrayList();
            ArrayList goodEntriesList = new ArrayList();
            CacheEntry[] goodEntries = null;
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.CacheImpl);

                if (Internal != null)
                {
                    #region -- PART I -- Cascading Dependency Operation
                    for (int i = 0; i < cacheEntries.Length; i++)
                    {
                        object[] tempKeys = cacheEntries[i].KeysIAmDependingOn;
                        if (tempKeys != null)
                        {
                            Hashtable goodKeysTable = Contains(tempKeys, operationContext);

                            if (goodKeysTable.ContainsKey("items-found") && goodKeysTable["items-found"] != null && tempKeys.Length == ((ArrayList)goodKeysTable["items-found"]).Count)
                            {
                                goodKeysList.Add(keys[i]);
                                goodEntriesList.Add(cacheEntries[i]);
                            }
                            else
                            {
                                badKeysList.Add(keys[i]);
                            }
                        }
                        else
                        {
                            goodKeysList.Add(keys[i]);
                            goodEntriesList.Add(cacheEntries[i]);
                        }
                    }

                    #endregion


                    goodEntries = new CacheEntry[goodEntriesList.Count];
                    goodEntriesList.CopyTo(goodEntries);

                    table = Internal.Add(goodKeysList.ToArray(), goodEntries, notify, operationContext);

                    #region --Part II-- Cascading Dependency Operations

                    object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);
                    if (generateQueryInfo == null)
                    {
                        operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                    }

                    for (int i = 0; i < goodKeysList.Count; i++)
                    {
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                        if (table[goodKeysList[i]] is Exception)
                            continue;
                        CacheAddResult retVal = (CacheAddResult)table[goodKeysList[i]];
                        KeyDependencyInfo[] keyDepInfos = goodEntries[i].KeysIAmDependingOnWithDependencyInfo;

                        if (retVal == CacheAddResult.Success && keyDepInfos != null)
                        {
                            
                            Hashtable keyDepInfoTable = new Hashtable();
                            foreach (KeyDependencyInfo keyDepInfo in keyDepInfos)
                            {
                                if (keyDepInfoTable[keyDepInfo.Key] == null)
                                {
                                    keyDepInfoTable.Add(keyDepInfo.Key, new ArrayList());
                                }
                                ((ArrayList)keyDepInfoTable[keyDepInfo.Key]).Add(new KeyDependencyInfo(goodKeysList[i].ToString()));
                            }
                            Internal.AddDepKeyList(keyDepInfoTable, operationContext);
                        }
                    }

                    if (generateQueryInfo == null)
                    {
                        operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                    }

                    for (int i = 0; i < badKeysList.Count; i++)
                    {
                        table.Add(badKeysList[i],  new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND)));
                    }
                    #endregion
                }
                return table;
            }
            finally
            {
                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.CacheImpl);

                if (goodEntries != null)
                    goodEntries.MarkFree(NCModulesConstants.CacheImpl);
            }
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.CacheImpl);
                operationContext?.MarkInUse(NCModulesConstants.LocalCache);
                CacheInsResultWithEntry retVal = null;
                if (Internal != null)
                {
                    #region -- PART I -- Cascading Dependency Operation
                    object[] dependingKeys = cacheEntry.KeysIAmDependingOn;
                    if (dependingKeys != null)
                    {
                        Hashtable goodKeysTable = Contains(dependingKeys, operationContext);

                        if (!goodKeysTable.ContainsKey("items-found"))
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                        if (goodKeysTable["items-found"] == null)
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                        if (dependingKeys.Length != ((ArrayList)goodKeysTable["items-found"]).Count)
                            throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));
                    }
                    #endregion
                  
                    retVal = Internal.Insert(key, cacheEntry, notify, lockId, version, accessType, operationContext);
                    if (retVal == null) retVal = retVal = CacheInsResultWithEntry.CreateCacheInsResultWithEntry(_context.TransactionalPoolManager);
                    #region -- PART II -- Cascading Dependency Operation
                    if (retVal.Result == CacheInsResult.Success || retVal.Result == CacheInsResult.SuccessOverwrite)
                    {
                        #region OLD KEY DEPENDENCY CODE
                        // Hashtable table = null; 
                        #endregion
                        Hashtable keyDepInfosTable = null;

                        object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);
                        if (generateQueryInfo == null)
                        {
                            operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                        }
                        
                        if (generateQueryInfo == null)
                        {
                            operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                        }
                    }
                    #endregion

                }
                return retVal;
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.CacheImpl);

            }
        }


        /// <summary>
        /// Adds key and value pairs to the cache. If any of the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">the cache entries.</param>
        /// <returns>returns the results for inserted keys</returns>
        public override Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            Hashtable retVal = null;

            ArrayList goodKeysList = new ArrayList();
            ArrayList goodEntriesList = new ArrayList();
            ArrayList badKeysList = new ArrayList();
            CacheEntry[] goodEntries = null;
            try
            {
                if (cacheEntries != null)
                    cacheEntries.MarkInUse(NCModulesConstants.CacheImpl);

                if (Internal != null)
                {
                    #region -- PART I -- Cascading Dependency Operation
                    for (int i = 0; i < cacheEntries.Length; i++)
                    {
                        object[] tempKeys = cacheEntries[i].KeysIAmDependingOn;
                        if (tempKeys != null)
                        {
                            Hashtable goodKeysTable = Contains(tempKeys, operationContext);

                            if (goodKeysTable.ContainsKey("items-found") && goodKeysTable["items-found"] != null && tempKeys.Length == ((ArrayList)goodKeysTable["items-found"]).Count)
                            {
                                goodKeysList.Add(keys[i]);
                                goodEntriesList.Add(cacheEntries[i]);
                            }
                            else
                            {
                                badKeysList.Add(keys[i]);
                            }
                        }
                        else
                        {
                            goodKeysList.Add(keys[i]);
                            goodEntriesList.Add(cacheEntries[i]);
                        }
                    }

                    #endregion


                    goodEntries = new CacheEntry[goodEntriesList.Count];
                    goodEntriesList.CopyTo(goodEntries);

                    retVal = Internal.Insert(goodKeysList.ToArray(), goodEntries, notify, operationContext);

                    #region -- PART II -- Cascading Dependency Operation
                    object generateQueryInfo = operationContext.GetValueByField(OperationContextFieldName.GenerateQueryInfo);
                    if (generateQueryInfo == null)
                    {
                        operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                    }

                    for (int i = 0; i < goodKeysList.Count; i++)
                    {
                        if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                        CacheInsResultWithEntry result = retVal[goodKeysList[i]] as CacheInsResultWithEntry;

                        if (result != null && (result.Result == CacheInsResult.Success || result.Result == CacheInsResult.SuccessOverwrite))
                        {
                            Hashtable keyDepInfosTable = null;

                            if (result.Entry != null && result.Entry.KeysIAmDependingOnWithDependencyInfo != null)
                            {
                                keyDepInfosTable = GetFinalKeysListWithDependencyInfo(result.Entry, goodEntries[i]);
                                Hashtable oldKeysTable = GetKeysTable(goodKeysList[i], (KeyDependencyInfo[])keyDepInfosTable["oldKeys"]);
                                Internal.RemoveDepKeyList(oldKeysTable, operationContext);

                                oldKeysTable = GetKeyDependencyInfoTable(goodKeysList[i], (KeyDependencyInfo[])keyDepInfosTable["newKeys"]);
                                Internal.AddDepKeyList(oldKeysTable, operationContext);
                            }
                            else if (goodEntries[i].KeysIAmDependingOn != null)
                            {
                                Hashtable newKeysTable = GetKeyDependencyInfoTable(goodKeysList[i], goodEntries[i].KeysIAmDependingOnWithDependencyInfo);
                                Internal.AddDepKeyList(newKeysTable, operationContext);
                            }
                        }
                    }

                    //Now Remove GeneratedQueryInfo If added at this step
                    if (generateQueryInfo == null)
                    {
                        operationContext.RemoveValueByField(OperationContextFieldName.GenerateQueryInfo);
                    }

                    for (int i = 0; i < badKeysList.Count; i++)
                    {
                        retVal.Add(badKeysList[i],  new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND)));
                    }

                    #endregion
                }
                return retVal;
            }
            finally
            {
                if (goodEntries != null)
                    goodEntries.MarkFree(NCModulesConstants.CacheImpl);

                if (cacheEntries != null)
                    cacheEntries.MarkFree(NCModulesConstants.CacheImpl);
            }
        }

        public override object RemoveSync(object[] keys, ItemRemoveReason reason, bool notify, OperationContext operationContext)
        {
            ArrayList depenedentItemList = new ArrayList();
            try
            {

                Hashtable totalRemovedItems = new Hashtable();

                CacheEntry entry = null;
                IDictionaryEnumerator ide = null;

                Hashtable result = Internal.Remove(keys, reason, false, operationContext);
                ide = result.GetEnumerator();

                while (ide.MoveNext())
                {
                    entry = ide.Value as CacheEntry;

                    if (entry != null)
                    {
                        totalRemovedItems.Add(ide.Key, entry);
                        if (entry.KeysDependingOnMe != null && entry.KeysDependingOnMe.Count > 0)
                        {
                            depenedentItemList.AddRange(entry.KeysDependingOnMe.Keys);
                        }
                    }

                }
                ide = totalRemovedItems.GetEnumerator();
                while (ide.MoveNext())
                {
                    try
                    {
                        entry = ide.Value as CacheEntry;
                        if (entry != null)
                        {
                            if (IsItemRemoveNotifier)
                            {
                                EventId eventId = null;
                                OperationID opId = operationContext.OperatoinID;
                                EventContext eventContext = null;

                                //generate event id
                                if (!operationContext.Contains(OperationContextFieldName.EventContext)) //for atomic operations
                                {
                                    eventId = EventId.CreateEventId(opId);
                                }
                                else //for bulk
                                {
                                    eventId = ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                                }
                            }

                            if (entry.Notifications != null)
                            {
                                EventId eventId = null;
                                OperationID opId = operationContext.OperatoinID;
                                Caching.Notifications cbEtnry = entry.Notifications;// e.DeflattedValue(_context.SerializationContext);
                                EventContext eventContext = null;

                                if (cbEtnry != null && cbEtnry.ItemRemoveCallbackListener != null && cbEtnry.ItemRemoveCallbackListener.Count > 0)
                                {
                                    //generate event id
                                    if (!operationContext.Contains(OperationContextFieldName.EventContext)) //for atomic operations
                                    {
                                        eventId = EventId.CreateEventId(opId);
                                    }
                                    else //for bulk
                                    {
                                        eventId = ((EventContext)operationContext.GetValueByField(OperationContextFieldName.EventContext)).EventID;
                                    }

                                    eventId.EventType = EventType.ITEM_REMOVED_CALLBACK;
                                    eventContext = new EventContext();
                                    eventContext.Add(EventContextFieldName.EventID, eventId);
                                    EventCacheEntry eventCacheEntry = CacheHelper.CreateCacheEventEntry(cbEtnry.ItemRemoveCallbackListener, entry, Context);
                                    eventContext.Item = eventCacheEntry;
                                    eventContext.Add(EventContextFieldName.ItemRemoveCallbackList, cbEtnry.ItemRemoveCallbackListener.Clone());
                                    eventContext.UniqueId = GenerateEventType(EventType.ITEM_REMOVED_CALLBACK)+entry.Version.ToString();
                                    //Will always reaise the whole entry for old clients
                                    NotifyCustomRemoveCallback(ide.Key, entry, reason, true, operationContext, eventContext);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return depenedentItemList;
        }

        public string GenerateEventType(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.ITEM_REMOVED_CALLBACK:
                    return "Removed_cb";
                    break;
                case EventType.ITEM_UPDATED_CALLBACK:
                    return "Updated_cb";
                    break;
                case EventType.ITEM_ADDED_EVENT:
                    return "Added_event";
                    break;
                case EventType.ITEM_REMOVED_EVENT:
                    return "Removed_event";
                    break;
                case EventType.ITEM_UPDATED_EVENT:
                    return "Updated_event";
                    break;
                case EventType.ITEM_ADDED_CALLBACK:
                    return "Added_cb";
                    break;
                default:
                    return "";

            }
            return "";

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
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = Internal.Remove(key, ir, notify, lockId, version, accessType, operationContext);
            if (retVal != null && retVal.KeysIAmDependingOn != null)
            {
                Internal.RemoveDepKeyList(GetKeysTable(key, retVal.KeysIAmDependingOn), operationContext);
            }
            return retVal;
        }

        /// <summary>
        /// Removes the key and pairs from the cache. The keys are specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="keys">key of the entries.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>removed keys list</returns>
        public override Hashtable Remove(IList keys, ItemRemoveReason ir, bool notify, OperationContext operationContext)
        {
            Hashtable retVal = Internal.Remove(keys, ir, notify, operationContext);

            foreach (object key in keys)
            {
                if (operationContext.CancellationToken != null && operationContext.CancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(ExceptionsResource.OperationFailed);

                CacheEntry entry = (CacheEntry)retVal[key];
                if (entry != null && entry.KeysIAmDependingOn != null)
                {
                    Internal.RemoveDepKeyList(GetKeysTable(key, entry.KeysIAmDependingOn), operationContext);
                }
            }

            return retVal;
        }

        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        public override Hashtable Remove(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            ArrayList list = GetGroupKeys(group, subGroup, operationContext);
            if (list != null && list.Count > 0)
            {
                object[] grpKeys = MiscUtil.GetArrayFromCollection(list);
                return Remove(grpKeys, ItemRemoveReason.Removed, notify, operationContext);
            }
            return null;
        }


      


        /// <summary>
        /// Broadcasts a user-defined event across the cluster.
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="data"></param>
        /// <param name="async"></param>
        public override void SendNotification(object notifId, object data, OperationContext operationContext)
        {
            Internal.SendNotification(notifId, data, operationContext);
        }

        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        /// <returns>IDictionaryEnumerator enumerator.</returns>
        public override IDictionaryEnumerator GetEnumerator()
        {
            return Internal.GetEnumerator();
        }

        public override EnumerationDataChunk GetNextChunk(EnumerationPointer pointer, OperationContext operationContext)
        {
            return Internal.GetNextChunk(pointer, operationContext);
        }

        #endregion

        #region/            --- Key based notification registration ---           /

        public override void RegisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            Internal.RegisterKeyNotification(key, updateCallback, removeCallback, operationContext);
        }

        public override void RegisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            Internal.RegisterKeyNotification(keys, updateCallback, removeCallback, operationContext);
        }

        public override void UnregisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            Internal.UnregisterKeyNotification(key, updateCallback, removeCallback, operationContext);
        }

        public override void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext)
        {
            Internal.UnregisterKeyNotification(keys, updateCallback, removeCallback, operationContext);
        }

        #endregion

 

        public override void UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
            Internal.UnLock(key, lockId, isPreemptive, operationContext);
        }

        public override LockOptions Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            return Internal.Lock(key, lockExpiration, ref lockId, ref lockDate, operationContext);
        }

        public override LockOptions IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            return Internal.IsLocked(key, ref lockId, ref lockDate, operationContext);
        }

        #region /               --- Stream Operations---                        /

        public override bool OpenStream(string key, string lockHandle, Alachisoft.NCache.Common.Enum.StreamModes mode, string group, string subGroup, ExpirationHint hint, Alachisoft.NCache.Caching.EvictionPolicies.EvictionHint evictinHint, OperationContext operationContext)
        {
            #region -- PART I -- Cascading Dependency Operation
            object[] keys = CacheHelper.GetKeyDependencyTable(hint);

            if (keys != null && mode == Alachisoft.NCache.Common.Enum.StreamModes.Write)
            {
                Hashtable goodKeysTable = Contains(keys, operationContext);

                if (!goodKeysTable.ContainsKey("items-found"))
                    throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                if (goodKeysTable["items-found"] == null)
                    throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));

                if (((ArrayList)goodKeysTable["items-found"]).Count != keys.Length)
                    throw new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND));
            }
            #endregion

            bool streamopened = Internal.OpenStream(key, lockHandle, mode, group, subGroup, hint, evictinHint, operationContext);

            #region -- PART II -- Cascading Dependency Operation

            if (streamopened && mode == Alachisoft.NCache.Common.Enum.StreamModes.Write && keys != null)
            {
                Hashtable keyDepInfoTable = new Hashtable();
                keyDepInfoTable = GetKeyDependencyInfoTable(key, (KeyDependencyInfo[])CacheHelper.GetKeyDependencyInfoTable(hint));
                Internal.AddDepKeyList(keyDepInfoTable, operationContext);
            }
            #endregion

            return streamopened;
        }

        public override void CloseStream(string key, string lockHandle, OperationContext operationContext)
        {
            Internal.CloseStream(key, lockHandle, operationContext);
        }

        public override int ReadFromStream(ref Alachisoft.NCache.Common.DataStructures.VirtualArray vBuffer, string key, string lockHandle, int offset, int length, OperationContext operationContext)
        {
            return Internal.ReadFromStream(ref vBuffer, key, lockHandle, offset, length, operationContext);
        }

        public override void WriteToStream(string key, string lockHandle, Alachisoft.NCache.Common.DataStructures.VirtualArray vBuffer, int srcOffset, int dstOffset, int length, OperationContext operationContext)
        {
            Internal.WriteToStream(key, lockHandle, vBuffer, srcOffset, dstOffset, length, operationContext);
        }

        public override long GetStreamLength(string key, string lockHandle, OperationContext operationContext)
        {
            return Internal.GetStreamLength(key, lockHandle, operationContext);
        }
        #endregion

        #region ICacheEventsListener Members

        void ICacheEventsListener.OnItemAdded(object key, OperationContext operationContext, EventContext eventContext)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void ICacheEventsListener.OnItemUpdated(object key, OperationContext operationContext, EventContext eventContext)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void ICacheEventsListener.OnItemRemoved(object key, object val, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void ICacheEventsListener.OnItemsRemoved(object[] keys, object[] vals, ItemRemoveReason reason, OperationContext operationContext, EventContext[] eventContext)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void ICacheEventsListener.OnCacheCleared(OperationContext operationContext, EventContext eventContext)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void ICacheEventsListener.OnCustomEvent(object notifId, object data, OperationContext operationContext, EventContext eventContext)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void ICacheEventsListener.OnCustomUpdateCallback(object key, object value, OperationContext operationContext, EventContext eventContext)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void ICacheEventsListener.OnPollNotify(string clientId, short callbackId, NCache.Caching.Events.EventTypeInternal eventType)
        {
            base.NotifyPollRequestCallback(clientId, callbackId, true, eventType);
        }

        void ICacheEventsListener.OnCustomRemoveCallback(object key, object value, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext)
        {
            throw new Exception("The method or operation is not implemented.");
        }
#if !CLIENT && !DEVELOPMENT
        void ICacheEventsListener.OnHashmapChanged(Alachisoft.NCache.Common.DataStructures.NewHashmap newHashmap, bool updateClientMap)
        {
            throw new Exception("The method or operation is not implemented.");
        }
#endif
        void ICacheEventsListener.OnWriteBehindOperationCompletedCallback(OpCode operationCode, object result, Caching.Notifications notification)
        {
            throw new Exception("The method or operation is not implemented.");
        }


        internal override void Touch(List<string> keys, OperationContext operationContext)
        {
            Internal.Touch(keys, operationContext);
        }

        #endregion

        #region ------------------------------- Messaging ------------------------------ 

        #region ---------------------- IMessageStore Implementation --------------------

        public override bool AssignmentOperation(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo, TopicOperationType type, OperationContext context)
        {
            if (Internal == null)
                throw new InvalidOperationException();

            return Internal.AssignmentOperation(messageInfo, subscriptionInfo, type, context);
        }

        public override void RemoveMessages(IList<MessageInfo> messagesTobeRemoved, MessageRemovedReason reason, OperationContext context)
        {
            if (Internal == null)
                throw new InvalidOperationException();


            Internal.RemoveMessages(messagesTobeRemoved, reason, context);
        }



        public override bool StoreMessage(string topic, Message message, OperationContext context)
        {
            if (Internal == null)
                throw new InvalidOperationException();

            return Internal.StoreMessage(topic, message, context);
        }

        public override void AcknowledgeMessageReceipt(string clientId, IDictionary<string, IList<string>> topicWiseMessageIds, OperationContext operationContext)
        {
            if (Internal == null)
                throw new InvalidOperationException();

            Internal.AcknowledgeMessageReceipt(clientId, topicWiseMessageIds, operationContext);

        }

        public override bool TopicOperation(TopicOperation channelOperation, OperationContext operationContext)
        {
            if (Internal == null)
                throw new InvalidOperationException();

            return Internal.TopicOperation(channelOperation, operationContext);
        }

        #endregion

        public override long GetMessageCount(string topicName, OperationContext operationContext)
        {
            if (Internal == null)
            {
                throw new InvalidOperationException();
            }
            return Internal.GetMessageCount(topicName, operationContext);
        }

        public override void ClientConnected(string client, bool isInproc, Runtime.Caching.ClientInfo clientInfo)
        {
            if (Internal != null)
            {
                Internal.ClientConnected(client, isInproc, clientInfo);
            }

        }

        public override void ClientDisconnected(string client, bool isInproc, Runtime.Caching.ClientInfo clientInfo)
        {
            CacheStatistics stats = InternalCache.Statistics;
            if (stats != null && stats.ConnectedClients != null)
            {
                lock (stats.ConnectedClients.SyncRoot)
                {
                    if (stats.ConnectedClients.Contains(client))
                    {
                        stats.ConnectedClients.Remove(client);
                    }
                }
            }

            if (Internal != null)
            {
                Internal.ClientDisconnected(client, isInproc, clientInfo);
            }
          
        }

        public void OnOperationModeChanged(OperationMode mode)
        {

        }

        #endregion

       
       

    }
}
