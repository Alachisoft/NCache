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
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Operations
{
    public class StoreMessageOperation : ICompactSerializable
    {
        private string _topic;
        private Message _message;
        private OperationContext _context;

        public string Topic
        {
            get { return _topic; }
        }

        public Message Message
        {
            get { return _message; }
        }

        public OperationContext Context
        {
            get { return _context; }
        }


        public StoreMessageOperation(string topic, Message message, OperationContext context)
        {
            _topic = topic;
            _message = message;
            _context = context;
        }

        public void Deserialize(CompactReader reader)
        {
            _topic = reader.ReadObject() as string;
            _message = reader.ReadObject() as Message;
            _context = reader.ReadObject() as OperationContext;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_topic);
            writer.WriteObject(_message);
            writer.WriteObject(_context);
        }

        public override string ToString()
        {
            return " Topic Name: " + _topic + " Message ID: " + _message.MessageId ;
        }
    }
}
