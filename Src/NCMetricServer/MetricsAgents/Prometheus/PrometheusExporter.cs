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
using System.IO;
using System.Threading;
using Prometheus;

namespace Alachisoft.NCache.MetricServer.MetricsAgents
{
    /// <summary>
    /// Renders every metric the <see cref="PrometheusAgent"/> publishes (the prometheus-net default
    /// registry) in the Prometheus text exposition format, so any HTTP host can serve it.
    ///
    /// On .NET the NCache Service does not open a separate Prometheus listener any more: the service
    /// HTTP server (NCacheServer.RestPort, default 9804) serves this output at
    /// <see cref="RoutePath"/> on the same listener as /metrics/json and the health endpoints.
    /// </summary>
    public static class PrometheusExporter
    {
        /// <summary>Path at which the service HTTP server exposes the Prometheus scrape endpoint.</summary>
        public const string RoutePath = "/metrics";

        /// <summary>Content type Prometheus expects for the text exposition format.</summary>
        public static string ContentType
        {
            get { return PrometheusConstants.ExporterContentType; }
        }

        /// <summary>Collects the default registry and returns the exposition text encoded as UTF-8 bytes.</summary>
        public static byte[] Export()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream, CancellationToken.None)
                    .GetAwaiter().GetResult();
                return stream.ToArray();
            }
        }
    }
}
