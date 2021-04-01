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

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Runtime.Caching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Alachisoft.NCache.Client
{
    
    internal class CacheHelper
    {
        

        /// <summary>
        /// Get object for GetCacheItem call and get handler for the Distributed Data type.
        /// </summary>
        /// <param name="name">Key of the object</param>
        /// <param name="entryType">Enum that tells the type of the object</param>
        /// <param name="obj">value agianst that key</param>
        /// <param name="cache">RemoteCache instance</param>
        /// <returns>object</returns>
        internal static object GetObjectOrDataTypeForCacheItem(string key, EntryType entryType, object obj)
        {
            switch (entryType)
            {

                default:
                    if (obj.GetType().Equals(typeof(ValueEmissary)))
                        return obj;

                    return new ValueEmissary() { Key = key, Data = obj, Type = entryType };
            }
        }

        /// <summary>
        /// Get object for simple cache item and Get handler for the Distributed Data type.
        /// </summary>
        /// <param name="name">Key of the object</param>
        /// <param name="entryType">Enum that tells the type of the object</param>
        /// <param name="obj">value agianst that key</param>
        /// <param name="cache">RemoteCache instance</param>
        /// <returns>object</returns>
        internal static object GetObjectOrInitializedCollection(string key, EntryType entryType, object obj, Cache cache)
        {
            return GetObjectOrInitializedCollection<object>(key, entryType, obj, cache.GetCacheInstance());
        }

        /// <summary>
        /// Get object for simple cache item and Get handler for the Distributed Data type.
        /// </summary>
        /// <param name="name">Key of the object</param>
        /// <param name="entryType">Enum that tells the type of the object</param>
        /// <param name="obj">value agianst that key</param>
        /// <param name="cache">RemoteCache instance</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>object</returns>
        internal static object GetObjectOrInitializedCollection<T>(string key, EntryType entryType, object obj, Cache cache)
        {
           
                    return obj;

              
            
        }

        /// <summary>
        /// Get object for simple cache item and Get handler for the Distributed Data type for provided Bulk.
        /// </summary>
        /// <param name="keyValueDic">KeyValue Pair of Keys and CompressedValueEntries agaianst them</param>
        /// <param name="cache">RemoteCache instance</param>
        /// <returns>Hashtable</returns>
        internal static Hashtable BulkGetObjectOrInitializedCollection(Hashtable KeyValueDic, Cache cache)
        {
            return BulkGetObjectOrInitializedCollection<object>(KeyValueDic, cache);
        }

        /// <summary>
        /// Get object for simple cache item and Get handler for the Distributed Data type for provided Bulk.
        /// </summary>
        /// <param name="keyValueDic">KeyValue Pair of Keys and CompressedValueEntries agaianst them</param>
        /// <param name="cache">RemoteCache instance</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Hashtable</returns>
        internal static Hashtable BulkGetObjectOrInitializedCollection<T>(Hashtable KeyValueDic, Cache cache)
        {
            foreach (DictionaryEntry item in KeyValueDic)
            {
                CompressedValueEntry cmpEntry = (CompressedValueEntry)item.Value;
                cmpEntry.Value = GetObjectOrInitializedCollection<T>(item.Key.ToString(), cmpEntry.Type, cmpEntry.Value, cache.GetCacheInstance());
            }

            return KeyValueDic;
        }

        internal static CacheItemRemovedReason GetWebItemRemovedReason(ItemRemoveReason reason)
        {
            switch (reason)
            {
                case ItemRemoveReason.Expired:
                    return CacheItemRemovedReason.Expired;

                case ItemRemoveReason.Underused:
                    return CacheItemRemovedReason.Underused;
            }
            return CacheItemRemovedReason.Removed;
        }

        internal static void EvaluateTagsParameters(Hashtable queryInfo, string group)
        {
            if (queryInfo != null)
            {
                if (!String.IsNullOrEmpty(group) && queryInfo["tag-info"] != null)
                    throw new ArgumentException("You cannot set both groups and tags on the same cache item.");
            }
        }

        internal static byte EvaluateExpirationParameters(DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            if (Cache.NoAbsoluteExpiration.Equals(absoluteExpiration) &&
               Cache.NoSlidingExpiration.Equals(slidingExpiration))
            {
                return 2;

            }

            if (Cache.NoAbsoluteExpiration.Equals(absoluteExpiration))
            {
                if (slidingExpiration != Cache.DefaultSliding && slidingExpiration != Cache.DefaultSlidingLonger)
                {
                    if (slidingExpiration.CompareTo(TimeSpan.Zero) < 0)
                        throw new ArgumentOutOfRangeException("slidingExpiration");

                    if (slidingExpiration.CompareTo(DateTime.Now.AddYears(1) - DateTime.Now) >= 0)
                        throw new ArgumentOutOfRangeException("slidingExpiration");
                }
                return 0;
            }

            if (Cache.NoSlidingExpiration.Equals(slidingExpiration))
            {
                return 1;
            }

            throw new ArgumentException("You cannot set both sliding and absolute expirations on the same cache item.");
        }

        
        internal static void UpdateArgItemForRaisedEvent(Cache cache, EventCacheItem eventCacheItem, string itemKey)
        {
            
        }

        internal static bool IsDefaultOrNull<T>(T value)
        {
            return value != null ? value.Equals(default(T)) : true;
        }

        internal static T GetSafeValue<T>(object value)
        {
            return (T)(value ?? default(T));
        }


       

    }
}
