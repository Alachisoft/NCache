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

using System.Collections;
using System.Threading;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.SocketServer.CallbackTasks;
using Alachisoft.NCache.SocketServer.EventTask;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.SocketServer.Util;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.DataStructures;
using System.Net;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Caching.Queries;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Protobuf;
using EnumerationPointer = Alachisoft.NCache.Common.DataStructures.EnumerationPointer;
using Exception = System.Exception;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.MapReduce.Notifications;
using Alachisoft.NCache.Runtime.Caching;

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
        private EventDataFilter _addDataFilter = EventDataFilter.None;
        private EventDataFilter _updateDataFilter = EventDataFilter.None;
        private EventDataFilter _removeDataFilter = EventDataFilter.None;
        private short _addSeq = -1;
        private short _removeSeq = -1;
        private short _updateSeq = -1;

        private object sync_lock_AddDataFilter = new object();
        private object sync_lock_UpdateDataFilter = new object();
        private object sync_lock_RemoveDataFilter = new object();

        private ActiveQueryCallback _activeQuery = null;

        private NodeJoinedCallback _nodeJoined = null;
        private NodeLeftCallback _nodeLeft = null;
        private CacheStoppedCallback _cacheStopped = null;
        private HashmapChangedCallback _hashmapChanged = null;

        private BlockClientActivity _blockClientActivity = null;
        private UnBlockClientActivity _unblockClientActivity = null;

        private Alachisoft.NCache.Caching.TaskCallbackListener _taskCallback = null;

        /// <summary>flag to determine if client has registered to cache stopped event.
        /// if so, client will be notified about cache stop.
        /// We cannot unregister cache stopped event upon client request because it is 
        /// used to dispose client too.</summary>
        private bool _cacheStoppedEventRegistered = false;
        private CacheClientConnectivityChangedCallback _clientConnectivtyChanged;
        private bool _clientConnectivityChangeRegistered;

        internal NCache(string cacheId, bool isDonNetClient, ClientManager client, Runtime.Caching.ClientInfo clientInfo)
            : this(cacheId, isDonNetClient, client, string.Empty, clientInfo)
        {
        }

        /// <summary>
        /// Initialize the cache instance.
        /// </summary>
        internal NCache(string cacheId, bool isDotNetClient, ClientManager client, string licenceInfo, Runtime.Caching.ClientInfo clientInfo)
        {
            this._cacheId = cacheId;
            this._isDotNetClient = isDotNetClient;
            this._client = client;
            this._licenceCode = licenceInfo;
            
            _cache = CacheProvider.Provider.GetCacheInstanceIgnoreReplica(cacheId);
                    
            if (_cache == null) throw new Exception("Cache is not registered");
            
            if (!_cache.IsRunning) throw new Exception("Cache is not running");

            EventHelper.EventDataFormat = _cache.SocketServerDataService;
            
            _onItemUpdatedCallback = new CustomUpdateCallback(CustomUpdate);
            _cache.CustomUpdateCallbackNotif += _onItemUpdatedCallback;

            _onItemRemoveCallback = new CustomRemoveCallback(CustomRemove);
            _cache.CustomRemoveCallbackNotif += _onItemRemoveCallback;

            _pollRequestCallback = new Caching.PollRequestCallback(PollRequest);
            _cache.PollRequestCallbackNotif += _pollRequestCallback;

            _activeQuery = new ActiveQueryCallback(ActiveQueryChanged);
            _cache.ActiveQueryCallbackNotif += _activeQuery;

            if (SocketServer.Logger.IsErrorLogsEnabled)
            {
                SocketServer.Logger.NCacheLog.Error(_cacheserver+".ctor", "Registering cache stopped event for " + _client.ClientID);
            }

            _cacheStopped = new CacheStoppedCallback(OnCacheStopped);
            _cache.CacheStopped += _cacheStopped;

            if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error(_cacheserver+".ctor", "Cache stopped event registered for " + _client.ClientID);

            _asyncOperationCallback = new AsyncOperationCompletedCallback(AsyncOperationCompleted);
            _cache.AsyncOperationCompleted += _asyncOperationCallback;

            _dsUpdatedCallback = new DataSourceUpdatedCallback(DataSourceUpdated);
            _cache.DataSourceUpdated += _dsUpdatedCallback;

            _configModified = new ConfigurationModified(OnConfigModified);
            _cache.ConfigurationModified += _configModified;

            _blockClientActivity = new BlockClientActivity(BlockClientActivity);
            _cache.BlockActivity += this._blockClientActivity;

            _unblockClientActivity = new UnBlockClientActivity(UnBlockClientActivity);
            _cache.UnBlockActivity += this._unblockClientActivity;

            _cache.OnClientConnected(client.ClientID, cacheId, clientInfo);

            _cache.TaskCallback += new Caching.TaskCallbackListener(TaskCallbackHandler);
        }


        void TaskCallbackHandler(string taskId, Alachisoft.NCache.MapReduce.Notifications.TaskCallbackInfo callbackInfo, EventContext eventcontext)
        {
            if (_client != null)
            {
                short taskSts = 0;
                TaskStatus status = eventcontext.TaskStatus;
                if(status == TaskStatus.Success) taskSts = 0;
                else if(status == TaskStatus.Failure) taskSts = 1;
                else if(status == TaskStatus.Cancelled) taskSts = 2;

                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse taskCallback = new Common.Protobuf.BulkEventItemResponse();
                    taskCallback.eventType = Common.Protobuf.BulkEventItemResponse.EventType.TASK_CALLBACK;

                    taskCallback.TaskCallbackEvent = EventHelper.GetTaskCallbackResponse(eventcontext, taskId, taskSts, eventcontext.TaskFailureReason, callbackInfo.CallbackId);

                    // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                    ClientManager client = _client;
                    if (client != null) client.ConnectionManager.EnqueueEvent(taskCallback, _client.SlaveId);
                }
                else
                {
                    lock (ConnectionManager.CallbackQueue)
                    {
                        ConnectionManager.CallbackQueue.Enqueue(new Alachisoft.NCache.SocketServer.CallbackTasks.TaskCallbackTask(_client.ClientID, taskId, taskSts, eventcontext.TaskFailureReason, (short)callbackInfo.CallbackId, eventcontext));
                        Monitor.Pulse(ConnectionManager.CallbackQueue);
                    }
                }
            }
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
                            _addDataFilter = datafilter;
                            _addSeq = sequence;
                        } 
                    }
                    break;
                case NotificationsType.RegRemoveNotif:
                    lock (sync_lock_RemoveDataFilter)
                    {
                        if (_removeSeq < sequence)
                        {
                            _removeDataFilter = datafilter;
                            _removeSeq = sequence;
                        } 
                    }
                    break;
                case NotificationsType.RegUpdateNotif:
                    lock (sync_lock_UpdateDataFilter)
                    {
                        if (_updateSeq < sequence)
                        {
                            _updateDataFilter = datafilter;
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
            // First of all fire the CacheStoppedCallback for the remote client.
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

            // Now break the connection of the socket server with the client.
            if (_client != null) _client.OnCacheStopped(cacheId);
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
                // Just a dummy call to dispose enumerator
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
                    _addDataFilter = EventDataFilter.None;
                    _addSeq = -1;
                }
            }
            if (_itemUpdated != null)
            {
                _cache.ItemUpdated -= _itemUpdated;
                _itemUpdated = null;
                lock (sync_lock_UpdateDataFilter)
                {
                    _updateDataFilter = EventDataFilter.None;
                    _updateSeq = -1;
                }
            }
            if (_itemRemoved != null)
            {
                _cache.ItemRemoved -= _itemRemoved;
                _itemRemoved = null;
                lock (sync_lock_RemoveDataFilter)
                {
                    _removeDataFilter = EventDataFilter.None;
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
            if (_configModified != null)
            {
                _cache.ConfigurationModified -= _configModified;
                _configModified = null;
            }


            if (this._hashmapChanged != null)
            {
                this._cache.HashmapChanged -= this._hashmapChanged;
                _hashmapChanged = null;
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

            if (this._activeQuery != null)
            {
                this._cache.ActiveQueryCallbackNotif -= this._activeQuery;
                this._activeQuery = null;
            }

            if (_clientConnectivtyChanged != null)
            {
                _cache.UnRegisterClientActivityCallback(_client.ClientID);
                _clientConnectivtyChanged = null;
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
                    // client older then 4.1 sp2 private patch 4 does not support bulk Events
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

                        // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                        ClientManager client = _client;
                        if (client != null) client.ConnectionManager.EnqueueEvent(eventitem, _client.SlaveId);
                    }
                    else
                    {
                        lock (ConnectionManager.CallbackQueue)
                        {
                            ConnectionManager.CallbackQueue.Enqueue(new AsyncOpCompletedCallback(opCode, result, _cacheId));
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
                    // Client older then 4.1 sp2 private patch 4 does not support bulk Events
                    if (_client.ClientVersion >= 4124)
                    {
                        Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventitem = new Common.Protobuf.BulkEventItemResponse();
                        eventitem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.ITEM_UPDATED_CALLBACK;
                        eventitem.ItemUpdatedCallback = EventHelper.GetItemUpdatedCallbackResponse(eventContext, (string)key, (short)cbInfo.Callback, cbInfo.DataFilter);

                        // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
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
        /// Called when active query is updated
        /// </summary>
        /// <param name="key">key of the item being updated</param>
        /// <param name="callbackEntry">callback entry that contains the updated value</param>
        private void ActiveQueryChanged(object key, QueryChangeType changeType, List<CQCallbackInfo> activeQueries, EventContext eventContext)
        {
            if (_client != null)
            {
                if (activeQueries != null && activeQueries.Count > 0)
                {
                    lock (ConnectionManager.CallbackQueue)
                    {
                        foreach (CQCallbackInfo queryinfo in activeQueries)
                        {
                            foreach (string clientId in queryinfo.ClientIds)
                            {
                                if (clientId == _client.ClientID)
                                {
                                    // Client older then 4.1 sp2 private patch 4 does not support bulk Events
                                    if (_client.ClientVersion >= 4124)
                                    {
                                        Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                                        eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.CQ_CALLBACK;

                                        EventDataFilter datafilter = EventDataFilter.None;
                                        queryinfo.DataFilters.TryGetValue(clientId, out datafilter);
                                        eventItem.CQCallback = EventHelper.GetCQCallbackResponse(eventContext, (string)key, queryinfo.CQId, changeType, datafilter);

                                        // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                                        ClientManager client = _client;
                                        if (client != null) client.ConnectionManager.EnqueueEvent(eventItem, _client.SlaveId);
                                    }
                                    else
                                    {
                                        EventDataFilter datafilter = EventDataFilter.None;
                                        queryinfo.DataFilters.TryGetValue(clientId, out datafilter);
                                        ConnectionManager.CallbackQueue.Enqueue(new CQCallbackTask(queryinfo.CQId, (string)key, changeType, _client.ClientID, eventContext, datafilter));
                                        
                                        Monitor.Pulse(ConnectionManager.CallbackQueue);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

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
                        // Client older then 4.1 sp2 private patch 4 does not support bulk Events
                        if (_client.ClientVersion >= 4124)
                        {
                            Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                            eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.ITEM_REMOVED_CALLBACK;
                            eventItem.itemRemoveCallback = EventHelper.GetItemRemovedCallbackResponse(eventContext, (short)cbInfo.Callback, (string)key, (UserBinaryObject)val, Flag, reason,cbInfo.DataFilter);

                            // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
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

        private void PollRequest(string clientId, short callbackId, EventType eventType)
        {
            if (_client != null)
            {
                // Client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                    eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.POLL_NOTIFY_EVENT;
                    eventItem.pollNotifyEvent = EventHelper.GetPollNotifyEvent(callbackId, eventType);

                    // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
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
                // Client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventitem = new Common.Protobuf.BulkEventItemResponse();
                    eventitem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.ITEM_ADDED_EVENT;
                    eventitem.itemAddedEvent = EventHelper.GetItemAddedEventResponse(eventContext, (string)key, this._addDataFilter);

                    // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                    ClientManager client = _client;
                    if (client != null) client.ConnectionManager.EnqueueEvent(eventitem, _client.SlaveId);
                }
                else
                {
                    lock (ConnectionManager.CallbackQueue)
                    {
                        if (_client != null)
                        {
                            ConnectionManager.CallbackQueue.Enqueue(new ItemAddedEvent((string)key, _cacheId, _client.ClientID, eventContext, _addDataFilter));
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
                // Client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                    eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.ITEM_UPDATED_EVENT;
                    eventItem.itemUpdatedEvent = EventHelper.GetItemUpdatedEventResponse(eventContext, (string)key, this._updateDataFilter);

                    // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
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
            // Value which arrives will be null
            value = eventContext.Item.Value;

            if (_client != null)
            {
                if (value is CallbackEntry)
                {
                    value = ((CallbackEntry)value).Value;
                }

                // Client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                    eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.ITEM_REMOVED_EVENT;
                    eventItem.itemRemovedEvent = EventHelper.GetItemRemovedEventResponse(eventContext, (string)key, this._removeDataFilter, Flag, reason, (UserBinaryObject)value);

                    // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
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
                // Client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                    eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.CACHE_CLEARED_EVENT;
                    eventItem.cacheClearedEvent = EventHelper.GetCacheClearedResponse(eventContext);

                    // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
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
                // Client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                    eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.CACHE_STOPPED_EVENT;
                    eventItem.cacheStoppedEvent = EventHelper.GetCacheStoppedEventResponse(_cacheId);

                    // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
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

        private void CustomNotification(object notifId, object value, EventContext eventContext)
        {
            if (_client != null)
            {
                // Client older then 4.1 sp2 private patch 4 does not support bulk Events
                if (_client.ClientVersion >= 4124)
                {
                    Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                    eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.RAISE_CUSTOM_EVENT;
                    eventItem.CustomEvent = EventHelper.GetCustomEventResponse((byte[])notifId, (byte[])value);

                    // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
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

        private void DataSourceUpdated(object result, CallbackEntry cbEntry, OpCode operationCode)
        {
            if (cbEntry != null)
            {
                if (_client == null) return;
                AsyncCallbackInfo asyncInfo = cbEntry.WriteBehindOperationCompletedCallback as AsyncCallbackInfo;
                if (asyncInfo.Client == _client.ClientID)
                {
                    // Client older then 4.1 sp2 private patch 4 does not support bulk Events
                    if (_client.ClientVersion >= 4124)
                    {
                        StringBuilder keyPackage = new StringBuilder();
                        keyPackage.AppendFormat("DSUPDATECALLBACK \"{0}\"{1}\"{2}\"", (short)asyncInfo.Callback, operationCode, ((Hashtable)result).Count);

                        Alachisoft.NCache.Common.Protobuf.DSUpdatedCallbackResponse dsUpdatedCallback = EventHelper.GetDSUPdateCallbackResponse((short)asyncInfo.Callback, operationCode, result);

                        Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                        eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.DS_UPDATED_CALLBACK;
                        eventItem.DSUpdatedCallback = dsUpdatedCallback;

                        // To avoid NullReference problem if both evnt and NCache.Dispose are called simultaenously
                        ClientManager client = _client;
                        if (client != null) client.ConnectionManager.EnqueueEvent(eventItem, _client.SlaveId);
                    }
                    else
                    {
                        lock (ConnectionManager.CallbackQueue)
                        {
                            ConnectionManager.CallbackQueue.Enqueue(new DataSourceUpdatedCallbackTask((short)asyncInfo.Callback, result, operationCode, _client.ClientID));
                            Monitor.Pulse(ConnectionManager.CallbackQueue);
                        }
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

        private void OnCacheClientConnectivityChanged(string cacheId, Runtime.Caching.ClientInfo client, ConnectivityStatus status)
        {
            if (_client != null)
            {
                if (_client.ClientVersion >= 4124)
                {
                    BulkEventItemResponse eventItem = new BulkEventItemResponse();
                    eventItem.eventType = BulkEventItemResponse.EventType.CLIENT_CONNECTIVITY;
                    eventItem.clientConnectivityChangeEvent =
                        EventHelper.GetClientConnectivityChangeEventResponse(cacheId, client, status);
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

        #region ICommandExecuter Members

        public string ID
        {
            get
            {
                return _cacheId;
            }
        }

        public void OnClientConnected(string clientID, string cacheId, Runtime.Caching.ClientInfo clientInfo)
        {
            if (_cache != null) _cache.OnClientConnected(clientID, cacheId, clientInfo);
        }

        /// <summary>
        /// This function is called client is dis-connected 
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="cacheId"></param>
        public void OnClientDisconnected(string clientID, string cacheId)
        {
            if (_cache != null) _cache.OnClientDisconnected(clientID, cacheId);
        }

        // Fix for BroadAge 4.1 Sp2 patch 4
        // Forcefully disconnected when Socket is busy for more then configured interval
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
            // We need not to implement this function here have implemented it in NBridge.cs. Just returning true in all cases.
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

                case NotificationsType.RegisterClientConnectivityNotif:

                    _clientConnectivtyChanged = new CacheClientConnectivityChangedCallback(OnCacheClientConnectivityChanged);
                    _cache.RegisterClientActivityCallback(_client.ClientID, _clientConnectivtyChanged);
                    this._clientConnectivityChangeRegistered = true;
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
                            _addDataFilter = EventDataFilter.None;
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
                            _updateDataFilter = EventDataFilter.None;
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
                            _removeDataFilter = EventDataFilter.None;
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

                case NotificationsType.UnregisterClientConnectivityNotif:
                    if (_clientConnectivtyChanged != null)
                    {
                        _cache.UnRegisterClientActivityCallback(_client.ClientID);
                        _clientConnectivtyChanged = null;
                        _clientConnectivityChangeRegistered = false;
                    }
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
