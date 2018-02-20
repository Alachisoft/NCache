// Copyright (c) 2018 Alachisoft
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
using System;
using System.IO;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public class StateTxfrInfo : Runtime.Serialization.ICompactSerializable
    {
        public HashVector data;
        public bool transferCompleted;
        private bool _isMessageData;
        private long sendDataSize;
        private Stream stream;
        private bool _hasLoggedOperations;
    
        public StateTxfrInfo(bool transferCompleted)
        {
            this.transferCompleted = transferCompleted;
            data = null;
        }

        public StateTxfrInfo(HashVector data, bool transferCompleted, long dataSize, Stream st, bool isMessageData) 
        {
            this.data = data;
            this.transferCompleted = transferCompleted;
            sendDataSize = dataSize;
            stream = st;
            _isMessageData = isMessageData;
        }

        public Stream SerlizationStream
        {
            get { return stream; }
        }

        public long DataSize
        {
            get { return sendDataSize; }
        }

        public bool IsMessageData
        {
            get
            {
                return _isMessageData;
            }
        }

        public bool HasLoggedOperations
        {
            get { return _hasLoggedOperations; }
            set { _hasLoggedOperations = value; }
        }

        #region ICompactSerializable Members

        void Runtime.Serialization.ICompactSerializable.Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            data = (HashVector)reader.ReadObject();
            transferCompleted = reader.ReadBoolean();
            sendDataSize = reader.ReadInt64();
            _isMessageData = reader.ReadBoolean();
            _hasLoggedOperations = reader.ReadBoolean();
        }

        void Runtime.Serialization.ICompactSerializable.Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(data);
            writer.Write(transferCompleted);
            writer.Write(sendDataSize);
            writer.Write(IsMessageData);
            writer.Write(_hasLoggedOperations);
        }

        #endregion
    }
}