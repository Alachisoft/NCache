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
    public static class MetricsSnapshotStore
    {
        private static readonly ConcurrentDictionary<
            string,
            MetricsSnapshot> _snapshots =
            new ConcurrentDictionary<string, MetricsSnapshot>();

        public static void Update(
            string sessionId,
            CounterDataCollection data)
        {
            if (string.IsNullOrEmpty(sessionId))
                return;

            if (data == null || data.Values == null || data.Values.Count == 0)
                return;

            // CounterDataCollection can contain multiple interval records.
            // The last one is the most recent data.
            IntervalCounterDataCollection latestData =
                data.Values[data.Values.Count - 1];

            if (latestData == null || latestData.Values == null)
                return;

            string cacheConfigID =
                MetricsMetadataStore.GetCacheConfigID(sessionId);

            MetricsSnapshot snapshot =
                new MetricsSnapshot(
                    sessionId,
                    data,
                    latestData,cacheConfigID);

                string snapshotKey =
                sessionId + ":" +
                (snapshot.FromReplica ? "replica" : "main");

                _snapshots.AddOrUpdate(
                snapshotKey,
                snapshot,
                (key, oldValue) => snapshot);

        }

        public static IReadOnlyCollection<MetricsSnapshot> GetSnapshots()
        {
            DateTime cutoff = DateTime.UtcNow.AddSeconds(-30);

            List<MetricsSnapshot> snapshots =
                new List<MetricsSnapshot>();

            foreach (KeyValuePair<string, MetricsSnapshot> item in _snapshots)
            {
                MetricsSnapshot snapshot = item.Value;

                if (snapshot.Timestamp < cutoff)
                {
                    MetricsSnapshot removedSnapshot;

                    _snapshots.TryRemove(
                        item.Key,
                        out removedSnapshot);

                    continue;
                }

                snapshots.Add(snapshot);
            }

            return snapshots;
        }

        public static bool TryGetSnapshot(
            string sessionId,
            out MetricsSnapshot snapshot)
        {
            return _snapshots.TryGetValue(
                sessionId,
                out snapshot);
        }

        public static void Remove(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
                _snapshots.TryRemove(sessionId, out _);
        }

        public static void Clear()
        {
            _snapshots.Clear();
        }
    }
}
