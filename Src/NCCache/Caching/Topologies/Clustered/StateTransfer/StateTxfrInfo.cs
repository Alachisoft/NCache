//  Copyright (c) 2019 Alachisoft
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
using System.IO;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public enum DataType : byte
    {
        None,
        LoggedOperations,
        CacheItems,
        CollectionItems,
        Messages
    }

    public class StateTxfrInfo : Runtime.Serialization.ICompactSerializable
    {
        public HashVector data;
        public bool transferCompleted;
        private DataType _dataType;
        private long sendDataSize;
        private Stream stream;
        
        public StateTxfrInfo(bool transferCompleted)
        {
            this.transferCompleted = transferCompleted;
            data = null;
        }

        public StateTxfrInfo(HashVector data, bool transferCompleted, long dataSize, Stream st, DataType dataType)
        {
            this.data = data;
            this.transferCompleted = transferCompleted;
            sendDataSize = dataSize;
            stream = st;
            _dataType = dataType;
        }

        public Stream SerlizationStream
        {
            get { return stream; }
        }

        public long DataSize
        {
            get { return sendDataSize; }
        }

        public DataType DataType
        {
            get
            {
                return _dataType;
            }
        }

        #region ICompactSerializable Members

        void Runtime.Serialization.ICompactSerializable.Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            data = (HashVector)reader.ReadObject();
            transferCompleted = reader.ReadBoolean();
            sendDataSize = reader.ReadInt64();
            _dataType = (DataType)reader.ReadByte();
        }

        void Runtime.Serialization.ICompactSerializable.Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(data);
            writer.Write(transferCompleted);
            writer.Write(sendDataSize);
            writer.Write((byte)DataType);
        }

        #endregion
    }
}