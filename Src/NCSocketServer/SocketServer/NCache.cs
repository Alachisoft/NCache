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
using System.Collections;

using System.Threading;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.SocketServer.CallbackTasks;
using Alachisoft.NCache.SocketServer.EventTask;
using Alachisoft.NCache.Runtime.Exceptions;


using Alachisoft.NCache.SocketServer.Util;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.DataStructures;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Protobuf;
using EnumerationPointer = Alachisoft.NCache.Common.DataStructures.EnumerationPointer;
using Exception = System.Exception;
using Alachisoft.NCache.Runtime.Events;

using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Caching;

namespace Alachisoft.NCache.SocketServer
{
    internal sealed class NCache : ICommandExecuter
    {
        string _cacheserver="NCache";
        private ClientManager _client;
        private bool _isDotNetClient;

        private Alachisoft.NCache.Caching.Cache _cache = null;
        private string _cacheId = null;
        private string _licenceCode = string.Empty;

        private CustomUpdateCallback _onItemUpdatedCallback = null;
        private Caching.PollRequestCallback _pollRequestCallback = null;
        private CustomRemoveCallback _onItemRemoveCallback = null;
        private AsyncOperationCompletedCallback _asyncOperationCallback = null;

        private ItemAddedCallback _itemAdded = null;
        private ItemUpdatedCallback _itemUpdated = null;
        private ItemRemovedCallback _itemRemoved = null;
        private CacheClearedCallback _cacheCleared = null;
        private CustomNotificationCallback _customNotif = null;
        private DataSourceUpdatedCallback _dsUpdatedCallback = null;
        private ConfigurationModified _configModified = null;
        private CompactTypeModifiedCallback _onCompactTypeModifiedCallback = null;
        private short _addSeq = -1;
        private short _removeSeq = -1;
        private short _updateSeq = -1;

        private object sync_lock_AddDataFilter = new object();
        private object sync_lock_UpdateDataFilter = new object();
        private object sync_lock_RemoveDataFilter = new object();


        //muds:
        private NodeJoinedCallback _nodeJoined = null;
        private NodeLeftCallback _nodeLeft = null;
        private CacheStoppedCallback _cacheStopped = null;
        private CacheBecomeActiveCallback _cacheBecomeActive = null;
#if !DEVELOPMENT
        private HashmapChangedCallback _hashmapChanged = null;
#endif
        private OperationModeChangedCallback _operationModeChanged = null;
        private BlockClientActivity _blockClientActivity = null;
        private UnBlockClientActivity _unblockClientActivity = null;

        /// <summary>flag to determine if client has registered to cache stopped event.
        /// if so, client will be notified about cache stop.
        /// We cannot unregister cache stopped event upon client request because it is 
        /// used to dispose client too.</summary>
        private bool _cacheStoppedEventRegistered = false;
      
        private bool _clientConnectivityChangeRegistered;

        EventDataFilter _itemAddedFilter = EventDataFilter.None;

        EventDataFilter _itemUpdatedFilter = EventDataFilter.None;

        EventDataFilter _itemRemovedFilter = EventDataFilter.None;
        /// <summary>
        /// Initialize the cache instance.
        /// </summary>
        /// 


        public EventDataFilter ItemAddedFilter
        {
            get { return _itemAddedFilter; }
            set { _itemAddedFilter = value; }
        }


        public EventDataFilter ItemRemovedFilter
        {
            get { return _itemRemovedFilter; }
            set { _itemRemovedFilter = value; }
        }


        public EventDataFilter ItemUpdatedFilter
        {
            get { return _itemUpdatedFilter; }
            set { _itemUpdatedFilter = value; }
        }


        internal NCache(string cacheId, bool isDotNetClient, ClientManager client, string licenceInfo, string userId, string password, byte[] userIdBinary, byte[] paswordBinary, Runtime.Caching.ClientInfo clientInfo)
        {
            this._cacheId = cacheId;
            this._isDotNetClient = isDotNetClient;
            this._client = client;
            this._licenceCode = licenceInfo;

            
                _cache = CacheProvider.Provider.GetCacheInstanceIgnoreReplica(cacheId);
                   
            if (_cache == null) throw new Exception("Cache is not registered");
            
            if (!_cache.IsRunning) throw new Exception("Cache is not running");

            EventHelper.EventDataFormat = _cache.SocketServerDataService;
#if !CLIENT
            if (_cache.CacheType.Equals("mirror-server") && !_cache.IsCoordinator)
                throw new OperationNotSupportedException("Cannot connect to Passive Node in Mirror Cluster.");
#endif

            _onItemUpdatedCallback = new CustomUpdateCallback(CustomUpdate);
            _cache.CustomUpdateCallbackNotif += _onItemUpdatedCallback;

            _onItemRemoveCallback = new CustomRemoveCallback(CustomRemove);
            _cache.CustomRemoveCallbackNotif += _onItemRemoveCallback;

            _pollRequestCallback = new Caching.PollRequestCallback(PollRequest);
            _cache.PollRequestCallbackNotif += _pollRequestCallback;


            if (SocketServer.Logger.IsErrorLogsEnabled)
            {
                SocketServer.Logger.NCacheLog.Error(_cacheserver+".ctor", "Registering cache stopped event for " + _client.ClientID);
            }

            _cacheStopped = new CacheStoppedCallback(OnCacheStopped);
            _cache.CacheStopped += _cacheStopped;

            if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error(_cacheserver+".ctor", "Cache stopped event registered for " + _client.ClientID);



              
            _asyncOperationCallback = new AsyncOperationCompletedCallback(AsyncOperationCompleted);
            _cache.AsyncOperationCompleted += _asyncOperationCallback;

           
            _cacheBecomeActive = new CacheBecomeActiveCallback(OnCacheBecomeActive);
            _cache.CacheBecomeActive += _cacheBecomeActive;

            _configModified = new ConfigurationModified(OnConfigModified);
            _cache.ConfigurationModified += _configModified;

            _onCompactTypeModifiedCallback = new CompactTypeModifiedCallback(CompactTypesModified);
            _cache.CompactTypeModified += _onCompactTypeModifiedCallback;

            _blockClientActivity = new BlockClientActivity(BlockClientActivity);
            _cache.BlockActivity += this._blockClientActivity;

            _unblockClientActivity = new UnBlockClientActivity(UnBlockClientActivity);
            _cache.UnBlockActivity += this._unblockClientActivity;
            
            _operationModeChanged += new OperationModeChangedCallback(OperationModeChanged);
            _cache.OperationModeChanged += _operationModeChanged;
        }


        private void InitAuthorizeCredentials(string userId, string password)
        {
           
        }
       
        internal void MaxEventRequirement(EventDataFilter datafilter, NotificationsType eventType, short sequence)
        {
            switch (eventType)
            {
                case NotificationsType.RegAddNotif:
                    lock (sync_lock_AddDataFilter)
                    {
                        if (_addSeq < sequence)
                        {
                            ItemAddedFilter = datafilter;
                            _addSeq = sequence;
                        } 
                    }
                    break;
                case NotificationsType.RegRemoveNotif:
                    lock (sync_lock_RemoveDataFilter)
                    {
                        if (_removeSeq < sequence)
                        {
                            ItemRemovedFilter = datafilter;
                            _removeSeq = sequence;
                        } 
                    }
                    break;
                case NotificationsType.RegUpdateNotif:
                    lock (sync_lock_UpdateDataFilter)
                    {
                        if (_updateSeq < sequence)
                        {
                            ItemUpdatedFilter = datafilter;
                            _updateSeq = sequence;
                        } 
                    }
                    break;
            }
        }

        /// <summary>
        /// Get the cacheId
        /// </summary>
        internal string CacheId
        {
            get { return _cacheId; }
        }

        /// <summary>Instance of cache</summary>
        /// <remarks>You must obtain lock before using cache</remarks>
        internal Alachisoft.NCache.Caching.Cache Cache
        {
            get { return _cache; }
        }



        internal AsyncOperationCompletedCallback AsyncOperationCallback
        {
            get { return _asyncOperationCallback; }
        }

        internal CustomRemoveCallback RemoveCallback
        {
            get { return _onItemRemoveCallback; }
        }

        internal CustomUpdateCallback UpdateCallback
        {
            get { return _onItemUpdatedCallback; }
        }

        /// <summary>
        /// Determine whether the client connected is a .net client
        /// </summary>
        internal bool IsDotnetClient
        {
            get { return this._isDotNetClient; }
        }

        /// <summary>
        /// This function is called by CacheStoppedCallback
        /// </summary>
        public void OnCacheStopped(string cacheId, EventContext eventContext)
        {
            //muds:
            //first of all fire the CacheStoppedCallback for the remote client.
            try
            {
                if (this._cacheStoppedEventRegistered)
                {
                    CacheStopped();
                }
            }
            catch (Exception e)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error(_cacheserver+".OnCacheStopped", e.ToString());
            }

            //now break the connection of the socket server with the client.
            if (_client != null) _client.OnCacheStopped(cacheId);
        }

        public void OnCacheBecomeActive(string cacheId, EventContext eventContext)
        {
           
        }
        /// <summary>
        /// Dispose the cahce and Unregister the callbacks being registered with that cache
        /// </summary>
        public void Dispose()
        {
            if (_cache != null)
            {
                UnRegisterNotifications();
            }
        }


        public void DisposeEnumerator(EnumerationPointer pointer)
        {
            if (_cache != null)
            {
                //Just a dummy call to dispose enumerator
                pointer.isDisposable = true;
                pointer.IsSocketServerDispose = true;
                _cache.GetNextChunk(pointer, new OperationContext());
            }
        }

        /// <summary>
        /// Unregister the callbacks registered with cache
        /// </summary>

        private void UnRegisterNotifications()
        {
            if (_onItemUpdatedCallback != null)
            {
                _cache.CustomUpdateCallbackNotif -= _onItemUpdatedCallback;
                _onItemUpdatedCallback = null;
            }
            if (_onItemRemoveCallback != null)
            {
                _cache.CustomRemoveCallbackNotif -= _onItemRemoveCallback;
                _onItemRemoveCallback = null;
            }
            if (_pollRequestCallback != null)
            {
                _cache.PollRequestCallbackNotif -= _pollRequestCallback;
                _pollRequestCallback = null;
            }

            if (_cacheCleared != null)
            {
                _cache.CacheCleared -= _cacheCleared;
                _cacheCleared = null;
            }
            if (_cacheStopped != null)
            {
                _cache.CacheStopped -= _cacheStopped;
                _cacheStopped = null;
            }
            if(_onCompactTypeModifiedCallback !=null)
            {
                _cache.CompactTypeModified -= _onCompactTypeModifiedCallback;
                _onCompactTypeModifiedCallback = null;
            }

            if (_asyncOperationCallback != null)
            {
                _cache.AsyncOperationCompleted -= _asyncOperationCallback;
                _asyncOperationCallback = null;
            }
            if (_dsUpdatedCallback != null)
            {
                _cache.DataSourceUpdated -= _dsUpdatedCallback;
                _dsUpdatedCallback = null;
            }

            if (_itemAdded != null)
            {
                _cache.ItemAdded -= _itemAdded;
                _itemAdded = null;
                lock (sync_lock_AddDataFilter)
                {
                   ItemAddedFilter = EventDataFilter.None;
                    _addSeq = -1;
                }
            }
            if (_itemUpdated != null)
            {
                _cache.ItemUpdated -= _itemUpdated;
                _itemUpdated = null;
                lock (sync_lock_UpdateDataFilter)
                {
                    ItemUpdatedFilter = EventDataFilter.None;
                    _updateSeq = -1;
                }
            }
            if (_itemRemoved != null)
            {
                _cache.ItemRemoved -= _itemRemoved;
                _itemRemoved = null;
                lock (sync_lock_RemoveDataFilter)
                {
                    ItemRemovedFilter = EventDataFilter.None;
                    _removeSeq = -1;
                }
            }

            if (_customNotif != null)
            {
                _cache.CustomNotif -= _customNotif;
                _customNotif = null;
            }

            if (_nodeJoined != null)
            {
                _cache.MemberJoined -= _nodeJoined;
                _nodeJoined = null;
            }
            if (_nodeLeft != null)
            {
                _cache.MemberLeft -= _nodeLeft;
                _nodeLeft = null;
            }
            if (_cacheBecomeActive != null)
            {
                _cache.CacheBecomeActive -= _cacheBecomeActive;
                _cacheBecomeActive = null;
            }
            if (_configModified != null)
            {
                _cache.ConfigurationModified -= _configModified;
                _configModified = null;
            }
           
#if !DEVELOPMENT
            if (this._hashmapChanged != null)
            {
                this._cache.HashmapChanged -= this._hashmapChanged;
                _hashmapChanged = null;
            }
#endif

            if (_operationModeChanged != null)
            {
                _cache.OperationModeChanged -= _operationModeChanged;
                _operationModeChanged = null;
            }
            if (this._blockClientActivity != null)
            {
                this._cache.BlockActivity -= this._blockClientActivity;
                _blockClientActivity = null;
            }

            if (this._unblockClientActivity != null)
            {
                this._cache.UnBlockActivity -= this._unblockClientActivity;
                _unblockClientActivity = null;
            }
           
         
        }


        /// <summary>
        /// Called when an async operation is completed
        /// </summary>
        /// <param name="key">key being used for async operation</param>
        /// <param name="callbackEntry">callback entry being used for async operation</param>
        private void AsyncOperationCompleted(object opCode, object result, EventContext eventContext)
        {
            if (result is object[])
            {
                if (_client != null)
                {
                    AsyncCallbackInfo cbInfo = ((object[])result)[1] as AsyncCallbackInfo;
                    if (cbInfo != null && cbInfo.Client != _client.ClientID) return;
                    //client older then 4.1 sp2 private patch 4 does not support bulk Events
                    if (_client.ClientVersion >= 4124)
                    {
                        object[] package = null;
                        package = (object[])SerializationUtil.CompactDeserialize(result, _cacheId);

                        string key = (string)package[0];
                        AsyncCallbackInfo cbInformation = (AsyncCallbackInfo)package[1];
                        object opResult = package[2];

                        Alachisoft.NCache.Common.Protobuf.AsyncOperationCompletedCallbackResponse asyncOperationCompleted = EventHelper.GetAsyncOpCompletedResponse(_client, cbInformation, opResult, opCode, key);

                        Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventitem = new Common.Protobuf.BulkEventItemResponse();
                        eventitem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.ASYNC_OP_COMPLETED_EVENT;
                        eventitem.asyncOperationCompletedCallback = asyncOperationCompleted;

                        //_client.EnqueueEvent(eventitem);
                        //To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                        ClientManager client = _client;
                        if(client != null) client.ConnectionManager.EnqueueEvent(eventitem, _client.SlaveId);                       
                        
                    }
                    else
                    {
                        lock (ConnectionManager.CallbackQueue)
                        {
                            ConnectionManager.CallbackQueue.Enqueue(new AsyncOpCompletedCallback(/*notification.CallerID,*/ opCode, result, /*notification.ClientSocket,*/ _cacheId));
                            Monitor.Pulse(ConnectionManager.CallbackQueue);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Called to find cache level events status
        /// </summary>
        public EventStatus GetEventsStatus()
        {
            EventStatus eventStatus = new EventStatus();
            if (_cacheCleared != null)
            {
                eventStatus.IsCacheClearedEvent = true;
            }
            if (_itemUpdated != null)
            {
                eventStatus.IsItemUpdatedEvent = true;
            }
            if (_itemAdded != null)
            {
                eventStatus.IsItemAddedEvent = true;
            }
            if (_itemRemoved != null)
            {
                eventStatus.IsItemRemovedEvent = true;
            }
            return eventStatus;
        }
        /// <summary>
        /// Called when item is updated
        /// </summary>
        /// <param name="key">key of the item being updated</param>
        /// <param name="callbackEntry">callback entry that contains the updated value</param>
        private void CustomUpdate(object key, object callbackInfo, EventContext eventContext)
        {
            if (_client != null)
            {
                CallbackInfo cbInfo = callbackInfo as CallbackInfo;
                if (cbInfo != null && cbInfo.Client == _client.ClientID)
                {
                    //client older then 4.1 sp2 private patch 4 does not support bulk Events
                    if (_client.ClientVersion >= 4124)
                    {
                        Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventitem = new Common.Protobuf.BulkEventItemResponse();
                        eventitem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.ITEM_UPDATED_CALLBACK;
                        eventitem.ItemUpdatedCallback = EventHelper.GetItemUpdatedCallbackResponse(eventContext, (string)key, (short)cbInfo.Callback, cbInfo.DataFilter);

                        //To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                        ClientManager client = _client;
                        if (client != null) client.ConnectionManager.EnqueueEvent(eventitem, _client.SlaveId);                       
                    }
                    else
                    {
                        lock (ConnectionManager.CallbackQueue)
                        {
                            ConnectionManager.CallbackQueue.Enqueue(new ItemUpdateCallback((short)cbInfo.Callback, (string)key, cbInfo.Client, eventContext, cbInfo.DataFilter));
                            Monitor.Pulse(ConnectionManager.CallbackQueue);
                        }
                    }
                }
            }
        }

        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="callbackEntry"></param>
        /// <param name="reason"></param>
        private void CustomRemove(object key, object value, ItemRemoveReason reason, BitSet Flag, EventContext eventContext)
        {
            if (_client != null)
            {
                object[] args = value as object[];
                if (args != null)
                {
                    object val = args[0];
                    CallbackInfo cbInfo = args[1] as CallbackInfo;
                    if (cbInfo != null && cbInfo.Client == _client.ClientID)
                    {
                        //client older then 4.1 sp2 private patch 4 does not support bulk Events
                        if (_client.ClientVersion >= 4124)
                        {
                            Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                            eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.ITEM_REMOVED_CALLBACK;
                            eventItem.itemRemoveCallback = EventHelper.GetItemRemovedCallbackResponse(eventContext, (short)cbInfo.Callback, (string)key, (UserBinaryObject)val, Flag, reason,cbInfo.DataFilter);

                           // _client.EnqueueEvent(eventItem);
                            //To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                            ClientManager client = _client;
                            if (client != null) client.ConnectionManager.EnqueueEvent(eventItem, _client.SlaveId);
                        }
                        else
                        {
                            lock (ConnectionManager.CallbackQueue)
                            {
                                ConnectionManager.CallbackQueue.Enqueue(new ItemRemoveCallback((short)cbInfo.Callback, (string)key, val, reason, _client.ClientID, Flag, eventContext,cbInfo.DataFilter));
                                Monitor.Pulse(ConnectionManager.CallbackQueue);
                            }
                        }
                    }
                }
            }
        }

        private void PollRequest(string clientId, short callbackId, EventTypeInternal eventType)
        {
            if (_client != null)
            {
                //client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                    eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.POLL_NOTIFY_EVENT;
                    eventItem.pollNotifyEvent = EventHelper.GetPollNotifyEvent(callbackId, eventType);

                    //To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                    ClientManager client = _client;
                    if (client != null) client.ConnectionManager.EnqueueEvent(eventItem, _client.SlaveId);
                }
                else
                {
                    lock (ConnectionManager.CallbackQueue)
                    {
                        ConnectionManager.CallbackQueue.Enqueue(new CallbackTasks.PollRequestCallback(_client.ClientID, callbackId, eventType));
                        Monitor.Pulse(ConnectionManager.CallbackQueue);
                    }
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="updatedCompactTypes"></param>
        private void CompactTypesModified(Hashtable updatedCompactTypes, EventContext eventContext)
        {
            if (_client != null)
            {
                lock (ConnectionManager.CallbackQueue)
                {

                    ConnectionManager.CallbackQueue.Enqueue(new CompactTypeRegisterCallback(updatedCompactTypes));
                    Monitor.Pulse(ConnectionManager.CallbackQueue);
                }
            }
        }

        /// <summary>
        /// This function is called by ConfigurationModified callback
        /// </summary>
        /// <param name="hotConfig"></param>
        private void OnConfigModified(HotConfig hotConfig)
        {
            lock (ConnectionManager.CallbackQueue)
            {
                if (_client != null)
                {
                    ConnectionManager.CallbackQueue.Enqueue(new ConfigModifiedEvent(hotConfig, _cacheId, _client.ClientID));
                    Monitor.Pulse(ConnectionManager.CallbackQueue);
                }
            }
        }

        /// <summary>
        /// Notify the connected client about logging information change
        /// </summary>
        /// <param name="enableErrorLogs"></param>
        /// <param name="enableDetailedLogs"></param>
        /// <param name="clientId"></param>
        internal void OnLoggingInfoModified(bool enableErrorLogs, bool enableDetailedLogs, string clientId)
        {
            lock (ConnectionManager.CallbackQueue)
            {
                if (_client != null)
                {
                    ConnectionManager.CallbackQueue.Enqueue(new LoggingInfoModifiedEvent(enableErrorLogs, enableDetailedLogs, clientId));
                    Monitor.Pulse(ConnectionManager.CallbackQueue);
                }
            }
        }

        /// <summary>
        /// This function is called by ItemAddedCallback
        /// </summary>
        /// <param name="key"></param>
        private void ItemAdded(object key, EventContext eventContext)
        {
            if (_client != null)
            {
                //client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventitem = new Common.Protobuf.BulkEventItemResponse();
                    eventitem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.ITEM_ADDED_EVENT;
                    eventitem.itemAddedEvent = EventHelper.GetItemAddedEventResponse(eventContext, (string)key, this.ItemAddedFilter);

                    //_client.EnqueueEvent(eventitem);
                    //To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                    ClientManager client = _client;
                    if (client != null) client.ConnectionManager.EnqueueEvent(eventitem, _client.SlaveId);
                }
                else
                {
                    lock (ConnectionManager.CallbackQueue)
                    {
                        if (_client != null)
                        {
                            ConnectionManager.CallbackQueue.Enqueue(new ItemAddedEvent((string)key, _cacheId, _client.ClientID, eventContext, this.ItemAddedFilter));
                            Monitor.Pulse(ConnectionManager.CallbackQueue);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This function is called by ItemUpdatedCallback
        /// </summary>
        /// <param name="key"></param>
        private void ItemUpdated(object key, EventContext eventContext)
        {
            if (_client != null)
            {
                //client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                    eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.ITEM_UPDATED_EVENT;
                    eventItem.itemUpdatedEvent = EventHelper.GetItemUpdatedEventResponse(eventContext, (string)key, this.ItemUpdatedFilter);

                    //To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                    ClientManager client = _client;
                    if (client != null) client.ConnectionManager.EnqueueEvent(eventItem, _client.SlaveId);
                }
                else
                {
                    lock (ConnectionManager.CallbackQueue)
                    {
                        if (_client != null)
                        {
                            ConnectionManager.CallbackQueue.Enqueue(new ItemUpdatedEvent((string)key, _cacheId, _client.ClientID, eventContext));
                            Monitor.Pulse(ConnectionManager.CallbackQueue);
                        }
                    }
                }
            }


        }

        /// <summary>
        /// This function is called by ItemRemovedCallback
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="reason"></param>
        private void ItemRemoved(object key, object value, ItemRemoveReason reason, BitSet Flag, EventContext eventContext)
        {
            //value which arrives will be null
            value = eventContext.Item.Value;

            if (_client != null)
            {
                //client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                    eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.ITEM_REMOVED_EVENT;
                    eventItem.itemRemovedEvent = EventHelper.GetItemRemovedEventResponse(eventContext, (string)key, this.ItemRemovedFilter, Flag, reason, (UserBinaryObject)value);

                   // _client.EnqueueEvent(eventItem);
                    //To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                    ClientManager client = _client;
                    if (client != null) client.ConnectionManager.EnqueueEvent(eventItem, _client.SlaveId);
                }
                else
                {
                    lock (ConnectionManager.CallbackQueue)
                    {                        
                        ConnectionManager.CallbackQueue.Enqueue(new ItemRemovedEvent((string)key, _cacheId, reason, (UserBinaryObject)value, _client.ClientID, Flag, eventContext));
                        Monitor.Pulse(ConnectionManager.CallbackQueue);
                    }
                }
            }


        }

        /// <summary>
        /// This function is called by cacheClearedCallback
        /// </summary>
        private void CacheCleared(EventContext eventContext)
        {
            if (_client != null)
            {
                //client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                    eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.CACHE_CLEARED_EVENT;
                    eventItem.cacheClearedEvent = EventHelper.GetCacheClearedResponse(eventContext);

                    //_client.EnqueueEvent(eventItem);
                    //To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                    ClientManager client = _client;
                    if (client != null) client.ConnectionManager.EnqueueEvent(eventItem, _client.SlaveId);
                }
                else
                {
                    lock (ConnectionManager.CallbackQueue)
                    {
                        ConnectionManager.CallbackQueue.Enqueue(new CacheClearedEvent(_cacheId, _client.ClientID, eventContext));
                        Monitor.Pulse(ConnectionManager.CallbackQueue);
                    }
                }
            }
        }

        /// <summary>
        /// This function is called by CacheStopedCallback
        /// </summary>
        private void CacheStopped()
        {
            if (_client != null)
            {
                //client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                    eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.CACHE_STOPPED_EVENT;
                    eventItem.cacheStoppedEvent = EventHelper.GetCacheStoppedEventResponse(_cacheId);

                    //_client.EnqueueEvent(eventItem);
                    //To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                    ClientManager client = _client;
                    if (client != null) client.ConnectionManager.EnqueueEvent(eventItem, _client.SlaveId);
                }
                else
                {
                    lock (ConnectionManager.CallbackQueue)
                    {
                        ConnectionManager.CallbackQueue.Enqueue(new CacheStoppedEvent(_cacheId, _client.ClientID));
                        Monitor.Pulse(ConnectionManager.CallbackQueue);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="value"></param>
        private void CustomNotification(object notifId, object value, EventContext eventContext)
        {
            if (_client != null)
            {
                //client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                    eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.RAISE_CUSTOM_EVENT;
                    eventItem.CustomEvent = EventHelper.GetCustomEventResponse((byte[])notifId, (byte[])value);

                    //_client.EnqueueEvent(eventItem);
                    //To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                    ClientManager client = _client;
                    if (client != null) client.ConnectionManager.EnqueueEvent(eventItem, _client.SlaveId);
                }
                else
                {
                    lock (ConnectionManager.CallbackQueue)
                    {
                        if (_client == null) return;
                        ConnectionManager.CallbackQueue.Enqueue(new CustomEvent(_cacheId, (byte[])notifId, (byte[])value, _client.ClientID));
                        Monitor.Pulse(ConnectionManager.CallbackQueue);
                    }
                }
            }
        }

       

        /// <summary>
        /// This function is called by NodeJoinedCallback
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="value"></param>
        private void NodeJoined(object clusterAddress, object serverAddress, bool reconnect, EventContext eventContext)
        {
            lock (ConnectionManager.CallbackQueue)
            {
                if (_client == null) return;
                ConnectionManager.CallbackQueue.Enqueue(new NodeJoinedEvent(_cacheId, clusterAddress as Alachisoft.NCache.Common.Net.Address, serverAddress as Alachisoft.NCache.Common.Net.Address, _client.ClientID, reconnect));
                Monitor.Pulse(ConnectionManager.CallbackQueue);
            }
        }

        /// <summary>
        /// This function is called by NodeLeftCallback
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="value"></param>
        private void NodeLeft(object clusterAddress, object serverAddress, EventContext eventContext)
        {
            lock (ConnectionManager.CallbackQueue)
            {
                if (_client == null) return;
                ConnectionManager.CallbackQueue.Enqueue(new NodeLeftEvent(_cacheId, clusterAddress as Alachisoft.NCache.Common.Net.Address, serverAddress as Alachisoft.NCache.Common.Net.Address, _client.ClientID));
                Monitor.Pulse(ConnectionManager.CallbackQueue);
            }
        }

        private void OnCacheClientConnectivityChanged(string cacheId, Runtime.Caching.ClientInfo client)
        {
            if (_client != null)
            {
                if (_client.ClientVersion >= 4124)
                {
                    BulkEventItemResponse eventItem = new BulkEventItemResponse();
                    eventItem.eventType = BulkEventItemResponse.EventType.CLIENT_CONNECTIVITY;
                    eventItem.clientConnectivityChangeEvent =
                        EventHelper.GetClientConnectivityChangeEventResponse(cacheId, client);
                    ClientManager clientReference = _client;
                    if (clientReference != null)
                        clientReference.ConnectionManager.EnqueueEvent(eventItem, clientReference.SlaveId);
                }
            }
        }


#if !DEVELOPMENT
        private void HashmapChanged(NewHashmap newmap, EventContext eventContext)
        {

            lock (ConnectionManager.CallbackQueue)
            {
                if (_client != null)
                {
                    ConnectionManager.CallbackQueue.Enqueue(new HashmapChangedEvent(_cacheId, _client.ClientID, newmap, this._isDotNetClient));
                    Monitor.Pulse(ConnectionManager.CallbackQueue);
                }
            }

        }
#endif
        private void BlockClientActivity(string uniqueId, string serverIp, long timeoutInterval, int port)
        {

            lock (ConnectionManager.CallbackQueue)
            {
                if (_client != null)
                {
                    ConnectionManager.CallbackQueue.Enqueue(new BlockActivityEvent(uniqueId, _cacheId, _client.ClientID, serverIp, timeoutInterval, port));
                    Monitor.Pulse(ConnectionManager.CallbackQueue);
                }
            }
        }



        private void UnBlockClientActivity(string uniqueId, string serverIp, int port)
        {

            lock (ConnectionManager.CallbackQueue)
            {
                if (_client != null)
                {
                    ConnectionManager.CallbackQueue.Enqueue(new UnBlockActivityEvent(uniqueId, _cacheId, _client.ClientID, serverIp, port));
                    Monitor.Pulse(ConnectionManager.CallbackQueue);
                }
            }
        }


        private void OperationModeChanged(OperationMode mode)
        {
            if (mode == OperationMode.OFFLINE)
            {
                lock (ConnectionManager.CallbackQueue)
                {
                    if (_client != null)
                    {
                        ConnectionManager.CallbackQueue.Enqueue(new OperationModeChangedEvent(_client.ClientID));
                        Monitor.Pulse(ConnectionManager.CallbackQueue);
                    }
                }
            }
        }


        #region ICommandExecuter Members

        public string ID
        {
            get
            {
                return _cacheId;
            }
        }

        public void OnClientConnected(string clientID, string cacheId, Runtime.Caching.ClientInfo clientInfo, long count)
        {

            if (_cache != null) _cache.OnClientConnected(clientID, cacheId, clientInfo, count);

        }

        /// <summary>
        /// This function is called client is dis-connected 
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="cacheId"></param>
        public void OnClientDisconnected(string clientID, string cacheId, Runtime.Caching.ClientInfo clientInfo, long count)
        {

            if (_cache != null) _cache.OnClientDisconnected(clientID, cacheId, clientInfo, count);

        }

        //fix for BroadAge 4.1 Sp2 patch 4
        //forcefully disconnected when Socket is busy for more then configured interval
        public void OnClientForceFullyDisconnected(string clientId)
        {
            if (_cache != null) _cache.OnClientForceFullyDisconnected(clientId);
        }

        public void UpdateSocketServerStats(SocketServerStats stats)
        {
            if (_cache != null) _cache.UpdateSocketServerStats(stats);
        }

        public bool IsCoordinator(string srcCacheID)
        {
            //we need not to implement this function here have implemented it in NBridge.cs. Just returning true in all cases.
            return false;
        }

       
        /// <summary>
        /// Register callbacks with cache.
        /// </summary>
        /// <param name="type"></param>
        public void RegisterNotification(NotificationsType type)
        {

            switch (type)
            {
                case NotificationsType.RegAddNotif:
                    if (_itemAdded == null)
                    {
                        _itemAdded = new ItemAddedCallback(ItemAdded);
                        _cache.ItemAdded += _itemAdded;
                    }
                    break;

                case NotificationsType.RegUpdateNotif:
                    if (_itemUpdated == null)
                    {
                        _itemUpdated = new ItemUpdatedCallback(ItemUpdated);
                        _cache.ItemUpdated += _itemUpdated;
                    }
                    break;

                case NotificationsType.RegRemoveNotif:
                    if (_itemRemoved == null)
                    {
                        _itemRemoved = new ItemRemovedCallback(ItemRemoved);
                        _cache.ItemRemoved += _itemRemoved;
                    }
                    break;

                case NotificationsType.RegClearNotif:
                    if (_cacheCleared == null)
                    {
                        _cacheCleared = new CacheClearedCallback(CacheCleared);
                        _cache.CacheCleared += _cacheCleared;
                    }
                    break;

                case NotificationsType.RegCustomNotif:
                    if (_customNotif == null)
                    {
                        _customNotif = new CustomNotificationCallback(CustomNotification);
                        _cache.CustomNotif += _customNotif;
                    }
                    break;

                case NotificationsType.RegNodeJoinedNotif:
                    if (_nodeJoined == null)
                    {
                        _nodeJoined = new NodeJoinedCallback(NodeJoined);
                        _cache.MemberJoined += _nodeJoined;
                    }
                    break;

                case NotificationsType.RegNodeLeftNotif:
                    if (_nodeLeft == null)
                    {
                        _nodeLeft = new NodeLeftCallback(NodeLeft);
                        _cache.MemberLeft += _nodeLeft;
                    }
                    break;

                case NotificationsType.RegCacheStoppedNotif:
                    
                    _cacheStopped = new CacheStoppedCallback(OnCacheStopped);
                    _cache.CacheStopped += _cacheStopped;
                    this._cacheStoppedEventRegistered = true;
                    break;

              

#if !DEVELOPMENT
                case NotificationsType.RegHashmapChangedNotif:
                    if (this._hashmapChanged == null)
                    {
                        this._hashmapChanged = new HashmapChangedCallback(HashmapChanged);
                        this._cache.HashmapChanged += this._hashmapChanged;
                    }
                    break;
#endif
                case NotificationsType.UnregAddNotif:
                    if (_itemAdded != null)
                    {
                        _cache.ItemAdded -= _itemAdded;
                        _itemAdded = null;
                        lock (sync_lock_AddDataFilter)
                        {
                            ItemAddedFilter = EventDataFilter.None;
                            _addSeq = -1;
                        }
                    }
                    break;

                case NotificationsType.UnregUpdateNotif:
                    if (_itemUpdated != null)
                    {
                        _cache.ItemUpdated -= _itemUpdated;
                        _itemUpdated = null;
                        lock (sync_lock_UpdateDataFilter = -1)
                        {
                            ItemUpdatedFilter = EventDataFilter.None;
                            _updateSeq = -1;
                        }
                    }
                    break;

                case NotificationsType.UnregRemoveNotif:
                    if (_itemRemoved != null)
                    {
                        _cache.ItemRemoved -= _itemRemoved;
                        _itemRemoved = null;
                        lock (sync_lock_RemoveDataFilter)
                        {
                            ItemRemovedFilter = EventDataFilter.None;
                            _removeSeq = -1;
                        }
                    }
                    break;

                case NotificationsType.UnregClearNotif:
                    if (_cacheCleared != null)
                    {
                        _cache.CacheCleared -= _cacheCleared;
                        _cacheCleared = null;
                    }
                    break;

                case NotificationsType.UnregCustomNotif:
                    if (_customNotif != null)
                    {
                        _cache.CustomNotif -= _customNotif;
                        _customNotif = null;
                    }
                    break;

                case NotificationsType.UnregNodeJoinedNotif:
                    if (_nodeJoined != null)
                    {
                        _cache.MemberJoined -= _nodeJoined;
                        _nodeJoined = null;
                    }
                    break;



                case NotificationsType.UnregNodeLeftNotif:
                    if (_nodeLeft != null)
                    {
                        _cache.MemberLeft -= _nodeLeft;
                        _nodeLeft = null;
                    }
                    break;

                case NotificationsType.UnregCacheStoppedNotif:
                    this._cacheStoppedEventRegistered = false;
                    break;

             
#if !DEVELOPMENT
                case NotificationsType.UnregHashmapChangedNotif:
                    if (this._hashmapChanged != null)
                    {
                        this._cache.HashmapChanged -= this._hashmapChanged;
                        this._hashmapChanged = null;
                    }
                    break;
#endif


            }

        }

        #endregion

    }
}
