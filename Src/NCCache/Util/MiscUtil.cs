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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config.Dom;

using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Serialization.Surrogates;
#if !CLIENT && !DEVELOPMENT
using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Caching.Topologies.Clustered;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
using Alachisoft.NCache.Caching.Topologies.Clustered.Results;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations.Messaging;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Alachisoft.NCache.Common.Topologies.Clustered;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling.Stats;
using Alachisoft.NCache.Common.Monitoring;


namespace Alachisoft.NCache.Util
{
    public class MiscUtil
    {
        static int _numCores;
        static int _numProcessors;
        static ArrayList _addresses = null;
        static StringBuilder installCode = null;
        static bool nclicensedllLoaded = false;


        static MiscUtil()
        {
          
        }
       
        /// <summary>
        /// Registers types with the Compact Serializatin Framework. Range of reserved
        /// typeHandle is (61 - 1000). 
        /// </summary>
        static public void RegisterCompactTypes(PoolManager poolManager)
        {
            TypeSurrogateSelector.RegisterTypeSurrogate(new ArraySerializationSurrogate(typeof(CacheEntry[])));
            TypeSurrogateSelector.RegisterTypeSurrogate(new CustomArraySerializationSurrogate(typeof(CustomArraySerializationSurrogate)));
            CompactFormatterServices.RegisterCompactType(typeof(CacheEntry), 61);
            CompactFormatterServices.RegisterCompactType(typeof(CounterHint), 62);
            CompactFormatterServices.RegisterCompactType(typeof(TimestampHint), 63);
            CompactFormatterServices.RegisterCompactType(typeof(PriorityEvictionHint), 64);
            CompactFormatterServices.RegisterCompactType(typeof(CacheStatistics), 65);
            CompactFormatterServices.RegisterCompactType(typeof(ClusterCacheStatistics), 66);
            CompactFormatterServices.RegisterCompactType(typeof(NodeInfo), 67);
            CompactFormatterServices.RegisterCompactType(typeof(AggregateExpirationHint), 68);
            CompactFormatterServices.RegisterCompactType(typeof(IdleExpiration), 69, pool: null);
            CompactFormatterServices.RegisterCompactType(typeof(LockExpiration), 135, pool: null);
            CompactFormatterServices.RegisterCompactType(typeof(FixedExpiration), 70);
            CompactFormatterServices.RegisterCompactType(typeof(FixedIdleExpiration), 72, pool: null);
            CompactFormatterServices.RegisterCompactType(typeof(DependencyHint), 73, pool: null);
            CompactFormatterServices.RegisterCompactType(typeof(CompactCacheEntry), 105);
            CompactFormatterServices.RegisterCompactType(typeof(Caching.Notifications), 107, null);
            CompactFormatterServices.RegisterCompactType(typeof(CallbackInfo), 111);
            CompactFormatterServices.RegisterCompactType(typeof(AsyncCallbackInfo), 112);
            CompactFormatterServices.RegisterCompactType(typeof(BucketStatistics), 117);
            CompactFormatterServices.RegisterCompactType(typeof(CacheInsResultWithEntry), 118);
          
            CompactFormatterServices.RegisterCompactType(typeof(UserBinaryObject), 125, pool: null);
            CompactFormatterServices.RegisterCompactType(typeof(Runtime.Caching.ClientInfo), 370);
            CompactFormatterServices.RegisterCompactType(typeof(ClientActivityNotification), 371);
            CompactFormatterServices.RegisterCompactType(typeof(Common.ProductVersion), 302);
            CompactFormatterServices.RegisterCompactType(typeof(Common.DataStructures.RequestStatus), 303);
            CompactFormatterServices.RegisterCompactType(typeof(BucketStatistics.TopicStats), 383);
#if (!CLIENT && !DEVELOPMENT)
            CompactFormatterServices.RegisterCompactType(typeof(ReadFromStreamOperation), 138);
            CompactFormatterServices.RegisterCompactType(typeof(WriteToStreamOperation), 139);
            CompactFormatterServices.RegisterCompactType(typeof(GetStreamLengthOperation), 140);
            CompactFormatterServices.RegisterCompactType(typeof(ClusterOperationResult), 141);
            CompactFormatterServices.RegisterCompactType(typeof(OpenStreamResult), 142);
            CompactFormatterServices.RegisterCompactType(typeof(CloseStreamResult), 143);
            CompactFormatterServices.RegisterCompactType(typeof(ReadFromStreamResult), 144);
            CompactFormatterServices.RegisterCompactType(typeof(WriteToStreamResult), 145);
            CompactFormatterServices.RegisterCompactType(typeof(GetStreamLengthResult), 146);
            CompactFormatterServices.RegisterCompactType(typeof(OpenStreamOperation), 147);
            CompactFormatterServices.RegisterCompactType(typeof(CloseStreamOperation), 148);
           
            CompactFormatterServices.RegisterCompactType(typeof(DataAffinity), 106);
            CompactFormatterServices.RegisterCompactType(typeof(Function), 75);
            CompactFormatterServices.RegisterCompactType(typeof(AggregateFunction), 76);
            CompactFormatterServices.RegisterCompactType(typeof(MirrorCacheBase.Identity), 129);
            CompactFormatterServices.RegisterCompactType(typeof(AcknowledgeMessageOperation), 358);
            CompactFormatterServices.RegisterCompactType(typeof(AssignmentOperation), 359);
            CompactFormatterServices.RegisterCompactType(typeof(ClusterTopicOperation), 360);
            CompactFormatterServices.RegisterCompactType(typeof(RemoveMessagesOperation), 361);
            CompactFormatterServices.RegisterCompactType(typeof(StoreMessageOperation), 362);
            CompactFormatterServices.RegisterCompactType(typeof(AtomicAcknowledgeMessageOperation), 384);
            CompactFormatterServices.RegisterCompactType(typeof(GetTransferrableMessageOperation), 385);
            CompactFormatterServices.RegisterCompactType(typeof(AtomicRemoveMessageOperation), 386);
            CompactFormatterServices.RegisterCompactType(typeof(GetAssignedMessagesResponse), 388);
            CompactFormatterServices.RegisterCompactType(typeof(GetAssignedMessagesOperation), 389);
            CompactFormatterServices.RegisterCompactType(typeof(CacheItemBase), 431, pool: null);
      
          
           
         
          
           
            CompactFormatterServices.RegisterCompactType(typeof(ReplicaStateTxfrInfo), 469);
         
#endif

            CompactFormatterServices.RegisterCompactType(typeof(VirtualArray), 149);
            CompactFormatterServices.RegisterCompactType(typeof(Common.Locking.LockManager), 150);
            CompactFormatterServices.RegisterCompactType(typeof(DistributionMaps), 160);
            CompactFormatterServices.RegisterCompactType(typeof(EventCacheEntry), 262);
            CompactFormatterServices.RegisterCompactType(typeof(EventContext), 263);
            CompactFormatterServices.RegisterCompactType(typeof(LockMetaInfo), 264);

            CompactFormatterServices.RegisterCompactType(typeof(NodeExpiration), 74);

            CompactFormatterServices.RegisterCompactType(typeof(SqlCmdParams), 134);

            CompactFormatterServices.RegisterCompactType(typeof(StateTransferInfo), 130);
            CompactFormatterServices.RegisterCompactType(typeof(ReplicatorStatusInfo), 131);
            CompactFormatterServices.RegisterCompactType(typeof(CompressedValueEntry), 133);
            CompactFormatterServices.RegisterCompactType(typeof(OperationContext), 153);
            CompactFormatterServices.RegisterCompactType(typeof(OperationContext[]), 345);
            CompactFormatterServices.RegisterCompactType(typeof(EventContext[]), 346);
            CompactFormatterServices.RegisterCompactType(typeof(OperationID), 163);
            CompactFormatterServices.RegisterCompactType(typeof(Persistence.Event), 258);
            CompactFormatterServices.RegisterCompactType(typeof(Persistence.EventInfo), 259);
            CompactFormatterServices.RegisterCompactType(typeof(Common.Events.PollingResult), 357);
            CompactFormatterServices.RegisterCompactType(typeof(Common.TopicOperation), 373);
            CompactFormatterServices.RegisterCompactType(typeof(Common.SubscriptionOperation), 374);
            CompactFormatterServices.RegisterCompactType(typeof(Common.SubscriptionInfo), 375);
            CompactFormatterServices.RegisterCompactType(typeof(Common.MessageMetaData), 376);
            CompactFormatterServices.RegisterCompactType(typeof(MessageInfo), 377);
            CompactFormatterServices.RegisterCompactType(typeof(Caching.Messaging.Message), 379);
            CompactFormatterServices.RegisterCompactType(typeof(ClientSubscriptionManager.State), 380);
            CompactFormatterServices.RegisterCompactType(typeof(TransferrableMessage), 381);
            CompactFormatterServices.RegisterCompactType(typeof(Topic.State), 382);
            CompactFormatterServices.RegisterCompactType(typeof(Common.Monitoring.TopicStats), 387);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Config.NewDom.CacheServerConfig), 393);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Config.NewDom.CacheDeployment), 394);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Config.NewDom.ServersNodes), 396);

            CompactFormatterServices.RegisterCompactType(typeof(Caching.Messaging.EventMessage), 470);
            CompactFormatterServices.RegisterCompactType(typeof(MultiCastMessage), 471);
            CompactFormatterServices.RegisterCompactType(typeof(Caching.EventId), 472);
          
            CompactFormatterServices.RegisterCompactType(typeof(MessageResponse), 503);
            CompactFormatterServices.RegisterCompactType(typeof(TopicState), 505);
            CompactFormatterServices.RegisterCompactType(typeof(SubscriptionIdentifier), 506);
            CompactFormatterServices.RegisterCompactType(typeof(Subscriptions), 507);
            CompactFormatterServices.RegisterCompactType(typeof(ExclusiveSubscriptions), 508);
            
            CompactFormatterServices.RegisterCompactType(typeof(EventSubscriptions), 515);
            CompactFormatterServices.RegisterCompactType(typeof(EventMessageBase), 519);
            CompactFormatterServices.RegisterCompactType(typeof(ExpireSubscriptionOperation), 521);
          
            CompactFormatterServices.RegisterCompactType(typeof(BucketStatistics[]), 523);
            CompactFormatterServices.RegisterCompactType(typeof(ClientProfile), 538);
          
            #region - [PoolStats] -
            CompactFormatterServices.RegisterCompactType(typeof(PoolStats), 526);
            CompactFormatterServices.RegisterCompactType(typeof(ArrayPoolStats), 527);
            CompactFormatterServices.RegisterCompactType(typeof(ObjectPoolStats), 528);
            CompactFormatterServices.RegisterCompactType(typeof(StringPoolStats), 529);
            CompactFormatterServices.RegisterCompactType(typeof(PoolStatsRequest), 530);
            CompactFormatterServices.RegisterCompactType(typeof(ArrayPoolStats[]), 531);
            CompactFormatterServices.RegisterCompactType(typeof(ObjectPoolStats[]), 532);
            CompactFormatterServices.RegisterCompactType(typeof(StringPoolStats[]), 533);
            #endregion


        }

        



#if SERVER
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
#endif

        /// <summary>
        /// Returns an array containing list of keys contained in the cache. Null if there are no
        /// keys or if timeout occurs.
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        internal static object[] GetKeyset(CacheBase cache, int timeout)
        {
            int index = 0;
            object[] objects = null;
            cache.Sync.AcquireWriterLock(timeout);
            try
            {
                if (!cache.Sync.IsWriterLockHeld || cache.Count < 1) return objects;
                objects = new object[cache.Count];

                for (IEnumerator i = cache.GetEnumerator(); i.MoveNext() && index < objects.Length;)
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
        /// Returns an array containing list of keys contained in the cache. Null if there are no
        /// keys or if timeout occurs.
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        internal static object[] GetKeys(CacheBase cache, int timeout)
        {
            int index = 0;
            var keys = cache.Keys;
            object[] objects = null;

            if (keys.Length < 1) return objects;

            cache.Sync.AcquireWriterLock(timeout);
            try
            {
                objects = new object[keys.Length];

                foreach (var key in keys)
                {
                    if (index > keys.Length)
                        break;

                    objects[index++] = key;
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
        /// Get the contents of list as array
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<string> GetListFromCollection(ICollection col)
        {
            if (col == null) return null;
            List<string> arr = new List<string>();
            foreach (var item in col)
            {
                arr.Add(item as string);
            }
            return arr;
        }

        /// <summary>
        /// Get the keys that are not in the list
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static object[] GetNotAvailableKeys(object[] keys, ClusteredArrayList list)
        {
            HashVector table = new HashVector();
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
        public static object[] GetNotAvailableKeys(object[] keys, HashVector table)
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
                if (table.Contains(key))
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

#if !CLIENT
        /// <summary>Returns a random value in the range [1 - range] </summary>
        public static int Random(long range)
        {
            return (int)((Global.Random.NextDouble() * 100000) % range) + 1;
        }
#endif

        #region Collection

      
     

        public static EntryType ProtoItemTypeToEntryType(CacheItemType.ItemType itemType)
        {
            switch (itemType)
            {
                case (CacheItemType.ItemType.CACHEITEM):
                    return EntryType.CacheItem;
                    break;
               
                default:
                    throw new ArgumentException("ItemType must be properly specified");
            }

        }

        public static CacheItemType.ItemType EntryTypeToProtoItemType(EntryType entryType)
        {
            switch (entryType)
            {
                case (EntryType.CacheItem):
                    return CacheItemType.ItemType.CACHEITEM;
                    break;
               
                default:
                    throw new ArgumentException("EntryType must be properly specified");
            }

        }

   
        #endregion

        #region Returning Items to Pool

        internal static bool ReturnEntryToPool(CacheEntry e, PoolManager poolManager)
        {
            if (poolManager == null)
                return false;

            if (e != null && e.FromPool(poolManager))
            {
                e.ReturnLeasableToPool();
                poolManager.GetCacheEntryPool().Return(e);
                return true;
            }

            return false;
        }

        internal static bool ReturnExpirationHintToPool(ExpirationHint expirationHint, PoolManager poolManager)
        {
            if (poolManager == null)
                return false;

            if (expirationHint != null && expirationHint.FromPool(poolManager))
            {
                expirationHint.ReturnLeasableToPool();

                switch (expirationHint._hintType)
                {
                    case ExpirationHintType.FixedExpiration:
                        poolManager.GetFixedExpirationPool().Return((FixedExpiration)expirationHint);
                        break;

                    case ExpirationHintType.TTLExpiration:
                        poolManager.GetTTLExpirationPool().Return((TTLExpiration)expirationHint);
                        break;

                    case ExpirationHintType.FixedIdleExpiration:
                        poolManager.GetFixedIdleExpirationPool().Return((FixedIdleExpiration)expirationHint);
                        break;
#if !(DEVELOPMENT || CLIENT)
                    case ExpirationHintType.NodeExpiration:
                        poolManager.GetNodeExpirationPool().Return((NodeExpiration)expirationHint);
                        break;
#endif
                    case ExpirationHintType.IdleExpiration:
                        poolManager.GetIdleExpirationPool().Return((IdleExpiration)expirationHint);
                        break;

                    case ExpirationHintType.AggregateExpirationHint:
                        poolManager.GetAggregateExpirationHintPool().Return((AggregateExpirationHint)expirationHint);
                        break;
                        
                    default:
                        throw new System.Exception("Invalid expiration hint.");
                }

                return true;
            }

            return false;
        }

        internal static bool ReturnEvictionHintToPool(EvictionHint hint, PoolManager poolManager)
        {
            if (poolManager == null)
                return false;

            if (hint != null && hint.FromPool(poolManager))
            {
                hint.ReturnLeasableToPool();

                switch (hint._hintType)
                {
                    case EvictionHintType.CounterHint:
                        poolManager.GetCounterHintPool().Return((CounterHint)hint);
                        break;
                    case EvictionHintType.PriorityEvictionHint:
                        poolManager.GetPriorityEvictionHintPool().Return((PriorityEvictionHint)hint);
                        break;
                    case EvictionHintType.TimestampHint:
                        poolManager.GetTimestampHintPool().Return((TimestampHint)hint);
                        break;
                }

                return true;
            }

            return false;
        }

        internal static bool ReturnCompressedEntryToPool(CompressedValueEntry e, PoolManager poolManager)
        {
            if (poolManager == null)
                return false;

            if (e != null && e.FromPool(poolManager))
            {
                e.ReturnLeasableToPool();
                poolManager.GetCompressedValueEntryPool().Return(e);
                return true;
            }

            return false;
        }


        internal static bool ReturnCacheInsResultToPool(CacheInsResultWithEntry result, PoolManager poolManager)
        {
            if (poolManager == null || result == null || !result.FromPool(poolManager))
                return false;

            result.ReturnLeasableToPool();
            poolManager.GetCacheInsResultWithEntryPool().Return(result);
            return true;
        }

        internal static void ReturnEntriesToPool(List<CacheEntry> entries, PoolManager poolManager)
        {
            if (poolManager == null)
                return;

            if (entries != null)
            {
                //using for loop instead of foreach to avoid enumerator creation
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    if (e != null && e.FromPool(poolManager))
                    {
                        e.ReturnLeasableToPool();
                        poolManager.GetCacheEntryPool().Return(e);
                    }
                }
            }
        }

        internal static void ReturnEntriesToPool(IList<CacheEntry> entries, PoolManager poolManager)
        {
            if (poolManager == null || entries == null || entries.Count == 0)
                return;

            // Using for loop instead of foreach to avoid enumerator creation
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (entry != null && entry.FromPool(poolManager))
                {
                    entry.ReturnLeasableToPool();
                    poolManager.GetCacheEntryPool().Return(entry);
                }
            }
        }

        internal static bool ReturnOperationContextToPool(OperationContext context, PoolManager poolManager)
        {
            if (poolManager == null)
                return false;

            if (context != null && context.FromPool(poolManager))
            {
                context.ReturnLeasableToPool();
                poolManager.GetOperationContextPool().Return(context);
                return true;
            }

            return false;
        }

        internal static bool ReturnBitsetToPool(BitSet bitset, PoolManager poolManager)
        {
            if (poolManager == null)
                return false;

            if (bitset != null && bitset.FromPool(poolManager))
            {
                bitset.ReturnLeasableToPool();
                poolManager.GetBitSetPool().Return(bitset);
                return true;
            }

            return false;
        }
        

        internal static bool ReturnUserBinaryObjectToPool(UserBinaryObject userBinaryObject, PoolManager poolManager)
        {
            if (poolManager == null || userBinaryObject == null || !userBinaryObject.FromPool(poolManager))
                return false;

            userBinaryObject.ReturnLeasableToPool();

            if (userBinaryObject is SmallUserBinaryObject smallUserBinaryObject)
                poolManager.GetSmallUserBinaryObjectPool().Return(smallUserBinaryObject);

            else if (userBinaryObject is LargeUserBinaryObject largeUserBinaryObject)
                poolManager.GetLargeUserBinaryObjectPool().Return(largeUserBinaryObject);

            return true;
        }

        internal static bool ReturnByteArrayToPool(byte[] bytes, PoolManager poolManager)
        {
            if (poolManager == null || bytes == null)
                return false;

            poolManager.GetByteArrayPool().Return(bytes);
            return true;
        }

        #endregion

    }
}
