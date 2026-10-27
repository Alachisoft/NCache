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
using Alachisoft.NCache.Common.Monitoring.MetricsServer.PublishingData;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Management.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.ServiceControl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Alachisoft.NCache.Management
{
    public static class MetricsMetadataStore
    {
        private static readonly ConcurrentDictionary<string, MetricsMetadata> _metadata =
            new ConcurrentDictionary<string, MetricsMetadata>();

        public static void Update(
        string sessionId,
        CounterMetadataCollection metadata)
        {
            if (string.IsNullOrEmpty(sessionId) ||
                metadata == null ||
                metadata.Counters == null)
            {
                return;
            }

            CounterIDMap map = new CounterIDMap();
            map.AssignAndAddCounters(metadata.Counters);

            MetricsMetadata metricsMetadata = new MetricsMetadata
            {
                CounterMap = map,
                CacheConfigID = metadata.CacheConfigID
            };

            _metadata.AddOrUpdate(
                sessionId,
                metricsMetadata,
                delegate (string key, MetricsMetadata existingValue)
                {
                    return metricsMetadata;
                });
        }

        public static string GetCounterName(
        string sessionId,
        short counterId)
        {
            MetricsMetadata metadata;

            if (_metadata.TryGetValue(sessionId, out metadata))
            {
                string name = metadata.CounterMap.GetCounterName(counterId);

                if (!string.IsNullOrEmpty(name))
                    return name;
            }

            return counterId.ToString();
        }

        public static string GetCacheConfigID(string sessionId)
        {
            MetricsMetadata metadata;

            if (_metadata.TryGetValue(sessionId, out metadata))
                return metadata.CacheConfigID;

            return null;
        }
    }

    public sealed class MetricsMetadata
    {
        public CounterIDMap CounterMap { get; set; }

        public string CacheConfigID { get; set; }
    }
}
