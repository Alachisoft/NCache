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
using System.Net;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.EvictionPolicies;

using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Caching;

#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered;
using Alachisoft.NCache.Caching.DataGrouping;
#endif

using Alachisoft.NCache.Caching.AutoExpiration;

using Alachisoft.NCache.Serialization.Surrogates;

using Alachisoft.NCache.Caching.CacheSynchronization;
using Alachisoft.NCache.Caching.DatasourceProviders;


using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Net;
#if (COMMUNITY ) && (!CLIENT)
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
using Alachisoft.NCache.Caching.Topologies.Clustered.Results;
#endif

using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;

using Alachisoft.NCache.MapReduce;

using Alachisoft.NCache.Config.Dom;
using System.Collections.Generic;
using Alachisoft.NCache.Caching.Messaging;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations.Messaging;
#endif

namespace Alachisoft.NCache.Util
{
    public class MiscUtil
    {       

        /// <summary>
        /// Returns 0 or 1, If VM based OS found returns 1 else 0
        /// </summary>        
        public static int IsEmulatedOS
        {
            get
            {

                if (IsHyperV() == 1)
                    return 1;

                return 0;
            }
        }

        /// <summary>
        /// Returns 0 or 1, If VM based OS found returns 1 else 0
        /// </summary>        
        public static int IsHyperV()
        {
            MSHyperVThread mst = new MSHyperVThread();
            return mst.IsHyperV();
        }


        /// <summary>
        /// Returns a list of mac addresses found on the system.
        /// </summary>
        public static ArrayList AdapterAddresses
        {
            get
            {

                StringBuilder addrList = new StringBuilder(2048);
                string address = addrList.ToString();
                string[] addresses = address.Split(new Char[] { ':' });

                ArrayList addrs = new ArrayList(addresses.Length);
                for (int i = 0; i < addresses.Length; i++)
                {
                    addrs.Add(addresses[i].ToLower());
                }
                return addrs;
            }
        }



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
            CompactFormatterServices.RegisterCompactType(typeof(AggregateExpirationHint), 68);
            CompactFormatterServices.RegisterCompactType(typeof(IdleExpiration), 69);
            CompactFormatterServices.RegisterCompactType(typeof(LockExpiration), 135);
            CompactFormatterServices.RegisterCompactType(typeof(FixedExpiration), 70);
            CompactFormatterServices.RegisterCompactType(typeof(KeyDependency), 71);
            CompactFormatterServices.RegisterCompactType(typeof(FixedIdleExpiration), 72);
            CompactFormatterServices.RegisterCompactType(typeof(DependencyHint), 73);
            CompactFormatterServices.RegisterCompactType(typeof(CompactCacheEntry), 105);
            CompactFormatterServices.RegisterCompactType(typeof(CallbackEntry), 107);
            CompactFormatterServices.RegisterCompactType(typeof(CallbackInfo), 111);
            CompactFormatterServices.RegisterCompactType(typeof(AsyncCallbackInfo), 112);
            CompactFormatterServices.RegisterCompactType(typeof(CacheSyncDependency), 113);
            CompactFormatterServices.RegisterCompactType(typeof(BucketStatistics), 117);
            CompactFormatterServices.RegisterCompactType(typeof(CacheInsResultWithEntry), 118);
            CompactFormatterServices.RegisterCompactType(typeof(ExtensibleDependency), 119);
            CompactFormatterServices.RegisterCompactType(typeof(DSWriteOperation), 120);
            CompactFormatterServices.RegisterCompactType(typeof(DSWriteBehindOperation), 121);
            CompactFormatterServices.RegisterCompactType(typeof(UserBinaryObject), 125);

            CompactFormatterServices.RegisterCompactType(typeof(MapReduceOperation), 363);

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.MapReduce.Notifications.TaskCallbackInfo), 364);

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.MapReduce.TaskEnumeratorResult), 365);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.MapReduce.TaskEnumeratorPointer), 366);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.MapReduce.TaskOutputPair), 367);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Runtime.MapReduce.KeyValuePair), 368);
            
            CompactFormatterServices.RegisterCompactType(typeof(Runtime.Caching.ClientInfo), 370);
            CompactFormatterServices.RegisterCompactType(typeof(Config.Dom.ClientActivityNotification),371);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.ProductVersion), 302);

            CompactFormatterServices.RegisterCompactType(typeof(RequestStatus), 303);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.Statistics.BucketStatistics.TopicStats), 383);

#if (COMMUNITY ) && (!CLIENT)
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
            CompactFormatterServices.RegisterCompactType(typeof(VirtualArray), 149);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Locking.LockManager), 150);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataStructures.DistributionMaps), 160);
            CompactFormatterServices.RegisterCompactType(typeof(EventCacheEntry), 262);
            CompactFormatterServices.RegisterCompactType(typeof(EventContext), 263);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.AutoExpiration.LockMetaInfo), 264);

#endif

#if !( CLIENT)
            CompactFormatterServices.RegisterCompactType(typeof(WriteBehindQueueRequest), 122);
            CompactFormatterServices.RegisterCompactType(typeof(WriteBehindQueueResponse), 123);
#endif


#if !CLIENT

            CompactFormatterServices.RegisterCompactType(typeof(StateTxfrInfo), 116);
            CompactFormatterServices.RegisterCompactType(typeof(NodeExpiration), 74);
            CompactFormatterServices.RegisterCompactType(typeof(PartitionedCacheBase.Identity), 77);

      

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.AutoExpiration.SqlCmdParams), 134);



            CompactFormatterServices.RegisterCompactType(typeof(DataAffinity), 106);


            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataStructures.StateTransferInfo), 130);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataStructures.ReplicatorStatusInfo), 131);
            CompactFormatterServices.RegisterCompactType(typeof(CompressedValueEntry), 133);

            CompactFormatterServices.RegisterCompactType(typeof(Function), 75);
            CompactFormatterServices.RegisterCompactType(typeof(AggregateFunction), 76);
            CompactFormatterServices.RegisterCompactType(typeof(ReplicatedCacheBase.Identity), 78);

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.Queries.QueryResultSet), 151);
            CompactFormatterServices.RegisterCompactType(typeof(TaskConfiguration), 369);

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.OperationContext), 153);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.OperationContext[]), 345);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.EventContext[]), 346);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.OperationID), 163);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Persistence.Event), 258);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Persistence.EventInfo), 259);



            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataReader.ReaderResultSet), 354);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataReader.RecordSet), 348);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataReader.ColumnCollection), 349);

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataReader.RowCollection), 350);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataReader.SubsetInfo), 351);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataReader.RecordRow), 352);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataReader.RecordColumn), 353);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Processor.EntryProcessorResult), 356);

            // For clusterd_poll
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Events.PollingResult), 357);

            CompactFormatterServices.RegisterCompactType(typeof(AcknowledgeMessageOperation), 358);
            CompactFormatterServices.RegisterCompactType(typeof(AssignmentOperation), 359);
            CompactFormatterServices.RegisterCompactType(typeof(ClusterTopicOperation), 360);
            CompactFormatterServices.RegisterCompactType(typeof(RemoveMessagesOperation), 361);
            CompactFormatterServices.RegisterCompactType(typeof(StoreMessageOperation), 362);
            CompactFormatterServices.RegisterCompactType(typeof(Common.TopicOperation), 373);
            CompactFormatterServices.RegisterCompactType(typeof(Common.SubscriptionOperation), 374);
            CompactFormatterServices.RegisterCompactType(typeof(Common.SubscriptionInfo), 375);
            CompactFormatterServices.RegisterCompactType(typeof(Common.MessageMetaData), 376);
            CompactFormatterServices.RegisterCompactType(typeof(MessageInfo), 377);
            CompactFormatterServices.RegisterCompactType(typeof(Caching.Messaging.Message), 379);
            CompactFormatterServices.RegisterCompactType(typeof(Caching.Messaging.ClientSubscriptionManager.State), 380);
            CompactFormatterServices.RegisterCompactType(typeof(Caching.Messaging.TransferrableMessage), 381);
            CompactFormatterServices.RegisterCompactType(typeof(Caching.Messaging.Topic.State), 382);
         
            CompactFormatterServices.RegisterCompactType(typeof(AtomicAcknowledgeMessageOperation), 384);
            CompactFormatterServices.RegisterCompactType(typeof(GetTransferrableMessageOperation), 385);
            CompactFormatterServices.RegisterCompactType(typeof(AtomicRemoveMessageOperation), 386);
            CompactFormatterServices.RegisterCompactType(typeof(Common.Monitoring.TopicStats), 387);
            CompactFormatterServices.RegisterCompactType(typeof(GetAssignedMessagesResponse), 388);
            CompactFormatterServices.RegisterCompactType(typeof(GetAssignedMessagesOperation), 389);

#endif



        }


#if COMMUNITY
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

#if !CLIENT
        /// <summary>Returns a random value in the range [1 - range] </summary>
        public static int Random(long range)
        {
            return (int)((Global.Random.NextDouble() * 100000) % range) + 1;
        } 
#endif
    }
}
