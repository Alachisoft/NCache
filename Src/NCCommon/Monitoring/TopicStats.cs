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
using Alachisoft.NCache.Runtime.Serialization;
using System;

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public class TopicStats : ICompactSerializable, ICloneable
	{
		private long _currentMessageCount;
        private  int _subscriberCount;
        private  int _publisherCount;
        private int _durableSharedSubCount;
        private int _durableExclusiveSubCount;
        private int _nonDurableSubCount;
        private string  _topicName;

        public long CurrentMessageCount
        {
            get
            {
                return _currentMessageCount;
            }

            set
            {
                _currentMessageCount = value;
            }
        }

        public int SubscriberCount
        {
            get
            {
                return _subscriberCount;
            }

            set
            {
                _subscriberCount = value;
            }
        }

        public int PublisherCount
        {
            get
            {
                return _publisherCount;
            }

            set
            {
                _publisherCount = value;
            }
        }

        public string TopicName
        {
            get
            {
                return _topicName;
            }

            set
            {
                _topicName = value;
            }
        }
        public int DurableSharedCount
        {

            get { return _durableSharedSubCount; }
            set { _durableSharedSubCount = value; }
        }
        public int DurableExclusiveCount
        {
            get { return _durableExclusiveSubCount; }
            set { _durableExclusiveSubCount = value; }
        }
        public int NonDurableSubCount
        {
            get { return _nonDurableSubCount; }
            set { _nonDurableSubCount = value; }
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
	    {
            _topicName = reader.ReadObject() as string;
            _currentMessageCount = reader.ReadInt64();
	        _subscriberCount = reader.ReadInt32();
            _publisherCount = reader.ReadInt32();
            _durableSharedSubCount = reader.ReadInt32();
            _durableExclusiveSubCount = reader.ReadInt32();
            _nonDurableSubCount = reader.ReadInt32();
        }

	    public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_topicName);
            writer.Write(_currentMessageCount);
            writer.Write(_subscriberCount);
            writer.Write(_publisherCount);
            writer.Write(_durableSharedSubCount);
            writer.Write(_durableExclusiveSubCount);
            writer.Write(_nonDurableSubCount);
        }


        #endregion


        public object Clone()
        {
            TopicStats topicStat = new TopicStats();
            topicStat._topicName = _topicName;
            topicStat._currentMessageCount = _currentMessageCount;
            topicStat._subscriberCount = _subscriberCount;
            topicStat._publisherCount = _publisherCount;
            topicStat._durableSharedSubCount = _durableSharedSubCount;
            topicStat._durableExclusiveSubCount = _durableExclusiveSubCount;
            topicStat._nonDurableSubCount = _nonDurableSubCount;
            return topicStat;
        }


    }
}
