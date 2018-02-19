// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Net;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// Renders cache to its client.
    /// </summary>
    public abstract class CacheRenderer
    {
        public delegate void ClientConnected(string client, string cacheId);
        public delegate void ClientDisconnected(string client, string cacheId);

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

        public abstract int Port { get; }

        public abstract IPAddress IPAddress { get; }

        public abstract decimal ClusterHealthDetectionInterval { get; }

        public abstract int ManagementPort { get; set; }

        public abstract string ManagementIPAddress { get; set; }

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

    }
}
