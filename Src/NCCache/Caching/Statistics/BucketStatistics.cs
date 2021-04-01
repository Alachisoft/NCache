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
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Statistics
{
    [Serializable]
    public class BucketStatistics : ICompactSerializable
    {
        private long _count;
        private long _dataSize;
        private IDictionary<string, TopicStats> _topicWiseDataSize = new Dictionary<string, TopicStats>(StringComparer.InvariantCultureIgnoreCase);

        public BucketStatistics() { }

        public bool IsStateTransferStarted { get; set; }

        public long Count
        {
            get { return _count; }
            set { _count = value; }
        }

        public long DataSize
        {
            get { return _dataSize; }
            set { _dataSize = value; }
        }

        public void Increment(long dataSize)
        {
            lock (this)
            {
                _count++;
                _dataSize += dataSize;
            }
        }

        public void Decrement(long dataSize)
        {
            lock (this)
            {
                _count--;
                _dataSize -= dataSize;
            }
        }
        public void IncrementTopicStats(string topic,long dataSize)
        {
            lock (this)
            {
                TopicStats stats = null;
                if(!_topicWiseDataSize.TryGetValue(topic,out stats))
                {
                    stats = new TopicStats();
                    _topicWiseDataSize.Add(topic, stats);
                }
                stats.Count++; 
                stats.Size += dataSize;

                _count++;
                _dataSize += dataSize;
            }
           
        }

        public void DecrementTopicStats(string topic,long dataSize)
        {
            lock (this)
            {
                TopicStats stats = null;
                if (_topicWiseDataSize.TryGetValue(topic,out stats))
                {
                    stats.Count = stats.Count-1;
                    stats.Size = stats.Size - dataSize ;
                    _count--;
                    _dataSize -= dataSize;
                }
            }
        }

        public void RemoveTopic(string topic)
        {
            lock (this)
            {
                TopicStats stats = null;
                if (_topicWiseDataSize.TryGetValue(topic, out stats))
                {
                    _count -= stats.Count;
                    _dataSize -= stats.Size;
                }
            }
        }

        public void Clear()
        {
            lock (this)
            {
                _count = 0;
                _dataSize = 0;
                _topicWiseDataSize.Clear();
            }
        }

        public void SerializeLocal(CompactWriter writer)
        {
            writer.Write(_count);
            writer.Write(_dataSize);
            SerializationUtility.SerializeDictionary<string, TopicStats>(_topicWiseDataSize, writer);
        }

        public void DeserializeLocal(CompactReader reader)
        {
            _count = reader.ReadInt64();
            _dataSize = reader.ReadInt64();
            _topicWiseDataSize = SerializationUtility.DeserializeDictionary<string, TopicStats>(reader);
        }

        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            _count = reader.ReadInt64();
            _dataSize = reader.ReadInt64();
           
            _topicWiseDataSize = SerializationUtility.DeserializeDictionary<string, TopicStats>(reader);
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            writer.Write(_count);
            writer.Write(_dataSize);
            SerializationUtility.SerializeDictionary<string, TopicStats>(_topicWiseDataSize, writer);
        }

        #endregion


        internal class TopicStats:ICompactSerializable
        {
            public long Size { get; set; }
            public long Count { get; set; }

            public void Deserialize(CompactReader reader)
            {
                Size = reader.ReadInt64();
                Count = reader.ReadInt64();
            }

            public void Serialize(CompactWriter writer)
            {
                writer.Write(Size);
                writer.Write(Count);
            }
        }
    }
}