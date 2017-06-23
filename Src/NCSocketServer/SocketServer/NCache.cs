// Copyright (c) 2017 Alachisoft
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
using System.Threading;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.CallbackTasks;
using Alachisoft.NCache.SocketServer.EventTask;
using Alachisoft.NCache.SocketServer.Util;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.DataStructures;

namespace Alachisoft.NCache.SocketServer
{
    internal sealed class NCache : ICommandExecuter
    {
        string _cacheserver="NCache";
        private ClientManager _client;
        private bool _isDotNetClient;
       

        private Cache _cache = null;
        private string _cacheId = null;
        private string _licenceCode = string.Empty;

        private CustomUpdateCallback _onItemUpdatedCallback = null;
        private CustomRemoveCallback _onItemRemoveCallback = null;
        private ConfigurationModified _configModified = null;
        private CacheStoppedCallback _cacheStopped = null;
        private NodeJoinedCallback _nodeJoined = null;
        private NodeLeftCallback _nodeLeft = null;

        private HashmapChangedCallback _hashmapChanged = null;

        /// <summary>flag to determine if client has registered to cache stopped event.
        /// if so, client will be notified about cache stop.
        /// We cannot unregister cache stopped event upon client request because it is 
        /// used to dispose client too.</summary>
        private bool _cacheStoppedEventRegistered = false;

        internal NCache(string cacheId, bool isDonNetClient, ClientManager client)
            : this(cacheId, isDonNetClient, client, string.Empty)
        {
		}

        /// <summary>
        /// Initialize the cache instance.
        /// </summary>
        internal NCache(string cacheId, bool isDotNetClient, ClientManager client, string licenceInfo)
        {
            this._cacheId = cacheId;
            this._isDotNetClient = isDotNetClient;
            this._client = client;
            this._licenceCode = licenceInfo;
           

            try
            {
              _cache = CacheProvider.Provider.GetCacheInstanceIgnoreReplica(cacheId);
            }
            catch (Exception) { throw; }

            if (_cache == null) throw new Exception("Cache is not registered");
            if (!_cache.IsRunning) throw new Exception("Cache is not running");

                    
            _onItemUpdatedCallback = new CustomUpdateCallback(CustomUpdate);
            _cache.CustomUpdateCallbackNotif += _onItemUpdatedCallback;

            _onItemRemoveCallback = new CustomRemoveCallback(CustomRemove);
            _cache.CustomRemoveCallbackNotif += _onItemRemoveCallback;

            if (SocketServer.Logger.IsErrorLogsEnabled)
            {
                SocketServer.Logger.NCacheLog.Error(_cacheserver+".ctor", "Registering cache stopped event for " + _client.ClientID);
            }

            _cacheStopped = new CacheStoppedCallback(OnCacheStopped);
            _cache.CacheStopped += _cacheStopped;

            if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error(_cacheserver+".ctor", "Cache stopped event registered for " + _client.ClientID);
            _cache.ClientsInvalidated += new ClientsInvalidatedCallback(OnClientsInvalidated);
            
            _cache.OnClientConnected(client.ClientID, cacheId);
            
        }

        void OnClientsInvalidated(ArrayList invalidatedClientsList)
        {
            //invalidate the clients...
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
        internal Cache Cache
        {
            get { return _cache; }
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
            //break the connection of the socket server with the client.
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
                _client = null;
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

            if (_cacheStopped != null)
            {
                _cache.CacheStopped -= _cacheStopped;
                _cacheStopped = null;
            }
            if (_hashmapChanged != null)
            {
                this._cache.HashmapChanged -= this._hashmapChanged;
                _hashmapChanged = null;
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
                        if (_client.ClientVersion > 4124)
                        {
                            Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem = new Common.Protobuf.BulkEventItemResponse();
                            eventItem.eventType = Common.Protobuf.BulkEventItemResponse.EventType.ITEM_REMOVED_CALLBACK;
                            eventItem.itemRemoveCallback = EventHelper.GetItemRemovedCallbackResponse(eventContext, (short)cbInfo.Callback, (string)key, (UserBinaryObject)val, Flag, reason,cbInfo.DataFilter);

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


        #region ICommandExecuter Members

        public string ID
        {
            get
            {
                return _cacheId;
            }
        }

        public void OnClientConnected(string clientID, string cacheId)
        {
            if (_cache != null) _cache.OnClientConnected(clientID, cacheId);
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

        //fix for BroadAge 4.1 Sp2 patch 4
        //forcefully disconnected when Socket is busy for more then configured interval
        public void OnClientForceFullyDisconnected(string clientId)
        {
            if (_cache != null) _cache.OnClientForceFullyDisconnected(clientId);
        }

       

        public bool IsCoordinator(string srcCacheID)
        {
           
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
                case NotificationsType.RegHashmapChangedNotif:
                    if (this._hashmapChanged == null)
                    {
                        this._hashmapChanged = new HashmapChangedCallback(HashmapChanged);
                        this._cache.HashmapChanged += this._hashmapChanged;
                    }
                    break;
                case NotificationsType.UnregHashmapChangedNotif:
                    if (this._hashmapChanged != null)
                    {
                        this._cache.HashmapChanged -= this._hashmapChanged;
                        this._hashmapChanged = null;
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


            }
        }

        #endregion

    }
}
