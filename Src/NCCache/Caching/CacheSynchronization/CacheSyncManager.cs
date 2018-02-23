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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Runtime.Events;
using System.Threading;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.CacheSynchronization
{
    internal class CacheSyncManager : IDisposable
    {

        private HashVector _synCaches = HashVector.Synchronized(new HashVector(StringComparer.CurrentCultureIgnoreCase));
        private HashVector _dependenciesStatus = HashVector.Synchronized(new HashVector());
        private HashVector _dependenciesKeyMap = HashVector.Synchronized(new HashVector());
        private HashVector _listeners = HashVector.Synchronized(new HashVector(StringComparer.CurrentCultureIgnoreCase));

        private HashVector<string, string> _keysToBeTouched = new HashVector<string, string>();
        private object _touchLock = new object();

        IDictionary<string, Hashtable> _depDic = new Dictionary<string, Hashtable>();
        object _lock = new object();

        private Cache _cache;
       
        
        private object _pollLockObj = new object();

        private DateTime _lastPoll = DateTime.Now;
        
        private CallbackType _callbackType = CallbackType.PushBasedNotification;
        private bool _notificationSet = false;

        /// <summary>
        /// Client cache side polling thread
        /// </summary>
        private int _pollingInterval = 10;
        private Thread _pollingThread;

        /// <summary>
        /// Touch thread interval
        /// Read from service config in case of outproc
        /// </summary>
        private int _touchInterval = ServiceConfiguration.ItemTouchInterval * 1000;
        private Thread _touchThread;

        private bool _poll = false;
    
        ILogger NCacheLog
        {
            get { return _cache.NCacheLog; }
        }

        private CacheRuntimeContext _context;
        /// <summary>
        /// Contains all the inactive dependencies. These dependencies are activated when this node 
        /// becoms coordinator or sub-coordinator(incase of POR).
        /// </summary>
        private HashVector _inactiveDependencies = HashVector.Synchronized(new HashVector());

        public IDictionary InactiveDependencies
        {
            get { return _inactiveDependencies; }
        }

      
        public CacheSyncManager(Cache cache, CacheRuntimeContext context)
        {
            _cache = cache;
            _context = context;

            if (_cache.Configuration != null && _cache.Configuration.SynchronizationStrategy != null)
            {
                this._callbackType = _cache.Configuration.SynchronizationStrategy.CallbackType;
                this._pollingInterval = _cache.Configuration.SynchronizationStrategy.Interval;
            }
          
          
            if (_context.InProc)
            {
                try
                {
                    if (System.Configuration.ConfigurationManager.AppSettings["NCacheServer.TouchInterval"] != null)
                    {
                        _touchInterval = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings["NCacheServer.TouchInterval"]);
                        _touchInterval = _touchInterval * 1000;
                    }
                }
                catch {
                    _touchInterval = 5 * 1000;
                }

                if (_touchInterval < 1)
                    _touchInterval = 5 * 1000;
            }
            else
            {
                _touchInterval = ServiceConfiguration.ItemTouchInterval * 1000;
            }
           
            _touchThread = new Thread(TouchThread);
            _touchThread.IsBackground = true;
            _touchThread.Name = "ItemTouchThread";
            _touchThread.Start();
        }

        public void AddDependency(object[] keys, CacheEntry[] entries)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (entries[i].SyncDependency != null)
                    AddDependency(keys[i], entries[i].SyncDependency);
            }
        }

        public void AddBulkDependencies(ArrayList keys, IList<CacheSyncDependency> dependencies)
        {
            Hashtable dependencyList = new Hashtable();
            ISyncCache syncCache = null;
            SyncEventListener listener = null; 

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i] as string;
                CacheSyncDependency dependency = dependencies[i];

                if (_context.IsDbSyncCoordinator)
                {
                    syncCache = dependency.SyncCache;
                    listener = null;
                    if (dependency != null && dependency.CacheId != null)
                    {
                        lock (_dependenciesStatus.SyncRoot)
                        {
                            if (AddDependencyInternal(key, dependency))
                            {
                                List<string> dependencyKeyList = dependencyList[dependency.CacheId] as List<string>;

                                if (dependencyKeyList == null)
                                {
                                    dependencyKeyList = new List<string>(keys.Count);
                                    dependencyList.Add(dependency.CacheId, dependencyKeyList);
                                }
                                dependencyKeyList.Add(dependency.Key);
                            }
                        }
                    }
                }
                else
                {
                    SyncItem syncItem = new SyncItem(key, dependency.Key, dependency.CacheId);
                    lock(_inactiveDependencies.SyncRoot)
                    {
                        if (!_inactiveDependencies.Contains(syncItem))
                            _inactiveDependencies.Add(syncItem, dependency);
                    }
                }
            }

            IDictionaryEnumerator ide = dependencyList.GetEnumerator();
            
            while(ide.MoveNext())
            {
                listener = _listeners[ide.Key] as SyncEventListener;
                syncCache = _synCaches[ide.Key] as ISyncCache;
                List<string> depdencyKeyList = ide.Value as List<string>;

                if (syncCache != null && listener != null)
                {
                    syncCache.RegisterBulkSyncKeyNotifications(depdencyKeyList.ToArray(), listener, CallbackType.PushBasedNotification);
                }
            }
        }

        private bool AddDependencyInternal(string key, CacheSyncDependency dependency)
        {
            SyncEventListener listener = null;
            ISyncCache syncCache = dependency.SyncCache;

            if (dependency != null && dependency.CacheId != null)
            {
                lock (_dependenciesStatus.SyncRoot)
                {
                    SyncItem syncItem = new SyncItem(key, dependency.Key, dependency.CacheId);

                    ClusteredArrayList dependentKeys = null;

                    if (!_dependenciesKeyMap.Contains(syncItem))
                    { 
                        dependentKeys = new ClusteredArrayList();
                        dependentKeys.Add(syncItem.ThisKey);
                        _dependenciesKeyMap.Add(syncItem, dependentKeys);
                    }
                    else
                    {
                        dependentKeys = _dependenciesKeyMap[syncItem] as ClusteredArrayList;
                        if (!dependentKeys.Contains(syncItem.ThisKey))
                            dependentKeys.Add(syncItem.ThisKey);
                    }

                    if (!_dependenciesStatus.Contains(syncItem))
                        _dependenciesStatus.Add(syncItem, DependencyStatus.Unchanged);

                    if (_synCaches.Contains(dependency.CacheId))
                    {
                        syncCache = _synCaches[dependency.CacheId] as ISyncCache;

                        listener = _listeners[dependency.CacheId] as SyncEventListener;

                    }
                    else
                    {
                    
                        _synCaches.Add(dependency.CacheId, syncCache);
                        if (_listeners.Contains(dependency.CacheId))
                            listener = _listeners[dependency.CacheId] as SyncEventListener;
                        else
                        {
                            listener = new SyncEventListener(dependency.CacheId, this);
                            _listeners.Add(dependency.CacheId, listener);
                        }

                    }
                    ///This registering for every key needs reviewing
                    if (dependentKeys != null && dependentKeys.Count < 2)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Clear()
        {
            try
            {
                if (_synCaches != null)
                {
                    object[] caches = new object[_synCaches.Values.Count];
                    lock (_dependenciesStatus.SyncRoot)
                    {
                        _synCaches.Values.CopyTo(caches, 0);
                    }

                    if (caches != null)
                    {
                        foreach (ISyncCache syncCache in caches)
                        {
                            RemoveDependentItems(syncCache.CacheId, true, false);
                        }
                    }
                }

                lock (_dependenciesStatus.SyncRoot)
                {
                    _dependenciesStatus.Clear();
                }

                lock (_dependenciesKeyMap.SyncRoot)
                {
                    _dependenciesKeyMap.Clear();
                }
            }
            catch (Exception e)
            {
                _cache.NCacheLog.Error("CacheSyncManager:", e.ToString());
            }
        }

        public void RemoveDependency(object[] keys, CacheEntry[] entries, IDictionary failedKeys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (entries[i].SyncDependency != null)
                {
                    if (failedKeys.Contains(keys[i]))
                    {
                        if (failedKeys[keys[i]] is Alachisoft.NCache.Caching.Topologies.CacheAddResult)
                        {
                            if (!(((Alachisoft.NCache.Caching.Topologies.CacheAddResult)failedKeys[keys[i]]) == Alachisoft.NCache.Caching.Topologies.CacheAddResult.Success))
                                RemoveDependency(keys[i], entries[i].SyncDependency);
                        }
                        else if (failedKeys[keys[i]] is Alachisoft.NCache.Caching.Topologies.CacheInsResult)
                        {
                            if (!((((Alachisoft.NCache.Caching.Topologies.CacheInsResult)failedKeys[keys[i]]) == Alachisoft.NCache.Caching.Topologies.CacheInsResult.Success) ||
                                  (((Alachisoft.NCache.Caching.Topologies.CacheInsResult)failedKeys[keys[i]]) == Alachisoft.NCache.Caching.Topologies.CacheInsResult.SuccessOverwrite)))
                                RemoveDependency(keys[i], entries[i].SyncDependency);
                        }
                    }
                }
            }
        }

        internal ISyncCache GetSyncCacheInstance(CacheSyncDependency dependency)
        {
            if (_synCaches.Contains(dependency.CacheId))
            {
                return _synCaches[dependency.CacheId] as ISyncCache;
            }
            return dependency.SyncCache;
        }

        public void AddDependency(object key, CacheSyncDependency dependency)
        {
            if (_context.IsDbSyncCoordinator)
            {
                ISyncCache syncCache = dependency.SyncCache;
                SyncEventListener listener = null;
                if (dependency != null && dependency.CacheId != null)
                {
                    lock (_dependenciesStatus.SyncRoot)
                    {
                        SyncItem syncItem = new SyncItem(key, dependency.Key, dependency.CacheId);
                        ClusteredArrayList dependentKeys = null;

                        if (!_dependenciesKeyMap.Contains(syncItem))
                        {
                            dependentKeys = new ClusteredArrayList();
                            dependentKeys.Add(syncItem.ThisKey);
                            _dependenciesKeyMap.Add(syncItem, dependentKeys);
                        }
                        else
                        {
                            dependentKeys = _dependenciesKeyMap[syncItem] as ClusteredArrayList;
                            if (!dependentKeys.Contains(syncItem.ThisKey))
                                dependentKeys.Add(syncItem.ThisKey);
                        }

                        if (!_dependenciesStatus.Contains(syncItem))
                            _dependenciesStatus.Add(syncItem, DependencyStatus.Unchanged);

                        if (_synCaches.Contains(dependency.CacheId))
                        {
                            syncCache = _synCaches[dependency.CacheId] as ISyncCache;
                          
                            listener = _listeners[dependency.CacheId] as SyncEventListener;


                        }
                        else
                        {
                            //// Only the cordinator or sub-coordinator(inacse of POR) can initialize
                            //// SyncCache.
                          syncCache.Initialize();
                            
                           
                            _synCaches.Add(dependency.CacheId, syncCache);
                            if (_listeners.Contains(dependency.CacheId))
                                listener = _listeners[dependency.CacheId] as SyncEventListener;
                            else
                            {
                                listener = new SyncEventListener(dependency.CacheId, this);
                                _listeners.Add(dependency.CacheId, listener);
                            }

                        }

                        if (dependentKeys != null && dependentKeys.Count < 2)
                        {
                            syncCache.RegisterSyncKeyNotifications((string)syncItem.Key, listener,  CallbackType.PushBasedNotification);

                            
                        }
                    }
                }
            }
            else

            {
                SyncItem syncItem = new SyncItem(key, dependency.Key, dependency.CacheId);
                lock (_inactiveDependencies.SyncRoot)
                {
                    if (!_inactiveDependencies.Contains(syncItem))
                        _inactiveDependencies.Add(syncItem, dependency); 

                }
            }
        }

        public void RemoveDependency(object key,CacheSyncDependency dependency)
        {
            if (_context.IsDbSyncCoordinator)
            {
                if (dependency == null)
                    return;
                try
                {
                    SyncItem item = new SyncItem(key, dependency.Key, dependency.CacheId);

                    lock (_dependenciesStatus.SyncRoot)
                    {
                        if (_dependenciesKeyMap.Contains(item))
                        {
                            ClusteredArrayList dependentKeys = _dependenciesKeyMap[item] as ClusteredArrayList;
                            if (dependentKeys != null)
                            {
                                dependentKeys.Remove(key);
                                if (dependentKeys.Count > 0) return;
                            }
                            _dependenciesKeyMap.Remove(item);
                        }

                        if (_dependenciesStatus.Contains(item))
                            _dependenciesStatus.Remove(item);
                        else
                            return;
                    }
                    ISyncCache syncCache = _synCaches[item.CacheId] as ISyncCache;
                    ISyncCacheEventsListener listener = _listeners[item.CacheId] as ISyncCacheEventsListener;
                    if (syncCache != null && listener != null)
                    {
                        syncCache.UnRegisterSyncKeyNotifications((string)item.Key, listener);
                    }
                }
                catch (Exception e)
                {
                    NCacheLog.Error("CacheSyncManager:", e.ToString());
                }
            }
            else
            {
                lock (_inactiveDependencies.SyncRoot)
                {
                    _inactiveDependencies.Remove(key); 
                }
            }
        }

        public void RemoveBulkDependencies(ArrayList keys, IList<CacheSyncDependency> dependencies)
        {
            string key = null;
            CacheSyncDependency dependency = null;
            Hashtable dependencyList = new Hashtable();
            ISyncCache syncCache = null;

            for (int i = 0; i < keys.Count; i++)
            {
                key = keys[i] as string;
                dependency = dependencies[i];

                if (_context.IsDbSyncCoordinator)
                {
                    if (dependency == null)
                        return;
                    try
                    {
                        if (RemoveCacheSyncDependencyInternal(key, dependency))
                        {
                            List<string> dependencyKeyList = dependencyList[dependency.CacheId] as List<string>;
                            syncCache = _synCaches[dependency.CacheId] as ISyncCache;
                            if (dependencyKeyList == null)
                            {
                                dependencyKeyList = new List<string>(keys.Count);
                                dependencyList.Add(dependency.CacheId, dependencyKeyList);
                            }
                            dependencyKeyList.Add(dependency.Key);
                        }
                    }
                    catch (Exception e)
                    {
                        NCacheLog.Error("CacheSyncManager.RemoveBulkDependencies", e.ToString());
                    }
                }
                else
                {
                    lock (_inactiveDependencies.SyncRoot)
                    {
                        _inactiveDependencies.Remove(key); 
                    }
                }
            }

            if (dependencyList.Count > 0)
            {
                IDictionaryEnumerator ide = dependencyList.GetEnumerator();

                while (ide.MoveNext())
                {
                    try
                    {
                        SyncEventListener listener = _listeners[ide.Key] as SyncEventListener;
                        syncCache = _synCaches[ide.Key] as ISyncCache;
                        List<string> depdencyKeyList = ide.Value as List<string>;

                        if (syncCache != null && listener != null)
                        {
                            syncCache.UnRegisterBulkSyncKeyNotifications(depdencyKeyList.ToArray(), listener);
                        }
                    }
                    catch (Exception e)
                    {
                        NCacheLog.Error("CacheSyncManager.RemoveBulkDependencies", e.ToString());
                    }
                }
            }
        }
        
        private bool RemoveCacheSyncDependencyInternal(string key, CacheSyncDependency dependency)
        {
            SyncItem item = new SyncItem(key, dependency.Key, dependency.CacheId);
            lock (_dependenciesStatus.SyncRoot)
            {
                if (_dependenciesKeyMap.Contains(item))
                {
                    ClusteredArrayList dependentKeys = _dependenciesKeyMap[item] as ClusteredArrayList;
                    if (dependentKeys != null)
                    {
                        dependentKeys.Remove(key);
                        if (dependentKeys.Count > 0) return false;
                    }
                    _dependenciesKeyMap.Remove(item);
                }

                if (_dependenciesStatus.Contains(item))
                    _dependenciesStatus.Remove(item);
                else
                    return false;
            }

            return true;
        }
        private object GetItemFromSyncCache(SyncItem item, ref ulong version, ref BitSet flag,ref DateTime absoluteExpiration,ref TimeSpan slidingExpiration, ref long size, ref string group, ref string subGroup, ref Hashtable queryInfo)
        {
            if (item != null)
            {
                ISyncCache cache = _synCaches[item.CacheId] as ISyncCache;
                if (cache != null)
                {
                    try
                    {
                        absoluteExpiration = DateTime.MaxValue.ToUniversalTime();
                        slidingExpiration = TimeSpan.Zero;

                        return cache.Get((string)item.Key, ref version, ref flag,ref absoluteExpiration,ref slidingExpiration, ref size, ref group, ref subGroup, ref queryInfo);
                    }
                    catch (Exception e)
                    {
                        NCacheLog.Error("CacheSyncManager:", e.ToString());
                    }
                }
            }
            return null;
        }
       
        public DependencyStatus GetDependencyStatus(SyncItem syncItem)
        {
            DependencyStatus status = DependencyStatus.Expired;
            if (_dependenciesStatus.Contains(syncItem))
                status = (DependencyStatus)_dependenciesStatus[syncItem];

            return status;
        }

        public void Synchronize(string[] keys, string cacheId, DependencyStatus status)
        { }

        public void Synchronize(SyncItem syncItem, DependencyStatus status)
        {
            long size = 0;
            if (SetItemStatus(syncItem, status))
            {
                switch (status)
                {
                    case DependencyStatus.Expired:
                        RemoveSyncItem(syncItem,true);
                        break;

                    case DependencyStatus.HasChanged:
                        if (_cache != null)
                        {
                            ulong version = 0;
                            BitSet flag = new BitSet();
                            //Flag information should be add while synchronizing the entry.
                            DateTime absoluteExpiration = DateTime.MaxValue.ToUniversalTime();
                            TimeSpan slidingExpiration = TimeSpan.Zero;

                            // cache item could be fetched from client cache now.
                            string group = null;
                            string subgroup = null;
                            Hashtable queryInfo = null;

                            object updateItem = GetItemFromSyncCache(syncItem, ref version, ref flag,ref absoluteExpiration,ref slidingExpiration, ref size, ref group, ref subgroup, ref queryInfo);
                            if (updateItem != null)
                            {
                                ClusteredArrayList dependentKeys = _dependenciesKeyMap[syncItem] as ClusteredArrayList;
                                try
                                {
                                    ExpirationHint expiration = null;

                                    if (!DateTime.MaxValue.ToUniversalTime().Equals(absoluteExpiration))
                                    {
                                        expiration= ConvHelper.MakeExpirationHint(_context.CacheRoot.Configuration.ExpirationPolicy, absoluteExpiration.Ticks, true);
                                    }
                                    else if (!TimeSpan.Zero.Equals(slidingExpiration))
                                    {
                                        expiration = ConvHelper.MakeExpirationHint(_context.CacheRoot.Configuration.ExpirationPolicy, slidingExpiration.Ticks, false);
                                    }

                                    if (dependentKeys != null)
                                    {
                                        foreach (object key in dependentKeys)
                                        {
                                            //TODO :Need to add expiration with itme
                                            OperationContext context = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                                            context.Add(OperationContextFieldName.ValueDataSize, size);

                                            BitSet flagMap = new BitSet();
                                            updateItem = _context.CachingSubSystemDataService.GetClientData(updateItem, ref flagMap, Common.Util.LanguageContext.DOTNET);

                                         

                                            _cache.Insert(key, updateItem, expiration, null, null,
                                                null, null, null, flag, null, version, 
                                                LockAccessType.PRESERVE_VERSION, null, null, context);
                                        }
                                    }

                                    SetItemStatus(syncItem, DependencyStatus.Unchanged);
                                }
                                catch (Exception exception)
                                {
                                    RemoveSyncItem(syncItem, true);
                                }
                            }
                            
                        }
                        break;
                }
            }
        }

        public void RemoveDependentItems(string cacheId,bool unregisterCallbacks, bool removeFromCache)
        {
            object[] keys = null;
            lock (_dependenciesStatus.SyncRoot)
            {
                if (removeFromCache && _dependenciesStatus.Count > 0)
                {
                    keys = new object[_dependenciesStatus.Keys.Count];
                    _dependenciesStatus.Keys.CopyTo(keys, 0);                    
                }
            }

            if (keys != null)
            {
                foreach (SyncItem item in keys)
                {
                    if (item.CacheId == cacheId.ToLower())
                    {
                        RemoveSyncItem(item, unregisterCallbacks);
                    }
                }
            }
        }

        public void ClearCache()
        {
            object[] keys = null;
            lock (_dependenciesStatus.SyncRoot)
            {
                if (_dependenciesStatus.Count > 0)
                {
                    keys = new object[_dependenciesStatus.Keys.Count];
                    _dependenciesStatus.Keys.CopyTo(keys, 0);

                    foreach (SyncItem item in keys)
                    {
                        _dependenciesStatus.Remove(item);
                        _dependenciesKeyMap.Remove(item);
                    }
                }

            }
            _cache.Clear();
        }

        private void RemoveSyncItem(SyncItem item, bool unregisterCallbacks)
        {
            if (_cache != null)
            {
                try
                {
                    ClusteredArrayList dependentKeys = null;
                    lock (_dependenciesStatus.SyncRoot)
                    {
                        _dependenciesStatus.Remove(item);
                        if(_dependenciesKeyMap.Contains(item))
                        {
                            dependentKeys = _dependenciesKeyMap[item] as ClusteredArrayList;
                        }
                        _dependenciesKeyMap.Remove(item);
                    }
                    ISyncCache syncCache = _synCaches[item.CacheId] as ISyncCache;
                    ISyncCacheEventsListener listener = _listeners[item.CacheId] as ISyncCacheEventsListener;
                    if (syncCache != null && listener != null && unregisterCallbacks)
                    {
                        syncCache.UnRegisterSyncKeyNotifications((string)item.Key, listener);
                    }
                    if (dependentKeys != null)
                    {
                        foreach (object key in dependentKeys)
                        {

                            OperationContext context = new OperationContext(OperationContextFieldName.OperationType,OperationContextOperationType.CacheOperation);

                            
                            _cache.Remove(key, context);
                        }

                    }
                }
                catch (Exception e)
                {
                    NCacheLog.Error("CacheSyncManager:", e.ToString());
                }
            }
        }
    
        private bool SetItemStatus(SyncItem syncItem, DependencyStatus status)
        {
            bool statusSet = false;
            lock (_dependenciesStatus.SyncRoot)
            {
                if (_dependenciesStatus.ContainsKey(syncItem))
                {
                    _dependenciesStatus[syncItem] = status;
                    statusSet = true;
                }
            }
            return statusSet;
        }

        public void Dispose()
        {
            if (_synCaches != null)
            {
                object[] caches = new object[_synCaches.Values.Count];
                _synCaches.Values.CopyTo(caches, 0);

                if (caches != null)
                {
                    foreach (ISyncCache syncCache in caches)
                    {
                        try
                        {
                            syncCache.Dispose();
                        }
                        catch (Exception)
                        { }
                    }
                }
                _synCaches.Clear();

            }
            if (_depDic != null) _depDic.Clear();
			if (_dependenciesKeyMap != null) _dependenciesKeyMap.Clear();
			if (_dependenciesStatus != null) _dependenciesStatus.Clear();
			if (_listeners != null) _listeners.Clear();
            if (_keysToBeTouched != null) _keysToBeTouched.Clear();
            
            try
            {
                if (_pollingThread != null)
                {
#if !NETCORE
                    _pollingThread.Abort();
#else
                    _pollingThread.Interrupt();
#endif

                    _pollingThread = null;
                }
            }
            catch (Exception) { }
            try
            {
                if (_touchThread != null && _touchThread.IsAlive)
                {
#if !NETCORE
                    _touchThread.Abort();
#else
                    _touchThread.Interrupt();
#endif
                    _touchThread = null;
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Initializes all the inactive dependencies.
        /// </summary>
        public void ActivateDependencies()
        {
            try
            {
                
                IDictionaryEnumerator iDic = _inactiveDependencies.GetEnumerator();
                while (iDic.MoveNext())
                {
                    SyncItem item = (SyncItem)iDic.Key;
                    CacheSyncDependency dependency = (CacheSyncDependency)iDic.Value;

                    AddDependency(item.ThisKey, dependency);
                }
                lock (_inactiveDependencies.SyncRoot)
                {
                    _inactiveDependencies.Clear();
                }
            }
            catch (Exception e)
            {
                NCacheLog.Error("CacheSyncManager.ActivateDependencies()", e.ToString());
            }
        }

        #region ----------------- Polling and Touch ------------------ 

        private void TouchThread()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        Thread.Sleep(_touchInterval);
                        Touch();
                    }
                    catch (ThreadAbortException)
                    {
                        break;
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _context.NCacheLog.Error("CacheSyncManager.TouchThread", "Exception at Touch task: " + ex);
                    }
                }
            }
            catch (ThreadAbortException)
            { }
            catch (ThreadInterruptedException)
            { }
        }
      
        private void Touch()
        {
            if (_synCaches != null && _synCaches.Count > 0)
            {
                foreach (ISyncCache cache in _synCaches.Values)
                {
                    if (cache != null)
                    {
                        try
                        {
                            if (_keysToBeTouched.Count > 0)
                            {
                                List<string> touchableKeys = null;
                                lock (_touchLock)
                                {
                                    touchableKeys = new List<string>(_keysToBeTouched.Keys);
                                    _keysToBeTouched.Clear();
                                }


                            }
                        }
                        catch (Exception ex)
                        {
                            _context.NCacheLog.Error("CacheSyncMaanger.Touch", "Exception: " + ex);
                        }
                    }
                }
            }
        }
      
        public void AddToTouch(string key)
        {
            lock (_touchLock)
            {
                _keysToBeTouched[key] = null;
            }
        }



        private void UpdateSyncData(string cacheId, PollingResult result)
        {
            SyncEventListener listener = _listeners[cacheId] as SyncEventListener;
            if (listener != null)
            {
                foreach (string key in result.UpdatedKeys)
                {
                    if (key.Equals("$$$CacheClear$$C"))
                        listener.CacheCleared();

                    listener.SyncItemUpdated(key);
                }
                foreach (string key in result.RemovedKeys)
                {
                    listener.SyncItemRemoved(key);
                }
            }
        }
        #endregion

        #region /                  ---- Inner Classes ----                     /

        public class SyncItem
        {
            private object _key;
            private object _thisKey;

            private string _cacheId;
 

            public SyncItem(object key, string cacheid)
            {
                _key = key;
                _cacheId = cacheid;
            }
            public SyncItem(object thisKey,object key, string cacheid)
            {
                _key = key;
                _cacheId = cacheid;

                _thisKey = thisKey;
               

            }

            public object Key
            {
                get { return _key; }
                set { _key = value; }
            }

            public object ThisKey
            {
                get { return _thisKey; }
                set { _thisKey = value; }
            }
            public string CacheId
            {
                get { return _cacheId; }
                set 
                {
                    if (value != null)
                        _cacheId = value.ToLower();
                    else
                        _cacheId = value;
                }

            }
           

            public override bool Equals(object obj)
            {
                if (obj is SyncItem)
                {
                    SyncItem other = obj as SyncItem;
                    if (other._cacheId == _cacheId)
                    {
                        string key = other._key as string;
                        string key2 = _key as string; 
                        if (key == key2)
                            return true;
                    }
                }
                return false;
            }

            public override int GetHashCode()
            {

                if (_cacheId != null && _key != null)
                    return ((string)(_cacheId + _key)).GetHashCode();

                return base.GetHashCode();
            }
        }

        class SyncEventListener : ISyncCacheEventsListener
        {
            private string _cacheid;
            CacheSyncManager _synchronizer;
  
            public SyncEventListener(string cacheId, CacheSyncManager synchronizer)
            {
                _cacheid = cacheId;
                _synchronizer = synchronizer;
            }

            public override bool Equals(object obj)
            {
                if (obj is CacheSyncManager.SyncEventListener)
                {
                    if (_cacheid == ((CacheSyncManager.SyncEventListener)obj)._cacheid)
                        return true;
                }
                return false;
            }
        
            #region ISyncCacheEventsListener Members

            public void SyncItemUpdated(string key)
            {
                if (_synchronizer != null)
                    _synchronizer.Synchronize(new SyncItem(key, _cacheid), DependencyStatus.HasChanged);
            }

            public void SyncItemRemoved(string key)
            {
                if (_synchronizer != null)
                    _synchronizer.Synchronize(new SyncItem(key, _cacheid), DependencyStatus.Expired);
            }

            public void CacheStopped(string cacheId)
            {
                if (_synchronizer != null)
                    _synchronizer.RemoveDependentItems(_cacheid, false, true);
            }

            public void CacheCleared()
            {
                if (_synchronizer != null)
                    _synchronizer.ClearCache();
            }

            public void SyncItemUpdated(string[] keys)
            {
                throw new NotImplementedException();
            }

            public void SyncItemRemoved(string[] keys)
            {
                throw new NotImplementedException();
            }


            #endregion
        }


        #endregion


  


    }
}