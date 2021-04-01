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
#if !NETCORE
using System;
using System.Collections;
using System.Management.Instrumentation;

namespace Alachisoft.NCache.Instrumentation
{
    [InstrumentationClass(InstrumentationType.Instance)]
    public class WMINodeStats : System.Management.Instrumentation.Instrumentation
    {
        private     string      _processID;             //the process ID assigned to NCache Service or some user program incase of INProc Cache
        //private     string      _nodeId;                //unique id of node. In this case it will be ip address and port combined
        private     string      _nodeName;              //Name of the current node
        private     DateTime    _nodeUpTime;            //time at which the node was started
        private     float       _addSec=0;              //Avg no of items addded on current node per sec
        private     float       _remvSec=0;             //Avg no of items removed from current node per sec
        private     float       _insertSec = 0;         //Avg no of items updated on current node per sec
        private     float       _getSec = 0;            //Avg no of items retrieved from current node per sec
        private     ulong       _hitsSec=0;             //Avg no of requests recieved on current node per sec
        private     ulong       _missesSec=0;           //Avg no of requests not fullfiled on current node per sec
        private     int         _totalClients=0;        //Number of Clients connected to the current node
        private     Hashtable   _clientnodes = new Hashtable(); //List of client nodes connected to the current server node

        private float _instantAdds = 0;                  //Number of adds in the current sec
        private float _instantGets = 0;                  //Number of Gets in the current sec
        private float _instantUpdates = 0;               //Number of Updates in the current sec                  
        private float _instantRemoves = 0;               //Number of Removes in the current sec
        private float _instantHits = 0;                  //Number of Hits in the current sec
        private float _instantMisses = 0;                //Number of Misses in the current sec
        private float _clusterOpsPerSec = 0;
        private float _bytesSentPerSec = 0;
        private float _bytesRecievedPerSec = 0;
        private float _requestSentPerSec = 0;
        private float _evictionsPerSec = 0;
        private float _expirationPerSec = 0;
        private float _stateTransferPerSec = 0;
        private float _dataBalancePerSec = 0;
        private float _clientsRequestsPerSec = 0;
        private float _clientsBytesSendPerSec = 0;
        private float _clientsBytesRecievedPerSec = 0;
        private float _mirrorQueueCount = 0;

        /// operations time
        private float _addTime;
        private float _getTime;
        private float _insertTime;
        private float _removeTime;

        private int _serverPort;                         //Current server port on which the NCache service is listening
        private string _partitionId = string.Empty;
        private ulong _totalNodeOperations = 0;          //Total operations performed on the current node
        private ulong _dataSize = 0;                     //Size of data stored by the current cache
        /// <summary>
        /// Making this Node object available to the WMI Service
        /// </summary>
        public WMINodeStats()
        {
            try
            {
                /* Commented to stop posting on WMI */
                System.Management.Instrumentation.Instrumentation.Publish(this);
            }
            catch { }
        }

        /// <summary>
        /// Initializing and making available this object to WMI service
        /// </summary>
        /// <param name="nodeName"></param>
        /// <param name="nodeupTime"></param>
        public WMINodeStats(string port, string nodeName, DateTime nodeupTime, int serverPort, bool isInproc)
        {
            try
            {
                _nodeName = nodeName + "." + port;
                _nodeUpTime = nodeupTime;
                _serverPort = serverPort;
                if (isInproc) _partitionId = _nodeName;
                else _partitionId = nodeName;
                _processID = Convert.ToString(System.Diagnostics.Process.GetCurrentProcess().Id);

                /* Commented to stop posting on WMI */
                System.Management.Instrumentation.Instrumentation.Publish(this);
            }
            catch { }
        }

        /// <summary>
        /// Initializing and making available this object to WMI service
        /// </summary>
        /// <param name="nodeName"></param>
        /// <param name="nodeupTime"></param>
        public WMINodeStats(string port, string nodeName, DateTime nodeupTime, int serverPort)
        {
            try
            {
                _nodeName = nodeName + "." + port;
                _nodeUpTime = nodeupTime;
                _serverPort = serverPort;                
                _processID = Convert.ToString(System.Diagnostics.Process.GetCurrentProcess().Id);

                /* Commented to stop posting on WMI */
                System.Management.Instrumentation.Instrumentation.Publish(this);
            }
            catch { }
        }

        /// <summary>
        /// All the public members that are to be publised on WMI
        /// </summary>
#region Public variables
        [CLSCompliant(false)]
        public ulong ItemCount;              //Total items stored on the current node of the cluster    
        public string clusterName;            //Name of the cluster this node is part of 

        public string CurrentProcessID
        {
            get { return _processID; }
        }
        /// <summary>
        /// Name of the Node
        /// </summary>
        public string NodeName
        {
            get
            {
                return _nodeName;
            }
            set
            {
                _nodeName = value;
            }
        }

        /// <summary>
        /// The time at which the Node was started
        /// </summary>
        public DateTime NodeUpTime
        {
            get
            {
                return _nodeUpTime;
            }
        }

        public float InstantAdds
        {
            get { return _instantAdds; }
            set { _instantAdds = value; }
        }

        public float InstantGets
        {
            get { return _instantGets; }
            set { _instantGets = value; }
        }

        public float InstantRemoves
        {
            get { return _instantRemoves; }
            set { _instantRemoves = value; }
        }

        public float InstantUpdates
        {
            get { return _instantUpdates; }
            set { _instantUpdates = value; }
        }

        public float InstantHits
        {
            get { return _instantHits; }
            set { _instantHits = value; }
        }

        public float InstantMisses
        {
            get { return _instantMisses; }
            set { _instantMisses = value; }
        }

        public float AddTime
        {
            get { return this._addTime; }
            set { this._addTime = value; }
        }

        public float UpdateTime
        {
            get { return this._insertTime; }
            set { this._insertTime = value; }
        }

        public float GetTime
        {
            get { return this._getTime; }
            set { this._getTime = value; }
        }

        public float RemoveTime
        {
            get { return this._removeTime; }
            set { this._removeTime = value; }
        }

       

        /// <summary>
        /// Total # of Clients connected to the Cache on the current node
        /// </summary>
        public int TotalClients
        {
            get
            {
                return _totalClients;
            }

            set
            {
                _totalClients = value;
            }
        }

        public float ClusterOpsPerSec
        {
            get { return this._clusterOpsPerSec; }
            set { this._clusterOpsPerSec = value; }
        }

        public float ClusterBytesSentPerSec
        {
            get { return this._bytesSentPerSec; }
            set { this._bytesSentPerSec = value; }
        }

        public float ClusterBytesRecievedPerSec
        {
            get { return this._bytesRecievedPerSec; }
            set { this._bytesRecievedPerSec = value; }
        }


        public float EvictionsPerSec
        {
            get { return this._evictionsPerSec; }
            set { this._evictionsPerSec = value; }
        }

        public float ExpirationsPerSec
        {
            get { return this._expirationPerSec; }
            set { this._expirationPerSec = value; }
        }

        public float StateTransferPerSec
        {
            get { return this._stateTransferPerSec; }
            set { this._stateTransferPerSec = value; }
        }

        public float DataBalancedPerSec
        {
            get { return this._dataBalancePerSec; }
            set { this._dataBalancePerSec = value; }
        }

        public float ClientsBytesSentPerSec
        {
            get { return this._clientsBytesSendPerSec; }
            set { this._clientsBytesSendPerSec = value; }
        }

        public float ClientsBytesRecievedPerSec
        {
            get { return this._clientsBytesRecievedPerSec; }
            set { this._clientsBytesRecievedPerSec = value; }
        }

        public float ClientsRequestPerSec
        {
            get { return this._clientsRequestsPerSec; }
            set { this._clientsRequestsPerSec = value; }
        }

        public float MirrorQueueCount
        {
            get { return this._mirrorQueueCount; }
            set { this._mirrorQueueCount = value; }
        }

        public int ServerPort
        {
            set { _serverPort = value; }
            get { return _serverPort; }
        }

        public string PartitionId
        {
            set { _partitionId = value; }
            get { return _partitionId == null ? string.Empty : _partitionId; }
        }

        [CLSCompliant(false)]
        public ulong TotalOperations
        {
            set { _totalNodeOperations = value; }
            get { return _totalNodeOperations; }
        }

        [CLSCompliant(false)]
        public ulong DataSize
        {
            set { _dataSize = value; }
            get { return _dataSize; } 
        }


#endregion

        /// <summary>
        /// Removing the published object from the WMI repository, so that the current
        /// Object may not be visible over the network anymore
        /// </summary>
        public void Dispose()
        {
            try 
            {
                this.ClearClients();
                System.Management.Instrumentation.Instrumentation.Revoke(this); 
            }
            catch { }
        }

        /// <summary>
        /// Called each time a new Client gets connected to the current node
        /// </summary>
        /// <param name="Name"></param>
        public void AddClient(String Name) //ToDo Add stats parameter
        {
            try
            {
                lock (_clientnodes)
                {
                    if (_clientnodes.Contains(Name))
                        this.RemoveClientStatistics(Name);
                    
                    /* Commented to stop posting on WMI */
                    
                    _clientnodes.Add(Name, new ClientStats(Name, _nodeName, _serverPort, clusterName));
                }
            }
            catch { }
        }

        public void ClearClients()
        {
            try
            {
                //ClientStats removed;
                int _clntCount = _clientnodes.Count;
                lock (_clientnodes)
                {
                    foreach (DictionaryEntry de in _clientnodes)
                    {
                        try
                        {
                            ClientStats removed = (ClientStats)de.Value;
                            removed.Dispose();
                        }
                        catch { }
                    }
                    _clientnodes.Clear();
                }
            }
            catch { }
        }
        /// <summary>
        /// Called each time a client node leaves the cluster
        /// </summary>
        /// <param name="Name"></param>
        public void RemoveClientStatistics(String Name)
        {
            try
            {
                ClientStats remved;
                lock (_clientnodes)
                {
                    remved = (ClientStats)_clientnodes[Name];
                    remved.Dispose();

                    /* Commented to stop posting on WMI */
                    _clientnodes.Remove(Name);
                }
            }
            catch { }
        }

        /// <summary>
        /// Sets the amount data transferred by a client to the server node
        /// </summary>
        /// <param name="ClientID"></param>
        /// <param name="DataSize"></param>
        public void ClientDataRecived(string ClientID, long DataSize, IncommingOperationType OperationType, int OperationsCount)
        {
            try
            {
                _totalNodeOperations++;             // Incrementing the Current node operation count

                lock (_clientnodes)
                {
                    ClientStats client;
                    client = (ClientStats)_clientnodes[ClientID];
                    client.DataTransfered += (ulong)DataSize;
                    client.OperationsPerformed++;

                    switch (OperationType)
                    {
                        case IncommingOperationType.Add:
                        case IncommingOperationType.AddBulk:
                            client.TotalAddOperations = (ulong)OperationsCount;
                            break;

                        case IncommingOperationType.Get:
                        case IncommingOperationType.GetBulk:
                            client.TotalGetOperations = (ulong)OperationsCount;
                            break;

                        case IncommingOperationType.Insert:
                        case IncommingOperationType.InsertBulk:
                            client.TotalInsertOperations = (ulong)OperationsCount;
                            break;

                        case IncommingOperationType.Remove:
                        case IncommingOperationType.RemoveBulk:
                            client.TotalRemoveOperations = (ulong)OperationsCount;
                            break;
                    }
                }
            }
            catch { }
        }
    }
}
#endif