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
using System.Collections;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// Does the load balancing based upon the number of objects contained at each node.
    /// </summary>
    class CoordinatorBiasedObjectCountBalancer : IActivityDistributor
    {

        string loggerName;
        //private NewTrace nTrace;
        ILogger _ncacheLog;

        ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }
        public CoordinatorBiasedObjectCountBalancer(CacheRuntimeContext context)
        {
            _ncacheLog = context.NCacheLog;
        }

        /// <summary>
        /// Returns an ordered list of nodes, based upon the preferred order of
        /// load balancing algorithm. 
        /// </summary>
        /// <param name="memberInfo">collected information about all the server nodes</param>
        /// <returns>ordered list of server nodes</returns>
        NodeInfo IActivityDistributor.SelectNode(ClusterCacheStatistics clusterStats, object data)
        {
            ArrayList memberInfos = clusterStats.Nodes;
            string group = data as string;
            bool gpAfStrict = false;
            NodeInfo min = null;
            NodeInfo gMin = null;
            NodeInfo sMin = null;
			
            lock (memberInfos.SyncRoot)
            {
                if (group != null)
                    gpAfStrict = clusterStats.ClusterDataAffinity != null ? clusterStats.ClusterDataAffinity.Contains(group) : false;

                for (int i = 0; i < memberInfos.Count; i++)
                {
                    NodeInfo curr = (NodeInfo) memberInfos[i];
                    if (curr.Status.IsAnyBitSet(NodeStatus.Coordinator | NodeStatus.SubCoordinator))
                    {
                        if (curr.Statistics == null) continue;

                        if(min == null || (curr.Statistics.Count < min.Statistics.Count)) 
                            min = curr;

                        if (curr.DataAffinity != null)
                        {
                            if (curr.DataAffinity.IsExists(group))
                            {
                                if (gMin == null || (curr.Statistics.Count < gMin.Statistics.Count))
                                    gMin = (NodeInfo)memberInfos[i];
                            }
                            else if (curr.DataAffinity.Strict == false)
                                sMin = min;
                            else
                                min = sMin;
                        }
                        else
                            sMin = min;
                    }
                }
            }

            if (gpAfStrict && gMin == null)
            {
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CoordinatorBiasedObjectCountBalancer.SelectNode", "strict group affinity, no node found to accomodate " + group + " data");
                return null;
            }
            return (gMin == null) ? sMin : gMin;
        }
    }
}