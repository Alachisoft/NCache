using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Monitoring;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Alachisoft.NCache.Caching.Statistics
{
    public class PerfStatsCollectorBase: ICounterMonitorableEntity
    {

        /// <summary> Instance name. </summary>
        protected string _instanceName;
        private List<PerformanceCounter> _availableCounters = new List<PerformanceCounter>();
        private CounterIDMap _counterIDMap;
        private ILogger _ncacheLog;
        protected bool _populationSuccessful = false;
        protected Dictionary<string, PerformanceCounter> _ncacheCounters = new Dictionary<string, PerformanceCounter>();

        /// <summary>
        /// Returns true if the current user has the rights to read/write to performance counters
        /// under the category of object cache.
        /// </summary>
        public string InstanceName
        {
            get { return _instanceName; }
            set { _instanceName = value; }
        }

        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
            set { _ncacheLog = value; }
        }

        /// <summary>
        /// Returns true if the current user has the rights to read/write to performance counters
        /// under the category of object cache.
        /// </summary>
        public bool UserHasAccessRights
        {
            get
            {
                try
                {
                    return true;

                }
                catch (Exception e)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PerfStatsCollector.UserHasAccessRights", e.Message);
                    return false;
                }

            }
        }
        public Category Category { get; set; }
        public bool IsPORReplica { get; set; }


        CounterMetadataCollection ICounterMonitorableEntity.Metadata { get { return Metadata(); } }

        IntervalCounterDataCollection ICounterMonitorableEntity.Data { get { return Data(); } }

        public Publisher PublisherType { get { return Publisher.NCache; } }

        public bool MergeCounters { get { return true; } }

        MonitoringEntityType IMonitorableEntity.GetEntityType
        {
            get { return MonitoringEntityType.Stats; }
        }
        public bool IsPrimary { get { return true; } }
    
        public MetricsPublisher StatsPublisher { get; set; }

        public PerfStatsCollectorBase(string instanceName, bool inProc)
        {
            _instanceName = GetInstanceName(instanceName, 0, inProc);
        }

        public PerfStatsCollectorBase(string instanceName, int port, bool inProc)
        {
            _instanceName = GetInstanceName(instanceName, port, inProc);
        }
        /// <summary>
        /// Creates Instancename 
        /// For outproc instanceName = CacheID
        /// For inProc instanceNAme = CacheID +"-" + ProcessID + ":" +port
        /// </summary>
        /// <param name="name"></param>
        /// <param name="port"></param>
        /// <param name="inProc"></param>
        /// <returns></returns>
        public string GetInstanceName(string instanceName, int port, bool inProc)
        {
            // This will not be replaced with ServiceConfiguration as this if for DEV only and loads from NCHOST config
            if (System.Configuration.ConfigurationSettings.AppSettings["InstanceNameText"] != null)
                instanceName = System.Configuration.ConfigurationSettings.AppSettings["InstanceNameText"] + "_" + instanceName;

            return !inProc ? instanceName : instanceName + " - " + Process.GetCurrentProcess().Id.ToString() + ":" + port.ToString();
        }

        #region Monitoring Entities

        internal void PopulateCounters()
        {

            if (Category != null && Category.Publish)
            {
                foreach (var counter in Category.Counters)
                {
                    PerformanceCounter performanceCounter;
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

        private CounterMetadataCollection Metadata()
        {
            IPerfInstaller perfInstaller = new PerfInstaller();
            var counterMetadataCollection = StatsMetricsUtil.Metadata(perfInstaller.CounterData, Publisher.NCache, Category);
            GenerateIDMap(counterMetadataCollection);
            counterMetadataCollection.FromReplica = IsPORReplica ? true : false;
            counterMetadataCollection.Category = Publisher.NCache;
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


        private IntervalCounterDataCollection Data()
        {
            Dictionary<short, double> counterData = new Dictionary<short, double>();

            lock (_availableCounters)
            {
                try
                {
                    foreach (var data in _availableCounters)
                    {
                        if (!counterData.ContainsKey(_counterIDMap.GetCounerID(data.CounterName)))
                            counterData.Add(_counterIDMap.GetCounerID(data.CounterName), data.NextValue());
                        else
                        {
                            throw new Exception(data.CounterName);
                        }
                    }

                    return new IntervalCounterDataCollection
                    {
                        Values = counterData,
                        Timestamp = DateTime.UtcNow,
                        FromReplica = IsPORReplica ? true : false,
                        PublisherType = Publisher.NCache,
                        InstanceName = InstanceName
                    };

                }
                catch (Exception ex)
                {
                    throw;
                }
            }


        }

        #endregion

    }
}
