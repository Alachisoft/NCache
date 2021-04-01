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
using System.Collections.Generic;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Threading;

namespace Alachisoft.NCache.Caching
{
    internal class ClientDeathDetector
    {
        private class DeadClientNotificationTask : TimeScheduler.Task
        {
            ClientDeathDetector _parent;
            string _clientId;
            long _gracePeriod;
            bool _cancelled = false;

            public DeadClientNotificationTask(ClientDeathDetector parent, string clientId, DateTime disconnectTime)
            {
                _gracePeriod =
                    Convert.ToInt32(
                        Math.Ceiling(
                            (disconnectTime.AddMilliseconds(parent._gracePeriod) - disconnectTime).TotalMilliseconds));

                _parent = parent;
                _clientId = clientId;
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
                if (!_cancelled && !_parent._disposed && !_parent._cacheImpl.IsClientConnected(_clientId))
                {
                    Cancel();
                    lock (_parent.syncRoot)
                        _parent._deadClientFinalizers.Remove(_clientId);

                    _parent._notifier.Invoke(_clientId);
                }
                else
                {
                    Cancel();
                }
            }
        }

        private static TimeScheduler s_scheduler;
        static ClientDeathDetector() { s_scheduler = new TimeScheduler(true); }
        /// <summary> Cache Impl instance </summary>
        private CacheBase _cacheImpl = null;
        /// <summary> Cache statistics </summary>
        private CacheStatistics _stats = null;
        /// <summary> Configurable time after which client will be declared dead </summary>
        private int _gracePeriod;
        private IDictionary<string, DeadClientNotificationTask> _deadClientFinalizers = new HashVector<string, DeadClientNotificationTask>();
        bool _disposed;
        private readonly object syncRoot = new object();

        private DeadClientNotification _notifier;
        public ClientDeathDetector(int gracePeriod)
        {
            _gracePeriod = gracePeriod * 1000;
        }

        public CacheBase CacheImpl
        {
            get
            {
                return _cacheImpl;
            }
            set
            {
                _cacheImpl = value;
                _stats = value.Statistics;

            }
        }

        public event DeadClientNotification OnDeadClientsDetected
        {
            add
            {
                lock (this)
                {
                    _notifier += value;
                }
            }
            remove
            {
                lock (this)
                {
                    _notifier -= value;
                }
            }
        }

        internal void ClientDisconnected(string client, DateTime dcTime)
        {
            if (_stats == null) return;

            if (_stats is ClusterCacheStatistics && _stats.ClassName != "mirror-server")
            {
                bool clientExist = false;
                ClusterCacheStatistics clusterStats = _stats as ClusterCacheStatistics;
                foreach (NodeInfo node in clusterStats.Nodes)
                {
                    if (node.ConnectedClients.Contains(client))
                    {
                        clientExist = true;
                        break;
                    }
                }
                if (!clientExist)
                    CreateNewDeadClientFinalizer(client, dcTime);
            }
            else
                CreateNewDeadClientFinalizer(client, dcTime);
        }

        private void CreateNewDeadClientFinalizer(string clientId, DateTime time)
        {
            DeadClientNotificationTask finalizer = new DeadClientNotificationTask(this, clientId, time);
            lock (syncRoot)
                _deadClientFinalizers.Add(clientId, finalizer);

            s_scheduler.AddTask(finalizer);
        }

        internal void ClientConnected(string client)
        {
            DeadClientNotificationTask finalizer;
            lock (syncRoot)
            {
                if (_deadClientFinalizers.TryGetValue(client, out finalizer))
                    finalizer.Cancel();
            }
        }

        internal void Dispose()
        {
            _disposed = true;
        }
    }
}
