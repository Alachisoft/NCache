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
using Alachisoft.NCache.Caching.Statistics;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// The base class for load balancers. Sorts the nodes in the order of 
    /// preferred load balancing algorithm.
    /// </summary>
    interface IActivityDistributor
    {
        /// <summary>
        /// Returns an ordered list of nodes, based upon the preferred order of
        /// load balancing algorithm. 
        /// </summary>
        /// <param name="memberInfo">collected information about all the server nodes</param>
        /// <param name="data">Any data that may be required</param>
        /// <returns>ordered list of server nodes</returns>
        NodeInfo SelectNode(ClusterCacheStatistics clusterStats, object hint);
    }
}