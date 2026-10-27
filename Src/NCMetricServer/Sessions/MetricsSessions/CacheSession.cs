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
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.MetricServer.DataModel;
using Alachisoft.NCache.MetricServer.Sessions;
using System;
using System.Collections.Generic;
using Counter = Alachisoft.NCache.MetricServer.DataModel.Counter;

namespace Alachisoft.NCache.MetricServer.MetricsSessions
{
    /// <summary>
    /// Session of one cache host (or replica). Cache metrics carry the labels
    /// cache_id / config_id / server / is_mirror; the system metrics (cpu, memory, network) that
    /// the host publishes alongside carry server_ip / installation_type instead.
    /// </summary>
    public sealed class CacheSession : MetricsSession
    {
        private readonly CacheConfig _cacheMetadata;
        private bool _needToPublishMetaCounters = true;

        public CacheConfig CacheMetadata { get => _cacheMetadata; }

        public CacheSession(string sessionId, CacheConfig metadata, string version = null)
            : base(sessionId, MetricPublisher.NCache, metadata == null ? null : metadata.ConfigId + (metadata.FromReplica ? "_Replica" : ""))
        {
            Version = version;
            _cacheMetadata = metadata ?? new CacheConfig();
        }

        public override void PublishMetric(string instanceName, CounterData counterData)
        {
            try
            {
                var counterName = ExtractCounterName(instanceName);
                if (IsSystemMetric(counterName))
                    _metricsAgentManager.PublishCounterData(instanceName, counterData, GetSystemMetricsParamsList());
                else
                    PublishCacheMetrics(instanceName, counterData);
            }
            catch (Exception e)
            {
                Logger.Error("Metric Server", $"Unable to publish metric for Session: {SessionId}\n{e}");
            }
        }

        private void PublishCacheMetrics(string instanceName, CounterData counterData)
        {
            _metricsAgentManager.PublishCounterData(instanceName, counterData, GetParamsList());
            if (_needToPublishMetaCounters)
            {
                PublishTotalCacheSizeCounter();
                PublishConfiguredServersCounter();
                _needToPublishMetaCounters = false;
            }
        }

        private void PublishConfiguredServersCounter()
        {
            var counter = CreateCounter("configured_servers_count", "Total number of configured servers", CounterType.NumberOfItemCounter);
            var counterData = new CounterData { Data = _cacheMetadata.ConfiguredServersCount, CounterMetadata = counter };
            var instanceName = GetCounterInstanceName(GetCounterNameWithLiteral(counter.Name, counter.Category));
            _metricsAgentManager.PublishCounterData(instanceName, counterData, GetConfiguredServersParamsList());
        }

        private void PublishTotalCacheSizeCounter()
        {
            var counter = CreateCounter("total_cache_size", "Total size of the cache", CounterType.NumberOfItemCounter);
            var counterData = new CounterData { Data = _cacheMetadata.CacheSize, CounterMetadata = counter };
            var instanceName = GetCounterInstanceName(GetCounterNameWithLiteral(counter.Name, counter.Category));
            _metricsAgentManager.PublishCounterData(instanceName, counterData, GetParamsList());
        }

        // Label values must never be null: prometheus-net rejects them.
        protected override List<string> GetParamsList()
        {
            return new List<string>
            {
                _cacheMetadata.CacheId ?? string.Empty,
                _cacheMetadata.ConfigId ?? string.Empty,
                _cacheMetadata.ClientServerIP ?? string.Empty,
                _cacheMetadata.FromReplica.ToString()
            };
        }

        private List<string> GetSystemMetricsParamsList()
        {
            return new List<string>
            {
                _cacheMetadata.ClusterIP ?? string.Empty,
                _cacheMetadata.InstallationType ?? string.Empty
            };
        }

        private List<string> GetConfiguredServersParamsList()
        {
            List<string> list = GetParamsList();
            list.Add(_cacheMetadata.ConfiguredServers ?? string.Empty);
            return list;
        }

        private static Counter CreateCounter(string counterName, string description, CounterType type)
        {
            return new Counter
            {
                Name = counterName,
                Type = type,
                Description = description,
                Category = Alachisoft.NCache.Common.Monitoring.Publisher.NCache
            };
        }

        private static bool IsSystemMetric(string counterName)
        {
            return counterName == "ncache_cpu_usage" || counterName == "ncache_memory_usage" || counterName == "ncache_network_usage";
        }

        public override void Dispose()
        {
            lock (_counters)
            {
                foreach (var counterInstance in _counters)
                {
                    if (counterInstance.Contains("configured_servers_count"))
                        _metricsAgentManager.DiposeCounterInstance(counterInstance, GetConfiguredServersParamsList());
                    else if (IsSystemMetric(ExtractCounterName(counterInstance)))
                        _metricsAgentManager.DiposeCounterInstance(counterInstance, GetSystemMetricsParamsList());
                    else
                        _metricsAgentManager.DiposeCounterInstance(counterInstance, GetParamsList());
                }
            }
        }

        private static string ExtractCounterName(string instanceName)
        {
            int idx = instanceName.IndexOf(':');
            return idx < 0 ? string.Empty : instanceName.Substring(idx + 1);
        }
    }
}
