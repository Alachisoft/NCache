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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Caching;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Client.Caching;
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Common.Pooling;


namespace Alachisoft.NCache.Client
{

    internal class CacheImplBase
    {
        private ClientInfo _clientInfo;
        private string _clientID;

        private SerializationFormat _serializationFormat;

        internal CacheImplBase()
        {
            _clientInfo = new ClientInfo();
            _clientInfo.ProcessID = AppUtil.CurrentProcess.Id;
            _clientInfo.ClientID = System.Guid.NewGuid().ToString();
            _clientInfo.MachineName = Environment.MachineName;

            //Client version has following format :
            //[2 digits for major version][1 digit for service paack][1 digit for private patch]
            //e.g. 4122 means 4.1 major , 2 for service pack 2 and last 4 for private patch 4
            _clientInfo.ClientVersion = 5000; //changed for 5.0

            _clientID = ClientInfo.GetLegacyClientID(_clientInfo);
        }

        internal ClientInfo LocalClientInfo { get { return _clientInfo; } }

        protected internal virtual bool SerializationEnabled { get { return true; } }

        protected internal virtual TypeInfoMap TypeMap { get { return null; } set { } }

        protected internal virtual EventManager EventManager
        {
            get { return null; }
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

        public virtual long Count { get { return 0; } }

        public string ClientID { get { return _clientID; } }

        public virtual string Name { get { return null; } }
        
        internal virtual PoolManager PoolManager { get; }

        public virtual void Dispose(bool disposing) { }
    
        public virtual void RegisterGeneralNotification(EventTypeInternal eventType, EventDataFilter datafilter, short sequenceNumber) { }
        public virtual void UnRegisterGeneralNotification(EventTypeInternal unregister, short sequenceNumber) { }


        public virtual void RegisterAddEvent() { }
        public virtual void RegisterRemoveEvent() { }
        public virtual void RegisterUpdateEvent() { }
        public virtual void RegisterCustomEvent() { }
        public virtual void RegisterNodeJoinedEvent() { }
        public virtual void RegisterNodeLeftEvent() { }
        public virtual void UnregisterAddEvent() { }
        public virtual void UnregisterRemoveEvent() { }
        public virtual void UnregisterUpdateEvent() { }
        public virtual void UnregisterCustomEvent() { }
        public virtual void UnregisterNodeJoinedEvent() { }
        public virtual void UnregisterNodeLeftEvent() { }
        public virtual void UnregisterHashmapChangedEvent() { }

       
        public virtual void RegisterCacheStoppedEvent() { }
        public virtual void UnregisterCacheStoppedEvent() { }
        public virtual void RegisterClearEvent() { }
        public virtual void UnregisterClearEvent() { }

        internal virtual void MakeTargetCacheActivePassive(bool makeActive) { }

      
        public virtual void Add(string key, object value, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, short onDsItemAddedCallback, bool isResyncExpiredItems,
            Hashtable queryInfo, BitSet flagMap, string providerName, string resyncProviderName, EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, long size, bool encryptionEnabled, string clientId, string typeName)
        {
        }
        
        public virtual IDictionary<string, Exception> Add(string[] keys, CacheItem[] items,
            short onDataSourceItemsAdded, string providerName, long[] sizes, bool encryptionEnabled,
            string clientId, short updateCallbackId, short removeCallbackId,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            return null;
        }
        public virtual void Clear(BitSet flagMap, short onDsClearedCallback, string providerName)
        {

        }


        public virtual void ClearAsync(BitSet flagMap, short onDsClearedCallback, string providerName)
        {

        }

        public virtual void ClearAsync(BitSet flagMap, short onAsyncCacheClearCallback, short onDsClearedCallback, string providerName)
        {

        }
        
        public virtual bool Contains(string key)
        {
            return false;
        }

        public virtual IDictionary<string, bool> ContainsBulk(string[] keys)
        {
            return null;
        }

        public virtual CompressedValueEntry Get<T>(string key, BitSet flagMap, string group, string subGroup, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType)
        {
            return null;
        }

        public virtual void RaiseCustomEvent(object notifId, object data) { }

        public virtual IDictionary Get<T>(string[] keys, BitSet flagMap)
        {
            return null;
        }


        public virtual object GetCacheItem(string key, BitSet flagMap, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType)
        {
            return null;
        }

        public virtual IDictionary GetCacheItemBulk(string[] keys, BitSet flagMap)
        {
            return null;
        }


        public virtual void Insert(string key, object value,  DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, short onDsItemUpdatedCallback, bool isResyncExpiredItems,
             Hashtable queryInfo, BitSet flagMap, object lockId, LockAccessType accessType, string providerName,
            string resyncProviderName, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, long size, bool encryptionEnabled, string clientId, string typeName, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
        }


        public virtual IDictionary<string, Exception> Insert(string[] keys,
            CacheItem[] items, short onDsItemsUpdatedCallback, string providerName,
            long[] sizes, bool encryptionEnabled, string clientId,
            short updateCallbackId, short removeCallbackId,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            return null;
        }

        public virtual CompressedValueEntry Remove<T>(string key, BitSet flagMap, short dsItemRemovedCallbackId, object lockId, LockAccessType accessType, string ProviderName)
        {
            return null;
        }

        public virtual void Delete(string key, BitSet flagMap, short dsItemRemovedCallbackId, object lockId, LockAccessType accessType)
        {
        }


        public virtual IDictionary Remove<T>(string[] keys, BitSet flagMap, string providerName, short onDsItemsRemovedCallback)
        {
            return null;
        }

        public virtual void Delete(string[] keys, BitSet flagMap, string providerName, short onDsItemsRemovedCallback)
        {

        }

        //Delete that can be use to Delete nay item in cache by providing only key
        public virtual void Delete(string key)
        {
          
            LockHandle lockHandle=null;
           
            LockAccessType accessType= LockAccessType.IGNORE_LOCK;
            object lockId = (lockHandle == null) ? null : lockHandle.LockId; 
            BitSet flagMap = new BitSet();            
            short dsItemRemovedCallbackId = -1;

            this.Delete(key, flagMap, dsItemRemovedCallbackId, lockId, accessType);

        }

        public virtual void Remove(string group, string subGroup)
        {

        }

       
       

        public virtual object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag, CacheImplBase cacheImpl, ref long size, UserObjectType userObjectType,bool isCustomAttributeBaseSerialzed=false)
        {
            return null;
        }

        public virtual T SafeDeserialize<T>(object serializedObject, string serializationContext, BitSet flag, CacheImplBase cacheImpl, UserObjectType userObjectType)
        {
            return default(T);
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

        public virtual Hashtable GetCompactTypes()
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

        public virtual bool CheckCSecurityAuthorization(string cacheId, byte[] password, string userId)
        {
            return false;
        }

        internal virtual bool IsLocked(string key, ref LockHandle lockHandle)
        {
            return false;
        }

        public virtual void RegisterKeyNotificationCallback(string key, short updateCallbackid, short removeCallbackid, bool notifyOnItemExpiration) { }
        public virtual void UnRegisterKeyNotificationCallback(string key, short updateCallbackid, short removeCallbackid) { }

        public virtual void RegisterKeyNotificationCallback(string key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration, CallbackType callbackType = CallbackType.PushBasedNotification) { }

        public virtual void RegisterKeyNotificationCallback(string key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration) { }

        public virtual void UnRegisterKeyNotificationCallback(string key, short update, short remove, EventTypeInternal eventType) { }

        public virtual void RegisterKeyNotificationCallback(string[] keys, short updateCallbackid, short removeCallbackid, string clientId, CallbackType callbackType = CallbackType.PullBasedCallback) { }
        public virtual void UnRegisterKeyNotificationCallback(string[] keys, short updateCallbackid, short removeCallbackid) { }

        public virtual void RegisterKeyNotificationCallback(string[] key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration) { }
        public virtual void UnRegisterKeyNotificationCallback(string[] key, short update, short remove, EventTypeInternal eventType) { }

        public virtual void RegisterKeyNotificationCallback(string[] key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration, CallbackType callbackType = CallbackType.PushBasedNotification) { }

        public virtual void RegisterPollingNotification(short pollingCallbackId) { }

        
        public virtual bool SetAttributes(string key, CacheItemAttributes attribute)
        {
            return false;
        }
      
        public virtual void Dispose(string serverAddress)
        { }

        internal virtual PollingResult Poll()
        {
            return null;
        }


        public virtual void RegisterCacheClientConnectivityEvent() { }
        public virtual void UnregisterCacheClientConnectivityEvent() { }

        public virtual IList<ClientInfo> GetConnectedClientList()
        {
            return null;
        }

     
        #region	/                 --- Touch ---           /
        internal virtual void Touch(List<string> key) { }
        #endregion

        #region  ----- Messaging pub/sub------

        internal virtual long GetMessageCount(string topicName)
        {
            return 0;
        }

        internal virtual bool GetOrCreate(string topicName, TopicOperationType type)
        {
            return false;
        }


        internal virtual bool Subscribe(string topicName, string subscriptionName, SubscriptionType pubSubType, long creationTime, long expiration, SubscriptionPolicyType subscriptionPolicy = SubscriptionPolicyType.NonDurableExclusiveSubscription)
        {
            return false;
        }

        internal virtual bool UnSubscribe(string topicName, string recepientId, SubscriptionPolicyType subscriptionPolicy, SubscriptionType pubSubType,bool dispose=false)
        {
            return false;
        }

        internal virtual void PublishMessage(string messageId, object payLoad, long creationTime, long expirationTime, Hashtable metadata, BitSet flagMap)
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

