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
using System;
using System.Collections.Generic;

namespace Alachisoft.NCache.MetricServer.Sessions
{
    /// <summary>
    /// Session of one client process publishing the "NCache Client" counters; metrics carry
    /// cache_id / config_id / client_ip.
    /// </summary>
    public class ClientSession : MetricsSession
    {
        private readonly Client _clientMetadata;

        public Client ClientMetadata { get => _clientMetadata; }

        public ClientSession(string sessionId, Client clientMeta = null) : base(sessionId, MetricPublisher.Client, "")
        {
            _clientMetadata = clientMeta ?? new Client();
        }

        public override void PublishMetric(string instanceName, CounterData counterData)
        {
            try
            {
                _metricsAgentManager.PublishCounterData(instanceName, counterData, GetParamsList());
            }
            catch (Exception e)
            {
                _logger.Error("MetricServer", e.ToString());
            }
        }

        protected override List<string> GetParamsList()
        {
            return new List<string>
            {
                _clientMetadata.CacheName ?? string.Empty,
                _clientMetadata.CacheConfigId ?? string.Empty,
                _clientMetadata.BindIP ?? string.Empty
            };
        }
    }
}
