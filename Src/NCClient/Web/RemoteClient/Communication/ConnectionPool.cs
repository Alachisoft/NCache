//  Copyright (c) 2026 Alachisoft
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
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Net;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Holds the connection objects to the cache servers.
    /// </summary>
    internal sealed class ConnectionPool
    {
        private Hashtable _connections;
        private QueueDictionary<Address> loadBalancerQueue;

        public ConnectionPool()
        {
            this._connections = Hashtable.Synchronized(new Hashtable(5));
            this.loadBalancerQueue = new QueueDictionary<Address>();
        }

        /// <summary>
        /// Get or set connection in connection pool
        /// </summary>
        /// <param name="ip">ip of system to which connection is made</param>
        /// <returns>connection object</returns>
        public Connection this[Alachisoft.NCache.Common.Net.Address ip]
        {
            get { lock (this._connections.SyncRoot) return this._connections[ip] as Connection; }
            set
            {
                lock (this._connections.SyncRoot)
                {
                    this._connections[ip] = value;
                    this.loadBalancerQueue.Enqueue(ip);
                }
            }
        }

        /// <summary>
        /// Get count of connection in pool
        /// </summary>
        public int Count
        {
            get { lock (this._connections.SyncRoot) return this._connections.Count; }
        }

        public Hashtable Connections
        {
            get { return _connections; }
        }

        public ICollection Servers
        {
            get
            {
                lock (_connections.SyncRoot)
                {
                    return _connections.Keys;
                }
            }
        }

     

        public Connection GetAnyConnection()
        {
            Connection connection = null;
            Connection firstConnection = null;
            foreach (Connection conn in _connections.Values)
            {
                if (firstConnection == null) firstConnection = conn;
                if (conn.IsConnected)
                {
                    connection = conn;
                    break;
                }
            }

            if (connection == null)
                connection = firstConnection;

            return connection;
        }

        /// <summary>
        /// Add connection to pool. If connection already exists, it will be replaced with new
        /// connection instance
        /// </summary>
        /// <param name="ip">ip address of machine to which connection is made</param>
        /// <param name="connection">connection object</param>
        public void Add(Address ip, Connection connection)
        {
            lock (this._connections.SyncRoot)
            {
                this._connections[ip] = connection;
                this.loadBalancerQueue.Enqueue(ip);
            }
        }

        /// <summary>
        /// Remove connection from pool
        /// </summary>
        /// <param name="ip">ip address of machine to which connection is made</param>
        public void Remove(Alachisoft.NCache.Common.Net.Address ip)
        {
            lock (this._connections.SyncRoot)
            {
                this._connections.Remove(ip);
                this.loadBalancerQueue.Remove(ip);
            }
        }

        /// <summary>
        /// Check whether connection pool contains specified connection
        /// </summary>
        /// <param name="ip">ip address of machine to which connection is made</param>
        /// <returns>true if the connection exists, false otherwise</returns>
        public bool Contains(Address ip)
        {
            lock (this._connections.SyncRoot) return this._connections.Contains(ip);
        }

        /// <summary>
        /// Clone connection table.
        /// </summary>
        /// <returns>cloned connection table</returns>
        public Hashtable CloneConnectionTable()
        {
            lock (this._connections.SyncRoot) return this._connections.Clone() as Hashtable;
        }

        public bool FullyDisConnnected
        {
            get
            {
                lock (this._connections.SyncRoot)
                {
                    foreach (Connection conn in _connections.Values)
                    {
                        if (conn.IsConnected)
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{ [");
            foreach (Address ip in _connections.Keys)
            {
                builder.Append(ip.IpAddress.ToString());
                builder.Append(",");
            }
            builder.Remove(builder.Length - 1, 1);
            builder.Append("] }");
            return builder.ToString();
        }

        internal Address GetNextAddress()
        {
            lock (this._connections.SyncRoot)
            {
                var node = loadBalancerQueue.Dequeue();

                if (node == null)
                {
                    var connection = GetAnyConnection();

                    if (connection == null || !connection.IsConnected)
                    {
                        return null;
                    }
                    else
                    {
                        foreach (Address ad in this._connections.Keys)
                        {
                            loadBalancerQueue.Enqueue(ad);
                        }
                    }
                }

                Connection conn = _connections[node] as Connection;

                if (conn == null || !conn.IsConnected)
                {
                    conn = GetAnyConnection();

                    if (conn == null || !conn.IsConnected)
                        return null;
                }
                else
                {
                    loadBalancerQueue.Enqueue(node);
                }

                return conn.ServerAddress;
            }
        }

        internal void MarkConnectionsIdle()
        {
            lock (_connections.SyncRoot)
                foreach (Connection conn in _connections.Values)
                    conn.IsIdle = true;
        }

        internal List<Connection> GetIdleConnections()
        {
            var conList = new List<Connection>();

            lock (_connections.SyncRoot)
                foreach (Connection conn in _connections.Values)
                    if (conn.IsIdle) conList.Add(conn);

            return conList;
        }   
    }
}
