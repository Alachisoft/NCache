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
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Operations
{
    class RemoveMessagesOperation : ICompactSerializable
    {
        private IList<MessageInfo> _messagesTobeRemoved= new List<MessageInfo>();
        private MessageRemovedReason _reason = MessageRemovedReason.Delivered;
        private OperationContext _context;

        public IList<MessageInfo> MessagesToRemove
        {
            get { return _messagesTobeRemoved; }
        }

        public MessageRemovedReason Reason
        {
            get { return _reason; }
        }

        public OperationContext Context { get { return _context; } set { _context = value; } }

        public RemoveMessagesOperation(IList<MessageInfo> messagesTobeRemoved,MessageRemovedReason reason,OperationContext context)
        {
            _messagesTobeRemoved = messagesTobeRemoved;
            _reason = reason;
            _context = context;
        }

        public void Deserialize(CompactReader reader)
        {
            _messagesTobeRemoved= SerializationUtility.DeserializeList<MessageInfo>(reader);
            _reason = (MessageRemovedReason)reader.ReadInt32();
            _context = reader.ReadObject() as OperationContext;
        }

        public void Serialize(CompactWriter writer)
        {
            SerializationUtility.SerializeList(_messagesTobeRemoved, writer);
            writer.Write((int)_reason);
            writer.WriteObject(_context);
        }

        public override string ToString()
        {
            string builder = string.Empty;
            foreach (var messageInfo in _messagesTobeRemoved)
            {
                builder += " Topic Name: " + messageInfo.Topic + " Message ID: " + messageInfo.MessageId + " Remove Reason : " + _reason.ToString();
            }
            return builder;
        }
    }
}
