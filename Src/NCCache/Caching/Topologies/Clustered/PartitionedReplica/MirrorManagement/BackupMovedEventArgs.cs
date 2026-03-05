//  Copyright (c) 2026 Alachisoft
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
using Alachisoft.NCache.Common.Mirroring;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Mirroring
{
    public class BackupMovedEventArgs : EventArgs
    {
        CacheNode node;
        string lastBackup;

        /// <summary>
        /// This nodes backup has changed or moved.
        /// </summary>
        public CacheNode AffectedNode
        {
            get { return node; }
        }

        /// <summary>
        /// The Last backupNodeId where this nodes mirror existed and is now changed.
        /// </summary>
        public string LastBackup
        {
            get { return lastBackup; }
        }

        public BackupMovedEventArgs(CacheNode affectedNode, string lastBackup)
        {
            node = affectedNode;
            this.lastBackup = lastBackup;
        }
    }
}