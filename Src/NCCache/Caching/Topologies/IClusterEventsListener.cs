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
namespace Alachisoft.NCache.Caching.Topologies
{
    /// <summary>
    /// Events callback interface used by the listeners of Cluster events. 
    /// </summary>
    public interface IClusterEventsListener
    {
        /// <summary>
        /// Fired when a new node joins the cluster.
        /// </summary>
        /// <param name="clusterAddress">The cluster IP and Port of the newly joined node</param>
        /// <param name="serverAddress">The server IP and Port of the newly joined node</param>
        void OnMemberJoined(Alachisoft.NCache.Common.Net.Address clusterAddress, Alachisoft.NCache.Common.Net.Address serverAddress);

        /// <summary>
        /// Fired when a node leaves the cluster.
        /// </summary>
        /// <param name="clusterAddress">The cluster IP and Port of the leaving node</param>
        /// <param name="serverAddress">The server IP and Port of the leaving node</param>
        void OnMemberLeft(Alachisoft.NCache.Common.Net.Address clusterAddress, Alachisoft.NCache.Common.Net.Address serverAddress);
    }
}