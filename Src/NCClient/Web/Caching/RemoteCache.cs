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
using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Caching;
using System.Collections.Generic;
using System.Diagnostics;
using ClientInfo = Alachisoft.NCache.Runtime.Caching.ClientInfo;
using EnumerationPointer = Alachisoft.NCache.Common.DataStructures.EnumerationPointer;
using Exception = System.Exception;
using GroupEnumerationPointer = Alachisoft.NCache.Common.DataStructures.GroupEnumerationPointer;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Management.Statistics;
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Common.Pooling;



namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Implements the clustered cache for an application. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// One instance of this class is created per application domain, and it remains 
    /// valid as long as the application domain remains active. This object is accessible through the 
    /// <see cref="CacheManager.Cache"/> property of the <see cref="CacheManager"/> object
    /// </remarks>
    /// <requirements>
    /// </requirements>
    internal sealed class RemoteCache : CacheImplBase
    {
        private Broker _broker = null;
        private Hashtable _compactTypes;
        internal Cache _parent;
        private Hashtable _encryption;

        private string _cacheId;
        private bool _clearNotifRegistered;
        private CacheAsyncEventsListener _asyncListener;
        private CacheEventsListener _eventListener;

        StatisticsCounter _perfStatsCollector;


        private bool _addNotifRegistered;
        private bool _removeNotifRegistered;
        private bool _updateNotifRegistered;
        private bool _customNotifRegistered;

        
        private long _compressionThresholdSize;
        private bool _compressionEnabled;
        private TypeInfoMap _typeMap;
        private bool _isAuthorized;
        
        private Hashtable _expirations;
        Stopwatch _dedicatedCallMonitoring = new Stopwatch();
        bool _isDedicatedCall = false;
        object _mutexlock = new object();

        #region ---------------------- Properties ----------------------



        protected override internal TypeInfoMap TypeMap
        {
            get { return _typeMap; }
            set { _typeMap = value; }
        }

        /// <summary>
        /// Get the name of this cache
        /// </summary>
        internal string CacheId
        {
            get { return _cacheId; }
        }

  

        /// <summary>
        /// Set the CompactTypes that are read from configuration
        /// </summary>
        internal Hashtable CompactTypes
        {
            set { _compactTypes = value; }
        }

        /// <summary>
        /// Set the Encryption information that are read from configuration 
        /// </summary>
        internal Hashtable Encryption
        {
            get { return _encryption; }
        }

        /// <summary>
        /// Gets the event listener for the remote cache.
        /// </summary>
        public RemoteCache.CacheEventsListener EventListener
        {
            get { return _eventListener; }
        }

        /// <summary>
        /// Gets the Asynchronous event listener for the remote cache.
        /// </summary>
        public RemoteCache.CacheAsyncEventsListener AsyncEventHandler
        {
            get { return _asyncListener; }
        }


        protected override internal EventManager EventManager
        {
            get { return _parent.EventManager; }
        }

        internal override sealed PoolManager PoolManager
        {
            get;
        }

        #endregion

        #region ---------------------- Fields and Events ----------------------
        /// <summary>
        /// Used in the <paramref name="_absoluteExpiration"/> parameter in an Insert method call to 
        /// indicate the item should never expire. This field is read-only.
        /// </summary>
        /// <remarks>When used, this field sets the <paramref name="_absoluteExpiration"/> parameter equal 
        /// to <see cref="DateTime.MaxValue"/>, which is a constant representing the largest possible 
        /// <see cref="DateTime"/> value, 12/31/9999 11:59:59 PM.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="NoAbsoluteExpiration"/> field to disable 
        /// absolute expirations when inserting an item in the <see cref="Web"/>.
        /// <code>
        /// 
        ///	NCache.Web.Insert("DSN", connectionString, null, Web.NoAbsoluteExpiration, TimeSpan.FromSeconds(10));
        /// 
        /// </code>
        /// </example>
        public static readonly DateTime NoAbsoluteExpiration = DateTime.MaxValue.ToUniversalTime();

        /// <summary>
        /// Used as the <paramref name="_slidingExpiration"/> parameter in an Insert method call 
        /// to disable sliding expirations. This field is read-only.
        /// </summary>
        /// <remarks>When used, this field sets the <paramref name="_slidingExpiration"/> parameter equal to the <see cref="TimeSpan.Zero"/>
        /// field, which has a constant value of zero. The cached item then expires 
        /// in accordance with the <paramref name="_absoluteExpiration"/> parameter.</remarks>
        /// <example>The following example demonstrates how to use the Insert method to add an item to the 
        /// <see cref="Web"/> object using the <see cref="NoSlidingExpiration"/> field.
        /// <code>
        /// 
        ///	NCache.Web.Insert("DSN", connectionString, null, DateTime.Now.AddMinutes(2), Web.NoSlidingExpiration);
        /// 
        /// </code>
        /// </example>
        public static readonly TimeSpan NoSlidingExpiration = TimeSpan.Zero;
        private bool _clientConnectivityRegistered;

        public override void RegisterClearEvent()
        {
            _clearNotifRegistered = true;
            RegisterNotifications(NotificationsType.RegClearNotif);
        }

        public override void UnregisterClearEvent()
        {
            _clearNotifRegistered = false;
            UnRegisterNotifications(NotificationsType.UnregClearNotif);
        }

        public override void RegisterCacheStoppedEvent()
        {
            RegisterNotifications(NotificationsType.RegCacheStoppedNotif);
        }

        public override void UnregisterCacheStoppedEvent()
        {
            RegisterNotifications(NotificationsType.UnregCacheStoppedNotif);
        }


        public override void RegisterAddEvent()
        {
            _addNotifRegistered = true;
            RegisterNotifications(NotificationsType.RegAddNotif);
        }

        public override void RegisterGeneralNotification(EventTypeInternal eventType, EventDataFilter datafilter, short sequenceNumber)
        {
            NotificationsType notificationType;

            if ((eventType & EventTypeInternal.ItemAdded) != 0)
                notificationType = NotificationsType.RegAddNotif;
            else if ((eventType & EventTypeInternal.ItemRemoved) != 0)
                notificationType = NotificationsType.RegRemoveNotif;
            else if ((eventType & EventTypeInternal.ItemUpdated) != 0)
                notificationType = NotificationsType.RegUpdateNotif;
            else
                return;

            RegisterNotificationCommand command = new RegisterNotificationCommand(notificationType, datafilter, sequenceNumber);

            CommandResponse res = ExecuteCacheRequest(command, false);
            res.ParseResponse();
        }

        public override void UnRegisterGeneralNotification(EventTypeInternal unregister, short sequenceNumber)
        {
            NotificationsType notificationType;


            if ((unregister & EventTypeInternal.ItemAdded) != 0)
                notificationType = NotificationsType.UnregAddNotif;
            else if ((unregister & EventTypeInternal.ItemRemoved) != 0)
                notificationType = NotificationsType.UnregRemoveNotif;
            else if ((unregister & EventTypeInternal.ItemRemoved) != 0)
                notificationType = NotificationsType.UnregUpdateNotif;
            else
                return;

            RegisterNotificationCommand command = new RegisterNotificationCommand(notificationType, sequenceNumber);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }


        /// <summary>
        /// Will be called by broker incase of multiple connection
        /// </summary>
        /// <param name="connection"></param>
        internal void RegisterAddEvent(Connection connection)
        {
            RegisterNotifications(NotificationsType.RegAddNotif, connection);
        }

        public override void RegisterRemoveEvent()
        {
            _removeNotifRegistered = true;
            RegisterNotifications(NotificationsType.RegRemoveNotif);
        }

        /// <summary>
        /// Will be called by broker incase of multiple connection
        /// </summary>
        /// <param name="connection"></param>
        internal void RegisterRemoveEvent(Connection connection)
        {
            RegisterNotifications(NotificationsType.RegRemoveNotif, connection);
        }

        public override void RegisterUpdateEvent()
        {
            _updateNotifRegistered = true;
            RegisterNotifications(NotificationsType.RegUpdateNotif);
        }

        /// <summary>
        /// Will be called by broker incase of multiple connection
        /// </summary>
        /// <param name="connection"></param>
        internal void RegisterUpdateEvent(Connection connection)
        {
            RegisterNotifications(NotificationsType.RegUpdateNotif, connection);
        }

        /// <summary>
        /// Will be called by broker incase of multiple connection
        /// </summary>
        /// <param name="connection"></param>
        internal void RegisterClearEvent(Connection connection)
        {
            RegisterNotifications(NotificationsType.RegClearNotif, connection);
        }

        public override void RegisterCustomEvent()
        {
            _customNotifRegistered = true;
            RegisterNotifications(NotificationsType.RegCustomNotif);
        }

        /// <summary>
        /// Will be called by broker incase of multiple connection
        /// </summary>
        /// <param name="connection"></param>
        internal void RegisterCustomEvent(Connection connection)
        {
            RegisterNotifications(NotificationsType.RegCustomNotif, connection);
        }

        public override void RegisterNodeJoinedEvent()
        {
            RegisterNotifications(NotificationsType.RegNodeJoinedNotif);
        }

        /// <summary>
        /// Will be called by broker incase of multiple connection
        /// </summary>
        /// <param name="connection"></param>
        internal void RegisterNodeJoinedEvent(Connection connection)
        {
            RegisterNotifications(NotificationsType.RegNodeJoinedNotif, connection);
        }

        public override void RegisterNodeLeftEvent()
        {
            RegisterNotifications(NotificationsType.RegNodeLeftNotif);
        }

        /// <summary>
        /// Will be called by broker incase of multiple connection
        /// </summary>
        /// <param name="connection"></param>
        internal void RegisterNodeLeftEvent(Connection connection)
        {
            RegisterNotifications(NotificationsType.RegNodeLeftNotif, connection);
        }

        internal void RegisterHashmapChangedEvent(Connection connection)
        {
            RegisterNotifications(NotificationsType.RegHashmapChangedNotif, connection);
        }

        public override void UnregisterAddEvent()
        {
            _addNotifRegistered = false;
            UnRegisterNotifications(NotificationsType.UnregAddNotif);
        }

        public override void UnregisterUpdateEvent()
        {
            _updateNotifRegistered = false;
            UnRegisterNotifications(NotificationsType.UnregUpdateNotif);
        }

        public override void UnregisterCustomEvent()
        {
            _customNotifRegistered = false;
            UnRegisterNotifications(NotificationsType.UnregCustomNotif);
        }

        public override void UnregisterRemoveEvent()
        {
            _removeNotifRegistered = false;
            UnRegisterNotifications(NotificationsType.UnregRemoveNotif);
        }

        public override void UnregisterNodeJoinedEvent()
        {
        }

        public override void UnregisterNodeLeftEvent()
        {
        }

        /// <summary>
        /// Re- register the general nofication when reconnects to some other node.
        /// </summary>
        public void ReRegisterGeneralNotification(Connection connection)
        {
            if (_parent != null && _parent.EventManager != null)
            {
                EventManager.EventRegistrationInfo[] generalEventRegistrations = _parent.EventManager.GetEventRegistrationInfo();
                //Re-registers general events with new server
                if (generalEventRegistrations != null)
                {
                    for (int i = 0; i < generalEventRegistrations.Length; i++)
                    {
                        EventManager.EventRegistrationInfo eventRegistration = generalEventRegistrations[i];
                        RegisterNotifications(eventRegistration.EventTYpe, eventRegistration.DataFilter, eventRegistration.RegistrationSequence, connection);
                    }
                }
            }
            if (_clearNotifRegistered)
                RegisterClearEvent(connection);
            if (_customNotifRegistered)
                RegisterCustomEvent(connection);

            if (_clientConnectivityRegistered)
                RegisterCacheClientConnectivityEvent();

            RegisterNodeJoinedEvent(connection);
            RegisterNodeLeftEvent(connection);
        }

        #endregion


        internal RemoteCache(string cacheId, Cache parent,  CacheConnectionOptions cacheConnectionOptions, StatisticsCounter perfStatsCol)
            : base()
        {
            _parent = parent;
            LocalClientInfo.AppName = cacheConnectionOptions.AppName ?? AppUtil.CurrentProcess.ProcessName;
            _perfStatsCollector = perfStatsCol;


            if (_parent != null)
            {
                _asyncListener = new CacheAsyncEventsListener(_parent.AsyncListener);
                _eventListener = new CacheEventsListener(_parent.EventListener);

            }

            _cacheId = cacheId;

            _broker = new Broker(this, cacheConnectionOptions, perfStatsCol, LocalClientInfo);

            //moiz: changed this for init param task
            ServerInfo server = _broker.GetInitialServer();

            _broker.StartServices(cacheId, server.IpString, server.Port);

            if (!_broker.IsConnected)
                throw new OperationFailedException(ErrorCodes.Common.NO_SERVER_AVAILABLE_FOR_CACHE,ErrorMessages.GetErrorMessage(ErrorCodes.Common.NO_SERVER_AVAILABLE_FOR_CACHE,_cacheId));

            #region --------------------------------- [ Pooling ] ---------------------------------

            PoolManager = null;

            
            // Initialize needed pools here

            #endregion
        }


        internal static bool BalanceNodes(string cacheId)
        {
            return ConfigReader.ReadItemValue("client.ncconf", cacheId, "load-balance");
        }

        internal static bool ImportHashmap(string cacheId)
        {
            return true;
        }
        
        public override string Name
        {
            get
            {
                return _cacheId;
            }
        }

      

        #region ---------------------- Add ----------------------

        public override void Add(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority,
            short removeCallback, short updateCallback, short dsItemAddedCallback, bool isResyncExpiredItems,Hashtable queryInfo, BitSet flagMap,
            string providerName, string resyncProviderName, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, long size, bool encryptionEnabled, string clientId, string typeName)
        {
            AddCommand command = new AddCommand(key, (byte[])value,
                absoluteExpiration,
                slidingExpiration,
                priority,
                removeCallback,
                updateCallback,
                dsItemAddedCallback,
                isResyncExpiredItems,
                -1,
                false, queryInfo,
                flagMap, providerName, resyncProviderName,
                encryptionEnabled,
                CacheId,
                updateCallbackFilter,
                removeCallabackFilter, TargetMethodAttribute.MethodOverload, clientId, typeName);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }


        /// <summary>
        /// Add array of <see cref="CacheItem"/> to the cache.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <returns>keys that are added or that alredy exists in the cache and their status.</returns>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>		
        /// <example>The following example demonstrates how to add items to the cache with an absolute 
        /// expiration 2 minutes from now, a priority of 
        /// high, and that notifies the application when the item is removed from the cache.
        /// 
        /// First create a CacheItems.
        /// <code>
        /// string keys = {"ORD_23", "ORD_67"};
        /// CacheItem items = new CacheItem[2]
        /// items[0] = new CacheItem(new Order());
        /// items[0].AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// items[0].Priority = CacheItemPriority.High;
        /// items[0].ItemRemoveCallback = onRemove;
        ///
        /// items[1] = new CacheItem(new Order());
        /// items[1].AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// items[1].Priority = CacheItemPriority.Low;
        /// items[1].ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then add CacheItem to the cache
        /// <code>
        /// 
        ///	NCache.Cache.Add(keys, items, "Customer", "Orders");
        ///
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Add(keys, items, "Customer", "Orders");
        /// 
        /// </code>
        /// </example>
        public override IDictionary<string, Exception> Add(string[] keys, CacheItem[] items,
            short onDataSourceItemsAdded, string providerName, long[] sizes, bool encryptionEnabled,
            string clientId, short itemUpdatedCallback, short itemRemovedCallback,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                bool isMrgKeys = _broker.GetKeysDistributionMap(keys, items, ref keysDistributionMap);
                foreach (Address serverAddress in keysDistributionMap.Keys)
                {
                    KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                    BulkAddCommand command = new BulkAddCommand(keysAndItems.Key, keysAndItems.Value,
                        onDataSourceItemsAdded, _parent, providerName, encryptionEnabled, CacheId,
                        TargetMethodAttribute.MethodOverload, clientId, itemUpdatedCallback, itemRemovedCallback,
                        updateCallbackFilter, removeCallabackFilter, callbackType);
                    if (isMrgKeys)
                        command.ClientLastViewId = Broker.ForcedViewId;
                    else
                        command.ClientLastViewId = _broker.ClientLastViewId;
                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                BulkAddCommand command = new BulkAddCommand(keys, items, onDataSourceItemsAdded, _parent, providerName,
                    encryptionEnabled, CacheId, TargetMethodAttribute.MethodOverload, clientId,
                    itemUpdatedCallback, itemRemovedCallback, updateCallbackFilter, removeCallabackFilter,
                     callbackType);
                request = _broker.CreateRequest(command);
            }

            _broker.ExecuteRequest(request);
            CommandResponse res = request.Response;
            res.ParseResponse();
            return HashtableUtil.ToDictionary(res.KeyValueDic);
        }

       
        public override bool SetAttributes(string key, CacheItemAttributes attributes)
        {
            AddAttributeCommand command = new AddAttributeCommand(key, attributes.AbsoluteExpiration, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.OperationWasuccessfull();
        }


        #endregion


        #region ---------------------- Insert ----------------------

        public override void Insert(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, short onDsItemUpdatedCallback,
            bool isResyncExpiredItems, Hashtable queryInfo, BitSet flagMap, object lockId, LockAccessType accessType,
            string providerName, string resyncProviderName, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, long size, bool encryptionEnabled, string clientId, string typeName, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            InsertCommand command = new InsertCommand(key,
                (byte[])value,
                absoluteExpiration,
                slidingExpiration,
                priority,
                onRemoveCallback,
                onUpdateCallback,
                onDsItemUpdatedCallback,
                isResyncExpiredItems,
                -1,
                false, queryInfo,
                flagMap,
                lockId,
                0 ,
                accessType, providerName, resyncProviderName,
                encryptionEnabled,
                CacheId, updateCallbackFilter, removeCallabackFilter, TargetMethodAttribute.MethodOverload, clientId, typeName, callbackType);

            Request request = _broker.CreateRequest(command);

            bool waitForResponse = true; //bool.Parse(ConfigurationManager.AppSettings["WaitForResponse"]);

            if (waitForResponse)
            {
                _broker.ExecuteRequest(request);

                CommandResponse res = request.Response;
                res.ParseResponse();

            }
            else
            {
                _broker.ExecuteRequest(request, null, false, false);
            }
        }

        /// <summary> Insert list of <see cref="CacheItem"/> to the cache </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <returns>returns keys that are added or updated successfully and their status.</returns>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>
        /// <example>The following example demonstrates how to assign an item high priority when you insert 
        /// it into your application's <see cref="Cache"/> object.
        ///	<para><b>Note: </b>For more information about how to use this method with the <see cref="CacheItemRemovedCallback"/> 
        ///	delegate, see <see cref="CacheItemRemovedCallback"/>.
        ///	</para>
        /// First create CacheItems.
        /// <code>
        /// string[] keys = {"SQLDSN", "ORADSN"};
        /// CacheItem items[] = new CacheItem[2];
        /// items[0] = new CacheItem(sqlConnectionString);
        /// item.AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// item.SlidingExpiration = TimeSpan.Zero;
        /// item.Priority = CacheItemPriority.High;
        /// item.ItemRemoveCallback = onRemove;
        /// 
        /// items[1] = new CacheItem(oraConnectionString);
        /// item.AbsoluteExpiration = DateTime.Now.AddMinutes(1);
        /// item.SlidingExpiration = TimeSpan.Zero;
        /// item.Priority = CacheItemPriority.Low;
        /// item.ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then insert CacheItems to the cache
        /// <code>
        ///
        ///	NCache.Cache.Insert(keys, items, "Connection", null);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Insert(keys, items, "Connection", null);
        /// 
        /// </code>
        /// </example>
        public override IDictionary<string, Exception> Insert(string[] keys, CacheItem[] items,
            short onDsItemsUpdatedCallback, string providerName, long[] sizes,
            bool encryptionEnabled, string clientId, short itemUpdateCallback, short itemRemovedCallback,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                bool isMrgKeys = _broker.GetKeysDistributionMap(keys, items, ref keysDistributionMap);
                foreach (Address serverAddress in keysDistributionMap.Keys)
                {
                    KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                    BulkInsertCommand command = new BulkInsertCommand(keysAndItems.Key, keysAndItems.Value,
                        onDsItemsUpdatedCallback, _parent, providerName, encryptionEnabled, CacheId,
                        TargetMethodAttribute.MethodOverload, clientId, itemUpdateCallback, itemRemovedCallback,
                        updateCallbackFilter, removeCallabackFilter, callbackType);
                    if (isMrgKeys)
                        command.ClientLastViewId = Broker.ForcedViewId;
                    else
                        command.ClientLastViewId = _broker.ClientLastViewId;
                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                BulkInsertCommand command = new BulkInsertCommand(keys, items, onDsItemsUpdatedCallback, _parent,
                    providerName, encryptionEnabled, CacheId, TargetMethodAttribute.MethodOverload,
                    clientId, itemUpdateCallback, itemRemovedCallback,
                    updateCallbackFilter, removeCallabackFilter, callbackType);
                request = _broker.CreateRequest(command);
            }
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();


            return HashtableUtil.ToDictionary(res.KeyValueDic);
        }

        #endregion

     

        public override void Unlock(string key)
        {
            UnlockCommand command = new UnlockCommand(key, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void Unlock(string key, object lockId)
        {
            UnlockCommand command = new UnlockCommand(key, lockId, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        internal override bool IsLocked(string key, ref LockHandle lockHandle)
        {
            if (lockHandle == null) lockHandle = new LockHandle();
            IsLockedCommand command = new IsLockedCommand(key, lockHandle.LockId);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            lockHandle.LockId = res.LockId as string;
            lockHandle.LockDate = res.LockDate;

            return res.IsLocked;
        }

        public override bool Lock(string key, TimeSpan lockTimeout, out LockHandle lockHanlde)
        {
            LockCommand command = new LockCommand(key, lockTimeout, AppUtil.CurrentProcess.Id, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            lockHanlde = new LockHandle(res.LockId as string, res.LockDate);
            return res.LockAcquired;
        }

        #region ---------------------- Get ----------------------

        /// <summary>
        /// Retrieves the specified item from the Cache object. If the object is read thru the data source,
        /// put is against the given group and sub group.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>
        /// <param name="group">Group of the object.</param>
        /// <param name="subGroup">Sub group of the group.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="group"/>  contains a null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// <para>
        /// Note: The group and subGroup parameters are used only if the object is read thru the data source. 
        /// Otherwise the object will be returned from the cache whether it belongs to the given group and sub group or not.
        /// </para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// 
        ///	NCache.Cache.Get("MyTextBox.Value", "Customer", null);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Get("MyTextBox.Value", "Customer", null);
        /// 
        /// </code>
        /// </example>
        public override CompressedValueEntry Get<T>(string key, BitSet flagMap, string group, string subGroup, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType)
        {
            if (lockHandle == null) lockHandle = new LockHandle();
            object lockId = lockHandle.LockId;

           
            GetCommand command = new GetCommand(key, flagMap, group, subGroup, accessType, lockId, lockTimeout, AppUtil.CurrentProcess.Id, TargetMethodAttribute.MethodOverload);


            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            lockHandle.LockId = res.LockId as string;
            lockHandle.LockDate = res.LockDate;

            res.FlagValueEntry.Flag.Data |= flagMap.Data;
            res.FlagValueEntry.Type = res.EntryType;
            return res.FlagValueEntry;

           
        }

        /// <summary>
        /// Retrieves the object from the cache for the given keys as key value pairs
        /// </summary>
        /// <param name="keys">The keys against which items are to be fetched.</param>
        /// <returns>The retrieved cache items.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// 
        ///	NCache.Cache.Get(keys);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Get(keys);
        /// 
        /// </code> 
        /// </example>
        public override IDictionary Get<T>(string[] keys, BitSet flagMap)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                bool isMrgKeys = _broker.GetKeysDistributionMap(keys, null, ref keysDistributionMap);
                foreach (Address serverAddress in keysDistributionMap.Keys)
                {
                    KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                    BulkGetCommand command = new BulkGetCommand(keysAndItems.Key, flagMap, TargetMethodAttribute.MethodOverload);
                    if (isMrgKeys)
                        command.ClientLastViewId = Broker.ForcedViewId;
                    else
                        command.ClientLastViewId = _broker.ClientLastViewId;
                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                BulkGetCommand command = new BulkGetCommand(keys, flagMap,  TargetMethodAttribute.MethodOverload);
                request = _broker.CreateRequest(command);
            }

            CommandResponse res = null;
            try
            {
                _broker.ExecuteRequest(request);
                res = request.Response;
            }
            catch (ActivityBlockedException ex)
            {
                if (request.CommandRequestType == RequestType.NonKeyBulkRead)
                {
                    BulkGetCommand command = new BulkGetCommand(keys, flagMap, TargetMethodAttribute.MethodOverload);
                    res = ExecuteCacheRequest(command, true);
                }
            }

            res.ParseResponse();
            res.KeyValueDic = CacheHelper.BulkGetObjectOrInitializedCollection<T>(res.KeyValueDic, _parent);

            return res.KeyValueDic;

        }

        public override object GetCacheItem(string key, BitSet flagMap, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType)
        {

            if (lockHandle == null) lockHandle = new LockHandle();
            object lockId = lockHandle.LockId;


            

            GetCacheItemCommand command = new GetCacheItemCommand(key, flagMap, accessType, lockId, lockTimeout, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            lockHandle.LockId = res.LockId as string;
            lockHandle.LockDate = res.LockDate;
            return res.Item;
        }

        public override IDictionary GetCacheItemBulk(string[] keys, BitSet flagMap)
        {
            var request = default(Request);
            var keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();

            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                var isMergeKeys = _broker.GetKeysDistributionMap(keys, null, ref keysDistributionMap);

                foreach (var serverAddress in keysDistributionMap.Keys)
                {
                    var keysAndItems = keysDistributionMap[serverAddress];
                    var command = new BulkGetCacheItemCommand(keysAndItems.Key, flagMap, TargetMethodAttribute.MethodOverload);

                    if (isMergeKeys)
                    {
                        command.ClientLastViewId = Broker.ForcedViewId;
                    }
                    else
                    {
                        command.ClientLastViewId = _broker.ClientLastViewId;
                    }
                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                var command = new BulkGetCacheItemCommand(keys, flagMap, TargetMethodAttribute.MethodOverload);
                request = _broker.CreateRequest(command);
            }

            var response = default(CommandResponse);

            try
            {
                _broker.ExecuteRequest(request);
                response = request.Response;
            }
            catch (ActivityBlockedException)
            {
                if (request.CommandRequestType == RequestType.NonKeyBulkRead)
                {
                    var command = new BulkGetCacheItemCommand(keys, flagMap, TargetMethodAttribute.MethodOverload);
                    response = ExecuteCacheRequest(command, true);
                }
            }

            response.ParseResponse();
            return response.KeyValueDic;
        }

        #endregion

        #region ---------------------- Count ----------------------

        /// <summary>
        /// Gets the number of items stored in the cache.
        /// </summary>
        /// <remarks>This property can be useful when monitoring your application's performance 
        /// or when using ASP.NET tracing functionality.
        /// <para><b>Note:</b> In a partitioned cluster this operation is an expensive one as it might 
        /// result in network calls. It is therefore advised to use this property only when required.
        /// </para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <value>The number of items stored in the cache.</value>
        public override long Count
        {
            get
            {
                CountCommand command = new CountCommand(TargetMethodAttribute.MethodOverload);

                Request request = _broker.CreateRequest(command);
                _broker.ExecuteRequest(request);

                CommandResponse res = request.Response;
                res.ParseResponse();

                return res.Count;
            }
        }
        #endregion

        #region ---------------------- Contains ----------------------

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="Cache"/>.</param>
        /// <returns><b>true</b> if the <see cref="Cache"/> contains an element 
        /// with the specified key; otherwise, <b>false</b>.</returns>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> In a partitioned cluster this operation is an expensive one as it might 
        /// result in network calls. It is therefore advised to use this property only when required.
        /// </para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <example>The following example demonstrates how to check for containment of an item in the <see cref="Cache"/>.
        /// <code>
        /// 
        ///	if(NCache.Cache.Contains("MyTextBox.Value"))
        ///	{
        ///		Response.Write("Item found!");
        ///	}
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	if(Cache.Contains("MyTextBox.Value"))
        ///	{
        ///		Response.Write("Item found!");
        ///	}
        /// 
        /// </code>
        /// </example>
        public override bool Contains(string key)
        {
            var containsResult = false;
            var bulkResult = ContainsBulk(new string[] { key });

            // The following code is a bit aggressive.
            // That's cuz I'm paranoid like that.
            if (bulkResult?.Count > 0)
            {
                if (!bulkResult.TryGetValue(key, out containsResult))
                {
                    containsResult = false;
                }
            }
            return containsResult;
        }

        public override IDictionary<string, bool> ContainsBulk(string[] keys)
        {
            var request = default(Request);
            var keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();

            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                var isMergeKeys = _broker.GetKeysDistributionMap(keys, null, ref keysDistributionMap);

                foreach (var serverAddress in keysDistributionMap.Keys)
                {
                    var keysAndItems = keysDistributionMap[serverAddress];
                    var command = new ContainsBulkCommand(keysAndItems.Key, TargetMethodAttribute.MethodOverload);

                    if (isMergeKeys)
                    {
                        command.ClientLastViewId = Broker.ForcedViewId;
                    }
                    else
                    {
                        command.ClientLastViewId = _broker.ClientLastViewId;
                    }
                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                var command = new ContainsBulkCommand(keys, TargetMethodAttribute.MethodOverload);
                request = _broker.CreateRequest(command);
            }

            var response = default(CommandResponse);

            try
            {
                _broker.ExecuteRequest(request);
                response = request.Response;
            }
            catch (ActivityBlockedException)
            {
                if (request.CommandRequestType == RequestType.NonKeyBulkRead)
                {
                    var command = new ContainsBulkCommand(keys, TargetMethodAttribute.MethodOverload);
                    response = ExecuteCacheRequest(command, true);
                }
            }

            response.ParseResponse();
            return HashtableUtil.ExtractKeyStatus(response.KeyValueDic, keys);
        }

        #endregion

        #region ---------------------- Remove ----------------------

        /// <summary>
        /// Removes the object from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <returns>The item removed from the Cache. If the value in the key parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove("timestamp");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove("timestamp");
        /// 
        /// </code>
        /// </example>
        public override CompressedValueEntry Remove<T>(string key, BitSet flagMap, short onDsItemRemovedCallback, object lockId, LockAccessType accessType, string ProviderName)
        {
            RemoveCommand command = new RemoveCommand(key, flagMap, -1, false, onDsItemRemovedCallback, lockId,  0 , accessType, ProviderName, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            if (res.EntryType == EntryType.CacheItem)
                return res.FlagValueEntry;

            return CompressedValueEntry.CreateCompressedCacheEntry(null, CacheHelper.GetObjectOrInitializedCollection<T>(key, res.EntryType, res.FlagValueEntry.Value, _parent.GetCacheInstance()),
                flagMap, res.EntryType);
           
        }

        /// <summary>
        /// Removes the object from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove("timestamp");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove("timestamp");
        /// 
        /// </code>
        /// </example>
        public override void Delete(string key, BitSet flagMap, short onDsItemRemovedCallback, object lockId, LockAccessType accessType)
        {
            DeleteCommand command = new DeleteCommand(key, flagMap, -1, false, onDsItemRemovedCallback, lockId, 0, accessType,  TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        /// <summary>
        /// Removes the objects from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the item.</param>
        /// <returns>The items removed from the Cache. If the value in the keys parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove(keys);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove(keys);
        /// 
        /// </code>
        /// </example>
        public override IDictionary Remove<T>(string[] keys, BitSet flagMap, string providerName, short onDsItemRemovedCallback)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                bool isMrgKeys = _broker.GetKeysDistributionMap(keys, null, ref keysDistributionMap);
                foreach (Address serverAddress in keysDistributionMap.Keys)
                {
                    KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                    BulkRemoveCommand command = new BulkRemoveCommand(keysAndItems.Key, flagMap, providerName,
                        onDsItemRemovedCallback, TargetMethodAttribute.MethodOverload);
                    if (isMrgKeys)
                        command.ClientLastViewId = Broker.ForcedViewId;
                    else
                        command.ClientLastViewId = _broker.ClientLastViewId;

                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                BulkRemoveCommand command = new BulkRemoveCommand(keys, flagMap, providerName, onDsItemRemovedCallback, TargetMethodAttribute.MethodOverload);
                request = _broker.CreateRequest(command);
            }
            _broker.ExecuteRequest(request);
            CommandResponse res = request.Response;
            res.ParseResponse();
            res.KeyValueDic = CacheHelper.BulkGetObjectOrInitializedCollection<T>(res.KeyValueDic, _parent);
            return res.KeyValueDic;
        }

        /// <summary>
        /// Removes the objects from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the item.</param>
        /// <returns>The items removed from the Cache. If the value in the keys parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove(keys);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove(keys);
        /// 
        /// </code>
        /// </example>
        public override void Delete(string[] keys, BitSet flagMap, string providerName, short onDsItemRemovedCallback)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                bool isMrgKeys = _broker.GetKeysDistributionMap(keys, null, ref keysDistributionMap);

                foreach (Address serverAddress in keysDistributionMap.Keys)
                {
                    KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                    BulkDeleteCommand command = new BulkDeleteCommand(keysAndItems.Key, flagMap, providerName,
                        onDsItemRemovedCallback, TargetMethodAttribute.MethodOverload);
                    if (isMrgKeys)
                        command.ClientLastViewId = Broker.ForcedViewId;
                    else
                        command.ClientLastViewId = _broker.ClientLastViewId;

                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                BulkDeleteCommand command = new BulkDeleteCommand(keys, flagMap, providerName, onDsItemRemovedCallback, TargetMethodAttribute.MethodOverload);
                request = _broker.CreateRequest(command);
            }

            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        #endregion

        #region ---------------------- Clear ----------------------

        /// <summary>
        /// Removes all elements from the <see cref="Cache"/>.
        /// </summary>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to check for containment of an item in the <see cref="Cache"/>.
        /// <code>
        /// 
        ///	NCache.Cache.Clear();
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Clear();
        /// 
        /// </code>
        /// </example>
        public override void Clear(BitSet flagMap, short onDsClearedCallback, string providerName)
        {
            ClearCommand command = new ClearCommand(-1, false, flagMap, onDsClearedCallback, providerName, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }


        /// <summary>
        /// Removes all elements from the <see cref="Cache"/> asynchronously.
        /// </summary>
        /// <remarks>
        /// This is similar to <see cref="Clear"/> except that the operation is performed asynchronously.
        /// A <see cref="CacheCleared"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to clear the <see cref="Cache"/>.
        /// <code>
        /// 
        ///	NCache.Cache.ClearAsync();
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.ClearAsync();
        /// 
        /// </code>
        /// </example>
        public override void ClearAsync(BitSet flagMap, short onDsClearedCallback, string providerName)
        {
            ClearCommand command = new ClearCommand(-1, true, flagMap, onDsClearedCallback, providerName, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);
        }

        /// <summary>
        /// Removes all elements from the <see cref="Cache"/> asynchronously.
        /// </summary>
        /// <remarks>
        /// This is similar to <see cref="Clear"/> except that the operation is performed asynchronously.
        /// A <see cref="CacheCleared"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// <param name="onAsyncCacheClearedCallback">A delegate that, that can be used to get the result of 
        /// the Asynchronous Clear operation.</param>
        /// </remarks>
        /// <example>The following example demonstrates how to clear the <see cref="Cache"/>.
        /// <code>
        /// 
        /// void OnAsyncCacheCleared(object result)
        /// {
        /// ...
        /// }
        /// 
        ///	NCache.Cache.ClearAsync(new AsyncCacheClearedCallback(OnAsyncCacheCleared));
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.ClearAsync(new AsyncCacheClearedCallback(OnAsyncCacheCleared));
        /// 
        /// </code>
        /// </example>
        public override void ClearAsync(BitSet flagMap, short onAsyncCacheClearCallback, short onDsClearedCallback, string providerName)
        {
            ClearCommand command = new ClearCommand(onAsyncCacheClearCallback, true, flagMap, onDsClearedCallback, providerName, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);
        }

        #endregion

       

       
        public override object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag,
            CacheImplBase cacheImpl, ref long size, UserObjectType userObjectType,bool isCustomAttributeBaseSerialzed=false)
        {
            return SerializationUtil.SafeSerializeOutProc(serializableObject, serializationContext, ref flag, cacheImpl.SerializationEnabled, _parent.SerializationFormat, ref size, userObjectType, isCustomAttributeBaseSerialzed);
        }
        public override T SafeDeserialize<T>(object serializedObject, string serializationContext, BitSet flag, CacheImplBase cacheImpl, UserObjectType userObjectType)
        {
            object deserialized = serializedObject;

            try
            {
                deserialized = SerializationUtil.SafeDeserializeOutProc<T>(serializedObject, serializationContext, flag, cacheImpl.SerializationEnabled, userObjectType);
            }
            catch (InvalidOperationException)
            {
                // Because of trying to Json format data that is binary serialized
                deserialized = serializedObject;
                throw;
            }
            catch (InvalidCastException)
            {
                // Because of trying to Json format data that is binary serialized
                deserialized = serializedObject;
                throw;
            }
            catch (Newtonsoft.Json.JsonException je)
            {
                throw new OperationFailedException($"Failed to deserialize data. {je.Message}", je);
            }
            catch (Exception)
            {
                //Kill the exception; it is possible that object was serialized by Java
                //or from any other domain which can not be deserialized by us.
                deserialized = serializedObject;
            }

            return (T)deserialized;
        }

        /// <summary>
        /// Broadcasts a custom application defined event.
        /// </summary>
        /// <param name="notifId">Application specific notification code/id</param>
        /// <param name="data">Application specific data</param>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// <para><b>Note:</b> Custom event notifications can not be disabled through configuration.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to raise a custom application defined event.
        /// <code>
        /// 
        ///	NCache.Cache.RaiseCustomEvent(MyNotificationCodes.ConsumeItem,
        ///	      new ItemData(DateTime.Now));
        /// 
        ///	Cache.RaiseCustomEvent(MyNotificationCodes.ConsumeItem,
        ///	      new ItemData(DateTime.Now)); 
        /// 
        /// </code>
        /// </example>
        public override void RaiseCustomEvent(object notifId, object data)
        {
            RaiseCustomEventCommand command = new RaiseCustomEventCommand(notifId, data, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }


        /// <summary>
        /// Decerements the reference count of the cache and performs application-defined tasks 
        /// associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// The behavior of this method depends upon the cache's sharing mode (inproc/outproc) 
        /// specified in the configuration.
        /// <para>If the application uses the cache in <b>inproc</b> mode, calling Dispose closes the cache 
        /// and releases the resources used by it. If in a cluster the calling node leaves the 
        /// cluster as well. 
        /// </para>
        /// <para>If the application uses the cache in <b>outproc</b> mode, calling Dispose releases the 
        /// reference to the cache object. The cache itself remains operational and cluster remains intact. 
        /// </para>
        /// </remarks>
        public override void Dispose(bool disposing)
        {
            if (_broker != null)
            {
               
                _broker.Dispose(disposing);
                _broker = null;
            }
        }

        private void RegisterNotifications(NotificationsType notifMask)
        {
            RegisterNotificationCommand command = new RegisterNotificationCommand(notifMask);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        private void UnRegisterNotifications(NotificationsType notifMask)
        {
            RegisterNotificationCommand command = new RegisterNotificationCommand(notifMask);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }


        private void RegisterNotifications(NotificationsType notifMask, Connection connection)
        {
            RegisterNotificationCommand command = new RegisterNotificationCommand(notifMask);


            Request request = new Request(false, _broker.OperationTimeOut);
            Address ipAddress = connection.ServerAddress;
            request.AddCommand(ipAddress, command);

            _broker.ExecuteRequest(request, connection, true, true);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }
        private void RegisterNotifications(EventTypeInternal eventType, EventDataFilter dataFilter, short sequenceNumber, Connection connection)
        {
            NotificationsType notificationType;

            if ((eventType & EventTypeInternal.ItemAdded) != 0)
                notificationType = NotificationsType.RegAddNotif;
            else if ((eventType & EventTypeInternal.ItemRemoved) != 0)
                notificationType = NotificationsType.RegRemoveNotif;
            else if ((eventType & EventTypeInternal.ItemUpdated) != 0)
                notificationType = NotificationsType.RegUpdateNotif;
            else
                return;

            RegisterNotificationCommand command = new RegisterNotificationCommand(notificationType, dataFilter, sequenceNumber);

            Request request = new Request(false, _broker.OperationTimeOut);
            Address ipAddress = connection.ServerAddress;
            request.AddCommand(ipAddress, command);

           // _broker.ExecuteRequest(request, connection, true, true);

            CommandResponse res = ExecuteCacheRequest(command,true);
            res.ParseResponse();
        }

        #region ---------------------- Enumerator ----------------------

        /// <summary>
        /// Retrieves a dictionary enumerator used to iterate
        /// through the key settings and their values 
        /// contained in the cache.
        /// </summary>
        /// <remarks>
        /// If items are added or removed from the cache while enumerating through the items
        /// the behavior is not predictable. It is therefore advised not to update the cache keys
        /// while enumerating.
        /// <para><b>Note:</b> Just like <see cref="Cache.Count"/> in a cluster especially partitioned 
        /// this operation is an expensive one and may require network calls. It is therefore advised to use 
        /// this method only when required.
        /// </para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <returns>An enumerator to iterate through the <see cref="Cache"/> object.</returns>
        public override IEnumerator GetEnumerator()
        {
            return null;
        }

        public override List<EnumerationDataChunk> GetNextChunk(List<EnumerationPointer> pointers)
        {
            List<EnumerationDataChunk> nextChunk = null;
            Request request;
            CommandBase command = null;
            if (pointers.Count > 0 && pointers[0].NodeIpAddress != null)
            {
                if (_broker.ImportHashmap)
                    request = new Request(true, _broker.OperationTimeOut);
                else
                    request = new Request(false, _broker.OperationTimeOut);

                for (int i = 0; i < pointers.Count; i++)
                {
                    EnumerationPointer pointer = pointers[i];

                    if (pointer.IsGroupPointer)
                        command = new GetGroupNextChunkCommand(pointer as GroupEnumerationPointer);
                    else
                        command = new GetNextChunkCommand(pointer);

                    request.AddCommand(pointer.NodeIpAddress, command);
                }
            }
            else
            {
                if (!_broker.PoolHasAllServers || !_broker.ImportHashmap)
                {
                    EnumerationPointer pointer = pointers[0];
                    if (pointer.IsGroupPointer)
                        command = new GetGroupNextChunkCommand(pointer as GroupEnumerationPointer);
                    else
                        command = new GetNextChunkCommand(pointer);

                    request = _broker.CreateRequest(command);
                }
                else
                {
                    request = new Request(true, _broker.OperationTimeOut);
                    EnumerationPointer pointer = pointers[0];
                    for (int i = 0; i < _broker.ClientServerList.Count; i++)
                    {
                        command = new GetNextChunkCommand(pointer);
                        command.clientLastViewId = -1;
                        ServerInfo server = (ServerInfo)_broker.ClientServerList[i];
                        command.intendedRecipient = server.IP.ToString().ToString();
                        request.AddCommand(new Address(server.IP.ToString(), server.Port), command);
                    }
                }
            }

            _broker.ExecuteRequest(request);

            CommandResponse response = request.Response;
            response.ParseResponse();

            nextChunk = response.NextChunk;

            return nextChunk;
        }

        #endregion

        #region ---------------------- Compact Framework ----------------------

        public override Hashtable GetCompactTypes()
        {
            return _compactTypes;
        }

        #endregion

        #region /                       --- Event Listener Classes ---                      /

        ///// <summary>
        ///// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
        ///// internal class and must not be used from outside
        ///// </summary>
        internal class CacheAsyncEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>
            private Cache.CacheAsyncEventsListener _parent;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            internal CacheAsyncEventsListener(Cache.CacheAsyncEventsListener parent)
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
            public void Dispose()
            {
            }

            #endregion

            private object PackageResult(string key, short callbackId, object result)
            {
                object[] package = new object[3];
                package[0] = key;

                AsyncCallbackInfo notification = new AsyncCallbackInfo(-1, null, callbackId);
                package[1] = notification;
                package[2] = result;

                return package;
            }

            public void OnAsyncAddCompleted(string key, short callbackId, object result, bool notifyAsync)
            {
                OnAsyncOperationCompleted(AsyncOpCode.Add, PackageResult(key, callbackId, result), notifyAsync);
            }

            public void OnAsyncInsertCompleted(string key, short callbackId, object result, bool notifyAsync)
            {
                OnAsyncOperationCompleted(AsyncOpCode.Update, PackageResult(key, callbackId, result), notifyAsync);
            }

            public void OnAsyncRemoveCompleted(string key, short callbackId, object result, bool notifyAsync)
            {
                OnAsyncOperationCompleted(AsyncOpCode.Remove, PackageResult(key, callbackId, result), notifyAsync);
            }

            public void OnAsyncClearCompleted(short callbackId, object result, bool notifyAsync)
            {
                OnAsyncOperationCompleted(AsyncOpCode.Clear, PackageResult(null, callbackId, result), notifyAsync);
            }

            public void OnAsyncOperationCompleted(object opCode, object result, bool notifyAsync)
            {
                _parent.OnAsyncOperationCompleted(opCode, result, notifyAsync);
            }

          
        }

        /// <summary>
        /// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
        /// internal class and must not be used from outside
        /// </summary>
        internal class CacheEventsListener : MarshalByRefObject, IDisposable
        {
            //            /// <summary> Underlying implementation of NCache. </summary>
            private Cache.CacheEventsListener _listener;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            internal CacheEventsListener(Cache.CacheEventsListener parent)
            {
                _listener = parent;
            }

            #region	/                 --- IDisposable ---           /

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or 
            /// resetting unmanaged resources.
            /// </summary>
            /// <remarks>
            /// </remarks>
            public void Dispose()
            {
            }

            #endregion


            public void OnItemAdded(string key, bool notifyAsync, EventCacheItem item, BitSet flag)
            {
                try
                {
                    if (_listener != null)
                        _listener.OnItemAdded(key, notifyAsync, item, flag);
                }
                catch { }
            }


            public void OnItemUpdated(string key, bool notifyAsync, EventCacheItem item, EventCacheItem oldItem, BitSet flag)
            {
                try
                {
                    if (_listener != null)
                        _listener.OnItemUpdated(key, notifyAsync, item, oldItem, flag);
                }
                catch { }
            }

            public void OnPollNotified(short callbackId, EventTypeInternal eventType)
            {
                try
                {
                    if (_listener != null)
                        _listener.OnPollNotified(callbackId, eventType);
                }
                catch { }
            }

            public void OnItemRemoved(string key, object value, CacheItemRemovedReason reason, BitSet flag, bool notifyAsync, EventCacheItem item)
            {
                try
                {
                    if (_listener != null)
                        _listener.OnItemRemoved(key, value, reason, flag, notifyAsync, item);
                }
                catch (Exception e)
                { }
            }

            public void OnCustomNotification(object notifId, object data, bool notifyAsync)
            {
                try
                {
                    if (_listener != null)
                        _listener.OnCustomNotification(notifId, data, notifyAsync);
                }
                catch { }
            }


        
            public void OnCustomRemoveCallback(short callbackId, string key, object value, CacheItemRemovedReason reason, BitSet Flag, bool notifyAsync, EventCacheItem item, EventDataFilter dataFilter)
            {
                object[] val = new object[] { value, new CallbackInfo(null, callbackId, dataFilter) };
                _listener.OnCustomRemoveCallback(key, val, reason, Flag, notifyAsync, item);
            }

            public void OnCustomUpdateCallback(short callbackId, string key, bool notifyAsync, EventCacheItem item, EventCacheItem oldItem, BitSet flag, EventDataFilter dataFilter)
            {
                CallbackInfo cbInfo = new CallbackInfo(null, callbackId, dataFilter);
                _listener.OnCustomUpdateCallback(key, cbInfo, notifyAsync, item, oldItem, flag);
            }

         


           

            public void OnTaskCompletedCallback(string taskId, short taskStatus, string taskFailureReason, short callbackId)
            {
                try
                {
                    if (_listener != null)
                        _listener.OnTaskCompletedCallback(taskId, taskStatus, taskFailureReason, callbackId);
                }
                catch { }
            }
 

            public void OnReregisterTopic()
            {
                try
                {
                    if (_listener != null) _listener.OnReregisterTopic();
                }
                catch { }
            }
        }

        #endregion


        #region/            --- Key base notification registration ---      /

        public override void RegisterKeyNotificationCallback(string key, short updateCallbackid, short removeCallbackid, bool notifyOnitemExpiration)
        {
            CommandBase command = new RegisterKeyNotificationCommand(key, updateCallbackid, removeCallbackid, notifyOnitemExpiration);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void UnRegisterKeyNotificationCallback(string key, short updateCallbackid, short removeCallbackid)
        {
            CommandBase command = new UnRegisterKeyNotificationCommand(key, updateCallbackid, removeCallbackid);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void RegisterKeyNotificationCallback(string key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
            CommandBase command = new RegisterKeyNotificationCommand(key, update, remove, datafilter, notifyOnItemExpiration);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void RegisterKeyNotificationCallback(string key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            CommandBase command = new RegisterKeyNotificationCommand(key, update, remove, datafilter, notifyOnItemExpiration, callbackType);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void RegisterKeyNotificationCallback(string[] key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            CommandBase command = new RegisterBulkKeyNotificationCommand(key, update, remove, datafilter, notifyOnItemExpiration, callbackType);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void UnRegisterKeyNotificationCallback(string key, short update, short remove, EventTypeInternal eventType)
        {
            CommandBase command = new UnRegisterKeyNotificationCommand(key, update, remove);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void UnRegisterKeyNotificationCallback(string[] key, short update, short remove, EventTypeInternal eventType)
        {
            CommandBase command = new UnRegisterBulkKeyNotificationCommand(key, update, remove);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }



        public override void RegisterKeyNotificationCallback(string[] key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
            CommandBase command = new RegisterBulkKeyNotificationCommand(key, update, remove, datafilter, notifyOnItemExpiration);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void RegisterKeyNotificationCallback(string[] keys, short updateCallbackid, short removeCallbackid, string clientId, CallbackType callbackType = CallbackType.PullBasedCallback)
        {
            CommandBase command = new RegisterBulkKeyNotificationCommand(keys, updateCallbackid, removeCallbackid, clientId, callbackType);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void UnRegisterKeyNotificationCallback(string[] keys, short updateCallbackid, short removeCallbackid)
        {
            CommandBase command = new UnRegisterBulkKeyNotificationCommand(keys, updateCallbackid, removeCallbackid);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        #endregion


     

        internal void GetTypeInfoMap(Connection connection)
        {
            GetTypeInfoMapCommand command = new GetTypeInfoMapCommand(false);

            Request request = new Request(false, _broker.OperationTimeOut);
            Address ipAddress = connection.ServerAddress;
            request.AddCommand(ipAddress, command);

            _broker.ExecuteRequest(request, connection, true, true);

            CommandResponse res = request.Response;
            res.ParseResponse();

            this.TypeMap = res.TypeMap;
        }


        internal void GetCompactTypesFromServer(Connection connection)
        {
            GetCompactTypesCommand command = new GetCompactTypesCommand(false);

            Request request = new Request(false, _broker.OperationTimeOut);
            Address ipAddress = connection.ServerAddress;
            request.AddCommand(ipAddress, command);

            _broker.ExecuteRequest(request, connection, true, true);

            CommandResponse res = request.Response;
            res.ParseResponse();
            SerializationUtil.NCacheLog = _broker.Logger.NCacheLog;

            if (res.KeyValueDic != null)
            {
                Hashtable typesMap = HashtableUtil.CreateCustomComparerHashtable(res.KeyValueDic);
                this.CompactTypes = SerializationUtil.GetCompactTypes(typesMap, false, Name);
            }
        }

        public override Hashtable GetEncryptionInfo()
        {
            return _encryption;
        }

        public override Hashtable GetExpirationInfo()
        {
            return _expirations;
        }

        internal void GetSerializationFormat(Connection connection)
        {
            GetSerializationFormatCommand command = new GetSerializationFormatCommand(false);

            Request request = new Request(false, _broker.OperationTimeOut);
            Address ipAddress = connection.ServerAddress;
            request.AddCommand(ipAddress, command);

            _broker.ExecuteRequest(request, connection, true, true);

            CommandResponse res = request.Response;
            res.ParseResponse();

            _parent.SerializationFormat = res.SerializationFormat;
        }

        
        internal CommandResponse ExecuteCacheRequest(CommandBase command, bool isDedicated)
        {
            Request request = null;
            if (IsDedicated)
            {
                request = _broker.CreateDedicatedRequest(command);
            }
            else
            {
                request = _broker.CreateRequest(command);
            }
            _broker.ExecuteRequest(request);
            return request.Response;
        }

       

        #region Polling
        internal override Common.Events.PollingResult Poll()
        {
            PollCommand command = new PollCommand();

            CommandResponse response = null;

            try
            {
                response = ExecuteCacheRequest(command, false);
            }
            catch (ActivityBlockedException ex)
            {
                response = ExecuteCacheRequest(command, true);
            }

            try
            {
                response.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();

                response = ExecuteCacheRequest(command, true);
                response.ParseResponse();
            }
            return response.PollingResult;
        }

        public override void RegisterPollingNotification(short pollingCallbackId)
        {
            RegisterPollingNotificationCommand command = new RegisterPollingNotificationCommand(pollingCallbackId);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse response = request.Response;

            response.ParseResponse();
        }
        #endregion

        public override void RegisterCacheClientConnectivityEvent()
        {
            _clientConnectivityRegistered = true;
            RegisterNotifications(NotificationsType.RegisterClientConnectivityNotif);
        }

        public override void UnregisterCacheClientConnectivityEvent()
        {
            _clientConnectivityRegistered = false;
            UnRegisterNotifications(NotificationsType.UnregisterClientConnectivityNotif);
        }

        public override IList<ClientInfo> GetConnectedClientList()
        {
            Request request = _broker.CreateRequest(new GetConnectedClientsCommand());
            _broker.ExecuteRequest(request);
            CommandResponse response = request.Response;
            response.ParseResponse();
            return response.ConnectedClients;
        }

        private bool IsDedicated
        {
            get
            {
                lock (_mutexlock)
                {
                    if (_dedicatedCallMonitoring.ElapsedMilliseconds > 60000)
                    {
                        _dedicatedCallMonitoring.Stop();
                        _isDedicatedCall = false;
                    }
                    return _isDedicatedCall;
                }
            }
        }

        private void SetDedicatedCall()
        {
            lock (_mutexlock)
            {
                _isDedicatedCall = true;
                _dedicatedCallMonitoring.Restart();
            }
        }

      


        #region ---------------------- Touch ----------------------

        internal override void Touch(List<string> keys)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                bool isMrgKeys = _broker.GetKeysDistributionMap(keys.ToArray(), null, ref keysDistributionMap);

                foreach (Address serverAddress in keysDistributionMap.Keys)
                {
                    KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                    List<string> list = new List<string>();
                    list.AddRange(keysAndItems.Key);
                    TouchCommand command = new TouchCommand(list);
                    if (isMrgKeys)
                        command.ClientLastViewId = Broker.ForcedViewId;
                    else
                        command.ClientLastViewId = _broker.ClientLastViewId;

                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                TouchCommand command = new TouchCommand(keys);
                request = _broker.CreateRequest(command);
            }

            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

        }

        #endregion




        #region  ----- Messaging pub/sub------

        internal override long GetMessageCount(string topicName)
        {
            MessageCountCommand command = new MessageCountCommand(topicName);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse commandResponse = request.Response;
            commandResponse.ParseResponse();

            return commandResponse.MessageCount;
        }

        internal override bool GetOrCreate(string topicName, TopicOperationType type)
        {
            CommandBase command = new GetTopicCommand(topicName, type);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
            return res.OperationWasuccessfull();

        }

        internal override bool Subscribe(string topicName, string subscriptionName, SubscriptionType pubSubType,long creationTime,long expirationTime, SubscriptionPolicyType subscriptionPolicy = SubscriptionPolicyType.NonDurableExclusiveSubscription)
        {
            CommandBase command = new SubscribeTopicCommand(topicName, subscriptionName, pubSubType, creationTime, expirationTime, subscriptionPolicy);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
            return res.OperationWasuccessfull();
        }

        internal override bool UnSubscribe(string topicName, string recepientId, SubscriptionPolicyType subscriptionPolicy, SubscriptionType pubSubType,bool isDispose=false)
        {
            CommandBase command = new UnSubscribeTopicCommand(topicName, recepientId, pubSubType,subscriptionPolicy,isDispose);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
            return res.OperationWasuccessfull();
        }

        internal override void PublishMessage(string messageId, object payLoad, long creationTime, long expirationTime, Hashtable metadata, BitSet flagMap)
        {
            CommandBase command = new MessagePublishCommand(messageId, (byte[])payLoad, creationTime, expirationTime, metadata, flagMap);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        internal override bool RemoveTopic(string topicName, bool forcefully)
        {
            CommandBase command = new RemoveTopicCommand(topicName, forcefully);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
            return res.OperationWasuccessfull();
        }




        internal override object GetMessageData(BitSet flagMap)
        {
            var command = new GetMessageCommand(flagMap);
            ReceivedMessages response = new ReceivedMessages();
            CommandResponse res = ExecuteCacheRequest(command, false);

            if (res != null)
            {
                try
                {
                    res.ParseResponse();
                }
                catch (StateTransferInProgressException)
                {
                    SetDedicatedCall();
                    command = new GetMessageCommand(flagMap);

                    res = ExecuteCacheRequest(command, true);
                    res.ParseResponse();
                }
                response.AssignedMessages = res.MessageDic;
                response.RegisteredPatterns = res.RegisteredPatterns;

                return response;
            }
            return null;
        }

        internal override void AcknowledgeMessageReceipt(IDictionary<string, IList<string>> topicWiseMessageIds)
        {

            Request request;
            if (topicWiseMessageIds == null) return;
            if (_broker.ImportHashmap)
            {
                Dictionary<Address, Dictionary<string, IList<string>>> messagesDistributionMap;
                request = new Request(true, _broker.OperationTimeOut);
                bool isMrgKeys = _broker.GetMessageDistribution(topicWiseMessageIds, out messagesDistributionMap);
                foreach (Address serverAddress in messagesDistributionMap.Keys)
                {
                    Dictionary<string, IList<string>> topicWiseMessages = messagesDistributionMap[serverAddress];
                    var command = new MesasgeAcknowledgmentCommand(topicWiseMessages);
                    command.ClientLastViewId = isMrgKeys ? Broker.ForcedViewId : _broker.ClientLastViewId;
                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                var command = new MesasgeAcknowledgmentCommand(topicWiseMessageIds);
                request = _broker.CreateRequest(command);
            }

            CommandResponse res = null;
            try
            {
                _broker.ExecuteRequest(request);
                res = request.Response;
            }
            catch (ActivityBlockedException)
            {
                if (request.CommandRequestType == RequestType.NonKeyBulkRead)
                {
                    var command = new MesasgeAcknowledgmentCommand(topicWiseMessageIds);
                    res = ExecuteCacheRequest(command, true);
                }
            }

            if (res != null)
            {
                res.ParseResponse();
            }
        }

        #endregion

     

        



      
        private CommandResponse ExecuteCommand(CommandBase command)
        {
            Request request = _broker.CreateRequest(command);

            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            return res;
        }
    }
}
