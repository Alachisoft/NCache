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
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common.Events
{
    public class PollingResult : ICompactSerializable
    {
        ClusteredList<string> _removedKeys = new ClusteredList<string>();
        ClusteredList<string> _updatedKeys = new ClusteredList<string>();

        public ClusteredList<string> RemovedKeys
        {
            get { return _removedKeys; }
            set { _removedKeys = value; }
        }

        public ClusteredList<string> UpdatedKeys
        {
            get { return _updatedKeys; }
            set { _updatedKeys = value; }
        }

       
        public void Deserialize(CompactReader reader)
        {
            _removedKeys = SerializationUtility.DeserializeClusteredList<string>(reader);
            _updatedKeys = SerializationUtility.DeserializeClusteredList<string>(reader);
            
        }

        public void Serialize(CompactWriter writer)
        {
            SerializationUtility.SerializeClusteredList(_removedKeys, writer);
            SerializationUtility.SerializeClusteredList(_updatedKeys, writer);
        }
    }
}
