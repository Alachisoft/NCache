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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common.DataStructures
{
    /// <summary>
    /// Represents the actual status of the Replicator status.
    /// </summary>
    public class ReplicatorStatusInfo : ICompactSerializable
    {
        private bool _isConnected;
        private bool _isActive;
     

        public bool IsConnected
        {
            get {return _isConnected;}
            set { _isConnected = value; }
        }

        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; }
        }

     

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _isConnected = reader.ReadBoolean();
            _isActive = reader.ReadBoolean();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(IsConnected);
            writer.Write(_isActive);
        }

        #endregion
    }
}