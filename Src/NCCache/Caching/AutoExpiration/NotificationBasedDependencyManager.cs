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
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Logger;
using System.Reflection;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Dependencies;


namespace Alachisoft.NCache.Caching.AutoExpiration
{
    /// <summary>
    /// This class is the listener for all the sqlYukonDepdendency changes.
    /// It maintains the current status of the sqlYukonDependency and expiration manger
    /// asks for these changes from this class.
    /// </summary>
    /// 

    public class NotificationBasedDependencyManager : Alachisoft.NCache.Caching.AutoExpiration.IDBConnectionPool, Alachisoft.NCache.Caching.AutoExpiration.IDependencyChangeListener
    {
        /// <summary>
        /// holds the dependency listeners. a yukon dependency expires if its
        /// listener is no more present in this table.
        /// </summary>
        private Hashtable _listeners;
        private Hashtable _silentListeners;
        private CacheRuntimeContext _context;
        private DbConnectionPool _dbConPool;
        private Hashtable _dependencyTable = Hashtable.Synchronized(new Hashtable());

        NotificationBasedDependencyManager.AsyncOnDepenencyChange _asyncDepChange;

        internal NotificationBasedDependencyManager(CacheRuntimeContext context)
        {
            _context = context;
            _listeners = new Hashtable();
            _silentListeners = new Hashtable();
            _dbConPool = new DbConnectionPool(_context.NCacheLog);
            _asyncDepChange = new AsyncOnDepenencyChange(this);
            _asyncDepChange.Start();
        }

        internal NotificationBasedDependencyManager.AsyncOnDepenencyChange AsyncOnDependencyChange
        {
            get { return _asyncDepChange; }
        }

        /// <summary>
        /// All active listeners.
        /// </summary>
        public Hashtable NotifBasedDependencyListeners
        {
            get { return _listeners; }
        }

        /// <summary>
        /// All non-active listeners.
        /// </summary>
        public Hashtable SilentListeners
        {
            get { return _silentListeners; }
        }

        /// <summary>
        /// Create and initialize dependency listener and Add it to the listener list and 
        /// </summary>
        /// <param name="key">key used to reference object</param>
        /// <param name="connString">connection string used to connect to database</param>
        /// <param name="queryString">query string for which dataset is created to be monitored</param>
        /// <param name="doInitialize"></param>
        /// <param name="hintType"></param>
        /// <returns></returns>
        public bool Add(string key, string connString, string queryString, bool doInitialize, ExpirationHintType hintType)
        {
            DependencyListener listener = null;


            if (hintType == ExpirationHintType.OracleCacheDependency)
            {
#if NET40
                string fullAssemblyName = "Alachisoft.NCache.RuntimeDependencies, Version=" + Assembly.GetExecutingAssembly().GetName().Version;
                System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(fullAssemblyName);
                Type factoryType = assembly.GetType("Alachisoft.NCache.RuntimeDependencies.DependencyListenerFactory");
                Object[] oDLArgs = new Object[7];
                oDLArgs[0] = key;
                oDLArgs[1] = connString;
                oDLArgs[2] = queryString;
                oDLArgs[3] = this;
                oDLArgs[4] = this;
                oDLArgs[5] = _context.NCacheLog;
                oDLArgs[6] = hintType;
                Object dLFactory = Activator.CreateInstance(factoryType);
                MethodInfo dLFactoryCreateMethod = factoryType.GetMethod("Create");

                listener = (DependencyListener)(dLFactoryCreateMethod.Invoke(dLFactory, oDLArgs));
#else
                return true;
#endif
            }
            else if (hintType == ExpirationHintType.SqlYukonCacheDependency)


                if (hintType == ExpirationHintType.SqlYukonCacheDependency)
                listener = new YukonDependencyListener(key, connString, queryString, _context, hintType);
            else if (hintType == ExpirationHintType.NosDBCacheDependency)
                listener = new NosDBDependencyListener(key, connString, queryString, _context, hintType);
            if (doInitialize)
            {
                listener.Initialize();
                lock (_listeners.SyncRoot)
                {
                    if (!_listeners.Contains(listener.CacheKey))
                    {
                        ArrayList _dblistner = new ArrayList();
                        _dblistner.Add(listener);
                        _listeners[listener.CacheKey] = _dblistner;
                    }
                    else
                    {
                        ArrayList _dblistner = (ArrayList)_listeners[listener.CacheKey];
                        _dblistner.Add(listener);
                        _listeners[listener.CacheKey] = _dblistner;
                    }
                }
            }
            else
            {
                lock (_silentListeners.SyncRoot)
                {
                    _silentListeners[listener.CacheKey] = listener;
                }
            }
            return true;
        }

        public bool Add(string key, string connString, string queryString, bool doInitialize, ExpirationHintType hintType, CommandType cmdType, IDictionary cmdParams, int timeout)
        {
          
            DependencyListener listener = null;

            if (hintType == ExpirationHintType.SqlYukonCacheDependency)
            {
                listener = new YukonDependencyListener(key, connString, queryString, _context, hintType, cmdType, cmdParams);
            }
            else if (hintType == ExpirationHintType.NosDBCacheDependency)
            {
                listener = new NosDBDependencyListener(key, connString, queryString, _context, hintType, timeout, cmdParams);
            }

            else if (hintType == ExpirationHintType.OracleCacheDependency)
            {
#if NET40
                string currentAssemblyFullName = System.Reflection.Assembly.GetExecutingAssembly().FullName;
                string fullAssemblyName = currentAssemblyFullName.Replace("Alachisoft.NCache.Cache", "Alachisoft.NCache.RuntimeDependencies");
                System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(fullAssemblyName);
                Type factoryType = assembly.GetType("Alachisoft.NCache.RuntimeDependencies.DependencyListenerFactory");
                Object[] oDLArgs = new Object[9];
                oDLArgs[0] = key;
                oDLArgs[1] = connString;
                oDLArgs[2] = queryString;
                oDLArgs[3] = this;
                oDLArgs[4] = this;
                oDLArgs[5] = _context.NCacheLog;
                oDLArgs[6] = hintType;
                oDLArgs[7] = cmdType;
                oDLArgs[8] = cmdParams;
                Object dLFactory = Activator.CreateInstance(factoryType);
                MethodInfo dLFactoryCreateMethod = factoryType.GetMethod("Create");

                listener = (DependencyListener)(dLFactoryCreateMethod.Invoke(dLFactory, oDLArgs));
#else
                return true;
#endif
            }


            if (doInitialize)
            {
                listener.Initialize();
                lock (_listeners.SyncRoot)
                {
                    if (!_listeners.Contains(listener.CacheKey))
                    {
                        ArrayList _dblistner = new ArrayList();
                        _dblistner.Add(listener);
                        _listeners[listener.CacheKey] = _dblistner;
                    }
                    else
                    {
                        ArrayList _dblistner = (ArrayList)_listeners[listener.CacheKey];
                        _dblistner.Add(listener);
                        _listeners[listener.CacheKey] = _dblistner;
                    }
                }
            }
            else
            {
                lock (_silentListeners.SyncRoot)
                {
                    _silentListeners[listener.CacheKey] = listener;
                }
            }
            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(string key)
        {
            return _listeners.Contains(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            ArrayList _dblistner = (ArrayList)_listeners[key];
            DependencyListener depLisner;
            lock (_listeners.SyncRoot)
            {
                if (_dblistner != null)
                {
                    for (int i = 0; i < _dblistner.Count; i++)
                    {
                        depLisner = _dblistner[i] as DependencyListener;

                        if (depLisner != null)
                            depLisner.Stop();
                    }
                }
                _listeners.Remove(key);
                depLisner = null;
            }
            lock (_silentListeners.SyncRoot)
            {
                DependencyListener listener = (DependencyListener)_silentListeners[key];
                if (listener != null)
                {
                    _silentListeners.Remove(key);
                    listener = null;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public void RemoveWithoutStop(string key)
        {
            lock (_listeners.SyncRoot)
            {
                ArrayList _dblistner = (ArrayList)_listeners[key];


                if (_dblistner != null)
                {
                    _listeners.Remove(key);
                    _dblistner = null;
                }
            }
            lock (_silentListeners.SyncRoot)
            {
                DependencyListener listener = (DependencyListener)_silentListeners[key];
                if (listener != null)
                {
                    _silentListeners.Remove(key);
                    listener = null;
                }
            }
        }

        public void EndOperations()
        {
            //This thread will only run on the Main node and not on replica
            _asyncDepChange.Stop(true);
        }

        public void Clear()
        {
            lock (_listeners.SyncRoot)
            {
                IDictionaryEnumerator ine = _listeners.GetEnumerator();
                while (ine.MoveNext())
                {
                    ArrayList _dblistner = (ArrayList)ine.Value;
                    DependencyListener depLisner;

                    if (_dblistner != null)
                    {
                        for (int i = 0; i < _dblistner.Count; i++)
                        {
                            depLisner = _dblistner[i] as DependencyListener;

                            if (depLisner != null)
                                depLisner.Stop();
                        }
                    }
                    depLisner = null;
                }
                _listeners.Clear();
            }
            _silentListeners.Clear();
            Stop();
        }

        public void OnDependencyChanged(DependencyListener sender, string cacheKey, bool changed, bool restart, bool error, bool invalid)
        {
            lock (AsyncOnDependencyChange.Queue)
            {
                AsyncOnDependencyChange.Queue.Enqueue(new DependencyListnerInformation(cacheKey, changed, restart, error, invalid, sender));
                Monitor.Pulse(AsyncOnDependencyChange.Queue);
            }

        }

        internal void ActivateSilentListeners()
        {
            lock (_listeners.SyncRoot)
            {
                try
                {
                    IDictionaryEnumerator listnersDic = _silentListeners.GetEnumerator();
                    while (listnersDic.MoveNext())
                    {
                        DependencyListener listener = (DependencyListener)listnersDic.Value;
                        listener.Initialize();
                        lock (_listeners.SyncRoot)
                        {
                            _listeners[listener.CacheKey] = new ArrayList().Add(listener);
                        }
                    }
                    _silentListeners.Clear();
                }
                catch (Exception)
                {
                    if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("YukonDependencyManager", "silent Listeners Activated. Listeners: " + _listeners.Count + " SilentListeners: " + _silentListeners.Count);
                }
            }
            if (_context.NCacheLog.IsErrorEnabled) _context.NCacheLog.Error("YukonDependencyManager", "silent Listeners Activated. Listeners: " + _listeners.Count + " SilentListeners: " + _silentListeners.Count);
        }

        #region Notification Based Dependency Manager: System.Data.SqlClient.SqlDependency Start/Stop

        private void StopDependencyAsync(object state)
        {
          
            string[] depSettings = (string[])state;
#if !NETCORE
            //TODO: ALACHISOFT
            SqlDependency.Stop(depSettings[0], depSettings[1]);
#endif
        }

        public void StopDependency(string connectionString, string queueName)
        {
            try
            {
                if (_dependencyTable != null)
                {
                    lock (_dependencyTable.SyncRoot)
                    {
                        if (_dependencyTable.Contains(connectionString))
                        {
                            int refCount = (int)_dependencyTable[connectionString];
                            if (--refCount == 0)
                            {
                                _dependencyTable.Remove(connectionString);
                                if (_dependencyTable.Count == 0) _dependencyTable = null;
                                ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(StopDependencyAsync), new String[] { connectionString, queueName });
                             }
                            else
                            {                                     
                                _dependencyTable[connectionString] = refCount;
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }


        public void Stop()
        {
            try
            {

                if (_dependencyTable != null)
                {
                    ICollection keys = _dependencyTable.Keys;
                    foreach (string key in keys)
                    {
                        try {
#if !NETCORE
                            //TODO: ALACHISOFT
                            SqlDependency.Stop(key); 
#endif
                        }
                        catch (Exception exp)
                        {
                            _context.NCacheLog.Error("NotificationBasedDependencyManager.Stop()", exp.ToString());

                        }
                    }

                }
            }
            catch (Exception) { }
        }

        public void StartDependency(string connectionString, string queueName)
        {
            try
            {
                if (_dependencyTable == null) _dependencyTable = Hashtable.Synchronized(new Hashtable());

                lock (_dependencyTable.SyncRoot)
                {
                    if (!_dependencyTable.Contains(connectionString))
                    {
                        int refCount = 1;
#if !NETCORE
                        //TODO: ALACHISOFT
                        SqlDependency.Start(connectionString, queueName);
#endif
                        _dependencyTable[connectionString] = refCount;
                    }
                    else
                    {
                        int refCount = (int)_dependencyTable[connectionString];
                        refCount++;
                        _dependencyTable[connectionString] = refCount;
                    }
                }
            }
            catch (Exception ex) { throw ex; }
        }

#endregion
        
        #region Notification Based Dependency Manager: DB Conection pool operations

        /// <summary>
        /// Creates a new SqlConnection resource if not already exists. 
        /// </summary>
        /// <param name="connectionString">connection string</param>
        /// <param name="connection">An initialized connection object</param>
        /// <returns>sqlconnection for the given connection string.</returns>
        public IDbConnection AddToDbConnectionPool(string connectionString, IDbConnection connection)
        {
            return _dbConPool.PoolConnection(connectionString, connection);
        }

        /// <summary>
        /// Decrements the sqlconnection reference count by 1. when ref count reaches 0 it closes the connection.
        /// </summary>
        /// <param name="connectionString">Connection string for the connection.</param>
        /// <param name="isConnectionInvalid">true if connection is not valid any more.</param>
        public void RemoveFromDbConnectionPool(string connectionString, bool isConnectionInvalid)
        {
            if (isConnectionInvalid)
                _dbConPool.RemoveSeveredConnection(connectionString);
            else
                _dbConPool.RemoveConnection(connectionString);
        }

        /// <summary>
        /// Decrements the ref count and closes the connection if ref count reaches zero.
        /// </summary>
        /// <param name="connectionString"></param>
        public void RemoveFromDbConnectionPool(string connectionString)
        {
            _dbConPool.RemoveConnection(connectionString);
        }
#endregion

        internal class DependencyListnerInformation
        {

            private DependencyListener _listner;

            internal DependencyListener Listner
            {
                get { return _listner; }
                set { _listner = value; }
            }

            private string _cacheKey;

            public string CacheKey
            {
                get { return _cacheKey; }
                set { _cacheKey = value; }
            }

            private bool _change;

            public bool isChanged
            {
                get { return _change; }
                set { _change = value; }
            }

            private bool _restart;

            public bool isRestart
            {

                get { return _restart; }
                set { _restart = value; }
            }

            private bool _error;

            public bool isError
            {
                get { return _error; }
                set { _error = value; }
            }

            private bool _invalid;

            public bool isInvalid
            {
                get { return _invalid; }
                set { _invalid = value; }
            }


            private NotificationBasedDependencyManager _notifBasedDepManager;

            public NotificationBasedDependencyManager NotifBasedDepManager
            {
                get { return _notifBasedDepManager; }
                set { _notifBasedDepManager = value; }
            }

            public DependencyListnerInformation(string cacheKey, bool change, bool restart, bool error, bool invalid, DependencyListener listner)
            {
                _cacheKey = cacheKey;
                _change = change;
                _restart = restart;
                _invalid = invalid;
                _error = error;
                _listner = listner;
            }
        }

        internal class AsyncOnDepenencyChange
        {
            private bool _isThreadStopped = true;
            private Thread _dependencyChangeThread;
            private bool _trimToSize = false;
            private NotificationBasedDependencyManager _notificationManager;
            private Queue _queue;

            public AsyncOnDepenencyChange(NotificationBasedDependencyManager notif)
            {
                _notificationManager = notif;
            }
            
            public Queue Queue
            {
                get { return _queue; }
            }

            internal void Start()
            {
                if (this._isThreadStopped)
                {
                    _queue = new Queue(5, 5);
                    _isThreadStopped = false;
                    _dependencyChangeThread = new Thread(new ThreadStart(this.Run));
                    _dependencyChangeThread.Name = "AsyncOnDepenencyChange.Run";
                    _dependencyChangeThread.IsBackground = true;
                    _dependencyChangeThread.Start();
                }
            }

            internal void Stop(bool graceFullStop)
            {
                _isThreadStopped = true;
                if (_dependencyChangeThread != null && _dependencyChangeThread.IsAlive)
                {

                    lock (this.Queue)
                    {
                        _queue.Clear();
                        Monitor.Pulse(this.Queue);
                    }
                    if (graceFullStop)
                    {
                        if (!_dependencyChangeThread.Join(6000))    //Wait for 5 secs and then abort the thread
                        {
                            //If not terminated then Abort the thread
                            try
                            {
                                _dependencyChangeThread.Interrupt();
                                _dependencyChangeThread.Abort();
                            }
                            catch (Exception) { }
                        }
                    }
                    else
                    {
                        try
                        {
                            _dependencyChangeThread.Interrupt();
                            _dependencyChangeThread.Abort();
                        }
                        catch (Exception) { }
                    }


                }
            }

            private void Run()
            {
                while (!this._isThreadStopped)
                {
                    lock (this.Queue)
                    {
                        if (this.Queue.Count == 0)
                            Monitor.Wait(this.Queue, 3000);
                        if (this._isThreadStopped)
                            break;

                        if (this.Queue.Count == 0)
                        {
                            if (!_trimToSize)
                                continue;
                            this.Queue.TrimToSize();
                            this._trimToSize = false;
                            continue;
                        }

                        if (this.Queue.Count > 100)
                            this._trimToSize = true;
                    }

                    DependencyListnerInformation instance = null;
                    lock (this.Queue)
                    {
                        instance = this.Queue.Dequeue() as DependencyListnerInformation;

                        if (instance == null)
                        {
                           continue;
                        }
                    }
                   
                    try
                    {
                        DependencyListener depListner = instance.Listner;
                        if (instance.isChanged || instance.isRestart || instance.isError)
                        {
                            if (_notificationManager._context.CacheImpl != null)
                            {
                                CacheEntry entry = _notificationManager._context.CacheImpl.Get(instance.CacheKey, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

                                if (entry != null && entry.ExpirationHint != null && entry.ExpirationHint.NeedsReSync && _notificationManager._context.DsMgr != null)
                                {
                                    _notificationManager._context.DsMgr.ResyncCacheItemAsync(instance.CacheKey, entry.ExpirationHint, null, entry.GroupInfo, entry.QueryInfo, entry.ResyncProviderName);
                                }
                                else
                                {
                                    _notificationManager._context.NCacheLog.Info("DependencyListener.OnDependencyChanged", String.Format("Removing {0} ", instance.CacheKey));
                                    CacheEntry ent = _notificationManager._context.CacheImpl.Remove(instance.CacheKey, ItemRemoveReason.DependencyChanged, true, null, 0, LockAccessType.IGNORE_LOCK, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

                                    if (ent != null)
                                    {
                                        _notificationManager._context.CacheImpl.RemoveCascadingDependencies(instance.CacheKey, ent, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                                    }

                                }

                                lock (_notificationManager._context.ExpiryMgr.NotifBasedDepManager.NotifBasedDependencyListeners.SyncRoot)
                                {
                                    _notificationManager._context.ExpiryMgr.NotifBasedDepManager.Remove(instance.CacheKey);
                                }

                               
                            }
                        }
                        else if (instance.isInvalid) //This status is only sent by SQL Dependency
                        {
                            if (_notificationManager._context.CacheImpl != null)
                            {
                                CacheEntry entry = _notificationManager._context.CacheImpl.Get(instance.CacheKey, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

                                _notificationManager._context.NCacheLog.Error("DependencyListener.OnDependencyChanged", String.Format("Removing {0} because SQLDependency cannot be registered SqlNotificationEventArgs.Info is returned with Invalid status", instance.CacheKey));
                                CacheEntry ent = _notificationManager._context.CacheImpl.Remove(instance.CacheKey, ItemRemoveReason.DependencyInvalid, true, null, 0, LockAccessType.IGNORE_LOCK, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                               
                                if (ent != null)
                                {
                                    _notificationManager._context.CacheImpl.RemoveCascadingDependencies(instance.CacheKey, ent, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                                }

                                if (ent != null && ((CacheEntry)ent).Value is CallbackEntry)
                                {
                                    CallbackEntry cbEtnry = ((CacheEntry)ent).Value as CallbackEntry;
                                    if (cbEtnry != null && cbEtnry.ItemRemoveCallbackListener != null && cbEtnry.ItemRemoveCallbackListener.Count > 0)
                                    {
                                        _notificationManager._context.CacheInternal.NotifyCustomRemoveCallback(instance.CacheKey, cbEtnry, ItemRemoveReason.DependencyInvalid, true, null, null);
                                    }
                                }

                                lock (_notificationManager._context.ExpiryMgr.NotifBasedDepManager.NotifBasedDependencyListeners.SyncRoot)
                                {
                                    _notificationManager._context.ExpiryMgr.NotifBasedDepManager.Remove(instance.CacheKey);
                                }

                            }
                        }                      
                    }
                    catch (Exception exception)
                    {
                        _notificationManager._context.NCacheLog.Error("DependencyListener.OnDependencyChanged", exception.ToString());
                    }                    
                }


            }

        }

        /// <summary>
        /// Class that holds the instances of the YukonDependecy instances in it.
        /// It is also the event handler for the dependency.
        /// </summary>
        private sealed class YukonDependencyListener : DependencyListener
        {
#if !NETCORE
            private SqlDependency _sqlYukonDep = null;
#endif
            private IDictionary _cmdParams;
            private CommandType _cmdType;
            protected CacheRuntimeContext _context;
            protected NotificationBasedDependencyManager _notifBasedDepManager = null;

#if !NETCORE
            System.Data.SqlClient.OnChangeEventHandler _handler;
#endif
            /// <summary>
            /// Initialize instance of oracle dependency listener
            /// </summary>
            /// <param name="key">key used to reference object</param>
            /// <param name="connString">connection string used to connect database</param>
            /// <param name="queryString">query string for which dataset is created to be monitored</param>
            /// <param name="context">current cache runtime context</param>
            /// <param name="hintType">expiration hint type</param>
            internal YukonDependencyListener(string key, string connString, string queryString, CacheRuntimeContext context, ExpirationHintType hintType)
                : this(key, connString, queryString, context, hintType, CommandType.Text, null)
            {
            }

            internal YukonDependencyListener(string key, string connString, string queryString, CacheRuntimeContext context, ExpirationHintType hintType, CommandType cmdType, IDictionary cmdParams)
                : base(key, connString, queryString, context.ExpiryMgr.NotifBasedDepManager, context.ExpiryMgr.NotifBasedDepManager, context.NCacheLog, hintType)
            {
                _cmdParams = cmdParams;
                _cmdType = cmdType;
                _context = context;

                _notifBasedDepManager = _context.ExpiryMgr.NotifBasedDepManager;

            }

            /// <summary>
            /// Initializes the sql dependency instance. registers the change event handler for it.
            /// </summary>
            /// <returns>true if the dependency was successfuly initialized.</returns>
            public override bool Initialize()
            {
                SqlCommand sqlCmd = null;
                SqlConnection con = null;

                try
                {
                    con = (SqlConnection)_connectionPool.AddToDbConnectionPool(_connString, new SqlConnection(base._connString));
                    sqlCmd = this.GetCommand(con);
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                if (sqlCmd != null)
                {
                    try
                    {
                        this.RegisterNotification(sqlCmd);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        if (ex is System.InvalidOperationException || (ex is System.Data.SqlClient.SqlException && ((System.Data.SqlClient.SqlException)ex).Number == 233))
                        {
                            NCacheLog.Error("NotificationBasedDependencyManager.YukonDependencyListener: " + ex.ToString());
                            con.Close();
                            _connectionPool.RemoveFromDbConnectionPool(_connString, true);

                            con = (SqlConnection)_connectionPool.AddToDbConnectionPool(_connString, new SqlConnection(base._connString));
                            sqlCmd = this.GetCommand(con);

                            if (sqlCmd != null)
                            {
#if !NETCORE
                                try
                                {
                                    SqlDependency.Start(_connString);
                                    this.RegisterNotification(sqlCmd);
                                    return true;
                                }
                                catch (Exception exc)
                                {
                                    if (ex is System.InvalidOperationException || (ex is System.Data.SqlClient.SqlException && ((System.Data.SqlClient.SqlException)ex).Number == 233))
                                    {
                                        NCacheLog.Error("NotificationBasedDependencyManager.YukonDependencyListener: " + exc.ToString());
                                        con.Close();
                                        _connectionPool.RemoveFromDbConnectionPool(_connString, true);

                                        con = (SqlConnection)_connectionPool.AddToDbConnectionPool(_connString, new SqlConnection(base._connString));
                                        sqlCmd = this.GetCommand(con);

                                        if (sqlCmd != null)
                                        {
                                            try
                                            {
                                                SqlDependency.Start(_connString);
                                                this.RegisterNotification(sqlCmd);
                                                return true;
                                            }
                                            catch (Exception e)
                                            {
                                                _connectionPool.RemoveFromDbConnectionPool(_connString);
                                                con.Close();
                                                throw e;
                                            }
                                        }
                                        return true;
                                    }
                                    else
                                        throw exc;
                                }
#elif NETCORE
                                //TODO: ALACHISOFT
                                throw new NotImplementedException();
#endif
                            }
                            return true;
                        }
                        else
                            throw ex;
                    }
                }
                return false;
            }

            private SqlCommand GetCommand(SqlConnection con)
            {
                SqlCommand sqlCmd = null;

                sqlCmd = new SqlCommand(_queryString, con);

                switch (_cmdType)
                {
                    case CommandType.Text:
                        sqlCmd.CommandType = CommandType.Text;
                        break;

                    case CommandType.StoredProcedure:
                        sqlCmd.CommandType = CommandType.StoredProcedure;
                        break;

                    default:
                        sqlCmd.CommandType = CommandType.Text;
                        break;
                }

                if (_cmdParams != null && _cmdParams.Count > 0)
                {
                    IDictionaryEnumerator ide = _cmdParams.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        SqlCmdParams param = (SqlCmdParams)ide.Value;
                        SqlParameter sqlparam = new SqlParameter();
                        sqlparam.CompareInfo = param.CmpInfo;
                        sqlparam.Direction = param.Direction;
                        sqlparam.IsNullable = param.IsNullable;
                        sqlparam.LocaleId = param.LocaleID;
                        sqlparam.Offset = param.Offset;
                        sqlparam.ParameterName = ide.Key.ToString();
                        sqlparam.Precision = param.Precision;
                        sqlparam.Scale = param.Scale;
                        sqlparam.Size = param.ParamSize;
                        sqlparam.SourceColumn = param.SourceColumn;
                        sqlparam.SourceColumnNullMapping = param.SourceColumnNullMapping;
                        sqlparam.SourceVersion = param.SrcVersion;
                        sqlparam.SqlDbType = param.DbType;
                        sqlparam.SqlValue = param.SqlValue;
                        sqlparam.TypeName = param.TypeName;
#if !NETCORE
                        sqlparam.UdtTypeName = param.UdtName;
#endif
                        sqlparam.Value = param.Value == null ? DBNull.Value : param.Value;

                        sqlCmd.Parameters.Add(sqlparam);
                    }
                }

                return sqlCmd;
            }

            /// <summary>
            /// Register callback and associate query result set with the command
            /// </summary>
            /// <param name="command"></param>
            private void RegisterNotification(SqlCommand sqlCmd)
            {

                SqlDataReader reader = null;
                try
                {
#if !NETCORE
                    if (_context.SQLDepSettings.UseDefaultServiceQueue)   //If configured to use existing service and queue
                    {
                        this._sqlYukonDep = new SqlDependency();
                        this._sqlYukonDep.AddCommandDependency(sqlCmd);
                    }
                    else
                    {
                        this._sqlYukonDep = new SqlDependency(sqlCmd, _context.SQLDepSettings.GetDependencyOptions(base._connString), int.MaxValue);
                    }

                    _notifBasedDepManager.StartDependency(base._connString, _context.SQLDepSettings.QueueName);
                  
                    _handler = new System.Data.SqlClient.OnChangeEventHandler(this.OnYukonDependencyChanged);
                    this._sqlYukonDep.OnChange += _handler;
                    reader = sqlCmd.ExecuteReader();
                    reader.Read();
#elif NETCORE
                    //TODO: ALACHISOFT
                    throw new NotImplementedException();
#endif
                }
                catch (Exception ex)
                {
                    NCacheLog.Error("YukoNDep", ex.ToString());
                    throw ex;
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }

            }

#if !NETCORE
            /// <summary>
            /// When Sql Server 2005 notifies that the data has changed, SqlDependency OnChange
            /// Event is called which in turn calls this handler.
            /// Here we set that our SqlYukonDependency object has changed.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>  
            //TODO: ALACHISOFT
            private void OnYukonDependencyChanged(object sender, SqlNotificationEventArgs eventArgs)
            {
                base.OnDependencyChanged(eventArgs.Type == SqlNotificationType.Change, //data set changed
                    eventArgs.Info == SqlNotificationInfo.Restart, //sql server is restarted
                    eventArgs.Info == SqlNotificationInfo.Error,
                    eventArgs.Info == SqlNotificationInfo.Invalid //statement provided that cannot be notified
                    );//some error occurred on server
            }
#endif

            public override void Stop()
            {
                try
                {
                    base.Stop();
#if !NETCORE
                    if (_handler != null) this._sqlYukonDep.OnChange -= _handler;
#endif
                    //if QueueName is null default queue will be stopped
                    _notifBasedDepManager.StopDependency(base._connString, _context.SQLDepSettings.QueueName);


                }
                catch (Exception exc)
                {
                    NCacheLog.Error("NotificationBasedDependencyManager.YukonDependencyListener", exc.ToString());
                }
            }

            /// <summary>
            /// returns a unique hashcode as this instance needs to 
            /// be placed in a hashtable.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return base._cacheKey.GetHashCode();
            }

            /// <summary>
            /// compares two instances of this class for equality.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                if (obj is NotificationBasedDependencyManager.YukonDependencyListener)
                {
                    bool key, conn, query;
                    key = base._cacheKey.Equals(((NotificationBasedDependencyManager.YukonDependencyListener)obj)._cacheKey);
                    conn = base._connString.Equals(((NotificationBasedDependencyManager.YukonDependencyListener)obj)._connString);
                    query = base._queryString.Equals(((NotificationBasedDependencyManager.YukonDependencyListener)obj)._queryString);
                    if (key && conn && query)
                    {
                        return true;
                    }

                }
                return false;
            }

        }

        internal void Dispose(bool graceFull)
        {
            Clear();
            if (_dbConPool != null)
                _dbConPool.Dispose();
            if (_asyncDepChange != null)
                _asyncDepChange.Stop(graceFull);
        }

        public IDbConnection GetConnection(string connString)
        {
            return this._dbConPool.GetConnection(connString);
        }

        private sealed class NosDBDependencyListener : DependencyListener, IDependencyListener
        {

            private readonly IDictionary _cmdParams;
            private readonly int _timeout;
            private IDependencyProvider _dependencyProvider;

            internal NosDBDependencyListener(string key, string connString, string queryString,
                CacheRuntimeContext context, ExpirationHintType hintType, int timeOut = 0, IDictionary cmdParams = null)
                : base(
                    key, connString, queryString, context.ExpiryMgr.NotifBasedDepManager,
                    context.ExpiryMgr.NotifBasedDepManager, context.NCacheLog, hintType)
            {
                _cmdParams = cmdParams;
                _timeout = timeOut;
            }

            /// <summary>
            /// Initializes the sql dependency instance. registers the change event handler for it.
            /// </summary>
            /// <returns>true if the dependency was successfuly initialized.</returns>
            public override bool Initialize()
            {
                RegisterNotification();
                return true;

            }

            /// <summary>
            /// Register callback and associate query result set with the command
            /// </summary>
            private void RegisterNotification()
            {
                try
                {
                    string fullAssemblyName = "Alachisoft.NCache.NosDBCacheDependency, Version=" + ServiceConfiguration.NosDBDependencyProviderVersion + ", Culture=neutral, PublicKeyToken=cff5926ed6a53769, processorArchitecture=MSIL";
                    Assembly assembly = Assembly.Load(fullAssemblyName);
                    Type providerType = assembly.GetType("Alachisoft.NCache.NosDBCacheDependency.NosDBDependencyProvider");
                    if (providerType == null)
                        throw new Exception("Alachisoft.NCache.NosDBCacheDependency.NosDBDependencyProvider is not found in  assembly Alachisoft.NCache.NosDBCacheDependency");
                    _dependencyProvider = Activator.CreateInstance(providerType) as IDependencyProvider;
                    if (_dependencyProvider != null) _dependencyProvider.RegisterNotification(_connString, _queryString, _cmdParams, _timeout, this);
                }
                catch (Exception ex)
                {
                    NCacheLog.Error("NosDBDependencyListener", ex.ToString());
                    throw ex;
                }
            }

            public new void OnDependencyChanged(bool changed, bool restart, bool error, bool invalid)
            {
                base.OnDependencyChanged(changed, restart, error, invalid);
            }

            public override void Stop()
            {
                try
                {
                    base.Stop();
                    if (_dependencyProvider != null) _dependencyProvider.Dispose();
                }
                catch (Exception exc)
                {
                    NCacheLog.Error("NotificationBasedDependencyManager.NosDBDependencyListener", exc.ToString());
                }
            }
            /// <summary>
            /// returns a unique hashcode as this instance needs to 
            /// be placed in a hashtable.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                if (_cacheKey != null) return _cacheKey.GetHashCode();

                return 0;
            }

            /// <summary>
            /// compares two instances of this class for equality.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                NosDBDependencyListener listener = obj as NosDBDependencyListener;
                if (listener != null)
                {
                    bool key = _cacheKey.Equals(listener._cacheKey);
                    bool conn = _connString.Equals(listener._connString);
                    bool query = _queryString.Equals(listener._queryString);
                    if (key && conn && query)
                    {
                        return true;
                    }

                }
                return false;
            }

        }
    }
}
