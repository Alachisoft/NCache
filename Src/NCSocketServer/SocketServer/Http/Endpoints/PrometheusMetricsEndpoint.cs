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
using System.Net;
using Alachisoft.NCache.MetricServer.MetricsAgents;

namespace Alachisoft.NCache.SocketServer.Http.Endpoints
{
    /// <summary>
    /// GET /metrics — Prometheus scrape endpoint (text exposition format) for every counter the metric
    /// server publishes. It is served by the same <see cref="ServiceHttpServer"/> listener as
    /// /metrics/json and the health endpoints, so Prometheus scrapes NCacheServer.RestPort
    /// (default 9804) and no separate Prometheus port is opened.
    /// Registered only when NCacheServer.EnablePrometheusMonitoring is true.
    /// </summary>
    public sealed class PrometheusMetricsEndpoint : IServiceHttpEndpoint
    {
        public const string RoutePath = PrometheusExporter.RoutePath;

        public string Path
        {
            get { return RoutePath; }
        }

        public IEnumerable<string> Methods
        {
            get { return new[] { "GET" }; }
        }

        public ServiceHttpResponse Handle(ServiceHttpRequest request)
        {
            return new ServiceHttpResponse((int)HttpStatusCode.OK, PrometheusExporter.ContentType, PrometheusExporter.Export());
        }
    }
}
