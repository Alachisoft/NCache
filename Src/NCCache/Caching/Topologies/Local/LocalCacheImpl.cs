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
using System.Text;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Caching.Queries;
using System.Collections.Generic;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Persistence;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    internal class LocalCacheImpl : CacheBase, ICacheEventsListener 
    {

        /// <summary> The en-wrapped instance of cache. </summary>
        private CacheBase _cache;

        public LocalCacheImpl()
        {

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


        public override void UpdateClientsList(Hashtable list)
        {
            Internal.UpdateClientsList(list);
        }


        #region	/                 --- ICache ---           /

        /// Removes all entries from the store.
        /// </summary>
        public override void Clear(CallbackEntry cbEntry, OperationContext operationContext)
        {
            Internal.Clear(cbEntry, operationContext);
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
        public override Hashtable Contains(object[] keys, OperationContext operationContext)
        {
            return Internal.Contains(keys, operationContext);
        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="lockId"></param>
        /// <param name="lockDate"></param>
        /// <param name="lockExpiration"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
        /// <returns>cache entry.</returns>
        public override CacheEntry Get(object key, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry entry = Internal.Get(key, ref lockId, ref lockDate, lockExpiration, accessType, operationContext);
            if (entry != null && KeepDeflattedValues)
            {
                entry.KeepDeflattedValue(_context.SerializationContext);
            }
            return entry;
        }
       
        /// <summary>
        /// Retrieve the objects from the cache. An array of keys is passed as parameter.
        /// </summary>
        /// <param name="key">keys of the entries.</param>
        /// <returns>cache entries.</returns>
        public override Hashtable Get(object[] keys, OperationContext operationContext)
        {
            Hashtable data = Internal.Get(keys, operationContext);
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
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public override CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
        {
            CacheAddResult result = CacheAddResult.Failure;
            if (Internal != null)
            {
                result = Internal.Add(key, cacheEntry, notify, operationContext);
            }
            return result;
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
            bool result = false;
            if (Internal != null)
            {
                 result = Internal.Add(key, eh, operationContext);
            }
            return result;
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

            if (Internal != null)
            {
                table = Internal.Add(keys, cacheEntries, notify, operationContext);

            }
            return table;
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>returns the result of operation.</returns>
        public override CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            CacheInsResultWithEntry retVal = new CacheInsResultWithEntry();
            if (Internal != null)
            {
                retVal = Internal.Insert(key, cacheEntry, notify, lockId, accessType, operationContext);
            }
            return retVal;
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

            if (Internal != null)
            {

                retVal = Internal.Insert(keys, cacheEntries, notify, operationContext);
            }
            return retVal;
        }

        public override object RemoveSync(object[] keys, ItemRemoveReason reason, bool notify, OperationContext operationContext)
        {
            ArrayList depenedentItemList = new ArrayList();
            try
            {

                Hashtable totalRemovedItems = new Hashtable();

                CacheEntry entry = null;
                IDictionaryEnumerator ide = null;


                for (int i = 0; i < keys.Length; i++)
                {
                    try
                    {
                        if (keys[i] != null)
                            entry = Internal.Remove(keys[i], reason, false, null, LockAccessType.IGNORE_LOCK, operationContext);


                        if (entry != null)
                        {
                            totalRemovedItems.Add(keys[i], entry);
                        }
                    }
                    catch (Exception ex)
                    {
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
                            if (entry.Value is CallbackEntry)
                            {
                                EventId eventId = null;
                                OperationID opId = operationContext.OperatoinID;
                                CallbackEntry cbEtnry = (CallbackEntry)entry.Value;
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
                                    EventCacheEntry eventCacheEntry = CacheHelper.CreateCacheEventEntry(cbEtnry.ItemRemoveCallbackListener, entry);
                                    eventContext.Item = eventCacheEntry;
                                    eventContext.Add(EventContextFieldName.ItemRemoveCallbackList, cbEtnry.ItemRemoveCallbackListener.Clone());

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

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="ir"></param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <param name="lockId"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <returns>item value</returns>
        public override CacheEntry Remove(object key, ItemRemoveReason ir, bool notify, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            CacheEntry retVal = Internal.Remove(key, ir, notify, lockId, accessType, operationContext);
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
        public override Hashtable Remove(object[] keys, ItemRemoveReason ir, bool notify, OperationContext operationContext)
        {
            Hashtable retVal = Internal.Remove(keys, ir, notify, operationContext);
            return retVal;
        }

        public override QueryResultSet Search(string query, IDictionary values, OperationContext operationContext)
        {
            return Internal.Search(query, values, operationContext);
        }

        public override QueryResultSet SearchEntries(string query, IDictionary values, OperationContext operationContext)
        {
            QueryResultSet resultSet = Internal.SearchEntries(query, values, operationContext);

            if (resultSet.SearchEntriesResult != null && KeepDeflattedValues)
            {
                IDictionaryEnumerator ide = resultSet.SearchEntriesResult.GetEnumerator();
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

            return resultSet;
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

        #region ICacheEventsListener Members

        void ICacheEventsListener.OnCustomUpdateCallback(object key, object value, OperationContext operationContext, EventContext eventContext)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void ICacheEventsListener.OnCustomRemoveCallback(object key, object value, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void ICacheEventsListener.OnHashmapChanged(Alachisoft.NCache.Common.DataStructures.NewHashmap newHashmap, bool updateClientMap)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
