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
using Alachisoft.NCache.Web.Caching.Util;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Web.Command;
using Alachisoft.NCache.Web.RemoteClient.Config;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Web.Statistics;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Caching;
using System.Collections.Generic;
using System.Diagnostics;
using AddAttributeCommand = Alachisoft.NCache.Web.Command.AddAttributeCommand;
using AddCommand = Alachisoft.NCache.Web.Command.AddCommand;
using AddDependencyCommand = Alachisoft.NCache.Web.Command.AddDependencyCommand;
using AddSyncDependencyCommand = Alachisoft.NCache.Web.Command.AddSyncDependencyCommand;
using BulkAddCommand = Alachisoft.NCache.Web.Command.BulkAddCommand;
using BulkDeleteCommand = Alachisoft.NCache.Web.Command.BulkDeleteCommand;
using BulkGetCommand = Alachisoft.NCache.Web.Command.BulkGetCommand;
using BulkInsertCommand = Alachisoft.NCache.Web.Command.BulkInsertCommand;
using BulkRemoveCommand = Alachisoft.NCache.Web.Command.BulkRemoveCommand;
using ClearCommand = Alachisoft.NCache.Web.Command.ClearCommand;
using ClientInfo = Alachisoft.NCache.Runtime.Caching.ClientInfo;
using CloseStreamCommand = Alachisoft.NCache.Web.Command.CloseStreamCommand;
using ContainsCommand = Alachisoft.NCache.Web.Command.ContainsCommand;
using CountCommand = Alachisoft.NCache.Web.Command.CountCommand;
using DeleteCommand = Alachisoft.NCache.Web.Command.DeleteCommand;
using DeleteQueryCommand = Alachisoft.NCache.Web.Command.DeleteQueryCommand;
using DisposeReaderCommand = Alachisoft.NCache.Web.Command.DisposeReaderCommand;
using EnumerationPointer = Alachisoft.NCache.Common.DataStructures.EnumerationPointer;
using Exception = System.Exception;
using GetCacheItemCommand = Alachisoft.NCache.Web.Command.GetCacheItemCommand;
using GetCommand = Alachisoft.NCache.Web.Command.GetCommand;
using GetExpirationCommand = Alachisoft.NCache.Web.Command.GetExpirationCommand;
using GetGroupCommand = Alachisoft.NCache.Web.Command.GetGroupCommand;
using GetGroupNextChunkCommand = Alachisoft.NCache.Web.Command.GetGroupNextChunkCommand;
using GetKeysByTagCommand = Alachisoft.NCache.Web.Command.GetKeysByTagCommand;
using GetNextChunkCommand = Alachisoft.NCache.Web.Command.GetNextChunkCommand;
using GetRunningTasksCommand = Alachisoft.NCache.Web.Command.GetRunningTasksCommand;
using GetStreamLengthCommand = Alachisoft.NCache.Web.Command.GetStreamLengthCommand;
using GetTagCommand = Alachisoft.NCache.Web.Command.GetTagCommand;
using GetTypeInfoMapCommand = Alachisoft.NCache.Web.Command.GetTypeInfoMapCommand;
using GroupEnumerationPointer = Alachisoft.NCache.Common.DataStructures.GroupEnumerationPointer;
using InsertCommand = Alachisoft.NCache.Web.Command.InsertCommand;
using InvokeEntryProcessorCommand = Alachisoft.NCache.Web.Command.InvokeEntryProcessorCommand;
using IsLockedCommand = Alachisoft.NCache.Web.Command.IsLockedCommand;
using LockCommand = Alachisoft.NCache.Web.Command.LockCommand;
using MapReduceTaskCommand = Alachisoft.NCache.Web.Command.MapReduceTaskCommand;
using OpenStreamCommand = Alachisoft.NCache.Web.Command.OpenStreamCommand;
using QueryResultSet = Alachisoft.NCache.Caching.Queries.QueryResultSet;
using RaiseCustomEventCommand = Alachisoft.NCache.Web.Command.RaiseCustomEventCommand;
using ReaderResultSet = Alachisoft.NCache.Common.DataReader.ReaderResultSet;
using ReadFromStreamCommand = Alachisoft.NCache.Web.Command.ReadFromStreamCommand;
using RegisterCQCommand = Alachisoft.NCache.Web.Command.RegisterCQCommand;
using RegisterPollingNotificationCommand = Alachisoft.NCache.Web.Command.RegisterPollingNotificationCommand;
using RemoveByTagCommand = Alachisoft.NCache.Web.Command.RemoveByTagCommand;
using RemoveCommand = Alachisoft.NCache.Web.Command.RemoveCommand;
using RemoveGroupCommand = Alachisoft.NCache.Web.Command.RemoveGroupCommand;
using SearchCommand = Alachisoft.NCache.Web.Command.SearchCommand;
using SearchCQCommand = Alachisoft.NCache.Web.Command.SearchCQCommand;
using TaskCallbackCommand = Alachisoft.NCache.Web.Command.TaskCallbackCommand;
using TaskCancelCommand = Alachisoft.NCache.Web.Command.TaskCancelCommand;
using TaskEnumeratorCommand = Alachisoft.NCache.Web.Command.TaskEnumeratorCommand;
using TaskProgressCommand = Alachisoft.NCache.Web.Command.TaskProgressCommand;
using UnlockCommand = Alachisoft.NCache.Web.Command.UnlockCommand;
using UnRegisterCQCommand = Alachisoft.NCache.Web.Command.UnRegisterCQCommand;
using WriteToStreamCommand = Alachisoft.NCache.Web.Command.WriteToStreamCommand;
using TouchCommand = Alachisoft.NCache.Web.Command.TouchCommand;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.MapReduce;
using Alachisoft.NCache.Web.MapReduce;
using Alachisoft.NCache.Common.MapReduce;
using Alachisoft.NCache.Runtime.Processor;

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Implements the clustered cache for an application. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// One instance of this class is created per application domain, and it remains 
    /// valid as long as the application domain remains active. This object is accessible through the 
    /// <see cref="NCache.Cache"/> property of the <see cref="NCache"/> object
    /// </remarks>
    /// <requirements>
    /// </requirements>
    internal sealed class RemoteCache : CacheImplBase, IRecordSetLoader
    {
        private Broker _broker = null;
        private Hashtable _compactTypes;
        internal Cache _parent;
        private Hashtable _encryption;

        private string _cacheId;
        private string _serverId;
        private bool _clearNotifRegistered;
        private CacheAsyncEventsListener _asyncListener;
        private CacheEventsListener _eventListener;

        PerfStatsCollector2 _perfStatsCollector;


        private bool _addNotifRegistered;
        private bool _removeNotifRegistered;
        private bool _updateNotifRegistered;
        private bool _customNotifRegistered;

        private Dictionary<string, Dictionary<IRecordSetEnumerator, Object>> validReaders =
            new Dictionary<string, Dictionary<IRecordSetEnumerator, Object>>();


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
        /// Get the server name with which this cache is connected
        /// </summary>
        internal string ServerId
        {
            get { return _serverId; }
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
        public CacheEventsListener EventListener
        {
            get { return _eventListener; }
        }

        /// <summary>
        /// Gets the Asynchronous event listener for the remote cache.
        /// </summary>
        public CacheAsyncEventsListener AsyncEventHandler
        {
            get { return _asyncListener; }
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

        public override void RegisterGeneralNotification(EventType eventType, EventDataFilter datafilter,
            short sequenceNumber)
        {
            NotificationsType notificationType;

            if ((eventType & EventType.ItemAdded) != 0)
                notificationType = NotificationsType.RegAddNotif;
            else if ((eventType & EventType.ItemRemoved) != 0)
                notificationType = NotificationsType.RegRemoveNotif;
            else if ((eventType & EventType.ItemUpdated) != 0)
                notificationType = NotificationsType.RegUpdateNotif;
            else
                return;

            RegisterNotificationCommand command =
                new RegisterNotificationCommand(notificationType, datafilter, sequenceNumber);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void UnRegisterGeneralNotification(EventType unregister, short sequenceNumber)
        {
            NotificationsType notificationType;


            if ((unregister & EventType.ItemAdded) != 0)
                notificationType = NotificationsType.UnregAddNotif;
            else if ((unregister & EventType.ItemRemoved) != 0)
                notificationType = NotificationsType.UnregRemoveNotif;
            else if ((unregister & EventType.ItemRemoved) != 0)
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
        /// Will be called by broker in case of multiple connection
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
        /// Will be called by broker in case of multiple connection
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
        /// Will be called by broker in case of multiple connection
        /// </summary>
        /// <param name="connection"></param>
        internal void RegisterUpdateEvent(Connection connection)
        {
            RegisterNotifications(NotificationsType.RegUpdateNotif, connection);
        }

        /// <summary>
        /// Will be called by broker in case of multiple connection
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
        /// Will be called by broker in case of multiple connection
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
        /// Will be called by broker in case of multiple connection
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
        /// Will be called by broker in case of multiple connection
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
        /// Re- register the general notification when reconnects to some other node.
        /// </summary>
        public void ReRegisterGeneralNotification(Connection connection)
        {
            if (_parent != null && _parent.EventManager != null)
            {
                EventRegistrationInfo[] generalEventRegistrations =
                    _parent.EventManager.GetEventRegistrationInfo();
                //Re-registers general events with new server
                if (generalEventRegistrations != null)
                {
                    for (int i = 0; i < generalEventRegistrations.Length; i++)
                    {
                        EventRegistrationInfo eventRegistration = generalEventRegistrations[i];
                        RegisterNotifications(eventRegistration.EventTYpe, eventRegistration.DataFilter,
                            eventRegistration.RegistrationSequence, connection);
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


        internal RemoteCache(string cacheId, Cache parent, CacheInitParams initParams, PerfStatsCollector2 perfStatsCol)
            : base()
        {
            _parent = parent;
            LocalClientInfo.AppName = initParams.AppName ?? Process.GetCurrentProcess().ProcessName;
            _perfStatsCollector = perfStatsCol;


            if (_parent != null)
            {
                _asyncListener = new CacheAsyncEventsListener(_parent.AsyncListener);
                _eventListener = new CacheEventsListener(_parent.EventListener);
            }

            _cacheId = cacheId;

            _broker = new Broker(this, initParams, perfStatsCol, LocalClientInfo);


            RemoteServer server = _broker.GetInitialServer();

            _broker.StartServices(cacheId, server.IpString, server.Port);

            if (!_broker.IsConnected)
                throw new OperationFailedException("No server is available to process the request for " + _cacheId);

            _serverId = initParams.Server;
        }


        internal static bool BalanceNodes(string cacheId)
        {
            return ConfigReader.ReadItemValue("client.ncconf", cacheId, "load-balance");
        }

        internal static bool ImportHashmap(string cacheId)
        {
            return true;
        }


        #region ---------------------- Indexers ----------------------

        /// <summary>
        /// Gets or sets the cache item at the specified key.
        /// </summary>
        /// <param name="key">A <c>string</c> object that represents the key for the cache item.</param>
        /// <value>The specified cache item.</value>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>You can use this property to retrieve the value of a specified 
        /// cache item, or to add an item and a key for it to the cache.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following examples demonstrates using this property to retrieve and insert the values of cached item.
        /// <code>
        /// 
        ///	void cmdReset_Click(object objSender, EventArgs objArgs)
        ///	{
        ///		txtValue.Text = NCache.Cache[txtName.Text].ToString();
        ///	}
        ///	
        ///	void cmdAdd_Click(object objSender, EventArgs objArgs)
        ///	{
        ///		if (txtName.Text != "")
        ///		{
        ///			// Add this item to the cache.
        ///			NCache.Cache[txtName.Text] = txtValue.Text;
        ///		}
        ///	}
        ///		
        ///	void cmdReset_Click(object objSender, EventArgs objArgs)
        ///	{
        ///		txtValue.Text = Cache[txtName.Text].ToString();
        ///	}
        ///	
        ///	void cmdAdd_Click(object objSender, EventArgs objArgs)
        ///	{
        ///		if (txtName.Text != "")
        ///		{
        ///			// Add this item to the cache.
        ///			Cache[txtName.Text] = txtValue.Text;
        ///		}
        ///	}
        /// 
        /// </code>
        /// </example>
        public override object this[string key]
        {
            get
            {
                LockHandle lockHandle = null;
                CacheItemVersion version = null;
                return Get(key, new BitSet(), null, null, ref version, ref lockHandle, TimeSpan.Zero,
                    LockAccessType.IGNORE_LOCK, null);
            }
            set { }
        }


        public override string Name
        {
            get { return _cacheId; }
        }

        #endregion

        #region ---------------------- Add ----------------------

        public override object Add(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority,
            short removeCallback, short updateCallback, short dsItemAddedCallback, bool isResyncExpiredItems,
            string group, string subGroup, Hashtable queryInfo, BitSet flagMap,
            string providerName, string resyncProviderName, EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, long size, string clientId)
        {
            AddCommand command = new Alachisoft.NCache.Web.Command.AddCommand(key, (byte[]) value,
                dependency,
                syncDependency,
                absoluteExpiration,
                slidingExpiration,
                priority,
                removeCallback,
                updateCallback,
                dsItemAddedCallback,
                isResyncExpiredItems,
                group,
                subGroup,
                -1,
                false, queryInfo,
                flagMap, providerName, resyncProviderName,
                CacheId,
                updateCallbackFilter,
                removeCallabackFilter, TargetMethodAttribute.MethodOverload, clientId);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
            return res.ItemVersion;
        }


        public override void AddAsync(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, short onAsyncItemAddCallback,
            short dsItemAddedCallback, bool isResyncExpiredItems, string group,
            string subGroup, Hashtable queryInfo, BitSet flagMap, string providerName, string resyncProviderName,
            EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, long size, string clientId)
        {
            AddCommand command = new AddCommand(key,
                (byte[]) value,
                dependency,
                syncDependency,
                absoluteExpiration,
                slidingExpiration,
                priority,
                onRemoveCallback,
                onUpdateCallback,
                dsItemAddedCallback,
                isResyncExpiredItems,
                group,
                subGroup,
                onAsyncItemAddCallback,
                true, queryInfo,
                flagMap, providerName, resyncProviderName,
                CacheId,
                updateCallbackFilter,
                removeCallabackFilter, TargetMethodAttribute.MethodOverload, clientId);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);
        }


        /// <summary>
        /// Add array of <see cref="CacheItem"/> to the cache.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="group">The data group of the item</param>
        /// <param name="subGroup">Sub group of the group</param>
        /// <returns>keys that are added or that already exists in the cache and their status.</returns>
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
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Add(keys, items, "Customer", "Orders");
        /// 
        /// </code>
        /// </example>
        public override IDictionary Add(string[] keys, CacheItem[] items,
            short onDataSourceItemsAdded, string providerName, long[] sizes,
            string clientId, short itemUpdatedCallback, short itemRemovedCallback,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, bool returnVersions,
            out IDictionary itemVersions, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap =
                new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                bool isMrgKeys = _broker.GetKeysDistributionMap(keys, items, ref keysDistributionMap);
                foreach (Address serverAddress in keysDistributionMap.Keys)
                {
                    KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                    BulkAddCommand command = new BulkAddCommand(keysAndItems.Key, keysAndItems.Value,
                        onDataSourceItemsAdded, _parent, providerName, CacheId,
                        TargetMethodAttribute.MethodOverload, clientId, itemUpdatedCallback, itemRemovedCallback,
                        updateCallbackFilter, removeCallabackFilter, returnVersions, callbackType);
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
                    CacheId, TargetMethodAttribute.MethodOverload, clientId,
                    itemUpdatedCallback, itemRemovedCallback, updateCallbackFilter, removeCallabackFilter,
                    returnVersions, callbackType);
                request = _broker.CreateRequest(command);
            }

            _broker.ExecuteRequest(request);
            CommandResponse res = request.Response;
            res.ParseResponse();

            itemVersions = res.KeyVersionDic;
            return res.KeyValueDic;
        }

        /// <summary>
        /// Add dependency to the cache item.
        /// </summary>
        /// <param name="key">key used to reference the required object</param>
        /// <param name="dependency">CacheDependency to be added</param>
        /// <param name="isResyncRequired">If set, then at the time of expiration, a fresh copy of the 
        /// item is fetched from the master data source provided that the read-thru is enabled. Otherwise it is 
        /// expired as normal.</param>
        /// <returns>True if operations succeeded else false</returns>
        public override bool AddDependency(string key, CacheDependency dependency, bool isResyncRequired)
        {
            AddDependencyCommand command = new AddDependencyCommand(key, dependency, isResyncRequired,
                TargetMethodAttribute.MethodOverload
            );
            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);
            CommandResponse res = request.Response;
            res.ParseResponse();
            return res.OperationWasuccessfull();
        }

        /// <summary>
        /// Add dependency to the cache item.
        /// </summary>
        /// <param name="key">key used to reference the required object</param>
        /// <param name="dependency">CacheDependency to be added</param>
        /// <param name="isResyncRequired">If set, then at the time of expiration, a fresh copy of the 
        /// item is fetched from the master data source provided that the read-thru is enabled. Otherwise it is 
        /// expired as normal.</param>
        /// <returns>True if operations succeeded else false</returns>
        public override bool AddDependency(string key, CacheSyncDependency syncDependency)
        {
            AddSyncDependencyCommand command = new AddSyncDependencyCommand(key, syncDependency,
                TargetMethodAttribute.MethodOverload
            );

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.OperationWasuccessfull();
        }

        public override bool SetAttributes(string key, CacheItemAttributes attributes)
        {
            AddAttributeCommand command = new AddAttributeCommand(key, attributes.AbsoluteExpiration,
                attributes.Dependency, TargetMethodAttribute.MethodOverload
            );

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);
            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.OperationWasuccessfull();
        }

        #endregion

        #region ---------------------- Insert ----------------------

        public override CacheItemVersion Insert(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, short onDsItemUpdatedCallback,
            bool isResyncExpiredItems, string group, string subGroup, Hashtable queryInfo, BitSet flagMap,
            object lockId, CacheItemVersion version, LockAccessType accessType,
            string providerName, string resyncProviderName, EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, long size, string clientId,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            InsertCommand command = new InsertCommand(key,
                (byte[]) value,
                dependency,
                syncDependency,
                absoluteExpiration,
                slidingExpiration,
                priority,
                onRemoveCallback,
                onUpdateCallback,
                onDsItemUpdatedCallback,
                isResyncExpiredItems,
                group,
                subGroup,
                -1,
                false, queryInfo,
                flagMap,
                lockId,
                version == null ? 0 : version.Version,
                accessType, providerName, resyncProviderName,
                CacheId, updateCallbackFilter, removeCallabackFilter, TargetMethodAttribute.MethodOverload, clientId,
                callbackType);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            return new CacheItemVersion(res.ItemVersion);
        }


        public override void InsertAsync(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, short onAsyncItemUpdateCallback,
            short onDsItemUpdatedCallback, bool isResyncExpiredItems, string group, string subGroup,
            Hashtable queryInfo, BitSet flagMap, string providerName, string resyncProviderName,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallbackFilter, long size, string clientId)
        {
            InsertCommand command = new InsertCommand(key,
                (byte[]) value,
                dependency,
                syncDependency,
                absoluteExpiration,
                slidingExpiration,
                priority,
                onRemoveCallback,
                onUpdateCallback,
                onDsItemUpdatedCallback,
                isResyncExpiredItems,
                group,
                subGroup,
                onAsyncItemUpdateCallback,
                true, queryInfo,
                flagMap,
                null,
                0,
                LockAccessType.IGNORE_LOCK,
                providerName, resyncProviderName,
                CacheId,
                updateCallbackFilter,
                removeCallbackFilter, TargetMethodAttribute.MethodOverload, clientId);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);
        }


        /// <summary> Insert list of <see cref="CacheItem"/> to the cache </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="group">The group with which this object is associated</param>
        /// <param name="subGroup">The subGroup of the group</param>
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
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Insert(keys, items, "Connection", null);
        /// 
        /// </code>
        /// </example>
        public override IDictionary Insert(string[] keys, CacheItem[] items,
            short onDsItemsUpdatedCallback, string providerName, long[] sizes,
            string clientId, short itemUpdateCallback, short itemRemovedCallback,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, bool returnVersions,
            out IDictionary itemVersions, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap =
                new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                bool isMrgKeys = _broker.GetKeysDistributionMap(keys, items, ref keysDistributionMap);
                foreach (Address serverAddress in keysDistributionMap.Keys)
                {
                    KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                    BulkInsertCommand command = new BulkInsertCommand(keysAndItems.Key, keysAndItems.Value,
                        onDsItemsUpdatedCallback, _parent, providerName, CacheId,
                        TargetMethodAttribute.MethodOverload, clientId, itemUpdateCallback, itemRemovedCallback,
                        updateCallbackFilter, removeCallabackFilter, returnVersions, callbackType);
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
                    providerName, CacheId, TargetMethodAttribute.MethodOverload,
                    clientId, itemUpdateCallback, itemRemovedCallback,
                    updateCallbackFilter, removeCallabackFilter, returnVersions, callbackType);
                request = _broker.CreateRequest(command);
            }

            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            itemVersions = res.KeyVersionDic;
            return res.KeyValueDic;
        }

        #endregion

        #region ---------------------- Cache Data Reader ----------------------

        /// <summary>
        /// Provide data reader on <see cref="Cache"/> based on the query specified.
        /// </example>
        public override IRecordSetEnumerator ExecuteReader(string query, IDictionary values, bool getData,
            int chunkSize)
        {
            ExecuteReader command =
                new ExecuteReader(query, values, getData, chunkSize, TargetMethodAttribute.MethodOverload);
            CommandResponse res = null;

            try
            {
                res = ExecuteCacheRequest(command, false);
            }
            catch (ActivityBlockedException ex)
            {
                command = new ExecuteReader(query, values, getData, chunkSize, TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true); //dedicated request in case of node down
            }

            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                command = new ExecuteReader(query, values, getData, chunkSize, TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
                res.ParseResponse();
            }

            List<ReaderResultSet> readerResultSets = null;
            readerResultSets = res.ReaderResultSets;
            List<IRecordSetEnumerator> prsEnum = new System.Collections.Generic.List<IRecordSetEnumerator>();
            foreach (ReaderResultSet resultSet in readerResultSets)
            {
                if (resultSet.RecordSet != null)
                {
                    IRecordSetEnumerator rse = resultSet.RecordSet.GetEnumerator();
                    PartitionRSEnumerator partitionRS = new PartitionRSEnumerator(rse, resultSet.ReaderID,
                        resultSet.NodeAddress, resultSet.NextIndex, this);
                    prsEnum.Add(partitionRS);
                    UpdateOpenReaders(partitionRS.Server, partitionRS);
                }
            }

            DistributedRSEnumerator drsEnum = null;
            if (readerResultSets.Count > 0)
            {
                if (readerResultSets[0].IsGrouped)
                {
                    drsEnum = new DistributedGroupedRSEnumerator(prsEnum, readerResultSets[0].OrderByArguments,
                        validReaders);
                }
                else if (readerResultSets[0].OrderByArguments != null && readerResultSets[0].OrderByArguments.Count > 0)
                {
                    drsEnum = new DistributedOrderedRSEnumerator(prsEnum, readerResultSets[0].OrderByArguments,
                        validReaders);
                }
                else
                {
                    drsEnum = new DistributedRSEnumerator(prsEnum, validReaders);
                }
            }

            return drsEnum;
        }


        public override IRecordSetEnumerator ExecuteReaderCQ(ContinuousQuery continuousQuery, bool getData,
            int chunkSize, string clientUniqueId, bool notifyAdd, bool notifyUpdate, bool notifyRemove)
        {
            ExecuteReaderCQ executeReaderCQCommand = new ExecuteReaderCQ(continuousQuery.Query, continuousQuery.Values,
                getData, chunkSize, clientUniqueId, true,
                notifyAdd, notifyUpdate, notifyRemove,
                (int) continuousQuery.MaxFilter(EventType.ItemAdded),
                (int) continuousQuery.MaxFilter(EventType.ItemRemoved),
                (int) continuousQuery.MaxFilter(EventType.ItemUpdated), TargetMethodAttribute.MethodOverload);
            CommandResponse response = null;

            try
            {
                response = ExecuteCacheRequest(executeReaderCQCommand, false);
            }
            catch (ActivityBlockedException ex)
            {
                executeReaderCQCommand = new ExecuteReaderCQ(continuousQuery.Query, continuousQuery.Values,
                    getData, chunkSize, clientUniqueId, true,
                    notifyAdd, notifyUpdate, notifyRemove,
                    (int) continuousQuery.MaxFilter(EventType.ItemAdded),
                    (int) continuousQuery.MaxFilter(EventType.ItemRemoved),
                    (int) continuousQuery.MaxFilter(EventType.ItemUpdated), TargetMethodAttribute.MethodOverload);

                response = ExecuteCacheRequest(executeReaderCQCommand, true);
            }

            try
            {
                response.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                executeReaderCQCommand = new ExecuteReaderCQ(continuousQuery.Query, continuousQuery.Values,
                    getData, chunkSize, clientUniqueId, true,
                    notifyAdd, notifyUpdate, notifyRemove,
                    (int) continuousQuery.MaxFilter(EventType.ItemAdded),
                    (int) continuousQuery.MaxFilter(EventType.ItemRemoved),
                    (int) continuousQuery.MaxFilter(EventType.ItemUpdated), TargetMethodAttribute.MethodOverload);

                response = ExecuteCacheRequest(executeReaderCQCommand, true);
                response.ParseResponse();
            }

            List<ReaderResultSet> readerResultSets = null;
            readerResultSets = response.ReaderResultSets;
            List<IRecordSetEnumerator> prsEnum = new List<IRecordSetEnumerator>();
            foreach (ReaderResultSet readerResultSet in readerResultSets)
            {
                if (readerResultSet.RecordSet != null)
                {
                    IRecordSetEnumerator recordSetEnumerator = readerResultSet.RecordSet.GetEnumerator();
                    PartitionRSEnumerator partitionRS = new PartitionRSEnumerator(recordSetEnumerator,
                        readerResultSet.ReaderID, readerResultSet.NodeAddress, readerResultSet.NextIndex, this);
                    prsEnum.Add(partitionRS);
                    UpdateOpenReaders(partitionRS.Server, partitionRS);
                }
            }

            DistributedRSEnumerator drsEnum = null;
            if (readerResultSets.Count > 0)
            {
                if (readerResultSets[0].IsGrouped)
                {
                    drsEnum = new DistributedGroupedRSEnumerator(prsEnum, readerResultSets[0].OrderByArguments,
                        validReaders);
                }
                else if (readerResultSets[0].OrderByArguments != null && readerResultSets[0].OrderByArguments.Count > 0)
                {
                    drsEnum = new DistributedOrderedRSEnumerator(prsEnum, readerResultSets[0].OrderByArguments,
                        validReaders);
                }
                else
                {
                    drsEnum = new DistributedRSEnumerator(prsEnum, validReaders);
                }
            }

            return drsEnum;
        }

        #region ---------------------- IRecordSetLoader ----------------------

        public ReaderResultSet GetRecordSet(string readerID, int nextIndex, string nodeIP, string reciepent)
        {
            GetRecordSetNextChunk command = new GetRecordSetNextChunk(readerID, reciepent, nextIndex);
            Request request;
            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                request.AddCommand(new Address(nodeIP, _broker._clientConfig.ServerPort), command);
            }
            else
            {
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
                command = new GetRecordSetNextChunk(readerID, nodeIP, nextIndex); // retry request
                res = ExecuteCacheRequest(command, false);
            }

            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                command = new GetRecordSetNextChunk(readerID, nodeIP, nextIndex); // retry request
                res = ExecuteCacheRequest(command, false);
                res.ParseResponse();
            }

            return res.ReaderNextChunk;
        }

        /// <summary>
        /// Dispose reader resources on node specified
        /// </summary>
        public void DisposeReader(string readerId, string nodeIp)
        {
            DisposeReaderCommand command =
                new DisposeReaderCommand(readerId, nodeIp, TargetMethodAttribute.MethodOverload);
            Request request;
            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                request.AddCommand(new Address(nodeIp, _broker._clientConfig.ServerPort), command);
            }
            else
            {
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
                command = new DisposeReaderCommand(readerId, nodeIp, TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, false); // retry request
            }

            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                command = new DisposeReaderCommand(readerId, nodeIp, TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, false); // retry request
                res.ParseResponse();
            }
        }

        private void UpdateOpenReaders(string address, PartitionRSEnumerator partition)
        {
            lock (validReaders)
            {
                var readers = default(Dictionary<IRecordSetEnumerator, Object>);

                if (validReaders.ContainsKey(address))
                {
                    validReaders.TryGetValue(address, out readers);
                    if (readers != null) readers.Add(partition, null);
                }
                else
                {
                    readers = new Dictionary<IRecordSetEnumerator, Object>();
                    readers.Add(partition, null);
                }

                validReaders[address] = readers;
            }
        }

        #endregion

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
            LockCommand command = new LockCommand(key, lockTimeout, System.Diagnostics.Process.GetCurrentProcess().Id,
                TargetMethodAttribute.MethodOverload);

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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
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
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Get("MyTextBox.Value", "Customer", null);
        /// 
        /// </code>
        /// </example>
        public override CompressedValueEntry Get(string key, BitSet flagMap, string group, string subGroup,
            ref CacheItemVersion version, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType,
            string providerName)
        {
            if (lockHandle == null) lockHandle = new LockHandle();
            object lockId = lockHandle.LockId;

            if (version == null) version = new CacheItemVersion();
            ulong itemVersion = version.Version;

            GetCommand command = new GetCommand(key, flagMap, group, subGroup, accessType, lockId, lockTimeout,
                itemVersion, providerName, System.Diagnostics.Process.GetCurrentProcess().Id,
                TargetMethodAttribute.MethodOverload);


            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            lockHandle.LockId = res.LockId as string;
            lockHandle.LockDate = res.LockDate;
            version = res.ItemVersion != 0 ? new CacheItemVersion(res.ItemVersion) : null;

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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// 
        ///	NCache.Cache.Get(keys);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Get(keys);
        /// 
        /// </code> 
        /// </example>
        public override IDictionary Get(string[] keys, BitSet flagMap, string providerName)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap =
                new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                bool isMrgKeys = _broker.GetKeysDistributionMap(keys, null, ref keysDistributionMap);
                foreach (Address serverAddress in keysDistributionMap.Keys)
                {
                    KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                    BulkGetCommand command = new BulkGetCommand(keysAndItems.Key, flagMap, providerName,
                        TargetMethodAttribute.MethodOverload);
                    if (isMrgKeys)
                        command.ClientLastViewId = Broker.ForcedViewId;
                    else
                        command.ClientLastViewId = _broker.ClientLastViewId;
                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                BulkGetCommand command =
                    new BulkGetCommand(keys, flagMap, providerName, TargetMethodAttribute.MethodOverload);
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
                    BulkGetCommand command = new BulkGetCommand(keys, flagMap, providerName,
                        TargetMethodAttribute.MethodOverload);
                    res = ExecuteCacheRequest(command, true);
                }
            }

            res.ParseResponse();

            return res.KeyValueDic;
        }

        /// <summary>
        /// Retrieves the keys of items in a group or sub group.
        /// </summary>
        /// <param name="group">The group whose keys are to be returned.</param>
        /// <param name="subGroup">The sub group of the group foe which keys are to be returned.</param>
        /// <returns>The list of keys of a group or a sub group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="group"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// <para>
        /// If only group is specified, keys for the group and all the sub groups of the group are returned.
        /// If both the group and sub group are specified. Only the keys related to the sub group are returned.
        /// </para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// 
        ///	ArrayList list = NCache.Cache.Get("Customer", "Orders");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	ArrayList list = Cache.Get("Customer", "Orders");
        /// 
        /// </code>
        /// </example>
        public override ArrayList GetGroupKeys(string group, string subGroup)
        {
            GetGroupCommand command = new GetGroupCommand(group, subGroup, true, TargetMethodAttribute.MethodOverload);
            CommandResponse res = null;

            try
            {
                res = ExecuteCacheRequest(command, false);
            }
            catch (ActivityBlockedException ex)
            {
                command = new GetGroupCommand(group, subGroup, true, TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
            }

            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                command = new GetGroupCommand(group, subGroup, true, TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
                res.ParseResponse();
            }

            return res.KeyList;
        }

        /// <summary>
        /// Retrieves the key and value pairs in a group or sub group.
        /// </summary>
        /// <param name="group">The group whose data is to be returned.</param>
        /// <param name="subGroup">The sub group of the group for which data is to be returned.</param>
        /// <returns>The list of key and value pairs of a group or a sub group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="group"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// <para>
        /// If only group is specified, data for the group and all the sub groups of the group are returned.
        /// If both the group and sub group are specified. Only the data related to the sub group are returned.
        /// </para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// 
        ///	Hashtable table = NCache.Cache.Get("Customer", "Orders");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Hashtable table = Cache.Get("Customer", "Orders");
        /// 
        /// </code>
        /// </example>
        public override IDictionary GetGroupData(string group, string subGroup)
        {
            GetGroupCommand command = new GetGroupCommand(group, subGroup, false, TargetMethodAttribute.MethodOverload);
            CommandResponse res = null; // request.Response;
            try
            {
                res = ExecuteCacheRequest(command, false);
            }
            catch (ActivityBlockedException ex)
            {
                command = new GetGroupCommand(group, subGroup, false, TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
            }

            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                command = new GetGroupCommand(group, subGroup, false, TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
                res.ParseResponse();
            }

            return res.KeyValueDic;
        }


        public override object GetCacheItem(string key, BitSet flagMap, string group, string subGroup,
            ref CacheItemVersion version, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType,
            string providerName)
        {
            if (lockHandle == null) lockHandle = new LockHandle();
            object lockId = lockHandle.LockId;

            if (version == null) version = new CacheItemVersion();
            ulong itemVersion = version.Version;


            GetCacheItemCommand command = new GetCacheItemCommand(key, flagMap, group, subGroup, accessType, lockId,
                lockTimeout, itemVersion, providerName, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            lockHandle.LockId = res.LockId as string;
            lockHandle.LockDate = res.LockDate;
            if (res.Item != null)
            {
                version = res.Item.Version;
            }

            return res.Item;
        }


        public override Hashtable GetByTag(Tag[] tags, TagComparisonType comparisonType)
        {
            GetTagCommand command = new GetTagCommand(CacheHelper.GetTags(tags), comparisonType,
                TargetMethodAttribute.MethodOverload);
            CommandResponse res = null;
            try
            {
                res = ExecuteCacheRequest(command, false);
            }
            catch (ActivityBlockedException ex)
            {
                command = new GetTagCommand(CacheHelper.GetTags(tags), comparisonType,
                    TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
            }

            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                command = new GetTagCommand(CacheHelper.GetTags(tags), comparisonType,
                    TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
                res.ParseResponse();
            }

            return res.KeyValueDic;
        }

        public override ICollection GetKeysByTag(Tag[] tags, TagComparisonType comparisonType)
        {
            GetKeysByTagCommand command = new GetKeysByTagCommand(CacheHelper.GetTags(tags), comparisonType,
                TargetMethodAttribute.MethodOverload);
            CommandResponse res = null;
            try
            {
                res = ExecuteCacheRequest(command, false);
            }
            catch (ActivityBlockedException ex)
            {
                command = new GetKeysByTagCommand(CacheHelper.GetTags(tags), comparisonType,
                    TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
            }

            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                command = new GetKeysByTagCommand(CacheHelper.GetTags(tags), comparisonType,
                    TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
                res.ParseResponse();
            }

            return res.KeyList;
        }


        public override void RemoveByTag(Tag[] tags, TagComparisonType comparisonType)
        {
            RemoveByTagCommand command = new RemoveByTagCommand(CacheHelper.GetTags(tags), comparisonType,
                TargetMethodAttribute.MethodOverload);
            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);
            CommandResponse res = request.Response;
            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                command = new RemoveByTagCommand(CacheHelper.GetTags(tags), comparisonType,
                    TargetMethodAttribute.MethodOverload);
                request = _broker.CreateDedicatedRequest(command);
                _broker.ExecuteRequest(request);
                res = request.Response;
                res.ParseResponse();
            }
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
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
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
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
            ContainsCommand command = new ContainsCommand(key, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.Exists;
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove("timestamp");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove("timestamp");
        /// 
        /// </code>
        /// </example>
        public override CompressedValueEntry Remove(string key, BitSet flagMap, short onDsItemRemovedCallback,
            object lockId, CacheItemVersion version, LockAccessType accessType, string ProviderName)
        {
            RemoveCommand command = new RemoveCommand(key, flagMap, -1, false, onDsItemRemovedCallback, lockId,
                version == null ? 0 : version.Version, accessType, ProviderName, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.FlagValueEntry;
        }

        /// <summary>
        /// Removes the object from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove("timestamp");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove("timestamp");
        /// 
        /// </code>
        /// </example>
        public override void Delete(string key, BitSet flagMap, short onDsItemRemovedCallback, object lockId,
            CacheItemVersion version, LockAccessType accessType, string ProviderName)
        {
            DeleteCommand command = new DeleteCommand(key, flagMap, -1, false, onDsItemRemovedCallback, lockId,
                version == null ? 0 : version.Version, accessType, ProviderName, TargetMethodAttribute.MethodOverload);

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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove(keys);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove(keys);
        /// 
        /// </code>
        /// </example>
        public override IDictionary Remove(string[] keys, BitSet flagMap, string providerName,
            short onDsItemRemovedCallback)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap =
                new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
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
                BulkRemoveCommand command = new BulkRemoveCommand(keys, flagMap, providerName, onDsItemRemovedCallback,
                    TargetMethodAttribute.MethodOverload);
                request = _broker.CreateRequest(command);
            }

            _broker.ExecuteRequest(request);
            CommandResponse res = request.Response;
            res.ParseResponse();

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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove(keys);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove(keys);
        /// 
        /// </code>
        /// </example>
        public override void Delete(string[] keys, BitSet flagMap, string providerName, short onDsItemRemovedCallback)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap =
                new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
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
                BulkDeleteCommand command = new BulkDeleteCommand(keys, flagMap, providerName, onDsItemRemovedCallback,
                    TargetMethodAttribute.MethodOverload);
                request = _broker.CreateRequest(command);
            }

            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        /// <summary>
        /// Removes the object from the <see cref="Cache"/> asynchronously.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <returns>The item removed from the Cache. If the value in the key parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// This is similar to <see cref="Remove"/> except that the operation is performed asynchronously.
        /// A <see cref="ItemRemoved"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.RemoveAsync("timestamp");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.RemoveAsync("timestamp");
        /// 
        /// </code>
        /// </example>
        public override void RemoveAsync(string key, BitSet flagMap, short onDsItemRemovedCallback)
        {
            RemoveCommand command = new RemoveCommand(key, flagMap, -1, true, onDsItemRemovedCallback, null, 0,
                LockAccessType.IGNORE_LOCK, null, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);
        }

        /// <summary>
        /// Removes the object from the <see cref="Cache"/> asynchronously.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="onAsyncItemRemoveCallback">The delegate that can be used by the client application to get the
        /// result of the Asynchronous Remove operation.</param>
        /// <returns>The item removed from the Cache. If the value in the key parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// This is similar to <see cref="Remove"/> except that the operation is performed asynchronously.
        /// A <see cref="ItemRemoved"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        /// OnAsyncItemRemoved(string key, object result)
        /// {
        /// ...
        /// }
        /// 
        ///	NCache.Cache.RemoveAsync("timestamp", new AsyncItemRemovedCallback(OnAsyncItemRemoved));
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.RemoveAsync("timestamp", new AsyncItemRemovedCallback(OnAsyncItemRemoved));
        /// 
        /// </code>
        /// </example>
        public override void RemoveAsync(string key, BitSet flagMap, short onAsyncItemRemoveCallback,
            short onDsItemRemovedCallback, string providerName)
        {
            RemoveCommand command = new RemoveCommand(key, flagMap, onAsyncItemRemoveCallback, true,
                onDsItemRemovedCallback, null, 0, LockAccessType.IGNORE_LOCK, providerName,
                TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);
        }


        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        public override void Remove(string group, string subGroup)
        {
            RemoveGroupCommand command = new RemoveGroupCommand(group, subGroup, TargetMethodAttribute.MethodOverload);
            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);
            CommandResponse res = request.Response;
            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                command = new RemoveGroupCommand(group, subGroup, TargetMethodAttribute.MethodOverload);
                request = _broker.CreateDedicatedRequest(command);
                _broker.ExecuteRequest(request);
                res = request.Response;
                res.ParseResponse();
            }
        }

        #endregion

        #region ---------------------- Clear ----------------------

        /// <summary>
        /// Removes all elements from the <see cref="Cache"/>.
        /// </summary>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to check for containment of an item in the <see cref="Cache"/>.
        /// <code>
        /// 
        ///	NCache.Cache.Clear();
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Clear();
        /// 
        /// </code>
        /// </example>
        public override void Clear(BitSet flagMap, short onDsClearedCallback, string providerName)
        {
            ClearCommand command = new ClearCommand(-1, false, flagMap, onDsClearedCallback, providerName,
                TargetMethodAttribute.MethodOverload);

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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to clear the <see cref="Cache"/>.
        /// <code>
        /// 
        ///	NCache.Cache.ClearAsync();
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.ClearAsync();
        /// 
        /// </code>
        /// </example>
        public override void ClearAsync(BitSet flagMap, short onDsClearedCallback, string providerName)
        {
            ClearCommand command = new ClearCommand(-1, true, flagMap, onDsClearedCallback, providerName,
                TargetMethodAttribute.MethodOverload);

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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
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
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.ClearAsync(new AsyncCacheClearedCallback(OnAsyncCacheCleared));
        /// 
        /// </code>
        /// </example>
        public override void ClearAsync(BitSet flagMap, short onAsyncCacheClearCallback, short onDsClearedCallback,
            string providerName)
        {
            ClearCommand command = new ClearCommand(onAsyncCacheClearCallback, true, flagMap, onDsClearedCallback,
                providerName, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);
        }

        #endregion

        #region ---------------------- Search ----------------------

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax to query objects from cache</param>
        /// <returns>Returns a list of cache keys</returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , <> , < , > , <=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// 
        /// <code>
        /// 
        /// "select Test.Application.Employee where this.Name = 'Paul Jones'"
        /// "select Test.Application.Employee where this.Salary > 2000"
        /// "select Test.Application.Employee where this.Name = 'Paul Jones' and this.Salary > 2000"
        /// "select Test.Application.Employee where Not(this.Name = 'Paul Jones' and this.Salary > 2000)"
        /// 
        /// </code>
        /// </example>
        public override QueryResultSet Search(string query, IDictionary values)
        {
            SearchCommand command = new SearchCommand(query, values, false, TargetMethodAttribute.MethodOverload);
            CommandResponse res = null;

            try
            {
                res = ExecuteCacheRequest(command, false);
            }
            catch (ActivityBlockedException ex)
            {
                command = new SearchCommand(query, values, false, TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
            }

            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                command = new SearchCommand(query, values, false, TargetMethodAttribute.MethodOverload);

                res = ExecuteCacheRequest(command, true);
                res.ParseResponse();
            }

            return res.ResultSet;
        }

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax to query objects from cache</param>
        /// <returns>Returns a list of cache keys</returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , <> , < , > , <=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// 
        /// <code>
        /// 
        /// "select Test.Application.Employee where this.Name = 'Paul Jones'"
        /// "select Test.Application.Employee where this.Salary > 2000"
        /// "select Test.Application.Employee where this.Name = 'Paul Jones' and this.Salary > 2000"
        /// "select Test.Application.Employee where Not(this.Name = 'Paul Jones' and this.Salary > 2000)"
        /// 
        /// </code>
        /// </example>
        public override QueryResultSet SearchCQ(ContinuousQuery query, string clientUniqueId, bool notifyAdd,
            bool notifyUpdate, bool notifyRemove)
        {
            SearchCQCommand command = new SearchCQCommand(query.Query, query.Values, clientUniqueId, false, notifyAdd,
                notifyUpdate, notifyRemove
                , (int) query.MaxFilter(EventType.ItemAdded)
                , (int) query.MaxFilter(EventType.ItemRemoved)
                , (int) query.MaxFilter(EventType.ItemUpdated), TargetMethodAttribute.MethodOverload);
            CommandResponse res = null;

            try
            {
                res = ExecuteCacheRequest(command, false);
            }
            catch (ActivityBlockedException ex)
            {
                command = new SearchCQCommand(query.Query, query.Values, clientUniqueId, false, notifyAdd, notifyUpdate,
                    notifyRemove
                    , (int) query.MaxFilter(EventType.ItemAdded)
                    , (int) query.MaxFilter(EventType.ItemRemoved)
                    , (int) query.MaxFilter(EventType.ItemUpdated), TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
            }

            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                command = new SearchCQCommand(query.Query, query.Values, clientUniqueId, false, notifyAdd, notifyUpdate,
                    notifyRemove
                    , (int) query.MaxFilter(EventType.ItemAdded)
                    , (int) query.MaxFilter(EventType.ItemRemoved)
                    , (int) query.MaxFilter(EventType.ItemUpdated), TargetMethodAttribute.MethodOverload);

                res = ExecuteCacheRequest(command, true);
                res.ParseResponse();
            }

            return res.ResultSet;
        }

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax to query objects from cache</param>
        /// <returns>Returns a dictionary containing cache keys and associated objects</returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , <> , < , > , <=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// 
        /// <code>
        /// 
        /// "select Test.Application.Employee where this.Name = 'Paul Jones'"
        /// "select Test.Application.Employee where this.Salary > 2000"
        /// "select Test.Application.Employee where this.Name = 'Paul Jones' and this.Salary > 2000"
        /// "select Test.Application.Employee where Not(this.Name = 'Paul Jones' and this.Salary > 2000)"
        /// 
        /// </code>
        /// </example>
        public override QueryResultSet SearchEntries(string query, IDictionary values)
        {
            SearchCommand command = new SearchCommand(query, values, true, TargetMethodAttribute.MethodOverload);
            Request request = _broker.CreateRequest(command);
            CommandResponse res = null;

            try
            {
                res = ExecuteCacheRequest(command, false);
            }
            catch (ActivityBlockedException ex)
            {
                command = new SearchCommand(query, values, true, TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
            }

            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                command = new SearchCommand(query, values, true, TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
                res.ParseResponse();
            }

            return res.ResultSet;
        }

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax to query objects from cache</param>
        /// <returns>Returns a dictionary containing cache keys and associated objects</returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , <> , < , > , <=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// 
        /// <code>
        /// 
        /// "select Test.Application.Employee where this.Name = 'Paul Jones'"
        /// "select Test.Application.Employee where this.Salary > 2000"
        /// "select Test.Application.Employee where this.Name = 'Paul Jones' and this.Salary > 2000"
        /// "select Test.Application.Employee where Not(this.Name = 'Paul Jones' and this.Salary > 2000)"
        /// 
        /// </code>
        /// </example>
        public override QueryResultSet SearchEntriesCQ(ContinuousQuery query, string clientUniqueId, bool notifyAdd,
            bool notifyUpdate, bool notifyRemove)
        {
            SearchCQCommand command = new SearchCQCommand(query.Query, query.Values, clientUniqueId, true, notifyAdd,
                notifyUpdate, notifyRemove
                , (int) query.MaxFilter(EventType.ItemAdded)
                , (int) query.MaxFilter(EventType.ItemRemoved)
                , (int) query.MaxFilter(EventType.ItemUpdated), TargetMethodAttribute.MethodOverload);
            CommandResponse res = null;

            try
            {
                res = ExecuteCacheRequest(command, false);
            }
            catch (ActivityBlockedException ex)
            {
                command = new SearchCQCommand(query.Query, query.Values, clientUniqueId, true, notifyAdd, notifyUpdate,
                    notifyRemove
                    , (int) query.MaxFilter(EventType.ItemAdded)
                    , (int) query.MaxFilter(EventType.ItemRemoved)
                    , (int) query.MaxFilter(EventType.ItemUpdated), TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
            }

            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                command = new SearchCQCommand(query.Query, query.Values, clientUniqueId, true, notifyAdd, notifyUpdate,
                    notifyRemove
                    , (int) query.MaxFilter(EventType.ItemAdded)
                    , (int) query.MaxFilter(EventType.ItemRemoved)
                    , (int) query.MaxFilter(EventType.ItemUpdated), TargetMethodAttribute.MethodOverload);

                res = ExecuteCacheRequest(command, true);
                res.ParseResponse();
            }

            return res.ResultSet;
        }

        #endregion

        #region ------------ Delete/RemoveQuery -----------

        /// <summary>
        /// Remove on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax to query objects from cache</param>
        /// <returns>Returns a dictionary containing removed cache keys and associated objects</returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , <> , < , > , <=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// 
        /// <code>
        /// 
        /// "delete Test.Application.Employee where this.Name = 'Paul Jones'"
        /// "delete Test.Application.Employee where this.Salary > 2000"
        /// "delete Test.Application.Employee where this.Name = 'Paul Jones' and this.Salary > 2000"
        /// "delete Test.Application.Employee where Not(this.Name = 'Paul Jones' and this.Salary > 2000)"
        /// 
        /// </code>
        /// </example>
        public override int ExecuteNonQuery(string query, IDictionary values)
        {
            Request request;
            CommandResponse res;
            DeleteQueryCommand command =
                new DeleteQueryCommand(query, values, true, TargetMethodAttribute.MethodOverload);
            try
            {
                request = _broker.CreateRequest(command);
                _broker.ExecuteRequest(request);
                res = request.Response;
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                command = new DeleteQueryCommand(query, values, true, TargetMethodAttribute.MethodOverload);
                request = _broker.CreateDedicatedRequest(command);
                _broker.ExecuteRequest(request);
                res = request.Response;
                res.ParseResponse();
            }

            return res.RemovedKeyCount;
        }

        #endregion

        public override object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag,
            CacheImplBase cacheImpl, ref long size)
        {
            if (serializableObject != null && cacheImpl.SerializationEnabled)
            {
                Type type = serializableObject.GetType();

                if (typeof(byte[]).Equals(type) && flag != null)
                {
                    flag.SetBit(BitSetConstants.BinaryData);
                    size = serializableObject is byte[] ? ((byte[]) serializableObject).Length : 0;
                    return serializableObject;
                }

                serializableObject = CompactBinaryFormatter.ToByteBuffer(serializableObject, serializationContext);
                size = serializableObject is byte[] ? ((byte[]) serializableObject).Length : 0;
            }

            return serializableObject;
        }

        public override object SafeDeserialize(object serializedObject, string serializationContext, BitSet flag,
            CacheImplBase cacheImpl)
        {
            object deserialized = serializedObject;

            try
            {
                if (serializedObject is byte[] && cacheImpl.SerializationEnabled)
                {
                    if (flag != null && flag.IsBitSet(BitSetConstants.BinaryData)) return serializedObject;

                    deserialized =
                        CompactBinaryFormatter.FromByteBuffer((byte[]) serializedObject, serializationContext);
                }
            }
            catch (Exception exception)
            {
                //Kill the exception; it is possible that object was serialized by Java
                //or from any other domain which can not be deserialized by us.
                deserialized = serializedObject;
            }

            return deserialized;
        }


        /// <summary>
        /// Broadcasts a custom application defined event.
        /// </summary>
        /// <param name="notifId">Application specific notification code/id</param>
        /// <param name="data">Application specific data</param>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
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
            RaiseCustomEventCommand command =
                new RaiseCustomEventCommand(notifId, data, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }


        /// <summary>
        /// Decrements the reference count of the cache and performs application-defined tasks 
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
                if (validReaders != null && validReaders.Count > 0)
                {
                    foreach (var innnerDict in validReaders)
                    {
                        foreach (var obj in innnerDict.Value)
                        {
                            obj.Key.Dispose();
                        }
                    }
                }

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

        private void RegisterNotifications(EventType eventType, EventDataFilter dataFilter, short sequenceNumber,
            Connection connection)
        {
            NotificationsType notificationType;

            if ((eventType & EventType.ItemAdded) != 0)
                notificationType = NotificationsType.RegAddNotif;
            else if ((eventType & EventType.ItemRemoved) != 0)
                notificationType = NotificationsType.RegRemoveNotif;
            else if ((eventType & EventType.ItemUpdated) != 0)
                notificationType = NotificationsType.RegUpdateNotif;
            else
                return;

            RegisterNotificationCommand command =
                new RegisterNotificationCommand(notificationType, dataFilter, sequenceNumber);

            Request request = new Request(false, _broker.OperationTimeOut);
            Address ipAddress = connection.ServerAddress;
            request.AddCommand(ipAddress, command);

            _broker.ExecuteRequest(request, connection, true, true);


            CommandResponse res = request.Response;
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
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
                        RemoteServer server = (RemoteServer) _broker.ClientServerList[i];
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

        #region/            --- Key base notification registration ---      /

        public override void RegisterKeyNotificationCallback(string key, short updateCallbackid, short removeCallbackid,
            bool notifyOnitemExpiration)
        {
            CommandBase command =
                new RegisterKeyNotificationCommand(key, updateCallbackid, removeCallbackid, notifyOnitemExpiration);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void UnRegisterKeyNotificationCallback(string key, short updateCallbackid,
            short removeCallbackid)
        {
            CommandBase command = new UnRegisterKeyNotificationCommand(key, updateCallbackid, removeCallbackid);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void RegisterKeyNotificationCallback(string key, short update, short remove,
            EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
            CommandBase command =
                new RegisterKeyNotificationCommand(key, update, remove, datafilter, notifyOnItemExpiration);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void RegisterKeyNotificationCallback(string key, short update, short remove,
            EventDataFilter datafilter, bool notifyOnItemExpiration,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            CommandBase command = new RegisterKeyNotificationCommand(key, update, remove, datafilter,
                notifyOnItemExpiration, callbackType);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void RegisterKeyNotificationCallback(string[] key, short update, short remove,
            EventDataFilter datafilter, bool notifyOnItemExpiration,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            CommandBase command = new RegisterBulkKeyNotificationCommand(key, update, remove, datafilter,
                notifyOnItemExpiration, callbackType);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void UnRegisterKeyNotificationCallback(string key, short update, short remove,
            EventType eventType)
        {
            CommandBase command = new UnRegisterKeyNotificationCommand(key, update, remove);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void UnRegisterKeyNotificationCallback(string[] key, short update, short remove,
            EventType eventType)
        {
            CommandBase command = new UnRegisterBulkKeyNotificationCommand(key, update, remove);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }


        public override void RegisterKeyNotificationCallback(string[] key, short update, short remove,
            EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
            CommandBase command =
                new RegisterBulkKeyNotificationCommand(key, update, remove, datafilter, notifyOnItemExpiration);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void RegisterKeyNotificationCallback(string[] keys, short updateCallbackid,
            short removeCallbackid, string clientId, CallbackType callbackType = CallbackType.PullBasedCallback)
        {
            CommandBase command =
                new RegisterBulkKeyNotificationCommand(keys, updateCallbackid, removeCallbackid, clientId,
                    callbackType);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void UnRegisterKeyNotificationCallback(string[] keys, short updateCallbackid,
            short removeCallbackid)
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


        public override Hashtable GetExpirationInfo()
        {
            return _expirations;
        }


        #region /                   ---- Stream Operations ---          /

        public override string OpenStream(string key, StreamModes mode, string group, string subGroup,
            DateTime absExpiration, TimeSpan slidingExpiration, CacheDependency dependency, CacheItemPriority priority)
        {
            OpenStreamCommand command = new OpenStreamCommand(key, mode, group, subGroup, absExpiration,
                slidingExpiration, dependency, priority, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.LockId as string;
        }


        public override void CloseStream(string key, string lockHandle)
        {
            CloseStreamCommand command = new CloseStreamCommand(key, lockHandle, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }


        public override int ReadFromStream(ref byte[] buffer, string key, string lockHandle, int offset,
            int streamOffset, int length)
        {
            ReadFromStreamCommand command = new ReadFromStreamCommand(key, lockHandle, streamOffset, length);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            if (res.BytesRead > 0)
            {
                VirtualArray vBuffer = new VirtualArray(res.DataList);
                vBuffer.CopyData(buffer, offset, length);
            }

            return res.BytesRead;
        }


        public override void WriteToStream(string key, string lockHandle, byte[] buffer, int srcOffset, int dstOffset,
            int length)
        {
            WriteToStreamCommand command =
                new WriteToStreamCommand(key, lockHandle, srcOffset, dstOffset, length, buffer);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }


        public override long GetStreamLength(string key, string lockHandle)
        {
            GetStreamLengthCommand command = new GetStreamLengthCommand(key, lockHandle);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.StreamLegnth;
        }

        #endregion

        #region MapReduce Methods

        private object ValidateValue(object value)
        {
            string valueString = String.Empty;

            if (value == null)
                throw new System.Exception("NCache query does not support null values");
            if (value is IList)
            {
                foreach (object obj in (IList) value)
                {
                    if (obj == null)
                        throw new System.Exception("NCache query does not support null values");
                }
            }

            return value;
        }

        private Hashtable ValidateQueryParameters(Hashtable paramms)
        {
            Hashtable newParams = new Hashtable();
            IDictionaryEnumerator ie = paramms.GetEnumerator();
            while (ie.MoveNext())
            {
                newParams.Add(ie.Key, ValidateValue(ie.Value));
            }

            return newParams;
        }

        public override void ExecuteMapReduceTask(MapReduceTask task, string taskId, MapReduce.MROutputOption option,
            short callbackId, IKeyFilter keyFilter, string query, Hashtable parameters)
        {
            long size = 0;
            BitSet set = new BitSet();

            if (parameters != null)
                parameters = ValidateQueryParameters(parameters);

            object serializedMapper = null;
            object serializedReducer = null;
            object serializedCombiner = null;
            object serializedFilter = null;
            object serializedInput = null;
            object serializedParameters = null;
            if (task != null)
            {
                if (task.Mapper != null)
                    serializedMapper = SafeSerialize(task.Mapper, this.CacheId, ref set, this, ref size);
                if (task.Reducer != null)
                    serializedReducer = SafeSerialize(task.Reducer, this.CacheId, ref set, this, ref size);
                if (task.Combiner != null)
                    serializedCombiner = SafeSerialize(task.Combiner, this.CacheId, ref set, this, ref size);
                if (task.InputProvider != null)
                    serializedInput = SafeSerialize(task.InputProvider, this.CacheId, ref set, this, ref size);
            }

            if (keyFilter != null)
                serializedFilter = SafeSerialize(keyFilter, this.CacheId, ref set, this, ref size);
            if (parameters != null)
                serializedParameters = SafeSerialize(parameters, this.CacheId, ref set, this, ref size);

            CommandBase command = new MapReduceTaskCommand(
                serializedMapper, serializedReducer, serializedCombiner, serializedInput, taskId, option, callbackId,
                serializedFilter, query, serializedParameters, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            try
            {
                _broker.ExecuteRequest(request);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }

            CommandResponse response = request.Response;
            if (response != null)
            {
                response.ParseResponse();
            }
        }

        public override void RegisterMapReduceCallbackListener(short callbackId, string taskId)
        {
            TaskCallbackCommand command = new TaskCallbackCommand(taskId, callbackId);
            Request request = _broker.CreateRequest(command);
            try
            {
                _broker.ExecuteRequest(request);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }

            CommandResponse response = request.Response;
            if (response != null)
            {
                response.ParseResponse();
            }
        }

        public override void CancelTask(string taskId)
        {
            TaskCancelCommand cancelCommand = new TaskCancelCommand(taskId, false);
            Request request = _broker.CreateRequest(cancelCommand);
            try
            {
                _broker.ExecuteRequest(request);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }

            CommandResponse response = request.Response;
            if (response != null)
            {
                response.ParseResponse();
            }
        }

        public override ArrayList GetRunningTasks()
        {
            GetRunningTasksCommand command = new GetRunningTasksCommand();
            Request request = _broker.CreateRequest(command);
            try
            {
                _broker.ExecuteRequest(request);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }

            CommandResponse response = request.Response;
            if (response != null)
            {
                response.ParseResponse();
                return response.RunningTasks;
            }

            return null;
        }


        public override Runtime.MapReduce.TaskStatus GetTaskProgress(string taskId)
        {
            TaskProgressCommand command = new TaskProgressCommand(taskId);
            Request request = _broker.CreateRequest(command);
            try
            {
                _broker.ExecuteRequest(request);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }

            CommandResponse response = request.Response;
            if (response != null)
            {
                response.ParseResponse();
                return response.TaskProgress;
            }

            return null;
        }

        public override IDictionaryEnumerator GetTaskEnumerator(string taskId, short callbackId)
        {
            CommandResponse res = null;
            TaskEnumeratorCommand taskEnumeratorCommand = new TaskEnumeratorCommand(taskId, callbackId);
            Request request = _broker.CreateRequest(taskEnumeratorCommand);
            try
            {
                _broker.ExecuteRequest(request);
                res = request.Response;
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }

            if (res != null)
            {
                res.ParseResponse();
            }

            List<TaskEnumeratorResult> enumeratorResultSet = res.TaskEnumerator;
            TaskEnumerator mrResultEnumerator = new TaskEnumerator(enumeratorResultSet, this);
            return mrResultEnumerator;
        }

        public override Common.MapReduce.TaskEnumeratorResult NextRecord(string serverAddress,
            Common.MapReduce.TaskEnumeratorPointer pointer)
        {
            CommandResponse res = null;
            TaskNextRecordCommand nextRecordCommand = new TaskNextRecordCommand(pointer.ClientId, pointer.TaskId,
                pointer.CallbackId, pointer.ClientAddress, pointer.ClusterAddress);
            Request request = _broker.CreateRequestOnServer(serverAddress, nextRecordCommand);
            try
            {
                _broker.ExecuteRequest(request);
                res = request.Response;
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }

            if (res != null)
            {
                res.ParseResponse();
            }

            TaskEnumeratorResult enumeratorResultSet = res.TaskNextRecord;
            return enumeratorResultSet;
        }

        public override void Dispose(string serverAddress)
        {
        }

        #endregion

        #region Entry Processor Methods

        public override Hashtable InvokeEntryProcessor(string[] keys, IEntryProcessor entryProcessor,
            string defaultReadThru, string defaultWriteThru, params object[] arguments)
        {
            List<byte[]> argumentList = null;
            byte[] ep = null;

            long size = long.MinValue;
            BitSet flag = new BitSet();

            if (entryProcessor != null)
            {
                ep = (byte[]) SafeSerialize(entryProcessor, this.CacheId, ref flag, this, ref size);
            }

            if (arguments != null && arguments.Length > 0)
            {
                argumentList = new List<byte[]>();
                foreach (Object obj in arguments)
                {
                    argumentList.Add((byte[]) SafeSerialize(obj, this.CacheId, ref flag, this, ref size));
                }
            }

            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap =
                new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;

            if (_broker.ImportHashmap && _broker.PoolFullyConnected)
            {
                request = new Request(true, _broker.OperationTimeOut);
                _broker.GetKeysDistributionMap(keys, null, ref keysDistributionMap);
                foreach (Address serverAddress in keysDistributionMap.Keys)
                {
                    KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                    InvokeEntryProcessorCommand command = new InvokeEntryProcessorCommand(keysAndItems.Key, ep,
                        argumentList, new BitSet(), defaultReadThru, new BitSet(), defaultWriteThru,
                        TargetMethodAttribute.MethodOverload);
                    command.ClientLastViewId = _broker.ClientLastViewId;
                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                InvokeEntryProcessorCommand command = new InvokeEntryProcessorCommand(keys, ep, argumentList,
                    new BitSet(), defaultReadThru, new BitSet(), defaultWriteThru,
                    TargetMethodAttribute.MethodOverload);
                request = _broker.CreateRequest(command);
            }

            CommandResponse response = null;
            try
            {
                _broker.ExecuteRequest(request);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }

            response = request.Response;

            response.ParseResponse();

            return (Hashtable) response.KeyValueDic;
        }

        internal override Hashtable InvokeEntryProcessor(string[] keys, IEntryProcessor entryProcessor,
            string readThruProviderName, BitSet dsReadOptionFlag, string writeThruProviderName,
            BitSet dsWriteOptionFlag, params object[] arguments)
        {
            List<byte[]> argumentList = null;
            byte[] ep = null;

            long size = long.MinValue;
            BitSet flag = new BitSet();

            if (entryProcessor != null)
            {
                ep = (byte[]) SafeSerialize(entryProcessor, this.CacheId, ref flag, this, ref size);
            }

            if (arguments != null && arguments.Length > 0)
            {
                argumentList = new List<byte[]>();
                foreach (Object obj in arguments)
                {
                    argumentList.Add((byte[]) SafeSerialize(obj, this.CacheId, ref flag, this, ref size));
                }
            }

            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap =
                new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;

            if (_broker.ImportHashmap && _broker.PoolFullyConnected)
            {
                request = new Request(true, _broker.OperationTimeOut);
                _broker.GetKeysDistributionMap(keys, null, ref keysDistributionMap);
                foreach (Address serverAddress in keysDistributionMap.Keys)
                {
                    KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                    InvokeEntryProcessorCommand command = new InvokeEntryProcessorCommand(keysAndItems.Key, ep,
                        argumentList, dsReadOptionFlag, readThruProviderName, dsWriteOptionFlag, writeThruProviderName,
                        TargetMethodAttribute.MethodOverload);
                    command.ClientLastViewId = _broker.ClientLastViewId;
                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                InvokeEntryProcessorCommand command = new InvokeEntryProcessorCommand(keys, ep, argumentList,
                    dsReadOptionFlag, readThruProviderName, dsWriteOptionFlag, writeThruProviderName,
                    TargetMethodAttribute.MethodOverload);
                request = _broker.CreateRequest(command);
            }

            CommandResponse response = null;
            try
            {
                _broker.ExecuteRequest(request);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }

            response = request.Response;

            response.ParseResponse();

            return (Hashtable) response.KeyValueDic;
        }

        #endregion

        public override string RegisterCQ(ContinuousQuery query, string clientUniqueId, bool notifyAdd,
            bool notifyUpdate, bool notifyRemove)
        {
            RegisterCQCommand command = new RegisterCQCommand(query.Query, query.Values, clientUniqueId, notifyAdd,
                notifyUpdate, notifyRemove
                , (int) query.MaxFilter(EventType.ItemAdded)
                , (int) query.MaxFilter(EventType.ItemRemoved)
                , (int) query.MaxFilter(EventType.ItemUpdated), TargetMethodAttribute.MethodOverload);


            CommandResponse res = null;

            try
            {
                res = ExecuteCacheRequest(command, false);
            }
            catch (ActivityBlockedException ex)
            {
                command = new RegisterCQCommand(query.Query, query.Values, clientUniqueId, notifyAdd, notifyUpdate,
                    notifyRemove
                    , (int) query.MaxFilter(EventType.ItemAdded)
                    , (int) query.MaxFilter(EventType.ItemRemoved)
                    , (int) query.MaxFilter(EventType.ItemUpdated), TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);
            }

            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                SetDedicatedCall();
                command = new RegisterCQCommand(query.Query, query.Values, clientUniqueId, notifyAdd, notifyUpdate,
                    notifyRemove
                    , (int) query.MaxFilter(EventType.ItemAdded)
                    , (int) query.MaxFilter(EventType.ItemRemoved)
                    , (int) query.MaxFilter(EventType.ItemUpdated), TargetMethodAttribute.MethodOverload);
                res = ExecuteCacheRequest(command, true);

                res.ParseResponse();
            }

            return res.QueryId;
        }

        public override void UnRegisterCQ(string serverUniqueId, string clientUniqueId)
        {
            CommandBase command =
                new UnRegisterCQCommand(serverUniqueId, clientUniqueId, TargetMethodAttribute.MethodOverload);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
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
                if (isDedicated)
                    request = _broker.CreateDedicatedRequest(command);
                else
                    request = _broker.CreateRequest(command);
            }

            _broker.ExecuteRequest(request);
            return request.Response;
        }

        internal void InvalidateReaders(string serverAddress)
        {
            lock (validReaders)
            {
                if (validReaders.ContainsKey(serverAddress))
                {
                    var readers = default(Dictionary<IRecordSetEnumerator, Object>);
                    validReaders.TryGetValue(serverAddress, out readers);
                    if (readers != null)
                    {
                        foreach (PartitionRSEnumerator partition in readers.Keys)
                            partition.IsValid = false;
                    }

                    validReaders.Remove(serverAddress);
                }
            }
        }

        internal void GetExpirationFromServer(Connection connection)
        {
            GetExpirationCommand command = new GetExpirationCommand(false);
            Request request = new Request(false, _broker.OperationTimeOut);
            Address ipAddress = connection.ServerAddress;
            request.AddCommand(ipAddress, command);

            _broker.ExecuteRequest(request, connection, true, true);


            CommandResponse res = request.Response;
            res.ParseResponse();
            if (res.KeyValueDic != null)
                _expirations = res.KeyValueDic;
        }


        #region Polling

        internal override Common.Events.PollingResult Poll()
        {
            Alachisoft.NCache.Web.Command.PollCommand command = new Alachisoft.NCache.Web.Command.PollCommand();

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse response = request.Response;

            response.ParseResponse();

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
            Request request = _broker.CreateRequest(new Command.GetConnectedClientsCommand());
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
#if NET20
            _dedicatedCallMonitoring.Stop();
            _dedicatedCallMonitoring.Start();
#else
                _dedicatedCallMonitoring.Restart();
#endif
            }
        }


        #region ---------------------- Touch ----------------------

        internal override void Touch(List<string> keys)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap =
                new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
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

        internal override bool GetOrCreate(string topicName, TopicOperationType type)
        {
            CommandBase command = new Command.GetTopicCommand(topicName, type);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
            return res.OperationWasuccessfull();
        }

        internal override bool Subscribe(string topicName, string recepientId, SubscriptionType pubSubType)
        {
            CommandBase command = new Command.SubscribeTopicCommand(topicName, recepientId, pubSubType);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
            return res.OperationWasuccessfull();
        }

        internal override bool UnSubscribe(string topicName, string recepientId, SubscriptionType pubSubType)
        {
            CommandBase command = new Command.UnSubscribeTopicCommand(topicName, recepientId, pubSubType);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
            return res.OperationWasuccessfull();
        }

        internal override void PublishMessage(string messageId, object payLoad, long creationTime, long expirationTime,
            Hashtable metadata, BitSet flagMap)
        {
            CommandBase command = new Command.MessagePublishCommand(messageId, (byte[]) payLoad, creationTime,
                expirationTime, metadata, flagMap);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        internal override bool RemoveTopic(string topicName, bool forcefully)
        {
            CommandBase command = new Command.RemoveTopicCommand(topicName, forcefully);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
            return res.OperationWasuccessfull();
        }


        internal override object GetMessageData(BitSet flagMap)
        {
            var command = new Command.GetMessageCommand(flagMap);

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
                    command = new Command.GetMessageCommand(flagMap);

                    res = ExecuteCacheRequest(command, true);
                    res.ParseResponse();
                }

                return res.MessageDic;
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
                    var command = new Command.MesasgeAcknowledgmentCommand(topicWiseMessages);
                    command.ClientLastViewId = isMrgKeys ? Broker.ForcedViewId : _broker.ClientLastViewId;
                    request.AddCommand(serverAddress, command);
                }
            }
            else
            {
                var command = new Command.MesasgeAcknowledgmentCommand(topicWiseMessageIds);
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
                    var command = new Command.MesasgeAcknowledgmentCommand(topicWiseMessageIds);
                    res = ExecuteCacheRequest(command, true);
                }
            }

            if (res != null)
            {
                res.ParseResponse();
            }
        }

        #endregion
    }
}