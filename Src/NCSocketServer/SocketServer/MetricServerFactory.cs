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
using Alachisoft.NCache.Common.Monitoring;
using System;
using System.Diagnostics;

namespace Alachisoft.NCache.SocketServer
{
    /// <summary>
    /// Creates the process-wide metric server (Alachisoft.NCache.MetricServer) that receives the
    /// counters published by cache hosts and exposes them to Prometheus. The Enterprise build loads
    /// the assembly by path for client-only installations; OpenSource only ships the server, so the
    /// project is referenced directly.
    /// </summary>
    public class MetricServerFactory
    {
        private static IMetricServer s_metricServer = null;
        private static readonly object s_lock = new object();

        public static IMetricServer GetMetricServer()
        {
#if NETCORE
            lock (s_lock)
            {
                if (s_metricServer == null)
                {
                    try
                    {
                        s_metricServer = new Alachisoft.NCache.MetricServer.MetricServer();
                    }
                    catch (Exception ex)
                    {
                        AppUtil.LogEvent("MetricServer", "Unable to start the metric server: " + ex, EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                    }
                }
            }
#endif
            return s_metricServer;
        }
    }
}
