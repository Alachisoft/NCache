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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Caching.Messaging;

using Alachisoft.NCache.Caching.Statistics;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
#endif
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Propagator;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Topologies.Clustered;
using Alachisoft.NCache.Common.Util;


using Alachisoft.NCache.Persistence;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using EventTypeInternal = Alachisoft.NCache.Caching.Events.EventTypeInternal;
using RequestStatus = Alachisoft.NCache.Common.DataStructures.RequestStatus;

using Alachisoft.NCache.Common.Pooling;

using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Util;

namespace Alachisoft.NCache.Caching.Topologies
{
    /// <summary>
    /// Base class for all cache implementations. Implements the ICache interface. 
    /// </summary>

    internal class CacheBase : ICache, IMessageStore, IDisposable
    {
        #region	/                 --- Inner/Nested Classes ---           /

        /// <summary>
        /// Asynchronous notification dispatcher.
        /// </summary>
        private class AsyncLocalNotifyAdd : AsyncProcessor.IAsyncTask
        {
            /// <summary> The listener class </summary>
            private ICacheEventsListener _listener;

            /// <summary> Message to broadcast </summary>
            private object _key;
            private OperationContext _operationContext;
            private EventContext _eventContext;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="listener"></param>
            /// <param name="data"></param>
            public AsyncLocalNotifyAdd(ICacheEventsListener listener, object key, OperationContext operationContext, EventContext eventContext)
            {
                _listener = listener;
                _key = key;
                _operationContext = operationContext;
                _eventContext = eventContext;
            }

            /// <summary>
            /// Implementation of message sending.
            /// </summary>

            void AsyncProcessor.IAsyncTask.Process()
            {
                //_listener.OnItemAdded(_key, _operationContext, _eventContext);
            }

        }

        /// <summary>
        /// Asynchronous notification dispatcher.
        /// </summary>
        private class AsyncLocalNotifyCacheClear : AsyncProcessor.IAsyncTask
        {
            /// <summary> The listener class </summary>
            private ICacheEventsListener _listener;
            private OperationContext _operationContext;
            private EventContext _eventContext;
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="listener"></param>
            /// <param name="data"></param>
            public AsyncLocalNotifyCacheClear(ICacheEventsListener listener, OperationContext operationContext, EventContext eventContext)
            {
                _listener = listener;
                _operationContext = operationContext;
                _eventContext = eventContext;
            }

            /// <summary>
            /// Implementation of message sending.
            /// </summary>
            /// 

            void AsyncProcessor.IAsyncTask.Process()
            {
                _listener.OnCacheCleared(_operationContext, _eventContext);
            }

        }

        /// <summary>
        /// Asynchronous notification dispatcher.
        /// </summary>
        private class AsyncLocalNotifyUpdate : AsyncProcessor.IAsyncTask
        {
            /// <summary> The listener class </summary>
            private ICacheEventsListener _listener;

            /// <summary> Message to broadcast </summary>
            private object _key;

            private OperationContext _operationContext;
            private EventContext _eventContext;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="listener"></param>
            /// <param name="data"></param>
            public AsyncLocalNotifyUpdate(ICacheEventsListener listener, object key, OperationContext operationContext, EventContext eventContext)
            {
                _listener = listener;
                _key = key;
                _operationContext = operationContext;
                _eventContext = eventContext;
            }

            /// <summary>
            /// Implementation of message sending.
            /// </summary>

            void AsyncProcessor.IAsyncTask.Process()
            {
              //  _listener.OnItemUpdated(_key, _operationContext, _eventContext);
            }

        }


        /// <summary>
        /// Asynchronous notification dispatcher.
        /// </summary>
        private class AsyncLocalNotifyRemoval : AsyncProcessor.IAsyncTask
        {
            /// <summary> The listener class </summary>
            private ICacheEventsListener _listener;

            /// <summary> Message to broadcast </summary>
            private object _key, _value;
            private ItemRemoveReason _reason = ItemRemoveReason.Removed;

            private OperationContext _operationContext;
            private object _eventContext;
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="listener"></param>
            /// <param name="data"></param>
            public AsyncLocalNotifyRemoval(ICacheEventsListener listener, object key, object value, ItemRemoveReason reason, OperationContext operationContext, object eventContext)
            {
                _listener = listener;
                _key = key;
                _value = value;
                _reason = reason;
                _operationContext = operationContext;
                _eventContext = eventContext;
            }

            /// <summary>
            /// Implementation of message sending.
            /// </summary>

            void AsyncProcessor.IAsyncTask.Process()
            {
                if (_key is object[])
                {
                    _listener.OnItemsRemoved((object[])_key,
                        (object[])_value, _reason, _operationContext, (EventContext[])_eventContext);
                }
                else
                {
                    _listener.OnItemRemoved(_key, _value, _reason, _operationContext, (EventContext)_eventContext);
                }
            }

        }

      

        /// <summary>
        /// Asynchronous notification dispatcher.
        /// </summary>
        private class AsyncLocalNotifyCustomEvent : AsyncProcessor.IAsyncTask
        {
            /// <summary> The listener class </summary>
            private ICacheEventsListener _listener;

            /// <summary> Message to broadcast </summary>
            private object _notifId, _data;
            private OperationContext _operationContext;
            private EventContext _eventContext;
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="listener"></param>
            /// <param name="data"></param>
            public AsyncLocalNotifyCustomEvent(ICacheEventsListener listener, object notifId, object data, OperationContext operationContext, EventContext eventContext)
            {
                _listener = listener;
                _notifId = notifId;
                _data = data;
                _operationContext = operationContext;
                _eventContext = eventContext;
            }

            /// <summary>
            /// Implementation of message sending.
            /// </summary>

            void AsyncProcessor.IAsyncTask.Process()
            {
                {
                    _listener.OnCustomEvent(_notifId, _data, _operationContext, _eventContext);
                }
            }

        }

        /// <summary>
        ///Asynchronous notification dispatcher. 
        /// </summary>
        private class AsyncLocalNotifyUpdateCallback : AsyncProcessor.IAsyncTask
        {
            /// <summary> The listener class </summary>
            private ICacheEventsListener _listener;

            /// <summary> Message to broadcast </summary>
            private object _key;

            private object _entry;
            private OperationContext _operationContext;
            private EventContext _eventContext;
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="listener"></param>
            /// <param name="data"></param>
            public AsyncLocalNotifyUpdateCallback(ICacheEventsListener listener, object key, object entry, OperationContext operationContext, EventContext eventContext)
            {
                _listener = listener;
                _key = key;
                _entry = entry;
                _operationContext = operationContext;
                _eventContext = eventContext;
            }

            /// <summary>
            /// Implementation of message sending.
            /// </summary>

            void AsyncProcessor.IAsyncTask.Process()
            {
                _listener.OnCustomUpdateCallback(_key, _entry, _operationContext, _eventContext);
            }

        }

        /// <summary>
        ///Asynchronous notification dispatcher. 
        /// </summary>
        private class AsyncLocalNotifyRemoveCallback : AsyncProcessor.IAsyncTask
        {
            /// <summary> The listener class </summary>
            private ICacheEventsListener _listener;

            /// <summary> Message to broadcast </summary>
            private object _key;

            /// <summary> Callbackentry, having callback info. </summary>
            private object _entry;

            /// <summary> reaon for item removal </summary>
            private ItemRemoveReason _reason;
            private OperationContext _operationContext;
            private EventContext _eventContext;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="listener"></param>
            /// <param name="data"></param>
            public AsyncLocalNotifyRemoveCallback(ICacheEventsListener listener, object key, object entry, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext)
            {
                _listener = listener;
                _key = key;
                _entry = entry;
                _reason = reason;
                _operationContext = operationContext;
                _eventContext = eventContext;
            }

            /// <summary>
            /// Implementation of message sending.
            /// </summary>

            void AsyncProcessor.IAsyncTask.Process()
            {
                _listener.OnCustomRemoveCallback(_key, _entry, _reason, _operationContext, _eventContext);
            }

        }
        /// <summary>
        ///Asynchronous notification dispatcher. 
        /// </summary>
        private class AsyncLocalPollRequestCallback : AsyncProcessor.IAsyncTask
        {
            /// <summary> The listener class </summary>
            private ICacheEventsListener _listener;

            /// <summary> Message to broadcast </summary>
            private string _clientId;

            private short _callbackId;
            private EventTypeInternal _eventType;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="listener"></param>
            /// <param name="data"></param>
            public AsyncLocalPollRequestCallback(ICacheEventsListener listener, string clientId, short callbackId, EventTypeInternal eventType)
            {
                _listener = listener;
                _clientId = clientId;
                _callbackId = callbackId;
                _eventType = eventType;
            }

            /// <summary>
            /// Implementation of message sending.
            /// </summary>
            void AsyncProcessor.IAsyncTask.Process()
            {
                _listener.OnPollNotify(_clientId, _callbackId, _eventType);
            }
        }

#if !CLIENT && !DEVELOPMENT

        /// <summary>
        /// Asynchronous hashmap notification dispatcher
        /// </summary>
        private class AsyncLocalNotifyHashmapCallback : AsyncProcessor.IAsyncTask
        {
            private ICacheEventsListener _listener;
            private NewHashmap _hashMap;
            private bool _updateClientMap;


            public AsyncLocalNotifyHashmapCallback(ICacheEventsListener listener, long lastViewid, Hashtable newmap, ArrayList members, bool updateClientMap, bool forcefullUpdate)
            {
                this._listener = listener;
                this._hashMap = new NewHashmap(lastViewid, newmap, members);
                this._hashMap.ForcefulUpdate = forcefullUpdate;
                this._updateClientMap = updateClientMap;

            }

            #region IAsyncTask Members

            void AsyncProcessor.IAsyncTask.Process()
            {
                _listener.OnHashmapChanged(this._hashMap, this._updateClientMap);
            }

            #endregion
        }
#endif

        /// <summary>
        /// Asynchronous task notification dispatcher.
        /// </summary>
        private class AsyncLocalNotifyTaskCallback : AsyncProcessor.IAsyncTask
        {

            private ICacheEventsListener _listener;
            private string _taskID;
            private Object _entry;
            private OperationContext _operationContext;
            private EventContext _eventContext;

            public AsyncLocalNotifyTaskCallback(ICacheEventsListener listener, string taskID, Object entry, OperationContext operationContext, EventContext eventContext)
            {
                _listener = listener;
                _taskID = taskID;
                _entry = entry;
                _operationContext = operationContext;
                _eventContext = eventContext;
            }

            public void Process()
            {
            }

        }


        #endregion

        /// <summary>
        /// Enumeration that defines flags for various notifications.
        /// </summary>
        [Flags]
        public enum Notifications
        {
            None = 0x0000,
            ItemAdd = 0x0001,
            ItemUpdate = 0x0002,
            ItemRemove = 0x0004,
            CacheClear = 0x0008,
            All = ItemAdd | ItemUpdate | ItemRemove | CacheClear
        }

        /// <summary> Name of the cache </summary>
        private string _name = string.Empty;

        /// <summary> listener of Cache events. </summary>
        private ICacheEventsListener _listener;

        /// <summary> Reader, writer lock manager for keys. </summary>
        [CLSCompliant(false)]
        protected KeyLockManager<object> _keyLockManager;

        /// <summary> Reader, writer lock to be used for synchronization. </summary>
        [CLSCompliant(false)]
        protected ReaderWriterLock _syncObj;

        /// <summary> The runtime context associated with the current cache. </summary>
        [CLSCompliant(false)]
        internal CacheRuntimeContext _context;

        /// <summary> Flag that controls notifications. </summary>
        private Notifications _notifiers = Notifications.None;

        private int _eventExpiryTime = 15;

        private bool _isInProc = false;

        private bool _keepDeflattedObjects = false;

        public System.IO.StreamWriter writer;

        public IAlertPropagator alertPropagator;

        protected System.Collections.Specialized.OrderedDictionary _cacheConnectedClients = new System.Collections.Specialized.OrderedDictionary();
        public Cache Parent { get; set; }

      
        private bool _enableGeneralEvents = false;
        private EventManager _eventManager = new EventManager();
        public CacheStatistics stats = new CacheStatistics();
        public ArrayList ClientsInfoList = ArrayList.Synchronized(new ArrayList());
        private static int oldClients = 0;
       
       


        /// <summary>
        /// Default constructor.
        /// </summary>
        protected CacheBase()
        {
        }

        /// <summary>
        /// Overloaded constructor. Takes the listener as parameter.
        /// </summary>
        /// <param name="listener">listener of Cache events.</param>
        public CacheBase(IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
        {
            _context = context;
            _listener = listener;
            _syncObj = new ReaderWriterLock();
            _keyLockManager = new KeyLockManager<object>(); 
          

       
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (writer != null)
            {
                lock (writer)
                {
                    writer.Close();
                    writer = null;
                }
            }
            _listener = null;
            _keyLockManager = null;
            _syncObj = null;
            GC.SuppressFinalize(this);
        }

        internal virtual void StopServices()
        {
        }

        #endregion

        public bool IsInProc
        {
            get { return _isInProc; }
            set { _isInProc = value; }
        }

        public bool KeepDeflattedValues
        {
            get { return _keepDeflattedObjects; }
            set { _keepDeflattedObjects = value; }
        }

        /// <summary>
        /// get/set the name of the cache.
        /// </summary>
        public virtual string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public ILogger NCacheLog
        {
            get { return _context.NCacheLog; }
        }

        /// <summary>
        /// Gets the Cache Runtime context.
        /// </summary>
        public CacheRuntimeContext Context
        {
            get { return _context; }
        }
        /// <summary>
        /// get/set listener of Cache events. 'null' implies no listener.
        /// </summary>
        public virtual ICacheEventsListener Listener
        {
            get { return _listener; }
            set { _listener = value; }
        }

        /// <summary>
        /// Notifications are enabled.
        /// </summary>
        public virtual Notifications Notifiers
        {
            get { return _notifiers; }
            set { _notifiers = value; }
        }

        public KeyLockManager<object> KeyLocker
        {
            get { return _keyLockManager; }
        }

        /// <summary>
        /// get the synchronization object for this store.
        /// </summary>
        public ReaderWriterLock Sync
        {
            get { return _syncObj; }
        }

        /// <summary>
        /// returns the number of objects contained in the cache.
        /// </summary>
        public virtual long Count
        {
            get { return 0; }
        }

        /// <summary>
        /// Get the size of data in store, in bytes.
        /// </summary>
        internal virtual long Size
        {
            get
            {
                return 0;
            }
        }

        public virtual long SessionCount
        {
            get { return 0; }
        }

        public virtual int ServersCount
        {
            get { return 0; }
        }

        public virtual bool IsServerNodeIp(Address clientAddress)
        {
            return false;
        }

        public virtual IPAddress ServerJustLeft
        {
            get { return null; }
            set {; }
        }

        /// <summary>
        /// returns the statistics of the Clustered Cache. 
        /// </summary>
        public virtual CacheStatistics Statistics
        {
            get { return null; }
        }

        public virtual List<CacheNodeStatistics> GetCacheNodeStatistics()
        {
            return null;
        }
        internal virtual CacheStatistics ActualStats
        {
            get { return null; }
        }

        public virtual TypeInfoMap TypeInfoMap
        {
            get { return null; }
        }

        public virtual bool IsEvictionAllowed
        {
            get { return true; }
            set { }
        }


        /// <summary>
        /// Returns true if cache is started as backup mirror cache, false otherwise.
        /// </summary>
        public virtual bool IsStartedAsMirror
        {
            get { return false; }
        }

        /// <summary>
        /// Returns true if cache is started as backup mirror cache, false otherwise.
        /// In Case of replica space will not be checked at storage level
        /// </summary>
        /// 
        public virtual bool VirtualUnlimitedSpace
        {
            get { return false; }
            set {; }
        }


        /// <summary> 
        /// Returns the cache local to the node, i.e., internal cache.
        /// </summary>
        protected internal virtual CacheBase InternalCache
        {
            get { return null; }
        }

        internal virtual long MaxSize
        {
            get { return 0; }
            set { }
        }

        internal virtual float EvictRatio
        {
            get { return 0; }
            set { }
        }

        internal virtual void UpdateLockInfo(object key, object lockId, DateTime lockDate, LockExpiration lockExpiration, OperationContext operationContext)
        {
        }

        internal virtual bool CanChangeCacheSize(long size)
        {
            if (_context != null && _context.CacheImpl != null && _context.CacheImpl.InternalCache != null)
                return _context.CacheImpl.InternalCache.CanChangeCacheSize(size);

            return false;
        }

        internal virtual void EnqueueForReplication(object key, int opCode, object data)
        {
        }

        internal virtual void EnqueueForReplication(object key, int opCode, object data, int size, Array userPayLoad, long payLoadSize)
        {
        }

        internal virtual bool RequiresReplication
        {
            get { return false; }
        }

        internal virtual bool IsBucketFunctionalOnReplica(string key)
        {
            return false;
        }

        /// <summary>
        /// This method is called when a cache which was the target of a bridge becomes
        /// active. 
        /// </summary>
        internal virtual void CacheBecomeActive() { }


        public virtual Array Keys
        {
            get
            {
                return null;
            }
        }
        

     

        /// <summary>
        /// Removes a bucket completely.
        /// </summary>
        /// <param name="bucket"></param>
        public virtual void RemoveBucket(int bucket)
        {

        }

        public ArrayList ActiveServers
        {
            get { return null; }
        }

        /// <summary>
        /// Removes all the extra buckets that do not belong to this instance; according to 
        /// BucketOwnerShipMap.
        /// </summary>
        /// <param name="bucketIds"></param>
        public virtual void RemoveExtraBuckets(ArrayList bucketIds)
        { }

        public virtual void DoWrite(string module, string message, OperationContext operationContext)
        {
            if (writer != null)
            {
                lock (writer)
                {
                    writer.WriteLine("[" + module + "]" + message + "\t" + DateTime.Now.ToLongTimeString());
                }
            }
        }

        #region	/                 --- Initialization ---           /

        /// <summary>
        /// Method that allows the object to initialize itself. Passes the property map down 
        /// the object hierarchy so that other objects may configure themselves as well..
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        protected virtual void Initialize(IDictionary cacheClasses, IDictionary properties)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {

                Name = Convert.ToString(properties["id"]);

                if (properties.Contains("notifications"))
                {
                    IDictionary notifconfig = properties["notifications"] as IDictionary;
                    if (notifconfig.Contains("cache-clear"))
                    {
                        if (Convert.ToBoolean(notifconfig["cache-clear"]))
                            _notifiers |= Notifications.CacheClear;
                    }
                    if (notifconfig.Contains("expiration-time"))
                    {
                        _eventExpiryTime = Convert.ToInt32(notifconfig["expiration-time"]);
                    }
                }
                else
                    _notifiers |= Notifications.All;


            }
            catch (ConfigurationException e)
            {
                Dispose();
                throw;
            }
            catch (Exception e)
            {
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }

        public virtual void Initialize(IDictionary cacheClasses, IDictionary properties, bool twoPhaseInitialization)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                Name = Convert.ToString(properties["id"]);

                if (properties.Contains("notifications"))
                {
                    IDictionary notifconfig = properties["notifications"] as IDictionary;

                    if (notifconfig.Contains("cache-clear"))
                    {
                        if (Convert.ToBoolean(notifconfig["cache-clear"]))
                            _notifiers |= Notifications.CacheClear;
                    }
                }
                else
                    _notifiers |= Notifications.All;

            }
            catch (ConfigurationException e)
            {
                Dispose();
                throw;
            }
            catch (Exception e)
            {
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }

        #endregion

        public virtual void ClientConnected(string client, bool isInproc, ClientInfo clientInfo)
        {

            stats = InternalCache.Statistics;

            if (stats != null && stats.ConnectedClients != null)
            {
                lock (stats.ConnectedClients.SyncRoot)
                {
                    if (!stats.ConnectedClients.Contains(client))
                    {
                        stats.ConnectedClients.Add(client);
                    }

                    if (clientInfo.ClientVersion < 5000)
                    {
                        Interlocked.Increment(ref oldClients);
                        if (!ClientsInfoList.Contains(client))
                        {
                            ClientsInfoList.Add(client);
                        }
                        if (ClientsInfoList.Count==1)
                        {
                            OperationContext operationContext = null;

                            try
                            {
                                _eventManager.StartPolling(_context, operationContext);
                            }
                            finally
                            {
                                MiscUtil.ReturnOperationContextToPool(operationContext, Context.TransactionalPoolManager);
                                operationContext?.MarkFree(NCModulesConstants.LocalCache);
                            }
                        }

                       
                      
                    }
                }
            }

            if(clientInfo.IPAddress != null && client != null)
            {
                lock (_cacheConnectedClients)
                {                   
                    if (_cacheConnectedClients.Contains(clientInfo.IPAddress))
                    {
                        IList<string> list = _cacheConnectedClients[clientInfo.IPAddress] as List<string>;
                        if (list != null)
                            list.Add(client);
                    }
                    else
                        _cacheConnectedClients.Add(clientInfo.IPAddress, new List<string>() { client });
                    
                }
            }

        }
        public virtual void ClientDisconnected(string client, bool isInproc, Runtime.Caching.ClientInfo clientInfo)
        {
            CacheStatistics stats = InternalCache.Statistics;
            if (stats != null && stats.ConnectedClients != null)
            {
                lock (stats.ConnectedClients.SyncRoot)
                {
                    if (stats.ConnectedClients.Contains(client))
                    {
                        stats.ConnectedClients.Remove(client);
                    }
                }
                if (ClientsInfoList.Contains(client))
                {
                    Interlocked.Decrement(ref oldClients);
                    lock (ClientsInfoList.SyncRoot)
                    {
                        ClientsInfoList.Remove(client);
                    }
                }


                if (oldClients == 0)
                {
                    _eventManager.StopPolling();
                   
                }

            }
            
            if (clientInfo.IPAddress != null && client != null)
            {
                lock (_cacheConnectedClients)
                {
                    if (_cacheConnectedClients.Contains(clientInfo.IPAddress))
                    {
                        IList<string> list = _cacheConnectedClients[clientInfo.IPAddress] as List<string>;
                        if (list != null)
                            list.Remove(client);

                        if (list == null || list.Count == 0)
                            _cacheConnectedClients.Remove(clientInfo.IPAddress);
                    }
                }
            }
        }

        #region/                --- Custom Callback Registration ---            /

        public virtual void RegisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext) { }
        public virtual void RegisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext) { }
      
        public virtual void UnregisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext) { }
        public virtual void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext) { }
      
        #endregion

       

        

        

       
        public virtual OrderedDictionary GetMessageList(int bucketId,bool includeEventMessages)
        {
            return null;
        } 
        public virtual long CurrentViewId
        {
            get { return 0; }
        }

        public virtual ulong OperationSequenceId
        {
            get { return 0; }
        }


       

#if !DEVELOPMENT
        public virtual NewHashmap GetOwnerHashMapTable(out int bucketSize)
        {
            bucketSize = 0;
            return null;
        }
#endif


        #region	/                 --- ICache ---           /

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public virtual void Clear(Caching.Notifications notification, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
        {
        }

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public virtual void Clear(Caching.Notifications notification, DataSourceUpdateOptions updateOptions, string taskId, OperationContext operationContext)
        {
        }

     

        public virtual bool Contains(object key, string group, OperationContext operationContext)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element 
        /// with the specified key; otherwise, false.</returns>
        public virtual bool Contains(object key, OperationContext operationContext)
        {
            return Contains(key, null, operationContext);
        }

        public virtual CacheEntry Get(object key, OperationContext operationContext)
        {
            Object lockId = null;
            DateTime lockDate = DateTime.Now;
            ulong version = 0;
            return Get(key, ref version, ref lockId, ref lockDate, null, LockAccessType.IGNORE_LOCK, operationContext);
        }

        public virtual CacheEntry Get(object key, bool isUserOperation, OperationContext operationContext)
        {
            Object lockId = null;
            DateTime lockDate = DateTime.Now;
            ulong version = 0;

            return Get(key, isUserOperation, ref version, ref lockId, ref lockDate, null, LockAccessType.IGNORE_LOCK, operationContext);
        }

        public virtual CacheEntry Get(object key, bool isUserOperation, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            return null;
        }


        public virtual IDictionary GetEntryAttributeValues(object key, IList<string> columns, OperationContext operationContext)
        {
            return null;
        }
        
        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        public virtual CacheEntry Get(object key, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            lockId = null;
            lockDate = DateTime.Now;
            return null;
        }
        public virtual HashVector GetTagData(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            return null;
        }

        internal virtual ICollection GetTagKeys(string[] tags, TagComparisonType comparisonType, OperationContext operationContext)
        {
            return null;
        }

        public virtual Hashtable Remove(string[] tags, TagComparisonType tagComparisonType, bool notify, OperationContext operationContext)
        {
            return null;
        }

        public virtual LockOptions Lock(object key, LockExpiration lockExpiration, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            lockId = null;
            lockDate = DateTime.Now;
            return null;
        }

        public virtual LockOptions IsLocked(object key, ref object lockId, ref DateTime lockDate, OperationContext operationContext)
        {
            lockId = null;
            lockDate = DateTime.Now;
            return null;
        }

        public virtual void UnLock(object key, object lockId, bool isPreemptive, OperationContext operationContext)
        {
        }

        /// <summary>
        /// Get the item size stored in cache
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>item size</returns>
        public virtual int GetItemSize(object key)
        {
            return 0;
        }

        /// <summary>
        /// Returns the list of keys in the group or sub group
        /// </summary>
        /// <param name="group">group for which keys are required</param>
        /// <param name="subGroup">sub group within the group</param>
        /// <returns>list of keys in the group</returns>
        public virtual ArrayList GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Checks whether data group exist or not.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public virtual bool GroupExists(string group)
        {
            return false;
        }

        /// <summary>
        /// Gets the group information of the item.
        /// </summary>
        /// <param name="key">Key of the item</param>
        /// <returns>GroupInfo for the item.</returns>
        public virtual DataGrouping.GroupInfo GetGroupInfo(object key, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Gets the group information of the items.
        /// </summary>
        /// <param name="key">Keys of the item</param>
        /// <returns>GroupInfo for the item.</returns>
        public virtual Hashtable GetGroupInfoBulk(object[] keys, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Gets list of data groups.
        /// </summary>
        public virtual ArrayList DataGroupList
        {
            get { return null; }
        }

        /// <summary>
        /// Returns the list of key and value pairs in the group or sub group
        /// </summary>
        /// <param name="group">group for which data is required</param>
        /// <param name="subGroup">sub group within the group</param>
        /// <returns>list of key and value pairs in the group</returns>
        public virtual HashVector GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            return null;
        }
        public virtual CacheEntry GetGroup(object key, string group, string subGroup, OperationContext operationContext)
        {
            object lockId = null;
            DateTime lockDate = DateTime.Now;
            ulong version = 0;
            return GetGroup(key, group, subGroup, ref version, ref lockId, ref lockDate, null, LockAccessType.IGNORE_LOCK, operationContext);
        }

        public virtual CacheEntry GetGroup(object key, string group, string subGroup, ref ulong version, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>returns the result of operation.</returns>
        public virtual CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
        {
            return CacheAddResult.Failure;
        }

        public virtual CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, string taskId, OperationContext operationContext)
        {
            return CacheAddResult.Failure;
        }

        public virtual CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, bool isUserOperation, OperationContext operationContext)
        {
            return CacheAddResult.Failure;
        }

        public virtual void SetStateTransferKeyList(Hashtable keylist)
        {
        }

        public virtual void UnSetStateTransferKeyList()
        {
        }

        public virtual void NotifyBlockActivity(string uniqueId, long interval)
        {

        }

       
        public virtual void WindUpReplicatorTask()
        {

        }

        public virtual void WaitForReplicatorTask(long interval)
        {

        }

        public virtual List<ShutDownServerInfo> GetShutDownServers()
        {
            return null;
        }

        public virtual bool IsShutdownServer(Address server)
        {
            return false;
        }

        public virtual void NotifyUnBlockActivity(string uniqueId)
        {
        }

        public virtual bool IsOperationAllowed(object key, AllowedOperationType opType)
        {
            return true;
        }

        public virtual bool IsOperationAllowed(IList key, AllowedOperationType opType, OperationContext operationContext)
        {
            return true;
        }

        public virtual bool IsOperationAllowed(AllowedOperationType opType, OperationContext operationContext)
        {
            return true;
        }

        /// <summary>
        /// Add an ExpirationHint against a given key
        /// Key must already exist in the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="eh"></param>
        public virtual bool Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
            return Add(key, null, eh, operationContext);
        }

        public virtual bool Add(object key, string group, ExpirationHint eh, OperationContext operationContext)
        {
            return false;
        }

        public virtual bool Add(object key, OperationContext operationContext)
        {
            return false;
        }

        public virtual bool Add(object key, string group, OperationContext operationContext)
        {
            return false;
        }
        /// <summary>
        /// Adds a pair of key and value to the cache. If the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>returns the result of operation.</returns>
        public virtual CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            return new CacheInsResultWithEntry();
        }

        public virtual CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, string taskId, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            return new CacheInsResultWithEntry();
        }

        public virtual CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, bool isUserOperation, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            return new CacheInsResultWithEntry();
        }

        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        public virtual Hashtable Remove(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            return null;
        }

        public virtual Hashtable Cascaded_remove(Hashtable keyValues, ItemRemoveReason ir, bool notify, bool isUserOperation, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>item value</returns>
        public virtual CacheEntry Remove(object key, ItemRemoveReason removalReason, bool notify, string taskId, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            return null;
        }

        public virtual CacheEntry Remove(object key, ItemRemoveReason removalReason, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            return Remove(key, null, removalReason, notify, lockId, version, accessType, operationContext);
        }

        public virtual CacheEntry Remove(object key, ItemRemoveReason removalReason, bool notify, bool isUserOperation, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            return null;
        }

        public virtual CacheEntry Remove(object key, string group, ItemRemoveReason removalReason, bool notify, object lockId, ulong version, LockAccessType accessType, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Remove item from the cluster to synchronize the replicated nodes.
        /// [WARNING]This method should be only called while removing items from
        /// the cluster in order to synchronize them.[]
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="reason"></param>
        /// <param name="notify"></param>
        /// <returns></returns>
        public virtual object RemoveSync(object[] keys, ItemRemoveReason reason, bool notify, OperationContext operationContext)
        {
            return null;
        }


        /// <summary>
        /// Broadcasts a user-defined event across the cluster.
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="data"></param>
        /// <param name="async"></param>
        public virtual void SendNotification(object notifId, object data, OperationContext operationContext)
        {
        }

        
        public virtual List<Event> GetFilteredEvents(string clientID, Hashtable events, EventStatus _registeredEventStatus)
        {
            return null;
        }

        #region   /--           Bulk Operations              --

        public virtual Hashtable Contains(IList keys, string group, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>List of keys that are not found in the cache.</returns>
        public virtual Hashtable Contains(IList keys, OperationContext operationContext)
        {
            return Contains(keys, null, operationContext);
        }


        /// <summary>
        /// Retrieve the objects from the cache. An array of keys is passed as parameter.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>key and value pairs</returns>
        public virtual IDictionary Get(object[] keys, OperationContext operationContext)
        {
            return Get(keys, operationContext);
        }

        public virtual Hashtable GetGroup(object[] keys, string group, string subGroup, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>List of keys that are added or that alredy exists in the cache and their status</returns>
        public virtual Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            return null;
        }

        public virtual Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, string taskId, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Adds key and value pairs to the cache. If any of the specified key already exists 
        /// in the cache; it is updated, otherwise a new item is added to the cache.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="cacheEntries">the cache entries.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>returns successful keys and thier status.</returns>
        public virtual Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
        {
            return null;
        }

        public virtual Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, string taskId, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Removes key and value pairs from the cache. The keys are specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>List of keys and values that are removed from cache</returns>
        public virtual Hashtable Remove(IList keys, ItemRemoveReason removalReason, bool notify, OperationContext operationContext)
        {
            return Remove(keys, null, removalReason, notify, operationContext);
        }

        public virtual Hashtable Remove(IList keys, ItemRemoveReason removalReason, bool notify, string taskId, OperationContext operationContext)
        {
            return null;
        }

        public virtual Hashtable Remove(IList keys, string group, ItemRemoveReason removalReason, bool notify, OperationContext operationContext)
        {
            return null;
        }

        public virtual Hashtable Remove(IList keys, ItemRemoveReason removalReason, bool notify, bool isUserOperation, OperationContext operationContext)
        {
            return null;
        }

        #endregion

        /// <summary>
        /// Returns a .NET IEnumerator interface so that a client should be able
        /// to iterate over the elements of the cache store.
        /// </summary>
        /// <returns>IDictionaryEnumerator enumerator.</returns>
        public virtual IDictionaryEnumerator GetEnumerator()
        {
            return null;
        }

        
        public virtual EnumerationDataChunk GetNextChunk(EnumerationPointer pointer, OperationContext operationContext)
        {
            return null;
        }

        public virtual bool HasEnumerationPointer(EnumerationPointer pointer)
        {
            return false;
        }

        #endregion

        #region	/                 --- Event Notifiers ---           /

        /// <summary> Notifications are enabled. </summary>
        protected bool IsItemAddNotifier { get { return (Notifiers & Notifications.ItemAdd) == Notifications.ItemAdd; } }
        protected bool IsItemUpdateNotifier { get { return (Notifiers & Notifications.ItemUpdate) == Notifications.ItemUpdate; } }
        protected bool IsItemRemoveNotifier { get { return (Notifiers & Notifications.ItemRemove) == Notifications.ItemRemove; } }

        protected bool IsCacheClearNotifier { get { return (Notifiers & Notifications.CacheClear) == Notifications.CacheClear; } }


        
        protected virtual void NotifyOldItemAdded(object key, bool async, OperationContext operationContext, EventContext eventContext)
        {
            if ((Listener != null) && IsItemAddNotifier)
            {
                if (!async)
                    Listener.OnItemAdded(key, operationContext, eventContext);
                else
                    _context.AsyncProc.Enqueue(new AsyncLocalNotifyAdd(Listener, key, operationContext, eventContext));
            }
        }
        protected virtual void NotifyOldItemUpdated(object key, bool async, OperationContext operationContext, EventContext eventContext)
        {
            if ((Listener != null) && IsItemUpdateNotifier)
            {
                if (!async)
                    Listener.OnItemUpdated(key, operationContext, eventContext);
                else
                    _context.AsyncProc.Enqueue(new AsyncLocalNotifyUpdate(Listener, key, operationContext, eventContext));
            }
        }
        /// <summary>
        /// Notify the listener that an item is added to the cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="async">flag indicating that the notification is asynchronous</param>
        protected virtual void NotifyItemAdded(object key, bool async, OperationContext operationContext, EventContext eventContext)
        {
           
        }

        protected bool ShouldNotifyCustomUpdate(Caching.Notifications cbEtnry)
        {
            return Listener != null && cbEtnry!=null;
        }
        protected bool ShouldNotifyCustomRemove(Caching.Notifications cbEtnry)
        {
            return Listener != null && cbEtnry != null;
        }

        protected virtual void NotifyOldCustomUpdateCallback(object key, object value, bool async, OperationContext operationContext, EventContext eventContext)
        {
            if ((Listener != null))
            {
                if (!async)
                    Listener.OnCustomUpdateCallback(key, value, operationContext, eventContext);
                else
                    _context.AsyncProc.Enqueue(new AsyncLocalNotifyUpdateCallback(Listener, key, value, operationContext, eventContext));
            }
        }
        public virtual void NotifyOldCustomRemoveCallback(object key, object value, ItemRemoveReason reason, bool async, OperationContext operationContext, EventContext eventContext)
        {
            if ((Listener != null))
            {
                if (!async)
                    Listener.OnCustomRemoveCallback(key, value, reason, operationContext, eventContext);
                else
                {
                    bool notify = true;

                    if (reason == ItemRemoveReason.Expired)
                    {


                        int notifyOnExpirationCount = 0;
                        if (eventContext != null)
                        {
                            ArrayList removedListener = eventContext.GetValueByField(EventContextFieldName.ItemRemoveCallbackList) as ArrayList;
                            if (removedListener != null)
                            {
                                for (int i = 0; i < removedListener.Count; i++)
                                {
                                    CallbackInfo removeCallbackInfo = (CallbackInfo)removedListener[i];
                                    if (removeCallbackInfo != null && removeCallbackInfo.NotifyOnExpiration)
                                        notifyOnExpirationCount++;
                                }
                            }
                        }
                        if (notifyOnExpirationCount <= 0) notify = false;
                    }

                        _context.AsyncProc.Enqueue(new AsyncLocalNotifyRemoveCallback(Listener, key, value, reason, operationContext, eventContext));
                }
            }
        }
        /// <summary>
        /// Notifies the listener that an item is updated which has an item update callback.
        /// </summary>
        /// <param name="key">key of cache item</param>
        /// <param name="entry">Callback entry which contains the item update call back.</param>
        /// <param name="async">flag indicating that the notification is asynchronous</param>
        public virtual void NotifyCustomUpdateCallback(object key, object value, bool async, OperationContext operationContext, EventContext eventContext)
        {
            if (!async)
            {
                string messageID = AppUtil.GenerateMessageID(key.ToString(),eventContext.UniqueId);
                EventMessage message =null;
                try
                {
                    message = new EventMessage(messageID);
                    message = GenerateEventMessage(key.ToString(), message, TopicConstant.ItemLevelEventsTopic, eventContext, operationContext);
                    message.IsMulticast = true;
                    message.CallbackInfos = new ArrayList();
                    message.CallbackInfos = (ArrayList)value;
                    List<string> clients = message.GetDestinationClientIds();
                    if (clients.Count > 0)
                    {
                        message.AddSpecificReciepients(clients);
                        bool flag = StoreMessage(message.MessageMetaData.TopicName, message, operationContext);
                    }
                }
                finally
                {
                    message.FlagMap?.MarkFree(NCModulesConstants.Global);
                }
            }
            else
                _context.AsyncProc.Enqueue(new AsyncLocalNotifyUpdateCallback(Listener, key, value, operationContext, eventContext));

        }

        /// <summary>
        /// Notifies the listener that an item is removed which has an item removed callback.
        /// </summary>
        /// <param name="key">key of cache item</param>
        /// <param name="value">Callback entry which contains the item remove call back.</param>
        /// <param name="async">flag indicating that the notification is asynchronous</param>
        public virtual void NotifyCustomRemoveCallback(object key, object value, ItemRemoveReason reason, bool async, OperationContext operationContext, EventContext eventContext)
        {
          
            string messageID = AppUtil.GenerateMessageID(key.ToString(), eventContext.UniqueId);
            EventMessage message = null;
            try
            {
                message = new EventMessage(messageID);
                message.RemoveReason = reason;
                message = GenerateEventMessage(key.ToString(), message, TopicConstant.ItemLevelEventsTopic, eventContext, operationContext);
                message.IsMulticast = true;
                message.CallbackInfos = new ArrayList();
                message.CallbackInfos = (ArrayList)value;
                List<string> clients = message.GetDestinationClientIds();
                if (clients.Count > 0)
                {
                    message.AddSpecificReciepients(clients);
                    bool flag = StoreMessage(message.MessageMetaData.TopicName, message, operationContext);
                }
            }
            finally
            {
                message.FlagMap?.MarkFree(NCModulesConstants.Global);

            }

          
        }

        /// <summary>
        /// Notifies the listener that an item is modified so pull latest notifications.
        /// </summary>
        public virtual void NotifyPollRequestCallback(string clientId, short callbackId, bool isAsync, EventTypeInternal eventType)
        {
            if ((Listener != null))
            {
                if (!isAsync)
                    Listener.OnPollNotify(clientId, callbackId, eventType);
                else
                {
                    _context.AsyncProc.Enqueue(new AsyncLocalPollRequestCallback(Listener, clientId, callbackId, eventType));
                }
            }
        }

        protected bool ShouldNotifyItemUpdated
        {
            get { return (Listener != null) && _context.CacheImpl.IsItemUpdateNotifier; }
        }

        /// <summary>
        /// Notify the listener that an item is updated in the cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="async">flag indicating that the notification is asynchronous</param>
        protected virtual void NotifyItemUpdated(object key, bool async, OperationContext operationContext, EventContext eventContext)
        {
    
        }

        protected bool ShouldNotifyItemRemoved
        {
            get { return (Listener != null) && _context.CacheImpl.IsItemRemoveNotifier; }
        }

        /// <summary>
        /// Notify the listener that an item is removed from the cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="val">item itself</param>
        /// <param name="reason">reason the item was removed</param>
        /// <param name="async">flag indicating that the notification is asynchronous</param>
        public virtual void NotifyItemRemoved(object key, object val, ItemRemoveReason reason, bool async, OperationContext operationContext, EventContext eventContext)
        {
            if (ShouldNotifyItemRemoved)
            {
                string messageID = AppUtil.GenerateMessageID(key.ToString(), eventContext.UniqueId);
                EventMessage message = null;
      

            }


        }

        /// <summary>
        /// Notify the listener that items are removed from the cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="val">item itself</param>
        /// <param name="reason">reason the item was removed</param>
        /// <param name="async">flag indicating that the notification is asynchronous</param>
        public virtual void NotifyItemsRemoved(object[] keys, object[] vals, ItemRemoveReason reason, bool async, OperationContext operationContext, EventContext[] eventContext)
        {
            if ((Listener != null) && IsItemRemoveNotifier)
            {
                if (!async)
                    Listener.OnItemsRemoved(keys, vals, reason, operationContext, eventContext);
                else
                    _context.AsyncProc.Enqueue(new AsyncLocalNotifyRemoval(Listener, keys, vals, reason, operationContext, eventContext));
            }
        }
        protected virtual void NotifyOldItemRemoved(object key, object val, ItemRemoveReason reason, bool async, OperationContext operationContext, EventContext eventContext)
        {
            if ((Listener != null) && IsItemRemoveNotifier)
            {
                if (!async)
                    Listener.OnItemRemoved(key, val, reason, operationContext, eventContext);
                else
                    _context.AsyncProc.Enqueue(new AsyncLocalNotifyRemoval(Listener, key, val, reason, operationContext, eventContext));
            }
        }
    

        /// <summary>
        /// Fire when the cache is cleared.
        /// </summary>
        /// <param name="async">flag indicating that the notification is asynchronous</param>
        protected virtual void NotifyCacheCleared(bool async, OperationContext operationContext, EventContext eventContext)
        {
            if ((Listener != null) && IsCacheClearNotifier)
            {
                if (!async)
                    Listener.OnCacheCleared(operationContext, eventContext);
                else
                    _context.AsyncProc.Enqueue(new AsyncLocalNotifyCacheClear(Listener, operationContext, eventContext));
            }
        }

        /// <summary>
        /// Fire when user wishes.
        /// </summary>
        /// <param name="notifId"></param>
        /// <param name="data"></param>
        /// <param name="async"></param>
        public virtual void NotifyCustomEvent(object notifId, object data, bool async, OperationContext operationContext, EventContext eventContext)
        {
            if (!IsCacheOperationAllowed(operationContext))
                return;
            if ((Listener != null))
            {
                if (!async)
                    Listener.OnCustomEvent(notifId, data, operationContext, eventContext);
                else
                    _context.AsyncProc.Enqueue(new AsyncLocalNotifyCustomEvent(Listener, notifId, data, operationContext, eventContext));
            }
        }

        /// <summary>
        /// Notify all connected clients that hashmap has changed
        /// </summary>
        /// <param name="viewId">View id</param>
        /// <param name="newmap">New hashmap</param>
        /// <param name="members">Current members list (contains Address)</param>
        /// <param name="async"></param>
        protected virtual void NotifyHashmapChanged(long viewId, Hashtable newmap, ArrayList members, bool async, bool updateClientMap, bool forcefulUpdate = false)
        {
            if (Listener != null)
            {
#if !CLIENT && !DEVELOPMENT
                _context.AsyncProc.Enqueue(new AsyncLocalNotifyHashmapCallback(Listener, viewId, newmap, members, updateClientMap, forcefulUpdate));
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operationCode"></param>
        /// <param name="result"></param>
        /// <param name="notification"></param>
        protected virtual void NotifyWriteBehindTaskCompleted(OpCode operationCode, Hashtable result, Caching.Notifications notification, OperationContext operationContext)
        {
            if (Listener != null)
            {
                InternalCache.DoWrite("CacheBase.NotifyWriteBehindTaskCompleted", "", operationContext);
                Listener.OnWriteBehindOperationCompletedCallback(operationCode, result, notification);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operationCode"></param>
        /// <param name="result"></param>
        /// <param name="entry"></param>
        /// <param name="taskId"></param>
        public virtual void NotifyWriteBehindTaskStatus(OpCode operationCode, Hashtable result, Caching.Notifications notification, string taskId, string providerName, OperationContext operationContext)
        {
            if (notification != null && notification.WriteBehindOperationCompletedCallback != null)
            {
                NotifyWriteBehindTaskCompleted(operationCode, result, notification, operationContext);
            }
        }


        #endregion

        public virtual void ReplicateConnectionString(string connString, bool isSql)
        { }

        public virtual void ReplicateOperations(IList keys, IList cacheEntries, IList userPayloads, IList compilationInfo, ulong seqId, long viewId)
        { }

        public virtual void ValidateItems(ArrayList keys, ArrayList userPayloads)
        { }

        public virtual void ValidateItems(object key, object userPayloads)
        { }

        /// <summary>
        /// Add the 'key' to the cascading keys list of all the 'keys'.
        /// </summary>
        /// <param name="key">The key to be added. </param>
        /// <param name="keys">The keys whose lists should be updated. </param>
        /// <returns></returns>
        public virtual Hashtable AddDepKeyList(Hashtable table, OperationContext operationContext)
        { return null; }

        /// <summary>
        /// Remove the 'key' from the cascaded list of all the 'keys'.
        /// </summary>
        /// <param name="key">The key to be removed.</param>
        /// <param name="keys">The keys whose list should be updated. </param>
        /// <returns></returns>
        public virtual Hashtable RemoveDepKeyList(Hashtable table, OperationContext operationContext)
        { return null; }

        /// <summary>
        /// Removes all the cascading dependent items, initiating from this entry.
        /// </summary>
        /// <param name="e">last entry in the chain of cascading entries</param>
        /// <param name="context">Runtime context to remove the items from cache.</param>
        public void RemoveCascadingDependencies(object key, CacheEntry e, OperationContext operationContext)
        {
            // 'false' means the call is from remove and hence remove the cache items anyways
            try
            {
                if (e != null)
                    e.MarkInUse(NCModulesConstants.Topology);
                RemoveCascadingDependencies(key, e, operationContext, false);
            }
            finally
            {
                if (e != null)
                    e.MarkFree(NCModulesConstants.Topology);
            }
        }

        public void RemoveCascadingDependencies(object key, HashVector keysDependingOnMe, OperationContext operationContext, bool isFromInsertCall)
        {
            Hashtable entriesTable = new Hashtable();

            if (keysDependingOnMe != null)
            {

                ArrayList partialNextRemovalKeys = new ArrayList();
                string[] nextRemovalKeys = new string[keysDependingOnMe.Count];
                keysDependingOnMe.Keys.CopyTo(nextRemovalKeys, 0);

                while (nextRemovalKeys != null && nextRemovalKeys.Length > 0)
                {
                    try
                    {
                        foreach (string nextRemovalKey in nextRemovalKeys)
                        {
                            if (!partialNextRemovalKeys.Contains(nextRemovalKey))
                            {
                                partialNextRemovalKeys.Add(nextRemovalKey);
                            }
                        }
                        nextRemovalKeys = null;
                        string[] partialNextRemovalKeysArr = new string[partialNextRemovalKeys.Count];
                        partialNextRemovalKeys.CopyTo(partialNextRemovalKeysArr, 0);

                        if (partialNextRemovalKeysArr.Length > 0)
                        {
                            entriesTable = _context.CacheImpl.Remove(partialNextRemovalKeysArr, ItemRemoveReason.DependencyChanged, true, operationContext);
                            nextRemovalKeys = ExtractKeys(entriesTable);
                        }
                    }
                    finally
                    {
                        if (entriesTable != null && entriesTable.Values != null)
                        {
                            IEnumerator enm = entriesTable.Values.GetEnumerator();
                            if (enm != null)
                            {
                                while (enm.MoveNext())
                                {
                                    if (enm.Current != null && enm.Current is CacheEntry)
                                        ((CacheEntry)enm.Current).MarkFree(NCModulesConstants.Global);
                                }
                            }
                        }

                        if (entriesTable?.Count > 0)
                        {
                            var valuesEnumerator = entriesTable.Values.GetEnumerator();

                            while (valuesEnumerator.MoveNext())
                            {
                                switch (valuesEnumerator.Current)
                                {
                                    case CacheEntry returnableCacheEntry:
                                        MiscUtil.ReturnEntryToPool(returnableCacheEntry, Context.TransactionalPoolManager);
                                        break;

                                    case CacheInsResultWithEntry returnableCacheInsResultWithEntry:
                                        MiscUtil.ReturnEntryToPool(returnableCacheInsResultWithEntry.Entry, Context.TransactionalPoolManager);
                                        MiscUtil.ReturnCacheInsResultToPool(returnableCacheInsResultWithEntry, Context.TransactionalPoolManager);
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }

            }
        }
        public void RemoveCascadingDependencies(object key, CacheEntry e, OperationContext operationContext, bool isFromInsertCall)
        {
            if (e != null)
            {
                try
                {
                    if (e != null)
                        e.MarkInUse(NCModulesConstants.Topology);
                    operationContext?.MarkInUse(NCModulesConstants.Topology);

                    RemoveCascadingDependencies(key, e.KeysDependingOnMe, operationContext, isFromInsertCall);
                }
                finally
                {
                    if (e != null)
                        e.MarkFree(NCModulesConstants.Topology);
                    operationContext?.MarkFree(NCModulesConstants.Topology);
                }
            }
        }

        public void RemoveCascadingDependencies(Hashtable removedItems, OperationContext operationContext)
        {
            // 'false' means the call is from remove and hence remove the cache items anyways
            RemoveCascadingDependencies(removedItems, operationContext, false);
        }

        public void RemoveCascadingDependencies(Hashtable removedItems, OperationContext operationContext, bool isFromInsertCall)
        {
            if (removedItems == null)
                return;

            if (removedItems.Count == 0)
                return;

            KeyDependencyInfo[] nextRemovalKeyDepInfos = ExtractKeyDependencyInfos(removedItems);
            ArrayList partialNextRemovalKeyDepInfos = new ArrayList();
            Hashtable entriesTable = null;

            while (nextRemovalKeyDepInfos != null && nextRemovalKeyDepInfos.Length > 0)
            {
                try
                {
                    foreach (KeyDependencyInfo nextRemovalKeyDepInfo in nextRemovalKeyDepInfos)
                    {
                        if (nextRemovalKeyDepInfo != null)
                        {
                            if (!partialNextRemovalKeyDepInfos.Contains(nextRemovalKeyDepInfo.Key))
                            {
                                partialNextRemovalKeyDepInfos.Add(nextRemovalKeyDepInfo.Key);
                            }
                           
                        }
                    }
                    nextRemovalKeyDepInfos = null;
                    string[] partialNextRemovalKeyDepInfosArr = new string[partialNextRemovalKeyDepInfos.Count];
                    partialNextRemovalKeyDepInfos.CopyTo(partialNextRemovalKeyDepInfosArr, 0);

                    if (partialNextRemovalKeyDepInfosArr.Length > 0)
                    {
                        entriesTable = _context.CacheImpl.Remove(partialNextRemovalKeyDepInfosArr, ItemRemoveReason.DependencyChanged, true, operationContext);
                        nextRemovalKeyDepInfos = ExtractKeyDependencyInfos(entriesTable);
                    }
                }
                finally
                {
                    if (entriesTable?.Count > 0)
                    {
                        var valuesEnumerator = entriesTable.Values.GetEnumerator();

                        while (valuesEnumerator.MoveNext())
                        {
                            switch (valuesEnumerator.Current)
                            {
                                case CacheEntry returnableCacheEntry:
                                    MiscUtil.ReturnEntryToPool(returnableCacheEntry, Context.TransactionalPoolManager);
                                    break;

                                case CacheInsResultWithEntry returnableCacheInsResultWithEntry:
                                    MiscUtil.ReturnEntryToPool(returnableCacheInsResultWithEntry.Entry, Context.TransactionalPoolManager);
                                    MiscUtil.ReturnCacheInsResultToPool(returnableCacheInsResultWithEntry, Context.TransactionalPoolManager);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public void RemoveDeleteQueryCascadingDependencies(Hashtable removedItems, OperationContext operationContext)
        {
            // 'false' means the call is from remove and hence remove the cache items anyways
            RemoveDeleteQueryCascadingDependencies(removedItems, operationContext, false);
        }

        public void RemoveDeleteQueryCascadingDependencies(Hashtable removedItems, OperationContext operationContext, bool isFromInsertCall)
        {
            if (removedItems == null || removedItems.Count == 0)
            {
                return;
            }

            KeyDependencyInfo[] nextRemovalKeyDepInfos = ExtractKeyDependencyInfos(removedItems);
            ArrayList partialNextRemovalKeyDepInfos = new ArrayList();
            Hashtable entriesTable = null;

            while (nextRemovalKeyDepInfos != null && nextRemovalKeyDepInfos.Length > 0)
            {
                try
                {
                    foreach (KeyDependencyInfo nextRemovalKeyDepInfo in nextRemovalKeyDepInfos)
                    {
                        if (nextRemovalKeyDepInfo != null)
                        {
                            if (!partialNextRemovalKeyDepInfos.Contains(nextRemovalKeyDepInfo.Key))
                            {
                                partialNextRemovalKeyDepInfos.Add(nextRemovalKeyDepInfo.Key);
                            }
                        }
                    }

                    nextRemovalKeyDepInfos = null;
                    string[] partialNextRemovalKeysArr = new string[partialNextRemovalKeyDepInfos.Count];
                    partialNextRemovalKeyDepInfos.CopyTo(partialNextRemovalKeysArr, 0);

                    if (partialNextRemovalKeysArr.Length > 0)
                    {
                        entriesTable = _context.CacheImpl.Remove(partialNextRemovalKeysArr, ItemRemoveReason.DependencyChanged, true, operationContext);
                        nextRemovalKeyDepInfos = ExtractKeyDependencyInfos(entriesTable);
                    }
                }
                finally
                {
                    if (entriesTable?.Count > 0)
                    {
                        var valuesEnumerator = entriesTable.Values.GetEnumerator();

                        while (valuesEnumerator.MoveNext())
                        {
                            switch (valuesEnumerator.Current)
                            {
                                case CacheEntry returnableCacheEntry:
                                    MiscUtil.ReturnEntryToPool(returnableCacheEntry, Context.TransactionalPoolManager);
                                    break;

                                case CacheInsResultWithEntry returnableCacheInsResultWithEntry:
                                    // Although this case may not arise but just in case it does
                                    MiscUtil.ReturnEntryToPool(returnableCacheInsResultWithEntry.Entry, Context.TransactionalPoolManager);
                                    MiscUtil.ReturnCacheInsResultToPool(returnableCacheInsResultWithEntry, Context.TransactionalPoolManager);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }

        protected string[] ExtractKeys(Hashtable table)
        {
            Hashtable keysTable = new Hashtable();
            keysTable = ExtractDependentKeys(table);

            string[] retVal = new string[keysTable.Count];
            keysTable.Keys.CopyTo(retVal, 0);
            return retVal;
        }

        /// <summary>
        /// Returns the keys from all the keydependency hints.
        /// </summary>
        protected Hashtable ExtractDependentKeys(Hashtable table)
        {
            Hashtable keysTable = new Hashtable();
            IDictionaryEnumerator entries = table.GetEnumerator();
            while (entries.MoveNext())
            {
                if (entries.Value != null)
                {
                    HashVector keys = null;
                    if (entries.Value is CacheEntry)
                    {
                        keys = ((CacheEntry)entries.Value).KeysDependingOnMe;
                    }
                    else if (entries.Value is CacheInsResultWithEntry) //In Bulk Insert case CacheInsResultWithEntry is received
                    {
                        CacheInsResultWithEntry cacheInsResultWithEntry = (CacheInsResultWithEntry)entries.Value;
                        if (cacheInsResultWithEntry.Entry != null)
                        {
                            keys = cacheInsResultWithEntry.Entry.KeysDependingOnMe;
                        }
                    }
                    if (keys != null && keys.Count > 0)
                    {
                        IDictionaryEnumerator keysDic = keys.GetEnumerator();
                        while (keysDic.MoveNext())
                        {
                            if (!keysTable.ContainsKey(keysDic.Key))
                                keysTable.Add(keysDic.Key, null);
                        }
                    }
                }
            }

            return keysTable;
        }

        /// <summary>
        /// Returns the keys from all the keydependency hints with entry mapped against key.
        /// </summary>
        protected Hashtable ExtractDependentKeysWithCacheEntry(Hashtable table,bool returnEntryToPool)
        {
            Hashtable keysTable = new Hashtable();
            IDictionaryEnumerator entries = table.GetEnumerator();
            CacheEntry entry = null;
            while (entries.MoveNext())
            {
                if (entries.Value != null)
                {
                    HashVector keys = null;
                    entry = null;

                    if (entries.Value is CacheEntry)
                    {
                        entry = (CacheEntry)entries.Value;
                        keys = entry.KeysDependingOnMe;
                    }
                    else if (entries.Value is CacheInsResultWithEntry) //In Bulk Insert case CacheInsResultWithEntry is received
                    {
                        var cacheInsResultWithEntry = (CacheInsResultWithEntry)entries.Value;

                        entry = cacheInsResultWithEntry.Entry;
                        if (cacheInsResultWithEntry.Entry != null)
                        {
                            keys = cacheInsResultWithEntry.Entry.KeysDependingOnMe;
                        }
                    }

                    if (entry != null) MiscUtil.ReturnEntryToPool(entry, _context.TransactionalPoolManager);
                    if (keys != null && keys.Count > 0)
                    {
                        IDictionaryEnumerator keysDic = keys.GetEnumerator();

                        while (keysDic.MoveNext())
                        {
                            if (!keysTable.ContainsKey(keysDic.Key))
                            {
                                keysTable.Add(keysDic.Key, entries.Value);
                            }
                        }
                    }
                }
            }

            return keysTable;
        }

        /// <summary>
        /// Gets the <see cref="KeyDependencyInfo"/> instances for the associated cache entries 
        /// passed in the <paramref name="table"/> argument.
        /// </summary>
        /// <param name="table">Hashtable containing cache entries mapped to cache keys.</param>
        /// <returns>Array of <see cref="KeyDependencyInfo"/>.</returns>
        protected KeyDependencyInfo[] ExtractKeyDependencyInfos(Hashtable table)
        {
            Hashtable keyDepInfosTable = ExtractKeyDependencyInfoTable(table);
            KeyDependencyInfo[] keyDependencyInfos = new KeyDependencyInfo[keyDepInfosTable.Count];

            if (keyDepInfosTable.Count > 0)
                keyDepInfosTable.Values.CopyTo(keyDependencyInfos, 0);

            return keyDependencyInfos;
        }

        /// <summary>
        /// Gets the <see cref="KeyDependencyInfo"/> instances for the associated cache entries 
        /// passed in the <paramref name="table"/> argument.
        /// </summary>
        /// <param name="table">Hashtable containing cache entries mapped to cache keys.</param>
        /// <returns>Hashtable of <see cref="string"> Key and <see cref="KeyDependencyInfo"/> Value.</returns>
        protected Hashtable ExtractKeyDependencyInfoTable(Hashtable table)
        {
            Hashtable keyDepInfosTable = new Hashtable();
            IDictionaryEnumerator entries = table.GetEnumerator();

            while (entries.MoveNext())
            {
                if (entries.Value != null)
                {
                    HashVector keyDepInfos = null;

                    if (entries.Value is CacheEntry)
                    {
                        keyDepInfos = ((CacheEntry)entries.Value).KeysDependingOnMe;
                    }
                    else if (entries.Value is CacheInsResultWithEntry)  // In case of Bulk Insert, CacheInsResultWithEntry is received
                    {
                        CacheInsResultWithEntry cacheInsResultWithEntry = (CacheInsResultWithEntry)entries.Value;

                        if (cacheInsResultWithEntry.Entry != null)
                        {
                            keyDepInfos = cacheInsResultWithEntry.Entry.KeysDependingOnMe;
                        }
                    }
                }
            }
            return keyDepInfosTable;
        }

        /// <summary>
        /// Removes any occurances of old keys in the new Keys table.
        /// </summary>
        /// <param name="pKeys">Contains old Depending Keys.</param>
        /// <param name="nKeys">Contains new Depending Keys.</param>
        /// <returns></returns>
        public Hashtable GetFinalKeysList(object[] pKeys, object[] nKeys)
        {
            Hashtable table = new Hashtable();

            if (pKeys == null || nKeys == null)
            {
                table.Add("oldKeys", new object[0]);
                table.Add("newKeys", new object[0]);
            }
            else if (pKeys != null && nKeys != null)
            {
                ArrayList oldKeys = new ArrayList(pKeys);
                ArrayList newKeys = new ArrayList(nKeys);

                for (int i = 0; i < pKeys.Length; i++)
                {
                    for (int j = 0; j < nKeys.Length; j++)
                    {
                        if (pKeys[i] == nKeys[j])
                        {

                            oldKeys.Remove(pKeys[i]);
                            newKeys.Remove(nKeys[j]);
                            break;
                        }
                    }
                }

                table.Add("oldKeys", oldKeys.ToArray());
                table.Add("newKeys", newKeys.ToArray());
            }

            return table;
        }

        public Hashtable GetFinalKeysListWithDependencyInfo(CacheEntry prevEntry, CacheEntry newEntry)
        {
            try
            {
                Hashtable table = new Hashtable();
                if (prevEntry == null || newEntry == null)
                {
                  
                    if (newEntry != null)
                        newEntry.MarkInUse(NCModulesConstants.Topology);
                    if (prevEntry != null)
                        prevEntry.MarkInUse(NCModulesConstants.Topology);
                }
               
                return table;
            }
            finally
            {
                if (newEntry != null)
                    newEntry.MarkFree(NCModulesConstants.Topology);
                if (prevEntry != null)
                    prevEntry.MarkFree(NCModulesConstants.Topology);

            }
        }

        /// <summary>
        /// Create a key table that will then be used to remove old dependencies or create
        /// new dependencies
        /// </summary>
        /// <param name="key">key associated with entry</param>
        /// <param name="keys">Array of keys</param>
        /// <returns>Table containing keys in order</returns>
        protected Hashtable GetKeysTable(object key, object[] keys)
        {
            if (keys == null) return null;

            Hashtable keyTable = new Hashtable(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                if (!keyTable.Contains(keys[i]))
                {
                    keyTable.Add(keys[i], new ArrayList());
                }
                ((ArrayList)keyTable[keys[i]]).Add(key);
            }
            return keyTable;
        }

        protected Hashtable GetKeysTable(object key, KeyDependencyInfo[] keyDepInfos)
        {
            if (keyDepInfos == null) return null;

            Hashtable keyTable = new Hashtable(keyDepInfos.Length);
            for (int i = 0; i < keyDepInfos.Length; i++)
            {
                if (!keyTable.Contains(keyDepInfos[i].Key))
                {
                    keyTable.Add(keyDepInfos[i].Key, new ArrayList());
                }
                ((ArrayList)keyTable[keyDepInfos[i].Key]).Add(key);
            }
            return keyTable;
        }

        protected Hashtable GetKeyDependencyInfoTable(object key, KeyDependencyInfo[] keyDepInfos)
        {
            if (keyDepInfos == null)
            {
                return null;
            }
            Hashtable keyDepInfoTable = new Hashtable(keyDepInfos.Length);
            foreach (KeyDependencyInfo keyDepInfo in keyDepInfos)
            {
                if (!keyDepInfoTable.Contains(keyDepInfo.Key))
                {
                    keyDepInfoTable.Add(keyDepInfo.Key, new ArrayList());
                }
                ((ArrayList)keyDepInfoTable[keyDepInfo.Key]).Add(new KeyDependencyInfo(key.ToString()));
            }
            return keyDepInfoTable;
        }

        protected Hashtable GetKeyDependencyInfoTable(object key, CacheEntry entry)
        {
            if (entry == null)
            {
                return null;
            }
            try
            {
                if (entry != null)
                    entry.MarkInUse(NCModulesConstants.Topology);
                return GetKeyDependencyInfoTable(key, entry.KeysIAmDependingOnWithDependencyInfo);
            }
            finally
            {
                if (entry != null)
                    entry.MarkFree(NCModulesConstants.Topology);
            }
        }


        /// <summary>
        /// Returns the thread safe synchronized wrapper over cache.
        /// </summary>
        /// <param name="cacheStore"></param>
        /// <returns></returns>
        public static CacheBase Synchronized(CacheBase cache)
        {
            return new CacheSyncWrapper(cache);
        }

        public virtual void BalanceDataLoad()
        {
        }

        #region/                        --- Stream ---                              /

        public virtual bool OpenStream(string key, string lockHandle, StreamModes mode, string group, string subGroup, ExpirationHint hint, EvictionHint evictinHint, OperationContext operationContext)
        {
            return false;
        }

        public virtual void CloseStream(string key, string lockHandle, OperationContext operationContext)
        {

        }

        public virtual int ReadFromStream(ref VirtualArray vBuffer, string key, string lockHandle, int offset, int length, OperationContext operationContext)
        {
            return 0;
        }

        public virtual void WriteToStream(string key, string lockHandle, VirtualArray vBuffer, int srcOffset, int dstOffset, int length, OperationContext operationContext)
        {
        }

        public virtual long GetStreamLength(string key, string lockHandle, OperationContext operationContext)
        {
            return 0;
        }

        #endregion


        public virtual void DeclareDeadClients(string deadClient, ClientInfo info)
        {
            InternalCache.DeclareDeadClients(deadClient, info);
        }

        public virtual void UnRegisterCQ(string serverUniqueId, string clientUniqueId, string clientId)
        {
        }

        public virtual void UnRegisterCQ(string queryId)
        {
        }



        
       

      
        public virtual RequestStatus GetClientRequestStatus(string clientId, long requestId, long commandId, Address intendedServer)
        {
            return null;
        }
      

        public virtual bool IsClientConnected(string client)
        {
            if (string.IsNullOrEmpty(client))
                return false;
            try
            {
                return InternalCache.Statistics.ConnectedClients.Contains(client);
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("CacheBase.IsClientConnected()", e.ToString());
            }
            finally
            {
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("CacheBase.IsClientConnected()", "Determining client connectivity completed");
            }
            return false;
        }
        public virtual ArrayList DetermineClientConnectivity(ArrayList clients)
        {
            if (clients == null) return null;
            try
            {
                ArrayList result = new ArrayList();
                CacheStatistics stats = InternalCache.Statistics as CacheStatistics;
                foreach (string client in clients)
                {
                    if (!stats.ConnectedClients.Contains(client))
                    {
                        result.Add(client);
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                Context.NCacheLog.Error("Client-Death-Detection.DetermineClientConnectivity()", e.ToString());
            }
            finally
            {
                if (Context.NCacheLog.IsInfoEnabled) Context.NCacheLog.Info("Client-Death-Detection.DetermineClientConnectivity()", "determining client connectivity completed");
            }
            return null;
        }

        public virtual void DeclaredDeadClients(ArrayList clients)
        {
            InternalCache.DeclaredDeadClients(clients);
        }

        public Hashtable Remove(object[] keys, ItemRemoveReason ir, bool notify, OperationContext operationContext)
        {
            return Remove((IList)keys, ir, notify, operationContext);
        }

        public virtual PollingResult Poll(OperationContext operationContext)
        {
            return new PollingResult();
        }


        public virtual void LogBackingSource()
        {
            InternalCache.LogBackingSource();
        }

        public virtual double GetReplicaCounters(string counterName)
        {
            return 0;
        }

        public virtual void RegisterPollingNotification(short callbackId, OperationContext operationContext)
        { }

        internal virtual void HandleDeadClientsNotification(string deadClient, ClientInfo info)
        {

        }

        internal virtual void NotifyClusterOfClientActivity(string cacheId, ClientInfo client, ConnectivityStatus status)
        {

        }

       
        internal virtual void UnregisterClientActivityCallback(string clientId) { }

        public virtual IEnumerable<ClientInfo> GetConnectedClientsInfo()
        {
            return null;
        }

      

        internal virtual void Touch(List<string> keys, OperationContext operationContext)
        {

        }
        
        internal virtual void SetClusterInactive(string reason)
        {

        }
        

        public virtual bool IsClusterInStateTransfer()
        {
            return false;
        }
        internal virtual void ExitMaintenance(bool notify)
        { }

        internal virtual bool IsClusterUnderMaintenance()
        {
            return false;
        }

       
        internal virtual bool IsClusterAvailableForMaintenance()
        {
            return true;
        }

        #region /                     --- IMessageStore Implementation -----                      /

        public virtual bool StoreMessage(string topic, Messaging.Message message, OperationContext context)
        {
            return ((IMessageStore)InternalCache).StoreMessage(topic, message, context);
        }

        public virtual MessageInfo GetNextUnassignedMessage(TimeSpan timeout, OperationContext context)
        {
            return ((IMessageStore)InternalCache).GetNextUnassignedMessage(timeout, context);
        }

        public virtual SubscriptionInfo GetSubscriber(string topic, Common.Enum.SubscriptionType type, OperationContext context)
        {
            return ((IMessageStore)InternalCache).GetSubscriber(topic, type, context);
        }

        public virtual IList<SubscriptionInfo> GetAllSubscriber(string topic, OperationContext context)
        {
            return ((IMessageStore)InternalCache).GetAllSubscriber(topic, context);
        }

        public virtual bool AssignmentOperation(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo, TopicOperationType type, OperationContext context)
        {

            throw new NotImplementedException();
        }

        public virtual MessageResponse GetAssignedMessage(SubscriptionInfo subscriptionInfo, OperationContext operationContext)
        {
            return ((IMessageStore)InternalCache).GetAssignedMessage(subscriptionInfo, operationContext);
        }

        public virtual void AcknowledgeMessageReceipt(string clientId, IDictionary<string, IList<string>> topicWiseMessageIds, OperationContext operationContext)
        {
            ((IMessageStore)InternalCache).AcknowledgeMessageReceipt(clientId, topicWiseMessageIds, operationContext);
        }

        public virtual void AcknowledgeMessageReceipt(string clientId, string topic, string messageId, OperationContext operationContext)
        {

        }

        public virtual IList<MessageInfo> GetUnacknowledgeMessages(TimeSpan assginmentTimeout)
        {
            return ((IMessageStore)InternalCache).GetUnacknowledgeMessages(assginmentTimeout);
        }


        public virtual void RevokeAssignment(MessageInfo message, SubscriptionInfo subscription, OperationContext context)
        {
            ((IMessageStore)InternalCache).RevokeAssignment(message, subscription, context);
        }
        public virtual IList<MessageInfo> GetDeliveredMessages()
        {
            return ((IMessageStore)InternalCache).GetDeliveredMessages();
        }

        public virtual IList<MessageInfo> GetExpiredMessages()
        {
            return ((IMessageStore)InternalCache).GetExpiredMessages();
        }

        public virtual IList<MessageInfo> GetEvicatableMessages(long sizeToEvict)
        {
            return ((IMessageStore)InternalCache).GetEvicatableMessages(sizeToEvict);
        }

        public virtual IList<string> GetNotifiableClients()
        {
            return ((IMessageStore)InternalCache).GetNotifiableClients();
        }

        public virtual void RemoveMessages(IList<MessageInfo> messagesTobeRemoved, MessageRemovedReason reason, OperationContext context)
        {

        }

        public virtual void RemoveMessages(MessageInfo messagesTobeRemoved, MessageRemovedReason reason, OperationContext context)
        {

        }


        
        public virtual void RemoveExpiredSubscriptions(IDictionary<string, IList<SubscriptionIdentifier>> removeSubscriptions, OperationContext operationContext)
        {
            ((IMessageStore)InternalCache).RemoveExpiredSubscriptions(removeSubscriptions, operationContext);
        }
       
        
        public virtual void RemoveDurableSubscriptions(IList<SubscriptionIdentifier> toRemove,OperationContext context)
        {

        }
        

        public virtual bool TopicOperation(TopicOperation operation, OperationContext operationContext)
        {
            return false;
        }

        public virtual void RegiserTopicEventListener(ITopicEventListener listener)
        {
            ((IMessageStore)InternalCache).RegiserTopicEventListener(listener);
        }

        public virtual TopicState GetTopicsState()
        {
            return ((IMessageStore)InternalCache).GetTopicsState();
        }

        public virtual void SetTopicsState(TopicState topicState)
        {
            ((IMessageStore)InternalCache).SetTopicsState(topicState);
        }

        public virtual TransferrableMessage GetTransferrableMessage(string topic, string messageId)
        {
            return ((IMessageStore)InternalCache).GetTransferrableMessage(topic, messageId);
        }

        public virtual bool StoreTransferrableMessage(string topic, TransferrableMessage message)
        {
            return ((IMessageStore)InternalCache).StoreTransferrableMessage(topic, message);
        }

        public virtual Dictionary<string, TopicStats> GetTopicsStats(bool defaultTopicStats = false)
        {
            return ((IMessageStore)InternalCache).GetTopicsStats(defaultTopicStats);
        }

        public virtual IDictionary<string, IList<string>> GetInActiveClientSubscriptions(TimeSpan inactivityThreshold)
        {
            return ((IMessageStore)InternalCache).GetInActiveClientSubscriptions(inactivityThreshold);
        }

        public virtual IDictionary<string, IList<string>> GetActiveClientSubscriptions(TimeSpan inactivityThreshold)
        {
            return ((IMessageStore)InternalCache).GetActiveClientSubscriptions(inactivityThreshold);
        }



        #endregion


        public virtual long GetMessageCount(string topicName, OperationContext operationContext)
        {
            return 0;
        }

        #region Event Message Generation

        /// <summary>
        /// Populates the base event message.
        /// </summary>
        /// <param name="key">The key of entry regarding who the event is raised.</param>
        /// <param name="message">The messazge to be populated.</param>
        /// <param name="topicName">The name of topic against who the event message is to be published.</param>
        /// <param name="eventContext">The context created for collection event.</param>
        /// <param name="operationContext">The operation context created for the operation triggering the event.</param>
        /// <returns>
        /// Populated <paramref name="message"/>. Note since this argument is not passed by ref, therefore, you have to store it into the same literal again.
        /// </returns>
        private EventMessageBase GenerateEventMessageBase(string key, EventMessageBase message, string topicName, EventContext eventContext, OperationContext operationContext)
        {
            message.Key = key;
            message.FlagMap =BitSet.CreateAndMarkInUse(Context.FakeObjectPool,NCModulesConstants.Global) ;
            message.FlagMap.Data |= 0;
            message.CreationTime = DateTime.Now;
            message.MessageMetaData = new MessageMetaData(message.MessageId);
            message.MessageMetaData.SubscriptionType = SubscriptionType.Subscriber;

            bool notifyDeliveryFailure = false;
            DeliveryOption deliverOption = new DeliveryOption();
            deliverOption = DeliveryOption.All;

            TimeSpan expirationTime;
            if (_context.CacheImpl._eventExpiryTime > 0)
                expirationTime = new TimeSpan(0, 0, _context.CacheImpl._eventExpiryTime);
            else
                expirationTime = new TimeSpan(0, 0, _eventExpiryTime);

            Hashtable metaData = new Hashtable();
            metaData.Add(TopicConstant.NotifyOption, notifyDeliveryFailure.ToString());
            message.MessageMetaData.TopicName = topicName;
            message.MessageMetaData.IsNotify = notifyDeliveryFailure;
            message.MessageMetaData.DeliveryOption = deliverOption;
            message.MessageMetaData.ExpirationTime = expirationTime.Ticks;
            message.MessageMetaData.TimeToLive = AppUtil.DiffSeconds(DateTime.Now) + new TimeSpan(expirationTime.Ticks).TotalSeconds;
            message.EventID = eventContext.EventID;

            return message;
        }

        /// <summary>
        // Sets information regarding event in message.
        /// </summary>
        /// <param name="message">Event message to be stored.</param>
        /// <returns></returns>
        private EventMessage GenerateEventMessage(string key, EventMessage message, string topicName, EventContext eventContext, OperationContext operationContext)
        {
            message = (EventMessage)GenerateEventMessageBase(key, message, topicName, eventContext, operationContext);
            message.Item = eventContext.Item;
            message.OldItem = eventContext.OldItem;
            return message;
        }

     

        #endregion


        #region RaiseNotifiers 

        public virtual void RaiseItemAddNotifier(object key, CacheEntry entry, OperationContext context,
          EventContext eventContext)
        {
            try
            {
                if (entry != null)
                    entry.MarkInUse(NCModulesConstants.Topology);

                InternalCache.RaiseItemAddNotifier(key, entry, context, eventContext);
            }
            finally
            {
                if (entry != null)
                    entry.MarkFree(NCModulesConstants.Topology);
            }
        }

        public virtual void RaiseItemUpdateNotifier(object key, OperationContext operationContext, EventContext eventcontext)
        {
            InternalCache.RaiseItemUpdateNotifier(key, operationContext, eventcontext);
        }

        public virtual void RaiseOldItemRemoveNotifier(object key, OperationContext operationContext, EventContext eventcontext)
        {
            InternalCache.RaiseOldItemRemoveNotifier(key, operationContext, eventcontext);
        }

        public virtual void RaiseItemRemoveNotifier(object data)
        {
            InternalCache.RaiseItemRemoveNotifier(data);
        }
        public virtual void RaiseOldItemRemoveNotifier(object data)
        {
            InternalCache.RaiseOldItemRemoveNotifier(data);
        }
        public virtual void RaiseCustomUpdateCalbackNotifier(object key, ArrayList itemUpdateCallbackListener,
            EventContext eventContext)
        {
            InternalCache.RaiseCustomUpdateCalbackNotifier(key, itemUpdateCallbackListener, eventContext);
        }
        public virtual void RaiseOldCustomUpdateCalbackNotifier(object key, ArrayList itemUpdateCallbackListener,
            EventContext eventContext)
        {
            InternalCache.RaiseOldCustomUpdateCalbackNotifier(key, itemUpdateCallbackListener, eventContext);

        }
        public virtual void RaiseOldCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason,
         OperationContext operationContext, EventContext eventContext)
        {
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);
                InternalCache.RaiseOldCustomRemoveCalbackNotifier(key, cacheEntry, reason, operationContext, eventContext);
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);
            }
        }
        public virtual void RaiseCustomRemoveCalbackNotifier(object key, CacheEntry cacheEntry, ItemRemoveReason reason,
           OperationContext operationContext, EventContext eventContext)
        {
            try
            {
                if (cacheEntry != null)
                    cacheEntry.MarkInUse(NCModulesConstants.Topology);
                InternalCache.RaiseCustomRemoveCalbackNotifier(key, cacheEntry, reason, operationContext, eventContext);
            }
            finally
            {
                if (cacheEntry != null)
                    cacheEntry.MarkFree(NCModulesConstants.Topology);
            }
        }

  
        #endregion


        #region Module

       

        internal virtual void ReplicateModuleOperation(byte[] operationBytes)
        {

        }

        internal virtual List<byte[]> ExecuteProxyCommand(Address destination, string moduleName, string version, string clientID, List<byte[]> payload)
        {
            return null;
        }
        #endregion

        protected bool IsCacheOperationAllowed(OperationContext operationContext)
        {
            bool result = true;
            lock (_cacheConnectedClients)
            {
                if (_cacheConnectedClients.Count > 2)
                {
                    IPAddress clientip = null;
                    if (operationContext != null)
                    {
                        if (operationContext.Contains(OperationContextFieldName.ClientIpAddress))
                            clientip = operationContext.GetValueByField(OperationContextFieldName.ClientIpAddress) as IPAddress;
                    }

                    if (clientip != null)
                    {
                        int count = 0;
                        foreach (var item in _cacheConnectedClients.Keys)
                        {
                            count++;
                            var ipAddress = item as IPAddress;
                            if (ipAddress != null && ipAddress == clientip)
                                break;
                            if (count == 2)
                            {
                                result = false;
                                break;
                            }
                        }
                    }
                } 
            }
            return result;
        }
    }
}
