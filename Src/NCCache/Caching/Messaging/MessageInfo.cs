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
using System;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Messaging
{
    public class MessageInfo : ICompactSerializable
    {
        private string _messageId;
        private string _topic;
        private DeliveryOption _deliveryOption;
        private bool _notifyOnDeliveryFailure;

        public string MessageId
        {
            get { return _messageId; }
            set { _messageId = value; }
        }
        public string Topic
        {
            get { return _topic; }
            set { _topic = value; }
        }
        public DeliveryOption DeliveryOption
        {
            get { return _deliveryOption; }
            set { _deliveryOption = value; }
        }

        public bool NotifyOnDeliveryFailure
        {
            get { return _notifyOnDeliveryFailure; }
            set { _notifyOnDeliveryFailure = value; }
        }

        public bool IsEventMessage { get; internal set; }

        public void Deserialize(CompactReader reader)
        {
            _messageId = reader.ReadObject() as string;
            _topic = reader.ReadObject() as string;
            _deliveryOption = (DeliveryOption)reader.ReadByte();
            _notifyOnDeliveryFailure = reader.ReadBoolean();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_messageId);
            writer.WriteObject(_topic);
            writer.Write((byte)_deliveryOption);
            writer.Write(_notifyOnDeliveryFailure);

        }

        public override bool Equals(object obj)
        {
            if(obj is MessageInfo)
            {
                return String.Compare(((MessageInfo)obj).MessageId, this.MessageId, true) == 0;
            }

            return false;
        }

        public override int GetHashCode()
        {
            if (MessageId != null) return MessageId.GetHashCode();
            return 0;
        }
    }
}