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

using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
	/// A class that helps balancing calls across cluster members.
	/// </summary>
	class CallBalancer : IActivityDistributor
	{
		/// <summary> Call balancing variable, index of the server last contacted. </summary>
		private int _lastServ = 0;

		/// <summary>
		/// Return the next node in call balacing order that is fully functional.
		/// </summary>
		/// <returns></returns>
		NodeInfo IActivityDistributor.SelectNode(ClusterCacheStatistics clusterStats, object hint)
		{
			ArrayList memberInfos = clusterStats.Nodes;
			lock (memberInfos.SyncRoot)
			{
				int maxtries = memberInfos.Count;
				NodeInfo info = null;
				do
				{
					info = (NodeInfo)memberInfos[_lastServ % memberInfos.Count];
					_lastServ = ++_lastServ % memberInfos.Count;
					if (info.Status.IsAnyBitSet(NodeStatus.Running))
					{
						return info;
					}
					maxtries--;
				}
				while (maxtries > 0);
			}
			return null;
		}
	}
}
