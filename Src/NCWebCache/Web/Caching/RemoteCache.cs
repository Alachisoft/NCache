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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Runtime.Caching;
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
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Caching.Queries;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Events;

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
    internal sealed class RemoteCache : CacheImplBase
    {
        private Broker _broker = null;
        internal Cache _parent;

        private string _cacheId;
        private string _serverId;
        private bool _clearNotifRegistered;
        private CacheAsyncEventsListener _asyncListener;
        private CacheEventsListener _eventListener;
        PerfStatsCollector2 _perfStatsCollector;
        private TypeInfoMap _typeMap;
     
       
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

        #endregion

        /// <summary>
        /// Will be called by broker incase of multiple connection
        /// </summary>
        /// <param name="connection"></param>
        internal void RegisterNodeJoinedEvent(Connection connection)
        {
            RegisterNotifications(NotificationsType.RegNodeJoinedNotif, connection);
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

        private void RegisterNotifications(NotificationsType notifMask, Connection connection)
        {
            RegisterNotificationCommand command = new RegisterNotificationCommand(notifMask,-1);

            Request request = new Request(false, _broker.OperationTimeOut);
            Address ipAddress = connection.ServerAddress;
            request.AddCommand(ipAddress, command);

            _broker.ExecuteRequest(request, connection, true, true);

            CommandResponse res = (CommandResponse)request.Response;
            res.ParseResponse();
        }

     

        internal void ReRegisterGeneralNotification(Connection connection)
        {

            RegisterNodeJoinedEvent(connection);
            RegisterNodeLeftEvent(connection);
        }


        internal static bool ImportHashmap(string cacheId)
        {
            return true;
        }

        internal RemoteCache(string cacheId, Cache parent, CacheInitParams initParams, PerfStatsCollector2 perfStatsCol)
            : base()
        {
            _parent = parent;
            _perfStatsCollector = perfStatsCol;
            if (_parent != null)
            {
                _asyncListener = new CacheAsyncEventsListener(_parent.AsyncListener);
                _eventListener = new CacheEventsListener(_parent.EventListener);
            }

            _cacheId = cacheId;
            _broker = new Broker(this, initParams, perfStatsCol);
            // changed this for init param task
            RemoteServer server = _broker.GetInitialServer();
            _broker.StartServices(cacheId, server.IpString, server.Port);

            if (!_broker.IsConnected)
                throw new OperationFailedException("No server is available to process the request for " + _cacheId);
            if (initParams.ServerList != null && initParams.ServerList.Length > 0)
               _serverId = initParams.ServerList[0].Name; 
        }


        #region ---------------------- Indexers ----------------------
        public override string Name
        {
            get
            {
                return _cacheId;
            }
        }

       

        #endregion

        #region ---------------------- Add ----------------------

        public override void Add(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority,
            short removeCallback, short updateCallback, Hashtable queryInfo, BitSet flagMap,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, long size)
        {
            AddCommand command = new AddCommand(key, (byte[])value,
                absoluteExpiration,
                slidingExpiration,
                priority,
                removeCallback,
                updateCallback,
                queryInfo,
                flagMap,
                CacheId,
                updateCallbackFilter,
                removeCallabackFilter);

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
        ///	NCache.Cache.Add(keys, items);
        ///
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Add(keys, items);
        /// 
        /// </code>
        /// </example>
        public override IDictionary Add(string[] keys, CacheItem[] items, long[] sizes)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                if (!_broker.PoolFullyConnected)
                {
                    BulkAddCommand command = new BulkAddCommand(keys, items, _parent, CacheId);
                    request = _broker.CreateDedicatedRequest(command);
                }
                else
                {
                    request = new Request(true, _broker.OperationTimeOut);
                    _broker.GetKeysDistributionMap(keys, items, ref keysDistributionMap);
                    foreach (Address serverAddress in keysDistributionMap.Keys)
                    {
                        KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                        BulkAddCommand command = new BulkAddCommand(keysAndItems.Key, keysAndItems.Value, _parent, CacheId);
                        command.ClientLastViewId = _broker.ClientLastViewId;
                        request.AddCommand(serverAddress, command);
                    }
                }
            }
            else
            {
                BulkAddCommand command = new BulkAddCommand(keys, items,  _parent, CacheId);
                request = _broker.CreateRequest(command);
            }

            _broker.ExecuteRequest(request);
            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.KeyValueDic;
          
        }

        public override bool SetAttributes(string key, CacheItemAttributes attributes)
        {
            AddAttributeCommand command = new AddAttributeCommand(key, attributes.AbsoluteExpiration);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.OperationWasuccessfull();
        }
        #endregion

        #region ---------------------- Insert ----------------------

        public override void Insert(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, 
            Hashtable queryInfo, BitSet flagMap, object lockId, LockAccessType accessType, 
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, long size)
        {
            InsertCommand command = new InsertCommand(key,
                (byte[])value,
                absoluteExpiration,
                slidingExpiration,
                priority,
                onRemoveCallback,
                onUpdateCallback,
                queryInfo,
                flagMap,
                lockId,
                accessType, 
                CacheId, updateCallbackFilter, removeCallabackFilter);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        ///  <summary> Insert list of <see cref="CacheItem"/> to the cache </summary>
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
        ///	NCache.Cache.Insert(keys, items);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Insert(keys, items);
        /// 
        /// </code>
        /// </example>
        public override IDictionary Insert(string[] keys, CacheItem[] items, long[] sizes)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                if (!_broker.PoolFullyConnected)
                {
                    BulkInsertCommand command = new BulkInsertCommand(keys, items, _parent, CacheId);
                    request = _broker.CreateDedicatedRequest(command);
                }
                else
                {
                    request = new Request(true, _broker.OperationTimeOut);
                    _broker.GetKeysDistributionMap(keys, items, ref keysDistributionMap);
                    foreach (Address serverAddress in keysDistributionMap.Keys)
                    {
                        KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                        BulkInsertCommand command = new BulkInsertCommand(keysAndItems.Key, keysAndItems.Value, _parent, CacheId);
                        command.ClientLastViewId = _broker.ClientLastViewId;
                        request.AddCommand(serverAddress, command);
                    }
                }
            }
            else
            {
                BulkInsertCommand command = new BulkInsertCommand(keys, items, _parent, CacheId);
                request = _broker.CreateRequest(command);
            }

            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.KeyValueDic;
        }

        #endregion

        #region ----------------Lock-----------------------
        public override void Unlock(string key)
        {
            UnlockCommand command = new UnlockCommand(key);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        public override void Unlock(string key, object lockId)
        {
            UnlockCommand command = new UnlockCommand(key, lockId);

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
            LockCommand command = new LockCommand(key, lockTimeout);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            lockHanlde = new LockHandle(res.LockId as string, res.LockDate);
            return res.LockAcquired;
        }

        #endregion

        #region ---------------------- Get ----------------------

        /// <summary>
        /// Retrieves the specified item from the Cache object. If the object is read thru the data source,
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>
                /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        ///  <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
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
        public override CompressedValueEntry Get(string key, BitSet flagMap, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType)
        {
            if (lockHandle == null) lockHandle = new LockHandle();
            object lockId = lockHandle.LockId;

            GetCommand command = new GetCommand(key, flagMap, accessType, lockId, lockTimeout);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            lockHandle.LockId = res.LockId as string;
            lockHandle.LockDate = res.LockDate;
            return res.FlagValueEntry;
            
        }

        ///  <summary>
        ///  Retrieves the object from the cache for the given keys as key value pairs
        ///  </summary>
        /// <param name="keys">The keys against which items are to be fetched.</param>
        /// <returns>The retrieved cache items.</returns>
        ///  <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        ///  <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        ///  <remarks>
        ///  <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        ///  setting, this property throws exception incase of failure.</para>
        ///  </remarks>
        ///  <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        ///  box server control.
        ///  <code>
        ///  
        /// 	NCache.Cache.Get(keys);
        ///  
        ///  </code>
        ///  Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        ///  <code>
        ///  
        /// 	Cache.Get(keys);
        ///  
        ///  </code> 
        ///  </example>
        public override IDictionary Get(string[] keys, BitSet flagMap)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                if (!_broker.PoolFullyConnected)
                {
                    BulkGetCommand command = new BulkGetCommand(keys, flagMap);
                    request = _broker.CreateDedicatedRequest(command);
                }
                else
                {
                    request = new Request(true, _broker.OperationTimeOut);
                    _broker.GetKeysDistributionMap(keys, null, ref keysDistributionMap);
                    foreach (Address serverAddress in keysDistributionMap.Keys)
                    {
                        KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                        BulkGetCommand command = new BulkGetCommand(keysAndItems.Key, flagMap);
                        command.ClientLastViewId = _broker.ClientLastViewId;
                        request.AddCommand(serverAddress, command);
                    }
                }
            }
            else
            {
                BulkGetCommand command = new BulkGetCommand(keys, flagMap);
                request = _broker.CreateRequest(command);
            }

            _broker.ExecuteRequest(request);
            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.KeyValueDic;
        }

        public override object GetCacheItem(string key, BitSet flagMap, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType)
        {
            if (lockHandle == null) lockHandle = new LockHandle();
            object lockId = lockHandle.LockId;


            GetCacheItemCommand command = new GetCacheItemCommand(key, flagMap, accessType, lockId, lockTimeout);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            lockHandle.LockId = res.LockId as string;
            lockHandle.LockDate = res.LockDate;

            return res.Item;
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
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <value>The number of items stored in the cache.</value>
        public override long Count
        {
            get
            {
                CountCommand command = new CountCommand();

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
            ContainsCommand command = new ContainsCommand(key);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.Exists;
        }
        #endregion

        #region ---------------------- Remove ----------------------

        ///  <summary>
        ///  Removes the object from the <see cref="Cache"/>.
        ///  </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <returns>The item removed from the Cache. If the value in the key parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
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
        public override CompressedValueEntry Remove(string key, BitSet flagMap, object lockId, LockAccessType accessType)
        {
            RemoveCommand command = new RemoveCommand(key, flagMap, lockId, accessType);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.FlagValueEntry;
        }

        ///  <summary>
        ///  Removes the object from the <see cref="Cache"/>.
        ///  </summary>
        /// <param name="key">The cache key used to reference the item.</param>
 				/// <param name="flagMap"></param>
        /// <param name="lockId"></param>
        /// <param name="accessType"></param>        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
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
        public override void Delete(string key, BitSet flagMap, object lockId, LockAccessType accessType)
        {
            DeleteCommand command = new DeleteCommand(key, flagMap, lockId, accessType);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        ///  <summary>
        ///  Removes the objects from the <see cref="Cache"/>.
        ///  </summary>
        /// <param name="keys">The cache keys used to reference the item.</param>
        /// <param name="flagMap"></param>
        /// <returns>The items removed from the Cache. If the value in the keys parameter 
        ///  is not found, returns a null reference (Nothing in Visual Basic).</returns>
        ///  <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        ///  <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        ///  <remarks>
        ///  <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        ///  setting, this property throws exception incase of failure.</para>
        ///  </remarks>
        ///  <example>The following example demonstrates how you can remove an item from your application's 
        ///  <see cref="Cache"/> object.
        ///  <code>
        ///  
        /// 	NCache.Cache.Remove(keys);
        ///  
        ///  </code>
        ///  Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        ///  <code>
        ///  
        /// 	Cache.Remove(keys);
        ///  
        ///  </code>
        ///  </example>
        public override IDictionary Remove(string[] keys, BitSet flagMap)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                if (!_broker.PoolFullyConnected)
                {
                    BulkRemoveCommand command = new BulkRemoveCommand(keys, flagMap);
                    request = _broker.CreateDedicatedRequest(command);
                }
                else
                {
                    request = new Request(true, _broker.OperationTimeOut);
                    _broker.GetKeysDistributionMap(keys, null, ref keysDistributionMap);
                    foreach (Address serverAddress in keysDistributionMap.Keys)
                    {
                        KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                        BulkRemoveCommand command = new BulkRemoveCommand(keysAndItems.Key, flagMap);
                        command.ClientLastViewId = _broker.ClientLastViewId;

                        request.AddCommand(serverAddress, command);
                    }
                }
            }
            else
            {
                BulkRemoveCommand command = new BulkRemoveCommand(keys, flagMap);
                request = _broker.CreateRequest(command);
            }

            _broker.ExecuteRequest(request);
            CommandResponse res = request.Response;
            res.ParseResponse();

            return res.KeyValueDic;

        }

        ///  <summary>
        ///  Removes the objects from the <see cref="Cache"/>.
        ///  </summary>
        /// <param name="keys">The cache keys used to reference the item.</param>
        /// <param name="flagMap"></param>
        /// <returns>The items removed from the Cache. If the value in the keys parameter 
        ///  is not found, returns a null reference (Nothing in Visual Basic).</returns>
        ///  <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        ///  <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        ///  <remarks>
        ///  <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        ///  setting, this property throws exception incase of failure.</para>
        ///  </remarks>
        ///  <example>The following example demonstrates how you can remove an item from your application's 
        ///  <see cref="Cache"/> object.
        ///  <code>
        ///  
        /// 	NCache.Cache.Remove(keys);
        ///  
        ///  </code>
        ///  Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        ///  <code>
        ///  
        /// 	Cache.Remove(keys);
        ///  
        ///  </code>
        ///  </example>
        public override void Delete(string[] keys, BitSet flagMap)
        {
            Dictionary<Address, KeyValuePair<string[], CacheItem[]>> keysDistributionMap = new Dictionary<Address, KeyValuePair<string[], CacheItem[]>>();
            Request request;
            if (_broker.ImportHashmap)
            {
                request = new Request(true, _broker.OperationTimeOut);
                if (!_broker.PoolFullyConnected)
                {
                    BulkDeleteCommand command = new BulkDeleteCommand(keys, flagMap);
                    command.ClientLastViewId = _broker.ForcedViewId;
                    Connection conn = _broker.GetAnyConnection();
                    if (conn != null)
                    {
                        request.AddCommand(conn.ServerAddress, command);
                    }
                    else
                    {
                        throw new OperationFailedException("No server is available to process the request");
                    }
                }
                else
                {
                    _broker.GetKeysDistributionMap(keys, null, ref keysDistributionMap);

                    foreach (Address serverAddress in keysDistributionMap.Keys)
                    {
                        KeyValuePair<string[], CacheItem[]> keysAndItems = keysDistributionMap[serverAddress];
                        BulkDeleteCommand command = new BulkDeleteCommand(keysAndItems.Key, flagMap);
                        command.ClientLastViewId = _broker.ClientLastViewId;

                        request.AddCommand(serverAddress, command);
                    }
                }
            }
            else
            {
                BulkDeleteCommand command = new BulkDeleteCommand(keys, flagMap);
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
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
        public override void Clear(BitSet flagMap)
        {
            ClearCommand command = new ClearCommand(flagMap);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }
        #endregion

        #region ---------------------- Query----------------------

        #region ---------------------- Search ----------------------

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax t oquery objects from cache</param>
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
            SearchCommand command = new SearchCommand(query, values, false);
            CommandResponse res = ExecuteCacheRequest(command, false);
            
            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                command = new SearchCommand(query, values, false);
                res = ExecuteCacheRequest(command, true);
                res.ParseResponse();
            }

            return res.ResultSet;
        }

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax t oquery objects from cache</param>
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
            SearchCommand command = new SearchCommand(query, values, true);
            Request request = _broker.CreateRequest(command);
            CommandResponse res = ExecuteCacheRequest(command, false);
            
            try
            {
                res.ParseResponse();
            }
            catch (StateTransferInProgressException ex)
            {
                command = new SearchCommand(query, values, true);
                res = ExecuteCacheRequest(command, true);
                res.ParseResponse();
            }

            return res.ResultSet;
        }
        #endregion

  
        #endregion

        #region ---------------------- Serialize----------------------

        public override object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag, CacheImplBase cacheImpl, ref long size)
        {
            if (serializableObject != null && cacheImpl.SerializationEnabled)
            {
                System.Type type = serializableObject.GetType();

                if (typeof(byte[]).Equals(type) && flag != null)
                {
                    flag.SetBit(BitSetConstants.BinaryData);
                    size = serializableObject is byte[] ? ((byte[])serializableObject).Length : 0;
                    return serializableObject;
                }
                size = serializableObject is byte[] ? ((byte[])serializableObject).Length : 0;
                serializableObject = CompactBinaryFormatter.ToByteBuffer(serializableObject, serializationContext);
            }
            return serializableObject;
        }

        public override object SafeDeserialize(object serializedObject, string serializationContext, BitSet flag, CacheImplBase cacheImpl)
        {
            object deserialized = serializedObject;

            try
            {
                if (serializedObject is byte[] && cacheImpl.SerializationEnabled)
                {
                    if (flag != null && flag.IsBitSet(BitSetConstants.BinaryData)) return serializedObject;

                    deserialized = CompactBinaryFormatter.FromByteBuffer((byte[])serializedObject, serializationContext);
                }
            }
            catch (Exception exception)
            {
                //Kill the exception; it is possible that object was serialized by Java
                //or from any other domain which can not be deserialized by us.
                deserialized = serializedObject;

                //throw;
            }

            return deserialized;
        }

        #endregion



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
            if (pointers.Count > 0 && pointers[0].NodeIpAddress!=null)
            {
                if (_broker.ImportHashmap)
                    request = new Request(true, _broker.OperationTimeOut);
                else
                    request = new Request(false, _broker.OperationTimeOut);

                for (int i = 0; i < pointers.Count; i++)
                {
                    EnumerationPointer pointer = pointers[i];

                    command = new GetNextChunkCommand(pointer);

                    request.AddCommand(pointer.NodeIpAddress, command);
                }
            }
            else
            {
                if (!_broker.PoolHasAllServers || !_broker.ImportHashmap)
                {
                    EnumerationPointer pointer = pointers[0];
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
                        RemoteServer server = (RemoteServer)_broker.ClientServerList[i];
                        command.intendedRecipient = server.IP.ToString().ToString();
                        request.AddCommand(new Address(server.IP.ToString(),server.Port), command);
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

        #region ------------- Inner Classes --------------------

        ///// <summary>
        ///// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
        ///// internal class and must not be used from outside
        ///// </summary>
        public class CacheAsyncEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>

            private Web.Caching.Cache.CacheAsyncEventsListener _parent;

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

                AsyncCallbackInfo cbEntry = new AsyncCallbackInfo(-1, null, callbackId);
                package[1] = cbEntry;
                package[2] = result;

                return package;
            }
        }


        /// <summary>
        /// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
        /// internal class and must not be used from outside
        /// </summary>
        public class CacheEventsListener : MarshalByRefObject, IDisposable
        {
            private Web.Caching.Cache.CacheEventsListener _listener;

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


            public void OnCustomRemoveCallback(short callbackId, string key, object value, CacheItemRemovedReason reason, BitSet Flag, bool notifyAsync, EventCacheItem item,EventDataFilter dataFilter)
            {
                object[] val = new object[] { value, new CallbackInfo(null, callbackId,dataFilter) };
                _listener.OnCustomRemoveCallback(key, val, reason, Flag, notifyAsync, item);
            }

            public void OnCustomUpdateCallback(short callbackId, string key, bool notifyAsync, EventCacheItem item, EventCacheItem oldItem, BitSet flag,EventDataFilter dataFilter)
            {
                CallbackInfo cbInfo = new CallbackInfo(null, callbackId,dataFilter);
                _listener.OnCustomUpdateCallback(key, cbInfo, notifyAsync, item, oldItem, flag);
            }

            public void OnCacheStopped(string cacheId, bool notifyAsync)
            {
                try
                {
                    if (_listener != null) _listener.OnCacheStopped(cacheId, notifyAsync);
                }
                catch { }
            }
        }

        /// <summary>
        /// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
        /// internal class and must not be used from outside
        /// </summary>
        public class ClusterEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>

            private Web.Caching.Cache.ClusterEventsListener _listener;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            internal ClusterEventsListener(Cache.ClusterEventsListener parent)
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
        }

        #endregion

        #region ----------------Key base notification registration ----------------

        public override void RegisterKeyNotificationCallback(string key, short updateCallbackid, short removeCallbackid, bool notifyOnitemExpiration)
        {
            CommandBase command = new RegisterKeyNotificationCommand(key, updateCallbackid, removeCallbackid,notifyOnitemExpiration);

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

        public override void UnRegisterKeyNotificationCallback(string key, short update,short remove, EventType eventType)
        {
            CommandBase command = new UnRegisterKeyNotificationCommand(key, update, remove);

            Request request = _broker.CreateRequest(command);
            _broker.ExecuteRequest(request);

            CommandResponse res = request.Response;
            res.ParseResponse();
        }

        #endregion

        #region ---------------Util Methods----------------

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
        
        internal void GetThresholdSize(Connection connection)
        {
            GetThresholdCommand command = new GetThresholdCommand(false);

            Request request = new Request(false, _broker.OperationTimeOut);
            Address ipAddress = connection.ServerAddress;
            request.AddCommand(ipAddress, command);

            _broker.ExecuteRequest(request, connection, true, true);
            CommandResponse res = request.Response;
            res.ParseResponse();
        }
        
        internal CommandResponse ExecuteCacheRequest(CommandBase command, bool isDedicated)
        {
            Request request = null;
            if (isDedicated)
                request = _broker.CreateDedicatedRequest(command);
            else
                request = _broker.CreateRequest(command);

            _broker.ExecuteRequest(request);
            return request.Response;
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
        #endregion


    }
}
