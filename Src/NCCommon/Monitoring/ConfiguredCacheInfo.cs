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
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public class ConfiguredCacheInfo: ICompactSerializable
    {
        private string _cacheId;
        private CacheTopology _topology;
        private bool _running;
        private long _dataCapacity;
        private string _cachePropString;
        private string _partId;
        private int _pid;
        private int _managementPort;

        public int ManagementPort
        {
            get { return _managementPort; }
            set { _managementPort = value; }
        }

        public int ProcessID
        {
            get { return _pid; }
            set { _pid = value; }
        }
        /// <summary>
        /// Gets the cache id
        /// </summary>
        public string CacheId
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        /// <summary>
        /// Gets/Sets the topology of the cache.
        /// </summary>
        public CacheTopology Topology
        {
            get { return _topology; }
            set { _topology = value; }
        }

        /// <summary>
        /// Gets/Sets the running status of cache.
        /// </summary>
        public bool IsRunning
        {
            get { return _running; }
            set { _running = value; }
        }

        /// <summary>
        /// Gets/Sets the data capacity of the cache in MB.
        /// </summary>
        public long DataCapacity
        {
            get { return _dataCapacity; }
            set { _dataCapacity = value; }
        }

        public string CachePropString
        {
            get { return _cachePropString; }
            set { _cachePropString = value; }
        }

        public string PartId
        {
            get { return _partId; }
            set { _partId = value; }
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _cacheId = reader.ReadObject() as string;
            _topology = (CacheTopology) reader.ReadInt32();
            _running = reader.ReadBoolean();
            _dataCapacity = reader.ReadInt64();
            _cachePropString = reader.ReadObject() as string;
            _partId = reader.ReadObject() as string;
            _managementPort = reader.ReadInt32();
            _pid = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_cacheId);
            writer.Write((int)_topology);
            writer.Write(_running);
            writer.Write(_dataCapacity);
            writer.WriteObject(_cachePropString);
            writer.WriteObject(_partId);
            writer.Write(_managementPort);
            writer.Write(_pid);
        }

        #endregion
    }
}
