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
using System.Collections.Generic;
using System.Reflection;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Runtime.DatasourceProviders;
using Alachisoft.NCache.Runtime.Caching;

using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Caching.DataGrouping;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.CacheLoader;

using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.DatasourceProviders
{
	/// <summary>
	/// Manager class for read-trhough and write-through operations
	/// </summary>

	internal class DatasourceMgr: IDisposable
	{
         
		/// <summary> The runtime context associated with the current cache. </summary>
		private CacheRuntimeContext		_context;
        public CacheRuntimeContext Context {
            get
            {
                return _context;

            }
        }

        private CacheBase _cacheImpl;
        private string _cacheName;
        

        private bool _clientCacheImpl;

        /// <summary> The external datasource writer </summary>
        private Dictionary<string, ReadThruProviderMgr> _readerProivder = new Dictionary<string, ReadThruProviderMgr>();

		/// <summary> The external datasource writer </summary>
        private Dictionary<string, WriteThruProviderMgr> _writerProivder = new Dictionary<string, WriteThruProviderMgr>();

        /// <summary> keep language context against each provider </summary>
        private Dictionary<string, LanguageContext> _updateOpProviderMgr = new Dictionary<string, LanguageContext>();

		/// <summary> Asynchronous event processor. </summary>
		[CLSCompliant(false)]
		public AsyncProcessor			_asyncProc;

        /// <summary>Asynchronous write behind task processor</summary>
        [CLSCompliant(false)]
        public WriteBehindAsyncProcessor _writeBehindAsyncProcess;

        
        public DSAsyncUpdatesProcessor _dsUpdateProcessor;
		/// <summary> The external datasource reader </summary>
		private Hashtable				_queue;
        public Hashtable Queue
        {
            get
            {
                return _queue;
            }
        }
        private string _defaultReadThruProvider;

        private string _defaultWriteThruProvider;

        bool anyWriteBehindEnabled = false;

        bool anyWriteThruEnabled = false;

        private Type _type = typeof(Runtime.Serialization.ICompactSerializable);


        /////////// Assembly Cache
        private static Dictionary<string, Assembly> _assemblyCache = new Dictionary<string, Assembly>();

        public static Dictionary<string, Assembly> AssemblyCache
        {
            get { return DatasourceMgr._assemblyCache; }
            set { DatasourceMgr._assemblyCache = value; }
        }
        ///////////

        /// <summary>
		/// Initializes the object based on the properties specified in configuration
		/// </summary>
		/// <param name="properties">properties collection for this cache.</param>
        public DatasourceMgr(string cacheName, IDictionary properties, CacheRuntimeContext context, long timeout)
		{
            _cacheName = cacheName;
			_context = context;
            _queue = new Hashtable();
			Initialize(properties, timeout);
		}

      
        public string CacheName
        {
            get { return _cacheName; }
        }

        internal CacheBase CacheImpl
        {   get
            { 
            return _context.CacheImpl; 
        }
            set
            {
                if (_writeBehindAsyncProcess != null)
                    _writeBehindAsyncProcess.CacheImpl = _context.CacheImpl;
                _cacheImpl = value;
            }
        }
		/// <summary>
		/// Check if ReadThru is enabled
		/// </summary>
		public bool IsReadThruEnabled
		{
            get { return (_readerProivder.Count > 0); }
		}

		/// <summary>
		/// Check if WriteThru is enabled
		/// </summary>
		public bool IsWriteThruEnabled
		{
            get { return ( anyWriteThruEnabled ); }
        }

        public string DefaultReadThruProvider
        {
            get { 
                if(_defaultReadThruProvider != null) return _defaultReadThruProvider.ToLower();
                return null;
            }
            set { _defaultReadThruProvider = value; }
        }

        internal WriteThruProviderMgr GetProvider(string providerName)
        {
            WriteThruProviderMgr writeThruManager = null;
            if (String.IsNullOrEmpty(providerName))
                providerName = DefaultWriteThruProvider;
            if (_writerProivder != null && _writerProivder.ContainsKey(providerName.ToLower()))
            {
                _writerProivder.TryGetValue(providerName.ToLower(), out writeThruManager);
            }
            return writeThruManager;
        }

        public string DefaultWriteThruProvider
        {
            get 
            {
                if(_defaultWriteThruProvider != null)
                    return _defaultWriteThruProvider.ToLower();
                return null;
            }
            set { _defaultWriteThruProvider = value; }
        }

        /// <summary>
        /// Check if WriteBehind is enabled
        /// </summary>
        public bool IsWriteBehindEnabled
        {
            get { return ( anyWriteBehindEnabled ); }
        }

        public void WindUpTask()
        {
            _context.NCacheLog.CriticalInfo("DatasourceMgr", "WindUp Task Started.");

            _writeBehindAsyncProcess.WindUpTask();

            if (_dsUpdateProcessor != null)
                _dsUpdateProcessor.WindUpTask();
            _context.NCacheLog.CriticalInfo("DatasourceMgr", "WindUp Task Ended.");
        }

        public void WaitForShutDown(long interval)
        {
            _context.NCacheLog.CriticalInfo("DatasourceMgr", "Waiting for  Write Behind queue shutdown task completion.");

            DateTime startShutDown = DateTime.Now;

            if (_writeBehindAsyncProcess != null)
                _writeBehindAsyncProcess.WaitForShutDown(interval);

            _context.NCacheLog.CriticalInfo("DatasourceMgr", "Waiting for  Write Behind Update queue shutdown task completion.");

            if (_dsUpdateProcessor != null)
            {
                long startTime = (startShutDown.Ticks - 621355968000000000) / 10000;
                long timeout = Convert.ToInt32(interval * 1000) - (int)((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - startTime);
                timeout = timeout / 1000;
                if (timeout > 0)
                    _dsUpdateProcessor.WaitForShutDown(timeout);
            }
            _context.NCacheLog.CriticalInfo("DatasourceMgr", "Shutdown task completed.");
        }

		#region	/                 --- IDisposable ---           /

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or 
		/// resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{           
			if(_readerProivder != null)
			{
                IEnumerator enu = _readerProivder.Values.GetEnumerator();
                while (enu.MoveNext())
                {
                    if (enu.Current != null)
                    {

                        ((IDisposable)enu.Current).Dispose();
                    }
                }
                
                _readerProivder = null;
			}
            if (_writeBehindAsyncProcess != null)
            {
                _writeBehindAsyncProcess.Stop();
                _writeBehindAsyncProcess = null;
            }
			if(_writerProivder != null)
			{
                IEnumerator enu = _writerProivder.Values.GetEnumerator();
                while (enu.MoveNext())
                {
                    if (enu.Current != null)
                    {

                        ((IDisposable)enu.Current).Dispose();
                    }
                }
				_writerProivder = null;
			}
			if(_asyncProc != null)
			{
				_asyncProc.Stop();
				_asyncProc = null;
			}
            if (_dsUpdateProcessor != null)
            {
                _dsUpdateProcessor.Stop();
                _dsUpdateProcessor = null;
            }
		}

		#endregion

		#region	/                 --- Initialization ---           /

		/// <summary>
		/// Method that allows the object to initialize itself. Passes the property map down 
		/// the object hierarchy so that other objects may configure themselves as well..
		/// sample config string
		/// 
		/// backing-source
		/// (
		/// 	read-thru
		/// 	(
		/// 		 assembly='Diyatech.Sample';
		/// 		 class='mySync.DB.Reader'
		/// 	);
		/// 	write-thru
		/// 	(
		/// 		  assembly='Diyatech.Sample';
		/// 		  class='mySync.DB.Writer'
		/// 	)
		/// )
		/// </summary>
		/// <param name="properties">properties collection for this cache.</param>
		private void Initialize(IDictionary properties, long timeout)
		{
            string mode=string.Empty;
            int throttlingRate=0, requeueLimit=0, evictionRate=0;
            long batchInterval=0, operationDelay=0;
            if(properties == null)
				throw new ArgumentNullException("properties");
            try
			{
                if (properties.Contains("read-thru"))
                {
                    IDictionary readThruProps = (IDictionary)properties["read-thru"];
                    string enabled = (string)readThruProps["enabled"];
                    if (enabled.ToLower() == "true")
                    {
                        IDictionary providers = (IDictionary)readThruProps["read-thru-providers"];
                        if (providers != null)
                        {
                            IDictionaryEnumerator ide = providers.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                if (!_readerProivder.ContainsKey(ide.Key.ToString().ToLower()))
                                    _readerProivder.Add(ide.Key.ToString().ToLower(), new ReadThruProviderMgr(_cacheName, providers[ide.Key] as Hashtable, _context));
                            }
                        }
                    }
                }
                if (properties.Contains("write-thru"))
                {
                    IDictionary writeThruProps = (IDictionary)properties["write-thru"];
                    string enabled = (string)writeThruProps["enabled"];
                    if (enabled.ToLower() == "true")
                    {
                        anyWriteThruEnabled = true;//previous async mode config flag is removed now
                        if (writeThruProps.Contains("write-behind"))
                        {
                            anyWriteBehindEnabled = true;
                            IDictionary writeBehind = (IDictionary)writeThruProps["write-behind"];
                            mode = (string)writeBehind["mode"];
                            throttlingRate = Convert.ToInt32(writeBehind["throttling-rate-per-sec"]);
                            requeueLimit = Convert.ToInt32(writeBehind["failed-operations-queue-limit"]);
                            evictionRate = Convert.ToInt32(writeBehind["failed-operations-eviction-ratio"]);
                            if (mode.ToLower() == "batch")
                            {
                                IDictionary batchConfig = (IDictionary)writeBehind["batch-mode-config"];
                                batchInterval = Convert.ToInt64(batchConfig["batch-interval"]);
                                operationDelay = Convert.ToInt64(batchConfig["operation-delay"]);
                            }
                        }
                        IDictionary providers = (IDictionary)writeThruProps["write-thru-providers"];
                        if (providers != null)
                        {
                            IDictionaryEnumerator ide = providers.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                if (!_writerProivder.ContainsKey(ide.Key.ToString().ToLower()))
                                {
                                    _writerProivder.Add(ide.Key.ToString().ToLower(),
                                        new WriteThruProviderMgr(_cacheName, providers[ide.Key] as Hashtable, _context,
                                            operationDelay, ide.Key.ToString()));
                                }
                            }
                        }

                    }
                }
                
                if (_writerProivder != null && anyWriteThruEnabled)
                {
                    _dsUpdateProcessor = new DSAsyncUpdatesProcessor(this,_context.NCacheLog);
                }
                if (_writerProivder != null && anyWriteBehindEnabled)
                {
                    _writeBehindAsyncProcess = new WriteBehindAsyncProcessor(this, throttlingRate, mode, batchInterval, operationDelay, requeueLimit, evictionRate, timeout, _writerProivder, _context.CacheImpl, _context);
                }
                if (_readerProivder != null)
                {
                    _asyncProc = new AsyncProcessor(_context.NCacheLog);
                    _asyncProc.Start();
                }
			}
			catch(ConfigurationException)
			{
				throw;
			}
			catch(Exception e)
			{
				throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
			}		
		}

		#endregion


		private CacheResyncTask GetQueuedReadRequest(object key)
		{
			lock(_queue) { return (CacheResyncTask)_queue[key]; }
		}

        /// <summary>
        /// Start the async processor thread
        /// </summary>
        public void StartWriteBehindProcessor()
        {

            if (_writeBehindAsyncProcess != null) _writeBehindAsyncProcess.Start();

        }

		/// <summary>
		/// Responsible for updating/inserting an object to the data source. The Key and the 
		/// object are passed as parameter.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="e"></param>
		/// <returns></returns>
		public object ResyncCacheItemAsync(object key, ExpirationHint exh, EvictionHint evh, GroupInfo groupInfo, Hashtable queryInfo, string resyncProviderName)
		{
			lock(_queue)
			{
                if (_asyncProc != null && GetQueuedReadRequest(key) == null)
                {
					AsyncProcessor.IAsyncTask task = new CacheResyncTask(this, key as string, exh, evh,  groupInfo, queryInfo, resyncProviderName);
                    _queue[key] = task;
                    _asyncProc.Enqueue(task);
                }
				return null;
			}
		}

        public object GetCacheEntry(string key, ProviderCacheItem item, ref BitSet flag, string group, string subGroup, out CacheEntry cacheEntry, LanguageContext langContext)
        {
            object userObject = null;
            cacheEntry = null;

            switch (langContext)
            {
                case LanguageContext.DOTNET:
                    userObject = GetCacheEntryDotNet(key, item, ref flag, group, subGroup, out cacheEntry);
                break;

               

            }

            return userObject;
        }

     

        private object GetCacheEntryDotNet(string key, ProviderCacheItem item, ref BitSet flag, string group, string subGroup, out CacheEntry cacheEntry)
        {
            object userObject = null;
            cacheEntry = null;
            object val = null;

            if (item != null && item.Value != null)
            {
                if (item.Group == null && item.SubGroup != null)
                {
                    throw new OperationFailedException("Error occurred while synchronization with data source; group must be specified for sub group");
                }
               

                if (flag == null) flag = new BitSet();
                val = item.Value;
                //query and tag info...

                Hashtable queryInfo = new Hashtable();

                Alachisoft.NCache.Common.Util.TypeInfoMap typeMap = _context.CacheRoot.GetTypeInfoMap();
                queryInfo["query-info"] = Alachisoft.NCache.Caching.CacheLoader.CacheLoaderUtil.GetQueryInfo(item.Value, typeMap);
                if (item.Tags != null)
                {
                    Hashtable tagInfo = CacheLoaderUtil.GetTagInfo(item.Value, item.Tags);
                    if (tagInfo != null)
                    {
                        queryInfo.Add("tag-info", tagInfo);
                    }
                }

                if (item.NamedTags != null)
                {
                    try
                    {
                        Hashtable namedTagInfo = CacheLoaderUtil.GetNamedTagsInfo(item.Value, item.NamedTags, typeMap);
                        if (namedTagInfo != null)
                        {
                            queryInfo.Add("named-tag-info", namedTagInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new OperationFailedException("Error occurred while synchronization with data source; " + ex.Message);
                    }
                }
                //}
                ////verify group/subgroup and tags
                
                if (!item.Value.GetType().IsSerializable && !_type.IsAssignableFrom(item.Value.GetType())) throw new OperationFailedException("Read through provider returned an object that is not serializable.");

                userObject = _context.CacheReadThruDataService.GetClientData(val, ref flag, LanguageContext.DOTNET);

                EvictionHint evh = new PriorityEvictionHint(item.ItemPriority);
                ExpirationHint exh = Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(_context.CacheRoot.Configuration.ExpirationPolicy, item.Dependency, item.AbsoluteExpiration, item.SlidingExpiration);

                if (exh != null)
                {
                    exh.CacheKey = key;
                    if (item.ResyncItemOnExpiration)
                        exh.SetBit(ExpirationHint.NEEDS_RESYNC);
                }

                cacheEntry = new CacheEntry(userObject, exh, evh);

                if (item is InternalProviderCacheItem)
                {
                    InternalProviderCacheItem internalItem = item as InternalProviderCacheItem;
                    if (internalItem.InternalSize > 0)
                        cacheEntry.DataSize = internalItem.InternalSize;
                    cacheEntry.Version = internalItem.Version;
                }
                cacheEntry.Flag = flag;

                if (!String.IsNullOrEmpty(item.Group))
                    cacheEntry.GroupInfo = new GroupInfo(item.Group, item.SubGroup);

                cacheEntry.QueryInfo = queryInfo;
                cacheEntry.ResyncProviderName = item.ResyncProviderName == null ? null : item.ResyncProviderName.ToLower();
                if (cacheEntry.Version == 0)
                {
                    cacheEntry.Version = (UInt64)(DateTime.Now - new System.DateTime(2016, 1, 1, 0, 0, 0)).TotalMilliseconds;
                }
            }

            return userObject;
        }

		/// <summary>
		/// Responsible for updating/inserting an object to the data source. The Key and the 
		/// object are passed as parameter.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="e"></param>
		/// <param name="group"></param>
		/// <param name="subGroup"></param>
		/// <returns></returns>
        public object ResyncCacheItem(string key, out CacheEntry entry, ref BitSet flag, string group, string subGroup, string providerName, OperationContext operationContext)
        {
			ProviderCacheItem item = null;
            LanguageContext langContext;

            ReadThru(key, out item, providerName, out langContext, operationContext);
            object userObject = null;
          
            ulong version = 0;

            CacheSynchronization.CacheSyncDependency dep = null;
            InternalProviderCacheItem internalItem = item as InternalProviderCacheItem;
            if (internalItem != null)
            {
                dep = internalItem.SyncDependency;
                version = internalItem.Version;
            }

            try
            {
                userObject = GetCacheEntry(key, item, ref flag, group, subGroup, out entry, langContext);

                if (userObject == null || entry == null)
                    return userObject;

                CacheEntry clone = (CacheEntry)entry.Clone();


                LockAccessType lockAccessType = LockAccessType.IGNORE_LOCK;

               
                if(clone.Version <= 0)
                    clone.Version = version;

                CacheInsResultWithEntry result = _context.CacheImpl.Insert(key, clone, false, null, 
                                                    clone.Version, lockAccessType, operationContext);
                if (result != null && result.Result == CacheInsResult.IncompatibleGroup) throw new OperationFailedException("Data group of the inserted item does not match the existing item's data group");
                if (result.Result == CacheInsResult.Failure) throw new OperationFailedException("Operation failed to synchronize with data source");
                else if (result.Result == CacheInsResult.NeedsEviction) throw new OperationFailedException("The cache is full and not enough items could be evicted.");

                if (!string.IsNullOrEmpty(group) && !(CacheHelper.CheckDataGroupsCompatibility(new GroupInfo(item.Group, item.SubGroup), new GroupInfo(group, subGroup))))
                    return null;

            }
            catch (Exception ex)
            {
				throw new OperationFailedException("Error occurred while synchronization with data source. Error: " + ex.Message, ex);
            }
            
            return userObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="e"></param>
        /// <param name="flag"></param>
        /// <param name="group"></param>
        /// <param name="subGroup"></param>
        public void ResyncCacheItem(HashVector orginalTable, string[] keys, CacheEntry[] e, BitSet[] flag, string providerName, OperationContext operationContext)
        {
			Dictionary<string, ProviderCacheItem> cacheItems;
            LanguageContext langContext;

            cacheItems = ReadThru(keys, providerName, out langContext, operationContext);

			if (cacheItems == null || (cacheItems != null && cacheItems.Count == 0)) return;

			object[] refinedKeys = new object[cacheItems.Count];
			CacheEntry[] refinedEnteries = new CacheEntry[cacheItems.Count];

            int counter = 0;
            for (int i = 0; i < keys.Length; i++)
            {
				ProviderCacheItem cacheItem;   
				if (!cacheItems.TryGetValue(keys[i], out cacheItem) || cacheItem == null)
				{
					continue;
				}

                CacheSynchronization.CacheSyncDependency dep = null;
                InternalProviderCacheItem internalItem = cacheItem as InternalProviderCacheItem;
                if (internalItem != null)
                {
                    dep = internalItem.SyncDependency;
                }

                try
                {
                    CacheEntry entry;
                    object userBinaryObject = GetCacheEntry(keys[i], cacheItem,ref flag[i], null, null, out entry, langContext);

                    if (userBinaryObject == null)
                        continue;

                    refinedKeys[counter] = keys[i];
                    refinedEnteries[counter++] = entry;     
                }
                catch (Exception exception)
                {
                    _context.NCacheLog.Error("DatasourceMgr.ResyncCacheItem", "Error occurred while synchronization with data source; " + exception.Message);
                    continue;
                }          
                
            }

            if (counter == 0) return;

            Cache.Resize(ref refinedKeys, counter);
            Cache.Resize(ref refinedEnteries, counter);

            Hashtable insertedValues = null;

            try
            {
                
                insertedValues = _context.CacheImpl.Insert(refinedKeys, refinedEnteries, false, operationContext);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException("error while trying to synchronize the cache with data source. Error: " + ex.Message, ex);
            }

            for (int i = 0; i < refinedKeys.Length; i++)
            {
                if (insertedValues.ContainsKey(refinedKeys[i]))
                {
                    CacheInsResultWithEntry insResult = insertedValues[refinedKeys[i]] as CacheInsResultWithEntry;
                    if (insResult != null && (insResult.Result == CacheInsResult.Success || insResult.Result == CacheInsResult.SuccessOverwrite))
                    {
                        object value = refinedEnteries[i].Value;
                        if (value is CallbackEntry)
                        {
                            value = ((CallbackEntry)value).Value;
                        }
                        orginalTable.Add(refinedKeys[i], new CompressedValueEntry(value, refinedEnteries[i].Flag));
                    }
                }
                else
                {
                    object value = refinedEnteries[i].Value;
                    if (value is CallbackEntry)
                    {
                        value = ((CallbackEntry)value).Value;
                    }
                    orginalTable.Add(refinedKeys[i], new CompressedValueEntry(value, refinedEnteries[i].Flag));
                }
            }
        }       

        /// <summary>
        /// Responsible for loading the object from the external data source. 
        /// Key is passed as parameter.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public void ReadThru(string key, out ProviderCacheItem item, string providerName, out LanguageContext langContext, OperationContext operationContext)
        {
            item = null;
            langContext = LanguageContext.NONE;
            if (!IsReadThruEnabled) return;
            if (_readerProivder == null) 
                return;
            
            ReadThruProviderMgr readThruManager = null;

            if (String.IsNullOrEmpty(providerName))
                providerName = DefaultReadThruProvider;

            if (_readerProivder.ContainsKey(providerName.ToLower()))
            {
                _readerProivder.TryGetValue(providerName.ToLower(), out readThruManager);

                try
                {
                    langContext = readThruManager.ProviderType;

                    if (readThruManager != null)
                        readThruManager.ReadThru(key, out item, operationContext);
                }
                catch (Exception e)
                {
                    throw;
                }
            }
            else
            {
                throw new OperationFailedException("Specified backing source not available. Verify backing source settings.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="exh"></param>
        /// <param name="evh"></param>
        /// <returns></returns>
        public Dictionary<string, ProviderCacheItem> ReadThru(string[] keys,string providerName, out LanguageContext langContext, OperationContext operationContext)
        {
            langContext = LanguageContext.NONE;
            if (_readerProivder == null) 
                return null; 

            ReadThruProviderMgr readThruManager = null;

            if (String.IsNullOrEmpty(providerName))
                providerName = DefaultReadThruProvider;

            if (_readerProivder.ContainsKey(providerName.ToLower()))
            {
                _readerProivder.TryGetValue(providerName.ToLower(), out readThruManager);
                if (readThruManager != null)
                {
                    langContext = readThruManager.ProviderType;
                    return readThruManager.ReadThru(keys, operationContext);
                }
            }
            else
            {
                throw new OperationFailedException("Specified backing source not available. Verify backing source settings.");
            }
            return null;
        }

		/// <summary>
		/// Responsible for updating/inserting an object to the data source. The Key and the 
		/// object are passed as parameter.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="val"></param>
		/// <returns></returns>
        public void WriteBehind(CacheBase internalCache, object key, CacheEntry entry, string source, string taskId, string providerName, OpCode operationCode)
        {
            if (_writerProivder == null) return;
            WriteThruProviderMgr writeThruManager = GetProvider(providerName);
            if (writeThruManager != null)
                writeThruManager.WriteBehind(internalCache, key, entry, source, taskId, operationCode);
        }

        /// <summary>
        /// Responsible for updating/inserting an object to the data source. The Key and the 
        /// object are passed as parameter.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public void WriteBehind(CacheBase internalCache, object key, CacheEntry entry, string source, string taskId, string providerName, OpCode operationCode, WriteBehindAsyncProcessor.TaskState state)
        {
            if (_writerProivder == null) return;
            WriteThruProviderMgr writeThruManager = GetProvider(providerName);
            if (writeThruManager != null)
                writeThruManager.WriteBehind(internalCache, key, entry, source, taskId, operationCode, state);   
        }

        /// <summary>
        /// Responsible for updating/inserting an object to the data source. The Key and the 
        /// object are passed as parameter.
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public void WriteBehind(CacheBase internalCache, object[] keys,  CacheEntry[] entries, string source, string taskId, string providerName, OpCode operationCode)
        {
            if (_writerProivder == null) return;
            WriteThruProviderMgr writeThruManager = GetProvider(providerName);
            if (writeThruManager != null)
            {
                writeThruManager.WriteBehind(internalCache, keys,  entries, source, taskId, operationCode);
            }
                }


        /// <summary>
        /// Responsible for updating/inserting an object to the data source. The Key and the 
        /// object are passed as parameter.
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public void WriteBehind(CacheBase internalCache, object[] keys,  CacheEntry[] entries, string source, string taskId, string providerName, OpCode operationCode, WriteBehindAsyncProcessor.TaskState state)
        {
            if (_writerProivder == null) return;
            WriteThruProviderMgr writeThruManager = GetProvider(providerName);
            if (writeThruManager != null)
            {
 
                writeThruManager.WriteBehind(internalCache, keys, entries, source, taskId, operationCode, state);
            }
       
        }

        internal void WriteBehind(DSWriteBehindOperation operation)
        {
            if (_writerProivder == null) return;
            WriteThruProviderMgr writeThruManager = GetProvider(operation.ProviderName);
            if (writeThruManager != null)
                writeThruManager.WriteBehind(operation);
        }

        internal void WriteBehind(ArrayList operations)
        {
            if (operations==null || _writerProivder == null) return;
            DSWriteBehindOperation operation = operations[0] as DSWriteBehindOperation;
            WriteThruProviderMgr writeThruManager = (operation != null) ? GetProvider(operation.ProviderName) : null;//bulk write thru call have same provider
            if (writeThruManager != null)
                writeThruManager.WriteBehind(operations);
        }
        public void SetState(string taskId, string providerName, OpCode opCode, WriteBehindAsyncProcessor.TaskState state)
        {
            foreach (WriteThruProviderMgr provider in GetWriteThruMgr(providerName, this._writerProivder, opCode))
            {
                if (provider != null)
                {
                    provider.SetState(taskId, state);
                }
            }
        }

        public void SetState(string taskId, string providerName, OpCode opCode, WriteBehindAsyncProcessor.TaskState state, Hashtable table)
        {
            foreach (WriteThruProviderMgr provider in GetWriteThruMgr(providerName, this._writerProivder, opCode))
            {
                if (provider != null)
                {
                    provider.SetState(taskId, state, table);
                }
            }
        }

        private IEnumerable<WriteThruProviderMgr> GetWriteThruMgr(string providerName, IDictionary<string, WriteThruProviderMgr> providers, OpCode operationCode)
        {
            if (String.IsNullOrEmpty(providerName))
            {
                providerName = _defaultWriteThruProvider;
            }
            if (providerName == null)
            {
                yield break;
            }

            if (operationCode != OpCode.Clear)
            {
                WriteThruProviderMgr selected = null;
                providers.TryGetValue(providerName.ToLower(), out selected);
                yield return selected;
                yield break;
            }
            foreach (KeyValuePair<string, WriteThruProviderMgr> provider in providers)
            {
                yield return provider.Value;
            }
        }

        /// <summary>
        /// Deqeueu a task mathcing taskId
        /// </summary>
        /// <param name="taskId">taskId</param>
        public void DequeueWriteBehindTask(string[] taskId, string providerName)
        {
            WriteThruProviderMgr provider = null;
            if (String.IsNullOrEmpty(providerName)) 
                providerName = _defaultWriteThruProvider;
            this._writerProivder.TryGetValue(providerName.ToLower(), out provider);
            
            if (provider != null)
            {
                provider.DequeueWriteBehindTask(taskId);
            }
        }

		/// <summary>
		/// Responsible for updating/inserting an object to the data source. The Key and the 
		/// object are passed as parameter.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="val"></param>
		/// <returns></returns>
        public OperationResult WriteThru(string key, CacheEntry val, OpCode opCode, string providerName, OperationContext operationContext)
        {
            OperationResult result = null;
            if (_writerProivder == null) return null;
            WriteThruProviderMgr writeThruManager = GetProvider(providerName);
            if (writeThruManager != null)
            {
                DSWriteOperation operation = new DSWriteOperation(_context, key,  val, opCode, providerName);
                
                // Set for client cache impls
                result = writeThruManager.WriteThru(_context.CacheImpl, operation, false, operationContext);
                //synchronously applying update to cache store in case of write thru
                if (result != null && result.UpdateInCache)
                {
                    if (result.DSOperationStatus == OperationResult.Status.Success)
                        DSUpdateInCache(result.Operation, writeThruManager.LanguageContext);
                }
            }
            return result;
        }

        internal void DSUpdateInCache(WriteOperation updatedOp, LanguageContext languageContext)
        {
            object userBrinaryObject = null;
            Exception exc = null;
            bool rollback=ValidateWriteOperation(updatedOp);
            try
            {
               if (!rollback)
               {
                        switch (updatedOp.OperationType)
                        {
                            case WriteOperationType.Add:
                            case WriteOperationType.Update:
                                ProviderCacheItem item = updatedOp.ProviderCacheItem;
                                CacheEntry entry = null;
                                BitSet flag = new BitSet();
                                userBrinaryObject = GetCacheEntry(updatedOp.Key, item, ref flag, updatedOp.ProviderCacheItem.Group != null ? updatedOp.ProviderCacheItem.Group : null, updatedOp.ProviderCacheItem.SubGroup != null ? updatedOp.ProviderCacheItem.SubGroup : null, out entry, languageContext);
                                if (userBrinaryObject != null)
                                {
                                    _context.PerfStatsColl.MsecPerDSUpdBeginSample();
                                    CacheInsResultWithEntry result = _context.CacheImpl.Insert(updatedOp.Key, entry, true, null, 0, LockAccessType.IGNORE_LOCK, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                                    _context.PerfStatsColl.MsecPerDSUpdEndSample();
                                    _context.PerfStatsColl.IncrementDSUpdatePerSec();
                                    if (result != null && result.Result == CacheInsResult.IncompatibleGroup)
                                    {
                                        rollback = true;
                                        _context.NCacheLog.Error("DatasourceMgr.UpdateInCache", "Data group of the inserted item does not match the existing item's data group");
                                    }
                                }
                                break;
                        }
                 }
            }
            catch (Exception e)
            {
                exc = e;
                _context.NCacheLog.Error("DatasourceMgr.UpdateInCache", "Error:" + e.Message + " " + e.StackTrace);
            }
            finally
            {
                if (exc != null || rollback)
                {
                    try
                    {
                        //rollback, removing key from cache
                        _context.NCacheLog.Error("Data source Update in cache failed, removing key:" + updatedOp.Key);
                        _context.CacheImpl.Remove(updatedOp.Key, ItemRemoveReason.Removed, true, null, 0, LockAccessType.IGNORE_LOCK, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                        
                    }
                    catch (Exception ex)
                    {
                        _context.NCacheLog.Error("DatasourceMgr.UpdateInCache", "Error in removing key: " + updatedOp.Key + "Error: "+ex.Message + " " + ex.StackTrace);
                    }
                }
                _context.PerfStatsColl.IncrementCountStats(_context.CacheInternal.Count);
            }
        }

        private bool ValidateWriteOperation(WriteOperation operation)
        {
            if (operation == null)
            {
                _context.NCacheLog.Error("DatasourceMgr.UpdateInCache", "Write operation is not provided");
                return true;
            }
            else if (operation.ProviderCacheItem == null)
            {
                _context.NCacheLog.Error("DatasourceMgr.UpdateInCache", "Provider cache item is not provided for key: "+operation.Key);
                return true;
            }
            else if (operation.ProviderCacheItem.Value == null)
            {
                _context.NCacheLog.Error("DatasourceMgr.UpdateInCache", "Provider cache item value is not provided for key: " + operation.Key);
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="opCode"></param>
        /// <returns></returns>
        public OperationResult[] WriteThru(string[] keys, CacheEntry[] entries, Hashtable returnSet, OpCode opCode, string providerName, OperationContext operationContext)
        {
            OperationResult[] resultSet = null;
            if (_writerProivder == null) return null; 
            WriteThruProviderMgr writeThruManager = GetProvider(providerName);
            if (writeThruManager != null)
            {
                DSWriteOperation[] dsOperations = new DSWriteOperation[keys.Length];
               
                for (int i = 0; i < keys.Length; i++)
                {
                    dsOperations[i] = new DSWriteOperation(_context, keys[i],  entries[i], opCode, providerName);
                    
                   
                }
                resultSet = writeThruManager.WriteThru(_context.CacheImpl, dsOperations, returnSet, false, operationContext);
                if (resultSet == null) return null;
                //synchronously applying update to cache store in case of write thru
                DSUpdateInCache(resultSet, writeThruManager.LanguageContext);
            }
            return resultSet;
        }
        
        internal OperationResult WriteThru(DSWriteBehindOperation operation, OperationContext context)
        {
            OperationResult result = null;
            if (_writerProivder == null) return null;
            WriteThruProviderMgr writeThruManager = GetProvider(operation.ProviderName);
            if (writeThruManager != null)
            {
                result = writeThruManager.WriteThru(_context.CacheImpl, operation, true, context);
                if (result != null && result.UpdateInCache)
                {
                    if (result.DSOperationStatus == OperationResult.Status.Success && result.Operation != null)
                    {
                        switch (result.Operation.OperationType)
                        {
                            case WriteOperationType.Add:
                            case WriteOperationType.Update:
                                _updateOpProviderMgr[result.Operation.Key] = writeThruManager.LanguageContext;
                                this._dsUpdateProcessor.Enqueue(result.Operation);
                                break;
                        }                     
                    }
                }
            }
            return result;
        }

        internal OperationResult[] WriteThru(DSWriteBehindOperation[] operations, string provider, Hashtable returnSet, OperationContext context)
        {
            OperationResult[] result = null;
            if (_writerProivder == null) return null; 
            WriteThruProviderMgr writeThruManager = GetProvider(provider);
            if (writeThruManager != null)
            {
                result = writeThruManager.WriteThru(_context.CacheImpl, operations, returnSet, true, context);
                if (result == null) return null;
                //enqueue operations in update queue
                for (int i = 0; i < result.Length; i++)
                {
                    if (result[i] != null && result[i].UpdateInCache)
                    {
                        if (result[i].DSOperationStatus == OperationResult.Status.Success && result[i].Operation != null)
                        {
                            switch (result[i].Operation.OperationType)
                            {
                                case WriteOperationType.Add:
                                case WriteOperationType.Update:
                                    _updateOpProviderMgr[result[i].Operation.Key] = writeThruManager.LanguageContext;
                                    this._dsUpdateProcessor.Enqueue(result[i].Operation);
                                    break;
                            }
                        }
                    }
                }
            }
            return result;
        }
        internal void DSUpdateInCache(OperationResult[] resultSet, LanguageContext languageContext)
        {
            if (resultSet == null) return;
            BitSet[]  flags = new BitSet[resultSet.Length];
            object[] keys = new object[resultSet.Length];
            CacheEntry[] enteries = new CacheEntry[resultSet.Length];
            ArrayList keysToRemove = new ArrayList();
            int counter = 0;
            OperationResult.Status status;
            for (int i = 0; i < resultSet.Length; i++)
            {
                if (!(resultSet[i].UpdateInCache)) continue;

                WriteOperation operation = resultSet[i].Operation;
                bool rollback = ValidateWriteOperation(operation);

                if (rollback) 
                    keysToRemove.Add(operation);
                else if (resultSet[i].DSOperationStatus == OperationResult.Status.Success)
                {
                    ProviderCacheItem cacheItem = resultSet[i].Operation.ProviderCacheItem;
                    if (cacheItem == null) continue;

                    try
                    {
                        CacheEntry entry;
                        flags[i] = new BitSet();
                        object userBinaryObject = GetCacheEntry(resultSet[i].Operation.Key, cacheItem, ref flags[i], operation.ProviderCacheItem.Group != null ? operation.ProviderCacheItem.Group : null, operation.ProviderCacheItem.SubGroup != null ? operation.ProviderCacheItem.SubGroup : null, out entry, languageContext);

                        if (userBinaryObject == null)
                            continue;

                        keys[counter] = resultSet[i].Operation.Key;
                        enteries[counter++] = entry;
                    }
                    catch (Exception exception)
                    {
                        _context.NCacheLog.Error("DSWrite Operation", "Error occurred while updating key: "+resultSet[i].Operation.Key+" after write operations; " + exception.Message);
                        continue;
                    }
                }
            }
            if (counter == 0) return;

            Cache.Resize(ref keys, counter);
            Cache.Resize(ref enteries, counter);
            Hashtable keysInserted = null;
            try
            {
                _context.PerfStatsColl.MsecPerDSUpdBeginSample();
                keysInserted = _context.CacheImpl.Insert(keys, enteries, true, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                _context.PerfStatsColl.MsecPerDSUpdEndSample(keys.Length);
                _context.PerfStatsColl.IncrementDSUpdatePerSecBy(keys.Length);
            }
            catch (Exception ex)
            {
                try
                {
                    _context.NCacheLog.Error("DSWrite Operation:UpdateInCache", "Data source Update in cache failed, Error: " + ex.Message);
                    _context.CacheImpl.Remove(keys, ItemRemoveReason.Removed, true, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                }
                catch (Exception exp)
                {
                    _context.NCacheLog.Error("DSWrite Operation:UpdateInCache", "Error occurred while removing keys ; Error: " + exp.Message);
                }
            }
            finally 
            {
                //removing failed keys
                if (keysInserted != null && keysInserted.Count > 0)
                {
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (keysInserted.ContainsKey(keys[i]))
                        {
                            if (!(keysInserted[keys[i]] is Exception))
                            {
                                CacheInsResultWithEntry insResult = keysInserted[keys[i]] as CacheInsResultWithEntry;
                                if (insResult != null && (insResult.Result != CacheInsResult.Success && insResult.Result != CacheInsResult.SuccessOverwrite))
                                {
                                    keysToRemove.Add(keys[i]);
                                }
                            }
                            else
                                keysToRemove.Add(keys[i]);
                        }
                    }
                }
                try
                {
                    if (keysToRemove.Count > 0)
                    {
                        string[] selectedKeys=new string[keysToRemove.Count];
                        Array.Copy(keysToRemove.ToArray(),selectedKeys,keysToRemove.Count);
                        _context.CacheImpl.Remove(selectedKeys, ItemRemoveReason.Removed, true, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                    }
                }
                catch (Exception exp)
                {
                    _context.NCacheLog.Error("DSWrite Operation:UpdateInCache", "Error occurred while removing keys ; Error: " + exp.Message);
                }
            }
        }

        internal void DSAsyncUpdateInCache(WriteOperation operation)
        {
           if (_updateOpProviderMgr==null) return; 
           LanguageContext languageContext = LanguageContext.NONE;
            if (_updateOpProviderMgr.ContainsKey(operation.Key))
            {
                _updateOpProviderMgr.TryGetValue(operation.Key, out languageContext);
                if (languageContext != LanguageContext.NONE)
                {
                    DSUpdateInCache(operation,languageContext);
                }
            }
        }

        internal void HotApplyWriteBehind(string mode, int throttlingrate, int requeueLimit, int requeueEvictionRatio, int batchInterval, int operationDelay)
        {
            if (_writeBehindAsyncProcess != null)
                _writeBehindAsyncProcess.SetConfigDefaults(mode,throttlingrate,batchInterval,operationDelay,requeueLimit,requeueEvictionRatio);

        }

        internal ReadThruProviderMgr GetReadThruProviderMgr(string providerName)
        {
            if (_readerProivder.ContainsKey(providerName))
            {
                return _readerProivder[providerName];
            }
            return null;
        }
    }
}
            