// Copyright (c) 2017 Alachisoft
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Caching.Statistics;
using System.Collections;
using System.Threading;
using Alachisoft.NCache.Caching.Topologies;

namespace Alachisoft.NCache.Caching
{
    internal class ClientDeathDetectionMgr
    {
        /// <summary> Cache Impl instance </summary>
        private CacheBase _cacheImpl = null;
        /// <summary> Cache statistics </summary>
        private CacheStatistics _stats = null;
        /// <summary> List of suspected clients against their disconnect time </summary>
        private Dictionary<string, DateTime> _suspectedClients = new Dictionary<string, DateTime>();
        /// <summary> List of dead clients against their disconnect time</summary>
        private Dictionary<string, DateTime> _deadClients = new Dictionary<string, DateTime>();
        /// <summary> Configurable time after which client will be declared dead </summary>
        private int _gracePeriod;
        /// <summary> The worker thread. </summary>
        private Thread _worker = null;

        private Object syncRoot = new Object();
        public ClientDeathDetectionMgr(int gracePeriod) 
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
        /// <summary>
        /// Start monitoring
        /// </summary>
        internal void StartMonitoringClients()
        {
            if (this._worker == null)
            {
                this._worker = new Thread(new ThreadStart(this.MonitorDeadClients));
                this._worker.IsBackground = true;
                this._worker.Name = "Client-Death-Detection";
                this._worker.Start();
            }
        }

        private void MonitorDeadClients()
        {
            while (this._worker != null && this._worker.IsAlive)
            {
                ArrayList suspectedClients = new ArrayList();
                lock (syncRoot)
                {
                    if (this._deadClients.Count < 1)
                        Monitor.Wait(syncRoot);
                    Dictionary<string, DateTime> tempList = new Dictionary<string, DateTime>(_deadClients);
                    Dictionary<string, DateTime>.Enumerator ide = tempList.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        DateTime expireTime = ide.Current.Value.AddMilliseconds(this._gracePeriod);
                        if (expireTime < DateTime.Now) continue;
                        suspectedClients.Add(ide.Current.Key);
                        if (_deadClients.ContainsKey(ide.Current.Key)) 
                            lock (syncRoot) _deadClients.Remove(ide.Current.Key);
                    }
                }
                if (suspectedClients.Count == 0) continue;
                ArrayList deadClients = _cacheImpl.DetermineClientConnectivity(suspectedClients);
                _cacheImpl.DeclaredDeadClients(deadClients);
            }
        }

        private void RemoveFromDeadClients(ArrayList suspectedClients)
        {
            lock (syncRoot)
            {
                foreach (string client in suspectedClients)
                    _deadClients.Remove(client);
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
                lock (syncRoot)
                {
                    if (clientExist)
                    {
                        if (_stats.ClassName != "replicated-server")
                           _suspectedClients[client] = dcTime;
                    }
                    else
                    {
                        if (_suspectedClients.ContainsKey(client))
                            _suspectedClients.Remove(client);
                        _deadClients[client] = dcTime;
                        Monitor.Pulse(syncRoot);
                    }
                }
            }
            else
                lock (syncRoot)
                {
                    _deadClients[client] = dcTime;
                    Monitor.Pulse(syncRoot);
                }
        }

        internal void ClientConnected(string client)
        {
            if (_deadClients.ContainsKey(client))
            {
                lock (syncRoot)  _deadClients.Remove(client);
            }
            else if (_suspectedClients.ContainsKey(client))
            {
                if (_stats is ClusterCacheStatistics)
                {
                    bool clientExist = true;
                    ClusterCacheStatistics clusterStats = _stats as ClusterCacheStatistics;
                    foreach (NodeInfo node in clusterStats.Nodes)
                    {
                        if (!node.ConnectedClients.Contains(client))
                        {
                            clientExist = false;
                            break;
                        }
                    }
                    if (clientExist) lock (syncRoot) _suspectedClients.Remove(client);
                }  
            }
        }

        internal void Dispose()
        {
            if (this._worker != null && this._worker.IsAlive)
            {
                this._worker.Abort();
                this._worker = null;
            }
        }

        internal void UpdateClientStatus(ArrayList localNodeClients, ArrayList updatedNodeClients)
        {
            string[] tempLocalClient = (string[])localNodeClients.ToArray(typeof(string));
            string[] tempUpdatedClient = (string[])updatedNodeClients.ToArray(typeof(string));

            string[] missingClients = new string[1];
            missingClients = tempLocalClient.Intersect(tempUpdatedClient).ToArray();
            if (updatedNodeClients.Count > localNodeClients.Count)
                for (int i = 0; i < missingClients.Length; i++)
                    this.ClientConnected(missingClients[i]);
            else
                for (int i = 0; i < missingClients.Length; i++)
                    this.ClientDisconnected(missingClients[i],DateTime.Now);//client will be mark as suspected from now on.
        }
    }
}
