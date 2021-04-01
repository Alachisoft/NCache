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
using System.Threading;

using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Net;
using System.Text;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.FeatureUsageData;

namespace Alachisoft.NCache.Caching.AutoExpiration
{

    /// <summary>
    /// Summary description for ExpirationManager.
    /// </summary>

    internal class ExpirationManager : IDisposable, ISizableIndex
    {
        #region	/                 --- Monitor Task ---           /

        /// <summary>
        /// The Task that takes care of auto-expiration of items.
        /// </summary>
        class AutoExpirationTask : TimeScheduler.Task
        {
            /// <summary> Reference to the parent. </summary>
            private ExpirationManager _parent = null;

            /// <summary> Periodic interval </summary>
            private long _interval = 1000;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="interval"></param>
            internal AutoExpirationTask(ExpirationManager parent, long interval)
            {
                _parent = parent;
                _interval = interval;
            }

            public long Interval
            {
                get { lock (this) { return _interval; } }
                set { lock (this) { _interval = value; } }
            }

            /// <summary>
            /// Sets the cancel flag.
            /// </summary>
            public void Cancel()
            {
                lock (this) { _parent = null; }
            }

            /// <summary>
            /// True if task is cancelled, false otherwise
            /// </summary>
            public bool IsCancelled
            {
                get { return this._parent == null; }
            }

            /// <summary>
            /// returns true if the task has completed.
            /// </summary>
            /// <returns>bool</returns>
            bool TimeScheduler.Task.IsCancelled()
            {
                lock (this) { return _parent == null; }
            }

            /// <summary>
            /// tells the scheduler about next interval.
            /// </summary>
            /// <returns></returns>
            long TimeScheduler.Task.GetNextInterval()
            {
                lock (this) { return _interval; }
            }

            /// <summary>
            /// This is the main method that runs as a thread. CacheManager does all sorts of house 
            /// keeping tasks in that method.
            /// </summary>
            void TimeScheduler.Task.Run()
            {
                if (_parent == null) return;
                try
                {
                    bool expired = _parent.Expire();
                }
                catch (Exception)
                {
                }
            }
        }


        #endregion

        /// <summary> The top level Cache. esentially to remove the items on the whole cluster for the cascaded dependencies. </summary>
        private NCache.Caching.Cache _topLevelCache;

        /// <summary> The runtime context associated with the current cache. </summary>
        private CacheRuntimeContext _context;

        /// <summary> The periodic auto-expiration task. </summary>
        private AutoExpirationTask _taskExpiry;

        /// <summary> clean interval for expiration in milliseconds. </summary>
        private int _cleanInterval = 30000;

        /// <summary>maximum ratio of items that can be removed on each clean interval. </summary>
        private float _cleanRatio = 1;


        /// <summary> to determine the last slot so expiration can be round robin. </summary>
        private bool _allowClusteredExpiry = true;

        private HashVector _mainIndex = new HashVector();
        /// <summary> Index for all type of expirations. it is used only when main index is

        /// locked for the selection of expired items. </summary>
        private HashVector _transitoryIndex = new HashVector();

        /// A flag used to indicate that index is cleared while expiration was in progress. </summary>
        private bool _indexCleared;

        /// <summary>
        /// flag that indicates that the cache has been cleared after we have selected the keys for expiration.
        /// If set we dont continue with the expiration coz cache has already been cleared.
        /// </summary>
        private bool _cacheCleared = false;

        private object _status_mutex = new object();

        ///It is the interval between two consecutive removal of items from the cluster so that user operation is not affected
        private int _sleepInterval = 0;  //milliseconds

        ///No of items which can be removed in a single clustered operation.
        private int _removeThreshhold = 10;

        private bool _inProgress;
        
        
        /// <summary>Is this node the coordinator node. useful to synchronize database dependent items. </summary>
        private bool _isCoordinator = true;

        /// <summary>Is this node the sub-coordinator node in partitione-of-replica topology. for all other tolpologies its false. </summary>
        private bool _isSubCoordinator = false;

        private long _expirationManagerSize = 0;

        private ILogger _ncacheLog;

        ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

        private int _cacheLastAccessLoggingInterval = 20;
        private int _cacheLastAccessLoggingIntervalPassed;
        private int _cacheLastAccessInterval;
        private bool _cacheLastAccessCountEnabled;
        private bool _cacheLastAccessCountLoggingEnabled;
        private NewTrace _cacheLastAccessTimeLogger;

        /// <summary>
        /// True if this node is a "cordinator" or a "subcordinator" in case of partition-of-replica.
        /// </summary>
        public bool IsCoordinatorNode
        {
            get { return _isCoordinator; }
            set { _isCoordinator = value; }
        }

        /// <summary>
        /// True if this node is a "sub-cordinator". This property only applies to partition-of-replica cluster topology.
        /// </summary>
        public bool IsSubCoordinatorNode
        {
            get { return _isSubCoordinator; }
            set { _isSubCoordinator = value; }
        }

        /// <summary>
        /// A flage which indicates whether expiration is in progress
        /// </summary>
        public bool IsInProgress
        {
            get { lock (_status_mutex) { return _inProgress; } }
            set { lock (_status_mutex) { _inProgress = value; } }
        }

        /// <summary>
        /// Top Level Cache only to remove the cascaded dependencies on clean interval. which is started from top level cache.
        /// </summary>
        internal Cache TopLevelCache
        {
            get { return _topLevelCache; }
            set { _topLevelCache = value; }
        }

        private bool IsCacheLastAccessCountEnabled
        {
            get
            {
                return ServiceConfiguration.EnableCacheLastAccessCount;
            }
        }

        private bool IsCacheLastAccessLoggingEnabled
        {
            get
            {
                bool isCachelastAccessLogEnabled = false;

                isCachelastAccessLogEnabled = ServiceConfiguration.EnableCacheLastAccessCountLogging;

                if (IsCacheLastAccessCountEnabled && isCachelastAccessLogEnabled)
                {

                    string path = System.IO.Path.Combine(AppUtil.LogDir, "log-files");
                    NCacheLog.Info(_context.SerializationContext + (_context.IsStartedAsMirror ? "-replica" : "") + "." + "cache-last-acc-log " + path);
                }

                return isCachelastAccessLogEnabled;
            }
        }

        private int CacheLastAccessCountInterval
        {
            get
            {
                return ServiceConfiguration.CacheLastAccessCountInterval;
            }
        }

        private int CacheLastAccessLoggingInterval
        {
            get
            {
                return ServiceConfiguration.CacheLastAccessLogInterval;
            }
        }



        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="timeSched"></param>
        public ExpirationManager(IDictionary properties, CacheRuntimeContext context)
        {
            _context = context;
            _ncacheLog = context.NCacheLog;

            Initialize(properties);
           
            //new way to do this...
            _sleepInterval = ServiceConfiguration.ExpirationBulkRemoveDelay;
            _removeThreshhold = ServiceConfiguration.ExpirationBulkRemoveSize;

        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (_taskExpiry != null)
            {
                _taskExpiry.Cancel();
                _taskExpiry = null;
            }
            

            lock (_status_mutex)
            {
                _mainIndex.Clear();
                _mainIndex = null;

                _transitoryIndex.Clear();
                _transitoryIndex = null;

                _expirationManagerSize = 0;
            }
            GC.SuppressFinalize(this);
        }

        #endregion

        public long CleanInterval
        {
            get { return _taskExpiry.Interval; }
            set { _taskExpiry.Interval = value; }
        }

        /// <summary>
        /// True if expiry task is disposed, flase otherwise.
        /// </summary>
        public bool IsDisposed
        {
            get { return !(_taskExpiry != null && !_taskExpiry.IsCancelled); }
        }


        /// <summary>
        /// keys on which key dependency exists.
        /// </summary>
        public bool AllowClusteredExpiry
        {
            get { lock (this) { return _allowClusteredExpiry; } }
            set { lock (this) { _allowClusteredExpiry = value; } }
        }


     
        
        #region	/                 --- Initialization ---           /

        /// <summary>
        /// Initialize expiration manager based on Configuration
        /// </summary>
        /// <param name="properties"></param>
        private void Initialize(IDictionary properties)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            if (properties.Contains("clean-interval"))
                _cleanInterval = Convert.ToInt32(properties["clean-interval"]) * 1000;
          
            _cacheLastAccessCountEnabled = IsCacheLastAccessCountEnabled;
            _cacheLastAccessCountLoggingEnabled = IsCacheLastAccessLoggingEnabled;
            _cacheLastAccessInterval = CacheLastAccessCountInterval;
            _cacheLastAccessLoggingInterval = CacheLastAccessLoggingInterval;

        }

        /// <summary>
        /// Start the auto-expiration task
        /// </summary>
        public void Start()
        {
            if (_taskExpiry == null)
            {
                _taskExpiry = new AutoExpirationTask(this, _cleanInterval);
                _context.TimeSched.AddTask(_taskExpiry);
            }
        }

        /// <summary>
        /// Stop the auto-expiration task
        /// </summary>
        public void Stop()
        {
            if (_taskExpiry != null)
            {
                _taskExpiry.Cancel();
            }
        }

        #endregion

        /// <summary>
        /// Initialize the new hint. if no new hint is specified then dispose the old hint.
        /// </summary>
        /// <param name="oldHint"></param>
        /// <param name="newHint"></param>
        public void ResetHint(ExpirationHint oldHint, ExpirationHint newHint)
        {
            lock (this)
            {
                if (newHint != null)
                {
                    if (oldHint != null) ((IDisposable)oldHint).Dispose(); 
                    newHint.Reset(_context);
                }
            }
        }

        public void ResetVariant(ExpirationHint hint)
        {
            lock (this)
            {
                hint.ResetVariant(_context);
            }
        }

        /// <summary>
        /// Clear the expiration index
        /// </summary>
        public void Clear()
        {
            lock (this)
            {
            
                _cacheCleared = true;
            }
            lock (_status_mutex)
            {
                if (!IsInProgress)
                {
                    _mainIndex = new HashVector();
                    _transitoryIndex = new HashVector();
                }
                else
                {
                    _transitoryIndex = new HashVector();

                    _indexCleared = true;
                }
                _expirationManagerSize = 0;
            }
        }

        /// <summary>
        /// Called by the scheduler to remove the items that has expired
        /// </summary>
        public bool Expire()
        {
            //indicates whether some items expired during this interval or not...
            bool expired = false;

            //if user has updated the file then the new values will be reloaded.

            _sleepInterval = ServiceConfiguration.ExpirationBulkRemoveDelay;
            _removeThreshhold = ServiceConfiguration.ExpirationBulkRemoveSize;

            //notification is sent for a max of 100k data if multiple items...
            //otherwise if a single item is greater than 100k then notification is sent for
            //that item only...
            int notifThreshold = 30 * 1024;

            CacheBase cacheInst = _context.CacheImpl;
            CacheBase cache = _context.CacheInternal;
            Cache rootCache = _context.CacheRoot;
            object[] keys = null;
            object[] values = null;

            if (cache == null)
                throw new InvalidOperationException("No cache instance defined");




            bool allowExpire = AllowClusteredExpiry;

         

            //in case of replication and por, only the coordinator/sub-coordinator is responsible to expire the items.
            if (!allowExpire) return false;
            ClusteredArrayList selectedKeys = new ClusteredArrayList();
            ClusteredArrayList dependencyChangedSelectedKeys = new ClusteredArrayList();
            int oldItemsCount = 0;
            HashVector oldeItems = null;

            try
            {
                StartLogging();
                DateTime startTime = DateTime.Now;
                int currentTime = AppUtil.DiffSeconds(startTime);
                
                int cleanSize = (int)Math.Ceiling(cache.Count * _cleanRatio);

                //set the flag that we are going to expire the items.

                if (_cacheLastAccessLoggingIntervalPassed >= _cacheLastAccessLoggingInterval)
                {
                    _cacheLastAccessLoggingInterval = CacheLastAccessLoggingInterval;
                    _cacheLastAccessCountEnabled = IsCacheLastAccessCountEnabled;
                    _cacheLastAccessCountLoggingEnabled = IsCacheLastAccessLoggingEnabled;
                    _cacheLastAccessInterval = CacheLastAccessCountInterval;
                }
                else
                    _cacheLastAccessLoggingIntervalPassed++;


                if (_cacheLastAccessCountEnabled && _cacheLastAccessCountLoggingEnabled)
                {
                    if (_cacheLastAccessLoggingIntervalPassed >= _cacheLastAccessLoggingInterval)
                    {
                        _cacheLastAccessLoggingIntervalPassed = 0;
                        oldeItems = new HashVector();
                    }
                }


                lock (_mainIndex.SyncRoot)
                {
                    IDictionaryEnumerator em = _mainIndex.GetEnumerator();

                    if (em != null)
                    {
                        while (em.MoveNext())
                        {
                            ExpirationHint hint = em.Value as ExpirationHint;

                            if (hint != null)
                            {
                                
                                if (hint.IsFromPool && !hint.PoolManager.IsUsingFakePools && !hint.IsOutOfPool) continue;

                                if (_cacheLastAccessCountEnabled && hint is IdleExpiration)
                                {
                                    IdleExpiration slidingExpHint = hint as IdleExpiration;
                                    TimeSpan diff = AppUtil.GetDateTime(AppUtil.DiffSeconds(DateTime.Now)) - AppUtil.GetDateTime(slidingExpHint.LastAccessTime);
                                    if (diff.TotalMinutes >= _cacheLastAccessInterval)
                                    {
                                        oldItemsCount++;
                                        if (oldeItems != null)
                                        {
                                            oldeItems.Add(em.Key, null);
                                        }
                                    }
                                }
                            }
                            if (hint == null || hint.SortKey.CompareTo(currentTime) >= 0)
                                continue;

                            if (!allowExpire && hint.IsRoutable) continue;

                            if (hint.DetermineExpiration(_context))
                            {

                               

                                    if (hint.GetExpiringHint() is FixedExpiration || hint.GetExpiringHint() is IdleExpiration)
                                        selectedKeys.Add(em.Key);
                                    else
                                        dependencyChangedSelectedKeys.Add(em.Key);

                               
                                if (cleanSize > 0 && selectedKeys.Count == cleanSize) break;
                            }

                            LogFeatureUsage(hint);

                        }
                    }
                }


                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ExpirationManager.Expire()", String.Format("Expiry time for {0}/{1} Items: " + (DateTime.UtcNow - startTime), selectedKeys.Count, /*_expiryIndex.KeyCount*/cache.Count));
            }
            catch (Exception e)
            {
                NCacheLog.Error("ExpirationManager.Expire(bool)", "LocalCache(Expire): " + e.ToString());
            }
            finally
            {

                _context.PerfStatsColl.IncrementCacheLastAccessCountStats(oldItemsCount);


                ApplyLoggs();
                ClusteredArrayList dependentItems = new ClusteredArrayList();
                ClusteredArrayList removedItems = new ClusteredArrayList();
                DateTime startTime = DateTime.Now;

                HashVector expiredItemTable = new HashVector();

                expiredItemTable.Add(ItemRemoveReason.Expired, selectedKeys);//Time based expiration
                expiredItemTable.Add(ItemRemoveReason.DependencyChanged, dependencyChangedSelectedKeys); //FileDependency or any other 

                try
                {
                    IDictionaryEnumerator ide = expiredItemTable.GetEnumerator();

                    while (ide.MoveNext())
                    {
                        selectedKeys = ide.Value as ClusteredArrayList;
                        ItemRemoveReason removedReason = (ItemRemoveReason)ide.Key;

                        if (selectedKeys.Count > 0)
                        {
                            //new architectural changes begins from here.

                            ClusteredArrayList keysTobeRemoved = new ClusteredArrayList();

                            for (int i = 0; i < selectedKeys.Count && !_cacheCleared; i++)
                            {
                                keysTobeRemoved.Add(selectedKeys[i]);
                                if (keysTobeRemoved.Count % _removeThreshhold == 0)
                                {
                                    try
                                    {
                                        if (this.IsDisposed) break;

                                        OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                                        operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                                        object[][] keysExposed = keysTobeRemoved.ToInternalArray();
                                        foreach (object[] keyCollection in keysExposed)
                                        {
                                            ArrayList removed = cache.RemoveSync(keyCollection, removedReason, false, operationContext) as ArrayList;
                                            if (removed != null)
                                                removedItems.AddRange(removed);
                                        }

                                        //set the flag that item has expired from cache...
                                        expired = true;


                                        if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementExpiryPerSecStatsBy(keysTobeRemoved.Count);

                                    }
                                    catch (Exception e)
                                    {
                                        NCacheLog.Error("ExpiryManager.Expire", "an error occurred while removing expired items. Error " + e.ToString());
                                    }
                                    keysTobeRemoved.Clear();
                                    if (removedItems != null && removedItems.Count > 0)
                                    {
                                        dependentItems.AddRange(removedItems);
                                        removedItems.Clear();
                                    }
                                    //we stop the activity of the current thread so that normal user operation is not affected.
                                    Thread.Sleep(_sleepInterval * 1000);
                                }
                            }

                            if (!this.IsDisposed && keysTobeRemoved.Count > 0)
                            {
                                try
                                {
                                    OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                                    operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

                                    object[][] keysExposed = keysTobeRemoved.ToInternalArray();
                                    foreach (object[] keyCollection in keysExposed)
                                    {
                                        ArrayList removed = cache.RemoveSync(keyCollection, removedReason, false, operationContext) as ArrayList;
                                        if (removed != null)
                                            removedItems.AddRange(removed);
                                    }
                                    //set the flag that item has expired from cache...
                                    expired = true;


                                    if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementExpiryPerSecStatsBy(keysTobeRemoved.Count);

                                    if (removedItems != null && removedItems.Count > 0)
                                    {
                                        dependentItems.AddRange(removedItems);
                                        removedItems.Clear();
                                    }
                                }
                                catch (Exception e)
                                {
                                    NCacheLog.Error("ExpiryManager.Expire", "an error occurred while removing expired items. Error " + e.ToString());
                                }
                            }
                        }
                    }

                    if (!this.IsDisposed && dependentItems.Count > 0)
                    {
                        ClusteredArrayList removableList = new ClusteredArrayList();
                        if (rootCache != null)
                        {
                            foreach (object depenentItme in dependentItems)
                            {
                                if (depenentItme == null) continue;
                                removableList.Add(depenentItme);
                                if (removableList.Count % 100 == 0)
                                {
                                    try
                                    {
                                        if (this.IsDisposed) break;
                                        OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                                        operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

                                        rootCache.CascadedRemove(removableList, ItemRemoveReason.DependencyChanged, true, operationContext);


                                        if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementExpiryPerSecStatsBy(removableList.Count);

                                    }
                                    catch (Exception exc)
                                    {
                                        NCacheLog.Error("ExpiryManager.Expire", "an error occurred while removing dependent items. Error " + exc.ToString());
                                    }
                                    removableList.Clear();
                                }
                            }
                            if (!this.IsDisposed && removableList.Count > 0)
                            {
                                try
                                {
                                    OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                                    operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

                                    rootCache.CascadedRemove(removableList, ItemRemoveReason.DependencyChanged, true, operationContext);


                                    if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementExpiryPerSecStatsBy(removableList.Count);

                                }
                                catch (Exception exc)
                                {
                                    NCacheLog.Error("ExpiryManager.Expire", "an error occurred while removing dependent items. Error " + exc.ToString());
                                }
                                removableList.Clear();
                            }
                        }
                    }
                }
                finally
                {

                    _transitoryIndex.Clear();

                    lock (this)
                    {
                        _cacheCleared = false;
                    }

                    if (oldeItems != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        IDictionaryEnumerator ide = oldeItems.GetEnumerator();
                        int count = 1;
                        while (ide.MoveNext())
                        {
                            sb.Append(ide.Key + ", ");

                            if (count % 10 == 0)
                            {
                                sb.Append("\r\n");
                                count = 1;
                            }
                            else
                                count++;
                        }

                        NCacheLog.Info(sb.ToString().Trim());

                    }
                }
            }
            return expired;
        }

        public void UpdateIndex(object key, CacheEntry entry)
        {
            if (key == null || entry == null || entry.ExpirationHint == null || !entry.ExpirationHint.IsIndexable) return;

            ExpirationHint hint = entry.ExpirationHint;

            lock (_status_mutex)
            {
                int addSize = hint.InMemorySize;

                if (!IsInProgress)
                {
                    if (_mainIndex.Contains(key))
                    {
                        ExpirationHint expHint = _mainIndex[key] as ExpirationHint;
                        addSize -= expHint.InMemorySize;
                    }

                    _mainIndex[key] = hint;
                }
                else
                {
                    if (_transitoryIndex.ContainsKey(key))
                    {
                        ExpirationHint expHint = _transitoryIndex[key] as ExpirationHint;
                        if (expHint != null)
                        {
                            addSize -= expHint.InMemorySize;
                        }
                    }

                    _transitoryIndex[key] = hint;
                }
                _expirationManagerSize += addSize;
            }
        }


        public void RemoveFromIndex(object key)
        {
            lock (_status_mutex)
            {
                int removeSize = 0;

                if (!IsInProgress)
                {
                    ExpirationHint expHint = _mainIndex[key] as ExpirationHint;
                    if (expHint != null)
                    {
                        removeSize = expHint.InMemorySize;
                    }
                    _mainIndex.Remove(key);
                }
                else
                {
                    //Adding a with null value indicates that this key has been
                    //removed so we should remove it from the main index.

                    ExpirationHint expHint = _transitoryIndex[key] as ExpirationHint;
                    if (expHint != null)
                    {
                        removeSize = expHint.InMemorySize;
                    }

                    _transitoryIndex[key] = null;
                }
                _expirationManagerSize -= removeSize;
            }
        }

        /// <summary>
        /// We log all the operations in a transitory index when we are iterating on
        /// the main index to determine the expired items. StartLogging causes all the 
        /// the subsequent operation to be directed to the transitory index.
        /// </summary>
        private void StartLogging()
        {
            IsInProgress = true;
        }

        /// <summary>
        /// We log all the operations in a transitory index when we are iterating on
        /// the main index to determine the expired items. StopLogging should be called
        /// after selection of item is completd. We apply all the logs from transitory
        /// index to the main index. A null value in transitory index against a key 
        /// indicates that this item is removed during logging, so we should remove
        /// it from the main log as well.
        /// </summary>
        private void ApplyLoggs()
        {
            lock (_status_mutex)
            {
                IsInProgress = false;
                if (_indexCleared)
                {
                    _mainIndex = new HashVector(25000, 0.7f);
                    _indexCleared = false;
                }

                IDictionaryEnumerator ide = _transitoryIndex.GetEnumerator();

                object key;
                ExpirationHint expHint;
                while (ide.MoveNext())
                {
                    key = ide.Key;
                    expHint = ide.Value as ExpirationHint;

                    ExpirationHint oldEntry = (ExpirationHint)_mainIndex[key];

                    if (expHint != null)
                    {
                        _mainIndex[key] = expHint;
                    }
                    else
                    {
                        //it means this item has been removed;
                        _mainIndex.Remove(key);
                    }

                    if (oldEntry != null)
                        _expirationManagerSize -= oldEntry.InMemorySize;
                }
            }
        }

        public long IndexInMemorySize
        {
            get
            {
                int mainBucketCount = _mainIndex != null ? _mainIndex.BucketCount : 0;
                int transitoryBucketCount = _transitoryIndex != null ? _transitoryIndex.BucketCount : 0;
                return (_expirationManagerSize +
                    ((mainBucketCount + transitoryBucketCount) * Common.MemoryUtil.NetHashtableOverHead));
            }
        }

        private void LogFeatureUsage(ExpirationHint hint)
        {
            switch (hint._hintType)
            {
                case ExpirationHintType.NULL:
                    break;
                case ExpirationHintType.Parent:
                    break;
                case ExpirationHintType.FixedExpiration:
                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.absolute_expiration, FeatureEnum.expiration).UpdateUsageTime();
                    break;
                case ExpirationHintType.TTLExpiration:
                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.sliding_expiration, FeatureEnum.expiration).UpdateUsageTime();
                    break;
                case ExpirationHintType.TTLIdleExpiration:
                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.sliding_expiration, FeatureEnum.expiration).UpdateUsageTime();
                    break;
                case ExpirationHintType.FixedIdleExpiration:
                    FeatureUsageCollector.Instance.GetFeature(FeatureEnum.absolute_expiration, FeatureEnum.expiration).UpdateUsageTime();
                    break;
                case ExpirationHintType.IdleExpiration:
                    break;
                case ExpirationHintType.AggregateExpirationHint:
                    break;               
                default:
                    break;
            }
        }
    }
}
