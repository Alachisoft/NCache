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
using Alachisoft.NCache.Common.Monitoring.MetricsServer.PublishingData;
using System;

namespace Alachisoft.NCache.MetricServer
{
    /// <summary>
    /// The receiving end of metrics publishing. Cache hosts and clients push their counters to the
    /// NCache service over the management port (MetricsPublisher -> MetricsTransporter ->
    /// CacheServer.PublishData); the service hands them to this class, which keeps one session per
    /// publisher and exposes the latest values through the Prometheus agent.
    ///
    /// This is the OpenSource port of the Enterprise metric server: only the Prometheus agent is
    /// present (no SNMP sub-agent, no bridge sessions, no persistence database).
    /// </summary>
    public class MetricServer : IMetricServer
    {
        private SessionManager _sessionManager;
        private readonly NCacheLogger _logger = new NCacheLogger();

        public MetricServer()
        {
            Intialize();
        }

        public void Intialize()
        {
            if (_sessionManager == null)
            {
                _logger.Initialize(LoggerNames.MetricServer);
                _sessionManager = new SessionManager(_logger);
            }
        }

        public void Dispose()
        {
            if (_sessionManager != null)
                _sessionManager.Dispose();
            _logger.Close();
        }

        #region Publishing - Metadata

        public void PublishMetadata(string sessionId, string version, CacheMetaData cacheMeta)
        {
            try
            {
                _sessionManager.PublishCacheMetadata(sessionId, version, cacheMeta);
            }
            catch (Exception e)
            {
                _logger.Error("Metric Server", e.ToString());
            }
        }

        public void PublishMetadata(string sessionId, string version, ClientMetaData clientMeta)
        {
            try
            {
                _sessionManager.PublishClientMetadata(sessionId, version, clientMeta);
            }
            catch (Exception e)
            {
                _logger.Error("Metric Server", e.ToString());
            }
        }

        public void PublishMetadata(string sessionId, CounterMetadataCollection counterMeta)
        {
            try
            {
                _sessionManager.PublishCountersMetadata(sessionId, counterMeta);
            }
            catch (Exception e)
            {
                _logger.Error("Metric Server", e.ToString());
            }
        }

        #endregion

        #region Publishing - Data

        public PublishCountersDataResult PublishData(string session, CounterDataCollection data)
        {
            try
            {
                return _sessionManager.PublishCountersData(session, data);
            }
            catch (Exception e)
            {
                _logger.Error("Metric Server", e.ToString());
                throw;
            }
        }

        public void PublishData(string session, ClusterHealthData data)
        {
            // Cluster health is not exported to Prometheus.
        }

        #endregion
    }
}
