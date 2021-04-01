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
using System;
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Runtime.Caching;

namespace Alachisoft.NCache.Caching
{
    public class ConnectedClientsLedger
    {
        internal delegate void DeadClientNotification(string deadClient, ClientInfo info);
        internal class ConnectedClient
        {
            string _clientId;
            ClientInfo _info;
            HashSet<Address> _connectedNodes;

            public ConnectedClient()
            {
                _connectedNodes = new HashSet<Address>();
            }

            public void AddNode(Address address)
            {
                if (!_connectedNodes.Contains(address))
                    _connectedNodes.Add(address);
            }

            public int RemoveNode(Address address)
            {
                {
                    _connectedNodes.Remove(address);
                    return _connectedNodes.Count;
                }
            }

            public bool ContainsNode(Address address)
            {
                return _connectedNodes.Contains(address);
            }

            public Address[] Nodes
            {
                get
                {
                    {
                        Address[] addresses = new Address[_connectedNodes.Count];
                        _connectedNodes.CopyTo(addresses);
                        return addresses;
                    }
                }
            }

            public int NodeCount { get { lock (this) return _connectedNodes.Count; } }

            public string ClientId
            {
                get { return _clientId; }
                set { _clientId = value; }
            }

            public ClientInfo Info
            {
                get { return _info; }
                set { _info = value; }
            }
        }
        internal class NotificationSpecification
        {
            public DeadClientNotification Callback { get; set; }
            public int Period { get; set; }
            private IDictionary<string, DeadClientNotificationTask> _tasks;

            public NotificationSpecification()
            {
                _tasks = new Dictionary<string, DeadClientNotificationTask>();
            }

            public void AddNotificationSpecificationTask(string clientId, DeadClientNotificationTask task)
            {
                lock (this)
                {
                    if (_tasks != null)
                    {
                        if (!_tasks.ContainsKey(clientId))
                            _tasks.Add(clientId, task);
                    }
                }
            }

            public bool ContainsKey(string clientId)
            {
                lock (this)
                {
                    return _tasks.ContainsKey(clientId);

                }
            }

            public void RemoveNotificationSpecificationTask(string clientId)
            {
                lock (this)
                {
                    if (_tasks != null && _tasks.ContainsKey(clientId))
                    {
                        _tasks.Remove(clientId);
                    }
                }
            }

            public DeadClientNotificationTask GetNotificationSpecificationTask(string clientId)
            {
                DeadClientNotificationTask task = null;
                lock (this)
                {
                    if (_tasks != null)
                    {
                        _tasks.TryGetValue(clientId, out task);
                    }
                    return task;
                }
            }

        }

        internal class DeadClientNotificationTask : TimeScheduler.Task
        {
            ConnectedClientsLedger _parent;
            NotificationSpecification _specification;
            ClientInfo _info;
            string _clientId;
            long _gracePeriod;
            bool _cancelled;

            public DeadClientNotificationTask(ConnectedClientsLedger parent, string clientId, DateTime disconnectTime, NotificationSpecification specification, ClientInfo info)
            {
                _gracePeriod =
                    Convert.ToInt32(
                        Math.Ceiling(
                            (disconnectTime.AddSeconds(specification.Period) - disconnectTime).TotalMilliseconds));

                _parent = parent;
                _clientId = clientId;
                _specification = specification;
                _info = info;
            }

            public void Cancel()
            {
                lock (this)
                    _cancelled = true;
            }

            public bool IsCancelled()
            {
                return _cancelled;
            }

            public long GetNextInterval()
            {
                return _gracePeriod;
            }

            public void Run()
            {
                lock (this)
                {
                    if (!_cancelled && !_parent.IsClientConnected(_clientId))
                    {
                        Cancel();
                        _specification.RemoveNotificationSpecificationTask(_clientId);
                        _specification.Callback.Invoke(_clientId, _info);
                    }
                    else
                    {
                        Cancel();
                        _specification.RemoveNotificationSpecificationTask(_clientId);
                    }
                }
            }
        }

        const string ClientDeathDetection = "$Detection_ClientDeath$";
        const string ClientDeathNotification = "$Notification_ClientDeath$";

        private static TimeScheduler s_scheduler;

        IDictionary<string, ConnectedClient> _connectedClients;
        readonly object _lock = new object();
        Dictionary<string, NotificationSpecification> _specifications;

        static ConnectedClientsLedger() { s_scheduler = new TimeScheduler(true); }
        public ConnectedClientsLedger()
        {
            _connectedClients = new HashVector<string, ConnectedClient>();
            _specifications = new Dictionary<string, NotificationSpecification>();
        }

        internal void AddClientDeathNotificationSpecification(NotificationSpecification specification)
        {

            _specifications.Add(ClientDeathNotification, specification);
        }

        internal void RemoveClientDeathNotificationSpecification()
        {
            _specifications.Remove(ClientDeathNotification);
        }

        internal bool IsClientDeathNotificationSpecified
        {
            get { return _specifications.ContainsKey(ClientDeathNotification); }
        }

        internal void AddClientDeathDetectionSpecification(NotificationSpecification specification)
        {

            _specifications.Add(ClientDeathDetection, specification);
        }

        internal void RemoveClientDeathDetectionSpecification()
        {
            _specifications.Remove(ClientDeathDetection);
        }

        internal bool IsClientDeathDetectionSpecified
        {
            get { return _specifications.ContainsKey(ClientDeathDetection); }
        }

        public void ClientConnected(string clientId, ClientInfo clientInfo, Address connectedNode)
        {
            ConnectedClient client;
            DeadClientNotificationTask task;
            bool raiseNotification = true;
            lock (_lock)
            {
                if (_connectedClients.ContainsKey(clientId))
                    raiseNotification = false;

                foreach (var notificationSpecification in _specifications)
                {
                    task = notificationSpecification.Value.GetNotificationSpecificationTask(clientId);
                    if (task != null)
                    {
                        raiseNotification = task.IsCancelled();
                        task.Cancel();
                        notificationSpecification.Value.RemoveNotificationSpecificationTask(clientId);
                    }
                }

                if (!_connectedClients.TryGetValue(clientId, out client))
                {
                    if (!raiseNotification)
                    {
                        clientInfo = (ClientInfo)clientInfo.Clone();
                    }

                    clientInfo.Status = ConnectivityStatus.Connected;

                    client = new ConnectedClient();
                    client.ClientId = clientId;
                    client.Info = clientInfo;

                    _connectedClients.Add(clientId, client);
                }
                client.AddNode(connectedNode);
            }
        }

        public void SynchronizeConnectedClients(IDictionary<string, ClientInfo> clients, Address node)
        {
            ConnectedClient client;
            lock (_lock)
            {
                foreach (var clientInfo in clients)
                {
                    if (!_connectedClients.TryGetValue(clientInfo.Key, out client))
                    {
                        client = new ConnectedClient();
                        client.ClientId = clientInfo.Key;
                        client.Info = clientInfo.Value;
                        _connectedClients.Add(clientInfo.Key, client);
                    }
                    client.AddNode(node);
                }
            }
        }

        /// <summary>
        /// returns true if the client is declared dead by all nodes
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="node"></param>
        /// <param name="disconnectTime"></param>
        /// <returns></returns>
        public bool ClientDisconnected(string clientId, Address node, DateTime disconnectTime)
        {
            lock (_lock)
            {
                return ClientDisconnectedInternal(clientId, node, disconnectTime);
            }
        }

        /// <summary>
        /// returns true if the client is declared dead by all nodes
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="node"></param>
        /// <param name="disconnectTime"></param>
        /// <returns></returns>
        private bool ClientDisconnectedInternal(string clientId, Address node, DateTime disconnectTime)
        {
            ConnectedClient client;
            if (_connectedClients.TryGetValue(clientId, out client))
            {
                if (client.RemoveNode(node) == 0)
                {
                    _connectedClients.Remove(clientId);
                    foreach (var notificationSpecification in _specifications)
                    {
                        if (!notificationSpecification.Value.ContainsKey(clientId))
                        {
                            var task = new DeadClientNotificationTask(this, clientId, disconnectTime,
                                notificationSpecification.Value, client.Info);
                            notificationSpecification.Value.AddNotificationSpecificationTask(clientId, task);
                            s_scheduler.AddTask(task);
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        

        /// <summary>
        /// returns the list of clients that have been considered dead by all nodes
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="node"></param>
        /// <param name="disconnectTime"></param>
        /// <returns></returns>
        public List<string> ClientsDisconnected(ArrayList clients, Address node, DateTime disconnectTime)
        {
            List<string> deadClientIds = new List<string>(); 
            lock (_lock)
            {
                foreach (string client in clients)
                {
                    if(ClientDisconnectedInternal(client, node, disconnectTime))
                        deadClientIds.Add(client);
                }
            }
            return deadClientIds;
        }

        public void NodeLeft(Address node, DateTime leaveTime)
        {
            HashVector<string, ConnectedClient> deadClients = new HashVector<string, ConnectedClient>();
            lock (_lock)
            {
                foreach (var connectedClient in _connectedClients)
                {
                    if (connectedClient.Value.RemoveNode(node) == 0)
                    {
                        deadClients.Add(connectedClient.Key, connectedClient.Value);
                    }
                }
                if (deadClients.Count > 0)
                {
                    foreach (var deadClient in deadClients)
                    {
                        _connectedClients.Remove(deadClient.Key);
                        foreach (var notificationSpecification in _specifications)
                        {
                            var task = new DeadClientNotificationTask(this, deadClient.Key, leaveTime,
                                notificationSpecification.Value, deadClient.Value.Info);
                            notificationSpecification.Value.AddNotificationSpecificationTask(deadClient.Key, task);
                            s_scheduler.AddTask(task);
                        }
                    }
                }
            }
        }

        public bool IsClientConnected(string clientId)
        {
            lock (_lock)
            {
                return _connectedClients.ContainsKey(clientId);
            }
        }

        public bool IsClientPotentiallyDead(string clientId)
        {
            DeadClientNotificationTask task;
            lock (_lock)
            {
                foreach (var notificationSpecification in _specifications)
                {
                    task = notificationSpecification.Value.GetNotificationSpecificationTask(clientId);
                    if (task != null)
                    {
                        if (!task.IsCancelled())
                            return true;
                    }
                }
            }
            return false;
        }

        public bool IsClientConnectedOrPotentiallyDead(string clientId)
        {
            lock (_lock)
            {
                if (_connectedClients.ContainsKey(clientId))
                    return true;
                DeadClientNotificationTask task;
                foreach (var notificationSpecification in _specifications)
                {
                    task = notificationSpecification.Value.GetNotificationSpecificationTask(clientId);
                    if (task != null)
                    {
                        if (!task.IsCancelled())
                            return true;
                    }
                }
            }
            return false;
        }

        public bool GetConnectedClientInfo(string clientId, out ClientInfo info)
        {
            ConnectedClient client;
            lock (_lock)
            {
                if (_connectedClients.TryGetValue(clientId, out client))
                {
                    info = client.Info;
                    return true;
                }

            }
            info = null;
            return false;
        }

        public IDictionary<string, ClientInfo> GetAllConnectedClientInfos()
        {
            var connectedClientInfos = new HashVector<string, ClientInfo>();
            lock (_lock)
            {
                foreach (var connectedClient in _connectedClients)
                {
                    connectedClientInfos.Add(connectedClient.Key, connectedClient.Value.Info);
                }
            }
            return connectedClientInfos;
        }
    }
}
