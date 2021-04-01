//  Copyright (c) 2021 Alachisoft
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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.CacheManagement;
using Alachisoft.NCache.Runtime.Caching;
using System;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Topologies.Clustered;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Pooling.Stats;
using Alachisoft.NCache.Common.FeatureUsageData.Dom;

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// Renders cache to its client.
    /// </summary>
    public abstract class CacheRenderer
    {
        public delegate void ClientConnected(string client, string cacheId, ClientInfo cacheInfo, long count);
        public delegate void ClientDisconnected(string client, string cacheId, Runtime.Caching.ClientInfo clientInfo, long count);

        private ClientConnected _clientConnected;
        private ClientDisconnected _clientDisconnected;

        public event ClientConnected OnClientConnected
        {
            add { _clientConnected += value; }
            remove { _clientConnected -= value; }
        }

        public event ClientDisconnected OnClientDisconnected
        {
            add { _clientDisconnected += value; }
            remove { _clientDisconnected -= value; }
        }
        public abstract List<string> DeadClients { get; }

        public abstract int Port { get; }

        public abstract IPAddress IPAddress { get; }

        public abstract decimal ClusterHealthDetectionInterval { get; }

        public abstract int ManagementPort { get; set; }

        public abstract string ManagementIPAddress { get; set; }

        public UserInfo UserInfo { get; set; } 

        /// <summary>
        /// Get logging status for logging type
        /// </summary>
        /// <param name="type">Type of logging</param>
        /// <returns>Current status of logging</returns>
        public abstract LoggingInfo.LogsStatus GetLoggingStatus(LoggingInfo.LoggingSubsystem subsystem, LoggingInfo.LoggingType type);

        /// <summary>
        /// Set and apply logging status for a logging type
        /// </summary>
        /// <param name="type">Type of logging</param>
        /// <param name="status">Logging status to set</param>
        public abstract void SetLoggingStatus(LoggingInfo.LoggingSubsystem subsystem, LoggingInfo.LoggingType type, LoggingInfo.LogsStatus status);

        public virtual List<Alachisoft.NCache.Common.Monitoring.ClientNode> GetClientList(string cacheId) { return null; }

        public virtual List<Alachisoft.NCache.Common.Monitoring.ClientProcessStats> GetClientProcessStats(string cacheId) { return null; }


        public abstract double GetCounterValue(string counterName);

        public virtual Alachisoft.NCache.Common.DataStructures.RequestStatus GetRequestStatus(string clientId, long requestId, long commandId) { return null; }

        public virtual void RegisterOperationModeChangeEvent() { throw new NotImplementedException(); }

        public abstract void InitializePools(bool createFakePools);

        public abstract PoolStats GetPoolStats(PoolStatsRequest request);
        public abstract ClientProfileDom GetClientProfile();
    }
}