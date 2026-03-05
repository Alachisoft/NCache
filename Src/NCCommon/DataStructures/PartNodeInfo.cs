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
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Newtonsoft.Json;

namespace Alachisoft.NCache.Common.DataStructures
{

    public class PartNodeInfo : ICompactSerializable
    {
        Address _address;
        String _subGroupId;
        bool _isCoordinator;
        int _priorityIndex;

        public PartNodeInfo()
        {
            _address = new Address();
            _subGroupId = "";
            _isCoordinator = false;
            _priorityIndex = -1;
        }

        public PartNodeInfo(Address address, string subGroup, bool isCoordinator)
        {
            _address = address;
            _subGroupId = subGroup;
            _isCoordinator = isCoordinator;
        }

        [JsonProperty("NodeAddress")]
        public Address NodeAddress
        {
            get { return _address; }
            set { _address = value; }
        }
        [JsonProperty("SubGroup")]
        public string SubGroup
        {
            get { return _subGroupId; }
            set { _subGroupId = value; }
        }
        [JsonProperty("IsCoordinator")]
        public bool IsCoordinator
        {
            get { return _isCoordinator; }
            set { _isCoordinator = value; }
        }
        [JsonProperty("PriorityIndex")]
        public int PriorityIndex
        {
            get { return _priorityIndex; }
            set { _priorityIndex = value; }
        }

        public void Deserialize(CompactReader reader)
        {
            _address = reader.ReadObject() as Address;
            _subGroupId = reader.ReadString();
            _isCoordinator = reader.ReadBoolean();
            _priorityIndex = reader.ReadInt32();
        }

        public override bool Equals(object obj)
        {
            if (obj is PartNodeInfo)
            {
                PartNodeInfo other = (PartNodeInfo)obj;
                if ((this.NodeAddress.Equals(other.NodeAddress)) && this._subGroupId == other._subGroupId)
                    return true;
            }
            return false;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_address);
            writer.Write(_subGroupId);
            writer.Write(_isCoordinator);
            writer.Write(_priorityIndex);
        }

        public override string ToString()
        {
            return "PartNodeInfo(" + NodeAddress.ToString() + ", " + SubGroup + "," + IsCoordinator.ToString() + ")";
        }
    }
}