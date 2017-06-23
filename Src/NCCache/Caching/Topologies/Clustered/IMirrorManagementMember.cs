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

using Alachisoft.NCache.Common.Mirroring;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// Mirror Management Interface exposes the mirror management functionaly to the cluster services layer. 
    /// The Mirror Manager returns the map of cluster with respect to the joining opr leaving of new node. 
    /// </summary>
    interface IMirrorManagementMember
    {
        /// <summary>
        /// Updates the MirroMap with this new list of cacheNodes. The sequence Idis automatically adjusted.
        /// </summary>
        /// <param name="cacheNodes">List of CacheNodes</param>
        void UpdateMirrorMap(CacheNode[] cacheNodes);

        /// <summary>
        /// Called on the coordinator node when the view changes. 
        /// It returns the mirror map. be sure to add the new node before retreiving the Map.
        /// </summary>
        /// <returns>Returns the object containg the array of CacheNodes (<cref>CacheNode</cref>[]).
        /// </returns>
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

        /// <summary>
        /// Adds the new joinging node in the mirror manager. So mirror manager can rearrange 
        /// the mirrors. If this node is mirror then it only returns the group_id of this mirror.
        /// </summary>
        /// <param name="affectedNode">The new joining node.</param>
        /// <param name="isStartedAsMirror">Whether this node joined as Mirror.</param>
        /// <returns>The groupId this node belongs to. If this is the new joinging node and is 
        /// not mirror then the
        /// GroupId would be its name or Ip.</returns>
        string AddNode(Address affectedNode, bool isStartedAsMirror);

        /// <summary>
        /// Removes the node from the Mirror mapping. If it is a mirror instance then the mirror
        /// mapping is not affected.
        /// </summary>
        /// <param name="affectedNode">The affectedNode leaving the cluster.</param>
        /// <param name="isStartedAsMirror">Whether this node joined as Mirror.</param>
        /// <returns>The groupId this node belond to. Empty String or null if this node was not 
        /// in teh mirror mapping.</returns>
        string RemoveNode(Address affectedNode, bool isStartedAsMirror);
    }
}
