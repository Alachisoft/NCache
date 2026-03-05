using Alachisoft.NCache.Common.Caching.Statistics.CustomCounters;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Monitoring;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Alachisoft.NCache.Caching.Statistics
{
    public class CustomStatsCollectorBase : ICounterMonitorableEntity
    {
        protected string _instanceName;
        private List<PerformanceCounterBase> _availableCounters = new List<PerformanceCounterBase>();
        protected Dictionary<string, PerformanceCounterBase> _ncacheCounters = new Dictionary<string, PerformanceCounterBase>();
        private CounterIDMap _counterIDMap;
        protected bool _populationSuccessful = false;
        private ILogger _ncacheLog;

        public MetricsPublisher StatsPublisher { get; set; }
        public bool IsPORReplica { get; set; }
        public string InstanceName { get { return _instanceName; } set { _instanceName = value; } }
        public Category Category { get; set; }
        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
            set { _ncacheLog = value; }
        }

        public bool UserHasAccessRights
        {
            get { return true; }
        }
        public bool MergeCounters
        {
            get { return true; }
        }

        #region Monitorable Entity

        MonitoringEntityType IMonitorableEntity.GetEntityType
        {
            get { return MonitoringEntityType.Stats; }
        }

        CounterMetadataCollection ICounterMonitorableEntity.Metadata { get { return Metadata(); } }

        IntervalCounterDataCollection ICounterMonitorableEntity.Data { get { return Data(); } }

        public bool IsPrimary { get { return true; } }


        #endregion


        private Publisher _publisher = Publisher.NCache;
        public Publisher PublisherType { get { return _publisher; } }

        public CustomStatsCollectorBase(string instanceName, bool inProc, Publisher publisher)
        {
            _publisher = publisher;
            _instanceName = GetInstanceName(instanceName, 0, inProc);
        }

        public CustomStatsCollectorBase(string instanceName, int port, bool inProc, Publisher publisher)
        {
            _publisher = publisher;
            _instanceName = GetInstanceName(instanceName, port, inProc);
        }

        public CounterMetadataCollection Metadata()
        {
            CounterMetadataCollection counterMetadataCollection = null;
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var customCountersInstaller = GetCustomCountersInstaller();
                counterMetadataCollection = StatsMetricsUtil.Metadata(customCountersInstaller.CounterData, PublisherType, Category);
            }
            else
            {
                IPerfInstaller perfInstaller = GetPerfInstaller();
                counterMetadataCollection = StatsMetricsUtil.Metadata(perfInstaller.CounterData, PublisherType, Category);
            }
#else
            IPerfInstaller perfInstaller = GetPerfInstaller();
            counterMetadataCollection = StatsMetricsUtil.Metadata(perfInstaller.CounterData, PublisherType, Category);
# endif
            GenerateIDMap(counterMetadataCollection);
            counterMetadataCollection.FromReplica = IsPORReplica;
            counterMetadataCollection.Category = PublisherType;
            counterMetadataCollection.Version = StatsPublisher.NCacheVersion;
            counterMetadataCollection.InstanceName = InstanceName;
            return counterMetadataCollection;
        }

        internal virtual ICustomCountersInstaller GetCustomCountersInstaller()
        {
            return null;
        }

        internal virtual IPerfInstaller GetPerfInstaller()
        {
            return null;
        }

        private void GenerateIDMap(CounterMetadataCollection counterMetadataCollection)
        {
            _counterIDMap = new CounterIDMap();
            _counterIDMap.AssignAndAddCounters(counterMetadataCollection.Counters);

        }


        public IntervalCounterDataCollection Data()
        {
            Dictionary<short, double> counterData = new Dictionary<short, double>();
            lock (_availableCounters)
            {

                try
                {
                    foreach (var data in _availableCounters)
                    {
                        var counterId = _counterIDMap.GetCounerID(data.Name);
                        if (counterId == -10) continue;
                        counterData.Add(counterId, data.Value);
                    }
                    return new IntervalCounterDataCollection
                    {
                        Values = counterData,
                        Timestamp = DateTime.UtcNow,
                        FromReplica = IsPORReplica ? true : false,
                        PublisherType = PublisherType
                    };

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public string GetInstanceName(string instanceName, int port, bool inProc)
        {
            return !inProc ? instanceName : instanceName + " - " + Process.GetCurrentProcess().Id.ToString() + ":" + port.ToString();
        }

        #region Monitoring Entites

        internal void PopulateCounters()
        {
            if (Category.Publish)
            {
                foreach (var counter in Category.Counters)
                {
                    PerformanceCounterBase performanceCounter;
                    if (counter.Publish)
                    {
                        if (_ncacheCounters.TryGetValue(counter.Name, out performanceCounter))
                        {
                            if (!_availableCounters.Contains(performanceCounter))
                                _availableCounters.Add(performanceCounter);
                        }
                    }
                }
            }

        }

        internal void RegisterAsMonitorableEntity()
        {
            if (Category != null && Category.Publish)
            {
                if (_populationSuccessful)
                    StatsPublisher.RegisterMonitorableEntity(this);
            }
        }


        public MonitoringEntityType GetEntityType()
        {
            return MonitoringEntityType.Stats;
        }

        #endregion


    }
}
