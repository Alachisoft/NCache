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

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Operations.Messaging
{
    public class GetAssignedMessagesOperation : ICompactSerializable
    {
        SubscriptionInfo _subscriptionInfo;
        OperationContext _operationContext;

        public SubscriptionInfo SubscriptionInfo
        {
            get { return _subscriptionInfo; }
        }

        public OperationContext OperationContext
        {
            get { return _operationContext; }
        }

        public GetAssignedMessagesOperation(SubscriptionInfo subscriptionInfo,
            OperationContext context)
        {
            _subscriptionInfo = subscriptionInfo;
            _operationContext = context;
        }

        public void Deserialize(CompactReader reader)
        {
            _subscriptionInfo = reader.ReadObject() as SubscriptionInfo;
            _operationContext = reader.ReadObject() as OperationContext;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_subscriptionInfo);
            writer.WriteObject(_operationContext);
        }
    }
}
