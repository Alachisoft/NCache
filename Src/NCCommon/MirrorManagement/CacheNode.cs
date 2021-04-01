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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Alachisoft.NCache.Common.Mirroring
{
    /// <summary>
    /// It serves as a node in the logical linked list of cache nodes in which
    /// each node's mirror is maintained on the next node in the list.
    /// 
    /// Note: Mirror of a node is knows as backup (next node in the list).
    /// </summary>
    [Serializable]
    public class CacheNode: ICloneable, ISerializable
    {
        static int sequenceSeed = 1;        
        string nodeId;
        string backupNodeId;
        string prevNodeId;
        int mySequence;

        private CacheNode() { }

        public CacheNode(string nodeId, int sequence)
        {
            this.nodeId = nodeId;
            this.mySequence = sequence;
        }

        protected CacheNode(SerializationInfo info, StreamingContext context)
        {
            nodeId = info.GetString("nodeId");
            backupNodeId = info.GetString("backupNodeId");
            prevNodeId = info.GetString("prevNodeId");
            mySequence = info.GetInt32("mySequence");
        }

        public int Sequence
        {
            get { return mySequence; }
        }

        public string NodeId
        {
            get { return nodeId; }
        }
        
        public string BackupNodeId
        {
            get { return backupNodeId; }
            set { backupNodeId = value; }
        }

        public string PreviousNodeId
        {
            get { return prevNodeId; }
            set { prevNodeId = value; }
        }

        public override string ToString()
        {
            return String.Format("Node: {0}, Backup: {1}", nodeId, backupNodeId);
        }

        #region ICloneable Members

        public object Clone()
        {
            CacheNode clone = new CacheNode();
            clone.nodeId = NodeId;
            clone.PreviousNodeId = PreviousNodeId;
            clone.BackupNodeId = BackupNodeId;
            clone.mySequence = Sequence;
            return clone;
        }

        #endregion

        #region ISerializable Members
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter =true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("nodeId", nodeId);
            info.AddValue("backupNodeId", backupNodeId);
            info.AddValue("prevNodeId", prevNodeId);
            info.AddValue("mySequence", mySequence);
        }

        #endregion
    }
}
