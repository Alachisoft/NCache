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
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    internal sealed class ClusterNodeInformation
    {
        private int _serverPort;
        private int _connectedClients;
        private int _refCount;
        /// <summary>partitioned to which clients are connected.</summary>
        private Address _activePartitioned;

        internal ClusterNodeInformation(int serverPort, int connectedClients)
        {
            this._serverPort = serverPort;
            this._connectedClients = connectedClients;
            AddRef();
        }

        internal void AddRef()
        {
            lock (this)
            {
                _refCount++;
            }
        }
        /// <summary>
        /// Decreases the reference count.
        /// </summary>
        /// <returns>True if no reference is left otherwise false</returns>
        internal bool RemoveRef()
        {
            lock (this)
            {
                _refCount--;
                if (_refCount == 0) return true;
            }
            return false;
        }

        internal int RefCount
        {
            get { return _refCount; }
        }
        internal int ServerPort
        {
            get { return this._serverPort; }
        }

        internal Address ActivePartition
        {
            get { return _activePartitioned; }
            set { _activePartitioned = value; }
        }

        internal int ConnectedClients
        {
            get { return this._connectedClients; }
            set { this._connectedClients = value; }
        }
    }
}