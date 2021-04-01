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
using System.Linq;
using System.Collections;
using System.Reflection;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Common.DataSource;
using System.Threading.Tasks;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Management.Statistics;
using Alachisoft.NCache.Client.Services;
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.ErrorHandling;
#if SERVER
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
#endif

namespace Alachisoft.NCache.Client
{
    internal partial class Cache : ICache, IEnumerable, IDisposable
    {
        #region Fields 
        private const int _compressed = BitSetConstants.Compressed;
        private int _refCount;
        private int _refCacheStoppedCount = 0;
        private int _refClearCount = 0;
        private int _refCustomCount = 0;
        private bool _encryptionEnabled = false;
        internal string _cacheId;
        private string _serializationContext;
        private string _defaultReadThruProvider;
        private string _defaultWriteThruProvider;
        private string _cacheAlias;
      
        private CacheImplBase _cacheImpl;
        private CacheConfig _config;
        private ResourcePool _callbackIDsMap = new ResourcePool();
        private ResourcePool _callbacksMap = new ResourcePool();
        
        internal event CacheStoppedCallback _cacheStopped;
        private CacheAsyncEventsListener _asyncListener;
        private CacheEventsListener _listener;
        private EventManager _eventManager;
        internal MessagingService _messagingService;
        internal StatisticsCounter _perfStatsCollector;
        private ClusterEventsListener _clusterListener;
        private ResourcePool _asyncCallbackIDsMap = new ResourcePool();
        private ResourcePool _asyncCallbacksMap = new ResourcePool();

        private short _dsiacbInitialVal = 6000;
        private short _dsiucbInitialVal = 7000;
        private short _dsircbInitialVal = 8000;

        private GeneralDataNotificationWrapper _notificationWrapper;
        internal event CustomEventCallback _customEvent;
        internal event MemberJoinedCallback _memberJoined;
        internal event MemberLeftCallback _memberLeft;
        private ArrayList _secondaryInprocInstances;
        private Hashtable _apiLogHastable = Hashtable.Synchronized(new Hashtable());
        internal static readonly DateTime NoAbsoluteExpiration = DateTime.MaxValue.ToUniversalTime();
        internal static readonly TimeSpan NoSlidingExpiration = TimeSpan.Zero;
        internal static readonly TimeSpan NoLockExpiration = TimeSpan.Zero;
        internal static readonly DateTime DefaultAbsolute = DateTime.MinValue.AddYears(1);
        internal static readonly DateTime DefaultAbsoluteLonger = DateTime.MinValue.AddYears(2);
        internal static readonly TimeSpan DefaultSliding = TimeSpan.MinValue.Add(new TimeSpan(0, 0, 1));
        internal static readonly TimeSpan DefaultSlidingLonger = TimeSpan.MinValue.Add(new TimeSpan(0, 0, 2));

        [ThreadStatic]
        private static bool? _serializationEnabled = true;
        private TypeInfoMap _queryTypeMap;
        private bool _expectionEnable = true;

        
        private SerializationFormat _serializationFormat;
 
        #endregion

        #region Properties 

        public virtual IList<ClientInfo> GetConnectedClientList()
        {
            return CacheImpl.GetConnectedClientList();
        }

        public virtual ClientInfo ClientInfo { get { return CacheImpl.LocalClientInfo; } }

        public IList<ClientInfo> ConnectedClientList
        {
            get
            {
                return CacheImpl.GetConnectedClientList();
            }
        }

        public virtual IMessagingService MessagingService { get { return _messagingService; } }

       
        
        internal virtual string SerializationContext
        {
            get { return _serializationContext; }
            set { _serializationContext = value; }
        }

        internal virtual EventManager EventManager
        {
            get { return _eventManager; }
        }

        internal string CacheAlias
        {
            get { return _cacheAlias; }
            set { _cacheAlias = value; }
        }

        internal Hashtable APILogHashTable
        {
            get { return _apiLogHastable; }
        }

        internal bool InternalSerializationEnabled
        {
            get
            {
                if (_serializationEnabled.HasValue)
                    return _serializationEnabled.Value;

                return true;
            }
            set
            {
                _serializationEnabled = value;
            }
        }

        internal virtual SerializationFormat SerializationFormat
        {
            get { return _serializationFormat; }
            set { _serializationFormat = value; }
        }

        

        internal virtual event CustomEventCallback CustomEvent
        {
            add
            {
                _customEvent += value;
                if (CacheImpl != null && ++_refCustomCount == 1) CacheImpl.RegisterCustomEvent();
            }
            remove
            {
                int beforeLength, afterLength = 0;
                lock (this)
                {
                    if (_customEvent != null)
                    {
                        beforeLength = _customEvent.GetInvocationList().Length;
                        _customEvent -= value;

                        if (_customEvent != null)
                            afterLength = _customEvent.GetInvocationList().Length;

                        if (beforeLength - afterLength == 1)
                            if (CacheImpl != null && --_refCustomCount == 0) CacheImpl.UnregisterCustomEvent();
                    }
                }
            }
        }


        internal virtual CacheAsyncEventsListener AsyncListener
        {
            get { return _asyncListener; }
        }

        internal virtual CacheEventsListener EventListener
        {
            get { return _listener; }
        }

        internal virtual ResourcePool CallbackIDsMap
        {
            get { return _callbackIDsMap; }
        }

        internal virtual ResourcePool CallbacksMap
        {
            get { return _callbacksMap; }
        }

        internal virtual string CacheId
        {
            get { return _cacheId; }
        }
        
        internal virtual CacheImplBase CacheImpl
        {
            get { return _cacheImpl; }
            set
            {
                _cacheImpl = value;
                if (_cacheImpl != null)
                {
                    _cacheId = _serializationContext = _cacheImpl.Name;
                }
            }
        }

        internal virtual bool ExceptionsEnabled { get { return _expectionEnable; } set { _expectionEnable = value; } }

        #endregion

        #region Constructor 

        internal Cache()
        {
            _notificationWrapper = new GeneralDataNotificationWrapper(this);
            _eventManager = new EventManager(_cacheId, null, this);
            _listener = new CacheEventsListener(this, _eventManager);
        }

        internal Cache(CacheImplBase objectCache, CacheConfig config)
        {
            _notificationWrapper = new GeneralDataNotificationWrapper(this);

            CacheImpl = objectCache;
            _config = config;
            _cacheId = config.CacheId;

            if (CacheImpl != null)
            {
                _serializationContext = CacheImpl.Name; //Sets the serialization context.
                _cacheId = CacheImpl.Name;
            }

            _eventManager = new EventManager(_cacheId, null, this);
            _listener = new CacheEventsListener(this, _eventManager);
            _asyncListener = new CacheAsyncEventsListener(this);

            _clusterListener = new ClusterEventsListener(this);

            AddRef();

            _messagingService = new MessagingService(_eventManager, null, this);
           
        }

        internal Cache(CacheImplBase objectCache, string cacheId, StatisticsCounter perfStatsCollector)
        {
            _notificationWrapper = new GeneralDataNotificationWrapper(this);

            CacheImpl = objectCache;
            _cacheId = cacheId;
            if (CacheImpl != null)
            {
                _serializationContext = CacheImpl.Name; //Sets the serialization context.
            }

            _eventManager = new EventManager(_cacheId, null, this);
            _listener = new CacheEventsListener(this, _eventManager);
            _asyncListener = new CacheAsyncEventsListener(this);

            _perfStatsCollector = perfStatsCollector;

            _clusterListener = new ClusterEventsListener(this);

            AddRef();

            _messagingService = new MessagingService(_eventManager, perfStatsCollector, this);
           
        }

        #endregion

        #region	Count

        public virtual long Count
        {
            get
            {
                try
                {
                    if (CacheImpl != null) return CacheImpl.Count;
                }
                catch (Exception)
                {
                    if (ExceptionsEnabled) throw;
                }
                finally
                {
                }
                return 0;
            }
        }



        #endregion

        #region	Clear Operations


  
        public virtual void Clear()
        {
            ClearInternal();
        }

        private void ClearInternal()
        {
            if (CacheImpl == null) return;
            try
            {
                string providerName = null;
                BitSet flagMap = new BitSet();
                short dsClearedCallbackId = -1;

                
                CacheImpl.Clear(flagMap, dsClearedCallbackId, providerName);

                if (_callbackIDsMap != null)
                    _callbackIDsMap.RemoveAllResources();

                if (_callbacksMap != null)
                    _callbacksMap.RemoveAllResources();
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        #endregion

        #region	Contain Operations

        public virtual bool Contains(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");
            if (CacheImpl == null) return false;
            try
            {
                return CacheImpl.Contains(key);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
            }
            return false;
        }

        public virtual IDictionary<string, bool> ContainsBulk(IEnumerable<string> keys)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));
            if (keys == null) throw new ArgumentNullException("keys");

            string[] keysList = keys.ToArray();
            if (keysList.Length == 0) throw new ArgumentException("There is no key present in keys array");

            RemoveDuplicateKeys(ref keysList);

            try
            {
                return CacheImpl.ContainsBulk(keysList);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }

            return null;
        }



        #endregion

        #region	Add Operations

        public virtual void Add(string key, object value)
        {
            try
            {
                Add(key, new CacheItem(value));
            }
            catch (Exception) { if (ExceptionsEnabled) throw; }

        }

        public virtual void Add(string key, CacheItem item)
        {
            try
            {
                long size = 0;

                if (item == null)
                    throw new ArgumentNullException("CacheItem");
               
                string providerName = null;
              
                EventTypeInternal eventType=EventTypeInternal.None; ;

               

                AddOperation(key, item.GetValue<object>(),
                                    item.AbsoluteExpiration, item.SlidingExpiration,
                                    item.Priority,
                                    item.ItemRemoveCallback, item.ItemUpdateCallback,
                                     eventType,
                                    false,
                                    providerName, null,
                                    item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback,
                                    item.ItemUpdatedDataFilter, item.ItemRemovedDataFilter,
                                    ref size, true, null, -1, -1, -1);

            }
            catch (Exception) { if (ExceptionsEnabled) throw; }

        }

        public virtual IDictionary<string, Exception> AddBulk(IDictionary<string, CacheItem> items)
        {
            try
            {
              
                string providerName = null;
                long[] sizes = new long[items.Count];
                EventTypeInternal eventType= EventTypeInternal.None;
                IDictionary<string, Exception> keyValuePairs;

          

                return AddBulkOperation(items,  eventType, providerName, ref sizes, true, null, -1, -1, -1,
                    EventDataFilter.None, EventDataFilter.None, false, CallbackType.PushBasedNotification) as IDictionary<string, Exception>;
            }
            finally
            {
            }
        }






        internal virtual void AddOperation(string key, object value,
            DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, CacheItemRemovedCallback onRemoveCallback,
            CacheItemUpdatedCallback onUpdateCallback, EventTypeInternal eventType,
            bool isResyncExpiredItems, string providerName,
            string resyncProviderName, 
            CacheDataNotificationCallback cacheItemUdpatedCallback,
            CacheDataNotificationCallback cacheItemRemovedCallaback, EventDataFilter itemUpdateDataFilter,
            EventDataFilter itemRemovedDataFilter, ref long size, bool allowQueryTags, string clientId,
            short updateCallbackID, short removeCallbackID, short dsItemAddedCallbackID)

        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            
            string typeName = null;
            Hashtable queryInfo = null;
            BitSet flagMap = new BitSet();

            if (value is ClientCacheItem)
            {
                ClientCacheItem clientcacheItem = value as ClientCacheItem;
                value = clientcacheItem.Value;
                queryInfo = clientcacheItem.QueryInfo;

                if (clientcacheItem.Flags.IsAnyBitSet(BitSetConstants.BinaryData))
                    flagMap.SetBit(BitSetConstants.BinaryData);

                else if (clientcacheItem.Flags.IsAnyBitSet(BitSetConstants.JsonData))
                    flagMap.SetBit(BitSetConstants.JsonData);
                typeName = clientcacheItem.GroupDataType;
            }

            ValidateKeyValue(key, value);

            
            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();
            if (!string.IsNullOrEmpty(resyncProviderName))
                resyncProviderName = resyncProviderName.ToLower();

            UsageStats stats = new UsageStats();
            stats.BeginSample();

            if (queryInfo == null)
            {
                queryInfo = new Hashtable();
                if (allowQueryTags)
                {
                    queryInfo["query-info"] = GetQueryInfo(value);
                }

            }

            
            try
            {
                value = SafeSerialize(value, _serializationContext, ref flagMap, ref size, UserObjectType.CacheItem);

                if (_perfStatsCollector != null && value != null && value is byte[])
                    _perfStatsCollector.IncrementAvgItemSize(((byte[])value).Length);
                
                if (removeCallbackID == -1)
                {
                    if (cacheItemRemovedCallaback != null)
                    {
                        short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemRemovedCallaback, EventTypeInternal.ItemRemoved, itemRemovedDataFilter);
                        removeCallbackID = callabackIds[1];
                    }
                    else if (onRemoveCallback != null && cacheItemRemovedCallaback == null)
                    {
                        removeCallbackID = GetCallbackId(onRemoveCallback);
                        //old notification expects data
                        itemRemovedDataFilter = EventDataFilter.None;
                    }
                }

                if (updateCallbackID == -1)
                {
                    if (cacheItemUdpatedCallback != null)
                    {
                        short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemUdpatedCallback, EventTypeInternal.ItemUpdated, itemUpdateDataFilter);
                        updateCallbackID = callabackIds[0];
                    }
                    else if (onUpdateCallback != null)
                    {
                        updateCallbackID = GetCallbackId(onUpdateCallback);
                        itemUpdateDataFilter = EventDataFilter.None;
                    }
                }

              
                if (absoluteExpiration != null && absoluteExpiration != Cache.NoAbsoluteExpiration && slidingExpiration != null && slidingExpiration != Cache.NoSlidingExpiration)
                    throw new ArgumentException("You can not set both sliding and absolute expirations on a single item");
                if (absoluteExpiration != null && absoluteExpiration != Cache.NoAbsoluteExpiration)
                    absoluteExpiration = absoluteExpiration.ToUniversalTime();

                CacheImpl.Add(key, value,
                                  absoluteExpiration,
                                    slidingExpiration, priority, removeCallbackID,
                                    updateCallbackID, dsItemAddedCallbackID,
                                    isResyncExpiredItems,
                                    queryInfo, flagMap, providerName,
                                    resyncProviderName, itemUpdateDataFilter,
                                    itemRemovedDataFilter, size, false, clientId, typeName);


                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerAddSample(stats.Current);
                    _perfStatsCollector.IncrementAddPerSecStats();
                }


            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        internal virtual IDictionary<string, Exception> AddBulkOperation(IDictionary<string, CacheItem> items,
            EventTypeInternal eventType,
            string providerName, ref long[] sizes, bool allowQueryTags, string clientId,
            short updateCallbackId, short removeCallbackId, short dsItemAddedCallbackID,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter,
            bool returnVersions,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {

            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (items == null) throw new ArgumentNullException("items");
            if (items.Count == 0) throw new ArgumentException("Adding empty dictionary into cache.");


            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();

            CacheItem[] clonedItems = new CacheItem[items.Count];
            string[] keys = new string[items.Count];

            string[] dataTypes = new string[items.Count];

            UsageStats stats = new UsageStats();
            stats.BeginSample();

            int count = 0;
            foreach (var pair in items)
            {
                if (pair.Value == null) throw new ArgumentException("CacheItem cannot be null");

               
                CacheItem cloned = pair.Value.Clone() as CacheItem;
                string key = pair.Key;

                if (cloned == null) throw new ArgumentNullException("items[" + count + "]");

               

                if (cloned.AbsoluteExpiration != null && cloned.AbsoluteExpiration != NoAbsoluteExpiration && cloned.SlidingExpiration != null && cloned.SlidingExpiration != NoSlidingExpiration)
                    throw new ArgumentException("You can not set both sliding and absolute expirations on a single item");

                BitSet flagMap = new BitSet();

                

                long size = 0;
                if (sizes[count] > 0)
                    size = sizes[count];

                Hashtable queryInfo = null;

                if (cloned.GetValue<object>() is ClientCacheItem)
                {
                    ClientCacheItem clientcacheItem = cloned.GetValue<object>() as ClientCacheItem;
                    cloned.SetValue(clientcacheItem.Value);
                    queryInfo = clientcacheItem.QueryInfo;

                    if (clientcacheItem.Flags.IsAnyBitSet(BitSetConstants.BinaryData))
                        flagMap.SetBit(BitSetConstants.BinaryData);

                    else if (clientcacheItem.Flags.IsAnyBitSet(BitSetConstants.JsonData))
                        flagMap.SetBit(BitSetConstants.JsonData);
                    cloned.TypeName = clientcacheItem.GroupDataType;

                }

                ValidateKeyValue(key, cloned.GetValue<object>());

                if (queryInfo == null)
                {
                    queryInfo = new Hashtable();
                    if (allowQueryTags)
                    {
                        queryInfo["query-info"] = GetQueryInfo(cloned.GetValue<object>());
                       
                    }
                }

                cloned.QueryInfo = queryInfo;

                cloned.SetValue(SafeSerialize(cloned.GetValue<object>(), _serializationContext, ref flagMap, ref size, UserObjectType.CacheItem));
                sizes[count] = size;


                if (_perfStatsCollector != null)
                    if (cloned.GetValue<object>() != null && cloned.GetValue<object>() is byte[]) _perfStatsCollector.IncrementAvgItemSize(((byte[])cloned.GetValue<object>()).Length);

                cloned.FlagMap = flagMap;

                if (cloned.AbsoluteExpiration != null && cloned.AbsoluteExpiration != NoAbsoluteExpiration)
                    cloned.Expiration = new Expiration(ExpirationType.Absolute) { ExpireAfter = cloned.AbsoluteExpiration.ToUniversalTime() - DateTime.Now.ToUniversalTime() };

                clonedItems[count] = cloned;
                keys[count] = key;
                count++;
            }


        
            try
            {
                IDictionary<string, Exception> result = CacheImpl.Add(keys, clonedItems, dsItemAddedCallbackID, providerName, sizes, false, clientId, updateCallbackId, removeCallbackId,
                    updateCallbackFilter, removeCallabackFilter, callbackType);
                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerAddBulkSample(stats.Current);
                    _perfStatsCollector.IncrementByAddPerSecStats(keys.Length);
                }


                return result;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            return null;
        }

        #endregion

        #region Get Operations

        public virtual T Get<T>(string key)
        {
            LockAccessType accessType = LockAccessType.IGNORE_LOCK;
            LockHandle lockHandle = null;
           

            return GetInternal<T>(key, null, null, accessType, NoLockExpiration, ref lockHandle);
        }

        public virtual T Get<T>(string key, bool acquireLock, TimeSpan lockTimeout, ref LockHandle lockHandle)
        {
            LockAccessType accessType = acquireLock ? LockAccessType.ACQUIRE : LockAccessType.DONT_ACQUIRE;
           
            return GetInternal<T>(key, null, null, accessType, lockTimeout, ref lockHandle);
        }

        internal virtual T GetInternal<T>(string key, string group, string subGroup, LockAccessType accessType, TimeSpan lockTimeout, ref LockHandle lockHandle)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");


        
            CompressedValueEntry result = null;
            try
            {
                BitSet flagMap = new BitSet();
                UsageStats stats = new UsageStats();

                stats.BeginSample();

                result = CacheImpl.Get<T>(key, flagMap, group, subGroup, ref lockHandle, lockTimeout, accessType);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerGetSample(stats.Current);
                    _perfStatsCollector.IncrementGetPerSecStats();
                }

                if (result != null && result.Value != null && result.Type == EntryType.CacheItem)
                {
                    if (result.Flag.IsBitSet(_compressed))
                        result.Value = Decompress((byte[])result.Value);

                    if (_perfStatsCollector != null && result.Value != null && result.Value is byte[])
                        _perfStatsCollector.IncrementAvgItemSize(((byte[])result.Value).Length);

                    result.Value = SafeDeserialize<T>(result.Value, _serializationContext, result.Flag, UserObjectType.CacheItem);

                    return (T)result.Value;
                }

                return CacheHelper.GetSafeValue<T>(CacheHelper.GetObjectOrInitializedCollection<T>(key, result.Type, result.Value, this));
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                else return default(T);
            }
        }

        public virtual IDictionary<string, T> GetBulk<T>(IEnumerable<string> keys)
        {
            BitSet flagMap = new BitSet();
            UsageStats stats = new UsageStats();

            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (keys == null) throw new ArgumentNullException("keys");

            FindNull(keys);

            string[] keysList = keys.ToArray();

            if (keysList.Length == 0) throw new ArgumentException("There is no key present in keys array");

            RemoveDuplicateKeys(ref keysList);

           
            try
            {
                stats.BeginSample();

                var getResult = new Dictionary<string, T>();
                IDictionary table = CacheImpl.Get<T>(keysList, flagMap);

                if (table != null)
                {
                    object[] keyArr = new object[table.Count];

                    table.Keys.CopyTo(keyArr, 0);

                    IEnumerator ie = keyArr.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        CompressedValueEntry result = table[ie.Current] as CompressedValueEntry;
                        if (result != null && result.Type == EntryType.CacheItem)
                        {
                            if (result.Flag.IsBitSet(_compressed))
                                result.Value = Decompress((byte[])result.Value);

                            if (_perfStatsCollector != null && result.Value != null && result.Value is byte[])
                                _perfStatsCollector.IncrementAvgItemSize(((byte[])result.Value).Length);

                            getResult[ie.Current.ToString()] = SafeDeserialize<T>(result.Value, _serializationContext, result.Flag, UserObjectType.CacheItem);
                        }
                    }
                }

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerGetSample(stats.Current);
                    _perfStatsCollector.IncrementByGetPerSecStats(keysList.Length);
                }

                return getResult;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
            }
            return null;
        }

        public virtual CacheItem GetCacheItem(string key)
        {
            LockAccessType accessType = LockAccessType.IGNORE_LOCK;
            LockHandle lockHandle = null;
          
            return GetCacheItemInternal(key, accessType, NoLockExpiration, ref lockHandle);
        }

        public virtual CacheItem GetCacheItem(string key, bool acquireLock, TimeSpan lockTimeout, ref LockHandle lockHandle)
        {
            LockAccessType accessType = acquireLock ? LockAccessType.ACQUIRE : LockAccessType.DONT_ACQUIRE;
           

            return GetCacheItemInternal(key, accessType, lockTimeout, ref lockHandle);
        }

        internal CacheItem GetCacheItemFromEntry(string key, CacheEntry entry)
        {
            CacheItem item = new CacheItem();

            if (entry != null)
            {
                item.EntryType = entry.Type;
                item.FlagMap = entry.Flag;

                if (entry.Notifications != null)
                {
                    Notifications cb = entry.Notifications;
                    if (cb.ItemRemoveCallbackListener != null && cb.ItemRemoveCallbackListener.Count > 0)
                    {
                        foreach (CallbackInfo cbInfo in cb.ItemRemoveCallbackListener)
                        {
                            if (cbInfo.Client == CacheImpl.ClientID)
                            {
                                item.ItemRemoveCallback = (CacheItemRemovedCallback)_callbackIDsMap.GetResource(cbInfo.Callback);
                                break;
                            }
                        }
                    }
                    if (cb.ItemUpdateCallbackListener != null && cb.ItemUpdateCallbackListener.Count > 0)
                    {
                        foreach (CallbackInfo cbInfo in cb.ItemUpdateCallbackListener)
                        {
                            if (cbInfo.Client == CacheImpl.ClientID)
                            {
                                item.ItemUpdateCallback = (CacheItemUpdatedCallback)_callbackIDsMap.GetResource(cbInfo.Callback);
                                break;
                            }
                        }
                    }
                    item.SetValue(entry.Value);

                    if (item.FlagMap.IsBitSet(_compressed))
                        item.SetValue(Decompress((byte[])entry.Value));

                    if (_perfStatsCollector != null && item.GetValue<object>() != null && item.GetValue<object>() is byte[])
                        _perfStatsCollector.IncrementAvgItemSize(((byte[])item.GetValue<object>()).Length);
                }
                else
                {
                    item.SetValue(entry.Value);

                    if (item.FlagMap.IsBitSet(_compressed))
                    {
                        item.SetValue(Decompress((byte[])entry.Value));
                    }

                    if (_perfStatsCollector != null && item.GetValue<object>() != null && item.GetValue<object>() is byte[])
                        _perfStatsCollector.IncrementAvgItemSize(((byte[])item.GetValue<object>()).Length);

                    
                }

                if (entry != null)
                    item.Priority = entry.Priority;

                ExpirationHint hint = entry.ExpirationHint;

                DateTime absoluteExpiration = DateTime.MaxValue.ToUniversalTime();
                TimeSpan slidingExpiration = TimeSpan.Zero;

                if (hint != null)
                {
                    if (hint is FixedExpiration)
                    {
                        absoluteExpiration = ((FixedExpiration)hint).AbsoluteTime;
                    }
                    else if (hint is IdleExpiration)
                    {
                        slidingExpiration = ((IdleExpiration)hint).SlidingTime;
                    }
                }

                item.Expiration = ExpirationUtil.GetExpiration(absoluteExpiration, slidingExpiration);

                item.CreationTime = entry.CreationTime;
                item.LastModifiedTime = entry.LastModifiedTime;
                

                item.CacheInstance = GetCacheInstance();  // NOTE : This property SHOULD ALWAYS be set just before returning the cache item
                item.SetValue(CacheHelper.GetObjectOrDataTypeForCacheItem(key, entry.Type, item.GetValue<object>()));
                return item;
            }
            return null;
        }

        internal CacheItem GetCacheItemFromBinaryCacheItem(string key, CacheItem binaryCacheItem)
        {
            if (binaryCacheItem == null)
                return null;

            if (binaryCacheItem.FlagMap.IsBitSet(_compressed))
                binaryCacheItem.SetValue(Decompress((byte[])binaryCacheItem.GetValue<object>()));

            if (_perfStatsCollector != null && binaryCacheItem.GetValue<object>() != null && binaryCacheItem.GetValue<object>() is byte[])
                _perfStatsCollector.IncrementAvgItemSize(((byte[])binaryCacheItem.GetValue<object>()).Length);

            if (InternalSerializationEnabled)
                binaryCacheItem.Size = ((byte[])binaryCacheItem.GetValue<object>()).Length;

            binaryCacheItem.Expiration = ExpirationUtil.GetExpiration(binaryCacheItem.AbsoluteExpiration, binaryCacheItem.SlidingExpiration);
            binaryCacheItem.CacheInstance = this;  // NOTE : This property SHOULD ALWAYS be set just before returning the cache item
            binaryCacheItem.SetValue(CacheHelper.GetObjectOrDataTypeForCacheItem(key, binaryCacheItem.EntryType, binaryCacheItem.GetValue<object>()));

            return binaryCacheItem;
        }

        public virtual IDictionary<string, CacheItem> GetCacheItemBulk(IEnumerable<string> keys)
        {
            CacheEntry entry = null;
            BitSet flagMap = new BitSet();
            CacheItem item = new CacheItem();
            UsageStats stats = new UsageStats();

            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (keys == null) throw new ArgumentNullException("keys");

            string[] keysList = keys.ToArray();

            if (keysList.Length == 0) throw new ArgumentException("There is no key present in keys array");

            RemoveDuplicateKeys(ref keysList);
      
            try
            {
                stats.BeginSample();

                var getResult = new Dictionary<string, CacheItem>();

                IDictionary table = CacheImpl.GetCacheItemBulk(keysList, flagMap);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerGetSample(stats.Current);
                    _perfStatsCollector.IncrementGetPerSecStats();
                }

                if (table != null)
                {
                    IDictionaryEnumerator ide = table.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        var binaryCacheItem = ide.Value as CacheItem;

                        if (binaryCacheItem != null)
                        {
                            getResult[(string)ide.Key] = GetCacheItemFromBinaryCacheItem((string)ide.Key, binaryCacheItem);
                            continue;
                        }

                        getResult[(string)ide.Key] = GetCacheItemFromEntry((string)ide.Key, ide.Value as CacheEntry);

                    }
                }

                return getResult;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
            }
            return null;
        }

        internal virtual CacheItem GetCacheItemInternal(string key, LockAccessType accessType, TimeSpan lockTimeout, ref LockHandle lockHandle)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (key == null) throw new ArgumentNullException("keys");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");

            try
            {
                CacheEntry entry = null;
                BitSet flagMap = new BitSet();
                CacheItem item = new CacheItem();
                UsageStats stats = new UsageStats();

         

                stats.BeginSample();

                object value = CacheImpl.GetCacheItem(key, flagMap,ref lockHandle, lockTimeout, accessType);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerGetSample(stats.Current);
                    _perfStatsCollector.IncrementGetPerSecStats();
                }

                if (value == null) return null;

                if (value is CacheItem)
                    return GetCacheItemFromBinaryCacheItem(key, value as CacheItem);


                return GetCacheItemFromEntry(key, (CacheEntry)value);

            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            return null;
        }

        #endregion

        #region	Insert Operations

        public virtual bool UpdateAttributes(string key, CacheItemAttributes attributes)
        {
            if (key == null) throw new ArgumentNullException("key");

            if (attributes == null) throw new ArgumentNullException("attribute");

            return CacheImpl.SetAttributes(key, attributes);
        }

        public virtual void Insert(string key, object value)
        {
            Insert(key, new CacheItem(value));
        }

        public virtual void Insert(string key, CacheItem item, LockHandle lockHandle = null, bool releaseLock = false)
        {

            if (item == null)
                throw new ArgumentNullException("CacheItem");
            long size = 0;
           
          
            string providerName = null;
          
            EventTypeInternal eventType=EventTypeInternal.None; ;

           
            CacheItemVersion itemVersion = null;
            LockAccessType accessType = releaseLock ? LockAccessType.RELEASE : LockAccessType.DONT_RELEASE;

            if (lockHandle != null)
            {
                accessType = releaseLock ? LockAccessType.RELEASE : LockAccessType.DONT_RELEASE;
            }

            InsertOperation(key, item.GetValue<object>(), item.AbsoluteExpiration, item.SlidingExpiration,
                   item.Priority, item.ItemRemoveCallback, item.ItemUpdateCallback, eventType, false,
                   lockHandle, accessType, providerName,null,
                   item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback, item.ItemUpdatedDataFilter, item.ItemRemovedDataFilter,
                   ref size, true, null, -1, -1, -1);
        }

        public virtual IDictionary<string, Exception> InsertBulk(IDictionary<string, CacheItem> items)
        {
            try
            {
               
                string providerName = null;
                long[] sizes = new long[items.Count];
              
                EventTypeInternal eventType= EventTypeInternal.None; ;


                return InsertBulkOperation(items, eventType, providerName, ref sizes, true, null, -1, -1, -1, EventDataFilter.None, EventDataFilter.None, false, 
                    CallbackType.PushBasedNotification);
            }
            finally
            {
            }
        }





        internal virtual IDictionary<string, Exception> InsertBulkOperation(IDictionary<string, CacheItem> items,   EventTypeInternal eventType,

            string providerName, ref long[] sizes, bool allowQueryTags, string clientId, short updateCallbackId, short removeCallbackId, short dsItemUpdatedCallbackID,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, bool returnVersions, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (items == null) throw new ArgumentNullException("items");
            if (items.Count == 0) throw new ArgumentException("Adding empty dictionary into cache.");


           

            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();


            CacheItem[] clonedItems = new CacheItem[items.Count];
            string[] keys = new string[items.Count];

            UsageStats stats = new UsageStats();
            stats.BeginSample();

            int count = 0;

            foreach (var pair in items)
            {
                if (pair.Value == null) throw new Exception("CacheItem cannot be null");

                
                CacheItem cloned = pair.Value.Clone() as CacheItem;
                string key = pair.Key;
                BitSet flagMap = new BitSet();

                long size = 0;
                if (sizes[count] > 0) size = sizes[count];

                Hashtable queryInfo = null;

                if (cloned.GetValue<object>() is ClientCacheItem)
                {
                    ClientCacheItem clientcacheItem = cloned.GetValue<object>() as ClientCacheItem;
                    cloned.SetValue(clientcacheItem.Value);
                    queryInfo = clientcacheItem.QueryInfo;

                    if (clientcacheItem.Flags.IsAnyBitSet(BitSetConstants.BinaryData))
                        flagMap.SetBit(BitSetConstants.BinaryData);

                    else if (clientcacheItem.Flags.IsAnyBitSet(BitSetConstants.JsonData))
                        flagMap.SetBit(BitSetConstants.JsonData);
                    cloned.TypeName = clientcacheItem.GroupDataType;

                }

                ValidateKeyValue(key, cloned.GetValue<object>());

                if (queryInfo == null)
                {
                    queryInfo = new Hashtable();
                    if (allowQueryTags)
                    {
                        queryInfo["query-info"] = GetQueryInfo(cloned.GetValue<object>());
                    }
                }

                cloned.QueryInfo = queryInfo;

                cloned.SetValue(SafeSerialize(cloned.GetValue<object>(), _serializationContext, ref flagMap, ref size, UserObjectType.CacheItem));
                sizes[count] = size;

                if (_perfStatsCollector != null)
                {
                    if (cloned.GetValue<object>() != null) _perfStatsCollector.IncrementAvgItemSize(((byte[])cloned.GetValue<object>()).Length);
                }


                cloned.FlagMap = flagMap;

                if (cloned.AbsoluteExpiration != null && cloned.AbsoluteExpiration != NoAbsoluteExpiration && cloned.SlidingExpiration != null && cloned.SlidingExpiration != Cache.NoSlidingExpiration)
                    throw new ArgumentException("You can not set both sliding and absolute expirations on a single item");

                if (cloned.AbsoluteExpiration != null && cloned.AbsoluteExpiration != NoAbsoluteExpiration)
                    cloned.Expiration = new Expiration(ExpirationType.Absolute) { ExpireAfter = cloned.AbsoluteExpiration.ToUniversalTime() - DateTime.Now.ToUniversalTime() };

                clonedItems[count] = cloned;
                keys[count] = key;
                count++;
            }


            

            try
            {
                IDictionary<string, Exception> result = CacheImpl.Insert(keys,
                    clonedItems, dsItemUpdatedCallbackID,
                    providerName, sizes, false, clientId, updateCallbackId,
                    removeCallbackId, updateCallbackFilter, removeCallabackFilter,callbackType);
                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerUpdBulkSample(stats.Current);
                    _perfStatsCollector.IncrementByUpdPerSecStats(keys.Length);
                }

                return result;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
        }



        internal virtual void InsertOperation(string key, object value,  DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, CacheItemRemovedCallback onRemoveCallback, CacheItemUpdatedCallback onUpdateCallback,
            EventTypeInternal eventType, bool isResyncExpiredItems, LockHandle lockHandle,
            LockAccessType accessType, string providerName, string resyncProviderName, CacheDataNotificationCallback cacheItemUdpatedCallback,
            CacheDataNotificationCallback cacheItemRemovedCallaback, EventDataFilter itemUpdateDataFilter, EventDataFilter itemRemovedDataFilter, ref long size, bool allowQueryTags, string clientId,
            short updateCallbackId, short removeCallbackId, short dsItemUpdateCallbackId, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));


            if (absoluteExpiration != null && absoluteExpiration != Cache.NoAbsoluteExpiration && slidingExpiration != null && slidingExpiration != NoSlidingExpiration)
                throw new ArgumentException("You can not set both sliding and absolute expirations on a single item");
            
            Hashtable queryInfo = null;
            BitSet flagMap = new BitSet();
            string typeName = null;


            if (value is ClientCacheItem)
            {
                ClientCacheItem clientcacheItem = value as ClientCacheItem;
                value = clientcacheItem.Value;
                queryInfo = clientcacheItem.QueryInfo;

                if (clientcacheItem.Flags.IsAnyBitSet(BitSetConstants.BinaryData))
                    flagMap.SetBit(BitSetConstants.BinaryData);

                else if (clientcacheItem.Flags.IsAnyBitSet(BitSetConstants.JsonData))
                    flagMap.SetBit(BitSetConstants.JsonData);
                typeName = clientcacheItem.GroupDataType;
            }

            ValidateKeyValue(key, value);

            UsageStats stats = new UsageStats();
            stats.BeginSample();

         
            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();

            if (!string.IsNullOrEmpty(resyncProviderName))
                resyncProviderName = resyncProviderName.ToLower();

            object lockId = (lockHandle == null) ? null : lockHandle.LockId;

            if (queryInfo == null)
            {
                queryInfo = new Hashtable();

                if (allowQueryTags)
                {
                    queryInfo["query-info"] = GetQueryInfo(value);
                }
            }
            try
            {
                value = SafeSerialize(value, _serializationContext, ref flagMap, ref size, UserObjectType.CacheItem);

                if (_perfStatsCollector != null && value != null && value is byte[])
                    _perfStatsCollector.IncrementAvgItemSize(((byte[])value).Length);


                if (removeCallbackId == -1)
                {
                    if (cacheItemRemovedCallaback != null)
                    {
                        short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemRemovedCallaback, EventTypeInternal.ItemRemoved, itemRemovedDataFilter, callbackType);
                        removeCallbackId = callabackIds[1];
                    }
                    else if (onRemoveCallback != null)
                    {
                        removeCallbackId = GetCallbackId(onRemoveCallback, callbackType);
                    }
                }

                if (updateCallbackId == -1)
                {
                    if (cacheItemUdpatedCallback != null)
                    {
                        short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemUdpatedCallback, EventTypeInternal.ItemUpdated, itemUpdateDataFilter, callbackType);
                        updateCallbackId = callabackIds[0];
                    }
                    else if (onUpdateCallback != null)
                    {
                        updateCallbackId = GetCallbackId(onUpdateCallback, callbackType);
                    }
                }

                if (lockId != null && ((string)lockId) != string.Empty)
                    flagMap.SetBit(BitSetConstants.LockedItem);
                else
                    flagMap.UnsetBit(BitSetConstants.LockedItem);

                if (absoluteExpiration != null && absoluteExpiration != Cache.NoAbsoluteExpiration)
                    absoluteExpiration = absoluteExpiration.ToUniversalTime();

                CacheImpl.Insert(key,
                   value,
                   absoluteExpiration, slidingExpiration,
                   priority, removeCallbackId,
                   updateCallbackId, dsItemUpdateCallbackId,
                    isResyncExpiredItems,
                   queryInfo, flagMap, lockId,
                   accessType, providerName, resyncProviderName,
                   itemUpdateDataFilter, itemRemovedDataFilter,
                   size, false, clientId, typeName, callbackType);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerUpdSample(stats.Current);
                    _perfStatsCollector.IncrementUpdPerSecStats();
                }

            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        #endregion

        #region Locking Operations

        public virtual void Unlock(string key, LockHandle lockHandle = null)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (key == null) throw new ArgumentNullException("key is null.");

            object lockId = (lockHandle == null) ? null : lockHandle.LockId;

            try
            {
                CacheImpl.Unlock(key, lockId);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
            }
        }

        public virtual bool Lock(string key, TimeSpan lockTimeout, out LockHandle lockHandle)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));
            if (key == null) throw new ArgumentNullException("key is null.");

            lockHandle = null;
            bool lockAcquired = false;

            try
            {
                lockAcquired = CacheImpl.Lock(key, lockTimeout, out lockHandle);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }

            return lockAcquired;
        }

        internal virtual bool IsLocked(string key, ref LockHandle lockHandle)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));
            if (key == null) throw new ArgumentNullException("key is null.");

            try
            {
                return CacheImpl.IsLocked(key, ref lockHandle);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            return false;
        }
        #endregion

        #region	Remove/Delete Operation 

        #region Delete Operations

        public virtual void Remove(string key, LockHandle lockHandle = null)
        {
            CacheItemVersion version = null;
            string providerName = null;
          


            EventTypeInternal eventType=EventTypeInternal.None;

        

            LockAccessType accessType = LockAccessType.IGNORE_LOCK;


            if (lockHandle != null)
            {
                accessType = LockAccessType.DEFAULT;
            }

            DeleteInternal(key,  eventType, lockHandle, accessType, providerName);
        }

        public virtual void RemoveBulk(IEnumerable<string> keys)
        {
          
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));
            if (keys == null) throw new ArgumentNullException("keys");

            FindNull(keys);

            string[] keyList = keys.ToArray();

            if (keyList.Length == 0) throw new ArgumentException("There is no key present in keys array");
            RemoveDuplicateKeys(ref keyList);
            string providerName = null;
            BitSet flagMap = new BitSet();
            short dsItemsRemovedCallbackId = -1;
            UsageStats stats = new UsageStats();
            EventTypeInternal eventType;

      

            try
            {
                stats.BeginSample();
              

                CacheImpl.Delete(keyList.ToArray(), flagMap, providerName, dsItemsRemovedCallbackId);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerDelBulkSample(stats.Current);
                    _perfStatsCollector.IncrementByDelPerSecStats(keyList.Length);
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        internal virtual void DeleteInternal(string key, EventTypeInternal eventType, LockHandle lockHandle, LockAccessType accessType, string providerName)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new OperationFailedException(ErrorCodes.Common.EMPTY_KEY, ErrorMessages.GetErrorMessage(ErrorCodes.Common.EMPTY_KEY));


            
            try
            {
                UsageStats stats = new UsageStats();
                stats.BeginSample();

                object lockId = (lockHandle == null) ? null : lockHandle.LockId;
                BitSet flagMap = new BitSet();

                short dsItemRemovedCallbackId = -1;

               
                CacheImpl.Delete(key, flagMap, dsItemRemovedCallbackId, lockId, accessType);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerDelSample(stats.Current);
                    _perfStatsCollector.IncrementDelPerSecStats();
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        #endregion

        #region Remove Operations 

        public virtual bool Remove<T>(string key, out T removedItem, LockHandle lockHandle = null)
        {
            var removed = false;
            string providerName = null;
          
            EventTypeInternal eventType= EventTypeInternal.None; ;
           
            LockAccessType accessType = LockAccessType.IGNORE_LOCK;
            if (lockHandle != null)
            {
                accessType = LockAccessType.DEFAULT;
            }

            var removeResult = RemoveInternal<T>(key, eventType, lockHandle, accessType, providerName);
            removed = removeResult != null;

            if (removed)
                removedItem = (T)removeResult;
            else
                removedItem = default(T);

            return removed;
        }

        public virtual void RemoveBulk<T>(IEnumerable<string> keys, out IDictionary<string, T> removedItems)
        {
            removedItems = new Dictionary<string, T>();
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));
            if (keys == null) throw new ArgumentNullException("keys");

            FindNull(keys);

            string[] keyList = keys.ToArray();

            if (keyList.Length == 0) throw new ArgumentException("There is no key present in keys array");
            RemoveDuplicateKeys(ref keyList);
            short dsItemsRemovedCallbackId = -1;
            string providerName = null;
            BitSet flagMap = new BitSet();
            UsageStats stats = new UsageStats();

          
            EventTypeInternal eventType;

      

            try
            {

                stats.BeginSample();
            
                var removeResult = CacheImpl.Remove<T>(keyList.ToArray(), flagMap, providerName, dsItemsRemovedCallbackId);

                if (removeResult != null)
                {
                    object[] keyArr = new object[removeResult.Count];

                    removeResult.Keys.CopyTo(keyArr, 0);

                    IEnumerator ie = keyArr.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        CompressedValueEntry result = removeResult[ie.Current] as CompressedValueEntry;
                        if (result != null)
                        {
                            if (result.Type != EntryType.CacheItem)
                            {
                                removeResult[ie.Current] = null;
                                continue;
                            }

                            if (result.Flag.IsBitSet(_compressed))
                            {
                                result.Value = Decompress((byte[])result.Value);
                            }

                            if (_perfStatsCollector != null && result.Value != null && result.Value is byte[])
                                _perfStatsCollector.IncrementAvgItemSize(((byte[])result.Value).Length);

                            removedItems[ie.Current.ToString()] = SafeDeserialize<T>(result.Value, _serializationContext, result.Flag, UserObjectType.CacheItem);
                        }
                    }
                }

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerDelBulkSample(stats.Current);
                    _perfStatsCollector.IncrementByDelPerSecStats(keyList.Length);
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
            }
        }



        internal virtual object RemoveInternal<T>(string key,   EventTypeInternal eventType, LockHandle lockHandle, LockAccessType accessType, string providerName)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new OperationFailedException(ErrorCodes.Common.EMPTY_KEY, ErrorMessages.GetErrorMessage(ErrorCodes.Common.EMPTY_KEY));


           
            try
            {
                UsageStats stats = new UsageStats();
                stats.BeginSample();


                object lockId = (lockHandle == null) ? null : lockHandle.LockId; //Asif Imam  


                BitSet flagMap = new BitSet();

                short dsItemRemovedCallbackId = -1;

                CompressedValueEntry result = CacheImpl.Remove<T>(key, flagMap, dsItemRemovedCallbackId, lockId, accessType, providerName);

                if (result != null && result.Value != null)
                {
                    if (result.Flag.IsBitSet(_compressed))
                    {
                        result.Value = Decompress((byte[])result.Value);
                    }

                    if (_perfStatsCollector != null && result.Value != null && result.Value is byte[])
                        _perfStatsCollector.IncrementAvgItemSize(((byte[])result.Value).Length);

                    result.Value = SafeDeserialize<T>(result.Value, _serializationContext, result.Flag, UserObjectType.CacheItem);

                    if (_perfStatsCollector != null)
                    {
                        stats.EndSample();
                        _perfStatsCollector.IncrementMsecPerDelSample(stats.Current);
                        _perfStatsCollector.IncrementDelPerSecStats();
                    }
                    return result.Value;
                }
                else
                {

                    if (_perfStatsCollector != null)
                    {
                        stats.EndSample();
                        _perfStatsCollector.IncrementMsecPerDelSample(stats.Current);
                        _perfStatsCollector.IncrementDelPerSecStats();
                    }

                }

            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            return null;
        }

        #endregion
        #endregion

        #region GetEnumerator Operation

        public virtual IEnumerator GetEnumerator()
        {
            try
            {
                WebCacheEnumerator<object> enumerator = new WebCacheEnumerator<object>(_serializationContext, this);
                return enumerator;
            }
            finally
            {
            }
        }

        internal virtual List<EnumerationDataChunk> GetNextChunk(List<EnumerationPointer> pointer)
        {
            if (CacheImpl == null)
                throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            List<EnumerationDataChunk> chunks = null;

            try
            {
                chunks = CacheImpl.GetNextChunk(pointer);
            }
            catch (Exception ex)
            {
                //this is a empty call just to dispose the enumeration pointers for this particular enumerator
                //on all the nodes.
                for (int i = 0; i < pointer.Count; i++)
                {
                    pointer[i].isDisposable = true;
                }
                try
                {
                    CacheImpl.GetNextChunk(pointer);
                }
                catch (Exception)
                {

                }

                if (ExceptionsEnabled)
                    throw ex;
            }

            return chunks;
        }

        #endregion

        #region Finalizer 

        ~Cache()
        {
            Dispose(false);
        }
        #endregion

        #region IDisposable 

        private void Dispose(bool disposing)
        {
            try
            {
                lock (this)
                {
                    _refCount--;
                    if (_refCount > 0) return;
                    else if (_refCount < 0) _refCount = 0;

                    if (_messagingService != null)
                        _messagingService.Dispose();

                    string cacheIdWithAlias = _cacheId + (string.IsNullOrEmpty(_cacheAlias) ? "" : "(" + _cacheAlias + ")");



                    lock (CacheManager.Caches)
                    {
                        if (cacheIdWithAlias != null)
                            CacheManager.Caches.Remove(cacheIdWithAlias);

                    }

                    if (_listener != null) _listener.Dispose();

                    if (CacheImpl != null) CacheImpl.Dispose(disposing);
                    CacheImpl = null;

                    if (_secondaryInprocInstances != null)
                    {
                        foreach (Cache cache in _secondaryInprocInstances)
                        {
                            cache.Dispose();
                        }
                    }

                    if (_perfStatsCollector != null)
                        _perfStatsCollector.Dispose();




                    if (disposing) GC.SuppressFinalize(this);
                }
            }
            catch (Exception ex) { }
        }

        public virtual void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #region ToString() Implementation

        /// <summary>
        /// The string representation of the cache object. 
        /// </summary>
        public override string ToString()
        {
            return _cacheId;
        }
        #endregion

        #region Internal 

        #region  Events Listener Classes

        internal class CacheAsyncEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>

            private Cache _parent;


            private AsyncOperationCompletedCallback _asyncOperationCompleted;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            internal CacheAsyncEventsListener(Cache parent)
            {
                _parent = parent;
            }

            #region	/                 --- IDisposable ---           /

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or 
            /// resetting unmanaged resources.
            /// </summary>
            /// <remarks>
            /// </remarks>
            public virtual void Dispose()
            {
                try
                {
                }
                catch { }
            }

            #endregion


           
            public virtual void OnAsyncOperationCompleted(object opCode, object result, bool notifyAsync)
            {
                try
                {
                    BitSet flag = new BitSet();
                    object[] package = null;

                    package = (object[])_parent.SafeDeserialize<object[]>(result, _parent._serializationContext, flag, UserObjectType.CacheItem);

                    string key = (string)package[0];
                    AsyncCallbackInfo cbInfo = (AsyncCallbackInfo)package[1];
                    object res = package[2];

                    AsyncOpCode code = (AsyncOpCode)opCode;
                    int processid = AppUtil.CurrentProcess.Id;


                }

                catch { }
            }

        }

        internal class ClusterEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>

            private Cache _parent;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            internal ClusterEventsListener(Cache parent)
            {
                _parent = parent;

            }

            #region	/                 --- IDisposable ---           /

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or 
            /// resetting unmanaged resources.
            /// </summary>
            /// <remarks>
            /// </remarks>
            public virtual void Dispose()
            {
                try
                {

                }
                catch { }
            }

            #endregion


           

        }

        internal class CacheEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>
            private Cache _parent;
            private EventManager _eventManager;

            internal CacheEventsListener(Cache parent, EventManager eventManager)
            {
                _parent = parent;
                _eventManager = eventManager;

            }

            #region	/                 --- IDisposable ---           /

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or 
            /// resetting unmanaged resources.
            /// </summary>
            /// <remarks>
            /// </remarks>
            public virtual void Dispose()
            {
                try
                {

                }
                catch { }
            }

            #endregion

           

            public virtual void OnCustomRemoveCallback(string key, object value, ItemRemoveReason reason, BitSet Flag, bool notifyAsync)
            {
                try
                {
                    object[] args = value as object[];
                    if (args != null)
                    {
                        object val = args[0];

                        if (value is UserBinaryObject)
                            value = ((UserBinaryObject)value).GetFullObject();

                        CallbackInfo cbInfo = args[1] as CallbackInfo;
                        if (cbInfo != null)
                        {

                            val = _parent.SafeDeserialize<object>(val, _parent._serializationContext, Flag, UserObjectType.CacheItem);
                            int processid = AppUtil.CurrentProcess.Id;

                            CacheItemRemovedCallback cb = (CacheItemRemovedCallback)_parent._callbackIDsMap.GetResource(cbInfo.Callback);
                            if (cb != null)
                            {
                                if (notifyAsync)
                                {
#if !NETCORE
                                    cb.BeginInvoke(key, val, CacheHelper.GetWebItemRemovedReason(reason), null, null);
#elif NETCORE
                                    //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                                    TaskFactory factory = new TaskFactory();
                                    Task task = factory.StartNew(() => cb(key, val, CacheHelper.GetWebItemRemovedReason(reason)));
#endif
                                }
                                else
                                {
                                    cb(key, value, CacheHelper.GetWebItemRemovedReason(reason));
                                }
                            }
                        }
                    }

                }
                catch { }
            }

            public virtual void OnCustomRemoveCallback(string key, object value, CacheItemRemovedReason reason, BitSet flag, bool notifyAsync, EventCacheItem item)
            {
                try
                {
                    CallbackInfo cbInfo = value as CallbackInfo;
                    if (cbInfo != null)
                    {
                        if (_parent._perfStatsCollector != null)
                            _parent._perfStatsCollector.IncrementEventsProcessedPerSec();

                        if (item != null) item.SetValue(GetObject(item.GetValue<object>(), flag));
                        int handler = (int)cbInfo.Callback;
                        EventHandle handle = new EventHandle((short)handler);
                        _parent.EventManager.RaiseSelectiveCacheNotification(key, EventType.ItemRemoved, item, null, reason, notifyAsync, handle, cbInfo.DataFilter);
                    }
                }
                catch { }
            }

          

            public virtual void OnCustomUpdateCallback(string key, object value, bool notifyAsync, EventCacheItem item, EventCacheItem oldItem, BitSet flag)
            {
                try
                {
                    CallbackInfo cbInfo = value as CallbackInfo;

                    if (cbInfo != null)
                    {
                        if (item != null) item.SetValue(GetObject(item.GetValue<object>(), flag));
                        if (oldItem != null) oldItem.SetValue(GetObject(oldItem.GetValue<object>(), flag));

                        if (_parent._perfStatsCollector != null)
                            _parent._perfStatsCollector.IncrementEventsProcessedPerSec();
                        int handler = (int)cbInfo.Callback;
                        EventHandle handle = new EventHandle((short)handler);
                        this._eventManager.RaiseSelectiveCacheNotification(key, EventType.ItemUpdated, item, oldItem, CacheItemRemovedReason.Underused, notifyAsync, handle, cbInfo.DataFilter);
                    }
                }
                catch (Exception e) { }
            }

            public virtual void OnItemAdded(object key, bool notifyAsync, EventCacheItem item, BitSet flag)
            {
                try
                {
                    String keyString = key as string;
                    if (key != null)
                    {
                        if (item != null && item.GetValue<object>() != null)
                            item.SetValue(GetObject(item.GetValue<object>(), flag));
                        _eventManager.RaiseGeneralCacheNotification(keyString, EventType.ItemAdded, item, null, CacheItemRemovedReason.Underused, notifyAsync);
                    }
                }
                catch { }
            }

            public virtual void OnItemUpdated(object key, bool notifyAsync, EventCacheItem item, EventCacheItem oldItem, BitSet flag)
            {
                try
                {
                    string ketString = key as string;
                    if (ketString != null)
                    {
                        if (item != null && item.GetValue<object>() != null)
                            item.SetValue(GetObject(item.GetValue<object>(), flag));
                        if (oldItem != null && oldItem.GetValue<object>() != null)
                            oldItem.SetValue(GetObject(oldItem.GetValue<object>(), flag));
                        this._eventManager.RaiseGeneralCacheNotification(ketString, EventType.ItemUpdated, item, oldItem, CacheItemRemovedReason.Underused, notifyAsync);
                    }
                }
                catch (Exception e)
                { }
            }

            public virtual void OnItemRemoved(string key, object value, ItemRemoveReason reason, BitSet Flag, bool notifyAsync, EventCacheItem item)
            {
                try
                {
                    if (item != null && item.GetValue<object>() != null)
                    {
                        value = GetObject(value, Flag);
                        item.SetValue(value);
                    }
                    this._eventManager.RaiseGeneralCacheNotification(key, EventType.ItemRemoved, item, null, CacheHelper.GetWebItemRemovedReason(reason), notifyAsync);
                }
                catch { }
            }

            public virtual void OnItemRemoved(string key, object value, CacheItemRemovedReason reason, BitSet Flag, bool notifyAsync, EventCacheItem item)
            {
                try
                {
                    if (item != null && value != null)
                    {
                        value = GetObject(value, Flag);
                        item.SetValue(value);
                    }
                    this._eventManager.RaiseGeneralCacheNotification(key, EventType.ItemRemoved, item, null, reason, notifyAsync);
                }
                catch { }
            }

            public virtual void OnCustomNotification(object notifId, object data, bool notifyAsync)
            {
                try
                {
                    BitSet flag = new BitSet();
                    notifId = _parent.SafeDeserialize<object>(notifId, _parent._serializationContext, flag, UserObjectType.CacheItem);
                    data = _parent.SafeDeserialize<object>(data, _parent._serializationContext, flag, UserObjectType.CacheItem);
                    if (_parent._customEvent != null)
                    {
                        Delegate[] dltList = _parent._customEvent.GetInvocationList();
                        for (int i = dltList.Length - 1; i >= 0; i--)
                        {
                            CustomEventCallback subscriber = (CustomEventCallback)dltList[i];
                            try
                            {
                                if (notifyAsync)
                                {
#if !NETCORE
                                    subscriber.BeginInvoke(notifId, data, null, null);
#elif NETCORE
                                    //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                                    TaskFactory factory = new TaskFactory();
                                    Task task = factory.StartNew(() => subscriber(notifId, data));
#endif
                                }
                                else
                                    subscriber(notifId, data);

                                if (_parent._perfStatsCollector != null)
                                    _parent._perfStatsCollector.IncrementEventsProcessedPerSec();
                            }
                            catch (Exception e)
                            {
                            }
                        }
                    }
                }

                catch { }
            }

          

            public virtual void OnPollNotified(short callbackId, EventTypeInternal eventType)
            {
                _eventManager.RaisePollNotification(callbackId, eventType);
            }

            private object GetObject(object value, BitSet Flag)
            {
                try
                {
                    if (value is UserBinaryObject)
                        value = ((UserBinaryObject)value).GetFullObject();

                    return _parent.SafeDeserialize<object>(value, _parent._serializationContext, Flag, UserObjectType.CacheItem);
                }
                catch (Exception ex)
                {
                    return value;
                }
            }

            public virtual void OnTaskCompletedCallback(string taskId, short taskStatus, string failureReason, short callbackId)
            {
            }

         

            internal void OnReregisterTopic()
            {
                _parent._messagingService.OnReregisterTopic();
            }

        }

        internal class GeneralDataNotificationWrapper
        {
            private Cache _parentCache;

            public GeneralDataNotificationWrapper(Cache parentCache)
            {
                _parentCache = parentCache;
            }

            public void OnCacheDataNotification(string key, CacheEventArg eventArgs)
            {
                switch (eventArgs.EventType)
                {
                   
                }
            }
        }

        #endregion

        #region CallBacks
        internal virtual short GetCallbackId(CacheItemRemovedCallback removedCallback, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            if (removedCallback == null)
                return -1;

            return _eventManager.RegisterSelectiveCallback(removedCallback, callbackType);
        }

        internal virtual short GetCallbackId(CacheItemUpdatedCallback updateCallback, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            if (updateCallback == null)
                return -1;

            return _eventManager.RegisterSelectiveCallback(updateCallback, callbackType);
        }

      
     

        #endregion

        #region Internal Methods

        internal virtual void Touch(List<string> keys)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));
            if (keys == null) throw new ArgumentNullException("keys");

            Hashtable newKeys = new Hashtable();

            for (int i = 0; i < keys.Count; i++)
            {
                if (!string.IsNullOrEmpty(keys[i])) newKeys[keys[i]] = null;
            }

            try
            {
                CacheImpl.Touch(keys);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
            }
        }
        
        internal virtual void MakeTargetCacheActivePassive(bool makeActive)
        {
            try
            {
                CacheImpl.MakeTargetCacheActivePassive(makeActive);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        internal virtual TypeInfoMap GetQueryTypeMap()
        {
            return CacheImpl != null ? CacheImpl.TypeMap : null;
        }

        internal virtual void SetQueryTypeInfoMap(TypeInfoMap typeMap)
        {
            _queryTypeMap = typeMap;
        }

        internal virtual void AddRef()
        {
            lock (this)
            {
                _refCount++;
            }
        }

        internal virtual void AddSecondaryInprocInstance(Cache secondaryInstance)
        {
            if (_secondaryInprocInstances == null)
                _secondaryInprocInstances = new ArrayList();

            _secondaryInprocInstances.Add(secondaryInstance);
        }

        internal void SetMessagingServiceCacheImpl(CacheImplBase cacheImpl)
        {
            _messagingService.PubSubManager.CacheImpl = cacheImpl;
        }

        
        public virtual Cache GetCacheInstance()
        {
            return this;
        }

        internal virtual void InitializeCompactFramework()
        {

            if (CacheImpl is RemoteCache)
                MiscUtil.RegisterCompactTypes(null); //change null to CacheImpl.PoolManager when implemented pools for client

            CompactFormatterServices.RegisterCompactType(typeof(ProductVersion), 302);
            CompactFormatterServices.RegisterCompactType(typeof(Notifications), 107, null); //change null to CacheImpl.PoolManager.GetNotificationsPool() when implemented pools for client
            CompactFormatterServices.RegisterCompactType(typeof(Common.Net.Address), 110);
#if SERVER
            CompactFormatterServices.RegisterCompactType(typeof(OpenStreamOperation), 147);
            CompactFormatterServices.RegisterCompactType(typeof(CloseStreamOperation), 148);
            CompactFormatterServices.RegisterCompactType(typeof(VirtualArray), 149);
            CompactFormatterServices.RegisterCompactType(typeof(WriteToStreamOperation), 139);
#endif

            Hashtable types = null;
            if (CacheImpl != null)
                types = CacheImpl.GetCompactTypes();

            InitializeCompactFramework(types);
        }

        internal virtual void InitializeEncryption()
        {
            Hashtable encryptionInfo = CacheImpl.GetEncryptionInfo();
        }
        internal virtual object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag, ref long size, UserObjectType userObjectType, bool isCustomAttributeBaseSerialzed = false)
        {
            if (!InternalSerializationEnabled) return serializableObject;

            object serializedObject = null;

            if (CacheImpl == null)
                throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (serializableObject != null)
            {
                UsageStats statsSerialization = new UsageStats();
                statsSerialization.BeginSample();
                serializedObject = CacheImpl.SafeSerialize(serializableObject, serializationContext, ref flag, CacheImpl, ref size, userObjectType, isCustomAttributeBaseSerialzed);
                statsSerialization.EndSample();
                if (_perfStatsCollector != null)
                    _perfStatsCollector.IncrementMsecPerSerialization(statsSerialization.Current);
            }

            return serializedObject;
        }

        internal virtual T SafeDeserialize<T>(object serializedObject, string serializationContext, BitSet flag, UserObjectType userObjectType)
        {
            if (!InternalSerializationEnabled) return (T)serializedObject;


            T deSerializedObject = default(T);

            if (CacheImpl == null)
                throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (serializedObject != null)
            {
                UsageStats statsDeserialization = new UsageStats();
                statsDeserialization.BeginSample();
                deSerializedObject = CacheImpl.SafeDeserialize<T>(serializedObject, serializationContext, flag, CacheImpl, userObjectType);
                statsDeserialization.EndSample();
                if (_perfStatsCollector != null)
                    _perfStatsCollector.IncrementMsecPerDeserialization(statsDeserialization.Current);
            }

            return deSerializedObject;
        }

        internal byte[] Compress(byte[] value, ref BitSet flag, long threshold)
        {
            UsageStats statsCompression = new UsageStats();
            statsCompression.BeginSample();
            byte[] compressedValue = CompressionUtil.Compress(value, ref flag, threshold);
            statsCompression.EndSample();
            return compressedValue;
        }

        internal byte[] Decompress(byte[] value)
        {
            UsageStats statsCompression = new UsageStats();
            statsCompression.BeginSample();
            byte[] decompressedValue = CompressionUtil.Decompress(value);
            statsCompression.EndSample();
            return decompressedValue;
        }

        internal PollingResult Poll(bool isNotifiedPoll)
        {
            try
            {
                PollingResult result = CacheImpl.Poll();
                if (_perfStatsCollector != null && result != null)
                {
                    if (isNotifiedPoll)
                    {
                        // Do not set last poll result counter if resultant is 0
                        if (result.RemovedKeys.Count == 0 && result.UpdatedKeys.Count == 0)
                            return result;
                    }
                }
                return result;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
            }

            return new PollingResult();
        }

        internal bool CheckUserAuthorization(string cacheId, string password, string userId)
        {
            try
            {
                return CacheImpl.CheckCSecurityAuthorization(cacheId.ToLower(), Alachisoft.NCache.Common.EncryptionUtil.Encrypt(password), userId);
            }
            catch
            {
                return false;
            }
        }
        

        internal virtual object GetSerializedObject(string key, ref ulong v, ref BitSet flag, ref DateTime absoluteExpiration, ref TimeSpan slidingExpiration, ref Hashtable queryInfo)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new OperationFailedException(ErrorCodes.Common.EMPTY_KEY, ErrorMessages.GetErrorMessage(ErrorCodes.Common.EMPTY_KEY));

            CacheItem result = null;
            try
            {
                BitSet flagMap = new BitSet();

                queryInfo = null;

                LockHandle lockHandle = null;

                result = CacheImpl.GetCacheItem(key, flagMap,ref lockHandle, TimeSpan.Zero, LockAccessType.IGNORE_LOCK) as CacheItem;



                if (result == null) return null;
                //set the flag
                flag.Data = result.FlagMap.Data;
                if (result != null && result.GetValue<object>() != null)
                {

                    absoluteExpiration = GetCompatibleExpiration(result.AbsoluteExpiration, result.SlidingExpiration);


                    slidingExpiration = NoSlidingExpiration;

                    if (result.FlagMap.IsBitSet(_compressed))
                        result.SetValue(Decompress((byte[])result.GetValue<object>()));

                    return result;
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                else return null;
            }
            return null;
        }
        internal static DateTime GetCompatibleExpiration(DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            if (absoluteExpiration != Cache.NoAbsoluteExpiration)
                return absoluteExpiration;
            if (slidingExpiration == Cache.NoSlidingExpiration)
                return Cache.NoAbsoluteExpiration;
            return absoluteExpiration;
        }
        internal static bool FindDupliateKeys(string[] keys)
        {
            Hashtable hashtable = new Hashtable(keys.Length);
            bool duplicateFound = false;
            try
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i] == null)
                        throw new ArgumentNullException("key");

                    //If Count is less than the capacity of the Hashtable, this method is an O(1) operation. 
                    //If the capacity needs to be increased to accommodate the new element, this method becomes an O(n) operation, 
                    //where n is Count.
                    hashtable.Add(keys[i], null);
                }
            }
            catch (ArgumentNullException e)
            {
                throw new OperationFailedException(e.Message, e);
            }
            catch (ArgumentException e)
            {
                duplicateFound = true;
            }
            hashtable.Clear();
            return duplicateFound;
        }


        #endregion

        #endregion

        #region Private Methods 

        private DateTime ToUTC(DateTime date)
        {
            if (date != null && date != Cache.NoAbsoluteExpiration)
                return date.ToUniversalTime();
            return date;
        }


        private Hashtable GetQueryInfo(Object value)
        {
            Hashtable queryInfo = null;

            TypeInfoMap typeMap = _queryTypeMap != null ? _queryTypeMap : CacheImpl.TypeMap;

            if (typeMap == null)
                return null;

            try
            {
                string typeName = value.GetType().FullName;
                typeName = typeName.Replace("+", ".");

                int handleId = typeMap.GetHandleId(typeName);
                if (handleId != -1)
                {
                    queryInfo = new Hashtable();
                    Type valType = null; //(Cattering Case-InSensetive string comparisons.
                    ArrayList attribValues = new ArrayList();
                    ArrayList attributes = typeMap.GetAttribList(handleId);
                    for (int i = 0; i < attributes.Count; i++)
                    {
                        PropertyInfo propertyAttrib = value.GetType().GetProperty((string)attributes[i]);
                        if (propertyAttrib != null)
                        {
                            Object attribValue = propertyAttrib.GetValue(value, null);
                            //Donot lower strings here because we need to return the string in original form in case of MIN and MAX
                            attribValues.Add(attribValue);
                        }
                        else
                        {
                            FieldInfo fieldAttrib = value.GetType().GetField((string)attributes[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                            if (fieldAttrib != null)
                            {
                                Object attribValue = fieldAttrib.GetValue(value);
                                //Donot lower strings here because we need to return the string in original form in case of MIN and MAX
                                attribValues.Add(attribValue);
                            }
                            else
                            {
                                throw new Exception("Unable extracting query information from user object.");
                            }
                        }
                    }
                    queryInfo.Add(handleId, attribValues);
                }
            }
            catch (Exception) { }
            return queryInfo;
        }

     
       
      

        private void ValidateKeyValue(object key, object value)
        {
            Type type = typeof(ICompactSerializable);
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");
            if (key is string && (string)key == string.Empty) throw new OperationFailedException(ErrorCodes.Common.EMPTY_KEY, ErrorMessages.GetErrorMessage(ErrorCodes.Common.EMPTY_KEY));
            if (!key.GetType().IsSerializable && !type.IsAssignableFrom(key.GetType())) throw new ArgumentException("key is not serializable");
        }

       

        private void InitializeCompactFramework(Hashtable types)
        {
            if (types != null)
            {
                IDictionaryEnumerator ide = types.GetEnumerator();
                while (ide.MoveNext())
                {
                    // Code change for handling Non Compact Fields in Compact Serilization context
                    Hashtable handleNonCompactFields = (Hashtable)ide.Value;
                    Hashtable nonCompactFields = null;

                    short typeHandle = (short)handleNonCompactFields["handle"];
                    if (handleNonCompactFields.Contains("non-compact-fields"))
                        nonCompactFields = (Hashtable)handleNonCompactFields["non-compact-fields"];

                    CompactFormatterServices.RegisterCustomCompactType((Type)ide.Key, typeHandle, _serializationContext.ToLower(),
                        SerializationUtil.GetSubTypeHandle(_serializationContext, typeHandle.ToString(), (Type)ide.Key),
                        SerializationUtil.GetAttributeOrder(_serializationContext),
                        SerializationUtil.GetPortibilaty(typeHandle, _serializationContext), nonCompactFields);

                    //Also register array type for custom types.
                    typeHandle += SerializationUtil.UserdefinedArrayTypeHandle;  //Same handle is used at server side.
                    Type arrayType = ((Type)ide.Key).MakeArrayType();

                    CompactFormatterServices.RegisterCustomCompactType(arrayType, typeHandle, _serializationContext.ToLower(),
                        SerializationUtil.GetSubTypeHandle(_serializationContext, (typeHandle).ToString(), arrayType),
                        SerializationUtil.GetAttributeOrder(_serializationContext),
                        SerializationUtil.GetPortibilaty(typeHandle, _serializationContext), nonCompactFields);
                }
            }

        }

        private void RemoveDuplicateKeys(ref string[] keys)
        {
            Hashtable keysAndItems = new Hashtable(keys.Length);
            for (int item = 0; item < keys.Length; item++)
            {
                if (keys[item] != null)
                    keysAndItems[keys[item]] = null;
                else
                    throw new ArgumentNullException("keys", "Keys can not contain null values");
            }
            keys = new string[keysAndItems.Count];
            keysAndItems.Keys.CopyTo(keys, 0);
        }

      

        private void FindNull(IEnumerable<string> keys)
        {
            try
            {
                foreach (var item in keys)
                {
                    if (item == null)
                        throw new ArgumentNullException("key");
                }

            }
            catch (ArgumentNullException e)
            {
                throw new OperationFailedException(e.Message, e);
            }
        }


        private Task<TReturn> MakeTask<TReturn>(Func<TReturn> func)
        {
            return (new Task<TReturn>(func));
        }
        #endregion

        #region Search Service Methods 

        #endregion

        #region Messaging Service Methods


       

        internal virtual CacheEventDescriptor RegisterCacheNotificationInternal(string key, CacheDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter, bool notifyOnItemExpiration, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            CacheEventDescriptor discriptor = null;

            try
            {
                if (key != null)
                {
                    short[] callbackRefs = _eventManager.RegisterSelectiveEvent(callback, EventsUtil.GetEventTypeInternal(eventType), datafilter, callbackType);
                    CacheImpl.RegisterKeyNotificationCallback(key, callbackRefs[0], callbackRefs[1], datafilter, notifyOnItemExpiration, callbackType);
                }
                else
                {
                    discriptor = _eventManager.RegisterGeneralEvents(callback, eventType, datafilter);
                }
            }
            catch (Exception ex)
            {
                if (ExceptionsEnabled) throw;
            }
            return discriptor;
        }

        internal virtual void RegisterCacheDataNotificationCallback(string[] key, CacheDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter, bool notifyOnItemExpiration, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {

            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            try
            {
                if (key != null)
                {
                    short[] callbackRefs = _eventManager.RegisterSelectiveEvent(callback, EventsUtil.GetEventTypeInternal(eventType), datafilter, callbackType);
                    CacheImpl.RegisterKeyNotificationCallback(key, callbackRefs[0], callbackRefs[1], datafilter, notifyOnItemExpiration, callbackType);
                }
            }
            catch (Exception ex)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        internal virtual void UnRegisterCacheNotification(CacheEventDescriptor discriptor)
        {
            if (CacheImpl == null)
                if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));
            if (discriptor == null)
                throw new ArgumentNullException("CacheEventDiscriptor");

            if (!discriptor.IsRegistered)
                return;

            _eventManager.UnregisterDiscriptor(discriptor);
        }

        internal virtual void UnRegisterCacheNotification(string key, CacheDataNotificationCallback callback, EventTypeInternal eventType)
        {
            if (CacheImpl == null)
                if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            try
            {
                short[] value = _eventManager.UnregisterSelectiveNotification(callback, eventType);

                short update = value[0];
                short remove = value[1];

                CacheImpl.UnRegisterKeyNotificationCallback(key, update, remove);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        internal virtual void UnRegisterCacheNotification(string[] key, CacheDataNotificationCallback callback, EventTypeInternal eventType)
        {
            if (CacheImpl == null)
                if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            try
            {
                short[] value = _eventManager.UnregisterSelectiveNotification(callback, eventType);

                short update = value[0];
                short remove = value[1];

                CacheImpl.UnRegisterKeyNotificationCallback(key, update, remove);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }


        internal virtual void RegisterCacheNotification(string key, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentNullException("key");

            if (selectiveCacheDataNotificationCallback == null)
                throw new ArgumentException("selectiveCacheDataNotificationCallback");

            RegisterCacheNotificationInternal(key, selectiveCacheDataNotificationCallback, eventType, datafilter, true, callbackType);
        }

        internal virtual void UnregiserGeneralCacheNotification(EventTypeInternal eventType)
        {
            if (CacheImpl != null)
            {
                CacheImpl.UnRegisterGeneralNotification(eventType, -1);
            }
        }

        internal virtual void RegisterCacheNotificationDataFilter(EventTypeInternal eventType, EventDataFilter datafilter, short eventSequenceId)
        {
            if (CacheImpl != null)
            {
                CacheImpl.RegisterGeneralNotification(eventType, datafilter, eventSequenceId);
            }
        }

    

        internal virtual void RaiseCustomEvent(object notifId, object data)
        {
            BitSet flag = null;
            long size = 0;

            ValidateKeyValue(notifId, data);

            try
            {
                object serializeNotifId = Serialization.Formatters.CompactBinaryFormatter.ToByteBuffer(notifId, _serializationContext);
                object serializeData = Serialization.Formatters.CompactBinaryFormatter.ToByteBuffer(data, _serializationContext);

                CacheImpl.RaiseCustomEvent(serializeNotifId, serializeData);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
            }
        }


        internal virtual void RegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback, CacheItemRemovedCallback removeCallback, bool notifyOnItemExpiration, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (key == null) throw new ArgumentNullException("key");
            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            try
            {
                short updateCallbackid = -1;
                short removeCallbackid = -1;

                if (updateCallback != null) updateCallbackid = GetCallbackId(updateCallback, callbackType);
                if (removeCallback != null) removeCallbackid = GetCallbackId(removeCallback, callbackType);

                CacheImpl.RegisterKeyNotificationCallback(key, updateCallbackid, removeCallbackid, Runtime.Events.EventDataFilter.None, notifyOnItemExpiration, callbackType);

            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        [Obsolete("This method is deprecated. 'Please use RegisterCacheNotification(string key, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter)'", false)]
        internal virtual void RegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback, CacheItemRemovedCallback removeCallback)

        {
            try
            {
                RegisterKeyNotificationCallback(key, updateCallback, removeCallback, true);
            }
            finally
            {
            }

        }

        internal short RegisterUpdateSorrogateCallback(CacheItemUpdatedCallback onUpdateCallback, CacheDataNotificationCallback cacheItemUpdatedCallback, EventDataFilter eventDataFilter)
        {
            short updateCallbackId = -1;
            try
            {
                if (cacheItemUpdatedCallback != null)
                {
                    short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemUpdatedCallback,
                        EventTypeInternal.ItemUpdated, eventDataFilter);
                    updateCallbackId = callabackIds[0];
                }
                else if (onUpdateCallback != null)
                {
                    updateCallbackId = GetCallbackId(onUpdateCallback);
                }
            }
            catch { }
            return updateCallbackId;
        }

        internal short RegisterRemoveSorrogateCallback(CacheItemRemovedCallback onRemovedCallback, CacheDataNotificationCallback cacheItemRemovedCallback, EventDataFilter eventDataFilter)
        {
            short removeCallbackId = -1;
            try
            {
                if (cacheItemRemovedCallback != null)
                {
                    short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemRemovedCallback,
                        EventTypeInternal.ItemRemoved, eventDataFilter);
                    removeCallbackId = callabackIds[1];
                }
                else if (onRemovedCallback != null)
                {
                    removeCallbackId = GetCallbackId(onRemovedCallback);
                }
            }
            catch { }
            return removeCallbackId;
        }

        internal virtual void RegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback, CacheItemRemovedCallback removeCallback, bool notifyOnItemExpiration)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (key == null) throw new ArgumentNullException("key");
            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            try
            {
                short updateCallbackid = -1;
                short removeCallbackid = -1;

                if (updateCallback != null) updateCallbackid = GetCallbackId(updateCallback);
                if (removeCallback != null) removeCallbackid = GetCallbackId(removeCallback);

                CacheImpl.RegisterKeyNotificationCallback(key, updateCallbackid, removeCallbackid, notifyOnItemExpiration);

            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        [Obsolete("This method is deprecated. 'Please use RegisterCacheNotification(string[] keys, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter)'", false)]
        internal virtual void RegisterKeyNotificationCallback(string[] keys, CacheItemUpdatedCallback updateCallback, CacheItemRemovedCallback removeCallback, bool notifyOnExpiration, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("Keys count can not be zero");

            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            try
            {
                short updateCallbackid = -1;
                short removeCallbackid = -1;

                if (updateCallback != null) updateCallbackid = GetCallbackId(updateCallback, callbackType);
                if (removeCallback != null) removeCallbackid = GetCallbackId(removeCallback, callbackType);

                CacheImpl.RegisterKeyNotificationCallback(keys, updateCallbackid, removeCallbackid, EventDataFilter.None, notifyOnExpiration, callbackType);

            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        [Obsolete("This method is deprecated. 'Please use RegisterCacheNotification(string[] keys, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter)'", false)]
        internal virtual void UnRegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback, CacheItemRemovedCallback removeCallback)
        {
            try
            {
                if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

                if (key == null) throw new ArgumentNullException("key");
                if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

                short updateCallbackId = EventManager.UnRegisterSelectiveCallback(updateCallback);
                short removeCallbackId = EventManager.UnRegisterSelectiveCallback(removeCallback);

                if (updateCallbackId == -1 && removeCallbackId == -1) return;

                try
                {
                    CacheImpl.UnRegisterKeyNotificationCallback(key, updateCallbackId, removeCallbackId);
                }
                catch (Exception) { if (ExceptionsEnabled) throw; }
            }
            finally
            {
            }
        }

        [Obsolete("This method is deprecated. 'Please use RegisterCacheNotification(string[] keys, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter)'", false)]
        internal virtual void UnRegisterKeyNotificationCallback(string[] keys, CacheItemUpdatedCallback updateCallback, CacheItemRemovedCallback removeCallback)

        {
            if (CacheImpl == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));

            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("Keys count can not be zero");
            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            short updateCallbackId = EventManager.UnRegisterSelectiveCallback(updateCallback);
            short removeCallbackId = EventManager.UnRegisterSelectiveCallback(removeCallback);

            if (updateCallbackId == -1 && removeCallbackId == -1) return;

            try
            {
                CacheImpl.UnRegisterKeyNotificationCallback(keys, updateCallbackId, removeCallbackId);
            }
            catch (Exception) { if (ExceptionsEnabled) throw; }
        }

        internal virtual void RegisterPollingNotification(PollNotificationCallback callback, EventTypeInternal eventType)
        {
            if (callback != null)
            {
                short callbackId = EventManager.RegisterPollingEvent(callback, eventType);
            }
        }

        #endregion

        #region Notification Service Methods 

        internal virtual event CacheStoppedCallback CacheStopped
        {
            add
            {
                _cacheStopped += value;
                if (CacheImpl != null && ++_refCacheStoppedCount == 1) CacheImpl.RegisterCacheStoppedEvent();
            }
            remove
            {
                int beforeLength, afterLength = 0;
                lock (this)
                {
                    if (_cacheStopped != null)
                    {
                        beforeLength = _cacheStopped.GetInvocationList().Length;
                        _cacheStopped -= value;

                        if (_cacheStopped != null)
                            afterLength = _cacheStopped.GetInvocationList().Length;

                        if (beforeLength - afterLength == 1)
                            if (CacheImpl != null && --_refCacheStoppedCount == 0) CacheImpl.UnregisterCacheStoppedEvent();
                    }
                }
            }
        }

    

        internal virtual event MemberJoinedCallback MemberJoined
        {
            add
            {
                _memberJoined += value;
            }
            remove
            {
                _memberJoined -= value;
            }
        }

        internal virtual event MemberLeftCallback MemberLeft
        {
            add
            {
                _memberLeft += value;
            }
            remove
            {
                _memberLeft -= value;
            }
        }

     
        #endregion

        #region Execution Service Methods



        internal virtual ArrayList GetRunningTasks()
        {
            if (CacheImpl == null || _cacheId == null) throw new OperationFailedException(ErrorCodes.CacheInit.CACHE_NOT_INIT, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CACHE_NOT_INIT));
            
            return null;
        }

    
        #endregion

        #region Distributed Data Type Operations

        #region Data Type Manager

        EventTypeInternal ConvertToEventType(EventType type)
        {
            EventTypeInternal eventType = EventTypeInternal.None;
            switch (type)
            {
                case EventType.ItemAdded:
                    eventType = EventTypeInternal.ItemAdded;
                    break;
                case EventType.ItemRemoved:
                    eventType = EventTypeInternal.ItemRemoved;
                    break;
                case EventType.ItemUpdated:
                    eventType = EventTypeInternal.ItemUpdated;
                    break;

            }
            return eventType;
        }
        
       
        #endregion


  

    

       


        #endregion



    }
}
