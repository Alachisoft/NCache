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
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Operations
{
    public class ClusterTopicOperation : ICompactSerializable
    {
        private TopicOperation _topicOperation;
        private OperationContext _operationContext;

        public TopicOperation TopicOperation
        {
            get { return _topicOperation; }
        }

        public OperationContext OperationContext
        {
            get { return _operationContext; }
        }

        public ClusterTopicOperation(TopicOperation topicOperation, OperationContext operationContext)
        {
            _topicOperation = topicOperation;
            _operationContext = operationContext;
        }

        public void Deserialize(CompactReader reader)
        {
            _topicOperation = reader.ReadObject() as TopicOperation;
            _operationContext = reader.ReadObject() as OperationContext;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_topicOperation);
            writer.WriteObject(_operationContext);
        }
    }
}
