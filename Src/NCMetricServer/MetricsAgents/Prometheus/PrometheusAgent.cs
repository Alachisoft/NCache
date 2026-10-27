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
using System;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.MetricServer.DataModel;
using System.Collections.Generic;
using Prometheus;

namespace Alachisoft.NCache.MetricServer.MetricsAgents
{
    public class PrometheusAgent : IMetricsAgent
    {
        public string hostName { get; set; }
        public int port { get; set; }
        private Prometheus.MetricServer server;
        private static ILogger _logger;
        PrometheusCountersManager _counterManager;
        IDictionary<string, Collector> _metrics = new Dictionary<string, Collector>();
        object _metricsLock = new object();


        public void Initialize(ILogger logger)
        {
            // On .NET the metrics are served by the service HTTP server, so its port is the Prometheus port.
            port = ServiceConfiguration.RestPort;
            _logger = logger;
            _counterManager = new PrometheusCountersManager(logger);
        }

        public void Start()
        {
#if NETCORE
            // No listener of its own: the NCache Service HTTP server (ServiceHttpServer) hosts the
            // Prometheus scrape endpoint at /metrics on the same listener as /metrics/json and the
            // health endpoints. Two HttpListeners on one port would either conflict (Linux) or let the
            // "/metrics/" prefix of a standalone Prometheus listener swallow /metrics/json (Windows).
            if (port > 0)
                _logger.Info("Metric Server", "Prometheus agent has been started. Metrics are served by the service HTTP server at port " + port + PrometheusExporter.RoutePath);
            else
                _logger.Error("Metric Server", "Prometheus agent has been started but the service HTTP server is disabled (NCacheServer.RestPort is not a positive port), so " + PrometheusExporter.RoutePath + " is not reachable. Set NCacheServer.RestPort to expose Prometheus metrics.");
#else
            try
            {
                server = new Prometheus.MetricServer(port: this.port);
                server.Start();
                _logger.Info("Metric Server", "Prometheus agent has been started at port: " + port);
            }

            catch(Exception e)
            {
                _logger.Error("Metric Server", "Unable to start Prometheus agent " + e.ToString());
            }
#endif
        }

        public void InitalizeCounterInstance(string instanceName, string counterName, CounterMetadata counterMetaData)
        {
            //_logger.Info("MetricServer", counterName + " " + enumerator.Current.Category);
            lock (_metricsLock)
            {
                switch (counterMetaData.Type)
                {
                    case CounterType.NumberOfItemCounter:
                    case CounterType.AverageCounter:
                    case CounterType.SampleCounter:
                    case CounterType.RateOfCounter:
                        var guage = Metrics.CreateGauge(counterName, counterMetaData.Description, new GaugeConfiguration() { LabelNames = _counterManager.PopulateLabels(counterName, counterMetaData.Category).ToArray() });
                        if (!_metrics.ContainsKey(instanceName)) _metrics.Add(instanceName, guage);
                        break;
                } 
            }
        }

        public void PublishCounterData(string instanceName, CounterData counterData, List<string> counterParams)
        {
            lock (_metricsLock)
            {
                if (_metrics.ContainsKey(instanceName))
                {
                    var type = _counterManager.GetPrometheusCountertype(counterData.CounterMetadata.Type);
                    Collector counterInstance = _metrics[instanceName];
                    switch (type)
                    {
                        case PrometheusMetrics.Gauge:
                            var gauge = counterInstance as Gauge;
                            gauge.WithLabels(counterParams.ToArray()).Set(counterData.Data);
                            break;

                        case PrometheusMetrics.Counter:
                            var counter = counterInstance as Prometheus.Counter;
                            counter.WithLabels(counterParams.ToArray()).Inc(counterData.Data == -10 ? 0 : counterData.Data);
                            break;

                        case PrometheusMetrics.Histogram:
                            var histogram = counterInstance as Histogram;
                            histogram.WithLabels(counterParams.ToArray()).Observe(counterData.Data);
                            break;

                        case PrometheusMetrics.Summary:
                            var summary = counterInstance as Summary;
                            summary.WithLabels(counterParams.ToArray()).Observe(counterData.Data);
                            break;
                    }
                } 
            }
        }

        public void DiposeCounterInstance(string instanceName, List<string> counterParams = null)
        {
            lock (_metricsLock)
            {
                if (_metrics.TryGetValue(instanceName, out Collector collector))
                {
                    if (counterParams != null)
                    {
                        var gauge = collector as Gauge;
                        gauge.RemoveLabelled(counterParams.ToArray()); 
                    }
                    _metrics.Remove(instanceName);
                }
            }
        }

        public void Dispose()
        {
            if (server != null)
            {
                server.Stop();
                server = null;
            }
            _logger.Info("Metric Server", "Prometheus agent has been stopped successfully");
        }        
    }
}
