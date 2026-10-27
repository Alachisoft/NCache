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

namespace Alachisoft.NCache.MetricServer.MetricsAgents
{
    /// <summary>Fans every session event out to the registered agents.</summary>
    public class MetricsAgentManager : IDisposable
    {
        private readonly IList<IMetricsAgent> _meticsAgents = new List<IMetricsAgent>();

        public void AddMetricsAgent(IMetricsAgent metricsAgent)
        {
            if (metricsAgent != null)
                _meticsAgents.Add(metricsAgent);
        }

        public void InitalizeCounterInstance(string instanceName, string counterName, CounterMetadata counterMetaData)
        {
            foreach (var meticsAgent in _meticsAgents)
                meticsAgent.InitalizeCounterInstance(instanceName, counterName, counterMetaData);
        }

        public void PublishCounterData(string instanceName, CounterData counterData, List<string> counterParams)
        {
            foreach (var meticsAgent in _meticsAgents)
                meticsAgent.PublishCounterData(instanceName, counterData, counterParams);
        }

        public void DiposeCounterInstance(string instanceName, List<string> counterParams = null)
        {
            foreach (var meticsAgent in _meticsAgents)
                meticsAgent.DiposeCounterInstance(instanceName, counterParams);
        }

        public void Dispose()
        {
            foreach (var meticsAgent in _meticsAgents)
                meticsAgent.Dispose();
            _meticsAgents.Clear();
        }
    }
}
