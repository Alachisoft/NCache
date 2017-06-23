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

using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public class StateTxfrInfo : Runtime.Serialization.ICompactSerializable
    {
        public HashVector data;
        public bool transferCompleted;
        private long sendDataSize;
        private Stream stream;

        public StateTxfrInfo(bool transferCompleted)
        {
            this.transferCompleted = transferCompleted;
            data = null;
        }

        public StateTxfrInfo(HashVector data, bool transferCompleted, long dataSize, Stream st)
        {
            this.data = data;
            this.transferCompleted = transferCompleted;
            this.sendDataSize = dataSize;
            this.stream = st;
        }

        public Stream SerlizationStream
        {
            get { return this.stream; }
        }

        public long DataSize
        {
            get { return sendDataSize; }
        }
       
        #region ICompactSerializable Members

        void Runtime.Serialization.ICompactSerializable.Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            data = (HashVector)reader.ReadObject();
            transferCompleted = reader.ReadBoolean();
            this.sendDataSize = reader.ReadInt64();
        }

        void Runtime.Serialization.ICompactSerializable.Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(data);
            writer.Write(transferCompleted);
            writer.Write(this.sendDataSize);
        }

        #endregion
    }
}
