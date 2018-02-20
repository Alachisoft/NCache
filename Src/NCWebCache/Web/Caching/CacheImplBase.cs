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
using System.Collections;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Caching.Queries;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.MapReduce;
using Alachisoft.NCache.Web.MapReduce;
using Alachisoft.NCache.Common.MapReduce;
using Alachisoft.NCache.Runtime.Processor;
using Alachisoft.NCache.Common.Events;

namespace Alachisoft.NCache.Web.Caching
{
    internal class CacheImplBase : TaskEnumeratorHandler, ITaskManagement
    {
        private ClientInfo _clientInfo;

        private string _clientID;

        internal CacheImplBase()
        {
            _clientInfo = new ClientInfo();
            _clientInfo.ProcessID = System.Diagnostics.Process.GetCurrentProcess().Id;
            _clientInfo.ClientID = System.Guid.NewGuid().ToString();
            _clientInfo.MachineName = Environment.MachineName;
            _clientID = ClientInfo.GetLegacyClientID(_clientInfo);
        }

        internal ClientInfo LocalClientInfo
        {
            get { return _clientInfo; }
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


        /// <summary>
        /// Occurs in response to a <see cref="Cache.RaiseCustomEvent"/> method call.
        /// </summary>
        /// <remarks>
        /// You can use this event to handle custom application defined event notifications.
        /// <para>Doing a lot of processing inside the handler might have an impact on the performance 
        /// of the cache and cluster. It is therefore advisable to do minimal processing inside the handler.
        /// </para>
        /// For more information on how to use this callback see the documentation 
        /// for <see cref="CustomEventCallback"/>.
        /// </remarks>
        public event CustomEventCallback CustomEvent;


        public virtual long Count
        {
            get { return 0; }
        }

        public string ClientID
        {
            get { return _clientID; }
        }

        public virtual void RegisterGeneralNotification(EventType eventType, EventDataFilter datafilter,
            short sequenceNumber)
        {
        }

        public virtual void UnRegisterGeneralNotification(EventType unregister, short sequenceNumber)
        {
        }


        public virtual void RegisterAddEvent()
        {
        }

        public virtual void RegisterRemoveEvent()
        {
        }

        public virtual void RegisterUpdateEvent()
        {
        }

        public virtual void RegisterCustomEvent()
        {
        }

        public virtual void RegisterNodeJoinedEvent()
        {
        }

        public virtual void RegisterNodeLeftEvent()
        {
        }

        public virtual void UnregisterAddEvent()
        {
        }

        public virtual void UnregisterRemoveEvent()
        {
        }

        public virtual void UnregisterUpdateEvent()
        {
        }

        public virtual void UnregisterCustomEvent()
        {
        }

        public virtual void UnregisterNodeJoinedEvent()
        {
        }

        public virtual void UnregisterNodeLeftEvent()
        {
        }

        public virtual void UnregisterHashmapChangedEvent()
        {
        }


        public virtual void RegisterCacheStoppedEvent()
        {
        }

        public virtual void UnregisterCacheStoppedEvent()
        {
        }

        public virtual void RegisterClearEvent()
        {
        }

        public virtual void UnregisterClearEvent()
        {
        }


        public virtual string Name
        {
            get { return null; }
        }


        public virtual void Dispose(bool disposing)
        {
        }


        public virtual object this[string key]
        {
            get { return null; }
            set { }
        }


        public virtual object Add(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback,
            short onDsItemAddedCallback, bool isResyncExpiredItems,
            string group, string subGroup, Hashtable queryInfo, BitSet flagMap, string providerName,
            string resyncProviderName, EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, long size, string clientId)
        {
            return null;
        }


        /// <summary>
        /// Add array of <see cref="CacheItem"/> to the cache.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="group">The data group of the item</param>
        /// <param name="subGroup">Sub group of the group</param>
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
        ///	NCache.Cache.Add(keys, items, "Customer", "Orders");
        /// 
        ///	Cache.Add(keys, items, "Customer", "Orders");
        /// 
        /// </code>
        /// </example>
        public virtual IDictionary Add(string[] keys, CacheItem[] items,
            short onDataSourceItemsAdded, string providerName, long[] sizes,
            string clientId, short updateCallbackId, short removeCallbackId,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, bool returnVersions,
            out IDictionary itemVersions, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            itemVersions = null;
            return null;
        }

        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        public virtual void AddAsync(string key, object value,
            CacheDependency dependency, CacheSyncDependency syncDependency,
            DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback,
            short onAsyncItemAddCallback, short dsItemAddedCallback, bool isResyncExpiredItems,
            string group, string subGroup, Hashtable queryInfo, BitSet flagMap, string providerName,
            string resyncProviderName, EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, long size, string clientId)
        {
            return;
        }

        /// <summary>
        /// Add dependency to the cache item.
        /// </summary>
        /// <param name="key">key used to reference the required object</param>
        /// <param name="dependency">CacheDependency to be added</param>
        /// <param name="isResyncRequired">Boolean value indicating wether Resync is required or not</param>
        /// <returns>True if operations successeded else false</returns>
        public virtual bool AddDependency(string key, CacheDependency dependency, bool isResyncRequired)
        {
            return false;
        }

        public virtual bool AddDependency(string key, CacheSyncDependency syncDependency)
        {
            return false;
        }

        public virtual void Clear(BitSet flagMap, short onDsClearedCallback, string providerName)
        {
        }


        public virtual void ClearAsync(BitSet flagMap, short onDsClearedCallback, string providerName)
        {
        }

        public virtual void ClearAsync(BitSet flagMap, short onAsyncCacheClearCallback, short onDsClearedCallback,
            string providerName)
        {
        }


        public virtual bool Contains(string key)
        {
            return false;
        }


        public virtual CompressedValueEntry Get(string key, BitSet flagMap, string group, string subGroup,
            ref CacheItemVersion version, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType,
            string providerName)
        {
            return null;
        }

        public virtual Hashtable GetByTag(Tag[] tags, TagComparisonType comaprisonType)
        {
            return null;
        }

        public virtual ICollection GetKeysByTag(Tag[] tags, TagComparisonType comaprisonType)
        {
            return null;
        }

        public virtual void RemoveByTag(Tag[] tags, TagComparisonType comaprisonType)
        {
        }


        public virtual ArrayList GetGroupKeys(string group, string subGroup)
        {
            return null;
        }

        public virtual IDictionary GetGroupData(string group, string subGroup)
        {
            return null;
        }

        public virtual void RaiseCustomEvent(object notifId, object data)
        {
        }

        public virtual IDictionary Get(string[] keys, BitSet flagMap, string providerName)
        {
            return null;
        }


        public virtual object GetCacheItem(string key, BitSet flagMap, string group, string subGroup,
            ref CacheItemVersion version, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType,
            string providerName)
        {
            return null;
        }

        public virtual CacheItemVersion Insert(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback,
            short onDsItemUpdatedCallback, bool isResyncExpiredItems,
            string group, string subGroup, Hashtable queryInfo, BitSet flagMap, object lockId, CacheItemVersion version,
            LockAccessType accessType, string providerName,
            string resyncProviderName, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter,
            long size, string clientId, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            return null;
        }


        public virtual IDictionary Insert(string[] keys,
            CacheItem[] items, short onDsItemsUpdatedCallback, string providerName,
            long[] sizes, string clientId,
            short updateCallbackId, short removeCallbackId,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, bool returnVersions,
            out IDictionary itemVersions, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            itemVersions = null;
            return null;
        }

        public virtual void InsertAsync(string key, object value,
            CacheDependency dependency, CacheSyncDependency syncDependency,
            DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, short onRemoveCallback,
            short onUpdateCallback, short onAsyncItemUpdateCallback,
            short onDsItemUpdatedCallback, bool isResyncExpiredItems,
            string group, string subGroup, Hashtable queryInfo, BitSet flagMap,
            string providerName, string resyncProviderName,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallbackFilter,
            long size, string clientId)
        {
        }

        public virtual CompressedValueEntry Remove(string key, BitSet flagMap, short dsItemRemovedCallbackId,
            object lockId, CacheItemVersion version, LockAccessType accessType, string ProviderName)
        {
            return null;
        }

        public virtual void Delete(string key, BitSet flagMap, short dsItemRemovedCallbackId, object lockId,
            CacheItemVersion version, LockAccessType accessType, string ProviderName)
        {
        }


        public virtual IDictionary Remove(string[] keys, BitSet flagMap, string providerName,
            short onDsItemsRemovedCallback)
        {
            return null;
        }

        public virtual void Delete(string[] keys, BitSet flagMap, string providerName, short onDsItemsRemovedCallback)
        {
        }

        public virtual void RemoveAsync(string key, BitSet flagMap, short onDsItemRemovedCallback)
        {
        }

        public virtual void RemoveAsync(string key, BitSet flagMap, short onAsyncItemRemoveCallback,
            short onDsItemRemovedCallback, string providerName)
        {
        }

        public virtual void Remove(string group, string subGroup)
        {
        }

        public virtual IRecordSetEnumerator ExecuteReader(string query, IDictionary values, bool getData, int chunkSize)
        {
            return null;
        }

        public virtual IRecordSetEnumerator ExecuteReaderCQ(ContinuousQuery continuousQuery, bool getData,
            int chunkSize, string clientUniqueId, bool notifyAdd, bool notifyUpdate, bool notifyRemove)
        {
            return null;
        }

        public virtual QueryResultSet Search(string query, IDictionary values)
        {
            return null;
        }

        public virtual QueryResultSet SearchEntries(string query, IDictionary values)
        {
            return null;
        }

        public virtual QueryResultSet SearchCQ(ContinuousQuery query, string clientUniqueId, bool notifyAdd,
            bool notifyUpdate, bool notifyRemove)
        {
            return null;
        }

        public virtual QueryResultSet SearchEntriesCQ(ContinuousQuery query, string clientUniqueId, bool notifyAdd,
            bool notifyUpdate, bool notifyRemove)
        {
            return null;
        }

        public virtual int ExecuteNonQuery(string query, IDictionary values)
        {
            return 0;
        }


        public virtual object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag,
            CacheImplBase cacheImpl, ref long size)
        {
            return null;
        }

        public virtual object SafeDeserialize(object serializedObject, string serializationContext, BitSet flag,
            CacheImplBase cacheImpl)
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

        public virtual Hashtable GetEncryptionInfo()
        {
            return null;
        }

        public virtual Hashtable GetExpirationInfo()
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

        public virtual void RegisterKeyNotificationCallback(string key, short updateCallbackid, short removeCallbackid,
            bool notifyOnItemExpiration)
        {
        }

        public virtual void UnRegisterKeyNotificationCallback(string key, short updateCallbackid,
            short removeCallbackid)
        {
        }

        public virtual void RegisterKeyNotificationCallback(string key, short update, short remove,
            EventDataFilter datafilter, bool notifyOnItemExpiration,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
        }

        public virtual void RegisterKeyNotificationCallback(string key, short update, short remove,
            EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
        }

        public virtual void UnRegisterKeyNotificationCallback(string key, short update, short remove,
            EventType eventType)
        {
        }

        public virtual void RegisterKeyNotificationCallback(string[] keys, short updateCallbackid,
            short removeCallbackid, string clientId, CallbackType callbackType = CallbackType.PullBasedCallback)
        {
        }

        public virtual void UnRegisterKeyNotificationCallback(string[] keys, short updateCallbackid,
            short removeCallbackid)
        {
        }

        public virtual void RegisterKeyNotificationCallback(string[] key, short update, short remove,
            EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
        }

        public virtual void UnRegisterKeyNotificationCallback(string[] key, short update, short remove,
            EventType eventType)
        {
        }

        public virtual void RegisterKeyNotificationCallback(string[] key, short update, short remove,
            EventDataFilter datafilter, bool notifyOnItemExpiration,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
        }

        public virtual void RegisterPollingNotification(short pollingCallbackId)
        {
        }


        #region /           --- Stream Operations ---                      /

        public virtual string OpenStream(string key, StreamModes mode, string group, string subGroup,
            DateTime absExpiration, TimeSpan slidingExpiration, CacheDependency dependency, CacheItemPriority priority)
        {
            return null;
        }

        public virtual void CloseStream(string key, string lockHandle)
        {
        }

        public virtual int ReadFromStream(ref byte[] buffer, string key, string lockHandle, int offset,
            int streamOffset, int length)
        {
            return 0;
        }

        public virtual void WriteToStream(string key, string lockHandle, byte[] buffer, int srcOffset, int dstOffset,
            int length)
        {
        }

        public virtual long GetStreamLength(string key, string lockHandle)
        {
            return 0;
        }

        #endregion

        public virtual string RegisterCQ(ContinuousQuery query, string clientUniqueId, bool notifyAdd,
            bool notifyUpdate, bool notifyRemove)
        {
            return null;
        }

        public virtual void UnRegisterCQ(string serverUniqueId, string clientUniqueId)
        {
        }

        public virtual bool SetAttributes(string key, CacheItemAttributes attribute)
        {
            return false;
        }


        #region MapReduce Methods

        public virtual void ExecuteMapReduceTask(MapReduceTask task, string taskId, MROutputOption option,
            short callbackId, IKeyFilter keyfilter, string query, Hashtable parameters)
        {
        }

        public virtual void RegisterMapReduceCallbackListener(short callbackId, string taskId)
        {
        }

        public virtual ArrayList GetRunningTasks()
        {
            return null;
        }

        public virtual IDictionaryEnumerator GetTaskEnumerator(string taskId, short callbackId)
        {
            return null;
        }

        public virtual void CancelTask(string taskId)
        {
        }

        public virtual Runtime.MapReduce.TaskStatus GetTaskProgress(string taskId)
        {
            return null;
        }

        public virtual TaskEnumeratorResult NextRecord(string serverAddress, TaskEnumeratorPointer pointer)
        {
            return null;
        }

        public virtual void Dispose(string serverAddress)
        {
        }

        #endregion

        #region Entry Processor Methods

        public virtual Hashtable InvokeEntryProcessor(string[] keys, IEntryProcessor entryProcessor,
            string defaultReadThru, string defaultWriteThru, params object[] arguments)
        {
            return null;
        }

        internal virtual Hashtable InvokeEntryProcessor(string[] keys, IEntryProcessor entryProcessor,
            string readThruProviderName, BitSet dsReadOptionFlag, string writeThruProviderName,
            BitSet dsWriteOptionFlag, params object[] arguments)
        {
            return null;
        }

        #endregion


        internal virtual PollingResult Poll()
        {
            return null;
        }


        public virtual void RegisterCacheClientConnectivityEvent()
        {
        }

        public virtual void UnregisterCacheClientConnectivityEvent()
        {
        }

        public virtual IList<ClientInfo> GetConnectedClientList()
        {
            return null;
        }

        #region	/                 --- Touch ---           /

        internal virtual void Touch(List<string> key)
        {
        }

        #endregion


        #region  ----- Messaging pub/sub------

        internal virtual bool GetOrCreate(string topicName, TopicOperationType type)
        {
            return false;
        }


        internal virtual bool Subscribe(string topicName, string recepientId, SubscriptionType pubSubType)
        {
            return false;
        }

        internal virtual bool UnSubscribe(string topicName, string recepientId, SubscriptionType pubSubType)
        {
            return false;
        }

        internal virtual void PublishMessage(string messageId, object payLoad, long creationTime, long expirationTime,
            Hashtable metadata, BitSet flagMap)
        {
        }

        internal virtual object GetMessageData(BitSet flagMap)
        {
            return null;
        }

        internal virtual bool RemoveTopic(string topicName, bool forcefully)
        {
            return false;
        }

        internal virtual void AcknowledgeMessageReceipt(IDictionary<string, IList<string>> topicWiseMessageIds)
        {
        }

        #endregion
    }
}