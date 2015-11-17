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
using System.Net;
using System.Collections;
using System.Text;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Serialization.Surrogates;

using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;
using System.Text;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered;

#endif

namespace Alachisoft.NCache.Util
{

	public class MiscUtil
    {
        /// <summary>
        /// Registers types with the Compact Serializatin Framework. Range of reserved
        /// typeHandle is (61 - 1000). 
        /// </summary>
        static public void RegisterCompactTypes()
        {
            TypeSurrogateSelector.RegisterTypeSurrogate(new ArraySerializationSurrogate(typeof(CacheEntry[])));
            TypeSurrogateSelector.RegisterTypeSurrogate(new CustomArraySerializationSurrogate(typeof(CustomArraySerializationSurrogate)));
            //WARNING :  From 80 to  are also alredy in use , in different classes.
            CompactFormatterServices.RegisterCompactType(typeof(CacheEntry), 61);
            CompactFormatterServices.RegisterCompactType(typeof(PriorityEvictionHint), 64);
            CompactFormatterServices.RegisterCompactType(typeof(CacheStatistics), 65);
            CompactFormatterServices.RegisterCompactType(typeof(ClusterCacheStatistics), 66);
            CompactFormatterServices.RegisterCompactType(typeof(NodeInfo), 67);
            CompactFormatterServices.RegisterCompactType(typeof(IdleExpiration), 69);
            CompactFormatterServices.RegisterCompactType(typeof(LockExpiration), 135);
            CompactFormatterServices.RegisterCompactType(typeof(FixedExpiration), 70);
            CompactFormatterServices.RegisterCompactType(typeof(CompactCacheEntry), 105);
            CompactFormatterServices.RegisterCompactType(typeof(CallbackEntry), 107);
            CompactFormatterServices.RegisterCompactType(typeof(CallbackInfo), 111);
            CompactFormatterServices.RegisterCompactType(typeof(AsyncCallbackInfo), 112);
            CompactFormatterServices.RegisterCompactType(typeof(BucketStatistics), 117);
            CompactFormatterServices.RegisterCompactType(typeof(CacheInsResultWithEntry), 118);
            CompactFormatterServices.RegisterCompactType(typeof(UserBinaryObject), 125);
        
            CompactFormatterServices.RegisterCompactType(typeof(VirtualArray), 149);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Locking.LockManager), 150);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataStructures.DistributionMaps), 160);
            CompactFormatterServices.RegisterCompactType(typeof(EventCacheEntry), 262);
            CompactFormatterServices.RegisterCompactType(typeof(EventContext), 263);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.AutoExpiration.LockMetaInfo), 264);
#if !CLIENT
            CompactFormatterServices.RegisterCompactType(typeof(Function), 75);
            CompactFormatterServices.RegisterCompactType(typeof(AggregateFunction), 76);
            CompactFormatterServices.RegisterCompactType(typeof(PartitionedCacheBase.Identity), 77);
            CompactFormatterServices.RegisterCompactType(typeof(ReplicatedCacheBase.Identity), 78);
            CompactFormatterServices.RegisterCompactType(typeof(StateTxfrInfo), 116);
            CompactFormatterServices.RegisterCompactType(typeof(CompressedValueEntry), 133);
#endif
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.Queries.QueryResultSet), 151);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.OperationContext), 153);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.OperationContext[]), 345);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.EventContext[]), 346);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.OperationID), 163);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.NCacheSessionItem), 129);
        }
        /// <summary>
        /// Converts the Address into a System.Net.IPEndPoint.
        /// </summary>
        /// <param name="address">Address</param>
        /// <returns>System.Net.IPEndPoint</returns>
        public static IPEndPoint AddressToEndPoint(Address address)
        {
            Address ipAddr = address as Address;
            if (ipAddr == null) return null;
            return new IPEndPoint(ipAddr.IpAddress, ipAddr.Port);
        }


        /// <summary>
        /// Returns an array containing list of keys contained in the cache. Null if there are no
        /// keys or if timeout occurs.
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        internal static object[] GetKeyset(CacheBase cache, int timeout)
        {
            ulong index = 0;
            object[] objects = null;
            cache.Sync.AcquireWriterLock(timeout);
            try
            {
                if (!cache.Sync.IsWriterLockHeld || cache.Count < 1) return objects;
                objects = new object[cache.Count];
                for (IEnumerator i = cache.GetEnumerator(); i.MoveNext(); )
                {
                    objects[index++] = ((DictionaryEntry)i.Current).Key;
                }
            }
            finally
            {
                cache.Sync.ReleaseWriterLock();
            }
            return objects;
        }


        /// <summary>
        /// Get the contents of list as array
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static object[] GetArrayFromCollection(ICollection col)
        {
            if (col == null) return null;
            object[] arr = new object[col.Count];
            col.CopyTo(arr, 0);
            return arr;
        }


        /// <summary>
        /// Get the keys that are not in the list
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static object[] GetNotAvailableKeys(object[] keys, ArrayList list)
        {
            Hashtable table = new Hashtable();
            foreach (object key in list)
            {
                table.Add(key, "");
            }

            return GetNotAvailableKeys(keys, table);
        }


        /// <summary>
        /// Converts bytes into mega bytes (MB).
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>MB</returns>
        public static double ConvertToMegaBytes(long bytes)
        {
            return (double)bytes / (1024 * 1024);
        }
        /// <summary>
        /// Get the keys that are not in the Hashtable
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static object[] GetNotAvailableKeys(object[] keys, Hashtable table)
        {
            object[] unAvailable = new object[keys.Length - table.Count];

            int i = 0;
            foreach (object key in keys)
            {
                if (table.Contains(key) == false)
                {
                    unAvailable[i] = key;
                    i++;
                }
            }

            return unAvailable;
        }

        /// <summary>
        /// Fill unavailable keys, available keys and their relative data
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="entries"></param>
        /// <param name="unAvailable"></param>
        /// <param name="available"></param>
        /// <param name="data"></param>
        /// <param name="list"></param>
        public static void FillArrays(object[] keys, CacheEntry[] entries, object[] unAvailable, object[] available, CacheEntry[] data, ArrayList list)
        {
            Hashtable table = new Hashtable();
            foreach (object key in list)
            {
                table.Add(key, "");
            }
            FillArrays(keys, entries, unAvailable, available, data, table);
        }


        /// <summary>
        /// Fill unavailable keys, available keys and their relative data
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="entries"></param>
        /// <param name="unAvailable"></param>
        /// <param name="available"></param>
        /// <param name="data"></param>
        /// <param name="table"></param>
        public static void FillArrays(object[] keys, CacheEntry[] entries, object[] unAvailable, object[] available, CacheEntry[] data, Hashtable table)
        {
            int a = 0, u = 0, i = 0;
            foreach (object key in keys)
            {
                if (table.Contains(key) == false)
                {
                    available[a] = key;
                    data[a] = entries[i];
                    a++;
                }
                else
                {
                    unAvailable[u] = key;
                    u++;
                }
                i++;
            }
        }


        /// <summary>
        /// Fill available keys and their relative data
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="entries"></param>
        /// <param name="available"></param>
        /// <param name="data"></param>
        /// <param name="list"></param>
        public static void FillArrays(object[] keys, CacheEntry[] entries, object[] available, CacheEntry[] data, ArrayList list)
        {
            Hashtable table = new Hashtable();
            foreach (object key in list)
            {
                table.Add(key, "");
            }
            FillArrays(keys, entries, available, data, table);
        }

        /// <summary>
        /// Fill available keys and their relative data
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="entries"></param>
        /// <param name="available"></param>
        /// <param name="data"></param>
        /// <param name="table"></param>
        public static void FillArrays(object[] keys, CacheEntry[] entries, object[] available, CacheEntry[] data, Hashtable table)
        {
            int a = 0, i = 0;
            foreach (object key in keys)
            {
                if (table.Contains(key) == false)
                {
                    available[a] = key;
                    data[a] = entries[i];
                    a++;
                }
                i++;
            }
        }


        public static IDictionary DeepClone(IDictionary dic)
        {
            Hashtable table = new Hashtable();
            foreach (DictionaryEntry entry in dic)
            {
                ICloneable list = entry.Value as ICloneable;
                if (list != null)
                {
                    table[entry.Key] = list.Clone();
                }
                else 
                {
                    table[entry.Key] = entry.Value;
                }
            }
            return table;
        }
    }
}
