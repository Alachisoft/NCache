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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Propagator;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Persistence;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Topologies.Clustered;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Config.Dom;

namespace Alachisoft.NCache.Caching
{
    internal class CacheRuntimeContext : IDisposable
    {
        /// <summary> The one and only manager of the whole cache sytem. </summary>
        private Cache _cacheRoot;

        /// <summary> Logger used for NCache Logging. </summary>
        private ILogger _logger;

        /// <summary> The one and only manager of the whole cache sytem. </summary>
        private CacheBase _cacheImpl;
        /// <summary>Manager for implementing expiration</summary>
        public ExpirationManager ExpiryMgr;
        /// <summary> scheduler for auto-expiration tasks. </summary>
        public TimeScheduler TimeSched;
        /// <summary> Asynchronous event processor. </summary>
        public AsyncProcessor AsyncProc;

        /// <summary> The performance statistics collector object. </summary>
        public StatisticCounter PerfStatsColl;

        public PersistenceManager PersistenceMgr;
        public ConnectedClientsLedger ConnectedClients;

        /// <summary>Manager for events persistence</summary>

        /// <summary> Serialization context(actually name of the cache) used by the Compact framework.</summary>
        private string _serializationContext;
        /// <summary> Renders the cache to its client. </summary>
        private CacheRenderer _renderer;
        /// <summary> Cache synchronization manager used to synchronize with other caches. </summary>
     

        /// <summary></summary>
        private long _compressionThreshold;

        private bool _compressionEnabled = false;

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

        private SerializationFormat _serializationFormatter;

        public SerializationFormat SerializationFormat
        {
            get { return _serializationFormatter; }
            set { _serializationFormatter = value; }
        }
		
		public CacheTopology CacheTopology { get; set; }

		
        private bool? _isClusteredImpl;

        /// <summary> Contains the user defined types regisered with the Compact Framework. </summary>
        private Hashtable _cmptKnownTypes = new Hashtable(new EqualityComparer());
        public Hashtable _cmptKnownTypesforJava = new Hashtable(new EqualityComparer());
        public Hashtable _cmptKnownTypesforNet = new Hashtable(new EqualityComparer());
        private bool _isStartedAsMirror = false;


    

   

        private DataFormat _inMemoryDataFormat;

        private HealthAlerts healthAlerts = null;

        #region Compact Serialization
        /// <summary>
        /// Java and .Net types combined
        /// </summary>
        public Hashtable CompactKnownTypes
        {
            get { return _cmptKnownTypes; }
            set { _cmptKnownTypes = value; }
        }

        /// <summary>
        /// .Net types only
        /// </summary>
        public Hashtable CompactKnownTypesNET
        {
            get { return _cmptKnownTypesforNet; }
        }

        /// <summary>
        /// Java Types only
        /// </summary>
        public Hashtable CompactKnownTypesJAVA
        {
            get { return _cmptKnownTypesforJava; }
        }
        #endregion


        #region DataSharing
        #endregion

        /// <summary> The one and only manager of the whole cache sytem. </summary>
        public Cache CacheRoot
        {
            get { return _cacheRoot; }
            set { _cacheRoot = value; }
        }

        /// <summary> The one and only manager of the whole cache sytem. </summary>
        public bool IsClusteredImpl
        {
            get
            {
                if (!_isClusteredImpl.HasValue)
                {
                    _isClusteredImpl = Util.CacheHelper.IsClusteredCache(CacheImpl);
                    return _isClusteredImpl.Value;
                }
                return _isClusteredImpl.Value;
            }
        }

        /// <summary> The one and only manager of the whole cache sytem. </summary>
        public CacheBase CacheImpl
        {
            get { return _cacheImpl; }
            set { _cacheImpl = value; }
        }
        
        public bool IsStartedAsMirror
        {
            get { return _isStartedAsMirror; }
            set { _isStartedAsMirror = value; }
        }


        /// <summary> The one and only manager of the whole cache sytem. </summary>
        public CacheBase CacheInternal
        {
            get { return CacheImpl.InternalCache; }
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

        public long CompressionThreshold
        {
            get { return _compressionThreshold * 1024; }
            set { _compressionThreshold = value; }
        }

        public bool CompressionEnabled
        {
            get { return _compressionEnabled; }
            set { _compressionEnabled = value; }
        }

        SQLDependencySettings _sqlDependencySettings;

        public SQLDependencySettings SQLDepSettings
        {

            get { return _sqlDependencySettings; }
            set { _sqlDependencySettings = value; }
        }

       


      

        public ILogger NCacheLog
        {
            get { return _logger; }
            set { _logger = value; }
        }

        public HealthAlerts HealthAlerts
        {

            get { return healthAlerts; }
            set { healthAlerts = value; }
        }

        public bool IsDbSyncCoordinator
        {
            get
            {
                // incase of partitioned and local cache any node can initialize the hint 
                // but only coordinator can do synchronization
                if (CacheRoot.CacheType == "partitioned-server" || CacheRoot.CacheType == "local-cache" || CacheRoot.CacheType == "overflow-cache")
                    return true;
                // incase of replicated and partiotion of replica only coordinator/subcoordinator can initialize and synchronize the hint.
                else if (((CacheRoot.CacheType == "replicated-server" || CacheRoot.CacheType == "mirror-server") && ExpiryMgr.IsCoordinatorNode) ||
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
        
        public bool IsClusterInStateTransfer()
        {
            if (CacheImpl != null)
              return CacheImpl.IsClusterInStateTransfer();
            return false;
        }

        public void ExitMaintenance()
        {
            CacheImpl.ExitMaintenance(true);
        }

        public bool IsClusterUnderMaintenance()
        {
            return CacheImpl.IsClusterUnderMaintenance();
        }
        

        public bool IsClusterAvailableForMaintenance()
        {
            return CacheImpl.IsClusterAvailableForMaintenance();
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
                    
                
                    if (this.CompactKnownTypes != null)
                        this.CompactKnownTypes = null;

                    if (this.CompactKnownTypesJAVA != null)
                        this._cmptKnownTypesforJava = null;

                    if (this._cmptKnownTypesforNet != null)
                        this._cmptKnownTypesforNet = null;
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
               
                if (AsyncProc != null)
                {
                    AsyncProc.Stop();
                    AsyncProc = null;
                }


                if (TimeSched != null)
                {
                    TimeSched.Dispose();
                    TimeSched = null;
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

        //public PoolManager PoolManager { get; internal set; }

        /// <summary>
        /// Store level (InternalCache) object pool. 
        /// </summary>
        public PoolManager StorePoolManager { get; internal set; }

        /// <summary>
        /// Transactional pool mananager contains object pools of fixed size. Objects from these pools
        /// are rented only at operation level.
        /// </summary>
        public TransactionalPoolManager TransactionalPoolManager { get; internal set; }
        public PoolManager FakeObjectPool { get; internal set; }
    }
}
