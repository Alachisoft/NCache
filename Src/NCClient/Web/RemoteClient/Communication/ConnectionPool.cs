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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Net;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Hold connection objects and hashmap from partition and partition of replica.
    /// Makes decision on bases of key and hashmap.
    /// </summary>
    internal sealed class ConnectionPool
    {
        private Hashtable _connections;
        private QueueDictionary<Address> loadBalancerQueue;
        private Hashtable _hashMap;
        private ArrayList _hashMapMember;
 
        private int _bucketSize = 0;
        private long _lastViewId = -1;

        public ConnectionPool() : this(null) { }

        public ConnectionPool(Hashtable hashMap)
        {
            this._connections = Hashtable.Synchronized(new Hashtable(5));
            this.loadBalancerQueue = new QueueDictionary<Address>();

            if (hashMap != null)
            {
                this._hashMap = (!hashMap.IsSynchronized) ? Hashtable.Synchronized(hashMap) : hashMap;
            }
            else
            {
                this._hashMap = Hashtable.Synchronized(new Hashtable());
                _hashMapMember = new ArrayList();
            }
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
        /// Get or set bucket size
        /// </summary>
        public int BucketSize
        {
            get { return this._bucketSize; }
            set { this._bucketSize = value; }
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

        /// <summary>
        /// Get last view id that published this hashmap
        /// </summary>
        public long LastViewId
        {
            get { return this._lastViewId; }
            set { this._lastViewId = value; }
        }

        /// <summary>
        /// Get connection to server on which the item is residing.
        /// If no connection exists, null will be returned.
        /// </summary>
        /// <param name="key">key of item</param>
        /// <returns>connection to server on which item is residing</returns>
        public Connection GetConnection(string key)
        {
            object ip = GetIpInternal(key);
            Connection connection = null;

            if (ip != null) lock (this._connections.SyncRoot) connection = this._connections[ip] as Connection;
            return connection;
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
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private object GetIpInternal(string key)
        {
            if (key == null || key == string.Empty) return null;
            if (this._hashMap == null || this._bucketSize == 0) return null;
            int hashCode = AppUtil.GetHashCode(key);
            int index = hashCode / this._bucketSize;
            if (index < 0) index = index * -1;

            lock (this._hashMap.SyncRoot) return this._hashMap[index];
        }

        /// <summary>
        /// Get ip address of server on which key resides
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>ip address of server</returns>

        public Address GetIp(string key)
        {
            return GetIpInternal(key) as Address;
        }

#if !DEVELOPMENT
        /// <summary>
        /// Set new hashmap to this new hashmap
        /// </summary>
        /// <param name="hashMap">new hashmap</param>
        public void SetHashmap(NewHashmap hashMap)
        {
            this._lastViewId = hashMap.LastViewId;
            lock (this._hashMap.SyncRoot)
            {
                this._hashMap = (!hashMap.Map.IsSynchronized) ? Hashtable.Synchronized(hashMap.Map) : hashMap.Map;

            }
            lock (this._connections.SyncRoot)
            {
                _hashMapMember = hashMap.Members;
           
            }
        }
        
#endif
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

        public bool FullyConnnected
        {
            get
            {
                lock (this._connections.SyncRoot)
                {
                    if (_hashMapMember.Count != _connections.Count) return false;
                    foreach (Connection conn in _connections.Values)
                    {
                        if (!conn.IsConnected)
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
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
            builder.Append("{ ("); builder.Append(this._lastViewId); builder.Append(") ");
            builder.Append("[");
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
