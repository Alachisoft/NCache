// Copyright (c) 2017 Alachisoft
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

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime;

using Alachisoft.NCache.Caching.Queries;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.DataReader;

namespace Alachisoft.NCache.Web.Caching
{
    internal class CacheImplBase
    {
        private string _clientID;

        internal CacheImplBase()
        {
            _clientID = System.Guid.NewGuid().ToString() + ":" + Environment.MachineName + ":" + System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        protected internal virtual bool SerializationEnabled
        {
            get { return true; }
        }
        
        protected internal virtual TypeInfoMap TypeMap
        {
            get { return null; }
            set { }
        }

        public virtual long Count
        {
            get
            {
                return 0;
            }
        }

        public string ClientID { get { return _clientID; } }


        internal virtual void MakeTargetCacheActivePassive(bool makeActive) { }

        public virtual string Name
        {
            get { return null; }
        }

      

        public virtual void Dispose(bool disposing) { }

        public virtual void Add(string key, object value, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, 
           Hashtable queryInfo, BitSet flagMap, EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, long size)
        {
        }


        /// <summary>
        /// Add array of <see cref="CacheItem"/> to the cache.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <returns>keys that are added or that alredy exists in the cache and their status.</returns>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>		
        /// <example>The following example demonstrates how to add items to the cache with a sliding expiration of 5 minutes, a priority of 
        /// high, and that notifies the application when the item is removed from the cache.
        /// 
        /// First create a CacheItems.
        /// <code>
        /// string keys = {"ORD_23", "ORD_67"};
        /// CacheItem items = new CacheItem[2]
        /// items[0] = new CacheItem(new Order());
        /// items[0].SlidingExpiration = new TimeSpan(0,5,0);
        /// items[0].Priority = CacheItemPriority.High;
        /// items[0].ItemRemoveCallback = onRemove;
        ///
        /// items[1] = new CacheItem(new Order());
        /// items[1].SlidingExpiration = new TimeSpan(0,5,0);
        /// items[1].Priority = CacheItemPriority.Low;
        /// items[1].ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then add CacheItem to the cache
        /// <code>
        /// 
        ///	NCache.Cache.Add(keys, items);
        /// 
        ///	Cache.Add(keys, items);
        /// 
        /// </code>
        /// </example>
        public virtual IDictionary Add(string[] keys, CacheItem[] items, long[] sizes)
        {
            return null;
        }

        public virtual void Clear(BitSet flagMap)
        {

        }

        public virtual bool Contains(string key)
        {
            return false;
        }

        public virtual CompressedValueEntry Get(string key, BitSet flagMap, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType)
        {
            return null;
        }



        public virtual IDictionary Get(string[] keys, BitSet flagMap)
        {
            return null;
        }

        public virtual object GetCacheItem(string key, BitSet flagMap, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType)
        {
            return null;
        }

        public virtual void Insert(string key, object value, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, 
           Hashtable queryInfo, BitSet flagMap, object lockId, LockAccessType accessType, 
           EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, long size)
        {
        }

        public virtual IDictionary Insert(string[] keys, CacheItem[] items, long[] sizes)
        {
            return null;
        }

        public virtual CompressedValueEntry Remove(string key, BitSet flagMap, object lockId, LockAccessType accessType)
        {
            return null;
        }

        public virtual void Delete(string key, BitSet flagMap, object lockId, LockAccessType accessType)
        {
        }

        public virtual bool SetAttributes(string key, CacheItemAttributes attribute)
        {
            return false;
        }
        public virtual IDictionary Remove(string[] keys, BitSet flagMap)
        {
            return null;
        }

        public virtual void Delete(string[] keys, BitSet flagMap)
        {

        }

        public virtual QueryResultSet Search(string query, IDictionary values)
        {
            return null;
        }

        public virtual QueryResultSet SearchEntries(string query, IDictionary values)
        {
            return null;
        }

        public virtual IRecordSetEnumerator ExecuteReader(string query, IDictionary values, bool getData, int chunkSize)
        {
            return null;
        }
   
        public virtual object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag, CacheImplBase cacheImpl, ref long size)
        {

            return null;
        }

        public virtual object SafeDeserialize(object serializedObject, string serializationContext, BitSet flag, CacheImplBase cacheImpl)
        {
            return null;
        }

        public virtual IEnumerator GetEnumerator()
        {
            return null;
        }

        public virtual EnumerationDataChunk GetNextChunk(EnumerationPointer pointer)
        {
            return null;
        }

        public virtual List<EnumerationDataChunk> GetNextChunk(List<EnumerationPointer> pointers)
        {
            return null;
        }



        public virtual void Unlock(string key)
        {

        }

        public virtual void Unlock(string key, object lockId)
        {

        }

        public virtual bool Lock(string key, TimeSpan lockTimeout, out LockHandle lockHandle)
        {
            lockHandle = null;
            return false;
        }

        internal virtual bool IsLocked(string key, ref LockHandle lockHandle)
        {
            return false;
        }


        public virtual void RegisterKeyNotificationCallback(string key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration) { }

        public virtual void UnRegisterKeyNotificationCallback(string key, short updateCallbackid, short removeCallbackid) { }

        public virtual void UnRegisterKeyNotificationCallback(string[] keys, short updateCallbackid, short removeCallbackid) { }

        public virtual void RegisterKeyNotificationCallback(string[] key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration) { }

    }

}
