using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Monitoring.MetricsServer;
using Alachisoft.NCache.Common.Monitoring.MetricsServer.PublishingData;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;


namespace Alachisoft.NCache.Common.Monitoring
{
    public class MetricsPublisher : IDisposable
    {
        private long startTime = 0;
        private object _mutex = new object();
        private Thread _dataCollector, _dataPublisher;
        bool _ncacheMetaDataRequired = true;
        bool _noSqlMetaData = true;
        bool _publishMetadata = true;
        bool _publishNoSqlAndLuceneMetaData = false;
        private Dictionary<MonitoringEntityType, List<IMonitorableEntity>> _monitorableEntities = new Dictionary<MonitoringEntityType, List<IMonitorableEntity>>();
        private MonitorableEntitiesDataStore _entitiesDataStore = new MonitorableEntitiesDataStore(2);
        List<IntervalCounterDataCollection> _intervalCounterCollection = new List<IntervalCounterDataCollection>();
        private IMetricsTransporterFactory factoryInstance;
        private IMetricsTransporter _transporter;
        private IPublisherContextMetaPublisher _contextMetaPublisher;
        private Dictionary<string, MetricsException> _exceptionLogger = new Dictionary<string, MetricsException>();
        private IDictionary<string, ICollector> _systemMetricsDictionary = new Dictionary<string, ICollector>();
        private IDictionary<string, List<IntervalCounterDataCollection>> _counterDataMap = new Dictionary<string, List<IntervalCounterDataCollection>>();
        private IDictionary<string, MonitorableEntitiesDataStore> _monitorableEntityMap = new Dictionary<string, MonitorableEntitiesDataStore>();
        private IDictionary<string, CounterIDMap> _entityIdMap = new Dictionary<string, CounterIDMap>();
        public string SessionId { get; set; }
        public string ProcessId { get; set; }
        public string CacheConfigID { get; set; }
        public static MetricsPublisher Instance { get; private set; }
        public CounterMetadataCollection CounterMetadataCollection { get; set; }
        public CacheMetaData CacheMetaData { get; set; }
        public ILogger NCacheLog { get; set; }
        public string NCacheVersion { get; set; }
        public IDictionary<string, ICollector> SystemMetricsDictionary { get => _systemMetricsDictionary; set => _systemMetricsDictionary = value; }

        public MetricsPublisher(IMetricsTransporterFactory factory, IPublisherContextMetaPublisher contextMetaPublisher)
        {
            NCacheVersion = Version.GetVersionForNuGet();
            factoryInstance = factory;
            _contextMetaPublisher = contextMetaPublisher;
            ProcessId = Process.GetCurrentProcess().Id.ToString();
        }

        public void Initialize()
        {
            if (!ServiceConfiguration.EnableMetricsPublishing)
            {
                if (NCacheLog != null)
                    NCacheLog.Info("Monitoring Module", "Metrics publishing is not enable from service configuration file.");
                return;
            }
            if (!MonitoringConfigManager.isConfigFileFound)
            {
                if (NCacheLog != null)
                    NCacheLog.Error("Monitoring Module", "monitoring.ncconf not found");
                return;
            }
            StartGatheringData();
            StartPublishingData();
        }

        public bool RegisterMonitorableEntity(IMonitorableEntity monitorableEntity)
        {
            lock (_mutex)
            {
                try
                {
                    if (ServiceConfiguration.EnableMetricsPublishing)
                    {

                        MonitoringEntityType type = monitorableEntity.GetEntityType;

                        List<IMonitorableEntity> entities = new List<IMonitorableEntity>();
                        if (!_monitorableEntities.TryGetValue(type, out entities))
                        {
                            List<IMonitorableEntity> entityList = new List<IMonitorableEntity>();
                            entityList.Add(monitorableEntity);
                            _monitorableEntities.Add(type, entityList);
                        }
                        if (entities != null)
                            entities.Add(monitorableEntity);
                        return true;
                    }

                }
                catch (Exception ex)
                {
                    if (NCacheLog != null)
                        NCacheLog.Error("Monitoring Module", ex.ToString());
                    return false;
                }
            }
            return false;

        }
        #region Data Collection
        public void StartGatheringData()
        {
            if (_dataCollector == null)
            {
                _dataCollector = new Thread(new ThreadStart(PerformDataCollection));
                _dataCollector.IsBackground = true;
                _dataCollector.Name = "MetricsCollection";
                _dataCollector.Start();
            }
        }

        public void StopGatheringData()
        {
            if (_dataCollector != null && _dataCollector.IsAlive)
            {
#if !NETCORE
                _dataCollector.Abort();
#else
                _dataCollector.Interrupt();
#endif
            }
        }

        private void PerformDataCollection()
        {
            try
            {
                bool threadAbort = false;
                while (!threadAbort)
                {
                    try
                    {
                        foreach (var registeredEntities in _monitorableEntities)
                        {

                            switch (registeredEntities.Key)
                            {
                                case MonitoringEntityType.Stats:
                                    ProcessCounterStats(registeredEntities.Value);
                                    StoreStats();
                                    break;
                                case MonitoringEntityType.APILogs:
                                    break;
                                case MonitoringEntityType.Events:
                                    break;
                                case MonitoringEntityType.ClusterHealth:
                                    break;
                            }

                        }


                        //iterate through each monitorable entity
                        //1. call their native Data
                        //2. you may need to merge
                        // store data into monitorablestore

                        // sleep for 1 sec
                    }
                    catch (ThreadAbortException)
                    {
                        threadAbort = true;
                        break;
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (NCacheLog != null)
                            NCacheLog.Error("Data Collector", ex.ToString());
                    }
                    finally
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                if (NCacheLog != null)
                    NCacheLog.Error("Data Collector", ex.ToString());
            }
        }

        private void ProcessCounterStats(List<IMonitorableEntity> registeredEntities)
        {
            foreach (var entity in registeredEntities)
            {
                var storeMetrics = (ICounterMonitorableEntity)entity;

                if (_ncacheMetaDataRequired && entity.IsPrimary)
                {
                    CollectCountersMetadata(entity);
                    _ncacheMetaDataRequired = false;
                }
                if (_noSqlMetaData && !_publishNoSqlAndLuceneMetaData)
                {
                    CollectCountersMetadata(entity);
                    _noSqlMetaData = false;
                    _publishNoSqlAndLuceneMetaData = true;
                }
                else
                {
                    CollectCounterData((ICounterMonitorableEntity)entity);
                }
            }

        }

        private void StoreStats()
        {
            MergeDataFromModules();
        }

        private void MergeDataFromModules()
        {
            bool fromreplica = false;
            Publisher publisher = Publisher.NCache;
            DateTime dateTime = new DateTime();
            string instanceName = null;
            try
            {
                foreach (var intervalCounter in _counterDataMap)
                {
                    Dictionary<short, double> data = new Dictionary<short, double>();
                    foreach (var intCollection in intervalCounter.Value)
                    {
                        foreach (var val in intCollection.Values)
                        {
                            if (!data.ContainsKey(val.Key))
                                data.Add(val.Key, val.Value);
                        }
                        dateTime = intCollection.Timestamp;
                        fromreplica = intCollection.FromReplica;
                        publisher = intCollection.PublisherType;
                        instanceName = intCollection.InstanceName;
                    }

                    foreach (var id in _entityIdMap[intervalCounter.Key].CounterIds)
                    {
                        if (!data.ContainsKey(id))
                        {
                            data.Add(id, -10.00);
                        }
                    }
                    //_intervalCounterCollection.Clear();

                    var intervalData = new IntervalCounterDataCollection
                    {
                        Values = data,
                        Timestamp = dateTime,
                        FromReplica = fromreplica,
                        PublisherType = publisher,
                        InstanceName = instanceName
                    };

                    if (!_monitorableEntityMap.ContainsKey(intervalCounter.Key))
                    {
                        var _entitySore = new MonitorableEntitiesDataStore(2);
                        _entitySore.AddCounterData(intervalData);
                        _monitorableEntityMap.Add(intervalCounter.Key, _entitySore);
                    }
                    else
                    {
                        _monitorableEntityMap[intervalCounter.Key].AddCounterData(intervalData);
                    }

                    intervalCounter.Value.Clear();
                }
            }
            catch (Exception ex)
            {
                if (NCacheLog != null)
                    NCacheLog.Error("Monitoring Module", ex.Message);
                throw ex;
            }
        }

        private void CollectCounterData(ICounterMonitorableEntity entity)
        {
            try
            {
                if (_ncacheMetaDataRequired)
                    return;

                var key = entity.PublisherType.ToString();
                IntervalCounterDataCollection dataCollection = entity.Data;
                if (ServiceConfiguration.EnableSystemCountersMonitoring)
                {
                    CollectSystemCounterData(dataCollection, _entityIdMap[key]);
                }

                if (!_counterDataMap.ContainsKey(key))
                {
                    var dataList = new List<IntervalCounterDataCollection>();
                    dataList.Add(dataCollection);
                    _counterDataMap.Add(key, dataList);
                }
                else
                {
                    _counterDataMap[key].Add(dataCollection);
                }

                //_intervalCounterCollection.Add(dataCollection);
            }
            catch (Exception ex)
            {
                if (NCacheLog != null)
                    NCacheLog.Error("Monitoring Module Collect CounterData", ex.ToString());
                throw;
            }
        }

        private void CollectSystemCounterData(IntervalCounterDataCollection dataCollection, CounterIDMap counterIDMap)
        {
            try
            {
                if (_systemMetricsDictionary != null && _systemMetricsDictionary.Count != 0)
                {

                    foreach (var item in _systemMetricsDictionary)
                    {
                        dataCollection.Values.Add(counterIDMap.GetCounerID(item.Key), item.Value.Collectstats());
                    }
                }
            }
            catch (Exception ex)
            {
                if (NCacheLog != null)
                    NCacheLog.Error("Monitoring Module", ex.ToString());
                throw;
            }
        }

        private void CollectSystemCountersMetadata(CounterMetadataCollection counterMetadata)
        {
            if (_systemMetricsDictionary != null && _systemMetricsDictionary.Count != 0)
            {
                foreach (var item in _systemMetricsDictionary)
                {
                    counterMetadata.Counters.Add(new CounterMetadata { Name = item.Key, Type = CounterType.RateOfCounter, Description = item.Value.Name, Category = counterMetadata.Category });
                }
            }
        }

        private void CollectCountersMetadata(IMonitorableEntity monitorableEntity)
        {
            ICounterMonitorableEntity entity = (ICounterMonitorableEntity)monitorableEntity;
            string key = entity.PublisherType.ToString();
            CounterMetadataCollection = entity.Metadata;
            if (entity.PublisherType == Publisher.NCacheClient)
            {
                CounterMetadataCollection.CacheConfigID = CacheConfigID;
            }

            if (ServiceConfiguration.EnableSystemCountersMonitoring)
            {
                CollectSystemCountersMetadata(CounterMetadataCollection);
            }

            if (CounterMetadataCollection == null)
            {
                if (NCacheLog != null)
                    NCacheLog.Error("Monitoring Module", "Meta info is not populated.");
                throw new Exception("No meta info is added.");
            }
            CounterIDMap _counterIdMap = new CounterIDMap();
            _counterIdMap.AssignAndAddCounters(CounterMetadataCollection.Counters);
            if (!_entityIdMap.ContainsKey(key))
            {

                _entityIdMap.Add(key, _counterIdMap);
            }
            else
            {
                _entityIdMap[key] = _counterIdMap;
            }
        }
        #endregion

        #region Publishing Data
        public void StartPublishingData()
        {
            if (_dataPublisher == null)
            {
                _dataPublisher = new Thread(new ThreadStart(PublishData));
                _dataPublisher.IsBackground = true;
                _dataPublisher.Name = "DataPublisher";
                _dataPublisher.Start();
            }
        }

        public void StopPublishingData()
        {
            if (_dataPublisher != null && _dataPublisher.IsAlive)
            {
#if !NETCORE
                _dataPublisher.Abort();
#elif NETCORE
                _dataPublisher.Interrupt();
#endif
            }
        }

        private void PublishData()
        {
            while (true)
            {
                try
                {

                    InitializedTrasnpoter();
                    Thread.Sleep(ServiceConfiguration.MetricsMonitorPublishingInterval);


                    PublishMetadata();

                    PublishCounterData();

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
                    if (NCacheLog != null)
                    {
                        MetricsException metricsException = new MetricsException();
                        if (!_exceptionLogger.TryGetValue(ex.GetType().ToString(), out metricsException))
                        {
                            _exceptionLogger.Add(ex.GetType().ToString(), new MetricsException { TimeStamp = DateTime.UtcNow, Exception = ex, LastLogTime = DateTime.UtcNow });
                            if (NCacheLog != null)
                                NCacheLog.Error("Metrics Publisher", ex.Message);
                        }
                        else
                        {
                            metricsException = _exceptionLogger[ex.GetType().ToString()];
                            metricsException.TimeStamp = DateTime.UtcNow;
                            metricsException.Exception = ex;
                            if (metricsException.CompareAndUpdateifRequired())
                            {
                                if (NCacheLog != null)
                                    NCacheLog.Error("Metrics Publisher", ex.Message);
                                metricsException.LastLogTime = DateTime.UtcNow;
                            }

                        }
                    }
                }
            }
        }

        private void PublishMetadata()
        {
            foreach (var registeredEntities in _monitorableEntities)
            {
                foreach (var entity in registeredEntities.Value)
                {
                    var storeMetrics = (ICounterMonitorableEntity)entity;
                    var publisherString = storeMetrics.PublisherType.ToString();
                    if (_publishMetadata)
                    {
                        if (storeMetrics.Metadata == null) continue;
                        if (storeMetrics.PublisherType == Publisher.NCacheClient)
                        {
                            storeMetrics.Metadata.CacheConfigID = CacheConfigID;
                        }
                        // this method call is responsible for send metadata of counter publisher entries 
                        // such as caches, bridges, client,  bridge caches, clients 
                        _contextMetaPublisher.PublishMetadata(_transporter);

                        // this method call is responsible for send metadata of counters publish againts the above entities. 
                        if (_ncacheMetaDataRequired == false)
                        {
                            _transporter.PublishMetadata(SessionId + ":" + publisherString, storeMetrics.Metadata);
                        }
                        _publishMetadata = false;
                    }
                    if (_publishNoSqlAndLuceneMetaData)
                    {
                        _contextMetaPublisher.PublishMetadata(_transporter);
                        _transporter.PublishMetadata(SessionId + ":" + publisherString, storeMetrics.Metadata);
                        _publishNoSqlAndLuceneMetaData = false;
                    }
                }
            }

        }

        private void PublishCounterData()
        {
            if (_publishMetadata)
                return;
            long refStartTime = startTime;
            var dataList = new List<CounterDataCollection>();
            foreach (var entityStore in _monitorableEntityMap)
            {
                refStartTime = startTime;

                var dataToBePublished = entityStore.Value.GetStatsData(ref refStartTime);
                if (dataToBePublished != null && dataToBePublished.Count > 0)
                {
                    var first = dataToBePublished.FirstOrDefault();
                    CounterDataCollection counterDataCollection = new CounterDataCollection
                    {
                        Values = dataToBePublished,
                        Version = NCacheVersion,
                        Category = first.PublisherType,
                        InstanceName = first.InstanceName,
                        FromReplica = first.FromReplica,
                        CacheUniqueID = ProcessId
                    };
                    dataList.Add(counterDataCollection);
                }
            }
            startTime = refStartTime;
            foreach (var data in dataList)
            {
                var result = _transporter.PublishData(SessionId + ":" + data.Category.ToString(), data);
                if (result == PublishCountersDataResult.CountersSessionExpired || result == PublishCountersDataResult.CountersMetaDataNotPersisted)
                {
                    _publishMetadata = true;
                    _publishNoSqlAndLuceneMetaData = true;
                }
            }
            dataList.Clear();
        }


        private void InitializedTrasnpoter()
        {
            if (_transporter == null)
                _transporter = factoryInstance.Create();

            if (_transporter != null && !_transporter.IsConnected)
            {
                _transporter.Connect();
            }
        }
        #endregion

        public void RepublishMetaData()
        {
            _publishMetadata = true;
        }

        public void Dispose()
        {

            StopGatheringData();
            StopPublishingData();
            if (_transporter != null)
            {
                _transporter.Dispose();
            }
            _contextMetaPublisher = null;
        }
    }
}
