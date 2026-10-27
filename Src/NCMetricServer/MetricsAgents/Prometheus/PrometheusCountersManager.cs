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
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.MetricServer.Sessions;
using System.Collections.Generic;

namespace Alachisoft.NCache.MetricServer.MetricsAgents
{
    /// <summary>Maps NCache counter categories/types to Prometheus label sets and metric kinds.</summary>
    public class PrometheusCountersManager
    {
        private readonly ILogger _logger;
        private readonly LabelsManager _labelMananger;

        public PrometheusCountersManager(ILogger logger)
        {
            _logger = logger;
            _labelMananger = new LabelsManager();
        }

        public List<string> PopulateLabels(string counterName, Publisher category)
        {
            switch (counterName)
            {
                case "ncache_configured_servers_count":
                    var cacheLabels = DefaultCountersLabels(category);
                    cacheLabels.Add("configured_servers_list");
                    return cacheLabels;
                case "ncache_cpu_usage":
                case "ncache_memory_usage":
                case "ncache_network_usage":
                    return DefaultCountersLabels(Publisher.SystemMetrics);
                default:
                    return DefaultCountersLabels(category);
            }
        }

        public List<string> DefaultCountersLabels(Publisher category)
        {
            switch (category)
            {
                case Publisher.NCacheClient:
                    return _labelMananger.ClientMetricLabels;
                case Publisher.SystemMetrics:
                    return _labelMananger.SystemMetricsLabels;
                default:
                    return _labelMananger.CacheMetricLabels;
            }
        }

        public PrometheusMetrics GetPrometheusCountertype(CounterType type)
        {
            switch (type)
            {
                case CounterType.NumberOfItemCounter:
                case CounterType.AverageCounter:
                case CounterType.SampleCounter:
                case CounterType.RateOfCounter:
                default:
                    return PrometheusMetrics.Gauge;
            }
        }
    }

}
