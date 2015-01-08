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
using System.Threading;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.Statistics;

using Alachisoft.NCache.Common.DataStructures;


using Alachisoft.NCache.Runtime.Exceptions;


using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Caching.EvictionPolicies;

using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Common.Logger;
using System.Net;
#if !CLIENT

#endif


namespace Alachisoft.NCache.Caching.Topologies
{
    /// <summary>
    /// Base class for all cache implementations. Implements the ICache interface. 
    /// </summary>

	internal class CacheBase : ICache, IDisposable
    {
        #region	/                 --- Inner/Nested Classes ---           /

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
        /// Asynchronous hashmap notification dispatcher
        /// </summary>
        private class AsyncLocalNotifyHashmapCallback : AsyncProcessor.IAsyncTask
        {
            private ICacheEventsListener _listener;
            private NewHashmap _hashMap;
            private bool _updateClientMap;


            public AsyncLocalNotifyHashmapCallback(ICacheEventsListener listener, long lastViewid, Hashtable newmap, ArrayList members, bool updateClientMap)
            {
                this._listener = listener;
                this._hashMap = new NewHashmap(lastViewid, newmap, members);
                this._updateClientMap = updateClientMap;

            }

            #region IAsyncTask Members

            void AsyncProcessor.IAsyncTask.Process()
            {
                _listener.OnHashmapChanged(this._hashMap, this._updateClientMap);
            }
            #endregion
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

        /// <summary> Reader, writer lock to be used for synchronization. </summary>
        [CLSCompliant(false)]
        protected ReaderWriterLock _syncObj;

        /// <summary> The runtime context associated with the current cache. </summary>
        [CLSCompliant(false)]
        protected CacheRuntimeContext _context;

        /// <summary> Flag that controls notifications. </summary>
        private Notifications _notifiers = Notifications.None;

        private bool _isInProc = false;

        private bool _keepDeflattedObjects = false;

       

        public System.IO.StreamWriter writer;

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
        /// Gets the CacheRunTime context.
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

    
                


        public virtual int ServersCount
        {
            get { return 0; }
        }

        public virtual bool IsServerNodeIp(Address clientAddress)
        {
            return false;
        }


   

        /// <summary>
        /// returns the statistics of the Clustered Cache. 
        /// </summary>
        public virtual CacheStatistics Statistics
        {
            get { return null; }
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
            set { ; }
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
       
        public virtual bool Add(object key, ExpirationHint eh, OperationContext operationContext)
        {
            return false;
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

                Name = Convert.ToString(cacheClasses["name"]);


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

        public virtual void ClientConnected(string client, bool isInproc) { }
        public virtual void ClientDisconnected(string client, bool isInproc) { }

        #region/                --- Custom Callback Registration ---            /

        public virtual void RegisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext) { }
        public virtual void RegisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext) { }

        public virtual void UnregisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext) { }
        public virtual void UnregisterKeyNotification(string[] keys, CallbackInfo updateCallback, CallbackInfo removeCallback, OperationContext operationContext) { }

        #endregion

        #region /                 --- virtual methods to be overriden by hashed cache ---           /

        public virtual void RemoveBucketData(int bucketId)
        {
        }

        public virtual void RemoveBucketData(ArrayList bucketIds)
        {
            if (bucketIds != null && bucketIds.Count > 0)
            {
                for (int i = 0; i < bucketIds.Count; i++)
                {
                    RemoveBucketData((int)bucketIds[i]);
                }
            }
        }

        public virtual ArrayList GetKeyList(int bucketId, bool startLogging)
        {
            return null;
        }

        public virtual Hashtable GetLogTable(ArrayList bucketIds, ref bool isLoggingStopped)
        {
            return null;
        }

        public virtual void RemoveFromLogTbl(int bucketId)
        {
        }

        public virtual void StartLogging(int bucketId)
        {
        }

        public virtual void AddLoggedData(ArrayList bucketIds)
        {
        }

        public virtual void UpdateLocalBuckets(ArrayList bucketIds)
        {
        }

        public virtual Hashtable LocalBuckets
        {
            get { return null; }
            set { ;}
        }

        public virtual int BucketSize
        {
            set { ;}
        }

        public virtual long CurrentViewId
        {
            get { return 0; }
        }

        public virtual ulong OperationSequenceId
        {
            get { return 0; }
        }



        public virtual NewHashmap GetOwnerHashMapTable(out int bucketSize)
        {
            bucketSize = 0;
            return null;
        }

        #endregion


        public virtual void UpdateClientsList(Hashtable list)
        {
        }

        


        #region	/                 --- ICache ---           /
        

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public virtual void Clear(CallbackEntry cbEntry, OperationContext operationContext)
        {
        }

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the cache.</param>
        /// <returns>true if the cache contains an element 
        /// with the specified key; otherwise, false.</returns>
        public virtual bool Contains(object key, OperationContext operationContext)
        {
            return false;
        }

        public virtual CacheEntry Get(object key, OperationContext operationContext)
        {
            Object lockId = null;
            DateTime lockDate = DateTime.Now;
            return Get(key, ref lockId, ref lockDate, null, LockAccessType.IGNORE_LOCK, operationContext);
        }

        public virtual CacheEntry Get(object key, bool isUserOperation, OperationContext operationContext)
        {
            Object lockId = null;
            DateTime lockDate = DateTime.Now;

            return Get(key, isUserOperation, ref lockId, ref lockDate, null, LockAccessType.IGNORE_LOCK, operationContext);
        }

        public virtual CacheEntry Get(object key, bool isUserOperation, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="lockId"></param>
        /// <param name="lockDate"></param>
        /// <param name="lockExpiration"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
        /// <returns>cache entry.</returns>
        public virtual CacheEntry Get(object key, ref object lockId, ref DateTime lockDate, LockExpiration lockExpiration, LockAccessType accessType, OperationContext operationContext)
        {
            lockId = null;
            lockDate = DateTime.Now;
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
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <param name="operationContext"></param>
        /// <returns>returns the result of operation.</returns>
        public virtual CacheAddResult Add(object key, CacheEntry cacheEntry, bool notify, OperationContext operationContext)
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


       

       



       

        public virtual CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            return new CacheInsResultWithEntry();
        }

        public virtual CacheInsResultWithEntry Insert(object key, CacheEntry cacheEntry, bool notify, bool isUserOperation, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            return new CacheInsResultWithEntry();
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
        /// <param name="lockId"></param>
        /// <param name="accessType"></param>
        /// <param name="operationContext"></param>
        /// <returns>item value</returns>
        public virtual CacheEntry Remove(object key, ItemRemoveReason removalReason, bool notify, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            return null;
        }

        public virtual CacheEntry Remove(object key, ItemRemoveReason removalReason, bool notify, bool isUserOperation, object lockId, LockAccessType accessType, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Remove item from the cluster to synchronize the replicated nodes.
        /// [WARNING]This method should be only called while removing items from
        /// the cluster in order to synchronize them.[Taimoor]
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
        public virtual void SendNotification(object notifId, object data)
        {
        }

        public virtual QueryResultSet Search(string query, IDictionary values, OperationContext operationContext)
        {
            return null;
        }

        public virtual QueryResultSet SearchEntries(string query, IDictionary values, OperationContext operationContext)
        {
            return null;
        }

   

        #region   /--           Bulk Operations              --

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>List of keys that are not found in the cache.</returns>
        public virtual Hashtable Contains(object[] keys, OperationContext operationContext)
        {
            return null;
        }


        /// <summary>
        /// Retrieve the objects from the cache. An array of keys is passed as parameter.
        /// </summary>
        /// <param name="keys">keys of the entries.</param>
        /// <returns>key and value pairs</returns>
        public virtual Hashtable Get(object[] keys, OperationContext operationContext)
        {
            return Get(keys, operationContext);
        }

        /// <summary>
        /// Adds a pair of key and value to the cache. Throws an exception or reports error 
        /// if the specified key already exists in the cache.
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="cacheEntries"></param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <param name="operationContext"></param>
        /// <param name="key">key of the entry.</param>
        /// <param name="cacheEntry">the cache entry.</param>
        /// <returns>List of keys that are added or that alredy exists in the cache and their status</returns>
        public virtual Hashtable Add(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
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
        /// <param name="operationContext"></param>
        /// <returns>returns successful keys and thier status.</returns>
        public virtual Hashtable Insert(object[] keys, CacheEntry[] cacheEntries, bool notify, OperationContext operationContext)
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
        /// <param name="operationContext"></param>
        /// <returns>List of keys and values that are removed from cache</returns>
        public virtual Hashtable Remove(object[] keys, ItemRemoveReason removalReason, bool notify, OperationContext operationContext)
        {
            return null;
        }

        public virtual Hashtable Remove(object[] keys, ItemRemoveReason removalReason, bool notify, bool isUserOperation, OperationContext operationContext)
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

        /// <summary>
        /// Notifies the listener that an item is updated which has an item update callback.
        /// </summary>
        /// <param name="key">key of cache item</param>
        /// <param name="entry">Callback entry which contains the item update call back.</param>
        /// <param name="async">flag indicating that the nofitication is asynchronous</param>
        protected virtual void NotifyCustomUpdateCallback(object key, object value, bool async, OperationContext operationContext, EventContext eventContext)
        {
            if ((Listener != null))
            {
                if (!async)
                    Listener.OnCustomUpdateCallback(key, value, operationContext, eventContext);
                else
                    _context.AsyncProc.Enqueue(new AsyncLocalNotifyUpdateCallback(Listener, key, value, operationContext, eventContext));
            }
        }

        /// <summary>
        /// Notifies the listener that an item is removed which has an item removed callback.
        /// </summary>
        /// <param name="key">key of cache item</param>
        /// <param name="value">Callback entry which contains the item remove call back.</param>
        /// <param name="async">flag indicating that the nofitication is asynchronous</param>
        public virtual void NotifyCustomRemoveCallback(object key, object value, ItemRemoveReason reason, bool async, OperationContext operationContext, EventContext eventContext)
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
                        if (eventContext!= null )
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

                    if (notify)
                        _context.AsyncProc.Enqueue(new AsyncLocalNotifyRemoveCallback(Listener, key, value, reason, operationContext, eventContext));
                }
            }
        }
 


        /// <summary>
        /// Notify all connected clients that hashmap has changed
        /// </summary>
        /// <param name="viewId">View id</param>
        /// <param name="newmap">New hashmap</param>
        /// <param name="members">Current members list (contains Address)</param>
        /// <param name="async"></param>
        protected virtual void NotifyHashmapChanged(long viewId, Hashtable newmap, ArrayList members, bool async, bool updateClientMap)
        {
            if (Listener != null)
            {
                _context.AsyncProc.Enqueue(new AsyncLocalNotifyHashmapCallback(Listener, viewId, newmap, members, updateClientMap));

            }
        }


        #endregion

        public virtual void ReplicateOperations(Array keys, Array cacheEntries, Array userPayloads, ArrayList compilationInfo, ulong seqId, long viewId)
        { }

        public virtual void ValidateItems(ArrayList keys, ArrayList userPayloads)
        { }

        public virtual void ValidateItems(object key, object userPayloads)
        { }


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
    }
}
