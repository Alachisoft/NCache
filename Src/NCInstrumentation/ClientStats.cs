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
using System.Text;
using System.Management.Instrumentation;
using System.Management;


namespace Alachisoft.NCache.Instrumentation
{
    [InstrumentationClass(InstrumentationType.Instance)]
    public class ClientStats
    {
        string _clientName;             //Name of the Client node
        string _parentNode;             //Name of the Current cluster node
        string _clusterName;            //Name of the cluster this server node is part of
        int _serverPort;
        ulong _dataTransfered;           //Amount of data transferred by this node
        ulong _totalOperationsPerformed;      //Number of operations performed
        ulong _totalAdds;
        ulong _totalGets;
        ulong _totalInserts;
        ulong _totalRemoves;
        DateTime _clientUpTime;


        /// <summary>
        /// Initializing and making available the current client object to WMI
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="ParentNode"></param>
        /// <param name="Cluster"></param>
        public ClientStats(string Client, string ParentNode, int port, string Cluster)
        {
            try
            {
                _clientName = Client;
                _parentNode = ParentNode;                
                this._serverPort = port;
                _clusterName = Cluster;
                _dataTransfered = 0;
                _totalOperationsPerformed = 0;
                _clientUpTime = DateTime.Now;

                /* Commented to stop posting on WMI */
                System.Management.Instrumentation.Instrumentation.Publish(this);
            }
            catch { }
        }

        ~ClientStats()
        {

            /* Commented to stop posting on WMI */
            try { System.Management.Instrumentation.Instrumentation.Revoke(this); }
            catch { }
        }
        /// <summary>
        /// Public Data members
        /// </summary>
        #region Public Properties

        public string Client 
        { 
            get { return _clientName; } 
        }

        public DateTime ClientUpTime
        {
            get { return _clientUpTime; }
        }

        public string ParentNode 
        { 
            get { return _parentNode; } 
        }

        public int ServerPort
        {
            get { return this._serverPort; }
        }

        public string ClusterName 
        { 
            get { return _clusterName; } 
        }

        [CLSCompliant(false)]
        public ulong DataTransfered
        {
            set { _dataTransfered = value; }
            get { return _dataTransfered; }
        }

        [CLSCompliant(false)]
        public ulong OperationsPerformed
        {
            set { _totalOperationsPerformed = value; }
            get { return _totalOperationsPerformed; }
        }

        [CLSCompliant(false)]
        public ulong TotalAddOperations
        {
            set { _totalAdds += value; }
            get { return _totalAdds; }
        }

        [CLSCompliant(false)]
        public ulong TotalGetOperations
        {
            set { _totalGets += value; }
            get { return _totalGets; }
        }

        [CLSCompliant(false)]
        public ulong TotalRemoveOperations
        {
            set { _totalRemoves += value; }
            get { return _totalRemoves; }
        }

        [CLSCompliant(false)]
        public ulong TotalInsertOperations
        {
            set { _totalInserts += value; }
            get { return _totalInserts; }
        }
        #endregion

        /// <summary>
        /// Disposing and making this object unavailable 
        /// </summary>
        public void Dispose()
        {

            /* Commented to stop posting on WMI */
            try { System.Management.Instrumentation.Instrumentation.Revoke(this); }
            catch { }
        }
    }
}
#endif