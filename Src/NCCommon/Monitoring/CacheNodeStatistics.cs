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
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public class CacheNodeStatistics: ICompactSerializable
    {
        private ServerNode _node;
        private long _itemCount;
        private long _dataSize;
        private ushort _clientsConnected;
        private long _totalCacheSize;
        private CacheNodeStatus _nodeStatus = CacheNodeStatus.Stopped;

        public CacheNodeStatistics(ServerNode node)
        {
            _node = node;
        }

        /// <summary>
        /// Gets/Sets the status of the cache node.
        /// </summary>
        public CacheNodeStatus Status
        {
            get { return _nodeStatus; }
            set { _nodeStatus = value; }
        }

        /// <summary>
        /// Gets/Sets the item count.
        /// </summary>
        public long ItemCount
        {
            get { return _itemCount; }
            set { _itemCount = value; }
        }

        /// <summary>
        /// Gets/Sets the data size on the cache server node.
        /// </summary>
        public long DataSize
        {
            get { return _dataSize; }
            set { _dataSize = value; }
        }

        /// <summary>
        /// Gets/Sets the total size on the cache server node.
        /// </summary>
        public long TotalCacheSize
        {
            get { return _totalCacheSize; }
            set { _totalCacheSize = value; }
        }

        /// <summary>
        /// Gets/Sets the no of clients connected to a serve node.
        /// </summary>
        public ushort ClientCount
        {
            get { return _clientsConnected; }
            set { _clientsConnected = value; }
        }

        public ServerNode Node
        {
            get { return _node; }
            set { _node = value; }
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _node = reader.ReadObject() as ServerNode;
            _itemCount = reader.ReadInt64();
            _dataSize = reader.ReadInt64();
            _clientsConnected = reader.ReadUInt16();
            _totalCacheSize = reader.ReadInt64();
            _nodeStatus = (CacheNodeStatus) reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_node);
            writer.Write(_itemCount);
            writer.Write(_dataSize);
            writer.Write(_clientsConnected);
            writer.Write(_totalCacheSize);
            writer.Write((int)_nodeStatus);
        }

        #endregion
    }
}
