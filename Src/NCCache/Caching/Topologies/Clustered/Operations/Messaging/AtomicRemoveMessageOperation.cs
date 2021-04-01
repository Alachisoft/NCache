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
    internal class AtomicRemoveMessageOperation : ICompactSerializable
    {
        private MessageInfo _message;
        private MessageRemovedReason _reason = MessageRemovedReason.Delivered;
        private OperationContext _context;

        public MessageInfo MessagesToRemove
        {
            get { return _message; }
            set { _message = value; }
        }

        public MessageRemovedReason Reason
        {
            get { return _reason; }
            set { _reason = value; }
        }

        public OperationContext Context
        {
            get { return _context; }
            set { _context = value; }
        }

        public AtomicRemoveMessageOperation(MessageInfo messageTobeRemoved, MessageRemovedReason reason,OperationContext context)
        {
            _message = messageTobeRemoved;
            _reason = reason;
            _context = context;
        }

        public void Deserialize(CompactReader reader)
        {
            _message = reader.ReadObject() as MessageInfo;
            _reason = (MessageRemovedReason)reader.ReadInt32();
            _context = reader.ReadObject() as OperationContext;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_message);
            writer.Write((int)_reason);
            writer.WriteObject(_context);
        }

        public override string ToString()
        {
            string builder = string.Empty;
            builder+= " Topic Name: " + _message.Topic + " Message ID: " + _message.MessageId + " Remove Reason : " + _reason.ToString();
            return builder;
        }
    }
}