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
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Mirroring;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    interface IDistributionPolicyMember
    {
        /// <summary>
        /// Called on the coordinator node when the view changes. 
        /// It returns the new hashmap.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="isNew"></param>
        /// <returns></returns>
        DistributionMaps GetDistributionMaps(DistributionInfoData distInfo);

        ArrayList HashMap
        {
            get;
            set;
        }

        Hashtable BucketsOwnershipMap
        {
            get;
            set;
        }

        void EmptyBucket(int bucketId);

        void InstallHashMap(DistributionMaps distributionMaps, ArrayList leftMbrs);

        void InstallMirrorMap(CacheNode[] nodes);
        CacheNode[] GetMirrorMap();

        /// <summary>
        /// Gets the groupId for this node from the mirrorManager. If nodeIdentity is a mirror
        /// cache instance then groupId is the ip/name of the group it belongs to. Otherwise 
        /// retruns Empty String. This only fetches the groupId if there is no GroupId for this
        /// node then Empty String is returned. </summary>
        /// <param name="affectedNode">The Address of the AffectedNode.</param>
        /// <param name="isStartedAsMirror">Whether this node joined as Mirror.</param>
        /// <returns>GroupId of the node. If there is</returns>
        string GetGroupId(Address affectedNode, bool isStartedAsMirror);
    }
}