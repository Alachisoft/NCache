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

namespace Alachisoft.NCache.Common.Mirroring
{
    /// <summary>
    /// It represents a cache node.
    /// <para>
    /// The properties NodeGroup and MirrorGroup give the group id of the active
    /// and mirror cache on the same node.
    /// 
    /// Note: Mirror on the same node belongs to some other active node hence group id
    /// for both are different for a single node.
    /// </para>
    /// </summary>
    public class GroupInfo
    {
        string nodeGroup;
        string mirrorGroup;

        /// <summary>
        /// Group id for the active cache on the node
        /// </summary>
        public string NodeGroup
        {
            get { return nodeGroup; }
            set { nodeGroup = value; }
        }

        /// <summary>
        /// Group id for the mirror cache on the node
        /// </summary>
        public string MirrorGroup
        {
            get { return mirrorGroup; }
            set { mirrorGroup = value; }
        }

        public GroupInfo() { }

        public GroupInfo(string nodeGroup, string mirrorGroup)
        {
            this.nodeGroup = nodeGroup;
            this.mirrorGroup = mirrorGroup;
        }

        public override string ToString()
        {
            return String.Format("Group = {0}, Mirror = {1}", nodeGroup, mirrorGroup);
        }
    }
}
