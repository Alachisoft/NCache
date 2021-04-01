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

using System.IO;

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
    /// <summary>
    /// Data for state transfer of Mirror and Replicated topology.
    /// </summary>
    public class ReplicaStateTxfrInfo : Runtime.Serialization.ICompactSerializable
    {
        private DataType _dataType;
        private long sendDataSize;
        private Stream stream;
        public string key;
        public object data;
        public bool transferCompleted;

        public ReplicaStateTxfrInfo(bool transferCompleted)
        {
            this.transferCompleted = transferCompleted;
            data = null;
        }

        public ReplicaStateTxfrInfo(string key, object data, bool transferCompleted, long dataSize, Stream st, DataType dataType)
        {
            this.key = key;
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
            key = reader.ReadObject() as string;
            data = reader.ReadObject();
            transferCompleted = reader.ReadBoolean();
            sendDataSize = reader.ReadInt64();
            _dataType = (DataType)reader.ReadByte();
        }

        void Runtime.Serialization.ICompactSerializable.Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(key);
            writer.WriteObject(data);
            writer.Write(transferCompleted);
            writer.Write(sendDataSize);
            writer.Write((byte)DataType);
        }

        #endregion
    }
}