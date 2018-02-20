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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    class BucketStatistcs :ISizable
    {
        private int _bucketId;
        private HashVector _cacheKeys = new HashVector();
        private HashVector _topicWiseMessageIds = new HashVector(StringComparer.InvariantCultureIgnoreCase);
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
                return 0;
            }
        }

        public BucketStatistcs(int bucketId)
        {
            _bucketId = bucketId;
            _size = _cacheKeys.BucketCount* MemoryUtil.NetHashtableOverHead;
            _size += _topicWiseMessageIds.BucketCount * MemoryUtil.NetHashtableOverHead;
        }

        public void AddCacheKey(string cacheKey)
        {
            if (!_cacheKeys.ContainsKey(cacheKey))
            {
                int preUpdateBuckets = _cacheKeys.BucketCount;
                _cacheKeys.Add(cacheKey, null);
                int postUpdateBuckets = _cacheKeys.BucketCount;

                if(postUpdateBuckets != preUpdateBuckets)
                {
                    _size += (postUpdateBuckets - preUpdateBuckets) * MemoryUtil.NetHashtableOverHead;
                }
            }
        }

        public void RemoveCacheKey(string cacheKey)
        {
            if (_cacheKeys.ContainsKey(cacheKey))
            {
                int preUpdateBuckets = _cacheKeys.BucketCount;
                _cacheKeys.Remove(cacheKey);

                int postUpdateBuckets = _cacheKeys.BucketCount;
                if (postUpdateBuckets != preUpdateBuckets)
                {
                    _size -= (preUpdateBuckets - postUpdateBuckets) * MemoryUtil.NetHashtableOverHead;
                }
            }
        }

        public void AddMessage(string topic, string messageId)
        {
            TopicWiseBucketStatistics topicStats = _topicWiseMessageIds[topic] as TopicWiseBucketStatistics;
            if (topicStats == null)
            {
                topicStats = new TopicWiseBucketStatistics(topic);
                _topicWiseMessageIds.Add(topic, topicStats);
            }

            int preUpdateSize = topicStats.InMemorySize;
            topicStats.AddMessage(messageId);
            int postUpdateSize = topicStats.InMemorySize;

            if(preUpdateSize != postUpdateSize)
            {
                _size += postUpdateSize - preUpdateSize;
            }
        }

        public bool RemoveMessage(string topic, string messageId)
        {
            TopicWiseBucketStatistics topicStats = _topicWiseMessageIds[topic] as TopicWiseBucketStatistics;
            if (topicStats != null)
            {
                int preUpdateSize = topicStats.InMemorySize;

                if (!topicStats.Remove(messageId))
                {
                    int postUpdateSize = topicStats.InMemorySize;

                    if (preUpdateSize != postUpdateSize)
                    {
                        _size -= preUpdateSize - postUpdateSize;
                    }
                }
                else
                {
                    _topicWiseMessageIds.Remove(topic);
                    _size -= preUpdateSize;
                    return true;
                }
            }
            return false;
        }

        public bool RemoveTopic(string topic)
        {
            TopicWiseBucketStatistics topicStats = _topicWiseMessageIds[topic] as TopicWiseBucketStatistics;
            if (topicStats != null)
            {
                _size -= topicStats.InMemorySize;
                _topicWiseMessageIds.Remove(topic);
                return true;
            }
            return false;
        }

        public OrderedDictionary GetTopicWiseMessagIds()
        {
            OrderedDictionary topicWiseMessages = new OrderedDictionary();

            foreach(string topic in _topicWiseMessageIds.Keys)
            {
                TopicWiseBucketStatistics topicStats = _topicWiseMessageIds[topic] as TopicWiseBucketStatistics;

                if (topicStats != null)
                    topicWiseMessages.Add(topic, topicStats.GetMessageIds());
            }

            return topicWiseMessages;
        }

        internal ClusteredArrayList GetCacheKeys()
        {
            ClusteredArrayList keys = new ClusteredArrayList();
            keys.AddRange(_cacheKeys.Keys);

            return keys;
        }
    }
}