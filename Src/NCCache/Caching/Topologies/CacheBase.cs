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
using System.Threading;
using System.Collections.Generic;
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.DataStructures;

using Alachisoft.NCache.Runtime.Exceptions;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Common.Logger;
using System.Net;

using Alachisoft.NCache.Caching.Queries.Continuous;
using Alachisoft.NCache.Persistence;
using Alachisoft.NCache.Caching.DatasourceProviders;
using RequestStatus = Alachisoft.NCache.Common.DataStructures.RequestStatus;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Events;

using Alachisoft.NCache.MapReduce;
using Alachisoft.NCache.MapReduce.Notifications;
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Caching.DataReader;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Caching.Messaging;
using EventType = Alachisoft.NCache.Runtime.Events.EventType;

namespace Alachisoft.NCache.Caching.Topologies
{
    /// <summary>
    /// Base class for all cache implementations. Implements the ICache interface. 
    /// </summary>

	internal class CacheBase : ICache,IMessageStore,  IDisposable
    {
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
        protected CacheRuntimeContext _context;

        /// <summary> Flag that controls notifications. </summary>
        private Notifications _notifiers = Notifications.None;
        
        private bool _isInProc = false;

        private bool _keepDeflattedObjects = false;

        //public NewTrace nTrace = null;

        public System.IO.StreamWriter writer;


        internal ContinuousQueryManager CQManager;

        public Cache Parent { get; set; }

        internal ReaderResultSetManager RSManager;

       
        /// <summary>
        /// Default constructor.
        /// </summary>
        protected CacheBase()//:this(null)
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
            _keyLockManager = new KeyLockManager<object>(LockRecursionPolicy.SupportsRecursion);

            RSManager = new DataReader.ReaderResultSetManager(_context);
            context.ReaderMgr = RSManager;
            CQManager = new ContinuousQueryManager();
           
           
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
            set { ;}
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
        /// returns continuous query state information during state transfer.
        /// when a node starts state transfer from other node, it pulls continuous query
        /// state first of all and establishes continuous query analyzer framework.
        /// On receiving cache data, this analyzer becomes statefull.
        /// This state can be partial (only registered types are sent first) and predicate
        /// holders are pulled in subsequent requests or it can be full (whole state is sent at once).
        /// </summary>
        /// <returns></returns>

        internal virtual ContinuousQueryStateInfo GetContinuousQueryStateInfo()
        {
            return null;
        }

        /// <summary>
        /// When above state is partial, node pulls predicate holders for each registered type
        /// in subsequent requests using this method.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal virtual IList<PredicateHolder> GetContinuousQueryRegisteredPredicates(string type)
        {
            return null;
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
            //if (writer == null)
            //{
            //    writer = new System.IO.StreamWriter("c:\\" + DateTime.Now.ToLongTimeString().Replace(':', '-') + ".txt", false);
            //    writer.AutoFlush = true;
            //}

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
                    if (notifconfig.Contains("item-add"))
                    {
                        if (Convert.ToBoolean(notifconfig["item-add"]))
                            _notifiers |= Notifications.ItemAdd;
                    }
                    if (notifconfig.Contains("item-update"))
                    {
                        if (Convert.ToBoolean(notifconfig["item-update"]))
                            _notifiers |= Notifications.ItemUpdate;
                    }
                    if (notifconfig.Contains("item-remove"))
                    {
                        if (Convert.ToBoolean(notifconfig["item-remove"]))
                            _notifiers |= Notifications.ItemRemove;
                    }
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
                //nTrace.error("LocalCache.Initialize()", e.Message);
                Dispose();
                throw;
            }
            catch (Exception e)
            {
                //nTrace.error("CacheBase.Initialize()", e.Message);
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }

        public virtual void Initialize(IDictionary cacheClasses, IDictionary properties,  bool twoPhaseInitialization)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                Name = Convert.ToString(properties["id"]);

                if (properties.Contains("notifications"))
                {
                    IDictionary notifconfig = properties["notifications"] as IDictionary;
                    if (notifconfig.Contains("item-add"))
                    {
                        if (Convert.ToBoolean(notifconfig["item-add"]))
                            _notifiers |= Notifications.ItemAdd;
                    }
                    if (notifconfig.Contains("item-update"))
                    {
                        if (Convert.ToBoolean(notifconfig["item-update"]))
                            _notifiers |= Notifications.ItemUpdate;
                    }
                    if (notifconfig.Contains("item-remove"))
                    {
                        if (Convert.ToBoolean(notifconfig["item-remove"]))
                            _notifiers |= Notifications.ItemRemove;
                    }
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
                //nTrace.error("LocalCache.Initialize()", e.Message);
                Dispose();
                throw;
            }
            catch (Exception e)
            {
                //nTrace.error("CacheBase.Initialize()", e.Message);
                Dispose();
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }

        #endregion

        public virtual void ClientConnected(string client, bool isInproc, ClientInfo clientInfo)
        {
            CacheStatistics stats = InternalCache.Statistics;
            if (stats != null && stats.ConnectedClients != null)
            {
                lock (stats.ConnectedClients.SyncRoot)
                {
                    if (!stats.ConnectedClients.Contains(client))
                    {
                        stats.ConnectedClients.Add(client);
                    }
                }
            }
        }
        public virtual void ClientDisconnected(string client, bool isInproc)
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
            }
        }

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

        public virtual void GetKeyList(int bucketId, bool startLogging, out ClusteredArrayList keyList)
        {
            keyList = null;
        }
        public virtual OrderedDictionary GetMessageList(int bucketId)
        {
            return null; 
        }
        public virtual Hashtable GetLogTable(ArrayList bucketIds, ref bool isLoggingStopped, OPLogType type= OPLogType.Cache)
        {
            return null;
        }

        public virtual void RemoveFromLogTbl(int bucketId)
        {
        }

        public virtual void StartLogging(int bucketId)
        {
        }

        public virtual void AddLoggedData(ArrayList bucketIds, OPLogType type = OPLogType.Cache)
        {
        }

        public virtual void UpdateLocalBuckets(ArrayList bucketIds)
        {
        }

        public virtual void StartBucketFilteration(int bucketID,FilterType type)        
        { 
        }

        public virtual void StopBucketFilteration(IList<Int32> buckets, FilterType type)
        {
        }

        public virtual HashVector LocalBuckets
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


        public virtual ActiveQueryAnalyzer QueryAnalyzer
        {
            get { return null; }
        }

        public virtual NewHashmap GetOwnerHashMapTable(out int bucketSize)
        {
            bucketSize = 0;
            return null;
        }
        #endregion



        #region	/                 --- ICache ---           /

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public virtual void Clear(CallbackEntry cbEntry, DataSourceUpdateOptions updateOptions, OperationContext operationContext)
        {
        }

        /// <summary>
        /// Removes all entries from the store.
        /// </summary>
        public virtual void Clear(CallbackEntry cbEntry, DataSourceUpdateOptions updateOptions, string taskId, OperationContext operationContext)
        {
        }


        protected void ClearCQManager()
        {

            if (CQManager != null)
            {
                CQManager.Clear();
            }

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

        public virtual bool Add(object key, CacheSynchronization.CacheSyncDependency syncDependency, OperationContext operationContext)
        {
            return false;
        }

        public virtual bool Add(object key, string group, CacheSynchronization.CacheSyncDependency syncDependency, OperationContext operationContext)
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
        /// the cluster in order to synchronize them.
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

        public virtual QueryResultSet SearchCQ(string query, IDictionary values, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters)
        {
            return null;
        }

        public virtual QueryResultSet SearchEntriesCQ(string query, IDictionary values, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters)
        {
            return null;
        }

        public virtual QueryResultSet SearchCQ(ContinuousQuery query, OperationContext operationContext)
        {
            return null;
        }

        public virtual QueryResultSet SearchCQ(string queryId, OperationContext operationContext)
        {
            return null;
        }

        public virtual QueryResultSet SearchEntriesCQ(ContinuousQuery query, OperationContext operationContext)
        {
            return null;
        }

        public virtual QueryResultSet SearchEntriesCQ(string queryId, OperationContext operationContext)
        {
            return null;
        }

        public virtual DeleteQueryResultSet DeleteQuery(string query, IDictionary values, bool notify, bool isUserOperation, ItemRemoveReason ir, OperationContext operationContext)
        {
            return null;
        }
        public virtual List<Event> GetFilteredEvents(string clientID, Hashtable events, EventStatus _registeredEventStatus)
        {
            return null;
        }



        #region   /--           Bulk Operations              --

        public virtual Hashtable Contains(object[] keys, string group, OperationContext operationContext)
        {
            return null;
        }

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="keys">The keys to locate in the cache.</param>
        /// <returns>List of keys that are not found in the cache.</returns>
        public virtual Hashtable Contains(object[] keys, OperationContext operationContext)
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
            return GetEnumerator(null);
        }

        public virtual IDictionaryEnumerator GetEnumerator(string group)
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


        /// <summary>
        /// Notify the listener that an item is added to the cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="async">flag indicating that the nofitication is asynchronous</param>
        protected virtual void NotifyItemAdded(object key, bool async, OperationContext operationContext, EventContext eventContext)
        {
            if ((Listener != null) && IsItemAddNotifier)
            {
                if (!async)
                    Listener.OnItemAdded(key, operationContext, eventContext);
                else
                    _context.AsyncProc.Enqueue(new AsyncLocalNotifyAdd(Listener, key, operationContext, eventContext));
            }
        }

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
        /// Notifies the listener that an item is modified so pull latest notifications.
        /// </summary>
        public virtual void NotifyPollRequestCallback(string clientId, short callbackId, bool isAsync, EventType eventType)
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

        /// <summary>
        /// Notify the listener that an item is updated in the cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="async">flag indicating that the nofitication is asynchronous</param>
        protected virtual void NotifyItemUpdated(object key, bool async, OperationContext operationContext, EventContext eventContext)
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
        /// Notify the listener that an item is removed from the cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="val">item itself</param>
        /// <param name="reason">reason the item was removed</param>
        /// <param name="async">flag indicating that the nofitication is asynchronous</param>
        protected virtual void NotifyItemRemoved(object key, object val, ItemRemoveReason reason, bool async, OperationContext operationContext, EventContext eventContext)
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
        /// Notify the listener that items are removed from the cache.
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <param name="val">item itself</param>
        /// <param name="reason">reason the item was removed</param>
        /// <param name="async">flag indicating that the nofitication is asynchronous</param>
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

        /// <summary>
        /// Notifies the listener that active query is updated.
        /// </summary>
        /// <param name="key">key of cache item</param>
        /// <param name="entry">Callback entry which contains the item update call back.</param>
        /// <param name="async">flag indicating that the nofitication is asynchronous</param>
        protected virtual void NotifyCQUpdateCallback(object key, QueryChangeType changeType, List<CQCallbackInfo> queries, bool async, OperationContext operationContext, EventContext eventContext)
        {
            if ((Listener != null))
            {
                if (!async)
                    Listener.OnActiveQueryChanged(key, changeType, queries, operationContext, eventContext);
                else
                    _context.AsyncProc.Enqueue(new AsyncCQUpdateCallback(Listener, key, changeType, queries, operationContext, eventContext));
            }
        }

        /// <summary>
        /// Fire when the cache is cleared.
        /// </summary>
        /// <param name="async">flag indicating that the nofitication is asynchronous</param>
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
        protected virtual void NotifyHashmapChanged(long viewId, Hashtable newmap, ArrayList members, bool async, bool updateClientMap)
        {
            if (Listener != null)
            {
#if !CLIENT && !DEVELOPMENT
                _context.AsyncProc.Enqueue(new AsyncLocalNotifyHashmapCallback(Listener, viewId, newmap, members, updateClientMap));
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operationCode"></param>
        /// <param name="result"></param>
        /// <param name="cbEntry"></param>
        protected virtual void NotifyWriteBehindTaskCompleted(OpCode operationCode, Hashtable result, CallbackEntry cbEntry, OperationContext operationContext)
        {
            if (Listener != null)
            {
                InternalCache.DoWrite("CacheBase.NotifyWriteBehindTaskCompleted", "", operationContext);
                Listener.OnWriteBehindOperationCompletedCallback(operationCode, result, cbEntry);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operationCode"></param>
        /// <param name="result"></param>
        /// <param name="entry"></param>
        /// <param name="taskId"></param>
        public virtual void NotifyWriteBehindTaskStatus(OpCode operationCode, Hashtable result, CallbackEntry cbEntry, string taskId, string providerName, OperationContext operationContext)
        {
            if (cbEntry != null && cbEntry.WriteBehindOperationCompletedCallback != null)
            {
                NotifyWriteBehindTaskCompleted(operationCode, result, cbEntry, operationContext);
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
            if (e != null && e.KeysDependingOnMe != null)
            {
                Hashtable entriesTable = new Hashtable();
                entriesTable.Add(key, e);
                string[] nextRemovalKeys = new string[e.KeysDependingOnMe.Count];
                e.KeysDependingOnMe.Keys.CopyTo(nextRemovalKeys, 0);

                while (nextRemovalKeys != null && nextRemovalKeys.Length > 0)
                {
                    entriesTable = _context.CacheImpl.Remove(nextRemovalKeys, ItemRemoveReason.DependencyChanged, true, operationContext);
                    nextRemovalKeys = ExtractKeys(entriesTable);
                }
            }
        }

        public void RemoveCascadingDependencies(Hashtable removedItems, OperationContext operationContext)
        {
            if (removedItems == null) 
                return;

            if (removedItems.Count == 0) 
                return;

            string[] nextRemovalKeys = ExtractKeys(removedItems);
            Hashtable entriesTable = null;
            while (nextRemovalKeys != null && nextRemovalKeys.Length > 0)
            {
                entriesTable = _context.CacheImpl.Remove(nextRemovalKeys, ItemRemoveReason.DependencyChanged, true, operationContext);
                nextRemovalKeys = ExtractKeys(entriesTable);
            }
        }

        public void RemoveDeleteQueryCascadingDependencies(Hashtable removedItems, OperationContext operationContext)
        {
            if (removedItems == null || removedItems.Count == 0) 
                return;
     
            Hashtable entriesTable = null;
            string[] retVal = new string[removedItems.Count];
            removedItems.Keys.CopyTo(retVal, 0);

            while (retVal != null && retVal.Length > 0)
            {
                entriesTable = _context.CacheImpl.Remove(retVal, ItemRemoveReason.DependencyChanged, true, operationContext);
                retVal = ExtractKeys(entriesTable);
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


        #region MapReduce Methods

        public virtual void SubmitMapReduceTask(Runtime.MapReduce.MapReduceTask task, string taskId, TaskCallbackInfo callbackInfo, Filter filter, OperationContext operationContext)
        { }

        public virtual object TaskOperationReceived(MapReduceOperation operation) 
        { return null; }

        public virtual void RegisterTaskNotification(String taskID, TaskCallbackInfo callbackInfo, OperationContext operationContext) 
        { }
    
        public virtual void UnregisterTaskNotification(String taskID, TaskCallbackInfo callbackInfo, OperationContext operationContext) 
        { }

        public virtual void CancelMapReduceTask(String taskId, bool cancelAll)
        { }
            
        public virtual ArrayList GetRunningTasks()
        { return null; }

        public virtual Runtime.MapReduce.TaskStatus GetTaskStatus(string taskId)
        { return null; }

        public virtual List<Common.MapReduce.TaskEnumeratorResult> GetTaskEnumerator(Common.MapReduce.TaskEnumeratorPointer pointer, OperationContext operationContext)
        { return null; }

        public virtual Common.MapReduce.TaskEnumeratorResult GetNextRecord(Common.MapReduce.TaskEnumeratorPointer pointer, OperationContext operationContext)
        { return null; }

        /// <summary>
        /// Calls the registered callback for the task.
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="listenerList"></param>
        /// <param name="async"></param>
        /// <param name="operationContext"></param>
        /// <param name="eventContext"></param>
        /// 

        public void NotifyTaskCallback(string taskId, IList listenerList, bool async, OperationContext operationContext, EventContext eventContext)
        {
            if (Listener != null)
            {
                if (!async)
                {
                    Listener.OnTaskCallback(taskId, listenerList, operationContext, eventContext);
                }
                else
                {
                    _context.AsyncProc.Enqueue(new AsyncLocalNotifyTaskCallback(Listener, taskId, listenerList, operationContext, eventContext));
                }
            }
        }


        public virtual void SendMapReduceOperation(ArrayList dests, MapReduceOperation operation) { }
        public virtual void SendMapReduceOperation(Address target, MapReduceOperation operation) { }

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

        public virtual string RegisterCQ(string query, IDictionary values, string clientUniqueId, string clientId, bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext, QueryDataFilters datafilters)
        {
            return null;
        }

        public virtual void RegisterCQ(ContinuousQuery query, OperationContext operationContext)
        {
        }






        internal virtual void EnqueueDSOperation(DSWriteBehindOperation operation)
        {
            if (operation.TaskId == null)
                operation.TaskId = System.Guid.NewGuid().ToString();
            _context.DsMgr.WriteBehind(operation);
        }

        public virtual void NotifyWriteBehindTaskStatus(Hashtable opResult, string[]taskIds, string provider, OperationContext context)
        {
            CallbackEntry cbEntry = null;
            Object value = null;
            Hashtable status=null;

            if (opResult != null && opResult.Count>0)
            {
                IDictionaryEnumerator result= opResult.GetEnumerator();
                while(result.MoveNext())
                {
                    DSWriteBehindOperation dsOperation = result.Value as DSWriteBehindOperation;
                    if (dsOperation == null) continue;
                    cbEntry = dsOperation.Entry.Value as CallbackEntry;
                    if (cbEntry != null && cbEntry.WriteBehindOperationCompletedCallback !=null)
                    {
                        status = new Hashtable();
                        if (dsOperation.Exception != null)
                            status.Add(dsOperation.Key, dsOperation.Exception);
                        else
                            status.Add(dsOperation.Key, dsOperation.DSOpState);
                        NotifyWriteBehindTaskCompleted(dsOperation.OperationCode,status, cbEntry, context);
                    }
                }
            }
        }

        internal virtual void EnqueueDSOperation(ArrayList operations)
        {
             _context.DsMgr.WriteBehind(operations);
        }

        public virtual RequestStatus GetClientRequestStatus(string clientId, long requestId, long commandId, Address intendedServer)
        {
            return null;
        }
        #region--------------------------------Cache Data Reader----------------------------------------------

        public virtual ClusteredList<ReaderResultSet> ExecuteReader(string query, IDictionary values, bool getData, int chunkSize, bool isInproc, OperationContext operationContext)
        {
            ReaderResultSet result = InternalCache.Local_ExecuteReader(query, values, getData, chunkSize, isInproc, operationContext);
            ClusteredList<ReaderResultSet> resultList = new ClusteredList<ReaderResultSet>();
            if(result != null)
                resultList.Add(result);
            return resultList;
            
        }

        public virtual ReaderResultSet Local_ExecuteReader(string query, IDictionary values, bool getData, int chunkSize, bool isInproc, OperationContext operationContext)
        {
            return null;
        }

        public virtual List<ReaderResultSet> ExecuteReaderCQ(string query, IDictionary values, bool getData, int chunkSize, string clientUniqueId, string clientId,
            bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext,
            QueryDataFilters datafilters, bool isInproc)
        {
            ReaderResultSet result = InternalCache.Local_ExecuteReaderCQ(query, values, getData, chunkSize, clientUniqueId, clientId, notifyAdd, notifyUpdate, notifyRemove, operationContext, datafilters, isInproc);
            List<ReaderResultSet> resultList = new List<ReaderResultSet>();
            if(result != null)
                resultList.Add(result);
            return resultList;
        }

        public virtual ReaderResultSet Local_ExecuteReaderCQ(string queryId, bool getData, int chunkSize, OperationContext operationContext)
        {
            return null;
        }

        public virtual ReaderResultSet Local_ExecuteReaderCQ(string query, IDictionary values, bool getData, int chunkSize, string clientUniqueId, string clientId,
            bool notifyAdd, bool notifyUpdate, bool notifyRemove, OperationContext operationContext,
            QueryDataFilters datafilters, bool isInproc)
        {
            return null;
        }

        public virtual ReaderResultSet Local_ExecuteReaderCQ(ContinuousQuery query, bool getData, int chunkSize, OperationContext operationContext)
        {
            return null;
        }

        public virtual ReaderResultSet GetReaderChunk(string readerId, int nextChunk, bool isInproc, OperationContext operationContext)
        {
            return InternalCache.GetReaderChunk(readerId, nextChunk, isInproc, operationContext);
        }

        public virtual void DisposeReader(string readerId, OperationContext operationContext)
        {
            InternalCache.DisposeReader(readerId, operationContext);
        }
        #endregion

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

        public virtual void RegisterPollingNotification(short callbackId, OperationContext operationContext)
        { }

        internal virtual void HandleDeadClientsNotification(string deadClient, ClientInfo info)
        {

        }

        internal virtual void NotifyClusterOfClientActivity(string cacheId, ClientInfo client, ConnectivityStatus status)
        {
            
        }
        
        internal virtual void RegisterClientActivityCallback(string clientId, CacheClientConnectivityChangedCallback callback) { }

        internal virtual void UnregisterClientActivityCallback(string clientId) { }
        
        public virtual IEnumerable<ClientInfo> GetConnectedClientsInfo()
        {
            return null;
        }

        public virtual void ApplyHotConfiguration(HotConfig hotConfig)
        {
            
        }

        internal virtual void Touch(List<string> keys, OperationContext operationContext)
        {

        }

        internal virtual void SetClusterInactive(string reason)
        {

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

        public virtual MessageInfo GetNextUndeliveredMessage(TimeSpan timeout, OperationContext context)
        {
            return ((IMessageStore)InternalCache).GetNextUndeliveredMessage(timeout, context);
        }

        public virtual SubscriptionInfo GetSubscriber(string topic, SubscriptionType type, OperationContext context)
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

        public virtual IDictionary<string, IList<object>> GetAssignedMessage(SubscriptionInfo subscriptionInfo, OperationContext operationContext)
        {
            return ((IMessageStore)InternalCache).GetAssignedMessage(subscriptionInfo, operationContext);
        }

        public virtual void AcknowledgeMessageReceipt(string clientId, IDictionary<string, IList<string>> topicWiseMessageIds, OperationContext operationContext)
        {

        }

        public virtual void AcknowledgeMessageReceipt(string clientId, string topic,string messageId, OperationContext operationContext)
        {

        }

        public virtual IList<MessageInfo> GetUnacknowledgeMessages(TimeSpan assginmentTimeout)
        {
            return ((IMessageStore)InternalCache).GetUnacknowledgeMessages(assginmentTimeout);
        }


        public virtual void RevokeAssignment(MessageInfo message, SubscriptionInfo subscription, OperationContext context)
        {
            ((IMessageStore)InternalCache).RevokeAssignment(message,subscription,context);
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

        public virtual bool TopicOperation(TopicOperation operation, OperationContext operationContext)
        {
            return false;
        }

        public virtual void RegiserTopicEventListener(ITopicEventListener listener)
        {
            ((IMessageStore)InternalCache).RegiserTopicEventListener(listener);
        }

        public virtual ArrayList GetTopicsState()
        {
            return ((IMessageStore)InternalCache).GetTopicsState();
        }

        public virtual void SetTopicsState(ArrayList topicState)
        {
            ((IMessageStore)InternalCache).SetTopicsState(topicState);
        }

        public virtual TransferrableMessage GetTransferrableMessage(string topic, string messageId)
        {
            return ((IMessageStore)InternalCache).GetTransferrableMessage(topic,messageId);
        }

        public virtual bool StoreTransferrableMessage(string topic, TransferrableMessage message)
        {
             return ((IMessageStore)InternalCache).StoreTransferrableMessage(topic,message);
        }

        public virtual Dictionary<string, TopicStats> GetTopicsStats()
        {
            return ((IMessageStore)InternalCache).GetTopicsStats();
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
        
    }
}
