//  Copyright (c) 2019 Alachisoft
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
using System.Collections;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// A class that helps balancing calls across cluster members.
    /// </summary>
    internal class PORCallBalancer
    {
        /// <summary> Call balancing variable, index of the server last contacted. </summary>
        private int _lastServ = 0;

        /// <summary>
        /// Return the next node in call balacing order that is fully functional.
        /// </summary>
        /// <returns></returns>
        internal Address SelectNode(ClusterCacheStatistics clusterStats, SubCluster targetGroup, Address localAddress)
        {
            ArrayList servers = targetGroup.Servers;
            ArrayList memberInfos = clusterStats.Nodes;

            //lock (servers.SyncRoot)
            {
                int maxtries = servers.Count;
                Address address = null;

                //if local node participates as a backup in the 
                //target group then return the local node.
                foreach (Address node in servers)
                {
                    if (node.IpAddress.Equals(localAddress.IpAddress))
                    {
                        if (IsNodeRunning(node, memberInfos))
                            return node;
                    }
                }

                //if local node could not be selected then 
                //do the call balancing.
                do
                {
                    address = (Address)servers[_lastServ % servers.Count];
                    _lastServ = ++_lastServ % servers.Count;
                    if (IsNodeRunning(address, memberInfos))
                    {
                        return address;
                    }
                    maxtries--;
                }
                while (maxtries > 0);
            }
            return null;
        }

        private bool IsNodeRunning(Address node, ArrayList nodeInfos)
        {
            foreach (NodeInfo nodeInfo in nodeInfos)
            {
                if (nodeInfo.Address.CompareTo(node) == 0)
                    return nodeInfo.Status.IsAnyBitSet(NodeStatus.Running);
                continue;
            }
            return false;
        }
    }
}