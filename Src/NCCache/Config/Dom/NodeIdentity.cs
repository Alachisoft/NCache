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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class NodeIdentity:ICompactSerializable
    {
        private string _nodeName;
        private string _partitionId;
        private int _nodePriority = 1;

        public NodeIdentity(string nodeName, string partitionId)
        {
            _nodeName = nodeName;
            _partitionId = partitionId;
        }

        public NodeIdentity(string nodeName, string partitionId, int nodePriority)
            : this(nodeName, partitionId)
        {
            _nodePriority = nodePriority;
        }

        public string NodeName
        {
            get { return _nodeName; }
            set { _nodeName = value; }
        }

        public string PartitionId
        {
            get { return _partitionId; }
            set { _partitionId = value; }
        }

        public int NodePriority
        {
            get { return _nodePriority; }
            set { _nodePriority = value; }
        }

        /// <summary>
        /// if the nodeIdentity contains an IpAddress instead of the node name
        /// it returns an equalent node indentity with the node name.
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public NodeIdentity Resolve()
        {
            if (DnsCache.ResolveAddress(this.NodeName) != null)
            {
                string nodeName = DnsCache.ResolveAddress(this.NodeName);
                return new NodeIdentity(nodeName, this.PartitionId);
            }
            return this;
        }

        public override bool Equals(object obj)
        {
            if (obj is NodeIdentity)
            {
                NodeIdentity other = obj as NodeIdentity;

                if (PartitionId == null)
                    PartitionId = string.Empty;

                if (other.PartitionId == null)
                    other.PartitionId = string.Empty;

                if (other.NodeName == NodeName)
                {
                    if (other.PartitionId != null && PartitionId != null)
                        return other.PartitionId.Equals(PartitionId);

                    return false;
                }
            }
            else if (obj is string)
            {
                return ((string)obj == NodeName);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return  _nodeName != null ? _nodeName.GetHashCode() : base.GetHashCode();
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("Node Name : " + NodeName);
            sb.Append(", Partition Id : ");
            sb.Append(PartitionId == null ? "null" : PartitionId);
            return sb.ToString();
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _nodeName = reader.ReadObject() as string;
            _partitionId = reader.ReadObject() as string;
            _nodePriority = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_nodeName);
            writer.WriteObject(_partitionId);
            writer.Write(_nodePriority);
        }

        #endregion
    }
}
