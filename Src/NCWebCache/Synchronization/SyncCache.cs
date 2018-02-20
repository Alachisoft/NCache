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
using Alachisoft.NCache.Caching.CacheSynchronization;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Web.Caching.Util;

namespace Alachisoft.NCache.Web.Synchronization
{
    public class SyncCache : ISyncCache, ICompactSerializable
    {
        private Caching.Cache _syncCache;
        private string _cacheId;
        private string _server;
        private int _port;
        private CacheDataNotificationCallback _updateCallback;
        private CacheDataNotificationCallback _removedCallback;
        private CacheClearedCallback _cacheClearCallback;
        private PollNotificationCallback _pollNotificationCallback;
        private CacheStoppedCallback _cacheStoppedCallback;
        private bool _modeInproc;

        private ArrayList _synEventListeners = new ArrayList();
        private ArrayList _synPollEventListeners = new ArrayList();

        /// <summary>
        /// Given CacheID, server and port creates an instance of SyncCache.
        /// </summary>
        /// <param name="cacheId">Cache ID to sync with.</param>
        /// <param name="server">server to connect.</param>
        /// <param name="port">server port.</param>
        public SyncCache(string cacheId, string server, int port, bool modeInproc)
        {
            _cacheId = cacheId;
            _server = server;
            _port = port;
            _modeInproc = modeInproc;
        }

        /// <summary>
        /// Cache Inproc status in boolean
        /// </summary>
        public bool IsModeInProc
        {
            get { return _modeInproc; }
        }


        #region/            --- CacheEvent listener ----        /

        /// <summary>
        /// Raises CacheCleared event when SyncCache is cleared.
        /// </summary>
        public void OnCacheClear()
        {
            if (_synEventListeners != null)
            {
                foreach (ISyncCacheEventsListener eventListener in _synEventListeners)
                {
                    eventListener.CacheCleared();
                }
            }
        }

        /// <summary>
        /// Raises CacheStopped event when Cache Stops.
        /// </summary>
        /// <param name="caheId">CacheId of the cache stopped.</param>
        public void OnCacheStopped(string caheId)
        {
            if (_synEventListeners != null)
            {
                foreach (ISyncCacheEventsListener eventListener in _synEventListeners)
                {
                    eventListener.CacheStopped(caheId);
                }
            }
        }

        /// <summary>
        /// Raises SyncItemUpdated event when item is updated in the cache.
        /// </summary>
        /// <param name="key">updated key</param>
        public void OnItemChangedCallback(string key, CacheEventArg eventArg)
        {
            if (_synEventListeners != null)
            {
                if (eventArg.EventType == EventType.ItemUpdated)
                {
                    foreach (ISyncCacheEventsListener eventListener in _synEventListeners)
                    {
                        eventListener.SyncItemUpdated(key);
                    }
                }
                else if (eventArg.EventType == EventType.ItemRemoved)
                {
                    foreach (ISyncCacheEventsListener eventListener in _synEventListeners)
                    {
                        eventListener.SyncItemRemoved(key);
                    }
                }
            }
        }

        /// <summary>
        /// Raises SyncItemRemoved event when item is removed.
        /// </summary>
        /// <param name="key">key of the item removed.</param>
        /// <param name="value">value of the item removed.</param>
        /// <param name="reason">reason for the item removed.</param>
        public void OnItemRemovedCallback(string key, CacheEventArg eventArg)
        {
            if (_synEventListeners != null)
            {
                foreach (ISyncCacheEventsListener eventListener in _synEventListeners)
                {
                    eventListener.SyncItemRemoved(key);
                }
            }
        }

        #endregion

        #region/        --- ISyncCache Members  ---                     /

        /// <summary>
        /// Gets the item for the given key.
        /// </summary>
        /// <param name="key">the key to fetch from cache.</param>
        /// <returns>Value of the item. null if item does not exist in the cache.</returns>
        [CLSCompliant(false)]
        public object Get(string key, ref ulong version, ref BitSet flag, ref DateTime absoluteExpiration,
            ref TimeSpan slidingExpiration, ref long size, ref string group, ref string subGroup,
            ref Hashtable queryInfo)
        {
            object val = null;
            CacheItem item = null;
            if (_syncCache != null)
            {
                val = _syncCache.GetSerializedObject(key, DSReadOption.None, ref version, ref flag,
                    ref absoluteExpiration, ref slidingExpiration, ref group, ref subGroup, ref queryInfo);

                item = val as CacheItem;
                if (item != null)
                {
                    size = val is byte[] ? ((byte[]) val).Length : 0;
                    if (_modeInproc)
                    {
                        item.Value =
                            _syncCache.SafeDeserialize(item.Value, _syncCache.SerializationContext, item.FlagMap);
                    }

                    return item.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Registers the Sync Key notification with the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="eventListener">eventlistener to be notified.</param>
        public void RegisterSyncKeyNotifications(string key, ISyncCacheEventsListener eventListener,
            CallbackType callbackType)
        {
            if (!_synEventListeners.Contains(eventListener)) _synEventListeners.Add(eventListener);
            if (_syncCache != null && callbackType == CallbackType.PushBasedNotification)
            {
                _syncCache.RegisterCacheDataNotificationCallback(new string[] {key}, _updateCallback,
                    EventType.ItemUpdated | EventType.ItemRemoved, EventDataFilter.None, true);
            }
        }

        public void RegisterBulkSyncKeyNotifications(string[] keys, ISyncCacheEventsListener eventListener,
            CallbackType callbackType)
        {
            if (!_synEventListeners.Contains(eventListener)) _synEventListeners.Add(eventListener);
            if (_syncCache != null && callbackType == CallbackType.PushBasedNotification)
            {
                _syncCache.RegisterCacheDataNotificationCallback(keys, _updateCallback,
                    EventType.ItemUpdated | EventType.ItemRemoved, EventDataFilter.None, true);
            }
        }

        /// <summary>
        /// Unregister the snckey notifications.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="eventListener">eventlistener to unregister.</param>
        public void UnRegisterSyncKeyNotifications(string key, ISyncCacheEventsListener eventListener)
        {
            if (!_synEventListeners.Contains(eventListener)) _synEventListeners.Remove(eventListener);
        }

        /// <summary>
        /// Unregister key based notification in bulk
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="eventListener"></param>
        public void UnRegisterBulkSyncKeyNotifications(string[] keys, ISyncCacheEventsListener eventListener)
        {
            if (!_synEventListeners.Contains(eventListener)) _synEventListeners.Remove(eventListener);
        }

        /// <summary>
        /// CacheId
        /// </summary>
        public string CacheId
        {
            get
            {
                if (_syncCache != null) return _syncCache.CacheId;
                return null;
            }
            set { }
        }

        /// <summary>
        /// Initializes the SyncCache.
        /// </summary>
        public void Initialize()
        {
            try
            {
                CacheInitParams initParams = new CacheInitParams();
                initParams.Server = _server;
                initParams.Port = _port;
                initParams.LoadBalance = true;
                _syncCache = Caching.NCache.InitializeCache(_cacheId, initParams, false);
                _updateCallback = new CacheDataNotificationCallback(OnItemChangedCallback);
                _removedCallback = new CacheDataNotificationCallback(OnItemRemovedCallback);
                _cacheClearCallback = new CacheClearedCallback(OnCacheClear);
                _cacheStoppedCallback = new CacheStoppedCallback(OnCacheStopped);
                _synEventListeners = new ArrayList();

                if (_syncCache != null)
                {
                    _syncCache.ExceptionsEnabled = true;
                    _syncCache.CacheCleared += _cacheClearCallback;
                    _syncCache.CacheStopped += _cacheStoppedCallback;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to initialize " + _cacheId + " while creating CacheSyncDependency", ex);
            }
        }

        /// <summary>
        /// Disposes this SyncCache Instance.
        /// </summary>
        public void Dispose()
        {
            // We dispose the cache(client) asynchronously in order to avoid Deadlock at server end
            // because of both client cache as outproc and clustered cache on the same server
            if (_syncCache != null)
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(DisposeInternal),
                    _syncCache);
        }

        private void DisposeInternal(Object param)
        {
            try
            {
                ((Caching.Cache) param).Dispose();
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _cacheId = reader.ReadString();
            _server = reader.ReadString();
            _port = reader.ReadInt32();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_cacheId);
            writer.Write(_server);
            writer.Write(_port);
        }

        #endregion


        public CallbackType GetNotificationType(string cacheId)
        {
            string pollingType = ConfigReader.ReadItem("client.ncconf", cacheId, "notification-type");
            switch (pollingType)
            {
                case "polling":
                    return CallbackType.PullBasedCallback;
                case "push":
                    return CallbackType.PushBasedNotification;
                default:
                    return CallbackType.PushBasedNotification;
            }
        }


        public static Cache GetCache(string cacheName)
        {
            if (Caching.NCache.Caches.Contains(cacheName))
                return Caching.NCache.Caches.GetCache(cacheName);
            return null;
        }

        public static Cache InitializeInternally(string cacheName, CacheInitParams iparams,
            bool isPessim)
        {
            return Caching.NCache.InitializeCache(cacheName, iparams, isPessim);
        }
    }
}