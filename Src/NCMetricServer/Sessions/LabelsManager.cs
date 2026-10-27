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
using System.Collections.Generic;

namespace Alachisoft.NCache.MetricServer.Sessions
{
    /// <summary>
    /// Prometheus label names per publisher. The order must match the value lists built by
    /// <see cref="MetricsSessions.CacheSession"/> and <see cref="ClientSession"/>.
    /// </summary>
    public class LabelsManager
    {
        public List<string> CacheMetricLabels
        {
            get { return new List<string> { "cache_id", "config_id", "server", "is_mirror" }; }
        }

        public List<string> ClientMetricLabels
        {
            get { return new List<string> { "cache_id", "config_id", "client_ip" }; }
        }

        public List<string> SystemMetricsLabels
        {
            get { return new List<string> { "server_ip", "installation_type" }; }
        }
    }
}
