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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Management;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alachisoft.NCache.SocketServer.Http
{
    public sealed class MetricsJsonEndpoint : IServiceHttpEndpoint
    {
        public string Path
        {
            get { return "/metrics/json"; }
        }

        public IEnumerable<string> Methods
        {
            get { return new[] { "GET" }; }
        }

        public ServiceHttpResponse Handle(ServiceHttpRequest request)
        {
            IReadOnlyCollection<MetricsSnapshot> snapshots =
                MetricsSnapshotStore.GetSnapshots();


            JsonWriter writer = new JsonWriter();

            writer.BeginObject();

            writer.Property("exportedAt", DateTime.UtcNow.ToString("o"));
            string version = snapshots
            .Select(x => x.Version)
            .FirstOrDefault(x => !string.IsNullOrEmpty(x));

            writer.Property(
                "version",
                version ?? string.Empty);

            writer.PropertyName("metrics");
            writer.BeginArray();

            foreach (MetricsSnapshot snapshot in snapshots)
            {
                writer.BeginObject();
                writer.Property("category", snapshot.Category.ToString());

                if (snapshot.Category == Publisher.NCacheClient)
                {
                    writer.Property(
                        "clientAddress",
                        snapshot.InstanceName ?? string.Empty);

                    writer.Property(
                        "cacheName",
                        snapshot.CacheConfigID ?? string.Empty);
                }
                else
                {
                    writer.Property(
                        "cacheName",
                        snapshot.InstanceName ?? string.Empty);

                    writer.Property(
                        "fromReplica",
                        snapshot.FromReplica);
                }
                writer.Property("sessionId", snapshot.SessionId);

                writer.PropertyName("values");
                writer.BeginObject();
                foreach (KeyValuePair<short, double> value in snapshot.Values)
                {
                    string counterName =
                        MetricsMetadataStore.GetCounterName(
                            snapshot.SessionId,
                            value.Key);

                    writer.Property(
                        counterName,
                        value.Value.ToString(
                            CultureInfo.InvariantCulture));
                }
               

                writer.EndObject();
                writer.EndObject();
            }

            writer.EndArray();
            writer.EndObject();

            string json = writer.ToString();

            //NCacheServiceLogger.LogInfo("Generated JSON: " + json);

            return ServiceHttpResponse.Json(
                HttpStatusCode.OK,
                writer.ToString());
        }
    }
}

