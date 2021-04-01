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
using System.Threading;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Runtime.Events;
using System.Collections.Generic;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.ErrorHandling;
#if SERVER 
using Alachisoft.NCache.Caching.Topologies.Clustered;
#endif

namespace Alachisoft.NCache.Caching.Util
{
    //#if !THINCLIENT
    /// <summary>
    /// Class to help in common cache operations
    /// </summary>
    internal class CacheHelper
	{
		/// <summary>
		/// Returns the number of items in local instance of the cache.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static long GetLocalCount(CacheBase cache)
		{
			if(cache != null && cache.InternalCache != null)
				return (long)cache.InternalCache.Count;
			return 0;
		}

#if SERVER 
		/// <summary>
		/// Tells if the cache is a clustered cache or a local one
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static bool IsClusteredCache(CacheBase cache)
		{
            if (cache == null)
                return false;
            else
			    return cache.GetType().IsSubclassOf(typeof(ClusterCacheBase));
        }
#else
        /// <summary>
		/// Tells if the cache is a clustered cache or a local one
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static bool IsClusteredCache(CacheBase cache)
		{
			return false;
		}
#endif

#if SERVER 
		/// <summary>
		/// Copies the entries of a cache into an array. Used for state transfer.
		/// </summary>
		/// <param name="cache">cache object</param>
		/// <param name="count">number of entries to return</param>
		/// <returns>array of cache entries</returns>
		public static CacheEntry[] GetCacheEntries(CacheBase cache, long count)
		{
			long index = 0;
			CacheEntry[] entArr = null;
			CacheEntry	 ent = null;
			cache.Sync.AcquireReaderLock(Timeout.Infinite);
			try
			{
				if(count == 0 || count > cache.Count)
					count = cache.Count;
				entArr = new CacheEntry[ count ];
				IDictionaryEnumerator i = cache.GetEnumerator();
				while(index < count && i.MoveNext())
				{
					ent = i.Value as CacheEntry;
					entArr[index ++] = ent.RoutableClone(null);
				}
			}
			catch(Exception e)
			{
				cache.Context.NCacheLog.Error("CacheHelper.CreateLocalEntry()",e.Message);
				return null;
			}
			finally
			{
				cache.Sync.ReleaseReaderLock();
			}
			return entArr;
		}
#endif

        /// <summary>
        /// Merge the first entry i.e. c1 into c2
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns>returns merged entry c2</returns>
        public static CacheEntry MergeEntries(CacheEntry c1, CacheEntry c2)
        {
            if (c1 != null && c1.Notifications != null)
            {
                Notifications cbEtnry = c1.Notifications;

                if (cbEtnry.ItemRemoveCallbackListener != null)
                {
                    foreach (CallbackInfo cbInfo in cbEtnry.ItemRemoveCallbackListener)
                        c2.AddCallbackInfo(null, cbInfo,true);

                }
                if (cbEtnry.ItemUpdateCallbackListener != null)
                {
                    foreach (CallbackInfo cbInfo in cbEtnry.ItemUpdateCallbackListener)
                        c2.AddCallbackInfo(cbInfo, null,true);

                }
            }
            if (c1 != null && c1.EvictionHint != null)
            {
                if (c2.EvictionHint == null) c2.EvictionHint = c1.EvictionHint;
            }
            return c2;
        }

        public static object[] GetKeyDependencyTable(ExpirationHint hint)
        {
            return null;
        }

        public static object[] GetKeyDependencyInfoTable(ExpirationHint hint)
        {
            return null;
        }

        public static EventCacheEntry CreateCacheEventEntry(ArrayList listeners, CacheEntry cacheEntry, CacheRuntimeContext context)
        {
            EventCacheEntry entry = null;
            EventDataFilter maxFilter = EventDataFilter.None;


            return CreateCacheEventEntry(maxFilter, cacheEntry, context);
        }

        public static EventCacheEntry CreateCacheEventEntry(EventDataFilter? filter, CacheEntry cacheEntry, CacheRuntimeContext context)
        {
            
            return null;
        }

        public static bool ReleaseLock(CacheEntry existingEntry, CacheEntry newEntry)
        {
            if (CheckLockCompatibility(existingEntry, newEntry))
            {
                existingEntry.ReleaseLock();
                newEntry.ReleaseLock();
                return true;
            }
            return false;
        }

        public static bool CheckLockCompatibility(CacheEntry existingEntry, CacheEntry newEntry)
        {
            object lockId = null;
            DateTime lockDate = new DateTime();
            if (existingEntry.IsLocked(ref lockId, ref lockDate))
            {
                return existingEntry.LockId.Equals(newEntry.LockId);
            }
            return true;
        }


        /// <summary>
        /// Determines weather two data groups are compatible or not.
        /// </summary>
        /// <param name="g1"></param>
        /// <param name="g2"></param>
        /// <returns></returns>
        public static bool CheckDataGroupsCompatibility(DataGrouping.GroupInfo g1, DataGrouping.GroupInfo g2, bool fromDataSource = false)
        {
            if (fromDataSource)
            {
                bool compatible = false;
                if (g1 == null)
                    compatible = true; // if read from readthru, there might be no group
                else if (g1 == null && g2 == null)
                    compatible = true;
                else if (g1 != null && g2 != null)
                    compatible = (g1.Group == g2.Group && g1.SubGroup == g2.SubGroup);
                else if (g1 != null)
                {
                    if (g1.Group == null && g1.SubGroup == null)
                        compatible = true;
                }
                else if (g2.Group == null && g2.SubGroup == null)
                    compatible = true;

                return compatible;
            }
            else
                return true;
        }

        /// <summary>
        /// Gives the list of insertable items. An item to be inserted is said to be insertable if
        /// its data groups match the existing items data groups.
        /// </summary>
        /// <param name="existingItems">Table of the exsiting items data group info</param>
        /// <param name="newItems">Table of the data group info of the items to be inserted</param>
        /// <returns>Items which have no data grop conflicts</returns>
        public static Hashtable GetInsertableItems(Hashtable existingItems, Hashtable newItems)
        {
            Hashtable insertable = new Hashtable();
            object key;
            DataGrouping.GroupInfo newInfo;
            DataGrouping.GroupInfo oldInfo;
            if (existingItems != null)
            {
                IDictionaryEnumerator ide = existingItems.GetEnumerator();
                while (ide.MoveNext())
                {
                    key = ide.Key;
                    newInfo = ((CacheEntry)newItems[key]).GroupInfo as DataGrouping.GroupInfo;
                    oldInfo = ide.Value as DataGrouping.GroupInfo;
                    insertable.Add(key, newItems[key]);
                }
            }
            return insertable;
        }

        /// <summary>
        /// Gets the list of failed items.
        /// </summary>
        /// <param name="insertResults"></param>
        /// <returns></returns>
        public static Hashtable CompileInsertResult(Hashtable insertResults)
        {
            Hashtable failedTable = new Hashtable();
            object key;
            if (insertResults != null)
            {
                CacheInsResult result;
                IDictionaryEnumerator ide = insertResults.GetEnumerator();
                while (ide.MoveNext())
                {
                    key = ide.Key;
                    if (ide.Value is Exception)
                    {
                        failedTable.Add(key, ide.Value);
                    }
                    if (ide.Value is CacheInsResultWithEntry)
                    {
                        result = ((CacheInsResultWithEntry)ide.Value).Result;
                        switch (result)
                        {
                            case CacheInsResult.Failure:
                                failedTable.Add(key, new OperationFailedException("Generic operation failure; not enough information is available."));
                                break;
                            case CacheInsResult.NeedsEviction:
                                //failedTable.Add(key, new OperationFailedException("The cache is full and not enough items could be evicted."));
                                failedTable.Add(key, new OperationFailedException(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED, ErrorMessages.GetErrorMessage(ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED)));
                                break;
                            case CacheInsResult.IncompatibleGroup:
                                failedTable.Add(key, new OperationFailedException("Data group of the inserted item does not match the existing item's data group"));
                                break;
                            case CacheInsResult.DependencyKeyNotExist:
                                failedTable.Add(key, new OperationFailedException(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.Common.DEPENDENCY_KEY_NOT_FOUND)));
                                break;
                        }
                    }
                }
            }
            return failedTable;
        }

	}
}
