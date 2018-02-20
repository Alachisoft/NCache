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

using Alachisoft.NCache.Caching.CacheSynchronization;
using Alachisoft.NCache.Caching.DatasourceProviders;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Persistence;

using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Processor;

using Alachisoft.NCache.Caching.DataReader;
using Alachisoft.NCache.Caching.Messaging;

namespace Alachisoft.NCache.Caching
{
    internal class CacheRuntimeContext : IDisposable
    {
        /// <summary> The one and only manager of the whole cache system. </summary>
        private Cache _cacheRoot;

        /// <summary> Logger used for NCache Logging. </summary>
        private ILogger _logger;

        /// <summary> The one and only manager of the whole cache system. </summary>
        private CacheBase _cacheImpl;
        /// <summary>Manager for implementing expiration</summary>
        public ExpirationManager ExpiryMgr;
        /// <summary> scheduler for auto-expiration tasks. </summary>
        public TimeScheduler TimeSched;
        /// <summary> Asynchronous event processor. </summary>
        public AsyncProcessor AsyncProc;

        /// <summary> scheduler for cluster-health-collection tasks. </summary>
        public TimeScheduler HealthCollectorTimeSched;

        /// <summary> The performance statistics collector object. </summary>
        public StatisticCounter PerfStatsColl;

        public PersistenceManager PersistenceMgr;
        //public ClientDeathDetector ClientDeathDetection, ClientDeathNotifier;
        public ConnectedClientsLedger ConnectedClients;

        /// <summary>Manager for events persistence</summary>
        /// <summary> Serialization context(actually name of the cache) used by the Compact framework.</summary>
        private string _serializationContext;
        /// <summary> Renders the cache to its client. </summary>
        private CacheRenderer _renderer;
        /// <summary> Cache synchronization manager used to synchronize with other caches. </summary>
        private CacheSyncManager _cacheSyncMgr;

        /// <summary></summary>


        private string _cacheName;

        private DataFormat inMemoryDataFormat;

        public DataFormat InMemoryDataFormat
        {
            set
            {
                inMemoryDataFormat = value;
            }
            get
            {
                return inMemoryDataFormat;
            }
        }


        // new code for data sharing
        public Hashtable _dataSharingKnownTypesforNet = new Hashtable();
   
    

        /// <summary> Manager for read-through and write-through operations. </summary>
        private DatasourceMgr _dsMgr;

        private DataFormat _inMemoryDataFormat;

        private EntryProcessorManager _entryProcessorManager;

        /// <summary> The one and only manager of the whole cache system. </summary>
        public Cache CacheRoot
        {
            get { return _cacheRoot; }
            set { _cacheRoot = value; }
        }

        /// <summary> The one and only manager of the whole cache system. </summary>
        public CacheBase CacheImpl
        {
            get { return _cacheImpl; }
            set { _cacheImpl = value; }
        }




        /// <summary> The one and only manager of the whole cache system. </summary>
        public CacheBase CacheInternal
        {
            get { return CacheImpl.InternalCache; }
        }

        /// <summary> Cache synchronization manager used to synchronize with other caches. </summary>
        internal CacheSyncManager SyncManager
        {
            get { return _cacheSyncMgr; }
            set { _cacheSyncMgr = value; }
        }

        /// <summary> Gets Cache serialization context used by CompactSerialization Framework. </summary>
        public string SerializationContext
        {
            get
            {
                return _serializationContext;
            }
            set
            {
                _serializationContext = value;
            }
        }

        public CacheRenderer Render
        {
            get { return _renderer; }
            set { _renderer = value; }
        }

    

        SQLDependencySettings _sqlDependencySettings;

        public SQLDependencySettings SQLDepSettings
        {

            get { return _sqlDependencySettings; }
            set { _sqlDependencySettings = value; }
        }




        /// <summary> The one and only manager of the whole cache system. </summary>
        public DatasourceMgr DsMgr
        {
            get { return _dsMgr; }
            set { _dsMgr = value; }
        }

        public ReaderResultSetManager ReaderMgr { get; set; }
        

        public ILogger NCacheLog
        {
            get { return _logger; }
            set { _logger = value; }
        }


        /// <summary> The one and only manager of the whole cache system. </summary>
        public bool IsClusteredImpl
        {
            get { return Util.CacheHelper.IsClusteredCache(CacheImpl); }
        }


        public bool IsDbSyncCoordinator
        {
            get
            {
                // incase of partitioned and local cache any node can initialize the hint 
                // but only coordinator can do synchronization
                if (CacheRoot.CacheType == "partitioned-server" || CacheRoot.CacheType == "local-cache" || CacheRoot.CacheType == "overflow-cache")
                    return true;
                    // incase of replicated and partition of replica only coordinator/subcoordinator can initialize and synchronize the hint.
                else if (((CacheRoot.CacheType == "replicated-server" ) && ExpiryMgr.IsCoordinatorNode) ||
                         (CacheRoot.CacheType == "partitioned-replicas-server" && ExpiryMgr.IsSubCoordinatorNode))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public IDataFormatService CachingSubSystemDataService
        {
            get { return _cachingSubSystemDataService; }
            set { _cachingSubSystemDataService = value; }
        }

        private IDataFormatService _cachingSubSystemDataService;


        private IDataFormatService _cacheWriteThruDataService;

        public IDataFormatService CacheWriteThruDataService
        {
            get { return _cacheWriteThruDataService; }
            set { _cacheWriteThruDataService = value; }
        }

        private IDataFormatService _cacheReadThruDataService;

        public IDataFormatService CacheReadThruDataService
        {
            get { return _cacheReadThruDataService; }
            set { _cacheReadThruDataService = value; }
        }


        public EntryProcessorManager EntryProcessorManager
        {
            set
            {
                _entryProcessorManager = value;
            }
            get
            {
                return _entryProcessorManager;
            }
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            lock (this)
            {
                if (SerializationContext != null)
                {
                    CompactFormatterServices.UnregisterAllCustomCompactTypes(SerializationContext);
                }


                if (PerfStatsColl != null)
                {
                    PerfStatsColl.Dispose();
                    PerfStatsColl = null;
                }

                if (ExpiryMgr != null)
                {
                    ExpiryMgr.Dispose();
                    ExpiryMgr = null;
                }
                if (MessageManager != null)
                {
                    MessageManager.StopMessageProcessing();
                    MessageManager = null;

                }
                if (CacheImpl != null)
                {
                    CacheImpl.Dispose();
                    CacheImpl = null;
                }
                if (TimeSched != null)
                {
                    TimeSched.Dispose();
                    TimeSched = null;
                }
                if (AsyncProc != null)
                {
                    AsyncProc.Stop();
                    AsyncProc = null;
                }

                if (HealthCollectorTimeSched != null)
                {
                    HealthCollectorTimeSched.Dispose();
                    HealthCollectorTimeSched = null;
                }

                if (disposing) GC.SuppressFinalize(this);
            }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
        }

        #endregion

        public bool InProc { get; set; }

        public MessageManager MessageManager { get; internal set; }
    }
}
