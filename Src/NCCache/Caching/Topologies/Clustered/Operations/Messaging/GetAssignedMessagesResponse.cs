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
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Caching.Messaging;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Operations.Messaging
{
    public class GetAssignedMessagesResponse : ICompactSerializable
    {
        private MessageResponse messageResponse;

        public GetAssignedMessagesResponse()
        {
            Initialize();
        }

        public MessageResponse MessageResponse
        {
            get { return messageResponse; }
            set { messageResponse = value; }
        }


        public void Merge(GetAssignedMessagesResponse other)
        {
            foreach (KeyValuePair<string, IList<object>> pair in other.messageResponse.AssignedMessages)
            {

                if (!messageResponse.AssignedMessages.ContainsKey(pair.Key))
                    messageResponse.AssignedMessages.Add(pair.Key, pair.Value);
                else
                {
                    IList<object> messages = messageResponse.AssignedMessages[pair.Key];
                    foreach (var message in pair.Value)
                        messages.Add(message);
                }
            }
        }

        public void Deserialize(CompactReader reader)
        {
            Initialize();
            messageResponse.AssignedMessages = SerializationUtility.DeserializeDictionaryList<string, object>(reader);
         
        }

        public void Serialize(CompactWriter writer)
        {
            SerializationUtility.SerializeDictionaryList(messageResponse.AssignedMessages, writer);
           
        }

        private void Initialize()
        {
            messageResponse = new MessageResponse();
        }
    }
}
