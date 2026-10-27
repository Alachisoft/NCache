//  Copyright (c) 2018 Alachisoft
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
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.MetricServer.DataModel;
using Alachisoft.NCache.MetricServer.MetricsAgents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Counter = Alachisoft.NCache.MetricServer.DataModel.Counter;

namespace Alachisoft.NCache.MetricServer.Sessions
{
    /// <summary>
    /// Holds the counter metadata and the most recent counter values of one publisher, and turns
    /// NCache counter names ("Average us/add") into Prometheus metric names ("ncache_average_us_per_add").
    /// </summary>
    public abstract class CounterSession
    {
        private int _inMemoryInterval;
        protected ILogger _logger;
        protected MetricsAgentManager _metricsAgentManager;
        protected string sessionGuid = string.Empty;

        protected bool CountersMetaPersisted { get; private set; }
        protected SlidingIndex<MutableKeyValuePair<CounterData, bool>> _countersIndex = null;
        protected IList<string> _counters = new List<string>();

        public CounterIDMap CounterIDMapCommon { get; set; } = new CounterIDMap();
        public List<Counter> CountersMetadata = new List<Counter>();

        protected CounterSession(string cacheConfigId)
        {
            sessionGuid = string.IsNullOrEmpty(cacheConfigId) ? Guid.NewGuid().ToString() : cacheConfigId;
        }

        public void IntializeCounters(int inmemoryInterval, ILogger logger, MetricsAgentManager metricsAgentManager)
        {
            _inMemoryInterval = inmemoryInterval;
            _countersIndex = new SlidingIndex<MutableKeyValuePair<CounterData, bool>>(_inMemoryInterval);
            _logger = logger;
            _metricsAgentManager = metricsAgentManager;
        }

        #region Publishing to agents

        /// <summary>Pushes every value received since the last call to the agents.</summary>
        public void PersistCounters()
        {
            try
            {
                lock (_countersIndex)
                {
                    var enumer = _countersIndex.GetCurrentData(getReplicatData: false);
                    while (enumer.MoveNext())
                    {
                        if (enumer.Current.Value)
                        {
                            CounterData counter = enumer.Current.Key;
                            if (CounterIDMapCommon != null && counter.CounterMetadata != null && CountersMetaPersisted)
                            {
                                var name = CounterIDMapCommon.GetCounterName(counter.CounterId);
                                var counterName = GetCounterNameWithLiteral(name, counter.CounterMetadata.Category);
                                PublishMetric(GetCounterInstanceName(counterName), counter);
                            }
                            enumer.Current.Value = false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error("MetricServer", e.ToString());
            }
        }

        /// <summary>Registers one Prometheus collector per counter of this session.</summary>
        public virtual void CreateCounters()
        {
            lock (CountersMetadata)
            {
                if (CountersMetaPersisted)
                    return;

                foreach (Counter counter in CountersMetadata)
                {
                    // One bad name must not stop the remaining counters from being created.
                    try
                    {
                        var counterName = GetCounterNameWithLiteral(counter.Name, counter.Category);
                        var counterInstanceName = GetCounterInstanceName(counterName);
                        _metricsAgentManager.InitalizeCounterInstance(counterInstanceName, counterName, counter);
                        _counters.Add(counterInstanceName);
                    }
                    catch (Exception e)
                    {
                        _logger.Error("MetricServer", $"Unable to create metric for counter '{counter.Name}': {e.Message}");
                    }
                }
                CountersMetaPersisted = true;
            }
        }

        #endregion

        public virtual void PublishMetric(string instanceName, CounterData counterData) { }
        protected abstract List<string> GetParamsList();

        #region Add Counters To Session - Data + Metadata

        public void StoreCountersMetadata(CounterMetadataCollection counterMeta)
        {
            if (CountersMetadata.Count != 0)
                return;

            try
            {
                lock (CountersMetadata)
                {
                    CounterIDMapCommon.AssignAndAddCounters(counterMeta.Counters);
                    if (counterMeta.Category == Publisher.NCache)
                        PopulateCacheCounters(counterMeta);

                    foreach (CounterMetadata meta in counterMeta.Counters)
                    {
                        CountersMetadata.Add(new Counter
                        {
                            Category = counterMeta.Category,
                            Description = meta.Description,
                            Name = meta.Name,
                            Type = meta.Type,
                        });
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error("MetricServer", e.ToString());
            }
        }

        public virtual void StoreCountersData(string sessionId, CounterDataCollection counterData)
        {
            IList<CounterData> counterDatas = new List<CounterData>();
            for (int iterator = 0; iterator < counterData.Values.Count; iterator++)
            {
                IntervalCounterDataCollection intervalData = counterData.Values[iterator];
                DateTime interval = intervalData.Timestamp;
                foreach (KeyValuePair<short, double> entry in intervalData.Values)
                {
                    // -10 is the publisher's "counter could not be read this interval" marker
                    // (MetricsPublisher fills it in for every id without a sample); it is not a value.
                    if (entry.Value == -10)
                        continue;
                    CounterData data = new CounterData
                    {
                        Data = entry.Value,
                        CounterId = entry.Key,
                        Timestamp = interval,
                    };
                    PopulateCounterMeta(data);
                    counterDatas.Add(data);
                }
                AddCountersDataToIndex(counterDatas);
                counterDatas.Clear();
            }
        }

        #endregion

        public void PopulateCounterMeta(CounterData counterData)
        {
            string name = CounterIDMapCommon.GetCounterName(counterData.CounterId);
            foreach (Counter item in CountersMetadata)
            {
                if (item.Name == name)
                {
                    counterData.CounterMetadata = item;
                    break;
                }
            }
        }

        #region Private - Methods

        protected void AddCountersDataToIndex(IList<CounterData> counterDatas, bool persist = true)
        {
            lock (_countersIndex)
            {
                for (int iterator = 0; iterator < counterDatas.Count; iterator++)
                {
                    _countersIndex.AddToIndex(new MutableKeyValuePair<CounterData, bool>
                    {
                        Key = counterDatas[iterator],
                        Value = persist
                    });
                }
            }
        }

        // Two synthetic counters derived from the cache metadata rather than from PerfMon.
        private void PopulateCacheCounters(CounterMetadataCollection counterMeta)
        {
            counterMeta.Counters.Add(CreateCounter("configured_servers_count", "Total number of configured servers", CounterType.NumberOfItemCounter, Publisher.NCache));
            counterMeta.Counters.Add(CreateCounter("total_cache_size", "Total size of the cache", CounterType.NumberOfItemCounter, Publisher.NCache));
        }

        private static CounterMetadata CreateCounter(string counterName, string description, CounterType type, Publisher publisher)
        {
            return new CounterMetadata
            {
                Name = counterName,
                Type = type,
                Description = description,
                Category = publisher
            };
        }

        private static string GetCounterLiteral(Publisher category)
        {
            switch (category)
            {
                case Publisher.NCacheClient:
                    return "ncache_client_";
                default:
                    return "ncache_";
            }
        }

        /// <summary>
        /// Builds a valid Prometheus metric name: prefix + lower-cased counter name with every
        /// character outside [a-z0-9_] replaced, e.g. "Hits ratio/sec (%)" becomes
        /// "ncache_hits_ratio_per_sec_percent" and "Average µs/Published Messages" becomes
        /// "ncache_average_us_per_published_messages".
        /// </summary>
        protected string GetCounterNameWithLiteral(string name, Publisher category)
        {
            if (!name.Contains(" "))
                name = string.Concat(name.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');

            string body = name.Replace("#", "")
                              .Replace("/", "_per_")
                              .Replace("(", "")
                              .Replace(")", "")
                              .Replace("-", "_")
                              .Replace("%", "percent")
                              .Replace("µ", "u")
                              .Replace(" ", "_")
                              .ToLowerInvariant();

            var sb = new StringBuilder(body.Length);
            foreach (char c in body)
                sb.Append((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' ? c : '_');

            string counterName = GetCounterLiteral(category) + sb;
            while (counterName.Contains("__"))
                counterName = counterName.Replace("__", "_");
            return counterName.TrimEnd('_');
        }

        protected string GetCounterInstanceName(string counterName)
        {
            return string.Format("{0}:{1}", sessionGuid, counterName);
        }

        #endregion

        public virtual void Dispose()
        {
            lock (_counters)
            {
                foreach (var counterInstance in _counters)
                {
                    _metricsAgentManager.DiposeCounterInstance(counterInstance, GetParamsList());
                }
            }
        }
    }
}
