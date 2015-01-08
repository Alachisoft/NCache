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


namespace Alachisoft.NCache.Caching.AutoExpiration
{

    /// <summary>
    /// Summary description for ExpirationManager.
    /// </summary>
    internal class ExpirationManager : IDisposable,ISizableIndex
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
            /// returns true if the task has completed.
            /// </summary>
            /// <returns>bool</returns>
            public override bool IsCancelled()
            {
                lock (this) { return _parent == null; }
            }

            /// <summary>
            /// tells the scheduler about next interval.
            /// </summary>
            /// <returns></returns>
            public override long GetNextInterval()
            {
                lock (this) { return _interval; }
            }

            /// <summary>
            /// This is the main method that runs as a thread. CacheManager does all sorts of house 
            /// keeping tasks in that method.
            /// </summary>
            public override void Run()
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

        class ExpiryIndexEntry:ISizable
        {
            ExpirationHint _hint;

            public ExpiryIndexEntry(ExpirationHint hint)
            {
                _hint = hint;
            }


            public bool IsExpired(CacheRuntimeContext context)
            {
                if (_hint != null) return _hint.DetermineExpiration(context);
                return false;
            }

            public ExpirationHint Hint
            {
                get { return _hint; }
                set { _hint = value; }
            }

            #region ISizable Members

            public int Size
            {
                get { return ExpiryIndexEntrySize; }
            }

            public int InMemorySize
            {
                get
                {
                    int inMemorySize = this.Size;

                    inMemorySize += inMemorySize <= 24 ? 0 : Common.MemoryUtil.NetOverHead;

                    return inMemorySize;
                }
            }

            private int ExpiryIndexEntrySize
            {
                get
                {
                    return _hint.InMemorySize;
                }
            }

            #endregion
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

        /// <summary> A value used for place holder</summary>
        private static object _DATA = new object();

        /// <summary> A counter which tells how many time expiration occured </summary>
        private ulong _runCount;

        private Hashtable _mainIndex = new Hashtable(25000, 0.7f);
        /// <summary> Index for all type of expirations. it is used only when main index is

        /// locked for the selection of expired items. </summary>
        private Hashtable _transitoryIndex = new Hashtable(25000, 0.7f);

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

        /// <summary>Flag which indicates whether to explicitly call GC.Collect or not</summary>
        private bool _allowExplicitGCCollection = true;

        private bool _inProgress;

        //private ArrayList _nodesThatLeft;

        /// <summary>Is this node the coordinator node. useful to synchronize database dependent items. </summary>
        private bool _isCoordinator = true;

        /// <summary>Is this node the sub-coordinator node in partitione-of-replica topology. for all other tolpologies its false. </summary>
        private bool _isSubCoordinator = false;

        /// <summary> Accumulated size of Expiration Manager </summary>
        private long _expirationManagerSize = 0;

        /// <summary> MaxCount of Index Hashtable </summary>
        private long _mainIndexMaxCount = 0;

        /// <summary> MaxCount of Transitory Index Hashtable</summary>
        private long _transitoryIndexMaxCount = 0;

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

        private bool IsCacheLastAccessCountEnabled
        {
            get
            {
                string enableCacheLastAccessCount="NCacheServer.EnableCacheLastAccessCount";
                bool isCachelastAccessEnabled = false;
                try
                {
                    string str = System.Configuration.ConfigurationSettings.AppSettings[enableCacheLastAccessCount];
                    if (str != null && str != string.Empty)
                    {
                        isCachelastAccessEnabled = Convert.ToBoolean(str);
                    }
                }
                catch (Exception e)
                {
                    NCacheLog.Error("ExpirationManager.IsCacheLastAccessCountEnabled", "invalid value provided for " + enableCacheLastAccessCount);
                }
                return isCachelastAccessEnabled;
            }
        }

        private bool IsCacheLastAccessLoggingEnabled
        {
            get
            {
                bool isCachelastAccessLogEnabled = false;
                try
                {
                    string str = System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.EnableCacheLastAccessCountLogging"];
                    if (str != null && str != string.Empty)
                    {
                        isCachelastAccessLogEnabled = Convert.ToBoolean(str);
                    }
                }
                catch (Exception e)
                {
                    NCacheLog.Error("ExpirationManager.IsCacheLastAccessLoggingEnabled", "invalid value provided for NCacheServer.EnableCacheLastAccessCount");
                }

                if (IsCacheLastAccessCountEnabled && isCachelastAccessLogEnabled)
                {
                    string path = System.IO.Path.Combine(AppUtil.InstallDir, "log-files");
                    NCacheLog.Info(_context.SerializationContext + (_context.IsStartedAsMirror ? "-replica" : "") + "." + "cache-last-acc-log " + path);
                }

                return isCachelastAccessLogEnabled;
            }
        }

        private int CacheLastAccessCountInterval
        {
            get
            {
                string cacheLastAccessCount="NCacheServer.CacheLastAccessCountInterval";
                int isCachelastAccessInterval = _cacheLastAccessInterval;
                try
                {
                    string str = System.Configuration.ConfigurationSettings.AppSettings[cacheLastAccessCount];
                    if (str != null && str != string.Empty)
                    {
                        isCachelastAccessInterval = Convert.ToInt32(str);
                    }
                }
                catch (Exception e)
                {
                    NCacheLog.Error("ExpirationManager.CacheLastAccessCountInterval", "invalid value provided for " + cacheLastAccessCount);
                }
                return isCachelastAccessInterval;
            }
        }

        private int CacheLastAccessLoggingInterval
        {
            get
            {
                string cacheLastAccessLogInterval="NCacheServer.CacheLastAccessLogInterval";
                int isCachelastAccessLogingInterval = _cacheLastAccessLoggingInterval;
                try
                {
                    string str = System.Configuration.ConfigurationSettings.AppSettings[cacheLastAccessLogInterval];
                    if (str != null && str != string.Empty)
                    {
                        isCachelastAccessLogingInterval = Convert.ToInt32(str);
                    }
                }
                catch (Exception e)
                {
                    NCacheLog.Error("ExpirationManager.CacheLastAccessLogInterval", "invalid value provided for " + cacheLastAccessLogInterval);
                }
                return isCachelastAccessLogingInterval;
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

            //muds:
            //new way to do this...
            _sleepInterval = Convert.ToInt32(ServiceConfiguration.ExpirationBulkRemoveDelay);
            _removeThreshhold = Convert.ToInt32(ServiceConfiguration.ExpirationBulkRemoveSize);
            if (System.Configuration.ConfigurationSettings.AppSettings.Get("NCacheServer.EnableGCCollection") != null)
            {
                _allowExplicitGCCollection = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings.Get("NCacheServer.EnableGCCollection"));
            }
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
            get { return !(_taskExpiry != null && !_taskExpiry.IsCancelled()); }
        }


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
                    if (oldHint != null) ((IDisposable)oldHint).Dispose(); //dispose only if newHint is not null
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
                    _mainIndex = new Hashtable(25000, 0.7f);
                    _transitoryIndex = new Hashtable(25000, 0.7f);

                    _transitoryIndexMaxCount = 0;
                    _mainIndexMaxCount = 0;
                }
                else
                {
                    _transitoryIndex = new Hashtable(25000, 0.7f);
                    _transitoryIndexMaxCount = 0;

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

            ulong currentRun = 0;
            lock (this)
            {
                currentRun = _runCount++;
            }

            //if user has updated the file then the new values will be reloaded.
            _sleepInterval = Convert.ToInt32(ServiceConfiguration.ExpirationBulkRemoveDelay);
            _removeThreshhold = Convert.ToInt32(ServiceConfiguration.ExpirationBulkRemoveSize);
           
           
            CacheBase cacheInst = _context.CacheImpl;
            CacheBase cache = _context.CacheInternal;
            Cache rootCache = _context.CacheRoot;
           
            if (cache == null)
                throw new InvalidOperationException("No cache instance defined");

            bool allowExpire = AllowClusteredExpiry;

            //in case of replication, only the coordinator/sub-coordinator is responsible to expire the items.
            if (!allowExpire) return false;
            ArrayList selectedKeys = new System.Collections.ArrayList();
            int oldItemsCount = 0;
            Hashtable oldeItems = null;

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
                        oldeItems = new Hashtable();
                    }
                }
                lock (_mainIndex.SyncRoot)
                {
                    IDictionaryEnumerator em = _mainIndex.GetEnumerator(); 

                    if (em != null)
                    {
                        while (em.MoveNext())
                        {
                            ExpiryIndexEntry expirtyEntry = em.Value as ExpiryIndexEntry;
                            ExpirationHint hint = expirtyEntry.Hint;
                            if (hint != null && _cacheLastAccessCountEnabled && hint is IdleExpiration)
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
                            if (hint == null || hint.SortKey.CompareTo(currentTime) >= 0)
                                continue;

                            if (hint.DetermineExpiration(_context))
                            {
                                selectedKeys.Add(em.Key);
                                if (cleanSize > 0 && selectedKeys.Count == cleanSize) break;
                            }

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
                ArrayList dependentItems = new ArrayList();
                ArrayList removedItems = null;
                DateTime startTime = DateTime.Now;

                Hashtable expiredItemTable = new Hashtable();

                expiredItemTable.Add(ItemRemoveReason.Expired, selectedKeys);//Time based expiration
                try
                {
                    IDictionaryEnumerator ide = expiredItemTable.GetEnumerator();

                    while (ide.MoveNext())
                    {
                        selectedKeys = ide.Value as ArrayList;
                        ItemRemoveReason removedReason = (ItemRemoveReason)ide.Key;

                        if (selectedKeys.Count > 0)
                        {
                            //new architectural changes begins from here.

                            ArrayList keysTobeRemoved = new ArrayList();

                            for (int i = 0; i < selectedKeys.Count && !_cacheCleared; i++)
                            {
                                keysTobeRemoved.Add(selectedKeys[i]);
                                if (keysTobeRemoved.Count % _removeThreshhold == 0)
                                {
                                    try
                                    {
                                        if (this.IsDisposed) break;

                                        OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                                        removedItems = cache.RemoveSync(keysTobeRemoved.ToArray(), removedReason, false, operationContext) as ArrayList;

                                        //set the flag that item has expired from cache...
                                        expired = true;

                                        if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementExpiryPerSecStatsBy(keysTobeRemoved.Count);
                                    }
                                    catch (Exception e)
                                    {
                                        NCacheLog.Error("ExpiryManager.Expire", "an error occured while removing expired items. Error " + e.ToString());
                                    }
                                    keysTobeRemoved.Clear();
                                    //we stop the activity of the current thread so that normal user operation is not affected.
                                    Thread.Sleep(_sleepInterval);
                                }
                            }

                            if (!this.IsDisposed && keysTobeRemoved.Count > 0)
                            {
                                try
                                {
                                    OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                                    removedItems = cache.RemoveSync(keysTobeRemoved.ToArray(), removedReason, false, operationContext) as ArrayList;

                                    //set the flag that item has expired from cache...
                                    expired = true;
                                    if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementExpiryPerSecStatsBy(keysTobeRemoved.Count);
                                }
                                catch (Exception e)
                                {
                                    NCacheLog.Error("ExpiryManager.Expire", "an error occured while removing expired items. Error " + e.ToString());
                                }
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
            if (entry == null || entry.ExpirationHint == null) return;
            UpdateIndex(key, entry.ExpirationHint);
        }

        public void UpdateIndex(object key, ExpirationHint hint)
        {
            if (key == null || hint == null) return;

            lock (_status_mutex)
            {
                int addSize = 0;
                int removeSize = 0;

                if (!IsInProgress)
                {
                    if (!_mainIndex.Contains(key))
                    {
                        ExpiryIndexEntry entry = new ExpiryIndexEntry(hint);
                        _mainIndex[key] = entry;

                        addSize = entry.InMemorySize;

                        if (_mainIndex.Count > _mainIndexMaxCount)
                            _mainIndexMaxCount = _mainIndex.Count;                   
                    }
                    else
                    {
                        ExpiryIndexEntry expEntry = _mainIndex[key] as ExpiryIndexEntry;
                        if (expEntry != null)
                        {
                            removeSize = expEntry.InMemorySize;
                            expEntry.Hint = hint;
                            addSize = expEntry.InMemorySize;
                        }
                    }
                }
                else
                {
                    if (_transitoryIndex[key] == null)
                    {
                        ExpiryIndexEntry entry = new ExpiryIndexEntry(hint);
                        _transitoryIndex[key] = entry;
                        addSize = entry.InMemorySize;

                        if (_transitoryIndex.Count > _transitoryIndexMaxCount)
                            _transitoryIndexMaxCount = _transitoryIndex.Count;
                    }
                    else
                    {
                        ExpiryIndexEntry expEntry = _transitoryIndex[key] as ExpiryIndexEntry;
                        if (expEntry != null)
                        {
                            removeSize = expEntry.InMemorySize;
                            expEntry.Hint = hint;
                            addSize = expEntry.InMemorySize;
                        }
                    }
                }
                _expirationManagerSize -= removeSize;
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
                    ExpiryIndexEntry expEntry = _mainIndex[key] as ExpiryIndexEntry;
                    if (expEntry != null)
                    {
                        removeSize = expEntry.InMemorySize;
                    }
                    _mainIndex.Remove(key);
                }
                else
                {
                    //Adding a with null value indicates that this key has been
                    //removed so we should remove it from the main index.

                    ExpiryIndexEntry expEntry = _transitoryIndex[key] as ExpiryIndexEntry;
                    if (expEntry != null)
                    {
                        removeSize = expEntry.InMemorySize;
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
                    //_mainIndex.Clear();
                    _mainIndex = new Hashtable(25000, 0.7f);
                    _mainIndexMaxCount = 0;
                    _indexCleared = false;
                }

                IDictionaryEnumerator ide = _transitoryIndex.GetEnumerator();

                object key;
                ExpiryIndexEntry expEntry;
                while (ide.MoveNext())
                {
                    key = ide.Key;
                    expEntry = ide.Value as ExpiryIndexEntry;

                    ExpiryIndexEntry oldEntry = (ExpiryIndexEntry)_mainIndex[key];

                    if (expEntry != null)
                    {
                        _mainIndex[key] = expEntry;
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
                return (_expirationManagerSize + ((_mainIndexMaxCount + _transitoryIndexMaxCount) * Common.MemoryUtil.NetHashtableOverHead));
            }
        }
    }
}
