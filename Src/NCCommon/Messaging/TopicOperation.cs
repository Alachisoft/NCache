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
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common
{
    public class TopicOperation : ICompactSerializable
    {
        private string _topic;
        private TopicOperationType _topicOperationType;

        public string Topic
        {
            get { return _topic; }
            set { _topic = value; }
        }

        public TopicOperationType TopicOperationType
        {
            get { return _topicOperationType; }
            set { _topicOperationType = value; }
        }

        public TopicOperation(string topic, TopicOperationType topicOperationType)
        {
            _topic = topic;
            _topicOperationType = topicOperationType;
        }

        public virtual void Deserialize(CompactReader reader)
        {
            _topicOperationType = (TopicOperationType)reader.ReadInt32();
            _topic = reader.ReadObject() as string;
        }

        public virtual void Serialize(CompactWriter writer)
        {
            writer.Write((int)_topicOperationType);
            writer.WriteObject(_topic);
        }
    }
}
