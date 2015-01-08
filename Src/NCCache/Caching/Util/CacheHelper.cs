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

using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Events;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered;
#endif

namespace Alachisoft.NCache.Caching.Util
{
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

		/// <summary>
		/// Tells if the cache is a clustered cache or a local one
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
#if !CLIENT
        public static bool IsClusteredCache(CacheBase cache)
		{
            if (cache == null)
                return false;
            else
			    return cache.GetType().IsSubclassOf(typeof(ClusterCacheBase));
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
            if (c1 != null && c1.Value is CallbackEntry)
            {
                CallbackEntry cbEtnry = null;
                cbEtnry = c1.Value as CallbackEntry;

                if (cbEtnry.ItemRemoveCallbackListener != null)
                {
                    foreach (CallbackInfo cbInfo in cbEtnry.ItemRemoveCallbackListener)
                        c2.AddCallbackInfo(null, cbInfo);

                }
                if (cbEtnry.ItemUpdateCallbackListener != null)
                {
                    foreach (CallbackInfo cbInfo in cbEtnry.ItemUpdateCallbackListener)
                        c2.AddCallbackInfo(cbInfo, null);

                }
            }
            if (c1 != null && c1.EvictionHint != null)
            {
                if (c2.EvictionHint == null) c2.EvictionHint = c1.EvictionHint;
            }
            return c2;
        }

        public static EventCacheEntry CreateCacheEventEntry(ArrayList listeners, CacheEntry cacheEntry)
        {
            EventCacheEntry entry = null;
            EventDataFilter maxFilter = EventDataFilter.None;
            foreach (CallbackInfo cbInfo in listeners)
            {
                if (cbInfo.DataFilter > maxFilter) maxFilter = cbInfo.DataFilter;
                if (maxFilter == EventDataFilter.DataWithMetadata) break;
            }

            return CreateCacheEventEntry(maxFilter, cacheEntry);
        }


        public static EventCacheEntry CreateCacheEventEntry(EventDataFilter? filter, CacheEntry cacheEntry)
        {
            if (filter != EventDataFilter.None && cacheEntry != null)
            {
                cacheEntry = (CacheEntry)cacheEntry.Clone();
                EventCacheEntry entry = new EventCacheEntry(cacheEntry);
                entry.Flags = cacheEntry.Flag;

                if (filter == EventDataFilter.DataWithMetadata)
                {
                    if (cacheEntry.Value is CallbackEntry)
                    {
                        entry.Value = ((CallbackEntry)cacheEntry.Value).Value;
                    }
                    else
                        entry.Value = cacheEntry.Value;

                }
                return entry;
            }
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
                                failedTable.Add(key, new OperationFailedException("The cache is full and not enough items could be evicted."));
                                break;
                        }
                    }
                }
            }
            return failedTable;
        }

	}
}
