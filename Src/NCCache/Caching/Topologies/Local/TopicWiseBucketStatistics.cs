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

using System;
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    class TopicWiseBucketStatistics :ISizable
    {
        private string _topic;
        private HashVector _messageIds = new HashVector(StringComparer.InvariantCultureIgnoreCase);
        private int _size;

        public int Size
        {
            get
            {
                return InMemorySize;
            }
        }

        public int InMemorySize
        {
            get
            {
                return _size;
            }
        }

        public TopicWiseBucketStatistics(string topic)
        {
            _topic = topic;
            _size = _messageIds.BucketCount * MemoryUtil.NetHashtableOverHead + (_topic.Length * MemoryUtil.NetStringCharSize);
        }

        public void AddMessage(string messageId)
        {
            if(!_messageIds.ContainsKey(messageId))
            {
                int preUpdateBuckets = _messageIds.BucketCount;
                _messageIds.Add(messageId,null);

                int postUpdateBuckets = _messageIds.BucketCount;
                if (postUpdateBuckets != preUpdateBuckets)
                {
                    _size += (postUpdateBuckets - preUpdateBuckets) * MemoryUtil.NetHashtableOverHead;
                }

            }
        }

        public bool Remove(string messageId)
        {
            if (_messageIds.ContainsKey(messageId))
            {
                int preUpdateBuckets = _messageIds.BucketCount;
                _messageIds.Remove(messageId);

                int postUpdateBuckets = _messageIds.BucketCount;
                if (postUpdateBuckets != preUpdateBuckets)
                {
                    _size -= (preUpdateBuckets - postUpdateBuckets) * MemoryUtil.NetHashtableOverHead;
                }
                return _messageIds.Count == 0;
            }

            return false;
        }

        public IList GetMessageIds()
        {
            return new ClusteredArrayList(_messageIds.Keys);
        }

    }
}