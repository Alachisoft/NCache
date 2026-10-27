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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Alachisoft.NCache.Management
{
    public sealed class MetricsSnapshot
    {
        public string SessionId { get; }
        public string Version { get; }
        public Publisher Category { get; }
        public string InstanceName { get; }
        public bool FromReplica { get; }
        public string CacheUniqueID { get; }
        public DateTime Timestamp { get; }
        public string CacheConfigID { get; }

        public IReadOnlyDictionary<short, double> Values { get; }

        public MetricsSnapshot(
       string sessionId,
       CounterDataCollection data,
       IntervalCounterDataCollection latestData,
       string cacheConfigID)
        {
            SessionId = sessionId;
            Version = data.Version;
            Category = data.Category;
            InstanceName = data.InstanceName;
            FromReplica = data.FromReplica;
            CacheUniqueID = data.CacheUniqueID;
            Timestamp = latestData.Timestamp;
            CacheConfigID = cacheConfigID;

            Values = new Dictionary<short, double>(
                latestData.Values);
        }
    }
}

