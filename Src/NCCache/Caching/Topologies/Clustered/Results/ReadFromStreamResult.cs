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
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Results
{
    internal class ReadFromStreamResult :ClusterOperationResult, ICompactSerializable
    {
        private VirtualArray _vBuffer;
        private int _bytesRead;

        public ReadFromStreamResult() { }

        public ReadFromStreamResult(VirtualArray vBuffer, int bytesRead,ClusterOperationResult.Result result):base(result)
        {
            _vBuffer = vBuffer;
            _bytesRead = bytesRead;
        }

        public VirtualArray Buffer
        {
            get { return _vBuffer; }
            set { _vBuffer = value; }
        }

        public int BytesRead
        {
            get { return _bytesRead; }
            set { _bytesRead = value; }
        }



        #region ICompactSerializable Members

        public new void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _vBuffer = reader.ReadObject() as VirtualArray;
            _bytesRead = reader.ReadInt32();

        }

        public new void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.WriteObject(_vBuffer);
            writer.Write(_bytesRead);
        }

        #endregion
    }
}
